using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
public partial class CameraRenderer
{
    partial void DrawUnsupportedShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PreparedBuffer();
#if UNITY_EDITOR
    string SampleName { get; set; }

    partial void PreparedBuffer()
    {
        //����һ��ֻ���ڱ༭��ģʽ�²ŷ����ڴ�
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };
    //�������
    static Material errorMaterial;

    partial void DrawUnsupportedShaders()
    {
        //��֧�ֵ�ShaderTag��������ʹ�ô������ר��Shader����Ⱦ(��ɫ)
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            //�����������������ɫ����PassName����i=1��ʼ
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        //ʹ��Ĭ�����ü��ɣ������������Ķ��ǲ�֧�ֵ�
        var filteringSettings = FilteringSettings.defaultValue;
        //���Ʋ�֧�ֵ�ShaderTag���͵�����
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            //����л�����Scene��ͼ�����ô˷�����ɻ���
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }


#else
const string SampleName = bufferName;
#endif
}
