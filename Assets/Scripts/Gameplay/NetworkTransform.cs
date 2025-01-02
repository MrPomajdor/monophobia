using UnityEngine;

public abstract class NetworkTransform : MonoBehaviour
{
    private Rigidbody NetworkRb;
    public float maxVelDiff = 15f;
    public Transforms NetworkTransforms { get; private set; } = new Transforms();
    public Transforms NetworkTransformsLast { get; private set; } = new Transforms();

    //This ID is currently set manually by the inherited class. Should change in future.
    //TODO : MakeNetworkTransformID somehow set the ID automatilcly or think of a better method.
    public int NetworkTransformID = -1;

    private float updateInterval = 0.1f;
    private float lastUpdateTime;

    public bool HoldUpdate = false;
    void Start()
    {

    }
    protected virtual void OnEnable()
    {
        NetworkRb = GetComponent<Rigidbody>();
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.transform[0], ParseTransformPacket);
    }


    protected virtual void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.transform[0], ParseTransformPacket);
    }

    public void ParseTransformPacket(Packet packet)
    {
        ItemPosData itemPosData = packet.GetJson<ItemPosData>();
        if (itemPosData == null)
            return;

        if (itemPosData.id == NetworkTransformID)
        {
            NetworkTransforms = itemPosData.transforms;
            lastUpdateTime = 0;
        }
        
    }
    private bool VecEq(Vector3 v1, Vector3 v2)
    {
        //Debug.Log(Vector3.Distance(v1, v2));
        return Vector3.Distance(v1, v2) < 0.1f; //arbitrary value!!!
    }

    private bool CompareTransforms(Transforms t1, Transforms t2)
    {
        //return false;
        return VecEq(t1.position, t2.position) &
                VecEq(t1.rotation, t2.rotation) &
                VecEq(t1.real_velocity, t2.real_velocity) &
                VecEq(t1.real_angular_velocity, t2.real_angular_velocity);
    }

    private void CopyToLast()
    {
        NetworkTransformsLast.position = NetworkTransforms.position;
        NetworkTransformsLast.rotation = NetworkTransforms.rotation;
        NetworkTransformsLast.real_velocity = NetworkTransforms.real_velocity;
        NetworkTransformsLast.real_angular_velocity = NetworkTransforms.real_angular_velocity;
    }
    float vel_dif;
    protected virtual void Update()
    {
        if (!Global.connectionManager.LocaPlayerInitialized()) return;

        if (Global.connectionManager.IsSelfHost)
        {
            NetworkTransforms.position = NetworkRb.transform.position;
            NetworkTransforms.rotation = NetworkRb.transform.eulerAngles;
            NetworkTransforms.real_velocity = NetworkRb.velocity;
            NetworkTransforms.real_angular_velocity = NetworkRb.angularVelocity;
            lastUpdateTime += Time.deltaTime;
            //if (NetworkTransformsLast == null) NetworkTransformsLast = NetworkTransforms;

            Vector3 curr_vel = NetworkRb.velocity;
            vel_dif = Mathf.Abs((curr_vel - NetworkTransformsLast.real_velocity).magnitude / Time.fixedDeltaTime);

            //if ((lastUpdateTime >= updateInterval || lastUpdateTime>5 ) && !HoldUpdate)
            //{
                if (((vel_dif>maxVelDiff && lastUpdateTime>0.2f) || lastUpdateTime>5) && !HoldUpdate)
                {
                    CopyToLast();
                    SendItemLocationInfo();

                    lastUpdateTime = 0;
                }
            //}
        }
        else
        {
            if ((NetworkTransformsLast == null || NetworkTransformsLast != NetworkTransforms) && !HoldUpdate)
            {
                //Debug.Log("PIZDA");
                //NetworkTransformsLast = NetworkTransforms;
                lastUpdateTime += Time.deltaTime;
                Tools.UpdatePos(transform, NetworkRb, NetworkTransforms);
            }
        }
    }

    public void SendItemLocationInfo()
    {
        if (NetworkTransformID < 0)
        {
            Debug.LogError($"NetworkTransformID has not been set for {name}");
            return;
        }
        ItemPosData tranformData = new ItemPosData();
        tranformData.transforms = NetworkTransforms;
        tranformData.id = NetworkTransformID;



        string mes = JsonUtility.ToJson(tranformData);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.transform;
        packet.AddToPayload(mes);
        packet.Send(Global.connectionManager.udp_handler.client, Global.connectionManager.udp_handler.remoteEndPoint);
    }
    GUIStyle labelStyle = new GUIStyle();
    void OnGUI()
    {
        string t = $"{lastUpdateTime.ToString("F2")}\n{vel_dif.ToString("F3")}";
        Vector3 p = NetworkRb.transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(p);
        Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(t));
        
        labelStyle.fontSize = 38;
        labelStyle.normal.textColor = new Color(1 - Mathf.Clamp(lastUpdateTime, 0, 3), Mathf.Clamp(lastUpdateTime, 0, 5), 0);

        GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, textSize.x, textSize.y), t, labelStyle);

    }
}
