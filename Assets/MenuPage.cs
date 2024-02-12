using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum MenuBlockType
{
    FullScreen,
    Popup
}


public class MenuPage : MonoBehaviour
{
    public MenuBlockType type;
    public string menu_name;
    public Vector3 start_pos;
    public List<TMP_InputField> fieldsToClear = new List<TMP_InputField>();
    public void ClearInputs()
    {
        foreach (TMP_InputField field in fieldsToClear)
        {
            field.text = string.Empty;
        }
    }
    public void Hide()
    {
        if (type == MenuBlockType.Popup)
            transform.position = new Vector3(transform.position.x + Screen.width, transform.position.y, transform.position.z);
        else
            Debug.Log("Can't close a full-screen menu!");
        
    }
}
