Shader "Daima/HoloEmissive"
{
    Properties
    {
        _Color ("发光颜色", Color) = (0.35, 0.85, 1, 1)
        _Intensity ("发光强度", Range(0, 8)) = 2.2
        _FresnelBlend ("边缘聚拢（0=均匀，1=仅边缘）", Range(0, 1)) = 0.25
        _FresnelPower ("聚拢指数", Range(0.5, 8)) = 2.5
        _FadeBottom ("淡出起点（局部 Y，0-1）", Range(0, 1)) = 0
        _FadeTop ("淡出终点（局部 Y，0-1）", Range(0, 1)) = 1
        _FadePower ("淡出曲线", Range(0.2, 4)) = 1.4
        _PulseSpeed ("呼吸速度（次/秒）", Float) = 0.35
        _PulseAmount ("呼吸幅度", Range(0, 1)) = 0.15
        _PulsePhase ("呼吸相位", Float) = 0
        _FlowHeight ("上升光带周期（米）", Float) = 4
        _FlowSpeed ("上升光带速度", Float) = 0.12
        _FlowIntensity ("上升光带强度", Range(0, 4)) = 1.2
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float localY : TEXCOORD2;
            };

            fixed4 _Color;
            float _Intensity, _FresnelBlend, _FresnelPower;
            float _FadeBottom, _FadeTop, _FadePower;
            float _PulseSpeed, _PulseAmount, _PulsePhase;
            float _FlowHeight, _FlowSpeed, _FlowIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                // 内置基础网格的局部 Y 在 -0.5~0.5，映射到 0~1 便于做竖向淡出
                o.localY = v.vertex.y + 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fres = pow(1.0 - saturate(dot(n, v)), _FresnelPower);
                float shape = lerp(1.0, fres, _FresnelBlend);

                float top = max(_FadeTop, _FadeBottom + 0.0001);
                float fade = smoothstep(_FadeBottom, top, i.localY);
                fade = pow(saturate(fade), _FadePower);

                float pulse = 1.0 + _PulseAmount * sin((_Time.y * _PulseSpeed + _PulsePhase) * 6.2831853);

                // 世界空间里不断上升的亮带，扫过整个立方体
                float flowPhase = frac(i.worldPos.y / max(_FlowHeight, 0.01) - _Time.y * _FlowSpeed);
                float flow = pow(1.0 - saturate(abs(flowPhase - 0.5) * 2.0), 10.0) * _FlowIntensity;

                float3 col = _Color.rgb * _Intensity * shape * fade * pulse * (1.0 + flow);
                return fixed4(col, saturate(shape * fade * (1.0 + flow)));
            }
            ENDCG
        }
    }

    FallBack Off
}
