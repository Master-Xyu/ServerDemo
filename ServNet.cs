using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Data;

public class ServNet
{
    public Socket listenfd;
    public Conn[] conns;
    public int maxConn = 50;
    public static ServNet instance;

    System.Timers.Timer timer = new System.Timers.Timer(1000);
    public long heartBeatTime = 180;

    public ProtocolBase proto;

    public HandleConnMsg handleConnMsg = new HandleConnMsg();
    public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
    public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();
    public ServNet()
    {
        instance = this;
    }

    public int NewIndex()
    {
        if (conns == null)
            return -1;
        for(int i = 0; i < conns.Length; i++)
        {
            if(conns[i] == null)
            {
                conns[i] = new Conn();
                return i;
            }
            else if(conns[i].isUse == false)
            {
                return i;
            }
        }
        return -1;
    }

    public void Start(string host, int port)
    {
        timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
        timer.AutoReset = false;
        timer.Enabled = true;

        conns = new Conn[maxConn];
        for (int i = 0; i < maxConn; i++)
        {
            conns[i] = new Conn();
        }

        listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress ipAdr = IPAddress.Parse(host);
        IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
        listenfd.Bind(ipEp);

        listenfd.Listen(maxConn);

        listenfd.BeginAccept(AcceptCb, null);
        Console.WriteLine("【服务器】启动成功");
    }

    private void AcceptCb(IAsyncResult ar)
    {
        try
        {
            Socket socket = listenfd.EndAccept(ar);
            int index = NewIndex();

            if (index < 0)
            {
                socket.Close();
                Console.Write("【警告】连接已满");
            }
            else
            {
                Conn conn = conns[index];
                conn.init(socket);
                string adr = conn.GetAdress();
                Console.WriteLine("客户端连接【" + adr + "】 conn 池ID:" + index);
                conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
            }
            listenfd.BeginAccept(AcceptCb, null);
        }catch(Exception e){
            Console.WriteLine("AcceptCb失败：" + e.Message);
        }
    }

    public void Close()
    {
        for(int i = 0; i < conns.Length; i++)
        {
            Conn conn = conns[i];
            if (conn == null) continue;
            if (!conn.isUse) continue;
            lock (conn)
            {
                conn.Close();
            }
        }
    }

    private void ReceiveCb(IAsyncResult ar)
    {
        Conn conn = (Conn)ar.AsyncState;
        lock (conn) ;
        {
            try
            {
                int count = conn.socket.EndReceive(ar);

                if(count <= 0)
                {

                }
                conn.buffCount += count;
                ProcessData(conn);

                conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
            }catch(Exception e)
            {
                Console.WriteLine("收到【" + conn.GetAdress() + "】断开连接");
                conn.Close();
            }
        }
    }

    private void ProcessData(Conn conn)
    {
        if(conn.buffCount < sizeof(Int32))
        {
            return;
        }
        Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
        conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);
        if(conn.buffCount < conn.msgLength + sizeof(Int32))
        {
            return;
        }

        ProtocolBase protocol = proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
        HandleMsg(conn, protocol);

        int count = conn.buffCount - conn.msgLength - sizeof(Int32);
        Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
        conn.buffCount = count;
        if(conn.buffCount > 0)
        {
            ProcessData(conn);
        }
    }

    public void Send(Conn conn, ProtocolBase protocol)
    {
        byte[] bytes = protocol.Encode();
        byte[] length = BitConverter.GetBytes(bytes.Length);
        byte[] sendbuff = length.Concat(bytes).ToArray();
        try
        {
            conn.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
        }catch(Exception e)
        {
            Console.WriteLine("【发送消息】" + conn.GetAdress() + " : " + e.Message);
        }
    }

    public void HandleMainTimer(object sender, System.Timers.ElapsedEventArgs e)
    {
        HeartBeat();
        timer.Start();
    }

    public void HeartBeat()
    {
        //Console.WriteLine("【主定时器执行】");
        long timeNow = Sys.GetTimeStamp();

        for(int i = 0; i < conns.Length; i++)
        {
            Conn conn = conns[i];
            if (conn == null) continue;
            if (!conn.isUse) continue;

            if(conn.lastTickTime < timeNow - heartBeatTime)
            {
                Console.WriteLine("【心跳引起断开连接】" + conn.GetAdress());
                lock (conn)
                    conn.Close();
            }
        }
    }

    private void HandleMsg(Conn conn, ProtocolBase protoBase)
    {
        string name = protoBase.GetName();
        Console.WriteLine("【收到协议】" + name);

        string methodName = "Msg" + name;

        if(conn.player == null || name == "HeartBeat" || name == "Logout")
        {
            MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
            if(mm == null)
            {
                string str = "【警告】HandleMsg没有处理连接方法";
                Console.WriteLine(str + methodName);
                return;
            }
            Object[] obj = new object[] { conn, protoBase };
            Console.WriteLine("【处理连接消息】" + conn.GetAdress() + " :" + name);
            mm.Invoke(handleConnMsg, obj);
        }
        else
        {
            MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
            if (mm == null)
            {
                string str = "【警告】HandleMsg没有处理玩家方法";
                Console.WriteLine(str + methodName);
                return;
            }
            Object[] obj = new object[] { conn.player, protoBase };
            Console.WriteLine("【处理玩家消息】" + conn.GetAdress() + " :" + name);
            mm.Invoke(handlePlayerMsg, obj);
        }
    }

    public void Broadcast(ProtocolBase protocol)
    {
        for(int i = 0; i < conns.Length; i++)
        {
            if (!conns[i].isUse)
                continue;
            if (conns[i].player == null)
                continue;
            Send(conns[i], protocol);
        }
    }

    public void Print()
    {
        Console.WriteLine("===服务器登录信息===");
        for(int i = 0; i < conns.Length; i++)
        {
            if (conns[i] == null)
                continue;
            if (!conns[i].isUse)
                continue;
            string str = "连接【" + conns[i].GetAdress() + "]";
            if (conns[i].player != null)
                str += "玩家id" + conns[i].player.id;
            Console.WriteLine(str);
        }
    }
}
