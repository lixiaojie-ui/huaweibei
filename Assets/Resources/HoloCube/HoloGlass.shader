Shader "Daima/HoloGlass"
{
    Properties
    {
        _FaceColor ("玻璃面色", Color) = (0.10, 0.22, 0.42, 1)
        _FaceAlpha ("玻璃面不透明度", Range(0, 1)) = 0.55
        _FresnelColor ("边缘辉光颜色", Color) = (0.75, 0.90, 1, 1)
        _FresnelPower ("边缘辉光聚拢", Range(0.5, 8)) = 2.4
        _FresnelIntensity ("边缘辉光强度", Range(0, 5)) = 0.9
        _GridColor ("内部网格颜色", Color) = (0.55, 0.80, 1, 1)
        _GridCell ("网格尺寸（米）", Float) = 0.5
        _GridWidth ("网格线宽", Range(0.001, 0.2)) = 0.006
        _GridIntensity ("网格强度", Range(0, 2)) = 0.32
        _GridCenterScale ("面中心网格衰减", Range(0, 1)) = 0.12
        _CornerFalloff ("角部网格范围", Range(0.1, 1.5)) = 0.55
        _GridScroll ("网格流动速度", Float) = 0.08
        _ScanPeriod ("扫光周期（米）", Float) = 6
        _ScanSpeed ("扫光速度", Float) = 0.35
        _ScanIntensity ("扫光强度", Range(0, 3)) = 0.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }

        // 第一层：深色半透玻璃面，让盒体压得住背景
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
            };

            fixed4 _FaceColor;
            float _FaceAlpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_FaceColor.rgb, _FaceAlpha);
            }
            ENDCG
        }

        // 第二层：边缘辉光、流动网格、扫光与斜向扫带
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 localPos : TEXCOORD3;
            };

            fixed4 _FresnelColor, _GridColor;
            float _FresnelPower, _FresnelIntensity;
            float _GridCell, _GridWidth, _GridIntensity, _GridScroll;
            float _GridCenterScale, _CornerFalloff;
            float _ScanPeriod, _ScanSpeed, _ScanIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos - o.worldPos;
                o.localPos = v.vertex.xyz;
                return o;
            }

            float LineMask(float coord, float halfWidth)
            {
                float g = abs(frac(coord - 0.5) - 0.5);
                float aa = max(fwidth(coord), 0.0001);
                return 1.0 - smoothstep(halfWidth, halfWidth + aa * 1.5, g);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);
                float fres = pow(1.0 - saturate(dot(n, v)), _FresnelPower);

                // 每个面都用该面法线所在的两个世界轴取样，避免盒体侧面网格被拉伸
                float2 gridCoord;
                if (abs(n.y) > 0.5)
                    gridCoord = i.worldPos.xz;
                else if (abs(n.x) > 0.5)
                    gridCoord = i.worldPos.zy;
                else
                    gridCoord = i.worldPos.xy;

                float cell = max(_GridCell, 0.01);
                float2 coord = gridCoord / cell;
                // 网格整体从下往上流动
                coord.y -= _Time.y * _GridScroll;

                float grid = max(LineMask(coord.x, _GridWidth), LineMask(coord.y, _GridWidth));

                // 角部亮、面中心淡：按到最近一个立方体角的距离衰减
                float3 cornerCoord = abs(i.localPos) * 2.0;
                float cornerDist = length(1.0 - cornerCoord) * 0.5773503;
                float cornerMask = 1.0 - smoothstep(0.0, max(_CornerFalloff, 0.01), cornerDist);
                float gridStrength = _GridIntensity * lerp(_GridCenterScale, 1.0, cornerMask);

                // 竖向循环扫光，从下往上
                float period = max(_ScanPeriod, 0.01);
                float scan = 1.0 - saturate(abs(frac((i.worldPos.y - _Time.y * _ScanSpeed) / period) - 0.5) * 2.0);
                scan = pow(scan, 8.0);

                // 合成：边缘辉光 + 网格 + 竖向扫光
                float3 col = _FresnelColor.rgb * fres * _FresnelIntensity;
                col += _GridColor.rgb * grid * gridStrength * (0.4 + 0.6 * fres);
                col += _FresnelColor.rgb * scan * _ScanIntensity;

                float alpha = saturate(0.12 + fres * 0.8 + grid * 0.22 * gridStrength + scan * 0.5 * _ScanIntensity);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
