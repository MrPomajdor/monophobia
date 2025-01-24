using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class UI_IDCheck : EditorWindow
{

    [MenuItem("Window/Quick IDs")]
    public static void ShowWindow()
    {
        GetWindow(typeof(UI_IDCheck));
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Check"))
        {
            NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
            foreach (NetworkObject networkObject in networkObjects)
            {
                if (string.IsNullOrEmpty(networkObject.ObjectID))
                {
                    networkObject.GenerateID();
                    Debug.Log($"Generated ID for {networkObject.gameObject.name}");
                }
            }
            Debug.Log("ID Check done!");
        }
        
    }
}
