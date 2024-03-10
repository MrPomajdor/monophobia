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

    private void OnEnable()
    {
        pages = FindObjectsOfType<MenuPage>();
        foreach (MenuPage page in pages)
        {
            page.Hide();
        }
    }
    void Start()
    {
        


        ChangeMenu("main");
        HideAllPopups();
        conMan = FindObjectOfType<ConnectionManager>(); 
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }
    MenuPage pg=null;
    public void ChangeMenu(string nm) //26.02.2024 WHY THE FUCK THIS STOPS FUCKING WORKING OUT OF NOWHERE WHEN I TRY TO ACCESS THE pg VARIABLE GOD DAMMIT
                                      //TODO: Fix this piece of dogshit (ChangeMenu)
    {
        if(nm == null)
        { Debug.LogError("[MenuManager] ChangeMenu argument can't be null"); return; }
        Debug.Log($"Changing menu to {nm}");
        //print($"gowno0");
        pg = GetPage(nm);
        //print($"gowno1 {nm} found {pg}");

        if (pg != null)
        {
            //print($"gowno2 {nm}");
            if (pg.type != MenuBlockType.FullScreen)
            {
                //Debug.LogError("[MenuMamanger] Use ShowPopup to show popups and not ChangeMenu.");
                return;
            }

            //print($"gowno3 {nm} {pages.Length} {pages}");

            for (int i = 0; i < pages.Length; i++)
            {
                //print($"GOWNO KURWA MAC");
            }
            /*foreach (MenuPage page in pages)
            {
                if (page.type == MenuBlockType.Popup)
                    continue;

                page.Hide();
            }*/
            pg.Show();
            pg.ClearInputs();
            //print($"gowno5 {nm}");
            currentPage = pg;
        }
        else
            Debug.LogError($"[MenuManager] Page {nm} not found in scene");
    }

    public MenuPage GetPage(string nm)
    {
        foreach (MenuPage page in pages)
        {
            if(page.menu_name == nm)
                return page;
        }
        return null;
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


        pg.Show();
        pg.ClearInputs();

    }

    public void HideAllPopups()
    {
        foreach (MenuPage pg in pages)
        {
            if(pg.type == MenuBlockType.Popup)
                pg.Hide();
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
        if(max_players < 2)
        {
            //TODO: USER INPUT ERROR HANDLING (in this case MenuManager CreateLobby max players)
        }
        conMan.CreateLobby(name.text,max_players,password.text);
        
    }
}
