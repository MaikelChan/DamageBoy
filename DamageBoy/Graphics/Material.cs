using DamageBoy.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace DamageBoy.Graphics;

abstract class Material
{
    internal Shader Shader { get; }

    readonly BaseRenderer renderer;

    protected Material(BaseRenderer renderer, string vsSource, string fsSource)
    {
        this.renderer = renderer;

        Shader = Shader.Get(renderer, GetType().Name, vsSource, fsSource, GetType().Name);
    }

    #region Uniforms

    public abstract void SetUniforms(GlobalUniforms globalUniforms);

    protected void DefineUniform(string name, UniformTypes type)
    {
        if (Shader.Uniforms.ContainsKey(name)) return;

        int location = GL.GetUniformLocation(Shader.Program, name);

        if (location < 0)
        {
            Utils.Log(LogType.Warning, $"Uniform \"{name}\" not found in material \"{GetType().Name}\".");
            return;
        }

        Shader.Uniforms.Add(name, new UniformData(renderer, location, type));
    }

    // Uniforms are stored in shader program, so cache them in Shader to avoid unnecessary GL calls.
    // The following SetUniform functions must always be called after glUseProgram.

    protected void SetUniform(string uniformName, int value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, float value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, Vector2 value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, Vector3 value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, Vector4 value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, Matrix3 value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, Matrix4 value)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(value);
    }

    protected void SetUniform(string uniformName, TextureTarget textureTarget, int textureUnit, Texture2D texture)
    {
        bool found = Shader.Uniforms.TryGetValue(uniformName, out UniformData uniformData);
        if (!found) return;

        uniformData.SetValue(textureTarget, textureUnit, texture);
    }

    #endregion
}