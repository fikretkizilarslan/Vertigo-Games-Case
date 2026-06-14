Shader "Case1/Sh_Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Shine)]
        _ShineTex ("Shine Map (Clamp Wrap)", 2D) = "white" {}
        [HDR] _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineTravelSpeed ("Travel Speed", Float) = 2
        _ShinePauseDuration ("Pause Duration", Float) = 3
        _ShineAngle ("Angle (Degrees)", Range(-90, 90)) = 20
        _ShineStrength ("Shine Strength", Float) = 1.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Sh_Shine"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct shine_vert_in
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localCoord : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct shine_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localCoord : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _ShineTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _ShineTex_ST;
            fixed4 _ShineColor;
            float _ShineTravelSpeed;
            float _ShinePauseDuration;
            float _ShineAngle;
            float _ShineStrength;

            float2 RotateLocalUV(float2 uv, float angleDeg)
            {
                float rad = angleDeg * 0.0174532925;
                float sinA, cosA;
                sincos(rad, sinA, cosA);

                uv -= 0.5;
                float2 rotated;
                rotated.x = uv.x * cosA - uv.y * sinA;
                rotated.y = uv.x * sinA + uv.y * cosA;
                return rotated + 0.5;
            }

            float GetShineOffset()
            {
                float cycleLength = _ShinePauseDuration + (1.0 / max(_ShineTravelSpeed, 0.001));
                float phase = fmod(_Time.y, cycleLength);
                return lerp(-2.0, 2.0, saturate(phase * _ShineTravelSpeed));
            }

            shine_v2f vert(shine_vert_in v)
            {
                shine_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.localCoord = v.localCoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(shine_v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                float2 shineUV = RotateLocalUV(i.localCoord, _ShineAngle);
                shineUV.x -= GetShineOffset();

                half4 shineSample = half4(0, 0, 0, 0);
                if (shineUV.x >= 0.0 && shineUV.x <= 1.0)
                    shineSample = tex2D(_ShineTex, shineUV);

                half3 shineAdd = shineSample.rgb * shineSample.a * _ShineColor.rgb * _ShineColor.a * _ShineStrength * color.a;
                color.rgb += shineAdd;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
