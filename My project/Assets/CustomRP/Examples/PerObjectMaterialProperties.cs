using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cuttoff");
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");
    private static int emissionColorId = Shader.PropertyToID("_EmissionColor");
    
    [SerializeField]
    Color baseColor= Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f;
    [SerializeField, Range(0f, 1f)]
    float metallic = 0f;
    [SerializeField, Range(0f, 1f)]
    float smoothness = 0.5f;
    static MaterialPropertyBlock block;
    [SerializeField]
    Color emissionColor = Color.black;
    
    
    private void OnValidate()
    {
        if(block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId,metallic);
        block.SetFloat(smoothnessId,smoothness);
        block.SetColor(emissionColorId,emissionColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }

}
