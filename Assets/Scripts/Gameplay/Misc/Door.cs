using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    [SyncVariable]
    float openDegree = 0;
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Interact(Player interactee)
    {
        openDegree += 1;
        SyncVariables();
    }

    GUIStyle labelStyle = new GUIStyle();
    void OnGUI()
    {
        string t = $"ID : {ObjectID}\nVAL : {openDegree}";
        Vector3 p = transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(p);
        Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(t));

        labelStyle.fontSize = 38;
        labelStyle.normal.textColor = Color.green;

        GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, textSize.x, textSize.y), t, labelStyle);

    }
}
