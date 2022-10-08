#ifndef MY_BLUR
#define MY_BLUR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"//函数库：主要用于各种的空间变换

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    float _BlurRange;
    float4 _MainTex_TexelSize;
CBUFFER_END

struct appdata
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f 
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f vert(appdata v)
{
    v2f o;
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.uv;
    return o;
}
half4 GuassianBlurHorizontal(v2f i) : SV_Target
{
    float blurrange = _BlurRange / 50;
    float4 left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-blurrange, 0.0)) * 0.2;
    float4 mid =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv ) * 0.6;
    float4 right =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.uv + float2(blurrange, 0.0)) * 0.2;
    float4 col = left + mid + right;
    return col;
}
half4 GuassianBlurVertical(v2f i) : SV_Target
{
    float blurrange = _BlurRange / 50;
    float4 down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -blurrange)) * 0.2;
    float4 mid =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.uv ) * 0.6;
    float4 up =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, blurrange)) * 0.2;
    float4 col = down + mid + up;
    return col;
}
#endif
