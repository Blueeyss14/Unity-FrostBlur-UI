using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FrostBlurUI
{
    [DisallowMultipleRendererFeature("Frost Blur Feature")]
    public class FrostBlurFeature : ScriptableRendererFeature
    {
        public enum CornerMode { Linked, PerCorner }
        public enum InjectionPoint
        {
            AfterRenderingOpaques,
            BeforeRenderingTransparents,
            AfterRenderingPostProcessing
        }

        [Serializable]
        public class BlurSettings
        {
            [Range(0f, 100f)] public float strength   = 50f;
            public int   iterations  = 5;
            public float scale       = 2f;
        }

        [Serializable]
        public class BorderSettings
        {
            public bool enableBorder = true;
            public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
            [Range(0f, 32f)] public float thickness = 2f;
            public CornerMode cornerMode = CornerMode.Linked;
            [Range(0f, 512f)] public float radius = 24f;
            [Range(0f, 512f)] public float radiusTL = 24f;
            [Range(0f, 512f)] public float radiusTR = 24f;
            [Range(0f, 512f)] public float radiusBR = 24f;
            [Range(0f, 512f)] public float radiusBL = 24f;
        }

        [Serializable]
        public class AdvancedSettings
        {
            public InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
        }

        public BlurSettings     blurSettings     = new BlurSettings();
        public BorderSettings   borderSettings   = new BorderSettings();
        public AdvancedSettings advancedSettings = new AdvancedSettings();

        static readonly int s_BorderColor     = Shader.PropertyToID("_FBorderColor");
        static readonly int s_BorderThickness = Shader.PropertyToID("_FBorderThickness");
        static readonly int s_BorderEnabled   = Shader.PropertyToID("_FBorderEnabled");
        static readonly int s_CornerRadii     = Shader.PropertyToID("_FCornerRadii");

        FrostBlurPass _pass;

        public override void Create()
        {
            _pass = new FrostBlurPass
            {
                renderPassEvent = ToEvent(advancedSettings.injectionPoint)
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;
            PushGlobals();
            _pass.Setup(blurSettings);
            renderer.EnqueuePass(_pass);
        }

        void PushGlobals()
        {
            var b = borderSettings;
            Shader.SetGlobalFloat(s_BorderEnabled,   b.enableBorder ? 1f : 0f);
            Shader.SetGlobalColor(s_BorderColor,     b.enableBorder ? b.borderColor : Color.clear);
            Shader.SetGlobalFloat(s_BorderThickness, b.enableBorder ? b.thickness : 0f);
            Vector4 radii = b.cornerMode == CornerMode.Linked
                ? new Vector4(b.radius, b.radius, b.radius, b.radius)
                : new Vector4(b.radiusTL, b.radiusTR, b.radiusBR, b.radiusBL);
            Shader.SetGlobalVector(s_CornerRadii, radii);
        }

        static RenderPassEvent ToEvent(InjectionPoint ip)
        {
            return ip switch
            {
                InjectionPoint.AfterRenderingOpaques        => RenderPassEvent.AfterRenderingOpaques,
                InjectionPoint.BeforeRenderingTransparents  => RenderPassEvent.BeforeRenderingTransparents,
                InjectionPoint.AfterRenderingPostProcessing => RenderPassEvent.AfterRenderingPostProcessing,
                _ => RenderPassEvent.AfterRenderingPostProcessing
            };
        }
    }
}
