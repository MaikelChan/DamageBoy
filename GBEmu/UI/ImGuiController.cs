using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using GBEmu.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GBEmu.UI
{
    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
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

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
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

            io.MouseDown[0] = data.LeftMouseButtonDown;
            io.MouseDown[1] = data.RightMouseButtonDown;
            io.MouseDown[2] = data.MiddleMouseButtonDown;

            io.MousePos = new System.Numerics.Vector2(data.MousePosition.X, data.MousePosition.Y);

            io.KeysDown[(int)ImGuiKey.Tab] = data.KeyTab;
            io.KeysDown[(int)ImGuiKey.LeftArrow] = data.KeyLeftArrow;
            io.KeysDown[(int)ImGuiKey.RightArrow] = data.KeyRightArrow;
            io.KeysDown[(int)ImGuiKey.UpArrow] = data.KeyUpArrow;
            io.KeysDown[(int)ImGuiKey.DownArrow] = data.KeyDownArrow;
            io.KeysDown[(int)ImGuiKey.PageUp] = data.KeyPageUp;
            io.KeysDown[(int)ImGuiKey.PageDown] = data.KeyPageDown;
            io.KeysDown[(int)ImGuiKey.Home] = data.KeyHome;
            io.KeysDown[(int)ImGuiKey.End] = data.KeyEnd;
            io.KeysDown[(int)ImGuiKey.Insert] = data.KeyInsert;
            io.KeysDown[(int)ImGuiKey.Delete] = data.KeyDelete;
            io.KeysDown[(int)ImGuiKey.Backspace] = data.KeyBackspace;
            io.KeysDown[(int)ImGuiKey.Space] = data.KeySpace;
            io.KeysDown[(int)ImGuiKey.Enter] = data.KeyEnter;
            io.KeysDown[(int)ImGuiKey.Escape] = data.KeyEscape;
            io.KeysDown[(int)ImGuiKey.KeyPadEnter] = data.KeyKeyPadEnter;
            io.KeysDown[(int)ImGuiKey.A] = data.KeyA;
            io.KeysDown[(int)ImGuiKey.C] = data.KeyC;
            io.KeysDown[(int)ImGuiKey.V] = data.KeyV;
            io.KeysDown[(int)ImGuiKey.X] = data.KeyX;
            io.KeysDown[(int)ImGuiKey.Y] = data.KeyY;
            io.KeysDown[(int)ImGuiKey.Z] = data.KeyZ;

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

            io.MouseWheel = offset.Y;
            io.MouseWheelH = offset.X;
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)ImGuiKey.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)ImGuiKey.LeftArrow;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)ImGuiKey.RightArrow;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)ImGuiKey.UpArrow;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)ImGuiKey.DownArrow;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)ImGuiKey.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)ImGuiKey.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)ImGuiKey.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)ImGuiKey.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)ImGuiKey.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)ImGuiKey.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)ImGuiKey.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)ImGuiKey.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)ImGuiKey.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)ImGuiKey.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)ImGuiKey.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)ImGuiKey.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)ImGuiKey.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)ImGuiKey.Z;
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            renderer.PipelineState.CurrentVAO = _vertexArray;

            uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, totalVBSize);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                _vertexBufferSize = newSize;

                Utils.Log(LogType.Info, $"ImGui: Resized vertex buffer to new size {_vertexBufferSize}");
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, totalIBSize);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                _indexBufferSize = newSize;

                Utils.Log(LogType.Info, $"ImGui: Resized index buffer to new size {_indexBufferSize}");
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(vertexOffsetInVertices * Unsafe.SizeOf<ImDrawVert>()), cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);

                CheckGLError($"Data Vert {i}");

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexOffsetInElements * sizeof(ushort)), cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);

                CheckGLError($"Data Idx {i}");

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
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

            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            renderer.PipelineState.SetViewport(0, 0, _windowWidth, _windowHeight);
            renderer.PipelineState.Blend = true;
            renderer.PipelineState.BlendEquation = BlendEquationMode.FuncAdd;
            renderer.PipelineState.SetBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            renderer.PipelineState.ScissorTest = true;
            renderer.PipelineState.FaceCulling = false;
            renderer.PipelineState.DepthTest = false;
            renderer.PipelineState.PolygonMode = PolygonMode.Fill;

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
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

                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof(ushort)), vtx_offset);
                        CheckGLError("Draw");
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
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
}