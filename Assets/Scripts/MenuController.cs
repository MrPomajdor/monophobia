using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    bool menuVisible=false;
    bool lastMenuVisible=false;
    public Animator animator;
    public Transform uiCanvas;
    void Start()
    {
        if (!menuVisible)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;

        Cursor.visible = menuVisible;

        animator.Play("close");
    }

    // Update is called once per frame
    void Update()
    {
       
        if(NonUIInput.GetKeyDown(KeyCode.Escape)) {
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
