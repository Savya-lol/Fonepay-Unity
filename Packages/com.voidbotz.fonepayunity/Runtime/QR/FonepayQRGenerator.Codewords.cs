using System;
using System.Collections.Generic;

namespace Darkmatter.Fonepay
{
    public static partial class FonepayQRGenerator
    {
        static byte[] BuildCodewords(byte[] data, int version, EccLevel ecc,
            (int total, int ecPerBlock, int blocks) ecBlocks)
        {
            var bits = new List<bool>();
            bits.AddRange(new[] { false, true, false, false }); // byte mode
            int len = data.Length;
            for (int i = 7; i >= 0; i--) bits.Add(((len >> i) & 1) == 1);
            foreach (byte b in data)
                for (int i = 7; i >= 0; i--)
                    bits.Add(((b >> i) & 1) == 1);

            int dcBytes = DataCodewords(version, ecc);
            int targetBits = dcBytes * 8;
            for (int i = 0; i < 4 && bits.Count < targetBits; i++) bits.Add(false);
            while (bits.Count % 8 != 0) bits.Add(false);

            bool[] padA = { true, true, true, false, true, true, false, false };
            bool[] padB = { false, false, false, true, false, false, false, true };
            int pi = 0;
            while (bits.Count < targetBits)
            {
                bits.AddRange(pi % 2 == 0 ? padA : padB);
                pi++;
            }

            byte[] dc = new byte[dcBytes];
            for (int i = 0; i < dcBytes; i++)
                for (int b = 0; b < 8; b++)
                    if (bits[i * 8 + b])
                        dc[i] |= (byte)(1 << (7 - b));

            int blocks = ecBlocks.blocks;
            int ecPer = ecBlocks.ecPerBlock;
            int blockSize = dcBytes / blocks;
            byte[][] dcBlocks = new byte[blocks][];
            byte[][] ecBlocksArr = new byte[blocks][];
            for (int i = 0; i < blocks; i++)
            {
                dcBlocks[i] = new byte[blockSize];
                Array.Copy(dc, i * blockSize, dcBlocks[i], 0, blockSize);
                ecBlocksArr[i] = ReedSolomon(dcBlocks[i], ecPer);
            }

            var result = new List<byte>();
            for (int i = 0; i < blockSize; i++)
                for (int b = 0; b < blocks; b++)
                    result.Add(dcBlocks[b][i]);
            for (int i = 0; i < ecPer; i++)
                for (int b = 0; b < blocks; b++)
                    result.Add(ecBlocksArr[b][i]);

            return result.ToArray();
        }
    }
}
