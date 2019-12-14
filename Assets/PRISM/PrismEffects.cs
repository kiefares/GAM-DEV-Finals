using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Prism.Utils;

namespace Prism.Utils {
	//Unused atm
	public enum NoiseType { RandomNoise, RandomTimeNoise, GradientNoise };
	// Sample count for bloom
	public enum SampleCount { Low=3, Medium=6, High=10 };
	
	public enum TonemapType { Filmic, RomB, ACES};
	
	public enum AberrationType { Vignette, Vertical, Barrel};
	
	public enum DoFSamples { Low, Medium};

	public enum DebugDofPass { None, BackCoC, FrontCoC, Back, Front, Depth};
	
	public enum BloomType { Simple, HDR };

	public enum BloomThreshholdType { Gradual, Highlights, Quadratic };

	public enum BloomDownsampleAmount { None=1, Half=2, Quarter=4, Eighth=8 };

	public enum RaysDownsampleAmount { None=1 , Half=2, Quarter=4};
	
	public enum PrismPresetType { Full, Bloom, Fog, DepthOfField, ChromaticAberration, Vignette, Noise, Tonemap, Exposure, ColorCorrection, Nightvision, Gamma, AmbientObscurance, Godrays, Sharpen, Colorify, Brightness }
	
	public enum AOBlurType { Fast = 3, Wide = 4};

	public enum FogType { Linear = 1, Exponential = 2, ExponentialSquared = 3};

}

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("PRISM/Prism Effects")]
#if UNITY_5_4_OR_NEWER
[ImageEffectAllowedInSceneView]
#endif
public class PrismEffects : MonoBehaviour {

	#region variables
	public PrismPreset currentPrismPreset;

	/// <summary>
	/// Are we inside a PrismVolume?
	/// </summary>
	public bool currentlyInsideVolume = false;
	
	protected bool shouldSkipNextFrame = false;
	protected bool areEffectsDisabled = false;

	/// <summary>
	/// Saves some CPU time by not checking for new materials/shaders/LUTs each frame. Do not use unless in-game and 100% sure nothing will change.
	/// </summary>
	public bool enableGuaranteedMaterialOptimization = false;

    public bool autoTurnOffDepthTextures = true;

	//These variables are for multi-camera setups with depth effects.
	public bool renderDepthDebug = false;
	//End multi-camera depth effect vars

	//Just the main pass
	public Material m_Material;
	public Shader m_Shader;

	//Most other effects
	public Material m_Material2;
	public Shader m_Shader2;

	//AO only
	public Material m_AOMaterial;
	public Shader m_AOShader;

	//Rays
	public Material m_Material3;
	public Shader m_Shader3;

	//Other
	public Material m_DoFMaterial;
	public Shader m_DoFShader;

	//Flares
	public Material m_FlaresMaterial;
	public Shader m_FlaresShader;
	
	protected Camera m_Camera;
	
	public bool doBigPass = true;
	
	public bool useSeparableBlur = true;
	
	public Texture2D lensDirtTexture;
	
	public bool useLensDirt = true;

	//START BLOOM VARIABLES===================================

	[Space(10f)]
	public bool useBloom = false;
	public bool useBloomFlares = false;
	public bool bloomUseScreenBlend = false;
	public BloomType bloomType = BloomType.HDR;
	public bool useDifferentMobileBloom = false;

	public BloomThreshholdType bloomThreshType = BloomThreshholdType.Quadratic;
	
	public bool debugBloomTex = false;

	public BloomDownsampleAmount bloomDownsampleEnum = BloomDownsampleAmount.Half;

	[Range(1, 12)]
	public int bloomDownsample = 2;
	
	[Range(0, 12)]
	public int bloomBlurPasses = 2;
	
	public float bloomIntensity = 2.0f;

	public float bloomOneIntensity = 0.15f;
	public float bloomTwoIntensity = 0.15f;
	public float bloomThreeIntensity = 0.15f;
	public float bloomFourIntensity = 0.15f;

	[Range(-2f, 2f)]
	public float bloomThreshold = 1.1f;

	public float bloomRadius = 4.0f;

	public float bloomKnee = 0.5f;

	//public float bloomExposure = 0.15f;
	
	public float dirtIntensity = 0.3f;
	
	public bool useBloomStability = true;
	protected RenderTexture accumulationBuffer;

	//Bloom Flares
	public Texture2D bloomFlareStarburstTex;

	public float bloomFlareStrength = 1.9f;
	public float bloomFlareHaloWidth = 0.5f;
	public float bloomFlareGhostDispersal = 0.11f;
	public float bloomFlareChromaticDistortion = 1.03f;
	public float bloomFlareLensDirtIntensityMultiplier = 1.0f;
	public int bloomFlareNumberGhosts = 6;
	public float bloomFlareRadius1;
	public float bloomFlareRadius2;
	public float bloomFlareRadius3;
	//End Bloom Flares

	//END BLOOM VARIABLES===================================

	//Start UIBLUR VARIABLES================================
	public bool useUIBlur = false;
	public int uiBlurGrabTextureFromPassNumber = 2;
	protected RenderTexture uiBlurTexture;
	//End UIBLUR VARIABLES
	
	//START VIGNETTE VARIABLES===================================
	[Space(10f)]
	public bool useVignette = false;
	
	public float vignetteStart = 0.9f;
	public float vignetteEnd = 0.4f;
	public float vignetteStrength = 1f;
	public Color vignetteColor = Color.black;
	//public float 
	//END VIGNETTE VARIABLES===================================
	
	//START NOISE VARIABLES===================================
	[Space(10f)]
	public bool useNoise = false;

	public bool useDither = false;

	public Texture2D noiseTexture;

	public float noiseIntensity = 0.2f;
	public float secondaryNoiseMultiplier = 0.01f;
	//END NOISE VARIABLES======================================
	
	//START CHROMATIC VARIABLES================================
	[Space(10f)]
	public bool useChromaticAberration = false;
	
	public AberrationType aberrationType = AberrationType.Vertical;
	
	[Range(0f, 1f)]
	public float chromaticDistanceOne = 0.29f;
	[Range(0f, 1f)]
	public float chromaticDistanceTwo = 0.599f;
	public float chromaticIntensity = 0.03f;
	public float chromaticBlurWidth = 1f;
	public bool useChromaticBlur = false;

	public bool forceSecondChromaticPass 
	{
		get {
			return useChromaticAberration && useChromaticBlur;// false;//(((useDof && useChromaticAberration) || useChromaticBlur) && useChromaticAberration);
		}
	}
	//END CHROMATIC VARIABLES==================================
	
	//START TONEMAP VARIABLES================================
	[Space(10f)]
	
	public bool useTonemap = false;
	
	public TonemapType tonemapType = TonemapType.RomB;
	
	public Vector3 toneValues = new Vector3(-1.0f, 2.72f, 0.15f);
	public Vector3 secondaryToneValues = new Vector3(0.59f, 0.14f, 0.14f);
	//END TONEMAP VARIABLES================================

	//START BRIGHTNESS VARIABLES=================================
	public bool useBrightness = false;
	public float brightIntensity = 0.5f;
	public float brightCutoff = 0.2f;
	public float brightSoftness = 0.1f;
	//END BRIGHTNESS VARIABLES=================================
	
	//START EXPOSURE VARIABLES==================================
	public bool useExposure = false;
	public bool debugViewExposure = false;
	protected RenderTexture currentAdaptationTexture;
	
	public float exposureMiddleGrey = 0.12f;
	public float exposureLowerLimit = -6f;
	public float exposureUpperLimit = 6f;
	public float exposureSpeed = 6f;
	
	public int histWidth = 1;
	public int histHeight = 1;
	protected bool firstRun = true;
	
	//START GAMMA VARIABLES==================================
	public bool useGammaCorrection = false;
	public float gammaValue = 1.0f;
	//END GAMMA VARIABLES==================================
	
	//START DOF VARIABLES==================================
	[Space(10f)]
	RenderBuffer[] _DoFMRT;
	public bool useDof = false;
	public bool dofForceEnableMedian = false;
	public bool useMedianDoF 
	{
		get {
			return ((useBloom || useExposure || useDof) && dofForceEnableMedian);// ( (useBloom && useBloomStability) || useDof) && !dofForceDisableMedian;
		}
	}

	public bool useMedianDoFTextureForOtherEffect
	{
		get {
			return (useRays || useBloom || useChromaticBlur);// ( (useBloom && useBloomStability) || useDof) && !dofForceDisableMedian;
		}
	}
	public bool useNearDofBlur = false;
	public bool useFullScreenBlur = false;
	public float dofNearFocusPoint = 5f;
	public float dofNearFocusDistance = 15f;
	public float dofFocusPoint = 5f;
	public float dofFocusDistance = 15f;
	public BloomDownsampleAmount dofDownsample = BloomDownsampleAmount.Half;
	public bool dofUseMRT = false;
	
	public float dofRadius = 0.6f;
	public float dofBokehFactor = 60f;
	public DoFSamples dofSampleAmount = DoFSamples.Medium;
	public bool useDICEDof = false;
	public float dofShapeWeight = 0.577350269189626f;
	public bool dofBlurSkybox = true;
	
	public bool debugDofPass = false;
	public DebugDofPass dofDebugType = DebugDofPass.Front;
	
	/// <summary>
	/// The transform, which, if not null, will be used as a DoF Focus point
	/// </summary>
	public Transform dofFocusTransform;
	//END DOF VARIABLES====================================
	
	//START LUT VARIABLES==================================
	[Space(10f)]
	
	public bool useLut = false;
	
	public Texture2D twoDLookupTex = null;
	public Texture3D threeDLookupTex = null;
	
	public string basedOnTempTex = "";
	
	public float lutLerpAmount = 0.6f;
	
	public bool useSecondLut = false;
	
	public Texture2D secondaryTwoDLookupTex = null;
	public Texture3D secondaryThreeDLookupTex = null;
	
	public string secondaryBasedOnTempTex = "";
	
	public float secondaryLutLerpAmount = 0.0f;
	
	//END LUT VARIABLES====================================

	//START COLORIFY VARIABLES=============================
	public bool useColorify = false;
	public float colorifyAmount = 0.4f;
	//END COLORIFY VARIABLES===============================
	
	//START SHARP VARIABLES==================================
	[Space(10f)]
	
	public bool useSharpen = false;
	
	public float sharpenAmount = 0.65f;
	
	//END SHARP VARIABLES====================================
	
	//START FOG VARIABLES====================================
	public bool useFog = false;
	public bool fogAffectSkybox = false;
	public float fogHeight = 50f;
	public float fogIntensity = 0.0f;
	public float fogStartPoint = 50f;
	public float fogDistance = 170f;
	public Color fogColor = Color.white;
	public Color fogMidColor = Color.white;
	public Color fogEndColor = Color.gray;
	public float fogMidPoint = 200.0f;
	public float fogEndPoint = 250.0f;
	public float fogNoiseFactor1 = 0.1f;
	public float fogNoiseFactor2 = 0.5f;
	public FogType fogExpFactor = FogType.Exponential;
	//END FOG VARIABLES======================================
	
	//Start AO VARIABLES===================================
	public bool useAmbientObscurance = false;

	// Texture format used for storing AO
	protected RenderTextureFormat aoTextureFormat {
		get {
			if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8))
				return RenderTextureFormat.R8;
			else
				return RenderTextureFormat.Default;
		}
	}
	
	public SampleCount aoSampleCount = SampleCount.Low;
	
	public int aoSampleCountValue {
		get {
			switch (aoSampleCount) {
			case SampleCount.Low: return 3;
			case SampleCount.Medium:    return 6;
			case SampleCount.High: return 12;
			}
			return Mathf.Clamp((int)aoSampleCount, 1, 256);
		}
		set { aoSampleCount = (SampleCount)value; }
	}
	public bool useAODistanceCutoff = false;
	public float aoDistanceCutoffLength = 50.00f;
	public float aoDistanceCutoffStart = 500.0f;	
	public float aoIntensity = 0.7f;
    public float aoContrast = 0.6f;
	//This is normally 0, but if we set it from another AO script, it means we can use a different AO method.
	public float aoMinIntensity = 0.0f;
	public float aoRadius = 1.0f;
	public bool aoDownsample = false;
	public AOBlurType aoBlurType = AOBlurType.Fast;
	
	[Range (0,3)]
	public int aoBlurIterations = 1;
	public float aoBias = 0.1f;
	public float aoBlurFilterDistance = 1.25f;
	public float aoLightingContribution = 0.01f;
	public bool aoShowDebug = false;
	//End AO VARIABLES=====================================

	//START GODRAYS VARIABLES
	public bool useRays = false;

	public Transform rayTransform;
	public float rayWeight = 0.58767f;	
	public float rayDecay=0.96815f;
	public float rayExposure=0.2f;
	public float rayDensity=0.926f;

	[ColorUsageAttribute(true, true,0f,8f,0.125f,3f)]
	public Color rayColor = Color.white;

	[ColorUsageAttribute(true, true,0f,8f,0.125f,3f)]
	public Color rayThreshold = new Color(0.87f,0.74f,0.65f);

	public RaysDownsampleAmount rayDownsample = RaysDownsampleAmount.Quarter;

	public int rayDSA {
		get {
			return (int)rayDownsample;
		}
	}

	public bool raysShowDebug = false;
	//END GODRAYS VARIABLES
	#endregion
	
	//Helper variables=====================================
	public bool UsingTerrain {
		get {
			if(Terrain.activeTerrain)
			{
				return true;
			}
			return false;
		}
	}

    public enum OcclusionSource
    {
        DepthTexture=0, DepthNormalsTexture=1, GBuffer=2
    }

    [SerializeField, Tooltip(
        "Source buffer used for obscurance estimation")]
    public OcclusionSource occlusionSource = OcclusionSource.DepthNormalsTexture;

    // Check if the G-buffer is available
    public bool IsGBufferAvailable {
		get {
			var path = m_Camera.actualRenderingPath;
			return path == RenderingPath.DeferredShading;
		}
	}
	//EndHelper variables=====================================
	
	//Editor only variables
	[Space(10f)]
	public bool advancedVignette = false;
	public bool advancedAO = false;
	public bool advancedDof = false;
	
	//End editor only variables
	
	/// <summary>
	/// "Skips" PRISM effects from rendering the next frame
	/// </summary>
	public void DontRenderPrismThisFrame()
	{
		shouldSkipNextFrame = true;
	}

	/// <summary>
	/// Returns the camera that PRISM is attached to
	/// </summary>
	/// <returns>The prism camera.</returns>
	public Camera GetPrismCamera()
	{
		if(m_Camera == null)
			m_Camera = GetComponent<Camera>();
		return m_Camera;
	}

	/// <summary>
	/// Resets the tone parameters to default rom b.
	/// </summary>
	public void ResetToneParamsRomB()
	{
		toneValues = new Vector3(-1.0f, 2.72f, 0.15f);
		secondaryToneValues = new Vector3(0f, 0f, 0f);
	}

	/// <summary>
	/// Resets the tone parameters to default filmic.
	/// </summary>
	public void ResetToneParamsFilmic()
	{
		toneValues = new Vector3(6.2f, 0.5f, 1.7f);
		secondaryToneValues = new Vector3(0.004f, 0.06f, 0f);
	}

	public void ResetToneParamsACES()
	{
		toneValues = new Vector3(2.51f, 0.03f, 2.43f);
		secondaryToneValues = new Vector3(0.59f, 0.14f, 0f);
	}

	public void SetBloomIntensity(float value)
	{
		bloomIntensity = value;
	}

	public void SetBloomExposure(float value)
	{
		bloomThreshold = value;
	}

	/// <summary>
	/// Sets the value used in the gamma correction effect. For gamma conversions, 2.233 is a standard value.
	/// </summary>
	public void SetGamma(float value)
	{
		gammaValue = value;
	}

	/// <summary>
	/// Sets the intensity of the chromatic aberration effect. (max recommended = 0.15)
	/// </summary>
	public void SetChromaticIntensity(float value)
	{
		chromaticIntensity = value;
	}

	public void SetChromaticStart(float value)
	{
		chromaticDistanceOne = value;
	}

	public void SetChromaticEnd(float value)
	{
		chromaticDistanceTwo = value;
	}

	public void SetChromaticBlur(bool useBlur)
	{
		useChromaticBlur = useBlur;
	}

	public void SetChromaticType(bool useVertical)
	{
		if (useVertical)
		{
			aberrationType = AberrationType.Vertical;
		}
		else
		{
			aberrationType = AberrationType.Vignette;
		}
	}

	public void SetChromaticBlurWidth(float width)
	{
		chromaticBlurWidth = width;
	}

	/// <summary>
	/// Sets the intensity of the noise effect. (max recommended = ~0.5)
	/// </summary>
	public void SetNoiseIntensity(float value)
	{
		noiseIntensity = value;
	}

	public void SetNoiseMultiplier(float value)
	{
		secondaryNoiseMultiplier = value;
	}

	/// <summary>
	/// Sets the intensity of vignette. 0-1
	/// </summary>
	public void SetVignetteStrength(float value)
	{
		vignetteStrength = value;
	}

	public void SetVignetteStart(float value)
	{
		vignetteStart = value;
	}

	public void SetVignetteEnd(float value)
	{
		vignetteEnd = value;
	}

	/// <summary>
	/// Sets the strength of the 1st LookupTexture (1.0=100%)
	/// </summary>
	public void SetPrimaryLutStrength(float value)
	{
		lutLerpAmount = value;
	}

	/// <summary>
	/// Sets the strength of the 2nd LookupTexture (1.0=100%)
	/// </summary>
	public void SetSecondaryLutStrength(float value)
	{
		secondaryLutLerpAmount = value;
	}

	public void SetPrimaryLut(Texture2D newPrimaryLut)
	{
		twoDLookupTex = newPrimaryLut;
		Convert(newPrimaryLut, false);
	}

	public void SetSecondaryLut(Texture2D newSecondaryLut)
	{
		secondaryTwoDLookupTex = newSecondaryLut;
		Convert(newSecondaryLut, true);
	}
	
	/// <summary>
	/// Changes the values of PRISM to use the values of the provided preset, or removes all effects if the preset is null
	/// </summary>
	/// <param name="preset">Prism Preset.</param>
	/// <param name="presetType">Preset type (eg. a Bloom preset type will only modify PRISM's Bloom values).</param>
	public void SetPrismPreset(PrismPreset preset)
	{
		PrismEffects prism = this;
		
		if(!preset)
		{
			prism.useBloom = false;
			prism.useBloomFlares = false;
			prism.useDof = false;
			prism.useChromaticAberration = false;
			prism.useVignette = false;
			prism.useNoise = false;
			prism.useTonemap = false;
			prism.useFog = false;
			//prism.useNightVision = false;
			prism.useExposure = false;
			prism.useLut = false;
			prism.useSecondLut = false;
			prism.useGammaCorrection = false;
			prism.useAmbientObscurance = false;
			prism.useRays = false;
			prism.useUIBlur = false;
			prism.useBrightness = false;
			prism.useColorify = false;
			prism.useSharpen = false;
			return;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Bloom)
		{
			prism.useBloom = preset.useBloom;
			prism.bloomType = preset.bloomType;
			prism.bloomDownsample = preset.bloomDownsample;
			prism.bloomRadius = preset.bloomRadius;

			if(prism.bloomDownsample < 9)
			{
				prism.bloomDownsampleEnum = BloomDownsampleAmount.Eighth;
			}

			if(prism.bloomDownsample < 5)
			{
				prism.bloomDownsampleEnum = BloomDownsampleAmount.Quarter;
			}

			if(prism.bloomDownsample < 3)
			{
				prism.bloomDownsampleEnum = BloomDownsampleAmount.Half;
			}

			prism.bloomBlurPasses = preset.bloomBlurPasses;
			prism.bloomIntensity = preset.bloomIntensity;
			prism.bloomThreshold = preset.bloomThreshold;
			prism.useBloomStability = preset.useBloomStability;
			prism.bloomUseScreenBlend = preset.bloomUseScreenBlend;
			
			prism.useLensDirt = preset.useBloomLensdirt;
			prism.dirtIntensity = preset.bloomLensdirtIntensity;
			prism.lensDirtTexture = preset.bloomLensdirtTexture;
			prism.useFullScreenBlur = preset.useFullScreenBlur;

			prism.useUIBlur = preset.useUIBlur;
			prism.uiBlurGrabTextureFromPassNumber = preset.uiBlurGrabTextureFromPassNumber;

			prism.bloomKnee = preset.bloomKnee;
			prism.bloomThreshType = preset.bloomThreshType;
			prism.bloomDownsampleEnum = preset.bloomDownsampleEnum;

			prism.bloomFlareStarburstTex = preset.bloomFlareStarburstTex;
			
			prism.bloomFlareStrength = preset.bloomFlareStrength;
			prism.bloomFlareHaloWidth = preset.bloomFlareHaloWidth;
			prism.bloomFlareGhostDispersal = preset.bloomFlareGhostDispersal;
			prism.bloomFlareChromaticDistortion = preset.bloomFlareChromaticDistortion;
			prism.bloomFlareNumberGhosts = preset.bloomFlareNumberGhosts;
			prism.bloomFlareLensDirtIntensityMultiplier = preset.bloomFlareLensDirtIntensityMultiplier;

			prism.bloomOneIntensity = preset.bloomOneIntensity;
			prism.bloomTwoIntensity = preset.bloomTwoIntensity;
			prism.bloomThreeIntensity = preset.bloomThreeIntensity;
			prism.bloomFourIntensity = preset.bloomFourIntensity;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.DepthOfField)
		{
			prism.useDof = preset.useDoF;
			prism.dofRadius = preset.dofRadius;
			prism.dofSampleAmount = preset.dofSampleCount;
			prism.dofBokehFactor = preset.dofBokehFactor;
			prism.dofFocusPoint = preset.dofFocusPoint;
			prism.dofFocusDistance = preset.dofFocusDistance;
			prism.useNearDofBlur = preset.useNearBlur;
			prism.dofBlurSkybox = preset.dofBlurSkybox;
			prism.dofNearFocusDistance = preset.dofNearFocusDistance;
			prism.dofForceEnableMedian = preset.dofForceEnableMedian;
			prism.dofDownsample = preset.dofDownsample;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.ChromaticAberration)
		{
			prism.useChromaticAberration = preset.useChromaticAb;
			prism.aberrationType = preset.aberrationType;
			prism.chromaticIntensity = preset.chromIntensity;
			prism.chromaticDistanceOne = preset.chromStart;
			prism.chromaticDistanceTwo = preset.chromEnd;
			prism.useChromaticBlur = preset.useChromaticBlur;
			prism.chromaticBlurWidth = preset.chromaticBlurWidth;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Vignette)
		{
			prism.useVignette = preset.useVignette;
			prism.vignetteStrength = preset.vignetteIntensity;
			prism.vignetteEnd = preset.vignetteEnd;
			prism.vignetteStart = preset.vignetteStart;
			prism.vignetteColor = preset.vignetteColor;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Noise)
		{
			prism.useNoise = preset.useNoise;
			prism.noiseIntensity = preset.noiseIntensity;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Sharpen)
		{
			prism.useSharpen = preset.useSharpen;
			prism.sharpenAmount = preset.sharpenAmount;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Colorify)
		{
			prism.useColorify = preset.useColorify;
			prism.colorifyAmount = preset.colorifyAmount;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Tonemap)
		{
			prism.useTonemap = preset.useTonemap;
			prism.tonemapType = preset.toneType;
			prism.toneValues = preset.toneValues;
			prism.secondaryToneValues = preset.secondaryToneValues;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Exposure)
		{
			prism.useExposure = preset.useExposure;
			prism.exposureMiddleGrey = preset.exposureMiddleGrey;
			prism.exposureLowerLimit = preset.exposureLowerLimit;
			prism.exposureUpperLimit = preset.exposureUpperLimit;
			prism.exposureSpeed = preset.exposureSpeed;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Gamma)
		{
			prism.useGammaCorrection = preset.useGammaCorrection;
			prism.gammaValue = preset.gammaValue;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.ColorCorrection)
		{
			prism.useLut = preset.useLUT;
			prism.lutLerpAmount = preset.lutIntensity;
			prism.basedOnTempTex = preset.lutPath;
			prism.twoDLookupTex = preset.twoDLookupTex;
			
			prism.useSecondLut = preset.useSecondLut;
			prism.secondaryLutLerpAmount = preset.secondaryLutLerpAmount;
			prism.secondaryBasedOnTempTex = preset.secondaryLutPath;
			prism.secondaryTwoDLookupTex = preset.secondaryTwoDLookupTex;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Fog)
		{
			prism.useFog = preset.useFog;
			prism.fogIntensity = preset.fogIntensity;
			prism.fogStartPoint = preset.fogStartPoint;
			prism.fogDistance = preset.fogDistance;
			prism.fogColor = preset.fogColor;
			prism.fogEndColor = preset.fogEndColor;
			prism.fogHeight = preset.fogHeight;

			prism.fogEndPoint = preset.fogEndPoint;
			prism.fogMidPoint = preset.fogMidPoint;
			prism.fogMidColor = preset.fogMidColor;
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.AmbientObscurance)
		{
			prism.useAmbientObscurance = preset.useAmbientObscurance;
			prism.useAODistanceCutoff = preset.useAODistanceCutoff;
			prism.aoIntensity = preset.aoIntensity;
            prism.aoContrast = preset.aoContrast;
			prism.aoRadius = preset.aoRadius;
			prism.aoDistanceCutoffStart = preset.aoDistanceCutoffStart;
			prism.aoDownsample = preset.aoDownsample;
			prism.aoBlurIterations = preset.aoBlurIterations;
			prism.aoDistanceCutoffLength = preset.aoDistanceCutoffLength;
			prism.aoSampleCount = preset.aoSampleCount;
			prism.aoBias = preset.aoBias;
			prism.aoBlurFilterDistance = preset.aoBlurFilterDistance;
			prism.aoBlurType = preset.aoBlurType;
			prism.aoLightingContribution = preset.aoLightingContribution;
			prism.occlusionSource = preset.occlusionSource;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Godrays)
		{
			prism.useRays = preset.useRays;
			prism.rayWeight = preset.rayWeight;
			prism.rayColor = preset.rayColor;
			prism.rayThreshold = preset.rayThreshold;

			prism.rayDensity = preset.rayDensity;
			prism.rayDecay = preset.rayDecay;
			prism.rayExposure = preset.rayExposure;
			prism.rayDownsample = preset.rayDownsample;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Colorify)
		{
			prism.useColorify = preset.useColorify;
			prism.colorifyAmount = preset.colorifyAmount;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Sharpen)
		{
			prism.useSharpen = preset.useSharpen;
			prism.sharpenAmount = preset.sharpenAmount;
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Brightness)
		{
			prism.useBrightness = preset.useBrightness;
			prism.brightIntensity = preset.brightIntensity;
			prism.brightCutoff = preset.brightCutoff;
			prism.brightSoftness = preset.brightSoftness;
		}
		
		if(preset.presetType == PrismPresetType.Full || !prism.currentPrismPreset)
		{
			prism.currentPrismPreset = preset;
		}

		Reset();
	}
	
	/// <summary>
	/// Lerps all of PRISM's values from its current preset to the target preset, based on the supplied time.
	/// Note that this will not turn on individual effects, you should instead make sure that 
	/// any effects that you want are already enabled, but with 0 intensity etc.
	/// NOTE that this only lerps "lerpable" values, not enums, toggles, etc
	/// Does not support night vision lerps
	/// Clamped to 0-1
	/// </summary>
	/// <param name="preset">Preset.</param>
	/// <param name="t">Time.</param>
	public void LerpToPreset(PrismPreset preset, float t, bool switchLUTsAtOne = false)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		
		PrismEffects prism = this;
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Bloom)
		{
			prism.bloomDownsample = (int)Mathf.Lerp((float)prism.currentPrismPreset.bloomDownsample, preset.bloomDownsample, t);
			prism.bloomBlurPasses = (int)Mathf.Lerp((float)prism.currentPrismPreset.bloomBlurPasses, preset.bloomBlurPasses, t); 
			prism.bloomIntensity = Mathf.Lerp(prism.currentPrismPreset.bloomIntensity, preset.bloomIntensity, t);
			prism.bloomThreshold = Mathf.Lerp(prism.currentPrismPreset.bloomThreshold, preset.bloomThreshold, t);
			
			prism.dirtIntensity = Mathf.Lerp(prism.currentPrismPreset.bloomLensdirtIntensity, preset.bloomLensdirtIntensity, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.DepthOfField)
		{
			prism.dofRadius = Mathf.Lerp(prism.currentPrismPreset.dofRadius, preset.dofRadius, t);
			prism.dofBokehFactor = Mathf.Lerp(prism.currentPrismPreset.dofBokehFactor, preset.dofBokehFactor, t);
			prism.dofFocusPoint = Mathf.Lerp(prism.currentPrismPreset.dofFocusPoint, preset.dofFocusPoint, t);
			prism.dofFocusDistance = Mathf.Lerp(prism.currentPrismPreset.dofFocusDistance, preset.dofFocusDistance, t);
			prism.dofNearFocusDistance = Mathf.Lerp(prism.currentPrismPreset.dofNearFocusDistance, preset.dofNearFocusDistance, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.ChromaticAberration)
		{
			prism.chromaticIntensity = Mathf.Lerp(prism.currentPrismPreset.chromIntensity, preset.chromIntensity, t);
			prism.chromaticDistanceOne = Mathf.Lerp(prism.currentPrismPreset.chromStart, preset.chromStart, t);
			prism.chromaticDistanceTwo = Mathf.Lerp(prism.currentPrismPreset.chromEnd, preset.chromEnd, t);
			prism.chromaticBlurWidth = Mathf.Lerp(prism.currentPrismPreset.chromaticBlurWidth, preset.chromaticBlurWidth, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Vignette)
		{
			prism.vignetteStrength = Mathf.Lerp(prism.currentPrismPreset.vignetteIntensity, preset.vignetteIntensity, t);
			prism.vignetteEnd = Mathf.Lerp(prism.currentPrismPreset.vignetteEnd, preset.vignetteEnd, t);
			prism.vignetteStart = Mathf.Lerp(prism.currentPrismPreset.vignetteStart, preset.vignetteStart, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Noise)
		{
			prism.noiseIntensity = Mathf.Lerp(prism.currentPrismPreset.noiseIntensity, preset.noiseIntensity, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Tonemap)
		{
			prism.toneValues = Vector3.Lerp(prism.currentPrismPreset.toneValues, preset.toneValues, t);
			prism.secondaryToneValues = Vector3.Lerp(prism.currentPrismPreset.secondaryToneValues, preset.secondaryToneValues, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Exposure)
		{
			prism.exposureMiddleGrey = Mathf.Lerp(prism.currentPrismPreset.exposureMiddleGrey, preset.exposureMiddleGrey, t);
			prism.exposureLowerLimit = Mathf.Lerp(prism.currentPrismPreset.exposureLowerLimit, preset.exposureLowerLimit, t);
			prism.exposureUpperLimit = Mathf.Lerp(prism.currentPrismPreset.exposureUpperLimit, preset.exposureUpperLimit, t);
			prism.exposureSpeed = Mathf.Lerp(prism.currentPrismPreset.exposureSpeed, preset.exposureSpeed, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Gamma)
		{
			prism.gammaValue = Mathf.Lerp(prism.currentPrismPreset.gammaValue, preset.gammaValue, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.ColorCorrection)
		{
			if(switchLUTsAtOne == true && t == 1.0f)
			{
				prism.useLut = preset.useLUT;
				prism.useSecondLut = preset.useLUT;
				prism.basedOnTempTex = preset.lutPath;
				prism.twoDLookupTex = preset.twoDLookupTex;
				
				prism.useSecondLut = preset.useSecondLut;
				prism.secondaryBasedOnTempTex = preset.secondaryLutPath;
				prism.secondaryTwoDLookupTex = preset.secondaryTwoDLookupTex;
			} else if(switchLUTsAtOne == false){
				prism.useLut = preset.useLUT;
				prism.useSecondLut = preset.useLUT;
				prism.basedOnTempTex = preset.lutPath;
				prism.twoDLookupTex = preset.twoDLookupTex;
				
				prism.useSecondLut = preset.useSecondLut;
				prism.secondaryBasedOnTempTex = preset.secondaryLutPath;
				prism.secondaryTwoDLookupTex = preset.secondaryTwoDLookupTex;
			}

			prism.lutLerpAmount = Mathf.Lerp(prism.currentPrismPreset.lutIntensity, preset.lutIntensity, t);
			prism.secondaryLutLerpAmount = Mathf.Lerp(prism.currentPrismPreset.secondaryLutLerpAmount, preset.secondaryLutLerpAmount, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Fog)
		{
			prism.fogIntensity = Mathf.Lerp(prism.currentPrismPreset.fogIntensity, preset.fogIntensity, t);
			prism.fogStartPoint = Mathf.Lerp(prism.currentPrismPreset.fogStartPoint, preset.fogStartPoint, t);
			prism.fogDistance = Mathf.Lerp(prism.currentPrismPreset.fogDistance, preset.fogDistance, t);
			prism.fogColor = Color.Lerp(prism.currentPrismPreset.fogColor, preset.fogColor, t);
			prism.fogEndColor = Color.Lerp(prism.currentPrismPreset.fogEndColor, preset.fogEndColor, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.AmbientObscurance)
		{
			prism.aoDistanceCutoffStart = Mathf.Lerp(prism.currentPrismPreset.aoDistanceCutoffStart, preset.aoDistanceCutoffStart, t);
			prism.aoIntensity = Mathf.Lerp(prism.currentPrismPreset.aoIntensity, preset.aoIntensity, t);
			prism.aoRadius = Mathf.Lerp(prism.currentPrismPreset.aoRadius, preset.aoRadius, t);
			prism.aoBias = Mathf.Lerp(prism.currentPrismPreset.aoBias, preset.aoBias, t);
			prism.aoDistanceCutoffLength = Mathf.Lerp(prism.currentPrismPreset.aoDistanceCutoffLength, preset.aoDistanceCutoffLength, t);
			prism.aoBlurIterations = (int)Mathf.Lerp((float)prism.currentPrismPreset.aoBlurIterations, preset.aoBlurIterations, t);
			prism.aoLightingContribution = (int)Mathf.Lerp((float)prism.currentPrismPreset.aoLightingContribution, preset.aoLightingContribution, t);
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Godrays)
		{
			prism.rayWeight = Mathf.Lerp(prism.currentPrismPreset.rayWeight, preset.rayWeight, t);
			prism.rayColor = Color.Lerp(prism.currentPrismPreset.rayColor, preset.rayColor, t);
			prism.rayThreshold = Color.Lerp(prism.currentPrismPreset.rayThreshold, preset.rayThreshold, t);
		}

		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Colorify)
		{
			prism.colorifyAmount = Mathf.Lerp(prism.currentPrismPreset.colorifyAmount, preset.colorifyAmount, t);
		}
		
		if(preset.presetType == PrismPresetType.Full || preset.presetType == PrismPresetType.Brightness)
		{
			prism.brightIntensity = Mathf.Lerp(prism.currentPrismPreset.brightIntensity, preset.brightIntensity, t);
		}
	}
	
	void OnEnable()
	{
		m_Camera = GetComponent<Camera>();
		
		if(!m_Shader)
		{
			m_Shader = Shader.Find("Hidden/PrismEffects");
			
			if(!m_Shader)
			{
				Debug.LogError("Couldn't find shader for PRISM! You shouldn't see this error.");
			}
		}
		
		if(!m_Shader2)
		{
			m_Shader2 = Shader.Find("Hidden/PrismEffectsSecondary");
			
			if(!m_Shader2)
			{
				Debug.LogError("Couldn't find secondary shader for PRISM! You shouldn't see this error.");
			}
		}

		if(!m_Shader3)
		{
			m_Shader3 = Shader.Find("Hidden/PrismEffectsTertiary");
			
			if(!m_Shader3)
			{
				Debug.LogError("Couldn't find tertiary shader for PRISM! You shouldn't see this error.");
			}
		}

		if(!m_DoFShader)
		{
			m_DoFShader = Shader.Find("Hidden/PrismDepthOfField");

			if(!m_DoFShader)
			{
				Debug.LogError("Couldn't find DoF1 shader for PRISM! You shouldn't see this error.");
			}
		}
		
		if(!m_AOShader)
		{
			m_AOShader = Shader.Find("Hidden/PrismKinoObscurance");
			if(!m_AOShader)
			{
				Debug.LogError("Couldn't find ao shader for PRISM! You shouldn't see this error.");
			}
		}

		if(!m_FlaresShader)
		{
			m_FlaresShader = Shader.Find("Hidden/PrismFlares");
			if(!m_FlaresShader)
			{
				Debug.LogError("Couldn't find flares shader for PRISM! You shouldn't see this error.");
			}
		}
		
		if(!currentAdaptationTexture)
		{
			currentAdaptationTexture = new RenderTexture(histWidth, histHeight, 0, RenderTextureFormat.Default);
			currentAdaptationTexture.filterMode = FilterMode.Bilinear;
			#if UNITY_5_5_OR_NEWER
			currentAdaptationTexture.autoGenerateMips = false;
			#else 
			currentAdaptationTexture.generateMips = false;
			#endif
		}

		if(useUIBlur)
		{
			uiBlurTexture = new RenderTexture(Screen.width / bloomDownsample, Screen.height / bloomDownsample, 0, RenderTextureFormat.Default);
			uiBlurTexture.filterMode = FilterMode.Bilinear;
			#if UNITY_5_5_OR_NEWER
			uiBlurTexture.autoGenerateMips = false;
			#else 
			uiBlurTexture.generateMips = false;
			#endif
		}
		
		if(useDof || useFog || useRays)
		{
			m_Camera.depthTextureMode |= DepthTextureMode.Depth;
		}

		if(useAmbientObscurance)
		{
            SetupCameraDepthTexturesForAO();
		}
		firstRun = true;
	}

	protected void SetupCameraDepthTexturesForAO()
    {
        if (IsGBufferAvailable == false || UsingTerrain == true)
        {
            // Enable depth textures which the occlusion source requires.
            if (occlusionSource == OcclusionSource.DepthTexture)
                m_Camera.depthTextureMode |= DepthTextureMode.Depth;

            if (occlusionSource == OcclusionSource.DepthNormalsTexture)
                m_Camera.depthTextureMode |= DepthTextureMode.DepthNormals;
        }
    }
	
	void SetupMRT()
	{
		if(_DoFMRT == null)
		{
			_DoFMRT = new RenderBuffer[2];
		}
	}
	
	void OnDestroy () {
		if (threeDLookupTex)
			DestroyImmediate (threeDLookupTex);
		threeDLookupTex = null;
		basedOnTempTex = "";
		twoDLookupTex = null;
		
		if (secondaryThreeDLookupTex)
			DestroyImmediate (secondaryThreeDLookupTex);
		secondaryThreeDLookupTex = null;
		secondaryBasedOnTempTex = "";
		secondaryTwoDLookupTex = null;
	}
	
	void OnDisable () {

        GetPrismCamera();

		if (m_Material) {
			DestroyImmediate (m_Material);
			m_Material = null;
		}
		
		if (m_Material2) {
			DestroyImmediate (m_Material2);
			m_Material2 = null;
		}

		if (m_Material3) {
			DestroyImmediate (m_Material3);
			m_Material3 = null;
		}

		if (m_DoFMaterial) {
			DestroyImmediate (m_DoFMaterial);
			m_DoFMaterial = null;
		}
		
		if (m_AOMaterial) {
			DestroyImmediate (m_AOMaterial);
			m_AOMaterial = null;
		}

		if (m_FlaresMaterial) {
			DestroyImmediate (m_FlaresMaterial);
			m_FlaresMaterial = null;
		}

		if(m_AOShader == m_Shader || m_Shader2 == m_Shader || m_Shader3 == m_Shader || m_DoFShader == m_Shader || m_FlaresShader == m_Shader)
		{
			m_AOShader = null;
			m_Shader2 = null;
			m_Shader3 = null;
			m_DoFShader = null;
			m_FlaresShader = null;
		}
		
		if (threeDLookupTex)
		{
			DestroyImmediate(threeDLookupTex);
			basedOnTempTex = "";
		}
		
		if (secondaryThreeDLookupTex)
		{
			DestroyImmediate(secondaryThreeDLookupTex);
			secondaryBasedOnTempTex = "";
		}
		
		if(currentAdaptationTexture)
		{
			DestroyImmediate(currentAdaptationTexture);
			currentAdaptationTexture = null;
		}

		if(uiBlurTexture)
		{
			DestroyImmediate(uiBlurTexture);
			uiBlurTexture = null;
		}
		
		m_Camera.depthTextureMode = DepthTextureMode.None;
	}
	
	protected Material CreateMaterial(Shader shader)
	{
		if (!shader)
			return null;
		Material m = new Material(shader);
		m.hideFlags = HideFlags.HideAndDontSave;
		return m;
	}

	public void DisableEffects()
	{
		areEffectsDisabled = true;
	}

	public void EnableEffects()
	{
		areEffectsDisabled = false;
	}
	
	public void Reset() {
		if(enabled)
		{
			OnDisable();
			OnEnable();
		}
	}

    [ContextMenu("ResetCameraDepthTextures")]
    public void ResetCameraDepthTextureFlags()
    {
        m_Camera.depthTextureMode = DepthTextureMode.None;
    }

	protected bool CreateMaterials()
	{	
		if(enableGuaranteedMaterialOptimization == true && firstRun == false)
		{
			return true;
		}

		if (m_Material == null && m_Shader != null && m_Shader.isSupported)
		{
			m_Material = CreateMaterial(m_Shader);
		} else if(m_Shader.isSupported == false){

			Debug.LogError("(1) Prism is not supported on this platform, or you have a shader compilation error somewhere.");

			return false;
		}
		
		if (m_Material2 == null && m_Shader2 != null && m_Shader2.isSupported)
		{
			m_Material2 = CreateMaterial(m_Shader2);
		}

		if (m_Material3 == null && m_Shader3 != null && m_Shader3.isSupported)
		{
			m_Material3 = CreateMaterial(m_Shader3);
		}

		if (m_DoFMaterial == null && m_DoFShader != null && m_DoFShader.isSupported)
		{
			m_DoFMaterial = CreateMaterial(m_DoFShader);
		} else if(m_DoFShader.isSupported == false){
			Debug.LogError("Prism (DoF shader) is not supported on this platform. Disabling some effects.");
			useDof = false;
			m_DoFShader = m_Shader;
			m_DoFMaterial = CreateMaterial(m_DoFShader);
		}
		
		if (m_AOMaterial == null && m_AOShader != null && m_AOShader.isSupported)
		{
			m_AOMaterial = CreateMaterial(m_AOShader);
		} else if(m_AOShader.isSupported == false){
			Debug.LogError("Prism (AO) is not supported on this platform, or you have a shader compilation error somewhere. Disabling AO.");
			m_AOShader = m_Shader;
			useAmbientObscurance = false;
			m_AOMaterial = CreateMaterial(m_AOShader);
		}

		if (m_FlaresMaterial == null && m_FlaresShader != null && m_FlaresShader.isSupported)
		{
			m_FlaresMaterial = CreateMaterial(m_FlaresShader);
		} else if(m_FlaresShader.isSupported == false){
			Debug.LogError("Prism (flares shader) is not supported on this platform. Disabling some effects.");
			useBloomFlares = false;
			m_FlaresShader = m_Shader;
			m_FlaresMaterial = CreateMaterial(m_FlaresShader);
		}
		
		if(m_Shader2.isSupported == false)
		{
			Debug.LogError("Prism (secondary shader) is not supported on this platform. ");
			//enabled = false;
			//return false;
		}

		if(m_Shader3.isSupported == false)
		{
			Debug.LogError("Prism (tertiary shader) is not supported on this platform. Disabling some effects.");
			m_Shader3 = m_Shader;
			useRays = false;
		}
		
		if(useLut == true && threeDLookupTex == null)
		{
			if(twoDLookupTex)
			{
				Convert(twoDLookupTex);
				if(threeDLookupTex == null)
				{
					useLut = false;
					Debug.LogWarning("No LUT found - disabling LookupTexture");
				}
			}
		}
		
		if(useLut == true && useSecondLut == true && secondaryThreeDLookupTex == null)
		{
			if(secondaryTwoDLookupTex)
			{
				Convert(secondaryTwoDLookupTex, true);
				if(secondaryThreeDLookupTex == null)
				{
					useSecondLut = false;
					Debug.LogWarning("No secondary LUT found - disabling secondary LookupTexture");
				}
			}
		}
		
		return true;
	}

	/// <summary>
	/// Sets the shader variables for both the materials that PRISM uses.
	/// Called in OnRenderImage() - you don't need to call this
	/// </summary>
	public void UpdateShaderValues()
	{
		if (m_Material == null)
			return;
		
		// State switching		
		m_Material.shaderKeywords = null;
		m_Material2.shaderKeywords = null;
		m_Material3.shaderKeywords = null;
		m_DoFMaterial.shaderKeywords = null;
		m_AOMaterial.shaderKeywords = null;

		if(useBloom)
		{
			SetBloomShaderValues(m_Material);
			SetBloomShaderValues(m_Material2);
			SetBloomShaderValues(m_Material3);

            if (bloomUseScreenBlend)
            {
                Shader.EnableKeyword("PRISM_BLOOM_SCREENBLEND");
				m_Material.EnableKeyword("PRISM_BLOOM_SCREENBLEND");
				m_Material2.EnableKeyword("PRISM_BLOOM_SCREENBLEND");
			}
            else
            {
                Shader.DisableKeyword("PRISM_BLOOM_SCREENBLEND");
				m_Material.DisableKeyword("PRISM_BLOOM_SCREENBLEND");
				m_Material2.DisableKeyword("PRISM_BLOOM_SCREENBLEND");
			}

            if (useBloomFlares)
			{
				//Shader.EnableKeyword("PRISM_USE_FLARES");
				SetBloomFlaresShaderValues(m_FlaresMaterial);
				SetBloomFlaresShaderValues(m_Material);
			} else {
				m_Material.SetFloat("_FlareStrength", 0.0f);
			}

			//Simple bloom
			if(bloomType == BloomType.Simple)
			{
				Shader.DisableKeyword("PRISM_HDR_BLOOM");
				Shader.EnableKeyword("PRISM_SIMPLE_BLOOM");
			//HDR bloom
			} else if(bloomType == BloomType.HDR) {
				Shader.DisableKeyword("PRISM_SIMPLE_BLOOM");
				Shader.EnableKeyword("PRISM_HDR_BLOOM");
			}
			
			if(useUIBlur)
			{
				Shader.EnableKeyword("PRISM_UIBLUR");
			} else {
				Shader.DisableKeyword("PRISM_UIBLUR");
			}
		} else {
			//Shader.DisableKeyword("PRISM_USE_FLARES");
			//m_Material.DisableKeyword("PRISM_USE_FLARES");
			Shader.DisableKeyword("PRISM_BLOOM_SCREENBLEND");
            Shader.DisableKeyword("PRISM_HDR_BLOOM");
			Shader.EnableKeyword("PRISM_SIMPLE_BLOOM");
			Shader.DisableKeyword("PRISM_USE_STABLEBLOOM");
			Shader.DisableKeyword("PRISM_USE_BLOOM");
			Shader.DisableKeyword("PRISM_UIBLUR");
		}
		
		SetBloomStablityValues();
		
		if(useFog)
		{
			SetFogShaderValues(m_Material);
		} else {
			m_Material.SetFloat("_FogIntensity", 0.0f);
		}
		
		if(useVignette)
		{
			m_Material.SetFloat("_VignetteStart", vignetteStart);
			m_Material.SetFloat("_VignetteEnd", vignetteEnd);
			m_Material.SetFloat("_VignetteIntensity", vignetteStrength);
			m_Material.SetColor("_VignetteColor", vignetteColor);
		} else {
			m_Material.SetFloat("_VignetteIntensity", 0f);
		}

		if(useNoise || useDither)
		{
			m_Material.SetTexture("_GrainTex", noiseTexture);
		}
		
		if(useNoise)
		{
			m_Material.SetVector("_GrainIntensity", new Vector2(noiseIntensity * secondaryNoiseMultiplier, noiseIntensity));
			
			//Debug.Log("Hal: " + GetComponent<FrustumJitter>().UpdateJitterAndSample().x.ToString() + GetComponent<FrustumJitter>().UpdateJitterAndSample().y.ToString());
			var tempVec = new Vector2(0.5f - Random.value, 0.5f - Random.value);
			//Debug.Log("Rnd: " + tempVec.x.ToString() + tempVec.y.ToString());
			m_Material.SetVector("_RandomInts", tempVec);// GetComponent<FrustumJitter>().UpdateJitterAndSample());
		}
		
		//Chromatic
		if(useChromaticAberration)
		{
			float abMultiplier = 0f;
			float realIntensity = chromaticIntensity * chromaticIntensity;

			if(aberrationType == AberrationType.Barrel)
			{
				//realIntensity = Mathf.Clamp((realIntensity * -1f), -0.15f, -0.05f);
			}

			if(aberrationType == AberrationType.Vertical)
			{
				abMultiplier = 1f;		
			} else if (aberrationType == AberrationType.Vignette){
				abMultiplier = 0f;
			} else {
				abMultiplier = -1f;
			}
			m_Material.SetFloat("_ChromaticIntensity", realIntensity);	
			m_Material2.SetFloat("_ChromaticIntensity", realIntensity);
			
			m_Material.SetVector("_ChromaticParams", new Vector4(chromaticDistanceOne, chromaticDistanceTwo, abMultiplier, chromaticBlurWidth));
			m_Material2.SetVector("_ChromaticParams", new Vector4(chromaticDistanceOne, chromaticDistanceTwo, abMultiplier, chromaticBlurWidth));

			/*if(chromaticBlurWidth > 0.01f)
			{
				Shader.EnableKeyword("PRISM_CHROMATICBLURNOISE");
			} else {
				Shader.DisableKeyword("PRISM_CHROMATICBLURNOISE");
			}*/

		} else {
			m_Material.SetFloat("_ChromaticIntensity", 0f);
		}
		
		//Tonemap
		if(useTonemap)
		{
			m_Material.SetVector("_ToneParams", new Vector4(toneValues.x, toneValues.y, toneValues.z, toneValues.z));
			m_Material.SetVector("_SecondaryToneParams", new Vector4(secondaryToneValues.x, secondaryToneValues.y, secondaryToneValues.z, secondaryToneValues.z));
		}
		
		if(useGammaCorrection)
		{
			m_Material.SetFloat("_Gamma", gammaValue);
			//Shader.EnableKeyword("PRISM_USE_GAMMA");
		} else 	{
			m_Material.SetFloat("_Gamma", 0f);
			//Shader.DisableKeyword("PRISM_USE_GAMMA");
		}
		//end ex
		
		//Dof
		if(useDof)
		{
			Shader.EnableKeyword("PRISM_USE_DOF");
			SetDofValues(m_Material);
			SetupMRT();
		} else {
			Shader.DisableKeyword("PRISM_USE_DOF");
		}
		//End dof
		
		//Sharpen todo
		if(useSharpen)
		{
			m_Material2.SetFloat("_SharpenAmount", sharpenAmount);
		}
		
		if(useLensDirt)
		{
			m_Material.SetTexture("_DirtTex", lensDirtTexture);
			m_Material2.SetTexture("_DirtTex", lensDirtTexture);
			m_FlaresMaterial.SetTexture("_DirtTex", lensDirtTexture);
			//Unused at the moment
			//Shader.EnableKeyword("USE_LENSDIRT");
		} else 	{
			m_Material.SetFloat("_DirtIntensity", 0f);
			//Shader.DisableKeyword("USE_LENSDIRT");
		}
		
		if(useExposure)
		{
			//m_Material.SetFloat("_ExposureSpeed", exposureSpeed);	
			m_Material.SetFloat("_ExposureLowerLimit", exposureLowerLimit);
			m_Material.SetFloat("_ExposureUpperLimit", exposureUpperLimit);
			m_Material.SetFloat("_ExposureMiddleGrey", exposureMiddleGrey);
			
			m_Material2.SetFloat("_ExposureSpeed", exposureSpeed);	
			m_Material2.SetFloat("_ExposureLowerLimit", exposureLowerLimit);
			m_Material2.SetFloat("_ExposureUpperLimit", exposureUpperLimit);
			m_Material2.SetFloat("_ExposureMiddleGrey", exposureMiddleGrey);

			m_Material3.SetFloat("_ExposureLowerLimit", exposureLowerLimit);
			m_Material3.SetFloat("_ExposureUpperLimit", exposureUpperLimit);
			m_Material3.SetFloat("_ExposureMiddleGrey", exposureMiddleGrey);
			
			Shader.EnableKeyword("PRISM_USE_EXPOSURE");
		} else 	{
			Shader.DisableKeyword("PRISM_USE_EXPOSURE");
		}
		
		Shader.DisableKeyword("PRISM_DOF_LOWSAMPLE");
		Shader.DisableKeyword("PRISM_DOF_MEDSAMPLE");
		
		if(useDof)
		{
			if (dofSampleAmount == DoFSamples.Low)
				Shader.EnableKeyword("PRISM_DOF_LOWSAMPLE");
			else if (dofSampleAmount == DoFSamples.Medium)
				Shader.EnableKeyword("PRISM_DOF_MEDSAMPLE");
			else 
			{
				Shader.EnableKeyword("PRISM_DOF_MEDSAMPLE");
				Shader.DisableKeyword("PRISM_DOF_LOWSAMPLE");
				Debug.LogWarning("High sample DoF is currently deprecated. If you want to increase your DoF samples, change around line 81 ('DOF_SAMPLES') in PRISM.cginc to a higher number");
				dofSampleAmount = DoFSamples.Medium;
			}
			
			if(useNearDofBlur)
			{
				Shader.EnableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material.EnableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material2.EnableKeyword("PRISM_DOF_USENEARBLUR");
			} else {
				Shader.DisableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material.DisableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material2.DisableKeyword("PRISM_DOF_USENEARBLUR");
			}
		} else {
			Shader.DisableKeyword("PRISM_DOF_USENEARBLUR");

			if(useFullScreenBlur)
			{
				m_Material.EnableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material2.EnableKeyword("PRISM_DOF_USENEARBLUR");
			} else {
				m_Material.DisableKeyword("PRISM_DOF_USENEARBLUR");
				m_Material2.DisableKeyword("PRISM_DOF_USENEARBLUR");
			}
		}		

		SetPrimaryLutShaderValues();
		
		SetSecondaryLutShaderValues();
		
		if(!useLut)
		{
			m_Material.DisableKeyword("PRISM_GAMMA_LOOKUP");
			m_Material.DisableKeyword("PRISM_LINEAR_LOOKUP");
		} 
		#if UNITY_5_6_OR_NEWER
		else if(m_Camera.allowHDR)//HDR
		#else
		else if(m_Camera.hdr)//HDR
		#endif
		{
			m_Material.EnableKeyword("PRISM_LINEAR_LOOKUP");
			m_Material.DisableKeyword("PRISM_GAMMA_LOOKUP");
		} 
		else 
		{
			m_Material.EnableKeyword("PRISM_GAMMA_LOOKUP");
			m_Material.DisableKeyword("PRISM_LINEAR_LOOKUP");
		}
		
		if(useTonemap == true && tonemapType == TonemapType.Filmic)
		{
			Shader.DisableKeyword("PRISM_ROMB_TONEMAP");
			Shader.DisableKeyword("PRISM_ACES_TONEMAP");
			m_Material.DisableKeyword("PRISM_ROMB_TONEMAP");
			m_Material.DisableKeyword("PRISM_ACES_TONEMAP");

			m_Material.EnableKeyword("PRISM_FILMIC_TONEMAP");
			Shader.EnableKeyword("PRISM_FILMIC_TONEMAP");
		} else if(useTonemap == true && tonemapType == TonemapType.RomB){
			Shader.DisableKeyword("PRISM_FILMIC_TONEMAP");
			Shader.DisableKeyword("PRISM_ACES_TONEMAP");
			m_Material.DisableKeyword("PRISM_FILMIC_TONEMAP");
			m_Material.DisableKeyword("PRISM_ACES_TONEMAP");
			
			m_Material.EnableKeyword("PRISM_ROMB_TONEMAP");
			Shader.EnableKeyword("PRISM_ROMB_TONEMAP");

		} else if(useTonemap == true && tonemapType == TonemapType.ACES)
		{
			m_Material.DisableKeyword("PRISM_FILMIC_TONEMAP");
			m_Material.DisableKeyword("PRISM_ROMB_TONEMAP");
			Shader.DisableKeyword("PRISM_FILMIC_TONEMAP");
			Shader.DisableKeyword("PRISM_ROMB_TONEMAP");

			m_Material.EnableKeyword("PRISM_ACES_TONEMAP");
			Shader.EnableKeyword("PRISM_ACES_TONEMAP");
		} else {
			m_Material.DisableKeyword("PRISM_FILMIC_TONEMAP");
			m_Material.DisableKeyword("PRISM_ROMB_TONEMAP");
			m_Material.DisableKeyword("PRISM_ACES_TONEMAP");
			Shader.DisableKeyword("PRISM_FILMIC_TONEMAP");
			Shader.DisableKeyword("PRISM_ROMB_TONEMAP");
			Shader.DisableKeyword("PRISM_ACES_TONEMAP");
		}
		
		if(useNoise)
		{
			m_Material.SetFloat("useNoise", 1.0f);
		} else {
			m_Material.SetFloat("useNoise", 0.0f);
		}

		if(useBrightness)
		{
			SetBrightnessShaderValues(m_Material);
		} else {
			m_Material.SetVector("_BrightCurve", new Vector4(0f, 0f));
		}

		if(useRays)
		{
			SetGodraysShaderValues(m_Material3);

			SetGodraysShaderValues(m_Material);
		} else {
			m_Material.SetFloat("_SunWeight", 0.0f);
		}

		if(useColorify)
		{
			m_Material.SetFloat("_Colourfulness", colorifyAmount);
		} else {
			m_Material.SetFloat("_Colourfulness", 0f);
		}

		if(useAmbientObscurance)
		{
            if (IsGBufferAvailable == false || UsingTerrain == true)
            {
                SetupCameraDepthTexturesForAO();
            } else if (IsGBufferAvailable == true)
            {
                if (autoTurnOffDepthTextures)
                {
                    m_Camera.depthTextureMode &= ~DepthTextureMode.Depth;
                    m_Camera.depthTextureMode &= ~DepthTextureMode.DepthNormals;
                }
            }
			
			//NEED THIS
			m_Material.SetFloat("_AOIntensity", aoIntensity);
			m_Material.SetFloat("_AOLuminanceWeighting", aoLightingContribution * 100f);
			
			if(useAODistanceCutoff == true || useDof == true)
			{
				m_AOMaterial.EnableKeyword("_AOCUTOFF_ON");
			} else {
				m_AOMaterial.DisableKeyword("_AOCUTOFF_ON");
			}

            if (IsGBufferAvailable && UsingTerrain == false)
            {

            }

            if (occlusionSource == OcclusionSource.DepthTexture)
            {
                m_AOMaterial.EnableKeyword("SOURCE_DEPTH");
                m_AOMaterial.DisableKeyword("SOURCE_DEPTHNORMALS");
                m_AOMaterial.DisableKeyword("SOURCE_GBUFFER");
            } else if(occlusionSource == OcclusionSource.DepthNormalsTexture)
            {
                m_AOMaterial.DisableKeyword("SOURCE_DEPTH");
                m_AOMaterial.EnableKeyword("SOURCE_DEPTHNORMALS");
                m_AOMaterial.DisableKeyword("SOURCE_GBUFFER");
            } else
            {
                m_AOMaterial.DisableKeyword("SOURCE_DEPTH");
                m_AOMaterial.DisableKeyword("SOURCE_DEPTHNORMALS");
                m_AOMaterial.EnableKeyword("SOURCE_GBUFFER");
            }
			
			// Sample count
			if (aoSampleCount == SampleCount.Low)
			{
				m_AOMaterial.EnableKeyword("_AOSAMPLECOUNT_LOWEST");
				m_AOMaterial.DisableKeyword("_AOSAMPLECOUNT_CUSTOM");
				m_AOMaterial.SetInt("_AOSampleCount", aoSampleCountValue);
			}
			else
			{
				m_AOMaterial.EnableKeyword("_AOSAMPLECOUNT_CUSTOM");
				m_AOMaterial.DisableKeyword("_AOSAMPLECOUNT_LOWEST");
				m_AOMaterial.SetInt("_AOSampleCount", aoSampleCountValue);
			}
			
		} else {			
			m_Material.SetFloat("_AOIntensity", aoMinIntensity);
		}
	}

	protected void SetBloomStablityValues()
	{
		if(useBloom == true && useBloomStability == false || (useBloom && !accumulationBuffer))
		{
			Shader.EnableKeyword("PRISM_USE_BLOOM");
			Shader.DisableKeyword("PRISM_USE_STABLEBLOOM");
		} else if(useBloom == true && useBloomStability == true)
		{
			Shader.EnableKeyword("PRISM_USE_STABLEBLOOM");
			Shader.DisableKeyword("PRISM_USE_BLOOM");
		} else 
		{
			Shader.DisableKeyword("PRISM_USE_STABLEBLOOM");
			Shader.DisableKeyword("PRISM_USE_BLOOM");
		}
	}

	protected void SetBrightnessShaderValues(Material brightMaterial)
	{
		brightMaterial.SetVector("_BrightCurve", new Vector4(brightCutoff, Mathf.LerpUnclamped(1.0f,0.0001f, brightIntensity), brightSoftness, 0.0f));
	}

	protected void SetBloomShaderValues (Material bloomMaterial)
	{
		bloomMaterial.SetFloat("_BloomThreshold", bloomThreshold);
		bloomMaterial.SetFloat("_BloomRadius", bloomRadius);
		bloomMaterial.SetFloat("_BloomIntensity", bloomIntensity);

		bloomMaterial.SetFloat("_Bloom1Intensity", bloomOneIntensity);
		bloomMaterial.SetFloat("_Bloom2Intensity", bloomTwoIntensity);
		bloomMaterial.SetFloat("_Bloom3Intensity", bloomThreeIntensity);
		bloomMaterial.SetFloat("_Bloom4Intensity", bloomFourIntensity);
		
		float diSqsuared = dirtIntensity;
		if(useBloomFlares)
		{
				diSqsuared = dirtIntensity * bloomFlareLensDirtIntensityMultiplier;
		}
		
		bloomMaterial.SetFloat("_DirtIntensity", dirtIntensity);
		bloomMaterial.SetFloat("_FlareDirtIntensity", diSqsuared);

		if(bloomThreshType == BloomThreshholdType.Quadratic)
		{
			Shader.EnableKeyword("PRISM_CURVE_THRESH");
			Shader.DisableKeyword("PRISM_GRADUAL_THRESH");
			Shader.DisableKeyword("PRISM_HIGHLIGHT_THRESH");

			float knee = bloomThreshold * bloomKnee + 1e-5f;
			var curve = new Vector3(bloomThreshold - knee, knee * 2f, 0.25f / knee);
			bloomMaterial.SetVector("_BloomCurve", curve);
		} else if(bloomThreshType == BloomThreshholdType.Gradual)
		{
			Shader.EnableKeyword("PRISM_GRADUAL_THRESH");
			Shader.DisableKeyword("PRISM_CURVE_THRESH");
			Shader.DisableKeyword("PRISM_HIGHLIGHT_THRESH");
		} else if(bloomThreshType == BloomThreshholdType.Highlights) {
			Shader.EnableKeyword("PRISM_HIGHLIGHT_THRESH");
			Shader.DisableKeyword("PRISM_CURVE_THRESH");
			Shader.DisableKeyword("PRISM_GRADUAL_THRESH");
		}
	}

	protected void SetBloomFlaresShaderValues (Material flaresMaterial)
	{
		flaresMaterial.SetFloat("_FlareGhostDispersal", bloomFlareGhostDispersal);
		flaresMaterial.SetFloat("_FlareChromaticDistortion", bloomFlareChromaticDistortion);
		flaresMaterial.SetFloat("_FlareHaloWidth", bloomFlareHaloWidth);
		flaresMaterial.SetFloat("_FlareStrength", bloomFlareStrength);
		flaresMaterial.SetFloat("_CamRotation", m_Camera.transform.rotation.eulerAngles.y);
		flaresMaterial.SetInt("_FlareNumberOfGhosts", bloomFlareNumberGhosts);
		//flaresMaterial.SetTexture("_FlareRadialColorTex", bloomFlareRadialColorTex);
		flaresMaterial.SetTexture("_FlareStarburstTex", bloomFlareStarburstTex);
		//flaresMaterial.EnableKeyword("PRISM_USE_FLARES");
		//flaresMaterial.SetVector("_RadialValues", new Vector3(bloomFlareRadius1, bloomFlareRadius2, bloomFlareRadius3));
	}

	protected void SetGodraysShaderValues(Material raysMaterial)
	{
		raysMaterial.SetFloat("_SunWeight", rayWeight);
		raysMaterial.SetColor("_SunColor", rayColor);
		raysMaterial.SetColor("_SunThreshold", rayThreshold);

		raysMaterial.SetFloat("_SunDecay", rayDecay);
		raysMaterial.SetFloat("_SunDensity", rayDensity);
		raysMaterial.SetFloat("_SunExposure", rayExposure);

		if(!rayTransform)
		{
			raysMaterial.SetVector("_SunPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 1f));
		} else {
			Vector3 v = m_Camera.WorldToViewportPoint (rayTransform.position);
			raysMaterial.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, rayWeight));
			
			if (v.z >= 0.0f)
			{
				raysMaterial.SetColor ("_SunColor", rayColor);
			}
			else
			{
				//Debug.Log("waasda");
				raysMaterial.SetColor ("_SunColor", Color.black);
			}
				
		}
	}
	
	protected void SetFogShaderValues(Material fogMaterial)
	{
		fogMaterial.SetFloat("_FogIntensity", 1f);
		fogMaterial.SetFloat("_FogDistance", fogDistance);
		fogMaterial.SetFloat("_FogStart", fogStartPoint);
		fogMaterial.SetColor("_FogColor", fogColor);
		/*fogMaterial.SetFloat("_FogMidPoint", fogMidPoint);
		fogMaterial.SetFloat("_FogEndPoint", fogEndPoint);
		fogMaterial.SetColor("_FogMidColor", fogMidColor);
		fogMaterial.SetColor("_FogEndColor", fogEndColor);*/

		fogMaterial.SetVector("_FogParams", new Vector3(fogNoiseFactor1, fogNoiseFactor2, (float)fogExpFactor));

		if(fogAffectSkybox)
		{
			fogMaterial.SetFloat("_FogBlurSkybox", 1.0f);
		} else {
			fogMaterial.SetFloat("_FogBlurSkybox", 0.999999f);
		}
		
		fogMaterial.SetMatrix("_InverseView", m_Camera.cameraToWorldMatrix);
	}
	
	protected void SetAOShaderValues(Material aoMaterial)
	{
		m_Material.SetFloat("_AOLuminanceWeighting", aoLightingContribution * 100f);
        aoMaterial.SetFloat("_AOIntensity", aoIntensity);
        aoMaterial.SetVector("_AOIntensityContrast", new Vector2(aoIntensity, aoContrast));
        aoMaterial.SetFloat("_AORadius", aoRadius);
		aoMaterial.SetFloat("_AOBias", aoBias * 0.02f);
		aoMaterial.SetFloat("_AOTargetScale", aoDownsample ? 0.5f : 1f);

		if(useDof)
		{
			aoMaterial.SetFloat("_AOCutoff", dofFocusPoint);
			aoMaterial.SetFloat("_AOCutoffRange", dofFocusDistance);
		} else {
			aoMaterial.SetFloat("_AOCutoff", aoDistanceCutoffStart);
			aoMaterial.SetFloat("_AOCutoffRange", aoDistanceCutoffLength);
		}

        aoMaterial.SetMatrix("_AOCameraModelView", m_Camera.cameraToWorldMatrix);

        Matrix4x4 projMatrix = m_Camera.projectionMatrix;
		Vector4 projInfo = new Vector4
			((-2.0f / (Screen.width * projMatrix[0])),
			 (-2.0f / (Screen.height * projMatrix[5])),
			 ((1.0f - projMatrix[2]) / projMatrix[0]),
			 ((1.0f + projMatrix[6]) / projMatrix[5]));
		
		aoMaterial.SetVector ("_AOProjInfo", projInfo); // used for unprojection
	}
	
	protected void SetPrimaryLutShaderValues()
	{
		if(useLut == true && twoDLookupTex && threeDLookupTex)
		{
			int lutSize = threeDLookupTex.width;
			threeDLookupTex.wrapMode = TextureWrapMode.Clamp;
			m_Material.SetFloat("_LutScale", (lutSize - 1) / (1.0f*lutSize));
			m_Material.SetFloat("_LutOffset", 1.0f / (2.0f * lutSize));
			m_Material.SetTexture("_LutTex", threeDLookupTex);
			m_Material.SetFloat("_LutAmount", lutLerpAmount);
		} else {
			m_Material.SetFloat("_LutAmount", 0f);
		}
	}
	
	protected void SetSecondaryLutShaderValues()
	{
		if(useSecondLut == true && secondaryTwoDLookupTex && secondaryThreeDLookupTex)
		{
			int lutTwoSize = secondaryThreeDLookupTex.width;
			secondaryThreeDLookupTex.wrapMode = TextureWrapMode.Clamp;
			m_Material.SetFloat("_SecondLutScale", (lutTwoSize - 1) / (1.0f*lutTwoSize));
			m_Material.SetFloat("_SecondLutOffset", 1.0f / (2.0f * lutTwoSize));
			m_Material.SetTexture("_SecondLutTex", secondaryThreeDLookupTex);
			m_Material.SetFloat("_SecondLutAmount", secondaryLutLerpAmount);
		} else {
			m_Material.SetFloat("_SecondLutAmount", 0f);
		}
	}
	
	/// <summary>
	/// Sets a transform that the Depth of Field component of PRISM will use as a Focus Point
	/// </summary>
	/// <param name="target">Target. - The target transform</param>
	public void SetDofTransform(Transform target)
	{
		dofFocusTransform = target;
	}
	
	/// <summary>
	/// Sets a point in world space that the Depth of Field component of PRISM will use as a Focus Point
	/// </summary>
	/// <param name="point">Focus Point.</param>
	public void SetDofPoint(Vector3 point)
	{
		dofFocusPoint = Vector3.Distance(m_Camera.transform.position, point);
	}
	
	/// <summary>
	/// Resets the Depth of Field focus transform back to null. Call this to stop the transform from being used as a focus point.
	/// </summary>
	public void ResetDofTransform()
	{
		dofFocusTransform = null;
	}
	
	//Debug/helper stuff
	protected void SetDofValues(Material targetMat)
	{
		if(dofFocusTransform)
		{
			targetMat.SetFloat("_DofFocusPoint", Vector3.Distance(transform.position, dofFocusTransform.position));
		} else {
			targetMat.SetFloat("_DofFocusPoint", dofFocusPoint);
			targetMat.SetFloat("_DofNearFocusPoint", dofNearFocusPoint);
		}
		
		targetMat.SetFloat("_DofFocusDistance", dofFocusDistance);
		if(useDICEDof)
		{
			targetMat.SetFloat("_DofRadius", 32f * dofRadius);

			targetMat.SetFloat("_DofFactor", dofBokehFactor);

		} else {
			targetMat.SetFloat("_DofRadius", dofRadius);
			targetMat.SetFloat("_DofFactor", dofBokehFactor);
		}

		if(useNearDofBlur)
		{
			targetMat.SetFloat("_DofUseNearBlur", 1.0f);
			targetMat.SetFloat("_DofNearFocusDistance", dofNearFocusDistance);
		} else {
			targetMat.SetFloat("_DofUseNearBlur", 0.0f);
		}
		if(dofBlurSkybox)
		{
			targetMat.SetFloat("_DofBlurSkybox", 1.0f);
		} else {
			targetMat.SetFloat("_DofBlurSkybox", 0.9999999f);
		}
	}
	
	protected void HandleExposure(RenderTexture source, RenderTextureFormat rtFormat)
	{
		RenderTexture downSampledTex = RenderTexture.GetTemporary (source.width / 2, source.width / 2, 0, rtFormat);
		
		downSampledTex.filterMode = FilterMode.Bilinear;
		
		//2 = downsample 
		//3 = luminance
		Graphics.Blit(source, downSampledTex, m_Material2, 3);
		
		int targetSize = histWidth;
		while(downSampledTex.height > targetSize || downSampledTex.width > targetSize)
		{
			const int REDUCE_RATIO = 2; // our shader does 2x2 reduction
			int destW = downSampledTex.width / REDUCE_RATIO;
			if ( destW < targetSize ) destW = targetSize;
			int destH = downSampledTex.height / REDUCE_RATIO;
			if ( destH < targetSize ) destH = targetSize;
			RenderTexture rtTempDst = RenderTexture.GetTemporary(destW,destH, 0, rtFormat);
			Graphics.Blit (downSampledTex, rtTempDst, m_Material2, 2);
			
			// Release old src temporary, and make new temporary the source
			RenderTexture.ReleaseTemporary( downSampledTex );
			downSampledTex = rtTempDst;
		}
		
		//Before we adapt, set our last adaptation texture
		if(currentAdaptationTexture)
		{
			currentAdaptationTexture.MarkRestoreExpected();
		}

		
		//Just do this
		int adaptPass = 4;
		
		if(firstRun)
		{
			Graphics.Blit(downSampledTex, currentAdaptationTexture);
		} else {
			Graphics.Blit(downSampledTex, currentAdaptationTexture, m_Material2, adaptPass);
		}
		
		m_Material.SetTexture("_BrightnessTexture", currentAdaptationTexture);
		m_Material2.SetTexture("_BrightnessTexture", currentAdaptationTexture);
		m_Material3.SetTexture("_BrightnessTexture", currentAdaptationTexture);
		
		RenderTexture.ReleaseTemporary(downSampledTex);
	}

	/// <summary>
	/// Blurs the temporary rendertexture a number of times
	/// </summary>
	/// <returns>The tex.</returns>
	/// <param name="tex">Tex.</param>
	/// <param name="iterations">Iterations.</param>
	protected RenderTexture BlurTex(RenderTexture tex, int iterations = 1)
	{
		for(int i = 0; i < iterations; i++)
		{
			RenderTexture blurOne = RenderTexture.GetTemporary (tex.width, tex.height, 0, RenderTextureFormat.DefaultHDR);
			Graphics.Blit (tex, blurOne, m_Material2, 14);
			tex.DiscardContents();
			Graphics.Blit (blurOne, tex, m_Material2, 13);
			
			RenderTexture.ReleaseTemporary(blurOne);
		}
		
		RenderTexture.ReleaseTemporary(tex);
		
		return tex;
	}

	protected RenderTexture HandleDofBlurDICE(RenderTexture source, RenderTexture tex, int sizeX , int sizeY)
	{
		int rtW2 = sizeX;
		int rtH2 = sizeY;
		RenderTextureFormat rtFormat = RenderTextureFormat.DefaultHDR;
		RenderTextureReadWrite isLinear = RenderTextureReadWrite.Default;
		RenderTexture dof1 = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat, isLinear);
		
		RenderTexture dof2 = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat, isLinear);
		RenderTexture dof3 = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat, isLinear);
		RenderTexture dof4 = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat, isLinear);
		RenderTexture dof5 = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat, isLinear);

		float weight = dofShapeWeight;

		//MRT 1
		{
			//1
			m_DoFMaterial.SetVector("_DofBlurDir", new Vector2(0.0f, 1.0f));
			Graphics.Blit(source, dof1, m_DoFMaterial, 8);
			//2

			m_DoFMaterial.SetVector("_DofBlurDir", new Vector2(1.0f, -weight));
			Graphics.Blit(source, dof2, m_DoFMaterial, 8);
		}

		//Now move this asi

		//MRT 2
		{
			m_DoFMaterial.SetVector("_DofBlurDir", new Vector2(-1.0f, -weight));
			Graphics.Blit(dof1, dof3, m_DoFMaterial, 9);
			
			//4
			m_DoFMaterial.SetVector("_DofBlurDir", new Vector2(1.0f, -weight));
			Graphics.Blit(dof1, dof4, m_DoFMaterial, 9);
		}
		
		//5
		m_DoFMaterial.SetVector("_DofBlurDir", new Vector2(-1.0f, -weight));
		m_DoFMaterial.SetTexture("_DofBlur1", dof3);
		m_DoFMaterial.SetTexture("_DofBlur2", dof4);

		Graphics.Blit(dof2, tex, m_DoFMaterial, 10);

	//ReleaseDofRenderTextureArea:
		RenderTexture.ReleaseTemporary(dof1);
		RenderTexture.ReleaseTemporary(dof2);
		RenderTexture.ReleaseTemporary(dof3);
		RenderTexture.ReleaseTemporary(dof4);
		RenderTexture.ReleaseTemporary(dof5);
		return tex;
	}
	
	[ImageEffectTransformsToLDR]
	//[ImageEffectOpaque]
	protected void OnRenderImage(RenderTexture source, RenderTexture destination)
	{		
		//Todo here - make this bool do something :)
		bool usingAnyImageEffects = !areEffectsDisabled;
		
		if(shouldSkipNextFrame)
		{
			Graphics.Blit(source, destination);
			shouldSkipNextFrame = false;
			return;
		}
		
		if(CreateMaterials() == false || usingAnyImageEffects == false)
		{
			Graphics.Blit(source, destination);
			return;
		}
		
		UpdateShaderValues();
		
		if(threeDLookupTex == null && useLut == true)
		{
			SetIdentityLut(false);
		}
		
		if(secondaryThreeDLookupTex == null && useSecondLut == true)
		{
			SetIdentityLut(true);
		}
		
		var rtFormat = RenderTextureFormat.ARGBFloat;
		
		#if UNITY_5_6_OR_NEWER
		if(m_Camera.allowHDR)//HDR
		#else
		if(m_Camera.hdr)//HDR
		#endif
		{
			rtFormat = RenderTextureFormat.DefaultHDR;
		} else {
			rtFormat = RenderTextureFormat.Default;
		}
		
		//Get AO Texture etc
		int aoDSA = 1;
		if(aoDownsample == true) aoDSA = 2;
		
		var aortW2 = source.width/aoDSA;
		var aortH2= source.height/aoDSA;

		var raysTexWidth = source.width/rayDSA;
		var raysTexHeight = source.height/rayDSA;

		int sourceWidthDofSize = (int)(source.width / (int)dofDownsample);
		int sourceHeightDofSize = (int)(source.height / (int)dofDownsample);
		
		//Cheap RT for AO
		RenderTexture aoTex = RenderTexture.GetTemporary (aortW2, aortH2, 0, aoTextureFormat, RenderTextureReadWrite.Linear);
		//End AO Tex stuff

		RenderTexture tempDofDest = RenderTexture.GetTemporary(sourceWidthDofSize, sourceHeightDofSize, 0, rtFormat);
		RenderTexture raysTex = RenderTexture.GetTemporary(raysTexWidth, raysTexHeight, 0, rtFormat);
		//Do DOF after full pass
		RenderTexture fullSizeMedian = RenderTexture.GetTemporary (sourceWidthDofSize, sourceHeightDofSize, 0, rtFormat);	
		RenderTexture tempDest = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default);// rtFormat);			
		RenderTexture dofTex = RenderTexture.GetTemporary (source.width, source.height, 0, rtFormat);

		RenderTexture blur1Dof = null;
		//RenderTexture blur2Dof = null;
		
		bloomDownsample = (int)bloomDownsampleEnum;
		bloomDownsample = Mathf.Max(bloomDownsample, 2);
		int rtW2= (int)source.width/ bloomDownsample;
		int rtH2= (int)source.height/ bloomDownsample;

		//Flares
		RenderTexture featurePass = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
		RenderTexture flareBlurOne = RenderTexture.GetTemporary (featurePass.width, featurePass.height, 0, featurePass.format);

		if(useAmbientObscurance)
		{
			SetAOShaderValues(m_AOMaterial);
			//Most performant way. Yes, it's ugly, but it's actually SO much easier to manage
			goto AmbientOcclusionArea;
		}
		
	MainPrismArea:

		if(useDof)
		{
			SetDofValues(m_DoFMaterial);

			blur1Dof = RenderTexture.GetTemporary (sourceWidthDofSize, sourceHeightDofSize, 0, rtFormat);
			RenderTexture dofBack = null;
			RenderTexture dofFront = null;

			if(useMedianDoF)
			{
				//Debug.LogError("TODO - DISABLE MEDIAN DOF");
				Graphics.Blit(source, fullSizeMedian, m_Material2, 6);//, m_Material2, 6);
			} else {
				//Graphics.Blit(source, fullSizeMedian);
			}

			dofBack = RenderTexture.GetTemporary (sourceWidthDofSize, sourceHeightDofSize, 0, rtFormat);

			//No MRT COC
			if(useMedianDoF)
			{
				Graphics.Blit(fullSizeMedian, dofBack, m_DoFMaterial, 1);
			} else {
				Graphics.Blit(source, dofBack, m_DoFMaterial, 1);
			}

			m_DoFMaterial.SetTexture("_DoF2", dofBack);
			m_Material.SetTexture("_DoF2", dofBack);

			Graphics.Blit(null as RenderTexture, blur1Dof, m_DoFMaterial, 3);

			m_Material.SetTexture("_DoF2", blur1Dof);	

			
			//Finally combine
			if(debugDofPass)
			{
				if(dofDebugType == DebugDofPass.FrontCoC)
				{
					Graphics.Blit(dofBack, destination, m_DoFMaterial, 2);
				} else if(dofDebugType == DebugDofPass.BackCoC) {
					Graphics.Blit(dofBack, destination, m_DoFMaterial, 2);
				} else if(dofDebugType == DebugDofPass.Back) {
					Graphics.Blit(blur1Dof, destination);
				} else if(dofDebugType == DebugDofPass.Front) {
					Graphics.Blit(blur1Dof, destination);
				} else if(dofDebugType == DebugDofPass.Depth) {
					m_DoFMaterial.SetFloat("_DofFactor", -100.0f);
					Graphics.Blit(dofBack, destination, m_DoFMaterial, 2);
				}

				RenderTexture.ReleaseTemporary(dofBack);
				RenderTexture.ReleaseTemporary(dofFront);
				goto ReleaseRenderTextureArea;

			} else {

				//No need to combine here
				/*if(useMedianDoF)
				{
					Graphics.Blit(source, m_DoFMaterial, 5);
					//Graphics.Blit(fullSizeMedian, source, m_DoFMaterial, 5);
				} else {
					Graphics.Blit(source, source, m_DoFMaterial, 5);
				}*/
			}

			RenderTexture.ReleaseTemporary(dofBack);
			RenderTexture.ReleaseTemporary(dofFront);
		}
		
		if(useExposure)
		{
			HandleExposure(source, rtFormat);

			if(debugViewExposure)
			{
				Graphics.Blit(source, destination, m_Material3, 7);
				goto ReleaseRenderTextureArea;
			}
		}

		#region GODRAYS
		if(useRays)
		{		
			// downsample
			Graphics.Blit (source, raysTex, m_Material2, 2);
			
			RenderTexture preBlurtex = RenderTexture.GetTemporary (raysTex.width, raysTex.height, 0, RenderTextureFormat.Default);
			RenderTexture quaterResSecond = RenderTexture.GetTemporary (raysTex.width, raysTex.height, 0, RenderTextureFormat.Default);
			
			//Mask
			Graphics.Blit(raysTex, preBlurtex, m_Material3, 0);
			
			Graphics.Blit(preBlurtex, quaterResSecond, m_Material3, 1);
			
			m_Material.SetTexture("_RaysTexture", quaterResSecond);
			
			if(raysShowDebug)
			{
				Graphics.Blit(preBlurtex, destination);
				RenderTexture.ReleaseTemporary(preBlurtex);
				RenderTexture.ReleaseTemporary(quaterResSecond);
				goto ReleaseRenderTextureArea;
			}
			
			RenderTexture.ReleaseTemporary(preBlurtex);
			RenderTexture.ReleaseTemporary(quaterResSecond);
			
			goto BloomMainArea;

		}
		#endregion

		#region BLOOM
	BloomMainArea:
		//BLOOM============================================================================================================
		if(useBloom) {		

			// downsample
			RenderTexture halfRezColorDown = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
			halfRezColorDown.filterMode = FilterMode.Bilinear;

			if(bloomType == BloomType.Simple)
			{
				if(useBloomStability)
				{
					Graphics.Blit (source, halfRezColorDown, m_Material2, 11);
				} else {

					if(useDifferentMobileBloom)
					{
						Graphics.Blit (source, halfRezColorDown, m_Material2, 5);
					} else {
						Graphics.Blit (source, halfRezColorDown, m_Material2, 10);
					}
				}


				if(useBloomFlares)
				{
					//Flip and features
					Graphics.Blit(halfRezColorDown, featurePass, m_FlaresMaterial, 0);

					//Blur to make ghost artifacts less ghosty
					//featurePass = BlurTex(featurePass, 1);
					Graphics.Blit (featurePass, flareBlurOne, m_Material2, 14);
					featurePass.DiscardContents();
					Graphics.Blit (flareBlurOne, featurePass, m_Material2, 13);
					flareBlurOne.DiscardContents();
					Graphics.Blit (featurePass, flareBlurOne, m_Material2, 0);
					//RenderTexture.ReleaseTemporary(flareBlurOne);

					//Add
					m_Material.SetTexture("_FinalFlareTex", flareBlurOne);

					/*if(debugBloomTex)
					{
						Graphics.Blit(flareBlurOne, destination);
						goto ReleaseRenderTextureArea;
					}*/
				}

				halfRezColorDown = BlurTex(halfRezColorDown, bloomBlurPasses);

				m_Material.SetTexture("_Bloom1", halfRezColorDown);
				
				if(debugBloomTex)
				{
					m_Material3.SetTexture("_Bloom1", halfRezColorDown);
					m_Material3.SetFloat("_BloomThreshold", bloomThreshold);
					m_Material3.SetFloat("_BloomIntensity", bloomIntensity);
					Graphics.Blit(halfRezColorDown, destination, m_Material3, 4);
					
					goto ReleaseRenderTextureArea;
				}
				
				if(accumulationBuffer && useBloomStability)
				{
					m_Material.SetTexture("_BloomAcc", accumulationBuffer);
				}
				
				if(accumulationBuffer)
				{
					RenderTexture.ReleaseTemporary(accumulationBuffer);
					accumulationBuffer = null;
				}			
				if(useBloomStability)
				{
					accumulationBuffer = RenderTexture.GetTemporary(rtW2, rtH2, 0, rtFormat);
					accumulationBuffer.filterMode = FilterMode.Bilinear;

					Graphics.Blit(halfRezColorDown, accumulationBuffer);
                }

			} else if(bloomType == BloomType.HDR) {
				
				// get extra downsampled textures
				RenderTexture quarterRezColorDown = RenderTexture.GetTemporary  (rtW2/2, rtH2/2, 0, rtFormat);
				quarterRezColorDown.filterMode = FilterMode.Bilinear;
				RenderTexture eighthRezColorDown = RenderTexture.GetTemporary  (rtW2/4, rtH2/4, 0, rtFormat);
				eighthRezColorDown.filterMode = FilterMode.Bilinear;
				RenderTexture sixteenthRezColorDown = RenderTexture.GetTemporary  (rtW2/8, rtH2/8, 0, rtFormat);
				sixteenthRezColorDown.filterMode = FilterMode.Bilinear;

				//Median w/exposure and thresh
				if(useBloomStability)
				{
					Graphics.Blit (source, halfRezColorDown, m_Material2, 11);
				} else {
					Graphics.Blit (source, halfRezColorDown, m_Material2, 10);
				}

				if(useBloomFlares)
				{
					//Flip and features
					Graphics.Blit(halfRezColorDown, featurePass, m_FlaresMaterial, 0);

					//Blur to make ghost artifacts less ghosty
					//featurePass = BlurTex(featurePass, 1);
					Graphics.Blit (featurePass, flareBlurOne, m_Material2, 14);
					featurePass.DiscardContents();
					Graphics.Blit (flareBlurOne, featurePass, m_Material2, 13);
					flareBlurOne.DiscardContents();
					Graphics.Blit (featurePass, flareBlurOne, m_Material2, 0);
					//RenderTexture.ReleaseTemporary(flareBlurOne);

					//Add
					m_Material.SetTexture("_FinalFlareTex", flareBlurOne);

					/*if(debugBloomTex)
					{
						Graphics.Blit(flareBlurOne, destination);
						goto ReleaseRenderTextureArea;
					}*/
				}

				//Down
				Graphics.Blit (halfRezColorDown, quarterRezColorDown, m_Material2, 8);//, m_Material2, 2);
				Graphics.Blit (quarterRezColorDown, eighthRezColorDown, m_Material2, 8);//, m_Material2, 2);
				Graphics.Blit (eighthRezColorDown, sixteenthRezColorDown, m_Material2, 8);

				//Blur
				halfRezColorDown = BlurTex(halfRezColorDown, bloomBlurPasses);
				quarterRezColorDown = BlurTex(quarterRezColorDown, bloomBlurPasses);
				eighthRezColorDown = BlurTex(eighthRezColorDown, bloomBlurPasses);
				sixteenthRezColorDown = BlurTex(sixteenthRezColorDown, bloomBlurPasses);

				//Set textures
				m_Material.SetTexture("_Bloom1", halfRezColorDown);
				m_Material.SetTexture("_Bloom2", quarterRezColorDown);
				m_Material.SetTexture("_Bloom3", eighthRezColorDown);
				m_Material.SetTexture("_Bloom4", sixteenthRezColorDown);

				if(accumulationBuffer && useBloomStability)
				{
					m_Material.SetTexture("_BloomAcc", accumulationBuffer);
				}
				
				if(accumulationBuffer)
				{
					RenderTexture.ReleaseTemporary(accumulationBuffer);
					accumulationBuffer = null;
				}			

				//Todo
				if(useBloomStability)
				{
					accumulationBuffer = RenderTexture.GetTemporary(rtW2, rtH2, 0, rtFormat);
					accumulationBuffer.filterMode = FilterMode.Bilinear;
					
					Graphics.Blit(sixteenthRezColorDown, accumulationBuffer);
				}

				//If we're using UI blur, grab it
				if(useUIBlur == true && uiBlurTexture && uiBlurTexture.width > 0 && uiBlurTexture.height > 0)
				{
					Graphics.Blit(halfRezColorDown, uiBlurTexture);
					Shader.SetGlobalTexture("_BlurredScreenTex", uiBlurTexture);
				}

				if(debugBloomTex)
				{
					m_Material3.EnableKeyword("PRISM_HDR_BLOOM");
					m_Material3.EnableKeyword("PRISM_USE_BLOOM");
					m_Material3.SetTexture("_Bloom1", halfRezColorDown);
					m_Material3.SetTexture("_Bloom2", quarterRezColorDown);
					m_Material3.SetTexture("_Bloom3", eighthRezColorDown);
					m_Material3.SetTexture("_Bloom4", sixteenthRezColorDown);
					m_Material3.SetFloat("_BloomIntensity", bloomIntensity);
					Graphics.Blit(halfRezColorDown, destination, m_Material3, 4);
					goto ReleaseRenderTextureArea;
				}
			}
			//END BLOOM========================================================================================================	
			#endregion
		} 

		//Finally sharpen
		if(useSharpen)
		{
			//RenderTexture chromDest = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default);

			//Main pass
			Graphics.Blit(source, tempDest, m_Material, 0);
			//Sharpen
			Graphics.Blit(tempDest, destination, m_Material2, 0);

			//RenderTexture.ReleaseTemporary(chromDest);

		} else if(forceSecondChromaticPass && useChromaticBlur){

			//Main pass
			Graphics.Blit(source, tempDest, m_Material, 0);
			//Chrom
			Graphics.Blit(tempDest, destination, m_Material2, 12);

		} else {
			Graphics.Blit(source, destination, m_Material, 0);
		}
		
		goto ReleaseRenderTextureArea;
		
		//THIS IS USED DUE TO UNITYS COMPLETE INABILITY TO PROPERLY SUPPORT COMBINED POSTFX.
		//YES, IT IS UGLY. NO, I DON'T LIKE IT. THE ALTERNATE IS HAVING A 20MB RT IN RAM ALL THE TIME!
		//For Kino-style-but-not-really AO
	AmbientOcclusionArea:
			
			//AO
			Graphics.Blit(null, aoTex, m_AOMaterial, (int)occlusionSource);
		
		RenderTexture tmpRt3;

		if(aoBlurType == AOBlurType.Fast)
		{
			for (int i = 0; i < aoBlurIterations; i++) {
				m_AOMaterial.SetVector("_AOBlurVector", new Vector4(-1f, 0f, 0f, 0f));
				tmpRt3 = RenderTexture.GetTemporary (aortW2, aortH2, 0, aoTextureFormat, RenderTextureReadWrite.Linear);
				Graphics.Blit (aoTex, tmpRt3, m_AOMaterial, (int)aoBlurType);
				RenderTexture.ReleaseTemporary (aoTex);
				
				m_AOMaterial.SetVector("_AOBlurVector", new Vector4(0f, 1f, 0f, 0f));
				aoTex = RenderTexture.GetTemporary (aortW2, aortH2, 0, aoTextureFormat, RenderTextureReadWrite.Linear);
				Graphics.Blit (tmpRt3, aoTex, m_AOMaterial, (int)aoBlurType);
				RenderTexture.ReleaseTemporary (tmpRt3);
			}
		} else {
			//NOTE: SECONDARY BLUR HAS TO BE AT N+1 PASS NUMBER IN THE AO SHADER
			for (int i = 0; i < aoBlurIterations; i++) {
				for(int n = 0; n < 2; n++)
				{
					m_AOMaterial.SetVector("_AOBlurVector", new Vector4(-1f, 0f, 0f, 0f));
					tmpRt3 = RenderTexture.GetTemporary (aortW2, aortH2, 0, aoTextureFormat, RenderTextureReadWrite.Linear);
					Graphics.Blit (aoTex, tmpRt3, m_AOMaterial, (int)aoBlurType+n);
					RenderTexture.ReleaseTemporary (aoTex);
					
					m_AOMaterial.SetVector("_AOBlurVector", new Vector4(0f, 1f, 0f, 0f));
					aoTex = RenderTexture.GetTemporary (aortW2, aortH2, 0, aoTextureFormat, RenderTextureReadWrite.Linear);
					Graphics.Blit (tmpRt3, aoTex, m_AOMaterial, (int)aoBlurType+n);
					RenderTexture.ReleaseTemporary (tmpRt3);
				}
			}
		}

		if(aoShowDebug)
		{
			Graphics.Blit(aoTex, destination, m_AOMaterial, 4);
			goto ReleaseRenderTextureArea;
		}
		
		m_Material.SetTexture("_AOTex", aoTex);

		goto MainPrismArea;
		
		//Again... Yep.
	ReleaseRenderTextureArea:
		RenderTexture.ReleaseTemporary(raysTex);
		RenderTexture.ReleaseTemporary(tempDest);
		RenderTexture.ReleaseTemporary(tempDofDest);
		RenderTexture.ReleaseTemporary(fullSizeMedian);
		RenderTexture.ReleaseTemporary(dofTex);
		RenderTexture.ReleaseTemporary(aoTex);
		RenderTexture.ReleaseTemporary(blur1Dof);
		RenderTexture.ReleaseTemporary(featurePass);
		RenderTexture.ReleaseTemporary(flareBlurOne);
		firstRun = false;
		return;
		
	}
	
	//Editor mainly	
	/// <summary>
	/// Helper function for the Lookup textures creation
	/// </summary>
	public void SetIdentityLut (bool secondary = false) {
		int dim = 16;
		Color[] newC = new Color[dim*dim*dim];
		float oneOverDim = 1.0f / (1.0f * dim - 1.0f);
		
		for(int i = 0; i < dim; i++) {
			for(int j = 0; j < dim; j++) {
				for(int k = 0; k < dim; k++) {
					newC[i + (j*dim) + (k*dim*dim)] = new Color((i*1.0f)*oneOverDim, (j*1.0f)*oneOverDim, (k*1.0f)*oneOverDim, 1.0f);
				}
			}
		}
		
		if(!secondary)
		{
			if (threeDLookupTex)
				DestroyImmediate (threeDLookupTex);
			
			threeDLookupTex = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
			threeDLookupTex.SetPixels (newC);
			threeDLookupTex.Apply ();
			basedOnTempTex = "";
		} else {
			if (secondaryThreeDLookupTex)
				DestroyImmediate (secondaryThreeDLookupTex);
			
			secondaryThreeDLookupTex = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
			secondaryThreeDLookupTex.SetPixels (newC);
			secondaryThreeDLookupTex.Apply ();
			secondaryBasedOnTempTex = "";
		}
	}
	
	/// <summary>
	/// Helper function for the Lookup textures creation
	/// </summary>
	public bool ValidDimensions ( Texture2D tex2d) {
		
		if (!tex2d) 
			return false;
		
		if (tex2d.height != Mathf.FloorToInt(Mathf.Sqrt(tex2d.width)))
			return false;
		
		return true;
	}
	
	/// <summary>
	/// Helper function for the Lookup textures creation
	/// </summary>
	public void Convert ( Texture2D temp2DTex, bool secondaryLut = false) {
		
		// conversion fun: the given 2D texture needs to be of the format
		//  w * h, wheras h is the 'depth' (or 3d dimension 'dim') and w = dim * dim
		
		if (temp2DTex) {
			int dim = temp2DTex.width * temp2DTex.height;
			dim = temp2DTex.height;
			
			if (!ValidDimensions(temp2DTex)) {
				Debug.LogWarning ("The given 2D texture " + temp2DTex.name + " cannot be used as a 3D LUT.");
				if(!secondaryLut)
				{
					secondaryBasedOnTempTex = "";
				} else basedOnTempTex = "";				
				return;
			}
			
			Color[] c = temp2DTex.GetPixels();
			Color[] newC = new Color[c.Length];
			
			for(int i = 0; i < dim; i++) {
				for(int j = 0; j < dim; j++) {
					for(int k = 0; k < dim; k++) {
						int j_ = dim-j-1;
						newC[i + (j*dim) + (k*dim*dim)] = c[k*dim+i+j_*dim*dim];
					}
				}
			}
			
			if(!secondaryLut)
			{
				if (threeDLookupTex)
					DestroyImmediate (threeDLookupTex);
				threeDLookupTex = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
				threeDLookupTex.SetPixels (newC);
				threeDLookupTex.Apply ();
				basedOnTempTex = temp2DTex.name;
				twoDLookupTex = temp2DTex;
			} else {
				if (secondaryThreeDLookupTex)
					DestroyImmediate (secondaryThreeDLookupTex);
				secondaryThreeDLookupTex = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
				secondaryThreeDLookupTex.SetPixels (newC);
				secondaryThreeDLookupTex.Apply ();
				secondaryBasedOnTempTex = temp2DTex.name;
				secondaryTwoDLookupTex = temp2DTex;
			}
			
		}
		else {
			// error, something went terribly wrong
			Debug.LogError ("Couldn't color correct with 3D LUT texture. PRISM will be disabled.");
			enabled = false;
		}
	}	
	
}
