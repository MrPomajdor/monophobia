using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Mob : MonoBehaviour
{

    //okay so a mob should have an id too, so the host can send info about the position to the clients
    public MobData mobSettings;
    ConnectionManager conMan;
    public Vector3 targetPoint {  get; private set; }
    public Transform targetTransform {  get; private set; }

    private float t,t_tic;
    private NavMeshAgent agent;
    private bool pointTarget;


    void Start()
    {
        conMan = FindAnyObjectByType<ConnectionManager>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed += (int)mobSettings.aggresivness*3;
        agent.angularSpeed += ((int)mobSettings.aggresivness*25)/2;
        agent.acceleration += (int)mobSettings.aggresivness * 10;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (conMan.client_self.connectedPlayer == null)
            return;

        if (conMan.client_self.connectedPlayer.playerInfo.isHost) // I hate how that looks
        {
            if (!pointTarget && targetTransform != null) //if the mobs target is set to a transform, always update the destination position;
            {
                Vector3 xd = targetTransform.position;
                xd += -targetTransform.forward * 2; //TODO: check if player has turned around 180 so the mob can stay in place and scare player (sometimes)
                agent.SetDestination(xd);
            }
            t += Time.deltaTime;
            t_tic += Time.deltaTime;
            if (t > 0.5f)
            {
                t = 0; //this line is first because if the SendPositionData errors out it wont cause lag
                SendPositionData();
            }
            if (t_tic > 1)
            {
                t_tic = 0;
                BrainTick();
                Dupa();
            }
            
        }
        else
        {

        }
    }

    //Autor: Mateusz Olszewski
    public void Dupa()
    {


        print("witamn");
    }

    public void BrainTick()
    {
        print("Mob brain tick!");

        bool rng = UnityEngine.Random.value < 0.30f * Remap((int)mobSettings.aggresivness,1,6,1,3.44f);
       
        if (rng)
            switch (mobSettings.targetPointMode)
            {
                case TargetPointMode.Player:
                    Vector3 newPoint = RandomNavSphere(transform.position, mobSettings.WanderRange, -1); //TODO: rethink if always randomizing the point is a good idea
                    switch (mobSettings.personality)
                    {
                        
                        case Personality.Dumb:
                            //he's just dumb (walks in a random direction always)
                            SetTarget(newPoint);
                            break;
                        case Personality.Curious:
                            newPoint += transform.position - GetPlayer(mobSettings.targetSelectionMode).connectedPlayer.transform.position;
                            SetTarget(newPoint);
                            break;
                        case Personality.Fucker:
                            ClientHandle cli = GetPlayer(mobSettings.targetSelectionMode);
                            if (cli != null)
                            {
                                if(UnityEngine.Random.value < 0.50f * Remap((int)mobSettings.aggresivness, 1, 6, 1, 1.25f))
                                    SetTarget(cli.connectedPlayer.transform);
                                else
                                    SetTarget(cli.connectedPlayer.transform.position + cli.connectedPlayer.transform.forward*5);

                            }
                            else
                                Debug.LogError("SHIDSFSDFSDFSDFFDSDFSDSFDSFDSFSDFFFFFFFFFFFFFF");
                            break;
                    }
                    
                    break;
            }


    }
    //code yanked from https://forum.unity.com/threads/solved-random-wander-ai-using-navmesh.327950/
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }

    //TODO: Send info to other clients (if we're a host) that a mob changed it's target
    private void SetTarget(Transform _target)
    {
        targetTransform = _target;
        pointTarget = false;
        
    }
    private void SetTarget(Vector3 _target)
    {
        agent.SetDestination(_target);
        targetPoint = _target;
        pointTarget = true;
        
    }


    private void SendPositionData()
    {
        //TODO: Send position data
    }
    public void SetPositionData()
    {

    }

    public ClientHandle GetPlayer(TargetSelectionMode mode)
    {

        List<ClientHandle> cl = new List<ClientHandle>();
        cl.AddRange(conMan.clients);
        cl.Add(conMan.client_self);

        switch (mode) {
            case TargetSelectionMode.Closest:
            case TargetSelectionMode.Farthest:
                ClientHandle currSelected = null;
                float currSelectedDist = 0;
                foreach (ClientHandle pls in cl)
                {
                    float dist = Vector3.Distance(transform.position, pls.connectedPlayer.transform.position);
                    if ((mode == TargetSelectionMode.Closest && dist < currSelectedDist) || (mode == TargetSelectionMode.Farthest && dist > currSelectedDist))
                    {
                        currSelected = pls;
                        currSelectedDist = dist;
                    }

                }
                return currSelected;
            case TargetSelectionMode.Random:
                return cl[UnityEngine.Random.Range(0, cl.Count)];
            case TargetSelectionMode.TargetInFOV:
                throw new NotImplementedException("PLEASE IMPLEMENT TARGET IN FOV SELECTON");
        }
        return null;
    }


    float Remap(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }
}
