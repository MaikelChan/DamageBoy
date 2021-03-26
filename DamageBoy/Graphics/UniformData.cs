using DamageBoy.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace DamageBoy.Graphics
{
    public enum UniformTypes
    {
        Int1,
        Float1,
        Float2,
        Float3,
        Float4,
        Matrix3,
        Matrix4,
        Sampler2D
    }

    class UniformData
    {
        public int Location { get; }

        readonly BaseRenderer renderer;
        readonly UniformTypes type;

        int intValue;
        Vector4 vectorValue;
        Matrix4 matrixValue;

        public UniformData(BaseRenderer renderer, int location, UniformTypes type)
        {
            this.renderer = renderer;
            Location = location;
            this.type = type;

            intValue = 0;
            vectorValue = Vector4.Zero;
            matrixValue = Matrix4.Zero;
        }

        public void SetValue(int value)
        {
            if (type != UniformTypes.Int1)
            {
                Utils.Log(LogType.Warning, $"Trying to set an int in a uniform of type {type}.");
                return;
            }

            if (intValue == value) return;
            intValue = value;

            GL.Uniform1(Location, value);
        }

        public void SetValue(float value)
        {
            if (type != UniformTypes.Float1)
            {
                Utils.Log(LogType.Warning, $"Trying to set a float in a uniform of type {type}.");
                return;
            }

            Vector4 vec4 = new Vector4(value);
            if (vectorValue == vec4) return;
            vectorValue = vec4;

            GL.Uniform1(Location, value);
        }

        public void SetValue(Vector2 value)
        {
            if (type != UniformTypes.Float2)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Vector2 in a uniform of type {type}.");
                return;
            }

            Vector4 vec4 = new Vector4(value);
            if (vectorValue == vec4) return;
            vectorValue = vec4;

            GL.Uniform2(Location, value);
        }

        public void SetValue(Vector3 value)
        {
            if (type != UniformTypes.Float3)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Vector3 in a uniform of type {type}.");
                return;
            }

            Vector4 vec4 = new Vector4(value);
            if (vectorValue == vec4) return;
            vectorValue = vec4;

            GL.Uniform3(Location, value);
        }

        public void SetValue(Vector4 value)
        {
            if (type != UniformTypes.Float4)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Vector4 in a uniform of type {type}.");
                return;
            }

            if (vectorValue == value) return;
            vectorValue = value;

            GL.Uniform4(Location, value);
        }

        public void SetValue(Matrix3 value)
        {
            if (type != UniformTypes.Matrix3)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Matrix3 in a uniform of type {type}.");
                return;
            }

            Matrix4 mat4 = new Matrix4(value);
            if (matrixValue == mat4) return;
            matrixValue = mat4;

            GL.UniformMatrix3(Location, false, ref value);
        }

        public void SetValue(Matrix4 value)
        {
            if (type != UniformTypes.Matrix4)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Matrix4 in a uniform of type {type}.");
                return;
            }

            if (matrixValue == value) return;
            matrixValue = value;

            GL.UniformMatrix4(Location, false, ref value);
        }

        public void SetValue(TextureTarget textureTarget, int textureUnit, Texture2D texture)
        {
            if (type != UniformTypes.Sampler2D)
            {
                Utils.Log(LogType.Warning, $"Trying to set a Sampler2D in a uniform of type {type}.");
                return;
            }

            if (intValue != textureUnit)
            {
                intValue = textureUnit;
                GL.Uniform1(Location, textureUnit);
            }

            renderer.PipelineState.BindTexture(textureTarget, (uint)textureUnit, texture.TextureID);
        }
    }
}
