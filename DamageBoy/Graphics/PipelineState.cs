using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace DamageBoy.Graphics;

class PipelineState
{
    readonly BaseRenderer renderer;

    public PipelineState(BaseRenderer renderer)
    {
        this.renderer = renderer;

        // Default state that corresponds with OpenGL default state

        clearColor = new Color4(0f, 0f, 0f, 0f);
        clearDepth = 1d;
        depthTest = false;
        depthFunction = DepthFunction.Less;
        blend = false;
        blendEquation = BlendEquationMode.FuncAdd;
        blendSourceFactor = BlendingFactor.One;
        blendDestFactor = BlendingFactor.Zero;
        viewportX = 0;
        viewportY = 0;
        viewportWidth = 0;
        viewportHeight = 0;
        scissorTest = false;
        scissorX = 0;
        scissorY = 0;
        scissorWidth = 0;
        scissorHeight = 0;
        faceCulling = false;
        faceCullingMode = CullFaceMode.Back;
        polygonMode = PolygonMode.Fill;
        polygonOffsetLine = false;
        polygonOffsetLineFactor = 0f;
        polygonOffsetLineUnits = 0f;
        currentShader = null;
        currentVAO = 0;
        currentTextureUnit = 0;
        textureUnits = new uint[renderer.MaxTextureImageUnits];
        currentRenderTargetID = 0;
        currentRenderBuffer = 0;
    }

    Color4 clearColor;
    public Color4 ClearColor
    {
        get
        {
            return clearColor;
        }
        set
        {
            if (clearColor == value) return;
            clearColor = value;
            GL.ClearColor(value);
        }
    }

    double clearDepth;
    public double ClearDepth
    {
        get
        {
            return clearDepth;
        }
        set
        {
            if (clearDepth == value) return;
            clearDepth = value;
            GL.ClearDepth(value);
        }
    }

    bool depthTest;
    public bool DepthTest
    {
        get
        {
            return depthTest;
        }
        set
        {
            if (depthTest == value) return;
            depthTest = value;
            if (depthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
        }
    }

    DepthFunction depthFunction;
    public DepthFunction DepthFunction
    {
        get
        {
            return depthFunction;
        }
        set
        {
            if (depthFunction == value) return;
            depthFunction = value;
            GL.DepthFunc(value);
        }
    }

    bool blend;
    public bool Blend
    {
        get
        {
            return blend;
        }
        set
        {
            if (blend == value) return;
            blend = value;
            if (blend) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
    }

    BlendEquationMode blendEquation;
    public BlendEquationMode BlendEquation
    {
        get
        {
            return blendEquation;
        }
        set
        {
            if (blendEquation == value) return;
            blendEquation = value;
            GL.BlendEquation(blendEquation);
        }
    }

    BlendingFactor blendSourceFactor;
    BlendingFactor blendDestFactor;

    public void SetBlendFunc(BlendingFactor sourceFactor, BlendingFactor destFactor)
    {
        if (blendSourceFactor == sourceFactor && blendDestFactor == destFactor) return;
        blendSourceFactor = sourceFactor;
        blendDestFactor = destFactor;
        GL.BlendFunc(sourceFactor, destFactor);
    }

    int viewportX;
    int viewportY;
    int viewportWidth;
    int viewportHeight;
    public void SetViewport(int x, int y, int width, int height)
    {
        if (viewportX == x && viewportY == y && viewportWidth == width && viewportHeight == height) return;
        viewportX = x;
        viewportY = y;
        viewportWidth = width;
        viewportHeight = height;
        GL.Viewport(x, y, width, height);
    }

    bool scissorTest;
    public bool ScissorTest
    {
        get
        {
            return scissorTest;
        }
        set
        {
            if (scissorTest == value) return;
            scissorTest = value;
            if (scissorTest) GL.Enable(EnableCap.ScissorTest);
            else GL.Disable(EnableCap.ScissorTest);
        }
    }

    int scissorX;
    int scissorY;
    int scissorWidth;
    int scissorHeight;
    public void SetScissor(int x, int y, int width, int height)
    {
        if (scissorX == x && scissorY == y && scissorWidth == width && scissorHeight == height) return;
        scissorX = x;
        scissorY = y;
        scissorWidth = width;
        scissorHeight = height;
        GL.Scissor(x, y, width, height);
    }

    bool faceCulling;
    public bool FaceCulling
    {
        get
        {
            return faceCulling;
        }
        set
        {
            if (faceCulling == value) return;
            faceCulling = value;
            if (faceCulling) GL.Enable(EnableCap.CullFace);
            else GL.Disable(EnableCap.CullFace);
        }
    }

    CullFaceMode faceCullingMode;
    public CullFaceMode FaceCullingMode
    {
        get
        {
            return faceCullingMode;
        }
        set
        {
            if (faceCullingMode == value) return;
            faceCullingMode = value;
            GL.CullFace(value);
        }
    }

    PolygonMode polygonMode;
    public PolygonMode PolygonMode
    {
        get
        {
            return polygonMode;
        }
        set
        {
            if (polygonMode == value) return;
            polygonMode = value;
            GL.PolygonMode(MaterialFace.FrontAndBack, value);
        }
    }

    bool polygonOffsetLine;
    public bool PolygonOffsetLine
    {
        get
        {
            return polygonOffsetLine;
        }
        set
        {
            if (polygonOffsetLine == value) return;
            polygonOffsetLine = value;
            if (polygonOffsetLine) GL.Enable(EnableCap.PolygonOffsetLine);
            else GL.Disable(EnableCap.PolygonOffsetLine);
        }
    }

    float polygonOffsetLineFactor;
    float polygonOffsetLineUnits;
    public void SetPolygonOffsetLine(float factor, float units)
    {
        if (polygonOffsetLineFactor == factor && polygonOffsetLineUnits == units) return;
        polygonOffsetLineFactor = factor;
        polygonOffsetLineUnits = units;
        GL.PolygonOffset(factor, units);
    }

    Shader currentShader;
    public Shader CurrentShader
    {
        get
        {
            return currentShader;
        }
        set
        {
            if (currentShader == value) return;
            currentShader = value;
            GL.UseProgram(currentShader.Program);
        }
    }

    uint currentVAO;
    public uint CurrentVAO
    {
        get
        {
            return currentVAO;
        }
        set
        {
            if (currentVAO == value) return;
            currentVAO = value;
            GL.BindVertexArray(value);
        }
    }

    uint currentTextureUnit;
    public uint CurrentTextureUnit
    {
        get
        {
            return currentTextureUnit;
        }
        set
        {
            if (value >= renderer.MaxTextureImageUnits)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTextureUnit));
            }

            if (currentTextureUnit == value) return;
            currentTextureUnit = value;
            GL.ActiveTexture(TextureUnit.Texture0 + (int)value);
        }
    }

    readonly uint[] textureUnits;
    public void BindTexture(TextureTarget textureTarget, uint textureUnit, uint textureID)
    {
        if (textureUnit >= renderer.MaxTextureImageUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(textureUnit));
        }

        if (textureUnits[textureUnit] == textureID) return;
        textureUnits[textureUnit] = textureID;
        CurrentTextureUnit = textureUnit;
        GL.BindTexture(textureTarget, textureID);
    }

    uint currentRenderTargetID;
    public void BindRenderTarget(FramebufferTarget target, uint renderTargetID)
    {
        if (currentRenderTargetID == renderTargetID) return;
        currentRenderTargetID = renderTargetID;
        GL.BindFramebuffer(target, renderTargetID);
    }

    uint currentRenderBuffer;
    public uint CurrentRenderBuffer
    {
        get
        {
            return currentRenderBuffer;
        }
        set
        {
            if (currentRenderBuffer == value) return;
            currentRenderBuffer = value;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, value);
        }
    }
}