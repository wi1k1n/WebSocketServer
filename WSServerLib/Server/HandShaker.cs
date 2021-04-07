using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace WSServerLib
{
    class HandShaker
    {
        private const string WSGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private Stream stream;
        private int bufSize;
        private uint traffic = 0;
        private Dictionary<string, string> requestHeaders;
        private string requestPath = "";
        private string httpVersion = "";

        public HandShaker (Stream stream, int bufSize)
        {
            this.stream = stream;
            this.bufSize = bufSize;
        }

        public int HandShake ()
        {
            string request = readHandshakeRequest();
            if (request == null) return -1;

            bool requestStatus = validateRequest(request);
            string response = "";

            if (requestStatus)
                // Вычисляем секретный код для WebSocket'a
                response = generateResponse(101, "Connection: Upgrade", "Upgrade: websocket", string.Concat("Sec-Websocket-Accept: ", computeSecretKey(request)));
            else
            {
                //Consoler.WriteLine("Полученный от клиента запрос не валиден! Будет отправлен: '400 Bad request' ответ", ConsoleColor.Magenta);
                response = generateResponse(400, "Connection: Upgrade", "Upgrade: websocket");
                return -2;
            }

            // Переводим строку в массив байт и посылаем его
            byte[] responseBuf = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuf, 0, responseBuf.Length);
            traffic += (uint)responseBuf.Length;

            // Выводим в консоль отправленные заголовки
            //Consoler.WriteLine("HandShake Sent:", ConsoleColor.Green);
            //Consoler.WriteLine(response, ConsoleColor.DarkGreen);

            return (int)traffic;
            // Возвращаемые коды:
            // -1 - Ошибка получения запроса клиента
            // -2 - Полученный от клиента запрос невалиден
        }
        private string generateResponse (int responseCode, params string[] headers)
        {
            string res = string.Concat(httpVersion != "" ? httpVersion : "HTTP/1.1", " ", responseCode, " ");
            switch (responseCode)
            {
                case 101:
                    res += "Switching protocols\n";
                    break;
                case 400:
                    res += "Bad request\n";
                    break;
                default:
                    res = null;
                    break;
            }
            foreach (string header in headers)
                res += string.Concat(header, "\n");

            return string.Concat(res, "\n");
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
                while ((cnt = stream.Read(buf, 0, bufSize)) > 0)
                {
                    request += Encoding.ASCII.GetString(buf, 0, cnt);
                    if (request.IndexOf("\r\n\r\n") > 0)
                        break;
                }
            } catch { return null; }
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
        private string[] splitString (string s, string sep = "\r\n")
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
        private string computeSecretKey (string request)
        {
            // Алгоритм:
            // 1) Сформировать ключ конкатенацией занчения Sec-Websocket-Key и константного значения guid = 258EAFA5-E914-47DA-95CA-C5AB0DC85B11
            // 2) Вычислить SHA1 хэш-код для массива байт, представленного ключом из п. 1
            //    Результатом п. 2 является массив байт длины 20
            // 3) Закодировать хэш методом Base64
            string sec_key = string.Concat(getByKey(requestHeaders, "Sec-Websocket-Key"), WSGuid);
            byte[] keyToBytes = Encoding.ASCII.GetBytes(sec_key);
            byte[] hashInBytes = SHA1.Create().ComputeHash(keyToBytes);
            string result_key = Convert.ToBase64String(hashInBytes, Base64FormattingOptions.None);
            return result_key;
        }
    }
}
