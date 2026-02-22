#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FrostBlurUI
{
    public class FrostBlurShaderGUI : ShaderGUI
    {
        bool _foldTint   = true;
        bool _foldBorder = true;
        bool _foldCorner = true;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            _foldTint = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTint, "Blur & Tint");
            if (_foldTint)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProperty("_Color", props), "Tint Color");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _foldBorder = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBorder, "Border");
            if (_foldBorder)
            {
                EditorGUI.indentLevel++;
                var overrideProp = FindProperty("_OverrideBorder", props);
                editor.ShaderProperty(overrideProp, "Override Global Border");
                if (overrideProp.floatValue > 0.5f)
                {
                    editor.ShaderProperty(FindProperty("_BorderColor",     props), "Border Color");
                    editor.ShaderProperty(FindProperty("_BorderThickness", props), "Border Thickness");
                }
                else
                {
                    EditorGUILayout.HelpBox("Using global values from Frost Blur Feature.", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _foldCorner = EditorGUILayout.BeginFoldoutHeaderGroup(_foldCorner, "Corner Radius");
            if (_foldCorner)
            {
                EditorGUI.indentLevel++;
                var overrideProp = FindProperty("_OverrideCorner", props);
                editor.ShaderProperty(overrideProp, "Override Global Corner");
                if (overrideProp.floatValue > 0.5f)
                {
                    var perMode = FindProperty("_CornerPerMode", props);
                    editor.ShaderProperty(perMode, "Per-Corner Mode");
                    if (perMode.floatValue > 0.5f)
                    {
                        editor.ShaderProperty(FindProperty("_CornerTL", props), "Top Left");
                        editor.ShaderProperty(FindProperty("_CornerTR", props), "Top Right");
                        editor.ShaderProperty(FindProperty("_CornerBR", props), "Bottom Right");
                        editor.ShaderProperty(FindProperty("_CornerBL", props), "Bottom Left");
                    }
                    else
                    {
                        editor.ShaderProperty(FindProperty("_CornerRadius", props), "Corner Radius");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Using global values from Frost Blur Feature.", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);
            editor.RenderQueueField();
        }
    }
}
#endif
