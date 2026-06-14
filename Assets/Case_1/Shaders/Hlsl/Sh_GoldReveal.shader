Shader "Case1/Sh_GoldReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Metallic)]
        _MetallicMap ("Metallic Map", 2D) = "white" {}
        _MetallicTint ("Metallic Tint", Color) = (1, 0.85, 0.45, 1)
        _MetallicRepeat ("Metallic Repeat", Vector) = (1, 1, 0, 0)
        _MetallicDrift ("Metallic Drift", Vector) = (0, 0.15, 0, 0)
        _MetallicFollowReveal ("Follow Reveal", Range(0, 2)) = 1
        _MetallicGain ("Metallic Gain", Range(0, 3)) = 1.2
        _MetallicBlend ("Metallic Blend", Range(0, 1)) = 1
        _MetallicFloor ("Detail Floor", Range(0, 0.5)) = 0.08

        [Header(Reveal)]
        _RevealProgress ("Reveal Progress", Range(0, 1)) = 0
        _RevealSoftness ("Reveal Softness", Range(0.001, 0.3)) = 0.04
        _RevealRimColor ("Rim Color", Color) = (1, 0.95, 0.6, 1)
        _RevealRimPower ("Rim Power", Range(0, 1)) = 0.35

        [Header(Grain)]
        [NoScaleOffset] _GrainMap ("Grain Map (Optional)", 2D) = "gray" {}
        _GrainFrequency ("Grain Frequency", Float) = 6
        _GrainImpact ("Grain Impact", Range(0, 0.4)) = 0.1
        _GrainDrift ("Grain Drift", Vector) = (0, 0.05, 0, 0)
        [Toggle] _GrainProcedural ("Procedural Grain", Float) = 1

        [Header(Direction)]
        [Enum(BottomToTop,0,TopToBottom,1,LeftToRight,2,RightToLeft,3)] _RevealAxis ("Reveal Axis", Float) = 0
        _RevealCustomAngle ("Custom Angle", Range(0, 360)) = 0
        [Toggle] _RevealUseCustomAngle ("Use Custom Angle", Float) = 0

        [Header(Playback)]
        [Toggle] _RevealAutoPlay ("Auto Play", Float) = 0
        _RevealPlaySpeed ("Play Speed", Float) = 0.3
        _RevealPingPong ("Ping Pong", Range(0, 1)) = 0

        [Header(Render)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [MaterialToggle] _ZWrite ("Z Write", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "Sh_GoldReveal"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct gold_vert_in
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct gold_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MetallicMap;
            sampler2D _GrainMap;
            fixed4 _Color;
            fixed4 _MetallicTint;
            fixed4 _RevealRimColor;
            float4 _MainTex_ST;
            float4 _MetallicRepeat;
            float4 _MetallicDrift;
            float4 _GrainDrift;

            float _RevealProgress;
            float _RevealSoftness;
            float _RevealRimPower;
            float _MetallicFollowReveal;
            float _MetallicGain;
            float _MetallicBlend;
            float _MetallicFloor;
            float _GrainFrequency;
            float _GrainImpact;
            float _GrainProcedural;
            float _RevealAxis;
            float _RevealCustomAngle;
            float _RevealUseCustomAngle;
            float _RevealAutoPlay;
            float _RevealPlaySpeed;
            float _RevealPingPong;

            float PseudoRand(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float LayeredGrain(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 smooth = f * f * (3.0 - 2.0 * f);

                float a = PseudoRand(i);
                float b = PseudoRand(i + float2(1, 0));
                float c = PseudoRand(i + float2(0, 1));
                float d = PseudoRand(i + float2(1, 1));

                return lerp(lerp(a, b, smooth.x), lerp(c, d, smooth.x), smooth.y);
            }

            float FetchGrain(float2 uv)
            {
                float2 animated = uv + _Time.y * _GrainDrift.xy;

                if (_GrainProcedural > 0.5)
                {
                    float primary = LayeredGrain(animated * _GrainFrequency);
                    float detail = LayeredGrain(animated * _GrainFrequency * 2.13 + 17.3);
                    return (primary + detail * 0.5) / 1.5;
                }

                return tex2D(_GrainMap, animated * _GrainFrequency).r;
            }

            float BuildRevealAxis(float2 uv)
            {
                float2 pivot = uv - 0.5;

                if (_RevealUseCustomAngle > 0.5)
                {
                    float rad = _RevealCustomAngle * 0.0174532925;
                    float sinA = sin(rad);
                    float cosA = cos(rad);
                    return pivot.x * sinA + pivot.y * cosA + 0.5;
                }

                if (_RevealAxis < 0.5) return uv.y;
                if (_RevealAxis < 1.5) return 1.0 - uv.y;
                if (_RevealAxis < 2.5) return uv.x;
                return 1.0 - uv.x;
            }

            float ResolveRevealProgress()
            {
                if (_RevealAutoPlay < 0.5)
                    return saturate(_RevealProgress);

                float t = _Time.y * _RevealPlaySpeed;
                if (_RevealPingPong > 0.5)
                    return abs(frac(t * 0.5) * 2.0 - 1.0);

                return frac(t);
            }

            float2 BuildMetallicUV(float2 uv, float progress)
            {
                float2 mapUV = uv * _MetallicRepeat.xy + _Time.y * _MetallicDrift.xy;
                float travel = (1.0 - progress) * _MetallicFollowReveal;

                if (_RevealAxis < 0.5) mapUV.y -= travel;
                else if (_RevealAxis < 1.5) mapUV.y += travel;
                else if (_RevealAxis < 2.5) mapUV.x -= travel;
                else mapUV.x += travel;

                return mapUV;
            }

            gold_v2f vert(gold_vert_in v)
            {
                gold_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(gold_v2f i) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, i.texcoord) * i.color;

                float alphaEdge = fwidth(source.a) * 1.5;
                float alphaGate = smoothstep(0.0, 0.02 + alphaEdge, source.a);
                clip(source.a - 0.001);

                float progress = ResolveRevealProgress();
                float axis = BuildRevealAxis(i.texcoord);
                float grain = (FetchGrain(i.texcoord) - 0.5) * _GrainImpact;
                float threshold = axis + grain;

                float blendAA = fwidth(threshold) * 2.0;
                float band = max(_RevealSoftness * 0.5, 0.001);

                float metallicWeight = smoothstep(
                    threshold - band - blendAA,
                    threshold + band + blendAA,
                    progress
                );
                metallicWeight = max(metallicWeight, smoothstep(0.97, 1.0, progress));

                float2 metallicUV = BuildMetallicUV(i.texcoord, progress);
                fixed4 metallicSample = tex2D(_MetallicMap, metallicUV);
                float3 metallicRgb = metallicSample.rgb * _MetallicTint.rgb * _MetallicGain;

                float sourceLum = dot(source.rgb, float3(0.2126, 0.7152, 0.0722));
                float3 metallicStyled = metallicRgb * (sourceLum + _MetallicFloor);
                metallicStyled = lerp(source.rgb, metallicStyled, _MetallicBlend);

                float3 painted = lerp(source.rgb, metallicStyled, metallicWeight);

                float rimBand = abs(threshold - progress);
                float rim = 1.0 - saturate(rimBand / (band + blendAA));
                rim = pow(rim, 3.0) * _RevealRimPower * alphaGate;
                rim *= metallicWeight * (1.0 - metallicWeight) * 4.0;

                float3 outputRgb = lerp(painted, _RevealRimColor.rgb, rim);
                return fixed4(outputRgb, source.a);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
