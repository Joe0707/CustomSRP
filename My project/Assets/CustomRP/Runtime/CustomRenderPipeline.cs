using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing;
    private ShadowSettings shadowSettings;
    private bool useLightsPerObject;
    private PostFXSettings postFXSettings;
    private bool allowHDR;
    public CustomRenderPipeline(bool allowHDR,bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,bool useLightsPerObject,
        ShadowSettings shadowSettings,PostFXSettings postFXSettings)
    {
        this.allowHDR = allowHDR;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera,allowHDR, useDynamicBatching, useGPUInstancing, useLightsPerObject,shadowSettings,postFXSettings);
        }
    }
}