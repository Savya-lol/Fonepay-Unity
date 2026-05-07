using System;

namespace Darkmatter.Fonepay
{
    public static partial class FonepayQRGenerator
    {
        static bool[,] BuildMatrix(int version, byte[] codewords)
        {
            int size = version * 4 + 17;
            var matrix = new bool[size, size];
            var reserved = new bool[size, size];

            PlaceFinder(matrix, reserved, 0, 0);
            PlaceFinder(matrix, reserved, 0, size - 7);
            PlaceFinder(matrix, reserved, size - 7, 0);
            PlaceTiming(matrix, reserved, size);
            PlaceDarkModule(matrix, reserved, version);
            if (version >= 2) PlaceAlignment(matrix, reserved, version);
            ReserveFormat(reserved, size);
            PlaceData(matrix, reserved, codewords, size);

            int bestPenalty = int.MaxValue;
            bool[,] bestMatrix = null;
            for (int m = 0; m < 8; m++)
            {
                var candidate = (bool[,])matrix.Clone();
                ApplyMask(candidate, reserved, m, size);
                ApplyFormatInfo(candidate, reserved, 1, m, size);
                int penalty = CalcPenalty(candidate, size);
                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestMatrix = (bool[,])candidate.Clone();
                }
            }
            return bestMatrix;
        }

        static void PlaceFinder(bool[,] m, bool[,] r, int row, int col)
        {
            for (int dr = -1; dr <= 7; dr++)
            for (int dc = -1; dc <= 7; dc++)
            {
                int rr = row + dr, cc = col + dc;
                if (rr < 0 || cc < 0 || rr >= m.GetLength(0) || cc >= m.GetLength(1)) continue;
                r[rr, cc] = true;
                bool inOuter = dr >= 0 && dr <= 6 && dc >= 0 && dc <= 6;
                bool inBorder = (dr == 0 || dr == 6 || dc == 0 || dc == 6) && inOuter;
                bool inInner = dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4;
                m[rr, cc] = !(dr == -1 || dc == -1 || dr == 7 || dc == 7) && (inBorder || inInner);
            }
        }

        static void PlaceTiming(bool[,] m, bool[,] r, int size)
        {
            for (int i = 8; i < size - 8; i++)
            {
                bool v = i % 2 == 0;
                m[6, i] = v; r[6, i] = true;
                m[i, 6] = v; r[i, 6] = true;
            }
        }

        static void PlaceDarkModule(bool[,] m, bool[,] r, int ver)
        {
            int row = ver * 4 + 9;
            m[row, 8] = true;
            r[row, 8] = true;
        }

        static readonly int[][] _alignCenters =
        {
            new int[] { },
            new[] { 6, 18 },
            new[] { 6, 22 },
            new[] { 6, 26 },
            new[] { 6, 30 },
            new[] { 6, 34 },
            new[] { 6, 22, 38 },
            new[] { 6, 24, 42 },
            new[] { 6, 26, 46 },
            new[] { 6, 28, 50 },
        };

        static void PlaceAlignment(bool[,] m, bool[,] r, int version)
        {
            int[] centers = _alignCenters[version - 1];
            foreach (int row in centers)
            foreach (int col in centers)
            {
                if (r[row, col]) continue;
                for (int dr = -2; dr <= 2; dr++)
                for (int dc = -2; dc <= 2; dc++)
                {
                    bool dark = Math.Abs(dr) == 2 || Math.Abs(dc) == 2 || (dr == 0 && dc == 0);
                    m[row + dr, col + dc] = dark;
                    r[row + dr, col + dc] = true;
                }
            }
        }

        static void ReserveFormat(bool[,] r, int size)
        {
            for (int i = 0; i <= 8; i++)
            {
                r[8, i] = true;
                r[i, 8] = true;
            }
            for (int i = size - 8; i < size; i++)
            {
                r[8, i] = true;
                r[i, 8] = true;
            }
        }

        static void PlaceData(bool[,] m, bool[,] r, byte[] cw, int size)
        {
            int cwIdx = 0, bitIdx = 7;
            bool upward = true;
            int col = size - 1;

            while (col > 0)
            {
                if (col == 6) col--;
                for (int rowStep = 0; rowStep < size; rowStep++)
                {
                    int row = upward ? size - 1 - rowStep : rowStep;
                    for (int c = 0; c < 2; c++)
                    {
                        int cc = col - c;
                        if (r[row, cc]) continue;
                        bool bit = cwIdx < cw.Length && ((cw[cwIdx] >> bitIdx) & 1) == 1;
                        m[row, cc] = bit;
                        if (--bitIdx < 0)
                        {
                            bitIdx = 7;
                            cwIdx++;
                        }
                    }
                }
                upward = !upward;
                col -= 2;
            }
        }

        static void ApplyMask(bool[,] m, bool[,] r, int mask, int size)
        {
            for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                if (r[row, col]) continue;
                bool flip = mask switch
                {
                    0 => (row + col) % 2 == 0,
                    1 => row % 2 == 0,
                    2 => col % 3 == 0,
                    3 => (row + col) % 3 == 0,
                    4 => (row / 2 + col / 3) % 2 == 0,
                    5 => row * col % 2 + row * col % 3 == 0,
                    6 => (row * col % 2 + row * col % 3) % 2 == 0,
                    7 => ((row + col) % 2 + row * col % 3) % 2 == 0,
                    _ => false
                };
                if (flip) m[row, col] ^= true;
            }
        }

        static readonly ushort[] _formatL = { 0x77C4, 0x72F3, 0x7DAA, 0x789D, 0x662F, 0x6318, 0x6C41, 0x6976 };
        static readonly ushort[] _formatM = { 0x5412, 0x5125, 0x5E7C, 0x5B4B, 0x45F9, 0x40CE, 0x4F97, 0x4AA0 };
        static readonly ushort[] _formatQ = { 0x355F, 0x3068, 0x3F31, 0x3A06, 0x24B4, 0x2183, 0x2EDA, 0x2BED };
        static readonly ushort[] _formatH = { 0x1689, 0x13BE, 0x1CE7, 0x19D0, 0x0762, 0x0255, 0x0D0C, 0x083B };

        static void ApplyFormatInfo(bool[,] m, bool[,] r, int eccIndex, int mask, int size)
        {
            ushort[] table = eccIndex switch { 0 => _formatM, 1 => _formatL, 2 => _formatH, _ => _formatQ };
            ushort fmt = table[mask];

            int[] seq = { 0, 1, 2, 3, 4, 5, 7, 8 };
            for (int i = 0; i < 8; i++)
            {
                bool bit = ((fmt >> (14 - i)) & 1) == 1;
                m[8, seq[i]] = bit;
                m[seq[i < 6 ? i : i + 1 < 8 ? i : i], 8] = bit;
            }

            for (int i = 0; i < 7; i++)
            {
                bool bit = ((fmt >> i) & 1) == 1;
                m[size - 1 - i, 8] = bit;
            }
            for (int i = 7; i < 15; i++)
            {
                bool bit = ((fmt >> i) & 1) == 1;
                m[8, size - 15 + i] = bit;
            }
        }

        static int CalcPenalty(bool[,] m, int size)
        {
            int p = 0;
            for (int r = 0; r < size; r++)
            {
                int run = 1;
                for (int c = 1; c < size; c++)
                {
                    if (m[r, c] == m[r, c - 1])
                    {
                        run++;
                        if (run == 5) p += 3;
                        else if (run > 5) p++;
                    }
                    else run = 1;
                }
                run = 1;
                for (int c = 1; c < size; c++)
                {
                    if (m[c, r] == m[c - 1, r])
                    {
                        run++;
                        if (run == 5) p += 3;
                        else if (run > 5) p++;
                    }
                    else run = 1;
                }
            }

            for (int r = 0; r < size - 1; r++)
            for (int c = 0; c < size - 1; c++)
                if (m[r, c] == m[r, c + 1] && m[r, c] == m[r + 1, c] && m[r, c] == m[r + 1, c + 1])
                    p += 3;
            return p;
        }
    }
}
