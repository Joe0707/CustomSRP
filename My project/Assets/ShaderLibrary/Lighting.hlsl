//���������ؿ�
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
//�����������
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)) * light.color;
}
//������ճ��������ɫ���õ����յ�������ɫ
float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light) * surface.color;
}
//��������ı�����Ϣ��ȡ���չ��ս��
float3 GetLighting(Surface surface)
{
    //�ɼ�������������������ۼӵõ������������
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        color += GetLighting(surface, GetDirectionalLight(i));

    }
    return color;
}

#endif