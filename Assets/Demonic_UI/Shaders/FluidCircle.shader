Shader "UI/Custom/FluideURP_CircleMasked"
{
    Properties
    {
        _MainTex ("First Texture", 2D) = "white" {}
        _Speed1 ("Speed1", Vector) = (0,0,0,0)
        
        _SecondTex ("Second Texture", 2D) = "white" {}
        _Speed2 ("Speed2", Vector) = (0,0,0,0)
        
        _ThirdTex ("Third Texture", 2D) = "white" {}
        _Speed3 ("Speed3", Vector) = (0,0,0,0)
        
        _MainColor ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(1,100)) = 1
        
        _AlphaColor ("Unfilled Part Color", Color) = (0,0,0,0)
        _FillLevel ("Fill Level", Range(0,1)) = 1
        _FadeAreaHeight ("Fade Area Height", Range(0,0.2)) = 0.05
        
        _HotLineColor ("Hot Line Color", Color) = (1,1,1,1)
        _HotLineHeight ("Hot Line Height", Range(0,0.1)) = 0.01
        _HotLineBrightness ("Hot Line Brightness", Range(0,10)) = 1

        _CircleCenter ("Circle Center", Vector) = (0.5, 0.5, 0, 0)
        _CircleRadius ("Circle Radius", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True" 
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _Speed1;

            sampler2D _SecondTex;
            float4 _SecondTex_ST;
            float2 _Speed2;

            sampler2D _ThirdTex;
            float4 _ThirdTex_ST;
            float2 _Speed3;

            fixed4 _AlphaColor;
            fixed4 _HotLineColor;
            fixed4 _MainColor;
            float _FillLevel;
            float _FadeAreaHeight;
            float _HotLineHeight;
            float _Brightness;
            float _HotLineBrightness;

            float4 _CircleCenter;
            float _CircleRadius;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _MainColor;
                return OUT;
            }

            fixed4 ResolveMainColor(float2 uv)
            {
                float fill = lerp(-_FadeAreaHeight, 1.0, _FillLevel);
                fill *= _MainTex_ST.y;
                float filledColor = smoothstep(fill, fill + _FadeAreaHeight, uv.y - _MainTex_ST.w);
                return lerp(_MainColor, _AlphaColor, filledColor);
            }

            fixed4 ProcessTexture(float2 uv, sampler2D tex, float4 st, float2 speed)
            {
                float2 animUV = uv + speed * _Time.y;
                float2 tiledUV = animUV * st.xy + st.zw;
                return tex2D(tex, tiledUV);
            }

            fixed4 HotLineColorFunc(float2 uv)
            {
                fixed4 col = _HotLineColor;
                float alpha = col.a;
                float edge = abs(_FillLevel + _HotLineHeight - uv.y + _MainTex_ST.w);
                float factor = 1.0 - smoothstep(0.0, _HotLineHeight, edge);
                col *= factor * alpha * _HotLineBrightness;
                col.a = alpha * factor;
                return col;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Circle mask
                float dist = distance(IN.uv, _CircleCenter.xy);
                clip(_CircleRadius - dist); // discard outside circle

                fixed4 mainFillColor = ResolveMainColor(IN.uv);
                fixed4 tex1 = ProcessTexture(IN.uv, _MainTex, _MainTex_ST, _Speed1);
                fixed4 tex2 = ProcessTexture(IN.uv, _SecondTex, _SecondTex_ST, _Speed2);
                fixed4 tex3 = ProcessTexture(IN.uv, _ThirdTex, _ThirdTex_ST, _Speed3);

                fixed4 combinedTex = tex1 * tex2 * tex3;

                float resultAlpha = mainFillColor.a;
                float brightnessVal = (mainFillColor.r + mainFillColor.g + mainFillColor.b) / 3.0;

                fixed4 finalColor = combinedTex * mainFillColor;

                finalColor += brightnessVal * HotLineColorFunc(IN.uv);

                finalColor.rgb *= _Brightness;
                finalColor.a = resultAlpha * combinedTex.a;

                finalColor.a *= IN.color.a;

                clip(finalColor.a - 0.01);

                return finalColor;
            }
            ENDCG
        }
    }
}
