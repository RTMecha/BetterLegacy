using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor;
using BetterLegacy.Patchers;
using DG.Tweening;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            digitalGlitch._shader = LegacyPlugin.digitalGlitchShader;

            analogGlitch = glitchCamera.AddComponent<AnalogGlitch>();
            analogGlitch._shader = LegacyPlugin.analogGlitchShader;
        }

        public void SetResetOffsets() => offsets = ResetOffsets();

        public static bool Playable =>
            RTEffectsManager.inst &&
            EventManager.inst &&
            GameManager.inst &&
            (CoreHelper.Playing || CoreHelper.Reversing || LevelManager.LevelEnded) &&
            GameData.IsValid &&
            GameData.Current.eventObjects != null &&
            GameData.Current.eventObjects.allEvents != null &&
            GameData.Current.eventObjects.allEvents.Count > 0;

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

                if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus))
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
            var allEvents = GameData.Current.eventObjects.allEvents;
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
                var list = allEvents[i].OrderBy(x => x.eventTime).ToList();

                var nextKFIndex = list.FindIndex(x => x.eventTime > time);

                if (nextKFIndex >= 0)
                {
                    var prevKFIndex = nextKFIndex - 1;
                    if (prevKFIndex < 0)
                        prevKFIndex = 0;

                    var nextKF = list[nextKFIndex] as EventKeyframe;
                    var prevKF = list[prevKFIndex] as EventKeyframe;

                    if (events.Length <= i)
                        continue;

                    for (int j = 0; j < nextKF.eventValues.Length; j++)
                    {
                        if (events[i].Length <= j || prevKF.eventValues.Length <= j || events[i][j] == null)
                            continue;

                        var total = 0f;
                        var prevtotal = 0f;
                        for (int k = 0; k < nextKFIndex; k++)
                        {
                            if (((EventKeyframe)allEvents[i][k + 1]).relative)
                                total += allEvents[i][k].eventValues[j];
                            else
                                total = 0f;

                            if (((EventKeyframe)allEvents[i][k]).relative)
                                prevtotal += allEvents[i][k].eventValues[j];
                            else
                                prevtotal = 0f;
                        }

                        var next = nextKF.relative ? total + nextKF.eventValues[j] : nextKF.eventValues[j];
                        var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.eventValues[j];

                        bool isLerper = IsLerper(i, j);

                        if (float.IsNaN(prev) || !isLerper)
                            prev = 0f;

                        if (float.IsNaN(next))
                            next = 0f;

                        if (!isLerper)
                            next = 1f;

                        var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time)));

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
                else if (list.Count > 0)
                {
                    if (events.Length <= i)
                        continue;

                    for (int j = 0; j < list[list.Count - 1].eventValues.Length; j++)
                    {
                        if (events[i].Length <= j || events[i][j] == null)
                            continue;

                        var total = 0f;
                        for (int k = 0; k < list.Count - 1; k++)
                        {
                            if (((EventKeyframe)allEvents[i][k + 1]).relative)
                                total += allEvents[i][k].eventValues[j];
                            else
                                total = 0f;
                        }

                        bool isLerper = IsLerper(i, j);

                        var x = list[list.Count - 1].eventValues[j];

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
                            x = allEvents[i][allEvents[i].Count - 1].eventValues[j];

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
            var allEvents = GameData.Current.eventObjects.allEvents;

            var list = allEvents[type].OrderBy(x => x.eventTime).ToList();

            var nextKFIndex = list.FindIndex(x => x.eventTime > time);

            if (nextKFIndex < 0)
                nextKFIndex = 0;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex] as EventKeyframe;
            var prevKF = list[prevKFIndex] as EventKeyframe;

            type = Mathf.Clamp(type, 0, events.Length);
            valueIndex = Mathf.Clamp(valueIndex, 0, events[type].Length);

            if (prevKF.eventValues.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (((EventKeyframe)allEvents[type][k + 1]).relative)
                    total += allEvents[type][k].eventValues[valueIndex];
                else
                    total = 0f;

                if (((EventKeyframe)allEvents[type][k]).relative)
                    prevtotal += allEvents[type][k].eventValues[valueIndex];
                else
                    prevtotal = 0f;
            }

            var next = nextKF.relative ? total + nextKF.eventValues[valueIndex] : nextKF.eventValues[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.eventValues[valueIndex];

            bool isLerper = IsLerper(type, valueIndex);

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time)));

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
        public bool setPerspectiveCamClip = false;
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

                if (Updater.UseNewUpdateMethod)
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

                if (!float.IsNaN(EventManager.inst.camRot) && !editorCam)
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(inst.camRotOffset.x, inst.camRotOffset.y, EventManager.inst.camRot));
                else if (!float.IsNaN(inst.editorRotate))
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(inst.editorPerRotate.x, inst.editorPerRotate.y, inst.editorRotate));

                if (!editorCam)
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(EventManager.inst.camPos.x, EventManager.inst.camPos.y, inst.zPosition);
                else
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(inst.editorOffset.x, inst.editorOffset.y, inst.zPosition);

                EventManager.inst.camPer.fieldOfView = inst.fieldOfView;

                if (!inst.bgGlobalPosition)
                {
                    if (!editorCam)
                        EventManager.inst.camPer.transform.localPosition = new Vector3(EventManager.inst.camPer.transform.localPosition.x, EventManager.inst.camPer.transform.localPosition.y, -EventManager.inst.camZoom + inst.perspectiveZoom);
                    else
                        EventManager.inst.camPer.transform.localPosition = new Vector3(EventManager.inst.camPer.transform.localPosition.x, EventManager.inst.camPer.transform.localPosition.y, -inst.editorZoom + inst.perspectiveZoom);
                }
                else
                {
                    if (!editorCam)
                        EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -EventManager.inst.camZoom + inst.perspectiveZoom);
                    else
                        EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -inst.editorZoom + inst.perspectiveZoom);
                }

                if (inst.setPerspectiveCamClip)
                    EventManager.inst.camPer.nearClipPlane = -EventManager.inst.camPer.transform.position.z + inst.camPerspectiveOffset;

                #endregion

                #region Lerp Colors

                LSEffectsManager.inst.bloom.color.Override(
                    ChangeColorHSV(LerpColor(inst.prevBloomColor, inst.nextBloomColor, inst.bloomColor, Color.white),
                    inst.bloomHue, inst.bloomSat, inst.bloomVal));

                LSEffectsManager.inst.vignette.color.Override(
                    ChangeColorHSV(LerpColor(inst.prevVignetteColor, inst.nextVignetteColor, inst.vignetteColor, Color.black),
                    inst.vignetteHue, inst.vignetteSat, inst.vignetteVal));

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;
                GameManagerPatch.bgColorToLerp = ChangeColorHSV(LerpColor(inst.prevBGColor, inst.nextBGColor, inst.bgColor, beatmapTheme.backgroundColor), inst.bgHue, inst.bgSat, inst.bgVal);

                GameManagerPatch.timelineColorToLerp =
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
                    if (customPlayer.Player && customPlayer.Player.playerObjects.TryGetValue("RB Parent", out RTPlayer.PlayerObject rbParent))
                    {
                        var player = rbParent.gameObject.transform;
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
            var allEvents = GameData.Current.eventObjects.allEvents;
            var time = currentTime;

            FindColor(time, allEvents, ref EventManager.inst.LastTheme, ref EventManager.inst.NewTheme, 4, 0);
            FindColor(time, allEvents, ref prevBloomColor, ref nextBloomColor, 6, 4);
            FindColor(time, allEvents, ref prevVignetteColor, ref nextVignetteColor, 7, 6);
            FindColor(time, allEvents, ref prevGradientColor1, ref nextGradientColor1, ref prevGradientColor2, ref nextGradientColor2, 15, 2, 3);
            FindColor(time, allEvents, ref prevBGColor, ref nextBGColor, 20, 0);
            FindColor(time, allEvents, ref prevTimelineColor, ref nextTimelineColor, 22, 6);
            FindColor(time, allEvents, ref prevDangerColor, ref nextDangerColor, 30, 2);
        }

        void FindColor(float time, List<List<DataManager.GameData.EventKeyframe>> allEvents, ref int prev, ref int next, int type, int valueIndex)
        {
            if (allEvents.Count <= type || allEvents[type].Count <= 0)
                return;

            var nextKFIndex = allEvents[type].FindLastIndex(x => x.eventTime <= time) + 1;
            if (nextKFIndex < allEvents[type].Count)
            {
                var nextKF = allEvents[type][nextKFIndex];
                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].eventValues.Length > valueIndex)
                    prev = (int)allEvents[type][nextKFIndex - 1].eventValues[valueIndex];
                else if (allEvents[type][0].eventValues.Length > valueIndex)
                    prev = (int)allEvents[type][0].eventValues[valueIndex];

                if (nextKF.eventValues.Length > valueIndex)
                    next = (int)nextKF.eventValues[valueIndex];

                return;
            }

            var finalKF = allEvents[type][allEvents[type].Count - 1];

            int a = allEvents[type].Count - 2;
            if (a < 0)
                a = 0;

            if (allEvents[type][a].eventValues.Length > valueIndex)
                prev = (int)allEvents[type][a].eventValues[valueIndex];

            if (finalKF.eventValues.Length > valueIndex)
                next = (int)finalKF.eventValues[valueIndex];
        }

        void FindColor(float time, List<List<DataManager.GameData.EventKeyframe>> allEvents, ref int prev1, ref int next1, ref int prev2, ref int next2, int type, int valueIndex1, int valueIndex2)
        {
            if (allEvents.Count <= type || allEvents[type].Count <= 0)
                return;

            var nextKFIndex = allEvents[type].FindLastIndex(x => x.eventTime <= time) + 1;
            if (nextKFIndex < allEvents[type].Count)
            {
                var nextKF = allEvents[type][nextKFIndex];
                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].eventValues.Length > valueIndex1)
                    prev1 = (int)allEvents[type][nextKFIndex - 1].eventValues[valueIndex1];
                else if (allEvents[type][0].eventValues.Length > valueIndex1)
                    prev1 = (int)allEvents[type][0].eventValues[valueIndex1];

                if (nextKFIndex - 1 > -1 && allEvents[type][nextKFIndex - 1].eventValues.Length > valueIndex2)
                    prev2 = (int)allEvents[type][nextKFIndex - 1].eventValues[valueIndex2];
                else if (allEvents[type][0].eventValues.Length > valueIndex2)
                    prev2 = (int)allEvents[type][0].eventValues[valueIndex2];

                if (nextKF.eventValues.Length > valueIndex1)
                    next1 = (int)nextKF.eventValues[valueIndex1];

                if (nextKF.eventValues.Length > valueIndex2)
                    next2 = (int)nextKF.eventValues[valueIndex2];

                return;
            }

            var finalKF = allEvents[type][allEvents[type].Count - 1];

            int a = allEvents[type].Count - 2;
            if (a < 0)
                a = 0;

            if (allEvents[type][a].eventValues.Length > valueIndex1)
                prev1 = (int)allEvents[type][a].eventValues[valueIndex1];
            if (allEvents[type][a].eventValues.Length > valueIndex2)
                prev2 = (int)allEvents[type][a].eventValues[valueIndex2];

            if (finalKF.eventValues.Length > valueIndex1)
                next1 = (int)finalKF.eventValues[valueIndex1];
            if (finalKF.eventValues.Length > valueIndex2)
                next2 = (int)finalKF.eventValues[valueIndex2];
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
            for (int i = 0; i < GameData.Current.eventObjects.allEvents.Count; i++)
                for (int j = 0; j < GameData.Current.eventObjects.allEvents[i].Count; j++)
                    GameData.Current.eventObjects.allEvents[i][j].active = false;
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
            for (int i = 0; i < GameData.Current.eventObjects.allEvents.Count; i++)
                for (int j = 0; j < GameData.Current.eventObjects.allEvents[i].Count; j++)
                    GameData.Current.eventObjects.allEvents[i][j].active = false;
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
        public static void updateCameraPositionX(float x) => EventManager.inst.camPos.x = x;
        // 0 - 1
        public static void updateCameraPositionY(float x) => EventManager.inst.camPos.y = x;

        #endregion

        #region Zoom - 1

        // 1 - 0
        public static void updateCameraZoom(float x) => EventManager.inst.camZoom = x;

        #endregion

        #region Rotate - 2

        // 2 - 0
        public static void updateCameraRotation(float x) => EventManager.inst.camRot = x;

        #endregion

        #region Shake - 3

        // 3 - 0
        public static void updateCameraShakeMultiplier(float x) => EventManager.inst.shakeMultiplier = x;

        // 3 - 1
        public static void updateCameraShakeX(float x) => inst.shakeX = x;

        // 3 - 2
        public static void updateCameraShakeY(float x) => inst.shakeY = x;

        // 3 - 3
        public static void updateCameraShakeSmoothness(float x) => inst.shakeSmoothness = x;

        // 3 - 4
        public static void updateCameraShakeSpeed(float x) => inst.shakeSpeed = x;

        #endregion

        #region Theme - 4

        // 4 - 0
        public static void updateTheme(float x)
        {
            inst.themeLerp = x;
            var beatmapTheme = BeatmapTheme.DeepCopy((BeatmapTheme)GameManager.inst.LiveTheme);

            ((BeatmapTheme)GameManager.inst.LiveTheme).Lerp((BeatmapTheme)GetTheme(EventManager.inst.LastTheme), (BeatmapTheme)GetTheme(EventManager.inst.NewTheme), x);

            if (beatmapTheme != (BeatmapTheme)GameManager.inst.LiveTheme)
                GameManager.inst.UpdateTheme();
        }

        /// <summary>
        /// BeatmapTheme finder.
        /// </summary>
        /// <param name="id">Finds the BeatmapTheme with the matching ID.</param>
        /// <returns>The current BeatmapTheme.</returns>
        public static DataManager.BeatmapTheme GetTheme(int id) => DataManager.inst.AllThemes.TryFind(x => Parser.TryParse(x.id, 0) == id, out DataManager.BeatmapTheme beatmapTheme) ? beatmapTheme : DataManager.inst.AllThemes[0];

        #endregion

        #region Chroma - 5

        // 5 - 0
        public static void updateCameraChromatic(float x) => EventManager.inst.camChroma = x;

        #endregion

        #region Bloom - 6

        // 6 - 0
        public static void updateCameraBloom(float x) => EventManager.inst.camBloom = x;

        // 6 - 1
        public static void updateCameraBloomDiffusion(float x) => LSEffectsManager.inst.bloom.diffusion.Override(x);

        // 6 - 2
        public static void updateCameraBloomThreshold(float x) => LSEffectsManager.inst.bloom.threshold.Override(x);

        // 6 - 3
        public static void updateCameraBloomAnamorphicRatio(float x) => LSEffectsManager.inst.bloom.anamorphicRatio.Override(x);

        // 6 - 4
        public static void updateCameraBloomColor(float x) => inst.bloomColor = x;

        // 6 - 5
        public static void updateCameraBloomHue(float x) => inst.bloomHue = x;

        // 6 - 6
        public static void updateCameraBloomSat(float x) => inst.bloomSat = x;

        // 6 - 7
        public static void updateCameraBloomVal(float x) => inst.bloomVal = x;

        #endregion

        #region Vignette - 7

        // 7 - 0
        public static void updateCameraVignette(float x) => EventManager.inst.vignetteIntensity = x;

        // 7 - 1
        public static void updateCameraVignetteSmoothness(float x) => EventManager.inst.vignetteSmoothness = x;

        // 7 - 2
        public static void updateCameraVignetteRounded(float x) => EventManager.inst.vignetteRounded = x;

        // 7 - 3
        public static void updateCameraVignetteRoundness(float x) => EventManager.inst.vignetteRoundness = x;

        // 7 - 4
        public static void updateCameraVignetteCenterX(float x) => EventManager.inst.vignetteCenter.x = x;

        // 7 - 5
        public static void updateCameraVignetteCenterY(float x) => EventManager.inst.vignetteCenter.y = x;

        // 7 - 6
        public static void updateCameraVignetteColor(float x) => inst.vignetteColor = x;

        // 7 - 7
        public static void updateCameraVignetteHue(float x) => inst.vignetteHue = x;

        // 7 - 8
        public static void updateCameraVignetteSat(float x) => inst.vignetteSat = x;

        // 7 - 9
        public static void updateCameraVignetteVal(float x) => inst.vignetteVal = x;

        #endregion

        #region Lens - 8

        // 8 - 0
        public static void updateCameraLens(float x) => EventManager.inst.lensDistortIntensity = x;

        // 8 - 1
        public static void updateCameraLensCenterX(float x) => LSEffectsManager.inst.lensDistort.centerX.Override(x);

        // 8 - 2
        public static void updateCameraLensCenterY(float x) => LSEffectsManager.inst.lensDistort.centerY.Override(x);

        // 8 - 3
        public static void updateCameraLensIntensityX(float x) => LSEffectsManager.inst.lensDistort.intensityX.Override(x);

        // 8 - 4
        public static void updateCameraLensIntensityY(float x) => LSEffectsManager.inst.lensDistort.intensityY.Override(x);

        // 8 - 5
        public static void updateCameraLensScale(float x) => LSEffectsManager.inst.lensDistort.scale.Override(x);

        #endregion

        #region Grain - 9

        // 9 - 0
        public static void updateCameraGrain(float x) => EventManager.inst.grainIntensity = x;

        // 9 - 1
        public static void updateCameraGrainColored(float _colored) => EventManager.inst.grainColored = _colored;

        // 9 - 2
        public static void updateCameraGrainSize(float x) => EventManager.inst.grainSize = x;

        #endregion

        #region ColorGrading - 10

        // 10 - 0
        public static void updateCameraHueShift(float x) => inst.colorGradingHueShift = x;

        // 10 - 1
        public static void updateCameraContrast(float x) => inst.colorGradingContrast = x;

        // 10 - 2
        public static void updateCameraGammaX(float x) => inst.colorGradingGamma.x = x;

        // 10 - 3
        public static void updateCameraGammaY(float x) => inst.colorGradingGamma.y = x;

        // 10 - 4
        public static void updateCameraGammaZ(float x) => inst.colorGradingGamma.z = x;

        // 10 - 5
        public static void updateCameraGammaW(float x) => inst.colorGradingGamma.w = x;

        // 10 - 6
        public static void updateCameraSaturation(float x) => inst.colorGradingSaturation = x;

        // 10 - 7
        public static void updateCameraTemperature(float x) => inst.colorGradingTemperature = x;

        // 10 - 8
        public static void updateCameraTint(float x) => inst.colorGradingTint = x;

        #endregion

        #region Ripples - 11

        // 11 - 0
        public static void updateCameraRipplesStrength(float x) => inst.ripplesStrength = x;

        // 11 - 1
        public static void updateCameraRipplesSpeed(float x) => inst.ripplesSpeed = x;

        // 11 - 2
        public static void updateCameraRipplesDistance(float x) => inst.ripplesDistance = Mathf.Clamp(x, 0.0001f, float.PositiveInfinity);

        // 11 - 3
        public static void updateCameraRipplesHeight(float x) => inst.ripplesHeight = x;

        // 11 - 4
        public static void updateCameraRipplesWidth(float x) => inst.ripplesWidth = x;
        
        // 11 - 5
        public static void updateCameraRipplesMode(float x) => inst.ripplesMode = (int)x;

        #endregion

        #region RadialBlur - 12

        // 12 - 0
        public static void updateCameraRadialBlurIntensity(float x) => inst.radialBlurIntensity = x;

        // 12 - 1
        public static void updateCameraRadialBlurIterations(float x) => inst.radialBlurIterations = Mathf.Clamp((int)x, 1, 30);

        #endregion

        #region ColorSplit - 13

        // 13 - 0
        public static void updateCameraColorSplit(float x) => inst.colorSplitOffset = x;

        // 13 - 1
        public static void updateCameraColorSplitMode(float x) => inst.colorSplitMode = (int)x;

        #endregion

        #region Offset - 14

        // 14 - 0
        public static void updateCameraOffsetX(float x) => inst.camOffsetX = x;

        // 14 - 1
        public static void updateCameraOffsetY(float x) => inst.camOffsetY = x;

        #endregion

        #region Gradient - 15

        // 15 - 0
        public static void updateCameraGradientIntensity(float x) => inst.gradientIntensity = x;

        // 15 - 1
        public static void updateCameraGradientRotation(float x) => inst.gradientRotation = x;

        // 15 - 2
        public static void updateCameraGradientColor1(float x) => inst.gradientColor1 = x;

        // 15 - 3
        public static void updateCameraGradientColor2(float x) => inst.gradientColor2 = x;

        // 15 - 4
        public static void updateCameraGradientMode(float x) => RTEffectsManager.inst.gradient.blendMode.Override((SCPE.Gradient.BlendMode)(int)x);

        // 15 - 5
        public static void updateCameraGradientColor1Opacity(float x) => inst.gradientColor1Opacity = x;

        // 15 - 6
        public static void updateCameraGradientColor1Hue(float x) => inst.gradientColor1Hue = x;

        // 15 - 7
        public static void updateCameraGradientColor1Sat(float x) => inst.gradientColor1Sat = x;

        // 15 - 8
        public static void updateCameraGradientColor1Val(float x) => inst.gradientColor1Val = x;

        // 15 - 5
        public static void updateCameraGradientColor2Opacity(float x) => inst.gradientColor2Opacity = x;

        // 15 - 6
        public static void updateCameraGradientColor2Hue(float x) => inst.gradientColor2Hue = x;

        // 15 - 7
        public static void updateCameraGradientColor2Sat(float x) => inst.gradientColor2Sat = x;

        // 15 - 8
        public static void updateCameraGradientColor2Val(float x) => inst.gradientColor2Val = x;

        #endregion

        #region DoubleVision - 16

        // 16 - 0
        public static void updateCameraDoubleVision(float x) => inst.doubleVision = x;

        // 16 - 1
        public static void updateCameraDoubleVisionMode(float x) => inst.doubleVisionMode = (int)x;

        #endregion

        #region ScanLines - 17

        // 17 - 0
        public static void updateCameraScanLinesIntensity(float x) => inst.scanLinesIntensity = x;

        // 17 - 1
        public static void updateCameraScanLinesAmount(float x) => inst.scanLinesAmount = x;

        // 17 - 2
        public static void updateCameraScanLinesSpeed(float x) => inst.scanLinesSpeed = x;

        #endregion

        #region Blur - 18

        // 18 - 0
        public static void updateCameraBlurAmount(float x) => LSEffectsManager.inst.blur.amount.Override(!EventsConfig.Instance.ShowFX.Value ? 0f : x);

        // 18 - 1
        public static void updateCameraBlurIterations(float x) => LSEffectsManager.inst.blur.iterations.Override(Mathf.Clamp((int)x, 1, 30));

        #endregion

        #region Pixelize - 19

        // 19 - 0
        public static void updateCameraPixelize(float x) => inst.pixel = Mathf.Clamp(x, 0f, 0.99999f);

        #endregion

        #region BG - 20

        // 20 - 0
        public static void updateCameraBGColor(float x) => inst.bgColor = x;

        // 20 - 1
        public static void updateCameraBGActive(float x)
        {
            inst.bgActive = x;

            var i = (int)x;

            BackgroundManager.inst?.backgroundParent?.gameObject?.SetActive(i == 0);
        }

        public float bgActive = 0f;

        // 20 - 2
        public static void updateCameraBGHue(float x) => inst.bgHue = x;

        // 20 - 3
        public static void updateCameraBGSat(float x) => inst.bgSat = x;

        // 20 - 4
        public static void updateCameraBGVal(float x) => inst.bgVal = x;

        #endregion

        #region Invert - 21

        // 21 - 0
        public static void updateCameraInvert(float x) => inst.invertAmount = x;

        #endregion

        #region Timeline - 22

        // 22 - 0
        public static void updateTimelineActive(float x) => inst.timelineActive = (int)x == 0;

        // 22 - 1
        public static void updateTimelinePosX(float x) => inst.timelinePos.x = x;

        // 22 - 2
        public static void updateTimelinePosY(float x) => inst.timelinePos.y = x;

        // 22 - 3
        public static void updateTimelineScaX(float x) => inst.timelineSca.x = x;

        // 22 - 4
        public static void updateTimelineScaY(float x) => inst.timelineSca.y = x;

        // 22 - 5
        public static void updateTimelineRot(float x) => inst.timelineRot = x;

        // 22 - 6
        public static void updateTimelineColor(float x) => inst.timelineColor = x;

        // 22 - 7
        public static void updateTimelineOpacity(float x) => inst.timelineOpacity = x;

        // 22 - 8
        public static void updateTimelineHue(float x) => inst.timelineHue = x;

        // 22 - 9
        public static void updateTimelineSat(float x) => inst.timelineSat = x;

        // 22 - 10
        public static void updateTimelineVal(float x) => inst.timelineVal = x;

        #endregion

        #region Player - 23

        // 23 - 0
        public static void updatePlayerActive(float x)
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
        public static void updatePlayerMoveable(float x)
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
        public static void updatePlayerPositionX(float x) => inst.playerPositionX = x;

        // 23 - 3
        public static void updatePlayerPositionY(float x) => inst.playerPositionY = x;

        // 23 - 4
        public static void updatePlayerRotation(float x) => inst.playerRotation = x;

        // 23 - 5
        public static void updatePlayerOoB(float x) => RTPlayer.OutOfBounds = x == 1f;

        #endregion

        #region Camera Follows Player - 24

        // 24 - 0
        public static void updateDelayTrackerActive(float x) => inst.delayTracker.active = (int)x == 1;

        // 24 - 1
        public static void updateDelayTrackerMove(float x) => inst.delayTracker.move = (int)x == 1;

        // 24 - 2
        public static void updateDelayTrackerRotate(float x) => inst.delayTracker.rotate = (int)x == 1;

        // 24 - 3
        public static void updateDelayTrackerSharpness(float x) => inst.delayTracker.followSharpness = Mathf.Clamp(x, 0.001f, 1f);

        // 24 - 4
        public static void updateDelayTrackerOffset(float x) => inst.delayTracker.offset = x;

        // 24 - 5
        public static void updateDelayTrackerLimitLeft(float x) => inst.delayTracker.limitLeft = x;

        // 24 - 6
        public static void updateDelayTrackerLimitRight(float x) => inst.delayTracker.limitRight = x;

        // 24 - 7
        public static void updateDelayTrackerLimitUp(float x) => inst.delayTracker.limitUp = x;

        // 24 - 8
        public static void updateDelayTrackerLimitDown(float x) => inst.delayTracker.limitDown = x;

        // 24 - 9
        public static void updateDelayTrackerAnchor(float x) => inst.delayTracker.anchor = x;

        #endregion

        #region Audio - 25

        // 25 - 0
        public static void updateAudioPitch(float x) => AudioManager.inst.pitch = Mathf.Clamp(x, 0.001f, 10f) * CoreHelper.Pitch * inst.pitchOffset;

        // 25 - 1
        public static void updateAudioVolume(float x) => inst.audioVolume = Mathf.Clamp(x, 0f, 1f);

        #endregion

        #region Video BG Parent - 26

        // 26 - 0
        public static void updateVideoBGParentPositionX(float x) => inst.videoBGParentPos.x = x;
        // 26 - 1
        public static void updateVideoBGParentPositionY(float x) => inst.videoBGParentPos.y = x;
        // 26 - 2
        public static void updateVideoBGParentPositionZ(float x) => inst.videoBGParentPos.z = x;
        // 26 - 3
        public static void updateVideoBGParentScaleX(float x) => inst.videoBGParentSca.x = x;
        // 26 - 4
        public static void updateVideoBGParentScaleY(float x) => inst.videoBGParentSca.y = x;
        // 26 - 5
        public static void updateVideoBGParentScaleZ(float x) => inst.videoBGParentSca.z = x;
        // 26 - 6
        public static void updateVideoBGParentRotationX(float x) => inst.videoBGParentRot.x = x;
        // 26 - 7
        public static void updateVideoBGParentRotationY(float x) => inst.videoBGParentRot.y = x;
        // 26 - 8
        public static void updateVideoBGParentRotationZ(float x) => inst.videoBGParentRot.z = x;

        #endregion

        #region Video BG - 27

        // 27 - 0
        public static void updateVideoBGPositionX(float x) => inst.videoBGPos.x = x;
        // 27 - 1
        public static void updateVideoBGPositionY(float x) => inst.videoBGPos.y = x;
        // 27 - 2
        public static void updateVideoBGPositionZ(float x) => inst.videoBGPos.z = x;
        // 27 - 3
        public static void updateVideoBGScaleX(float x) => inst.videoBGSca.x = x;
        // 27 - 4
        public static void updateVideoBGScaleY(float x) => inst.videoBGSca.y = x;
        // 27 - 5
        public static void updateVideoBGScaleZ(float x) => inst.videoBGSca.z = x;
        // 27 - 6
        public static void updateVideoBGRotationX(float x) => inst.videoBGRot.x = x;
        // 27 - 7
        public static void updateVideoBGRotationY(float x) => inst.videoBGRot.y = x;
        // 27 - 8
        public static void updateVideoBGRotationZ(float x) => inst.videoBGRot.z = x;
        // 27 - 9
        public static void updateVideoBGRenderLayer(float x) => inst.videoBGRenderLayer = (int)x;

        #endregion

        #region Sharpen - 28

        // 28 - 0
        public static void updateSharpen(float x) => inst.sharpen = x;

        #endregion

        #region Bars - 29

        // 29 - 0
        public static void updateBlackBarsIntensity(float x) => inst.blackBarsIntensity = x;
        // 29 - 1
        public static void updateBlackBarsMode(float x) => inst.blackBarsMode = x;

        #endregion

        #region Danger - 30

        // 30 - 0
        public static void updateDangerIntensity(float x) => inst.dangerIntensity = x;
        // 30 - 1
        public static void updateDangerSize(float x) => inst.dangerSize = x;
        // 30 - 2
        public static void updateDangerColor(float x) => inst.dangerColor = x;

        // 30 - 3
        public static void updateCameraDangerOpacity(float x) => inst.dangerOpacity = x;

        // 30 - 4
        public static void updateCameraDangerHue(float x) => inst.dangerHue = x;

        // 30 - 5
        public static void updateCameraDangerSat(float x) => inst.dangerSat = x;

        // 30 - 6
        public static void updateCameraDangerVal(float x) => inst.dangerVal = x;

        #endregion

        #region 3D Rotation - 31

        // 31 - 0
        public static void updateCameraRotationX(float x) => inst.camRotOffset.x = x;
        // 31 - 1
        public static void updateCameraRotationY(float x) => inst.camRotOffset.y = x;

        #endregion

        #region Camera Depth - 32

        // 32 - 0
        public static void updateCameraDepth(float x) => inst.zPosition = x;
        // 32 - 1
        public static void updateCameraPerspectiveZoom(float x) => inst.perspectiveZoom = x;
        // 32 - 2
        public static void UpdateCameraPerspectiveGlobal(float x) => inst.bgGlobalPosition = x == 0f;

        #endregion

        #region Window Base - 33

        // 33 - 0
        public static void updateWindowForceResolution(float x) => inst.forceWindow = (int)x == 1;
        // 33 - 1
        public static void updateWindowResolutionX(float x) => inst.windowResolution.x = x;
        // 33 - 2
        public static void updateWindowResolutionY(float x) => inst.windowResolution.y = x;
        // 33 - 3
        public static void updateWindowAllowPositioning(float x) => inst.allowWindowPositioning = (int)x == 1;

        #endregion

        #region Window Position X - 34

        // 34 - 0
        public static void updateWindowPositionX(float x) => inst.windowPosition.x = x;

        #endregion

        #region Window Position Y - 35

        // 35 - 0
        public static void updateWindowPositionY(float x) => inst.windowPosition.y = x;

        #endregion

        #region Player Force - 36

        // 36 - 0
        public static void updatePlayerForceX(float x) => inst.playerForceX = x;

        // 36 - 1
        public static void updatePlayerForceY(float x) => inst.playerForceY = x;

        #endregion

        #region Mosaic - 37

        // 37 - 0
        public static void updateCameraMosaic(float x) => inst.mosaicIntensity = x;

        #endregion

        #region Analog Glitch - 38

        // 38 - 0
        public static void updateAnalogGlitchEnabled(float x) => inst.analogGlitchEnabled = (int)x == 1;

        // 38 - 1
        public static void updateAnalogGlitchColorDrift(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.colorDrift = x;
        }

        // 38 - 2
        public static void updateAnalogGlitchHorizontalShake(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.horizontalShake = x;
        }

        // 38 - 3
        public static void updateAnalogGlitchScanLineJitter(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.scanLineJitter = x;
        }

        // 38 - 4
        public static void updateAnalogGlitchVerticalJump(float x)
        {
            if (inst.analogGlitch)
                inst.analogGlitch.verticalJump = x;
        }

        #endregion

        #region Digital Glitch - 39

        // 39 - 0
        public static void updateDigitalGlitchIntensity(float x)
        {
            if (inst.digitalGlitch)
                inst.digitalGlitch.intensity = x;
        }

        #endregion

        #endregion

        #region Variables

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

        List<List<float>> ResetOffsets()
        {
            previousAudioTime = 0.0f;
            audioTimeVelocity = 0.0f;

            return new List<List<float>>
            {
                new List<float>
                {
                    0f, // Move X
                    0f, // Move Y
                }, // Move - 0
                new List<float>
                {
                    0f, // Zoom
                }, // Zoom - 1
                new List<float>
                {
                    0f, // Rotate
                }, // Rotate - 2
                new List<float>
                {
                    0f, // Shake
                    0f, // Shake X
                    0f, // Shake Y
                    0f, // Shake Interpolation
                    0f, // Shake Speed
                }, // Shake - 3
                new List<float>
                {
                    0f, // Theme
                }, // Theme - 4
                new List<float>
                {
                    0f, // Chromatic
                }, // Chromatic - 5
                new List<float>
                {
                    0f, // Bloom Intensity
                    0f, // Bloom Diffusion
                    0f, // Bloom Threshold
                    0f, // Bloom Anamorphic Ratio
                    0f, // Bloom Color
                    0f, // Bloom Hue
                    0f, // Bloom Sat
                    0f, // Bloom Val
                }, // Bloom - 6
                new List<float>
                {
                    0f, // Vignette Intensity
                    0f, // Vignette Smoothness
                    0f, // Vignette Rounded
                    0f, // Vignette Roundness
                    0f, // Vignette Center X
                    0f, // Vignette Center Y
                    0f, // Vignette Color
                    0f, // Vignette Hue
                    0f, // Vignette Sat
                    0f, // Vignette Val
                }, // Vignette - 7
                new List<float>
                {
                    0f, // Lens Intensity
                    0f, // Lens Center X
                    0f, // Lens Center Y
                    0f, // Lens Intensity X
                    0f, // Lens Intensity Y
                    0f, // Lens Scale
                }, // Lens - 8
                new List<float>
                {
                    0f, // Grain Intensity
                    0f, // Grain Colored
                    0f, // Grain Size
                }, // Grain - 9
                new List<float>
                {
                    0f, // ColorGrading Hueshift
                    0f, // ColorGrading Contrast
                    0f, // ColorGrading Gamma X
                    0f, // ColorGrading Gamma Y
                    0f, // ColorGrading Gamma Z
                    0f, // ColorGrading Gamma W
                    0f, // ColorGrading Saturation
                    0f, // ColorGrading Temperature
                    0f, // ColorGrading Tint
                }, // ColorGrading - 10
                new List<float>
                {
                    0f, // Ripples Strength
                    0f, // Ripples Speed
                    0f, // Ripples Distance
                    0f, // Ripples Height
                    0f, // Ripples Width
                    0f, // Ripples Mode
                }, // Ripples - 11
                new List<float>
                {
                    0f, // RadialBlur Intensity
                    0f, // RadialBlur Iterations
                }, // RadialBlur - 12
                new List<float>
                {
                    0f, // ColorSplit Offset
                    0f, // ColorSplit Mode
                }, // ColorSplit - 13
                new List<float>
                {
                    0f, // Camera Offset X
                    0f, // Camera Offset Y
                }, // Offset - 14
                new List<float>
                {
                    0f, // Gradient Intensity
                    0f, // Gradient Rotation
                    0f, // Gradient Color Top
                    0f, // Gradient Color Bottom
                    0f, // Gradient Mode
                    0f, // Gradient Color Top Opacity
                    0f, // Gradient Color Top Hue
                    0f, // Gradient Color Top Sat
                    0f, // Gradient Color Top Val
                    0f, // Gradient Color Bottom Opacity
                    0f, // Gradient Color Bottom Hue
                    0f, // Gradient Color Bottom Sat
                    0f, // Gradient Color Bottom Val
                }, // Gradient - 15
                new List<float>
                {
                    0f, // DoubleVision Intensity
                    0f, // DoubleVision Mode
                }, // DoubleVision
                new List<float>
                {
                    0f, // ScanLines Intensity
                    0f, // ScanLines Amount
                    0f, // ScanLines Speed
                }, // ScanLines
                new List<float>
                {
                    0f, // Blur Amount
                    0f, // Blur Iterations
                }, // Blur
                new List<float>
                {
                    0f, // Pixelize Amount
                }, // Pixelize
                new List<float>
                {
                    0f, // BG Color
                    0f, // BG Active
                    0f, // BG Hue
                    0f, // BG Sat
                    0f, // BG Val
                }, // BG
                new List<float>
                {
                    0f, // Invert Amount
                }, // Invert
                new List<float>
                {
                    0f, // Timeline Active
                    0f, // Timeline Pos X
                    0f, // Timeline Pos Y
                    0f, // Timeline Sca X
                    0f, // Timeline Sca Y
                    0f, // Timeline Rot
                    0f, // Timeline Color
                    0f, // Timeline Opacity
                    0f, // Timeline Hue
                    0f, // Timeline Sat
                    0f, // Timeline Val
                }, // Timeline
                new List<float>
                {
                    0f, // Player Active
                    0f, // Player Moveable
                    0f, // Player Velocity
                    0f, // Player Rotation
                    0f, // Player OoB
                },
                new List<float>
                {
                    0f, // Follow Player Active
                    0f, // Follow Player Move
                    0f, // Follow Player Rotate
                    0f, // Follow Player Sharpness
                    0f, // Follow Player Offset
                    0f, // Follow Player Limit Left
                    0f, // Follow Player Limit Right
                    0f, // Follow Player Limit Up
                    0f, // Follow Player Limit Down
                    0f, // Follow Player Anchor
                },
                new List<float>
                {
                    0f, // Audio Pitch
                    0f, // Audio Volume
                },
                new List<float>
                {
                    0f, // Video BG Parent Position X
                    0f, // Video BG Parent Position Y
                    0f, // Video BG Parent Position Z
                    0f, // Video BG Parent Scale X
                    0f, // Video BG Parent Scale Y
                    0f, // Video BG Parent Scale Z
                    0f, // Video BG Parent Rotation X
                    0f, // Video BG Parent Rotation Y
                    0f, // Video BG Parent Rotation Z
                },
                new List<float>
                {
                    0f, // Video BG Position X
                    0f, // Video BG Position Y
                    0f, // Video BG Position Z
                    0f, // Video BG Scale X
                    0f, // Video BG Scale Y
                    0f, // Video BG Scale Z
                    0f, // Video BG Rotation X
                    0f, // Video BG Rotation Y
                    0f, // Video BG Rotation Z
                    0f, // Video BG Render Layer
                },
                new List<float>
                {
                    0f, // Sharpen
                },
                new List<float>
                {
                    0f, // Bars Intensity
                    0f, // Bars Mode
                },
                new List<float>
                {
                    0f, // Danger Intensity
                    0f, // Danger Size
                    0f, // Danger Color
                    0f, // Danger Opacity
                    0f, // Danger Hue
                    0f, // Danger Sat
                    0f, // Danger Val
                }, // Danger
                new List<float>
                {
                    0f, // Rotation X
                    0f, // Rotation Y
                },
                new List<float>
                {
                    0f, // Camera Depth
                    0f, // Camera Perspective Zoom
                    0f, // Camera BG Global Position
                },
                new List<float>
                {
                    0f, // Force Window Resolution (1 = true, includes position)
					0f, // Window Resolution X
					0f, // Window Resolution Y
					0f, // Window Allow Positioning
                },
                new List<float>
                {
                    0f, // Window Position X
                },
                new List<float>
                {
                    0f, // Window Position Y
                },
                new List<float>
                {
                    0f, // Player Force X
                    0f, // Player Force Y
                }, // Player Force
                new List<float>
                {
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                }, // Analog Glitch
                new List<float>
                {
                    0f,
                }, // Digital Glitch
            };
        }

        public List<List<float>> offsets;

        #endregion

        #region Delegates

        public delegate void KFDelegate(float t);

        public KFDelegate[][] events = new KFDelegate[40][]
        {
            new KFDelegate[]
            {
                updateCameraPositionX,
                updateCameraPositionY,
            }, // Move
            new KFDelegate[]
            {
                updateCameraZoom
            }, // Rotate
            new KFDelegate[]
            {
                updateCameraRotation
            }, // Zoom
            new KFDelegate[]
            {
                updateCameraShakeMultiplier,
                updateCameraShakeX,
                updateCameraShakeY,
                updateCameraShakeSmoothness,
                updateCameraShakeSpeed
            }, // Shake
            new KFDelegate[]
            {
                updateTheme
            }, // Theme
            new KFDelegate[]
            {
                updateCameraChromatic
            }, // Chroma
            new KFDelegate[]
            {
                updateCameraBloom,
                updateCameraBloomDiffusion,
                updateCameraBloomThreshold,
                updateCameraBloomAnamorphicRatio,
                updateCameraBloomColor,
                updateCameraBloomHue,
                updateCameraBloomSat,
                updateCameraBloomVal,
            }, // Bloom
            new KFDelegate[]
            {
                updateCameraVignette,
                updateCameraVignetteSmoothness,
                updateCameraVignetteRounded,
                updateCameraVignetteRoundness,
                updateCameraVignetteCenterX,
                updateCameraVignetteCenterY,
                updateCameraVignetteColor,
                updateCameraVignetteHue,
                updateCameraVignetteSat,
                updateCameraVignetteVal,
            }, // Vignette
            new KFDelegate[]
            {
                updateCameraLens,
                updateCameraLensCenterX,
                updateCameraLensCenterY,
                updateCameraLensIntensityX,
                updateCameraLensIntensityY,
                updateCameraLensScale
            }, // Lens
            new KFDelegate[]
            {
                updateCameraGrain,
                updateCameraGrainColored,
                updateCameraGrainSize
            }, // Grain
            new KFDelegate[]
            {
                updateCameraHueShift,
                updateCameraContrast,
                updateCameraGammaX,
                updateCameraGammaY,
                updateCameraGammaZ,
                updateCameraGammaW,
                updateCameraSaturation,
                updateCameraTemperature,
                updateCameraTint
            }, // ColorGrading
            new KFDelegate[]
            {
                updateCameraRipplesStrength,
                updateCameraRipplesSpeed,
                updateCameraRipplesDistance,
                updateCameraRipplesHeight,
                updateCameraRipplesWidth,
                updateCameraRipplesMode
            }, // Ripples
            new KFDelegate[]
            {
                updateCameraRadialBlurIntensity,
                updateCameraRadialBlurIterations
            }, // RadialBlur
            new KFDelegate[]
            {
                updateCameraColorSplit,
                updateCameraColorSplitMode
            }, // ColorSplit
            new KFDelegate[]
            {
                updateCameraOffsetX,
                updateCameraOffsetY
            }, // Offset
            new KFDelegate[]
            {
                updateCameraGradientIntensity,
                updateCameraGradientRotation,
                updateCameraGradientColor1,
                updateCameraGradientColor2,
                updateCameraGradientMode,
                updateCameraGradientColor1Opacity,
                updateCameraGradientColor1Hue,
                updateCameraGradientColor1Sat,
                updateCameraGradientColor1Val,
                updateCameraGradientColor2Opacity,
                updateCameraGradientColor2Hue,
                updateCameraGradientColor2Sat,
                updateCameraGradientColor2Val,
            }, // Gradient
            new KFDelegate[]
            {
                updateCameraDoubleVision,
                updateCameraDoubleVisionMode
            }, // DoubleVision
            new KFDelegate[]
            {
                updateCameraScanLinesIntensity,
                updateCameraScanLinesAmount,
                updateCameraScanLinesSpeed
            }, // ScanLines
            new KFDelegate[]
            {
                updateCameraBlurAmount,
                updateCameraBlurIterations
            }, // Blur
            new KFDelegate[]
            {
                updateCameraPixelize
            }, // Pixelize
            new KFDelegate[]
            {
                updateCameraBGColor,
                updateCameraBGActive,
                updateCameraBGHue,
                updateCameraBGSat,
                updateCameraBGVal,
            }, // BG
            new KFDelegate[]
            {
                updateCameraInvert
            }, // Invert
            new KFDelegate[]
            {
                updateTimelineActive,
                updateTimelinePosX,
                updateTimelinePosY,
                updateTimelineScaX,
                updateTimelineScaY,
                updateTimelineRot,
                updateTimelineColor,
                updateTimelineOpacity,
                updateTimelineHue,
                updateTimelineSat,
                updateTimelineVal,
            }, // Timeline
            new KFDelegate[]
            {
                updatePlayerActive,
                updatePlayerMoveable,
                updatePlayerPositionX,
                updatePlayerPositionY,
                updatePlayerRotation,
                updatePlayerOoB
            }, // Player
            new KFDelegate[]
            {
                updateDelayTrackerActive,
                updateDelayTrackerMove,
                updateDelayTrackerRotate,
                updateDelayTrackerSharpness,
                updateDelayTrackerOffset,
                updateDelayTrackerLimitLeft,
                updateDelayTrackerLimitRight,
                updateDelayTrackerLimitUp,
                updateDelayTrackerLimitDown,
                updateDelayTrackerAnchor,
            }, // Camera Follows Player
            new KFDelegate[]
            {
                updateAudioPitch,
                updateAudioVolume
            }, // Audio
            new KFDelegate[]
            {
                updateVideoBGParentPositionX,
                updateVideoBGParentPositionY,
                updateVideoBGParentPositionZ,
                updateVideoBGParentScaleX,
                updateVideoBGParentScaleY,
                updateVideoBGParentScaleZ,
                updateVideoBGParentRotationX,
                updateVideoBGParentRotationY,
                updateVideoBGParentRotationZ,
            }, // Video BG Parent
            new KFDelegate[]
            {
                updateVideoBGPositionX,
                updateVideoBGPositionY,
                updateVideoBGPositionZ,
                updateVideoBGScaleX,
                updateVideoBGScaleY,
                updateVideoBGScaleZ,
                updateVideoBGRotationX,
                updateVideoBGRotationY,
                updateVideoBGRotationZ,
                updateVideoBGRenderLayer,
            }, // Video BG
            new KFDelegate[]
            {
                updateSharpen
            }, // Sharpen
            new KFDelegate[]
            {
                updateBlackBarsIntensity,
                updateBlackBarsMode
            }, // Bars
            new KFDelegate[]
            {
                updateDangerIntensity,
                updateDangerSize,
                updateDangerColor,
                updateCameraDangerOpacity,
                updateCameraDangerHue,
                updateCameraDangerSat,
                updateCameraDangerVal,
            }, // Danger
            new KFDelegate[]
            {
                updateCameraRotationX,
                updateCameraRotationY
            }, // 3D Rotation
            new KFDelegate[]
            {
                updateCameraDepth,
                updateCameraPerspectiveZoom,
                UpdateCameraPerspectiveGlobal
            }, // Camera Depth
            new KFDelegate[]
            {
                updateWindowForceResolution,
                updateWindowResolutionX,
                updateWindowResolutionY,
                updateWindowAllowPositioning
            }, // Window Base
            new KFDelegate[]
            {
                updateWindowPositionX
            }, // Window Position X
            new KFDelegate[]
            {
                updateWindowPositionY
            }, // Window Position Y
            new KFDelegate[]
            {
                updatePlayerForceX,
                updatePlayerForceY
            }, // Player Force
            new KFDelegate[]
            {
                updateCameraMosaic
            }, // Mosaic
            new KFDelegate[]
            {
                updateAnalogGlitchEnabled,
                updateAnalogGlitchColorDrift,
                updateAnalogGlitchHorizontalShake,
                updateAnalogGlitchScanLineJitter,
                updateAnalogGlitchVerticalJump,
            }, // Analog Glitch
            new KFDelegate[]
            {
                updateDigitalGlitchIntensity,
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
