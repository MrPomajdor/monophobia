using UnityEngine;

public class TVUI_Activator : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform cam;
    Vector3 mainPos;
    Quaternion mainRot;
    Camera xd;
    bool inTV = false;
    void Start()
    {
        mainPos = cam.position;
        mainRot = cam.rotation;
        cam.gameObject.SetActive(false);

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
                cam.gameObject.SetActive(false);
                return;
            }
            xd = FindAnyObjectByType<ConnectionManager>().client_self.connectedPlayer.cam;

            RaycastHit hit;
            if (Physics.Raycast(xd.transform.position, xd.transform.forward, out hit, 15))
            {
                if (!hit.collider.gameObject.CompareTag("TVUI")) return;

                if (Vector3.Distance(xd.transform.position, transform.position) > 15) return;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                inTV = true;
                cam.gameObject.SetActive(true);

                cam.rotation = xd.transform.rotation;
                cam.position = xd.transform.position;

            }
        }




    }
}

