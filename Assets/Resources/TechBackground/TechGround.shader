Shader "Daima/TechGround"
{
    Properties
    {
        _Color ("Color", Color) = (0.008, 0.025, 0.055, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _Center ("Center", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Float) = 200
    }

    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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

            fixed4 _Color;
            float _Alpha;
            float4 _Center;
            float _Radius;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.worldPos.xz - _Center.xz;
                float dist = length(d);

                float fade = 1.0 - smoothstep(_Radius * 0.55, _Radius, dist);
                float centerGlow = exp(-dist * dist / max(_Radius * _Radius * 0.16, 0.01));
                float3 col = _Color.rgb + _Color.rgb * centerGlow * 0.8;

                return fixed4(col, _Alpha * fade);
            }
            ENDCG
        }
    }

    FallBack Off
}
