using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

using UnityRandom = UnityEngine.Random;
using Ease = BetterLegacy.Core.Animation.Ease;

namespace BetterLegacy.Core.Runtime.Events
{
    /// <summary>
    /// Event animation engine class.
    /// </summary>
    public class EventEngine : Exists
    {
        public EventEngine()
        {
            events = new KFDelegate[40][]
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

            offsets = CreateOffsets();
        }

        #region Updates

        /// <summary>
        /// Updates all events based on <paramref name="time"/>.
        /// </summary>
        /// <param name="time">Time since the level started.</param>
        public void Update(float time)
        {
            var allEvents = GameData.Current.events;

            // find colors first
            FindColor(time, allEvents, ref prevTheme, ref nextTheme, 4, 0);
            FindColor(time, allEvents, ref prevBloomColor, ref nextBloomColor, 6, 4);
            FindColor(time, allEvents, ref prevVignetteColor, ref nextVignetteColor, 7, 6);
            FindColor(time, allEvents, ref prevGradientColor1, ref nextGradientColor1, ref prevGradientColor2, ref nextGradientColor2, 15, 2, 3);
            FindColor(time, allEvents, ref prevBGColor, ref nextBGColor, 20, 0);
            FindColor(time, allEvents, ref prevTimelineColor, ref nextTimelineColor, 22, 6);
            FindColor(time, allEvents, ref prevDangerColor, ref nextDangerColor, 30, 2);

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

        /// <summary>
        /// Interpolates through a specific event type and value index.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <param name="valueIndex">Value index to interpolate.</param>
        /// <param name="time">Time to interpolate through.</param>
        /// <returns>Returns the interpolated value of the event track.</returns>
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

        bool IsLerper(int i, int j)
            => !(i == 4 || i == 6 && j == 4 || i == 7 && j == 6 || i == 15 && (j == 2 || j == 3) || i == 20 && j == 0 || i == 22 && j == 6 || i == 30 && j == 2);

        /// <summary>
        /// Renders the interpolated events.
        /// </summary>
        public void Render()
        {
            #region Null Checks

            if (!RTEffectsManager.inst)
                return;
            
            if (!RTEventManager.inst)
                return;
            
            if (!EventManager.inst)
                return;
            
            if (!GameManager.inst)
                return;

            if (!(CoreHelper.Playing || CoreHelper.Reversing || LevelManager.LevelEnded))
                return;

            if (!GameData.Current || GameData.Current.events == null || GameData.Current.events.IsEmpty())
                return;

            #endregion

            #region Camera

            UpdateShake();

            if (float.IsNaN(EventManager.inst.camRot))
                EventManager.inst.camRot = 0f;
            if (float.IsNaN(EventManager.inst.camZoom) || EventManager.inst.camZoom == 0f)
                EventManager.inst.camZoom = 20f;

            var editorCam = EventsConfig.Instance.EditorCameraEnabled;
            if (!editorCam)
                EventManager.inst.cam.orthographicSize = EventManager.inst.camZoom;
            else if (EventsConfig.Instance.EditorCamSpeed.Value != 0f)
                EventManager.inst.cam.orthographicSize = editorCamZoom;

            if (RTEventManager.inst.uiCam)
                RTEventManager.inst.uiCam.orthographicSize = EventManager.inst.cam.orthographicSize;

            if (!float.IsNaN(EventManager.inst.camRot) && !editorCam)
                EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(camRotOffset.x, camRotOffset.y, EventManager.inst.camRot));
            else if (!float.IsNaN(editorCamRotate))
                EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(editorCamPerRotate.x, editorCamPerRotate.y, editorCamRotate));

            var camPos = editorCam ? editorCamPosition : EventManager.inst.camPos;
            EventManager.inst.camParentTop.transform.localPosition = new Vector3(camPos.x, camPos.y, zPosition);

            EventManager.inst.camPer.fieldOfView = fieldOfView;

            var bgZoom = editorCam ? -editorCamZoom + perspectiveZoom : -EventManager.inst.camZoom + perspectiveZoom;
            if (!bgGlobalPosition)
                EventManager.inst.camPer.transform.SetLocalPositionZ(bgZoom);
            else
                EventManager.inst.camPer.transform.SetPositionZ(bgZoom);

            // fixes bg camera position being offset if rotated for some reason...
            EventManager.inst.camPer.transform.SetLocalPositionX(0f);
            EventManager.inst.camPer.transform.SetLocalPositionY(0f);

            EventManager.inst.camPer.nearClipPlane = bgAlignNearPlane ? -EventManager.inst.camPer.transform.position.z + camPerspectiveOffset : 0.3f;

            #endregion

            #region Lerp Colors

            LSEffectsManager.inst.bloom.color.Override(
                RTColors.ChangeColorHSV(LerpColor(prevBloomColor, nextBloomColor, bloomColor, RTColors.defaultBloomColor),
                bloomHue, bloomSat, bloomVal));

            LSEffectsManager.inst.vignette.color.Override(
                RTColors.ChangeColorHSV(LerpColor(prevVignetteColor, nextVignetteColor, vignetteColor, RTColors.defaultVignetteColor),
                vignetteHue, vignetteSat, vignetteVal));

            var beatmapTheme = CoreHelper.CurrentBeatmapTheme;
            ThemeManager.inst.bgColorToLerp = RTColors.ChangeColorHSV(LerpColor(prevBGColor, nextBGColor, bgColor, beatmapTheme.backgroundColor), bgHue, bgSat, bgVal);

            ThemeManager.inst.timelineColorToLerp =
                LSColors.fadeColor(RTColors.ChangeColorHSV(LerpColor(prevTimelineColor, nextTimelineColor, timelineColor, beatmapTheme.guiColor),
                timelineHue, timelineSat, timelineVal), timelineOpacity);

            var dangerColorResult =
                LSColors.fadeColor(RTColors.ChangeColorHSV(LerpColor(prevDangerColor, nextDangerColor, dangerColor, RTColors.defaultDangerColor),
                dangerHue, dangerSat, dangerVal), dangerOpacity);

            RTEffectsManager.inst.gradient.color1.Override(
                LSColors.fadeColor(RTColors.ChangeColorHSV(LerpColor(prevGradientColor1, nextGradientColor1, gradientColor1, Color.black, RTColors.defaultGradientColor1),
                gradientColor1Hue, gradientColor1Sat, gradientColor1Val), gradientColor1Opacity));

            RTEffectsManager.inst.gradient.color2.Override(
                LSColors.fadeColor(RTColors.ChangeColorHSV(LerpColor(prevGradientColor2, nextGradientColor2, gradientColor2, Color.black, RTColors.defaultGradientColor2),
                gradientColor2Hue, gradientColor2Sat, gradientColor2Val), gradientColor2Opacity));

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
            if (!float.IsNaN(pixel))
                LSEffectsManager.inst.pixelize.amount.Override(!allowFX ? 0f : pixel);

            RTEventManager.inst.analogGlitch.enabled = allowFX && analogGlitchEnabled;
            RTEventManager.inst.digitalGlitch.enabled = allowFX;

            // trying to figure out colorgrading issues... might be OS dependant?
            if (float.IsNaN(colorGradingHueShift))
            {
                CoreHelper.LogError($"ColorGrading Hueshift was not a number!");
                colorGradingHueShift = 0f;
            }

            if (float.IsNaN(colorGradingContrast))
            {
                CoreHelper.LogError($"ColorGrading Contrast was not a number!");
                colorGradingContrast = 0f;
            }

            if (float.IsNaN(colorGradingSaturation))
            {
                CoreHelper.LogError($"ColorGrading Saturation was not a number!");
                colorGradingSaturation = 0f;
            }

            if (float.IsNaN(colorGradingTint))
            {
                CoreHelper.LogError($"ColorGrading Tint was not a number!");
                colorGradingTint = 0f;
            }

            //New effects
            RTEffectsManager.inst.UpdateColorGrading(
                !allowFX ? 0f : colorGradingHueShift,
                !allowFX ? 0f : colorGradingContrast,
                !allowFX ? new Vector4(1f, 1f, 1f, 0f) : colorGradingGamma,
                !allowFX ? 0f : colorGradingSaturation,
                !allowFX ? 0f : colorGradingTemperature,
                !allowFX ? 0f : colorGradingTint);

            if (!float.IsNaN(gradientIntensity))
                RTEffectsManager.inst.UpdateGradient(!allowFX ? 0f : gradientIntensity, gradientRotation);
            if (!float.IsNaN(ripplesStrength))
                RTEffectsManager.inst.UpdateRipples(!allowFX ? 0f : ripplesStrength, ripplesSpeed, ripplesDistance, ripplesHeight, ripplesWidth, ripplesMode);
            if (!float.IsNaN(doubleVision))
                RTEffectsManager.inst.UpdateDoubleVision(!allowFX ? 0f : doubleVision, doubleVisionMode);
            if (!float.IsNaN(radialBlurIntensity))
                RTEffectsManager.inst.UpdateRadialBlur(!allowFX ? 0f : radialBlurIntensity, radialBlurIterations);
            if (!float.IsNaN(scanLinesIntensity))
                RTEffectsManager.inst.UpdateScanlines(!allowFX ? 0f : scanLinesIntensity, scanLinesAmount, scanLinesSpeed);
            if (!float.IsNaN(sharpen))
                RTEffectsManager.inst.UpdateSharpen(!allowFX ? 0f : sharpen);
            if (!float.IsNaN(colorSplitOffset))
                RTEffectsManager.inst.UpdateColorSplit(!allowFX ? 0f : colorSplitOffset, colorSplitMode);
            if (!float.IsNaN(dangerIntensity))
                RTEffectsManager.inst.UpdateDanger(!allowFX ? 0f : dangerIntensity, dangerColorResult, dangerSize);
            if (!float.IsNaN(invertAmount))
                RTEffectsManager.inst.UpdateInvert(!allowFX ? 0f : invertAmount);
            if (!float.IsNaN(blackBarsIntensity))
                RTEffectsManager.inst.UpdateBlackBars(!allowFX ? 0f : blackBarsIntensity, !allowFX ? 0f : blackBarsMode);

            if (!float.IsNaN(mosaicIntensity))
                RTEffectsManager.inst.UpdateMosaic(!allowFX ? 0f : mosaicIntensity);

            if (!float.IsNaN(timelineRot))
            {
                GameManager.inst.timeline.transform.localPosition = new Vector3(timelinePos.x, timelinePos.y, 0f);
                GameManager.inst.timeline.transform.localScale = new Vector3(timelineSca.x, timelineSca.y, 1f);
                GameManager.inst.timeline.transform.eulerAngles = new Vector3(0f, 0f, timelineRot);
            }

            RTGameManager.inst.extraBG.localPosition = videoBGParentPos;
            RTGameManager.inst.extraBG.localScale = videoBGParentSca;
            RTGameManager.inst.extraBG.localRotation = Quaternion.Euler(videoBGParentRot);

            RTGameManager.inst.video.localPosition = videoBGPos;
            RTGameManager.inst.video.localScale = videoBGSca;
            RTGameManager.inst.video.localRotation = Quaternion.Euler(videoBGRot);
            RTGameManager.inst.video.gameObject.layer = videoBGRenderLayer == 0 ? 9 : 8;

            var screenScale = (float)Display.main.systemWidth / 1920f;
            if (allowWindowPositioning && CoreHelper.InEditorPreview)
            {
                if (!setWindow)
                {
                    setWindow = true;
                    var res = DataManager.inst.resolutions[(int)CoreConfig.Instance.Resolution.Value];

                    WindowController.SetResolution((int)res.x, (int)res.y, false);
                }

                WindowController.SetWindowPos(
                    WindowController.WindowHandle, 0, (int)(windowPosition.x * screenScale) + WindowController.WindowCenter.x, -(int)(windowPosition.y * screenScale) + WindowController.WindowCenter.y,
                    forceWindow ? (int)(windowResolution.x * screenScale) : 0, forceWindow ? (int)(windowResolution.y * screenScale) : 0, forceWindow ? 0 : 1);
                windowHasChanged = true;
                RTEventManager.windowPositionResolutionChanged = true;
            }

            if (forceWindow && !allowWindowPositioning && CoreHelper.InEditorPreview)
            {
                setWindow = true;
                WindowController.SetResolution((int)(windowResolution.x * screenScale), (int)(windowResolution.y * screenScale), false);
                windowHasChanged = true;
                RTEventManager.windowPositionResolutionChanged = true;
            }

            if (CoreHelper.InEditor && EditorManager.inst.isEditing)
                setWindow = false;

            if (!forceWindow && setWindow)
            {
                setWindow = false;
                WindowController.ResetResolution(false);
            }

            foreach (var customPlayer in PlayerManager.Players)
            {
                if (customPlayer.Player && customPlayer.Player.rb)
                {
                    var player = customPlayer.Player.rb.transform;
                    if (!playersCanMove)
                    {
                        player.localPosition = new Vector3(playerPositionX, playerPositionY, 0f);
                        player.localRotation = Quaternion.Euler(0f, 0f, playerRotation);
                    }
                }
            }

            RTPlayer.PlayerForce = new Vector2(playerForceX, playerForceY) * playerForceMultiplier;

            ThemeManager.inst.UpdateThemes();

            #endregion

            GameManager.inst.timeline.SetActive(!EventsConfig.Instance.HideTimeline.Value && timelineActive && EventsConfig.Instance.ShowGUI.Value);
            EventManager.inst.prevCamZoom = EventManager.inst.camZoom;
        }

        #endregion

        #region Lerp Colors

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

        #endregion

        #region Delegates

        public delegate void KFDelegate(float t);

        /// <summary>
        /// Event update delegates.
        /// </summary>
        public KFDelegate[][] events;

        float ShakeEase(float t) => Ease.InterpolateEase(t, Mathf.Clamp(shakeSmoothness, 0f, 4f));

        /// <summary>
        /// Updates the shake sequence.
        /// </summary>
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
                var x = UnityRandom.Range(-6f, 6f);
                var y = UnityRandom.Range(-6f, 6f);
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
                t += UnityRandom.Range(0.065f, 0.07f);
            }

            list.Add(new Vector2Keyframe(t, firstKeyframe.Value, Ease.Linear));
            shakeLength = t;
            shakeSequence = new Sequence<Vector2>(list);
        }

        /// <summary>
        /// Total length of the shake sequence.
        /// </summary>
        public float shakeLength = 999f;

        /// <summary>
        /// The shake sequence, which shake is interpolated from.
        /// </summary>
        public Sequence<Vector2> shakeSequence;

        /// <summary>
        /// Renders the shake event.
        /// </summary>
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
        void UpdateCameraPositionX(float x) => EventManager.inst.camPos.x = x;
        // 0 - 1
        void UpdateCameraPositionY(float x) => EventManager.inst.camPos.y = x;

        #endregion

        #region Zoom - 1

        // 1 - 0
        void UpdateCameraZoom(float x) => EventManager.inst.camZoom = x;

        #endregion

        #region Rotate - 2

        // 2 - 0
        void UpdateCameraRotation(float x) => EventManager.inst.camRot = x;

        #endregion

        #region Shake - 3

        // 3 - 0
        void UpdateCameraShakeMultiplier(float x) => EventManager.inst.shakeMultiplier = x;

        // 3 - 1
        void UpdateCameraShakeX(float x) => shakeX = x;

        // 3 - 2
        void UpdateCameraShakeY(float x) => shakeY = x;

        // 3 - 3
        void UpdateCameraShakeSmoothness(float x) => shakeSmoothness = x;

        // 3 - 4
        void UpdateCameraShakeSpeed(float x) => shakeSpeed = x;

        #endregion

        #region Theme - 4

        // 4 - 0
        void UpdateTheme(float x)
        {
            themeLerp = x;

            ThemeManager.inst.Current.Lerp(ThemeManager.inst.GetTheme(prevTheme), ThemeManager.inst.GetTheme(nextTheme), x);
        }

        #endregion

        #region Chroma - 5

        // 5 - 0
        void UpdateCameraChromatic(float x) => EventManager.inst.camChroma = x;

        #endregion

        #region Bloom - 6

        // 6 - 0
        void UpdateCameraBloom(float x) => EventManager.inst.camBloom = x;

        // 6 - 1
        void UpdateCameraBloomDiffusion(float x) => LSEffectsManager.inst.bloom.diffusion.Override(x);

        // 6 - 2
        void UpdateCameraBloomThreshold(float x) => LSEffectsManager.inst.bloom.threshold.Override(x);

        // 6 - 3
        void UpdateCameraBloomAnamorphicRatio(float x) => LSEffectsManager.inst.bloom.anamorphicRatio.Override(x);

        // 6 - 4
        void UpdateCameraBloomColor(float x) => bloomColor = x;

        // 6 - 5
        void UpdateCameraBloomHue(float x) => bloomHue = x;

        // 6 - 6
        void UpdateCameraBloomSat(float x) => bloomSat = x;

        // 6 - 7
        void UpdateCameraBloomVal(float x) => bloomVal = x;

        #endregion

        #region Vignette - 7

        // 7 - 0
        void UpdateCameraVignette(float x) => EventManager.inst.vignetteIntensity = x;

        // 7 - 1
        void UpdateCameraVignetteSmoothness(float x) => EventManager.inst.vignetteSmoothness = x;

        // 7 - 2
        void UpdateCameraVignetteRounded(float x) => EventManager.inst.vignetteRounded = x;

        // 7 - 3
        void UpdateCameraVignetteRoundness(float x) => EventManager.inst.vignetteRoundness = x;

        // 7 - 4
        void UpdateCameraVignetteCenterX(float x) => EventManager.inst.vignetteCenter.x = x;

        // 7 - 5
        void UpdateCameraVignetteCenterY(float x) => EventManager.inst.vignetteCenter.y = x;

        // 7 - 6
        void UpdateCameraVignetteColor(float x) => vignetteColor = x;

        // 7 - 7
        void UpdateCameraVignetteHue(float x) => vignetteHue = x;

        // 7 - 8
        void UpdateCameraVignetteSat(float x) => vignetteSat = x;

        // 7 - 9
        void UpdateCameraVignetteVal(float x) => vignetteVal = x;

        #endregion

        #region Lens - 8

        // 8 - 0
        void UpdateCameraLens(float x) => EventManager.inst.lensDistortIntensity = x;

        // 8 - 1
        void UpdateCameraLensCenterX(float x) => LSEffectsManager.inst.lensDistort.centerX.Override(x);

        // 8 - 2
        void UpdateCameraLensCenterY(float x) => LSEffectsManager.inst.lensDistort.centerY.Override(x);

        // 8 - 3
        void UpdateCameraLensIntensityX(float x) => LSEffectsManager.inst.lensDistort.intensityX.Override(x);

        // 8 - 4
        void UpdateCameraLensIntensityY(float x) => LSEffectsManager.inst.lensDistort.intensityY.Override(x);

        // 8 - 5
        void UpdateCameraLensScale(float x) => LSEffectsManager.inst.lensDistort.scale.Override(x);

        #endregion

        #region Grain - 9

        // 9 - 0
        void UpdateCameraGrain(float x) => EventManager.inst.grainIntensity = x;

        // 9 - 1
        void UpdateCameraGrainColored(float _colored) => EventManager.inst.grainColored = _colored;

        // 9 - 2
        void UpdateCameraGrainSize(float x) => EventManager.inst.grainSize = x;

        #endregion

        #region ColorGrading - 10

        // 10 - 0
        void UpdateCameraHueShift(float x) => colorGradingHueShift = x;

        // 10 - 1
        void UpdateCameraContrast(float x) => colorGradingContrast = x;

        // 10 - 2
        void UpdateCameraGammaX(float x) => colorGradingGamma.x = x;

        // 10 - 3
        void UpdateCameraGammaY(float x) => colorGradingGamma.y = x;

        // 10 - 4
        void UpdateCameraGammaZ(float x) => colorGradingGamma.z = x;

        // 10 - 5
        void UpdateCameraGammaW(float x) => colorGradingGamma.w = x;

        // 10 - 6
        void UpdateCameraSaturation(float x) => colorGradingSaturation = x;

        // 10 - 7
        void UpdateCameraTemperature(float x) => colorGradingTemperature = x;

        // 10 - 8
        void UpdateCameraTint(float x) => colorGradingTint = x;

        #endregion

        #region Ripples - 11

        // 11 - 0
        void UpdateCameraRipplesStrength(float x) => ripplesStrength = x;

        // 11 - 1
        void UpdateCameraRipplesSpeed(float x) => ripplesSpeed = x;

        // 11 - 2
        void UpdateCameraRipplesDistance(float x) => ripplesDistance = Mathf.Clamp(x, 0.0001f, float.PositiveInfinity);

        // 11 - 3
        void UpdateCameraRipplesHeight(float x) => ripplesHeight = x;

        // 11 - 4
        void UpdateCameraRipplesWidth(float x) => ripplesWidth = x;

        // 11 - 5
        void UpdateCameraRipplesMode(float x) => ripplesMode = (int)x;

        #endregion

        #region RadialBlur - 12

        // 12 - 0
        void UpdateCameraRadialBlurIntensity(float x) => radialBlurIntensity = x;

        // 12 - 1
        void UpdateCameraRadialBlurIterations(float x) => radialBlurIterations = Mathf.Clamp((int)x, 1, 30);

        #endregion

        #region ColorSplit - 13

        // 13 - 0
        void UpdateCameraColorSplit(float x) => colorSplitOffset = x;

        // 13 - 1
        void UpdateCameraColorSplitMode(float x) => colorSplitMode = (int)x;

        #endregion

        #region Offset - 14

        // 14 - 0
        void UpdateCameraOffsetX(float x) => camOffsetX = x;

        // 14 - 1
        void UpdateCameraOffsetY(float x) => camOffsetY = x;

        #endregion

        #region Gradient - 15

        // 15 - 0
        void UpdateCameraGradientIntensity(float x) => gradientIntensity = x;

        // 15 - 1
        void UpdateCameraGradientRotation(float x) => gradientRotation = x;

        // 15 - 2
        void UpdateCameraGradientColor1(float x) => gradientColor1 = x;

        // 15 - 3
        void UpdateCameraGradientColor2(float x) => gradientColor2 = x;

        // 15 - 4
        void UpdateCameraGradientMode(float x) => RTEffectsManager.inst.gradient.blendMode.Override((SCPE.Gradient.BlendMode)(int)x);

        // 15 - 5
        void UpdateCameraGradientColor1Opacity(float x) => gradientColor1Opacity = x;

        // 15 - 6
        void UpdateCameraGradientColor1Hue(float x) => gradientColor1Hue = x;

        // 15 - 7
        void UpdateCameraGradientColor1Sat(float x) => gradientColor1Sat = x;

        // 15 - 8
        void UpdateCameraGradientColor1Val(float x) => gradientColor1Val = x;

        // 15 - 5
        void UpdateCameraGradientColor2Opacity(float x) => gradientColor2Opacity = x;

        // 15 - 6
        void UpdateCameraGradientColor2Hue(float x) => gradientColor2Hue = x;

        // 15 - 7
        void UpdateCameraGradientColor2Sat(float x) => gradientColor2Sat = x;

        // 15 - 8
        void UpdateCameraGradientColor2Val(float x) => gradientColor2Val = x;

        #endregion

        #region DoubleVision - 16

        // 16 - 0
        void UpdateCameraDoubleVision(float x) => doubleVision = x;

        // 16 - 1
        void UpdateCameraDoubleVisionMode(float x) => doubleVisionMode = (int)x;

        #endregion

        #region ScanLines - 17

        // 17 - 0
        void UpdateCameraScanLinesIntensity(float x) => scanLinesIntensity = x;

        // 17 - 1
        void UpdateCameraScanLinesAmount(float x) => scanLinesAmount = x;

        // 17 - 2
        void UpdateCameraScanLinesSpeed(float x) => scanLinesSpeed = x;

        #endregion

        #region Blur - 18

        // 18 - 0
        void UpdateCameraBlurAmount(float x) => LSEffectsManager.inst.blur.amount.Override(!EventsConfig.Instance.ShowFX.Value ? 0f : x);

        // 18 - 1
        void UpdateCameraBlurIterations(float x) => LSEffectsManager.inst.blur.iterations.Override(Mathf.Clamp((int)x, 1, 30));

        #endregion

        #region Pixelize - 19

        // 19 - 0
        void UpdateCameraPixelize(float x) => pixel = Mathf.Clamp(x, 0f, 0.99999f);

        #endregion

        #region BG - 20

        // 20 - 0
        void UpdateCameraBGColor(float x) => bgColor = x;

        // 20 - 1
        void UpdateCameraBGActive(float x)
        {
            bgActive = x;

            var i = (int)x;

            BackgroundManager.inst?.backgroundParent?.gameObject?.SetActive(i == 0);
        }

        float bgActive = 0f;

        // 20 - 2
        void UpdateCameraBGHue(float x) => bgHue = x;

        // 20 - 3
        void UpdateCameraBGSat(float x) => bgSat = x;

        // 20 - 4
        void UpdateCameraBGVal(float x) => bgVal = x;

        #endregion

        #region Invert - 21

        // 21 - 0
        void UpdateCameraInvert(float x) => invertAmount = x;

        #endregion

        #region Timeline - 22

        // 22 - 0
        void UpdateTimelineActive(float x) => timelineActive = (int)x == 0;

        // 22 - 1
        void UpdateTimelinePosX(float x) => timelinePos.x = x;

        // 22 - 2
        void UpdateTimelinePosY(float x) => timelinePos.y = x;

        // 22 - 3
        void UpdateTimelineScaX(float x) => timelineSca.x = x;

        // 22 - 4
        void UpdateTimelineScaY(float x) => timelineSca.y = x;

        // 22 - 5
        void UpdateTimelineRot(float x) => timelineRot = x;

        // 22 - 6
        void UpdateTimelineColor(float x) => timelineColor = x;

        // 22 - 7
        void UpdateTimelineOpacity(float x) => timelineOpacity = x;

        // 22 - 8
        void UpdateTimelineHue(float x) => timelineHue = x;

        // 22 - 9
        void UpdateTimelineSat(float x) => timelineSat = x;

        // 22 - 10
        void UpdateTimelineVal(float x) => timelineVal = x;

        #endregion

        #region Player - 23

        // 23 - 0
        void UpdatePlayerActive(float x)
        {
            var active = (int)x == 0;

            var zen = !CoreHelper.InStory && (PlayerManager.IsZenMode || CoreHelper.InEditor);

            var a = active && !zen || active && EventsConfig.Instance.ShowGUI.Value;

            if (GameManager.inst.gameState == (GameManager.State.Paused | GameManager.State.Finish) && LevelManager.LevelEnded)
                a = false;

            GameManager.inst.players.SetActive(a);
            playersActive = a;
        }

        // 23 - 1
        void UpdatePlayerMoveable(float x)
        {
            playersCanMove = (int)x == 0;
            foreach (var customPlayer in PlayerManager.Players)
            {
                if (customPlayer.Player)
                {
                    customPlayer.Player.CanMove = playersCanMove;
                    customPlayer.Player.CanRotate = playersCanMove;
                }
            }
        }

        // 23 - 2
        void UpdatePlayerPositionX(float x) => playerPositionX = x;

        // 23 - 3
        void UpdatePlayerPositionY(float x) => playerPositionY = x;

        // 23 - 4
        void UpdatePlayerRotation(float x) => playerRotation = x;

        // 23 - 5
        void UpdatePlayerOoB(float x) => RTPlayer.OutOfBounds = x == 1f;

        #endregion

        #region Camera Follows Player - 24

        // 24 - 0
        void UpdateDelayTrackerActive(float x) => RTEventManager.inst.delayTracker.active = (int)x == 1;

        // 24 - 1
        void UpdateDelayTrackerMove(float x) => RTEventManager.inst.delayTracker.move = (int)x == 1;

        // 24 - 2
        void UpdateDelayTrackerRotate(float x) => RTEventManager.inst.delayTracker.rotate = (int)x == 1;

        // 24 - 3
        void UpdateDelayTrackerSharpness(float x) => RTEventManager.inst.delayTracker.followSharpness = Mathf.Clamp(x, 0.001f, 1f);

        // 24 - 4
        void UpdateDelayTrackerOffset(float x) => RTEventManager.inst.delayTracker.offset = x;

        // 24 - 5
        void UpdateDelayTrackerLimitLeft(float x) => RTEventManager.inst.delayTracker.limitLeft = x;

        // 24 - 6
        void UpdateDelayTrackerLimitRight(float x) => RTEventManager.inst.delayTracker.limitRight = x;

        // 24 - 7
        void UpdateDelayTrackerLimitUp(float x) => RTEventManager.inst.delayTracker.limitUp = x;

        // 24 - 8
        void UpdateDelayTrackerLimitDown(float x) => RTEventManager.inst.delayTracker.limitDown = x;

        // 24 - 9
        void UpdateDelayTrackerAnchor(float x) => RTEventManager.inst.delayTracker.anchor = x;

        #endregion

        #region Audio - 25

        // 25 - 0
        void UpdateAudioPitch(float x) => AudioManager.inst.pitch = Mathf.Clamp(x, 0.001f, 10f) * CoreHelper.Pitch * pitchOffset;

        // 25 - 1
        void UpdateAudioVolume(float x) => audioVolume = Mathf.Clamp(x, 0f, 1f);

        #endregion

        #region Video BG Parent - 26

        // 26 - 0
        void UpdateVideoBGParentPositionX(float x) => videoBGParentPos.x = x;
        // 26 - 1
        void UpdateVideoBGParentPositionY(float x) => videoBGParentPos.y = x;
        // 26 - 2
        void UpdateVideoBGParentPositionZ(float x) => videoBGParentPos.z = x;
        // 26 - 3
        void UpdateVideoBGParentScaleX(float x) => videoBGParentSca.x = x;
        // 26 - 4
        void UpdateVideoBGParentScaleY(float x) => videoBGParentSca.y = x;
        // 26 - 5
        void UpdateVideoBGParentScaleZ(float x) => videoBGParentSca.z = x;
        // 26 - 6
        void UpdateVideoBGParentRotationX(float x) => videoBGParentRot.x = x;
        // 26 - 7
        void UpdateVideoBGParentRotationY(float x) => videoBGParentRot.y = x;
        // 26 - 8
        void UpdateVideoBGParentRotationZ(float x) => videoBGParentRot.z = x;

        #endregion

        #region Video BG - 27

        // 27 - 0
        void UpdateVideoBGPositionX(float x) => videoBGPos.x = x;
        // 27 - 1
        void UpdateVideoBGPositionY(float x) => videoBGPos.y = x;
        // 27 - 2
        void UpdateVideoBGPositionZ(float x) => videoBGPos.z = x;
        // 27 - 3
        void UpdateVideoBGScaleX(float x) => videoBGSca.x = x;
        // 27 - 4
        void UpdateVideoBGScaleY(float x) => videoBGSca.y = x;
        // 27 - 5
        void UpdateVideoBGScaleZ(float x) => videoBGSca.z = x;
        // 27 - 6
        void UpdateVideoBGRotationX(float x) => videoBGRot.x = x;
        // 27 - 7
        void UpdateVideoBGRotationY(float x) => videoBGRot.y = x;
        // 27 - 8
        void UpdateVideoBGRotationZ(float x) => videoBGRot.z = x;
        // 27 - 9
        void UpdateVideoBGRenderLayer(float x) => videoBGRenderLayer = (int)x;

        #endregion

        #region Sharpen - 28

        // 28 - 0
        void UpdateSharpen(float x) => sharpen = x;

        #endregion

        #region Bars - 29

        // 29 - 0
        void UpdateBlackBarsIntensity(float x) => blackBarsIntensity = x;
        // 29 - 1
        void UpdateBlackBarsMode(float x) => blackBarsMode = x;

        #endregion

        #region Danger - 30

        // 30 - 0
        void UpdateDangerIntensity(float x) => dangerIntensity = x;
        // 30 - 1
        void UpdateDangerSize(float x) => dangerSize = x;
        // 30 - 2
        void UpdateDangerColor(float x) => dangerColor = x;

        // 30 - 3
        void UpdateCameraDangerOpacity(float x) => dangerOpacity = x;

        // 30 - 4
        void UpdateCameraDangerHue(float x) => dangerHue = x;

        // 30 - 5
        void UpdateCameraDangerSat(float x) => dangerSat = x;

        // 30 - 6
        void UpdateCameraDangerVal(float x) => dangerVal = x;

        #endregion

        #region 3D Rotation - 31

        // 31 - 0
        void UpdateCameraRotationX(float x) => camRotOffset.x = x;
        // 31 - 1
        void UpdateCameraRotationY(float x) => camRotOffset.y = x;

        #endregion

        #region Camera Depth - 32

        // 32 - 0
        void UpdateCameraDepth(float x) => zPosition = x;
        // 32 - 1
        void UpdateCameraPerspectiveZoom(float x) => perspectiveZoom = x;
        // 32 - 2
        void UpdateCameraPerspectiveGlobal(float x) => bgGlobalPosition = x == 0f;
        // 32 - 3
        void UpdateCameraPerspectiveAlign(float x) => bgAlignNearPlane = x == 1f;

        #endregion

        #region Window Base - 33

        // 33 - 0
        void UpdateWindowForceResolution(float x) => forceWindow = (int)x == 1;
        // 33 - 1
        void UpdateWindowResolutionX(float x) => windowResolution.x = x;
        // 33 - 2
        void UpdateWindowResolutionY(float x) => windowResolution.y = x;
        // 33 - 3
        void UpdateWindowAllowPositioning(float x) => allowWindowPositioning = (int)x == 1;

        #endregion

        #region Window Position X - 34

        // 34 - 0
        void UpdateWindowPositionX(float x) => windowPosition.x = x;

        #endregion

        #region Window Position Y - 35

        // 35 - 0
        void UpdateWindowPositionY(float x) => windowPosition.y = x;

        #endregion

        #region Player Force - 36

        // 36 - 0
        void UpdatePlayerForceX(float x) => playerForceX = x;

        // 36 - 1
        void UpdatePlayerForceY(float x) => playerForceY = x;

        #endregion

        #region Mosaic - 37

        // 37 - 0
        void UpdateCameraMosaic(float x) => mosaicIntensity = x;

        #endregion

        #region Analog Glitch - 38

        // 38 - 0
        void UpdateAnalogGlitchEnabled(float x) => analogGlitchEnabled = (int)x == 1;

        // 38 - 1
        void UpdateAnalogGlitchColorDrift(float x)
        {
            if (RTEventManager.inst.analogGlitch)
                RTEventManager.inst.analogGlitch.colorDrift = x;
        }

        // 38 - 2
        void UpdateAnalogGlitchHorizontalShake(float x)
        {
            if (RTEventManager.inst.analogGlitch)
                RTEventManager.inst.analogGlitch.horizontalShake = x;
        }

        // 38 - 3
        void UpdateAnalogGlitchScanLineJitter(float x)
        {
            if (RTEventManager.inst.analogGlitch)
                RTEventManager.inst.analogGlitch.scanLineJitter = x;
        }

        // 38 - 4
        void UpdateAnalogGlitchVerticalJump(float x)
        {
            if (RTEventManager.inst.analogGlitch)
                RTEventManager.inst.analogGlitch.verticalJump = x;
        }

        #endregion

        #region Digital Glitch - 39

        // 39 - 0
        void UpdateDigitalGlitchIntensity(float x)
        {
            if (RTEventManager.inst.digitalGlitch)
                RTEventManager.inst.digitalGlitch.intensity = x;
        }

        #endregion

        #endregion

        #region Variables

        public float previousAudioTime;

        public float fieldOfView = 50f;
        public float camPerspectiveOffset = 10f;

        public bool windowHasChanged;

        public static float playerForceMultiplier = 0.036f;

        /// <summary>
        /// The current shake speed.
        /// </summary>
        public float shakeSpeed = 1f;

        /// <summary>
        /// The current shake smoothness.
        /// </summary>
        public float shakeSmoothness = 1f;

        public float shakeTime;

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

        /// <summary>
        /// Updates the editor offset camera.
        /// </summary>
        public void UpdateEditorCamera()
        {
            if (!EditorManager.inst)
                return;

            var editorCamera = RTEventManager.inst.editorCamera;
            var editorSpeed = EventsConfig.Instance.EditorCamSpeed.Value;

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

                var vector = new Vector3(x * editorSpeed * multiplyController, y * editorSpeed * multiplyController, 0f);
                if (vector.magnitude > 1f)
                    vector = vector.normalized;

                editorCamPosition.x += vector.x;
                editorCamPosition.y += vector.y;

                if (editorCamera.ZoomOut.IsPressed)
                    editorCamZoom += 0.5f * editorSpeed * multiplyController;
                if (editorCamera.ZoomIn.IsPressed)
                    editorCamZoom -= 0.5f * editorSpeed * multiplyController;

                if (editorCamera.RotateAdd.IsPressed)
                    editorCamRotate += 0.1f * editorSpeed * multiplyController;
                if (editorCamera.RotateSub.IsPressed)
                    editorCamRotate -= 0.1f * editorSpeed * multiplyController;

                var xRot = editorCamera.Rotate.Vector.x;
                var yRot = editorCamera.Rotate.Vector.y;
                var vectorRot = new Vector3(xRot * editorSpeed * multiplyController, yRot * editorSpeed * multiplyController, 0f);
                if (vectorRot.magnitude > 1f)
                    vectorRot = vectorRot.normalized;

                editorCamPerRotate.x += vectorRot.x;
                editorCamPerRotate.y += vectorRot.y;

                if (editorCamera.ResetOffsets.WasPressed)
                {
                    editorCamPosition = EventManager.inst.camPos;
                    if (!float.IsNaN(EventManager.inst.camZoom))
                        editorCamZoom = EventManager.inst.camZoom;
                    if (!float.IsNaN(EventManager.inst.camRot))
                        editorCamRotate = EventManager.inst.camRot;
                    editorCamPerRotate = Vector2.zero;
                }

                if (CoreHelper.IsUsingInputField || !EventsConfig.Instance.EditorCamUseKeys.Value)
                    return;

                float multiply = 1f;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    multiply = 0.5f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    multiply = 2f;

                if (Input.GetKey(KeyCode.A))
                    editorCamPosition.x -= 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.D))
                    editorCamPosition.x += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.W))
                    editorCamPosition.y += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.S))
                    editorCamPosition.y -= 0.1f * editorSpeed * multiply;

                if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
                    editorCamZoom += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
                    editorCamZoom -= 0.1f * editorSpeed * multiply;

                if (Input.GetKey(KeyCode.Keypad4))
                    editorCamRotate += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.Keypad6))
                    editorCamRotate -= 0.1f * editorSpeed * multiply;

                if (Input.GetKey(KeyCode.LeftArrow))
                    editorCamPerRotate.y += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.RightArrow))
                    editorCamPerRotate.y -= 0.1f * editorSpeed * multiply;

                if (Input.GetKey(KeyCode.UpArrow))
                    editorCamPerRotate.x += 0.1f * editorSpeed * multiply;
                if (Input.GetKey(KeyCode.DownArrow))
                    editorCamPerRotate.x -= 0.1f * editorSpeed * multiply;

                if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    editorCamPosition = EventManager.inst.camPos;
                    if (!float.IsNaN(EventManager.inst.camZoom))
                        editorCamZoom = EventManager.inst.camZoom;
                    if (!float.IsNaN(EventManager.inst.camRot))
                        editorCamRotate = EventManager.inst.camRot;
                    editorCamPerRotate = Vector2.zero;
                }
            }
            else if (EventsConfig.Instance.EditorCamResetValues.Value)
            {
                editorCamPosition = EventManager.inst.camPos;
                if (!float.IsNaN(EventManager.inst.camZoom))
                    editorCamZoom = EventManager.inst.camZoom;
                if (!float.IsNaN(EventManager.inst.camRot))
                    editorCamRotate = EventManager.inst.camRot;
                editorCamPerRotate = Vector2.zero;
            }
        }

        /// <summary>
        /// Editor offset camera position.
        /// </summary>
        public Vector2 editorCamPosition = Vector2.zero;

        /// <summary>
        /// Editor offset camera zoom.
        /// </summary>
        public float editorCamZoom = 20f;

        /// <summary>
        /// Editor offset camera rotation.
        /// </summary>
        public float editorCamRotate = 0f;

        /// <summary>
        /// Editor offset camera perspective rotation.
        /// </summary>
        public Vector2 editorCamPerRotate = Vector2.zero;

        #endregion

        #region Offsets

        /// <summary>
        /// Pitch multiply offset.
        /// </summary>
        public float pitchOffset = 1f;

        /// <summary>
        /// Sets an event offset.
        /// </summary>
        /// <param name="eventType">Event type to offset.</param>
        /// <param name="indexValue">Event value index to offset.</param>
        /// <param name="value">Value to add to the event when rendering.</param>
        public void SetOffset(int eventType, int indexValue, float value)
        {
            if (offsets.InRange(eventType) && offsets[eventType].InRange(indexValue))
                offsets[eventType][indexValue] = value;
        }

        /// <summary>
        /// Resets the event offsets.
        /// </summary>
        public void ResetOffsets() => offsets = CreateOffsets();

        List<List<float>> CreateOffsets()
        {
            var list = new List<List<float>>();
            for (int i = 0; i < events.Length; i++)
            {
                list.Add(new List<float>());
                for (int j = 0; j < events[i].Length; j++)
                    list[i].Add(0f);
            }
            return list;
        }

        /// <summary>
        /// Offsets that are added to each event. Good for dynamic events in levels.
        /// </summary>
        public List<List<float>> offsets;

        #endregion
    }
}
