using UnityEngine;

public class ItemInteraction : MonoBehaviour
{
    private Camera m_Camera;
    private Player SelfPlayer;
    [SerializeField]
    private float m_maxRaycastDistance = 10;
    [SerializeField]
    private float m_translateStrength = 10;
    public Item currentlyPickedUp { get; private set; }
    private Collider currentlyPickedUpCollider;
    public Transform holdPoint;
    private float t;
    [SerializeField]
    private float lerpSpeed = 1;
    public bool remotePickedUp { get; private set; }
    public bool remote;

    void Start()
    {
        m_Camera = GetComponent<Camera>();
        SelfPlayer = transform.root.GetComponent<Player>();
    }

    public void RemotePickUp(Item item)
    {
        remotePickedUp = true;
        currentlyPickedUp = item;
        currentlyPickedUpCollider = currentlyPickedUp.gameObject.GetComponent<Collider>();
        currentlyPickedUpCollider.isTrigger = true;

    }
    public void RemoteDrop()
    {
        Debug.Log("Remote Dropping item");
        remotePickedUp = false;
        currentlyPickedUpCollider.isTrigger = false;
        currentlyPickedUp.rb.velocity = m_Camera.transform.forward*5; //TODO: set a variable for strength
        currentlyPickedUp = null;
        currentlyPickedUpCollider = null;

    }

    // Update is called once per frame
    void Update()
    {
        

        if (currentlyPickedUp != null)
        {
            if (holdPoint == null)
            {
                Debug.LogError("Pick-up hold point is unnasigned!");
                return;
            }

            t += Time.deltaTime;
            currentlyPickedUp.transform.rotation = Quaternion.Lerp(currentlyPickedUp.transform.rotation, holdPoint.transform.rotation, t * lerpSpeed);//TODO: make it fucking work

            currentlyPickedUp.rb.velocity = (holdPoint.transform.position - currentlyPickedUp.transform.position) * m_translateStrength;
        }
        else
        {
            t = 0;
        }

        if (remote) return; //-----------------------------LOCAL CODE BELOW----------------------------

        if (Input.GetKeyDown(KeyCode.Mouse0) && currentlyPickedUp.item.canBeActivated)
        {
            if (currentlyPickedUp != null )
            {
                currentlyPickedUp.interactionInfo.activated = !currentlyPickedUp.interactionInfo.activated;
                currentlyPickedUp.UpdateInteractionNetwork();
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentlyPickedUp != null)
            {
                currentlyPickedUp.interactionInfo.pickedUp = false;
                currentlyPickedUp.interactionInfo.pickedUpPlayerID = -1;
                currentlyPickedUp.rb.velocity = m_Camera.transform.forward * 5;
                currentlyPickedUp.interactionInfo.pickedUp = false;
                currentlyPickedUp.interactionInfo.pickedUpPlayerID = -1;
                currentlyPickedUp.UpdateInteractionNetwork();
                currentlyPickedUp = null;
                currentlyPickedUpCollider.isTrigger = false;
                currentlyPickedUpCollider = null;

                return;
            }
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, m_maxRaycastDistance))
            {
                print(hit.collider.gameObject.name);
                if (hit.collider == null) return;

                
                if (hit.collider.gameObject.GetComponent<Item>())
                {
                    Item _itm = hit.collider.gameObject.GetComponent<Item>();

                    if (_itm.interactionInfo.pickedUp) return; //this is for checking if another player is currentyl holding it. If yes, go fuck yourself.

                    switch (_itm.type)
                    {
                        case ItemType.PickUp:
                            currentlyPickedUp = _itm;
                            _itm.interactionInfo.pickedUp = true;
                            currentlyPickedUp.interactionInfo.pickedUp = true;
                            currentlyPickedUp.interactionInfo.pickedUpPlayerID = SelfPlayer.playerInfo.id;
                            currentlyPickedUp.UpdateInteractionNetwork();
                            hit.collider.isTrigger = true;
                            currentlyPickedUpCollider = hit.collider;
                            return;

                        case ItemType.StaticInteractive:
                            return;
                    }
                }
            }
        }
    }
}
