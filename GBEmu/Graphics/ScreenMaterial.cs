using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GBEmu.Graphics
{
    class ScreenMaterial : Material
    {
        public Texture2D MainTexture { get; set; }
        public Color4 OffColor { get; set; }
        public Color4 OnColor { get; set; }

        public ScreenMaterial(BaseRenderer renderer) : base(renderer, vsSource, fsSource)
        {
            DefineUniform("uMainTexture", UniformTypes.Sampler2D);
            DefineUniform("uOffColor", UniformTypes.Float3);
            DefineUniform("uOnColor", UniformTypes.Float3);
            DefineUniform("uTime", UniformTypes.Float1);
        }

        public override void SetUniforms(GlobalUniforms globalUniforms)
        {
            SetUniform("uMainTexture", TextureTarget.Texture2D, 0, MainTexture);
            SetUniform("uOffColor", new Vector3(OffColor.R, OffColor.G, OffColor.B));
            SetUniform("uOnColor", new Vector3(OnColor.R, OnColor.G, OnColor.B));
            SetUniform("uTime", globalUniforms.Time);
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
uniform vec3 uOffColor;
uniform vec3 uOnColor;
uniform float uTime;

out vec4 fragColor;

const float WIDTH = 160f;
const float HEIGHT = 144f;
const float PI = 3.1415926535897932384626433832795f;
const float DOTS_POWER = 1 / 3f;
const float FADE_SPEED = 0.2f;
const float FADE_SCALE = 1f / 2f;

void main()
{
    float pixels = texture(uMainTexture, uv0).r;

    //float fadeCoords = uv0.y; //floor(uv0.y * HEIGHT) / HEIGHT;
    //float fade = 1 - (fadeCoords * FADE_SCALE);
    //vec3 fadeColor = mix(uOffColor, uOnColor, 0.95);

    vec3 off = mix(uOffColor * 0.5, uOffColor * 1.2, uv0.y);
    //vec3 on = mix(uOnColor * 1.1, fadeColor, fract(fade + (uTime * FADE_SPEED)));
    vec3 on = uOnColor;

    vec3 color = mix(off, on, pixels);
    float grid = pow(cos((uv0.y * HEIGHT * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    grid *= pow(cos((uv0.x * WIDTH * PI * 2) + PI) * 0.5 + 0.5, DOTS_POWER);
    color *= grid * 0.2 + 0.8;

    fragColor = vec4(color, 1);
}";
    }
}