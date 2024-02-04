
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;
using System;
using System.Net.NetworkInformation;
using System.Linq;
using System.IO;
using System.Text;

public class ConnectionManager : MonoBehaviour
{
    public string _IPAddress = "127.0.0.1";
    public PacketParser parser;
    private TcpClient socket;
    private NetworkStream stream;
    private byte[] recvBuffer;
    public static int dataBufferSize = 1024;
    public bool connected;
    ByteArrayComparer comparer = new ByteArrayComparer();
    public ClientHandle client_self = new ClientHandle();
    [SerializeField]
    public List<ClientHandle> clients = new List<ClientHandle>();
    private LobbyManager lobbyManager;
    bool inLobby = false;
    private PlayerInfo[] players;
    private event Action mainThreadQueuedCallbacks;
    private event Action eventsClone;
    private UDPHandler udp_handler;


    private void OnDisable()
    {
        Disconnect();
    }
    public void PrintByteArray(byte[] bytes)
    {
        var sb = new StringBuilder("new byte[] { ");
        foreach (var b in bytes)
        {
            sb.Append(b + ", ");
        }
        sb.Append("}");
        Debug.Log(sb.ToString());
    }

    public void SendVoiceData(byte[] vo_packet)
    {
        //Debug.Log($"Sending {vo_packet.Length} bytes of voice data");
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.voice;
        packet.AddToPayload(vo_packet);
        packet.Send(udp_handler.client, udp_handler.remoteEndPoint);
    }

    private void Connect(IPAddress ip, int port = 1338)
    {
        
        socket = new TcpClient
        {
            ReceiveBufferSize = 2048,
            SendBufferSize = 2048
        };
        Debug.Log("Connecting...");
        socket.BeginConnect(ip, port, ConnectCallback, socket);
    }
    private void Disconnect()
    {
        if (socket.Connected)
        {
            connected = false;
            socket.Close();
            socket.Dispose();
            Debug.Log("Disconnected");
        }

    }
    private void CheckStatus()
    {

    }
    private void SendRawData(byte[] data)
    {
        Debug.Log("Sending data...");
        if(socket == null)
        {
            Debug.Log("Sending failed! (socket is null)");
            return;
        }

        stream.BeginWrite(data, 0, data.Length, null, null);
    }
    private void ConnectCallback(IAsyncResult asyncResult)
    {
        if (!socket.Connected)
        {
            Debug.Log("Could not connect!");

            return;
        }
        Debug.Log("Connected!");
        connected = true;
        //Debug.Log("Starting watchdog");

        //StartCoroutine(watchdog());

        socket.EndConnect(asyncResult);

        stream = socket.GetStream();
        recvBuffer = new byte[dataBufferSize];
        


        stream.BeginRead(recvBuffer, 0, dataBufferSize, ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult asyncResult)
    {
        if (!socket.Connected)
            return;
        try
        {

            int byteLength = stream.EndRead(asyncResult);
            if (byteLength <= 0)
            {
                Debug.Log("Disconnected!");
                return;
            }
            // Transfer data from receiveBuffer to data variable for handling
            byte[] data = new byte[byteLength];
            Array.Copy(recvBuffer, data, byteLength);

            HandleData(data);

            stream.BeginRead(recvBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving TCP data: {e.GetType()} {e}");
        }
    }
    public void DisconnectFromServer()
    {
        Disconnect();
    }


    public void ConnectToServer()
    {
        client_self.name = $"PlayerName{UnityEngine.Random.Range(0, 25565)}";
        Connect(IPAddress.Parse(_IPAddress));
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        lobbyManager = FindObjectOfType<LobbyManager>();

        parser = new PacketParser();

        udp_handler = FindObjectOfType<UDPHandler>();
        
        parser.RegisterHeaderProcessor(Headers.ack, ParseACK);
        parser.RegisterHeaderProcessor(Headers.echo, ParseECHO);
        parser.RegisterHeaderProcessor(Headers.hello, HelloFromServer);
        parser.RegisterHeaderProcessor(Headers.data, ParseData);
        parser.RegisterHeaderProcessor(Headers.rejected, ParseData);
        parser.RegisterHeaderProcessor(Headers.disconnecting, ParseDisconnect);
    }
    
    public void HandleData(byte[] _data)
    {
        parser.DigestMessage(_data);
    }

    private void ParseRejection(Packet packet)
    //friendzone :c
    {
        if (packet.flag[0] == Flags.Response.closing_con[0])
        {
            FindObjectOfType<MapLoader>().ReturnToMenu();
            Disconnect();
            return;
        }
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            string error = reader.ReadString(); //TODO: Display the error in the game
            Debug.LogError($"REMOTE ERROR: {error}");
        }
    }
    private void ParseData(Packet packet)
    {
        switch (packet.flag[0])
        {
            case var _ when packet.flag[0] == Flags.Response.idAssign[0]:
                ParseIDAssign(packet);
                break;
            case var _ when packet.flag[0] == Flags.Response.playerList[0]:
                ParsePlayerList(packet);
                break;
            case var _ when packet.flag[0] == Flags.Response.lobbyList[0]:
                ParseLobbyList(packet);
                break;
            case var _ when packet.flag[0] == Flags.Response.voice[0]:
                ParseVoiceData(packet);
                break;

            //-----------------------------JSON DATA-----------------------------
            case var _ when packet.flag[0] == Flags.Response.lobbyInfo[0]:
                ParseLobbyInfo(packet);
                break;
            case var _ when packet.flag[0] == Flags.Response.transformData[0]:
                ParseTransformData(packet);
                break;
        }

    }
    #region Assembling and Sending Packets
    public void JoinLobby(int id,string password="")
    {
        Debug.Log($"Joining lobby {id}");
        Packet join_packet = new Packet();
        join_packet.header = Headers.data;
        join_packet.flag = Flags.Post.joinLobby;
        join_packet.AddToPayload(id);
        join_packet.AddToPayload(password);
        join_packet.Send(stream);
    }

    public void SendLocationInfo(Player player)
    {
        Transforms transforms_ = new Transforms();
        transforms_.position = player.transform.position;
        transforms_.target_velocity = player.movement.MoveDirection;
        transforms_.real_velocity = player.rb.velocity;
        transforms_.rotation = player.movement.GetAngles();

        PlayerData playerData = new PlayerData();
        playerData.Inputs = new Inputs();
        playerData.Inputs.isCrouching = player.movement.isCrouching;
        playerData.Inputs.isSprinting = player.movement.isSprinting;
        playerData.Inputs.isMoving = player.movement.isMoving;
        playerData.id = player.playerInfo.id;
        playerData.transforms = transforms_;

        string mes = JsonUtility.ToJson(playerData);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.transformData;
        packet.AddToPayload(mes);
        packet.Send(udp_handler.client,udp_handler.remoteEndPoint);
    }
    #endregion

    #region Parsing Incoming Packets

    #region Non-json Data
    private void ParseVoiceData(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int player_id = reader.ReadInt32();
            ClientHandle cl = clients.FirstOrDefault(x => x.id == player_id);
            byte[] _pay = packet.payload;

            cl.connectedPlayer.voice.ReceiveAudioData(packet.payload.Skip(4).ToArray());

        }
    }
    private void ParseLobbyList(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int amount = reader.ReadInt32();
            Debug.Log($"Lobby amount: {amount}");
            if (amount < 1)
                return;
            for(int i=0; i < amount; i++)
            {

                int lobby_id = reader.ReadInt32();
                int stringLength = reader.ReadInt32();
                byte[] stringData = reader.ReadBytes(stringLength);
                string lobbyName = Encoding.UTF8.GetString(stringData);
                bool protected_ = reader.ReadBoolean();
                int current_players = reader.ReadInt32();
                int max_players = reader.ReadInt32();
                Debug.Log($"Lobby {i} name: {lobbyName}");
                lobbyManager.Clear();
                lobbyManager.AddLobbyToUI(lobby_id, lobbyName, max_players, current_players, protected_, this);
            }
        }
    }


    private void ParseIDAssign(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int id = reader.ReadInt32();
            client_self.id = id;
            Packet imHere = new Packet();
            imHere.header = Headers.imHere;
            imHere.flag = Flags.none;
            imHere.AddToPayload(client_self.id);
            imHere.Send(udp_handler.client,udp_handler.remoteEndPoint);

        }
    }
    private void ParsePlayerList(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int playerCount = reader.ReadInt32();
            for(int i = 0; i > playerCount; i++)
            {
                ClientHandle client = new ClientHandle();
                client.id = reader.ReadInt32();
                client.name = reader.ReadString();
                //clients.Add(client);
            }
        }
    }
    private void ParseDisconnect(Packet packet)
    {
        Debug.Log("Disconnected by server!");
        connected = false;
    }
    private void ParseACK(Packet packet)
    {
    }
    private void ParseECHO(Packet packet)
    {
        Packet ackp = new Packet();
        ackp.header = Headers.echo;
        ackp.flag = Flags.none;
        ackp.Send(stream);


    }

    private void HelloFromServer(Packet packet)
    {
        Packet hello = new Packet();
        hello.header = Headers.hello;
        hello.flag = Flags.none;
        hello.AddToPayload(client_self.name); //TODO: Set up player name choosing system (add to packets!!!)
        hello.Send(stream);

        Packet lobby_list_request = new Packet();
        lobby_list_request.header = Headers.data;
        lobby_list_request.flag = Flags.Request.lobbyList;
        lobby_list_request.Send(stream);
    }
    #endregion

    #region Json data
    private void ParseLobbyInfo(Packet packet)
    {
        Debug.Log("Got lobby info");
        ThreadManager.ExecuteOnMainThread(() =>
        {
            MapInfo mapInfo = packet.GetJson<MapInfo>();
            if (mapInfo == null)
                return;
            if (FindObjectOfType<MapLoader>().lobbyLoading) //I HATE MYSELF
                return;                                     //TODO: do something with this shitty ass hack

            if (FindObjectOfType<MapLoader>().CurrentMapManager == null)
            {
                FindObjectOfType<MapLoader>().LoadMap(mapInfo);
            }
            else
                FindObjectOfType<MapLoader>().UpdateMap(mapInfo);

            players = mapInfo.players;
        });


    }

    private void ParseTransformData(Packet packet)
    {
     
        ThreadManager.ExecuteOnMainThread(() =>
        { 

            PlayersDataPacket json_ = packet.GetJson<PlayersDataPacket>();
            foreach (PlayerData player in json_.players) // for each player in recieved json
            {
                if (player.id == client_self.id)
                    continue;
                
                ClientHandle matchingPlayer = clients.FirstOrDefault(x => x.id == player.id); //find the connected local player by id

                if (matchingPlayer != null)
                {
                    ThreadManager.ExecuteOnMainThread(() => //apply all the positions
                    {
                        matchingPlayer.connectedPlayer.real_velocity = player.transforms.real_velocity;
                        matchingPlayer.connectedPlayer.target_velocity = player.transforms.target_velocity;
                        matchingPlayer.connectedPlayer.position = player.transforms.position;
                        matchingPlayer.connectedPlayer.rotation = player.transforms.rotation;
                        matchingPlayer.connectedPlayer.lastTime = Time.realtimeSinceStartup;
                        matchingPlayer.connectedPlayer.movement.col.height = player.Inputs.isCrouching ? 2f : 0.8f; //TODO: make so the movement script handles movement. Split the movement script into local side and a remote side.
                    });
                }

            }
        });
    }
    #endregion

    #endregion


    IEnumerator watchdog()
    {
        while (connected)
        {
            Debug.Log("checking connection status...");

            CheckStatus();
            yield return new WaitForSeconds(1);
        }
    }

    private void Update()
    {
        if (mainThreadQueuedCallbacks != null)
        {
            eventsClone = mainThreadQueuedCallbacks;
            mainThreadQueuedCallbacks = null;
            eventsClone.Invoke();
            eventsClone = null;
        }
    }
}

