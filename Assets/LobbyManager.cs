#if UNITY_EDITOR
using UnityEditor.VersionControl;
#endif
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public TV_MessageBoxController MessageBoxController;
    public GameObject MonsterSelectorPrefab;
    public Transform MonsterSelectorMenuContent;
    public TVMenuController menuController;
    private MonsterData[] monsters;
    // Start is called before the first frame update
    void Start()
    {
        monsters = Resources.LoadAll<MonsterData>("");
        foreach (MonsterData monster in monsters)
        {
            MonsterSelectoElementrManager m = Instantiate(MonsterSelectorPrefab, MonsterSelectorMenuContent).GetComponent<MonsterSelectoElementrManager>();
            m.SetMonster(monster);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RequestToBeAMonster(MonsterData mData)
    {
        if (mData == null) return;
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.requestMonsterBeeing;
        packet.AddToPayload(mData.CodeName);
        Global.connectionManager.SendPacketAssertResponse(packet, MonsterRequestCallback);
    }

    public void MonsterRequestCallback(Packet packet)
    {
        if (packet == null) return;
        if (packet.header[0] == Headers.ack[0])
        {
            MessageBoxController.ShowMessageBox("Confirmation", "Done! You will now spawn as a Monster.", MessageBoxType.Info);
        }
        else
        {

            //string message = (string)packet.GetFromPayload(new string[] { "string" });
           //MessageBoxController.ShowMessageBox("Error", message, MessageBoxType.Error);
        }
    }

}
