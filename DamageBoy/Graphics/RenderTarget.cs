using OpenTK.Graphics.OpenGL;
using System;

namespace DamageBoy.Graphics
{
    class RenderTarget : IDisposable
    {
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        readonly uint colorTextureID;
        readonly uint depthRenderbufferID;
        readonly uint framebufferID;

        public RenderTarget(BaseRenderer renderer, uint width, uint height, bool hasDepthBuffer)
        {
            // Create color texture and buffers

            GL.GenTextures(1, out colorTextureID);

            if (hasDepthBuffer)
            {
                GL.GenRenderbuffers(1, out depthRenderbufferID);
            }

            GL.GenFramebuffers(1, out framebufferID);

            // Configure color texture

            renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, colorTextureID);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)width, (int)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Configure the depth buffer

            if (hasDepthBuffer)
            {
                renderer.PipelineState.CurrentRenderBuffer = depthRenderbufferID;
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent16, (int)width, (int)height);
            }

            // Bind color and depth to the framebuffer

            renderer.PipelineState.BindRenderTarget(FramebufferTarget.Framebuffer, framebufferID);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTextureID, 0);
            if (hasDepthBuffer)
            {
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderbufferID);
            }

            Width = width;
            Height = height;
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(framebufferID);
            GL.DeleteRenderbuffer(depthRenderbufferID);
            GL.DeleteTexture(colorTextureID);
        }
    }
}