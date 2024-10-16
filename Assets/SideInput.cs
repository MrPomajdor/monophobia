using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SideInput : StandaloneInputModule
{
    public void ClickAt(Vector2 pos, bool pressed)
    {
        Debug.Log($"Clicking at {pos}");
        Input.simulateMouseWithTouches = true;
        var pointerData = GetTouchPointerEventData(new Touch()
        {
            position = pos,
        }, out bool b, out bool bb);

        ProcessTouchPress(pointerData, pressed, !pressed);
    }
}
