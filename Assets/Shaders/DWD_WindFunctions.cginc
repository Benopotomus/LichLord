//© Dicewrench Designs LLC 2017-2025
//All Rights Reserved
//Last Owned by: Allen White (allen@dicewrenchdesigns.com)

#define FLT_EPSILON 1.192092896e-07 

sampler2D WIND_SETTINGS_TexNoise;
sampler2D WIND_SETTINGS_TexGust;

// globals
float4  WIND_SETTINGS_WorldDirectionAndSpeed;
float   WIND_SETTINGS_FlexNoiseScale;
float   WIND_SETTINGS_ShiverNoiseScale;
float   WIND_SETTINGS_Turbulence;
float   WIND_SETTINGS_GustSpeed;
float   WIND_SETTINGS_GustScale;
float   WIND_SETTINGS_GustWorldScale;

//globals for wind zones
uniform float4x4 WIND_SETTINGS_Points[4];


float WindPositivePow(float base, float power)
{
    return pow(max(abs(base), float(FLT_EPSILON)), power);
}

float AttenuateTrunk(float x, float s)
{
    float r = (x / s);
    return WindPositivePow(r, 1 / s);
}

float3 WindRotate(float3 pivot, float3 position, float3 rotationAxis, float angle)
{
    rotationAxis = normalize(rotationAxis);
    float3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
    float s = 0.0;
    float c = 0.0;
    sincos(angle, s, c);
    return cpa + ((position - cpa) * c + cross(rotationAxis, (position - cpa)) * s);
}

struct WindData
{
    float3 Direction;
    float Strength;
    float3 ShiverStrength;
    float3 ShiverDirection;
    //float Gust;
};

float3 texNoise(float3 worldPos, float lod)
{
    return tex2Dlod(WIND_SETTINGS_TexNoise, float4(worldPos.xz, 0, lod)).xyz - 0.5.xxx;
}

float texGust(float3 worldPos, float lod)
{
    return tex2Dlod(WIND_SETTINGS_TexGust, float4(worldPos.xz, 0, lod)).x;
}

float3 GetNormalizedDirForPoint(
    inout float3 normalizedDir,
    inout float3 worldOffset,
    inout float3 trunk,
    float3 PivotPosition,
    int index,
    float time)
{
    float4x4 packedPoint = WIND_SETTINGS_Points[index];

    float3 pointPos = packedPoint._m00_m01_m02;
    float3 pointRot = packedPoint._m10_m11_m12;
    float radius = packedPoint._m03;
    float main = packedPoint._m30;
    float turb = packedPoint._m31;
    float mag = packedPoint._m32;
    float freq = packedPoint._m33;

    //if the zone is directional we set the radius to -1.0f
    //so we can make a quick check and avoid branching
    float radCheck = saturate(radius * -1.0f);

    //if we're spherical we need to radiate out from the pos
    pointRot = lerp(normalize(PivotPosition - pointPos), pointRot, radCheck);
    float intensity = lerp(1.0 - saturate(distance(pointPos, PivotPosition) / radius), 1.0f, radCheck);
    float3 outputDir = normalize(pointRot) * intensity;
    normalizedDir += outputDir * main;

    float pulse = time * freq * turb;
    worldOffset += outputDir * main * time + (pulse * mag);
    trunk += outputDir * mag * pulse;
    return outputDir;
}

WindData GetAnalyticalWind(float3 WorldPosition, float3 PivotPosition, float drag, float shiverDrag, float initialBend, float4 time)
{
    WindData result;

    float3 normalizedDir = normalize(WIND_SETTINGS_WorldDirectionAndSpeed.xyz) * WIND_SETTINGS_WorldDirectionAndSpeed.w;
    float3 worldOffset = normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w * time.y;
    float3 gustWorldOffset = normalizedDir * WIND_SETTINGS_GustSpeed * time.y;

    GetNormalizedDirForPoint(normalizedDir, worldOffset, gustWorldOffset, PivotPosition, 0, time.y);
    GetNormalizedDirForPoint(normalizedDir, worldOffset, gustWorldOffset, PivotPosition, 1, time.y);
    GetNormalizedDirForPoint(normalizedDir, worldOffset, gustWorldOffset, PivotPosition, 2, time.y);
    GetNormalizedDirForPoint(normalizedDir, worldOffset, gustWorldOffset, PivotPosition, 3, time.y);

    // Trunk noise is base wind + gusts + noise

    float3 trunk = float3(0, 0, 0);

    if (WIND_SETTINGS_WorldDirectionAndSpeed.w > 0.0 || WIND_SETTINGS_Turbulence > 0.0)
    {
        trunk = texNoise((PivotPosition - worldOffset) * WIND_SETTINGS_FlexNoiseScale, 3);
    }

    float gust = 0.0;

    if (WIND_SETTINGS_GustSpeed > 0.0)
    {
        gust = texGust((PivotPosition - gustWorldOffset) * WIND_SETTINGS_GustWorldScale, 3);
        gust = pow(gust, 2) * WIND_SETTINGS_GustScale;
    }

    float3 trunkNoise =
        (
            (normalizedDir)
            +(gust * normalizedDir * WIND_SETTINGS_GustSpeed)
            + (trunk * WIND_SETTINGS_Turbulence)
            ) * drag;

    // Shiver Noise
    float3 shiverNoise = texNoise((WorldPosition - worldOffset) * WIND_SETTINGS_ShiverNoiseScale, 0) * shiverDrag * WIND_SETTINGS_Turbulence;

    float3 dir = trunkNoise;
    float flex = length(trunkNoise) + initialBend;
    float shiver = length(shiverNoise);

    result.Direction = dir;
    result.ShiverDirection = shiverNoise;
    result.Strength = flex;
    result.ShiverStrength = shiver + shiver * gust;
    //result.Gust = (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
    //  + (trunk * WIND_SETTINGS_Turbulence);

    return result;
}

void ApplyWindDisplacement(
    inout float3    positionWS,
    inout WindData    windData,
    float3          normalWS,
    float3          rootWP,
    float           stiffness,
    float           drag,
    float           shiverDrag,
    float           initialBend,
    float           shiverMask,
    float4          time)
{
    WindData wind = GetAnalyticalWind(positionWS, rootWP, drag, shiverDrag, initialBend, time);

    if (wind.Strength > 0.0)
    {
        float att = AttenuateTrunk(distance(positionWS, rootWP), stiffness);
        float3 rotAxis = cross(float3(0, 1, 0), wind.Direction);

        positionWS = WindRotate(rootWP, positionWS, rotAxis, (wind.Strength) * 0.001 * att);

        float3 shiverDirection = normalize(lerp(normalWS, normalize(wind.Direction + wind.ShiverDirection), 0));
        positionWS += wind.ShiverStrength * shiverDirection * shiverMask;
    }
    windData = wind;

}

float3 WindTransformObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return UnityObjectToWorldDir(normalOS);
#else
    return normalize(mul(normalOS, (float3x3)GetWorldToObjectMatrix()));
#endif
}