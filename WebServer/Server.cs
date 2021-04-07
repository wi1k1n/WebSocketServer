using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace WebSocketServer
{
    class Server
    {
        private int port;
        private TcpListener listener;
        private bool isRunning;
        private List<Client> clientList;

        public int Port { get { return port; } private set { this.port = value; } }
        public bool IsRunning { get { return isRunning; } }
        public List<Client> ClientList { get { return clientList; } }
        public int ClientCount { get { return clientList.Count; } }

        public Server (int port = 80)
        {
            // Записываем порт и создаем слушателя порта
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
            isRunning = false;
            clientList = new List<Client>();
        }

        public void StartServer ()
        {
            // Запускаем слушателя и в бесконечном цикле принимаем клиентов
            listener.Start();
            isRunning = true;
            while (true)
            {
                // Для каждого клиента запускаем отдельный поток, где анонимной функцией создаем новый экземпляр клиента
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(delegate(object obj) {
                    Client newClient = new Client((TcpClient)obj, clientList.Count); 
                    clientList.Add(newClient); 
                    newClient.Start();
                });
                thread.Start(client);
            }
        }

        public Client getClientByUID (int uid)
        {
            foreach (Client client in clientList)
                if (client.UID == uid)
                    return client;
            return null;
        }

        public override string ToString ()
        {
            string res = @"WebSocketServer.Server:
Port\t\t\t" + port + @"
IsRunning\t\t" + isRunning + @"
ClientCount\t\t" + ClientCount;
            return res.Replace(@"\t", "\t");
        }
    }
}
