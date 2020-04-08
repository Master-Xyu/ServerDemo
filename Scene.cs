using System;
using System.Collections.Generic;
using System.Text;

public class Scene
{
    public static Scene instance;
    public Scene()
    {
        instance = this;
    }

    List<ScenePlayer> list = new List<ScenePlayer>();

    private ScenePlayer GetScenePlayer(string id)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (list[i].id == id)
                return list[i];
        }
        return null;
    }

    public void AddPlayer(string id)
    {
        lock (list)
        {
            ScenePlayer p = new ScenePlayer();
            p.id = id;
            list.Add(p);
        }
    }

    public void DelPlayer(string id)
    {
        lock (list)
        {
            ScenePlayer p = GetScenePlayer(id);
            if (p != null)
                list.Remove(p);
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("PlayerLeave");
            protocol.AddString(id);
            ServNet.instance.Broadcast(protocol);
        }
    }

    public void SendPlayerList(Player player)
    {
        int count = list.Count;
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("GetList");
        protocol.AddInt(count);
        for(int i = 0; i < count; i++)
        {
            ScenePlayer p = list[i];
            protocol.AddString(p.id);
            protocol.AddFloat(p.x);
            protocol.AddFloat(p.y);
            protocol.AddFloat(p.z);
        }
        player.Send(protocol);
    }

    public void UpdateInfo(string id, float x, float y, float z)
    {
        int count = list.Count;
        ProtocolBytes protocol = new ProtocolBytes();
        ScenePlayer p = GetScenePlayer(id);
        if (p == null)
            return;
        p.x = x;
        p.y = y;
        p.z = z;
    }
}