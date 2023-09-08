//�ƹ�������ؿ�
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

//��Ӱͼ��
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
//���������Ͱ�Χ������
int _CascadeCount;
float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
//��Ӱ����
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
//float _ShadowDistance;
//��Ӱ���Ⱦ���
float4 _ShadowDistanceFade;
CBUFFER_END

//��Ӱ��������Ϣ
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};

//������Ӱͼ��
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

//������Ӱ˥��
float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if (data.strength <= 0.0)
    {
        return 1.0;
    }
    //ͨ����Ӱת������ͱ���λ�õõ�����Ӱ����ͼ�飩�ռ��λ�ã�Ȼ���ͼ�����в���
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    //������Ӱ˥��ֵ����Ӱǿ�Ⱥ�˥�����ӵĲ�ֵ
    return lerp(1.0, shadow, data.strength);
}

//��Ӱ����
struct ShadowData
{
    int cascadeIndex;
    //�Ƿ������Ӱ�ı�ʶ
    float strength;
};

//��ʽ������Ӱ����ʱ��ǿ��
float FadedShadowStrength(float distance,float scale,float fade)
{
    return saturate((1.0-distance*scale)*fade);
}

//�õ�����ռ�ı�����Ӱ����
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    //ͨ����ʽ�õ������Թ��ɵ���Ӱǿ��
    data.strength = FadedShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    data.cascadeIndex = 0;
    int i;
    //���������浽���ĵ�ƽ������С������뾶��ƽ������˵������������㼶����Χ���У��õ����ʵļ����㼶����
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position,sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            break;
        }
    }
    if(i==_CascadeCount)
    {
        data.strength = 0.0;
    }
    data.cascadeIndex = i;
    return data;
}


#endif
