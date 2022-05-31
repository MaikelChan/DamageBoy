using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace DamageBoy.Graphics;

class ScreenMaterial : Material
{
    public Texture2D MainTexture { get; set; }
    public Color4 Color0 { get; set; }
    public Color4 Color1 { get; set; }
    public Color4 Color2 { get; set; }
    public Color4 Color3 { get; set; }
    public float LcdEffect { get; set; }

    public ScreenMaterial(BaseRenderer renderer) : base(renderer, vsSource, fsSource)
    {
        DefineUniform("uViewportSize", UniformTypes.Float2);
        DefineUniform("uMainTexture", UniformTypes.Sampler2D);
        DefineUniform("uColor0", UniformTypes.Float3);
        DefineUniform("uColor1", UniformTypes.Float3);
        DefineUniform("uColor2", UniformTypes.Float3);
        DefineUniform("uColor3", UniformTypes.Float3);
        DefineUniform("uLcdEffect", UniformTypes.Float1);
    }

    public override void SetUniforms(GlobalUniforms globalUniforms)
    {
        SetUniform("uViewportSize", globalUniforms.ViewportSize);
        SetUniform("uMainTexture", TextureTarget.Texture2D, 0, MainTexture);
        SetUniform("uColor0", new Vector3(Color0.R, Color0.G, Color0.B));
        SetUniform("uColor1", new Vector3(Color1.R, Color1.G, Color1.B));
        SetUniform("uColor2", new Vector3(Color2.R, Color2.G, Color2.B));
        SetUniform("uColor3", new Vector3(Color3.R, Color3.G, Color3.B));
        SetUniform("uLcdEffect", LcdEffect);
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
uniform vec3 uColor0;
uniform vec3 uColor1;
uniform vec3 uColor2;
uniform vec3 uColor3;
uniform float uLcdEffect;

out vec4 fragColor;

const float WIDTH = 160.0;
const float HEIGHT = 144.0;
const float PI = 3.1415926535897932384626433832795;
const float DOTS_POWER = 1.0 / 3.0;
const float GRID_VISIBILITY_MIN_HEIGHT = 400.0;
const float GRID_FADE_MAX_HEIGHT = 800.0;

float Remap(float value, float low1, float high1, float low2, float high2)
{
    return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
}

void main()
{
    float pixels = texture(uMainTexture, uv0).r;

    //vec3 off = mix(uColor0 * 0.5, uColor0 * 1.2, uv0.y);
    //vec3 on = uColor3;
    //vec3 color = mix(off, on, pixels);

    vec3 color;
    if (pixels < 0.25) color = uColor0;
    else if (pixels >= 0.25 && pixels < 0.5) color = uColor1;
    else if (pixels >= 0.5 && pixels < 0.75) color = uColor2;
    else color = uColor3;

    float grid = pow(cos((uv0.y * HEIGHT * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    grid *= pow(cos((uv0.x * WIDTH * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    grid = grid * 0.2 + 0.8;
    float gridVisibility = clamp(Remap(uViewportSize.y, GRID_VISIBILITY_MIN_HEIGHT, GRID_FADE_MAX_HEIGHT, 0.0, 1.0), 0.0, 1.0);

    color *= mix(1, mix(1, grid, gridVisibility), uLcdEffect);

    fragColor = vec4(color, 1);
}";
}