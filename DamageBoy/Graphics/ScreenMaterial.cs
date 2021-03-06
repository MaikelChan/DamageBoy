using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace DamageBoy.Graphics
{
    class ScreenMaterial : Material
    {
        public Texture2D MainTexture { get; set; }
        public Color4 OffColor { get; set; }
        public Color4 OnColor { get; set; }

        public ScreenMaterial(BaseRenderer renderer) : base(renderer, vsSource, fsSource)
        {
            DefineUniform("uViewportSize", UniformTypes.Float2);
            DefineUniform("uMainTexture", UniformTypes.Sampler2D);
            DefineUniform("uOffColor", UniformTypes.Float3);
            DefineUniform("uOnColor", UniformTypes.Float3);
        }

        public override void SetUniforms(GlobalUniforms globalUniforms)
        {
            SetUniform("uViewportSize", globalUniforms.ViewportSize);
            SetUniform("uMainTexture", TextureTarget.Texture2D, 0, MainTexture);
            SetUniform("uOffColor", new Vector3(OffColor.R, OffColor.G, OffColor.B));
            SetUniform("uOnColor", new Vector3(OnColor.R, OnColor.G, OnColor.B));
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

uniform vec2 uViewportSize;
uniform sampler2D uMainTexture;
uniform vec3 uOffColor;
uniform vec3 uOnColor;

out vec4 fragColor;

const float WIDTH = 160f;
const float HEIGHT = 144f;
const float PI = 3.1415926535897932384626433832795f;
const float DOTS_POWER = 1 / 3f;
const float GRID_VISIBILITY_MIN_HEIGHT = 400.0;
const float GRID_FADE_MAX_HEIGHT = 800.0;

float Remap(float value, float low1, float high1, float low2, float high2)
{
    return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
}

void main()
{
    float pixels = texture(uMainTexture, uv0).r;

    vec3 off = mix(uOffColor * 0.5, uOffColor * 1.2, uv0.y);
    vec3 on = uOnColor;

    vec3 color = mix(off, on, pixels);
    float grid = pow(cos((uv0.y * HEIGHT * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    grid *= pow(cos((uv0.x * WIDTH * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    grid = grid * 0.2 + 0.8;
    float gridVisibility = clamp(Remap(uViewportSize.y, GRID_VISIBILITY_MIN_HEIGHT, GRID_FADE_MAX_HEIGHT, 0.0, 1.0), 0.0, 1.0);
    color *= mix(1, grid, gridVisibility);

    fragColor = vec4(color, 1);
}";
    }
}