using UnityEngine;

public class MapManager : MonoBehaviour
{
    public LobbyInfo mapInfo;
    ConnectionManager conMan;
    MapLoader mapLoader;
    private void Start()
    {
        conMan = FindAnyObjectByType<ConnectionManager>();
        mapLoader = FindAnyObjectByType<MapLoader>();
    }

    public void SetSetting(string key, object value)
    {
        //todo
    }
    public void StartMap()
    {
        if (!conMan.client_self.connectedPlayer.playerInfo.isHost) return;
        mapInfo.mapName = "grid0";
        if (mapInfo.mapName == "lobby0") return; //TODO: show the error that you have to select a map
        SendMapSettings();

        mapLoader.LoadMap(mapInfo);

        
    }

    private void SendMapSettings()
    {
        if (!conMan.client_self.connectedPlayer.playerInfo.isHost) return;

        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.lobbyInfo;
        string mes = JsonUtility.ToJson(mapInfo);
        packet.AddToPayload(mes);
        packet.Send(conMan.stream);
    }


}
