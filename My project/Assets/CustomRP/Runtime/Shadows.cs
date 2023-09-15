using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private static string[] dircetionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };
    
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");

    private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");

    // private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");

    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    //级联数据
    private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    private static Vector4[] cascadeData = new Vector4[maxCascades];
    
    //可投射阴影的定向光数量
    private const int maxShadowedDirectionalLightCount = 4;

    //最大级联数量
    private const int maxCascades = 4;

    //存储阴影转换矩阵
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        //斜度比例偏差值
        public float slopeScaleBias;
        //阴影视锥体近裁剪平面偏移
        public float nearPlaneOffset;
    }

    //存储可投射阴影的可见光源的索引
    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    //已存储的可投射阴影的平行光数量
    private int ShadowedDirectionalLightCount;

    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings settings;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }
    
    //设置关键字开启哪种PCF滤波模式
    void SetKeywords()
    {
        int enabledIndex = (int)settings.direcitonal.filter - 1;
        for (int i = 0; i < dircetionalFilterKeywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(dircetionalFilterKeywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(dircetionalFilterKeywords[i]);
            }
        }
    }
    
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //存储可见光的阴影数据
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引,前提是光源开启了阴影投射并且阴影强度不能为0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0f
            //还需要加上一个判断，是否在阴影最大投射距离内，有被该光源影响且需要投影的物体存在，如果没有就不需要渲染该光源的阴影贴图了
            && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
                { visibleLightIndex = visibleLightIndex ,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane
                };
            //返回阴影强度和阴影图块的偏移
            return new Vector3(light.shadowStrength,
                settings.direcitonal.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias);
        }

        return Vector3.zero;
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }

    //渲染定向光阴影
    void RenderDirectionalShadows()
    {
        //创建renderTexture,并指定该类型是阴影贴图
        int atlasSize = (int)settings.direcitonal.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到RT中
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清楚深度缓冲区
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //遍历所有方向光渲染阴影
        //要分割的图块大小和数量
        int tiles = ShadowedDirectionalLightCount * settings.direcitonal.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        //将级联数据和保卫求数据发送到GPU
        buffer.SetGlobalInt(cascadeCountId, settings.direcitonal.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //级联数据发送GPU
        buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
        //阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        // buffer.SetGlobalFloat(shadowDistanceId,settings.maxDistance);
        //最大阴影距离和阴影过度距离发送GPU
        //最大阴影距离和淡入距离发送GPU
        float f = 1f - settings.direcitonal.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,
            new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        SetKeywords();
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    //返回一个从世界空间到阴影空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //如果使用了反向ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        //设置矩阵坐标
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }


    //调整渲染视口来渲染单个图块
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //计算索引图块的偏移位置
        Vector2 offset = new Vector2(index % split, index / split);
        //设置渲染视口，拆分成多个图块
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //包围球直径除以阴影图块尺寸=纹素大小
        float texelSize = 2f * cullingSphere.w / tileSize;
        //得到半径的平方值
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, texelSize * 1.4142136f);
    }
    
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //得到级联阴影贴图需要的参数
        int cascadeCount = settings.direcitonal.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.direcitonal.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            //计算视图和投影矩阵和裁剪空间的立方体
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount,
                ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            //得到第一个光源的包围球数据
            if (index == 0)
            {
                //设置级联数据
                SetCascadeData(i,splitData.cullingSphere,tileSize);
            }

            shadowSettings.splitData = splitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] =
                ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //设置深度偏差
            buffer.SetGlobalDepthBias(0,light.slopeScaleBias);
            //绘制阴影
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f,0f);
        }

        //设置渲染视口
        //SetTileViewport(index, split, tileSize);
        //投影矩阵乘以视图矩阵，得到从世界空间到灯光空间的转换矩阵
        //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
        //设置视图投影矩阵
    }

    //渲染单个光源阴影 
    void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero,
            tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    //释放临时渲染纹理
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
}