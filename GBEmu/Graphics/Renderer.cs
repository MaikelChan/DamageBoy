using GBEmu.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace GBEmu.Graphics
{
    class Renderer : BaseRenderer
    {
        readonly Color4 clearColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
        readonly Color4 pixelOffColor = new Color4(27, 64, 51, 255);
        readonly Color4 pixelOnColor = new Color4(195, 245, 162, 255);

        readonly uint vao;
        readonly Texture2D screenTexture;
        readonly ScreenMaterial screenMaterial;

        int viewportX, viewportY, viewportWidth, viewportHeight;
        double elapsedTime;

        public Renderer() : base()
        {
            // A VAO is not needed, but OpenGL complains if there's no one bound
            GL.GenVertexArrays(1, out vao);
            PipelineState.CurrentVAO = vao;

#if DEBUG
            if (KHRDebugExtension)
            {
                string vaoName = $"Dummy VAO";
                GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, vao, vaoName.Length, vaoName);
            }
#endif

            screenTexture = new Texture2D(this, PPU.RES_X, PPU.RES_Y, TextureFormats.R8, IntPtr.Zero, "Screen");
            screenTexture.SetMinFilter(TextureMinFilter.Nearest);
            screenTexture.SetMagFilter(TextureMagFilter.Nearest);
            screenTexture.SetWrapS(TextureWrapMode.ClampToEdge);
            screenTexture.SetWrapT(TextureWrapMode.ClampToEdge);

            screenMaterial = new ScreenMaterial(this);
            screenMaterial.MainTexture = screenTexture;
        }

        public override void Dispose()
        {
            base.Dispose();

            GL.DeleteVertexArray(vao);
            screenTexture.Dispose();
            Shader.DisposeAll();
        }

        public override void Render(double deltaTime)
        {
            PipelineState.SetViewport(viewportX, viewportY, viewportWidth, viewportHeight);
            PipelineState.Blend = false;
            PipelineState.ScissorTest = false;
            PipelineState.FaceCulling = true;
            PipelineState.DepthTest = false;

            PipelineState.ClearColor = clearColor;
            GL.Clear(ClearBufferMask.ColorBufferBit);

            PipelineState.PolygonMode = PolygonMode.Fill;
            PipelineState.CurrentVAO = vao;
            PipelineState.CurrentShader = screenMaterial.Shader;

            screenMaterial.OffColor = pixelOffColor;
            screenMaterial.OnColor = pixelOnColor;
            screenMaterial.SetUniforms(globalUniforms);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            elapsedTime += deltaTime;
            globalUniforms.Time = (float)elapsedTime;
        }

        public override void Resize(int width, int height)
        {
            float windowAspectRatio = (float)width / height;
            float lcdAspectRatio = (float)PPU.RES_X / PPU.RES_Y;

            if (windowAspectRatio > lcdAspectRatio)
            {
                viewportWidth = (int)(height * lcdAspectRatio);
                viewportHeight = height;
                viewportX = (width - viewportWidth) >> 1;
                viewportY = 0;
            }
            else
            {
                viewportWidth = width;
                viewportHeight = (int)(width / lcdAspectRatio);
                viewportX = 0;
                viewportY = (height - viewportHeight) >> 1;
            }
        }

        public override void ScreenUpdate(byte[] pixels)
        {
            screenTexture.Update(pixels);
        }

        public override void SetColors()
        {

        }
    }
}