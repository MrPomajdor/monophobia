using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPHandler : MonoBehaviour
{
    Thread receiveThread;

    UdpClient client;
    IPEndPoint remoteEndPoint;
    int local_port = 0;
    int remote_port = 1338;
    ConnectionManager conMan;
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = "";


    // Use this for initialization
    void Start()
    {
        conMan = FindObjectOfType<ConnectionManager>();
        local_port = UnityEngine.Random.Range(2000, 5000);
        client = new UdpClient(local_port);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), remote_port);

        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void ReceiveData() //TODO: Send Transform packets when moving (DONE),Recieve transform packets (in progress), make a handler for udp packets, and shit like that
                               //im going to bed now its fucking 3 am and im not going to school because i want to do this nonesense.
                               //Bye future me please dont be mad :c
                               //i was mad for the first 5 seconds xd
    {

        while (true)
        {

            //try
            //{
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref anyIP);
            string text = Encoding.UTF8.GetString(data);
            lastReceivedUDPPacket = text;
            var t = JsonUtility.FromJson<UDPPacket>(text);
            switch (t.type)
            {
                case "OtherPlayersPositionData":
                    PlayersDataPacket json_ = JsonUtility.FromJson<PlayersDataPacket>(text);
                    Debug.Log(json_);
                    Debug.Log(text);
                    foreach (ClientHandle client in conMan.clients)
                    {
                        //Debug.Log($"OG Text: {text}, type {t.type} and final json {json_.players.Length}");

                        PlayerData matchingPlayer = json_.players.FirstOrDefault(x => x.id == client.id);

                        if (matchingPlayer != null)
                        {
                            client.connectedPlayer.movement.rb.velocity = new Vector3(matchingPlayer.transforms.velocity.x, matchingPlayer.transforms.velocity.y, matchingPlayer.transforms.velocity.z);
                            client.connectedPlayer.movement.transform.position = new Vector3(matchingPlayer.transforms.position.x, matchingPlayer.transforms.position.y, matchingPlayer.transforms.position.z);
                            Vector3 rot = new Vector3(matchingPlayer.transforms.rotation.x, matchingPlayer.transforms.rotation.y, matchingPlayer.transforms.rotation.z);
                            client.connectedPlayer.movement.transform.eulerAngles = new Vector3(rot.x, rot.y, 0);
                            client.connectedPlayer.cam.transform.eulerAngles = new Vector3(rot.z, client.connectedPlayer.cam.transform.eulerAngles.y, client.connectedPlayer.cam.transform.eulerAngles.z);
                        }

                    }
                    break;
            }
            //}
            //catch (Exception err)
            //{
            //    Debug.LogError(err.Message);
            //}
        }
    }

    public void Send()
    {

    }
    public void SendString(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }





    public string getLatestUDPPacket()
    {
        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }
}