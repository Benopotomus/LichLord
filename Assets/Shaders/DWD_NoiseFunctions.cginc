//© Dicewrench Designs LLC 2020-2022
//All Rights Reserved, used with permission
//Last Owned by: Allen White (allen@dicewrenchdesigns.com)


float hash( float2 a )
{
    a  = frac( a*0.3183099+.1 );
    a *= 17.0;
    return frac( a.x*a.y*(a.x+a.y) );
}

float Noise( float2 U )
{
    float2 id = floor( U );
    U = frac( U );
    U *= U * ( 3. - 2. * U );  

    float2 A = float2( hash(id)            , hash(id + float2(0,1)) ); 
    float2 B = float2( hash(id + float2(1,0)), hash(id + float2(1,1)) );  
    float2 C = lerp( A, B, U.x);

    return lerp( C.x, C.y, U.y );
}

float hash3D(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float Noise3D(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(lerp(hash3D(i + float3(0, 0, 0)), hash3D(i + float3(1, 0, 0)), f.x),
             lerp(hash3D(i + float3(0, 1, 0)), hash3D(i + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(hash3D(i + float3(0, 0, 1)), hash3D(i + float3(1, 0, 1)), f.x),
             lerp(hash3D(i + float3(0, 1, 1)), hash3D(i + float3(1, 1, 1)), f.x), f.y), f.z);
}