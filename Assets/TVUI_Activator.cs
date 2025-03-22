using UnityEngine;
using UnityEngine.EventSystems;

public class TVUI_Activator : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform cam;
    Vector3 mainPos;
    Quaternion mainRot;
    Camera xd;
    bool inTV = false;
    public EventSystem pierdolsie;
    public EventSystem pierdolsie_pl;
    void Start()
    {
        mainPos = cam.position;
        mainRot = cam.rotation;
        cam.gameObject.SetActive(false);
        NonUIInput.ForceBlock = false;
            
    }
    private void OnEnable()
    {
        Global.connectionManager.AddLocalPlayerAction(asdasd);
    }

    public void asdasd()
    {
        pierdolsie_pl = Global.connectionManager.client_self.connectedPlayer.transform.Find("InGameUI").GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        cam.transform.position = Vector3.Lerp(cam.transform.position, mainPos, Time.deltaTime * 5.25f);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, mainRot, Time.deltaTime * 5.25f);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inTV)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                inTV = false;
                NonUIInput.ForceBlock = false;
                cam.gameObject.SetActive(false);
                return;
            }
            xd = Global.connectionManager.client_self.connectedPlayer.cam;

            

            RaycastHit hit;
            if (Physics.Raycast(xd.transform.position, xd.transform.forward, out hit, 15))
            {
                Debug.Log(hit.transform.gameObject.name);
                Debug.Log(hit.transform.root.name);
                if (!hit.collider.gameObject.CompareTag("TVUI")) return;

                if (Vector3.Distance(xd.transform.position, transform.position) > 15) return;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                inTV = true;
                NonUIInput.ForceBlock = true;

                cam.gameObject.SetActive(true);

                cam.rotation = xd.transform.rotation;
                cam.position = xd.transform.position;

            }

            
            
        }
        if(pierdolsie_pl != null)
             pierdolsie_pl.enabled = !inTV;
        //pierdolsie.enabled = inTV;



    }
}

