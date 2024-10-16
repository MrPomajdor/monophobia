using UnityEngine;

public class TVUI_Interaction : MonoBehaviour
{
    public SideInput input;
    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    private Vector3 MouseToPlane()
    {

        RaycastHit hit;
        Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 hitPoint = hit.point;
            if (!hit.collider.gameObject.CompareTag("TVUI")) { Debug.Log("Cursor out of bounds"); return Vector3.zero; }

            return hit.collider.transform.InverseTransformPoint(hitPoint);

        }
        return Vector3.zero;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector3 planePos = MouseToPlane();
            

            input.ClickAt(new Vector2(Remap(-planePos.x, -5, 5, 0, 1500), Remap(-planePos.z, -5, 5, 0, 1000)), true);
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            Vector3 planePos = MouseToPlane();

            input.ClickAt(new Vector2(Remap(-planePos.x, -5, 5, 0, 1500), Remap(-planePos.z, -5, 5, 0, 1000)), false);
        }
    }

}
