using System;

public class HandlePlayerEvent
{
    public void OnLogin(Player player)
    {
    }

    public void OnLogout(Player player)
    {
        if(player.tempData.status == PlayerTempData.Status.Room)
        {
            Room room = player.tempData.room;
            RoomMgr.instance.LeaveRoom(player);
            if (room != null)
                room.Broadcast(room.GetRoomInfo());
        }
    }
}

public partial class HandleConnMsg
{
    public void MsgHeartBeat(Conn conn, ProtocolBase protoBase)
    {
        conn.lastTickTime = Sys.GetTimeStamp();
        Console.WriteLine("【更新心跳时间】" + conn.GetAdress());
    }

    public void MsgLogin(Conn conn, ProtocolBase protoBase)
    {
        int start = 0;
        ProtocolBytes protocol = (ProtocolBytes)protoBase;
        string protoName = protocol.GetString(start, ref start);
        string id = protocol.GetString(start, ref start);
        string strFormat = "【收到登陆协议】" + conn.GetAdress();
        Console.WriteLine(strFormat + " 用户名：" + id);

        ProtocolBytes protocolRet = new ProtocolBytes();
        protocolRet.AddString("Login");

        ProtocolBytes protocolLogout = new ProtocolBytes();
        protocolLogout.AddString("Logout");
        if(!Player.KickOff(id, protocolLogout))
        {
            protocolRet.AddInt(-1);
            conn.Send(protocolRet);
            return;
        }

        conn.player = new Player(id, conn);

        ServNet.instance.handlePlayerEvent.OnLogin(conn.player);

        protocolRet.AddInt(0);
        conn.Send(protocolRet);
        return;
    }

    public void MsgLogout(Conn conn, ProtocolBase protoBase)
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("Logout");
        protocol.AddInt(0);
        if(conn.player == null)
        {
            conn.Send(protocol);
            conn.Close();
        }
        else
        {
            conn.Send(protocol);
            conn.player.Logout();
        }
    }
}

public partial class HandlePlayerMsg
{
    public void MsgGetList(Player player, ProtocolBase protoBase)
    {
        Scene.instance.SendPlayerList(player);
    }

    public void MsgUpdateInfo(Player player, ProtocolBase protoBase)
    {
        int start = 0;
        ProtocolBytes protocol = (ProtocolBytes)protoBase;
        string protoName = protocol.GetString(start, ref start);
        float x = protocol.GetFloat(start, ref start);
        float y = protocol.GetFloat(start, ref start);
        float z = protocol.GetFloat(start, ref start);
        Scene.instance.UpdateInfo(player.id, x, y, z);

        ProtocolBytes protocolRet = new ProtocolBytes();
        protocolRet.AddString("UpdateInfo");
        protocolRet.AddString("Player.id");
        protocolRet.AddFloat(x);
        protocolRet.AddFloat(y);
        protocolRet.AddFloat(z);
        ServNet.instance.Broadcast(protocolRet);
    }
}
