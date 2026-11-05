// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Shader dla powierzchni Hirshfelda — per-pixel lighting + alpha blending
// bez artefaktów typowych dla BasicEffect z BlendState.AlphaBlend.

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
//  ŚWIATŁO 1 (primary)
// ═══════════════════════════════════════════════════════════════════
float3 LightDirection;          // kierunek (od źródła do sceny)
float3 LightDiffuseColor;
float3 LightSpecularColor;
float  Light1Enabled;           // 1.0 = on, 0.0 = off

// ═══════════════════════════════════════════════════════════════════
//  ŚWIATŁO 2 (secondary)
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
float  FrontFadeStrength;       // 0..1 — jak mocno wygładzać fresnel na krawędziach

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

    // Normalna — zakładamy brak skalowania nierównomiernego
    output.Normal = normalize(mul(input.Normal, (float3x3)World));

    output.Color = input.Color;

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
    // Jeśli patrzymy na tylną ścianę, normalna jest odwrócona.
    // Bez tego przezroczysta powierzchnia wygląda jak "wklęsła".
    if (dot(N, V) < 0.0)
        N = -N;

    // ═══ AMBIENT ═══
    float3 ambient = AmbientColor * input.Color.rgb;

    // ═══ ŚWIATŁO 1 ═══
    float3 light1Diffuse = 0.0;
    float3 light1Spec = 0.0;

    if (Light1Enabled > 0.5)
    {
        float3 L1 = normalize(-LightDirection);

        // Diffuse (Lambert)
        float ndl1 = saturate(dot(N, L1));
        light1Diffuse = LightDiffuseColor * ndl1 * input.Color.rgb;

        // Specular (Blinn-Phong)
        float3 H1 = normalize(L1 + V);
        float ndh1 = saturate(dot(N, H1));
        light1Spec = LightSpecularColor * pow(ndh1, SpecularPower) * SpecularColor;
    }

    // ═══ ŚWIATŁO 2 ═══
    float3 light2Diffuse = 0.0;
    float3 light2Spec = 0.0;

    if (Light2Enabled > 0.5)
    {
        float3 L2 = normalize(-Light2Direction);

        float ndl2 = saturate(dot(N, L2));
        light2Diffuse = Light2DiffuseColor * ndl2 * input.Color.rgb;

        float3 H2 = normalize(L2 + V);
        float ndh2 = saturate(dot(N, H2));
        light2Spec = Light2SpecularColor * pow(ndh2, SpecularPower) * SpecularColor;
    }

    // ═══ KOŃCOWY KOLOR RGB ═══
    float3 finalColor = ambient
                      + light1Diffuse + light1Spec
                      + light2Diffuse + light2Spec;

    // ═══ FRESNEL — krawędzie bardziej przezroczyste ═══
    // Dzięki temu powierzchnia wygląda jak "szkło" — brzegi prześwitują,
    // środek pozostaje gęsty.
    // FrontFadeStrength = 0 → brak efektu (jednolita alpha)
    // FrontFadeStrength = 1 → silny efekt szkła
    float fresnel = 1.0 - saturate(dot(N, V));
    float fresnelPow = pow(fresnel, 2.0);

    float finalAlpha = input.Color.a;
    if (FrontFadeStrength > 0.0)
    {
        finalAlpha = lerp(input.Color.a, input.Color.a * (0.4 + 0.6 * fresnelPow),
                          FrontFadeStrength);
    }

    return float4(finalColor, finalAlpha);
}

// ═══════════════════════════════════════════════════════════════════
//  TECHNIKA
// ═══════════════════════════════════════════════════════════════════
technique HirshfeldSurface
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();

        // Standardowe stany
        AlphaBlendEnable = TRUE;
        SrcBlend = SRCALPHA;
        DestBlend = INVSRCALPHA;
        BlendOp = ADD;

        ZEnable = TRUE;
        ZWriteEnable = FALSE;       // ⭐ KLUCZOWE — bez zapisu do Z-bufora
        ZFunc = LESSEQUAL;

        CullMode = NONE;            // ⭐ KLUCZOWE — bez backface cullingu
    }
}