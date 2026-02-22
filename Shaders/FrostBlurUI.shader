Shader "FrostBlurUI/FrostBlurUI"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,0.9)

        [HideInInspector] _MainTex          ("",2D)    = "white"{}
        [HideInInspector] _StencilComp      ("",Float) = 8
        [HideInInspector] _Stencil          ("",Float) = 0
        [HideInInspector] _StencilOp        ("",Float) = 0
        [HideInInspector] _StencilWriteMask ("",Float) = 255
        [HideInInspector] _StencilReadMask  ("",Float) = 255
        [HideInInspector] _ColorMask        ("",Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "PreviewType"     = "Plane"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "FrostBlurUI"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FrostBlurTexture);
            SAMPLER(sampler_FrostBlurTexture);

            float4 _FBorderColor;
            float  _FBorderThickness;
            float  _FBorderEnabled;
            float4 _FCornerRadii;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv    : TEXCOORD0;
                half4  color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posHCS    : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                half4  color     : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.posHCS    = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv        = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.posHCS);
                OUT.color     = IN.color * _Color;
                return OUT;
            }

            float RoundedRectSDF(float2 p, float2 halfSize, float4 r)
            {
                float radius;
                if      (p.x <  0.0 && p.y >= 0.0) radius = r.x;
                else if (p.x >= 0.0 && p.y >= 0.0) radius = r.y;
                else if (p.x >= 0.0 && p.y <  0.0) radius = r.z;
                else                                radius = r.w;
                radius = min(radius, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 rectSize = abs(float2(1.0 / ddx(IN.uv.x), 1.0 / ddy(IN.uv.y)));
                float2 p        = (IN.uv - 0.5) * rectSize;
                float2 halfSize = rectSize * 0.5;
                float  dist     = RoundedRectSDF(p, halfSize, _FCornerRadii);

                float fillAlpha = 1.0 - smoothstep(-1.0, 0.0, dist);
                clip(fillAlpha - 0.001);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                half4  blur     = SAMPLE_TEXTURE2D(_FrostBlurTexture, sampler_FrostBlurTexture, screenUV);

                half4 result = blur * IN.color;
                result.a     = fillAlpha * IN.color.a;

                if (_FBorderEnabled > 0.5)
                {
                    float borderAlpha = smoothstep(-_FBorderThickness - 1.0, -_FBorderThickness, dist)
                                      * (1.0 - smoothstep(-1.0, 0.0, dist));
                    result.rgb = lerp(result.rgb, _FBorderColor.rgb, borderAlpha * _FBorderColor.a);
                }

                return result;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
    CustomEditor "FrostBlurUI.FrostBlurShaderGUI"
}