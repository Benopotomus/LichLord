//© Dicewrench Designs LLC 2017-2024
//Licensed for use in 'Music Visualizer' App
//All Rights Reserved
//Last Owned by: Allen White (allen@dicewrenchdesigns.com)


half3 ApplyOverlay(half3 base, half3 overlay)
{
	return (base + (base * ( (2.0.xxx * overlay - 1.0.xxx)) * (1.0.xxx - base)));
}

half Hash( half2 a )
{
    a  = frac( a*0.3183099+.1 );
    a *= 17.0;
    return frac( a.x*a.y*(a.x+a.y) );
}

half Noise( half2 U )
{
    half2 id = floor( U );
    U = frac( U );
    U *= U * ( 3. - 2. * U );  

    half2 A = half2( Hash(id), Hash(id + half2(0,1)) ); 
    half2 B = half2( Hash(id + half2(1,0)), Hash(id + half2(1,1)) );  
    half2 C = lerp( A, B, U.x);

    return lerp( C.x, C.y, U.y );
}

half3 Greyscale(half3 base, half amount)
{
    half3 grey = half3(0.2125, 0.7154, 0.0721);
    return lerp(base,dot(base.rgb, grey), amount);
}

half GetMix(half4 base, half4 mixer)
{
	half scale = mixer.r + mixer.g + mixer.b + mixer.a;
	half blend = (base.r * mixer.r) + (base.g * mixer.g) + (base.b * mixer.b) + (base.a * mixer.a);
	blend /= scale;
	return saturate(blend);
}

half GetMix(half3 base, half4 mixer)
{
	half scale = mixer.r + mixer.g + mixer.b + mixer.a;
	half blend = (base.r * mixer.r) + (base.g * mixer.g) + (base.b * mixer.b);
	blend /= scale;
	return saturate(blend);
}

half GetMix(half3 base, half3 mixer)
{
	half scale = mixer.r + mixer.g + mixer.b;
	half blend = (base.r * mixer.r) + (base.g * mixer.g) + (base.b * mixer.b);
	blend /= scale;
	return saturate(blend);
}

half ZeroOneZero(half grad)
{
    return 1.0 - saturate(abs((grad * 2.0) - 1.0));
}

half2 ZeroOneZero(half2 grad)
{
    return 1.0 - saturate(abs((grad * 2.0) - 1.0));
}

half3 Saturation(half3 base, half amount)
{
	half3 grey = half3(0.2125, 0.7154, 0.0721);
    half3 greyscale = dot(base, grey);
    return lerp(greyscale, base, amount);
}

half Wrap(half ramp, half wrap)
{
	return lerp(ramp, ramp * 0.5 + 0.5, wrap);
}

half Rim(half3 surfaceNormal, half3 tangentSpaceViewDir)
{
	return 1.0 - saturate(dot(surfaceNormal.xyz, tangentSpaceViewDir.xyz));
}

half Edge(half ramp, half thickness, half thickPower, half thickBoost)
{
	half thickRamp = saturate(ramp * (1.0 + thickness));
	half baseRamp = saturate(thickRamp + thickness);
	half invRamp = 1.0 - saturate(thickRamp - thickness);
	return saturate( pow(abs(baseRamp * invRamp), thickPower) * thickBoost);
}

half3 ColorizeApproximation(half3 color, half ramp, half rim, half edge, half falloff, half power, half boost, half mask)
{
	return color * saturate( (pow(abs(rim - falloff), power) * boost) * mask * (1.0 - ramp) + edge).xxx;
}

half4 BlendHeight(half3 colorOne, half heightOne, half3 colorTwo, half heightTwo, half Noise, half blend)
{
	heightOne *= (1.0 - Noise);
	heightTwo *= Noise;
	half s = max(heightOne, heightTwo) - blend;
	half levelOne = max(heightOne - s, 0.0);
	half levelTwo = max(heightTwo - s, 0.0);
	half3 balance = (colorOne * levelOne.xxx) + (colorTwo * levelTwo.xxx);
	half height = (levelOne + levelTwo);
	balance /= height.xxx;
	return half4(balance, height);
}

half4 BlendHeight(half3 colorOne, half heightOne, half3 colorTwo, half heightTwo, half Noise, half blend, inout half2 mask)
{
	heightOne *= (1.0 - Noise);
	heightTwo *= Noise;
	half s = max(heightOne, heightTwo) - blend;
	half levelOne = max(heightOne - s, 0.0);
	half levelTwo = max(heightTwo - s, 0.0);
	mask = saturate(half2(levelOne, levelTwo));
	half3 balance = (colorOne * levelOne.xxx) + (colorTwo * levelTwo.xxx);
	half height = (levelOne + levelTwo);
	balance /= height.xxx;
	return half4(balance, height);
}

float ThreeSixtyAngle(float angle)
{
   return angle * 0.01745399;
}

float2 ComputePivotRotation(float2 baseUV, float angle, float2 pivot)
{
   float a = ThreeSixtyAngle(angle);
   float rot_cos;
   float rot_sin;
   sincos(a, rot_sin, rot_cos);
   float2x2 rotationMatrix = float2x2(rot_cos, -rot_sin, rot_sin, rot_cos);
   baseUV -= pivot;
   float2 newUV = mul(baseUV, rotationMatrix).xy;
   return newUV  + pivot;
}

float2 ComputeRotatedUV(float2 baseUV, float angle)
{
   return ComputePivotRotation(baseUV, angle, float2(0.5,0.5));
} 

//Take a UV and create a Box Mask around the edges
half VignetteBoxMask(half2 uv, half fill, half size, half power, half boost, half alpha)
{
   fill *= 2.0;
   half left = uv.x - size;
   half right = (1.0 - uv.x) - size;
   half bottom = uv.y - size;
   half top = (1.0 - uv.y - size);

   half mask = min(min(min(left,right),bottom),top);
   mask = pow(saturate(mask),power) * fill;
   mask *= boost;
   return saturate(mask) * alpha;
}

//Take a UV and create a Circle Mask around the edges
half VignetteCircleMask(half2 uv, half size, half power, half boost, half alpha)
{
   half dist = distance(uv, half2(0.5,0.5));
   dist += size;
   dist = pow(dist, power);
   dist *= boost;
   return saturate(dist) * alpha;
}

float3 RotateAroundYInDegrees(float3 vertex, float degrees)
{
	float alpha = degrees * 3.1415926 / 180.0;
	float sina, cosa;
	sincos(alpha, sina, cosa);
	float2x2 m = float2x2(cosa, -sina, sina, cosa);
	return float3(mul(m, vertex.xz).xy, vertex.y).xzy;
}

float3 Spin(float3 vertPos, float time, float rate, float rotation)
{
	float spin = time * rate;
	return RotateAroundYInDegrees(vertPos, rotation + spin);
}