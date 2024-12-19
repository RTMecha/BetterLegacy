using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using SCPE;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Configs;

namespace BetterLegacy.Menus
{
    public class MenuEffectsManager : MonoBehaviour
    {
        public static MenuEffectsManager inst;

        public static bool ShowEffects => MenuConfig.Instance.ShowFX.Value;

        void Awake()
        {
            if (!inst)
                inst = this;
            else if (inst != this)
            {
                Destroy(inst);
                inst = this;
            }
            prevShowEffects = ShowEffects;
        }

		void Start()
		{
            // Create a new camera since putting the glitch effects on the Main Camera make the objects disappear.
            var glitchCamera = Creator.NewGameObject("Glitch Camera", transform);
            glitchCamera.transform.localPosition = Vector3.zero;

            glitchCam = glitchCamera.AddComponent<Camera>();
            glitchCam.allowMSAA = false;
            glitchCam.clearFlags = CameraClearFlags.Depth;
            glitchCam.cullingMask = 310;
            glitchCam.depth = 2f;
            glitchCam.farClipPlane = 10000f;
            glitchCam.forceIntoRenderTexture = true;
            glitchCam.nearClipPlane = 9999.9f;
            glitchCam.orthographic = true;
            glitchCam.rect = new Rect(0.001f, 0.001f, 0.999f, 0.999f);

            digitalGlitch = glitchCamera.AddComponent<DigitalGlitch>();
            digitalGlitch._shader = LegacyPlugin.digitalGlitchShader;

            analogGlitch = glitchCamera.AddComponent<AnalogGlitch>();
            analogGlitch._shader = LegacyPlugin.analogGlitchShader;

            analogGlitch = glitchCamera.GetComponent<AnalogGlitch>() ?? glitchCamera.AddComponent<AnalogGlitch>();
			analogGlitch._shader = LegacyPlugin.analogGlitchShader;
			digitalGlitch = glitchCamera.GetComponent<DigitalGlitch>() ?? glitchCamera.AddComponent<DigitalGlitch>();
			digitalGlitch._shader = LegacyPlugin.digitalGlitchShader;
            analogGlitch.enabled = false; // disabled by default due to there still being a slight effect when it is enabled.

            RegisterFunction(UpdateAnalogGlitchEnabled);
            RegisterFunction(UpdateAnalogGlitchScanLineJitter);
            RegisterFunction(UpdateAnalogGlitchVerticalJump);
            RegisterFunction(UpdateAnalogGlitchHorizontalShake);
            RegisterFunction(UpdateAnalogGlitchColorDrift);
            RegisterFunction(UpdateDigitalGlitch);

            var camera = Camera.main;

            try
            {
                if (!postProcessResourcesAssetBundle)
                    postProcessResourcesAssetBundle = CoreHelper.LoadAssetBundle("effectresources.asset");
                var postProcessLayer = camera.gameObject.GetComponent<PostProcessLayer>() ?? camera.gameObject.AddComponent<PostProcessLayer>();
                postProcessResources = postProcessResourcesAssetBundle.LoadAsset<PostProcessResources>("postprocessresources.asset");
                HarmonyLib.AccessTools.Field(typeof(PostProcessLayer), "m_Resources").SetValue(postProcessLayer, postProcessResources);
                postProcessLayer.volumeLayer = 1824;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            // Chroma
			chroma = ScriptableObject.CreateInstance<ChromaticAberration>();
			chroma.enabled.Override(true);
			chroma.intensity.Override(0f);
			chroma.fastMode.Override(true);
            RegisterFunction(UpdateChroma);

            // Bloom
            bloom = ScriptableObject.CreateInstance<Bloom>();
			bloom.enabled.Override(true);
			bloom.intensity.Override(0f);
			bloom.fastMode.Override(true);
            RegisterFunction(UpdateBloomIntensity);
            RegisterFunction(UpdateBloomDiffusion);
            RegisterFunction(UpdateBloomThreshold);
            RegisterFunction(UpdateBloomAnamorphicRatio);
            RegisterFunction(UpdateBloomColor);

            // Vignette
            vignette = ScriptableObject.CreateInstance<Vignette>();
			vignette.enabled.Override(true);
			vignette.intensity.Override(0f);
            RegisterFunction(UpdateVignetteIntensity);
            RegisterFunction(UpdateVignetteSmoothness);
            RegisterFunction(UpdateVignetteRounded);
            RegisterFunction(UpdateVignetteRoundness);
            RegisterFunction(UpdateVignetteCenterX);
            RegisterFunction(UpdateVignetteCenterY);
            RegisterFunction(UpdateVignetteColor);

            // Lens Distort
            lensDistort = ScriptableObject.CreateInstance<LensDistortion>();
			lensDistort.enabled.Override(true);
			lensDistort.intensity.Override(0f);
            RegisterFunction(UpdateLensDistortIntensity);
            RegisterFunction(UpdateLensDistortCenterX);
            RegisterFunction(UpdateLensDistortCenterY);
            RegisterFunction(UpdateLensDistortIntensityX);
            RegisterFunction(UpdateLensDistortIntensityY);
            RegisterFunction(UpdateLensDistortScale);

            // Grain
            grain = ScriptableObject.CreateInstance<Grain>();
			grain.enabled.Override(true);
			grain.intensity.Override(0f);
            RegisterFunction(UpdateGrainIntensity);
            RegisterFunction(UpdateGrainScale);
            RegisterFunction(UpdateGrainColored);

            // Pixelize
            pixelize = ScriptableObject.CreateInstance<Pixelize>();
			pixelize.enabled.Override(true);
			pixelize.amount.Override(0f);
            RegisterFunction(UpdatePixelize);

            // ColorGrading
            colorGrading = ScriptableObject.CreateInstance<ColorGrading>();
            colorGrading.enabled.Override(true);
            colorGrading.hueShift.Override(0f);
            colorGrading.contrast.Override(0f);
            colorGrading.gamma.Override(Vector4.zero);
            colorGrading.saturation.Override(0f);
            colorGrading.temperature.Override(0f);
            colorGrading.tint.Override(0f);

            // Gradient
            gradient = ScriptableObject.CreateInstance<SCPE.Gradient>();
            gradient.enabled.Override(true);
            gradient.intensity.Override(0f);
            gradient.color1.Override(new Color(0f, 0.8f, 0.56f, 0.5f));
            gradient.color2.Override(new Color(0.81f, 0.37f, 1f, 0.5f));
            gradient.rotation.Override(0f);
            gradient.blendMode.Override(SCPE.Gradient.BlendMode.Linear);

            // Double Vision
            doubleVision = ScriptableObject.CreateInstance<DoubleVision>();
            doubleVision.enabled.Override(true);
            doubleVision.intensity.Override(0f);
            doubleVision.mode.Override(DoubleVision.Mode.FullScreen);

            // Radial Blur
            radialBlur = ScriptableObject.CreateInstance<RadialBlur>();
            radialBlur.enabled.Override(true);
            radialBlur.amount.Override(0f);
            radialBlur.iterations.Override(6);

            // Scan Lines
            scanlines = ScriptableObject.CreateInstance<Scanlines>();
            scanlines.enabled.Override(true);
            scanlines.intensity.Override(0f);
            scanlines.amountHorizontal.Override(10f);
            scanlines.speed.Override(1f);
            RegisterFunction(UpdateScanlinesIntensity);
            RegisterFunction(UpdateScanlinesAmount);
            RegisterFunction(UpdateScanlinesSpeed);

            // Sharpen
            sharpen = ScriptableObject.CreateInstance<Sharpen>();
            sharpen.enabled.Override(true);
            sharpen.amount.Override(0f);

            // Ripples
            ripples = ScriptableObject.CreateInstance<Ripples>();
            ripples.enabled.Override(true);
            ripples.strength.Override(0f);
            ripples.speed.Override(3f);
            ripples.distance.Override(5f);
            ripples.height.Override(1f);
            ripples.width.Override(1.5f);

            // Blur
            blur = ScriptableObject.CreateInstance<Blur>();
            blur.enabled.Override(true);
            blur.amount.Override(0f);
            blur.iterations.Override(6);

            // Color Split
            colorSplit = ScriptableObject.CreateInstance<ColorSplit>();
            colorSplit.enabled.Override(true);
            colorSplit.offset.Override(0f);
            colorSplit.mode.Override(ColorSplit.SplitMode.Single);

            // Black Bars
            blackBars = ScriptableObject.CreateInstance<BlackBars>();
            blackBars.enabled.Override(true);
            blackBars.size.Override(0f);
            blackBars.mode.Override(BlackBars.Direction.Horizontal);

            // Danger
            danger = ScriptableObject.CreateInstance<Danger>();
            danger.enabled.Override(true);
            danger.intensity.Override(0f);
            danger.color.Override(new Color(0.66f, 0f, 0f));

            // Invert
            invert = ScriptableObject.CreateInstance<Invert>();
            invert.enabled.Override(true);
            invert.amount.Override(0f);

            // Mosaic
            mosaic = ScriptableObject.CreateInstance<Mosaic>();
            mosaic.enabled.Override(true);
            mosaic.size.Override(0f);
            mosaic.mode.Override(Mosaic.MosaicMode.Hexagons);

            // Tilt Shift
            tiltShift = ScriptableObject.CreateInstance<TiltShift>();
            tiltShift.enabled.Override(true);
            tiltShift.amount.Override(0f);

            // Tube Distortion
            tubeDistortion = ScriptableObject.CreateInstance<TubeDistortion>();
            tubeDistortion.enabled.Override(true);
            tubeDistortion.amount.Override(0f);
            tubeDistortion.mode.Override(TubeDistortion.DistortionMode.Buldged);

            ResetEffects();

            ppvolume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f,
				chroma,
                bloom,
                vignette,
                lensDistort,
                grain,
                blur,
                pixelize,
                ripples,
                colorGrading,
                gradient,
                doubleVision,
                radialBlur,
                scanlines,
                sharpen,
                colorSplit,
                blackBars,
                danger,
                invert,
                mosaic,
                tiltShift,
                tubeDistortion);

			ppvolume.isGlobal = true;
		}

        bool prevShowEffects;
        void Update() => CoreHelper.UpdateValue(prevShowEffects, ShowEffects, x =>
        {
            prevShowEffects = ShowEffects;

            analogGlitch.enabled = ShowEffects && analogGlitchEnabled;
            digitalGlitch.enabled = ShowEffects;
            ppvolume.enabled = ShowEffects;
        });

        void OnDestroy()
        {
            if (ppvolume && ppvolume.gameObject)
                Destroy(ppvolume.gameObject);
        }

        public void ResetEffects()
        {
            chroma.intensity.Override(0f);
            bloom.intensity.Override(0f);
            vignette.intensity.Override(0f);
            lensDistort.intensity.Override(0f);
            grain.intensity.Override(0f);
            pixelize.amount.Override(0f);

            colorGrading.hueShift.Override(0f);
            colorGrading.contrast.Override(0f);
            colorGrading.gamma.Override(Vector4.zero);
            colorGrading.saturation.Override(0f);
            colorGrading.temperature.Override(0f);
            colorGrading.tint.Override(0f);

            gradient.intensity.Override(0f);
            gradient.color1.Override(new Color(0f, 0.8f, 0.56f, 0.5f));
            gradient.color2.Override(new Color(0.81f, 0.37f, 1f, 0.5f));
            gradient.rotation.Override(0f);
            gradient.blendMode.Override(SCPE.Gradient.BlendMode.Linear);

            doubleVision.intensity.Override(0f);
            doubleVision.mode.Override(DoubleVision.Mode.FullScreen);
            radialBlur.amount.Override(0f);
            radialBlur.iterations.Override(6);
            scanlines.intensity.Override(0f);
            scanlines.amountHorizontal.Override(10f);
            scanlines.speed.Override(1f);
            sharpen.amount.Override(0f);

            ripples.strength.Override(0f);
            ripples.speed.Override(3f);
            ripples.distance.Override(5f);
            ripples.height.Override(1f);
            ripples.width.Override(1.5f);

            blur.amount.Override(0f);
            blur.iterations.Override(6);

            colorSplit.offset.Override(0f);
            colorSplit.mode.Override(ColorSplit.SplitMode.Single);
            blackBars.size.Override(0f);
            blackBars.mode.Override(BlackBars.Direction.Horizontal);
            danger.intensity.Override(0f);
            danger.color.Override(new Color(0.66f, 0f, 0f));
            invert.amount.Override(0f);
            mosaic.size.Override(0f);
            mosaic.mode.Override(Mosaic.MosaicMode.Hexagons);
            tiltShift.amount.Override(0f);
            tubeDistortion.amount.Override(0f);
            tubeDistortion.mode.Override(TubeDistortion.DistortionMode.Buldged);
        }

        public void SetDefaultEffects()
        {
            UpdateChroma(0.1f);
        }

        #region Transform

        public void MoveCameraX(float x)
        {
            var cam = Camera.main.transform;
            var camPos = cam.localPosition;
            cam.localPosition = new Vector3(x, camPos.y, -10f);
        }
        
        public void MoveCameraY(float y)
        {
            var cam = Camera.main.transform;
            var camPos = cam.localPosition;
            cam.localPosition = new Vector3(camPos.x, y, -10f);
        }

		public void MoveCamera(Vector2 pos) => Camera.main.transform.localPosition = new Vector3(pos.x, pos.y, -10f);

		public void ZoomCamera(float zoom) => Camera.main.orthographicSize = zoom == 0 ? 5f : zoom;

		public void RotateCamera(float rotate) => Camera.main.transform.SetLocalRotationEulerZ(rotate);

        #endregion

        #region Glitch

        public void UpdateAnalogGlitchEnabled(float enabled) => UpdateAnalogGlitchEnabled(enabled == 1f);
        public void UpdateAnalogGlitchEnabled(bool enabled)
        {
            analogGlitch.enabled = enabled;
            analogGlitchEnabled = enabled;
        }
        public void UpdateAnalogGlitchScanLineJitter(float scanLineJitter) => analogGlitch.scanLineJitter = scanLineJitter;
        public void UpdateAnalogGlitchVerticalJump(float verticalJump) => analogGlitch.verticalJump = verticalJump;
        public void UpdateAnalogGlitchHorizontalShake(float horizontalShake) => analogGlitch.horizontalShake = horizontalShake;
        public void UpdateAnalogGlitchColorDrift(float colorDrift) => analogGlitch.colorDrift = colorDrift;

		public void UpdateAnalogGlitch(bool enabled, float scanLineJitter, float verticalJump, float horizontalShake, float colorDrift)
        {
			analogGlitch.enabled = enabled;
			analogGlitch.scanLineJitter = scanLineJitter;
			analogGlitch.verticalJump = verticalJump;
			analogGlitch.horizontalShake = horizontalShake;
			analogGlitch.colorDrift = colorDrift;
        }

		public void UpdateDigitalGlitch(float intensity) => digitalGlitch.intensity = intensity;

        #endregion

        #region Chroma

        public void UpdateChroma(float intensity) => chroma?.intensity?.Override(intensity);

        #endregion

        #region Bloom

        public void UpdateBloomIntensity(float intensity) => bloom?.intensity?.Override(intensity);
        public void UpdateBloomDiffusion(float diffusion) => bloom?.diffusion?.Override(diffusion);
        public void UpdateBloomThreshold(float threshold) => bloom?.threshold?.Override(threshold);
        public void UpdateBloomAnamorphicRatio(float anamorphicRatio) => bloom?.anamorphicRatio?.Override(anamorphicRatio);
        public void UpdateBloomColor(float colorSlot)
        {
            if (!InterfaceManager.inst || InterfaceManager.inst.CurrentInterface == null)
                return;
            int slot = (int)colorSlot;
            UpdateBloomColor(slot == InterfaceManager.inst.CurrentInterface.Theme.effectColors.Count ? Color.white : InterfaceManager.inst.CurrentInterface.Theme.GetFXColor(slot));
        }
        public void UpdateBloomColor(Color color) => bloom?.color?.Override(color);

		public void UpdateBloom(float intensity, float diffusion, float threshold, float anamorphicRatio, Color color)
        {
			bloom?.intensity?.Override(intensity);
			bloom?.diffusion?.Override(diffusion);
			bloom?.threshold?.Override(threshold);
			bloom?.anamorphicRatio?.Override(anamorphicRatio);
			bloom?.color?.Override(color);
        }

        #endregion

        #region Vignette

        public void UpdateVignetteIntensity(float intensity) => vignette?.intensity?.Override(intensity);
        public void UpdateVignetteSmoothness(float smoothness) => vignette?.smoothness?.Override(smoothness);
        public void UpdateVignetteRounded(float rounded) => vignette?.rounded?.Override(rounded == 1f);
        public void UpdateVignetteRounded(bool rounded) => vignette?.rounded?.Override(rounded);
        public void UpdateVignetteRoundness(float roundness) => vignette?.roundness?.Override(roundness);
        public void UpdateVignetteCenterX(float x) => vignette?.center?.Override(new Vector2(x, vignette?.center?.value.y ?? 0f));
        public void UpdateVignetteCenterY(float y) => vignette?.center?.Override(new Vector2(vignette?.center?.value.x ?? 0f, y));
        public void UpdateVignetteColor(float colorSlot)
        {
            if (!InterfaceManager.inst || InterfaceManager.inst.CurrentInterface == null)
                return;
            int slot = (int)colorSlot;
            UpdateVignetteColor(slot == InterfaceManager.inst.CurrentInterface.Theme.effectColors.Count ? Color.black : InterfaceManager.inst.CurrentInterface.Theme.GetFXColor(slot));
        }
        public void UpdateVignetteColor(Color color) => vignette?.color?.Override(color);

        public void UpdateVignette(float intensity, float smoothness, bool rounded, float roundness, Vector2 center, Color color)
        {
            vignette?.intensity?.Override(intensity);
            vignette?.smoothness?.Override(smoothness);
            vignette?.rounded?.Override(rounded);
            vignette?.roundness?.Override(roundness);
            vignette?.center?.Override(center);
            vignette?.color?.Override(color);
        }

        #endregion

        #region Lens Distort

        public void UpdateLensDistortIntensity(float intensity) => lensDistort?.intensity?.Override(intensity);
        public void UpdateLensDistortCenterX(float x) => lensDistort?.centerX?.Override(x);
        public void UpdateLensDistortCenterY(float y) => lensDistort?.centerY?.Override(y);
        public void UpdateLensDistortIntensityX(float x) => lensDistort?.intensityX?.Override(x);
        public void UpdateLensDistortIntensityY(float y) => lensDistort?.intensityY?.Override(y);
        public void UpdateLensDistortScale(float scale) => lensDistort?.scale?.Override(scale);

        #endregion

        #region Grain

        public void UpdateGrainIntensity(float intensity) => grain?.intensity?.Override(intensity);
        public void UpdateGrainScale(float size) => grain?.size?.Override(size);
        public void UpdateGrainColored(float colored) => grain?.colored?.Override(colored == 1f);
        public void UpdateGrainColored(bool colored) => grain?.colored?.Override(colored);

        #endregion

        #region Pixelize

        public void UpdatePixelize(float amount) => pixelize?.amount?.Override(amount);

        #endregion

        #region Scan Lines

        public void UpdateScanlinesIntensity(float intensity) => scanlines?.intensity?.Override(intensity);

        public void UpdateScanlinesAmount(float amountHorizontal) => scanlines?.amountHorizontal?.Override(amountHorizontal);

        public void UpdateScanlinesSpeed(float speed) => scanlines?.speed?.Override(speed);

        #endregion

        void RegisterFunction(Action<float> action)
        {
            try
            {
                functions[action.Method.Name.Remove("Update")] = action;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public Dictionary<string, Action<float>> functions = new Dictionary<string, Action<float>>();

        public bool analogGlitchEnabled;

        PostProcessVolume ppvolume;

        static AssetBundle postProcessResourcesAssetBundle;
        static PostProcessResources postProcessResources;

        public Camera glitchCam;

        public AnalogGlitch analogGlitch;
		public DigitalGlitch digitalGlitch;

        public ChromaticAberration chroma;

        public Bloom bloom;

        public Vignette vignette;

        public LensDistortion lensDistort;

        public Grain grain;

        public Blur blur;

        public Pixelize pixelize;

        public Ripples ripples;

        public Dithering dithering;

        public ColorGrading colorGrading;
        public SCPE.Gradient gradient;
        public DoubleVision doubleVision;
        public RadialBlur radialBlur;
        public Scanlines scanlines;
        public Sharpen sharpen;

        public BlackBars blackBars;
        public ColorSplit colorSplit;
        public Danger danger;
        public Invert invert;
        public Mosaic mosaic;
        public TiltShift tiltShift;
        public TubeDistortion tubeDistortion;
    }
}
