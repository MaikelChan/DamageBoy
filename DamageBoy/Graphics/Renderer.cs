using DamageBoy.Core;
using DamageBoy.Properties;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.IO;
using System.IO.Compression;

namespace DamageBoy.Graphics
{
    class Renderer : BaseRenderer
    {
        readonly Settings settings;

        readonly Color4 clearColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);

        readonly uint vao;
        readonly Texture2D screenTexture;
        readonly ScreenMaterial screenMaterial;
        readonly Texture2D logoTexture;
        readonly UnlitMaterial logoMaterial;

        int viewportX, viewportY, viewportWidth, viewportHeight;
        int logoViewportX, logoViewportY, logoViewportWidth, logoViewportHeight;
        double elapsedTime;

        public enum RenderModes { Logo, LCD }
        public RenderModes RenderMode { get; set; }

        const int LOGO_WIDTH = 1024;
        const int LOGO_HEIGHT = 1024;

        byte[] pixels;

        public Renderer(Settings settings) : base()
        {
            this.settings = settings;

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

            // Screen texture and material

            screenTexture = new Texture2D(this, Constants.RES_X, Constants.RES_Y, TextureFormats.R8, IntPtr.Zero, "Screen Texture");
            screenTexture.SetMinFilter(TextureMinFilter.Nearest);
            screenTexture.SetMagFilter(TextureMagFilter.Nearest);
            screenTexture.SetWrapS(TextureWrapMode.ClampToEdge);
            screenTexture.SetWrapT(TextureWrapMode.ClampToEdge);

            screenMaterial = new ScreenMaterial(this);
            screenMaterial.MainTexture = screenTexture;

            // Get pixel data from logo image from Resources

            byte[] compressedIcon = Resources.BigIcon;
            byte[] icon;

            using (MemoryStream compressedStream = new MemoryStream(compressedIcon))
            using (BrotliStream decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                decompressionStream.CopyTo(decompressedStream);
                icon = decompressedStream.ToArray();
            }

            // Logo texture and material

            unsafe
            {
                fixed (byte* p = icon)
                {
                    IntPtr logoPtr = (IntPtr)p;
                    logoTexture = new Texture2D(this, LOGO_WIDTH, LOGO_HEIGHT, TextureFormats.RGBA8888, logoPtr, "Logo Texture");
                }
            }

            logoTexture.SetMinFilter(TextureMinFilter.Linear);
            logoTexture.SetMagFilter(TextureMagFilter.Linear);
            logoTexture.SetWrapS(TextureWrapMode.ClampToEdge);
            logoTexture.SetWrapT(TextureWrapMode.ClampToEdge);

            logoMaterial = new UnlitMaterial(this);
            logoMaterial.MainTexture = logoTexture;

            RenderMode = RenderModes.Logo;
        }

        public override void Dispose()
        {
            base.Dispose();

            GL.DeleteVertexArray(vao);
            screenTexture.Dispose();
            logoTexture.Dispose();
            Shader.DisposeAll();
        }

        public override void Render(double deltaTime)
        {
            // Reset some states that can be modified by ImGui

            PipelineState.ScissorTest = false;
            PipelineState.FaceCulling = true;
            PipelineState.DepthTest = false;
            PipelineState.PolygonMode = PolygonMode.Fill;
            PipelineState.CurrentVAO = vao;

            // Clear framebuffer

            PipelineState.ClearColor = clearColor;
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Render screen or logo

            if (RenderMode == RenderModes.LCD)
            {
                PipelineState.Blend = false;
                PipelineState.SetViewport(viewportX, viewportY, viewportWidth, viewportHeight);

                PipelineState.CurrentShader = screenMaterial.Shader;

                if (pixels != null)
                {
                    screenTexture.Update(pixels);
                }

                screenMaterial.Color0 = settings.Data.GbColor0.ToColor4();
                screenMaterial.Color1 = settings.Data.GbColor1.ToColor4();
                screenMaterial.Color2 = settings.Data.GbColor2.ToColor4();
                screenMaterial.Color3 = settings.Data.GbColor3.ToColor4();
                screenMaterial.LcdEffect = settings.Data.LcdEffectVisibility;
                screenMaterial.SetUniforms(globalUniforms);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            }
            else
            {
                PipelineState.Blend = true;
                PipelineState.BlendEquation = BlendEquationMode.FuncAdd;
                PipelineState.SetBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                PipelineState.SetViewport(logoViewportX, logoViewportY, logoViewportWidth, logoViewportHeight);

                PipelineState.CurrentShader = logoMaterial.Shader;
                logoMaterial.SetUniforms(globalUniforms);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            }

            // Increase timer for animated shader effects (if any)

            elapsedTime += deltaTime;
            globalUniforms.Time = (float)elapsedTime;
        }

        public override void Resize(int width, int height)
        {
            float windowAspectRatio = (float)width / height;

            // LCD Viewport

            float lcdAspectRatio = (float)Constants.RES_X / Constants.RES_Y;

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

            globalUniforms.ViewportSize = new Vector2(viewportWidth, viewportHeight);

            // Logo Viewport

            float logoAspectRatio = (float)LOGO_WIDTH / LOGO_HEIGHT;
            const float logoSizeDivider = 3f;

            int lw = (int)(width / logoSizeDivider);
            int lh = (int)(height / logoSizeDivider);

            if (windowAspectRatio > logoAspectRatio)
            {
                logoViewportWidth = (int)(lh * logoAspectRatio);
                logoViewportHeight = lh;
            }
            else
            {
                logoViewportWidth = lw;
                logoViewportHeight = (int)(lw / logoAspectRatio);
            }

            logoViewportX = (width - logoViewportWidth) >> 1;
            logoViewportY = (height - logoViewportHeight) >> 1;
        }

        public override void ScreenUpdate(byte[] pixels)
        {
            this.pixels = pixels;
        }
    }
}