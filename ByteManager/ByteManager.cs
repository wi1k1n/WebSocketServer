using System;
using System.Diagnostics;

public class ByteManager
{
    #region Parsing Methods
    // Возвращает n-тый бит в байте b
    public static bool getBit(byte b, int n)
    {
        if (n < 0 || n > 7)
            throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

        return (b & (1 << n)) > 0;
    }
    // Устанавливает n-тый бит в байте b в занчение val
    public static byte setBit(byte b, int n, bool val)
    {
        if (n < 0 || n > 7)
            throw new ArgumentOutOfRangeException("Значение номера бита должно находиться на отрезке [0; 7]");

        return (byte)((b & ~(1 << n)) | ((val ? 1 : 0) << b));
    }

    // Используется для конструирования числа по битам, заключенным между d_bound и u_bound включительно.
    public static byte constructByte(byte b, int s, int w)
    {
        int r = s + w - 1;
        if (s < 0 || s > 7 || r < 0 || r > 7 || s > r)
            throw new ArgumentOutOfRangeException("Значение нижней и верхней границ должны находиться на отрезке [0; 7], и значение нижней границы должно быть не больше значения верхней границы");

        return (byte)((((255 << (7 - r)) & (255 >> s)) & b) >> (7 - r));
    }
    // Используется, для конструирования числа по битам, передаваемым в качестве параметров
    public static byte constructByte(params bool[] b)
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
    public static UInt16 Int16FromBytes(byte b1, byte b2)
    {
        return (ushort)((b1 << 8) | b2);
    }
    // Возвращает Int64, интерпретируя 8 байтов (из массива b, начиная с n-того байта) как одно беззнаковое 64-битное число
    public static UInt64 Int64FromBytes(byte[] b, int n)
    {
        if (b.Length - n < 8)
            throw new ArgumentOutOfRangeException("Не хватает места в массиве b, начиная с ячейки n");

        ulong r = 0;
        for (int i = 0; i < 8; i++)
            r = (r << 8) | b[n + i];
        return r;
    }

    //TODO: Совместить bytesFromInt16 и bytesFromInt64 в один метод
    // Конструируем массив байт длиной 2, интерпретируя ushort как 16-битное число
    public static byte[] bytesFromInt16(UInt16 num)
    {
        //TODO: Ловим эксепшны!
        return new byte[] { (byte)(num >> 8), (byte)(num & 255) };
    }
    // Аналогично bytesFromInt16, записывает ulong в массив байт длины 8
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
    #endregion
}
