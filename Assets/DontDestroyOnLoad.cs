
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    void Update()
    {
        DontDestroyOnLoad(this);    
    }
}
