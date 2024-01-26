using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Voice : MonoBehaviour
{
    AudioClip mic, recieved;
    public AudioSource src;
    int pos, lastPos;
    IPEndPoint endPoint;
    Socket sock;
    byte[] recieved_bytes;
    //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
    //UdpClient receivingUdpClient = new UdpClient(1338);
    private static int sizeOfBuffer = 4096;

    // Start is called before the first frame update
    void Start()
    {
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),1338);

        mic = Microphone.Start(null, true, 1, 22050);
        src.clip = recieved;
        src.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if ((pos = Microphone.GetPosition(null)) > 0)
        {
            if (lastPos > pos) lastPos = 0;
            if (pos - lastPos > 0)
            {
                int len = (pos - lastPos) * mic.channels;
                float[] samples = new float[len];
                mic.GetData(samples, lastPos);
                if (samples.Length * 4 <= 4096)
                {
                    byte[] byteArray = new byte[samples.Length * 4];
                    Debug.Log(byteArray.Length);
                    Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);

                    //recieving
                    IPEndPoint ep1 = new IPEndPoint(IPAddress.Any, 1338);
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        UdpClient receiveClient = new UdpClient();
                        receiveClient.ExclusiveAddressUse = false;
                        receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        receiveClient.Client.Bind(ep1);
                        recieved_bytes = receiveClient.Receive(ref ep1);
                    });
                    float[] floatArray2 = new float[byteArray.Length / 4];
                    Buffer.BlockCopy(recieved_bytes, 0, floatArray2, 0, byteArray.Length);
                    recieved.SetData(floatArray2,22050);
                    
                    //sending
                    UdpClient sendClient = new UdpClient();
                    sendClient.ExclusiveAddressUse = false;
                    sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    IPEndPoint ep2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1338);
                    sendClient.Client.Bind(ep1);
                    sendClient.Send(byteArray, sizeOfBuffer, ep2);
                }
                lastPos = pos;
            }
        }
    }

    private static void SendAndReceive()
    {
       
    }
}
