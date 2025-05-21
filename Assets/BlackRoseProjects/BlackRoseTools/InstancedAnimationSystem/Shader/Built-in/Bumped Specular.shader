Shader "BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Bumped Specular" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
    }

    CGINCLUDE
    sampler2D _MainTex;
    sampler2D _BumpMap;
    fixed4 _Color;
    half _Shininess;

    struct Input {
        float2 uv_MainTex;
        float2 uv_BumpMap;
    };

    void surf (Input IN, inout SurfaceOutput o) {
        fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
        o.Albedo = tex.rgb * _Color.rgb;
        o.Gloss = tex.a;
        o.Alpha = tex.a * _Color.a;
        o.Specular = _Shininess;
        o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
    }
    ENDCG

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhong vertex:animVert
        #include "AnimationInstancingCustom.cginc"
        #pragma shader_feature _ _BONE2 _BONE3 _BONE4
        #pragma multi_compile INSTANCING_NORMAL_TRANSITION_BLENDING
        #pragma target 3.0
        ENDCG
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhong nodynlightmap vertex:animVert
        #include "AnimationInstancingCustom.cginc"
        #if UNITY_VERSION >= 202200 && UNITY_VERSION < 202300 // Fix for error on Unity 2022 causing not inheriting pragmas defined from included cginc's
        #pragma shader_feature _ _BONE2 _BONE3 _BONE4
        #pragma multi_compile INSTANCING_NORMAL_TRANSITION_BLENDING
        #endif
        ENDCG
    }

Fallback "BlackRoseProjects/Built-in/Standard"
}
