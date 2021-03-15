using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace GBEmu.Graphics
{
    class Shader : IDisposable
    {
        internal int Program { get; }

        internal Dictionary<string, UniformData> Uniforms { get; }

        private Shader(BaseRenderer renderer, string vsSource, string fsSource, string debugName)
        {
            int vertexShader = CreateShader(ShaderType.VertexShader, vsSource);
            int fragmentShader = CreateShader(ShaderType.FragmentShader, fsSource);

#if DEBUG
            if (renderer.KHRDebugExtension && !string.IsNullOrEmpty(debugName))
            {
                string vsName = $"{debugName} Vertex Shader";
                GL.ObjectLabel(ObjectLabelIdentifier.Shader, vertexShader, vsName.Length, vsName);

                string fsName = $"{debugName} Fragment Shader";
                GL.ObjectLabel(ObjectLabelIdentifier.Shader, fragmentShader, fsName.Length, fsName);
            }
#endif

            Program = CreateShaderProgram(vertexShader, fragmentShader);
            Uniforms = new Dictionary<string, UniformData>();

#if DEBUG
            if (renderer.KHRDebugExtension && !string.IsNullOrEmpty(debugName))
            {
                string programName = $"{debugName} Shader Program";
                GL.ObjectLabel(ObjectLabelIdentifier.Program, Program, programName.Length, programName);
            }
#endif
        }

        public void Dispose()
        {
            GL.DeleteProgram(Program);
        }

        #region Shader creation

        private static int CreateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);

            if (shader == 0)
            {
                throw new Exception($"Unable to create {type} shader.");
            }

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            string infoLog = GL.GetShaderInfoLog(shader);
            if (!string.IsNullOrEmpty(infoLog)) throw new Exception($"Error compiling {type}: {infoLog}");

            return shader;
        }

        private static int CreateShaderProgram(int vertexShader, int fragmentShader)
        {
            int shaderProgram = GL.CreateProgram();

            if (shaderProgram == 0)
            {
                throw new Exception("Unable to create shader program.");
            }

            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            GL.LinkProgram(shaderProgram);

            string infoLog = GL.GetProgramInfoLog(shaderProgram);
            if (!string.IsNullOrEmpty(infoLog)) throw new Exception($"Error linking shader program: {infoLog}");

            GL.DetachShader(shaderProgram, vertexShader);
            GL.DetachShader(shaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        #endregion

        #region Shader Manager

        private static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        internal static Shader Get(BaseRenderer renderer, string name, string vsSource, string psSource, string debugName)
        {
            string key = name;

            bool found = shaders.TryGetValue(key, out Shader shader);

            if (!found)
            {
                shader = new Shader(renderer, vsSource, psSource, debugName);
                shaders.Add(key, shader);
            }

            return shader;
        }

        internal static void DisposeAll()
        {
            foreach (KeyValuePair<string, Shader> keyValue in shaders)
            {
                keyValue.Value.Dispose();
            }

            shaders.Clear();
        }

        #endregion
    }
}