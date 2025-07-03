// Made with Amplify Shader Editor v1.9.2.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/WaterRunningSimple"
{
	Properties
	{
		_ColorA("Color A", Color) = (1,1,1,0)
		_ColorB("Color B", Color) = (0,0,0,0)
		_MainTex("Albedo", 2D) = "white" {}
		_MainTex1("Albedo B", 2D) = "white" {}
		_Metalness("Metalness", Float) = 0.5
		_Smoothness("Smoothness", Float) = 0.5
		_BumpMap("Normal A", 2D) = "bump" {}
		_BumpMap1("Normal B", 2D) = "bump" {}
		_NormalScale("NormalScale", Float) = 1
		_Transparency("Transparency", Float) = 1
		_Speed("Speed", Float) = 1
		_SpeedAB("Speed A (XY) B (ZW)", Vector) = (0,1,0,0.5)
		_FoamIntensity("FoamIntensity", Float) = 0
		_FoamDepth("FoamDepth", Float) = 0.9
		_FoamFalloff("FoamFalloff", Float) = -3
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 4.6
		#pragma surface surf Standard alpha:fade keepalpha exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _BumpMap;
		uniform float _Speed;
		uniform float4 _SpeedAB;
		uniform float _NormalScale;
		uniform sampler2D _BumpMap1;
		uniform float4 _ColorA;
		uniform float4 _ColorB;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _FoamIntensity;
		uniform float _FoamDepth;
		uniform float _FoamFalloff;
		uniform sampler2D _MainTex;
		uniform sampler2D _MainTex1;
		uniform float _Metalness;
		uniform float _Smoothness;
		uniform float _Transparency;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float mulTime11 = _Time.y * _Speed;
			float2 appendResult37 = (float2(_SpeedAB.x , _SpeedAB.y));
			float2 panner5 = ( mulTime11 * appendResult37 + i.uv_texcoord);
			float2 appendResult38 = (float2(_SpeedAB.z , _SpeedAB.w));
			float2 panner32 = ( mulTime11 * appendResult38 + i.uv_texcoord);
			o.Normal = BlendNormals( UnpackScaleNormal( tex2D( _BumpMap, panner5 ), _NormalScale ) , UnpackScaleNormal( tex2D( _BumpMap1, panner32 ), _NormalScale ) );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth76 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float4 lerpResult39 = lerp( tex2D( _MainTex, panner5 ) , tex2D( _MainTex1, panner32 ) , float4( 0.5,0,0,0 ));
			float4 lerpResult12 = lerp( _ColorA , _ColorB , ( saturate( pow( ( ( abs( ( eyeDepth76 - ase_screenPos.w ) ) * _FoamIntensity ) + _FoamDepth ) , _FoamFalloff ) ) * (lerpResult39).a ));
			float4 temp_output_13_0 = ( lerpResult12 * lerpResult39 );
			o.Albedo = temp_output_13_0.rgb;
			o.Metallic = saturate( ( (lerpResult39).a * _Metalness ) );
			o.Smoothness = saturate( ( (lerpResult39).a * _Smoothness ) );
			o.Alpha = saturate( ( (temp_output_13_0).a * _Transparency ) );
		}

		ENDCG
	}
	Fallback "DLNK Shaders/ASE/Nature/WaterSimple"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19201
Node;AmplifyShaderEditor.RangedFloatNode;6;-889.3884,260.3493;Inherit;False;Property;_Speed;Speed;10;0;Create;True;0;0;0;False;0;False;1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;34;-1258.388,212.3493;Inherit;False;Property;_SpeedAB;Speed A (XY) B (ZW);11;0;Create;False;0;0;0;False;0;False;0,1,0,0.5;0,1,0,0.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;38;-1041.388,307.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;37;-1040.388,214.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;11;-883.3884,187.3493;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1031,69.5;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;5;-816.3884,71.3493;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;32;-891.3884,375.3493;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-888.3884,507.3493;Inherit;False;Property;_NormalScale;NormalScale;8;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;15;-390.3884,-415.6507;Inherit;False;Property;_ColorB;Color B;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.09776608,0.1056235,0.1226415,0.2980392;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;9;-577.3884,-76.6507;Inherit;True;Property;_MainTex;Albedo;2;0;Create;False;0;0;0;False;0;False;-1;None;f2beae2b45a98c24fb345e38b03d54e8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;14;-607.3884,-414.6507;Inherit;False;Property;_ColorA;Color A;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.3186632,0.8773585,0.8100458,0.5176471;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-580.3884,124.3493;Inherit;True;Property;_MainTex1;Albedo B;3;0;Create;False;0;0;0;False;0;False;-1;None;95384353d7b0dfc4db7e46cfbba172f7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;24;-684.3884,351.3493;Inherit;True;Property;_BumpMap;Normal A;6;0;Create;False;0;0;0;False;0;False;-1;None;97e6f79f1ba439c498ca3807fecba7a5;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-685.3884,552.3493;Inherit;True;Property;_BumpMap1;Normal B;7;0;Create;False;0;0;0;False;0;False;-1;None;abf52b33395d30546aaedcfda7f6c739;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;39;-256.7491,-133.4002;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0.5,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-206.3884,140.3493;Inherit;False;Property;_Transparency;Transparency;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-29.38843,98.3493;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;40;108.2509,127.5998;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;12;-155.1253,-293.1946;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;442,-42;Float;False;True;-1;6;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/Nature/WaterRunningSimple;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;True;0.897;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;DLNK Shaders/ASE/Nature/WaterSimple;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.BlendNormalsNode;31;-367.1401,315.0606;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;92.16173,-173.6543;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-78.77065,404.6665;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;0.5;0.55;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-71.21681,574.0935;Inherit;False;Property;_Metalness;Metalness;4;0;Create;True;0;0;0;False;0;False;0.5;0.16;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;50;-104.6703,492.0778;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;47;-91.72089,324.8089;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;123.0306,494.2362;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;112.4844,394.3579;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;52;117.6348,591.3596;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;45;108.7548,312.4557;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;77;-1306.572,-488.9279;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;74;-1745.626,-457.1516;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenDepthNode;76;-1537.552,-469.1205;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;75;-1521.29,-371.037;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.AbsOpNode;78;-1156.917,-465.5709;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-1036.071,-437.8806;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-1288.445,-382.3698;Inherit;False;Property;_FoamIntensity;FoamIntensity;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;-1028.955,-204.2493;Inherit;False;Property;_FoamDepth;FoamDepth;13;0;Create;True;0;0;0;False;0;False;0.9;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;84;-872.0712,-209.9501;Inherit;False;Property;_FoamFalloff;FoamFalloff;14;0;Create;True;0;0;0;False;0;False;-3;2.16;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;85;-1009.455,-305.5854;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;86;-871.5422,-310.7897;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;87;-665.3613,-218.705;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-459.7753,-218.7928;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;18;-253.1134,36.09409;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;89;-797.5288,-118.2078;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
WireConnection;38;0;34;3
WireConnection;38;1;34;4
WireConnection;37;0;34;1
WireConnection;37;1;34;2
WireConnection;11;0;6;0
WireConnection;5;0;1;0
WireConnection;5;2;37;0
WireConnection;5;1;11;0
WireConnection;32;0;1;0
WireConnection;32;2;38;0
WireConnection;32;1;11;0
WireConnection;9;1;5;0
WireConnection;29;1;32;0
WireConnection;24;1;5;0
WireConnection;24;5;28;0
WireConnection;30;1;32;0
WireConnection;30;5;28;0
WireConnection;39;0;9;0
WireConnection;39;1;29;0
WireConnection;16;0;18;0
WireConnection;16;1;17;0
WireConnection;40;0;16;0
WireConnection;12;0;14;0
WireConnection;12;1;15;0
WireConnection;12;2;88;0
WireConnection;0;0;13;0
WireConnection;0;1;31;0
WireConnection;0;3;52;0
WireConnection;0;4;45;0
WireConnection;0;9;40;0
WireConnection;31;0;24;0
WireConnection;31;1;30;0
WireConnection;13;0;12;0
WireConnection;13;1;39;0
WireConnection;50;0;39;0
WireConnection;47;0;39;0
WireConnection;49;0;50;0
WireConnection;49;1;51;0
WireConnection;46;0;47;0
WireConnection;46;1;48;0
WireConnection;52;0;49;0
WireConnection;45;0;46;0
WireConnection;77;0;76;0
WireConnection;77;1;75;4
WireConnection;76;0;74;0
WireConnection;78;0;77;0
WireConnection;79;0;78;0
WireConnection;79;1;80;0
WireConnection;85;0;79;0
WireConnection;85;1;83;0
WireConnection;86;0;85;0
WireConnection;86;1;84;0
WireConnection;87;0;86;0
WireConnection;88;0;87;0
WireConnection;88;1;89;0
WireConnection;18;0;13;0
WireConnection;89;0;39;0
ASEEND*/
//CHKSM=866B4DB5A946DA62DF74982619473F4657B333AB