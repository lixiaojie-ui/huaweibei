// 悬浮光点：无贴图软圆点，顶点色 × 淡色调，Additive
Shader "DarkGround/ParticleAdditive"
{
    Properties
    {
        _TintColor ("Tint", Color) = (0.80, 0.95, 1.0, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.2
        _UseVertexColor ("Use Vertex Color", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float4 _TintColor;
            float _Intensity;
            float _UseVertexColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 vertexColor = lerp(float4(1, 1, 1, 1), i.color, saturate(_UseVertexColor));

                float2 d = i.uv - 0.5;
                float r = saturate(length(d) * 2.0);
                float soft = 1.0 - r;
                soft *= soft;

                float alpha = soft * vertexColor.a;
                return float4(_TintColor.rgb * vertexColor.rgb * _Intensity, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
