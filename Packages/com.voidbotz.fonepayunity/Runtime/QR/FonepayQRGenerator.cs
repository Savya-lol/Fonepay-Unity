using System.Collections;
using QRCoder;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    /// <summary>
    /// QR code generator backed by QRCoder (MIT). See Plugins/QRCoder-LICENSE.txt.
    /// </summary>
    public static class FonepayQRGenerator
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

            var pixels = new Color[texSize * texSize];
            for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                Color c = matrix[row, col] ? dark : light;
                int yBase = (size - 1 - row) * pixelSize;
                int xBase = col * pixelSize;
                for (int py = 0; py < pixelSize; py++)
                {
                    int rowStart = (yBase + py) * texSize + xBase;
                    for (int px = 0; px < pixelSize; px++)
                        pixels[rowStart + px] = c;
                }
            }
            tex.SetPixels(pixels);
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
            if (string.IsNullOrEmpty(text))
                throw new System.ArgumentException("QR text must be non-empty", nameof(text));

            using var data = new QRCodeGenerator().CreateQrCode(text, MapEcc(ecc), forceUtf8: true);
            int size = data.ModuleMatrix.Count;
            var matrix = new bool[size, size];
            for (int row = 0; row < size; row++)
            {
                BitArray bits = data.ModuleMatrix[row];
                for (int col = 0; col < size; col++)
                    matrix[row, col] = bits[col];
            }
            return matrix;
        }

        static QRCodeGenerator.ECCLevel MapEcc(EccLevel e) => e switch
        {
            EccLevel.L => QRCodeGenerator.ECCLevel.L,
            EccLevel.M => QRCodeGenerator.ECCLevel.M,
            EccLevel.Q => QRCodeGenerator.ECCLevel.Q,
            EccLevel.H => QRCodeGenerator.ECCLevel.H,
            _ => QRCodeGenerator.ECCLevel.M
        };
    }
}
