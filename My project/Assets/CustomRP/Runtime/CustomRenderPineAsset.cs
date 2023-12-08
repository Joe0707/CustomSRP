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
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher,useLightsPerObject,shadows);
    }
}
