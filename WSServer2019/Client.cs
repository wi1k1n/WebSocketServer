using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace WebSocketServer
{
    public class Client
    {
        public delegate void WSPacketReceivedEventHandler (object sender, WSPacketReceivedEventArgs e);
        public delegate void WSAvailableToTransmitEventHandler (object sender, WSClientConnectedEventArgs e);

        public event WSPacketReceivedEventHandler OnPacketReceived;
        public event WSAvailableToTransmitEventHandler OnAvailableToTransmit;

        private int uid;
        private const int bufSize = 1024;
        private TcpClient client;
        private WSListener wsl;
        private HandShaker handShaker;
        private UInt64 traffic = 0;
        private UInt64 dataTotalLength = 0;

        public int UID { get { return uid; } private set { uid = value; } }
        public bool Connected { get { return client.Connected; } }
        public bool AvailableToTransmit { get { return wsl != null ? wsl.IsListening : false; } }

        public Client (TcpClient client, int uid)
        {
            this.client = client;
            this.uid = uid;
            handShaker = new HandShaker(client.GetStream(), bufSize);
        }

        public void Start ()
        {
            Consoler.WriteLine("Client #{0} connected!", ConsoleColor.White, uid);

            if (handShaker.HandShake() <= 0)
                Stop();

            wsl = new WSListener(client.GetStream());
            wsl.OnStarted += wsl_OnStarted;
            wsl.OnSinglePacketReceived += wsl_SinglePacketReceived;
            wsl.OnFragmentReceived += wsl_FragmentReceived;
            wsl.OnFragmentedPacketReceived += wsl_FragmentedPacketReceived;
            wsl.Start();
        }

        void wsl_OnStarted (object sender, EventArgs e)
        {
            onAvailableToTrnsmit();
        }

        // Остановка соединения
        public void Stop ()
        {
            try
            {
                Close();
            } catch { }
            wsl.Stop();
            client.Close();
        }

        //TODO: Объединить общую часть отправки сообщений, разделив хвосты на отправку строковых и бинарных данных

        // Отправка строкового сообщения
        public bool Send (string msg)
        {
            return sendData(true, false, false, false, 1, false, stringToByteArray(msg));
        }
        // Отправка закрывающего пакета с кодом 1001
        private bool Close ()
        {
            return sendData(true, false, false, false, 8, false, new byte[] { 3, 233 });
        }

        // В данном методе собирается (НЕФРАГМЕНТИРОВАННЫЙ!!!) пакет данных и отправляется
        private bool sendData (bool fin, bool rsv1, bool rsv2, bool rsv3, byte opcode, bool masked, byte[] data)
        {
            //TODO: ловим исключение: opcode >= 0 && opcode < 16

            // Собираем в массив байт длину сообщение в соответствии с документацией по WebSockets
            byte[] dataSendingLength = constructLength((uint)data.Length);

            // Вычисляем общую длину заголовочных данных пакета (2 - константа, содержащая fin, rsvs, opcode, maskFkag и length)
            int headersLen = 2 + (dataSendingLength.Length > 1 ? dataSendingLength.Length : 0) + (masked ? 4 : 0);
            // Конструируем массив заголовков
            byte[] headers = new byte[headersLen];
            headers[0] = formHeader1(fin, rsv1, rsv2, rsv3, opcode);
            headers[1] = formHeader2(masked, (uint)data.Length);
            int cursor = 2;
            if (dataSendingLength.Length > 1)
            {
                dataSendingLength.CopyTo(headers, 2);
                cursor += dataSendingLength.Length;
            }
            //TODO: не работает маскирование данных (браузер закрывает соединение с ошибкой 1002), оно должно работать в напревлении сервер -> браузер?
            byte[] mask = masked ? generateMask(ref headers, cursor) : null;
            byte[] packet = new byte[headers.Length + data.Length];
            headers.CopyTo(packet, 0);
            for (int i = 0; i < data.Length; i++)
                packet[cursor + i] = masked ? (byte)(data[i] ^ mask[i % 4]) : data[i];

            try
            {
                // Выбрасываем в поток сформированный массив данных
                client.GetStream().Write(packet, 0, packet.Length);
            } catch { return false; }
            return true;
        }

        // Зная длину формируем массив байт, в котором и содержится эта длина
        private byte[] constructLength (uint len)
        {
            byte[] res = new byte[1];
            if (len < 126)
                res[0] = (byte)len;
            else if (len < 65536)
                res = ByteManager.bytesFromInt16((ushort)len);
            else
                res = ByteManager.bytesFromInt64((ulong)len);
            return res;
        }
        // Генерирует 4-ех байтный массив маски
        private byte[] generateMask (ref byte[] headers, int cursor)
        {
            Random rnd = new Random();
            byte[] res = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                res[i] = (byte)rnd.Next(0, 256);
                headers[i + cursor] = res[i];
            }
            return res;
        }
        // Формируем первый заголовочный байт
        private byte formHeader1 (bool fin, bool rsv1, bool rsv2, bool rsv3, byte opcode)
        {
            //TODO: ловим исключение: opcode >= 0 && opcode < 16
            return ByteManager.constructByte(fin, rsv1, rsv2, rsv3, ByteManager.getBit(opcode, 4), ByteManager.getBit(opcode, 5), ByteManager.getBit(opcode, 6), ByteManager.getBit(opcode, 7));
        }
        // Формируем второй заголовочный байт. len - длина передаваемого сообщения
        private byte formHeader2 (bool mask, uint len)
        {
            return ByteManager.setBit((byte)(len < 126 ? len : (uint)(len < 65536 ? 127 : 128)), 0, mask);
        }


        #region DataReceived EventHandlers
        void wsl_FragmentedPacketReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(0, e.LastPacket, "Last Frame #" + e.PacketFragments.Count + " received:");
            onPacketReceived(e);
        }
        void wsl_FragmentReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(1, e.LastPacket, "Single Frame #" + e.PacketFragments.Count + " received:");
        }
        void wsl_SinglePacketReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(2, e.LastPacket, "Single Packet received:");
            onPacketReceived(e);
        }
        private void ReceivingHandler (int uid, WSPacket packet, string header)
        {
            dataTotalLength += (uint)packet.Data.Length;
            traffic += (uint)packet.HeadersLength;
            ConsoleColor clr = Console.ForegroundColor;
            switch (uid)
            {
                case 0: clr = ConsoleColor.Cyan;
                    break;
                case 1: clr = ConsoleColor.Yellow;
                    break;
                case 2: clr = ConsoleColor.Blue;
                    break;
            }
            Consoler.WriteLine(header, clr);
            switch (packet.Opcode)
            {
                case 1:
                    Consoler.WriteLine(byteArrayToString(packet.Data));
                    break;
                case 2:
                    Consoler.WriteLine("Получены бинарные данные!", ConsoleColor.Magenta);
                    break;
                case 8:
                    Consoler.WriteLine("Соединение закрыто удаленной стороной! Код причины: {0}", ConsoleColor.Magenta, ByteManager.Int16FromBytes(packet.Data[0], packet.Data[1]));
                    Stop();
                    break;
                case 9:
                    Consoler.WriteLine("Получен пакет 'PING'!", ConsoleColor.Magenta);
                    break;
                case 10:
                    Consoler.WriteLine("Получен пакет 'PONG'!", ConsoleColor.Magenta);
                    break;
            }
            Consoler.WriteLine();
        }

        private void onPacketReceived (WSDataReceivedEventArgs e)
        {
            if (OnPacketReceived != null)
                OnPacketReceived(this, new WSPacketReceivedEventArgs(e, uid));
        }
        private void onAvailableToTrnsmit ()
        {
            if (OnAvailableToTransmit != null)
                OnAvailableToTransmit(this, new WSClientConnectedEventArgs(uid));
        }
        #endregion

        // Конвертация массива байт в строку (byte[] -> string)
        private string byteArrayToString (byte[] data)
        {
            string res = "";
            foreach (byte b in data)
                res += (char)b;
            return res;
        }
        // Конвертация строки в массив байт (string -> byte[])
        private byte[] stringToByteArray (string data)
        {
            byte[] res = new byte[data.Length];
            for (int i = 0; i < res.Length; i++)
                res[i] = (byte)data[i];
            return res;
        }
    }

    // Класс аргументов для делегата WSPacketReceived, передаваемых классу Server
    public class WSPacketReceivedEventArgs
    {
        public int UID { get; private set; }
        public WSDataReceivedEventArgs Packets { get; private set; }

        public WSPacketReceivedEventArgs (WSDataReceivedEventArgs e, int uid)
        {
            Packets = e;
            UID = uid;
        }
    }
}
