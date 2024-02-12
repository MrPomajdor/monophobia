using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Net;

public enum Protocol
{
    TCP,
    UDP
}
public static class Headers
{
    public static byte[] ack = new byte[] { 0x00, 0x00 };
    public static byte[] echo = new byte[] { 0x01, 0x00 };
    public static byte[] hello = new byte[] { 0x02, 0x00 };
    public static byte[] data = new byte[] { 0x03, 0x00 };
    public static byte[] disconnecting = new byte[] { 0x04, 0x00 };
    public static byte[] rejected = new byte[] { 0xFF, 0xFF };
    public static byte[] imHere = new byte[] { 0xAA, 0xAA};

}
public static class Flags
{
    public static byte[] none = new byte[] { 0x00 };
    public static class Request
    {
        public static byte[] playerList = new byte[]{ 0x04 };
        public static byte[] lobbyList = new byte[]{ 0x07 };

    }
    public static class Post
    {
        public static byte[] joinLobby = new byte[] { 0x08 };
        public static byte[] createLobby = new byte[] { 0x11 };
        public static byte[] updateLobbyInfo = new byte[] { 0x10 };

        public static byte[] transformData = new byte[] { 0xA0 };
        public static byte[] lobbyInfo = new byte[] { 0xA1 };
        public static byte[] voice = new byte[] { 0xAC };

    }
    public static class Response
    {
        public static byte[] idAssign = new byte[] { 0x03 };
        public static byte[] playerList = new byte[] { 0x05 };
        public static byte[] lobbyList = new byte[] { 0x06 };
        public static byte[] error = new byte[] { 0xFF };
        public static byte[] closing_con = new byte[] { 0xF0 };
        public static byte[] lobbyListChanged = new byte[] { 0x0A };

        public static byte[] transformData = new byte[] { 0xB0 };
        public static byte[] lobbyInfo = new byte[] { 0x09 };
        public static byte[] voice = new byte[] { 0x0C };

    }
}










public class Packet
{
    public byte[] header;
    public byte[] flag;
    public byte[] payload;

    public Packet()
    {
        header = Headers.ack;
        flag = Flags.none;
        payload = new byte[] { };
    }
    public void DigestData(byte[] data)
    {
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                // Read the header (2 bytes)
                header = reader.ReadBytes(2);

                // Read the flag (1 byte)
                flag = reader.ReadBytes(1);

                if (data.Length > 3)
                {
                    // Read the payload (the rest of the bytes)
                    int payloadLength = (int)(memoryStream.Length - memoryStream.Position);
                    payload = reader.ReadBytes(payloadLength);
                }
                else
                {
                    payload = new byte[] { };
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    public void Send(NetworkStream stream)
    {
        try
        {
            
            byte[] buf = PacketParser.AssembleMessage(header, flag, payload);
            if(buf == null)
            {
                Debug.LogError("Packet buffer is null! Cannot send.");
                return;
            }

            stream.BeginWrite(buf, 0, buf.Length, null, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send a packet {e}");
        }
    }
    public void Send(UdpClient socket,IPEndPoint endPoint )
    {
        try
        {

            byte[] buf = PacketParser.AssembleMessage(header, flag, payload);
            if (buf == null)
            {
                Debug.LogError("Packet buffer is null! Cannot send.");
                return;
            }

            socket.Send(buf, buf.Length, endPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send a packet {e}");
        }
    }
    public void AddToPayload(float value)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            writer.Write(value);
            payload = ConcatArrays(payload, memoryStream.ToArray());
        }
    }

    public void AddToPayload(string value)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            writer.Write(valueBytes.Length);
            writer.Write(valueBytes);
            payload = ConcatArrays(payload, memoryStream.ToArray());
        }
    }

    public void AddToPayload(int value)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            writer.Write(value);
            payload = ConcatArrays(payload, memoryStream.ToArray());
        }
    }

    public void AddToPayload(bool value)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            writer.Write(value);
            payload = ConcatArrays(payload, memoryStream.ToArray());
        }
    }

    public void AddToPayload(byte[] value)
    {
        payload = ConcatArrays(payload, value);
    }

    private byte[] ConcatArrays(byte[] array1, byte[] array2)
    {
        if (array1 == null)
            return array2;

        if (array2 == null)
            return array1;

        byte[] result = new byte[array1.Length + array2.Length];
        Array.Copy(array1, result, array1.Length);
        Array.Copy(array2, 0, result, array1.Length, array2.Length);
        return result;
    }

    public T GetJson<T>()
    {
        using (MemoryStream memoryStream = new MemoryStream(payload))
        using (BinaryReader reader = new BinaryReader(memoryStream))
        {
            try
            {
                int stringLength = reader.ReadInt32();
                byte[] stringData = reader.ReadBytes(stringLength);
                string ms = Encoding.UTF8.GetString(stringData);

                return JsonUtility.FromJson<T>(ms);
            }
            catch
            {
                Debug.LogError("Bad json packet");
                return default(T);
            }
        }
    }

}
public class PacketParser
{
    private Dictionary<byte[], Action<Packet>> headerProcessors = new Dictionary<byte[], Action<Packet>>(new ByteArrayComparer());
    private Dictionary<byte[], Action<Packet>> UDPheaderProcessors = new Dictionary<byte[], Action<Packet>>(new ByteArrayComparer());

    public static byte[] AssembleMessage(byte[] header, byte[] flag, byte[] payload)
    {
        MemoryStream memoryStream = new MemoryStream();
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            writer.Write(header);
            writer.Write(flag);
            if(payload.Length > 0)
                writer.Write(payload);
        }
        return memoryStream.ToArray();
    }

    public void DigestMessage(byte[] receivedMessage)
    {
        if (receivedMessage.Length < 2)
        {
            Debug.LogError("Received message is too short");
            return;
        }
        byte[] headerBytes = new byte[] { receivedMessage[0], receivedMessage[1] };
        if (headerProcessors.TryGetValue(headerBytes, out var processor))
        {
            Packet packet = new Packet();

            packet.DigestData(receivedMessage);
            try
            {
                processor.Invoke(packet);
            }catch(Exception e)
            {
                Debug.LogError($"Failed to run function for header {headerBytes[0]} {headerBytes[1]}. {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Received message with unknown header ({headerBytes[0]} {headerBytes[1]})");
        }
    }

    public void RegisterHeaderProcessor(byte[] header, Action<Packet> processor,Protocol protocol=Protocol.TCP)
    {
        switch (protocol)
        {
            case Protocol.TCP:
                headerProcessors[header] = processor;
                break;
            case Protocol.UDP:
                break;
        }
    }



}

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] x, byte[] y)
    {
        if (x == null || y == null)
        {
            return x == y;
        }

        if (x.Length != y.Length)
        {
            return false;
        }

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(byte[] obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        int hash = 17;

        foreach (byte b in obj)
        {
            hash = hash * 31 + b.GetHashCode();
        }

        return hash;
    }
}
