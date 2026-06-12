Shader "Case1/UI/PanningTile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Panning)]
        _Tiling ("Tile Count (XY)", Vector) = (4, 4, 0, 0)
        _PanSpeed ("Pan Speed (UV/s)", Vector) = (0.05, 0.02, 0, 0)
        _PanOffset ("Pan Offset", Vector) = (0, 0, 0, 0)
        _Rotation ("Rotation (Degrees)", Float) = 0
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777

        [Header(Center Glow)]
        [Toggle] _UseCenterGlow ("Enable Center Glow", Float) = 1
        [Enum(Procedural,0, Sprite,1)] _GlowSource ("Glow Source", Float) = 0
        _GlowColor ("Glow Tint", Color) = (0.92, 0.75, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 1.15
        _GlowShine ("Glow Shine", Range(0, 3)) = 1
        [HDR] _GlowEmissionColor ("Emission Color", Color) = (0.85, 0.65, 1, 1)
        _GlowEmission ("Emission Strength", Range(0, 5)) = 0
        _GlowCenter ("Glow Center (XY)", Vector) = (0.5, 0.5, 0, 0)
        [Toggle] _InvertGlow ("Invert (ic kisim soluk)", Float) = 1
        _EdgeDarkness ("Edge Darkness", Range(0, 1)) = 0.45
        _CenterAlphaBoost ("Center Alpha Boost", Range(1, 12)) = 6.5

        [Header(Procedural Glow)]
        [Enum(Radial,0, VerticalBand,1)] _GlowMode ("Glow Shape", Float) = 0
        _GlowWidth ("Glow Width", Range(0.05, 2.5)) = 1.43
        _GlowSoftness ("Glow Softness", Range(1, 16)) = 5.9
        _GlowAspect ("Screen Aspect (W/H)", Float) = 1.777

        [Header(Glow Sprite)]
        _GlowTex ("Glow Sprite", 2D) = "white" {}
        _GlowTexScale ("Glow Size (buyuk=genis)", Vector) = (0.58, 0.85, 0, 0)
        _GlowTexOffset ("Glow Offset", Vector) = (0, 0, 0, 0)

        [Header(Blend Mode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10

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
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIPanningTile"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
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
            sampler2D _GlowTex;
            float4 _GlowTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _Tiling;
            float4 _PanSpeed;
            float4 _PanOffset;
            float _Rotation;
            float _AspectRatio;
            float _UseCenterGlow;
            float _GlowSource;
            float _GlowMode;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowShine;
            fixed4 _GlowEmissionColor;
            float _GlowEmission;
            float _GlowWidth;
            float _GlowSoftness;
            float4 _GlowCenter;
            float4 _GlowTexScale;
            float4 _GlowTexOffset;
            float _GlowAspect;
            float _InvertGlow;
            float _EdgeDarkness;
            float _CenterAlphaBoost;

            float GetProceduralGlowMask(float2 uv)
            {
                float2 centered = uv - _GlowCenter.xy;

                float dist;
                if (_GlowMode < 0.5)
                {
                    centered.x *= _GlowAspect;
                    dist = length(centered);
                }
                else
                {
                    dist = abs(centered.x);
                }

                float edgeMask = smoothstep(0.0, max(_GlowWidth, 0.001), dist);
                return 1.0 - pow(saturate(edgeMask), _GlowSoftness);
            }

            float GetGlowSpriteMask(float2 uv)
            {
                float2 centered = uv - _GlowCenter.xy + _GlowTexOffset.xy;
                float2 glowUV = centered / max(_GlowTexScale.xy, 0.001) + 0.5;
                glowUV = glowUV * _GlowTex_ST.xy + _GlowTex_ST.zw;

                fixed4 glowSample = tex2D(_GlowTex, glowUV);
                float luminance = dot(glowSample.rgb, float3(0.299, 0.587, 0.114));
                return saturate(max(glowSample.a, luminance));
            }

            float GetGlowMask(float2 uv)
            {
                if (_GlowSource > 0.5)
                    return GetGlowSpriteMask(uv);

                return GetProceduralGlowMask(uv);
            }

            float2 RotateUV(float2 uv, float angleDeg, float aspect)
            {
                float rad = angleDeg * 0.0174532925;
                float cosA = cos(rad);
                float sinA = sin(rad);

                uv -= 0.5;
                uv.x *= aspect;

                float2 rotated;
                rotated.x = uv.x * cosA - uv.y * sinA;
                rotated.y = uv.x * sinA + uv.y * cosA;

                rotated.x /= aspect;
                rotated += 0.5;
                return rotated;
            }

            float2 GetPanningUV(float2 uv)
            {
                uv = RotateUV(uv, _Rotation, _AspectRatio);
                return frac(uv * _Tiling.xy + _PanOffset.xy + _Time.y * _PanSpeed.xy);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = GetPanningUV(i.texcoord);
                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * i.color;

                if (_UseCenterGlow > 0.5)
                {
                    float glowMask = GetGlowMask(i.texcoord);
                    float visMask = glowMask;
                    if (_InvertGlow > 0.5)
                        visMask = 1.0 - glowMask;

                    half glowStrength = _GlowIntensity * _GlowShine;
                    half3 glowTint = _GlowColor.rgb * glowStrength;

                    half3 edgeRgb = color.rgb * _EdgeDarkness;
                    half3 centerRgb = color.rgb + glowTint * visMask;
                    centerRgb += _GlowColor.rgb * glowStrength * visMask * visMask * 0.5;

                    color.rgb = lerp(edgeRgb, centerRgb, visMask);
                    color.rgb += _GlowEmissionColor.rgb * _GlowEmission * glowMask;
                    color.a *= lerp(_EdgeDarkness, _CenterAlphaBoost, visMask);
                }

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
