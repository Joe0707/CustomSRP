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
    private static CameraSettings defaultCameraSettings = new CameraSettings();

    private Material material;

    private static int sourceTextureId = Shader.PropertyToID("_SourceTexture");

    private Texture2D missingTexture;
    
    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0,0,Color.white * 0.5f);
        missingTexture.Apply(true,true);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }
    
    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings bufferSettings, bool useDynamicBatching,
        bool useGPUInstancing, bool useLightsPerObject,
        ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = bufferSettings.copyDepthReflection;
        }
        else
        {
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }
        
        //????????????????????????????????????滻???????????????
        if (cameraSettings.overridePostFX)
        {
            postFXSettings = cameraSettings.postFXSettings;
        }


        PreparedBuffer();

        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject,
            cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        postFXStack.Setup(context, camera, postFXSettings, useHDR, colorLUTResolution, cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject,
            cameraSettings.renderingLayerMask);

        DrawUnsupportedShaders();

        DrawGizmosBeforeFX();

        if (postFXStack.IsActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if(useIntermediateBuffer)
        {
            Draw(colorAttachmentId,BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }

        DrawGizmosAfterFX();

        Cleanup();
        Submit();
    }

    void Cleanup()
    {
        lighting.Cleanup();

        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);

            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
            
        }
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to)
    {
        buffer.SetGlobalTexture(sourceTextureId,from);
        buffer.SetRenderTarget(to,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
    }
    
    //拷贝深度数据
    void CopyAttachments()
    {
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Point,RenderTextureFormat.Depth);
            buffer.CopyTexture(depthAttachmentId,depthTextureId);
            ExecuteBuffer();
        }
    }
    

    //??????????????
    CullingResults cullingResults;

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;

        if (camera.TryGetCullingParameters(out p))
        {
            //?????????????,????????????????????????????????????
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    private static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    private static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");

    private static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    //是否正在使用深度纹理
    private bool useDepthTexture;
    //是否使用中间帧缓冲
    private bool useIntermediateBuffer;
    
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //????????clear flags
        CameraClearFlags flags = camera.clearFlags;
        useIntermediateBuffer = useDepthTexture || postFXStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }

            buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear,
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            
            buffer.GetTemporaryRT(depthAttachmentId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Point,RenderTextureFormat.Depth);
            
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }

        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(depthTextureId,missingTexture);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,
        int renderingLayerMask)
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
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe |
                            PerObjectData.OcclusionProbe | PerObjectData.LightProbeProxyVolume |
                            PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes |
                            lightsPerObjectFlags
        };
        //???CustomLit?????pass??
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings =
            new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
        //1.????????????
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        //2.????????
        context.DrawSkybox(camera);

        CopyAttachments();
        
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