using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    bool menuVisible=false;
    bool lastMenuVisible=false;
    public Animator animator;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            menuVisible = !menuVisible;

            if(menuVisible)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = menuVisible;

            if (lastMenuVisible != menuVisible)
            {
                if (lastMenuVisible)
                    animator.Play("close");
                else
                    animator.Play("open");
                lastMenuVisible = menuVisible;

            }
        }
        

    }
}
