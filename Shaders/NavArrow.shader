// NavArrow.shader — Thomapsian Navigation Path
// Procedural scrolling chevron arrow for the Line Renderer.
// Compatible with Unity 6 + URP 17.x
// ZTest Always ensures the path is always drawn on top of floors and walls.

Shader "Thomapsian/NavArrow"
{
    Properties
    {
        _ArrowColor     ("Arrow Color",     Color)         = (1, 0.843, 0, 1)
        _BgColor        ("Glow Color",      Color)         = (1, 0.843, 0, 0.12)
        _ArrowFrequency ("Arrow Frequency", Float)         = 3.0
        _ArrowSharpness ("Arrow Sharpness", Float)         = 10.0
        _ArrowAngle     ("Arrow Angle",     Range(0.1,0.9))= 0.45
        _Offset         ("UV Offset",       Float)         = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "NavArrowForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest  Always
            Cull   Off
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            // Minimal URP include — avoids pulling in heavy lighting code
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Per-material constant buffer (SRP Batcher compatible)
            CBUFFER_START(UnityPerMaterial)
                half4 _ArrowColor;
                half4 _BgColor;
                float _ArrowFrequency;
                float _ArrowSharpness;
                float _ArrowAngle;
                float _Offset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // --- Scroll U toward destination (positive U = forward) ---
                float scrolledU = uv.x * _ArrowFrequency - _Offset;

                // --- Fractional position within one arrow tile [0..1) ---
                float tileU = frac(scrolledU);

                // --- Centered V: [-0.5, 0.5] ---
                float centeredV = uv.y - 0.5;

                // --- Build chevron: diagonal offset of sawtooth by V position ---
                // abs(centeredV) gives symmetry; _ArrowAngle controls the pointiness
                float chevron = frac(tileU - abs(centeredV) * _ArrowAngle * 2.0);

                // --- Threshold: bright front stripe of each tile is the arrowhead ---
                float mask = smoothstep(
                    0.5 - 1.0 / _ArrowSharpness,
                    0.5 + 1.0 / _ArrowSharpness,
                    chevron
                );

                // --- Soft lateral fade so line edges aren't harsh ---
                float edgeFade = 1.0 - smoothstep(0.3, 0.5, abs(centeredV));

                // --- Compose: background glow + arrow on top ---
                half4 col   = lerp(_BgColor, _ArrowColor, mask);
                col.a      *= edgeFade;

                return col;
            }
            ENDHLSL
        }
    }

    // Fallback keeps the asset valid even if URP isn't found
    FallBack Off
}
