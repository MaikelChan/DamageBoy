using OpenTK.Graphics.OpenGL;
using System;

namespace DamageBoy.Graphics;

public enum TextureFormats { RGBA8888, RGB555, R8 }

class Texture2D : Texture
{
    readonly TextureFormats textureFormat;

    public Texture2D(BaseRenderer renderer, int width, int height, TextureFormats textureFormat, IntPtr data, string debugName) : base(renderer, width, height)
    {
        this.textureFormat = textureFormat;

        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);

        switch (textureFormat)
        {
            case TextureFormats.RGBA8888:
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                break;
            case TextureFormats.RGB555:
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb5A1, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedShort5551, data);
                break;
            case TextureFormats.R8:
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, width, height, 0, PixelFormat.Red, PixelType.UnsignedByte, data);
                break;
            default:
                throw new NotImplementedException($"Texture format {textureFormat} is not implemented.");
        }

        //if (IsPowerOf2(image.Width) && IsPowerOf2(image.Height))
        //{
        //    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        //}
        //else
        //{
        SetWrapS(TextureWrapMode.Repeat);
        SetWrapT(TextureWrapMode.Repeat);
        SetMinFilter(TextureMinFilter.Linear);
        //}

#if DEBUG
        if (renderer.KHRDebugExtension && !string.IsNullOrEmpty(debugName))
        {
            string name = $"{debugName} Texture 2D";
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, TextureID, name.Length, name);
        }
#endif
    }

    public void Update(ushort[] pixelData)
    {
        if (pixelData == null)
        {
            throw new ArgumentNullException(nameof(pixelData));
        }

        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);

        switch (textureFormat)
        {
            case TextureFormats.RGBA8888:
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
                break;
            case TextureFormats.RGB555:
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedShort5551, pixelData);
                break;
            case TextureFormats.R8:
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Red, PixelType.UnsignedByte, pixelData);
                break;
            default:
                throw new NotImplementedException($"Texture format {textureFormat} is not implemented.");
        }
    }

    public void SetMinFilter(TextureMinFilter filter)
    {
        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
    }

    public void SetMagFilter(TextureMagFilter filter)
    {
        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
    }

    public void SetWrapS(TextureWrapMode mode)
    {
        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)mode);
    }

    public void SetWrapT(TextureWrapMode mode)
    {
        renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, TextureID);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)mode);
    }
}