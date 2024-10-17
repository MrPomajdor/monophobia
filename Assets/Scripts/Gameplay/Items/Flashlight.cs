using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Flashlight : Item
{
    public Light m_Light;
    public GameObject glowingThing;
    public override void Interact()
    {
        if(Global.connectionManager.client_self.connectedPlayer.playerInfo.isLocal)
            interactionInfo.activated = !interactionInfo.activated;
        Refresh();
        
    }
    public override void ItemStart()
    {
        Refresh();
    }

    private void Refresh()
    {
        m_Light.enabled = interactionInfo.activated;
        glowingThing.SetActive(interactionInfo.activated);
    }
}
