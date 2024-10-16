using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;





public abstract class NetworkCodeInteraction : MonoBehaviour
{


    public abstract void Interact(Player interactee, string message = "");
    public abstract void Initialize();

    public void InternalInteract(Player Interactee)
    {
        Interact(Interactee);
        SendInteractionInfo(Interactee);
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.codeInteractionMessage[0], ParseInteractableMessage);
        Initialize();
    }
    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.codeInteractionMessage[0], ParseInteractableMessage);
    }

    private void ParseInteractableMessage(Packet packet)
    {
        InteractionInfo intInfo = packet.GetJson<InteractionInfo>();
        Interact(Global.connectionManager.clients.FirstOrDefault(x => x.id == intInfo.PlayerID).connectedPlayer);
    }

    private void SendInteractionInfo(Player Interactee, string message = "")
    {
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.interactableMessage;
        InteractionInfo inf = new InteractionInfo();
        inf.PlayerID = Interactee.playerInfo.id;
        inf.InteractionMessage = message;
        packet.AddToPayload(JsonUtility.ToJson(inf));
    }



}
