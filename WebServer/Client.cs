using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace WebSocketServer
{
    class Client
    {
        private const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private int uid;
        private const int bufSize = 1024;
        private TcpClient client;
        private WSListener wsl;
        private Dictionary<string, string> requestHeaders;
        private string requestPath = "";
        private string httpVersion = "";
        private UInt64 traffic = 0;
        private UInt64 dataTotalLength = 0;

        public int UID { get { return uid; } private set { uid = value; } }
        public bool Connected { get { return client.Connected; } }

        public Client (TcpClient client, int uid = -1)
        {
            this.client = client;
            this.uid = uid;
        }

        public void Start ()
        {
            Consoler.WriteLine("Client #{0} connected!", ConsoleColor.White, uid);

            HandShake();

            wsl = new WSListener(client.GetStream());
            wsl.SinglePacketReceived += wsl_SinglePacketReceived;
            wsl.FragmentReceived += wsl_FragmentReceived;
            wsl.FragmentedPacketReceived += wsl_FragmentedPacketReceived;
            wsl.Start();
        }

        private void HandShake ()
        {
            string request = readHandshakeRequest();

            // Выводим в консоль полученные заголовки
            Consoler.WriteLine("HandShake Received:", ConsoleColor.Red);
            Consoler.WriteLine(request, ConsoleColor.DarkRed);

            bool requestStatus = validateRequest(request);
            string response = "";
            if (requestStatus)
            {
                // Вычисляем секретный код для WebSocket'a
                string sec_key = computeSecretKey(request);

                // Формируем заголовки для "рукопожатия"
                response += string.Concat(httpVersion != "" ? httpVersion : "HTTP/1.1", " ", 101, " ", "Switching protocols", "\n");
                response += "Connection: Upgrade\n";
                response += "Upgrade: websocket\n";
                response += string.Concat("Sec-Websocket-Accept: ", sec_key, "\n");
                response += "\n";
            } else
            {
                Consoler.WriteLine("Полученный от клиента запрос не валиден! Будет отправлен: '400 Bad request' ответ", ConsoleColor.Magenta);
                response += string.Concat(httpVersion != "" ? httpVersion : "HTTP/1.1", " ", 400, " ", "Bad request", "\n");
                response += "Connection: Upgrade\n";
                response += "Upgrade: websocket\n";
                response += "\n";
            }

            // Переводим строку в массив байт и посылаем его
            byte[] responseBuf = Encoding.ASCII.GetBytes(response);
            client.GetStream().Write(responseBuf, 0, responseBuf.Length);

            // Выводим в консоль отправленные заголовки
            Consoler.WriteLine("HandShake Sent:", ConsoleColor.Green);
            Consoler.WriteLine(response, ConsoleColor.DarkGreen);
        }

        private string readHandshakeRequest ()
        {
            string request = "";
            int cnt;
            byte[] buf = new byte[bufSize];

            // Пока в потоке есть даныне для чтения, считываем их в строку
            // Остановкой цикла служит последовательность 2-ух CRLF подряд
            try
            {
                while ((cnt = client.GetStream().Read(buf, 0, bufSize)) > 0)
                {
                    request += Encoding.ASCII.GetString(buf, 0, cnt);
                    if (request.IndexOf("\r\n\r\n") > 0)
                        break;
                }
            } catch { Stop(); }
            traffic += (uint)request.Length;
            return request;
        }
        private bool validateRequest (string request)
        {
            bool ret = true;

            string[] lines = exceptEmpty(splitString(request));
            if (lines.Length < 6) ret = false;

            string[] topLineElements = lines[0].Split(' ');
            if (topLineElements.Length != 3 || topLineElements[0] != "GET") ret = false;
            httpVersion = topLineElements[2];
            requestPath = topLineElements[1];

            requestHeaders = new Dictionary<string, string>(lines.Length - 1);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] headerElements = lines[i].Split(':');
                requestHeaders.Add(removeLeadingSpaces(headerElements[0]), removeLeadingSpaces(headerElements[1]));
            }
            if (!checkIfKeyExists(requestHeaders, "Host")) ret = false;
            if (!checkIfKeyExists(requestHeaders, "Upgrade")) ret = false;
            else if (requestHeaders["Upgrade"].IndexOf("websocket", StringComparison.InvariantCultureIgnoreCase) < 0) ret = false;
            if (!checkIfKeyExists(requestHeaders, "Connection")) ret = false;
            else if (requestHeaders["Connection"].IndexOf("Upgrade", StringComparison.InvariantCultureIgnoreCase) < 0) ret = false;
            if (!checkIfKeyExists(requestHeaders, "Sec-Websocket-Key")) ret = false;
            if (!checkIfKeyExists(requestHeaders, "Sec-Websocket-Version")) ret = false;
            else if (getByKey(requestHeaders, "Sec-Websocket-Version") != "13") ret = false;
            //TODO: Если клиент предлагает на выбор версию, надо не просто вернуть Bad request, а отправить выбранную версию!

            return ret;
        }

        private bool checkIfKeyExists (Dictionary<string, string> d, string s)
        {
            foreach (KeyValuePair<string, string> pair in d)
                if (pair.Key.ToLower() == s.ToLower())
                    return true;
            return false;
        }
        private string getByKey (Dictionary<string, string> d, string key)
        {
            foreach (KeyValuePair<string, string> pair in d)
                if (pair.Key.ToLower() == key.ToLower())
                    return pair.Value;
            return null;
        }
        
        public void Stop ()
        {
            wsl.Stop();
            client.Close();
            requestHeaders = null;
        }

        #region DataReceived EventHandlers
        void wsl_FragmentedPacketReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(0, e.LastPacket, "Last Frame #" + e.PacketFragments.Count + " received:");
        }
        void wsl_FragmentReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(1, e.LastPacket, "Single Frame #" + e.PacketFragments.Count + " received:");
        }
        void wsl_SinglePacketReceived (object sender, WSDataReceivedEventArgs e)
        {
            ReceivingHandler(2, e.LastPacket, "Single Packet received:");
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
                    Consoler.WriteLine("Соединение закрыто удаленной стороной! Код причины: {0}", ConsoleColor.Magenta, WSListener.Int16FromBytes(packet.Data[0], packet.Data[1]));
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
        #endregion

        // Конвертация массива байт в строку (byte -> char)
        private string byteArrayToString (byte[] data)
        {
            string res = "";
            foreach (byte b in data)
                res += (char)b;
            return res;
        }

        // Вычисляет секретный код для ответа в заголовках WebSocket
        private string computeSecretKey (string request)
        {
            // Алгоритм:
            // 1) Сформировать ключ конкатенацией занчения Sec-Websocket-Key и константного значения guid = 258EAFA5-E914-47DA-95CA-C5AB0DC85B11
            // 2) Вычислить SHA1 хэш-код для массива байт, представленного ключом из п. 1
            //    Результатом п. 2 является массив байт длины 20
            // 3) Закодировать хэш методом Base64
            string sec_key = string.Concat(getByKey(requestHeaders, "Sec-Websocket-Key"), guid);
            byte[] keyToBytes = Encoding.ASCII.GetBytes(sec_key);
            byte[] hashInBytes = SHA1.Create().ComputeHash(keyToBytes);
            string result_key = Convert.ToBase64String(hashInBytes, Base64FormattingOptions.None);
            return result_key;
        }
        // Возвращает массив строк
        private string[] splitString(string s, string sep = "\r\n") 
        {
            string[] result = new string[32];
            int i = 0, cnt = 0, mainCnt = 0, lastInd = 0;
            for (; i < s.Length; i++)
                if (s[i] != sep[cnt])
                    cnt = 0;
                else
                    if (++cnt == sep.Length)
                    {
                        if (result.Length - 1 <= mainCnt)
                        {
                            string[] t = new string[result.Length * 2];
                            result.CopyTo(t, 0);
                            result = t;
                        }
                        result[mainCnt++] = s.Substring(lastInd, i - lastInd - sep.Length + 1);
                        lastInd = i + 1;
                        cnt = 0;
                    }
            string[] temp = new string[mainCnt];
            for (i = 0; i < mainCnt; i++)
                temp[i] = result[i];
            return temp;
        }
        private string[] exceptEmpty (string[] arr)
        {
            string[] temp = new string[arr.Length];
            int cnt = 0;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i].Length > 0)
                    temp[cnt++] = arr[i];
            string[] result = new string[cnt];
            for (int i = 0; i < cnt; i++)
                result[i] = temp[i];
            return result;
        }
        private string removeLeadingSpaces (string s)
        {
            string result = s;
            int i = 0;
            for (; i < result.Length; i++)
                if (result[i] != ' ')
                    break;
            result = result.Substring(i);
            for (i = result.Length - 1; i >= 0; i--)
                if (result[i] != ' ')
                    break;
            return result;
        }

        public override string ToString ()
        {
            string res = @"WebSocketServer.Client:
UID\t\t\t" + uid + @"
Connected\t\t" + Connected + @"
RequestPath\t\t" + requestPath + @"
Traffic(bytes)\t\t" + traffic; ;
            return res.Replace(@"\t", "\t");
        }
    }
}
