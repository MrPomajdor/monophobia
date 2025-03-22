using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Content;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;

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
    public static byte[] imHere = new byte[] { 0xAA, 0xAA };

}
public static class Flags
{
    public static byte[] none = new byte[] { 0x00 };
    public static class Request
    {
        public static byte[] playerList = new byte[] { 0x04 };
        public static byte[] lobbyList = new byte[] { 0x07 };
        public static byte[] worldState = new byte[] { 0xD0 };
        public static byte[] itemList = new byte[] { 0x1A };
        public static byte[] NetworkVars = new byte[] { 0xEB };
    }
    public static class Post
    {
        public static byte[] joinLobby = new byte[] { 0x08 };
        public static byte[] createLobby = new byte[] { 0x11 };
        public static byte[] updateLobbyInfo = new byte[] { 0x10 };

        public static byte[] playerTransformData = new byte[] { 0xA0 };
        public static byte[] lobbyInfo = new byte[] { 0xA1 };
        public static byte[] worldState = new byte[] { 0xA2 };

        public static byte[] itemPickup = new byte[] { 0xA5 };
        public static byte[] itemDrop = new byte[] { 0xA6 };
        public static byte[] inventorySwitch = new byte[] { 0xA7 };
        public static byte[] startMap = new byte[] { 0xA8 };
        public static byte[] itemIntInf = new byte[] { 0xA4 };
        public static byte[] voice = new byte[] { 0xAC };

        public static byte[] interactableMessage = new byte[] { 0xAD };
        public static byte[] codeInteractionMessage = new byte[] { 0xAE };

        public static byte[] NetworkVarSync = new byte[] { 0xBE };


        public static byte[] transform = new byte[] { 0xAF };

        public static byte[] chatMessage = new byte[] { 0xE1 };

        public static byte[] requestMonsterBeeing = new byte[] { 0xE2 };




    }
    public static class Response
    {
        public static byte[] idAssign = new byte[] { 0x03 };
        public static byte[] playerList = new byte[] { 0x05 };
        public static byte[] lobbyList = new byte[] { 0x06 };
        public static byte[] error = new byte[] { 0xFF };
        public static byte[] closing_con = new byte[] { 0xF0 };
        public static byte[] lobbyListChanged = new byte[] { 0x0A };
        public static byte[] lobbyClosing = new byte[] { 0xF1 };

        public static byte[] playerTransforms = new byte[] { 0xB0 };
        public static byte[] lobbyInfo = new byte[] { 0x09 };
        public static byte[] worldState = new byte[] { 0xC2 };

        public static byte[] itemIntInf = new byte[] { 0xC5 };
        public static byte[] itemList = new byte[] { 0xA1 };
        public static byte[] itemPickup = new byte[] { 0xC6 };
        public static byte[] itemDrop = new byte[] { 0xC7 };
        public static byte[] inventorySwitch = new byte[] { 0xC8 };
        public static byte[] startMap = new byte[] { 0xC9 };

        public static byte[] playerData = new byte[] { 0xC4 }; //warning: contents explosive
        public static byte[] voice = new byte[] { 0x0C };

        public static byte[] interactableMessage = new byte[] { 0x0D };
        public static byte[] codeInteractionMessage = new byte[] { 0x0E };

        public static byte[] transform = new byte[] { 0x0F };

        public static byte[] frag_received = new byte[] { 0xDF };

        public static byte[] NetworkVarSync = new byte[] { 0xEE };

        public static byte[] chatMessage = new byte[] { 0xE0 };





    }
}









public static class DataFormats
{
    public static readonly Dictionary<string, int> FormatsLengthDict = new Dictionary<string, int>
    {
        { "float", 4 },
        { "double", 8 },
        { "char", 1 },
        { "byte", 1 },
        { "short", 2 },
        { "ushort", 2 },
        { "int", 4 },
        { "uint", 4 },
        { "long", 8 },
        { "ulong", 8 },
        { "bool", 1 },
        { "string", -1 } // Variable length
    };
}
public class Packet
{
    public byte[] header;
    public byte[] flag;
    public byte[] payload;
    public byte[] callbackGUID = null;
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


                int lenn = reader.ReadInt32();

                if (data.Skip(3).Take(14) == Encoding.UTF8.GetBytes("AssertResponse"))
                {
                    memoryStream.Seek(14, SeekOrigin.Current);
                    callbackGUID = reader.ReadBytes(16);
                }

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
    private void WriteFragments(NetworkStream stream, List<byte[]> fragments)
    {
        foreach (byte[] fragment in fragments)
        {
            //WriteFragment(stream, fragment);
            stream.Write(fragment, 0, fragment.Length);
            stream.Flush();
            Thread.Sleep(20);

        }
    }
    public void Send(NetworkStream stream)
    {
        try
        {

            byte[] buf = PacketParser.AssembleMessage(header, flag, payload, callbackGUID);
            if (buf == null)
            {
                Debug.LogError("Packet buffer is null! Cannot send.");
                return;
            }
            /*
            if (false)//(buf.Length > 512 * 3)
            {
                Debug.LogWarning("Sending fragmented packet");
                FragmentedPacket fragmentedPacket = Fragmentator.Fragment(buf);
                Global.connectionManager.SendFragmented(fragmentedPacket);
                
            }
            else*/
            stream.BeginWrite(buf, 0, buf.Length, null, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send a packet {e}");
        }
    }
    public void Send(UdpClient socket, IPEndPoint endPoint)
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

    public void AddToPayload(int value) //int32
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
    public void AddSerializable<T>(T value) where T : class
    {
        using (MemoryStream memoryStream = new())
        using (BinaryWriter writer = new(memoryStream))
        {
           SerializeObject(value, writer);
            Debug.Log(BitConverter.ToString(memoryStream.ToArray()));
            payload = ConcatArrays(payload, memoryStream.ToArray());
        }
        
    }

    private void SerializeObject(object obj, BinaryWriter writer)
    {
        FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
        foreach (FieldInfo field in fields)
        {
            //Debug.Log($"Serializing {field.Name} type {field.FieldType} value {field.GetValue(obj)}");
            Type type = field.FieldType;
            if (type == typeof(string))
            {
                writer.Write(((string)field.GetValue(obj)).Length);

                writer.Write((string)field.GetValue(obj));
            }
            else if (type == typeof(int))
            {
                writer.Write((int)field.GetValue(obj));
                
            }
            else if (type == typeof(float))
            {
                writer.Write((float)field.GetValue(obj));

            }
            else if (type == typeof(bool))
            {
                writer.Write((bool)field.GetValue(obj));

            }
            else if (type == typeof(Vector3))
            {
                writer.Write(((Vector3)field.GetValue(obj)).x);
                writer.Write(((Vector3)field.GetValue(obj)).y);
                writer.Write(((Vector3)field.GetValue(obj)).z);

            }
            else if (type.IsClass && type.GetCustomAttribute<SerializableAttribute>() != null)
            {
                
                
                SerializeObject(field.GetValue(obj), writer);
               
            }
            else
            {
                throw new NotSupportedException($"Unsupported field type: {type}");
            }

        }
    }
    public bool GetFromPayload<T>(T output) where T : class
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output), "Output cannot be null");
        }
        using (MemoryStream memoryStream = new MemoryStream(payload))
        using (BinaryReader reader = new BinaryReader(memoryStream))
        {
            try
            {
                DeserializeObject(output, reader);
            }
            catch (Exception ex) 
            {
                Debug.LogError($"Invalid payload for deserialization {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private void DeserializeObject(object obj, BinaryReader reader)
    {
        FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (FieldInfo field in fields)
        {
            //Debug.Log($"Deserializing {field.Name}");
            Type type = field.FieldType;
            if (type == typeof(string))
            {
                int strlen = reader.ReadInt32();
                byte[] strbytes = reader.ReadBytes(strlen);
                field.SetValue(obj, Encoding.UTF8.GetString(strbytes));
            }
            else if (type == typeof(int))
            {
                field.SetValue(obj, reader.ReadInt32());
            }
            else if (type == typeof(float))
            {
                field.SetValue(obj, reader.ReadSingle());

            }
            else if (type == typeof(bool))
            {
                field.SetValue(obj, reader.ReadBoolean());

            }
            else if (type == typeof(Vector3))
            {
                Vector3 nvec = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                field.SetValue(obj, nvec);

            }
            else if (!type.IsArray && type.IsClass && type.GetCustomAttribute<SerializableAttribute>() != null)
            {
                // Handle nested class (recursively deserialize)
                object nestedInstance = Activator.CreateInstance(type);
                DeserializeObject(nestedInstance, reader);
                field.SetValue(obj, nestedInstance);
            }
            else if (type.IsArray)
            {
                Type ArrayElemType = type.GetElementType();
                int ArraySize = reader.ReadInt32();

                Array arrayInstance = Array.CreateInstance(ArrayElemType, ArraySize);
                
                for ( int i = 0; i<ArraySize; i++)
                {
                    object arrayElementInstance = Activator.CreateInstance(ArrayElemType);
                    DeserializeObject(arrayElementInstance, reader);
                    arrayInstance.SetValue(arrayElementInstance, i);

                }
                field.SetValue(obj, arrayInstance);
            }
            else
            {
                throw new NotSupportedException($"Unsupported field type: {type}");
            }

        }
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
    /*
    public object GetFromPayload(string[] formats)
    {
        byte[] _pay = payload;
        List<object> result = new List<object>();
        foreach (var format in formats)
        {
            if (format == "string")
            {
                int length = BitConverter.ToInt32(_pay, 0);
                string data = Encoding.UTF8.GetString(_pay, 4, length);
                result.Add(data);
                _pay = _pay.Skip(4 + length).ToArray();
            }
            else if (DataFormats.FormatsLengthDict.ContainsKey(format))
            {
                int formatLength = DataFormats.FormatsLengthDict[format];
                object data = format switch
                {
                    "float" => BitConverter.ToSingle(_pay, 0),
                    "double" => BitConverter.ToDouble(_pay, 0),
                    "char" => (char)_pay[0],
                    "byte" => _pay[0],
                    "short" => BitConverter.ToInt16(_pay, 0),
                    "ushort" => BitConverter.ToUInt16(_pay, 0),
                    "int" => BitConverter.ToInt32(_pay, 0),
                    "uint" => BitConverter.ToUInt32(_pay, 0),
                    "long" => BitConverter.ToInt64(_pay, 0),
                    "ulong" => BitConverter.ToUInt64(_pay, 0),
                    "bool" => BitConverter.ToBoolean(_pay, 0),
                    _ => throw new Exception("Unsupported format")
                };
                _pay = _pay.Skip(formatLength).ToArray();
                result.Add(data);
            }
        }
        return formats.Length > 1 ? result.ToArray() : result[0];
    }
    public T GetJson<T>()
    {
        using (MemoryStream memoryStream = new MemoryStream(payload))
        using (BinaryReader reader = new BinaryReader(memoryStream))
        {
            string ms = "";
            try
            {
                int stringLength = reader.ReadInt32();
                byte[] stringData = reader.ReadBytes(stringLength);
                ms = Encoding.UTF8.GetString(stringData);

                return JsonUtility.FromJson<T>(ms);
            }
            catch (Exception e)
            {
                Debug.LogError($"Bad json packet:\n {e.Message}\n {ms}");
                return default(T);
            }
        }
    }*/



}
public class PacketParser
{
    private Dictionary<byte[], Action<Packet>> headerProcessors = new Dictionary<byte[], Action<Packet>>(new ByteArrayComparer());

    //private Dictionary<byte[], Action<Packet>> UDPheaderProcessors = new Dictionary<byte[], Action<Packet>>(new ByteArrayComparer());

    public static byte[] AssembleMessage(byte[] header, byte[] flag, byte[] payload, byte[] guid = null)
    {
        MemoryStream memoryStream = new MemoryStream();
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {

            writer.Write(header);
            writer.Write(flag);

            writer.Write(guid == null ? payload.Length + 7 : payload.Length + 7 + 30); // header 2 bytes + flag 1 byte + 4 bytes msg len, if guid present add the necesary bytes
            if (guid != null)
            {
                writer.Write(Encoding.UTF8.GetBytes("AssertResponse"));
                writer.Write(guid);
            }
            if (payload.Length > 0)
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
            if (packet.callbackGUID != null)
                Global.connectionManager.PacketCallback(packet.callbackGUID, packet);
            try
            {
                processor.Invoke(packet);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to run function for header {headerBytes[0]} {headerBytes[1]} - flag {receivedMessage[2]}. {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Received message with unknown header ({headerBytes[0].ToString("X")} {headerBytes[1].ToString("X")})");

        }
    }

    public void RegisterHeaderProcessor(byte[] header, Action<Packet> processor, Protocol protocol = Protocol.TCP)
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
