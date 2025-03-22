
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
#if UNITY_EDITOR
using UnityEditor.Sprites;
#endif
using UnityEngine;

public static class Global
{
    public static ConnectionManager connectionManager;
    public static Chat chat;

}
public static class Tools
{
    public static void UpdatePos(Transform transform, Rigidbody rb, Transforms transforms, Player player = null, Inputs inputs = null, float smoothingFactor = 0.5f)
    {

        //Velocity
        rb.velocity = transforms.real_velocity;
        rb.velocity += transforms.position - transform.position;

        //angular velocity


        //position
        if (Vector3.Distance(transform.position, transforms.position) > 2)
            transform.position = transforms.position;
        if (!player)
        {
            rb.angularVelocity = transforms.real_angular_velocity;
            float rot_diff = Quaternion.Angle(transform.rotation, Quaternion.Euler(transforms.rotation.x, transforms.rotation.y, transforms.rotation.z));
            if (rot_diff > 10)
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transforms.rotation.x, transforms.rotation.y, transforms.rotation.z), smoothingFactor * Time.deltaTime);
            if (rot_diff > 25)
                transform.rotation = Quaternion.Euler(transforms.rotation.x, transforms.rotation.y, transforms.rotation.z);
            //else
            //{

            //    rb.angularVelocity += transform.rotation.eulerAngles - transforms.rotation;
            //}
            //    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transforms.rotation.x, transforms.rotation.y, transforms.rotation.z), smoothingFactor * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, transforms.rotation.y, 0), smoothingFactor * Time.deltaTime);
            player.cam.transform.rotation = Quaternion.Slerp(player.cam.transform.rotation, Quaternion.Euler(transforms.rotation.x, transforms.rotation.y, 0), smoothingFactor * Time.deltaTime);
            rb.AddForce(inputs.MoveDirection * 500 * Time.deltaTime, ForceMode.Force);
        }

    }

    public static bool Difference(Transforms a, Transforms b)
    {
        return Vector3.Distance(a.position, b.position) > 2 || Quaternion.Angle(Quaternion.Euler(a.rotation.x, a.rotation.y, a.rotation.z), Quaternion.Euler(b.rotation.x, b.rotation.y, b.rotation.z)) >= 5;
    }

    public static bool Difference(Inputs a, Inputs b)
    {
        return (a.isCrouching != b.isCrouching || a.isMoving != b.isMoving || a.isSprinting != b.isSprinting);
    }
}
[Serializable]
public class WorldState
{
    public List<NetworkItemStruct> items = new List<NetworkItemStruct>();
    //public List<InteractableState> interactables = new List<InteractableState> ();
    //TODO: Add mobs
}

public class ConnectionManager : MonoBehaviour
{

    public WorldState worldState = new WorldState();
    public List<Item> items = new List<Item>();

    public string _IPAddress = "127.0.0.1";
    public PacketParser parser;
    private TcpClient socket;
    public NetworkStream stream { get; private set; }
    private byte[] recvBuffer;
    public static int dataBufferSize = 1024;
    public bool connected;
    ByteArrayComparer comparer = new ByteArrayComparer();
    public ClientHandle client_self = new ClientHandle();
    [SerializeField]
    public List<ClientHandle> clients = new List<ClientHandle>();
    public UI_LobbyManager lobbyManager;
    public PlayerInfo[] players;
    private event Action mainThreadQueuedCallbacks;
    private event Action eventsClone;
    public UDPHandler udp_handler { get; private set; }
    private MenuManager menuManager;
    public MapLoader mapLoader;


    private Dictionary<byte, List<Action<Packet>>> ReceiversMap = new Dictionary<byte, List<Action<Packet>>>(); //looks wierd xd
    private Dictionary<byte[], Action<Packet>> PacketResponseCallbackMap = new Dictionary<byte[], Action<Packet>>();

    private Dictionary<string, bool> fragmentsReceivedMap = new Dictionary<string, bool>();

    private Defragmentator defragmentator = new Defragmentator();

    private string AppVersion;
    private Queue<Action> LocalPlayerActions = new Queue<Action>();

    private float lastEchoTime;
    public float Ping { get; private set; }
    //public MumbleManager mumbleManager { get; private set; }

    /// <summary>Adds an action to received flag</summary>
    /// <param name="action">The action to be executed</param>
    /// <param name="flag">corresponding flag</param>
    public void RegisterFlagReceiver(byte flag, Action<Packet> action)
    {

        //Debug.Log($"Registering action for flag {flag.ToString("X")} ");
        if (ReceiversMap.ContainsKey(flag))
            ReceiversMap[flag].Add(action);
        else
        {
            ReceiversMap.Add(flag, new List<Action<Packet>>());
            ReceiversMap[flag].Add(action);
        }
    }

    /// <summary>Removes an action to received flag</summary>
    /// <param name="action">The action to be removed</param>
    /// <param name="flag">corresponding flag</param>
    public void UnregisterFlagReceiver(byte flag, Action<Packet> action)
    {
        if (ReceiversMap.ContainsKey(flag))
            if (ReceiversMap[flag].Contains(action))
                ReceiversMap[flag].Remove(action);
    }

    public void AddLocalPlayerAction(Action action)
    {
        LocalPlayerActions.Enqueue(action);
    }

    public bool IsSelfHost
    {
        get
        {
            float b = 0;
            while (client_self.connectedPlayer == null)
            {
                b += Time.deltaTime;
                if (b > 5) Debug.LogError("Waiting for connected player timeout");
                break;
            }

            if (client_self != null && this.client_self.connectedPlayer != null)
                return this.client_self.connectedPlayer.playerInfo.isHost;
            else return false;
        }
    }



    private void OnDisable()
    {
        if (Toolz.Tools.ApplicationIsAboutToExitPlayMode() == false)
        {
            udp_handler.Dispose();
        }
        Disconnect();
        worldState.items.Clear();

    }




    public void PrintByteArray(byte[] bytes, bool str = false)
    {
        var sb = new StringBuilder("new byte[] { ");
        foreach (var b in bytes)
        {
            if (!str)
                sb.Append(b + ", ");
            else
                sb.Append((char)b + ", ");
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
        //
        socket = new TcpClient
        {
            ReceiveBufferSize = 2048,
            SendBufferSize = 2048
        };
        Debug.Log("Connecting...");
        socket.BeginConnect(ip, port, ConnectCallback, socket);
    }
    //29.03.2024 brecause I forgot to dispose and stop everything related to networking I lost my mind.
    //I almost fucking exploded trying to find why when I enter play mode couple of times my cpu starts to fucking burn extra calories
    //(udp handler was creating a THREAD with a WHILE (TRUE) loop that I didn't stop anywhere ;-;)
    //note to self: don't code like an 8th grader
    private void Disconnect()
    {
        if (socket != null && socket.Connected)
        {
            connected = false;
            socket.Close();
            socket.Dispose();
        }

        if (stream != null)
        {
            stream.Close();
            stream.Dispose();
        }

        udp_handler.Dispose();
        Debug.Log("Disconnected");


    }
    private void CheckStatus()
    {

    }
    private void SendRawData(byte[] data)
    {
        Debug.Log("Sending data...");
        if (socket == null)
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
            //Try to connect again.
            //socket.EndConnect(asyncResult);
            //Disconnect();
            Connect(IPAddress.Parse(_IPAddress));
            return;
        }
        Debug.Log("Connected!");
        //socket.NoDelay = true;
        connected = true;

        //Debug.Log("Starting watchdog");

        //StartCoroutine(watchdog());

        socket.EndConnect(asyncResult);

        stream = socket.GetStream();
        recvBuffer = new byte[dataBufferSize];

        Packet hello = new Packet();
        hello.header = Headers.hello;
        hello.flag = Flags.none;
        hello.AddToPayload(client_self.name);
        hello.AddToPayload(client_self.steamID);
        hello.AddToPayload(AppVersion);
        hello.Send(stream);

        

        stream.BeginRead(recvBuffer, 0, dataBufferSize, ReceiveCallback, null);

        //if (SceneManager.GetActiveScene().name != "mainmenu")
        //    SceneManager.LoadScene("mainmenu", LoadSceneMode.Single);
        ThreadManager.ExecuteOnMainThread(() => { menuManager.ChangeMenu("main"); });

    }
    private void ReceiveTCP()
    {

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
            if (byteLength < 7)
            {
                Debug.LogWarning($"Message length too short! Content:\n{BitConverter.ToString(recvBuffer)}");

            }

            byte[] data = new byte[byteLength];
            Array.Copy(recvBuffer, data, byteLength);

            int msgLenth = BitConverter.ToInt32(data, 3);
            if (msgLenth > dataBufferSize)
            {
                Debug.Log($"Total len of message {msgLenth}");
                byte[] buffer = new byte[msgLenth];
                Array.Copy(data, 0, buffer, 0, byteLength);

                int totalRead = byteLength;
                int toRead = msgLenth - byteLength;

                while (toRead > 0)
                {
                    int chunkSize = Math.Min(toRead, dataBufferSize);
                    int bytesRead = stream.Read(recvBuffer, 0, chunkSize);

                    if (bytesRead <= 0) throw new IOException("Connection lost during message read.");

                    Array.Copy(recvBuffer, 0, buffer, totalRead, bytesRead);
                    totalRead += bytesRead;
                    toRead -= bytesRead;
                }

                Debug.Log($"Read full message of size {buffer.Length}");
                HandleData(buffer);
            }
            else
            {
                HandleData(data);
            }

            stream.BeginRead(recvBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }
        catch (IOException)
        {
            Debug.Log($"Disconnected by the server");
            FindObjectOfType<MapLoader>().ReturnToMenu();
            //TODO: Return to main menu if disconnected
            Disconnect();
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



    private void OnEnable()
    {
        Global.connectionManager = this;
        AppVersion = Application.version;
        // mumbleManager = GetComponent<MumbleManager>(); 
        StartCoroutine(WaitForLocalPlayer());
    }

    private void Start()
    {

        if (SteamManager.Initialized)
        {
            Debug.Log($"Steam username: {SteamFriends.GetPersonaName()}");
            client_self.steamID = SteamUser.GetSteamID().ToString();
        }
        else
        {
            Debug.LogError("Steam not initialized!");
            WindowsMessageBox.ShowMessageBox("Steam is not running! Please run Steam and launch the game again.", "Error", 16);
            Application.Quit();

        }
        DontDestroyOnLoad(gameObject);
        menuManager = FindAnyObjectByType<MenuManager>();
        //lobbyManager = FindAnyObjectByType<UI_LobbyManager>();

        parser = new PacketParser();

        udp_handler = FindObjectOfType<UDPHandler>();
        mapLoader = FindObjectOfType<MapLoader>();

        parser.RegisterHeaderProcessor(Headers.ack, ParseACK);
        parser.RegisterHeaderProcessor(Headers.echo, ParseECHO);
        //parser.RegisterHeaderProcessor(Headers.hello, HelloFromServer);
        parser.RegisterHeaderProcessor(Headers.data, ParseData);
        parser.RegisterHeaderProcessor(Headers.rejected, ParseRejection);
        parser.RegisterHeaderProcessor(Headers.disconnecting, ParseDisconnect);

        RegisterFlagReceiver(Flags.Response.idAssign[0], ParseIDAssign);
        RegisterFlagReceiver(Flags.Response.lobbyListChanged[0], RequestLobbyList);
        RegisterFlagReceiver(Flags.Response.frag_received[0], FragReceivedPacket);

        client_self.name = $"{SteamFriends.GetPersonaName()}";
        ThreadManager.ExecuteOnMainThread(() => { menuManager.ChangeMenu("connecting"); });
        StartCoroutine(PingCheck());
        Connect(IPAddress.Parse(_IPAddress));
    }


    public void HandleData(byte[] _data)
    {
        if (_data.Take(4).SequenceEqual(Fragmentator.FCHHeader) || _data.Take(4).SequenceEqual(Fragmentator.FInitHeader))
        {
            Debug.Log("Got fragmented packet!");
            FragmentedPacket res = defragmentator.PushData(_data);
            if (res.isDone)
            {
                Debug.Log("Digested fragmented packet");
                parser.DigestMessage(res.payload);
            }
        }
        else
        {
            parser.DigestMessage(_data);
        }
    }


    private void ParseData(Packet packet)
    {

        List<Action<Packet>> actions;
        if (ReceiversMap.TryGetValue(packet.flag[0], out actions))
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                foreach (Action<Packet> action in actions) { action.Invoke(packet); }

            });
        }
        else
        {
            Debug.LogWarning($"Receiver not found for flag {packet.flag[0].ToString("X")}\n{BitConverter.ToString(packet.payload)}");
        }


    }

    public void SendPacketAssertResponse(Packet packet, Action<Packet> responseCallback)
    {
        if (socket == null) return;
        if (responseCallback == null) return;

        byte[] newCallbackID = Guid.NewGuid().ToByteArray();

        PacketResponseCallbackMap.Add(newCallbackID, responseCallback);

        packet.callbackGUID = newCallbackID;
        Debug.Log("Sending asserted00");
        packet.Send(stream);


    }

    public void PacketCallback(byte[] guid,  Packet packet)
    {
        if(!PacketResponseCallbackMap.ContainsKey(guid)) {
            Debug.LogWarning($"Got assertet response packet without callback present! GUID: {BitConverter.ToString(guid)}");
            return;
        }
        Action<Packet> responseCallback = PacketResponseCallbackMap[guid];
        PacketResponseCallbackMap.Remove(guid);
        responseCallback.Invoke(packet);
        

    }
    #region Assembling and Sending Packets
    public void JoinLobby(int id, string password = "")
    {
        Debug.Log($"Joining lobby {id}");
        Packet join_packet = new Packet();
        join_packet.header = Headers.data;
        join_packet.flag = Flags.Post.joinLobby;
        join_packet.AddToPayload(id);
        join_packet.AddToPayload(password);
        join_packet.Send(stream);
    }

    public void CreateLobby(string name, int max_players, string password = "")
    {
        Debug.Log($"Creatin lobby {name}");
        Packet create = new Packet();
        create.header = Headers.data;
        create.flag = Flags.Post.createLobby;
        create.AddToPayload(name);
        create.AddToPayload(max_players);
        create.AddToPayload(password != "");
        create.AddToPayload(password);
        create.Send(stream);
    }

    public void SendPlayerLocationInfo(Player player)
    {
        Transforms transforms_ = player.transforms;
        transforms_.rotation = player.movement.GetAngles();

        PlayerData playerData = new PlayerData();
        playerData.inputs = player.inputs;
        playerData.id = player.playerInfo.id;
        playerData.transforms = transforms_;

        string mes = JsonUtility.ToJson(playerData);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.playerTransformData;
        packet.AddToPayload(mes);
        packet.Send(udp_handler.client, udp_handler.remoteEndPoint);
    }


   
    public void SendItemInteractionInfo(ItemInteractionInfo interactionInfo)
    {
        string mes = JsonUtility.ToJson(interactionInfo);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.itemIntInf;
        packet.AddToPayload(mes);
        packet.Send(stream);
    }
    public void SendWorldState()
    {
        if (client_self == null)
            return;
        if (!IsSelfHost)
            return;

        string mes = JsonUtility.ToJson(worldState);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.worldState;
        packet.AddToPayload(mes);
        packet.Send(stream);
    }
    #endregion

    #region Parsing Incoming Packets

    #region Non-json Data

    private void ParseRejection(Packet packet)
    //friendzone :c
    {
        if (packet.flag[0] == Flags.Response.closing_con[0])
        {
            ThreadManager.ExecuteOnMainThread(() => { FindObjectOfType<MapLoader>().ReturnToMenu(); });
            Disconnect();
            return;
        }
        if (packet.flag[0] == Flags.Response.lobbyClosing[0])
        {
            ThreadManager.ExecuteOnMainThread(() => { FindObjectOfType<MapLoader>().ReturnToMenu(); });
            return;
        }
        if (packet.payload.Length < 4)
            return;//no payload, no error message
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int pay_len = reader.ReadInt32();
            string error = Encoding.UTF8.GetString(reader.ReadBytes(pay_len)); //TODO: Display the error in the game
            Debug.LogError($"REMOTE ERROR: {error}");
        }
    }

    public void RequestWorldState()
    {
        if (client_self.connectedPlayer.playerInfo.isHost)
        {
            Debug.LogError("Can't request a world state when we are a host!");
            return;
        }

        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Request.worldState;
        packet.Send(stream);
    }
    private void RequestLobbyList(Packet packet)
    {
        if (client_self.lobbyInfo == null)
            return;

        Packet lobby_list_request = new Packet();
        lobby_list_request.header = Headers.data;
        lobby_list_request.flag = Flags.Request.lobbyList;
        lobby_list_request.Send(stream);
    }

    public void RequestNetworkVars()
    {
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Request.NetworkVars;
        packet.Send(stream);
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
            imHere.Send(udp_handler.client, udp_handler.remoteEndPoint);

        }

        RequestLobbyList(null);
    }





    private void ParseDisconnect(Packet packet)
    {
        Debug.Log("Disconnected by server!");
        Debug.Log($"Disconnecting message: {Encoding.UTF8.GetString(packet.payload)}");
        //TODO: display Disconnected by server in game
        connected = false;
    }
    private void ParseACK(Packet packet)
    {
    }
    private void ParseECHO(Packet packet)
    {
        ThreadManager.ExecuteOnMainThread(() =>
        {
            Ping = Time.realtimeSinceStartup - lastEchoTime;
        });
        
    }



   
    #endregion


    #endregion



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

    IEnumerator WaitForLocalPlayer()
    {
        while (true)
        {
            if (client_self.connectedPlayer != null)
            {
                if (LocalPlayerActions.Count > 0)
                {
                    Action action = LocalPlayerActions.Dequeue();
                    action.Invoke();
                }
            }
            yield return null;
        }
    }

    public bool LocaPlayerInitialized()
    {
        return client_self.connectedPlayer != null;
    }

    public void SendFragmented(FragmentedPacket fragmentedPacket)
    {
        fragmentsReceivedMap.Add(fragmentedPacket.hash, false);
        Debug.Log($"Added hashmap entry for {fragmentedPacket.hash}");
        StartCoroutine(SendFragmentedData(fragmentedPacket));
    }

    private void FragReceivedPacket(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            string hash = Encoding.UTF8.GetString(reader.ReadBytes(packet.payload.Length));
            if (fragmentsReceivedMap.ContainsKey(hash))
                fragmentsReceivedMap[hash] = true;

            else
                fragmentsReceivedMap.Add(hash, true);
            Debug.Log($"XDDDD for {hash}");
        }
    }
    IEnumerator SendFragmentedData(FragmentedPacket fragmentedPacket)
    {
        if (!fragmentsReceivedMap.ContainsKey(fragmentedPacket.hash))
        {
            Debug.LogError($"Hash {fragmentedPacket.hash} does not exist in hash-hashmap for sending framgented packets!");
        }
        int index = 1;
        stream.BeginWrite(fragmentedPacket.chunks[0], 0, fragmentedPacket.chunks[0].Length, null, null); // send the initial packet
        while (true)
        {
            if (fragmentsReceivedMap[fragmentedPacket.hash])
            {

                stream.BeginWrite(fragmentedPacket.chunks[index], 0, fragmentedPacket.chunks[index].Length, null, null);
                fragmentsReceivedMap[fragmentedPacket.hash] = false;
                if (index + 1 >= fragmentedPacket.chunks.Count)
                    break;
                index++;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }
    }

    IEnumerator PingCheck()
    {
        while (true)
        {
            if (socket!=null && socket.Connected)
            {
                Packet echoPac = new();
                echoPac.header = Headers.echo;
                echoPac.flag = Flags.none;
                echoPac.Send(stream);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    lastEchoTime = Time.realtimeSinceStartup;
                });
            }
            yield return new WaitForSeconds(2);
        }
    }

}

