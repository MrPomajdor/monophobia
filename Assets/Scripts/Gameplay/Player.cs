using UnityEngine;
using UnityEngine.UIElements;
[RequireComponent(typeof(Stats))]
[RequireComponent(typeof(SoundEffectsManager))]
public class Player : MonoBehaviour
{
    public PlayerInfo playerInfo;
    [field: SerializeField]
    public Camera cam { get; private set; }
    public MouseRotation mouseRotation;
    public Movement movement;
    [field: SerializeField]
    public Rigidbody rb { get; private set; }
    [field: SerializeField]
    public Transforms transforms;
    public float lastTime;

    [field: SerializeField]
    public VoiceManager voice { get; private set; }
    public Stats stats { get; private set; }
    private SoundEffectsManager sfxManager;
    private FootstepsSFX footsteps;
    [SerializeField]
    private bool m_debug = false;
    public Inputs inputs = new Inputs();
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<Stats>();
        sfxManager = GetComponent<SoundEffectsManager>();
        footsteps = GetComponent<FootstepsSFX>();
    }
    Vector3 prev, prevrot;
    // Update is called once per frame
    float lt2, lt3;
    bool s;
    GUIStyle style = new GUIStyle();
    private void OnGUI()
    {
        if (!playerInfo.isLocal) return;
        if (!m_debug) return;

        style.fontSize = 30;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 500, 1000), $"Local Player ID: {playerInfo.id}\nName: {Global.connectionManager.client_self.name}\nServer ip: {Global.connectionManager._IPAddress}\n\nMIC VOL: {voice.lastMicVolume}\nMIC ACTIVE: {voice.MicrophoneActive}", style);
    }
    float t;

    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.transformData[0], ParseTransformData);
    }
    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.transformData[0], ParseTransformData);
    }

    public void ParseTransformData(Packet packet)
    {
        PlayersDataPacket json_ = packet.GetJson<PlayersDataPacket>();
        foreach (PlayerData player in json_.players) // for each player in recieved json
        {

            if (player.id == Global.connectionManager.client_self.id)
                continue;
            if(player.id == playerInfo.id)
            {
                transforms = player.transforms;
                lastTime = Time.realtimeSinceStartup;
                movement.col.height = player.inputs.isCrouching ? 0.8f : 2f;
                inputs = player.inputs;
                stats.sanity = player.stats.sanity;
                stats.alcohol = player.stats.alcohol;
                break;
            }
        }
    }


    void Update()
    {
        
        //TODO: (and the one more) transforms.target_velocity = 
        
        if (!playerInfo.isLocal)
        {  //-----------------------------------------REMOTE CODE------------------------------------------------
            t += Time.deltaTime;
            Tools.UpdatePos(transform, rb, transforms,this, inputs,5f);
            if(inputs.isMoving)
            {
                float sin;
                if(inputs.isSprinting)
                    sin = Mathf.Sin(t * 2) * 0.1f;
                else
                    sin = Mathf.Sin(t) * 0.1f;

                if (sin < 0.05) footsteps.PlayStepSound();
            }
        }
        else
        {  //-----------------------------------------LOCAL CODE-------------------------------------------------
            lt2 += Time.deltaTime;
            lt3 += Time.deltaTime;
            transforms.position = transform.position;
            transforms.real_velocity = rb.velocity;

            //periodic position sending
            if (lt2 > 0.15f)
            {
                SendPlayerLocationInfo();
                lt2 = 0;
            }



        }
    }

    public void SendPlayerLocationInfo()
    {
        Transforms transforms_ = transforms;
        transforms_.rotation = movement.GetAngles();

        PlayerData playerData = new PlayerData();
        playerData.inputs = inputs;
        playerData.id = playerInfo.id;
        playerData.transforms = transforms_;

        string mes = JsonUtility.ToJson(playerData);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.playerTransformData;
        packet.AddToPayload(mes);
        packet.Send(Global.connectionManager.udp_handler.client, Global.connectionManager.udp_handler.remoteEndPoint);
    }


}
