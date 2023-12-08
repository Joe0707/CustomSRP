using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    private static string[] dircetionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };

    //??????????????
    private static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    private static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");

    private static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");

    private static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];


    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");

    private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");

    // private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");

    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

    //????????
    private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    private static Vector4[] cascadeData = new Vector4[maxCascades];

    private static string[] cascadeBlendKeywords = { "_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER" };

    private static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
    
    //??????????????????
    private const int maxShadowedDirectionalLightCount = 4;

    //???????????????????????
    private const int maxShadowedOtherLightCount = 16;

    //????????????????????????
    private int shadowedDirLightCount,shadowedOtherLightCount;


    //?????????
    private const int maxCascades = 4;

    //?洢??????????
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
    }

    //?洢??????????????????????
    private ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;

        //б?????????
        public float slopeScaleBias;

        //??????????ü???????
        public float nearPlaneOffset;
    }

    //?洢?????????????????????
    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    //??洢?????????????й?????
    private int ShadowedDirectionalLightCount;

    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings settings;

    private bool useShadowMask;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
        useShadowMask = false;
        shadowedOtherLightCount = 0;
    }

    //???ù???????????PCF?????
    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //?洢???????????????
    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }

        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }

        if (shadowedOtherLightCount >= maxShadowedOtherLightCount ||
            !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias
        };


        return new Vector4(light.shadowStrength, shadowedOtherLightCount++, 0f, maskChannel);
    }


    //?洢?????????????
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //?洢????????????,???????????????????????????????0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0f)
        {
            float maskChannel = -1;
            //????????ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
            if (
                lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
                lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask
            )
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            //?????????????????????б??ù???????????????????????????о?????????ù????????????
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            //?????????????????????
            return new Vector4(light.shadowStrength,
                settings.direcitonal.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias, maskChannel);
        }

        return new Vector4(0, 0, 0, -1);
    }

    private Vector4 atlasSizes;

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

        if (shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        }

        //????????????
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords,
            useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        //???????????????GPU
        buffer.SetGlobalInt(cascadeCountId, ShadowedDirectionalLightCount > 0 ? settings.direcitonal.cascadeCount : 0);
        //????????????????????GPU
        float f = 1f - settings.direcitonal.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,
            new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        //?????????С???????С
        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    //???????????
    void RenderDirectionalShadows()
    {
        //????renderTexture,???????????????????
        int atlasSize = (int)settings.direcitonal.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;

        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        //??????????洢??RT??
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //???????????
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId,1f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //???????з??????????
        //?????????С??????
        int tiles = ShadowedDirectionalLightCount * settings.direcitonal.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        //????????????????GPU
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        //???????????GPU
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //????????????GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //???ù????
        SetKeywords(dircetionalFilterKeywords, (int)settings.direcitonal.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.direcitonal.cascadeBlend - 1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex,BatchCullingProjectionType.Perspective);
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        
        //计算法线偏差
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)settings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        SetOtherTileData(index,bias);
        otherShadowMatrices[index] =
            ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(index, split, tileSize), split);
        
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        
        buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
        buffer.SetGlobalDepthBias(0f, 0f);
    }

    private static int otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");
    private static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
    void RenderOtherShadows()
    {
        //????renderTexture,???????????????????
        int atlasSize = (int)settings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        //??????????洢??RT??
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //???????????
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId,0f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //???????з??????????
        //?????????С??????
        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedOtherLightCount; i++)
        {
            RenderSpotShadows(i, split, tileSize);
        }

        //????????????GPU
        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        buffer.SetGlobalVectorArray(otherShadowTilesId,otherShadowTiles);
        //???ù????
        SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    
    //存储非定向光阴影图块数据
    void SetOtherTileData(int index, float bias)
    {
        Vector4 data = Vector4.zero;
        data.w = bias;
        otherShadowTiles[index] = data;
    }
    
    
    
    
    //?????????????????????????????
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //???????????ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        //???????????
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


    //??????????????????????
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //???????????????λ??
        Vector2 offset = new Vector2(index % split, index / split);
        //????????????????????
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //??Χ?????????????????=?????С
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.direcitonal.filter + 1f);

        cullingSphere.w -= filterSize;
        //???????????
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //?????????????????????
        int cascadeCount = settings.direcitonal.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.direcitonal.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.direcitonal.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            //????????????????ü???????????
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount,
                ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            //??????????????Χ??????
            if (index == 0)
            {
                //???ü???????
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }

            //??????
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            //?????????????????????????????????????????
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] =
                ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //??????????
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
            //???????
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }

        //??????????
        //SetTileViewport(index, split, tileSize);
        //???????????????????????????????????????
        //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
        //?????????????
    }

    //????????????? 
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

    //?????????????
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        if (shadowedOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }

        ExecuteBuffer();
    }
}