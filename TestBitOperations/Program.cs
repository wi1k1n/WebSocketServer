using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TestBitOperations
{
    class Program
    {
        static void Main(string[] args)
        {
            byte bo = 129;
            int l = 0;
            int u = 3;
            byte br = constructByte(bo, l, u);
            Console.WriteLine("{0} ({1}) -> {2} ({3})", bo, byte2str(bo), br, byte2str(br));
            return;
            Random rnd = new Random(1);
            byte[] bts = new byte[256]; for (int i = 0; i < 256; i++) bts[i] = (byte)i;

            for (int i = 0; i < bts.Length; i++)
            {
                bool[] bln = new bool[8];
                bool[] blo = new bool[8];
                for (int j = 0; j < 8; j++) {
                    bln[j] = ByteManager.getBit(bts[i], j);
                    blo[j] = ByteManagerOld.getBit(bts[i], j);
                    //if (bn != bo)
                    //    throw new Exception();
                }
                Console.WriteLine("{0} <-> {1}", boolArr2str(bln), boolArr2str(blo));
            }



            ////////// Test boolArr2str method
            ////for (byte i = 0; i < 255; i++)
            ////    Console.WriteLine("{0} -> {1}", i, boolArr2str(ByteManager.decomposeByte(i)));

            ////////// Test new construct byte from bool[8] method
            //// Generate N=5 random bytes
            //Random rnd = new Random(1);
            //List<byte> bts = new List<byte>(new byte[] { 117 });
            //for (int i = 0; i < 5; i++)
            //    bts.Add((byte)rnd.Next(0, 256));
            //bts = new List<byte>(256);
            //for (int i = 0; i < 256; i++)
            //    bts.Add((byte)i);

            //// For each of them
            //foreach (byte bt in bts)
            //{
            //    // Decompose it into bool[8]
            //    bool[] b = ByteManager.decomposeByte(bt);

            //    byte btn = ByteManager.constructByte(b);

            //    //if (btn != bt)
            //        // Output results
            //        Console.WriteLine(">> byte {0} -> bool[] {1} -> byte {2}", bt, boolArr2str(b), btn);
            //}

            //return;
        }
        public static byte constructByte(byte b, int s, int w)
        {
            int l = s;
            int u = w;

            //(255 >> (7 - u + l));
            return (byte)((b >> l) & (255 >> (7 - u + l)));

            int r = s + w - 1;
            if (s < 0 || s > 7 || r < 0 || r > 7 || s > r)
                throw new ArgumentOutOfRangeException("Значение нижней и верхней границ должны находиться на отрезке [0; 7], и значение нижней границы должно быть не больше значения верхней границы");

            return (byte)((((255 << (7 - r)) & (255 >> s)) & b) >> (7 - r));
        }
        static string boolArr2str(bool[] arr)
        {
            if (arr.Length != 8)
                throw new Exception("Input boolean array must be of length 8!");

            char[] res = new char[arr.Length];
            for (int i = arr.Length - 1; i >= 0; i--)
                res[arr.Length - i - 1] = arr[i] ? '1' : '0';
            return new string(res);
        }
        static string byte2str(byte b)
        {
            return boolArr2str(ByteManager.decomposeByte(b));
        }
        void oldMain()
        {
            Random rnd = new Random();
            Stopwatch sw = new Stopwatch();


            sw.Start();
            ulong v = 4693387652383541033;
            Console.WriteLine(v);
            byte[] res = bytesFromInt64(v);
            Console.WriteLine(Int64FromBytes(res, 0));
            sw.Stop();
            Console.WriteLine("res = {0} {1}\nticks: {2}", res[0], res[1], sw.ElapsedTicks);
            sw.Restart();
            //res = bytesFromInt16Old(v);
            sw.Stop();
            Console.WriteLine("res = {0} {1}\nticks: {2}", res[0], res[1], sw.ElapsedTicks);

            Console.ReadKey();
            return;



            byte b = (byte)rnd.Next(0, 256);
            int l = rnd.Next(0, 8);
            int r = rnd.Next(0, 8);
            if (r < l)
            {
                int t = r;
                r = l;
                l = t;
            }

            Console.Write("l = {0}\nr = {1}\nb = {2} = ", l, r, b);

            string bstr1 = Convert.ToString(b, 2);
            string bstr = "";
            for (int i = 0; i < 8 - bstr1.Length; i++)
                bstr += "0";
            bstr += bstr1;
            for (int i = 0; i < 8; i++)
            {
                if (i <= r && i >= l)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                Console.Write(bstr[i]);
            }
            //byte b1 = constructByte(b, l, r - l + 1);
            //string bstr2 = Convert.ToString(b1, 2);

            //Console.WriteLine("\nb' = {0} = {1}", b1, bstr2);

            Console.ReadKey();
        }
        public static byte[] bytesFromInt64(UInt64 num)
        {
            //TODO: Ловим эксепшны!
            byte[] res = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                res[7 - i] = (byte)(num & 255);
                num = num >> 8;
            }
            return res;
        }
        public static UInt64 Int64FromBytes(byte[] b, int n)
        {
            if (b.Length - n < 8)
                throw new ArgumentOutOfRangeException("Не хватает места в массиве b, начиная с ячейки n");

            ulong r = 0;
            for (int i = 0; i < 8; i++)
                r = (r << 8) | b[n + i];
            return r;
        }

        // Используется, для конструирования числа по битам, заключенным между d_bound и u_bound включительно.
        //public static byte constructByte(byte b, int s, int w)
        //{
        //    int r = s + w - 1;
        //    if (s < 0 || s > 7 || r < 0 || r > 7 || s > r)
        //        throw new ArgumentOutOfRangeException("Значение нижней и верхней границ должны находиться на отрезке [0; 7], и значение нижней границы должно быть не больше значения верхней границы");

        //    return (byte)((((255 << (7 - r)) & (255 >> s)) & b) >> (7 - r));
        //}
        public static bool getBit(byte b, int n)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            return (b & (1 << n)) > 0;
        }
        public static byte setBit(byte b, int n, bool val)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            return (byte)((b & ~(1 << n)) | ((val ? 1 : 0) << n));
        }
    }

    public static class ByteManagerOld
    {
        #region Parsing Methods
        // Возвращает n-тый бит в байте b
        public static bool getBit(byte b, int n)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            //return Convert.ToString(b, 2)[n] == '1' ? true : false;

            int k = 7 - n;
            for (int i = 0; i < k; i++)
                b /= 2;
            return b % 2 == 1 ? true : false;
        }
        // Устанавливает n-тый бит в байте b в занчение val
        public static byte setBit(byte b, int n, bool val)
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
        public static byte constructByte(byte b, int d_bound, int u_bound)
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
        public static byte constructByte(params bool[] b)
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
        public static UInt16 Int16FromBytes(byte b1, byte b2)
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
        public static UInt64 Int64FromBytes(byte[] b, int n)
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

        //TODO: Совместить bytesFromInt16 и bytesFromInt64 в один метод
        // Конструируем массив байт длиной 2, интерпретируя ushort как 16-битное число
        public static byte[] bytesFromInt16(UInt16 num)
        {
            //TODO: Ловим эксепшны!
            byte[] res = new byte[2];
            bool[] buf = new bool[8];
            for (int i = 0; i < 16; i++)
            {
                buf[7 - i % 8] = num % 2 == 1 ? true : false;
                num /= 2;
                if (i % 8 == 7)
                    res[1 - (int)(i / 8)] = constructByte(buf);
            }
            return res;
        }
        // Аналогично bytesFromInt16, записывает ulong в массив байт длины 8
        public static byte[] bytesFromInt64(UInt64 num)
        {
            //TODO: Ловим эксепшны!
            byte[] res = new byte[8];
            bool[] buf = new bool[8];
            for (int i = 0; i < 64; i++)
            {
                buf[7 - i % 8] = num % 2 == 1 ? true : false;
                num /= 2;
                if (i % 8 == 7)
                    res[7 - (int)(i / 8)] = constructByte(buf);
            }
            return res;
        }
        #endregion
    }
}
