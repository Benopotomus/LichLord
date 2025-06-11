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

