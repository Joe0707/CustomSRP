//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
//多个平行光的属性
CBUFFER_START(_CustomLight)
//float3 _DirectionalLightColor;
//float3 _DirectionalLightDirection;
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
//阴影数据
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//灯光的属性
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    return data;
}


////获取平行光的属性
//Light GetDirectionalLight()
//{
//    Light light;
//    light.color = _DirectionalLightColor;
//    light.direction = _DirectionalLightDirection;
//    return light;
//}
//获取方向光的数量
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

//获取指定索引的方向光的数据
Light GetDirectionalLight(int index, Surface surfaceWS,ShadowData shadowData)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, surfaceWS);
    // light.attenuation = shadowData.cascadeIndex/4.0;
    return light;
}

#endif
