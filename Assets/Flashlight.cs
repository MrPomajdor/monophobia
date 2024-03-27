using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Item))]
public class Flashlight : MonoBehaviour
{
    private Item m_Item;
    public Light m_Light;
    public GameObject glowingThing;
    void Start()
    {
        m_Item = GetComponent<Item>();
    }

    // Update is called once per frame
    void Update()
    {
        m_Light.enabled = m_Item.item.activated;
        glowingThing.SetActive(m_Item.item.activated);
    }
}
