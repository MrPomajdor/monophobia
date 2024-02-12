using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_LobbyManager : MonoBehaviour
{
    public GameObject LobbyPrefab;
    public GameObject EmptyPrefab;
    ConnectionManager cmanager;
    public void Clear()
    {
        ThreadManager.ExecuteOnMainThread(() =>
        {
           foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }

        });
     }
    public void AddLobbyToUI(int id, string name, int max_players, int current_players, bool password, ConnectionManager con)
    {
        ThreadManager.ExecuteOnMainThread(() =>
        {
            GameObject l = Instantiate(LobbyPrefab, transform);
            UI_LobbyPanel lobby = l.GetComponent<UI_LobbyPanel>();
            lobby.UpdateValues(id, name, max_players, current_players, password);
            lobby.SetJoinAction(JoinLobby);
            cmanager = con;
        });
    }

    public void JoinLobby(int id, string password)
    {
        cmanager.JoinLobby(id, password);
    }
    private event Action mainThreadQueuedCallbacks;
    private event Action eventsClone;
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

    public void IndicateNoLobbies()
    {
        ThreadManager.ExecuteOnMainThread(() =>
        {
            Instantiate(EmptyPrefab, transform);
        });
    }
}
