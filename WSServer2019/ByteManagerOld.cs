using System;

namespace WebSocketServer
{
    public static class ByteManager
    {
        #region Parsing Methods
        // Возвращает n-тый бит в байте b
        public static bool getBit (byte b, int n)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            return (b & (1 << (7 - n))) > 0;

            int k = 7 - n;
            for (int i = 0; i < k; i++)
                b /= 2;
            return b % 2 == 1 ? true : false;
        }
        // Устанавливает n-тый бит в байте b в занчение val
        public static byte setBit (byte b, int n, bool val)
        {
            if (n < 0 || n > 7)
                throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

            return (byte)((b & ~(1 << n)) | ((val ? 1 : 0) << b));

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
        public static byte constructByte (byte b, int l, int u)
        {
            //int l = 7 - u;
            //int u = 7 - l;
            if (l < 0 || l > 7 || u < 0 || u > 7 || l > u)
                throw new ArgumentOutOfRangeException("Значение нижней и верхней границ должны находиться на отрезке [0; 7], и значение нижней границы не должно превышать значение верхней границы");

            return (byte)((b >> l) & (255 >> (7 - u + l)));
        }
        // Используется, для конструирования числа по битам, передаваемым в качестве параметров
        public static byte constructByte (params bool[] b)
        {
            if (b.Length > 8)
                throw new ArgumentOutOfRangeException("Количество передаваемых параметров превышает максимально допустимое 8!");
            if (b.Length < 8)
            {
                bool[] bn = new bool[8];
                b.CopyTo(bn, 0);
            }

            int msbInd;
            // Finding index of Most Significant Bit (MSB) (aka last True in array)
            for (msbInd = b.Length - 1; msbInd >= 0; msbInd--)
                if (b[msbInd]) break;
            int res = 0;
            // Iterating over significant bits
            for (; msbInd >= 0; msbInd--)
            {
                res = res << 1;
                if (b[msbInd])
                    res += 1;
            }
            return (byte)res;
        }

        // Decompose byte into array of booleans
        public static bool[] decomposeByte(byte b)
        {
            bool[] res = new bool[8];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = b % 2 == 1;
                b = (byte)(b >> 1);
            }
            return res;
        }

        // Возвращает Int16, интерпретируя 2 байта как одно беззнаковое 16-битное число
        public static UInt16 Int16FromBytes (byte b1, byte b2)
        {
            // TODO: bitwise version is not tested!!
            //return (ushort)((b1 << 8) | b2);

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
        public static UInt64 Int64FromBytes (byte[] b, int n)
        {
            // TODO: bitwise version is not tested!!
            if (b.Length - n < 8)
                throw new ArgumentOutOfRangeException("Не хватает места в массиве b, начиная с ячейки n");

            ulong r = 0;
            for (int i = 0; i < 8; i++)
                r = (r << 8) | b[n + i];
            return r;

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
        public static byte[] bytesFromInt16 (UInt16 num)
        {
            //TODO: Ловим эксепшны!
            // TODO: bitwise version is not tested!!
            return new byte[] { (byte)(num >> 8), (byte)(num & 255) };

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
        public static byte[] bytesFromInt64 (UInt64 num)
        {
            //TODO: Ловим эксепшны!
            // TODO: bitwise version is not tested!!
            byte[] res = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                res[7 - i] = (byte)(num & 255);
                num = num >> 8;
            }
            return res;

            res = new byte[8];
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
