using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    private MenuPage[] pages;
    private MenuPage currentPage;
    ConnectionManager conMan;

    void Start()
    {
        pages = FindObjectsOfType<MenuPage>();
        foreach (MenuPage page in pages)
        {
            page.start_pos = page.transform.position; //save starting position
        }


        ChangeMenu("main");
        HideAllPopups();
        conMan = FindObjectOfType<ConnectionManager>(); 
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMenu(string nm)
    {
        
        MenuPage pg = GetPage(nm);
        if(pg.type != MenuBlockType.FullScreen)
        {
            Debug.LogError("[MenuMamanger] Use ShowPopup to show popups and not ChangeMenu.");
            return;
        }
        if (pg != null) 
        {
            foreach (MenuPage page in pages)
            {
                if (page.type == MenuBlockType.Popup)
                    continue;
                page.transform.position = new Vector3(page.start_pos.x + Screen.width, page.start_pos.y, page.start_pos.z);
            }

            if (currentPage != null)
                currentPage.transform.position = new Vector3(pg.start_pos.x + Screen.width, pg.start_pos.y , pg.start_pos.z);
            currentPage = pg;
            currentPage.transform.position = pg.start_pos;
            pg.ClearInputs();
        }
        else
            Debug.LogError($"[MenuManager] Page {nm} not found in scene");
    }

    public MenuPage GetPage(string nm)
    {
        return pages.FirstOrDefault(x => x.menu_name.ToLower() == nm.ToLower()); ;
    }

    public void ShowPopup(string nm)
    {
        MenuPage pg = GetPage(nm);

        if (pg.type != MenuBlockType.Popup)
        {
            Debug.LogError("[MenuManager] Use ChangeMenu() to change fullscreen menus");
            return;
        }

        if (pg == null)
        {
            Debug.LogError($"[MenuManager] Popup {nm} not found in scene");
            return;
        }


        pg.transform.position = pg.start_pos;
        pg.ClearInputs();

    }

    public void HideAllPopups()
    {
        foreach (MenuPage pg in pages)
        {
            if(pg.type == MenuBlockType.Popup)
                pg.transform.position = new Vector3(pg.start_pos.x + Screen.width, pg.start_pos.y, pg.start_pos.z);
        }
    }


    //Logic \/
    public void CreateLobby(MenuPage dialog)//TODO: IMPORTANT!! check if input data is correct
    {
        TMP_InputField[] all = dialog.GetComponentsInChildren<TMP_InputField>();
        TMP_InputField name = null;
        TMP_InputField players = null;
        TMP_InputField password = null;
        foreach (TMP_InputField pg in all)
        {
            if (pg.gameObject.name.Contains("name"))
                name = pg;
            else if (pg.gameObject.name.Contains("players"))
                players = pg;
            else if (pg.gameObject.name.Contains("password"))
                password = pg;
        }
        if (name == null || players == null || password == null)
            return;
        int max_players;
        if(!int.TryParse(players.text,out max_players))
        {
            return;
        }
        conMan.CreateLobby(name.text,max_players,password.text);
        
    }
}
