using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public partial class CustomRenderPineAsset : RenderPipelineAsset
{
    //�������״̬�ֶ�
    [SerializeField] bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    [SerializeField]
    //�Ƿ�ʹ����������
    private bool useLightsPerObject = true;

    //��Ӱ����
    [SerializeField] ShadowSettings shadows = default;

    [SerializeField] private PostFXSettings postFXSettings = default;


    [SerializeField] private CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale = 1f,
        fxaa = new CameraBufferSettings.FXAA()
        {
            fixedThreshold = 0.0833f,
            relativeThreshold = 0.166f,
            subpixelBlending = 0.75f
        }
    };

    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }

    //LUT�ֱ���
    [SerializeField] private ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    [SerializeField] private Shader cameraRendererShader = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher,
            useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution, cameraRendererShader);
    }
}