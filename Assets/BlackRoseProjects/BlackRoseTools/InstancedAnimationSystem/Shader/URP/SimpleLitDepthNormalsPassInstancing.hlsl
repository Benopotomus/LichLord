struct AttributesInstancing
{
    float4 color : COLOR;
    float4 positionOS : POSITION;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float4 texcoord2 : TEXCOORD2;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#include "AnimationInstancingBaseURP.hlsl"

Varyings DepthNormalsVertexInstancing(AttributesInstancing input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    input.positionOS = skinning(input);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
#if defined(_NORMALMAP)
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = half3(NormalizeNormalPerVertex(normalInput.normalWS));
#endif
    return output;
}