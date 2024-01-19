using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPineAsset))]
public class CustomLightEditor : LightEditor
{
    private static GUIContent renderingLayerMaskLabel =
        new GUIContent("Rendering Layer Mask", "Functional version of above property");

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // DrawRenderingLayerMask();
        RenderingLayerMaskDrawer.Draw(settings.renderingLayerMask,renderingLayerMaskLabel);
        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
        }

        settings.ApplyModifiedProperties();

        //如果光源的CullingMask不是Everything层，显示警告:CullingMask只影响阴影
        //如果不是定向光源，则提示除非开启逐对象光照，除了影响阴影还可以影响物体受光
        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(
                light.type == LightType.Directional
                    ? "Culling Mask only affects shadows."
                    : "Culling Mask only affects shadow unless Use Lights Per Objects is on.",
                MessageType.Warning
            );
        }
    }

    void DrawRenderingLayerMask()
    {
        SerializedProperty property = settings.renderingLayerMask;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int mask = property.intValue;
        if (mask == int.MaxValue)
        {
            mask = -1;
        }

        mask = EditorGUILayout.MaskField(renderingLayerMaskLabel, mask,
            GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames);
        if (EditorGUI.EndChangeCheck())
        {
            property.intValue = mask == -1 ? int.MaxValue : mask;
        }

        EditorGUI.showMixedValue = false;
    }
}