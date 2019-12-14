
//PRISM DOF - Based off DICE's model 
//http://www.slideshare.net/DICEStudio/five-rendering-ideas-from-battlefield-3-need-for-speed-the-run

#define USE_RANDOM

uniform float2 _DofBlurDir = float2( 0.0, 1.0 );
uniform float2 _DofBlurDirSecond = float2( 0.0, 1.0 );
uniform float _DofRandomAdd = 25.0;
// ====

const float blurdist_px = 32.0;
uniform sampler2D _DofBlur1;
half4 _DofBlur1_ST;
uniform sampler2D _DofBlur2;
half4 _DofBlur2_ST;

static const int NUM_SAMPLES = 16;

//#define GAMMA_SRGB
#if defined( GAMMA_SRGB )
// see http://www.opengl.org/registry/specs/ARB/framebuffer_sRGB.txt
float3 srgb2lin( float3 cs )
{
	float3 c_lo = cs / 12.92;
	float3 c_hi = pow( (cs + 0.055) / 1.055, (float3)2.4 );
	float3 s = step((float3)0.04045, cs);
	return lerp( c_lo, c_hi, s );
}
float3 lin2srgb( float3 cl )
{
	//cl = clamp( cl, 0.0, 1.0 );
	float3 c_lo = 12.92 * cl;
	float3 c_hi = 1.055 * pow(cl,(float3)0.41666) - 0.055;
	float3 s = step( (float3)0.0031308, cl);
	return lerp( c_lo, c_hi, s );
}
#else
float3 srgb2lin(float3 c) { 
	return c;//c*c; 
}
float3 lin2srgb(float3 c) { 
	return c;//sqrt(c); 
}
#endif 
//GAMMA_SRGB

//note: uniform pdf rand [0;1[
float hash12n(float2 p)
{
	p  = frac(p * float2(5.3987, 5.4421));
    p += dot(p.yx, p.xy + float2(21.5351, 14.3137));
	return frac(p.x * p.y * 95.4307);
}

float4 sampletexLin( sampler2D dofSamplr, dofSamplr_ST, float2 uv )
{
	//srgb2lin()
    return  tex2D( dofSamplr, UnityStereoScreenSpaceUVAdjust(uv, dofSamplr_ST));
}

float3 GetBlurColorWithBokeh(sampler2D dofSamplr, dofSamplr_ST, float2 blurVec, float2 fragCoord, float ifBackZero)
{
    float2 uv = fragCoord / _MainTex_TexelSize.zw; 
    
    float4 origCol = tex2D(dofSamplr, UnityStereoScreenSpaceUVAdjust(uv, dofSamplr_ST));
    
    float blurdist = _DofRadius;//blurdist_px;
    float2 p0 = uv;
    float2 p1 = uv + blurdist * blurVec;//blurfloat * _MainTex_TexelSize.xy;
    float2 stepfloat = (p1-p0) / float(NUM_SAMPLES);
    float2 p = p0;
    #if defined(USE_RANDOM)
  	p += (hash12n(uv+frac(_Time.x)+0.1)) * stepfloat;
    #endif
    
    float4 sumcol = ZEROES;
    float4 accumW = ZEROES;
    for (int i=0;i<NUM_SAMPLES;++i)
    {
        float4 sample = sampletexLin(dofSamplr, dofSamplr_ST, p);// - THRESHOLD ) / (1.0-THRESHOLD);
        
        if(sample.a <= ifBackZero) {sample = origCol;}
        
        //Do bokeh shit
        float4 bokeh = ONES + (sample * sample) * sample * (float4)_DofFactor;
        
        //New
        sumcol += sample * bokeh;
        
        accumW += bokeh;
        p += stepfloat;
    }
    
    sumcol /= accumW;//float(NUM_SAMPLES);
	
	sumcol.a = origCol.a;
		
    return sumcol.rgb;
}

float3 GetBlurColorNoBokeh(sampler2D dofSamplr, half4 dofSamplr_ST, float2 blurVec, float2 fragCoord, float ifBackZero)
{
    float2 uv = fragCoord / _MainTex_TexelSize.zw;
    
    float4 origCol = tex2D(dofSamplr, UnityStereoScreenSpaceUVAdjust(uv, dofSamplr_ST));
    
    float blurdist = _DofRadius;//blurdist_px;    
    float2 p0 = uv;
    float2 p1 = uv + blurdist * blurVec;//blurfloat * _MainTex_TexelSize.xy;
    float2 stepfloat = (p1-p0) / float(NUM_SAMPLES);
    float2 p = p0;
    #if defined(USE_RANDOM)
  	p += (hash12n(uv+frac(_Time.x)+0.1)) * stepfloat;
    #endif
    
    float4 sumcol = ZEROES;
    float4 accumW = ZEROES;
    for (int i=0;i<NUM_SAMPLES;++i)
    {
    	float4 sample = sampletexLin(dofSamplr, dofSamplr_ST, p);
    	
    	if(sample.a <= ifBackZero) {sample = origCol;}
    	
        //New
        sumcol += sample;// * sample;
        
        p += stepfloat;
    }
    accumW = float(NUM_SAMPLES);
    sumcol /= accumW;//float(NUM_SAMPLES);
    
    sumcol.a = origCol.a;
    
    return sumcol.rgb;
}

//
float4 fragDoFBlurDICEOne (v2fDoF i) : SV_Target
{		
	float2 fragCoord = i.vPosition.xy;
	float2 uv = i.uv;
	
    float2 blurvec = normalize(_DofBlurDir) / _MainTex_TexelSize.zw;

    return float4( GetBlurColorWithBokeh(_MainTex, _MainTex_ST, blurvec, fragCoord, 0.0), tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).a );
}

float4 fragDoFBlurDICETwo (v2fDoF i) : SV_Target
{		
	float2 fragCoord = i.vPosition.xy;
	float2 uv = i.uv;
    float2 blurvec = normalize(_DofBlurDir) / _MainTex_TexelSize.zw;

    return float4( GetBlurColorNoBokeh(_MainTex, _MainTex_ST, blurvec, fragCoord, 0.0), tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).a );
}
//
float4 fragDoFBlurDICEThree (v2fDoF i) : SV_Target
{		
	float4 col = (float4)0.0;
	float2 uv = i.uv;
	float2 fragCoord = i.vPosition.xy;
	float2 blurvec = normalize(_DofBlurDir) / _MainTex_TexelSize.zw;
	
    float3 sumcol = GetBlurColorNoBokeh(_MainTex, _MainTex_ST, blurvec, fragCoord, 0.0);
    
    float3 dof1 = srgb2lin(tex2D(_DofBlur1, UnityStereoScreenSpaceUVAdjust(uv, _DofBlur1_ST)).rgb);
    float3 dof2 = srgb2lin(tex2D(_DofBlur2, UnityStereoScreenSpaceUVAdjust(uv, _DofBlur2_ST)).rgb);
    
    //FOUND THE ***
    col.rgb = sumcol + dof1 + dof2;
    col.rgb = col.rgb / (3.);
    
    return float4(lin2srgb(col.rgb), tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).a);
}

DoFOutput fragDoFBlurDICEMRTOne (v2fDoF i)
{		
	float4 col = ZEROES;
	float4 col2 = ZEROES;
	
	float2 fragCoord = i.vPosition.xy;
	float2 uv = i.uv;
	
    float2 blurvecOne = normalize(_DofBlurDir) / _MainTex_TexelSize.zw;
    float2 blurvecTwo = normalize(_DofBlurDirSecond) / _MainTex_TexelSize.zw;
    
    col.rgb = GetBlurColorWithBokeh(_DoF1, _DoF1_ST, blurvecOne, fragCoord, 0.0);
    col2.rgb = GetBlurColorWithBokeh(_DoF2, _DoF2_ST, blurvecTwo, fragCoord, 1.0);
    
    col.a = tex2D(_DoF1, UnityStereoScreenSpaceUVAdjust(uv, _DoF1_ST)).a;
    col2.a = tex2D(_DoF2, UnityStereoScreenSpaceUVAdjust(uv, _DoF2_ST)).a;
	
	DoFOutput o;
	o.dofBack = col;
	o.dofFront = col2;
	return o;
}

DoFOutput fragDoFBlurDICEMRTTwo (v2fDoF i)
{		
	float4 col = ZEROES;
	float4 col2 = ZEROES;
	
	float2 fragCoord = i.vPosition.xy;
	float2 uv = i.uv;
	
    float2 blurvecOne = normalize(_DofBlurDir) / _MainTex_TexelSize.zw;
    float2 blurvecTwo = normalize(_DofBlurDirSecond) / _MainTex_TexelSize.zw;
    
    col.rgb = GetBlurColorWithBokeh(_DoF1, _DoF1_ST, blurvecOne, fragCoord, 0.0);
    col2.rgb = GetBlurColorWithBokeh(_DoF2, _DoF2_ST, blurvecTwo, fragCoord, 1.0);
    
    col.a = tex2D(_DoF1, UnityStereoScreenSpaceUVAdjust(uv, _DoF1_ST)).a;
    col2.a = tex2D(_DoF2, UnityStereoScreenSpaceUVAdjust(uv, _DoF2_ST)).a;
	
	DoFOutput o;
	o.dofBack = col;
	o.dofFront = col2;
	return o;
}



































