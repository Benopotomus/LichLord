Shader "Hidden/BlackRoseProjects/InstancedAnimationSystem/Built-in/CustomEditorOutline"
{
	Properties
	{
		[HideInInspector] _MainTex("Texture", 2D) = "white" {}
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

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
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					float4 vertex : SV_POSITION;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;

				float4 _OutlineColor;
				sampler2D _SelectionBuffer;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					#define DIV_SQRT_2 0.70710678118
					#define width 0.006
					float2 directions[8] = {float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1),
						float2(DIV_SQRT_2, DIV_SQRT_2), float2(-DIV_SQRT_2, DIV_SQRT_2),
						float2(-DIV_SQRT_2, -DIV_SQRT_2), float2(DIV_SQRT_2, -DIV_SQRT_2)};

					float aspect = _ScreenParams.x * (_ScreenParams.w - 1);
					float2 sampleDistance = float2(width / aspect, width);

					float maxAlpha = 0;
					for (uint index = 0; index < 8; index++) {
						float2 sampleUV = i.uv + directions[index] * sampleDistance;
						maxAlpha = max(maxAlpha, tex2D(_SelectionBuffer, sampleUV).a);
					}
					float modelAlpha = tex2D(_SelectionBuffer, i.uv).a;
					float border =min(1, max(0, maxAlpha - modelAlpha)+ modelAlpha* _OutlineColor.a);
					fixed4 col = tex2D(_MainTex, i.uv);
#ifndef UNITY_COLORSPACE_GAMMA
					_OutlineColor.rgb = GammaToLinearSpace(_OutlineColor.rgb);
#endif
					col = lerp(col, _OutlineColor, border);
					return col;
				}
				ENDCG
			}
		}
}