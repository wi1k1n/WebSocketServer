using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace WebSocketServer
{
    public class WSServer
    {
        public delegate void WSMsgReceivedEventHandler (object sender, WSMsgReceivedEventArgs e);
        public delegate void WSBinReceivedEventHandler (object sender, WSBinReceivedEventArgs e);
        public delegate void WSClientConnectedEventHandler (object sender, WSClientConnectedEventArgs e);
        public delegate void WSAvailableToTransmitEventHandler (object sender, WSClientConnectedEventArgs e);
        public delegate void WSClientQuitEventHandler (object sender, WSClientQuitEventArgs e);

        public event WSMsgReceivedEventHandler OnMessageReceived;
        public event WSBinReceivedEventHandler OnBinaryReceived;
        public event WSClientConnectedEventHandler OnClientConnected;
        public event WSAvailableToTransmitEventHandler OnAvailableToTransmit;
        public event WSClientQuitEventHandler OnClientQuit;

        private int port;
        private TcpListener listener;
        private bool isRunning;
        private List<Client> clientList;
        private int uidCnt = 1;

        public int Port { get { return port; } private set { this.port = value; } }
        public bool IsRunning { get { return isRunning; } }
        public List<Client> ClientList { get { return clientList; } }
        public int ClientCount { get { return clientList.Count; } }

        public WSServer (int port = 80, IPAddress ipAddress = null)
        {
            if (ipAddress == null)
                ipAddress = IPAddress.Any;

            // Записываем порт и создаем слушателя порта
            this.port = port;
            listener = new TcpListener(ipAddress, port);
            isRunning = false;
            clientList = new List<Client>();
        }

        public void Start ()
        {
            // Запускаем слушателя и в бесконечном цикле принимаем клиентов
            listener.Start();
            isRunning = true;
            while (true)
            {
                // Для каждого клиента запускаем отдельный поток, где анонимной функцией создаем новый экземпляр клиента
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(delegate(object obj)
                {
                    Client newClient = new Client((TcpClient)obj, uidCnt++);
                    newClient.OnPacketReceived += newClient_OnPacketReceived;
                    newClient.OnAvailableToTransmit += newClient_OnAvailableToTransmit;
                    clientList.Add(newClient);
                    onClientConnected(newClient.UID);
                    newClient.Start();
                });
                thread.Start(client);
            }
        }

        void newClient_OnAvailableToTransmit (object sender, WSClientConnectedEventArgs e)
        {
            onAvailableToTransmit(e.UID);
        }

        void newClient_OnPacketReceived (object sender, WSPacketReceivedEventArgs e)
        {
            //TODO: Собрать данные из разных пакетов в одно сообщение
            if (e.Packets.LastPacket.Opcode == 1)
                onMessageReceived(byteArrayToString(e.Packets.LastPacket.Data), e.UID);
            else if (e.Packets.LastPacket.Opcode == 2)
                onBinaryReceived(e.Packets.LastPacket.Data, e.UID);
            else if (e.Packets.LastPacket.Opcode == 8)
                onClientQuit(e.UID, ByteManager.Int16FromBytes(e.Packets.LastPacket.Data[0], e.Packets.LastPacket.Data[1]));
        }
        private string byteArrayToString (byte[] data)
        {
            string res = "";
            foreach (byte b in data)
                res += (char)b;
            return res;
        }

        private void onMessageReceived (string msg, int uid)
        {
            if (OnMessageReceived != null)
                OnMessageReceived(this, new WSMsgReceivedEventArgs(msg, uid));
        }
        private void onBinaryReceived (byte[] data, int uid)
        {
            if (OnBinaryReceived != null)
                OnBinaryReceived(this, new WSBinReceivedEventArgs(data, uid));
        }
        private void onClientConnected (int uid)
        {
            if (OnClientConnected != null)
                OnClientConnected(this, new WSClientConnectedEventArgs(uid));
        }
        private void onAvailableToTransmit (int uid)
        {
            if (OnAvailableToTransmit != null)
                OnAvailableToTransmit(this, new WSClientConnectedEventArgs(uid));
        }
        private void onClientQuit (int uid, int code)
        {
            if (OnClientQuit != null)
                OnClientQuit(this, new WSClientQuitEventArgs(uid, code));
            Client client = getClientByUID(uid);
            try
            {
                client.Stop();
            } catch { }
            clientList.Remove(client);
        }

        // Остановка всего сервера
        public void Stop ()
        {
            //TODO: Аккуратный стоп сервера
        }

        #region Методы Взаимодействия Сервера с Клиентами
        public void Broadcast (string msg)
        {
            Broadcast(msg, -1);
        }
        public void Broadcast (string msg, int uid)
        {
            foreach(Client cl in clientList)
                if (cl.UID != uid)
                    cl.Send(msg);
        }
        public bool Send (string msg, int uid)
        {
            Client client = getClientByUID(uid);
            if (client == null) return false;
            if (client.AvailableToTransmit)
                client.Send(msg);
            return true;
        }
        // Закрытие соединения, вызванное пользователем
        public bool Close (int uid)
        {
            Client client = getClientByUID(uid);
            if (client == null) return false;
            client.Stop();
            clientList.Remove(client);
            return true;
        }
        public void CloseAll ()
        {
            int len = clientList.Count;
            for (int i = 0; i < len; i++)
            {
                clientList[0].Stop();
                clientList.Remove(clientList[0]);
            }
        }
        #endregion

        private Client getClientByUID (int uid)
        {
            foreach (Client client in clientList)
                if (client.UID == uid)
                    return client;
            return null;
        }
    }

    public class WSMsgReceivedEventArgs
    {
        public string Msg { get; private set; }
        public int UID { get; private set; }

        public WSMsgReceivedEventArgs (string msg, int uid)
        {
            UID = uid;
            Msg = msg;
        }
    }
    public class WSBinReceivedEventArgs
    {
        public byte[] Data { get; private set; }
        public int UID { get; private set; }

        public WSBinReceivedEventArgs (byte[] data, int uid)
        {
            UID = uid;
            Data = data;
        }
    }
    public class WSClientConnectedEventArgs
    {
        public int UID { get; private set; }

        public WSClientConnectedEventArgs (int uid)
        {
            UID = uid;
        }
    }
    public class WSClientQuitEventArgs
    {
        public int UID { get; private set; }
        public int Code { get; private set; }

        public WSClientQuitEventArgs (int uid, int code)
        {
            UID = uid;
            Code = code;
        }
    }
}
