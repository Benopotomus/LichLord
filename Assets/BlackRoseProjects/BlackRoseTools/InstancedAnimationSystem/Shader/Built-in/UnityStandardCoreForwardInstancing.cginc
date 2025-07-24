// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_STANDARD_CORE_FORWARD_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_INCLUDED

#if defined(UNITY_NO_FULL_STANDARD_SHADER)
#   define UNITY_STANDARD_SIMPLE 1
#endif

#include "UnityStandardConfig.cginc"

#if UNITY_STANDARD_SIMPLE
  
    #include "UnityStandardCoreForwardSimple.cginc"
    #include "UnityStandardCoreForwardSimpleInstancing.cginc"
    VertexOutputBaseSimple vertBaseInstancing (VertexInputInstancing v) { return vertForwardBaseSimpleInstancing(v); }
    VertexOutputForwardAddSimple vertAddInstancing (VertexInputInstancing v) { return vertForwardAddSimpleInstancing(v); }
    half4 fragBase (VertexOutputBaseSimple i) : SV_Target { return fragForwardBaseSimpleInternal(i); }
    half4 fragAdd (VertexOutputForwardAddSimple i) : SV_Target { return fragForwardAddSimpleInternal(i); }
#else
  
    #include "UnityStandardCore.cginc"
    #include "UnityStandardCoreInstancing.cginc"
    VertexOutputForwardBase vertBaseInstancing (VertexInputInstancing v) { return vertForwardBaseInstancing(v); }
    VertexOutputForwardAdd vertAddInstancing (VertexInputInstancing v) { return vertForwardAddInstancing(v); }
    half4 fragBase (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternal(i); }
    half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddInternal(i); }
#endif

#endif // UNITY_STANDARD_CORE_FORWARD_INCLUDED
