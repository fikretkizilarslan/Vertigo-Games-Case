Shader "Case1/Sh_UIRadialBurst"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Mask (optional)", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 4
        _Opacity ("Opacity", Range(0, 1)) = 1

        [Header(Polar Flow)]
        _SpeedX ("Speed X (angle)", Float) = 0
        _SpeedY ("Speed Y (outward)", Float) = 2
        _Sides ("Ray Segments", Float) = 4.5
        _RingDensity ("Ring Density", Float) = 3
        _Power ("Sharpness", Range(0.1, 8)) = 1.05
        _InnerHole ("Center Hole", Range(0, 0.5)) = 0.12
        _OuterSoftness ("Outer Edge Softness", Range(0.01, 0.3)) = 0.08
        _NoiseTiling ("Noise Tiling (X=angle, Y=radius)", Vector) = (0.01, 2, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(Blend)]
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
            Name "Sh_UIRadialBurst"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            half _Intensity;
            half _Opacity;
            half _SpeedX;
            half _SpeedY;
            half _Sides;
            half _RingDensity;
            half _Power;
            half _InnerHole;
            half _OuterSoftness;
            half4 _NoiseTiling;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                half radius = length(centered) * 2.0h;
                half angle = atan2(centered.y, centered.x) * 0.159154943h + 0.5h;

                // X axis = angle (tangential), Y axis = radius flowing OUTWARD over time.
                half angleCoord = angle * _Sides + _Time.y * _SpeedX;
                half radialCoord = radius * _RingDensity - _Time.y * _SpeedY;

                float2 noiseUV;
                noiseUV.x = angleCoord * _NoiseTiling.x;
                noiseUV.y = radialCoord * _NoiseTiling.y;
                noiseUV = noiseUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw;

                half noise = tex2D(_NoiseTex, noiseUV).r;

                // Procedural outward rings (concentric bands moving center -> edge).
                half ring = frac(radialCoord);
                ring = pow(saturate(1.0h - abs(ring * 2.0h - 1.0h)), _Power);

                // Soft angular segmentation without full spin when SpeedX is 0.
                half segment = abs(sin(angleCoord * 6.2831853h));
                segment = pow(saturate(segment), _Power);

                half burst = saturate(noise * 0.55h + ring * 0.35h + segment * 0.1h);
                burst = pow(burst, _Power);

                // Circular mask: round shape, empty center, soft outer rim.
                half inner = smoothstep(_InnerHole, _InnerHole + 0.06h, radius);
                half outer = 1.0h - smoothstep(1.0h - _OuterSoftness, 1.0h, radius);
                burst *= inner * outer;

                half4 mask = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                half3 rgb = mask.rgb * burst * _Intensity;
                half alpha = mask.a * burst * _Opacity;

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
