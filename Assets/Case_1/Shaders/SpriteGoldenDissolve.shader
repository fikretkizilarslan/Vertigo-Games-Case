Shader "Case1/Sprite/GoldenDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Gold Texture)]
        _GoldTex ("Gold Texture", 2D) = "white" {}
        _GoldTint ("Gold Tint", Color) = (1, 0.85, 0.45, 1)
        _GoldTiling ("Gold Tiling", Vector) = (1, 1, 0, 0)
        _GoldScroll ("Gold Scroll Speed", Vector) = (0, 0.15, 0, 0)
        _GoldRiseAmount ("Gold Rise With Dissolve", Range(0, 2)) = 1
        _GoldBrightness ("Gold Brightness", Range(0, 3)) = 1.2
        _GoldOverlay ("Gold Overlay Strength", Range(0, 1)) = 1
        _GoldMinLum ("Gold Min Luminance", Range(0, 0.5)) = 0.08

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Softness", Range(0.001, 0.3)) = 0.04
        _EdgeColor ("Edge Glow Color", Color) = (1, 0.95, 0.6, 1)
        _EdgeIntensity ("Edge Glow Intensity", Range(0, 1)) = 0.35

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Noise Texture (Optional)", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 6
        _NoiseStrength ("Noise Strength", Range(0, 0.4)) = 0.1
        _NoiseSpeed ("Noise Scroll Speed", Vector) = (0, 0.05, 0, 0)
        [Toggle] _UseProceduralNoise ("Use Procedural Noise", Float) = 1

        [Header(Direction)]
        [Enum(BottomToTop,0,TopToBottom,1,LeftToRight,2,RightToLeft,3)] _Direction ("Dissolve Direction", Float) = 0
        _DirectionAngle ("Custom Angle (Degrees)", Range(0, 360)) = 0
        [Toggle] _UseCustomAngle ("Use Custom Angle", Float) = 0

        [Header(Animation)]
        [Toggle] _AutoAnimate ("Auto Animate", Float) = 0
        _AnimateSpeed ("Animate Speed", Float) = 0.3
        _AnimatePingPong ("Ping Pong (0=Loop, 1=PingPong)", Range(0, 1)) = 0

        [Header(Rendering)]
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
            Name "SpriteGoldenDissolve"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _GoldTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            fixed4 _GoldTint;
            fixed4 _EdgeColor;
            float4 _MainTex_ST;
            float4 _GoldTex_ST;
            float4 _GoldTiling;
            float4 _GoldScroll;
            float4 _NoiseSpeed;

            float _DissolveAmount;
            float _EdgeWidth;
            float _EdgeIntensity;
            float _GoldRiseAmount;
            float _GoldBrightness;
            float _GoldOverlay;
            float _GoldMinLum;
            float _NoiseScale;
            float _NoiseStrength;
            float _UseProceduralNoise;
            float _Direction;
            float _DirectionAngle;
            float _UseCustomAngle;
            float _AutoAnimate;
            float _AnimateSpeed;
            float _AnimatePingPong;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float SampleNoise(float2 uv)
            {
                float2 scrollUV = uv + _Time.y * _NoiseSpeed.xy;

                if (_UseProceduralNoise > 0.5)
                {
                    float n = ValueNoise(scrollUV * _NoiseScale);
                    n += ValueNoise(scrollUV * _NoiseScale * 2.13 + 17.3) * 0.5;
                    return n / 1.5;
                }

                return tex2D(_NoiseTex, scrollUV * _NoiseScale).r;
            }

            float GetDissolveAxis(float2 uv)
            {
                float2 centered = uv - 0.5;

                if (_UseCustomAngle > 0.5)
                {
                    float rad = _DirectionAngle * 0.0174532925;
                    float cosA = cos(rad);
                    float sinA = sin(rad);
                    return centered.x * sinA + centered.y * cosA + 0.5;
                }

                if (_Direction < 0.5)
                    return uv.y;
                if (_Direction < 1.5)
                    return 1.0 - uv.y;
                if (_Direction < 2.5)
                    return uv.x;

                return 1.0 - uv.x;
            }

            float GetDissolveAmount()
            {
                if (_AutoAnimate < 0.5)
                    return saturate(_DissolveAmount);

                float t = _Time.y * _AnimateSpeed;
                if (_AnimatePingPong > 0.5)
                    return abs(frac(t * 0.5) * 2.0 - 1.0);

                return frac(t);
            }

            float2 GetGoldUV(float2 uv, float dissolveAmount)
            {
                float2 goldUV = uv * _GoldTiling.xy;
                goldUV += _Time.y * _GoldScroll.xy;

                // Altin texture dissolve ile birlikte yukari kayar
                if (_Direction < 0.5)
                    goldUV.y -= (1.0 - dissolveAmount) * _GoldRiseAmount;
                else if (_Direction < 1.5)
                    goldUV.y += (1.0 - dissolveAmount) * _GoldRiseAmount;
                else if (_Direction < 2.5)
                    goldUV.x -= (1.0 - dissolveAmount) * _GoldRiseAmount;
                else
                    goldUV.x += (1.0 - dissolveAmount) * _GoldRiseAmount;

                return goldUV;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.texcoord) * i.color;

                // Sprite alpha sinirinda sert kesim yerine yumusak anti-alias
                float alpha = sprite.a;
                float alphaAA = fwidth(alpha) * 1.5;
                float alphaMask = smoothstep(0.0, 0.02 + alphaAA, alpha);
                clip(alpha - 0.001);

                float dissolveAmount = GetDissolveAmount();
                float axis = GetDissolveAxis(i.texcoord);
                float noise = (SampleNoise(i.texcoord) - 0.5) * _NoiseStrength;
                float dissolveValue = axis + noise;

                // Derivative tabanli yumusak gecis
                float edgeAA = fwidth(dissolveValue) * 2.0;
                float edgeHalf = max(_EdgeWidth * 0.5, 0.001);

                // dissolveAmount bu pikseli gectiyse altin — alttan yukari dolar
                float goldAmount = smoothstep(
                    dissolveValue - edgeHalf - edgeAA,
                    dissolveValue + edgeHalf + edgeAA,
                    dissolveAmount
                );
                // %100'de tum karakter altin kalsin, kaybolmasin
                goldAmount = max(goldAmount, smoothstep(0.97, 1.0, dissolveAmount));

                // Altin texture — sprite detaylari korunarak kaplama
                float2 goldUV = GetGoldUV(i.texcoord, dissolveAmount);
                fixed4 goldSample = tex2D(_GoldTex, goldUV);
                float3 goldMap = goldSample.rgb * _GoldTint.rgb * _GoldBrightness;

                // Sprite luminance'i koru: yuz, sac, kiyafet detaylari kaybolmaz
                float spriteLum = dot(sprite.rgb, float3(0.2126, 0.7152, 0.0722));
                float3 goldStyled = goldMap * (spriteLum + _GoldMinLum);
                goldStyled = lerp(sprite.rgb, goldStyled, _GoldOverlay);

                float3 baseColor = lerp(sprite.rgb, goldStyled, goldAmount);

                // Ince kenar pariltisi — sadece gecis bandinda
                float edgeDist = abs(dissolveValue - dissolveAmount);
                float rim = 1.0 - saturate(edgeDist / (edgeHalf + edgeAA));
                rim = pow(rim, 3.0) * _EdgeIntensity * alphaMask;
                rim *= goldAmount * (1.0 - goldAmount) * 4.0;
                float3 finalColor = lerp(baseColor, _EdgeColor.rgb, rim);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
