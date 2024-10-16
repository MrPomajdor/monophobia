using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [SerializeField]
    public float alcohol { get; set; } // (?, ?)
    [SerializeField]
    public float sanity { get; set; } // (-100, 100)

    private void Update()
    {
        sanity = Mathf.Clamp(sanity, -100, 100);
    }
}
