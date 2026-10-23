using System;

namespace KartRider.Security
{
    public static class HashCryptograph
    {
        public static uint HashPacket(
            byte[] pData,
            uint pKey,
            bool pEncrypt,
            uint npacketLength)
        {
            uint hash1 = pKey ^ 0x14B307C8u;
            uint hash2 = pKey ^ 0x8CBF12ACu;
            uint hash3 = pKey ^ 0x240397C1u;
            uint hash4 = pKey ^ 0xF3BD29C0u;

            int checksum = 0;

            Action<int, uint> doHash = delegate (int index, uint num)
            {
                pData[index] ^= (byte)(num & 0xFF);
                pData[index + 1] ^= (byte)((num >> 8) & 0xFF);
                pData[index + 2] ^= (byte)((num >> 16) & 0xFF);
                pData[index + 3] ^= (byte)((num >> 24) & 0xFF);
            };

            int offset = 0;
            int i;

            for (i = 0; i < (int)npacketLength >> 4; i++)
            {
                if (pEncrypt)
                {
                    checksum ^=
                        BitConverter.ToInt32(pData, offset + 12) ^
                        BitConverter.ToInt32(pData, offset + 8) ^
                        BitConverter.ToInt32(pData, offset + 4) ^
                        BitConverter.ToInt32(pData, offset);
                }

                doHash(offset, hash1);
                doHash(offset + 4, hash2);
                doHash(offset + 8, hash3);
                doHash(offset + 12, hash4);

                if (!pEncrypt)
                {
                    checksum ^=
                        BitConverter.ToInt32(pData, offset + 12) ^
                        BitConverter.ToInt32(pData, offset + 8) ^
                        BitConverter.ToInt32(pData, offset + 4) ^
                        BitConverter.ToInt32(pData, offset);
                }

                offset += 16;
            }

            int sub = 0;

            for (; offset < (int)npacketLength; offset++)
            {
                if (pEncrypt)
                {
                    checksum ^= pData[offset] << sub;
                }

                pData[offset] ^=
                    (byte)((hash1 >> (sub * 8)) & 0xFF);

                if (!pEncrypt)
                {
                    checksum ^= pData[offset] << sub;
                }

                sub++;
            }

            return (uint)checksum;
        }

        public static uint HashDecrypt(
            byte[] pData,
            uint nLength,
            uint nKey)
        {
            uint Key1 = nKey ^ 0x14B307C8u;
            uint Key2 = nKey ^ 0x8CBF12ACu;
            uint Key3 = nKey ^ 0x240397C1u;
            uint Key4 = nKey ^ 0xF3BD29C0u;

            int index = 0;
            uint checksum = 0u;

            int i;

            for (i = 0; i < nLength >> 4; i++)
            {
                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(pData, index) ^ Key1),
                    0,
                    pData,
                    index,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(pData, index + 4) ^ Key2),
                    0,
                    pData,
                    index + 4,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(pData, index + 8) ^ Key3),
                    0,
                    pData,
                    index + 8,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(pData, index + 12) ^ Key4),
                    0,
                    pData,
                    index + 12,
                    4);

                checksum ^=
                    BitConverter.ToUInt32(pData, index + 12) ^
                    BitConverter.ToUInt32(pData, index + 8) ^
                    BitConverter.ToUInt32(pData, index + 4) ^
                    BitConverter.ToUInt32(pData, index);

                index += 16;
            }

            i *= 16;
            index = 0;

            byte[] Key1Array =
                BitConverter.GetBytes(Key1);

            byte[] Key2Array =
                BitConverter.GetBytes(Key2);

            byte[] Key3Array =
                BitConverter.GetBytes(Key3);

            byte[] Key4Array =
                BitConverter.GetBytes(Key4);

            byte[] TotalArray =
                new byte[16];

            Buffer.BlockCopy(
                Key1Array,
                0,
                TotalArray,
                0,
                4);

            Buffer.BlockCopy(
                Key2Array,
                0,
                TotalArray,
                4,
                4);

            Buffer.BlockCopy(
                Key3Array,
                0,
                TotalArray,
                8,
                4);

            Buffer.BlockCopy(
                Key4Array,
                0,
                TotalArray,
                12,
                4);

            while (i < nLength)
            {
                pData[i] ^=
                    TotalArray[index];

                checksum ^=
                    (uint)(pData[i] << index);

                i++;
                index++;
            }

            return checksum;
        }

        public static uint HashEncrypt(
            byte[] pData,
            uint nLength,
            uint nKey)
        {
            uint Key1 = nKey ^ 0x14B307C8u;
            uint Key2 = nKey ^ 0x8CBF12ACu;
            uint Key3 = nKey ^ 0x240397C1u;
            uint Key4 = nKey ^ 0xF3BD29C0u;

            int index = 0;
            uint checksum = 0u;

            int i;

            for (i = 0; i < nLength >> 4; i++)
            {
                checksum ^=
                    BitConverter.ToUInt32(
                        pData,
                        index + 12) ^
                    BitConverter.ToUInt32(
                        pData,
                        index + 8) ^
                    BitConverter.ToUInt32(
                        pData,
                        index + 4) ^
                    BitConverter.ToUInt32(
                        pData,
                        index);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(
                            pData,
                            index) ^ Key1),
                    0,
                    pData,
                    index,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(
                            pData,
                            index + 4) ^ Key2),
                    0,
                    pData,
                    index + 4,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(
                            pData,
                            index + 8) ^ Key3),
                    0,
                    pData,
                    index + 8,
                    4);

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        BitConverter.ToUInt32(
                            pData,
                            index + 12) ^ Key4),
                    0,
                    pData,
                    index + 12,
                    4);

                index += 16;
            }

            i *= 16;
            index = 0;

            byte[] Key1Array =
                BitConverter.GetBytes(Key1);

            byte[] Key2Array =
                BitConverter.GetBytes(Key2);

            byte[] Key3Array =
                BitConverter.GetBytes(Key3);

            byte[] Key4Array =
                BitConverter.GetBytes(Key4);

            byte[] TotalArray =
                new byte[16];

            Buffer.BlockCopy(
                Key1Array,
                0,
                TotalArray,
                0,
                4);

            Buffer.BlockCopy(
                Key2Array,
                0,
                TotalArray,
                4,
                4);

            Buffer.BlockCopy(
                Key3Array,
                0,
                TotalArray,
                8,
                4);

            Buffer.BlockCopy(
                Key4Array,
                0,
                TotalArray,
                12,
                4);

            while (i < nLength)
            {
                checksum ^=
                    (uint)(pData[i] << index);

                pData[i] ^=
                    TotalArray[index];

                i++;
                index++;
            }

            return checksum;
        }
    }
}