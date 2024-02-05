using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;
    bool useDynamicBatching, useGPUInstancing;
    private ShadowSettings shadowSettings;
    private bool useLightsPerObject;
    private PostFXSettings postFXSettings;
    private int colorLUTResolution;
    private CameraBufferSettings cameraBufferSettings;
    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings,bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,bool useLightsPerObject,
        ShadowSettings shadowSettings,PostFXSettings postFXSettings,int colorLUTResolution,Shader cameraRendererShader)
    {
        this.cameraBufferSettings = cameraBufferSettings;
        this.colorLUTResolution = colorLUTResolution;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
        renderer = new CameraRenderer(cameraRendererShader);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera,cameraBufferSettings, useDynamicBatching, useGPUInstancing, useLightsPerObject,shadowSettings,postFXSettings,colorLUTResolution);
        }
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context,cameras[i],cameraBufferSettings,useDynamicBatching,useGPUInstancing,useLightsPerObject,shadowSettings,postFXSettings,colorLUTResolution);
        }
    }
    

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dispose();
    }
}