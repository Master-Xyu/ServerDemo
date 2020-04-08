using System;
using System.Net;
using System.Net.Sockets;

public class main 
{ 
    public static void Main(string[] args)
    {
        Scene scene = new Scene();
        ServNet servNet = new ServNet();
        servNet.proto = new ProtocolBytes();
        servNet.Start("127.0.0.1", 1234);
        RoomMgr roomMgr = new RoomMgr();
        Console.ReadLine();
        while (true)
        {
            string str = Console.ReadLine();
            switch (str)
            {
                case "quit":
                    servNet.Close();
                    return;
                case "print":
                    servNet.Print();
                    break;
            }
        }
    }
}
