using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Threading;

public class Conn
{
    public const int BUFFER_SIZE = 1024;
    public Socket socket;
    public bool isUse = false;

    public byte[] readBuff = new byte[BUFFER_SIZE];
    public int buffCount = 0;

    public byte[] lenBytes = new byte[sizeof(UInt32)];
    public Int32 msgLength = 0;

    public long lastTickTime = long.MinValue;
    public Player player;

    public Conn()
    {
        readBuff = new byte[BUFFER_SIZE];
    }

    public void init (Socket socket)
    {
        this.socket = socket;
        isUse = true;
        buffCount = 0;

        lastTickTime = Sys.GetTiimeStamp();
    }

    public int BuffRemain()
    {
        return BUFFER_SIZE - buffCount;
    }

    public string GetAdress()
    {
        if (!isUse)
        {
            return "无法获取地址";
        }
        return socket.RemoteEndPoint.ToString();
    }

    public void Close()
    {
        if (!isUse)
            return;
        if (player != null)
        {
            //player.Logout();
            return;
        }
        Console.WriteLine("【断开连接】" + GetAdress());
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
        isUse = false;
    }
    /*
    public void Send(ProtocalBase protocol)
    {
        ServNet.instance.Send(this, protocol);
    }
    */
}
