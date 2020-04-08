using System;
using System.Collections.Generic;
using System.Linq;

public class Room
{
    public enum Status
    {
        Prepare = 1,
        Fight = 2,
    }

    public Status status = Status.Prepare;

    public int maxPlayers = 6;
    public Dictionary<string, Player> list = new Dictionary<string, Player>();
    public System.Timers.Timer timer;

    public Room()
    {
        timer = new System.Timers.Timer(300000);
        timer.Elapsed += new System.Timers.ElapsedEventHandler(timeout);
        timer.AutoReset = false;
        timer.Enabled = true;
    }

    public bool AddPlayer(Player player)
    {
        lock (list)
        {
            if (list.Count >= maxPlayers)
                return false;
            PlayerTempData tempData = player.tempData;
            tempData.room = this;
            tempData.team = SwitchTeam();
            tempData.status = PlayerTempData.Status.Room;

            if (list.Count == 0)
                tempData.isOwner = true;
            string id = player.id;
            list.Add(id, player);
        }
        return true;
    }

    public int SwitchTeam()
    {
        int count1 = 0;
        int count2 = 0;
        foreach(Player player in list.Values)
        {
            if (player.tempData.team == 1) count1++;
            if (player.tempData.team == 2) count2++;
        }
        if (count1 <= count2)
            return 1;
        else
            return 2;
    }

    public void DelPlayer(string id, int status)
    {
        lock (list)
        {
            if (!list.ContainsKey(id))
                return;
            bool isOwner = list[id].tempData.isOwner;
            list[id].tempData.status = PlayerTempData.Status.None;
            list[id].tempData.isOwner = false;
            if (status == 1)
            {
                ProtocolBytes protocol = new ProtocolBytes();
                protocol.AddString("BeKicked");
                protocol.AddInt(0);
                list[id].Send(protocol);
            }else if(status == 2){
                ProtocolBytes protocol = new ProtocolBytes();
                protocol.AddString("BeKicked");
                protocol.AddInt(1);
                list[id].Send(protocol);
            }
            list.Remove(id);
            if (isOwner)
                UpdateOwner();
            
        }
    }

    public void UpdateOwner()
    {
        lock (list)
        {
            if (list.Count <= 0)
                return;
            foreach(Player player in list.Values)
            {
                player.tempData.isOwner = false;
            }
            Player p = list.Values.First();
            p.tempData.isOwner = true;
        }
    }

    public void Broadcast(ProtocolBase protocol)
    {
        foreach(Player player in list.Values)
        {
            player.Send(protocol);
        }
    }

    public ProtocolBytes GetRoomInfo()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("GetRoomInfo");

        protocol.AddInt(list.Count);

        foreach(Player p in list.Values)
        {
            protocol.AddString(p.id);
            protocol.AddInt(p.tempData.team);
            int isOwner = p.tempData.isOwner ? 1 : 0;
            protocol.AddInt(isOwner);
        }
        return protocol;
    }

    public void timeout(object source, System.Timers.ElapsedEventArgs e)
    {
        while(list.Count() != 0)
        {
            DelPlayer(list.Keys.First(), 2);
        }
        RoomMgr.instance.list.Remove(this);
    }
}
