using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    //�洢��Ӱת������
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];
    //��Ͷ����Ӱ�Ķ��������
    private const int maxShadowedDirectionalLightCount = 4;

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    
    //�洢��Ͷ����Ӱ�Ŀɼ���Դ������
    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    
    //�Ѵ洢�Ŀ�Ͷ����Ӱ��ƽ�й�����
    private int ShadowedDirectionalLightCount;
    
    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings settings;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    //�洢�ɼ������Ӱ����
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //�洢�ɼ���Դ������,ǰ���ǹ�Դ��������ӰͶ�䲢����Ӱǿ�Ȳ���Ϊ0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0f
        //����Ҫ����һ���жϣ��Ƿ�����Ӱ���Ͷ������ڣ��б��ù�ԴӰ������ҪͶӰ��������ڣ����û�оͲ���Ҫ��Ⱦ�ù�Դ����Ӱ��ͼ��
        && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex};
            //������Ӱǿ�Ⱥ���Ӱͼ���ƫ��
            return new Vector2(light.shadowStrength, ShadowedDirectionalLightCount++);
        }
        return Vector2.zero;
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }
    
    //��Ⱦ�������Ӱ
    void RenderDirectionalShadows()
    {
        //����renderTexture,��ָ������������Ӱ��ͼ
        int atlasSize = (int)settings.direcitonal.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        //ָ����Ⱦ���ݴ洢��RT��
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        //�����Ȼ�����
        buffer.ClearRenderTarget(true,false,Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        //�������з������Ⱦ��Ӱ
        //Ҫ�ָ��ͼ���С������
        int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split,tileSize);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    //����һ��������ռ䵽��Ӱ�ռ��ת������
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,Vector2 offset,int split)
    {
        //���ʹ���˷���ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //���þ�������
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30)+offset.x*m.m30)*scale;
        m.m01 = (0.5f * (m.m01 + m.m31)+offset.x*m.m31)*scale;
        m.m02 = (0.5f * (m.m02 + m.m32)+offset.x*m.m32)*scale;
        m.m03 = (0.5f * (m.m03 + m.m33)+offset.x*m.m33)*scale;
        m.m10 = (0.5f * (m.m10 + m.m30)+offset.y*m.m30)*scale;
        m.m11 = (0.5f * (m.m11 + m.m31)+offset.y*m.m31)*scale;
        m.m12 = (0.5f * (m.m12 + m.m32)+offset.y*m.m32)*scale;
        m.m13 = (0.5f * (m.m13 + m.m33)+offset.y*m.m33)*scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }


    //������Ⱦ�ӿ�����Ⱦ����ͼ��
    Vector2 SetTileViewport(int index,int split,float tileSize)
    {
        //��������ͼ���ƫ��λ��
        Vector2 offset = new Vector2(index % split, index / split);
        //������Ⱦ�ӿڣ���ֳɶ��ͼ��
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero,
            tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        //������Ⱦ�ӿ�
        //SetTileViewport(index, split, tileSize);
        //ͶӰ���������ͼ���󣬵õ�������ռ䵽�ƹ�ռ��ת������
        //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix*viewMatrix,SetTileViewport(index,split,tileSize),split);
        //������ͼͶӰ����
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);

    }
    
    //��Ⱦ������Դ��Ӱ 
    void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults,light.visibleLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero,
            tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }
    
    //�ͷ���ʱ��Ⱦ����
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
    
}
