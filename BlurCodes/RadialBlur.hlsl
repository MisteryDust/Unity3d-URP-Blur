#ifndef RADIAL_BLUR
#define RADIAL_BLUR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
float4 _MainTex_ST;

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

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float _BlurRange;
int _LoopCount;
float _X;
float _Y;

v2f vert(appdata i)
{
    v2f o;
    o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
    o.uv = i.uv;
    return o;
}
half4 frag(v2f i) :SV_TARGET
{
    float4 col = 0;
    float2 dir = (float2(_X,_Y) - i.uv) * _BlurRange * 0.01;

    for(int t = 0; t < _LoopCount; t++)
    {
        col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        i.uv += dir; 
    }
    return col / _LoopCount;
}

#endif