using UnityEngine;

public abstract class NetworkTransform : MonoBehaviour
{
    private Rigidbody NetworkRb;
    public Transforms NetworkTransforms { get; private set; } = new Transforms();
    private Transforms NetworkTransformsLast = new Transforms();

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
            NetworkTransforms = itemPosData.transforms;
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
            if ((lastUpdateTime >= updateInterval || lastUpdateTime>5 ) && !HoldUpdate)
            {
                if (!CompareTransforms(NetworkTransformsLast, NetworkTransforms) || lastUpdateTime>5)
                {
                    CopyToLast();
                    SendItemLocationInfo();

                    lastUpdateTime = 0;
                }
            }
        }
        else
        {
            if ((NetworkTransformsLast == null || NetworkTransformsLast != NetworkTransforms) && !HoldUpdate)
            {
                //Debug.Log("PIZDA");
                //NetworkTransformsLast = NetworkTransforms;
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
        Debug.Log($"Sending pos data from {NetworkTransformID}");
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
}
