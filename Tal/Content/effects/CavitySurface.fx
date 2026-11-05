// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Shader dla powierzchni cavity (Marching Cubes) — per-pixel lighting,
// dwustronne oświetlenie, alpha blending bez Z-write.
// Wzorowany na HirshfeldSurface.fx.

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// ═══════════════════════════════════════════════════════════════════
//  MACIERZE
// ═══════════════════════════════════════════════════════════════════
float4x4 World;
float4x4 View;
float4x4 Projection;

// ═══════════════════════════════════════════════════════════════════
//  KAMERA
// ═══════════════════════════════════════════════════════════════════
float3 CameraPosition;

// ═══════════════════════════════════════════════════════════════════
//  ŚWIATŁO 1
// ═══════════════════════════════════════════════════════════════════
float3 LightDirection;
float3 LightDiffuseColor;
float3 LightSpecularColor;
float  Light1Enabled;

// ═══════════════════════════════════════════════════════════════════
//  ŚWIATŁO 2
// ═══════════════════════════════════════════════════════════════════
float3 Light2Direction;
float3 Light2DiffuseColor;
float3 Light2SpecularColor;
float  Light2Enabled;

// ═══════════════════════════════════════════════════════════════════
//  MATERIAŁ
// ═══════════════════════════════════════════════════════════════════
float3 AmbientColor;
float3 SpecularColor;
float  SpecularPower;
float  Alpha;

// ═══════════════════════════════════════════════════════════════════
//  WEJŚCIE / WYJŚCIE
// ═══════════════════════════════════════════════════════════════════
struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float4 Color    : COLOR0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 Normal   : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float4 Color    : COLOR0;
};

// ═══════════════════════════════════════════════════════════════════
//  VERTEX SHADER
// ═══════════════════════════════════════════════════════════════════
PSInput MainVS(VSInput input)
{
    PSInput output;

    float4 worldPos = mul(input.Position, World);
    output.WorldPos = worldPos.xyz;
    output.Position = mul(worldPos, View);
    output.Position = mul(output.Position, Projection);

    output.Normal = normalize(mul(input.Normal, (float3x3)World));
    output.Color  = input.Color;

    return output;
}

// ═══════════════════════════════════════════════════════════════════
//  PIXEL SHADER
// ═══════════════════════════════════════════════════════════════════
float4 MainPS(PSInput input) : SV_TARGET
{
    float3 N = normalize(input.Normal);
    float3 V = normalize(CameraPosition - input.WorldPos);

    // ═══ DWUSTRONNE OŚWIETLENIE ═══
    if (dot(N, V) < 0.0)
        N = -N;

    // ═══ AMBIENT ═══
    float3 ambient = AmbientColor * input.Color.rgb;

    // ═══ ŚWIATŁO 1 ═══
    float3 light1Diffuse = 0.0;
    float3 light1Spec    = 0.0;

    if (Light1Enabled > 0.5)
    {
        float3 L1 = normalize(-LightDirection);
        float  ndl1 = saturate(dot(N, L1));
        light1Diffuse = LightDiffuseColor * ndl1 * input.Color.rgb;

        float3 H1 = normalize(L1 + V);
        float  ndh1 = saturate(dot(N, H1));
        light1Spec = LightSpecularColor * pow(ndh1, SpecularPower) * SpecularColor;
    }

    // ═══ ŚWIATŁO 2 ═══
    float3 light2Diffuse = 0.0;
    float3 light2Spec    = 0.0;

    if (Light2Enabled > 0.5)
    {
        float3 L2 = normalize(-Light2Direction);
        float  ndl2 = saturate(dot(N, L2));
        light2Diffuse = Light2DiffuseColor * ndl2 * input.Color.rgb;

        float3 H2 = normalize(L2 + V);
        float  ndh2 = saturate(dot(N, H2));
        light2Spec = Light2SpecularColor * pow(ndh2, SpecularPower) * SpecularColor;
    }

    // ═══ KOŃCOWY KOLOR ═══
    float3 finalColor = ambient
                      + light1Diffuse + light1Spec
                      + light2Diffuse + light2Spec;

    // ⭐ Alpha: jeśli shader ma globalną Alpha < 1, użyj jej;
    //    inaczej użyj alpha z wierzchołka (per-vertex colors)
    float finalAlpha = input.Color.a * Alpha;

    return float4(finalColor, finalAlpha);
}

// ═══════════════════════════════════════════════════════════════════
//  TECHNIKA
// ═══════════════════════════════════════════════════════════════════
technique CavitySurface
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();

        AlphaBlendEnable = TRUE;
        SrcBlend  = SRCALPHA;
        DestBlend = INVSRCALPHA;
        BlendOp   = ADD;

        ZEnable      = TRUE;
        ZWriteEnable = FALSE;
        ZFunc        = LESSEQUAL;

        CullMode = NONE;
    }
}