using System;

namespace Darkmatter.Fonepay
{
    public static partial class FonepayQRGenerator
    {
        // [version-1][eccLevel] = (totalCodewords, ecCodewordsPerBlock, blocks)
        static readonly (int total, int ecPerBlock, int blocks)[,] _caps =
        {
            { (19, 7, 1), (16, 10, 1), (13, 13, 1), (9, 17, 1) },
            { (34, 10, 1), (28, 16, 1), (22, 22, 1), (16, 28, 1) },
            { (55, 15, 1), (44, 26, 1), (34, 18, 2), (26, 22, 2) },
            { (80, 20, 2), (64, 18, 2), (48, 26, 4), (36, 16, 4) },
            { (108, 26, 2), (86, 24, 2), (62, 18, 2), (46, 22, 2) },
            { (136, 18, 4), (108, 16, 4), (76, 24, 4), (60, 28, 4) },
            { (156, 20, 4), (124, 18, 4), (88, 18, 6), (66, 26, 4) },
            { (194, 24, 4), (154, 22, 4), (110, 22, 6), (86, 26, 4) },
            { (232, 30, 4), (182, 22, 5), (132, 20, 8), (100, 24, 4) },
            { (274, 18, 6), (216, 26, 6), (154, 24, 8), (122, 28, 6) },
        };

        static int DataCodewords(int ver, EccLevel ecc)
        {
            var (total, ecPer, blks) = _caps[ver - 1, (int)ecc];
            return total - ecPer * blks;
        }

        static (int version, (int total, int ecPerBlock, int blocks) ecBlocks)
            ChooseVersion(int byteLen, EccLevel ecc)
        {
            for (int v = 1; v <= 10; v++)
            {
                int dc = DataCodewords(v, ecc);
                if (dc >= byteLen + 2)
                    return (v, _caps[v - 1, (int)ecc]);
            }
            throw new Exception($"Data too large for versions 1-10 (byte mode, ecc={ecc})");
        }
    }
}
