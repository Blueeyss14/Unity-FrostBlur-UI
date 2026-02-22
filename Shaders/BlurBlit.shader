Shader "Hidden/FrostBlurUI/BlurBlit"
{
    Properties
    {
        _MainTex    ("Source",      2D)    = "white" {}
        _BlurOffset ("Blur Offset", Float) = 1.0
        _BlurScale  ("Blur Scale",  Float) = 1.0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Gaussian"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragGaussian
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurOffset;
            float _BlurScale;

            static const int   TAPS = 4;
            static const float W[5] = { 0.2270270, 0.1945946, 0.1216216, 0.0540541, 0.0162162 };

            half4 FragGaussian(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv   = input.texcoord;
                float2 step = _BlurScale * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float2 dir  = (_BlurOffset >= 0) ? float2(step.x, 0) : float2(0, step.y);
                float  s    = abs(_BlurOffset);

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * W[0];
                UNITY_UNROLL
                for (int i = 1; i <= TAPS; i++)
                {
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + dir * i * s) * W[i];
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - dir * i * s) * W[i];
                }
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}