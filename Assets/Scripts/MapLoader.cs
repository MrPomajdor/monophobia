using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MapLoader : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public GameObject InGameUI;

    public bool lobbyLoading; //oh god no
    public MapManager CurrentMapManager
    {
        get { return mapManager; }
    }

    private MapManager mapManager;

    public void ParseWorldStateData(Packet packet)
    {
        WorldState wS = new WorldState();
            
        if (!packet.GetFromPayload(wS))
            return;

        UpdateWorldState(wS);
    }
    private void ParseLobbyInfo(Packet packet)
    {
        Debug.Log("Got lobby info");

        LobbyInfo lobbyInfo = new LobbyInfo();
        if (!packet.GetFromPayload(lobbyInfo))
            return;
        Global.connectionManager.client_self.lobbyInfo = lobbyInfo;

        if (lobbyLoading) //I HATE MYSELF (jk)
            return;                                     //TODO: do something with this shitty ass hack
        foreach (NetworkPlayerInfo npi in lobbyInfo.players)
        {
            Global.connectionManager.players.Append(npi.ToPlayerInfo());
        }
        

        if (CurrentMapManager == null)
        {
            LoadMap(lobbyInfo);
        }
        else
            UpdateMap(lobbyInfo);

        //We assume this channel always exists because the server should create it when creating lobby.
        //Also the server should only allow players that are in a lobby to join this channel
       // Global.connectionManager.mumbleManager.JoinChannel(lobbyInfo.lobbyName);


    }
    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.lobbyInfo[0], ParseLobbyInfo);
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.worldState[0], ParseWorldStateData);
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.startMap[0], RemoteMapStart);
    }
    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.lobbyInfo[0], ParseLobbyInfo);
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.worldState[0], ParseWorldStateData);
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.startMap[0], RemoteMapStart);
    }
    void Start()
    {
        DontDestroyOnLoad(this);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            ReturnToMenu();
    }
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("mainmenu", LoadSceneMode.Single);
    }

    public void LoadMap(LobbyInfo mapInfo)
    {
        Debug.Log($"Loading map {mapInfo.mapName}");
        MenuManager menuManager = FindObjectOfType<MenuManager>();
        if (menuManager)
        {
            menuManager.ChangeMenu("loading");
        }
        StartCoroutine(LoadMapAsync(mapInfo));

    }
    public void RemoteMapStart(Packet packet)
    {
        LoadMap(CurrentMapManager.mapInfo);
    }
    public void UpdateMap(LobbyInfo mapInfo) //WHAT
                                             //TODO: Detect when lobby owner changes and change it localy
    {
        Debug.Log("Updating map------------------------");
        Debug.Log(Global.connectionManager.clients.Count);
        foreach (NetworkPlayerInfo npi in CurrentMapManager.mapInfo.players)
        {
            PlayerInfo client = npi.ToPlayerInfo();
            if (mapInfo.players.FirstOrDefault(x => (x.name == client.name && x.id == client.id)) == null)
            {
                GameObject xd = Global.connectionManager.clients.FirstOrDefault(x => x.id == client.id).connectedPlayer.transform.root.gameObject;
                Global.connectionManager.clients.Remove(Global.connectionManager.clients.FirstOrDefault(x => x.id == client.id));
                Destroy(xd);

                Debug.Log($"Player {client.name}/{client.id} has left the lobby");
                Global.chat.AddMessage($"   Player {client.name} has left the lobby", "#d4f542");

            }
        }

        foreach (NetworkPlayerInfo npi in mapInfo.players)
        {
            PlayerInfo client = npi.ToPlayerInfo();

            if (CurrentMapManager.mapInfo.players.FirstOrDefault(x => (x.name == client.name && x.id == client.id)) == null)
            {

                Debug.Log($"Player {client.name}/{client.id} has joined the lobby");
                Global.chat.AddMessage($"   Player {client.name} has joined the lobby", "#d4f542");
                CreatePlayer(client);

            }
        }

        CurrentMapManager.mapInfo = mapInfo;


    }

    private IEnumerator LoadMapAsync(LobbyInfo mapInfo) //TODO: 23.01.2024 Configure the MapManager, and make the LoadMapAsync apply values from MapInfo object
    {
        lobbyLoading = true; //god please please no why 
        ConnectionManager conMan = FindObjectOfType<ConnectionManager>();
        Global.connectionManager.worldState = new WorldState();
        string name = mapInfo.mapName;
        if (Application.CanStreamedLevelBeLoaded(name))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            Global.connectionManager.clients.Clear();
            mapManager = FindObjectOfType<MapManager>();
            mapManager.mapInfo = mapInfo;
            bool isSelfHost = false;
            foreach (NetworkPlayerInfo npi in mapInfo.players)
            {
                PlayerInfo pl = npi.ToPlayerInfo();
                if (pl.id != Global.connectionManager.client_self.id) //check if player is local
                    CreatePlayer(pl); //remote player creation
                else
                {
                    isSelfHost = pl.isHost;

                }
            }
            //TODO: make soimething to save settings and ever choose them.
            //Here we should use the CreatePlayer function but IT WONT FUCKING WORK AND IM SICK OF IT
            //i should really use one function for this
            PlayerSpawnPosition[] spawns = FindObjectsOfType<PlayerSpawnPosition>();
            if (spawns.Length == 0)
            {
                Debug.LogError("No spawn positions present on the map!");
                yield break;
            }
            Debug.Log("Creating local player");
            PlayerSpawnPosition r_spawn_pos = spawns[UnityEngine.Random.Range(0, spawns.Length)];
            GameObject local_player = Instantiate(PlayerPrefab, r_spawn_pos.transform.position, r_spawn_pos.transform.rotation);
            local_player.name = Global.connectionManager.client_self.name;
            Player lcp = local_player.GetComponent<Player>();
            lcp.playerInfo.isLocal = true;
            lcp.playerInfo.id = Global.connectionManager.client_self.id;
            lcp.playerInfo.name = Global.connectionManager.client_self.name;
            lcp.playerInfo.isHost = isSelfHost;

            if (Global.connectionManager.client_self == null)
                Global.connectionManager.client_self = new ClientHandle();
            Global.connectionManager.client_self.connectedPlayer = lcp;
            lcp.voice.Initialize(RevisitedVoiceManager.Role.Sender);
            lcp.voice.audioSource.bypassReverbZones = true;

            //DontDestroyOnLoad(local_player);


            //WORLD STATE SYNCING
            if (isSelfHost)
            {
                RefreshWorldState();
            }
            else
            {
                foreach (Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
                {
                    Destroy(item.gameObject);
                }
                Global.connectionManager.RequestWorldState();
                
            }





        }
        else
        {
            Debug.LogError($"Scene {name} does not exist");

        }
        lobbyLoading = false; //GOD DAMMIT
    }

    public void RefreshWorldState()
    {


        if (!Global.connectionManager.client_self.connectedPlayer.playerInfo.isHost) return;
        Debug.Log("Refreshing local hosted world state");

        Global.connectionManager.items = FindObjectsByType<Item>(FindObjectsSortMode.None).ToList();
        Global.connectionManager.worldState.items.Clear();
        foreach (Item item in Global.connectionManager.items)
        {
            item.itemStruct.id = Global.connectionManager.worldState.items.Count;
            NetworkItemStruct networkItem = new NetworkItemStruct();
            networkItem.id = item.itemStruct.id;
            networkItem.name = item.itemStruct.name;
            networkItem.transforms = new Transforms();
            networkItem.transforms.position = item.transform.position;
            networkItem.transforms.rotation = item.transform.eulerAngles;
            Global.connectionManager.worldState.items.Add(networkItem);
        }
        Global.connectionManager.SendWorldState();




    }
    public void UpdateWorldState(WorldState newWorldState)
    {
        Debug.Log("World state update!");
        if (Global.connectionManager.worldState.Equals(newWorldState))
            return;

        NetworkItemStruct[] itemsToRemove = Global.connectionManager.worldState.items.Except(newWorldState.items).ToArray();
        NetworkItemStruct[] newItems = newWorldState.items.Except(Global.connectionManager.worldState.items).ToArray();
        Item[] itemsLoaded = FindObjectsByType<Item>(FindObjectsSortMode.None);
        if (itemsToRemove.Length > 0)
            foreach (NetworkItemStruct item in itemsToRemove)
            {
                Item x = itemsLoaded.FirstOrDefault(x => x.itemStruct.id == item.id);
                if (x != null)
                    Destroy(x.gameObject);
            }

        if (newItems.Length > 0)
            foreach (NetworkItemStruct item in newItems)
            {
                GameObject itemObject = Instantiate(Resources.Load<GameObject>(item.name));

                Item itemObjectScript = itemObject.GetComponent<Item>();
                itemObjectScript.itemStruct.id = item.id;
                itemObjectScript.NetworkTransforms.position = item.transforms.position;
                itemObjectScript.NetworkTransforms.rotation = item.transforms.rotation;
                itemObjectScript.NetworkTransforms.real_velocity = item.transforms.real_velocity;
                itemObjectScript.NetworkTransforms.real_angular_velocity = item.transforms.real_angular_velocity;

                itemObject.transform.position = item.transforms.position;
                itemObject.transform.eulerAngles = item.transforms.rotation;

                itemObjectScript.InternalItemStart();
            }

        Global.connectionManager.worldState = newWorldState;
        Global.connectionManager.items = FindObjectsByType<Item>(FindObjectsSortMode.None).ToList();
        Global.connectionManager.RequestNetworkVars();
        //TODO: Add mobs
    }



    void CreatePlayer(PlayerInfo pl = null, bool self = false) //27.01.2024
    {                                                      //TODO: add every other variable that is to player.
                                                           //the fuck this function gets called out of nowhere? Booooo...
                                                           //no fr this time why the fuck this shit is getting called TWO FUCKING TIMES IN A ROW WHEN I CAN CLEARLY SEE LIKE IN THE DEBUGGER THAT IT SHOULD BE CALLED ONCE?
                                                           //forgot to update. its working
                                                           //GOD DAMMIT SELF CREATING ISNT WORKING D:
                                                           //and i wanna clarify - i know why this shit isn't working just i dont have the iron will to fix it xd
        Debug.Log($"Creating player {pl.name} with id {pl.id} ({self}) - is a monster ?: {pl.isMonster}");
        PlayerSpawnPosition[] spawns = FindObjectsOfType<PlayerSpawnPosition>();
        if (spawns.Length == 0)
        {
            Debug.LogError("No spawn positions present on the map!");
            return;
        }
        PlayerSpawnPosition r_spawn_pos = spawns[UnityEngine.Random.Range(0, spawns.Length)];

        if (Global.connectionManager.clients.FirstOrDefault(x => x.id == pl.id) != null)
        {
            Debug.LogWarning("Tried to create multiple player models with the same id");
            return;
        }
        GameObject new_player;
        if (!pl.isMonster)
            new_player = Instantiate(PlayerPrefab, r_spawn_pos.transform.position, r_spawn_pos.transform.rotation);
        else
        {
            
            new_player = Instantiate(Resources.Load<GameObject>(pl.monsterData.Name), r_spawn_pos.transform.position, r_spawn_pos.transform.rotation);
        }

        new_player.name = pl.name;
        new_player.GetComponent<MenuController>().uiCanvas.gameObject.SetActive(false);
        new_player.GetComponent<MenuController>().enabled = self;
        if(self)
            Instantiate(InGameUI,new_player.transform);
        //new_player.transform.Find("InGameUI").gameObject.SetActive(self);
        Player npl = new_player.GetComponent<Player>();
        bool isMonster = npl.playerInfo.isMonster;
        MonsterData mData = npl.playerInfo.monsterData;
        npl.playerInfo = pl;
        npl.playerInfo.isMonster = isMonster; 
        npl.playerInfo.monsterData = mData;
        npl.movement.enabled = self;
        npl.cam.enabled = self;
        npl.cam.gameObject.GetComponent<ItemInteraction>().remote = !self;
        npl.cam.gameObject.GetComponent<MouseRotation>().enabled = self;
        new_player.GetComponent<Feelings>().heartbeat.enabled = self;
        new_player.GetComponent<Feelings>().enabled = self;

        npl.movement.enabled = self;
        npl.playerInfo.isLocal = self;
        npl.playerInfo.isHost = pl.isHost;
        npl.playerInfo.id = self ? Global.connectionManager.client_self.id : npl.playerInfo.id;
        npl.playerInfo.name = self ? Global.connectionManager.client_self.name : npl.playerInfo.name;
        if (!pl.isMonster)
            npl.voice.Initialize(RevisitedVoiceManager.Role.Receiver); //TODO: Remember to change it depending on local/remote side
        npl.cam.GetComponent<AudioListener>().enabled = false;
        if(!pl.isMonster)
            npl.GetComponent<InventoryManager>().Remote = !self;

        ClientHandle binpl = new ClientHandle();
        binpl.id = self ? Global.connectionManager.client_self.id : npl.playerInfo.id;
        binpl.name = self ? Global.connectionManager.client_self.name : npl.playerInfo.name;
        binpl.connectedPlayer = npl;
        Global.connectionManager.clients.Add(binpl);
        if (self)
        {
            Global.connectionManager.client_self.connectedPlayer = npl;
        }

        //DontDestroyOnLoad(new_player);

    }
}
