
namespace DamageBoy.Graphics
{
    internal static class GraphicsUtils
    {
        public static void BgraToRgba(byte[] pixels, int pixelCount)
        {
            for (int p = 0; p < pixelCount; p++)
            {
                byte b = pixels[p * 4 + 0];
                byte g = pixels[p * 4 + 1];
                byte r = pixels[p * 4 + 2];
                byte a = pixels[p * 4 + 3];

                pixels[p * 4 + 0] = r;
                pixels[p * 4 + 1] = g;
                pixels[p * 4 + 2] = b;
                pixels[p * 4 + 3] = a;
            }
        }
    }
}