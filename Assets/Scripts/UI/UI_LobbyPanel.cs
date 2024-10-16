using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UI_LobbyPanel : MonoBehaviour
{
    public TextMeshProUGUI lobbyNameText;
    public TextMeshProUGUI playerCountText;
    public Image padlock;
    Action<int, string> invokeJoin;
    string lobbyName;
    int maxPlayers;
    int currentPlayers;
    int id;
    public void UpdateValues(int _id, string _lobbyName, int _maxPlayers, int _currentPlayers, bool _passwordProtected)
    {
        maxPlayers = _maxPlayers;
        currentPlayers = _currentPlayers;
        lobbyName = _lobbyName;
        id = _id;
        lobbyNameText.SetText(_lobbyName);
        playerCountText.SetText($"{_currentPlayers}/{_maxPlayers}");
        padlock.enabled = _passwordProtected;
    }

    public void Join()
    {
        if(currentPlayers < maxPlayers)
            invokeJoin.Invoke(id, "");
    }

    public void SetJoinAction(Action<int, string> action)
    {
        invokeJoin = action;
    }

}
