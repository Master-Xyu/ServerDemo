using System;
using System.Collections.Generic;
using System.Linq;

public class RoomMgr
{
    public static RoomMgr instance;
    public RoomMgr()
    {
        instance = this;
    }

    public List<Room> list = new List<Room>();

    public void CreateRoom(Player player)
    {
        Room room = new Room();
        lock (list)
        {
            list.Add(room);
            room.AddPlayer(player);
        }
    }

    public void LeaveRoom(Player player)
    {
        PlayerTempData tempData = player.tempData;
        if(tempData.status == PlayerTempData.Status.None)
        {
            return;
        }
        Room room = tempData.room;
        lock (list)
        {
            room.DelPlayer(player.id, 0);
            if (room.list.Count == 0)
                list.Remove(room);
        }
    }

    public ProtocolBytes GetRoomList()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("GetRoomList");
        int count = list.Count;

        protocol.AddInt(count);

        Console.WriteLine(count);

        for (int i = 0; i < count; i++){
            Room room = list[i];
            protocol.AddInt(room.list.Count);
            protocol.AddInt((int)room.status);
        }
        return protocol;
    }
}