#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)

UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)

UNITY_DEFINE_INSTANCED_PROP(float,_SoftParticlesDistance)
UNITY_DEFINE_INSTANCED_PROP(float,_SoftParticlesRange)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,name)

struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    float4 color;
    float3 flipbookUVB;
    bool flipbookBlending;
    bool nearFade;
    bool softParticles;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.baseUV = baseUV;
    c.color = 1.0;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    c.nearFade = false;
    c.softParticles = false;
    return c;
}


float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(InputConfig c)
{
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    if(c.flipbookBlending)
    {
        baseMap = lerp(baseMap,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,c.flipbookUVB.xy),c.flipbookUVB.z);
    }

    if(c.nearFade)
    {
        float nearAttenuation = (c.fragment.depth - INPUT_PROP(_NearFadeDistance)) /
            INPUT_PROP(_NearFadeRange);
        baseMap.a *= saturate(nearAttenuation);
    }

    if(c.softParticles)
    {
        float depthDelta = c.fragment.bufferDepth -c.fragment.depth;
        float nearAttenuation = (depthDelta - INPUT_PROP(_SoftParticlesDistance)) / INPUT_PROP(_SoftParticlesRange);
        baseMap.a *= saturate(nearAttenuation);
    }
    
    float4 baseColor = INPUT_PROP( _BaseColor);
    return baseMap * baseColor * c.color;
}


float3 GetEmission(InputConfig c)
{
    return GetBase(c).rgb;
}


float GetCutoff(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(InputConfig c)
{
    return 0.0;
}

float GetSmoothness(InputConfig c)
{
    return 0.0;
}

float GetFinalAlpha(float alpha)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ZWrite) ? 1.0 : alpha;
}


#endif
