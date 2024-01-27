using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapLoader : MonoBehaviour
{
    public GameObject PlayerPrefab;
    ConnectionManager conMan;
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
    }

    public void UpdateMap(MapInfo mapInfo) // TODO: Update settings too, or sync every info at once.
    {
        CurrentMapManager.mapInfo = mapInfo;

        if (conMan.clients.Count != mapInfo.players.Length) {
            UpdatePlayers(mapInfo);
            return;
        }
        foreach(PlayerInfo player in mapInfo.players)
        {
            if (conMan.clients.FirstOrDefault(x => x.name == player.name) == null)
            {
                UpdatePlayers(mapInfo);
            }
        }

        
    }
    private void UpdatePlayers(MapInfo mapInfo)
    {
        foreach (PlayerInfo client in mapInfo.players)
        {
            if(conMan.clients.FirstOrDefault(x => x.name == client.name) == null)
            {
                CreatePlayer(client);
            }
        }

        foreach (ClientHandle client in conMan.clients)
        {
            if (mapInfo.players.FirstOrDefault(x => x.name == client.name) == null)
            {
                conMan.clients.Remove(client);
            }
        }
        
    }
    private IEnumerator LoadMapAsync(MapInfo mapInfo) // TODO: Configure the MapManager, and make the LoadMapAsync apply values from MapInfo object
    {
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
            mapManager.mapInfo = mapInfo;/*
            foreach(ClientHandle client in conMan.clients)
            {
                Destroy(client.connectedPlayer.gameObject);
            }
            conMan.clients.Clear();
            Destroy(conMan.client_self.connectedPlayer.gameObject);*/
            foreach (PlayerInfo pl in mapInfo.players)
            {
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
    }
    void CreatePlayer(PlayerInfo pl)
    {
        GameObject new_player = Instantiate(PlayerPrefab);
        Player npl = new_player.GetComponent<Player>();
        npl.playerInfo = pl;
        npl.movement.enabled = false;
        npl.cam.enabled = false;
        npl.movement.enabled = false;
        npl.cam.GetComponent<AudioListener>().enabled = false;
        ClientHandle binpl = new ClientHandle();
        binpl.id = npl.playerInfo.id;
        binpl.name = npl.playerInfo.name;
        binpl.connectedPlayer = npl;
        conMan.clients.Add(binpl);
    }
}
