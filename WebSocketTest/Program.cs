using System;
using WebSocketServer;
using System.Threading;
using System.IO;

namespace WebSocketTest
{
    class Program
    {
        static void Main (string[] args)
        {
            float a = float.Parse("123.53 256.1".Split(' ')[1]);
            WSController.Start();
            Console.ReadKey();
            WSController.Start();
        }
    }

    public static class WSController
    {
        static WSServer server = new WSServer(8181);
        static Thread srvThread = new Thread(delegate () { server.Start(); });

        private const string MSGPATH = "messages.txt";

        static WSController()
        {
            server.OnClientConnected += server_OnClientConnected;
            server.OnAvailableToTransmit += server_OnAvailableToTransmit;
            server.OnClientQuit += server_OnClientQuit;
            server.OnMessageReceived += server_OnMessageReceived;
            server.OnBinaryReceived += server_OnBinaryReceived;
        }

        public static void Start()
        {
            //if (srvThread.ThreadState != ThreadState.Running)
            if (!srvThread.IsAlive)
                srvThread.Start();
            if (!File.Exists(MSGPATH))
                File.WriteAllText(MSGPATH, "");
            else
                File.AppendAllText(MSGPATH, "============= New entry =============" + Environment.NewLine);
        }

        static void server_OnClientQuit(object sender, WSClientQuitEventArgs e)
        {
            Console.WriteLine("Client #{0} quit! Reason Code: {1}", e.UID, e.Code);
        }

        static void server_OnClientConnected(object sender, WSClientConnectedEventArgs e)
        {
            Console.WriteLine("Client #{0} connected!", e.UID);
        }

        static void server_OnAvailableToTransmit(object sender, WSClientConnectedEventArgs e)
        {
            server.Send("UID:" + e.UID.ToString(), e.UID);
        }

        static void server_OnMessageReceived(object sender, WSMsgReceivedEventArgs e)
        {
            Console.WriteLine("[{0}]: {1}", e.UID, Convert.ToDouble(e.Msg));
        }

        static void server_OnBinaryReceived(object sender, WSBinReceivedEventArgs e)
        {
            Console.Write("[{0}]: ", e.UID);
            foreach (byte b in e.Data)
                Console.Write("{0} ", b);
            Console.WriteLine();
        }
    }
}
