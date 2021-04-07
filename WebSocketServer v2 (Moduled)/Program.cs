using System;
using System.Text;
using System.Threading;

namespace WebSocketServer
{
    class Program
    {
        static int MaximumThreadCount = 4 * Environment.ProcessorCount;
        static int MinimumThreadCount = 2;
        static WSServer server;

        static void Main (string[] args)
        {
            // Устанавливаем максимальное и минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(MinimumThreadCount, MinimumThreadCount);
            ThreadPool.SetMaxThreads(MaximumThreadCount, MaximumThreadCount);

            // Создаем экзмепляр сервера и запускаем его в отдельном потоке
            server = new WSServer(8181);
            server.OnClientConnected += server_OnClientConnected;
            server.OnAvailableToTransmit += server_OnAvailableToTransmit;
            server.OnClientQuit += server_OnClientQuit;
            server.OnMessageReceived += server_OnMessageReceived;
            server.OnBinaryReceived += server_OnBinaryReceived;
            Thread serverThread = new Thread(delegate() { server.Start(); });
            serverThread.Start();

            Thread testThread = new Thread(delegate () { SendTest.Start(server); });
            testThread.Start();

            while (true)
            {
                string line = Console.ReadLine();
                string cmd = line.Split(' ')[0];
                string uidNum, msg;
                int uid;
                switch (cmd)
                {
                    case "send":
                        if (line.IndexOf(' ') == -1) break;
                        uidNum = line.Split(' ')[1];
                        if (int.TryParse(uidNum, out uid))
                        {
                            msg = line.Substring("send".Length + uidNum.Length + 2);
                            if (!server.Send(msg, uid))
                                Console.WriteLine("Error while sending message to client #{0}. Client #{0} does not exist.", uid);
                        } else
                            Console.WriteLine("Could not parse client uid.");
                        break;
                    case "broadcast":
                        msg = line.Substring("broadcast".Length + 1);
                        server.Broadcast(msg);
                        break;
                    case "close":
                        if (line.IndexOf(' ') == -1) break;
                        uidNum = line.Split(' ')[1];
                        if (int.TryParse(uidNum, out uid))
                        {
                            if (!server.Close(uid))
                                Console.WriteLine("Error while closing client #{0}. Client #{0} does not exist.", uid);
                        } else
                            Console.WriteLine("Could not parse client uid.");
                        break;
                    case "closeall":
                        server.CloseAll();
                        break;
                }
            }
        }

        static void server_OnClientQuit (object sender, WSClientQuitEventArgs e)
        {
            Console.WriteLine("Client #{0} quit! Reason Code: {1}", e.UID, e.Code);
        }

        static void server_OnClientConnected (object sender, WSClientConnectedEventArgs e)
        {
            Console.WriteLine("Client #{0} connected!", e.UID);
        }

        static void server_OnAvailableToTransmit (object sender, WSClientConnectedEventArgs e)
        {
            server.Send("UID:" + e.UID.ToString(), e.UID);
        }

        static void server_OnMessageReceived (object sender, WSMsgReceivedEventArgs e)
        {
            Console.WriteLine("[{0}]: {1}", e.UID, e.Msg);
        }

        static void server_OnBinaryReceived (object sender, WSBinReceivedEventArgs e)
        {
            Console.Write("[{0}]: ", e.UID);
            foreach (byte b in e.Data)
                Console.Write("{0} ", b);
            Console.WriteLine();
        }
    }
}
