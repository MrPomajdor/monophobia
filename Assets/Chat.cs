using System.IO;
using System.Text;
using UnityEngine;
using TMPro;
public class Chat : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text text;
    public Animator animator;
    private bool chatOpen;
    private float chatOpenTime;
    public float MaxChatOpenTime;
    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.chatMessage[0], ParseChatPacket);
        inputField.onSubmit.RemoveListener(OnSubmitField);

    }
    private void Start()
    {

        Global.chat = this;
        inputField.onFocusSelectAll = true;
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.chatMessage[0], ParseChatPacket);
        inputField.onSubmit.AddListener(OnSubmitField);

        animator.Play("chat_close");
        chatOpen = false;
    }
    private void OnSubmitField(string msg)
    {
        if (inputField.text.Length <= 0)
            return;
        inputField.text = "";
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.chatMessage;
        packet.AddToPayload(msg);
        packet.Send(Global.connectionManager.stream);
       // AddMessage($"{Global.connectionManager.client_self.name} : {msg}");

    }

    private void ParseChatPacket(Packet packet)
    {

        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int msglen = reader.ReadInt32();
            string name = Encoding.UTF8.GetString(reader.ReadBytes(msglen));
            msglen = reader.ReadInt32();
            string msg = Encoding.UTF8.GetString(reader.ReadBytes(msglen));
            AddMessage($"{name} : {msg}");

        }

    }

    private void Update()
    {
        if (chatOpen)
        {
            chatOpenTime += Time.deltaTime;
            if (chatOpenTime >= MaxChatOpenTime)
            {
                animator.Play("chat_close");
                chatOpen = false;
            }
        }
        else
            chatOpenTime = 0;

        if (inputField.isFocused)
            chatOpenTime = 0;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            inputField.ActivateInputField();
            inputField.Select();
            animator.Play("chat_open");
            chatOpen = true;
        }
    }

    public void AddMessage(string msg,string color = "#8C8C8C")
    {
        animator.Play("chat_open");
        chatOpen = true;
        text.text += $"<{color}>{msg}</color>\n";
    }


}
