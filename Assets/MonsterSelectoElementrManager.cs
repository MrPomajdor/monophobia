using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MonsterSelectoElementrManager : MonoBehaviour
{
    public TMP_Text MonsterName;
    public Image MonsterSplash;
    public MonsterData monsterData;
   public void SetMonster(MonsterData monster)
    {
        monsterData = monster;
        MonsterName.text = monster.Name;
        MonsterSplash.sprite = monster.SplashArt;
    }

    public void SelectMonsterButton()
    {
        FindObjectOfType<LobbyManager>().RequestToBeAMonster(monsterData);
    }

    public void MoreInfoButton()
    {
        FindObjectOfType<LobbyManager>().MessageBoxController.ShowMessageBox("Error", "Feature not implemented.", MessageBoxType.Error);
    }
}
