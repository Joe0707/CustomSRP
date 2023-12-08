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

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject,
        ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;
        //����buffer������������
        PreparedBuffer();

        //��Game��ͼ���Ƶļ�����Ҳ���Ƶ�Scene��ͼ��
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings,useLightsPerObject);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing,useLightsPerObject);

        DrawUnsupportedShaders();
        //����Gizmos
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }

    //�洢����޳���Ľ��
    CullingResults cullingResults;

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;

        if (camera.TryGetCullingParameters(out p))
        {
            //�õ������Ӱ����,�����Զ�������Ƚϣ�ȡ��С���Ǹ���Ϊ��Ӱ����
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //�õ������clear flags
        CameraClearFlags flags = camera.clearFlags;
        //����������״̬
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags =
            useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        
        //���û���˳���ָ����Ⱦ���
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //������Ⱦ��shader pass����Ⱦ����
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //������Ⱦʱ�������ʹ��״̬
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps |PerObjectData.ShadowMask| PerObjectData.LightProbe |PerObjectData.OcclusionProbe| PerObjectData.LightProbeProxyVolume|PerObjectData.OcclusionProbeProxyVolume|PerObjectData.ReflectionProbes|lightsPerObjectFlags
        };
        //��ȾCustomLit��ʾ��pass��
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.���Ʋ�͸������
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        //2.������պ�
        context.DrawSkybox(camera);
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.����͸������
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