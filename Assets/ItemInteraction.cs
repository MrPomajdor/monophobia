
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInteraction : MonoBehaviour
{
    private Camera m_Camera;
    [SerializeField]
    private float m_maxRaycastDistance = 10;
    [SerializeField]
    private float m_translateStrength = 10;
    public Item currentlyPickedUp {  get; private set; }
    private Collider currentlyPickedUpCollider;
    public Transform holdPoint;
    private float t;
    [SerializeField]
    private float lerpSpeed=1;
    void Start()
    {
        m_Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if(currentlyPickedUp != null)
            {
                currentlyPickedUp.item.activated = ! currentlyPickedUp.item.activated;
            } 
        }
            if (Input.GetKeyDown(KeyCode.F))
        {
            RaycastHit hit;
            if(Physics.Raycast(transform.position,transform.forward, out hit,m_maxRaycastDistance))
            {
                print(hit.collider.gameObject.name);
                if (hit.collider == null) return;

                if (currentlyPickedUp != null)
                {
                    currentlyPickedUp.transform.position = hit.transform.position;
                    currentlyPickedUp.pickedUp = false;
                    currentlyPickedUp = null;
                    currentlyPickedUpCollider.isTrigger = false;
                    currentlyPickedUpCollider = null;
                    return;
                }
                if (hit.collider.gameObject.GetComponent<Item>())
                {
                    Item _itm = hit.collider.gameObject.GetComponent<Item>();
                    switch (_itm.type)
                    {
                        case ItemType.PickUp:
                            currentlyPickedUp = _itm;
                            _itm.pickedUp = true;
                            hit.collider.isTrigger = true;
                            currentlyPickedUpCollider = hit.collider;
                            return;

                        case ItemType.StaticInteractive:
                            return;
                    }
                }
            }
        }

        if(currentlyPickedUp != null)
        {
            if(holdPoint == null)
            {
                Debug.LogError("Pick-up hold point is unnasigned!");
                return;
            }

            t += Time.deltaTime;
            currentlyPickedUp.transform.rotation = Quaternion.Lerp(currentlyPickedUp.transform.rotation, holdPoint.transform.rotation, t*lerpSpeed);//TODO: make it fucking work
            
            currentlyPickedUp.rb.velocity = (holdPoint.transform.position -currentlyPickedUp.transform.position)*m_translateStrength;
        }
        else {
            t = 0;
        }
    }
}
