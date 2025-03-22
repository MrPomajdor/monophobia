using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UI;

public class ContainerMover : MonoBehaviour
{
    public float BoxW;
    public float Padding;
    private Vector3 targetPos;
    private Vector3 startPos;
    private RectTransform xd;
    HorizontalLayoutGroup lay;
    int elem;
    void Start()
    {
        xd= GetComponent<RectTransform>();
        startPos = xd.anchoredPosition;
        lay = GetComponent<HorizontalLayoutGroup>(); 

    }

    // Update is called once per frame
    void Update()
    {
        elem = Mathf.Clamp(elem, 0, transform.childCount - 1);
        xd.anchoredPosition = Vector3.Lerp(xd.anchoredPosition, startPos- new Vector3((BoxW + Padding)*elem, 0, 0),Time.deltaTime*4);
        
    }

    public void MoveNext()
    {
        elem += 1;
        

    }

    public void MoveBack()
    {
        elem -= 1;


    }
}
