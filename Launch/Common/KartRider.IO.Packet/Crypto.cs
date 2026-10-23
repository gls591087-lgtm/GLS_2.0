using System;

namespace KartRider.IO.Packet;

internal class Crypto
{
    public static byte[] specKeys2 = new byte[256]
    {
        186, 124, 112, 137, 33, 22, 176, 189, 181, 65,
        162, 87, 255, 32, 245, 239, 172, 127, 212, 84,
        41, 60, 130, 170, 218, 78, 152, 71, 203, 91,
        81, 52, 92, 146, 240, 237, 86, 63, 119, 83,
        106, 30, 24, 5, 2, 31, 8, 251, 148, 193,
        97, 113, 75, 205, 196, 132, 69, 55, 94, 39,
        234, 183, 74, 241, 201, 9, 179, 134, 177, 77,
        105, 215, 206, 192, 126, 73, 58, 164, 53, 0,
        89, 50, 168, 109, 226, 175, 111, 161, 235, 158,
        29, 16, 188, 250, 229, 173, 108, 104, 214, 45,
        72, 211, 101, 171, 198, 117, 224, 76, 6, 223,
        42, 202, 102, 68, 221, 200, 19, 142, 118, 253,
        13, 57, 238, 80, 17, 167, 14, 147, 88, 98,
        246, 247, 232, 37, 157, 210, 150, 11, 67, 129,
        160, 48, 144, 61, 163, 93, 213, 100, 217, 178,
        43, 103, 66, 197, 12, 231, 141, 184, 243, 136,
        49, 156, 143, 222, 131, 180, 187, 25, 107, 155,
        59, 208, 116, 4, 10, 233, 96, 114, 40, 190,
        36, 56, 3, 123, 138, 133, 227, 166, 249, 18,
        110, 248, 174, 82, 153, 51, 79, 85, 220, 154,
        1, 122, 199, 46, 242, 27, 121, 15, 38, 95,
        120, 151, 139, 149, 145, 28, 252, 115, 244, 44,
        21, 216, 64, 230, 185, 26, 236, 159, 54, 7,
        90, 35, 207, 204, 219, 140, 195, 135, 228, 23,
        62, 169, 194, 47, 99, 34, 128, 165, 125, 225,
        254, 20, 209, 182, 70, 191
    };

    public static byte[] specKeys1 = new byte[264]
    {
        79, 200, 44, 182, 173, 43, 108, 229, 46, 65,
        174, 137, 154, 120, 126, 207, 91, 124, 189, 116,
        251, 220, 5, 239, 42, 167, 225, 205, 215, 90,
        41, 45, 13, 4, 245, 231, 180, 133, 208, 59,
        178, 20, 110, 150, 219, 99, 203, 243, 141, 160,
        81, 195, 31, 78, 228, 57, 181, 121, 76, 170,
        21, 143, 240, 37, 222, 9, 152, 138, 113, 56,
        254, 27, 100, 75, 62, 52, 107, 69, 25, 196,
        123, 30, 193, 39, 19, 197, 36, 11, 128, 80,
        230, 29, 32, 145, 58, 209, 176, 50, 129, 244,
        147, 102, 112, 151, 97, 70, 40, 168, 96, 83,
        190, 86, 2, 51, 177, 217, 172, 105, 118, 38,
        210, 206, 201, 183, 1, 248, 74, 17, 246, 139,
        22, 164, 55, 185, 67, 237, 159, 3, 184, 212,
        235, 156, 117, 162, 142, 214, 33, 127, 48, 213,
        136, 211, 26, 194, 199, 169, 161, 134, 89, 227,
        140, 87, 10, 144, 77, 247, 187, 125, 82, 241,
        23, 103, 16, 95, 192, 85, 6, 68, 149, 66,
        165, 8, 253, 61, 157, 224, 0, 166, 92, 7,
        179, 255, 73, 49, 242, 236, 54, 153, 104, 202,
        115, 64, 111, 28, 233, 53, 72, 232, 171, 252,
        135, 101, 18, 146, 98, 71, 221, 148, 24, 234,
        198, 114, 163, 109, 106, 249, 84, 186, 238, 94,
        223, 155, 132, 175, 60, 88, 226, 35, 122, 15,
        34, 63, 204, 158, 218, 14, 130, 131, 191, 188,
        93, 47, 216, 119, 250, 12, 196, 37, 144, 0,
        0, 0, 0, 0
    };

    public static uint HashDecrypt(byte[] pData, uint nLength, uint nKey)
    {
        uint num = nKey ^ 0x14B307C8;
        uint num2 = nKey ^ 0x8CBF12ACu;
        uint num3 = nKey ^ 0x240397C1;
        uint num4 = nKey ^ 0xF3BD29C0u;
        int length = 0;
        uint hash = 0u;
        int i;
        for (i = 0; i < nLength >> 4; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, length) ^ num), 0, pData, length, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, length + 4) ^ num2), 0, pData, length + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, length + 8) ^ num3), 0, pData, length + 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, length + 12) ^ num4), 0, pData, length + 12, 4);
            hash ^= BitConverter.ToUInt32(pData, length + 12) ^ BitConverter.ToUInt32(pData, length + 8) ^ BitConverter.ToUInt32(pData, length + 4) ^ BitConverter.ToUInt32(pData, length);
            length += 16;
        }
        i *= 16;
        length = 0;
        byte[] bytes = BitConverter.GetBytes(num);
        byte[] bytes2 = BitConverter.GetBytes(num2);
        byte[] bytes3 = BitConverter.GetBytes(num3);
        byte[] bytes4 = BitConverter.GetBytes(num4);
        byte[] array = new byte[16];
        Buffer.BlockCopy(bytes, 0, array, 0, 4);
        Buffer.BlockCopy(bytes2, 0, array, 4, 4);
        Buffer.BlockCopy(bytes3, 0, array, 8, 4);
        Buffer.BlockCopy(bytes4, 0, array, 12, 4);
        while (i < nLength)
        {
            int num8 = i;
            pData[num8] ^= array[length];
            hash ^= (uint)(pData[i] << length);
            i++;
            length++;
        }
        return hash;
    }

    public static uint HashEncrypt(byte[] pData, uint nLength, uint nKey)
    {
        uint num = nKey ^ 0x14B307C8;
        uint num2 = nKey ^ 0x8CBF12ACu;
        uint num3 = nKey ^ 0x240397C1;
        uint num4 = nKey ^ 0xF3BD29C0u;
        int num5 = 0;
        uint num6 = 0u;
        int i;
        for (i = 0; i < nLength >> 4; i++)
        {
            num6 ^= BitConverter.ToUInt32(pData, num5 + 12) ^ BitConverter.ToUInt32(pData, num5 + 8) ^ BitConverter.ToUInt32(pData, num5 + 4) ^ BitConverter.ToUInt32(pData, num5);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num5) ^ num), 0, pData, num5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num5 + 4) ^ num2), 0, pData, num5 + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num5 + 8) ^ num3), 0, pData, num5 + 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num5 + 12) ^ num4), 0, pData, num5 + 12, 4);
            num5 += 16;
        }
        i *= 16;
        num5 = 0;
        byte[] bytes = BitConverter.GetBytes(num);
        byte[] bytes2 = BitConverter.GetBytes(num2);
        byte[] bytes3 = BitConverter.GetBytes(num3);
        byte[] bytes4 = BitConverter.GetBytes(num4);
        byte[] array = new byte[16];
        Buffer.BlockCopy(bytes, 0, array, 0, 4);
        Buffer.BlockCopy(bytes2, 0, array, 4, 4);
        Buffer.BlockCopy(bytes3, 0, array, 8, 4);
        Buffer.BlockCopy(bytes4, 0, array, 12, 4);
        while (i < nLength)
        {
            num6 ^= (uint)(pData[i] << num5);
            int num8 = i;
            pData[num8] ^= array[num5];
            i++;
            num5++;
        }
        return num6;
    }

    public static byte[] encryptBytes(byte[] original)
    {
        byte[] array = new byte[original.Length];
        for (byte b = 0; b < original.Length; b++)
        {
            array[b] = specKeys2[(b + original[b]) % 255];
        }
        return array;
    }

    public static byte[] decodeBytes(byte[] bytes)
    {
        byte[] array = new byte[bytes.Length];
        for (byte b = 0; b < bytes.Length; b++)
        {
            array[b] = (byte)(specKeys1[bytes[b]] - b);
        }
        return array;
    }
}
