using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;




public class Fragmentator : MonoBehaviour
{
    public static byte[] FInitHeader = new byte[] { 0x12, 0xFF, 0x34, 0xFF };
    public static byte[] FCHHeader = new byte[] { 0xFF, 0x99, 0xFF, 0x99 };
    public static string sha256_calc(string randomString)
    {
        var crypt = new System.Security.Cryptography.SHA256Managed();
        var hash = new System.Text.StringBuilder();
        byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
        foreach (byte theByte in crypto)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }

    public static FragmentedPacket Fragment(byte[] data)
    {
        if (data.Length < 1536)
            return null;

        int chunkNum = Mathf.CeilToInt(data.Length / 512);
        int chunkLen = Mathf.CeilToInt(data.Length / chunkNum);
        string hash = sha256_calc(data.ToString());

        List<byte[]> chunks = new List<byte[]>();

        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            writer.Write(FInitHeader);
            writer.Write(chunkNum);
            writer.Write(data.Length);
            writer.Write(Encoding.UTF8.GetBytes(hash));
            byte[] result = memoryStream.ToArray();
            Debug.LogWarning($"Result Bytes  Init: {BitConverter.ToString(result)}");
            chunks.Add(result);
            

        }

        for (int i = 0; i < chunkNum; i++)
        {
            byte[] curr_chunk = data.Skip(i * chunkLen).Take(chunkLen).ToArray();
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {

                writer.Write(FCHHeader);
                writer.Write(i);
                writer.Write(Encoding.UTF8.GetBytes(hash));
                writer.Write(curr_chunk);
                byte[] result = memoryStream.ToArray();
                Debug.LogWarning($"Result Bytes: {BitConverter.ToString(result)}");
                Debug.Log(result.Length);
                chunks.Add(result);
            }
        }

        FragmentedPacket res = new FragmentedPacket();
        res.chunks = chunks;
        res.hash = hash;
        res.total_fragments = chunkNum;
        res.total_len = data.Length;
        return res;
        

        
    }
}

public class FragmentedPacket
{
    public bool isDone = false;
    public int total_len;
    public int total_fragments;
    public string hash;
    public byte[] payload;
    public List<byte[]> chunks;
}

public class Defragmentator
{
    

    private Dictionary<string, FragmentedPacket> memory;
    public Defragmentator() { 

    }

    public FragmentedPacket PushData(byte[] data)
    {
        if (data.Take(4).SequenceEqual(Fragmentator.FInitHeader))
        {
            using (MemoryStream _stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(_stream))
            {
                _stream.Seek(4, SeekOrigin.Begin);
                int num_fragments = reader.ReadInt32();
                int total_len = reader.ReadInt32();
                string hash = reader.ReadBytes(64).ToString();
                FragmentedPacket fpacket = new FragmentedPacket();
                fpacket.total_len = total_len;
                fpacket.chunks = new List<byte[]>();
                fpacket.total_fragments = num_fragments;
                memory.Add(hash, fpacket);
                return fpacket;
            }
           
        }
        else if (data.Take(4).SequenceEqual(Fragmentator.FCHHeader))
        {
            using (MemoryStream _stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(_stream))
            {
                int frag_number = reader.ReadInt32();
                string hash = reader.ReadBytes(64).ToString();
                byte[] payload = reader.ReadBytes((int)(_stream.Length - _stream.Position));
                if(memory.ContainsKey(hash))
                {
                    memory[hash].chunks.Add(payload);
                    if (memory[hash].total_fragments-1 == frag_number)
                    {
                        byte[] result_payload = memory[hash].chunks.SelectMany(b => b).ToArray();
                        FragmentedPacket result = memory[hash];
                        result.isDone = true;
                        result.payload = result_payload;
                        memory.Remove(hash);
                        return result;
                    }
                }
            }
            
        }
        return null;
    }
}
