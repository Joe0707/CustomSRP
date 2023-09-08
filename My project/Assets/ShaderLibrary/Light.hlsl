//�ƹ�������ؿ�
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
//���ƽ�й������
CBUFFER_START(_CustomLight)
//float3 _DirectionalLightColor;
//float3 _DirectionalLightDirection;
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
//��Ӱ����
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//�ƹ������
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

//��ȡ��������Ӱ����
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    return data;
}


////��ȡƽ�й������
//Light GetDirectionalLight()
//{
//    Light light;
//    light.color = _DirectionalLightColor;
//    light.direction = _DirectionalLightDirection;
//    return light;
//}
//��ȡ����������
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

//��ȡָ�������ķ���������
Light GetDirectionalLight(int index, Surface surfaceWS,ShadowData shadowData)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //�õ���Ӱ����
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
    //�õ���Ӱ˥��
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, surfaceWS);
    // light.attenuation = shadowData.cascadeIndex/4.0;
    return light;
}

#endif
