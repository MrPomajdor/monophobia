using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    }
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("mainmenu", LoadSceneMode.Single);
    }
        
    public void LoadMap(LobbyInfo mapInfo){
        StartCoroutine(LoadMapAsync(mapInfo));
        Debug.Log("Loading map");
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
            }
        }

        foreach (PlayerInfo client in mapInfo.players)
        {
            if (CurrentMapManager.mapInfo.players.FirstOrDefault(x => (x.name == client.name && x.id == client.id)) == null)
            {

                CreatePlayer(client);

            }
        }

        CurrentMapManager.mapInfo = mapInfo;

        
    }

    private IEnumerator LoadMapAsync(LobbyInfo mapInfo) //TODO: 23.01.2024 Configure the MapManager, and make the LoadMapAsync apply values from MapInfo object
    {
        lobbyLoading = true; //god please please no why 
        ConnectionManager conMan = FindObjectOfType<ConnectionManager>();
        string name = mapInfo.mapName;
        if (Application.CanStreamedLevelBeLoaded(name))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

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
                    print("LOCAL PLAYER IS A HOST");
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
            PlayerSpawnPosition r_spawn_pos = spawns[UnityEngine.Random.Range(0, spawns.Length)];
            GameObject local_player = Instantiate(PlayerPrefab, r_spawn_pos.transform.position, r_spawn_pos.transform.rotation);
            Player lcp = local_player.GetComponent<Player>();
            lcp.playerInfo.isLocal = true;
            lcp.playerInfo.id = conMan.client_self.id;
            lcp.playerInfo.name = conMan.client_self.name;
            lcp.playerInfo.isHost = isSelfHost;
            if (conMan.client_self == null)
                conMan.client_self = new ClientHandle();
            conMan.client_self.connectedPlayer = lcp;
            lcp.voice.isLocal = true;
            lcp.voice.Init();
 





        }
        else
        {
            Debug.LogError($"Scene {name} does not exist");

        }
        lobbyLoading = false; //GOD DAMMIT
    }
    void CreatePlayer(PlayerInfo pl=null, bool self=false) //27.01.2024
    {                                                      //TODO: add every other variable that is to player.
                                                           //the fuck this function gets called out of nowhere? Booooo...
                                                           //no fr this time why the fuck this shit is getting called TWO FUCKING TIMES IN A ROW WHEN I CAN CLEARLY SEE LIKE IN THE DEBUGGER THAT IT SHOULD BE CALLED ONCE?
                                                           //forgot to update. its working
                                                           //GOD DAMMIT SELF CREATING ISNT WORKING D:
                                                           //and i wanna clarify - i know why this shit isn't working just i dont have the iron will to fix it xd
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
        Player npl = new_player.GetComponent<Player>();
        npl.playerInfo = pl;
        npl.movement.enabled = self;
        npl.cam.enabled = self;
        npl.movement.enabled = self;
        npl.playerInfo.isLocal = self;
        npl.playerInfo.isHost = pl.isHost;
        npl.playerInfo.id = self ? conMan.client_self.id : npl.playerInfo.id;
        npl.playerInfo.name = self ? conMan.client_self.name : npl.playerInfo.name;
        npl.voice.isLocal = self;
        npl.voice.Init();
        npl.cam.GetComponent<AudioListener>().enabled = false;
        ClientHandle binpl = new ClientHandle();
        binpl.id = self ? conMan.client_self.id : npl.playerInfo.id;
        binpl.name = self ? conMan.client_self.name : npl.playerInfo.name;
        binpl.connectedPlayer = npl;
        conMan.clients.Add(binpl);
        if (self)
        {
            conMan.client_self.connectedPlayer = npl;
        }
        Debug.Log($"Creating player {pl.name} with id {pl.id} ({self})");
    }
}
