Shader "Daima/TechGrid"
{
    Properties
    {
        _Color ("Line Color", Color) = (0.16, 0.42, 0.75, 1)
        _MajorColor ("Major Line Color", Color) = (0.45, 0.75, 1.0, 1)
        _FlowColor ("Flow Color", Color) = (0.55, 0.85, 1.0, 1)
        _CellSize ("Cell Size", Float) = 4
        _MajorEvery ("Major Every", Float) = 5
        _LineWidth ("Line Width", Range(0.001, 0.5)) = 0.02
        _FadeDistance ("Fade Distance", Float) = 400
        _Intensity ("Intensity", Range(0, 3)) = 1
        _FlowSpacing ("Flow Spacing (cells)", Float) = 24
        _FlowSpeed ("Flow Speed (cycles/s)", Float) = 0.09
        _FlowRadius ("Flow Radius", Float) = 100
        _Center ("Center", Vector) = (0, 0, 0, 0)
        _GlowRadius ("Glow Radius", Float) = 100
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _Color, _MajorColor, _FlowColor;
            float _CellSize, _MajorEvery, _LineWidth, _FadeDistance, _Intensity;
            float _FlowSpacing, _FlowSpeed;
            float _FlowRadius;
            float4 _Center;
            float _GlowRadius, _GlowIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float LineMask(float coord, float halfWidth)
            {
                float g = abs(frac(coord - 0.5) - 0.5);
                float aa = max(fwidth(coord), 0.0001);
                return 1.0 - smoothstep(halfWidth, halfWidth + aa * 1.5, g);
            }

            // 沿网格线移动的光流：头部亮、拖尾渐隐
            float FlowPulse(float along, float phase)
            {
                float f = frac(along - _Time.y * _FlowSpeed + phase);
                return pow(saturate(1.0 - f), 22.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 worldXZ = i.worldPos.xz;
                float cell = max(_CellSize, 0.01);
                float2 coord = worldXZ / cell;
                float width = _LineWidth;

                // 普通网格线与主线
                float minorX = LineMask(coord.y, width);
                float minorZ = LineMask(coord.x, width);
                float minor = max(minorX, minorZ);

                float2 majorCoord = coord / max(_MajorEvery, 1.0);
                float majorX = LineMask(majorCoord.y, width * 1.8);
                float majorZ = LineMask(majorCoord.x, width * 1.8);
                float major = max(majorX, majorZ);

                // 距离衰减
                float2 d2 = worldXZ - _Center.xz;
                float dist = length(d2);
                float fade = 1.0 - saturate(dist / max(_FadeDistance, 1.0));
                fade = pow(fade, 1.6);

                float3 col = _Color.rgb * minor;
                col += _MajorColor.rgb * major * 1.6;
                float alpha = (minor * 0.55 + major * 1.3) * _Intensity * fade;

                // 流光只出现在一部分线上，避免整片一起闪
                float lineIdZ = floor(coord.y + 0.5);
                float lineIdX = floor(coord.x + 0.5);
                float gateZ = step(0.50, hash11(lineIdZ * 1.37 + 11.0));
                float gateX = step(0.50, hash11(lineIdX * 2.11 + 37.0));

                float flowUnit = cell * max(_FlowSpacing, 1.0);
                float pulseX = FlowPulse(worldXZ.x / flowUnit, hash11(lineIdZ * 3.1) * 7.0)
                             * minorX * gateZ;
                float pulseZ = FlowPulse(worldXZ.y / flowUnit, hash11(lineIdX * 5.7) * 7.0)
                             * minorZ * gateX;
                // 流光只保留在车间附近
                float flowFade = 1.0 - smoothstep(_FlowRadius * 0.65, _FlowRadius, dist);
                float pulse = (pulseX + pulseZ) * fade * flowFade;

                col += _FlowColor.rgb * pulse * 2.4;
                alpha += pulse * 1.5;

                // 中心柔光
                float glow = exp(-(dist * dist) / max(_GlowRadius * _GlowRadius, 0.01));
                col += _Color.rgb * glow * _GlowIntensity;
                alpha += glow * _GlowIntensity * 0.5 * fade;

                return fixed4(col, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack Off
}
