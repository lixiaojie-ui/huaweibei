Shader "Holo/HoloCone"
{
    Properties
    {
        _ColorTop    ("顶部颜色", Color) = (0.10, 0.22, 0.55, 1)
        _ColorBottom ("底部颜色", Color) = (0.55, 0.78, 1.00, 1)
        _Intensity   ("亮度", Float) = 1.15
        _RimPower    ("边缘衰减", Float) = 2.0
        _RimStrength ("边缘强度", Float) = 0.85
        _Height      ("锥体高度", Float) = 5
        _GradPower   ("高度渐变", Float) = 1.8
        _BottomGlow  ("底部辉光", Float) = 1.0
        _NoiseScale  ("雾尺度", Float) = 1.1
        _NoiseSpeed  ("雾流速", Float) = 0.10
        _FogStrength ("雾浓度", Float) = 0.80
        _Alpha       ("基础不透明度", Float) = 0.80
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            // 预乘 alpha：既保留自发光，又能遮住背后的物体
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _ColorTop;
            fixed4 _ColorBottom;
            float _Intensity;
            float _RimPower;
            float _RimStrength;
            float _Height;
            float _GradPower;
            float _BottomGlow;
            float _NoiseScale;
            float _NoiseSpeed;
            float _FogStrength;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash13(i);
                float b = hash13(i + float3(1.0, 0.0, 0.0));
                float c = hash13(i + float3(0.0, 1.0, 0.0));
                float d = hash13(i + float3(1.0, 1.0, 0.0));
                float e = hash13(i + float3(0.0, 0.0, 1.0));
                float g = hash13(i + float3(1.0, 0.0, 1.0));
                float h = hash13(i + float3(0.0, 1.0, 1.0));
                float k = hash13(i + float3(1.0, 1.0, 1.0));

                return lerp(lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                            lerp(lerp(e, g, f.x), lerp(h, k, f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    v += amp * noise3(p);
                    p = p * 2.07 + 13.7;
                    amp *= 0.5;
                }
                return v / 0.875;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.positionOS = v.vertex.xyz;
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityWorldToClipPos(o.positionWS);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);

                // 物体空间高度：0 = 底部，1 = 顶部
                float h = saturate(i.positionOS.y / max(_Height, 0.001));
                float grad = pow(1.0 - h, _GradPower);

                // 缓慢上升的体积雾
                float3 np = i.positionWS * _NoiseScale;
                np.y -= _Time.y * _NoiseSpeed;
                float fog = fbm(np) * _FogStrength;

                float bottomGlow = exp(-h * 6.0) * _BottomGlow;

                float3 col = lerp(_ColorTop.rgb, _ColorBottom.rgb, grad);
                col += _ColorBottom.rgb * bottomGlow;

                float alpha = _Alpha * grad
                            + fresnel * _RimStrength
                            + fog * 0.6
                            + bottomGlow * 0.4;

                // 顶部渐隐，避免出现硬边
                alpha *= saturate(1.0 - smoothstep(0.78, 1.0, h));

                return fixed4(col * alpha * _Intensity, alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
