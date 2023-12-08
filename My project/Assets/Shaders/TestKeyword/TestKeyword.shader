Shader "CustomRP/TestKeywords"
{
    Properties
    {
        _BaseMap("Texture",2D) = "white" {}
        [HDR]_BaseColor("Color",Color) = (1.0,1.0,1.0,1.0)
        //͸���Ȳ��Ե���ֵ
        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping",Float) = 0
        //���û��ģʽ
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend",Float) = 0
        //Ĭ��д����Ȼ�����
        [Enum(Off,0,On,1)] _ZWrite("Z Write",Float) = 1

    }
    SubShader
    {
        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "../UnlitInput.hlsl"
        ENDHLSL
        Pass
        {
            //������ģʽ
            Blend[_SrcBlend][_DstBlend]
            //�Ƿ�д�����
            ZWrite[_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "../UnlitPass.hlsl"
            ENDHLSL
        }

    }
}