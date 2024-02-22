
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Toolz;
public class UDPHandler : MonoBehaviour 
{
    Thread receiveThread;

    public UdpClient client { get; private set; }
    public IPEndPoint remoteEndPoint { get; private set; }
    int remote_port = 1338;
    ConnectionManager conMan;
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = "";


    void SendHolePunchingPacket()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("holepunch");
        try
        {
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch { }
    }

    // Use this for initialization
    void Start()
    {
        conMan = FindObjectOfType<ConnectionManager>();
        client = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(conMan._IPAddress), remote_port);

        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        InvokeRepeating("SendHolePunchingPacket", 0.5f, 0.5f);

    }

    // Update is called once per frame
    void Update()
    {

    }

    //TODO: 25.01.2024 Send Transform packets when moving (DONE),Recieve transform packets (in progress), make a handler for udp packets, and shit like that
    //im going to bed now its fucking 3 am and im not going to school because i want to do this nonesense.
    //Bye future me please dont be mad :c
    //26.01.2024 i was mad for the first 5 seconds xd
    //27.01.2024 WHY THE FUCK THE CONNECTION IS CLOSING WHEN MORE THAN 1 PLAYERS JOIN FOR FUCKS SAKE
    //oh wait that is on server's side
    //why the fuck the server wants to send on port 0?
    //NOW WHY THE FUCK THIS SHIT DOWN BELOW IS NOT RECIEVING DATAAAAAAA
    //01.02.2024 now i now why its not recieving data. NAT. A motherfucking nat. this shit wont fucking make the connection for me god damn for fucks sake mother fuckking shit eatin ass shakin booty breakin piece of fucking bulls shit.
    //i've done it :DDDD
    private void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        Debug.Log("UDP Recieving thread started!");
        while (true)
        {
            if (client.Available < 1)
                continue;

            try
            {         
                byte[] data = client.Receive(ref anyIP);
                conMan.HandleData(data);

            }
            catch(SocketException)
            {
                //okay
            }
            catch (Exception e){
                Debug.LogError($"UDP Recieve error: {e}");
            }
        }
    }
    

    public void Send()
    {

    }
    public void Send(string message)
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

    public void Send(byte[] message)
    {
        try
        {
            client.Send(message, message.Length, remoteEndPoint);
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