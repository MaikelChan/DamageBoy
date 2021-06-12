using OpenTK.Graphics.OpenGL;

namespace DamageBoy.Graphics
{
    class UnlitMaterial : Material
    {
        public Texture2D MainTexture { get; set; }

        public UnlitMaterial(BaseRenderer renderer) : base(renderer, vsSource, fsSource)
        {
            DefineUniform("uMainTexture", UniformTypes.Sampler2D);
        }

        public override void SetUniforms(GlobalUniforms globalUniforms)
        {
            SetUniform("uMainTexture", TextureTarget.Texture2D, 0, MainTexture);
        }

        const string vsSource = @"#version 330 core
out vec2 uv0;

void main()
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0, 1);
    uv0.x = (x + 1.0) * 0.5;
    uv0.y = (1.0 - y) * 0.5;
}";

        const string fsSource = @"#version 330 core
in vec2 uv0;

uniform sampler2D uMainTexture;

out vec4 fragColor;

void main()
{
    fragColor = texture(uMainTexture, uv0);
}";
    }
}