// 车间外圈光柱：下宽上窄的柔光束 + 向上流动的亮脉冲
// 用两张交叉的竖直 Quad 渲染，无需贴图
Shader "DarkGround/LightBeam"
{
    Properties
    {
        _EmissionColor ("Beam Color", Color) = (0.098, 0.784, 1.0, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _BaseWidth ("Base Half Width (uv)", Range(0, 0.5)) = 0.18
        _TopWidth ("Top Half Width (uv)", Range(0, 0.5)) = 0.03
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.07
        _FlowSpeed ("Flow Speed", Float) = 0.28
        _FlowWidth ("Flow Width", Range(0.01, 0.5)) = 0.055
        _FlowIntensity ("Flow Intensity", Range(0, 4)) = 1.5
        _Phase ("Phase", Float) = 0
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _EmissionColor;
            float _Intensity, _BaseWidth, _TopWidth, _EdgeSoftness;
            float _FlowSpeed, _FlowWidth, _FlowIntensity, _Phase;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float y = saturate(i.uv.y);
                float x = abs(i.uv.x - 0.5);

                // 下宽上窄：底部的可见宽度约为顶部的 6 倍
                float halfWidth = lerp(_BaseWidth, _TopWidth, y);
                float soft = max(_EdgeSoftness, 0.001);
                float core = 1.0 - smoothstep(halfWidth - soft, halfWidth + soft, x);

                // 顶部收尖并淡出，底部留一点辉光
                float topFade = 1.0 - smoothstep(0.72, 1.0, y);
                float bottomFade = smoothstep(0.0, 0.035, y);
                float fade = topFade * bottomFade;

                // 两个错开的亮脉冲从下往上流动
                float flowPos = frac(_Time.y * _FlowSpeed + _Phase);
                float pulseA = exp(-pow((y - flowPos) / max(_FlowWidth, 0.005), 2.0));
                float pulseB = exp(-pow((y - frac(flowPos + 0.5)) / max(_FlowWidth * 1.6, 0.005), 2.0)) * 0.55;
                float pulse = pulseA + pulseB;

                float3 col = _EmissionColor.rgb * (0.55 + pulse * _FlowIntensity) * _Intensity;
                float alpha = core * fade * (0.42 + pulse * 0.75);

                return float4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
