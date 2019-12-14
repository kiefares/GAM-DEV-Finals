// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: commented out 'float4x4 _WorldToCamera', a built-in variable

// Upgrade NOTE: replaced '_WorldToCamera' with 'unity_WorldToCamera'

// Upgrade NOTE: replaced '_WorldToCamera' with 'unity_WorldToCamera'

// Upgrade NOTE: commented out 'float4x4 _WorldToCamera', a built-in variable
// Upgrade NOTE: replaced '_WorldToCamera' with 'unity_WorldToCamera'
//

//
// Kino/Obscurance - SSAO (screen-space ambient obscurance) effect for Unity
//
// Adapted by Alex Blaikie
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// --------
// Additional options for further customization
// --------

// The SampleNormal function normalizes samples from G-buffer because
// they're possibly unnormalized. We can eliminate this if it can be said
// that there is no wrong shader that outputs unnormalized normals.
// #define VALIDATE_NORMALS

// The constant below controls the geometry-awareness of the blur filter.
// The higher value, the more sensitive it is.
static const float kGeom = 50;

// The constants below are used in the AO estimator. Beta is mainly used
// for suppressing self-shadowing noise, and Epsilon is used to prevent
// calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
// for further details of these constants.
static const float kEpsilon = 1e-4;

// --------

#if _AOSAMPLECOUNT_LOWEST || SHADER_API_GLES
static const int _AOSampleCount = 3;
#else
int _AOSampleCount;
#endif

sampler2D _CameraGBufferTexture2;
half4 _CameraGBufferTexture2_ST;
sampler2D_float _CameraDepthNormalsTexture;
half4 _CameraDepthNormalsTexture_ST;
sampler2D_float _CameraDepthTexture;
half4 _CameraDepthTexture_ST;
// float4x4 _WorldToCamera;

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _MainTex_ST;

sampler2D _AOTex;
half4 _AOTex_ST;

// Material shader properties

// The constant below determines the contrast of occlusion. Altough this
// allows intentional over/under occlusion, currently is not exposed to the
// editor, because it’s thought to be rarely useful.
//Update: It is useful, so expose it. Derp.

uniform half2 _AOIntensityContrast;
uniform float _AORadius;
uniform float _AOBias;
uniform float _AOTargetScale;

uniform float _AOCutoff;
uniform float _AOCutoffRange;

uniform float2 _AOBlurVector;
uniform float4 _AOProjInfo;
uniform float4x4 _AOCameraModelView;

// Common vertex shader
struct v2fAO
{
    float4 pos : SV_POSITION;
    half2 uv : TEXCOORD0;    // Screen space UV (supports stereo rendering)
    half2 uv01 : TEXCOORD1;  // Original UV (from 0 to 1)
    half2 uvAlt : TEXCOORD2; // Alternative UV (supports v-flip case)
};

v2fAO vertAO(appdata_img v)
{
    half2 uvAlt = v.texcoord;
#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0.0) uvAlt.y = 1 - uvAlt.y;
#endif

    v2fAO o;
#if defined(UNITY_SINGLE_PASS_STEREO)
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
    o.uvAlt = UnityStereoScreenSpaceUVAdjust(uvAlt, _MainTex_ST);
#else
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;
    o.uvAlt = uvAlt;
#endif
    o.uv01 = uvAlt;

    return o;
}

// Utility for sin/cos
float2 CosSin(float theta)
{
    float sn, cs;
    sincos(theta, sn, cs);
    return float2(cs, sn);
}

// Pseudo random number generator with 2D argument
float UVRandom(float u, float v)
{
    float f = dot(float2(12.9898, 78.233), float2(u, v));
    return frac(43758.5453 * sin(f));
}

// Interleaved gradient function from CoD Advanced Warfare/Jimenez 2014 http://goo.gl/eomGso
float GradientNoise(float2 uv)
{
	//return 1.0;
    uv = floor(uv * _ScreenParams.xy);
    float f = dot(float2(0.06711056f, 0.00583715f), uv);
    return frac(52.9829189f * frac(f));
}

// Boundary check for depth sampler
// (returns a very large value if it lies out of bounds)
float CheckBounds(float2 uv, float d)
{
    float ob = any(uv < 0) + any(uv > 1);
#if defined(UNITY_REVERSED_Z)
    ob += (d <= 0.00001);
#else
    ob += (d >= 0.99999);
#endif
    return ob * 1e8;
}

float3 ReconstructCSFaceNormal(float3 C) {
	return normalize(cross(ddy(C), ddx(C)));
}

// Z buffer depth to linear 0-1 depth
float LinearizeDepth(float z)
{
    float isOrtho = unity_OrthoParams.w;
    float isPers = 1 - unity_OrthoParams.w;
    z *= _ZBufferParams.x;
    return (1 - isOrtho * z) / (isPers * z + _ZBufferParams.y);
}


// Check if the camera is perspective.
// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
    return lerp(x, 1, unity_OrthoParams.w);
}

// Depth/normal sampling functions
float SampleDepth(float2 uv)
{
#if defined(SOURCE_GBUFFER) || defined(SOURCE_DEPTH)
    float d = LinearizeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
#else
    float4 cdn = tex2D(_CameraDepthNormalsTexture, UnityStereoScreenSpaceUVAdjust(uv, _CameraDepthNormalsTexture_ST));
    float d = DecodeFloatRG(cdn.zw);
#endif
	return d *_ProjectionParams.z + CheckBounds(uv, d);
}

float3 SampleNormal(float2 uv)
{
#if defined(SOURCE_GBUFFER)
    float3 norm = tex2D(_CameraGBufferTexture2, UnityStereoScreenSpaceUVAdjust(uv, _CameraGBufferTexture2_ST)).xyz;
    norm = norm * 2 - any(norm); // gets (0,0,0) when norm == 0
    norm = mul((float3x3)unity_WorldToCamera, norm);
#if defined(VALIDATE_NORMALS)
    norm = normalize(norm);
#endif
    return norm;
#else
    float4 cdn = tex2D(_CameraDepthNormalsTexture, UnityStereoScreenSpaceUVAdjust(uv, _CameraDepthNormalsTexture_ST));
    return DecodeViewNormalStereo(cdn) * float3(1, 1, -1);
#endif
}

float SampleDepthNormal(float2 uv, out float3 normal)
{
#if defined(SOURCE_GBUFFER) || defined(SOURCE_DEPTH)
    normal = SampleNormal(uv);
    return SampleDepth(uv);
#else
    float4 cdn = tex2D(_CameraDepthNormalsTexture, UnityStereoScreenSpaceUVAdjust(uv, _CameraDepthNormalsTexture_ST));
    normal = DecodeViewNormalStereo(cdn) * float3(1, 1, -1);
    float d = DecodeFloatRG(cdn.zw);
    return d * _ProjectionParams.z + CheckBounds(uv, d);
#endif
}

// Reconstruct view-space position from UV and depth.
// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    //return float3((uv * 2 - 1 - p13_31) / p11_22, 1) * depth;
	return float3((uv * 2 - 1 - p13_31) / p11_22 * CheckPerspective(depth), depth);

}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeom, 1, dot(d1, d2));
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformaly distributed points on a unit sphere http://goo.gl/X2F1Ho
    float gn = GradientNoise(uv * _AOTargetScale);
    float u = frac(UVRandom(0, index) + gn) * 2 - 1;
    float theta = (UVRandom(1, index) + gn) * UNITY_PI * 2;
    float3 v = float3(CosSin(theta) * sqrt(1 - u * u), u);
    
    
    // Make them distributed between [0, _AORadius]
    float l = sqrt((index + 1) / _AOSampleCount) * _AORadius;
    return v * l;
}

// Geometry-aware separable blur filter
half SeparableBlur(sampler2D tex, half4 tex_ST, float2 uv, float2 delta)
{
    half3 n0 = SampleNormal(uv);

    half2 uv1 = uv - delta;
    half2 uv2 = uv + delta;
    half2 uv3 = uv - delta * 2;
    half2 uv4 = uv + delta * 2;

    half w0 = 3;
    half w1 = CompareNormal(n0, SampleNormal(uv1)) * 2;
    half w2 = CompareNormal(n0, SampleNormal(uv2)) * 2;
    half w3 = CompareNormal(n0, SampleNormal(uv3));
    half w4 = CompareNormal(n0, SampleNormal(uv4));

    half s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv, tex_ST)).r * w0;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1, tex_ST)).r * w1;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2, tex_ST)).r * w2;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv3, tex_ST)).r * w3;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv4, tex_ST)).r * w4;

    return s / (w0 + w1 + w2 + w3 + w4);
}

// Pass 0: Obscurance estimation
half4 frag_ao(v2fAO i) : SV_Target
{
    float2 uv = i.uv;
    float2 uv01 = i.uv01;

    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    float3 norm_o;
    float depth_o = SampleDepthNormal(uv, norm_o);

#if defined(SOURCE_DEPTHNORMALS)
    // Offset the depth value to avoid precision error.
    // (depth in the DepthNormals mode has only 16-bit precision)
    depth_o -= _ProjectionParams.z / 65536;
#endif

    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv01, depth_o, p11_22, p13_31);

    float ao = 0.0;

    for (int s = 0; s < _AOSampleCount; s++)
    {
        // Sample point
#if defined(SHADER_API_D3D11)
        // This 'floor(1.0001 * s)' operation is needed to avoid a NVidia
        // shader issue. This issue is only observed on DX11.
        float3 v_s1 = PickSamplePoint(uv, floor(1.0001 * s));
#else
        float3 v_s1 = PickSamplePoint(uv, s);
#endif
        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(proj, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / CheckPerspective(vpos_s1.z) + 1) * 0.5;
#if defined(UNITY_SINGLE_PASS_STEREO)
        float2 uv_s1 = UnityStereoScreenSpaceUVAdjust(uv_s1_01, _MainTex_ST);
#else
        float2 uv_s1 = uv_s1_01;
#endif

        // Depth at the sample point
        float depth_s1 = SampleDepth(uv_s1);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - _AOBias * depth_o, 0);
        float a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 / a2;
    }

    ao *= _AORadius; // intensity normalization

    // Apply other parameters.
    ao = pow(ao * _AOIntensityContrast.r / _AOSampleCount, _AOIntensityContrast.g);

	//AO isn't done before transparents, so do this
    if(depth_o > 99999)
    {
    	ao = 0.0;
    }
    
    #if _AOCUTOFF_ON
    float aoDistanceCutoff = depth_o - _AOCutoff;
    aoDistanceCutoff /= _AOCutoffRange;
    aoDistanceCutoff = max(aoDistanceCutoff, 0.0);
    ao = lerp(ao, 0.0, aoDistanceCutoff);
    #endif


	return ao;
    //return EstimateObscurance(i.uv);
}

// Obscurance estimator function
float EstimateObscurance(float2 uv)
{
    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    float3 norm_o;
    float depth_o = SampleDepthNormal(uv, norm_o);
    
#if defined(SOURCE_DEPTHNORMALS)
    // Offset the depth value to avoid precision error.
    // (depth in the DepthNormals mode has only 16-bit precision)
    depth_o -= _ProjectionParams.z / 65536;
#endif
    
    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv, depth_o, p11_22, p13_31);

    // Distance-based AO estimator based on Morgan 2011 http://goo.gl/2iz3P
    float ao = 0.0;

    for (int s = 0; s < _AOSampleCount; s++)
    {
	    #if SHADER_API_D3D11
		// This 'floor(1.0001 * s)' operation is needed to avoid a NVidia
		// shader issue. This issue is only observed on DX11.
			float3 v_s1 = PickSamplePoint(uv, floor(1.0001 * s));
		#else
	        float3 v_s1 = PickSamplePoint(uv, s);
		#endif
		
        #if SOURCE_GBUFFER
        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        #else
        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        #endif
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(proj, vpos_s1);
        float2 uv_s1 = (spos_s1.xy / vpos_s1.z + 1) * 0.5;

        // Depth at the sample point
        float depth_s1 = SampleDepth(uv_s1);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;
		
        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - _AOBias * depth_o, 0);
        float a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 / a2;
    }

    ao *= _AORadius; // intensity normalization
    
    //AO isn't done before transparents, so do this
    if(depth_o > 99999)
    {
    	ao = 0.0;
    }
    
    #if _AOCUTOFF_ON
    float aoDistanceCutoff = depth_o - _AOCutoff;
    aoDistanceCutoff /= _AOCutoffRange;
    aoDistanceCutoff = max(aoDistanceCutoff, 0.0);
    ao = lerp(ao, 0.0, aoDistanceCutoff);
    #endif

    // Apply other parameters.
    return pow(ao * _AOIntensityContrast.r / _AOSampleCount, _AOIntensityContrast.g);
}

// Pass1: Geometry-aware separable blur
half4 frag_blur(v2f_img i) : SV_Target
{
    float2 delta = _MainTex_TexelSize.xy * _AOBlurVector;
    return SeparableBlur(_MainTex, _MainTex_ST, i.uv, delta);
}

// Pass 2: Combiner for the forward mode
struct v2f_multitex
{
    float4 pos : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
};

v2f_multitex vert_multitex(appdata_img v)
{
    // Handles vertically-flipped case.
    float vflip = sign(_MainTex_TexelSize.y);

    v2f_multitex o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv0 = v.texcoord.xy;
    o.uv1 = (v.texcoord.xy - 0.5) * float2(1, vflip) + 0.5;
    return o;
}

float4 fragAOdebug (v2f_img i) : SV_Target
{
	float4 col = (float4)1.0;
	col.rgb -= (float3)tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)).r;
	return col;
}    	

static const float gaussian[5] = { 0.153170, 0.144893, 0.122649, 0.092902, 0.062970 };  // stddev = 2.0
#define EDGE_SHARPNESS 0.85

// Geometry-aware separable blur filter (large kernel)
half SeparableBlurLarge(sampler2D tex, half4 tex_ST, float2 uv, float2 delta)
{
#if !SHADER_API_MOBILE
    // 9-tap Gaussian blur with adaptive sampling
    float2 uv1a = uv - delta;
    float2 uv1b = uv + delta;
    float2 uv2a = uv - delta * 2;
    float2 uv2b = uv + delta * 2;
    float2 uv3a = uv - delta * 3.2307692308;
    float2 uv3b = uv + delta * 3.2307692308;

    half3 n0 = SampleNormal(uv);

    half w0 = 0.37004405286;
    half w1a = CompareNormal(n0, SampleNormal(uv1a)) * 0.31718061674;
    half w1b = CompareNormal(n0, SampleNormal(uv1b)) * 0.31718061674;
    half w2a = CompareNormal(n0, SampleNormal(uv2a)) * 0.19823788546;
    half w2b = CompareNormal(n0, SampleNormal(uv2b)) * 0.19823788546;
    half w3a = CompareNormal(n0, SampleNormal(uv3a)) * 0.11453744493;
    half w3b = CompareNormal(n0, SampleNormal(uv3b)) * 0.11453744493;

    half s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv, tex_ST)).r * w0;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1a, tex_ST)).r * w1a;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1b, tex_ST)).r * w1b;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2a, tex_ST)).r * w2a;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2b, tex_ST)).r * w2b;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv3a, tex_ST)).r * w3a;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv3b, tex_ST)).r * w3b;

    return s / (w0 + w1a + w1b + w2a + w2b + w3a + w3b);
#else
    // 9-tap Gaussian blur with linear sampling
    // (less quality but slightly fast)
    float2 uv1a = uv - delta * 1.3846153846;
    float2 uv1b = uv + delta * 1.3846153846;
    float2 uv2a = uv - delta * 3.2307692308;
    float2 uv2b = uv + delta * 3.2307692308;

    half3 n0 = SampleNormal(uv);

    half w0 = 0.2270270270;
    half w1a = CompareNormal(n0, SampleNormal(uv1a)) * 0.3162162162;
    half w1b = CompareNormal(n0, SampleNormal(uv1b)) * 0.3162162162;
    half w2a = CompareNormal(n0, SampleNormal(uv2a)) * 0.0702702703;
    half w2b = CompareNormal(n0, SampleNormal(uv2b)) * 0.0702702703;

    half s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv, tex_ST)).r * w0;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1a, tex_ST)).r * w1a;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1b, tex_ST)).r * w1b;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2a, tex_ST)).r * w2a;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2b, tex_ST)).r * w2b;

    return s / (w0 + w1a + w1b + w2a + w2b);
#endif
}

// Geometry-aware separable blur filter (small kernel)
half SeparableBlurSmall(sampler2D tex, half4 tex_ST, float2 uv, float2 delta)
{
    float2 uv1 = uv - delta;
    float2 uv2 = uv + delta;

    half3 n0 = SampleNormal(uv);

    half w0 = 2;
    half w1 = CompareNormal(n0, SampleNormal(uv1));
    half w2 = CompareNormal(n0, SampleNormal(uv2));

    half s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv, tex_ST)).r * w0;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv1, tex_ST)).r * w1;
    s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv2, tex_ST)).r * w2;

    return s / (w0 + w1 + w2);
}

// Pass 1: Geometry-aware separable blur (1st iteration)
half4 fragBlurInitial(v2f_img i) : SV_Target
{
    float2 delta = _MainTex_TexelSize.xy * _AOBlurVector;
    return SeparableBlurLarge(_MainTex, _MainTex_ST, i.uv, delta);
}

// Pass 2: Geometry-aware separable blur (2nd iteration)
half4 fragBlurSecondary(v2f_img i) : SV_Target
{
    float2 delta = _MainTex_TexelSize.xy * _AOBlurVector;
    return SeparableBlurSmall(_MainTex, _MainTex_ST, i.uv, delta);
}

