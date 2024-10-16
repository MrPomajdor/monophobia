using UnityEngine;

public class PlayerSpawnPosition : MonoBehaviour
{
    public bool OneTime = false;
    void Start() { GetComponent<MeshRenderer>().enabled = false; }
}
