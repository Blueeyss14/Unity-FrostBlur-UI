Shader "FrostBlurUI/FrostBlurUI"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,0.9)

        [Toggle] _OverrideBorder ("Override Global Border", Float) = 0
        _BorderColor     ("Border Color",     Color) = (1,1,1,0.8)
        [Range(0,32)]
        _BorderThickness ("Border Thickness", Float) = 2

        [Toggle] _OverrideCorner ("Override Global Corner", Float) = 0
        [Toggle] _CornerPerMode  ("Per-Corner Mode",        Float) = 0
        [Range(0,512)] _CornerRadius ("Corner Radius",       Float) = 24
        [Range(0,512)] _CornerTL    ("Corner Top Left",      Float) = 24
        [Range(0,512)] _CornerTR    ("Corner Top Right",     Float) = 24
        [Range(0,512)] _CornerBR    ("Corner Bottom Right",  Float) = 24
        [Range(0,512)] _CornerBL    ("Corner Bottom Left",   Float) = 24

        _RectSize ("Rect Size px", Vector) = (200,100,0,0)

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

            #pragma shader_feature_local _ _OVERRIDEBORDER_ON
            #pragma shader_feature_local _ _OVERRIDECORNER_ON
            #pragma shader_feature_local _ _CORNERPERMODE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FrostBlurTexture);
            SAMPLER(sampler_FrostBlurTexture);

            float4 _FBorderColor;
            float  _FBorderThickness;
            float  _FBorderEnabled;
            float4 _FCornerRadii;

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _BorderColor;
                float  _BorderThickness;
                float4 _RectSize;
                float  _CornerRadius;
                float  _CornerTL;
                float  _CornerTR;
                float  _CornerBR;
                float  _CornerBL;
                float  _OverrideBorder;
                float  _OverrideCorner;
                float  _CornerPerMode;
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

            float RoundedRectSDF(float2 uv, float2 size, float4 r)
            {
                float2 p = (uv - 0.5) * size;
                float radius;
                if      (p.x <  0.0 && p.y >= 0.0) radius = r.x;
                else if (p.x >= 0.0 && p.y >= 0.0) radius = r.y;
                else if (p.x >= 0.0 && p.y <  0.0) radius = r.z;
                else                                radius = r.w;
                radius = min(radius, min(size.x, size.y) * 0.5);
                float2 q = abs(p) - (size * 0.5 - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float4 radii;
                #if defined(_OVERRIDECORNER_ON)
                    #if defined(_CORNERPERMODE_ON)
                        radii = float4(_CornerTL, _CornerTR, _CornerBR, _CornerBL);
                    #else
                        radii = (_CornerRadius).xxxx;
                    #endif
                #else
                    radii = _FCornerRadii;
                #endif

                float  bThickness;
                float4 bColor;
                float  bEnabled;
                #if defined(_OVERRIDEBORDER_ON)
                    bThickness = _BorderThickness;
                    bColor     = _BorderColor;
                    bEnabled   = 1.0;
                #else
                    bThickness = _FBorderThickness;
                    bColor     = _FBorderColor;
                    bEnabled   = _FBorderEnabled;
                #endif

                float dist      = RoundedRectSDF(IN.uv, _RectSize.xy, radii);
                float fillAlpha = 1.0 - smoothstep(-1.0, 0.0, dist);
                clip(fillAlpha - 0.001);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                half4  blur     = SAMPLE_TEXTURE2D(_FrostBlurTexture, sampler_FrostBlurTexture, screenUV);

                half4 result = blur * IN.color;
                result.a     = fillAlpha * IN.color.a;

                if (bEnabled > 0.5)
                {
                    float borderAlpha = smoothstep(-bThickness - 1.0, -bThickness, dist)
                                      * (1.0 - smoothstep(-1.0, 0.0, dist));
                    result.rgb = lerp(result.rgb, bColor.rgb, borderAlpha * bColor.a);
                }

                return result;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
    CustomEditor "FrostBlurUI.FrostBlurShaderGUI"
}
