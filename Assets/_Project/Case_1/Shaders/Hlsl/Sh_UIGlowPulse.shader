Shader "Case1/Sh_UIGlowPulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glow Sprite", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Base Intensity", Float) = 1

        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed (cycles per sec)", Float) = 0.6
        _PulseMin ("Pulse Min", Range(0, 2)) = 0.35
        _PulseMax ("Pulse Max", Range(0, 4)) = 1.25
        _AlphaPulse ("Alpha Breathing (0..1)", Range(0, 1)) = 1

        [Header(Radial Falloff)]
        [Toggle(USE_RADIAL)] _UseRadial ("Use Radial Falloff", Float) = 1
        _RadialSoftness ("Radial Softness", Range(0.05, 6)) = 1.5

        // --- Standard UI plumbing (matches Unity's UI/Default) ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(Blend)]
        // Additive (5 1) reads best for a glow behind a card; switch to (5 10) for normal alpha.
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            Name "Sh_UIGlowPulse"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ USE_RADIAL

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            half _Intensity;
            half _PulseSpeed;
            half _PulseMin;
            half _PulseMax;
            half _AlphaPulse;
            half _RadialSoftness;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sine breathing in 0..1, then remapped to the user range.
                half wave = sin(_Time.y * _PulseSpeed * 6.28318530718h) * 0.5h + 0.5h;
                half pulse = lerp(_PulseMin, _PulseMax, wave);

                half4 tex = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                half3 rgb = tex.rgb * _Intensity * pulse;

                // Alpha breathes along with the glow so it fades in/out (the "azalma" feel).
                half alphaPulse = lerp(1.0h, saturate(pulse), _AlphaPulse);
                half alpha = tex.a * alphaPulse;

                #ifdef USE_RADIAL
                    // Soft radial mask so a square sprite reads as a round glow.
                    float2 c = i.texcoord - 0.5;
                    half dist = saturate(length(c) * 2.0);
                    half radial = pow(saturate(1.0h - dist), _RadialSoftness);
                    rgb *= radial;
                    alpha *= radial;
                #endif

                fixed4 col = fixed4(rgb, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
