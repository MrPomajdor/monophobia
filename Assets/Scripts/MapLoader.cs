using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MapLoader : MonoBehaviour
{
    public GameObject PlayerPrefab;
    ConnectionManager conMan;
    public bool lobbyLoading; //oh god no
    public MapManager CurrentMapManager 
    { 
        get { return mapManager; }
    }

    private MapManager mapManager;
    void Start()
    {
        DontDestroyOnLoad(this);
        conMan = FindObjectOfType<ConnectionManager>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F1))
            ReturnToMenu();
    }
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("mainmenu", LoadSceneMode.Single);
    }
        
    public void LoadMap(LobbyInfo mapInfo){
        Debug.Log($"Loading map {mapInfo.mapName}");
        StartCoroutine(LoadMapAsync(mapInfo));
        
    }

    public void UpdateMap(LobbyInfo mapInfo) //WHAT
                                             //TODO: Detect when lobby owner changes and change it localy
    {
        Debug.Log("Updating map------------------------");
        Debug.Log(conMan.clients.Count);
        foreach (PlayerInfo client in CurrentMapManager.mapInfo.players)
        {
            if (mapInfo.players.FirstOrDefault(x => (x.name == client.name && x.id == client.id)) == null)
            {
                GameObject xd = conMan.clients.FirstOrDefault(x => x.id == client.id).connectedPlayer.transform.root.gameObject;
                conMan.clients.Remove(conMan.clients.FirstOrDefault(x =>  x.id == client.id));
                Destroy(xd);

                Debug.Log($"Player {client.name}/{client.id} has left the lobby");
            }
        }

        foreach (PlayerInfo client in mapInfo.players)
        {
            if (CurrentMapManager.mapInfo.players.FirstOrDefault(x => (x.name == client.name && x.id == client.id)) == null)
            {

                Debug.Log($"Player {client.name}/{client.id} has joined the lobby");
                CreatePlayer(client);

            }
        }

        CurrentMapManager.mapInfo = mapInfo;

        
    }

    private IEnumerator LoadMapAsync(LobbyInfo mapInfo) //TODO: 23.01.2024 Configure the MapManager, and make the LoadMapAsync apply values from MapInfo object
    {
        lobbyLoading = true; //god please please no why 
        ConnectionManager conMan = FindObjectOfType<ConnectionManager>();
        conMan.worldState = new WorldState();
        string name = mapInfo.mapName;
        if (Application.CanStreamedLevelBeLoaded(name))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            conMan.clients.Clear();
            mapManager = FindObjectOfType<MapManager>();
            mapManager.mapInfo = mapInfo;
            bool isSelfHost=false;
            foreach (PlayerInfo pl in mapInfo.players)
            {
                if (pl.id != conMan.client_self.id) //check if player is local
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
            local_player.name = conMan.client_self.name;
            Player lcp = local_player.GetComponent<Player>();
            lcp.playerInfo.isLocal = true;
            lcp.playerInfo.id = conMan.client_self.id;
            lcp.playerInfo.name = conMan.client_self.name;
            lcp.playerInfo.isHost = isSelfHost;

            if (conMan.client_self == null)
                conMan.client_self = new ClientHandle();
            conMan.client_self.connectedPlayer = lcp;
            lcp.voice.Initialize(VoiceManager.Type.Local);
            lcp.voice.mainAudioSource.bypassReverbZones = true;

            //DontDestroyOnLoad(local_player);


            //WORLD STATE SYNCING
            if (isSelfHost)
            {
                conMan.items = FindObjectsByType<Item>(FindObjectsSortMode.None).ToList();
                foreach (Item item in conMan.items)
                {
                    conMan.worldState.items.Add(item.itemStruct);
                }
                conMan.SendWorldState();
            }
            else
            {
                foreach(Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
                {
                    Destroy(item.gameObject);
                }
                conMan.RequestWorldState();
            }





        }
        else
        {
            Debug.LogError($"Scene {name} does not exist");

        }
        lobbyLoading = false; //GOD DAMMIT
    }
    public void UpdateWorldState(WorldState newWorldState)
    {
        Debug.Log("World state update!");
        if (conMan.worldState.Equals(newWorldState))
            return;

        ItemStruct[] itemsToRemove = conMan.worldState.items.Except(newWorldState.items).ToArray();
        ItemStruct[] newItems = newWorldState.items.Except(conMan.worldState.items).ToArray();
        Item[] itemsLoaded = FindObjectsByType<Item>(FindObjectsSortMode.None);
        if (itemsToRemove.Length > 0)
            foreach (ItemStruct item in itemsToRemove)
            {
                Item x = itemsLoaded.FirstOrDefault(x => x.itemStruct.id == item.id);
                if (x != null)
                    Destroy(x.gameObject);
            }

        if (newItems.Length > 0)
            foreach (ItemStruct item in newItems)
            {
                GameObject itemObject = Instantiate(Resources.Load<GameObject>(item.name));

                //Item itemObjectScript = itemObject.GetComponent<Item>();
                //itemObjectScript.item = item;

                itemObject.transform.position = item.transforms.position;
                itemObject.transform.eulerAngles = item.transforms.rotation;
            }

        conMan.worldState = newWorldState;
        conMan.items = FindObjectsByType<Item>(FindObjectsSortMode.None).ToList();
        //TODO: Add mobs
    }



    void CreatePlayer(PlayerInfo pl=null, bool self=false) //27.01.2024
    {                                                      //TODO: add every other variable that is to player.
                                                           //the fuck this function gets called out of nowhere? Booooo...
                                                           //no fr this time why the fuck this shit is getting called TWO FUCKING TIMES IN A ROW WHEN I CAN CLEARLY SEE LIKE IN THE DEBUGGER THAT IT SHOULD BE CALLED ONCE?
                                                           //forgot to update. its working
                                                           //GOD DAMMIT SELF CREATING ISNT WORKING D:
                                                           //and i wanna clarify - i know why this shit isn't working just i dont have the iron will to fix it xd
        Debug.Log($"Creating player {pl.name} with id {pl.id} ({self})");
        PlayerSpawnPosition[] spawns = FindObjectsOfType<PlayerSpawnPosition>();
        if (spawns.Length == 0)
        {
            Debug.LogError("No spawn positions present on the map!");
            return;
        }
        PlayerSpawnPosition r_spawn_pos = spawns[UnityEngine.Random.Range(0, spawns.Length)];

        if (conMan.clients.FirstOrDefault(x =>  x.id == pl.id) != null)
        {
            Debug.LogWarning("Tried to create multiple player models with the same id");
            return;
        }
        
        GameObject new_player = Instantiate(PlayerPrefab,r_spawn_pos.transform.position,r_spawn_pos.transform.rotation);
        new_player.name = pl.name;
        new_player.GetComponent<MenuController>().uiCanvas.gameObject.SetActive(false);
        new_player.GetComponent<MenuController>().enabled = self;
        Player npl = new_player.GetComponent<Player>();
        npl.playerInfo = pl;
        npl.movement.enabled = self;
        npl.cam.enabled = self;
        npl.cam.gameObject.GetComponent<ItemInteraction>().remote = !self;
        npl.cam.gameObject.GetComponent<MouseRotation>().enabled = self;
        npl.movement.enabled = self;
        npl.playerInfo.isLocal = self;
        npl.playerInfo.isHost = pl.isHost;
        npl.playerInfo.id = self ? conMan.client_self.id : npl.playerInfo.id;
        npl.playerInfo.name = self ? conMan.client_self.name : npl.playerInfo.name;
        npl.voice.Initialize(VoiceManager.Type.Remote); //TODO: Remember to change it depending on local/remote side
        npl.cam.GetComponent<AudioListener>().enabled = false;
        npl.GetComponent<InventoryManager>().Remote = !self;

        ClientHandle binpl = new ClientHandle();
        binpl.id = self ? conMan.client_self.id : npl.playerInfo.id;
        binpl.name = self ? conMan.client_self.name : npl.playerInfo.name;
        binpl.connectedPlayer = npl;
        conMan.clients.Add(binpl);
        if (self)
        {
            conMan.client_self.connectedPlayer = npl;
        }

        //DontDestroyOnLoad(new_player);
        
    }
}
