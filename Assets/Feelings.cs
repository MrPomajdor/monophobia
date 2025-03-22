using System;
using UnityEngine;

public class Feelings : MonoBehaviour
{
    public AudioSource audioSource;
    private float breathTanhK = 1.5f;
    public Heartbeat heartbeat;
    public Movement movement;
    private float runningTime;
    public float RunningTimeBPMThreshold;
    float timer;
    [SerializeField]
    public float Stamina { get; private set; }
    public float BreathFunction { get; private set; }
    public AudioClip[] BreathIn;
    public AudioClip[] BreathOut;
    bool breath;
    float breathSoundFunction;
    void Start()
    {

    }

    public static float DirtyTangentInterpolation(float a, float b, float t)
    {
        return (float)(a + (b - a) * ((1.55741 / 3) + (Math.Tan(t * 2 - 1) / 3)));
    }
    float nextBreath;
    float brFunc = 0;
    void Update()
    {
        //float k = Stamina != 0 ? 1f - Stamina : 3;
        // breathSoundFunction = Mathf.Sin(Time.realtimeSinceStartup * (1f + (2f * k)));
        float breathInterval = 1f / (1f + (2f * (1f - Stamina)));
        BreathFunction = Mathf.Lerp(BreathFunction, brFunc, Time.deltaTime);
        
        if (Time.time >= nextBreath)
        {
            if (breath)
            {
               if(Stamina < 0.6f) audioSource.PlayOneShot(BreathOut[UnityEngine.Random.Range(0, BreathOut.Length)]);
                brFunc = 1;
            }
            else
            {
                if (Stamina < 0.6f) audioSource.PlayOneShot(BreathIn[UnityEngine.Random.Range(0, BreathIn.Length)]);
                brFunc = -1;
            }


            breath = !breath;
            nextBreath = Time.time +breathInterval;
        }
        

        if (movement.isSprinting)
        {
            runningTime += Time.deltaTime;
            timer += Time.deltaTime;
            if (runningTime > RunningTimeBPMThreshold)
            {
                Stamina -= Time.deltaTime * 0.05f;

                if (timer > 1)
                {
                    if (heartbeat.CurrentBPM < 170)
                        heartbeat.IncreaseBPM(20); //~12 seconds of running == 300 BPM

                    timer = 0;
                }
            }

        }
        else
        {
            runningTime -= Time.deltaTime * 3;
            Stamina += Time.deltaTime * 0.07f;
        }

        Stamina = Mathf.Clamp01(Stamina);
        if (runningTime < 0)
            runningTime = 0;
    }
    GUIStyle style = new GUIStyle();
    private void OnGUI()
    {
        style.fontSize = 25;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 400, 500, 1000), $"Stamina : {Stamina}\nSin value : {breathSoundFunction}\nMultiplier : {(2f + (2f * (1f - Stamina)))}", style);
        GUI.Label(new Rect(10, 600 + breathSoundFunction * 30, 500, 1000), $"SIN VISUALIZATION", style);

    }
}
