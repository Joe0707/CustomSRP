using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ShadowSettings
{
    //PCF滤波模式
    public enum FilterMode
    {
        PCF2x2,PCF3x3,PCF5X5,PCF7x7
    }
    
    //阴影最大距离
    [Min(0f)] public float maxDistance = 100f;
    //阴影过度距离
    [Range(0.001f,1f)]
    public float distanceFade = 0.1f;
    //阴影贴图大小
    public enum TextureSize
    {
        _256 = 256,_512=512,_1024=1024,
        _2048=2048,_4096=4096,_8192 = 8192
    }
    //平行光的阴影属性
    [System.Serializable]
    public struct Directional
    {
        public ShadowSettings.TextureSize atlasSize;

        public FilterMode filter;
        //级联数量
        [Range(1, 4)] public int cascadeCount;
        //级联比例
        [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1,cascadeRatio2,cascadeRatio3);
        //级联淡入值
        [Range(0.001f, 1f)] public float cascadeFade;
    }

    //默认尺寸为1024
    public Directional direcitonal = new Directional
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };

}

