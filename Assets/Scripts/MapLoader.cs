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
        
    public void LoadMap(MapInfo mapInfo){
        StartCoroutine(LoadMapAsync(mapInfo));
        Debug.Log("Loading map");
    }

    public void UpdateMap(MapInfo mapInfo) //WHAT
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

    private IEnumerator LoadMapAsync(MapInfo mapInfo) // TODO: Configure the MapManager, and make the LoadMapAsync apply values from MapInfo object
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
            foreach (PlayerInfo pl in mapInfo.players)
            {
                Debug.Log($"SPIEDFALAJ {pl.id} {pl.name}");
                if(pl.id!=conMan.client_self.id)
                    CreatePlayer(pl);
            }
            //TODO: make that the local player loads everything (started)
            //TODO: make soimething to save settings and ever choose them.
            GameObject local_player = Instantiate(PlayerPrefab);
            Player lcp = local_player.GetComponent<Player>();
            lcp.playerInfo.isLocal = true;
            lcp.playerInfo.id = conMan.client_self.id;
            lcp.playerInfo.name = conMan.client_self.name;
            conMan.client_self.connectedPlayer = lcp;

        }
        else
        {
            Debug.LogError($"Scene {name} does not exist");

        }
        lobbyLoading = false; //GOD DAMMIT
    }
    void CreatePlayer(PlayerInfo pl, bool self=false) //27.01.2024
                                                      //TODO: add every other variable that is to player.
                                                      //the fuck this function gets called out of nowhere? Booooo...
                                                      //no fr this time why the fuck this shit is getting called TWO FUCKING TIMES IN A ROW WHEN I CAN CLEARLY SEE LIKE IN THE DEBUGGER THAT IT SHOULD BE CALLED ONCE?
    {
       if(conMan.clients.FirstOrDefault(x =>  x.id == pl.id) != null)
        {
            Debug.LogWarning("Tried to create multiple player models with the same id");
            return;
        }
        Debug.Log($"Creating player {pl.name} with id {pl.id} ({self})");
        GameObject new_player = Instantiate(PlayerPrefab);
        Player npl = new_player.GetComponent<Player>();
        npl.playerInfo = pl;
        npl.movement.enabled = self;
        npl.cam.enabled = self;
        npl.movement.enabled = self;
        npl.playerInfo.isLocal = self;
        npl.playerInfo.id = self ? conMan.client_self.id : npl.playerInfo.id;
        npl.playerInfo.name = self ? conMan.client_self.name : npl.playerInfo.name;
        npl.cam.GetComponent<AudioListener>().enabled = false;
        ClientHandle binpl = new ClientHandle();
        binpl.id = self ? conMan.client_self.id : npl.playerInfo.id;
        binpl.name = self ? conMan.client_self.name : npl.playerInfo.name;
        binpl.connectedPlayer = npl;
        conMan.clients.Add(binpl);
        Debug.Log(conMan.clients.Count);
    }
}
