using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;


public class Flashlight : Item
{
    public Light m_Light;
    public GameObject glowingThing;
    public override void Interact()
    {
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
