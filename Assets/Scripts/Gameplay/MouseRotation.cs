using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseRotation : MonoBehaviour
{
    public float speedH = 2.0f;
    public float speedV = 2.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public float maxPitch = 70;
    public float minPitch = -70;


    private bool drunkWalking;
    public Transform drunkDirection;
    private Vector3 tempDir;
    public int drunkMult = 60;
    public Vector3 rotAdd;
    float t = 0;
    public Stats stats;

    public Rigidbody rb;

    public float CameraTiltForce=1;

    public float LerpSpeed=18;
    Vector3 xx;
    GameObject pinPoint;
    private void Start()
    {
    }
    void Update()
    {

        //camera tilt when strafing.
        Vector3 tilt = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, -Input.GetAxisRaw("Horizontal") * CameraTiltForce);

        //lerping view bobbing and camera tilt
        xx = Vector3.Lerp(xx, tilt, Time.deltaTime * LerpSpeed);
        if (stats.alcohol > 0.01f)
            drunkWalking = true;
        else
            drunkWalking = false;

        yaw += speedH * Input.GetAxis("Mouse X");

        pitch -= speedV * Input.GetAxis("Mouse Y");
        

        if (pitch >= maxPitch)
            pitch = maxPitch - 0.5f;
        if (pitch <= minPitch)
            pitch = minPitch + 0.5f;
        if (drunkWalking)
        {
            Vector3 tempDir_ = new Vector3(drunkDirection.rotation.x * drunkMult * stats.alcohol, drunkDirection.rotation.z * drunkMult * stats.alcohol, drunkDirection.rotation.z * drunkMult * stats.alcohol);

            //smothing out the jump in camera rotation when you drink alcohol
            tempDir = Vector3.Lerp(tempDir, tempDir_, t);
            t += Time.deltaTime * 0.02f;
            if (Vector3.Distance(tempDir_, tempDir) < 0.1f)
                t = 0;


            Vector3 tempAngles = new Vector3(pitch, transform.root.eulerAngles.y, xx.z) + new Vector3(tempDir[0], 0, tempDir[2]);

            if (tempAngles[0] >= maxPitch)
                tempAngles[0] = maxPitch - 0.5f;
            else if (tempAngles[0] <= minPitch)
                tempAngles[0] = minPitch + 0.5f;

            /*
            transform.root.eulerAngles = new Vector3(transform.root.eulerAngles.x, yaw, 0.0f);// + new Vector3(0, tempDir[1], 0) + rotAdd;
            transform.eulerAngles = tempAngles;
            print(tempDir[0]);
            */
            transform.root.eulerAngles = new Vector3(transform.root.eulerAngles.x, yaw, 0);// + rotAdd;
            transform.eulerAngles = tempAngles;//new Vector3(pitch, transform.eulerAngles.y, 0.0f);
        }
        else
        {
            transform.root.eulerAngles = new Vector3(transform.root.eulerAngles.x, yaw, 0);// + rotAdd;
            transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y, xx.z);

        }
    }

    IEnumerator drunkMovement()
    {
        drunkWalking = true;
        yield return new WaitForSeconds(Random.Range(1, Mathf.Clamp(stats.alcohol * 10, 1, 2)));
        yield return new WaitForSeconds(Random.Range(0, 3 - Mathf.Clamp(stats.alcohol * 5, 0, 4)));
        drunkWalking = false;

    }
}


