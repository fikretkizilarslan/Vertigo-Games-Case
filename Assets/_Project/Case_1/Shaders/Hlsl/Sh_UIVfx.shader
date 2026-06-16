Shader "Case1/Sh_UIVfx"
{
    Properties
    {
        [PerRendererData] _MainTex ("Main Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1

        [Header(Flipbook)]
        [Toggle(USE_FLIPBOOK)] _UseFlipbook ("Use Flipbook", Float) = 0
        _FlipbookGrid ("Grid (Cols, Rows)", Vector) = (1, 1, 0, 0)
        _FlipbookFPS ("Speed (FPS)", Float) = 10
        _FlipbookFrameCount ("Frame Count", Float) = 1

        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Toggle] _PremultiplyRGB ("Premultiply RGB", Float) = 0
        [Toggle] _ZWrite ("Z Write", Float) = 0
    }

    // URP primary
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "UIVfxURP"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #pragma shader_feature_local _ USE_FLIPBOOK

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticleInstancing.hlsl"
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
                half4 _FlipbookGrid;
                half _FlipbookFPS;
                half _FlipbookFrameCount;
                half _PremultiplyRGB;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half frameIndex : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 GetFlipbookUV(float2 uv, half frameIndex)
            {
                half cols = max(_FlipbookGrid.x, 1.0h);
                half rows = max(_FlipbookGrid.y, 1.0h);
                half maxFrames = min(_FlipbookFrameCount, cols * rows);
                frameIndex = floor(fmod(frameIndex, maxFrames));

                half col = fmod(frameIndex, cols);
                half row = floor(frameIndex / cols);
                half2 cellSize = half2(1.0h / cols, 1.0h / rows);
                half2 cellOffset = half2(col * cellSize.x, (rows - 1.0h - row) * cellSize.y);
                return uv * cellSize + cellOffset;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.texcoord.xy, _MainTex);
                output.color = input.color * _Color;

                #if defined(USE_FLIPBOOK)
                    half age01 = input.texcoord.z;
                    output.frameIndex = (age01 > 0.001h)
                        ? age01 * max(_FlipbookFrameCount - 1.0h, 0.0h)
                        : floor(_Time.y * _FlipbookFPS);
                #else
                    output.frameIndex = 0.0h;
                #endif

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                #if defined(USE_FLIPBOOK)
                    uv = GetFlipbookUV(uv, input.frameIndex);
                #endif

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 vert = input.color;

                half3 rgb = tex.rgb * vert.rgb * _Intensity;
                half  alpha = tex.a * vert.a;

                if (_PremultiplyRGB > 0.5h)
                    rgb *= alpha;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    // Built-in fallback
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

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "UIVfxBuiltin"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_particles
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #pragma shader_feature_local _ USE_FLIPBOOK

            #include "UnityCG.cginc"
            #include "UnityStandardParticleInstancing.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half _Intensity;
            half4 _FlipbookGrid;
            half _FlipbookFPS;
            half _FlipbookFrameCount;
            half _PremultiplyRGB;

            struct appdata
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half frameIndex : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 GetFlipbookUV(float2 uv, half frameIndex)
            {
                half cols = max(_FlipbookGrid.x, 1.0h);
                half rows = max(_FlipbookGrid.y, 1.0h);
                half maxFrames = min(_FlipbookFrameCount, cols * rows);
                frameIndex = floor(fmod(frameIndex, maxFrames));

                half col = fmod(frameIndex, cols);
                half row = floor(frameIndex / cols);
                half2 cellSize = half2(1.0h / cols, 1.0h / rows);
                half2 cellOffset = half2(col * cellSize.x, (rows - 1.0h - row) * cellSize.y);
                return uv * cellSize + cellOffset;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.color = v.color * _Color;

                #if defined(USE_FLIPBOOK)
                    half age01 = v.texcoord.z;
                    o.frameIndex = (age01 > 0.001h)
                        ? age01 * max(_FlipbookFrameCount - 1.0h, 0.0h)
                        : floor(_Time.y * _FlipbookFPS);
                #else
                    o.frameIndex = 0.0h;
                #endif

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv = i.uv;
                #if defined(USE_FLIPBOOK)
                    uv = GetFlipbookUV(uv, i.frameIndex);
                #endif

                half4 tex = tex2D(_MainTex, uv);
                half4 vert = i.color;

                half3 rgb = tex.rgb * vert.rgb * _Intensity;
                half  alpha = tex.a * vert.a;

                if (_PremultiplyRGB > 0.5h)
                    rgb *= alpha;

                return half4(rgb, alpha);
            }
            ENDCG
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
