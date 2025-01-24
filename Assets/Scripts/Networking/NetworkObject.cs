using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;


[AttributeUsage(AttributeTargets.Field)]
public class SyncVariableAttribute : Attribute
{
}


public class NetworkObject : MonoBehaviour
{
    [SerializeField]
    private string persistentID;

    
    public string ObjectID
    {
        get { return persistentID; }
    }

    private Dictionary<FieldInfo, object> fieldsValues = new Dictionary<FieldInfo, object>();
    public void GenerateID()
    {
        if(string.IsNullOrEmpty(persistentID))
        {
            persistentID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }


    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.NetworkVarSync[0], ReceiveNetworkVarUpdate);
    }

    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.NetworkVarSync[0], ReceiveNetworkVarUpdate);

    }
    private FieldInfo[] GetSyncFields()
    {
        return GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<SyncVariableAttribute>() != null)
            .ToArray();
    }

    public void SyncVariables()
    {
        foreach (var field in GetSyncFields())
        {
            var value = field.GetValue(this);

            if (!fieldsValues.ContainsKey(field))
                fieldsValues.Add(field, null);

            if (fieldsValues[field] == value)
                  return;

            fieldsValues[field] = value;
            Debug.Log($"Syncing {ObjectID}.{field.Name} with value: {value}");
            SendVariableUpdate(field.Name, value); 
        }
    }

    private void ReceiveNetworkVarUpdate(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            
            int st_len = reader.ReadInt32();
            string id = Encoding.UTF8.GetString(reader.ReadBytes(st_len));
            st_len = reader.ReadInt32();
            string value_name = Encoding.UTF8.GetString(reader.ReadBytes(st_len));

            FieldInfo field = GetType().GetField(value_name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Log($"{value_name} is beeing updated!");
            if (field != null)
            {
                // Get the field type
                Type fieldType = field.FieldType;

                // Use a switch to read and set the correct field type
                if (fieldType == typeof(int))
                {
                    int intValue = reader.ReadInt32();
                    field.SetValue(this, intValue);
                }
                else if (fieldType == typeof(float))
                {
                    float floatValue = reader.ReadSingle();
                    field.SetValue(this, floatValue);
                }
                else if (fieldType == typeof(bool))
                {
                    bool boolValue = reader.ReadBoolean();
                    field.SetValue(this, boolValue);
                }
                else if (fieldType == typeof(string))
                {
                    int str_len = reader.ReadInt32();
                    string stringValue = Encoding.UTF8.GetString(reader.ReadBytes(str_len));
                    field.SetValue(this, stringValue);
                }
                else
                {
                    Debug.LogError($"Unsupported field type: {fieldType}");
                }
            }
            else
            {
                Debug.LogError($"Field '{value_name}' not found.");
            }

        }
    }

    private void SendVariableUpdate(string value_name, object value)
    {
        Packet pack = new Packet();
        pack.header = Headers.data;
        pack.flag = Flags.Post.NetworkVarSync;
        pack.AddToPayload(ObjectID);
        pack.AddToPayload(value_name);
        if (value is int intValue)
            pack.AddToPayload(intValue);
        else if (value is float floatValue)
            pack.AddToPayload(floatValue);
        else if (value is bool boolValue)
            pack.AddToPayload(boolValue);
        else if (value is string stringValue)
            pack.AddToPayload(stringValue);
        else
        {
            Debug.LogError($"NetworkObjec with id {ObjectID} tried to sync var {value_name} that was of unsupported type!");
            return;
        }

        pack.Send(Global.connectionManager.stream);
        
    }
}
