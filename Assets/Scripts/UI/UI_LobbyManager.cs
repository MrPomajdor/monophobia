using System;
using System.IO;
using System.Text;
using UnityEngine;

public class UI_LobbyManager : MonoBehaviour
{
    public GameObject LobbyPrefab;
    public GameObject EmptyPrefab;

    public Transform lobbyList;
    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.lobbyList[0], ParseLobbyList);
    }

    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.lobbyList[0], ParseLobbyList);
    }
    private void ParseLobbyList(Packet packet)
    {

        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int amount = reader.ReadInt32();
            Debug.Log($"Lobby amount: {amount}");
            Clear();
            if (amount < 1)
            {
                IndicateNoLobbies();
                return;
            }
            for (int i = 0; i < amount; i++)
            {

                int lobby_id = reader.ReadInt32();
                int stringLength = reader.ReadInt32();
                byte[] stringData = reader.ReadBytes(stringLength);
                string lobbyName = Encoding.UTF8.GetString(stringData);
                bool protected_ = reader.ReadBoolean();
                int current_players = reader.ReadInt32();
                int max_players = reader.ReadInt32();
                Debug.Log($"Lobby {i} name: {lobbyName}");
                AddLobbyToUI(lobby_id, lobbyName, max_players, current_players, protected_);
            }
        }
    }

    public void Clear()
    {

        foreach (Transform child in lobbyList)
        {
            Destroy(child.gameObject);
        }
    }
    public void AddLobbyToUI(int id, string name, int max_players, int current_players, bool password)
    {
        GameObject l = Instantiate(LobbyPrefab, lobbyList);
        UI_LobbyPanel lobby = l.GetComponent<UI_LobbyPanel>();
        lobby.UpdateValues(id, name, max_players, current_players, password);
        lobby.SetJoinAction(JoinLobby);

    }

    public void JoinLobby(int id, string password)
    {
        Global.connectionManager.JoinLobby(id, password);
    }



    public void IndicateNoLobbies()
    {

        Instantiate(EmptyPrefab, lobbyList);

    }
}
