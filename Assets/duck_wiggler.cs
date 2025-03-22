using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class duck_wiggler : MonoBehaviour
{

    float t;
    Vector3 target, start;
    public float WiggleMag = 2;
    public float WiggleFreq = 2;
    public float WiggleLerpSpeed = 3;
    public float WiggleRotSpeed = 120;
    void Start()
    {
        start = transform.position;
        target = start;
    }


    void Update()
    {
        transform.eulerAngles = new Vector3(0,0,transform.eulerAngles.z + Time.deltaTime * WiggleRotSpeed);
        if (transform.eulerAngles.z >= 360)
            transform.eulerAngles = Vector3.zero;
        if (t > 1 / WiggleFreq)
        {
            target = start + new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f) * WiggleMag;
            t = 0;
        }
        t += Time.deltaTime;

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * WiggleLerpSpeed);

        
    }
}
