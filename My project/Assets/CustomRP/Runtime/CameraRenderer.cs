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

    public void Render(ScriptableRenderContext context,Camera camera,bool useDynamicBatching,bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;
        //���������������
        PreparedBuffer();

        //��Game��ͼ���Ƶļ�����Ҳ���Ƶ�Scene��ͼ��
        PrepareForSceneWindow();

        if (!Cull())
        {
            return;
        }

        Setup();

        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);

        DrawUnsupportedShaders();
        //����Gizmos
        DrawGizmos();
        Submit();
    }
    //�洢�޳���Ľ������
    CullingResults cullingResults;

    bool Cull()
    {
        ScriptableCullingParameters p;

        if(camera.TryGetCullingParameters(out p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //�õ�clear flags
        CameraClearFlags flags = camera.clearFlags;
        //����������״̬
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing)
    {
        //���û���˳����ƶ���Ⱦ���
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching= useDynamicBatching,
            enableInstancing= useGPUInstancing
        };

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
