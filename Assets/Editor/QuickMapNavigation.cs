using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public class QuickMapNavigation : EditorWindow
{
    [MenuItem("Window/Quick Level")]
    public static void ShowWindow()
    {
        GetWindow(typeof(QuickMapNavigation));
    }

    private void OnGUI()
    {
        GUILayout.Label("Change level to:");
       if(GUILayout.Button("Main Menu")){
            EditorSceneManager.OpenScene("Assets/Maps/mainmenu/mainmenu.unity");
        }

        if (GUILayout.Button("Grid"))
        {
            EditorSceneManager.OpenScene("Assets/Maps/grid0/grid0.unity");
        }
        if (GUILayout.Button("Lobby"))
        {
            EditorSceneManager.OpenScene("Assets/Maps/lobby0/lobby0.unity");
        }
    }
}
