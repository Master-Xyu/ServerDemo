using System;
using System.Net;
using System.Net.Sockets;

public class Main 
{ 
    public static void Main(string[] args)
    {
        ServNet servNet = new ServNet();
        servNet.proto = new ProtocolBytes();
        servNet.Start("127.0.0.1", 1234);
        Console.ReadLine();
    }
}
