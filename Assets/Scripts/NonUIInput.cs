using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class NonUIInput
{
    // Check if is currently focused on input field
    public static bool ForceBlock = false;
    public static bool IsEditingInputField =>ForceBlock || (EventSystem.current.currentSelectedGameObject?.TryGetComponent(out TMPro.TMP_InputField _) ?? false) ;

    // conditional layers over UnityEngine.Input.GetKey methods
    public static bool GetKeyDown(KeyCode key) => IsEditingInputField ? false : Input.GetKeyDown(key);

    public static bool GetKeyUp(KeyCode key) => IsEditingInputField ? false : Input.GetKeyUp(key);

    public static bool GetKey(KeyCode key) => IsEditingInputField ? false : Input.GetKey(key);

    public static float GetAxis(string axis) => IsEditingInputField ? 0 : Input.GetAxis(axis);

    public static float GetAxisRaw(string axis) => IsEditingInputField ? 0 : Input.GetAxisRaw(axis);





}
