Shader "Holo/GroundGlow"
{
    Properties
    {
        _RingColor     ("光环颜色", Color) = (0.55, 0.85, 1, 1)
        _RingRadius    ("光环半径", Float) = 1.5
        _RingWidth     ("光环宽度", Float) = 0.06
        _RingIntensity ("光环强度", Float) = 6
        _CoreColor     ("核心颜色", Color) = (0.9, 0.96, 1, 1)
        _PoolRadius    ("光池半径", Float) = 2.2
        _PoolIntensity ("光池强度", Float) = 1.2
        _PulseSpeed    ("呼吸速度", Float) = 1.2
        _PulseAmount   ("呼吸幅度", Float) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _RingColor;
            fixed4 _CoreColor;
            float _RingRadius;
            float _RingWidth;
            float _RingIntensity;
            float _PoolRadius;
            float _PoolIntensity;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityWorldToClipPos(o.positionWS);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 用物体原点到像素的世界距离画环，Quad 缩放不影响半径
                float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float r = length(i.positionWS.xz - origin.xz);

                float d = (r - _RingRadius) / max(_RingWidth, 0.0001);
                float ring = exp(-d * d);
                float pool = exp(-(r * r) / (_PoolRadius * _PoolRadius));
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float3 col = _RingColor.rgb * ring * _RingIntensity * pulse
                           + _CoreColor.rgb * pool * _PoolIntensity;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
