using System;
using UnityEngine.Rendering;

[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    
    public bool copyDepth;

    public bool copyDepthReflection;
}
