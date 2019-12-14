// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*
																	 ;
		                                                            ;;     
                                                                   ;;;     
      PRISM ---All-In-One Post-Processing for Unity              .;;;;     
      Created by  Alex Blaikie, @Gadget_Games                   :;;;;;     
                                                               ;;;;;;;     
                                                              ;;;;;;;;     
                                                             ;;;;;;;;;     
                                                           ,;;;;;;;;;;     
                                                          ;;;;;;;;;;;;     
                                                         ;;;;;;;;;;;;,     
                                                        ;;;;;;;;;;;,,,     
                                                      `;;;;;;;;;;;,,,,     
                                                     ,;;;;;;;;;;,,,,,,     
                                                    ;;;;;;;;;;:,,,,,,,     
                                                   ;;;;;;;;;;,,,,,,,,,     
                                                  ;;;;;;;;;,,,,,,,,,,,     
                                                `;;;;;;;;:,,,,,,,,,,,,     
                                               :;;;;;;;;,,,,,,,,,,,,,,     
                                              ;;;;;;;;,,,,,,,,,,,,,,,,     
                                             ;;;;;;;:,,,,,,,,,,,,,,,,,     
                                            ;;;;;;;,,,,,,,,,,,,,,,,,,,     
                                          .;;;;;;,,,,,,,,,,,,,,,,,,,,,     
                                         :;;;;;;,,,,,,,,,,,,,,,,,,,,,,     
                                        ;;;;;;,,,,,,,,,,,,,,,,,,,,,,,:     
                                       ;;;;;,,,,,,,,,,,,,,,,,,,,,,:;;;     
                                      ;;;;;,,,,,,,,,,,,,,,,,,,,;;;;;;;     
                                    ,;;;;,,,,,,,,,,,,,,,,,,:;;;;;;;;;;     
                                   ;;;;,,,,,,,,,,,,,,,,,:;;;;;;;;;;;;;     
                                  ;;;;,,,,,,,,,,,,,,:;;;;;;;;;;;;;;;;;     
                                 ;;;,,,,,,,,,,,,,:;;;;;;;;;;;;;;;:.        
        @@@@@@@@;              `;;:,,,,,,,,,,:;;;;;;;;;;;;;;,              
        @@@@@@@@@@            ,;;,,,,,,,,,:;;;;;;;;;;;:`                   
        @@@@@@@@@@@          ;;,,,,,,,,;;;;;;;;;:.                         
        @@@@;:'@@@@#       @;:,,,,,:;;;;;;;:`                              
        @@@@    @@@#      ,@:,,,:;;;;:.                                    
     ,::@@@@::::@@@@,,,,,,#@@;;;,`                                         
        @@@@    ,@@@     #@@@'                                             
        @@@@    +@@@ '''':'''.'''`  ''#@@@;  +@@@,                         
        @@@@   .@@@@@@@@@'@@@,@@@`  #@@@@@@@:@@@@@#                        
        @@@@@@@@@@@,@@@@@'@@@,@@@.  #@@@@@@@@@@@@@@                        
        @@@@@@@@@@@ @@@@@'@@@,@@@@` #@@@@@@@@@@@@@@                        
        @@@@@@@@@+  @@@  '@@@, +@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,  @@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@, @@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@@@` #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@;   #@@@ ;@@@  @@@@                        
*/                                                                           
		
	//s2 = a becomes smallest
	//b becomes biggest
	//switches a and b into smallest and biggest
	//
	//mn3 a b c = sorts from low to high
	//mx3 a b c = sorts from high to low
	
	
	//#define PRISM_DEBUG 0
	
	//You must set this to 1 if you are using a two-camera setup with depth effects
	
	//#define PRISM_DOF_USENEARBLUR 1

	#define MOD3 float3(443.8975, 397.2973, 491.1871)
	#define s2(a, b)				temp = a; a = min(a, b); b = max(temp, b);
	#define mn3(a, b, c)			s2(a, b); s2(a, c);
	#define mx3(a, b, c)			s2(b, c); s2(a, c);

	#define mnmx3(a, b, c)				mx3(a, b, c); s2(a, b);                                   // 3 exchanges
	#define mnmx4(a, b, c, d)			s2(a, b); s2(c, d); s2(a, c); s2(b, d);                   // 4 exchanges
	#define mnmx5(a, b, c, d, e)		s2(a, b); s2(c, d); mn3(a, c, e); mx3(b, d, e);           // 6 exchanges
	#define mnmx6(a, b, c, d, e, f) 	s2(a, d); s2(b, e); s2(c, f); mn3(a, b, c); mx3(d, e, f); // 7 exchanges
	
	//#define HD_COLORIFY

	// Single Pass rendering method for VR
	//#ifdef UNITY_SINGLE_PASS_STEREO
	//	#define UnityStereoScreenSpaceUVAdjust(x, y) UnityStereoScreenSpaceUVAdjustInternal(x, y)
	//#else
	//	#define UnityStereoScreenSpaceUVAdjust(x, y) x
	//#endif
    
    //If potato, shouldn't be using stochastic anyway
	#if SHADER_TARGET < 30
	  	static const int SAMPLE_COUNT = 1;
	#else 
	    #if PRISM_SAMPLE_LOW
	  	static const int SAMPLE_COUNT = 16;
	    #elif PRISM_SAMPLE_MEDIUM
	   	static const int SAMPLE_COUNT = 32;
	    #elif PRISM_SAMPLE_HIGH
	    static const int SAMPLE_COUNT = 64;
	    #else
	    static const int SAMPLE_COUNT = 12;    
	    #endif
	#endif
    
	#if SHADER_TARGET < 30
	  	static const int DOF_SAMPLES = 1;
	#else 
	    #if PRISM_DOF_LOWSAMPLE
	    static const int DOF_SAMPLES = 2;
	    #elif PRISM_DOF_MEDSAMPLE
	 	static const int DOF_SAMPLES = 3;//Default = 3
	    #else 
	    static const int DOF_SAMPLES = 5;//THIS IS UNUSED
	    #endif
	#endif
            
	static const float4 ONES = (float4)1.0;// float4(1.0, 1.0, 1.0, 1.0);
	static const float4 ZEROES = (float4)0.0;
	sampler2D _MainTex;
	half4 _MainTex_ST;
	half4 _MainTex_TexelSize;
	sampler2D _CameraGBufferTexture3;
	half4 _CameraGBufferTexture3_ST;
	
	//Normal
	sampler2D_float _CameraDepthTexture;
	half4 _CameraDepthTexture_ST;
	
	//Set by scripts
	uniform float useNoise = 0.0;
	
	//START SUN VARIABLES
	uniform half4 _SunColor;
	uniform half4 _SunPosition;
	uniform half4 _SunThreshold;
	uniform float _SunWeight;
	sampler2D _RaysTexture;
	half4 _RaysTexture_ST;
	
	uniform float _SunDecay=0.96815;
	uniform float _SunExposure=0.2;
	uniform float _SunDensity=0.926;
	//END SUN VARIABLES
		
	//START BLOOM VARIABLES=====================
	
#define HALF_MAX 65504.0

inline half  SafeHDR(half  c) { return min(c, HALF_MAX); }
inline half2 SafeHDR(half2 c) { return min(c, HALF_MAX); }
inline half3 SafeHDR(half3 c) { return min(c, HALF_MAX); }
inline half4 SafeHDR(half4 c) { return min(c, HALF_MAX); }

	int currentIteration = 1;
	uniform float _BloomIntensity = 1.5;	
	
	uniform float _Bloom1Intensity;
	uniform float _Bloom2Intensity;
	uniform float _Bloom3Intensity;
	uniform float _Bloom4Intensity;
	
	uniform float  _BloomRadius = 1.0;
	uniform float _DirtIntensity = 1.5;	
	uniform float _FlareDirtIntensity = 0.0;		uniform float _BloomThreshold = 0.5;
	uniform half3 _BloomCurve;
	
	#if SHADER_TARGET < 30
	uniform sampler2D _Bloom1;
	half4 _Bloom1_ST;
	half4 _Bloom1_TexelSize;
	#else
	uniform sampler2D _Bloom1;
	half4 _Bloom1_ST;
	half4 _Bloom1_TexelSize;
	uniform sampler2D _Bloom2;
	half4 _Bloom2_ST;
	half4 _Bloom2_TexelSize;
	uniform sampler2D _Bloom3;
	half4 _Bloom3_ST;
	half4 _Bloom3_TexelSize;
	uniform sampler2D _Bloom4;
	half4 _Bloom4_ST;
	half4 _Bloom4_TexelSize;
	#endif
//
	uniform sampler2D _BloomAcc;
	half4 _BloomAcc_ST;
	sampler2D _AdaptExposureTex;
	half4 _AdaptExposureTex_ST;
	uniform sampler2D _DirtTex;
	half4 _DirtTex_ST;
	
	uniform float3 _RadialValues;
	//END BLOOM VARIABLES=======================
	
	//START VIGNETTE VARIABLES=====================
	uniform float _VignetteStart = 0.5;
	uniform float _VignetteEnd = 0.45;
	uniform float _VignetteIntensity = 1.0;
	uniform half4 _VignetteColor;
	//END VIGNETTE VARIABLES=======================
	
	//START NOISE VARIABLES========================
	uniform sampler2D _GrainTex;
	half4 _GrainTex_ST;
	uniform float4 _GrainOffsetScale;
	uniform float2 _GrainIntensity;
	uniform int2 _RandomInts;
	//END NOISE VARIABLES========================
	
	//START CHROMATIC VARIABLES====================
	uniform float2 _BlurVector;
	uniform float _BlurGaussFactor;
	
	uniform float2 _ChromaticStartEnd;
	uniform float _ChromaticIntensity = 0.05;

	//Vector4(chromaticDistanceOne, chromaticDistanceTwo, abMultiplier, chromaticBlurWidth));
	uniform float4 _ChromaticParams;
	//END CHROMATIC VARIABLES======================
	
	//START EXPOSURE AND TONEMAP VARIABLES=======================
	uniform float _TonemapStrength;
	uniform float4 _ToneParams;
	uniform float4 _SecondaryToneParams;
	
	uniform float _ExposureSpeed;	
	uniform float _ExposureMiddleGrey;
	uniform float _ExposureLowerLimit;
	uniform float _ExposureUpperLimit;	
	uniform sampler2D _BrightnessTexture;
	half4 _BrightnessTexture_ST;
	
	uniform float _Gamma;
	//END EXPOSURE VARIABLES=======================
	
	//START BRIGHT VARIABLES//
	uniform float _BrightIntensity;
	uniform float4 _BrightCurve;
	//END BRIGHT VARIABLES
	
	//START DOF VARIABLES==========================
	uniform sampler2D _DoF1;
	half4 _DoF1_ST;
	half4 _DoF1_TexelSize;
	uniform sampler2D _DoF2;
	half4 _DoF2_ST;
	half4 _DoF2_TexelSize;
	uniform float _DofUseNearBlur = 0.0;
	uniform float _DofFactor = 64.0;
	uniform float _DofRadius = 2.0;
	uniform float _DofUseLerp = 0.0;
	uniform float _DofLensCoeff;
	uniform float _DofFocusPoint = 15.0;	
	uniform float _DofNearFocusPoint = 0.0;
	uniform float _DofFocusDistance = 35.0;	
	uniform float _DofNearFocusDistance;
	
	//0.99999 if we DONT Want to blur the skybox
	//And transparents
	uniform float _DofBlurSkybox;	
	//END DOF VARIABLES============================
	
	//Start LUT variables
	#if SHADER_TARGET < 30    
	
	#else
	uniform sampler3D _LutTex;
	#endif
	uniform float _LutScale;
	uniform float _LutOffset;
	uniform float _LutAmount;
	
	#if SHADER_TARGET < 30    
	
	#else
	uniform sampler3D _SecondLutTex;
	#endif
	uniform float _SecondLutScale;
	uniform float _SecondLutOffset;
	uniform float _SecondLutAmount;
	//End LUT variables
	
	//Start Sharpen variables
	// -- Sharpening --
	#define sharp_clamp    0.25  //[0.000 to 1.000] Limits maximum amount of sharpening a pixel recieves - Default is 0.035
	// -- Advanced sharpening settings --
	#define offset_bias 1.0  //[0.0 to 6.0] Offset bias adjusts the radius of the sampling pattern.
	                         //I designed the pattern for offset_bias 1.0, but feel free to experiment.
	uniform float _SharpenAmount = 1.0;
	//End sharpen variables
	
	//Start fog variables
	uniform float _FogHeight = 2.0;
    uniform float4 _FogColor;
    uniform half4 _FogParams;	
    uniform float _FogBlurSkybox = 1.0;
	uniform float4 _FogMidColor;
    uniform float4 _FogEndColor;
	uniform float _FogStart = 2.0;
    uniform float _FogMidPoint;
    uniform float _FogEndPoint;
	uniform float _FogDistance = 2.0;
	uniform float _FogIntensity = 1.0;
	uniform float _FogVerticalDistance = 1.0;
	uniform float4x4 _InverseView;
	//End fog variables
	
	//Start colorful variables
	//--------------------------------- Settings ------------------------------------------------
	const float _Colourfulness = 0.4;
	//#define colourfulness  0.4          // Degree of colourfulness, 0 = neutral [-1.0<->2.0]
	#define lim_luma       0.7          // Lower vals allow more change near clipping [0.1<->1.0]
	//-------------------------------------------------------------------------------------------

	// Soft limit, modified tanh approximation WHICH IS SLOW AS FUCK
	#define soft_lim(v,s)  ( clamp((v/s)*(27 + pow(v/s, 2))/(27 + 9*pow(v/s, 2)), -1, 1)*s )
	//#define soft_lim(v,s) tanh(v)

	// Weighted power mean, p=0.5
	#define wpmean(a,b,w)  ( pow((w*sqrt(abs(a)) + (1-w)*sqrt(abs(b))), 2) )

	// Max RGB components
	#define max3(RGB)      ( max((RGB).r, max((RGB).g, (RGB).b)) )
	#define min3(RGB)      ( min((RGB).r, min((RGB).g, (RGB).b)) )

	// sRGB gamma approximation
	#define to_linear(G)   ( pow((G) + 0.06, 2.4) )
	#define to_gamma(LL)   ( pow((LL), 1.0/2.4) - 0.06 )

	// Mean of Rec. 709 & 601 luma coefficients
	#define lumacoeff        float3(0.2558, 0.6511, 0.0931)

	//End colorful variables
	
	//AO
	uniform float _AOIntensity;
	uniform float _AOLuminanceWeighting;
	uniform sampler2D _AOTex;
	half4 _AOTex_ST;
	
	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		float2 texcoord1 : TEXCOORD1;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		
        #if UNITY_UV_STARTS_AT_TOP
			float2 uv2 : TEXCOORD1;
		#endif
	};

	struct appdata_DoF {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		float2 texcoord1 : TEXCOORD1;
	};
		////
	struct v2fDoF {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		
        #if UNITY_UV_STARTS_AT_TOP
			float2 uv2 : TEXCOORD1;
		#endif
		float4 vPosition : TEXCOORD2;
	};
	
    // MRT shader
    struct DoFOutput
    {
        float4 dofBack : SV_Target0;
        float4 dofFront : SV_Target1;
	};
	
	float3 Sample(float2 uv, float2 offsets, float weight)//float mipbias - done with weight
	{
	    float2 PixelSize = _MainTex_TexelSize.xy;
	    return tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + offsets * PixelSize, _MainTex_ST)).rgb * weight;
	}    	
	float PRISM_SAMPLE_DEPTH_TEXTURE(float2 uv)
	{
		return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(uv, _CameraDepthTexture_ST));
	}
	
    //Noises====================
    //
    // Interleaved gradient function from CoD AW.
    // http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
    float gnoise(float2 uv, float2 offs)
    {
        uv = uv / _MainTex_TexelSize.xy + offs;
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        return frac(magic.z * frac(dot(uv, magic.xy)));
    }
    
    float hash (float n)
    {
    	return frac(sin(n)*43758.5453123); 
    }    float nrand(float2 uv, float dx, float dy)
    {
        uv += float2(dx, dy);// + _Time.x);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }
    
    float nrandtime(float2 uv, float dx, float dy)
    {
        uv += float2(dx, dy + _Time.x);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    float hash12(float2 uv)
    {
    	float3 p3 = frac(float3(uv.xyx) * MOD3);
    	p3 += dot(p3, p3.yzx + 19.19);
    	return frac((p3.x + p3.y) * p3.z);
    }

    //End noises================
    
    //Conversions
    half3 srgb_to_linear(half3 c)
    {
        return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
    }

    half3 linear_to_srgb(half3 c)
    {
        return max(1.055 * pow(c, 0.416666667) - 0.055, 0.0);
	}	

	inline float3 ApplyDitherOld(float3 col, float2 uv)
	{
		// Iestyn's RGB dither (7 asm instructions) from Portal 2 X360, slightly modified for VR
		//vec3 vDither = vec3( dot( vec2( 171.0, 231.0 ), vScreenPos.xy + iGlobalTime ) );
		float2 vscreenPos = uv * _ScreenParams.xy;
	    float3 vDither = (float3)( dot( float2( 171.0, 231.0 ), vscreenPos ) );

	    vDither.rgb = frac( vDither.rgb / float3( 103.0, 71.0, 97.0 ) );
	    //return vDither.rgb / 255.0; //note: looks better without 0.375...

	    //note: not sure why the 0.5-offset is there...
	    //vDither.rgb = fract( vDither.rgb / vec3( 103.0, 71.0, 97.0 ) ) - vec3( 0.5, 0.5, 0.5 );
		//return (vDither.rgb / 255.0) * 0.375;
		//
        return col + (vDither.rgb / 255.0);
	}
	
	inline half Max3(half3 x) { return max(x.x, max(x.y, x.z)); }
	inline half Max3(half x, half y, half z) { return max(x, max(y, z)); }
	
	inline half Min3(half3 x) { return min(x.x, min(x.y, x.z)); }
	inline half Min3(half x, half y, half z) { return min(x, min(y, z)); }
	
	// Brightness function
	half Brightness(half3 c)
	{
	    return Max3(c);
	}

	// 3-tap median filter
	half3 Median(half3 a, half3 b, half3 c)
	{
	    return a + b + c - min(min(a, b), c) - max(max(a, b), c);
	}

	// TANH Function (Hyperbolic Tangent)
	float glslTanh(float val)
	{
		float tmp = exp(val);
		float tanH = (tmp - 1.0 / tmp) / (tmp + 1.0 / tmp);
		return tanH;
	}

	float3 glslTanh(float3 val)
	{
		float3 tmp = exp(val);
		float3 tanH = (tmp - 1.0 / tmp) / (tmp + 1.0 / tmp);
		return tanH;
	}
	
	//_RadialValues r = radius
	float radial(float2 pos, float radius)
	{
	    float result = length(pos)-radius;
	    result = frac(result*1.0);
	    float result2 = 1.0 - result;
	    float fresult = result * result2;
	    fresult = pow((fresult*_RadialValues.g),_RadialValues.b);
	    //fresult = clamp(0.0,1.0,fresult);
	    return fresult;
	}
	
	// Compatibility function
	#if (SHADER_TARGET < 50 && !defined(SHADER_API_PSSL))
	float fastReciprocal(float value)
	{
	    return 1. / value;
	}
	#else
	inline float fastReciprocal(float value)
	{
	    return value;
	}
	#endif
	
	// Tonemapper from http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
	float4 FastToneMapPRISM(in float4 color)
	{
	    return float4(color.rgb * fastReciprocal(Max3(color.rgb) + 1.), color.a);
	}
	
	//START BLURS============================================================

	//From Kawase, 2003 GDC presentation
	//http://developer.amd.com/wordpress/media/2012/10/Oat-ScenePostprocessing.pdf
	float4 KawaseBlur(sampler2D s, half4 s_ST, float2 uv, int iteration)//float2 pixelSize, 
	{
		//Dont need anymorefloat2 texCoordSample = 0;
		float2 pixelSize = _MainTex_TexelSize.xy * 0.5;//(float2)(1.0 / _ScreenParams.xy); //_MainTex_TexelSize.wx;
		float2 halfPixelSize = pixelSize / 2.0;
		float2 dUV = (pixelSize.xy * float(iteration)) + halfPixelSize.xy;
		float4 cOut;
		//We probably save like 1 operation from this, lol
		float4 cheekySample = float4(uv.x, uv.x, uv.y, uv.y) + float4(-dUV.x, dUV.x, dUV.y, dUV.y);
		float4 cheekySample2 = float4(uv.x, uv.x, uv.y, uv.y) + float4(dUV.x, -dUV.x, -dUV.y, -dUV.y);

		// Sample top left pixel
		cOut = tex2D(s, UnityStereoScreenSpaceUVAdjust(cheekySample.rb, s_ST));
		// Sample top right pixel
		cOut += tex2D(s, UnityStereoScreenSpaceUVAdjust(cheekySample.ga, s_ST));
		// Sample bottom right pixel
		cOut += tex2D(s, UnityStereoScreenSpaceUVAdjust(cheekySample.rb, s_ST));
		// Sample bottom left pixel
		cOut += tex2D(s, UnityStereoScreenSpaceUVAdjust(cheekySample.ga, s_ST));
		// Average
		cOut *= 0.25f;
		//return tex2D(s, uv);
		return cOut;
	}

	// fixed for Single Pass stereo rendering method
	float KawaseBlurMin(sampler2D s, half4 s_ST, float2 uv, int iteration, float2 pixelSize)//
	{
		//Dont need anymorefloat2 texCoordSample = 0;
		float2 halfPixelSize = pixelSize / 2.0;
		float2 dUV = (pixelSize.xy * float(iteration)) + halfPixelSize.xy;
		float4 cOut;
		//We probably save like 1 operation from this, lol
		float4 cheekySample = float4(uv.x, uv.x, uv.y, uv.y) + float4(-dUV.x, dUV.x, dUV.y, dUV.y);
		float4 cheekySample2 = float4(uv.x, uv.x, uv.y, uv.y) + float4(dUV.x, -dUV.x, -dUV.y, -dUV.y);

		// Sample top left pixel
		cOut.r = tex2Dlod(s, float4(UnityStereoScreenSpaceUVAdjust(cheekySample.rb, s_ST), 0.0, 0.0)).a;
		// Sample top right pixel
		cOut.g = tex2Dlod(s, float4(UnityStereoScreenSpaceUVAdjust(cheekySample.ga, s_ST), 0.0, 0.0)).a;
		// Sample bottom right pixel
		cOut.b = tex2Dlod(s, float4(UnityStereoScreenSpaceUVAdjust(cheekySample.rb, s_ST), 0.0, 0.0)).a;
		// Sample bottom left pixel
		cOut.a = tex2Dlod(s, float4(UnityStereoScreenSpaceUVAdjust(cheekySample.ga, s_ST), 0.0, 0.0)).a;
		// Average
		cOut.a = min(Min3(cOut.rgb), cOut.a);
		//return tex2D(s, uv);
		return cOut.a;
	}
	
	//END BLURS=====================================================================

	v2f vertPRISM (appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;
		
    	#if UNITY_UV_STARTS_AT_TOP
    		o.uv2 = v.texcoord.xy;				
    		if (_MainTex_TexelSize.y < 0.0)
    			o.uv2.y = 1.0 - o.uv2.y;
    	#endif
		
		return o;
	}
	
	v2fDoF vertPRISMDoF (appdata_DoF v)
	{
		v2fDoF o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.vPosition = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;
		
    	#if UNITY_UV_STARTS_AT_TOP
    		o.uv2 = v.texcoord.xy;				
    		if (_MainTex_TexelSize.y < 0.0)
    			o.uv2.y = 1.0 - o.uv2.y;
    	#endif
    	
		return o;
	}
			
	//Blurs
	float4 fragBlurBox (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
	
		//float4 col = tex2D(_MainTex, i.texcoord);
		float4 col;
		col = KawaseBlur(_MainTex, _MainTex_ST, uv, currentIteration);//BlurColor(i.texcoord).rgb;
		
		return col;
	}  	

	//lumacoeff instead
	//#define CoefLuma vec3(0.2126, 0.7152, 0.0722)      // BT.709 & sRBG luma coefficient (Monitors and HD Television)
	//#define CoefLuma vec3(0.299, 0.587, 0.114)       // BT.601 luma coefficient (SD Television)
	//#define CoefLuma vec3(1.0/3.0, 1.0/3.0, 1.0/3.0) // Equal weight coefficient
	///.. -1 ..
	///-1  5 -1
	///.. -1 ..
	//Tried prewitt sharpen - holy god that looks terrible. 
	/*
	LumaSharpen 1.4.1
	original hlsl by Christian Cann Schuldt Jensen ~ CeeJay.dk
	port to glsl by Anon
	It blurs the original pixel with the surrounding pixels and then subtracts this blur to sharpen the image.
	It does this in luma to avoid color artifacts and allows limiting the maximum sharpning to avoid or lessen halo artifacts.
	This is similar to using Unsharp Mask in Photoshop.
	*/
	half4 sharpen(float2 uv)
	{
		float4 colorInput = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		half2 PixelSize = _MainTex_TexelSize.xy;
	  	
		float3 ori = colorInput.rgb;

		// -- Combining the strength and luma multipliers --
		float3 sharp_strength_luma = (lumacoeff * _SharpenAmount); //I'll be combining even more multipliers with it later on
		
		// -- Gaussian filter --
		//   [ .25, .50, .25]     [ 1 , 2 , 1 ]
		//   [ .50,   1, .50]  =  [ 2 , 4 , 2 ]
	 	//   [ .25, .50, .25]     [ 1 , 2 , 1 ]
        float px = PixelSize.x;//1.0/
		float py = PixelSize.y;

		float3 blur_ori = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(px, -py) * 0.5 * offset_bias, _MainTex_ST)).rgb; // South East
		blur_ori += tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(-px, -py) * 0.5 * offset_bias, _MainTex_ST)).rgb;  // South West
		blur_ori += tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(px, py) * 0.5 * offset_bias, _MainTex_ST)).rgb; // North East
		blur_ori += tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(-px, py) * 0.5 * offset_bias, _MainTex_ST)).rgb; // North West

		blur_ori *= 0.25;  // ( /= 4) Divide by the number of texture fetches

		// -- Calculate the sharpening --
		float3 sharp = ori - blur_ori;  //Subtracting the blurred image from the original image

		// -- Adjust strength of the sharpening and clamp it--
		float4 sharp_strength_luma_clamp = float4(sharp_strength_luma * (0.5 / sharp_clamp),0.5); //Roll part of the clamp into the dot

		float sharp_luma = clamp((dot(float4(sharp,1.0), sharp_strength_luma_clamp)), 0.0,1.0 ); //Calculate the luma, adjust the strength, scale up and clamp
		sharp_luma = (sharp_clamp * 2.0) * sharp_luma - sharp_clamp; //scale down


		// -- Combining the values to get the final sharpened pixel	--
		colorInput.rgb = colorInput.rgb + sharp_luma;    // Add the sharpening to the input color.
		
		return clamp(colorInput, 0.0,1.0);
	}
	
	half4 fragSharpen (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		return sharpen(uv);
	}
	
	//Standard filmic tonemapping (heji dawson)
	half3 filmicTonemap(in float3 col)
	{
	    col = max(0, col - _SecondaryToneParams.x);
	    col = (col * (col * _ToneParams.x + _ToneParams.y)) / (col * (col * _ToneParams.x + _ToneParams.z)+ _SecondaryToneParams.y);
	    //#endif
	    col *= col;
	    
	    return col;
	}	
			
	//Roman Galashov (RomBinDaHouse) operator - https://www.shadertoy.com/view/MssXz7
	half3 filmicTonemapRomB(in float3 col)
	{
	    return exp( _ToneParams.x / ( _ToneParams.y * col.rgb + _ToneParams.z ) );
	}
	
	//https://www.shadertoy.com/view/MdSyWh
	half3 filmicTonemapJodieRobo(in float3 c)
	{
		float l = dot(c, float3(0.2126, 0.7152, 0.0722));
    	float3 tc=c/sqrt(c*c+1.);
    	return lerp(c/sqrt(l*l+1.),tc,tc);
	}	//ACES filmic tonemapping curve. Used in UE4 https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve//
	half3 ACESFilm( float3 x )
	{
	    float aa = _ToneParams.x;
	    float bb = _ToneParams.y;
	    float cc = _ToneParams.z;
	    float dd = _SecondaryToneParams.x;
	    float ee = _SecondaryToneParams.y;
	    return saturate((x*(aa*x+bb))/(x*(cc*x+dd)+ee));
	    
	}
	
	inline float3 ApplyExposure(float3 col, float linear_exposure)
	{
		return col *= max(_ExposureLowerLimit, min(_ExposureUpperLimit, _ExposureMiddleGrey / linear_exposure));
	}	
	
	//Tiltshift - unused atm
	float WeightIrisMode (float2 uv)
	{
		float2 tapCoord = uv*2.0-1.0;
		return (abs(tapCoord.y * _ChromaticIntensity));
	}
	
	inline half4 lookupGamma(sampler3D lookup, float4 c, half2 uv)
	{
		//float4 c = tex2D(_MainTex, uv);
		c.rgb = tex3D(lookup, c.rgb * _LutScale + _LutOffset).rgb;
		return c;
	}

	inline half4 lookupLinear(sampler3D lookup, float4 c, half2 uv)
	{
		//float4 c = tex2D(_MainTex, uv);
		c.rgb= sqrt(c.rgb);
		c.rgb = tex3D(lookup, c.rgb * _LutScale + _LutOffset).rgb;
		c.rgb = c.rgb*c.rgb; 
		return c;
	}
	
	//TV-style vignette (more rectangular than circular)
	inline float4 tvVignette(float4 col, float2 uv)
	{
		float vignette = (2.0 * 2.0 * uv.x * uv.y * (1.0 - uv.x) * (1.0 - uv.y));
		col.rgb *= (float3)(pow(vignette, _VignetteIntensity));
		return col;
	}

	// fixed for Single Pass stereo rendering method
	inline float3 sampleTexChromatic(sampler2D tex, half4 tex_ST, float2 uv, float2 direction, float3 distortion)
	{
		return float3(
			tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + direction * distortion.r, tex_ST)).r,
			tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + direction * distortion.g, tex_ST)).g,
			tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + direction * distortion.b, tex_ST)).b
		);
	}
	
	inline float GetChromaticVignetteAmount(float2 uv)
	{
		if(_ChromaticParams.z > 0.0)//Vertical
		{
			uv.y = uv.x;
		}
		float distChrom = distance(uv, float2(0.5,0.5));
		distChrom = smoothstep(_ChromaticParams.x,_ChromaticParams.y,distChrom);
		return distChrom;
	}

	const float2 cdistortion = float2(0.441, 0.156);
	const float4 cbackground = float4(0.0, 0.0, 0.0, 1.0);

	float poly(float val) {
	  	return 1.0 + (0.441 + 0.156 * val) * val;
	}
	float2 barrel(float2 v, float2 center) {
		  float2 w = v - center;
		  return poly(dot(w, w)) * w + center;
	}

	float3 ChromaticAberrationBarrel(float3 col, inout float2 uv)
	{
		   float distpow = 500.0 * _ChromaticIntensity;
		    
			//note: domain distortion//
			const float2 ctr = float2(0.5,0.5);
			float2 ctrvec = ctr - uv;
			float ctrdist = length( ctrvec );
			ctrvec /= ctrdist;
			//_ChromaticParams.x = 0.0025
			float amount = max(0.0, pow(ctrdist, distpow)-_ChromaticParams.x);
			uv += ctrvec * amount;
			//uv.y *= -1.;
			//col = tex2D(_MainTex, float2(0,1)+(float2(1,-1)*float2(uv.x, uv.y)) ).rgb;
			//col = tex2D(_MainTex, uv ).rgb;
			float3 chromWeights = float3(0.441, 0.156, Luminance(col)) * 5.;//(_ChromaticParams.y * 50.0);
			col.rgb = sampleTexChromatic(_MainTex, _MainTex_ST,  uv, normalize(float2(0.5,0.5) - uv )
			//normalize(uv)
			, chromWeights * _MainTex_TexelSize.x).rgb;

			return col;
	}
		
	inline float3 ChromaticAberration(float3 col, inout float2 uv)
	{

		float distChrom = 0.0;
	
		distChrom = GetChromaticVignetteAmount(uv);
		float amount = _ChromaticIntensity;
		amount *= distChrom;

		float3 tempChromaCol; 
		tempChromaCol.r = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(float2(uv.x + amount, uv.y + amount), _MainTex_ST)).r;
		tempChromaCol.g = col.g;
		tempChromaCol.b = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(float2(uv.x - amount, uv.y - amount), _MainTex_ST)).b;
		tempChromaCol *= (1.0 - amount * 0.5);
		col = lerp(col, tempChromaCol, distChrom);	
		
		return col;
	}
	

	//
    //Ra = max, rB = min
    #if 0
  float b = (t-rA)/(rB-rA);
  b = b*b*b;

  // Uncomment this line to shade image with false colors representing the number of steps
  //rgb = ShadeSteps(s);
  vec3 fog = vec3((0.5,0.5,0.5)*(0.9+0.01*noise(30.0*pixel.xyx+0.8*sin(iGlobalTime))+0.05*noise(10.0*pixel.xyx+0.5*cos(iGlobalTime))));//noise(pixel))*cos(iGlobalTime)+0.5); 
  rgb = mix(rgb,fog,b);

  fragColor=vec4(rgb, 1.0);
}
#endif

	// Noise
	// x : Point in space
	float noise( in float3 x )
	{
	    float3 p = floor(x);
	    float3 f = frac(x);
	    f = f*f*(3.0-2.0*f);
	    
	    float n = p.x + p.y*157.0 + 113.0*p.z;
	    return lerp(
	    			lerp(
	    				lerp( hash(n+  0.0), hash(n+  1.0),f.x),
	                   	lerp( hash(n+157.0), hash(n+158.0),f.x),f.y),
	               			lerp(
	               				lerp( hash(n+113.0), hash(n+114.0),f.x),
	                   			lerp( hash(n+270.0), hash(n+271.0),f.x),f.y),f.z);
	}

//
	inline float3 GlobalFog(float3 col, float2 uv, float4 blmAmount)
	{
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
		float depth_o = LinearEyeDepth(depth_r);	
		//first noise 0.01 second 0.05
		float fogFac = saturate((depth_o - _FogStart)/(_FogDistance - _FogStart));
		//Cube
		//fogFac = fogFac*fogFac*fogFac;
		fogFac = pow(fogFac, _FogParams.b);
		
		float3 fog = _FogColor.rgb;

		/* NOISE HERE
		*
											(
												0.8+_FogParams.r*noise(30.0*uv.xyx+0.8*sin(_Time.y))
												+_FogParams.g*noise(10.0*uv.xyx+0.5*cos(_Time.y))
											);
											*/
  //
      //If 1 > 0.99999 when _FogBlurSkybox is set
        if(Linear01Depth(depth_r) > _FogBlurSkybox)
        {
        	fogFac = 0.0;
        }
        
        //NOISE HERE
        //fogFac = min(fogFac, 1.0);
        
        //if(fogFac > 1.0)
        //return lerp(ONES, ZEROES, fogFac);
        
		col = lerp(col, fog, fogFac);
		//return fog;
		return col;
	}
	
	inline float3 GlobalFogOld(float3 col, float2 uv, float4 blmAmount)
	{
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
		float depth_o = LinearEyeDepth(depth_r);	
		float l = 1.0;
		
		float4 finalFogCol = float4(col.rgb, 1.0);
		
		finalFogCol = lerp(finalFogCol, _FogColor, saturate(((depth_o - _FogStart) / _FogDistance)));
		
		finalFogCol = lerp(finalFogCol, _FogMidColor, saturate(((depth_o - _FogMidPoint) / _FogDistance)));
		
		finalFogCol = lerp(finalFogCol, _FogEndColor, saturate(((depth_o - _FogEndPoint) / _FogDistance)));
		
	   	//If we have bloom, color the fog towards the blooms color based on it's intensity
	   	#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM
	   	finalFogCol.rgb += blmAmount.rgb;
		#endif
		
        //If 1 > 0.99999 when _FogBlurSkybox is set
        if(Linear01Depth(depth_r) > _FogBlurSkybox)
        {
        	l = 0.0;
        }
		//finalFogCol = ZEROES;//
		//finalFogCol.a = 1.0;//
	    return float3(lerp(col,finalFogCol.rgb, l*finalFogCol.a));
	}
	
	float3 highlights(float3 col, float thres)
	{
		#if PRISM_HIGHLIGHT_THRESH
		float val = (col.x + col.y + col.z) / 3.0;
		return col * smoothstep(thres - 0.1, thres + 0.1, val);
		#elif PRISM_GRADUAL_THRESH
		col *= saturate(Luminance(col) / thres);
		return col;
		#else
        // Pixel brightness
        half br = Brightness(col);
            
        // Under-threshold part: quadratic curve
        half rq = clamp(br - _BloomCurve.x, 0.0, _BloomCurve.y);
        rq = _BloomCurve.z * rq * rq;

        // Combine and apply the brightness response curve.
        col *= max(rq, br - thres) / max(br, 1e-4);
		#endif
		return col;
		
	}
	
	// fixed for Single Pass stereo rendering method
	half3 UpsampleFilter(sampler2D tex, half4 tex_ST, float2 uv, float2 texelSize, float sampleScale)
	{
#if MOBILE_OR_CONSOLE
		// 4-tap bilinear upsampler
		float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

		half3 s;
		s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.xy, tex_ST));
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.zy, tex_ST));
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.xw, tex_ST));
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.zw, tex_ST));

		return s * (1.0 / 4.0);
#else
		// 9-tap bilinear upsampler (tent filter)
		float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

		half3 s;
		s = tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv - d.xy, tex_ST));
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv - d.wy, tex_ST)) * 2.0;
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv - d.zy, tex_ST));

		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.zw, tex_ST)) * 2.0;
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv, tex_ST))        * 4.0;
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.xw, tex_ST)) * 2.0;

		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.zy, tex_ST));
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.wy, tex_ST)) * 2.0;
		s += tex2D(tex, UnityStereoScreenSpaceUVAdjust(uv + d.xy, tex_ST));

		return s * (1.0 / 16.0);
#endif
	}


	float4 fragSmartUpsample (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;	
		#endif
		
		float4 blm = ZEROES;
		
		blm.rgb = UpsampleFilter(_MainTex, _MainTex_ST, uv, _MainTex_TexelSize.xy, _BloomRadius);
		
		return blm;
	}
	
	
	float3 remap( float a, float b, float3 v ) {
    	//return (v-a) / (b-a);
		return clamp( (v-((float3)(a))) / ((float3)(b-a)), 0.0, 1.0 );
	}
	
	float SAbs(float x, float k)
	{
	    return sqrt(x * x + k);
	}

	float SRamp1(float x, float k)
	{
	    return 0.5 * (x - SAbs(x, k));
	}
	
	float SMin1(float a, float b, float k)
	{
	    return a + SRamp1(b - a, k);
	}
	
	float3 SMin1(float3 a, float3 b, float k)
	{
		return float3( SMin1( a.r, b.r, k ),
			     SMin1( a.g, b.g, k ),
			     SMin1( a.b, b.b, k ) );
	}
	
	float3 remap_noclamp( float a, float b, float3 v )
	{
		return (v-float3(a,a,a)) / (b-a);
	}
	
	//_BrightIntensity
	inline float3 gamma_softmin(float3 x)
	{
	    float4 parms = float4(0.0, _BrightCurve.g, _BrightCurve.r, _BrightCurve.b);

		x = max( x, parms.xxx ); //note: clamp below min
		x = remap_noclamp( parms.x, parms.y, x );

		//1:
		float pexp = 1.0 / (1.0 - 0.5*parms.z);
		x = pow( max(0.00000001, x), float3(pexp, pexp, pexp) );

		float pmin = parms.w;
		x = SMin1( x, float3(1.0, 1.0, 1.0), pmin ); //upper

		return x;
	}
	
	#define softmin0 gamma_softmin(ZEROES.rgb).r
	#define softmin1 gamma_softmin(ONES.rgb).r
	
	inline float3 AddBrightness( float3 col)
	{
	    float3 softmin = gamma_softmin( col);
	    
	    //note: renormalize (0,0) and (1,1) values
	    //note: should be done in app or vertex-shader
	    //float softmin0 = gamma_softmin( ZEROES.rgb).r;
	    //float softmin1 = gamma_softmin( ONES.rgb).r;
	    
	    return remap( softmin0, softmin1, softmin);
	}

	inline float3 CombineBloom(float3 col,float2 uv, out float3 blmTex)
	{
		#if	PRISM_HDR_BLOOM 
		
			#if SHADER_TARGET > 20  
			//DECENT GPUS
			blmTex = tex2D(_Bloom1, UnityStereoScreenSpaceUVAdjust(uv, _Bloom1_ST))* _Bloom1Intensity;
			
			blmTex += tex2D(_Bloom2, UnityStereoScreenSpaceUVAdjust(uv, _Bloom2_ST)) * _Bloom2Intensity;
			blmTex += tex2D(_Bloom3, UnityStereoScreenSpaceUVAdjust(uv, _Bloom3_ST)) * _Bloom3Intensity;
			
			
			#if PRISM_USE_STABLEBLOOM
			float4 blmFourth = tex2D(_Bloom4, UnityStereoScreenSpaceUVAdjust(uv, _Bloom4_ST));
			float3 accValue = tex2D(_BloomAcc, UnityStereoScreenSpaceUVAdjust(uv, _BloomAcc_ST)).rgb;
			blmTex += lerp(accValue.rgb, blmFourth.rgb, 0.7);
			blmTex *= _Bloom4Intensity;
			#else
			blmTex += tex2D(_Bloom4, UnityStereoScreenSpaceUVAdjust(uv, _Bloom4_ST)) * _Bloom4Intensity;
			#endif
			
			blmTex *= _BloomIntensity;
			
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, UnityStereoScreenSpaceUVAdjust(uv, _BrightnessTexture_ST)).r;
			blmTex.rgb = ApplyExposure(blmTex.rgb, linear_exposure);
			#endif
			
			//DEBUG
			//Dreturn tex2D(_Bloom4, uv).rgb * _Bloom4Intensity;
			
			return col + blmTex;
			#else 
			return ONES;//POTATO
			#endif
		
		#else
			//SIMPLE BLOOM
			blmTex = tex2D(_Bloom1, UnityStereoScreenSpaceUVAdjust(uv, _Bloom1_ST)).rgb;
			
			#if PRISM_USE_STABLEBLOOM
			float3 accValue = tex2D(_BloomAcc, UnityStereoScreenSpaceUVAdjust(uv, _BloomAcc_ST)).rgb;
			blmTex.rgb = lerp(accValue.rgb, blmTex.rgb, 0.7);
			#endif
			
			blmTex.rgb *= _BloomIntensity;
		
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, UnityStereoScreenSpaceUVAdjust(uv, _BrightnessTexture_ST)).r;
			blmTex.rgb = ApplyExposure(blmTex.rgb, linear_exposure);
			#endif
		
			#if PRISM_BLOOM_SCREENBLEND
			col.rgb = 1.0 - ((1.0 - blmTex.rgb)*(1.0 - col.rgb));
			//col.rgb = lerp(col.rgb, blmTex.rgb;
			#else
			col.rgb += blmTex.rgb;
			#endif
		#endif
		
		return col;
	}
	
	float4 fragBloomDebug (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;	
		#endif
		
		float4 blm = ZEROES;
		
		#if PRISM_UIBLUR
		//blm.rgb = lerp(blm.rgb, (float3)0.0, min((_BloomThreshold - blm.a), 1.0));
		//blm.rgb *= _BloomIntensity;
		#endif

		float3 derp;
		blm.rgb = CombineBloom(blm.rgb, uv, derp);
		
		return blm;
	}
	
	#pragma region flares
	
	uniform float _FlareGhostDispersal;
	uniform int _FlareNumberOfGhosts;
	//uniform sampler2D _FlareRadialColorTex;
	//half4 _FlareRadialColorTex_ST;
	uniform sampler2D _FlareStarburstTex;
	half4 _FlareStarburstTex_ST;
	uniform sampler2D _FinalFlareTex;
	half4 _FinalFlareTex_ST;
	half4 _FinalFlareTex_TexelSize;
	uniform float _FlareHaloWidth;
	uniform float _FlareChromaticDistortion;
	uniform float _FlareStrength;

	//http://john-chapman-graphics.blogspot.com.au/2013/02/pseudo-lens-flare.html//
	float4 fragFlareFeatures (v2f i) : SV_Target
	{
		//Flip
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = -i.uv2 + (half2)1.0;
		#else
		float2 uv = -i.uv + (half2)1.0;	
		#endif
		
      	float2 pixelSize = _MainTex_TexelSize.xy;
      	
   		// ghost vector to image centre:
      	float2 ghostVec = (float2(0.5,0.5) - uv) * _FlareGhostDispersal;
      	//TODO PIXELSIZE THIS
      	//Chromatic params
  	   	float3 distortion = float3(-pixelSize.x * _FlareChromaticDistortion, 0.0, pixelSize.x * _FlareChromaticDistortion);

   		float2 direction = normalize(ghostVec);
   
	   	// sample ghosts:  
      	float4 col = ZEROES;
      	
      	for (int i = 0; i < _FlareNumberOfGhosts; ++i) { 
      	
         	float2 offs = frac(uv + ghostVec * (float)i);
         	
         	//Bright spots from centre only!
         	float weight = length(float2(0.5,0.5) - offs) / length(float2(0.5,0.5));
      		weight = pow(1.0 - weight, 10.0);
         	
         	col.rgb += sampleTexChromatic(_MainTex, _MainTex_ST, offs, direction, distortion).rgb * weight;
      	}
      	
      	//Color the flare 
      	//length(float2(0.5,0.5)) - uv
      	col.rgb *= sampleTexChromatic(_FlareStarburstTex, _FlareStarburstTex_ST, uv, direction, distortion).rgb / length(float2(0.5,0.5));
		
		// sample halo:// 
	   	float2 haloVec = normalize(ghostVec) * _FlareHaloWidth;
	   	float haloWeight = length(float2(0.5,0.5) - frac(uv + haloVec)) / length(float2(0.5,0.5));
	   	haloWeight = pow(1.0 - haloWeight, 5.0);
	   	
	   	col.rgb += sampleTexChromatic(_MainTex, _MainTex_ST, uv + haloVec, direction, distortion).rgb * haloWeight;
		col.a = 1.0;
		

		
		//return tex2D(_MainTex, uv + haloVec);
		
      	return col;
	}
	uniform float _CamRotation;
	inline float3 CombineFlare(float2 uv, float3 col, float4 dirt)
	{
		float2 starUV = uv;
		//Rotate starburst
		#if !SHADER_API_METAL
		starUV.xy -= 0.5; // shift the center of the coordinates to (0,0)
		float s, c;
		sincos(radians(_CamRotation), s, c); // compute the sin and cosine
		float2x2 rotationMatrix = float2x2(c, -s, s, c);
		starUV = mul(starUV, rotationMatrix);
		starUV += 0.5; // shift the center of the coordinates back to (0.5,0.5)
		#endif

		//Scale flare tex
		float2 scaleCenter = float2(0.5f, 0.5f);
		starUV = (starUV - scaleCenter) * 0.7 + scaleCenter;

      	float4 lensStarburst = tex2D(_FlareStarburstTex, UnityStereoScreenSpaceUVAdjust(starUV,_FlareStarburstTex_ST));
      	float4 lensFlare = tex2D(_FinalFlareTex, UnityStereoScreenSpaceUVAdjust(uv, _FinalFlareTex_ST)) * _FlareStrength * lensStarburst;
      	lensFlare += dirt * lensFlare * _FlareDirtIntensity;
      	return col.rgb + lensFlare.rgb;	}
	
	inline float3 CombineFlareLight(float2 uv, float3 col)
	{
		float4 lensFlare = tex2D(_FinalFlareTex, UnityStereoScreenSpaceUVAdjust(uv, _FinalFlareTex_ST));
      	col.rgb += lensFlare.rgb * _FlareStrength;
      	return col;
	}
	
	float4 fragFlareAdd (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;	
		#endif
		
		float4 col = ZEROES;// tex2D(_MainTex, uv);
		//
      	col.rgb = CombineFlare(uv,col.rgb, ZEROES);
		
		return col;
	}
	
	#pragma endregion
	
	//depth_o = eyedepth
	inline float GetDoFCoCNew(float depth_o, float focusDist)
	{//
		float fadeAmount = 0.5 * (depth_o - focusDist) * _DofLensCoeff / depth_o;
        return fadeAmount;
	}
	
	inline float GetDoFCoC(float fadeAmount, float depth_o, float focusDist)
	{
        return fadeAmount / focusDist;//fadeAmount / _DofFocusDistance;
    	
	}
	
	float4 accumCol = ZEROES;
	float4 accumW = ZEROES;
	
	float4 fragDoFDebug (v2f i) : SV_Target
	{		
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
	
        float fadeAmount = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)).a;
		
        //fadeAmount = (clamp (fadeAmount, 0.0, 1.0)); 
        
        if(_DofFactor == -100.0)
        {
        	fadeAmount = LinearEyeDepth(PRISM_SAMPLE_DEPTH_TEXTURE(uv));
        	float4 ret = ZEROES;
        	ret.r = fadeAmount;
        	return ret;
        }
        
        float4 finalCol = float4(1.0, 0.0,0.0,0.0);
        if(fadeAmount < 0.0)
        {
        	finalCol = float4(0.0,1.0,0.0,0.0);
        }
        
        //half a = fadeAmount * 2;
		//return saturate(half4(abs(a), a, a, 1));
        
        return lerp(ZEROES, finalCol, abs(fadeAmount));
	}
	
	//_DofFocusDistance = lerp(_DofFocusPoint, _DofFocusDistance, saturate(_DofNearFocusDistance-_DofNearFocusPoint));
	float4 fragDoFPrepass (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(i.uv2);
		#else
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(i.uv);
		#endif

		float2 uv = i.uv;

		float depth_o = LinearEyeDepth(depth_r);
		
        float fadeAmount = (depth_o - _DofFocusPoint);
        fadeAmount /= _DofFocusDistance;
		
		#if defined(PRISM_DOF_USENEARBLUR)
		if(fadeAmount < 0.0)
		{
			float minfadeAmount = (depth_o - _DofNearFocusPoint);
        	minfadeAmount /= _DofNearFocusDistance;
        
        	if(minfadeAmount > 0.0 && minfadeAmount > fadeAmount) minfadeAmount = 0.0;	
        	fadeAmount = minfadeAmount;
		}
		
		
		#endif
		
		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		col.a = fadeAmount;
		
        if(Linear01Depth(depth_r) > _DofBlurSkybox)
        {
        	col.a = 0.0;
        }
		
		return col;
	}
	
	float4 bokehBlurColorAll(float4 origCol, sampler2D DoFSamplr, half4 DoFSamplr_ST, float2 uv, float2 uvSec, float2 pixelSize)
	{		
		float4 ret = ZEROES;
		float fadeAmount;
		
		#if defined(PRISM_DOF_USENEARBLUR)
		fadeAmount = clamp(origCol.a, -1.0, 1.0);
		
		//Actually, our fadeamount isn't 0 here. If 0, we check around to see what the LOWEST value (aka front) fadeamount value is first
		#if defined(UNITY_COMPILER_HLSL)
		[branch]	
		#endif
		if(fadeAmount <= 0.0)
		{
	        float4 minCols;
	        
	        minCols[0] = KawaseBlurMin(DoFSamplr, DoFSamplr_ST, uv, 0, pixelSize);   
	        minCols[1] = KawaseBlurMin(DoFSamplr, DoFSamplr_ST, uv, 1, pixelSize);   
	        minCols[2] = KawaseBlurMin(DoFSamplr, DoFSamplr_ST, uv, 2, pixelSize);   
	        minCols[3] = KawaseBlurMin(DoFSamplr, DoFSamplr_ST, uv, 3, pixelSize);   
	    	
	    	fadeAmount = min(Min3(minCols.rgb),minCols.a);
	    	//fadeAmount = newFadeAmount;
	    	//if(newFadeAmount < fadeAmount) return ONES;////
		}
		
		#else
		fadeAmount = clamp(origCol.a, 0.0, 1.0);
		#endif
	
		float4 accumCol = ZEROES;
		float4 accumW = ZEROES;
		
		//UNITY_UNROLL
		float signOrig = sign(origCol.a);
		
		float abFadeAmount = abs(fadeAmount);
		
		#if defined(UNITY_COMPILER_HLSL)
		#if SHADER_TARGET > 40
		[unroll]	
		#endif
		#endif
			for (int j = -DOF_SAMPLES; j < DOF_SAMPLES; j += 1)
			#if defined(UNITY_COMPILER_HLSL)
			#if SHADER_TARGET > 40
			[unroll]	
			#endif
			#endif
				for (int i = -DOF_SAMPLES; i < DOF_SAMPLES; i += 1){
				
					float2 pOffset = pixelSize * float2(i + j, j - i);
					
					float4 col = tex2D(DoFSamplr, UnityStereoScreenSpaceUVAdjust(uv + pOffset * (_DofRadius * abFadeAmount), DoFSamplr_ST));
					
					//If the original color IS IN THE BACK PLANE and the sampled color IS NOT IN THE SAME PLANE, use the ORIGINAL COLOR (dont blur)
					if(signOrig == 1.0 && sign(col.a) != signOrig) col = origCol;
					
					float4 bokeh = ONES + (col * col) * col * (float4)_DofFactor;
					accumCol += col * bokeh;
					accumW += bokeh;
				}		
				
		ret = accumCol/accumW;		

		return float4(ret.rgb * abFadeAmount, abFadeAmount);// origCol.a);
	}
	
	float4 fragDoFBlur (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float4 origCol = tex2D(_DoF2, UnityStereoScreenSpaceUVAdjust(uv, _DoF2_ST));
		
		#if SHADER_API_GLCORE || SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES2 || SHADER_API_GLES3
		origCol = bokehBlurColorAll(origCol, _DoF2, _DoF2_ST, i.uv, uv, _DoF2_TexelSize);
		#else
		origCol = bokehBlurColorAll(origCol, _DoF2, _DoF2_ST, i.uv2, i.uv, _DoF2_TexelSize);
		#endif		
		
		return origCol;
	}
	
	inline void CombineDoF (float2 uv, inout float4 col)
	{
		float4 dofBack = tex2D(_DoF2, UnityStereoScreenSpaceUVAdjust(uv, _DoF2_ST));
		
		col.rgb = col.rgb * lerp(1.0, 0.0, dofBack.a) + dofBack.rgb;
	}
	
	//
	float4 fragDoFCombine (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
						
		//Get base color
		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		//float4 col = ZEROES;//
		CombineDoF(uv, col);
		
		return col;
	}
	
	//Just outputs luminance
	float4 fragLuminance (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		return (float4)Luminance(col.rgb);//float4((float3)Luminance(col.rgb), col.a);
	}	
	
	//Don't make it harder than it needs to be :)
	float4 fragAdapt (v2f i) : SV_Target
	{
		//MainTex = current luminance
		//BrightTex = last adapted luminance
	
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif		
		
		float4 col = (float4)tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).r;
		
		col.a =0.0125 * _ExposureSpeed;
		return col;
	}
	
	float4 fragApplyAdaptDebug(v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		float linear_exposure = tex2D(_BrightnessTexture, UnityStereoScreenSpaceUVAdjust(uv, _BrightnessTexture_ST)).r;//float2(0.5, 0.5));
	    
	    if(uv.x < 0.50)
	    {
	    	return (float4)linear_exposure;
	    }
	    
	    col.rgb = ApplyExposure(col.rgb, linear_exposure);
	    
	    return col;
	}
	
	// Gamma encoding function for AO value
	// (do nothing if in the linear mode)
	half EncodeAO(half x)
	{
		//This is probably faster than the pow
	    // ColorSpaceLuminance.w is 0 (gamma) or 1 (linear).
		if(unity_ColorSpaceLuminance.w == 1)
		{
			return x;
		}
		
	    // Gamma encoding
	    return (1 - pow(1 - x, 1 / 2.2));
	}
	
	// Final ao combiner function
	inline float3 CombineObscurance(float3 src, float3 ao, float2 uv)
	{
		float3 hdrSrcZero = (float3)0.0;
		
		//Need to pow it so that all areas don't get -'ed a bit
		#if SHADER_TARGET < 30 || SHADER_API_MOBILE
		
		#else
		ao -= (pow(Luminance(src), 2.) * _AOLuminanceWeighting);
		#endif
		
		//return lerp(hdrSrcZero, src, max(EncodeAO(ao),0.0));
	    return lerp(src, hdrSrcZero,max(EncodeAO(ao),0.0));
	}
	
	inline float3 FilmicGrain(float3 col, float2 uv)
	{
		float rand = nrandtime(uv, _RandomInts.x, _RandomInts.y);
		
		uv.xy += rand;
		
		float3 grain = tex2D(_GrainTex, UnityStereoScreenSpaceUVAdjust(uv, _GrainTex_ST)).rgb * rand;
		
		//return lerp(col, col - grain, _GrainIntensity.x);
		//return grain;
		// 3x V_ADD_F32, 3x V_MIN_F32, 3x V_MAC_F32
		return grain * min(col + _GrainIntensity.x, _GrainIntensity.y) + col; 
		//
		return col;
	}
	
	half TransformColor (half4 skyboxValue) {
		return dot(max(skyboxValue.rgb - _SunThreshold.rgb, half3(0,0,0)), half3(1,1,1)); // threshold and convert to greyscale
	}
	
	float4 fragSunPrepass (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float depthSample = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
		depthSample = Linear01Depth (depthSample);
		
		half2 vec = _SunPosition.xy - i.uv;		
		half dist = saturate (_SunPosition.w - length (vec.xy));
		
		half4 outColor = 0;
		
		// consider shafts blockers
		if (depthSample >= 0.9999)
		{
			half4 tex = tex2D (_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
			outColor = TransformColor (tex) * dist;	
		}
		
		return outColor;
	}
	
	float4 fragSunDebug (v2f i) : SV_Target
	{
		return tex2D (_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
	}
	
	inline float3 CombineSun(float3 realColor, float2 uv)
	{
		float4 col = tex2D (_RaysTexture, UnityStereoScreenSpaceUVAdjust(uv, _RaysTexture_ST));
		
		col = FastToneMapPRISM(col);
 		
 		return ((float4((col.rgb * _SunExposure),1.))+(realColor*(1.1)));
	}

	float4 fragSunCombine (v2f i) : SV_Target
	{
		/*#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif*/
		float2 uv = i.uv;//
		
		half4 realColor = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
		realColor.rgb = CombineSun(realColor.rgb, uv);
		return realColor;
	}
	
	float4 fragSunBlur (v2f i) : SV_Target
	{
		/*#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif*/
		float2 uv = i.uv;//
		
		#if SHADER_TARGET < 30
		return ONES;
		#endif
		//float _SunWeight0.58767;
		
		const int NUM_SUN_SAMPLES = 98;
		
		float2 deltaTexCoord = (uv - _SunPosition.xy);
		deltaTexCoord *= 1.0 / (float)NUM_SUN_SAMPLES * _SunDensity;
		float illuminationDecay = 1.0;
		
		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST))*0.3;
		
		for(int i = 0; i < NUM_SUN_SAMPLES; i++)
		{
			uv -= deltaTexCoord;
			float4 sunSampleBeep = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST))*0.4;
			sunSampleBeep *= (illuminationDecay * _SunWeight);
			col += sunSampleBeep;
			illuminationDecay *= _SunDecay;
		}
		
		col.rgb = saturate (col.rgb * _SunColor.rgb);

		return col;
	}
	
	inline float3 CombineVignette(float3 col, float2 uv)
	{
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
		if(_VignetteIntensity > 0.0)
		{
			float d = distance(uv, float2(0.5,0.5));
			float vignette_black = smoothstep(_VignetteStart, _VignetteEnd, d * _VignetteIntensity);
			col.rgb = lerp(_VignetteColor.rgb * col.rgb, col.rgb, vignette_black); // here!
		}
		
		return col;
	}

	inline float3 ApplyDither(float3 col, float2 uv)
	{
		float3 ditherCol;
		float2 seed = uv;
		seed += frac(_Time.y);
		float ditherAmount = 0.045 * _GrainIntensity.y;

		ditherCol.r = ( hash12(seed) + hash12(seed+0.59374) - 0.5);
		seed += 0.1;
		ditherCol.g = ( hash12(seed) + hash12(seed+0.59374) - 0.5);
		seed += 0.04;
		ditherCol.b = ( hash12(seed) + hash12(seed+0.59374) - 0.5);

		col += ditherCol * ditherAmount;
		return col;
	}
	
	// Copyright (c) 2016, bacondither
	// All rights reserved.
	//
	// Redistribution and use in source and binary forms, with or without
	// modification, are permitted provided that the following conditions
	// are met:
	// 1. Redistributions of source code must retain the above copyright
	//    notice, this list of conditions and the following disclaimer
	//    in this position and unchanged.
	// 2. Redistributions in binary form must reproduce the above copyright
	//    notice, this list of conditions and the following disclaimer in the
	//    documentation and/or other materials provided with the distribution.
	//
	// THIS SOFTWARE IS PROVIDED BY THE AUTHORS ``AS IS'' AND ANY EXPRESS OR
	// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
	// IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
	// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
	// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
	// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
	// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
	// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
	// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	// EXPECTS FULL RANGE GAMMA LIGHT
	inline float3 ApplyColorify(float3 col)
	{
		col  = saturate(col);
		float luma = dot(col, lumacoeff);//Luminance(col);//to_gamma( max(dot(to_linear(col), lumacoeff), 0) );

		float3 colour = luma + (col - luma)*(max(_Colourfulness, -1) + 1);

		float3 diff = colour - col;
		
		//#if defined( HD_COLORIFY )
		#if SHADER_API_GLCORE || SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_GLES2 || SHADER_API_WIIU
		
		/*// 120% of colour clamped to max range + overshoot
		float3 ccldiff = clamp((diff*1.2) + col, -0.0001, 1.0001) - col;

		// Calculate maximum saturation-increase without altering ratios for RGB
		float3 diff_luma = col - luma;

		float poslim = (1.0001 - luma)/max3(max(diff_luma, 0));
		float neglim = (luma + 0.0001)/max3(abs(min(diff_luma, 0)));

		float diffmul = min(min(abs(poslim), abs(neglim)), 32);

		float3 diffmax = (luma + diff_luma*diffmul) - col;

		// Soft limit diff
		diff = soft_lim( diff, wpmean(diffmax, ccldiff, lim_luma) );*/
		diff = glslTanh(diff);

		#else
		//Or not, since that's slow
		diff = tanh(diff);
		#endif

		return float3( col + diff);
	}
	
	//Final combine //
	float4 fragFinal (v2f i) : SV_Target
	{	
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else                  
		float2 uv = i.uv;
		#endif                 
		float2 mainUv = i.uv;

		float4 col = ONES;   	
		float3 blmTex;
		float midEyeDepth = 0.0;
		
		//float4 accValue = tex2D(_BloomAcc, uv);
		//return accValue;
		
		//Potato devices and WebGL. Let's try for the usual ones...
		#if SHADER_TARGET < 30
		//test
			col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(mainUv, _MainTex_ST));
			
			if(_ChromaticIntensity > 0.0)
			{
				col.rgb = ChromaticAberration(col.rgb, mainUv);//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			} 
		
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, UnityStereoScreenSpaceUVAdjust(uv, _BrightnessTexture_ST)).r;
			col.rgb = ApplyExposure(col.rgb, linear_exposure);
			#endif
			
			//Add bloom	- CAN'T BE STABLE BLOOM
			#if PRISM_USE_BLOOM
			
				#if !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR		
				float4 blmTex = tex2D(_Bloom1, UnityStereoScreenSpaceUVAdjust(uv, _Bloom1_ST));				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
				col.rgb = blmTex.rgb;				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
				#elif PRISM_UIBLUR
				#endif
			
				col.rgb = CombineBloom(col.rgb, uv, blmTex);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			#endif			
			
			col.rgb = CombineVignette(col.rgb, uv);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			
			#if PRISM_FILMIC_TONEMAP
			col.rgb = filmicTonemapJodieRobo(col.rgb);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			#endif
			
			#if PRISM_ROMB_TONEMAP		
			col.rgb = filmicTonemapRomB(col.rgb);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			#endif
			
			#if PRISM_ACES_TONEMAP	
			col.rgb = ACESFilm(col.rgb);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			#endif
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
		    if(_Gamma > 0.0)
		    {
		    	col = pow(col, _Gamma);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
		    }
		    
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(_Colourfulness > 0.0)
			{
				col.rgb = ApplyColorify(col.rgb);				//POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			}
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(useNoise > 0.0)
			{
				col.rgb = ApplyDither(col.rgb, uv);				//ENDDDDDDDDDDDDDDD        POTATOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
			}
		
		#else
		
			col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(mainUv, _MainTex_ST));
		
			#if PRISM_USE_DOF
			//TODO OPENGL SHIT
			CombineDoF(uv, col);
			#else
			
			#endif
			
			#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_TARGET > 30
				[branch]
				#endif
			#endif
			if(_ChromaticIntensity > 0.0)
			{
				#if defined(UNITY_COMPILER_HLSL)
					#if SHADER_TARGET > 30
					[branch]
					#endif
				#endif
				if(_ChromaticParams.b == -1.0)
				{
					col.rgb = ChromaticAberrationBarrel(col.rgb, mainUv);
				} else {
					col.rgb = ChromaticAberration(col.rgb, mainUv);
				}
			} 

			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, UnityStereoScreenSpaceUVAdjust(uv, _BrightnessTexture_ST)).r;
			col.rgb = ApplyExposure(col.rgb, linear_exposure);
			#endif
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(_AOIntensity > 0.0)
			{
				half ao = tex2D(_AOTex, UnityStereoScreenSpaceUVAdjust(uv, _AOTex_ST)).r;
				
				col.rgb = CombineObscurance(col.rgb, (float3)ao, uv);
			}

			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(useNoise > 0.0)
			{
				col.rgb = ApplyDither(col.rgb, uv);
			}
			
			//Add bloom		
			#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM
			
				#if !PRISM_USE_DOF && !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR
				float4 blrTex = tex2D(_Bloom1, UnityStereoScreenSpaceUVAdjust(uv, _Bloom1_ST));
				col.rgb = blrTex.rgb;
				#endif
				
				col.rgb = CombineBloom(col.rgb, uv, blmTex);
				
			#endif
			//End add bloom
				
			#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM
				float4 dirtVal;
				#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_API_D3D11 || SHADER_API_D3D12
				[branch]
				#endif
				#endif	
				if(_DirtIntensity > 0.0)
				{
					//We now have the blurred texture. Now blend it with the original brightpass
					dirtVal = tex2D(_DirtTex, UnityStereoScreenSpaceUVAdjust(uv,_DirtTex_ST));

					#if defined(PRISM_HDR_BLOOM) && SHADER_TARGET > 40
					blmTex = UpsampleFilter(_Bloom4, _Bloom4_ST,UnityStereoScreenSpaceUVAdjust(uv,_Bloom4_ST), _Bloom4_TexelSize.xy, 2);
					#else
					blmTex = UpsampleFilter(_Bloom4, _Bloom4_ST,UnityStereoScreenSpaceUVAdjust(uv,_Bloom4_ST), _Bloom4_TexelSize.xy, 2);
					#endif

					float3 scratchBlend = col.rgb + dirtVal.rgb;

					float luminance = dot(blmTex.rgb, lumacoeff);// Luminance(blmTex.rgb);
					//return float4(blmTex.rgb, 1.0);
					float addOn = lerp(0.0, _DirtIntensity, clamp(luminance+0.1, 0.0,1.0));
					if(luminance > 0.01)
					{
						luminance += addOn;
					}

					float brightnessMap = smoothstep(0.1, 0.4, luminance * dirtVal.a);
					col.rgb = lerp(col.rgb, scratchBlend, brightnessMap);
				}	
				
				#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_API_D3D11 || SHADER_API_D3D12
				[branch]
				#endif
				#endif	
				if(_FlareStrength > 0.0)
				{
					col.rgb = CombineFlare(uv,col.rgb, dirtVal);
				}
				
			#endif
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(_FogIntensity > 0.0)
			{
				#if PRISM_HDR_BLOOM || PRISM_SIMPLE_BLOOM
				col.rgb = GlobalFog(col.rgb, uv, tex2D(_Bloom1, UnityStereoScreenSpaceUVAdjust(uv, _Bloom1_ST)));
				#else
				col.rgb = GlobalFog(col.rgb, uv, ZEROES);
				#endif
			}		
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(_BrightCurve.r > 0.0)
			{
				col.rgb = AddBrightness(col.rgb);
			}
			
			#if !USE_VIGNETTE
			col.rgb = CombineVignette(col.rgb, uv);
			#endif	
			
			#if PRISM_FILMIC_TONEMAP
			col.rgb = filmicTonemapJodieRobo(col.rgb);
			#endif
			
			#if PRISM_ROMB_TONEMAP		
			col.rgb = filmicTonemapRomB(col.rgb);
			#endif
			
			#if PRISM_ACES_TONEMAP		
			col.rgb = ACESFilm(col.rgb);
			#endif
			
		    //#if USE_GAMMA
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
		    if(_Gamma > 0.0)
		    {
		    	col = pow(col, _Gamma);
		    }
			
			#if PRISM_GAMMA_LOOKUP
			col = lerp(col, lookupGamma(_LutTex, col, uv), _LutAmount);
			
			#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_TARGET > 30
				[branch]
				#endif
			#endif
			if(_SecondLutAmount > 0.0)
			{
				col = lerp(col, lookupGamma(_SecondLutTex, col, uv), _SecondLutAmount);
			}
				
			#elif PRISM_LINEAR_LOOKUP
			col = lerp(col, lookupLinear(_LutTex, col, uv), _LutAmount);
			
			#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_TARGET > 30
				[branch]
				#endif
			#endif
			if(_SecondLutAmount > 0.0)
			{
				col = lerp(col, lookupLinear(_SecondLutTex, col, uv), _SecondLutAmount);
			}
			#endif
			
			#if defined(UNITY_COMPILER_HLSL)
			[branch]
			#endif
			if(_Colourfulness > 0.0)
			{
				col.rgb = ApplyColorify(col.rgb);
			}
			
			#if defined(UNITY_COMPILER_HLSL)
				#if SHADER_TARGET > 30
				[branch]
				#endif
			#endif
			if(useNoise > 5.0)
			{
				col.rgb = FilmicGrain(col.rgb, uv);
			}
		#endif

		return col;
	}
	
	float4 fragChromatic (v2f i) : SV_Target
	{		
#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
#else
		float2 uv = i.uv;
#endif

		float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
		col.rgb = ChromaticAberration(col.rgb, uv);
		return col;
	}
	
	
	#define SAMPLES 10
	float4 fragChromaticBlur (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		//No blur on SM2
		#if SHADER_TARGET < 30   
		
		float chromAmount = GetChromaticVignetteAmount(i.uv); 
		float3 col = (float3)0.0;
	    
	    col = lerp(tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).rgb, col, chromAmount);
	    
	    return float4(col, 1.0);
	    
		#else
		
		float chromAmount = GetChromaticVignetteAmount(i.uv); 
    	float3 res = ZEROES.rgb;
    	//UNITY_UNROLL
	    for(int i = 0; i < SAMPLES; ++i) {
	        res += tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)).xyz;
	        float2 d = float2(0.5, 0.5)-uv;
			//#ifdef PRISM_CHROMATICBLURNOISE
	       	//d *= .5 + .01*nrandtime(d, 0.0, 0.0);
	        //#endif
	        uv += d * _ChromaticParams.a * chromAmount;
	    }
	    
	    return float4(res/SAMPLES, 1.0);   
		#endif
	}
	
	float4 fragGaussianBlur (v2f i) : SV_Target
	{
#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
#else
		float2 uv = i.uv;
#endif
		
		//No blur on SM2
		#if SHADER_TARGET < 30   
		float3 col = (float3)0.0;
	    return float4(col, 1.0);
		#else
		float4 col = KawaseBlur(_MainTex, _MainTex_ST, uv, 1);
	    
	    return col;
		#endif
	}
	
	static const half coefficients[5] = { 0.0625, 0.25, 0.375, 0.25, 0.0625 };
	uniform float2 offsets[5];
	
	//https://d3cw3dd2w32x2b.cloudfront.net/wp-content/uploads/2012/06/faster_filters.pdf
	float4 fragBlurHorizontal (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		float2 ps = _MainTex_TexelSize.xy ;
	    float4 c = ZEROES;
	    
	    /*4.30908 * 055028
		-2.37532 * 0.244038
		-0.50000 * 401870
		1.37532 * 244038
		3.30908 * 055028*/
		
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(-4.30908, 0.5)*ps, _MainTex_ST))*0.055028;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(-2.37532, 0.5)*ps, _MainTex_ST))*0.244038;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(-0.50000, 0.5)*ps, _MainTex_ST))*0.401870;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(1.37532, 0.5)*ps, _MainTex_ST))*0.244038;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(3.30908, 0.5)*ps, _MainTex_ST))*0.055028;
	    
	    /*float4 fastOffsets = float4(-3.5, -1.5, 1.5, 3.5) * ps.xxxx;

	    c += coefficients[0] * tex2D(_MainTex, uv + float2(fastOffsets.x,0.0));
	    c += coefficients[1] * tex2D(_MainTex, uv + float2(fastOffsets.y,0.0));
	    c += coefficients[2] * tex2D(_MainTex, uv+float2(  0.0,0.0));
	    c += coefficients[3] * tex2D(_MainTex, uv + float2(fastOffsets.w,0.0));
	    c += coefficients[4] * tex2D(_MainTex, uv + float2(fastOffsets.z,0.0));*/

	    return c;
	}	
	
	float4 fragBlurVertical (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		float2 ps = _MainTex_TexelSize.xy;
		
	    float4 c = ZEROES;
	    
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(0.5, -4.30908)*ps, _MainTex_ST))*0.055028;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(0.5, -2.37532)*ps, _MainTex_ST))*0.244038;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(0.5, -0.50000)*ps, _MainTex_ST))*0.401870;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(0.5, 1.37532)*ps, _MainTex_ST))*0.244038;
		c+= tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + float2(0.5, 3.30908)*ps, _MainTex_ST))*0.055028;

	    return c;
	}
	
	struct v2fDepthCopy {
	    float4 pos : SV_POSITION;
	    float2 depth : TEXCOORD0;
	};
//
	v2fDepthCopy vertDepthCopy (appdata_base v) {
	    v2fDepthCopy o;
	    o.pos = UnityObjectToClipPos (v.vertex);
	    UNITY_TRANSFER_DEPTH(o.depth);
	    return o;
	}

	half4 fragDepthCopy(v2f i, out float outDepth : SV_Depth) : SV_Target
    {
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
        outDepth = depth;
        return 1;
    }	
	
	float4 fragDepthDebug(v2f i) : SV_Target {
#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
#else
		float2 uv = i.uv;
#endif
		float depth_r = SAMPLE_DEPTH_TEXTURE(_MainTex, uv);
		return lerp(ONES, ZEROES, depth_r);
	}
	
     //Start median==========================================================================================================     
	///*
	//3x3, 5x5etc Median
	//Morgan McGuire and Kyle Whitson
	//http://graphics.cs.williams.edu
	//
	float4 fragMedRGB (v2f i) : SV_Target
	{
		float2 ooRes = _MainTex_TexelSize.xy;//_ScreenParams.w;

		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv;
		#else
		float2 uv = i.uv;
		#endif
		
		//This doesn't work on SM2
		#if SHADER_TARGET < 30        
		float3 ofs = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		//
		float3 v[5];
		
		float4 midCol = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
        v[0] = midCol.rgb;
        v[1] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.xz, _MainTex_ST)).rgb;
        v[2] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.xz, _MainTex_ST)).rgb;
        v[3] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.zy, _MainTex_ST)).rgb;
        v[4] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.zy, _MainTex_ST)).rgb;
		
		float3 temp;
		mnmx5(v[0], v[1], v[2], v[3], v[4]);
		mnmx3(v[1], v[2], v[3]);
		
		return float4(SafeHDR(v[2].rgb), midCol.a);
		#else
		
		float4 midCol = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
		// -1  1  1
		// -1  0  1 
		// -1 -1  1  //3x3=9	
		float3 v[9];

		// Add the pixels which make up our window to the pixel array.
		UNITY_UNROLL
		for(int dX = -1; dX <= 1; ++dX) 
		{
		UNITY_UNROLL
			for(int dY = -1; dY <= 1; ++dY) 
			{
				float2 ofst = float2(float(dX), float(dY));

				// If a pixel in the window is located at (x+dX, y+dY), put it at index (dX + R)(2R + 1) + (dY + R) of the
				// pixel array. This will fill the pixel array, with the top left pixel of the window at pixel[0] and the
				// bottom right pixel of the window at pixel[N-1].
				v[(dX + 1) * 3 + (dY + 1)] = (float3)tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofst * ooRes, _MainTex_ST)).rgb;
			}
		}

		float3 temp;

		// Starting with a subset of size 6, remove the min and max each time
		mnmx6(v[0], v[1], v[2], v[3], v[4], v[5]);
		mnmx5(v[1], v[2], v[3], v[4], v[6]);
		mnmx4(v[2], v[3], v[4], v[7]);
		mnmx3(v[3], v[4], v[8]);
			
		return float4(SafeHDR(v[4].rgb), midCol.a);
		#endif
	}
	
	float4 fragBloomMedRGBHard (v2f i) : SV_Target
	{
		float2 ooRes = _MainTex_TexelSize.xy;//_ScreenParams.w;

		float2 uv = i.uv;
		
		//This doesn't work on SM2
		#if SHADER_TARGET < 30        
		float3 ofs = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		//
		float3 v[5];
		
		float4 midCol = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
        v[0] = midCol.rgb;
        v[1] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.xz, _MainTex_ST)).rgb;
        v[2] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.xz, _MainTex_ST)).rgb;
        v[3] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.zy, _MainTex_ST)).rgb;
        v[4] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.zy, _MainTex_ST)).rgb;
		
		float3 temp;
		mnmx5(v[0], v[1], v[2], v[3], v[4]);
		mnmx3(v[1], v[2], v[3]);
		
		v[2].rgb = highlights(v[2].rgb, _BloomThreshold);
		
		return float4(SafeHDR(v[2].rgb), midCol.a);
		#else
		
		float4 midCol = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
		// -1  1  1
		// -1  0  1 
		// -1 -1  1  //3x3=9	
		float3 v[9];

		// Add the pixels which make up our window to the pixel array.
		UNITY_UNROLL
		for(int dX = -1; dX <= 1; ++dX) 
		{
		UNITY_UNROLL
			for(int dY = -1; dY <= 1; ++dY) 
			{
				float2 ofst = float2(float(dX), float(dY));

				// If a pixel in the window is located at (x+dX, y+dY), put it at index (dX + R)(2R + 1) + (dY + R) of the
				// pixel array. This will fill the pixel array, with the top left pixel of the window at pixel[0] and the
				// bottom right pixel of the window at pixel[N-1].
				v[(dX + 1) * 3 + (dY + 1)] = (float3)tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofst * ooRes, _MainTex_ST)).rgb;
			}
		}

		float3 temp;

		// Starting with a subset of size 6, remove the min and max each time
		mnmx6(v[0], v[1], v[2], v[3], v[4], v[5]);
		mnmx5(v[1], v[2], v[3], v[4], v[6]);
		mnmx4(v[2], v[3], v[4], v[7]);
		mnmx3(v[3], v[4], v[8]);
		
		v[4].rgb = highlights(v[4].rgb, _BloomThreshold);
			
		return float4(SafeHDR(v[4].rgb), midCol.a);
		#endif
	}
	
	float4 fragBloomMedRGB (v2f i) : SV_Target
	{
		float2 ooRes = _MainTex_TexelSize.xy;//_ScreenParams.w;

		float2 uv = i.uv;
		    
		float3 ofs = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		//
		float3 v[5];
		
		float4 midCol = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
		
        v[0] = midCol.rgb;
        v[1] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.xz, _MainTex_ST)).rgb;
        v[2] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.xz, _MainTex_ST)).rgb;
        v[3] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv - ofs.zy, _MainTex_ST)).rgb;
        v[4] = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv + ofs.zy, _MainTex_ST)).rgb;
		
		float3 temp;
		mnmx5(v[0], v[1], v[2], v[3], v[4]);
		mnmx3(v[1], v[2], v[3]);
		
		v[2].rgb = highlights(v[2].rgb, _BloomThreshold);//highlights(v[2].rgb, _BloomThreshold);// saturate(SCurve(Luminance(v[4].rgb)) / _BloomThreshold);
		
		return float4(SafeHDR(v[2].rgb), midCol.a);
	}
              
                   
    //End median=======================================================================================================                            

	//Custom downsample
	struct v2fDownsample { 
		float4 position : SV_POSITION;  
		float2 uv[4]    : TEXCOORD0;
	}; 
	
	v2fDownsample vertDownsample (appdata_img v)
	{
		v2fDownsample o;
		o.position = UnityObjectToClipPos(v.vertex);
		float2 uv = v.texcoord;
		
		// Compute UVs to sample pixel block.
		o.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1,1);
		o.uv[1] = uv + _MainTex_TexelSize.xy * half2(1,1);
		o.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1,-1);
		o.uv[3] = uv + _MainTex_TexelSize.xy * half2(1,-1);
		
		return o;
	}

	half4 fragDownsample (v2fDownsample i) : SV_Target
	{
		// Sample pixel block
		half3 topLeft = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[0], _MainTex_ST)).rgb;
		half3 topRight = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[1], _MainTex_ST)).rgb;
		half3 bottomLeft = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[2], _MainTex_ST)).rgb;
		half3 bottomRight = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[3], _MainTex_ST)).rgb;
		
		half3 avg = ((topLeft + topRight + bottomLeft + bottomRight) / 4.0);
					
		return float4(avg.rgb, 1.0);
	}                                                                    
                                                                                         
         			float4 fragBloomPrePassCheap (v2fDownsample i) : SV_Target
	{
		half3 topLeft = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[0], _MainTex_ST)).rgb;
		half3 topRight = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[1], _MainTex_ST)).rgb;
		half3 bottomLeft = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[2], _MainTex_ST)).rgb;
		half3 bottomRight = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[3], _MainTex_ST)).rgb;
		
		half3 blm = ((topLeft + topRight + bottomLeft + bottomRight) / 4.0);
		//float4 blm = ZEROES;
		
		//Actually prefer doing it this way, simply subtracting the threshold. Gives a bit richer bloom colors
		//If we're using the near blur, not using low or med dof
		#if !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR
			//This is fullscreen blur
			blm.rgb = highlights(blm.rgb, _BloomThreshold);
			//blm.rgb *= _BloomIntensity;
			//blm.a = Luminance(blm.rgb);
		#else 
		//Just standard bloom
			blm.rgb = highlights(blm.rgb, _BloomThreshold);
			//blm.rgb *= _BloomIntensity;
			//blm.a = Luminance(blm.rgb);
		#endif
		
		return float4(blm,1.0);
	}
	                                                                                              
                                                                                                   
                                                                                                        
                                                                                                             
                                                                                                                  
                                                                                                                       
                                                                                                                            
                                                                                                                                 
                                                                                                                                      
                                                                                                                                           
      