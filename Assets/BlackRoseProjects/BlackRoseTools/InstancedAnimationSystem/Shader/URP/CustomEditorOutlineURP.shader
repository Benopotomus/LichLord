Shader "Hidden/BlackRoseProjects/InstancedAnimationSystem/URP/CustomEditorOutline"
{
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal"
        }

        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex2);
            SAMPLER(sampler_MainTex2);

            TEXTURE2D(_SelectionBuffer);
            SAMPLER(sampler_SelectionBuffer);
            float4 _OutlineColor;

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline half3 GammaToLinearSpace(half3 sRGB)
            {
                return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            float4 frag(Varyings i) : SV_Target
            {
                #define width 0.006
                #define DIV_SQRT_2 0.70710678118
                    float2 directions[8] = {float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1),
                        float2(DIV_SQRT_2, DIV_SQRT_2), float2(-DIV_SQRT_2, DIV_SQRT_2),
                        float2(-DIV_SQRT_2, -DIV_SQRT_2), float2(DIV_SQRT_2, -DIV_SQRT_2)};

                    float aspect = _ScreenParams.x * (_ScreenParams.w - 1);
                    float2 sampleDistance = float2(width / aspect, width);

                    float maxAlpha = 0;
                    for (uint index = 0; index < 8; index++) {
                        float2 sampleUV = i.uv + directions[index] * sampleDistance;
                        maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_SelectionBuffer, sampler_SelectionBuffer, sampleUV).a);
                    }
                    float modelAlpha = SAMPLE_TEXTURE2D(_SelectionBuffer, sampler_SelectionBuffer, i.uv).a;
                    float border = min(1, max(0, maxAlpha - modelAlpha) + modelAlpha * _OutlineColor.a);

                    float4 col2 = SAMPLE_TEXTURE2D(_MainTex2, sampler_MainTex2, i.uv);
#ifndef UNITY_COLORSPACE_GAMMA
                    _OutlineColor.rgb = GammaToLinearSpace(_OutlineColor.rgb);
#endif
                    float4 col = lerp(col2, _OutlineColor, border);

                    return col;
            }
            ENDHLSL
        }
    }
}