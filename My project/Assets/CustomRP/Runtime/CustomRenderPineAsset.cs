using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPineAsset : RenderPipelineAsset
{
    //定义合批状态字段
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    //是否使用逐对象光照
    private bool useLightsPerObject = true;

    //阴影设置
    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField] private PostFXSettings postFXSettings = default;
    
    //HDR设置
    [SerializeField] private bool allowHDR = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR,useDynamicBatching,useGPUInstancing,useSRPBatcher,useLightsPerObject,shadows,postFXSettings);
    }
}
