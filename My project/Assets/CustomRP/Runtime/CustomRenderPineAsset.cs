using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPineAsset : RenderPipelineAsset
{
    //�������״̬�ֶ�
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    //�Ƿ�ʹ����������
    private bool useLightsPerObject = true;

    //��Ӱ����
    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField] private PostFXSettings postFXSettings = default;
    
    //HDR����
    [SerializeField] private bool allowHDR = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR,useDynamicBatching,useGPUInstancing,useSRPBatcher,useLightsPerObject,shadows,postFXSettings);
    }
}
