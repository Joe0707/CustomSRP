//BRDF相关库
#ifndef CUSTOM_BRDF_INCLUDED
# define  CUSTOM_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
    float perceptualRoughness;
    float fresnel;
};

//电介质的反射率平均约0.04
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return diffuse * brdf.diffuse + reflection;
}

//获取给定表面的BRDF数据
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinuseReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinuseReflectivity;
    //透明度预乘
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    //光滑度转为实际粗糙度
    brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
    brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinuseReflectivity);
    return brdf;
}

//根据公式得到镜面反射强度
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

//直接光照的表面颜色
float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif
