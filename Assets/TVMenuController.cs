using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TVMenuController : MonoBehaviour
{
    public Image StaticImage;
    [Header("Texts")]
    public TMP_Text YouHostText;
    public TMP_Text HelloNickText;
    public TMP_Text MapSettingsToStart;

    [Header("Buttons")]
    public Button MapSettingsBtn;

    private List<Transform> _pages = new List<Transform>();
    private bool _changingMenu;
    private float _timer;
    private string _newMenu;
    void Start()
    {
        
    }
    private void OnEnable()
    {
        Global.connectionManager.AddLocalPlayerAction(OnLocalPlayer);
    }

    public void OnLocalPlayer()
    {
        StaticImage.enabled = false;
        YouHostText.text = Global.connectionManager.IsSelfHost ? "You are a host!" : "You are <u>not</u> a host.";
        HelloNickText.text = "Hello " + Global.connectionManager.client_self.connectedPlayer.playerInfo.name;
        MapSettingsToStart.enabled = Global.connectionManager.IsSelfHost;
        MapSettingsBtn.interactable = Global.connectionManager.IsSelfHost;

        foreach (Transform t in transform)
        {
            if (!t.CompareTag("TVUI_MenuPage"))
                continue;
            _pages.Add(t);
        }

        GoToMenu("Menu");
    }

    // Update is called once per frame
    void Update()
    {
        if( _changingMenu)
        {
            _timer += Time.deltaTime;
            StaticImage.enabled = true;
            if ( _timer >= 0.3f ) {

                ChangeMenu(_newMenu);

                StaticImage.enabled = false;
                _changingMenu = false;
                _timer = 0;
            }
        }
    }

    public void GoToMenu(string menuName)
    {
        _changingMenu = true;
        _newMenu = menuName;
    }

    private void ChangeMenu(string menuName)
    {
        foreach (Transform t in _pages)
        {

            if (t.name.ToLower() == _newMenu.ToLower())
            {
                t.gameObject.SetActive(true);
            }
            else
            {
                t.gameObject.SetActive(false);

            }
        }
    }
}
