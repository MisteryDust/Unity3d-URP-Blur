#ifndef KAWASE_BLUR
#define KAWASE_BLUR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"//函数库：主要用于各种的空间变换

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float _BlurRange;
float4 _MainTex_TexelSize;

struct a2v
{
    float4 positionOS:POSITION;
    float2 texcoord:TEXCOORD;
};
struct v2f
{
    float4 positionCS:SV_POSITION;
    float2 texcoord:TEXCOORD;
};

v2f vertex(a2v i)
{
    v2f o;
    o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
    o.texcoord = i.texcoord;
    return o;
}

half4 fragment(v2f i):SV_TARGET
{              
    half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.texcoord); //中心像素
    //四角像素
    //注意这个【_BlurRange】，这就是扩大卷积核范围的参数
    tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.texcoord+float2(-1,-1)*_MainTex_TexelSize.xy*_BlurRange); 
    tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.texcoord+float2(1,-1)*_MainTex_TexelSize.xy*_BlurRange);
    tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.texcoord+float2(-1,1)*_MainTex_TexelSize.xy*_BlurRange);
    tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.texcoord+float2(1,1)*_MainTex_TexelSize.xy*_BlurRange);
    return tex/5.0;
}
#endif