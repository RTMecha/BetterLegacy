using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using SCPE;

using BetterLegacy.Core;

namespace BetterLegacy.Menus
{
    public class MenuEffectsManager : MonoBehaviour
    {
        public static MenuEffectsManager inst;

        void Awake()
        {
            if (!inst)
                inst = this;
            else if (inst != this)
                Destroy(this);
        }

		void Start()
		{
			var camera = Camera.main;
			analogGlitch = camera.gameObject.GetComponent<AnalogGlitch>() ?? camera.gameObject.AddComponent<AnalogGlitch>();
			analogGlitch._shader = LegacyPlugin.analogGlitchShader;
			digitalGlitch = camera.gameObject.GetComponent<DigitalGlitch>() ?? camera.gameObject.AddComponent<DigitalGlitch>();
			digitalGlitch._shader = LegacyPlugin.digitalGlitchShader;
            analogGlitch.enabled = false; // disabled by default due to there still being a slight effect when it is enabled.

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
            colorGrading.hueShift.Override(0.1f);
            colorGrading.contrast.Override(0.1f);
            colorGrading.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            colorGrading.saturation.Override(0.1f);
            colorGrading.temperature.Override(0.1f);
            colorGrading.tint.Override(0.1f);

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
            blur.iterations.Override(0);

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

            ppvolume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f,
				chroma, bloom, vignette, lensDistort, grain, blur, pixelize, ripples);

			ppvolume.isGlobal = true;
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

		public void UpdateBloom(float intensity, float diffusion, float threshold, float anamorphicRatio, Color color)
        {
			bloom?.intensity?.Override(intensity);
			bloom?.diffusion?.Override(diffusion);
			bloom?.threshold?.Override(threshold);
			bloom?.anamorphicRatio?.Override(anamorphicRatio);
			bloom?.color?.Override(color);
        }

        public void UpdateVignette(float intensity, float smoothness, bool rounded, float roundness, Vector2 center, Color color)
        {
            vignette?.intensity?.Override(intensity);
            vignette?.smoothness?.Override(smoothness);
            vignette?.rounded?.Override(rounded);
            vignette?.roundness?.Override(roundness);
            vignette?.center?.Override(center);
            vignette?.color?.Override(color);
        }

        PostProcessVolume ppvolume;

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
