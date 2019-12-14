using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
using UnityEngine.Rendering.PostProcessing;
#endif
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && SSAA_HDRP
using UnityEngine.Experimental.Rendering.HDPipeline;
#endif
namespace MadGoat_SSAA {
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class MadGoatSSAA_InternalRenderer : MonoBehaviour {

        #region fields and properties
        // Render Multiplier used by main
        private float multiplier;
        public float Multiplier {
            get {
                return multiplier;
            }

            set {
                multiplier = value;
            }
        }

        // Shader Parameters
        private float sharpness;
        public float Sharpness {
            get {
                return sharpness;
            }

            set {
                sharpness = value;
            }
        }
        private bool useShader;
        public bool UseShader {
            get {
                return useShader;
            }

            set {
                useShader = value;
            }
        }
        private float sampleDistance;
        public float SampleDistance {
            get {
                return sampleDistance;
            }

            set {
                sampleDistance = value;
            }
        }
        private bool flipImage;
        public bool FlipImage {
            get {
                return flipImage;
            }
            set {
                flipImage = value;
            }
        }

        // Cameras
        private Camera main;
        public Camera Main {
            get {
                return main;
            }

            set {
                main = value;
            }
        }
        private Camera current;
        public Camera Current {
            get {
                return current;
            }

            set {
                current = value;
            }
        }

        // Shader Setup
        [SerializeField]
        private Shader shaderBilinear;
        public Shader ShaderBilinear {
            get {
                if (shaderBilinear == null)
                    shaderBilinear = Shader.Find("Hidden/SSAA_Bilinear");

                return shaderBilinear;
            }
        }
        [SerializeField]
        private Shader shaderBicubic;
        public Shader ShaderBicubic {
            get {
                if (shaderBicubic == null)
                    shaderBicubic = Shader.Find("Hidden/SSAA_Bicubic");

                return shaderBicubic;
            }
        }
        [SerializeField]
        private Shader shaderNeighbor;
        public Shader ShaderNeighbor {
            get {
                if (shaderNeighbor == null) {
                    shaderNeighbor = Shader.Find("Hidden/SSAA_Nearest");
                }
                return shaderNeighbor;
            }
        }
        [SerializeField]
        private Shader shaderDefault;
        public Shader ShaderDefault {
            get {
                if (shaderDefault == null) {
                    shaderDefault = Shader.Find("Hidden/SSAA_Def");
                }
                return shaderDefault;
            }
        }

        // Materials Instances
        private Material materialBilinear; // Bilinear Material
        public Material MaterialBilinear {
            get {
                if (materialBicubic == null)
                    materialBicubic = new Material(ShaderBicubic);
                return materialBicubic;
            }
        }
        private Material materialBicubic; // Bicubic
        public Material MaterialBicubic {
            get {
                if (materialBilinear == null)
                    materialBilinear = new Material(ShaderBilinear);
                return materialBilinear;
            }
        }
        private Material materialNearest; // Nearest Neighbor
        public Material MaterialNearest {
            get {
                if (materialNearest == null)
                    materialNearest = new Material(ShaderNeighbor);
                return materialNearest;
            }
        }
        private Material materialDefault; // Default
        public Material MaterialDefault {
            get {
                if (materialDefault == null)
                    materialDefault = new Material(ShaderDefault);
                return materialDefault;
            }
        }

        private Material materialCurrent;
        public Material MaterialCurrent {
            get {
                return materialCurrent;
            }

            set {
                materialCurrent = value;
            }
        }
        private Material materialOld = null;
        public Material MaterialOld {
            get {
                return materialOld;
            }

            set {
                materialOld = value;
            }
        }

        // Command buffer
        private CommandBuffer compositionCommand;
        public CommandBuffer CompositionCommand {
            get {
                if (compositionCommand == null)
                    compositionCommand = new CommandBuffer();
                return compositionCommand;
            }

            set {
                compositionCommand = value;
            }
        }

        // Main SSAA instance
        private MadGoatSSAA mainComponent;
        public MadGoatSSAA MainComponent {
            get {
                return mainComponent;
            }

            set {
                mainComponent = value;
            }
        }
        #endregion

        #region methods
        // Command buffer setup
        private void SetupCommand(CommandBuffer cb, CameraEvent evt) {
            // setup command
            cb.Clear();
            cb.name = "SSAA_COMPOSITION";

            // setup pass and blit
            RenderTargetIdentifier idBuff = new RenderTargetIdentifier(Main.targetTexture);
            cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cb.Blit(idBuff, BuiltinRenderTextureType.CameraTarget, MaterialCurrent, 0);

            // add to camera - todo hdrp
            var commands = new List<CommandBuffer>(Current.GetCommandBuffers(evt));
            if (commands.Find(x => x.name == "SSAA_COMPOSITION") == null) {
                if (!MadGoatSSAA_Utils.DetectSRP()) {
                    Current.AddCommandBuffer(evt, cb);
                }
                else {
                    // --- Prep for v2 native HDRP support
                    // Injection into render context goes here
                }
            }
        }
        private void ClearCommand(CommandBuffer cb, CameraEvent evt) {
            // clear command
            cb.Clear();

            // remove from camera
            var commands = new List<CommandBuffer>(Current.GetCommandBuffers(evt));
            if (commands.Find(x => x.name == "SSAA_COMPOSITION") != null) {
                if (!MadGoatSSAA_Utils.DetectSRP()) {
                    Current.RemoveCommandBuffer(evt, cb);
                }
                else {
                    // --- Prep for v2 native HDRP support
                    // Injection into render context goes here
                }
            }
        }
        private void UpdateCommand() {
            MaterialCurrent.SetFloat("_ResizeWidth", Screen.width);
            MaterialCurrent.SetFloat("_ResizeHeight", Screen.height);
            MaterialCurrent.SetFloat("_Sharpness", Sharpness);
            MaterialCurrent.SetFloat("_SampleDistance", SampleDistance);

            // --- Fix for deferred stacking
            // Layers with skyboxes or solid color clearflags should alwasy render opaque to avoid
            // alpha testing issues on deferred
            if (Current.clearFlags == CameraClearFlags.Color || Current.clearFlags == CameraClearFlags.Skybox) {
                MaterialCurrent.SetOverrideTag("RenderType", "Opaque");
                MaterialCurrent.SetInt("_SrcBlend", (int)BlendMode.One);
                MaterialCurrent.SetInt("_DstBlend", (int)BlendMode.Zero);
                MaterialCurrent.SetInt("_ZWrite", 1);
                MaterialCurrent.renderQueue = -1;
            }
            else {
                MaterialCurrent.SetOverrideTag("RenderType", "Transparent");
                MaterialCurrent.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                MaterialCurrent.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                MaterialCurrent.SetInt("_ZWrite", 0);
                MaterialCurrent.renderQueue = 3000;
            }
        }

        // Events from main behaviour
        public void OnMainEnable() {
            mainComponent = Main.GetComponent<MadGoatSSAA>();
            MaterialCurrent = materialDefault;

            if (MadGoatSSAA_Utils.DetectSRP())
                SetupCBSRP();
            else
                SetupCommand(CompositionCommand, CameraEvent.AfterImageEffects);
        }
        public void OnMainDisable() {
            if (!MadGoatSSAA_Utils.DetectSRP()) {
                ClearCommand(CompositionCommand, CameraEvent.AfterImageEffects);
            }
        }

        // Events from main renderer
        public void OnMainRender() {
            // Set up camera for hdrp
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && SSAA_HDRP
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                HDAdditionalCameraData data = GetComponent<HDAdditionalCameraData>();
                HDAdditionalCameraData dataMain = Main.GetComponent<HDAdditionalCameraData>();
                if (data == null)
                    data = Current.gameObject.AddComponent<HDAdditionalCameraData>();
                data.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                data.clearDepth = true;
                data.volumeLayerMask = 0;

            }
#endif
            // set up command buffers
            if (MadGoatSSAA_Utils.DetectSRP())
                // for SRP
                UpdateCBSRP();
            else
                // for legacy
                UpdateCommand();
        }
        public void OnMainRenderEnded() {
            // listen for screenshot
            HandleScreenshot();
        }
        public void OnMainFilterChanged(Filter Type) {
            MaterialOld = MaterialCurrent;
            PostVolumePassOld = PostVolumePass;

            // Point material_current to the given material
            switch (Type) {
                case Filter.NEAREST_NEIGHBOR:
                    MaterialCurrent = MaterialNearest;
                    PostVolumePass = 1; // for srp - deprecated
                    break;
                case Filter.BILINEAR:
                    MaterialCurrent = MaterialBilinear;
                    PostVolumePass = 2; // for srp - deprecated
                    break;
                case Filter.BICUBIC:
                    MaterialCurrent = MaterialBicubic;
                    PostVolumePass = 3; // for srp - deprecated
                    break;
            }

            // Hanle the correct pass
            if ((!useShader || multiplier == 1) && MaterialCurrent != MaterialDefault) {
                MaterialCurrent = MaterialDefault;
                PostVolumePass = 0;
            }

            // if material must be changed we have to reset the command buffer
            if (MaterialCurrent != MaterialOld) {
                MaterialOld = MaterialCurrent;
                PostVolumePassOld = PostVolumePass;

                ClearCommand(CompositionCommand, CameraEvent.AfterImageEffects);
                SetupCommand(CompositionCommand, CameraEvent.AfterImageEffects);
            }
        }

        // screenshot function - will be replaced with better implementation in v2
        private void HandleScreenshot() {
            if (MainComponent.screenshotSettings.takeScreenshot) {
                // Default material for screenshots is bicubic (we don't care about performance here, so we use whats best)
                Material material = new Material(ShaderBicubic);

                // buffer to store texture
                RenderTexture buff = new RenderTexture((int)MainComponent.screenshotSettings.outputResolution.x, (int)MainComponent.screenshotSettings.outputResolution.y, 24, RenderTextureFormat.ARGB32);
                bool sRGBWrite = GL.sRGBWrite;
                // enable srgb conversion for blit - fixes the color issue
                GL.sRGBWrite = true;

                // setup shader
                if (MainComponent.screenshotSettings.useFilter) {
                    material.SetFloat("_ResizeWidth", (int)MainComponent.screenshotSettings.outputResolution.x);
                    material.SetFloat("_ResizeHeight", (int)MainComponent.screenshotSettings.outputResolution.y);
                    material.SetFloat("_Sharpness", 0.85f);
                    Graphics.Blit(Main.targetTexture, buff, material, 0);
                }
                else // or blit as it is
                {
                    Graphics.Blit(Main.targetTexture, buff);
                }
                DestroyImmediate(material);
                RenderTexture.active = buff;

                // Copy from active texture to buffer
                Texture2D screenshotBuffer = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
                screenshotBuffer.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);

                // Create path if not available and write the screenshot to disk
                (new FileInfo(MainComponent.screenshotPath)).Directory.Create();

                if (MainComponent.imageFormat == ImageFormat.PNG)
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".png", screenshotBuffer.EncodeToPNG());
                else if (MainComponent.imageFormat == ImageFormat.JPG)
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".jpg", screenshotBuffer.EncodeToJPG(MainComponent.JPGQuality));
#if UNITY_5_6_OR_NEWER
                else
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".exr", screenshotBuffer.EncodeToEXR(MainComponent.EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif

                // Clean stuff
                RenderTexture.active = null;
                buff.Release();

                // restore the sRGBWrite to older state so it doesn't interfere with user's setting
                GL.sRGBWrite = sRGBWrite;

                DestroyImmediate(screenshotBuffer);
                MainComponent.screenshotSettings.takeScreenshot = false;
            }
        }
        public string GetScreenshotName // generate a string for the filename of the screenshot
        {
            get {
                return (MainComponent.useProductName ? Application.productName : MainComponent.namePrefix) + "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmssff") + "_" +
                    MainComponent.screenshotSettings.outputResolution.y.ToString() + "p";
            }
        }
        #endregion

        // To be deprecated in v2
        // - - - - - - - - - - - - - - - - - - - - - - - -
        #region deprecated
        private int postVolumePass = 0;
        public int PostVolumePass {
            get {
                return postVolumePass;
            }

            set {
                postVolumePass = value;
            }
        }

        private int postVolumePassOld = 0;
        public int PostVolumePassOld {
            get {
                return postVolumePassOld;
            }

            set {
                postVolumePassOld = value;
            }
        }

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
        private PostProcessVolume postVolume;
        public PostProcessVolume PostVolume
        {
            get
            {
                return postVolume;
            }

            set
            {
                postVolume = value;
            }
        }
     
#endif

        private void SetupCBSRP() {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
            // setup post processing on internal camera

            // get the first empty render layer
#if UNITY_EDITOR
            MadGoatSSAA_Utils.GrabRenderLayer();
#endif
            gameObject.layer = LayerMask.NameToLayer("SSAA_RENDER");

            PostProcessLayer pl;
            SphereCollider trigger;

            if ((pl = GetComponent<PostProcessLayer>()) == null)
            {
                pl = gameObject.AddComponent<PostProcessLayer>();
                pl.volumeLayer = 1 << LayerMask.NameToLayer("SSAA_RENDER");
                pl.volumeTrigger = transform;
            }
            if ((PostVolume = GetComponent<PostProcessVolume>()) == null)
            {
                PostVolume = gameObject.AddComponent<PostProcessVolume>();
                PostVolume.isGlobal = false;
            }

            if (!PostVolume.sharedProfile)
                PostVolume.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            if (PostVolume.sharedProfile.settings.Count == 0)
            {
                PostVolume.sharedProfile.AddSettings<SsaaSamplingUber>();
            }

            if ((trigger = gameObject.GetComponent<SphereCollider>()) == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.0001f;
            }
            // determine if flipping is required? HDRP

#endif
        }
        private void UpdateCBSRP() {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)

            // in case of user error
            if(gameObject.layer != LayerMask.NameToLayer("SSAA_RENDER"))
                gameObject.layer = LayerMask.NameToLayer("SSAA_RENDER");
            
            // setup shader pass
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).shaderPass.value = PostVolumePass;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sourceTex.value = Main.targetTexture;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).useFXAA.value = 
                MainComponent.ssaaUltra 
                && MainComponent.multiplier > 1 
                && MainComponent.renderMode != Mode.AdaptiveResolution;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).intensityFXAA.value = MainComponent.fssaaIntensity;

            // setup shader specs
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).resizeWidth.value = Screen.width;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).resizeHeight.value = Screen.height;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sharpness.value = Sharpness;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sampleDistance.value = SampleDistance;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).flip.value = MainComponent.flipImageFix && GraphicsSettings.renderPipelineAsset.ToString().Contains("(UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipelineAsset)");
#endif
        }
        #endregion
    }
}