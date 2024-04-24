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
    ConnectionManager conMan;
    [field: SerializeField]
    public Rigidbody rb { get; private set; }
    [field: SerializeField]
    public Transforms transforms;
    public float lastTime;

    [field: SerializeField]
    public VoiceManager voice { get; private set; }
    public Stats stats { get; private set; }
    private SoundEffectsManager sfxManager;
    [SerializeField]
    private bool m_debug = false;
    public Inputs inputs = new Inputs();
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        conMan = FindObjectOfType<ConnectionManager>();
        stats = GetComponent<Stats>();
        sfxManager = GetComponent<SoundEffectsManager>();
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
        GUI.Label(new Rect(10, 10, 500, 1000), $"Local Player ID: {playerInfo.id}\nName: {conMan.client_self.name}\nServer ip: {conMan._IPAddress}\n\nMIC VOL: {voice.lastMicVolume}\nMIC ACTIVE: {voice.MicrophoneActive}", style);
    }
    void Update()
    {
        
        //TODO: (and the one more) transforms.target_velocity = 
        
        if (!playerInfo.isLocal)
        {  //-----------------------------------------REMOTE CODE------------------------------------------------
            Tools.UpdatePos(transform, rb, transforms,this, inputs);
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
                conMan.SendPlayerLocationInfo(this);
                lt2 = 0;
            }



        }
    }




}
