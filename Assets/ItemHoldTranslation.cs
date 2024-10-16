using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHoldTranslation : MonoBehaviour
{
    private InventoryManager inventoryManager;
    public Transform holdPoint;
    public float TranslateStrength=2;
    public float LerpSpeed;

    void Start()
    {
        inventoryManager = transform.root.GetComponent<InventoryManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (inventoryManager.current == null) return;

        inventoryManager.current.transform.rotation = Quaternion.Slerp(inventoryManager.current.transform.rotation, holdPoint.rotation,  LerpSpeed*Time.deltaTime);

        inventoryManager.current.rb.velocity = (holdPoint.position - inventoryManager.current.transform.position) * TranslateStrength;
    }
}
