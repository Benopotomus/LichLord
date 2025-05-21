Shader "Hidden/BlackRoseProjects/Build-in/RimEffect"
{
    Properties
    {
        _Color("Rim Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal  : TEXCOORD1;
                float3 viewDirection : TEXCOORD2;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(v.normal);
                o.viewDirection = normalize(WorldSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float NdotV = 1 - saturate(dot(i.normal, i.viewDirection));
                float rim = pow(NdotV, 0.25);
                fixed4 col = rim * _Color;

                return col;
            }
            ENDCG
        }
    }
}