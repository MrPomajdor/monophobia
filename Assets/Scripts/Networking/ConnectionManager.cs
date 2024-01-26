
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
    public PacketParser parser;
    private TcpClient socket;
    private NetworkStream stream;
    private byte[] recvBuffer;
    public static int dataBufferSize = 1024;
    public bool connected;
    ByteArrayComparer comparer = new ByteArrayComparer();
    public ClientHandle client_self = new ClientHandle();
    public List<ClientHandle> clients = new List<ClientHandle>();
    private LobbyManager lobbyManager;
    bool inLobby = false;
    private PlayerInfo[] players;
    private event Action mainThreadQueuedCallbacks;
    private event Action eventsClone;
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
            Debug.LogError($"Error receiving TCP data: {e.Message} {e.StackTrace}");
        }
    }
    public void DisconnectFromServer()
    {
        Disconnect();
    }
    public void ConnectToServer()
    {
        Connect(IPAddress.Parse("127.0.0.1"));
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        lobbyManager = FindObjectOfType<LobbyManager>();

        parser = new PacketParser();

        parser.RegisterHeaderProcessor(Headers.ack, ParseACK);
        parser.RegisterHeaderProcessor(Headers.echo, ParseECHO);
        parser.RegisterHeaderProcessor(Headers.hello, HelloFromServer);
        parser.RegisterHeaderProcessor(Headers.data, ParseData);
        parser.RegisterHeaderProcessor(Headers.disconnecting, ParseDisconnect);
    }
    
    private void HandleData(byte[] _data)
    {
        parser.DigestMessage(_data);
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
            case var _ when packet.flag[0] == Flags.Response.lobbyInfo[0]:
                ParseLobbyInfo(packet);
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
        PrintByteArray(join_packet.payload);
        join_packet.AddToPayload(id);
        PrintByteArray(join_packet.payload);
        join_packet.AddToPayload(password);
        PrintByteArray(join_packet.payload);
        join_packet.Send(stream);
    }
    #endregion

    #region Parsing Incoming Packets
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


    private void ParseLobbyInfo(Packet packet) //TODO: Add map handling and lobby handling
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int stringLength = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes(stringLength);
            string json = Encoding.UTF8.GetString(stringData);
            ThreadManager.ExecuteOnMainThread(() =>
            {
                MapInfo mapInfo = JsonUtility.FromJson<MapInfo>(json);
                if (inLobby)
                {

                }
                else
                {
                    FindObjectOfType<MapLoader>().LoadMap(mapInfo);
                }
                players = mapInfo.players;
            });

        }
    }


    private void ParseIDAssign(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int id = reader.ReadInt32();
            client_self.id = id;

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
                clients.Add(client);
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
        Debug.Log("Ookay nice.");
    }
    private void ParseECHO(Packet packet)
    {
        Debug.Log("ooh an echo!  responding.");
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
        hello.AddToPayload("PlayerName123"); //TODO: Set up player name choosing system (add to packets!!!)
        hello.Send(stream);

        Packet lobby_list_request = new Packet();
        lobby_list_request.header = Headers.data;
        lobby_list_request.flag = Flags.Request.lobbyList;
        lobby_list_request.Send(stream);
    }

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

