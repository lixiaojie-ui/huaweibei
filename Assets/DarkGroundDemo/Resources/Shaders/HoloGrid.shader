// 数字孪生暗场地面：世界坐标双层全息网格 + 相机距离渐隐 + 沿 Z 往返扫描亮带
// Unlit / Transparent / Blend One One / ZWrite Off，不依赖任何贴图
Shader "DarkGround/HoloGrid"
{
    Properties
    {
        _Color ("Grid Color", Color) = (0.098, 0.784, 1.0, 1)
        _CellSize ("Cell Size (m)", Float) = 0.6
        _MajorEvery ("Major Every (cells)", Float) = 5
        _MinorWidth ("Minor Line Width (m)", Float) = 0.012
        _MajorWidth ("Major Line Width (m)", Float) = 0.03
        _MinorIntensity ("Minor Intensity", Range(0, 4)) = 0.55
        _MajorIntensity ("Major Intensity", Range(0, 4)) = 1.35
        _Intensity ("Total Intensity", Range(0, 4)) = 1
        _FadeStart ("Fade Start (m)", Float) = 60
        _FadeEnd ("Fade End (m)", Float) = 100
        _ScanPeriod ("Scan Period (s)", Float) = 15
        _ScanWidth ("Scan Width (m)", Float) = 8
        _ScanIntensity ("Scan Intensity", Range(0, 4)) = 1.6
        _ScanMinZ ("Scan Min Z", Float) = -40
        _ScanMaxZ ("Scan Max Z", Float) = 60
        _FlowIntensity ("Flow Intensity", Range(0, 4)) = 1.2
        _FlowWavelength ("Flow Wavelength (m)", Float) = 22
        _FlowSpeed ("Flow Speed", Float) = 1.8
        _SpotSpacing ("Spot Spacing (m)", Float) = 11
        _SpotSize ("Spot Size (m)", Float) = 0.32
        _SpotIntensity ("Spot Intensity", Range(0, 4)) = 0.9
        _SpotSpeed ("Spot Speed", Float) = 0.22
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Blend One One
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

            float4 _Color;
            float _CellSize, _MajorEvery, _MinorWidth, _MajorWidth;
            float _MinorIntensity, _MajorIntensity, _Intensity;
            float _FadeStart, _FadeEnd;
            float _ScanPeriod, _ScanWidth, _ScanIntensity, _ScanMinZ, _ScanMaxZ;
            float _FlowIntensity, _FlowWavelength, _FlowSpeed;
            float _SpotSpacing, _SpotSize, _SpotIntensity, _SpotSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // halfWidthCells 为线宽的一半（单位：格）；aa 用屏幕导数，保证远处不闪烁
            float GridLine(float2 coord, float halfWidthCells, float2 deriv)
            {
                float2 g = abs(frac(coord - 0.5) - 0.5);
                float2 aa = max(deriv, 1e-4);
                float2 edge = 1.0 - smoothstep(halfWidthCells, halfWidthCells + aa * 1.5, g);
                return max(edge.x, edge.y);
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 worldXZ = i.worldPos.xz;
                float cell = max(_CellSize, 0.01);
                float2 coord = worldXZ / cell;
                float2 deriv = max(fwidth(coord), 1e-4);

                // 细网格在屏幕上密到可能产生摩尔纹时自动淡出，只留主网格
                float density = max(deriv.x, deriv.y);
                float minorLod = 1.0 - smoothstep(0.10, 0.30, density);

                float minor = GridLine(coord, (_MinorWidth / cell) * 0.5, deriv) * minorLod;

                float majorEvery = max(_MajorEvery, 1.0);
                float2 majorCoord = coord / majorEvery;
                float2 majorDeriv = deriv / majorEvery;

                // 掠射角下主网格也做 LOD 淡出，避免地平线附近摩尔纹
                float majorDensity = max(majorDeriv.x, majorDeriv.y);
                float majorLod = 1.0 - smoothstep(0.08, 0.22, majorDensity);
                float major = GridLine(majorCoord, (_MajorWidth / (cell * majorEvery)) * 0.5, majorDeriv) * majorLod;

                // 以相机为圆心的距离渐隐（世界空间，网格不会随相机滑动）
                float camDist = distance(worldXZ, _WorldSpaceCameraPos.xz);
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, camDist);

                // 沿 Z 的三角波扫描带，往返移动，避免循环跳变
                float phase = abs(frac(_Time.y / max(_ScanPeriod, 0.01)) * 2.0 - 1.0);
                float scanZ = lerp(_ScanMinZ, _ScanMaxZ, phase);
                float dz = (i.worldPos.z - scanZ) / max(_ScanWidth, 0.1);
                float scanBand = exp(-dz * dz);

                float3 col = _Color.rgb * (minor * _MinorIntensity + major * _MajorIntensity);

                // 扫描带主要提亮网格线，空隙只留极淡的一层辉光
                float scan = scanBand * _ScanIntensity;
                col += _Color.rgb * (minor + major * 1.2) * scan;
                col += _Color.rgb * scanBand * 0.025;

                // 主网格线上流动的亮斑：波前带一点随机弯曲
                float flowWave = 6.2831853 / max(_FlowWavelength, 1.0);
                float flowTime = _Time.y * _FlowSpeed;
                float flowX = pow(0.5 + 0.5 * sin(i.worldPos.x * flowWave - flowTime
                    + sin(i.worldPos.z * 0.11) * 1.6), 14.0);
                float flowZ = pow(0.5 + 0.5 * sin(i.worldPos.z * flowWave + flowTime * 0.85
                    + sin(i.worldPos.x * 0.09) * 1.6), 14.0);
                float flowD = pow(0.5 + 0.5 * sin((i.worldPos.x + i.worldPos.z) * flowWave * 0.63
                    - flowTime * 1.3 + 1.7), 18.0) * 0.6;
                col += _Color.rgb * major * (flowX + flowZ + flowD) * _FlowIntensity;

                // 网格格子里随机游走的移动光斑
                float spotSpacing = max(_SpotSpacing, 1.0);
                float2 spotCoord = i.worldPos.xz / spotSpacing;
                float2 cellId = floor(spotCoord);
                float2 cellUv = frac(spotCoord) - 0.5;
                float2 rnd = Hash22(cellId);
                float2 orbit = 0.16 * float2(
                    sin(_Time.y * _SpotSpeed * (4.0 + rnd.y * 5.0) + rnd.x * 6.2831853),
                    cos(_Time.y * _SpotSpeed * (3.0 + rnd.x * 4.0) + rnd.y * 6.2831853));
                float2 delta = cellUv - ((rnd - 0.5) * 0.45 + orbit);
                float spotDistance = length(delta) * spotSpacing;
                float spot = exp(-spotDistance * spotDistance / max(_SpotSize * _SpotSize, 0.0001));
                float twinkle = 0.55 + 0.45 * sin(_Time.y * (0.7 + rnd.x * 1.6) + rnd.y * 25.0);
                col += _Color.rgb * spot * _SpotIntensity * twinkle;

                col *= fade * _Intensity;
                return float4(col, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
