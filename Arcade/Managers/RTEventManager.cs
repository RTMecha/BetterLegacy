using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using DG.Tweening;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor;

using Ease = BetterLegacy.Core.Animation.Ease;
using Random = UnityEngine.Random;

namespace BetterLegacy.Arcade.Managers
{
    public class RTEventManager : MonoBehaviour
    {
        public EventDelayTracker delayTracker;

        public static RTEventManager inst;

        public EditorCameraController editorCamera;

        public Camera glitchCam;
        public AnalogGlitch analogGlitch;
        public DigitalGlitch digitalGlitch;

        public Camera uiCam;

        public static bool windowPositionResolutionChanged = false;

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

            offsets = ResetOffsets();

            editorCamera = EditorCameraController.Bind();

            // Create a new camera since putting the glitch effects on the Main Camera make the objects disappear.
            var glitchCamera = new GameObject("Glitch Camera");
            glitchCamera.transform.SetParent(EventManager.inst.cam.transform);
            glitchCamera.transform.localPosition = Vector3.zero;
            glitchCamera.transform.localScale = Vector3.one;

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
            digitalGlitch._shader = LegacyResources.digitalGlitchShader;

            analogGlitch = glitchCamera.AddComponent<AnalogGlitch>();
            analogGlitch._shader = LegacyResources.analogGlitchShader;

            var uiCamera = new GameObject("UI Camera");
            uiCamera.transform.SetParent(EventManager.inst.cam.transform.parent);
            uiCamera.transform.localPosition = Vector3.zero;
            uiCamera.transform.localScale = Vector3.one;

            uiCam = uiCamera.AddComponent<Camera>();
            uiCam.allowMSAA = forceWindow;
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.cullingMask = 2080; // 2080 = layer 11
            uiCam.depth = 2;
            uiCam.orthographic = true;
            uiCam.farClipPlane = 10000f;
            uiCam.nearClipPlane = -10000f;
        }

        public void SetResetOffsets() => offsets = ResetOffsets();

        public static bool Playable =>
            RTEffectsManager.inst &&
            EventManager.inst &&
            GameManager.inst &&
            (CoreHelper.Playing || CoreHelper.Reversing || LevelManager.LevelEnded) &&
            GameData.Current &&
            GameData.Current.events != null &&
            !GameData.Current.events.IsEmpty();

        void EditorCameraHandler()
        {
            if (!EditorManager.inst)
                return;

            if (editorCamera.Activate.WasPressed)
                EventsConfig.Instance.EditorCamEnabled.Value = !EventsConfig.Instance.EditorCamEnabled.Value;

            if (EventsConfig.Instance.EditorCamEnabled.Value)
            {
                float multiplyController = 1f;

                if (editorCamera.SlowDown.IsPressed)
                    multiplyController = EventsConfig.Instance.EditorCamSlowSpeed.Value;
                if (editorCamera.SpeedUp.IsPressed)
                    multiplyController = EventsConfig.Instance.EditorCamFastSpeed.Value;

                float x = editorCamera.Move.Vector.x;
                float y = editorCamera.Move.Vector.y;

                var vector = new Vector3(x * EditorSpeed * multiplyController, y * EditorSpeed * multiplyController, 0f);
                if (vector.magnitude > 1f)
                    vector = vector.normalized;

                editorOffset.x += vector.x;
                editorOffset.y += vector.y;

                if (editorCamera.ZoomOut.IsPressed)
                    editorZoom += 0.5f * EditorSpeed * multiplyController;
                if (editorCamera.ZoomIn.IsPressed)
                    editorZoom -= 0.5f * EditorSpeed * multiplyController;

                if (editorCamera.RotateAdd.IsPressed)
                    editorRotate += 0.1f * EditorSpeed * multiplyController;
                if (editorCamera.RotateSub.IsPressed)
                    editorRotate -= 0.1f * EditorSpeed * multiplyController;

                var xRot = editorCamera.Rotate.Vector.x;
                var yRot = editorCamera.Rotate.Vector.y;
                var vectorRot = new Vector3(xRot * EditorSpeed * multiplyController, yRot * EditorSpeed * multiplyController, 0f);
                if (vectorRot.magnitude > 1f)
                    vectorRot = vectorRot.normalized;

                editorPerRotate.x += vectorRot.x;
                editorPerRotate.y += vectorRot.y;

                if (editorCamera.ResetOffsets.WasPressed)
                {
                    editorOffset = EventManager.inst.camPos;
                    if (!float.IsNaN(EventManager.inst.camZoom))
                        editorZoom = EventManager.inst.camZoom;
                    if (!float.IsNaN(EventManager.inst.camRot))
                        editorRotate = EventManager.inst.camRot;
                    editorPerRotate = Vector2.zero;
                }

                if (CoreHelper.IsUsingInputField || !EventsConfig.Instance.EditorCamUseKeys.Value)
                    return;

                float multiply = 1f;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.5f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;

                if (Input.GetKey(KeyCode.A))
                    editorOffset.x -= 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.D))
                    editorOffset.x += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.W))
                    editorOffset.y += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.S))
                    editorOffset.y -= 0.1f * EditorSpeed * multiply;

                if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
                    editorZoom += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
                    editorZoom -= 0.1f * EditorSpeed * multiply;

                if (Input.GetKey(KeyCode.Keypad4))
                    editorRotate += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.Keypad6))
                    editorRotate -= 0.1f * EditorSpeed * multiply;

                if (Input.GetKey(KeyCode.LeftArrow))
                    editorPerRotate.y += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.RightArrow))
                    editorPerRotate.y -= 0.1f * EditorSpeed * multiply;

                if (Input.GetKey(KeyCode.UpArrow))
                    editorPerRotate.x += 0.1f * EditorSpeed * multiply;
                if (Input.GetKey(KeyCode.DownArrow))
                    editorPerRotate.x -= 0.1f * EditorSpeed * multiply;

                if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    editorOffset = EventManager.inst.camPos;
                    if (!float.IsNaN(EventManager.inst.camZoom))
                        editorZoom = EventManager.inst.camZoom;
                    if (!float.IsNaN(EventManager.inst.camRot))
                        editorRotate = EventManager.inst.camRot;
                    editorPerRotate = Vector2.zero;
                }
            }
            else if (EventsConfig.Instance.EditorCamResetValues.Value)
            {
                editorOffset = EventManager.inst.camPos;
                if (!float.IsNaN(EventManager.inst.camZoom))
                    editorZoom = EventManager.inst.camZoom;
                if (!float.IsNaN(EventManager.inst.camRot))
                    editorRotate = EventManager.inst.camRot;
                editorPerRotate = Vector2.zero;
            }
        }

        float previousAudioTime;
        float audioTimeVelocity;
        float shakeTime;

        void Interpolate()
        {
            var allEvents = GameData.Current.events;
            var time = currentTime;

            if (shakeSequence != null && shakeSequence.keyframes != null && shakeSequence.keyframes.Length > 0 && EventsConfig.Instance.ShakeEventMode.Value == ShakeType.Catalyst)
            {
                for (int i = 0; i < shakeSequence.keyframes.Length; i++)
                    shakeSequence.keyframes[i].SetEase(ShakeEase);
                var speed = shakeSpeed < 0.001f ? 1f : shakeSpeed;
                shakeTime += (time - previousAudioTime) * speed;
                shakeTime = Mathf.Clamp(shakeTime, 0f, !AudioManager.inst.CurrentAudioSource.clip ? 0f : AudioManager.inst.CurrentAudioSource.clip.length);
                EventManager.inst.shakeVector = shakeSequence.Interpolate(shakeTime % shakeLength);
            }

            for (int i = 0; i < allEvents.Count; i++)
            {
                var list = allEvents[i].OrderBy(x => x.time).ToList();

                var nextKFIndex = list.FindIndex(x => x.time > time);

                if (nextKFIndex >= 0)
                {
                    var prevKFIndex = nextKFIndex - 1;
                    if (prevKFIndex < 0)
                        prevKFIndex = 0;

                    var nextKF = list[nextKFIndex];
                    var prevKF = list[prevKFIndex];

                    if (events.Length <= i)
                        continue;

                    for (int j = 0; j < nextKF.values.Length; j++)
                    {
                        if (events[i].Length <= j || prevKF.values.Length <= j || events[i][j] == null)
                            continue;

                        var total = 0f;
                        var prevtotal = 0f;
                        for (int k = 0; k < nextKFIndex; k++)
                        {
                            if (allEvents[i][k + 1].relative)
                                total += allEvents[i][k].values[j];
                            else
                                total = 0f;

                            if (allEvents[i][k].relative)
                                prevtotal += allEvents[i][k].values[j];
                            else
                                prevtotal = 0f;
                        }

                        var next = nextKF.relative ? total + nextKF.values[j] : nextKF.values[j];
                        var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.values[j];

                        bool isLerper = IsLerper(i, j);

                        if (float.IsNaN(prev) || !isLerper)
                            prev = 0f;

                        if (float.IsNaN(next))
                            next = 0f;

                        if (!isLerper)
                            next = 1f;

                        var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, time)));

                        if (prevKFIndex == nextKFIndex)
                            x = next;

                        float offset = 0f;
                        if (offsets.Count > i && offsets[i].Count > j && isLerper)
                            offset = offsets[i][j];

                        if (float.IsNaN(offset) || float.IsInfinity(offset))
                            offset = 0f;

                        if (float.IsNaN(x) || float.IsInfinity(x))
                            x = next;

                        events[i][j](x + offset);
                    }
                }
                else if (!list.IsEmpty())
                {
                    if (events.Length <= i)
                        continue;

                    for (int j = 0; j < list[list.Count - 1].values.Length; j++)
                    {
                        if (events[i].Length <= j || events[i][j] == null)
                            continue;

                        var total = 0f;
                        for (int k = 0; k < list.Count - 1; k++)
                        {
                            if (allEvents[i][k + 1].relative)
                                total += allEvents[i][k].values[j];
                            else
                                total = 0f;
                        }

                        bool isLerper = IsLerper(i, j);

                        var x = list[list.Count - 1].values[j];

                        if (float.IsNaN(x))
                            x = 0f;

                        if (!isLerper)
                            x = 1f;

                        float offset = 0f;
                        if (offsets.Count > i && offsets[i].Count > j && isLerper)
                            offset = offsets[i][j];

                        if (float.IsNaN(offset) || float.IsInfinity(offset))
                            offset = 0f;

                        if (float.IsNaN(x) || float.IsInfinity(x))
                            x = allEvents[i][allEvents[i].Count - 1].values[j];

                        events[i][j](x + offset + total);
                    }
                }
            }

            previousAudioTime = time;
        }

        bool IsLerper(int i, int j)
            => !(i == 4 || i == 6 && j == 4 || i == 7 && j == 6 || i == 15 && (j == 2 || j == 3) || i == 20 && j == 0 || i == 22 && j == 6 || i == 30 && j == 2);

        public float Interpolate(int type, int valueIndex, float time)
        {
            var allEvents = GameData.Current.events;

            var list = allEvents[type].OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = 0;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            type = Mathf.Clamp(type, 0, events.Length);
            valueIndex = Mathf.Clamp(valueIndex, 0, events[type].Length);

            if (prevKF.values.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (allEvents[type][k + 1].relative)
                    total += allEvents[type][k].values[valueIndex];
                else
                    total = 0f;

                if (allEvents[type][k].relative)
                    prevtotal += allEvents[type][k].values[valueIndex];
                else
                    prevtotal = 0f;
            }

            var next = nextKF.relative ? total + nextKF.values[valueIndex] : nextKF.values[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.values[valueIndex];

            bool isLerper = IsLerper(type, valueIndex);

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, time)));

            if (prevKFIndex == nextKFIndex)
                x = next;

            float offset = 0f;
            if (offsets.Count > type && offsets[type].Count > valueIndex && isLerper)
                offset = offsets[type][valueIndex];

            if (float.IsNaN(offset) || float.IsInfinity(offset))
                offset = 0f;

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x + offset;
        }

        public float fieldOfView = 50f;
        public float camPerspectiveOffset = 10f;

        public float currentTime;

        public bool windowHasChanged;

        public static void OnLevelTick()
        {
            inst.EditorCameraHandler();

            if (GameManager.inst.introMain != null && AudioManager.inst.CurrentAudioSource.time < 15f)
                GameManager.inst.introMain.SetActive(EventsConfig.Instance.ShowIntro.Value);

            if (Playable)
            {
                if (CoreConfig.Instance.ControllerRumble.Value && EventsConfig.Instance.ShakeAffectsController.Value)
                    InputDataManager.inst.SetAllControllerRumble(EventManager.inst.shakeMultiplier);

                if (RTLevel.UseNewUpdateMethod)
                {
                    var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
                    var smoothedTime = Mathf.SmoothDamp(inst.previousAudioTime, currentAudioTime, ref inst.audioTimeVelocity, 1.0f / 50.0f);
                    inst.currentTime = smoothedTime;
                }
                else
                    inst.currentTime = AudioManager.inst.CurrentAudioSource.time;

                #region New Sequences

                if (EventManager.inst.eventSequence == null)
                    EventManager.inst.eventSequence = DOTween.Sequence();
                if (EventManager.inst.themeSequence == null)
                    EventManager.inst.themeSequence = DOTween.Sequence();
                if (EventManager.inst.shakeSequence == null && EventsConfig.Instance.ShakeEventMode.Value == ShakeType.Original)
                {
                    EventManager.inst.shakeSequence = DOTween.Sequence();

                    float strength = 3f;
                    int vibrato = 10;
                    float randomness = 90f;
                    EventManager.inst.shakeSequence.Insert(0f, DOTween.Shake(() => Vector3.zero, delegate (Vector3 x)
                    {
                        EventManager.inst.shakeVector = x;
                    }, AudioManager.inst.CurrentAudioSource.clip.length, strength, vibrato, randomness, true, false));
                }

                #endregion

                inst.FindColors();
                inst.Interpolate();

                #region Camera

                inst.UpdateShake();

                if (float.IsNaN(EventManager.inst.camRot))
                    EventManager.inst.camRot = 0f;
                if (float.IsNaN(EventManager.inst.camZoom) || EventManager.inst.camZoom == 0f)
                    EventManager.inst.camZoom = 20f;

                var editorCam = EventsConfig.Instance.EditorCameraEnabled;
                if (!editorCam)
                    EventManager.inst.cam.orthographicSize = EventManager.inst.camZoom;
                else if (inst.EditorSpeed != 0f)
                    EventManager.inst.cam.orthographicSize = inst.editorZoom;

                if (inst.uiCam)
                    inst.uiCam.orthographicSize = EventManager.inst.cam.orthographicSize;

                if (!float.IsNaN(EventManager.inst.camRot) && !editorCam)
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(inst.camRotOffset.x, inst.camRotOffset.y, EventManager.inst.camRot));
                else if (!float.IsNaN(inst.editorRotate))
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(inst.editorPerRotate.x, inst.editorPerRotate.y, inst.editorRotate));

                var camPos = editorCam ? inst.editorOffset : EventManager.inst.camPos;
                EventManager.inst.camParentTop.transform.localPosition = new Vector3(camPos.x, camPos.y, inst.zPosition);

                EventManager.inst.camPer.fieldOfView = inst.fieldOfView;

                var bgZoom = editorCam ? -inst.editorZoom + inst.perspectiveZoom : - EventManager.inst.camZoom + inst.perspectiveZoom;
                if (!inst.bgGlobalPosition)
                    EventManager.inst.camPer.transform.SetLocalPositionZ(bgZoom);
                else
                    EventManager.inst.camPer.transform.SetPositionZ(bgZoom);

                // fixes bg camera position being offset if rotated for some reason...
                EventManager.inst.camPer.transform.SetLocalPositionX(0f);
                EventManager.inst.camPer.transform.SetLocalPositionY(0f);

                EventManager.inst.camPer.nearClipPlane = inst.bgAlignNearPlane ? -EventManager.inst.camPer.transform.position.z + inst.camPerspectiveOffset : 0.3f;

                #endregion

                #region Lerp Colors

                LSEffectsManager.inst.bloom.color.Override(
                    ChangeColorHSV(LerpColor(inst.prevBloomColor, inst.nextBloomColor, inst.bloomColor, Color.white),
                    inst.bloomHue, inst.bloomSat, inst.bloomVal));

                LSEffectsManager.inst.vignette.color.Override(
                    ChangeColorHSV(LerpColor(inst.prevVignetteColor, inst.nextVignetteColor, inst.vignetteColor, Color.black),
                    inst.vignetteHue, inst.vignetteSat, inst.vignetteVal));

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;
                ThemeManager.inst.bgColorToLerp = ChangeColorHSV(LerpColor(inst.prevBGColor, inst.nextBGColor, inst.bgColor, beatmapTheme.backgroundColor), inst.bgHue, inst.bgSat, inst.bgVal);

                ThemeManager.inst.timelineColorToLerp =
                    LSColors.fadeColor(ChangeColorHSV(LerpColor(inst.prevTimelineColor, inst.nextTimelineColor, inst.timelineColor, beatmapTheme.guiColor),
                    inst.timelineHue, inst.timelineSat, inst.timelineVal), inst.timelineOpacity);

                inst.dangerColorResult =
                    LSColors.fadeColor(ChangeColorHSV(LerpColor(inst.prevDangerColor, inst.nextDangerColor, inst.dangerColor, inst.defaultDangerColor),
                    inst.dangerHue, inst.dangerSat, inst.dangerVal), inst.dangerOpacity);

                RTEffectsManager.inst.gradient.color1.Override(
                    LSColors.fadeColor(ChangeColorHSV(LerpColor(inst.prevGradientColor1, inst.nextGradientColor1, inst.gradientColor1, Color.black, new Color(0f, 0.8f, 0.56f, 0.5f)),
                    inst.gradientColor1Hue, inst.gradientColor1Sat, inst.gradientColor1Val), inst.gradientColor1Opacity));

                RTEffectsManager.inst.gradient.color2.Override(
                    LSColors.fadeColor(ChangeColorHSV(LerpColor(inst.prevGradientColor2, inst.nextGradientColor2, inst.gradientColor2, Color.black, new Color(0.81f, 0.37f, 1f, 0.5f)),
                    inst.gradientColor2Hue, inst.gradientColor2Sat, inst.gradientColor2Val), inst.gradientColor2Opacity));

                #endregion

                #region Updates

                bool allowFX = EventsConfig.Instance.ShowFX.Value;

                if (!float.IsNaN(EventManager.inst.camChroma))
                    LSEffectsManager.inst.UpdateChroma(!allowFX ? 0f : EventManager.inst.camChroma);
                if (!float.IsNaN(EventManager.inst.camBloom))
                    LSEffectsManager.inst.UpdateBloom(!allowFX ? 0f : EventManager.inst.camBloom);
                if (!float.IsNaN(EventManager.inst.vignetteIntensity))
                    LSEffectsManager.inst.UpdateVignette(!allowFX ? 0f : EventManager.inst.vignetteIntensity, EventManager.inst.vignetteSmoothness, Mathf.RoundToInt(EventManager.inst.vignetteRounded) == 1, EventManager.inst.vignetteRoundness, EventManager.inst.vignetteCenter);
                if (!float.IsNaN(EventManager.inst.lensDistortIntensity))
                    LSEffectsManager.inst.UpdateLensDistort(!allowFX ? 0f : EventManager.inst.lensDistortIntensity);
                if (!float.IsNaN(EventManager.inst.grainIntensity))
                    LSEffectsManager.inst.UpdateGrain(!allowFX ? 0f : EventManager.inst.grainIntensity, Mathf.RoundToInt(EventManager.inst.grainColored) == 1, EventManager.inst.grainSize);
                if (!float.IsNaN(inst.pixel))
                    LSEffectsManager.inst.pixelize.amount.Override(!allowFX ? 0f : inst.pixel);

                inst.analogGlitch.enabled = allowFX && inst.analogGlitchEnabled;
                inst.digitalGlitch.enabled = allowFX;

                // trying to figure out colorgrading issues... might be OS dependant?
                if (float.IsNaN(inst.colorGradingHueShift))
                {
                    CoreHelper.LogError($"ColorGrading Hueshift was not a number!");
                    inst.colorGradingHueShift = 0f;
                }

                if (float.IsNaN(inst.colorGradingContrast))
                {
                    CoreHelper.LogError($"ColorGrading Contrast was not a number!");
                    inst.colorGradingContrast = 0f;
                }

                if (float.IsNaN(inst.colorGradingSaturation))
                {
                    CoreHelper.LogError($"ColorGrading Saturation was not a number!");
                    inst.colorGradingSaturation = 0f;
                }

                if (float.IsNaN(inst.colorGradingTint))
                {
                    CoreHelper.LogError($"ColorGrading Tint was not a number!");
                    inst.colorGradingTint = 0f;
                }

                //New effects
                RTEffectsManager.inst.UpdateColorGrading(
                    !allowFX ? 0f : inst.colorGradingHueShift,
                    !allowFX ? 0f : inst.colorGradingContrast,
                    !allowFX ? new Vector4(1f, 1f, 1f, 0f) : inst.colorGradingGamma,
                    !allowFX ? 0f : inst.colorGradingSaturation,
                    !allowFX ? 0f : inst.colorGradingTemperature,
                    !allowFX ? 0f : inst.colorGradingTint);

                if (!float.IsNaN(inst.gradientIntensity))
                    RTEffectsManager.inst.UpdateGradient(!allowFX ? 0f : inst.gradientIntensity, inst.gradientRotation);
                if (!float.IsNaN(inst.ripplesStrength))
                    RTEffectsManager.inst.UpdateRipples(!allowFX ? 0f : inst.ripplesStrength, inst.ripplesSpeed, inst.ripplesDistance, inst.ripplesHeight, inst.ripplesWidth, inst.ripplesMode);
                if (!float.IsNaN(inst.doubleVision))
                    RTEffectsManager.inst.UpdateDoubleVision(!allowFX ? 0f : inst.doubleVision, inst.doubleVisionMode);
                if (!float.IsNaN(inst.radialBlurIntensity))
                    RTEffectsManager.inst.UpdateRadialBlur(!allowFX ? 0f : inst.radialBlurIntensity, inst.radialBlurIterations);
                if (!float.IsNaN(inst.scanLinesIntensity))
                    RTEffectsManager.inst.UpdateScanlines(!allowFX ? 0f : inst.scanLinesIntensity, inst.scanLinesAmount, inst.scanLinesSpeed);
                if (!float.IsNaN(inst.sharpen))
                    RTEffectsManager.inst.UpdateSharpen(!allowFX ? 0f : inst.sharpen);
                if (!float.IsNaN(inst.colorSplitOffset))
                    RTEffectsManager.inst.UpdateColorSplit(!allowFX ? 0f : inst.colorSplitOffset, inst.colorSplitMode);
                if (!float.IsNaN(inst.dangerIntensity))
                    RTEffectsManager.inst.UpdateDanger(!allowFX ? 0f : inst.dangerIntensity, inst.dangerColorResult, inst.dangerSize);
                if (!float.IsNaN(inst.invertAmount))
                    RTEffectsManager.inst.UpdateInvert(!allowFX ? 0f : inst.invertAmount);
                if (!float.IsNaN(inst.blackBarsIntensity))
                    RTEffectsManager.inst.UpdateBlackBars(!allowFX ? 0f : inst.blackBarsIntensity, !allowFX ? 0f : inst.blackBarsMode);

                if (!float.IsNaN(inst.mosaicIntensity))
                    RTEffectsManager.inst.UpdateMosaic(!allowFX ? 0f : inst.mosaicIntensity);

                if (!float.IsNaN(inst.timelineRot))
                {
                    GameManager.inst.timeline.transform.localPosition = new Vector3(inst.timelinePos.x, inst.timelinePos.y, 0f);
                    GameManager.inst.timeline.transform.localScale = new Vector3(inst.timelineSca.x, inst.timelineSca.y, 1f);
                    GameManager.inst.timeline.transform.eulerAngles = new Vector3(0f, 0f, inst.timelineRot);
                }

                RTGameManager.inst.extraBG.localPosition = inst.videoBGParentPos;
                RTGameManager.inst.extraBG.localScale = inst.videoBGParentSca;
                RTGameManager.inst.extraBG.localRotation = Quaternion.Euler(inst.videoBGParentRot);

                RTGameManager.inst.video.localPosition = inst.videoBGPos;
                RTGameManager.inst.video.localScale = inst.videoBGSca;
                RTGameManager.inst.video.localRotation = Quaternion.Euler(inst.videoBGRot);
                RTGameManager.inst.video.gameObject.layer = inst.videoBGRenderLayer == 0 ? 9 : 8;

                var screenScale = (float)Display.main.systemWidth / 1920f;
                if (inst.allowWindowPositioning && CoreHelper.InEditorPreview)
                {
                    if (!inst.setWindow)
                    {
                        inst.setWindow = true;
                        var res = DataManager.inst.resolutions[(int)CoreConfig.Instance.Resolution.Value];

                        WindowController.SetResolution((int)res.x, (int)res.y, false);
                    }

                    WindowController.SetWindowPos(
                        WindowController.WindowHandle, 0, (int)(inst.windowPosition.x * screenScale) + WindowController.WindowCenter.x, -(int)(inst.windowPosition.y * screenScale) + WindowController.WindowCenter.y,
                        inst.forceWindow ? (int)(inst.windowResolution.x * screenScale) : 0, inst.forceWindow ? (int)(inst.windowResolution.y * screenScale) : 0, inst.forceWindow ? 0 : 1);
                    inst.windowHasChanged = true;
                    windowPositionResolutionChanged = true;
                }

                if (inst.forceWindow && !inst.allowWindowPositioning && CoreHelper.InEditorPreview)
                {
                    inst.setWindow = true;
                    WindowController.SetResolution((int)(inst.windowResolution.x * screenScale), (int)(inst.windowResolution.y * screenScale), false);
                    inst.windowHasChanged = true;
                    windowPositionResolutionChanged = true;
                }

                if (CoreHelper.InEditor && EditorManager.inst.isEditing)
                    inst.setWindow = false;

                if (!inst.forceWindow && inst.setWindow)
                {
                    inst.setWindow = false;
                    WindowController.ResetResolution(false);
                }

                foreach (var customPlayer in PlayerManager.Players)
                {
                    if (customPlayer.Player && customPlayer.Player.rb)
                    {
                        var player = customPlayer.Player.rb.transform;
                        if (!inst.playersCanMove)
                        {
                            player.localPosition = new Vector3(inst.playerPositionX, inst.playerPositionY, 0f);
                            player.localRotation = Quaternion.Euler(0f, 0f, inst.playerRotation);
                        }
                    }
                }

                RTPlayer.PlayerForce = new Vector2(inst.playerForceX, inst.playerForceY) * playerForceMultiplier;

                #endregion
            }

            GameManager.inst.timeline.SetActive(!EventsConfig.Instance.HideTimeline.Value && inst.timelineActive && EventsConfig.Instance.ShowGUI.Value);
            EventManager.inst.prevCamZoom = EventManager.inst.camZoom;
        }

        public static float playerForceMultiplier = 0.036f;

        void FixedUpdate()
        {
            if (delayTracker.leader == null && InputDataManager.inst.players.Count > 0 && GameManager.inst.players.transform.Find("Player 1/Player"))
            {
                delayTracker.leader = GameManager.inst.players.transform.Find("Player 1/Player");
            }
        }

        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            double num;
            double saturation;
            double value;
            LSColors.ColorToHSV(color, out num, out saturation, out value);
            return LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        #region Lerp Colors

        void FindColors()
        {
            var allEvents = GameData.Current.events;
            var time = currentTime;

            FindColor(time, allEvents, ref prevTheme, ref nextTheme, 4, 0);
            FindColor(time, allEvents, ref prevBloomColor, ref nextBloomColor, 6, 4);
            FindColor(time, allEvents, ref prevVignetteColor, ref nextVignetteColor, 7, 6);
            FindColor(time, allEvents, ref prevGradientColor1, ref nextGradientColor1, ref prevGradientColor2, ref nextGradientColor2, 15, 2, 3);
            FindColor(time, allEvents, ref prevBGColor, ref nextBGColor, 20, 0);
            FindColor(time, allEvents, ref prevTimelineColor, ref nextTimelineColor, 22, 6);
            FindColor(time, allEvents, ref prevDangerColor, ref nextDangerColor, 30, 2);
        }

        void FindColor(float time, List<List<EventKeyframe>> allEvents, ref int prev, ref int next, int type, int valueIndex)
        {
            if (allEvents.Count <= type || allEvents[type].IsEmpty())
                return;

            var nextKFIndex = allEvents[type].FindLastIndex(x => x.time <= time) + 1;
            if (nextKFIndex < allEvents[type].Count)
            {
                var nextKF = allEvents[type][nextKFIndex];
                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].values.Length > valueIndex)
                    prev = (int)allEvents[type][nextKFIndex - 1].values[valueIndex];
                else if (allEvents[type][0].values.Length > valueIndex)
                    prev = (int)allEvents[type][0].values[valueIndex];

                if (nextKF.values.Length > valueIndex)
                    next = (int)nextKF.values[valueIndex];

                return;
            }

            var finalKF = allEvents[type][allEvents[type].Count - 1];

            int a = allEvents[type].Count - 2;
            if (a < 0)
                a = 0;

            if (allEvents[type][a].values.Length > valueIndex)
                prev = (int)allEvents[type][a].values[valueIndex];

            if (finalKF.values.Length > valueIndex)
                next = (int)finalKF.values[valueIndex];
        }

        void FindColor(float time, List<List<EventKeyframe>> allEvents, ref int prev1, ref int next1, ref int prev2, ref int next2, int type, int valueIndex1, int valueIndex2)
        {
            if (allEvents.Count <= type || allEvents[type].IsEmpty())
                return;

            var nextKFIndex = allEvents[type].FindLastIndex(x => x.time <= time) + 1;
            if (nextKFIndex < allEvents[type].Count)
            {
                var nextKF = allEvents[type][nextKFIndex];
                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].values.Length > valueIndex1)
                    prev1 = (int)allEvents[type][nextKFIndex - 1].values[valueIndex1];
                else if (allEvents[type][0].values.Length > valueIndex1)
                    prev1 = (int)allEvents[type][0].values[valueIndex1];

                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].values.Length > valueIndex2)
                    prev2 = (int)allEvents[type][nextKFIndex - 1].values[valueIndex2];
                else if (allEvents[type][0].values.Length > valueIndex2)
                    prev2 = (int)allEvents[type][0].values[valueIndex2];

                if (nextKF.values.Length > valueIndex1)
                    next1 = (int)nextKF.values[valueIndex1];

                if (nextKF.values.Length > valueIndex2)
                    next2 = (int)nextKF.values[valueIndex2];

                return;
            }

            var finalKF = allEvents[type][allEvents[type].Count - 1];

            int a = allEvents[type].Count - 2;
            if (a < 0)
                a = 0;

            if (allEvents[type][a].values.Length > valueIndex1)
                prev1 = (int)allEvents[type][a].values[valueIndex1];
            if (allEvents[type][a].values.Length > valueIndex2)
                prev2 = (int)allEvents[type][a].values[valueIndex2];

            if (finalKF.values.Length > valueIndex1)
                next1 = (int)finalKF.values[valueIndex1];
            if (finalKF.values.Length > valueIndex2)
                next2 = (int)finalKF.values[valueIndex2];
        }

        static Color LerpColor(int prev, int next, float t, Color defaultColor)
        {
            Color prevColor = CoreHelper.CurrentBeatmapTheme.effectColors.Count > prev && prev > -1 ? CoreHelper.CurrentBeatmapTheme.effectColors[prev] : defaultColor;
            Color nextColor = CoreHelper.CurrentBeatmapTheme.effectColors.Count > next && next > -1 ? CoreHelper.CurrentBeatmapTheme.effectColors[next] : defaultColor;

            if (float.IsNaN(t) || t < 0f)
                t = 0f;

            return Color.Lerp(prevColor, nextColor, t);
        }

        static Color LerpColor(int prev, int next, float t, Color defaultColor, Color defaultColor2)
        {
            Color prevColor = CoreHelper.CurrentBeatmapTheme.effectColors.Count > prev && prev > -1 ? CoreHelper.CurrentBeatmapTheme.effectColors[prev] : prev == 19 ? defaultColor : defaultColor2;
            Color nextColor = CoreHelper.CurrentBeatmapTheme.effectColors.Count > next && next > -1 ? CoreHelper.CurrentBeatmapTheme.effectColors[next] : prev == 19 ? defaultColor : defaultColor2;

            if (float.IsNaN(t) || t < 0f)
                t = 0f;

            return Color.Lerp(prevColor, nextColor, t);
        }

        public Color dangerColorResult;

        public Color defaultDangerColor = new Color(0.66f, 0f, 0f);

        #endregion

        #region Update Methods

        public void UpdateEvents(int currentEvent)
        {
            SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
        }

        public IEnumerator UpdateEvents()
        {
            SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
            DOTween.Kill(false);

            yield break;
        }

        public void UpdateShake()
        {
            var vector = EventManager.inst.shakeVector * EventManager.inst.shakeMultiplier;
            vector.x *= shakeX == 0f && shakeY == 0f ? 1f : shakeX;
            vector.y *= shakeX == 0f && shakeY == 0f ? 1f : shakeY;
            vector.z = 0f;

            if (float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z))
                vector = Vector3.zero;

            if (!float.IsNaN(camOffsetX) && !float.IsNaN(camOffsetY))
                EventManager.inst.camParent.transform.localPosition = vector + new Vector3(camOffsetX, camOffsetY, 0f);
        }

        #region Move - 0

        // 0 - 0
        public static void UpdateCameraPositionX(float x) => EventManager.inst.camPos.x = x;
        // 0 - 1
        public static void UpdateCameraPositionY(float x) => EventManager.inst.camPos.y = x;

        #endregion

        #region Zoom - 1

        // 1 - 0
        public static void UpdateCameraZoom(float x) => EventManager.inst.camZoom = x;

        #endregion

        #region Rotate - 2

        // 2 - 0
        public static void UpdateCameraRotation(float x) => EventManager.inst.camRot = x;

        #endregion

        #region Shake - 3

        // 3 - 0
        public static void UpdateCameraShakeMultiplier(float x) => EventManager.inst.shakeMultiplier = x;

        // 3 - 1
        public static void UpdateCameraShakeX(float x) => inst.shakeX = x;

        // 3 - 2
        public static void UpdateCameraShakeY(float x) => inst.shakeY = x;

        // 3 - 3
        public static void UpdateCameraShakeSmoothness(float x) => inst.shakeSmoothness = x;

        // 3 - 4
        public static void UpdateCameraShakeSpeed(float x) => inst.shakeSpeed = x;

        #endregion

        #region Theme - 4

        // 4 - 0
        public static void UpdateTheme(float x)
        {
            inst.themeLerp = x;

            ThemeManager.inst.Current.Lerp(ThemeManager.inst.GetTheme(inst.prevTheme), ThemeManager.inst.GetTheme(inst.nextTheme), x);
            ThemeManager.inst.UpdateThemes();
        }

        #endregion

        #region Chroma - 5

        // 5 - 0
        public static void UpdateCameraChromatic(float x) => EventManager.inst.camChroma = x;

        #endregion

        #region Bloom - 6

        // 6 - 0
        public static void UpdateCameraBloom(float x) => EventManager.inst.camBloom = x;

        // 6 - 1
        public static void UpdateCameraBloomDiffusion(float x) => LSEffectsManager.inst.bloom.diffusion.Override(x);

        // 6 - 2
        public static void UpdateCameraBloomThreshold(float x) => LSEffectsManager.inst.bloom.threshold.Override(x);

        // 6 - 3
        public static void UpdateCameraBloomAnamorphicRatio(float x) => LSEffectsManager.inst.bloom.anamorphicRatio.Override(x);

        // 6 - 4
        public static void UpdateCameraBloomColor(float x) => inst.bloomColor = x;

        // 6 - 5
        public static void UpdateCameraBloomHue(float x) => inst.bloomHue = x;

        // 6 - 6
        public static void UpdateCameraBloomSat(float x) => inst.bloomSat = x;

        // 6 - 7
        public static void UpdateCameraBloomVal(float x) => inst.bloomVal = x;

        #endregion

        #region Vignette - 7

        // 7 - 0
        public static void UpdateCameraVignette(float x) => EventManager.inst.vignetteIntensity = x;

        // 7 - 1
        public static void UpdateCameraVignetteSmoothness(float x) => EventManager.inst.vignetteSmoothness = x;

        // 7 - 2
        public static void UpdateCameraVignetteRounded(float x) => EventManager.inst.vignetteRounded = x;

        // 7 - 3
        public static void UpdateCameraVignetteRoundness(float x) => EventManager.inst.vignetteRoundness = x;

        // 7 - 4
        public static void UpdateCameraVignetteCenterX(float x) => EventManager.inst.vignetteCenter.x = x;

        // 7 - 5
        public static void UpdateCameraVignetteCenterY(float x) => EventManager.inst.vignetteCenter.y = x;

        // 7 - 6
        public static void UpdateCameraVignetteColor(float x) => inst.vignetteColor = x;

        // 7 - 7
        public static void UpdateCameraVignetteHue(float x) => inst.vignetteHue = x;

        // 7 - 8
        public static void UpdateCameraVignetteSat(float x) => inst.vignetteSat = x;

        // 7 - 9
        public static void UpdateCameraVignetteVal(float x) => inst.vignetteVal = x;

        #endregion

        #region Lens - 8

        // 8 - 0
        public static void UpdateCameraLens(float x) => EventManager.inst.lensDistortIntensity = x;

        // 8 - 1
        public static void UpdateCameraLensCenterX(float x) => LSEffectsManager.inst.lensDistort.centerX.Override(x);

        // 8 - 2
        public static void UpdateCameraLensCenterY(float x) => LSEffectsManager.inst.lensDistort.centerY.Override(x);

        // 8 - 3
        public static void UpdateCameraLensIntensityX(float x) => LSEffectsManager.inst.lensDistort.intensityX.Override(x);

        // 8 - 4
        public static void UpdateCameraLensIntensityY(float x) => LSEffectsManager.inst.lensDistort.intensityY.Override(x);

        // 8 - 5
        public static void UpdateCameraLensScale(float x) => LSEffectsManager.inst.lensDistort.scale.Override(x);

        #endregion

        #region Grain - 9

        // 9 - 0
        public static void UpdateCameraGrain(float x) => EventManager.inst.grainIntensity = x;

        // 9 - 1
        public static void UpdateCameraGrainColored(float _colored) => EventManager.inst.grainColored = _colored;

        // 9 - 2
        public static void UpdateCameraGrainSize(float x) => EventManager.inst.grainSize = x;

        #endregion

        #region ColorGrading - 10

        // 10 - 0
        public static void UpdateCameraHueShift(float x) => inst.colorGradingHueShift = x;

        // 10 - 1
        public static void UpdateCameraContrast(float x) => inst.colorGradingContrast = x;

        // 10 - 2
        public static void UpdateCameraGammaX(float x) => inst.colorGradingGamma.x = x;

        // 10 - 3
        public static void UpdateCameraGammaY(float x) => inst.colorGradingGamma.y = x;

        // 10 - 4
        public static void UpdateCameraGammaZ(float x) => inst.colorGradingGamma.z = x;

        // 10 - 5
        public static void UpdateCameraGammaW(float x) => inst.colorGradingGamma.w = x;

        // 10 - 6
        public static void UpdateCameraSaturation(float x) => inst.colorGradingSaturation = x;

        // 10 - 7
        public static void UpdateCameraTemperature(float x) => inst.colorGradingTemperature = x;

        // 10 - 8
        public static void UpdateCameraTint(float x) => inst.colorGradingTint = x;

        #endregion

        #region Ripples - 11

        // 11 - 0
        public static void UpdateCameraRipplesStrength(float x) => inst.ripplesStrength = x;

        // 11 - 1
        public static void UpdateCameraRipplesSpeed(float x) => inst.ripplesSpeed = x;

        // 11 - 2
        public static void UpdateCameraRipplesDistance(float x) => inst.ripplesDistance = Mathf.Clamp(x, 0.0001f, float.PositiveInfinity);

        // 11 - 3
        public static void UpdateCameraRipplesHeight(float x) => inst.ripplesHeight = x;

        // 11 - 4
        public static void UpdateCameraRipplesWidth(float x) => inst.ripplesWidth = x;
        
        // 11 - 5
        public static void UpdateCameraRipplesMode(float x) => inst.ripplesMode = (int)x;

        #endregion

        #region RadialBlur - 12

        // 12 - 0
        public static void UpdateCameraRadialBlurIntensity(float x) => inst.radialBlurIntensity = x;

        // 12 - 1
        public static void UpdateCameraRadialBlurIterations(float x) => inst.radialBlurIterations = Mathf.Clamp((int)x, 1, 30);

        #endregion

        #region ColorSplit - 13

        // 13 - 0
        public static void UpdateCameraColorSplit(float x) => inst.colorSplitOffset = x;

        // 13 - 1
        public static void UpdateCameraColorSplitMode(float x) => inst.colorSplitMode = (int)x;

        #endregion

        #region Offset - 14

        // 14 - 0
        public static void UpdateCameraOffsetX(float x) => inst.camOffsetX = x;

        // 14 - 1
        public static void UpdateCameraOffsetY(float x) => inst.camOffsetY = x;

        #endregion

        #region Gradient - 15

        // 15 - 0
        public static void UpdateCameraGradientIntensity(float x) => inst.gradientIntensity = x;

        // 15 - 1
        public static void UpdateCameraGradientRotation(float x) => inst.gradientRotation = x;

        // 15 - 2
        public static void UpdateCameraGradientColor1(float x) => inst.gradientColor1 = x;

        // 15 - 3
        public static void UpdateCameraGradientColor2(float x) => inst.gradientColor2 = x;

        // 15 - 4
        public static void UpdateCameraGradientMode(float x) => RTEffectsManager.inst.gradient.blendMode.Override((SCPE.Gradient.BlendMode)(int)x);

        // 15 - 5
        public static void UpdateCameraGradientColor1Opacity(float x) => inst.gradientColor1Opacity = x;

        // 15 - 6
        public static void UpdateCameraGradientColor1Hue(float x) => inst.gradientColor1Hue = x;

        // 15 - 7
        public static void UpdateCameraGradientColor1Sat(float x) => inst.gradientColor1Sat = x;

        // 15 - 8
        public static void UpdateCameraGradientColor1Val(float x) => inst.gradientColor1Val = x;

        // 15 - 5
        public static void UpdateCameraGradientColor2Opacity(float x) => inst.gradientColor2Opacity = x;

        // 15 - 6
        public static void UpdateCameraGradientColor2Hue(float x) => inst.gradientColor2Hue = x;

        // 15 - 7
        public static void UpdateCameraGradientColor2Sat(float x) => inst.gradientColor2Sat = x;

        // 15 - 8
        public static void UpdateCameraGradientColor2Val(float x) => inst.gradientColor2Val = x;

        #endregion

        #region DoubleVision - 16

        // 16 - 0
        public static void UpdateCameraDoubleVision(float x) => inst.doubleVision = x;

        // 16 - 1
        public static void UpdateCameraDoubleVisionMode(float x) => inst.doubleVisionMode = (int)x;

        #endregion

        #region ScanLines - 17

        // 17 - 0
        public static void UpdateCameraScanLinesIntensity(float x) => inst.scanLinesIntensity = x;

        // 17 - 1
        public static void UpdateCameraScanLinesAmount(float x) => inst.scanLinesAmount = x;

        // 17 - 2
        public static void UpdateCameraScanLinesSpeed(float x) => inst.scanLinesSpeed = x;

        #endregion

        #region Blur - 18

        // 18 - 0
        public static void UpdateCameraBlurAmount(float x) => LSEffectsManager.inst.blur.amount.Override(!EventsConfig.Instance.ShowFX.Value ? 0f : x);

        // 18 - 1
        public static void UpdateCameraBlurIterations(float x) => LSEffectsManager.inst.blur.iterations.Override(Mathf.Clamp((int)x, 1, 30));

        #endregion

        #region Pixelize - 19

        // 19 - 0
        public static void UpdateCameraPixelize(float x) => inst.pixel = Mathf.Clamp(x, 0f, 0.99999f);

        #endregion

        #region BG - 20

        // 20 - 0
        public static void UpdateCameraBGColor(float x) => inst.bgColor = x;

        // 20 - 1
        public static void UpdateCameraBGActive(float x)
        {
            inst.bgActive = x;

            var i = (int)x;

            BackgroundManager.inst?.backgroundParent?.gameObject?.SetActive(i == 0);
        }

        public float bgActive = 0f;

        // 20 - 2
        public static void UpdateCameraBGHue(float x) => inst.bgHue = x;

        // 20 - 3
        public static void UpdateCameraBGSat(float x) => inst.bgSat = x;

        // 20 - 4
        public static void UpdateCameraBGVal(float x) => inst.bgVal = x;

        #endregion

        #region Invert - 21

        // 21 - 0
        public static void UpdateCameraInvert(float x) => inst.invertAmount = x;

        #endregion

        #region Timeline - 22

        // 22 - 0
        public static void UpdateTimelineActive(float x) => inst.timelineActive = (int)x == 0;

        // 22 - 1
        public static void UpdateTimelinePosX(float x) => inst.timelinePos.x = x;

        // 22 - 2
        public static void UpdateTimelinePosY(float x) => inst.timelinePos.y = x;

        // 22 - 3
        public static void UpdateTimelineScaX(float x) => inst.timelineSca.x = x;

        // 22 - 4
        public static void UpdateTimelineScaY(float x) => inst.timelineSca.y = x;

        // 22 - 5
        public static void UpdateTimelineRot(float x) => inst.timelineRot = x;

        // 22 - 6
        public static void UpdateTimelineColor(float x) => inst.timelineColor = x;

        // 22 - 7
        public static void UpdateTimelineOpacity(float x) => inst.timelineOpacity = x;

        // 22 - 8
        public static void UpdateTimelineHue(float x) => inst.timelineHue = x;

        // 22 - 9
        public static void UpdateTimelineSat(float x) => inst.timelineSat = x;

        // 22 - 10
        public static void UpdateTimelineVal(float x) => inst.timelineVal = x;

        #endregion

        #region Player - 23

        // 23 - 0
        public static void UpdatePlayerActive(float x)
        {
            var active = (int)x == 0;

            var zen = !CoreHelper.InStory && (PlayerManager.IsZenMode || CoreHelper.InEditor);

            var a = active && !zen || active && EventsConfig.Instance.ShowGUI.Value;

            if (GameManager.inst.gameState == (GameManager.State.Paused | GameManager.State.Finish) && LevelManager.LevelEnded)
                a = false;

            GameManager.inst.players.SetActive(a);
            inst.playersActive = a;
        }

        // 23 - 1
        public static void UpdatePlayerMoveable(float x)
        {
            inst.playersCanMove = (int)x == 0;
            foreach (var customPlayer in PlayerManager.Players)
            {
                if (customPlayer.Player)
                {
                    customPlayer.Player.CanMove = inst.playersCanMove;
                    customPlayer.Player.CanRotate = inst.playersCanMove;
                }
            }
        }

        // 23 - 2
        public static void UpdatePlayerPositionX(float x) => inst.playerPositionX = x;

        // 23 - 3
        public static void UpdatePlayerPositionY(float x) => inst.playerPositionY = x;

        // 23 - 4
        public static void UpdatePlayerRotation(float x) => inst.playerRotation = x;

        // 23 - 5
        public static void UpdatePlayerOoB(float x) => RTPlayer.OutOfBounds = x == 1f;

        #endregion

        #region Camera Follows Player - 24

        // 24 - 0
        public static void UpdateDelayTrackerActive(float x) => inst.delayTracker.active = (int)x == 1;

        // 24 - 1
        public static void UpdateDelayTrackerMove(float x) => inst.delayTracker.move = (int)x == 1;

        // 24 - 2
        public static void UpdateDelayTrackerRotate(float x) => inst.delayTracker.rotate = (int)x == 1;

        // 24 - 3
        public static void UpdateDelayTrackerSharpness(float x) => inst.delayTracker.followSharpness = Mathf.Clamp(x, 0.001f, 1f);

        // 24 - 4
        public static void UpdateDelayTrackerOffset(float x) => inst.delayTracker.offset = x;

        // 24 - 5
        public static void UpdateDelayTrackerLimitLeft(float x) => inst.delayTracker.limitLeft = x;

        // 24 - 6
        public static void UpdateDelayTrackerLimitRight(float x) => inst.delayTracker.limitRight = x;

        // 24 - 7
        public static void UpdateDelayTrackerLimitUp(float x) => inst.delayTracker.limitUp = x;

        // 24 - 8
        public static void UpdateDelayTrackerLimitDown(float x) => inst.delayTracker.limitDown = x;

        // 24 - 9
        public static void UpdateDelayTrackerAnchor(float x) => inst.delayTracker.anchor = x;

        #endregion

        #region Audio - 25

        // 25 - 0
        public static void UpdateAudioPitch(float x) => AudioManager.inst.pitch = Mathf.Clamp(x, 0.001f, 10f) * CoreHelper.Pitch * inst.pitchOffset;

        // 25 - 1
        public static void UpdateAudioVolume(float x) => inst.audioVolume = Mathf.Clamp(x, 0f, 1f);

        #endregion

        #region Video BG Parent - 26

        // 26 - 0
        public static void UpdateVideoBGParentPositionX(float x) => inst.videoBGParentPos.x = x;
        // 26 - 1
        public static void UpdateVideoBGParentPositionY(float x) => inst.videoBGParentPos.y = x;
        // 26 - 2
        public static void UpdateVideoBGParentPositionZ(float x) => inst.videoBGParentPos.z = x;
        // 26 - 3
        public static void UpdateVideoBGParentScaleX(float x) => inst.videoBGParentSca.x = x;
        // 26 - 4
        public static void UpdateVideoBGParentScaleY(float x) => inst.videoBGParentSca.y = x;
        // 26 - 5
        public static void UpdateVideoBGParentScaleZ(float x) => inst.videoBGParentSca.z = x;
        // 26 - 6
        public static void UpdateVideoBGParentRotationX(float x) => inst.videoBGParentRot.x = x;
        // 26 - 7
        public static void UpdateVideoBGParentRotationY(float x) => inst.videoBGParentRot.y = x;
        // 26 - 8
        public static void UpdateVideoBGParentRotationZ(float x) => inst.videoBGParentRot.z = x;

        #endregion

        #region Video BG - 27

        // 27 - 0
        public static void UpdateVideoBGPositionX(float x) => inst.videoBGPos.x = x;
        // 27 - 1
        public static void UpdateVideoBGPositionY(float x) => inst.videoBGPos.y = x;
        // 27 - 2
        public static void UpdateVideoBGPositionZ(float x) => inst.videoBGPos.z = x;
        // 27 - 3
        public static void UpdateVideoBGScaleX(float x) => inst.videoBGSca.x = x;
        // 27 - 4
        public static void UpdateVideoBGScaleY(float x) => inst.videoBGSca.y = x;
        // 27 - 5
        public static void UpdateVideoBGScaleZ(float x) => inst.videoBGSca.z = x;
        // 27 - 6
        public static void UpdateVideoBGRotationX(float x) => inst.videoBGRot.x = x;
        // 27 - 7
        public static void UpdateVideoBGRotationY(float x) => inst.videoBGRot.y = x;
        // 27 - 8
        public static void UpdateVideoBGRotationZ(float x) => inst.videoBGRot.z = x;
        // 27 - 9
        public static void UpdateVideoBGRenderLayer(float x) => inst.videoBGRenderLayer = (int)x;

        #endregion

        #region Sharpen - 28

        // 28 - 0
        public static void UpdateSharpen(float x) => inst.sharpen = x;

        #endregion

        #region Bars - 29

        // 29 - 0
        public static void UpdateBlackBarsIntensity(float x) => inst.blackBarsIntensity = x;
        // 29 - 1
        public static void UpdateBlackBarsMode(float x) => inst.blackBarsMode = x;

        #endregion

        #region Danger - 30

        // 30 - 0
        public static void UpdateDangerIntensity(float x) => inst.dangerIntensity = x;
        // 30 - 1
        public static void UpdateDangerSize(float x) => inst.dangerSize = x;
        // 30 - 2
        public static void UpdateDangerColor(float x) => inst.dangerColor = x;

        // 30 - 3
        public static void UpdateCameraDangerOpacity(float x) => inst.dangerOpacity = x;

        // 30 - 4
        public static void UpdateCameraDangerHue(float x) => inst.dangerHue = x;

        // 30 - 5
        public static void UpdateCameraDangerSat(float x) => inst.dangerSat = x;

        // 30 - 6
        public static void UpdateCameraDangerVal(float x) => inst.dangerVal = x;

        #endregion

        #region 3D Rotation - 31

        // 31 - 0
        public static void UpdateCameraRotationX(float x) => inst.camRotOffset.x = x;
        // 31 - 1
        public static void UpdateCameraRotationY(float x) => inst.camRotOffset.y = x;

        #endregion

        #region Camera Depth - 32

        // 32 - 0
        public static void UpdateCameraDepth(float x) => inst.zPosition = x;
        // 32 - 1
        public static void UpdateCameraPerspectiveZoom(float x) => inst.perspectiveZoom = x;
        // 32 - 2
        public static void UpdateCameraPerspectiveGlobal(float x) => inst.bgGlobalPosition = x == 0f;
        // 32 - 3
        public static void UpdateCameraPerspectiveAlign(float x) => inst.bgAlignNearPlane = x == 1f;

        #endregion

        #region Window Base - 33

        // 33 - 0
        public static void UpdateWindowForceResolution(float x) => inst.forceWindow = (int)x == 1;
        // 33 - 1
        public static void UpdateWindowResolutionX(float x) => inst.windowResolution.x = x;
        // 33 - 2
        public static void UpdateWindowResolutionY(float x) => inst.windowResolution.y = x;
        // 33 - 3
        public static void UpdateWindowAllowPositioning(float x) => inst.allowWindowPositioning = (int)x == 1;

        #endregion

        #region Window Position X - 34

        // 34 - 0
        public static void UpdateWindowPositionX(float x) => inst.windowPosition.x = x;

        #endregion

        #region Window Position Y - 35

        // 35 - 0
        public static void UpdateWindowPositionY(float x) => inst.windowPosition.y = x;

        #endregion

        #region Player Force - 36

        // 36 - 0
        public static void UpdatePlayerForceX(float x) => inst.playerForceX = x;

        // 36 - 1
        public static void UpdatePlayerForceY(float x) => inst.playerForceY = x;

        #endregion

        #region Mosaic - 37

        // 37 - 0
        public static void UpdateCameraMosaic(float x) => inst.mosaicIntensity = x;

        #endregion

        #region Analog Glitch - 38

        // 38 - 0
        public static void UpdateAnalogGlitchEnabled(float x) => inst.analogGlitchEnabled = (int)x == 1;

        // 38 - 1
        public static void UpdateAnalogGlitchColorDrift(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.colorDrift = x;
        }

        // 38 - 2
        public static void UpdateAnalogGlitchHorizontalShake(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.horizontalShake = x;
        }

        // 38 - 3
        public static void UpdateAnalogGlitchScanLineJitter(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.scanLineJitter = x;
        }

        // 38 - 4
        public static void UpdateAnalogGlitchVerticalJump(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.verticalJump = x;
        }

        #endregion

        #region Digital Glitch - 39

        // 39 - 0
        public static void UpdateDigitalGlitchIntensity(float x)
        {
            if (inst.digitalGlitch)
                inst.digitalGlitch.intensity = x;
        }

        #endregion

        #endregion

        #region Variables

        public int prevTheme;
        public int nextTheme;

        public bool bgAlignNearPlane = true;

        public bool bgGlobalPosition = false;

        public bool analogGlitchEnabled;

        public float timelineVal;
        public float timelineSat;
        public float timelineHue;
        public float timelineOpacity = 1f;

        public float dangerVal;
        public float dangerSat;
        public float dangerHue;
        public float dangerOpacity = 1f;

        public float bgVal;
        public float bgSat;
        public float bgHue;

        public float gradientColor2Val;
        public float gradientColor2Sat;
        public float gradientColor2Hue;
        public float gradientColor2Opacity = 1f;

        public float gradientColor1Val;
        public float gradientColor1Sat;
        public float gradientColor1Hue;
        public float gradientColor1Opacity = 1f;

        public float vignetteVal;
        public float vignetteSat;
        public float vignetteHue;

        public float bloomVal;
        public float bloomSat;
        public float bloomHue;

        public float playerForceX = 0f;
        public float playerForceY = 0f;

        public float mosaicIntensity = 0f;

        bool setWindow = false;

        public bool allowWindowPositioning = false;

        public Vector2 windowResolution = Vector2.zero;
        public Vector2 windowPosition = Vector2.zero;

        public bool forceWindow = false;

        public float zPosition = -10f;

        public float perspectiveZoom = 1f;

        public Vector2 camRotOffset = Vector2.zero;

        public Vector3 videoBGParentPos;
        public Vector3 videoBGParentSca;
        public Vector3 videoBGParentRot;
        public Vector3 videoBGPos;
        public Vector3 videoBGSca;
        public Vector3 videoBGRot;
        public int videoBGRenderLayer;

        public float themeLerp;

        public float camOffsetX;
        public float camOffsetY;

        public float audioVolume = 1f;

        public float playerRotation;
        public float playerPositionX;
        public float playerPositionY;

        public bool playersCanMove = true;
        public bool playersActive = true;

        public bool timelineActive = true;
        public Vector2 timelinePos;
        public Vector2 timelineSca;
        public float timelineRot;
        public float timelineColor;
        public int prevTimelineColor = 18;
        public int nextTimelineColor = 18;

        public float bgColor;
        public int prevBGColor = 18;
        public int nextBGColor = 18;

        public float invertAmount;

        public float bloomColor;
        public int prevBloomColor = 18;
        public int nextBloomColor = 18;

        public float vignetteColor;
        public int prevVignetteColor = 18;
        public int nextVignetteColor = 18;

        public float shakeX;
        public float shakeY;

        public float pixel;

        public float colorGradingHueShift = 0.1f;
        public float colorGradingContrast = 0.1f;
        public Vector4 colorGradingGamma = new Vector4(1f, 1f, 1f, 0f);
        public float colorGradingSaturation = 0.1f;
        public float colorGradingTemperature = 0.1f;
        public float colorGradingTint = 0.1f;

        public float gradientIntensity;
        public float gradientColor1;
        public int prevGradientColor1;
        public int nextGradientColor1;
        public float gradientColor2;
        public int prevGradientColor2;
        public int nextGradientColor2;
        public float gradientRotation;

        public float doubleVision;
        public int doubleVisionMode;

        public float radialBlurIntensity;
        public int radialBlurIterations;

        public float scanLinesIntensity;
        public float scanLinesAmount;
        public float scanLinesSpeed;

        public float sharpen;

        public float colorSplitOffset;
        public int colorSplitMode;

        public float dangerIntensity;
        public float dangerColor;
        public int prevDangerColor;
        public int nextDangerColor;
        public float dangerSize;

        public float blackBarsIntensity;
        public float blackBarsMode;

        public float ripplesStrength;
        public float ripplesSpeed;
        public float ripplesDistance;
        public float ripplesHeight;
        public float ripplesWidth;
        public int ripplesMode;

        #endregion

        #region Editor Offsets

        public float EditorSpeed => EventsConfig.Instance.EditorCamSpeed.Value;

        private Vector2 editorOffset = Vector2.zero;
        public float editorZoom = 20f;
        public float editorRotate = 0f;
        public Vector2 editorPerRotate = Vector2.zero;

        #endregion

        #region Offsets

        public float pitchOffset = 1f;

        public void SetOffset(int eventType, int indexValue, float value)
        {
            if (offsets.InRange(eventType) && offsets[eventType].InRange(indexValue))
                offsets[eventType][indexValue] = value;
        }

        List<List<float>> ResetOffsets()
        {
            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            var list = new List<List<float>>();
            for (int i = 0; i < events.Length; i++)
            {
                list.Add(new List<float>());
                for (int j = 0; j < events[i].Length; j++)
                    list[i].Add(0f);
            }
            return list;
        }

        public List<List<float>> offsets;

        #endregion

        #region Delegates

        public delegate void KFDelegate(float t);

        public KFDelegate[][] events = new KFDelegate[40][]
        {
            new KFDelegate[]
            {
                UpdateCameraPositionX,
                UpdateCameraPositionY,
            }, // Move
            new KFDelegate[]
            {
                UpdateCameraZoom
            }, // Rotate
            new KFDelegate[]
            {
                UpdateCameraRotation
            }, // Zoom
            new KFDelegate[]
            {
                UpdateCameraShakeMultiplier,
                UpdateCameraShakeX,
                UpdateCameraShakeY,
                UpdateCameraShakeSmoothness,
                UpdateCameraShakeSpeed
            }, // Shake
            new KFDelegate[]
            {
                UpdateTheme
            }, // Theme
            new KFDelegate[]
            {
                UpdateCameraChromatic
            }, // Chroma
            new KFDelegate[]
            {
                UpdateCameraBloom,
                UpdateCameraBloomDiffusion,
                UpdateCameraBloomThreshold,
                UpdateCameraBloomAnamorphicRatio,
                UpdateCameraBloomColor,
                UpdateCameraBloomHue,
                UpdateCameraBloomSat,
                UpdateCameraBloomVal,
            }, // Bloom
            new KFDelegate[]
            {
                UpdateCameraVignette,
                UpdateCameraVignetteSmoothness,
                UpdateCameraVignetteRounded,
                UpdateCameraVignetteRoundness,
                UpdateCameraVignetteCenterX,
                UpdateCameraVignetteCenterY,
                UpdateCameraVignetteColor,
                UpdateCameraVignetteHue,
                UpdateCameraVignetteSat,
                UpdateCameraVignetteVal,
            }, // Vignette
            new KFDelegate[]
            {
                UpdateCameraLens,
                UpdateCameraLensCenterX,
                UpdateCameraLensCenterY,
                UpdateCameraLensIntensityX,
                UpdateCameraLensIntensityY,
                UpdateCameraLensScale
            }, // Lens
            new KFDelegate[]
            {
                UpdateCameraGrain,
                UpdateCameraGrainColored,
                UpdateCameraGrainSize
            }, // Grain
            new KFDelegate[]
            {
                UpdateCameraHueShift,
                UpdateCameraContrast,
                UpdateCameraGammaX,
                UpdateCameraGammaY,
                UpdateCameraGammaZ,
                UpdateCameraGammaW,
                UpdateCameraSaturation,
                UpdateCameraTemperature,
                UpdateCameraTint
            }, // ColorGrading
            new KFDelegate[]
            {
                UpdateCameraRipplesStrength,
                UpdateCameraRipplesSpeed,
                UpdateCameraRipplesDistance,
                UpdateCameraRipplesHeight,
                UpdateCameraRipplesWidth,
                UpdateCameraRipplesMode
            }, // Ripples
            new KFDelegate[]
            {
                UpdateCameraRadialBlurIntensity,
                UpdateCameraRadialBlurIterations
            }, // RadialBlur
            new KFDelegate[]
            {
                UpdateCameraColorSplit,
                UpdateCameraColorSplitMode
            }, // ColorSplit
            new KFDelegate[]
            {
                UpdateCameraOffsetX,
                UpdateCameraOffsetY
            }, // Offset
            new KFDelegate[]
            {
                UpdateCameraGradientIntensity,
                UpdateCameraGradientRotation,
                UpdateCameraGradientColor1,
                UpdateCameraGradientColor2,
                UpdateCameraGradientMode,
                UpdateCameraGradientColor1Opacity,
                UpdateCameraGradientColor1Hue,
                UpdateCameraGradientColor1Sat,
                UpdateCameraGradientColor1Val,
                UpdateCameraGradientColor2Opacity,
                UpdateCameraGradientColor2Hue,
                UpdateCameraGradientColor2Sat,
                UpdateCameraGradientColor2Val,
            }, // Gradient
            new KFDelegate[]
            {
                UpdateCameraDoubleVision,
                UpdateCameraDoubleVisionMode
            }, // DoubleVision
            new KFDelegate[]
            {
                UpdateCameraScanLinesIntensity,
                UpdateCameraScanLinesAmount,
                UpdateCameraScanLinesSpeed
            }, // ScanLines
            new KFDelegate[]
            {
                UpdateCameraBlurAmount,
                UpdateCameraBlurIterations
            }, // Blur
            new KFDelegate[]
            {
                UpdateCameraPixelize
            }, // Pixelize
            new KFDelegate[]
            {
                UpdateCameraBGColor,
                UpdateCameraBGActive,
                UpdateCameraBGHue,
                UpdateCameraBGSat,
                UpdateCameraBGVal,
            }, // BG
            new KFDelegate[]
            {
                UpdateCameraInvert
            }, // Invert
            new KFDelegate[]
            {
                UpdateTimelineActive,
                UpdateTimelinePosX,
                UpdateTimelinePosY,
                UpdateTimelineScaX,
                UpdateTimelineScaY,
                UpdateTimelineRot,
                UpdateTimelineColor,
                UpdateTimelineOpacity,
                UpdateTimelineHue,
                UpdateTimelineSat,
                UpdateTimelineVal,
            }, // Timeline
            new KFDelegate[]
            {
                UpdatePlayerActive,
                UpdatePlayerMoveable,
                UpdatePlayerPositionX,
                UpdatePlayerPositionY,
                UpdatePlayerRotation,
                UpdatePlayerOoB
            }, // Player
            new KFDelegate[]
            {
                UpdateDelayTrackerActive,
                UpdateDelayTrackerMove,
                UpdateDelayTrackerRotate,
                UpdateDelayTrackerSharpness,
                UpdateDelayTrackerOffset,
                UpdateDelayTrackerLimitLeft,
                UpdateDelayTrackerLimitRight,
                UpdateDelayTrackerLimitUp,
                UpdateDelayTrackerLimitDown,
                UpdateDelayTrackerAnchor,
            }, // Camera Follows Player
            new KFDelegate[]
            {
                UpdateAudioPitch,
                UpdateAudioVolume
            }, // Audio
            new KFDelegate[]
            {
                UpdateVideoBGParentPositionX,
                UpdateVideoBGParentPositionY,
                UpdateVideoBGParentPositionZ,
                UpdateVideoBGParentScaleX,
                UpdateVideoBGParentScaleY,
                UpdateVideoBGParentScaleZ,
                UpdateVideoBGParentRotationX,
                UpdateVideoBGParentRotationY,
                UpdateVideoBGParentRotationZ,
            }, // Video BG Parent
            new KFDelegate[]
            {
                UpdateVideoBGPositionX,
                UpdateVideoBGPositionY,
                UpdateVideoBGPositionZ,
                UpdateVideoBGScaleX,
                UpdateVideoBGScaleY,
                UpdateVideoBGScaleZ,
                UpdateVideoBGRotationX,
                UpdateVideoBGRotationY,
                UpdateVideoBGRotationZ,
                UpdateVideoBGRenderLayer,
            }, // Video BG
            new KFDelegate[]
            {
                UpdateSharpen
            }, // Sharpen
            new KFDelegate[]
            {
                UpdateBlackBarsIntensity,
                UpdateBlackBarsMode
            }, // Bars
            new KFDelegate[]
            {
                UpdateDangerIntensity,
                UpdateDangerSize,
                UpdateDangerColor,
                UpdateCameraDangerOpacity,
                UpdateCameraDangerHue,
                UpdateCameraDangerSat,
                UpdateCameraDangerVal,
            }, // Danger
            new KFDelegate[]
            {
                UpdateCameraRotationX,
                UpdateCameraRotationY
            }, // 3D Rotation
            new KFDelegate[]
            {
                UpdateCameraDepth,
                UpdateCameraPerspectiveZoom,
                UpdateCameraPerspectiveGlobal,
                UpdateCameraPerspectiveAlign
            }, // Camera Depth
            new KFDelegate[]
            {
                UpdateWindowForceResolution,
                UpdateWindowResolutionX,
                UpdateWindowResolutionY,
                UpdateWindowAllowPositioning
            }, // Window Base
            new KFDelegate[]
            {
                UpdateWindowPositionX
            }, // Window Position X
            new KFDelegate[]
            {
                UpdateWindowPositionY
            }, // Window Position Y
            new KFDelegate[]
            {
                UpdatePlayerForceX,
                UpdatePlayerForceY
            }, // Player Force
            new KFDelegate[]
            {
                UpdateCameraMosaic
            }, // Mosaic
            new KFDelegate[]
            {
                UpdateAnalogGlitchEnabled,
                UpdateAnalogGlitchColorDrift,
                UpdateAnalogGlitchHorizontalShake,
                UpdateAnalogGlitchScanLineJitter,
                UpdateAnalogGlitchVerticalJump,
            }, // Analog Glitch
            new KFDelegate[]
            {
                UpdateDigitalGlitchIntensity,
            }, // Digital Glitch
        };

        public float shakeSpeed = 1f;
        public float shakeSmoothness = 1f;

        public static float ShakeEase(float t) => Ease.InterpolateEase(t, Mathf.Clamp(inst.shakeSmoothness, 0f, 4f));

        public void SetupShake()
        {
            if (shakeSequence != null)
                shakeSequence = null;

            var list = new List<IKeyframe<Vector2>>();

            Vector2Keyframe firstKeyframe = new Vector2Keyframe();
            bool setFirstKeyframe = false;

            float t = 0f;
            while (t < 10f)
            {
                var x = Random.Range(-6f, 6f);
                var y = Random.Range(-6f, 6f);
                if (x >= 0f)
                    x = Math.Min(3f, x);
                if (x < 0f)
                    x = Math.Max(-3f, x);
                if (y >= 0f)
                    y = Math.Min(3f, y);
                if (y < 0f)
                    y = Math.Max(-3f, y);

                var kf = new Vector2Keyframe(t, new Vector2(x, y), Ease.Linear);

                if (!setFirstKeyframe)
                {
                    firstKeyframe = kf;
                    setFirstKeyframe = true;
                }

                list.Add(kf);
                t += Random.Range(0.065f, 0.07f);
            }

            list.Add(new Vector2Keyframe(t, firstKeyframe.Value, Ease.Linear));
            shakeLength = t;
            shakeSequence = new Sequence<Vector2>(list);
        }

        public float shakeLength = 999f;

        public Sequence<Vector2> shakeSequence;

        #endregion
    }
}
