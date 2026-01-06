Shader "Custom/RobotDitherAtmospheric"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}

        _DitherScale ("Dither Scale", Float) = 1
        _DitherIntensity ("Dither Intensity", Range(0,1)) = 0.6

        _MonoColor ("Monochrome Color", Color) = (0.85, 0.95, 0.85, 1)

        _DistortionStrength ("Vertical Distortion", Range(0,0.02)) = 0.006
        _BreathStrength ("Luminance Breathing", Range(0,0.3)) = 0.08

        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.4
        _VignetteSoftness ("Vignette Softness", Range(0.1,3)) = 1.6

        _ShadowPulse ("Shadow Pulse Strength", Range(0,0.3)) = 0.06

        _Instability ("Instability", Range(0,1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _DitherScale;
            float _DitherIntensity;

            float4 _MonoColor;

            float _DistortionStrength;
            float _BreathStrength;

            float _VignetteStrength;
            float _VignetteSoftness;

            float _ShadowPulse;

            float _Instability;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float Dither4x4(float2 p)
            {
                int x = (int)p.x & 3;
                int y = (int)p.y & 3;
                int i = x + y * 4;

                static const float d[16] =
                {
                    0.0,   0.5,   0.125, 0.625,
                    0.75,  0.25,  0.875, 0.375,
                    0.1875,0.6875,0.0625,0.5625,
                    0.9375,0.4375,0.8125,0.3125
                };

                return d[i];
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;

                // --- Vertical industrial distortion
                float wave =
                    sin(i.uv.y * 12 + time * 0.6) *
                    _DistortionStrength *
                    _Instability;

                float2 uv = i.uv;
                uv.x += wave;

                float3 color = tex2D(_MainTex, uv).rgb;

                // --- Luminance
                float luminance = dot(color, float3(0.299, 0.587, 0.114));

                // --- Environmental breathing
                luminance +=
                    sin(time * 0.5) *
                    _BreathStrength *
                    _Instability;

                // --- Peripheral shadow pulsing
                luminance -=
                    sin(time * 0.2) *
                    _ShadowPulse *
                    _Instability;

                luminance = saturate(luminance);

                // --- Dither
                float2 pixelPos = i.uv / _MainTex_TexelSize.xy;
                float threshold = Dither4x4(pixelPos * _DitherScale);
                float dithered = step(threshold, luminance);
                float finalLum = lerp(luminance, dithered, _DitherIntensity);

                // --- Industrial vignette
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float dist = length(centeredUV);
                float vignette =
                    smoothstep(1.0, _VignetteSoftness, dist);

                vignette *= _VignetteStrength * (0.6 + _Instability * 0.4);

                finalLum *= (1.0 - vignette);

                // --- Monochrome tint
                float3 finalColor = finalLum * _MonoColor.rgb;

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}