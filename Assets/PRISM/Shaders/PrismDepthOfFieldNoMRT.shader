Shader "Hidden/PrismDepthOfFieldNoMRT" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"
		#include "Prism.cginc"
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma target 3.0
		#pragma multi_compile _ PRISM_DOF_USENEARBLUR
		#pragma multi_compile _ PRISM_DOF_LOWSAMPLE PRISM_DOF_MEDSAMPLE
	ENDCG
	
	SubShader {
	//Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}

	ZTest Off Cull Off ZWrite Off
	
	//0 - Depth downsample
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDepthCopy
		ENDCG
	}

	//1 -COC
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDoFPrepass
		ENDCG
	}
	
	//2 - DoF Backtexture
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDoFDebug
		ENDCG
	}
	
	//3 - DoF Blur 1
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDoFBlur
		ENDCG
	}
	
	//4 - DoF Dual CoC
	Pass {  
		//Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDepthDebug
		ENDCG
	}
	
	//5 - combine
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDoFCombine
		ENDCG
	}
	
	//6 - Upsample
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBlurBox
		ENDCG
	}
	
	
	
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	FallBack off
}
