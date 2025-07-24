sampler2D _boneTexture;
int _boneTextureBlockWidth;
int _boneTextureBlockHeight;
int _boneTextureWidth;
int _boneTextureHeight;

int _blockCount;
int _matCount;

#pragma warning (disable : 3556)
#pragma warning (disable : 3568)
#pragma shader_feature _ _BONE2 _BONE3 _BONE4
#pragma multi_compile INSTANCING_NORMAL_TRANSITION_BLENDING

UNITY_INSTANCING_BUFFER_START(AnimationProp)
UNITY_DEFINE_INSTANCED_PROP(float, preFrameIndex)
UNITY_DEFINE_INSTANCED_PROP(float, frameIndex)
UNITY_DEFINE_INSTANCED_PROP(float, transitionProgress)
UNITY_INSTANCING_BUFFER_END(AnimationProp)

half4x4 loadMatFromTexture(uint frameIndex, uint boneIndex)
{
	int2 uv;
	uv.y = frameIndex / _blockCount * _boneTextureBlockHeight;
	uv.x = _boneTextureBlockWidth * (frameIndex - _boneTextureWidth / _boneTextureBlockWidth * uv.y);

	uv.x = uv.x + (boneIndex % _matCount) * 4;
	uv.y = uv.y + boneIndex / _matCount;

	float2 uvFrame;
	uvFrame.x = uv.x / (float)_boneTextureWidth;
	uvFrame.y = uv.y / (float)_boneTextureHeight;
	half4 uvf = half4(uvFrame, 0, 0);

	float offset = 1.0f / (float)_boneTextureWidth;
	half4 c1 = tex2Dlod(_boneTexture, uvf);
	uvf.x = uvf.x + offset;
	half4 c2 = tex2Dlod(_boneTexture, uvf);
	uvf.x = uvf.x + offset;
	half4 c3 = tex2Dlod(_boneTexture, uvf);
	uvf.x = uvf.x + offset;
	half4 c4 = half4(0, 0, 0, 1);
	half4x4 m;
	m._11_21_31_41 = c1;
	m._12_22_32_42 = c2;
	m._13_23_33_43 = c3;
	m._14_24_34_44 = c4;
	return m;
}

half4 skinning(inout appdata_full v)
{
	fixed4 w = v.color;
	half4 bone = half4(v.texcoord2.x, v.texcoord2.y, v.texcoord2.z, v.texcoord2.w);
	float curFrame = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, frameIndex);
	float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, preFrameIndex);
	float progress = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, transitionProgress);
	int preFrame = curFrame;
	int nextFrame = curFrame + 1.0f;
	half4x4 localToWorldMatrixPre = loadMatFromTexture(preFrame, bone.x) * w.x;//for bones 1>
	half4x4 localToWorldMatrixNext = loadMatFromTexture(nextFrame, bone.x) * w.x;//for bones 1>
#if _BONE2 || _BONE3 || _BONE4
	localToWorldMatrixPre += loadMatFromTexture(preFrame, bone.y) * max(0, w.y);//for bones 2>
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.y) * max(0, w.y);//for bones 2>
#endif
#if _BONE3 || _BONE4
	localToWorldMatrixPre += loadMatFromTexture(preFrame, bone.z) * max(0, w.z);//for bones 3>
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.z) * max(0, w.z);//for bones 3>
#endif
#if _BONE4
	localToWorldMatrixPre += loadMatFromTexture(preFrame, bone.w) * max(0, w.w);//for bones 4>
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.w) * max(0, w.w);//for bones 4>
#endif

	half4 localPosPre = mul(v.vertex, localToWorldMatrixPre);
	half4 localPosNext = mul(v.vertex, localToWorldMatrixNext);
	half4 localPos = lerp(localPosPre, localPosNext, curFrame - preFrame);

	half3 localNormPre = mul(v.normal.xyz, (float3x3)localToWorldMatrixPre);
	half3 localNormNext = mul(v.normal.xyz, (float3x3)localToWorldMatrixNext);
	half3 localTanPre = mul(v.tangent.xyz, (float3x3)localToWorldMatrixPre);
	half3 localTanNext = mul(v.tangent.xyz, (float3x3)localToWorldMatrixNext);

	//for animation blending in crossFade
	int preFrame_ = preAniFrame;
	int nextFrame_ = preAniFrame + 1.0f;
	half4x4 localToWorldMatrixPreAni = loadMatFromTexture(preFrame_, bone.x);
	half4x4 localToWorldMatrixPreAniNext = loadMatFromTexture(nextFrame_, bone.x);
	half4 localPosPreAni = mul(v.vertex, localToWorldMatrixPreAni);
	half4 localPosPreAniNext = mul(v.vertex, localToWorldMatrixPreAniNext);
	half4 localPrePos = lerp(localPosPreAni, localPosPreAniNext, preAniFrame - preFrame_);

	//this is normal blending block for transitions
#if INSTANCING_NORMAL_TRANSITION_BLENDING
	half3 localNormPreAni = mul(v.normal.xyz, (float3x3)localToWorldMatrixPreAni);
	half3 localNormNextAni = mul(v.normal.xyz, (float3x3)localToWorldMatrixPreAniNext);
	half3 localTanPreAni = mul(v.tangent.xyz, (float3x3)localToWorldMatrixPreAni);
	half3 localTanNextAni = mul(v.tangent.xyz, (float3x3)localToWorldMatrixPreAniNext);
	v.normal = normalize(lerp(lerp(localNormPre, localNormNext, curFrame - preFrame), lerp(localNormPreAni, localNormNextAni, preAniFrame - preFrame_), (1.0f - progress)));
	v.tangent.xyz = normalize(lerp(lerp(localTanPre, localTanNext, curFrame - preFrame), lerp(localTanPreAni, localTanNextAni, preAniFrame - preFrame_), (1.0f - progress)));
#else
	//you can use this version if want performance instead of blending normals during transitions
	v.normal = normalize(lerp(localNormPre, localNormNext, curFrame - preFrame));
	v.tangent.xyz = normalize(lerp(localTanPre, localTanNext, curFrame - preFrame));
#endif

	localPos = lerp(localPos, localPrePos, (1.0f - progress));
	return localPos;
}

half4 skinningShadow(inout appdata_full v)
{
	half4 bone = half4(v.texcoord2.x, v.texcoord2.y, v.texcoord2.z, v.texcoord2.w);
	float curFrame = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, frameIndex);
	float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, preFrameIndex);
	float progress = UNITY_ACCESS_INSTANCED_PROP(AnimationProp, transitionProgress);
	int preFrame = curFrame;
	int nextFrame = curFrame + 1.0f;
	half4x4 localToWorldMatrixPre = loadMatFromTexture(preFrame, bone.x);
	half4x4 localToWorldMatrixNext = loadMatFromTexture(nextFrame, bone.x);
	half4 localPosPre = mul(v.vertex, localToWorldMatrixPre);
	half4 localPosNext = mul(v.vertex, localToWorldMatrixNext);
	half4 localPos = lerp(localPosPre, localPosNext, curFrame - preFrame);

	//for animation blending in crossFade
	int preFrame_ = preAniFrame;
	int nextFrame_ = preAniFrame + 1.0f;
	half4x4 localToWorldMatrixPreAni = loadMatFromTexture(preFrame_, bone.x);
	half4x4 localToWorldMatrixPreAniNext = loadMatFromTexture(nextFrame_, bone.x);
	half4 localPosPreAni = mul(v.vertex, localToWorldMatrixPreAni);
	half4 localPosPreAniNext = mul(v.vertex, localToWorldMatrixPreAniNext);
	half4 localPrePos = lerp(localPosPreAni, localPosPreAniNext, preAniFrame - preFrame_);

	localPos = lerp(localPos, localPrePos, (1.0f - progress));
	return localPos;
}

void animVert(inout appdata_full v)
{
#ifdef UNITY_PASS_SHADOWCASTER
	v.vertex = skinningShadow(v);
#else
	v.vertex = skinning(v);
#endif
}
#pragma warning (default : 3568)