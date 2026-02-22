using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FrostBlurUI
{
    public class FrostBlurPass : ScriptableRenderPass
    {
        static readonly int s_BlurTexGlobal = Shader.PropertyToID("_FrostBlurTexture");
        static readonly int s_TempA         = Shader.PropertyToID("_FB_TempA");
        static readonly int s_TempB         = Shader.PropertyToID("_FB_TempB");
        static readonly int s_Offset        = Shader.PropertyToID("_BlurOffset");
        static readonly int s_Scale         = Shader.PropertyToID("_BlurScale");

        const string k_ShaderName = "Hidden/FrostBlurUI/BlurBlit";

        Material _mat;
        FrostBlurFeature.BlurSettings _blur;
        bool _ready;

        public void Setup(FrostBlurFeature.BlurSettings blur)
        {
            _blur = blur;
            if (_mat == null)
            {
                var sh = Shader.Find(k_ShaderName);
                if (sh == null)
                {
                    Debug.LogError("[FrostBlurUI] Shader not found: " + k_ShaderName);
                    _ready = false;
                    return;
                }
                _mat = CoreUtils.CreateEngineMaterial(sh);
            }
            _ready = true;
        }

        void GetBlurParams(out int iterations, out float downsample, out float scale)
        {
            iterations = _blur.iterations;
            downsample = Mathf.Lerp(1f, 100f, _blur.strength / 100f);
            scale      = _blur.scale;
        }

#if UNITY_6000_0_OR_NEWER

        class BlitData
        {
            public Material mat;
            public float offset;
            public float scale;
            public TextureHandle src;
            public TextureHandle dst;
        }

        class GlobalData { public TextureHandle result; }

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frame)
        {
            if (!_ready) return;

            var res     = frame.Get<UniversalResourceData>();
            var camData = frame.Get<UniversalCameraData>();
            if (res.isActiveTargetBackBuffer) return;

            GetBlurParams(out int iterations, out float downsample, out float scale);

            var cam = camData.camera;
            int w   = Mathf.Max(1, Mathf.RoundToInt(cam.pixelWidth  / downsample));
            int h   = Mathf.Max(1, Mathf.RoundToInt(cam.pixelHeight / downsample));

            var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.Default, 0)
            {
                useMipMap = false, autoGenerateMips = false
            };

            TextureHandle ping = UniversalRenderer.CreateRenderGraphTexture(rg, desc, "_FB_Ping", false, FilterMode.Bilinear);
            TextureHandle pong = UniversalRenderer.CreateRenderGraphTexture(rg, desc, "_FB_Pong", false, FilterMode.Bilinear);

            RGBlit(rg, res.activeColorTexture, ping, scale, scale, "FB_Init");

            bool srcPing = true;
            for (int i = 0; i < iterations; i++)
            {
                RGBlit(rg, srcPing ? ping : pong, srcPing ? pong : ping,  scale, scale, $"FB_H{i}");
                srcPing = !srcPing;
                RGBlit(rg, srcPing ? ping : pong, srcPing ? pong : ping, -scale, scale, $"FB_V{i}");
                srcPing = !srcPing;
            }

            TextureHandle final = srcPing ? ping : pong;

            using (var builder = rg.AddUnsafePass<GlobalData>("FB_Global", out var d))
            {
                d.result = final;
                builder.UseTexture(final, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((GlobalData data, UnsafeGraphContext ctx) =>
                    ctx.cmd.SetGlobalTexture(s_BlurTexGlobal, data.result));
            }
        }

        void RGBlit(RenderGraph rg, TextureHandle src, TextureHandle dst, float offset, float scale, string name)
        {
            using (var builder = rg.AddUnsafePass<BlitData>(name, out var d))
            {
                d.mat    = _mat;
                d.offset = offset;
                d.scale  = scale;
                d.src    = src;
                d.dst    = dst;
                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((BlitData data, UnsafeGraphContext ctx) =>
                {
                    data.mat.SetFloat(s_Offset, data.offset);
                    data.mat.SetFloat(s_Scale,  data.scale);
                    ctx.cmd.SetRenderTarget(data.dst);
                    Blitter.BlitTexture(ctx.cmd, data.src, new Vector4(1, 1, 0, 0), data.mat, 0);
                });
            }
        }

#else

        RTHandle _camColor;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _camColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_ready) return;

            GetBlurParams(out int iterations, out float downsample, out float scale);

            var cam = renderingData.cameraData.camera;
            int w   = Mathf.Max(1, Mathf.RoundToInt(cam.pixelWidth  / downsample));
            int h   = Mathf.Max(1, Mathf.RoundToInt(cam.pixelHeight / downsample));

            var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.Default, 0)
            {
                useMipMap = false, autoGenerateMips = false
            };

            var cmd = CommandBufferPool.Get("FrostBlurPass");
            cmd.GetTemporaryRT(s_TempA, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(s_TempB, desc, FilterMode.Bilinear);
            cmd.Blit(_camColor, s_TempA);

            bool srcA = true;
            for (int i = 0; i < iterations; i++)
            {
                int src = srcA ? s_TempA : s_TempB;
                int dst = srcA ? s_TempB : s_TempA;
                _mat.SetFloat(s_Offset,  scale);
                _mat.SetFloat(s_Scale,   scale);
                cmd.Blit(src, dst, _mat, 0);
                srcA = !srcA;

                src = srcA ? s_TempA : s_TempB;
                dst = srcA ? s_TempB : s_TempA;
                _mat.SetFloat(s_Offset, -scale);
                _mat.SetFloat(s_Scale,   scale);
                cmd.Blit(src, dst, _mat, 0);
                srcA = !srcA;
            }

            cmd.SetGlobalTexture(s_BlurTexGlobal, srcA ? s_TempA : s_TempB);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_TempA);
            cmd.ReleaseTemporaryRT(s_TempB);
        }
#endif
    }
}