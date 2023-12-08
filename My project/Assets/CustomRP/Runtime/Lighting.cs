using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class Lighting
{
    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    //设置最大可见定向光数量
    const int maxDirLightCount = 4;

    //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    private static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
    private Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];
    
    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    //存储阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    CullingResults cullingResults;

    private Shadows shadows = new Shadows();

    //定义其他类型光源的最大数量
    private const int maxOtherLightCount = 64;

    private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    private static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    private static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
    private static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    private static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";
    
    
    //存储其他类型光源的颜色和位置数据
    private static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
    
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings,bool useLightsPerObject)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //传递阴影数据
        shadows.Setup(context, cullingResults, shadowSettings);
        //SetupDirectionalLight();
        //发送光源数据
        SetupLights(useLightsPerObject);
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    /// <summary>
    /// 将点光源的颜色和位置信息存储到数组
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleLight"></param>
    void SetupPointLight(int index,int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        //位置信息在本地到世界的转换矩阵的最后一列
        var position = visibleLight.localToWorldMatrix.GetColumn(3);
        //将光照范围的平方的倒数存储在光源位置的W分量中
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);
        Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    
    //存储并发送所有光源数据
    void SetupLights(bool useLightsPerObject)
    {
        //拿到光源索引列表
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;

        //得到所有影响相机渲染物体的可见光数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        
        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for ( i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        //VisibleLight结构很大,我们改为传递引用不是传递值，这样不会生成副本
                        SetupDirectionalLight(dirLightCount++,i, ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointLight(otherLightCount++,i,ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLight(otherLightCount++,i,ref visibleLight);
                    }
                    break;
            }

            if (useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }
        }
        
        //消除所有不可见光的索引
        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            } 
            
            
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lightsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }
        
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        
        buffer.SetGlobalInt(otherLightCountId,otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId,otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId,otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId,otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId,otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId,otherLightShadowData);
        }
    }

    //存储定向光的数据
    void SetupDirectionalLight(int index,int visibleIndex, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        //存储阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }
    
    //将聚光灯光源的颜色、位置和方向信息存储到数组
    void SetupSpotLight(int index,int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        //本地到世界的转换矩阵的第三列在求反得到光照方向
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    
    //释放阴影贴图RT内存
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}