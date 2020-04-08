using System;
using System.Collections.Generic;

public partial class HandlePlayerMsg
{
    public void MsgGetRoomList(Player player, ProtocolBase protoBase)
    {
        player.Send(RoomMgr.instance.GetRoomList());
    }

    public void MsgCreateRoom(Player player, ProtocolBase protoBase)
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("CreateRoom");
        if(player.tempData.status != PlayerTempData.Status.None)
        {
            Console.WriteLine("MsgCreateRoom Fail " + player.id);
            protocol.AddInt(-1);
            player.Send(protocol);
            return;
        }
        RoomMgr.instance.CreateRoom(player);
        protocol.AddInt(0);
        player.Send(protocol);
        Console.WriteLine("MsgCreateRoom Ok " + player.id);
    }

    public void MsgEnterRoom(Player player, ProtocolBase protoBase)
    {
        int start = 0;
        ProtocolBytes protocol = (ProtocolBytes)protoBase;
        string protoName = protocol.GetString(start, ref start);
        int index = protocol.GetInt(start, ref start);
        Console.WriteLine("【收到MsgEnterRoom】" + player.id + " " + index);

        protocol = new ProtocolBytes();
        protocol.AddString("EnterRoom");

        if(index < 0|| index >= RoomMgr.instance.list.Count)
        {
            Console.WriteLine("MsgEnterRoom index err " + player.id);
            protocol.AddInt(-1);
            player.Send(protocol);
            return;
        }

        Room room = RoomMgr.instance.list[index];
        if(room.status != Room.Status.Prepare)
        {
            Console.WriteLine("MsgEnterRoom status err " + player.id);
            protocol.AddInt(-1);
            player.Send(protocol);
            return;
        }

        if (room.AddPlayer(player))
        {
            room.Broadcast(room.GetRoomInfo());
            protocol.AddInt(0);
            player.Send(protocol);
        }
        else
        {
            Console.WriteLine("MsgEnterRoom max Player err " + player.id);
            protocol.AddInt(-1);
            player.Send(protocol);
        }
    }

    public void MsgGetRoomInfo(Player player, ProtocolBase protoBase)
    {
        if(player.tempData.status != PlayerTempData.Status.Room)
        {
            Console.WriteLine("MsgGetRoomInfo status err " + player.id);
            return;
        }
        Room room = player.tempData.room;
        player.Send(room.GetRoomInfo());
    }

    public void MsgLeaveRoom(Player player, ProtocolBase protoBase)
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("LeaveRoom");

        if(player.tempData.status != PlayerTempData.Status.Room)
        {
            Console.WriteLine("MsgLeaveRoom status err " + player.id);
            protocol.AddInt(-1);
            player.Send(protocol);
            return;
        }

        protocol.AddInt(0);
        player.Send(protocol);
        Room room = player.tempData.room;
        RoomMgr.instance.LeaveRoom(player);

        if (room != null)
            room.Broadcast(room.GetRoomInfo());
    }

    public void MsgKickPlayer(Player player, ProtocolBase protoBase)
    {
        int start = 0;
        ProtocolBytes protocol = (ProtocolBytes)protoBase;
        string protoName = protocol.GetString(start, ref start);
        string id = protocol.GetString(start, ref start);
        Room room = player.tempData.room;
        room.DelPlayer(id, 1);
        protocol = new ProtocolBytes();
        protocol.AddString("KickPlayer");
        protocol.AddInt(1);
        player.Send(protocol);
        if (room != null)
            room.Broadcast(room.GetRoomInfo());
    }
}