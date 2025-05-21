#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct AttributesInstancing
{
    float4 color        : Color;
    float4 positionOS     : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float4 texcoord2     : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#include "AnimationInstancingBaseURP.hlsl"

Varyings DepthOnlyVertexInstancing(AttributesInstancing input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    input.positionOS = skinning(input);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}