using UnityEngine;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// Pure C# QR Code generator. Byte mode, ECC L/M/Q/H, versions 1–10.
    /// </summary>
    public static partial class FonepayQRGenerator
    {
        public enum EccLevel { L, M, Q, H }

        public static Texture2D GenerateTexture(string text, int pixelSize = 10,
            EccLevel ecc = EccLevel.M, Color? darkColor = null, Color? lightColor = null)
        {
            bool[,] matrix = GenerateMatrix(text, ecc);
            int size = matrix.GetLength(0);
            int texSize = size * pixelSize;

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color dark = darkColor ?? Color.black;
            Color light = lightColor ?? Color.white;

            for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                Color c = matrix[row, col] ? dark : light;
                for (int py = 0; py < pixelSize; py++)
                for (int px = 0; px < pixelSize; px++)
                    tex.SetPixel(col * pixelSize + px,
                        (size - 1 - row) * pixelSize + py, c);
            }

            tex.Apply();
            return tex;
        }

        public static Sprite GenerateSprite(string text, int pixelSize = 10,
            EccLevel ecc = EccLevel.M, Color? darkColor = null, Color? lightColor = null,
            float pixelsPerUnit = 100f)
        {
            var tex = GenerateTexture(text, pixelSize, ecc, darkColor, lightColor);
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0, SpriteMeshType.FullRect);
            sprite.name = "FonepayQR";
            return sprite;
        }

        public static bool[,] GenerateMatrix(string text, EccLevel ecc = EccLevel.M)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            var (version, ecBlocks) = ChooseVersion(data.Length, ecc);
            byte[] codewords = BuildCodewords(data, version, ecc, ecBlocks);
            return BuildMatrix(version, codewords);
        }
    }
}
