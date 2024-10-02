using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BetterLegacy.Core.Managers
{
    public class RTEffectsManager : MonoBehaviour
    {
        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);
        }

        void Start()
        {
            //ColorGrading
            colorGrading = ScriptableObject.CreateInstance<ColorGrading>();
            colorGrading.enabled.Override(true);
            colorGrading.hueShift.Override(0.1f);
            colorGrading.contrast.Override(0.1f);
            colorGrading.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            colorGrading.saturation.Override(0.1f);
            colorGrading.temperature.Override(0.1f);
            colorGrading.tint.Override(0.1f);

            //Gradient
            gradient = ScriptableObject.CreateInstance<SCPE.Gradient>();
            gradient.enabled.Override(true);
            gradient.intensity.Override(0f);
            gradient.color1.Override(new Color(0f, 0.8f, 0.56f, 0.5f));
            gradient.color2.Override(new Color(0.81f, 0.37f, 1f, 0.5f));
            gradient.rotation.Override(0f);
            gradient.blendMode.Override(SCPE.Gradient.BlendMode.Linear);

            //Double Vision
            doubleVision = ScriptableObject.CreateInstance<SCPE.DoubleVision>();
            doubleVision.enabled.Override(true);
            doubleVision.intensity.Override(0f);
            doubleVision.mode.Override(SCPE.DoubleVision.Mode.FullScreen);

            //Radial Blur
            radialBlur = ScriptableObject.CreateInstance<SCPE.RadialBlur>();
            radialBlur.enabled.Override(true);
            radialBlur.amount.Override(0f);
            radialBlur.iterations.Override(6);

            //Scan Lines
            scanlines = ScriptableObject.CreateInstance<SCPE.Scanlines>();
            scanlines.enabled.Override(true);
            scanlines.intensity.Override(0f);
            scanlines.amountHorizontal.Override(10f);
            scanlines.speed.Override(1f);

            //Sharpen
            sharpen = ScriptableObject.CreateInstance<SCPE.Sharpen>();
            sharpen.enabled.Override(true);
            sharpen.amount.Override(0f);

            //Ripples
            ripples = ScriptableObject.CreateInstance<SCPE.Ripples>();
            ripples.enabled.Override(true);
            ripples.strength.Override(0f);
            ripples.speed.Override(3f);
            ripples.distance.Override(5f);
            ripples.height.Override(1f);
            ripples.width.Override(1.5f);

            //Blur
            blur = ScriptableObject.CreateInstance<SCPE.Blur>();
            blur.enabled.Override(true);
            blur.amount.Override(0f);
            blur.iterations.Override(0);

            //Color Split
            colorSplit = ScriptableObject.CreateInstance<SCPE.ColorSplit>();
            colorSplit.enabled.Override(true);
            colorSplit.offset.Override(0f);
            colorSplit.mode.Override(SCPE.ColorSplit.SplitMode.Single);

            //Black Bars
            blackBars = ScriptableObject.CreateInstance<SCPE.BlackBars>();
            blackBars.enabled.Override(true);
            blackBars.size.Override(0f);
            blackBars.mode.Override(SCPE.BlackBars.Direction.Horizontal);

            //Danger
            danger = ScriptableObject.CreateInstance<SCPE.Danger>();
            danger.enabled.Override(true);
            danger.intensity.Override(0f);
            danger.color.Override(new Color(0.66f, 0f, 0f));

            //Invert
            invert = ScriptableObject.CreateInstance<SCPE.Invert>();
            invert.enabled.Override(true);
            invert.amount.Override(0f);

            //Mosaic
            mosaic = ScriptableObject.CreateInstance<SCPE.Mosaic>();
            mosaic.enabled.Override(true);
            mosaic.size.Override(0f);
            mosaic.mode.Override(SCPE.Mosaic.MosaicMode.Hexagons);

            //Tilt Shift
            tiltShift = ScriptableObject.CreateInstance<SCPE.TiltShift>();
            tiltShift.enabled.Override(true);
            tiltShift.amount.Override(0f);

            //Tube Distortion
            tubeDistortion = ScriptableObject.CreateInstance<SCPE.TubeDistortion>();
            tubeDistortion.enabled.Override(true);
            tubeDistortion.amount.Override(0f);
            tubeDistortion.mode.Override(SCPE.TubeDistortion.DistortionMode.Buldged);

            dithering = ScriptableObject.CreateInstance<SCPE.Dithering>();
            dithering.enabled.Override(true);
            dithering.intensity.Override(0f);

            //PP Volume
            ppvolume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, new PostProcessEffectSettings[]
            {
                colorGrading,
                gradient,
                doubleVision,
                radialBlur,
                scanlines,
                sharpen,
                blackBars,
                colorSplit,
                danger,
                invert,
                mosaic,
                tiltShift,
                tubeDistortion,
                ripples,
                dithering
            });
            ppvolume.isGlobal = true;
        }

        public void UpdateColorGrading(float _hueShift, float _contrast, Vector4 _gamma, float _saturation, float _temperature, float _tint)
        {
            colorGrading.hueShift.Override(_hueShift);
            colorGrading.contrast.Override(_contrast);
            colorGrading.gamma.Override(_gamma);
            colorGrading.saturation.Override(_saturation);
            colorGrading.temperature.Override(_temperature);
            colorGrading.tint.Override(_tint);
        }

        public void UpdateGradient(float _intensity, float _rotation)
        {
            gradient.intensity.Override(_intensity);
            gradient.rotation.Override(_rotation);
        }

        public void UpdateDoubleVision(float _intensity, int mode)
        {
            doubleVision.intensity.Override(_intensity);
            doubleVision.mode.Override((SCPE.DoubleVision.Mode)mode);
        }

        public void UpdateRadialBlur(float _amount, int _iterations)
        {
            radialBlur.amount.Override(_amount);
            radialBlur.iterations.Override(_iterations);
        }

        public void UpdateScanlines(float _intensity, float _amountHorizontal, float _speed)
        {
            scanlines.intensity.Override(_intensity);
            scanlines.amountHorizontal.Override(_amountHorizontal);
            scanlines.speed.Override(_speed);
        }

        public void UpdateSharpen(float _amount)
        {
            sharpen.amount.Override(_amount);
        }

        public void UpdateRipples(float _strength, float _speed, float _distance, float _height, float _width, int mode)
        {
            ripples.strength.Override(_strength);
            ripples.speed.Override(_speed);
            ripples.distance.Override(_distance);
            ripples.height.Override(_height);
            ripples.width.Override(_width);
            ripples.mode.Override((SCPE.Ripples.RipplesMode)mode);
        }

        public void UpdateBlur(float _amount, int _iterations)
        {
            blur.amount.Override(_amount);
            blur.iterations.Override(_iterations);
        }

        public void UpdateDigitalGlitch(float _intensity)
        {
            digitalGlitch.intensity = _intensity;
        }

        public void UpdateAnalogGlitch(float _colorDrift, float _scanLineJitter, float _verticalJump, float _horizontalShake)
        {
            analogGlitch.colorDrift = _colorDrift;
            analogGlitch.scanLineJitter = _scanLineJitter;
            analogGlitch.verticalJump = _verticalJump;
            analogGlitch.horizontalShake = _horizontalShake;
        }

        public void UpdateBlackBars(float _size, float _mode)
        {
            blackBars.size.Override(_size);
            blackBars.mode.Override((SCPE.BlackBars.Direction)(int)_mode);
        }

        public void UpdateColorSplit(float _offset, int mode)
        {
            colorSplit.offset.Override(_offset);
            colorSplit.mode.Override((SCPE.ColorSplit.SplitMode)mode);
        }

        public void UpdateDanger(float _intensity, Color _color, float _size)
        {
            danger.intensity.Override(_intensity);
            danger.color.Override(_color);
            danger.size.Override(_size);
        }

        public void UpdateInvert(float _amount)
        {
            invert.amount.Override(_amount);
        }

        public void UpdateMosaic(float _size)
        {
            mosaic.size.Override(_size);
        }

        public void UpdateTiltShit(float _amount, float _areaSize)
        {
            tiltShift.amount.Override(_amount);
            tiltShift.areaSize.Override(_areaSize);
        }

        public void UpdateTubeDistortion(float _amount)
        {
            tubeDistortion.amount.Override(_amount);
        }

        public void UpdateLensDistort(float _centerX, float _centerY, float _intensityX, float _intensityY, float _scale)
        {
            LSEffectsManager.inst.lensDistort.centerX.Override(_centerX);
            LSEffectsManager.inst.lensDistort.centerY.Override(_centerY);
            LSEffectsManager.inst.lensDistort.intensityX.Override(_intensityX);
            LSEffectsManager.inst.lensDistort.intensityY.Override(_intensityY);
            LSEffectsManager.inst.lensDistort.scale.Override(_scale);
        }

        public static RTEffectsManager inst;
        private PostProcessVolume ppvolume;

        public ColorGrading colorGrading;
        public SCPE.Gradient gradient;
        public SCPE.DoubleVision doubleVision;
        public SCPE.RadialBlur radialBlur;
        public SCPE.Scanlines scanlines;
        public SCPE.Sharpen sharpen;

        public SCPE.Ripples ripples;
        public SCPE.Blur blur;
        public SCPE.Dithering dithering;

        public AnalogGlitch analogGlitch;
        public DigitalGlitch digitalGlitch;

        public SCPE.BlackBars blackBars;
        public SCPE.ColorSplit colorSplit;
        public SCPE.Danger danger;
        public SCPE.Invert invert;
        public SCPE.Mosaic mosaic;
        public SCPE.TiltShift tiltShift;
        public SCPE.TubeDistortion tubeDistortion;
    }
}
