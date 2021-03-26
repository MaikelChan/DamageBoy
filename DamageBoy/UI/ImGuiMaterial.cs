using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using DamageBoy.Graphics;

namespace DamageBoy.UI
{
    class ImGuiMaterial : Material
    {
        public Texture2D FontTexture { get; set; }
        public Matrix4 ProjectionMatrix { get; set; }

        public ImGuiMaterial(BaseRenderer renderer) : base(renderer, vsSource, fsSource)
        {
            DefineUniform("uProjectionMatrix", UniformTypes.Matrix4);
            DefineUniform("uFontTexture", UniformTypes.Sampler2D);
        }

        public override void SetUniforms(GlobalUniforms globalUniforms)
        {
            SetUniform("uProjectionMatrix", ProjectionMatrix);
            SetUniform("uFontTexture", TextureTarget.Texture2D, 0, FontTexture);
        }

        const string vsSource = @"#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aUV0;
layout(location = 2) in vec4 aColor;
uniform mat4 uProjectionMatrix;
out vec4 color;
out vec2 texCoord;
void main()
{
    gl_Position = uProjectionMatrix * vec4(aPosition, 0, 1);
    color = aColor;
    texCoord = aUV0;
}";

        const string fsSource = @"#version 330 core
uniform sampler2D uFontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
    outputColor = color * texture(uFontTexture, texCoord);
}";
    }
}