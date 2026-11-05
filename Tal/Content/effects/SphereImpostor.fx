#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float4x4 View;
float4x4 Projection;
float4x4 ViewProjection;
float4x4 InverseViewProjection;
float3 CameraPosition;

float3 LightDirection;
float3 LightDiffuseColor;
float3 LightSpecularColor;
float Light1Enabled;

float3 Light2Direction;
float3 Light2DiffuseColor;
float3 Light2SpecularColor;
float Light2Enabled;

float3 AmbientColor;
float3 SpecularColor;
float SpecularPower;
float Alpha;

struct VSInput {
    float3 Center : POSITION0;
    float2 Corner : TEXCOORD0;
    float Radius : TEXCOORD1;
    float3 Color : COLOR0;
};

struct VSOutput {
    float4 Position : SV_POSITION;
    float4 ClipPos : TEXCOORD0;
    float3 WorldCenter : TEXCOORD1;
    float Radius : TEXCOORD2;
    float3 Color : COLOR0;
    float2 Corner : TEXCOORD3;
};

VSOutput VSMain(VSInput input) {
    VSOutput o;

    float3 right = float3(View._11, View._21, View._31);
    float3 up = float3(View._12, View._22, View._32);

    float pad = 1.1;

    float3 worldPos = input.Center + right * (input.Corner.x * input.Radius * pad) + up * (input.Corner.y * input.Radius * pad);

    float4 clipPos = mul(float4(worldPos, 1), ViewProjection);

    o.Position = clipPos;
    o.ClipPos = clipPos;
    o.WorldCenter = input.Center;
    o.Radius = input.Radius;
    o.Color = input.Color;
    o.Corner = input.Corner;

    return o;
}

struct PSOutput {
    float4 Color : COLOR0;
#if !OPENGL
    float Depth : SV_Depth;
#endif
};

PSOutput PSMain(VSOutput input) {
    PSOutput o;

    float4 clipPos = input.ClipPos;
    float3 ndc = clipPos.xyz / clipPos.w;

    float4 worldNear = mul(float4(ndc.xy, 0.0, 1.0), InverseViewProjection);
    float4 worldFar = mul(float4(ndc.xy, 1.0, 1.0), InverseViewProjection);

    float3 worldNear3 = worldNear.xyz / worldNear.w;
    float3 worldFar3 = worldFar.xyz / worldFar.w;

    float3 ro = CameraPosition;
    float3 rd = normalize(worldFar3 - worldNear3);

    float3 ce = input.WorldCenter;
    float ra = input.Radius;

    float3 oc = ro - ce;
    float3 crossProd = cross(oc, rd);
    float h = ra * ra - dot(crossProd, crossProd);
    float b = dot(oc, rd);

// ? ANTYALIASING KRAWÊDZI SFERY
// h = ra^2 - |cross(oc, rd)|^2
// h < 0  ? piksel poza sfer¹ (odrzucamy)
// h ~ 0  ? piksel na krawêdzi (mieszamy alpha)
// h >> 0 ? piksel wewn¹trz sfery (pe³na alpha)

if (h < 0.0) discard;

float t = -b - sqrt(h);
if (t < 0.0) discard;

float3 hit = ro + rd * t;
float3 normal = normalize(hit - ce);

// ? Wyg³adzenie krawêdzi: im bli¿ej krawêdzi, tym mniejsza alpha.
//    Wartoœæ 0.01 to "gruboœæ" gradientu — mo¿esz j¹ stroiæ.
//    W jednostkach ra^2 — dla ma³ych sfer (radius ~0.1-0.5) daje ~1-2 px.
float edgeSoftness = ra * ra * 0.25f;   // skaluje siê z rozmiarem sfery
float edgeAlpha = saturate(h / max(edgeSoftness, 1e-6f));

// pe³na alpha wewn¹trz, 0 na krawêdzi (i poza — ale tam ju¿ discard)
float finalAlpha = Alpha * edgeAlpha;



    float3 N = normal;
    float3 V = normalize(CameraPosition - hit);

    float3 diffuse1 = float3(0, 0, 0);
    float3 specular1 = float3(0, 0, 0);

    if (Light1Enabled > 0.5)
    {
        float3 L1 = -normalize(LightDirection);
        float ndotl1 = saturate(dot(N, L1));

        diffuse1 = input.Color * LightDiffuseColor * ndotl1;

        float3 R1 = reflect(-L1, N);
        float spec1 = pow(saturate(dot(R1, V)), SpecularPower);
        specular1 = LightSpecularColor * spec1 * ndotl1;
    }

    float3 diffuse2 = float3(0, 0, 0);
    float3 specular2 = float3(0, 0, 0);

    if (Light2Enabled > 0.5)
    {
        float3 L2 = -normalize(Light2Direction);
        float ndotl2 = saturate(dot(N, L2));

        diffuse2 = input.Color * Light2DiffuseColor * ndotl2;

        float3 R2 = reflect(-L2, N);
        float spec2 = pow(saturate(dot(R2, V)), SpecularPower);
        specular2 = Light2SpecularColor * spec2 * ndotl2;
    }

    float3 ambient = input.Color * AmbientColor;

   o.Color = float4(ambient + diffuse1 + diffuse2 + specular1 + specular2, finalAlpha);

#if !OPENGL
    float4 clipHit = mul(float4(hit, 1), ViewProjection);
    o.Depth = clipHit.z / clipHit.w;
#endif

    return o;
}

technique SphereImpostor {
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL PSMain();
    }
}