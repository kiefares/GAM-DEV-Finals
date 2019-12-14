Shader "Hidden/PrismFlares" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE
	
		#include "UnityCG.cginc"
		#include "Prism.cginc"
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma target 3.0
	ENDCG
	
	SubShader {

	ZTest Always Cull Off ZWrite Off
	
	//0 - Flare Features
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragFlareFeatures
		ENDCG
	}
	
	//1 - Flare Add
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragFlareAdd
		ENDCG
	}
	
	////
	
	
	
	
	
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	FallBack off
}
