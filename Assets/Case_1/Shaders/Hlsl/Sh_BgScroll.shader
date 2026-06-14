Shader "Case1/Sh_BgScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Scroll)]
        _ScrollRepeat ("Repeat Count (XY)", Vector) = (4, 4, 0, 0)
        _ScrollVelocity ("Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
        _ScrollBias ("Scroll Offset", Vector) = (0, 0, 0, 0)
        _ScrollAngle ("Scroll Rotation", Float) = 0
        _ScrollAspect ("Canvas Aspect", Float) = 1.777

        [Header(Vignette)]
        [Toggle] _EnableVignette ("Enable Vignette", Float) = 1
        [Enum(Procedural,0, Texture,1)] _VignetteMode ("Vignette Mode", Float) = 0
        _VignetteTint ("Vignette Tint", Color) = (0.92, 0.75, 1, 1)
        _VignettePower ("Vignette Power", Range(0, 4)) = 1.15
        _VignetteBloom ("Vignette Bloom", Range(0, 3)) = 1
        [HDR] _VignetteEmitColor ("Emit Color", Color) = (0.85, 0.65, 1, 1)
        _VignetteEmitStrength ("Emit Strength", Range(0, 5)) = 0
        _VignetteAnchor ("Vignette Center", Vector) = (0.5, 0.5, 0, 0)
        [Toggle] _VignetteInvert ("Invert (core fade)", Float) = 1
        _BorderDim ("Border Dim", Range(0, 1)) = 0.45
        _CoreAlphaLift ("Core Alpha Lift", Range(1, 12)) = 6.5

        [Header(Vignette Procedural)]
        [Enum(Radial,0, VerticalBand,1)] _VignetteShape ("Shape", Float) = 0
        _VignetteRadius ("Radius", Range(0.05, 2.5)) = 1.43
        _VignetteFeather ("Feather", Range(1, 16)) = 5.9
        _VignetteAspect ("Shape Aspect", Float) = 1.777

        [Header(Vignette Texture)]
        _VignetteMap ("Vignette Map", 2D) = "white" {}
        _VignetteMapScale ("Map Scale (large=wide)", Vector) = (0.58, 0.85, 0, 0)
        _VignetteMapOffset ("Map Offset", Vector) = (0, 0, 0, 0)

        [Header(Blend)]
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
            Name "Sh_BgScroll"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct bg_vert_in
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct bg_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _VignetteMap;
            float4 _VignetteMap_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _ScrollRepeat;
            float4 _ScrollVelocity;
            float4 _ScrollBias;
            float _ScrollAngle;
            float _ScrollAspect;
            float _EnableVignette;
            float _VignetteMode;
            float _VignetteShape;
            fixed4 _VignetteTint;
            float _VignettePower;
            float _VignetteBloom;
            fixed4 _VignetteEmitColor;
            float _VignetteEmitStrength;
            float4 _VignetteAnchor;
            float _VignetteInvert;
            float _BorderDim;
            float _CoreAlphaLift;
            float _VignetteRadius;
            float _VignetteFeather;
            float _VignetteAspect;
            float4 _VignetteMapScale;
            float4 _VignetteMapOffset;

            float SampleProceduralVignette(float2 uv)
            {
                float2 delta = uv - _VignetteAnchor.xy;
                float metric;

                if (_VignetteShape < 0.5)
                {
                    delta.x *= _VignetteAspect;
                    metric = length(delta);
                }
                else
                {
                    metric = abs(delta.x);
                }

                float rim = smoothstep(0.0, max(_VignetteRadius, 0.001), metric);
                return 1.0 - pow(saturate(rim), _VignetteFeather);
            }

            float SampleTextureVignette(float2 uv)
            {
                float2 delta = uv - _VignetteAnchor.xy + _VignetteMapOffset.xy;
                float2 mapUV = delta / max(_VignetteMapScale.xy, 0.001) + 0.5;
                mapUV = mapUV * _VignetteMap_ST.xy + _VignetteMap_ST.zw;

                fixed4 sample = tex2D(_VignetteMap, mapUV);
                float luma = dot(sample.rgb, float3(0.299, 0.587, 0.114));
                return saturate(max(sample.a, luma));
            }

            float ResolveVignetteMask(float2 uv)
            {
                if (_VignetteMode > 0.5)
                    return SampleTextureVignette(uv);

                return SampleProceduralVignette(uv);
            }

            float2 ApplyScrollTransform(float2 uv)
            {
                float rad = _ScrollAngle * 0.0174532925;
                float cosA = cos(rad);
                float sinA = sin(rad);

                uv -= 0.5;
                uv.x *= _ScrollAspect;

                float2 spun;
                spun.x = uv.x * cosA - uv.y * sinA;
                spun.y = uv.x * sinA + uv.y * cosA;
                spun.x /= _ScrollAspect;
                spun += 0.5;

                return frac(spun * _ScrollRepeat.xy + _ScrollBias.xy + _Time.y * _ScrollVelocity.xy);
            }

            bg_v2f vert(bg_vert_in v)
            {
                bg_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(bg_v2f i) : SV_Target
            {
                float2 scrollUV = ApplyScrollTransform(i.texcoord);
                half4 baseColor = (tex2D(_MainTex, scrollUV) + _TextureSampleAdd) * i.color;

                if (_EnableVignette > 0.5)
                {
                    float shapeMask = ResolveVignetteMask(i.texcoord);
                    float paintMask = shapeMask;
                    if (_VignetteInvert > 0.5)
                        paintMask = 1.0 - shapeMask;

                    half accent = _VignettePower * _VignetteBloom;
                    half3 tint = _VignetteTint.rgb * accent;
                    half3 dimmed = baseColor.rgb * _BorderDim;
                    half3 lifted = baseColor.rgb + tint * paintMask;
                    lifted += _VignetteTint.rgb * accent * paintMask * paintMask * 0.5;

                    baseColor.rgb = lerp(dimmed, lifted, paintMask);
                    baseColor.rgb += _VignetteEmitColor.rgb * _VignetteEmitStrength * shapeMask;
                    baseColor.a *= lerp(_BorderDim, _CoreAlphaLift, paintMask);
                }

                #ifdef UNITY_UI_CLIP_RECT
                baseColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseColor.a - 0.001);
                #endif

                return baseColor;
            }
            ENDCG
        }
    }
}
