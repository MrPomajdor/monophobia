using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class test : MonoBehaviour
{
    public TMP_InputField field;
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            field.ActivateInputField();

            field.Select();
        }
    }
}
