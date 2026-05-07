using System;

namespace Darkmatter.Fonepay
{
    public static partial class FonepayQRGenerator
    {
        static readonly byte[] _exp = new byte[512];
        static readonly byte[] _log = new byte[256];

        static FonepayQRGenerator()
        {
            int x = 1;
            for (int i = 0; i < 255; i++)
            {
                _exp[i] = (byte)x;
                _log[x] = (byte)i;
                x <<= 1;
                if (x >= 256) x ^= 0x11d;
            }
            for (int i = 255; i < 512; i++) _exp[i] = _exp[i - 255];
        }

        static byte GfMul(byte a, byte b)
        {
            if (a == 0 || b == 0) return 0;
            return _exp[(_log[a] + _log[b]) % 255];
        }

        static byte GfPow(int b, int e) => _exp[(_log[(byte)b] * e) % 255];

        static byte[] ReedSolomon(byte[] data, int ecCount)
        {
            byte[] gen = RsGenerator(ecCount);
            byte[] msg = new byte[data.Length + ecCount];
            Array.Copy(data, msg, data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                byte coef = msg[i];
                if (coef == 0) continue;
                for (int j = 1; j < gen.Length; j++)
                    msg[i + j] ^= GfMul(gen[j], coef);
            }

            byte[] ec = new byte[ecCount];
            Array.Copy(msg, data.Length, ec, 0, ecCount);
            return ec;
        }

        static byte[] RsGenerator(int degree)
        {
            byte[] g = { 1 };
            for (int i = 0; i < degree; i++)
            {
                byte[] ng = new byte[g.Length + 1];
                byte root = GfPow(2, i);
                for (int j = 0; j < g.Length; j++)
                {
                    ng[j] ^= GfMul(g[j], root);
                    ng[j + 1] ^= g[j];
                }
                g = ng;
            }
            return g;
        }
    }
}
