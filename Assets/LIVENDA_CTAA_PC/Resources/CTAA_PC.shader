// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//LIVENDA CTAA - CINEMATIC TEMPORAL ANTI ALIASING PC
//Copyright Livenda Labs 2016
//pc-v2.5


Shader "Hidden/CTAA_PC" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	
}

SubShader {
	ZTest Always Cull Off ZWrite Off Fog { Mode Off }
	Pass {

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#pragma glsl
#pragma exclude_renderers d3d11_9x
#include "UnityCG.cginc"

            
float4 _MainTex_TexelSize;
uniform sampler2D _MainTex;
uniform sampler2D _Accum;
uniform sampler2D _Motion0;

uniform sampler2D _CameraDepthTexture;
uniform float _motionDelta;
uniform float _motionDeltaDynamic;
uniform float _AdaptiveResolve;

float4 _ControlParams;
sampler2D_half _CameraMotionVectorsTexture;


float LUMDJDHEkie(float3 Color)
{
	return (Color.g * 2.0) + (Color.r + Color.b);
}


inline float hddjeueafe(float3 Color, float Exposure) 
{
	return rcp(LUMDJDHEkie(Color) * Exposure + 4.0);
}

float ueyfjeheuueuhd(float3 Color, float Exposure) 
{
	return rcp(Color.g * Exposure + 1.0);
}

float hddrrrees(float Color, float Exposure) 
{
	return rcp(Color * Exposure + 1.0);
}

float opepopaee(float Color, float Exposure) 
{
	return rcp(Color * Exposure + 4.0);
}

inline float bdhgettettte(float3 Color, float Exposure) 
{
	return 4.0 * rcp(LUMDJDHEkie(Color) * (-Exposure) + 1.0);
}

float vcvveeerr(float3 Color, float Exposure) 
{
	return rcp(Color.g * (-Exposure) + 1.0);
}

float invdghre(float Color, float Exposure) 
{
	return 4.0 * rcp(Color * (-Exposure) + 1.0);
}

float neriaudie(float Color, float Exposure) 
{
	return rcp(Color * (-Exposure) + 1.0);
}

float lindkdkdde(float Channel, float Exposure) 
{
	return Channel * invdghre(Channel, Exposure);
}

float hfeiueihee(float Channel, float Exposure) 
{
	return Channel * neriaudie(Channel, Exposure);
}

float brbrehehiir(float3 Color, float Exposure) 
{
	float L = LUMDJDHEkie(Color);
	return L * opepopaee(L, Exposure);
}

float lumperper(float3 Color, float Exposure) 
{
	return Color.g * hddrrrees(Color.g, Exposure);
}

float lummmerre(float3 Color) 
{
	#if 1
		
		return dot(Color, float3(0.299, 0.587, 0.114));
	#else
		
		return dot(Color, float3(0.2126, 0.7152, 0.0722));
	#endif
}

float hihiekkekkd(float Channel) 
{
	return Channel * rcp(1.0 + Channel);
}
	
float jujueersd(float Channel) 
{
	return Channel * rcp(1.0 - Channel);
}

float perperdkfe(float3 Color, float Exposure) 
{
	return sqrt(hihiekkekkd(lummmerre(Color) * Exposure));
}

float linhuhuere(float Channel) 
{
	return jujueersd(Channel * Channel);
}

inline float fufuceeur(float3 Dir, float3 Org, float3 Box)
{	
	float3 RcpDir = rcp(Dir);
	float3 TNeg = (  Box  - Org) * RcpDir;
	float3 TPos = ((-Box) - Org) * RcpDir;
	return max(max(min(TNeg.x, TPos.x), min(TNeg.y, TPos.y)), min(TNeg.z, TPos.z));
}

inline float clclmsoeiur(float3 History, float3 filfileeeir, float3 mindiudmin, float3 maiucmsia)
{
	float3 Min = min(filfileeeir, min(mindiudmin, maiucmsia));
	float3 Max = max(filfileeeir, max(mindiudmin, maiucmsia));	
	float3 Avg2 = Max + Min;
	float3 Dir = filfileeeir - History;
	float3 Org = History - Avg2 * 0.5;
	float3 Scale = Max - Avg2 * 0.5;
	return saturate(fufuceeur(Dir, Org, Scale));	
}

float hfhfuiuieew(float3 Color, float Exposure) 
{
	return rcp(max(lummmerre(Color) * Exposure, 1.0));
}

float4 popoererwq(float4 ColorA, float4 ColorB, float Blend, float Exposure) 
{
	float BlendA = (1.0 - Blend) * hfhfuiuieew(ColorA.rgb, Exposure);
	float BlendB =        Blend  * hfhfuiuieew(ColorB.rgb, Exposure);
	float RcpBlend = rcp(BlendA + BlendB);
	BlendA *= RcpBlend;
	BlendB *= RcpBlend;
	return ColorA * BlendA + ColorB * BlendB;
}

struct v2f {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = v.texcoord.xy;

	return o;
}

float4 frag (v2f i) : COLOR
{

 float2 babajjdie;
 float2 iuiueiueiuwe;
		
 float  expexpeews = _ControlParams.x;
  
 float2  textexja = _MainTex_TexelSize.xy;

 half2 momoeew = tex2D(_CameraMotionVectorsTexture, i.uv ).rg;
 babajjdie =   momoeew;

 float hihiududw = 2;
 float opopweewq = saturate(abs(babajjdie.x) * hihiududw + abs(babajjdie.y) * hihiududw);
 	
	half2  uv = i.uv ;				
	half4 neneiwiwe = tex2D(_MainTex, uv.xy - textexja );
	half4 teterreew = tex2D(_MainTex, uv.xy + float2(  0, -textexja.y ) );
	half4 OPskeesdr = tex2D(_MainTex, uv.xy + float2(  textexja.x, -textexja.y ) );
	half4 OIOIEJDEAA = tex2D(_MainTex, uv.xy + float2(  -textexja.x, 0 ) );
	half4 UYUYSJSJE = tex2D(_MainTex, uv.xy);
	half4 VFVFDFDFSD = tex2D(_MainTex, uv.xy + float2(   textexja.x, 0 ) );
	half4 MNMNBHBHD = tex2D(_MainTex, uv.xy + float2( -textexja.x,  textexja.y ) );
	half4 ytyteuyeeb = tex2D(_MainTex, uv.xy + float2(  0,  textexja.y ) );
	half4 nunurtyde = tex2D(_MainTex, uv.xy + textexja );


		textexja.xy *=2;

        half uieyetdhhd = (lummmerre(tex2D(_MainTex, uv.xy +  float2(  0, -textexja.y  ) )) );
        half hgghdgygye = (lummmerre( tex2D(_MainTex, uv.xy + float2(  0,  textexja.y  ) )) );
        half netnetdkkdj = (lummmerre(tex2D(_MainTex, uv.xy  + float2(  textexja.x , 0  ) )) );
        half reressserf = (lummmerre(tex2D(_MainTex, uv.xy  + float2( -textexja.x , 0  ) )) );

        half cenedceng = (lummmerre(tex2D(_MainTex, uv.xy))) - (uieyetdhhd + hgghdgygye + netnetdkkdj + reressserf)*0.25;

        half gradeex       = saturate(abs(cenedceng));
        gradeex = saturate(pow(gradeex, 4.0)*0.5);

        expexpeews = lerp(2.5,12,gradeex);
        		
		neneiwiwe.rgb *= hddjeueafe(neneiwiwe.rgb, expexpeews);
		teterreew.rgb *= hddjeueafe(teterreew.rgb, expexpeews);
		OPskeesdr.rgb *= hddjeueafe(OPskeesdr.rgb, expexpeews);
		OIOIEJDEAA.rgb *= hddjeueafe(OIOIEJDEAA.rgb, expexpeews);
		UYUYSJSJE.rgb *= hddjeueafe(UYUYSJSJE.rgb, expexpeews);
		VFVFDFDFSD.rgb *= hddjeueafe(VFVFDFDFSD.rgb, expexpeews);
		MNMNBHBHD.rgb *= hddjeueafe(MNMNBHBHD.rgb, expexpeews);
		ytyteuyeeb.rgb *= hddjeueafe(ytyteuyeeb.rgb, expexpeews);
		nunurtyde.rgb *= hddjeueafe(nunurtyde.rgb, expexpeews);
						
		half4 filfileeeir = 
			neneiwiwe * 0.0625 + 
			teterreew * 0.125 +
			OPskeesdr * 0.0625 +
			OIOIEJDEAA * 0.125 +
			UYUYSJSJE * 0.25 +
			VFVFDFDFSD * 0.125 +
			MNMNBHBHD * 0.0625 +
			ytyteuyeeb * 0.125 +
			nunurtyde * 0.0625;

		float4	 oioieeoirm = filfileeeir;

		half4 neneytyurrd = min(min(neneiwiwe, OPskeesdr), min(MNMNBHBHD, nunurtyde));		
		half4 hshseertf = max(max(neneiwiwe, OPskeesdr), max(MNMNBHBHD, nunurtyde));		
		half4 mindiudmin = min(min(min(teterreew, OIOIEJDEAA), min(UYUYSJSJE, VFVFDFDFSD)), ytyteuyeeb);		
		half4 maiucmsia = max(max(max(teterreew, OIOIEJDEAA), max(UYUYSJSJE, VFVFDFDFSD)), ytyteuyeeb);		
		neneytyurrd = min(neneytyurrd, mindiudmin);
		hshseertf = max(hshseertf, maiucmsia);
	    mindiudmin = mindiudmin * 0.5 + neneytyurrd * 0.5;
		maiucmsia = maiucmsia * 0.5 + hshseertf * 0.5;
		
	    float4 ouysouejs = tex2D(_Accum, i.uv-babajjdie);
			
	    ouysouejs.rgb *= hddjeueafe(ouysouejs.rgb, expexpeews);
				
		float lmindumind = LUMDJDHEkie(mindiudmin.rgb);
		float mavudyedds = LUMDJDHEkie(maiucmsia.rgb);
		float uyduyduyehd = LUMDJDHEkie(ouysouejs.rgb);

		float huhuyyttdhd = mavudyedds - lmindumind;
		float blenrnffio = clclmsoeiur(ouysouejs.rgb, oioieeoirm.rgb, mindiudmin.rgb, maiucmsia.rgb);

		ouysouejs.rgb = lerp(ouysouejs.rgb, oioieeoirm.rgb, blenrnffio );

		float alidjfeiddai = saturate(opopweewq) * 0.5;
		float conlumdld =  _ControlParams.z;
		
		alidjfeiddai = saturate(alidjfeiddai + rcp(1.0 + huhuyyttdhd * conlumdld));
		filfileeeir.rgb = lerp(filfileeeir.rgb, UYUYSJSJE.rgb, alidjfeiddai);

		float hishiseoos = (1.0/_ControlParams.y) + opopweewq * (1.0/_ControlParams.y);
		float facfoord = uyduyduyehd * hishiseoos * (1.0 + opopweewq * hishiseoos * 4.0);
		float sirieidis = saturate(facfoord * rcp(uyduyduyehd + huhuyyttdhd));
						
		float finb = lerp(sirieidis, (sqrt(sirieidis)), saturate(length(babajjdie)*_AdaptiveResolve) );
			
		ouysouejs = lerp(ouysouejs, filfileeeir, finb);
		ouysouejs.rgb *= bdhgettettte(ouysouejs.rgb, expexpeews);
			
		ouysouejs.rgb = -min(-ouysouejs.rgb, 0.0);

	 	return ouysouejs;

	
}
ENDCG
	}
}

Fallback off

}