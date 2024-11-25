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

        public static bool ShowEffects => EventsConfig.Instance.ShowFX.Value;

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
			var camera = Camera.main;
			analogGlitch = camera.gameObject.GetComponent<AnalogGlitch>() ?? camera.gameObject.AddComponent<AnalogGlitch>();
			analogGlitch._shader = LegacyPlugin.analogGlitchShader;
			digitalGlitch = camera.gameObject.GetComponent<DigitalGlitch>() ?? camera.gameObject.AddComponent<DigitalGlitch>();
			digitalGlitch._shader = LegacyPlugin.digitalGlitchShader;
            analogGlitch.enabled = false; // disabled by default due to there still being a slight effect when it is enabled.

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

            // Bloom
            bloom = ScriptableObject.CreateInstance<Bloom>();
			bloom.enabled.Override(true);
			bloom.intensity.Override(0f);
			bloom.fastMode.Override(true);

            // Vignette
			vignette = ScriptableObject.CreateInstance<Vignette>();
			vignette.enabled.Override(true);
			vignette.intensity.Override(0f);

            // Lens Distort
			lensDistort = ScriptableObject.CreateInstance<LensDistortion>();
			lensDistort.enabled.Override(true);
			lensDistort.intensity.Override(0f);

            // Grain
			grain = ScriptableObject.CreateInstance<Grain>();
			grain.enabled.Override(true);
			grain.intensity.Override(0f);

            // Pixelize
			pixelize = ScriptableObject.CreateInstance<Pixelize>();
			pixelize.enabled.Override(true);
			pixelize.amount.Override(0f);

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
        void Update()
        {
            CoreHelper.UpdateValue(prevShowEffects, ShowEffects, x =>
            {
                prevShowEffects = ShowEffects;

                if (!ShowEffects)
                {
                    analogGlitch.enabled = false;
                    digitalGlitch.enabled = false;
                    ppvolume.enabled = false;
                }
            });
        }

        void OnDestroy()
        {
            //postProcessResourcesAssetBundle?.Unload(true);
            //postProcessResourcesAssetBundle = null;

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

        public void UpdateAnalogGlitchEnabled(bool enabled) => analogGlitch.enabled = enabled;
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

		public void UpdateChroma(float intensity) => chroma?.intensity?.Override(intensity);

        public void UpdateBloomIntensity(float intensity) => bloom?.intensity?.Override(intensity);
        public void UpdateBloomDiffusion(float diffusion) => bloom?.diffusion?.Override(diffusion);
        public void UpdateBloomThreshold(float threshold) => bloom?.threshold?.Override(threshold);
        public void UpdateBloomAnamorphicRatio(float anamorphicRatio) => bloom?.anamorphicRatio?.Override(anamorphicRatio);
        public void UpdateBloomColor(Color color) => bloom?.color?.Override(color);

		public void UpdateBloom(float intensity, float diffusion, float threshold, float anamorphicRatio, Color color)
        {
			bloom?.intensity?.Override(intensity);
			bloom?.diffusion?.Override(diffusion);
			bloom?.threshold?.Override(threshold);
			bloom?.anamorphicRatio?.Override(anamorphicRatio);
			bloom?.color?.Override(color);
        }

        public void UpdateVignetteIntensity(float intensity) => vignette?.intensity?.Override(intensity);
        public void UpdateVignetteSmoothness(float smoothness) => vignette?.smoothness?.Override(smoothness);
        public void UpdateVignetteRounded(bool rounded) => vignette?.rounded?.Override(rounded);
        public void UpdateVignetteRoundness(float roundness) => vignette?.roundness?.Override(roundness);
        public void UpdateVignetteCenterX(float x) => vignette?.center?.Override(new Vector2(x, vignette?.center?.value.y ?? 0f));
        public void UpdateVignetteCenterY(float y) => vignette?.center?.Override(new Vector2(vignette?.center?.value.x ?? 0f, y));
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

        public void UpdateLensDistortIntensity(float intensity) => lensDistort?.intensity?.Override(intensity);
        public void UpdateLensDistortCenterX(float x) => lensDistort?.centerX?.Override(x);
        public void UpdateLensDistortCenterY(float y) => lensDistort?.centerY?.Override(y);
        public void UpdateLensDistortIntensityX(float x) => lensDistort?.intensityX?.Override(x);
        public void UpdateLensDistortIntensityY(float y) => lensDistort?.intensityY?.Override(y);
        public void UpdateLensDistortScale(float scale) => lensDistort?.scale?.Override(scale);

        PostProcessVolume ppvolume;

        static AssetBundle postProcessResourcesAssetBundle;
        static PostProcessResources postProcessResources;

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
