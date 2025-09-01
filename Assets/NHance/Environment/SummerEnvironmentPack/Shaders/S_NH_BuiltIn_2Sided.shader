Shader "TwoSided Shader"
{
	Properties
	{
 _Color ("Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 _MainTex ("Albedo", 2D) = "white" { }
 _Cutoff ("Alpha Cutoff", Range(0.000000,1.000000)) = 0.500000
 _Glossiness ("Roughness", Range(0.000000,1.000000)) = 0.500000
 _SpecGlossMap ("Roughness Map", 2D) = "white" { }
[ToggleOff]  _SpecularHighlights ("Specular Highlights", Float) = 1.000000
[ToggleOff]  _GlossyReflections ("Glossy Reflections", Float) = 1.000000
 _BumpScale ("Scale", Float) = 1.000000
 _BumpMap ("Normal Map", 2D) = "bump" { }
 _Parallax ("Height Scale", Range(0.005000,0.080000)) = 0.020000
 _ParallaxMap ("Height Map", 2D) = "black" { }
 _OcclusionStrength ("Strength", Range(0.000000,1.000000)) = 1.000000
 _OcclusionMap ("Occlusion", 2D) = "white" { }
 _EmissionColor ("Color", Color) = (0.000000,0.000000,0.000000,1.000000)
 _EmissionMap ("Emission", 2D) = "white" { }
 _DetailMask ("Detail Mask", 2D) = "white" { }
 _DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" { }
 _DetailNormalMapScale ("Scale", Float) = 1.000000
 _DetailNormalMap ("Normal Map", 2D) = "bump" { }
[Enum(UV0,0,UV1,1)]  _UVSec ("UV Set for secondary textures", Float) = 0.000000
[HideInInspector]  _Mode ("__mode", Float) = 0.000000
[HideInInspector]  _SrcBlend ("__src", Float) = 1.000000
[HideInInspector]  _DstBlend ("__dst", Float) = 0.000000
[HideInInspector]  _ZWrite ("__zw", Float) = 1.000000
}
	CGINCLUDE
	#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG
    SubShader
    {
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 200
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase
			#include "UnityStandardCore.cginc"
			ENDCG
		}
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			Cull Off
			ZTest LEqual
			CGPROGRAM
			#pragma target 2.0
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd
			#include "UnityStandardCore.cginc"

			ENDCG
		}
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual
			CGPROGRAM
			#pragma target 2.0
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster
			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster
			#include "UnityStandardShadow.cginc"
			ENDCG
		}
		Pass
		{
			Name "META"
			Tags { "LightMode"="Meta" }
			Cull Off
			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
    FallBack "Diffuse"
	CustomEditor "StandardShaderGUI"
}
