using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WebSocketClient
{
    class Program
    {
        static TcpClient client;

        static void Main (string[] args)
        {
            string handshake = @"GET /chat HTTP/1.1
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==

";
            byte[] b = Encoding.ASCII.GetBytes(handshake);
            client = new TcpClient("localhost", 8181);
            if (client.Connected)
                client.GetStream().Write(b, 0, b.Length);

            while (true)
            {
                string s = Console.ReadLine();
                SendFragmented(s);
            }

        }

        static void Send (string s)
        {
            byte[] res = new byte[2 + s.Length];
            res[0] = 129;
            res[1] = (byte)s.Length;
            for (int i = 0; i < s.Length; i++)
                res[i + 2] = (byte)s[i];
            client.GetStream().Write(res, 0, res.Length);
        }
        static void SendFragmented (string s)
        {
            string[] fragments = s.Split(' ');

            for (int i = 0; i < fragments.Length - 1; i++)
            {
                byte[] res = new byte[2 + fragments[i].Length];
                res[0] = 1;
                res[1] = (byte)fragments[i].Length;
                for (int j = 0; j < fragments[i].Length; j++)
                    res[j + 2] = (byte)fragments[i][j];
                client.GetStream().Write(res, 0, res.Length);
            }
            string last = fragments[fragments.Length - 1];
            Send(last);
        }

        #region Parsing Methods
        public static bool getBit (byte b, int n)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            //return Convert.ToString(b, 2)[n] == '1' ? true : false;

            int k = 7 - n;
            for (int i = 0; i < k; i++)
                b /= 2;
            return b % 2 == 1 ? true : false;
        }
        public static byte setBit (byte b, int n, bool val)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            //string s = Convert.ToString(b, 2);
            //byte res = 0, mlt = 1;
            //for (int i = 0; i < 8; i++)
            //{
            //    res += s[7 - i] == '1' ? mlt : (byte)0;
            //    mlt *= 2;
            //}
            //return res;

            int k = 7 - n;
            bool[] byt = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                byt[i] = b % 2 == 1 ? true : false;
                b /= 2;
            }
            byt[k] = val;
            byte res = 0;
            byte mlt = 1;
            for (int i = 0; i < 8; i++)
            {
                res += (byt[i] ? mlt : (byte)0);
                mlt *= 2;
            }
            return res;
        }

        // Используется, для конструирования числа по битам, заключенным между d_bound и u_bound включительно.
        private static byte constructByte (byte b, int d_bound, int u_bound)
        {
            if (d_bound < 0 || d_bound > 7 || u_bound < 0 || u_bound > 7 || d_bound > u_bound)
                throw new ArgumentOutOfRangeException("Значение нижней и верхней границ должны находиться на отрезке [0; 7], и значение нижней границы не должно превышать значение верхней границы");

            byte res = 0;
            byte mlt = 1;
            for (int i = u_bound; i >= d_bound; i--)
            {
                res += getBit(b, i) ? mlt : (byte)0;
                mlt *= 2;
            }
            return res;
        }
        // Используется, для конструирования числа по битам, передаваемым в качестве параметров
        private static byte constructByte (params bool[] b)
        {
            if (b.Length > 8)
                throw new ArgumentOutOfRangeException("Количество передаваемых параметров превышает максимально допустимое 8!");

            byte res = 0;
            byte mlt = 1;
            for (int i = b.Length - 1; i >= 0; i--)
            {
                res += (b[i] ? mlt : (byte)0);
                mlt *= 2;
            }
            return res;
        }

        // Возвращает Int16, интерпретируя 2 байта как одно беззнаковое 16-битное число
        private static UInt16 Int16FromBytes (byte b1, byte b2)
        {
            UInt16 res = 0;
            int mlt = 1;
            byte[] b = new byte[2] { b1, b2 };
            for (int i = 15; i >= 0; i--)
            {
                if (getBit(b[(int)(i / 8)], i % 8))
                    res += (UInt16)mlt;
                mlt *= 2;
            }

            return res;
        }
        // Возвращает Int64, интерпретируя 8 байтов (из массива b, начиная с n-того байта) как одно беззнаковое 64-битное число
        private static UInt64 Int64FromBytes (byte[] b, int n)
        {
            if (b.Length - n < 8)
                throw new ArgumentOutOfRangeException("Не хватает места в массиве b, начиная с ячейки n");

            UInt64 res = 0;
            UInt64 mlt = 1;
            byte byt;
            for (int i = 63; i >= 0; i--)
            {
                byt = b[(int)(i / 8) + n];
                if (getBit(byt, i % 8))
                    res += mlt;
                mlt *= 2;
            }

            return res;
        }
        #endregion
    }
}
