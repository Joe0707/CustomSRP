using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ShadowSettings
{
    //��Ӱ������
    [Min(0f)] public float maxDistance = 100f;
    //��Ӱ��ͼ��С
    public enum TextureSize
    {
        _256 = 256,_512=512,_1024=1024,
        _2048=2048,_4096=4096,_8192 = 8192
    }
    
    [System.Serializable]
    public struct Directional
    {
        public ShadowSettings.TextureSize atlasSize;
    }

    //Ĭ�ϳߴ�Ϊ1024
    public Directional direcitonal = new Directional
    {
        atlasSize = TextureSize._1024
    };
}
