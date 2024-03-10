using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurController : MonoBehaviour
{
    public float Value;
    public Material material;

    // Update is called once per frame
    void Update()
    {
        material.SetFloat("_Amount", Value);
    }
}
