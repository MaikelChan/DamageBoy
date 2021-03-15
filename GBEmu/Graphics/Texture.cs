using OpenTK.Graphics.OpenGL;
using System;

namespace GBEmu.Graphics
{
    abstract class Texture : IDisposable
    {
        internal uint TextureID => textureID;
        readonly uint textureID;

        public int Width { get; }
        public int Height { get; }

        protected readonly BaseRenderer renderer;

        protected Texture(BaseRenderer renderer, int width, int height)
        {
            this.renderer = renderer;
            Width = width;
            Height = height;

            GL.GenTextures(1, out textureID);

            if (TextureID == 0)
            {
                throw new Exception("Unable to create Texture object.");
            }
        }

        public void Dispose()
        {
            GL.DeleteTexture(TextureID);
        }

        protected bool IsPowerOf2(int value)
        {
            return (value & (value - 1)) == 0;
        }
    }
}