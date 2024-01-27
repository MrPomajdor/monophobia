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
    public int local_port;
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
        remoteEndPoint = new IPEndPoint(IPAddress.Parse("88.135.184.123"), remote_port);

        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }
                               
    private void ReceiveData() //TODO: 25.01.2024 Send Transform packets when moving (DONE),Recieve transform packets (in progress), make a handler for udp packets, and shit like that
                               //im going to bed now its fucking 3 am and im not going to school because i want to do this nonesense.
                               //Bye future me please dont be mad :c
                               //26.01.2024 i was mad for the first 5 seconds xd
                               //27.01.2024 WHY THE FUCK THE CONNECTION IS CLOSING WHEN MORE THAN 1 PLAYERS JOIN FOR FUCKS SAKE
                               //oh wait that is on server's side
                               //why the fuck the server wants to send on port 0?
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
                    foreach (ClientHandle client in conMan.clients)
                    {
                        if (client.id == conMan.client_self.id)
                            continue;
                        //Debug.Log($"OG Text: {text}, type {t.type} and final json {json_.players.Length}");
                        Debug.Log($"Got player with id {client.id}");
                        PlayerData matchingPlayer = json_.players.FirstOrDefault(x => x.id == client.id);

                        if (matchingPlayer != null)
                        {
                            ThreadManager.ExecuteOnMainThread(() =>
                            {
                                client.connectedPlayer.velocity = new Vector3(matchingPlayer.transforms.velocity.x, matchingPlayer.transforms.velocity.y, matchingPlayer.transforms.velocity.z);
                                client.connectedPlayer.postion = new Vector3(matchingPlayer.transforms.position.x, matchingPlayer.transforms.position.y, matchingPlayer.transforms.position.z);
                                Vector3 rot = new Vector3(matchingPlayer.transforms.rotation.x, matchingPlayer.transforms.rotation.y, matchingPlayer.transforms.rotation.z);
                                client.connectedPlayer.rotation = rot;
                                client.connectedPlayer.lastTime = Time.realtimeSinceStartup;
                            });
                        }
                        else
                        {
                            Debug.LogWarning("no matching player found.");
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