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
    public List<TMP_InputField> fieldsToClear = new List<TMP_InputField>();
    [SerializeField]
    private List<Transform> children  = new List<Transform>();


    void OnEnable()
    {
        foreach(Transform t in transform)
        {
            children.Add(t);
        }
            
    }
    public void ClearInputs()
    {
        foreach (TMP_InputField field in fieldsToClear)
        {
            field.text = string.Empty;
        }
    }
    public void Hide()
    {
        foreach (Transform child in children)
        {
            child.gameObject.SetActive(false);
        }

    }

    public void Show()
    {
        foreach (Transform child in children)
        {
         
            child.gameObject.SetActive(true);
        }
    }
}
