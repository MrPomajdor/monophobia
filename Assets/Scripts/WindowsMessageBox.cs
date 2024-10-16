using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class WindowsMessageBox : MonoBehaviour
{

    // Import the MessageBox function from user32.dll
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);


    // Function to show the message box
    public static void ShowMessageBox(string message, string title, uint uType=0)
    {
        MessageBox(IntPtr.Zero, message, title, uType);
    }
}