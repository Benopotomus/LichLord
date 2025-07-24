Shader "Hidden/BlackRoseProjects/InstancedAnimationSystem/HDRP/CustomEditorOutline"
{
    Properties
    {
        _MainTex("Main Texture", 2DArray) = "grey" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma shader_feature _SCENEVIEW
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    TEXTURE2D_X(_MainTex);
    TEXTURE2D_X(_MainTex2);
    TEXTURE2D(_SelectionBuffer);

    float4 CustomOutline_OLD(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, input.texcoord).xyz;
        float3 color = lerp(sourceColor, Luminance(sourceColor), 1);
        return float4(color, 1);
    } 
        
    float4 CustomOutline(Varyings i) : SV_Target
    {
      #define DIV_SQRT_2 0.70710678118
      #define width 0.002
        float2 directions[8] = {float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1),
            float2(DIV_SQRT_2, DIV_SQRT_2), float2(-DIV_SQRT_2, DIV_SQRT_2),
            float2(-DIV_SQRT_2, -DIV_SQRT_2), float2(DIV_SQRT_2, -DIV_SQRT_2)};

        float aspect = _ScreenParams.x * (_ScreenParams.w - 1);
        float2 sampleDistance = float2(width / aspect, width);

        float maxAlpha = 0;
        for (uint index = 0; index < 8; index++) {
            float2 sampleUV = i.texcoord + directions[index] * sampleDistance;
           
            maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_SelectionBuffer, s_linear_clamp_sampler, sampleUV).a);
        }

        float border = max(0, maxAlpha - SAMPLE_TEXTURE2D(_SelectionBuffer, s_linear_clamp_sampler, i.texcoord).a);
        float4 col2 = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, i.texcoord);
        float4 col = lerp(col2, (1).xxxx, border);
        return col;
    }


    float4 ClearCopy(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, input.texcoord).xyz;
        return float4(sourceColor, 1);
    }

    float4 CopyToMain2(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, input.texcoord).xyz;
        return float4(sourceColor, 1);
    }

    ENDHLSL

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition"
        }
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "CustomOutline"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomOutline
                #pragma vertex Vert
            ENDHLSL
        }

        Pass
        {
            Name "Copy"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment ClearCopy
                #pragma vertex Vert
            ENDHLSL
        }
            Pass
        {
            Name "CopyMain2"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CopyToMain2
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}