Shader "Daima/TechSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.003, 0.006, 0.012, 1)
        _HorizonColor ("Horizon Color", Color) = (0.018, 0.032, 0.055, 1)
        _BottomColor ("Bottom Color", Color) = (0.002, 0.004, 0.009, 1)
        _GlowColor ("Glow Color", Color) = (0.14, 0.30, 0.48, 1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.30
        _StarDensity ("Star Density", Range(0, 1)) = 0.25
        _StarBrightness ("Star Brightness", Range(0, 3)) = 0.5
        _Vignette ("Vignette", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            fixed4 _TopColor, _HorizonColor, _BottomColor, _GlowColor;
            float _GlowStrength, _StarDensity, _StarBrightness, _Vignette;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float up = dir.y;

                // 上下渐变：顶部近黑，地平线附近偏蓝
                fixed3 col = lerp(_BottomColor.rgb, _HorizonColor.rgb, saturate(up * 3.0 + 0.5));
                col = lerp(col, _TopColor.rgb, saturate(up));

                // 地平线冷光带 + 缓慢起伏的极光
                float band = exp(-abs(up) * 10.0);
                float aurora = sin(dir.x * 1.6 + _Time.y * 0.06)
                             * cos(dir.z * 1.3 - _Time.y * 0.05);
                col += _GlowColor.rgb * band * (0.26 + 0.08 * aurora);

                // 星点
                float3 starCoord = dir * 240.0;
                float rnd = hash13(floor(starCoord));
                float3 f = frac(starCoord) - 0.5;
                float threshold = lerp(0.9996, 0.997, _StarDensity);
                float star = step(threshold, rnd) * smoothstep(0.18, 0.0, length(f));
                float twinkle = 0.65 + 0.35 * sin(_Time.y * (0.5 + rnd * 2.0) + rnd * 50.0);
                col += star * twinkle * _StarBrightness * fixed3(0.78, 0.86, 1.0);

                // 屏幕中心柔光 + 暗角，不随相机朝向移动
                float2 uv = i.pos.xy / _ScreenParams.xy;
                float2 d = uv - 0.5;
                float r = length(d * float2(_ScreenParams.x / _ScreenParams.y, 1.0));

                col += _GlowColor.rgb * exp(-r * r * 6.0) * _GlowStrength;
                col *= 1.0 - _Vignette * smoothstep(0.35, 1.1, r);

                // 轻微抖动，压掉暗部色带
                col += (hash13(float3(i.pos.xy, 1.0)) - 0.5) / 255.0;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
