using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    [SerializeField]
    private Image thumbnailImageUI;

    private bool selected;
    
    public void SetThumbnail(Sprite texture)
    {
        thumbnailImageUI.sprite = texture;
    }

    public void SetSelection(bool _selected)
    {
        //TODO
    }
}
