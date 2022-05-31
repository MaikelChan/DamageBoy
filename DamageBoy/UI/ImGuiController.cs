using DamageBoy.Core;
using DamageBoy.Graphics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DamageBoy.UI;

// https://github.com/NogginBops/ImGui.NET_OpenTK_Sample/blob/opentk4.0/Dear%20ImGui%20Sample/ImGuiController.cs

internal class ImGuiController : IDisposable
{
    private bool _frameBegun;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private int _vertexBufferSize;
    private uint _indexBuffer;
    private int _indexBufferSize;

    private Texture2D _fontTexture;
    private ImGuiMaterial _material;

    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

    readonly BaseRenderer renderer;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(BaseRenderer renderer, int width, int height)
    {
        this.renderer = renderer;

        _windowWidth = width;
        _windowHeight = height;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    void CreateDeviceResources()
    {
        // Create VAO

        GL.GenVertexArrays(1, out _vertexArray);

        if (_vertexArray == 0)
        {
            throw new Exception("Error when creating ImGui VAO.");
        }

        // Create vertex buffer

        GL.GenBuffers(1, out _vertexBuffer);

        if (_vertexBuffer == 0)
        {
            throw new Exception("Error when creating ImGui vertex buffer.");
        }

        _vertexBufferSize = 10000;

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        // Create index buffer

        GL.GenBuffers(1, out _indexBuffer);

        if (_indexBuffer == 0)
        {
            throw new Exception("Error when creating ImGui index buffer.");
        }

        _indexBufferSize = 2000;

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        // Font texture and material

        RecreateFontDeviceTexture();
        _material = new ImGuiMaterial(renderer);

        // Setup VAO

        renderer.PipelineState.CurrentVAO = _vertexArray;

        int stride = Unsafe.SizeOf<ImDrawVert>();

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        // Set debug names

#if DEBUG
        if (renderer.KHRDebugExtension)
        {
            string vaoDebugName = "ImGui VAO";
            string vbDebugName = "ImGui Vertex Buffer";
            string ibDebugName = "ImGui Index Buffer";
            GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, _vertexArray, vaoDebugName.Length, vaoDebugName);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _vertexBuffer, vbDebugName.Length, vbDebugName);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _indexBuffer, ibDebugName.Length, ibDebugName);
        }
#endif
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        _fontTexture = new Texture2D(renderer, width, height, TextureFormats.RGBA8888, pixels, "ImGui Text Atlas");
        _fontTexture.SetMagFilter(TextureMagFilter.Linear);
        _fontTexture.SetMinFilter(TextureMinFilter.Linear);

        io.Fonts.SetTexID((IntPtr)_fontTexture.TextureID);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());

            CheckGLError("Imgui Controller");
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(ImGuiInputData data, float deltaSeconds)
    {
        if (_frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(data);

        _frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth / _scaleFactor.X, _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    readonly List<char> PressedChars = new List<char>();

    private void UpdateImGuiInput(ImGuiInputData data)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.AddMouseButtonEvent(0, data.LeftMouseButtonDown);
        io.AddMouseButtonEvent(1, data.RightMouseButtonDown);
        io.AddMouseButtonEvent(2, data.MiddleMouseButtonDown);

        io.AddMousePosEvent(data.MousePosition.X, data.MousePosition.Y);

        io.AddKeyEvent(ImGuiKey.Tab, data.KeyTab);
        io.AddKeyEvent(ImGuiKey.LeftArrow, data.KeyLeftArrow);
        io.AddKeyEvent(ImGuiKey.RightArrow, data.KeyRightArrow);
        io.AddKeyEvent(ImGuiKey.UpArrow, data.KeyUpArrow);
        io.AddKeyEvent(ImGuiKey.DownArrow, data.KeyDownArrow);
        io.AddKeyEvent(ImGuiKey.PageUp, data.KeyPageUp);
        io.AddKeyEvent(ImGuiKey.PageDown, data.KeyPageDown);
        io.AddKeyEvent(ImGuiKey.Home, data.KeyHome);
        io.AddKeyEvent(ImGuiKey.End, data.KeyEnd);
        io.AddKeyEvent(ImGuiKey.Insert, data.KeyInsert);
        io.AddKeyEvent(ImGuiKey.Delete, data.KeyDelete);
        io.AddKeyEvent(ImGuiKey.Backspace, data.KeyBackspace);
        io.AddKeyEvent(ImGuiKey.Space, data.KeySpace);
        io.AddKeyEvent(ImGuiKey.Enter, data.KeyEnter);
        io.AddKeyEvent(ImGuiKey.Escape, data.KeyEscape);
        io.AddKeyEvent(ImGuiKey.KeypadEnter, data.KeyKeyPadEnter);
        io.AddKeyEvent(ImGuiKey.A, data.KeyA);
        io.AddKeyEvent(ImGuiKey.C, data.KeyC);
        io.AddKeyEvent(ImGuiKey.V, data.KeyV);
        io.AddKeyEvent(ImGuiKey.X, data.KeyX);
        io.AddKeyEvent(ImGuiKey.Y, data.KeyY);
        io.AddKeyEvent(ImGuiKey.Z, data.KeyZ);

        foreach (var c in PressedChars)
        {
            io.AddInputCharacter(c);
        }

        PressedChars.Clear();

        io.KeyCtrl = data.KeyCtrl;
        io.KeyAlt = data.KeyAlt;
        io.KeyShift = data.KeyShift;
        io.KeySuper = data.KeySuper;
    }

    internal void PressChar(char keyChar)
    {
        PressedChars.Add(keyChar);
    }

    internal void MouseScroll(Vector2 offset)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.AddMouseWheelEvent(offset.X, offset.Y);
    }

    private static void SetKeyMappings()
    {
        //ImGuiIOPtr io = ImGui.GetIO();
        //io.KeyMap[(int)ImGuiKey.Tab] = (int)ImGuiKey.Tab;
        //io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)ImGuiKey.LeftArrow;
        //io.KeyMap[(int)ImGuiKey.RightArrow] = (int)ImGuiKey.RightArrow;
        //io.KeyMap[(int)ImGuiKey.UpArrow] = (int)ImGuiKey.UpArrow;
        //io.KeyMap[(int)ImGuiKey.DownArrow] = (int)ImGuiKey.DownArrow;
        //io.KeyMap[(int)ImGuiKey.PageUp] = (int)ImGuiKey.PageUp;
        //io.KeyMap[(int)ImGuiKey.PageDown] = (int)ImGuiKey.PageDown;
        //io.KeyMap[(int)ImGuiKey.Home] = (int)ImGuiKey.Home;
        //io.KeyMap[(int)ImGuiKey.End] = (int)ImGuiKey.End;
        //io.KeyMap[(int)ImGuiKey.Delete] = (int)ImGuiKey.Delete;
        //io.KeyMap[(int)ImGuiKey.Backspace] = (int)ImGuiKey.Backspace;
        //io.KeyMap[(int)ImGuiKey.Enter] = (int)ImGuiKey.Enter;
        //io.KeyMap[(int)ImGuiKey.Escape] = (int)ImGuiKey.Escape;
        //io.KeyMap[(int)ImGuiKey.Space] = (int)ImGuiKey.Space;
        //io.KeyMap[(int)ImGuiKey.A] = (int)ImGuiKey.A;
        //io.KeyMap[(int)ImGuiKey.C] = (int)ImGuiKey.C;
        //io.KeyMap[(int)ImGuiKey.V] = (int)ImGuiKey.V;
        //io.KeyMap[(int)ImGuiKey.X] = (int)ImGuiKey.X;
        //io.KeyMap[(int)ImGuiKey.Y] = (int)ImGuiKey.Y;
        //io.KeyMap[(int)ImGuiKey.Z] = (int)ImGuiKey.Z;
    }

    private void RenderImDrawData(ImDrawDataPtr draw_data)
    {
        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newSize;

                //Console.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
            }

            int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _indexBufferSize = newSize;

                //Console.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
            }
        }

        //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        _material.ProjectionMatrix = mvp;
        _material.FontTexture = _fontTexture;
        renderer.PipelineState.CurrentShader = _material.Shader;
        _material.SetUniforms(null);

        renderer.PipelineState.CurrentVAO = _vertexArray;

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        renderer.PipelineState.Blend = true;
        renderer.PipelineState.BlendEquation = BlendEquationMode.FuncAdd;
        renderer.PipelineState.SetBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        renderer.PipelineState.ScissorTest = true;
        renderer.PipelineState.FaceCulling = false;
        renderer.PipelineState.DepthTest = false;

        // Reset some states that can be changed by the emulator 

        renderer.PipelineState.SetViewport(0, 0, _windowWidth, _windowHeight);
        renderer.PipelineState.PolygonMode = PolygonMode.Fill;

        // Render command lists

        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
            CheckGLError($"Data Vert {n}");

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
            CheckGLError($"Data Idx {n}");

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    renderer.PipelineState.BindTexture(TextureTarget.Texture2D, 0, (uint)pcmd.TextureId);

                    CheckGLError("Texture");

                    // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                    var clip = pcmd.ClipRect;
                    renderer.PipelineState.SetScissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                    CheckGLError("Scissor");

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                    {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                    }
                    else
                    {
                        GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }

                    CheckGLError("Draw");
                }
            }
        }
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        _fontTexture.Dispose();
        //_shader.Dispose(); // Shader is in _material and disposed automatically
    }

    [Conditional("DEBUG")]
    static void CheckGLError(string title)
    {
        var error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            Utils.Log(LogType.Error, $"{title}: {error}");
        }
    }
}