using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    
    ScriptableRenderContext context;

    Camera camera;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    public void Render(ScriptableRenderContext context,Camera camera)
    {
        this.context = context;
        this.camera = camera;

        if (!Cull())
        {
            return;
        }

        Setup();

        DrawVisibleGeometry();

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
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        //设置绘制顺序和制定渲染相机
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings);

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
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

}
