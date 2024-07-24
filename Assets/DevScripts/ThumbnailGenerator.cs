using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThumbnailGenerator : MonoBehaviour
{
    public GameObject obj;
    public Image UI_Image;

    Texture2D GenerateThumbnail(Camera cam)
    {
        var currentRT = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        cam.Render();

        Texture2D img = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        img.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        img.Apply();

        RenderTexture.active = currentRT;
        return img;
    }
    void Start()
    {
        
        
    }
    private void OnGUI()
    {
        if (GUILayout.Button("Capture"))
        {
            Sraka();
        }
    }
    void Sraka()
    {
        Texture2D tex = GenerateThumbnail(Camera.main);
        UI_Image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
    }


}
