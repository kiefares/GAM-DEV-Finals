Shader "Hidden/PrismEffectsSecondary" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE
	
		#include "UnityCG.cginc"
		#include "Prism.cginc"
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile _ PRISM_DOF_USENEARBLUR
		#pragma multi_compile _ PRISM_DOF_LOWSAMPLE PRISM_DOF_MEDSAMPLE
		#pragma multi_compile _ PRISM_USE_EXPOSURE
		#pragma shader_feature _ PRISM_HDR_BLOOM
		#pragma shader_feature _ PRISM_GRADUAL_THRESH PRISM_HIGHLIGHT_THRESH PRISM_CURVE_THRESH
		#pragma shader_feature _ PRISM_UIBLUR
		//#pragma shader_feature _ PRISM_CHROMATICBLURNOISE
		#pragma target 3.0
	ENDCG
	
	SubShader {

	ZTest Always Cull Off ZWrite Off
	
	//0 - Sharpen
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSharpen
		ENDCG
	}
	
	//1 - chromatic
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragChromatic
		ENDCG
	}
	
	//2 - downsample
	Pass {  
		CGPROGRAM
			#pragma vertex vertDownsample
			#pragma fragment fragDownsample
		ENDCG
	}
	
	//3 - get luminance
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragLuminance
		ENDCG
	}
		
	//4 - adapt
	Pass {  
	
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragAdapt
		ENDCG
	}
	
	//5 - BloomPrepassCheap
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBloomPrePassCheap
		ENDCG
	}
	
	//Medians
	//-----------------------------------------------------
	// Pass 6
	//-----------------------------------------------------		
	Pass{
			
		CGPROGRAM
		#pragma vertex vertPRISM	
		#pragma fragment fragMedRGB		
		ENDCG			
	}
	
	//7 - Blur box
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBlurBox
		ENDCG
	}
	
	//8- Gaussian blur
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSmartUpsample
		ENDCG
	}
	
	//9- DebugDoF
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDoFDebug
		ENDCG
	}
	
	//10 - Simpler median with exposure filter
	Pass {
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBloomMedRGB
		ENDCG
	}
	
	//11 hard median
	Pass {
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBloomMedRGBHard
		ENDCG
	}	
		
	//12 - Separable blur with chromatic
	Pass {
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragChromaticBlur
		ENDCG
	}	
	
	//13- Gaussian blur vert
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBlurVertical
		ENDCG
	}
	
	//14- Gaussian horiz
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBlurHorizontal
		ENDCG
	}
	
	
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	FallBack off
}
