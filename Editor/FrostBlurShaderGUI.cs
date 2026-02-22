#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FrostBlurUI
{
    public class FrostBlurShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            editor.ShaderProperty(FindProperty("_Color", props), "Tint Color");
            EditorGUILayout.Space(6);
            editor.RenderQueueField();
        }
    }
}
#endif