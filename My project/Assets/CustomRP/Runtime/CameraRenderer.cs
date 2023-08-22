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
    public void Render(ScriptableRenderContext context,Camera camera,bool useDynamicBatching,bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;
        //设置命令缓冲区名字
        PreparedBuffer();

        //在Game视图绘制的几何体也绘制到Scene视图中
        PrepareForSceneWindow();

        if (!Cull())
        {
            return;
        }

        Setup();
        lighting.Setup(context,cullingResults);
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);

        DrawUnsupportedShaders();
        //绘制Gizmos
        DrawGizmos();
        Submit();
    }
    //存储剔除后的结果数据
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
        //得到clear flags
        CameraClearFlags flags = camera.clearFlags;
        //设置相机清楚状态
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing)
    {
        //设置绘制顺序和制定渲染相机
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的shader pass和渲染排序
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching= useDynamicBatching,
            enableInstancing= useGPUInstancing
        };
        //渲染CustomLit表示的pass块
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        //2.绘制天空盒
        context.DrawSkybox(camera);
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.绘制透明物体
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
