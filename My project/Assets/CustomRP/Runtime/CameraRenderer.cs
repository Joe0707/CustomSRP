using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    Camera camera;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    Lighting lighting = new Lighting();

    private PostFXStack postFXStack = new PostFXStack();
    private bool useHDR;
    public void Render(ScriptableRenderContext context, Camera camera,bool allowHDR, bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject,
        ShadowSettings shadowSettings,PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;
        PreparedBuffer();

        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        useHDR = allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings,useLightsPerObject);
        postFXStack.Setup(context,camera,postFXSettings,useHDR);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing,useLightsPerObject);

        DrawUnsupportedShaders();
        
        DrawGizmosBeforeFX();

        if (postFXStack.IsActive)
        {
            postFXStack.Render(frameBufferId);
        }
        
        DrawGizmosAfterFX();
        
        Cleanup();
        Submit();
    }

    void Cleanup()
    {
        lighting.Cleanup();

        if (postFXStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }
    
    //?洢???????????
    CullingResults cullingResults;

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;

        if (camera.TryGetCullingParameters(out p))
        {
            //?????????????,???????????????????С???????????????
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }
    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机的clear flags
        CameraClearFlags flags = camera.clearFlags;
    
        if (postFXStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(frameBufferId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Bilinear,useHDR?RenderTextureFormat.DefaultHDR:RenderTextureFormat.Default);
            buffer.SetRenderTarget(frameBufferId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }
        
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags =
            useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        
        //????????????????????
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //?????????shader pass?????????
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //?????????????????????
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps |PerObjectData.ShadowMask| PerObjectData.LightProbe |PerObjectData.OcclusionProbe| PerObjectData.LightProbeProxyVolume|PerObjectData.OcclusionProbeProxyVolume|PerObjectData.ReflectionProbes|lightsPerObjectFlags
        };
        //???CustomLit?????pass??
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.????????????
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        //2.????????
        context.DrawSkybox(camera);
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.???????????
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}