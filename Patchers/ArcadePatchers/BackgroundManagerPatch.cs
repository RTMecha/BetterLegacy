using System.Collections;
using System.Linq;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundManager))]
    public class BackgroundManagerPatch : MonoBehaviour
    {
        public static AudioSource Audio => AudioManager.inst.CurrentAudioSource;

        public static BackgroundManager Instance { get => BackgroundManager.inst; set => BackgroundManager.inst = value; }

        static System.Diagnostics.Stopwatch sw;

        [HarmonyPatch(nameof(BackgroundManager.Update))]
        [HarmonyPrefix]
        static void UpdatePrefix()
        {
            if (!CoreConfig.Instance.ShowBackgroundObjects.Value || !CoreHelper.Playing || !GameData.Current || GameData.Current.backgroundObjects == null)
                return;

            var list = GameData.Current.backgroundObjects;

            if (Input.GetKeyDown(KeyCode.Alpha2))
                sw = CoreHelper.StartNewStopwatch();

            for (int i = 0; i < list.Count; i++)
            {
                var backgroundObject = list[i];

                if (backgroundObject.modifiers.Count <= 0)
                    continue;

                for (int j = 0; j < backgroundObject.modifiers.Count; j++)
                {
                    var modifiers = backgroundObject.modifiers[j];

                    if (backgroundObject.orderModifiers)
                    {
                        ModifiersHelper.RunModifiersLoop(modifiers, true);
                        continue;
                    }

                    ModifiersHelper.RunModifiersAll(modifiers, true);
                }
            }

            if (sw != null)
            {
                CoreHelper.StopAndLogStopwatch(sw, "BackgroundManager");
                sw = null;
            }
        }

        [HarmonyPatch(nameof(BackgroundManager.CreateBackgroundObject))]
        [HarmonyPrefix]
        static bool CreateBackgroundObjectPrefix(ref GameObject __result, DataManager.GameData.BackgroundObject __0)
        {
            __result = Updater.CreateBackgroundObject((BackgroundObject)__0);
            return false;
        }

        public static Color bgColorToLerp;

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgroundObjects))]
        [HarmonyPrefix]
        static bool UpdateBackgroundObjectsPrefix(BackgroundManager __instance)
        {
            var ldm = CoreConfig.Instance.LDM.Value;
            if (CoreConfig.Instance.ShowBackgroundObjects.Value && (CoreHelper.Playing || LevelManager.LevelEnded && ArcadeHelper.ReplayLevel) && BackgroundManager.inst?.backgroundParent?.gameObject)
            {
                __instance.sampleLow = Updater.samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
                __instance.sampleMid = Updater.samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
                __instance.sampleHigh = Updater.samples.Skip(156).Take(100).Average((float a) => a) * 6000f;

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                for (int bg = 0; bg < GameData.Current.backgroundObjects.Count; bg++)
                {
                    var backgroundObject = GameData.Current.backgroundObjects[bg];

                    var gameObject = backgroundObject.BaseObject;

                    if (backgroundObject.active && gameObject && gameObject.activeSelf != backgroundObject.Enabled)
                        gameObject.SetActive(backgroundObject.Enabled);

                    if (!backgroundObject.active || !backgroundObject.Enabled || !gameObject)
                        continue;

                    Color mainColor =
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.color), backgroundObject.hue, backgroundObject.saturation, backgroundObject.value);

                    if (backgroundObject.reactive)
                        mainColor =
                            RTMath.Lerp(mainColor,
                                CoreHelper.ChangeColorHSV(
                                    beatmapTheme.GetBGColor(backgroundObject.reactiveCol),
                                    backgroundObject.hue,
                                    backgroundObject.saturation,
                                    backgroundObject.value),
                                Updater.GetSample(backgroundObject.reactiveColSample, backgroundObject.reactiveColIntensity));

                    mainColor.a = 1f;

                    var fadeColor =
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.FadeColor), backgroundObject.fadeHue, backgroundObject.fadeSaturation, backgroundObject.fadeValue);

                    if (CoreHelper.ColorMatch(fadeColor, beatmapTheme.backgroundColor, 0.05f))
                        fadeColor = bgColorToLerp;
                    fadeColor.a = 1f;

                    int layer = backgroundObject.depth - backgroundObject.layer;
                    if (ldm && backgroundObject.renderers.Count > 0)
                    {
                        backgroundObject.renderers[0].material.color = mainColor;
                        if (backgroundObject.renderers.Count > 1 && backgroundObject.renderers[1].gameObject.activeSelf)
                        {
                            for (int i = 1; i < backgroundObject.renderers.Count; i++)
                                backgroundObject.renderers[i].gameObject.SetActive(false);
                        }
                    }
                    else
                        backgroundObject.renderers.ForLoop((renderer, i) =>
                        {
                            if (i == 0)
                            {
                                renderer.material.color = mainColor;
                                return;
                            }

                            if (!renderer.gameObject.activeSelf)
                                renderer.gameObject.SetActive(true);

                            float t = 1f / layer * i;

                            renderer.material.color = Color.Lerp(Color.Lerp(mainColor, fadeColor, t), fadeColor, t);
                        });

                    if (!backgroundObject.reactive)
                    {
                        backgroundObject.reactiveSize = Vector2.zero;

                        gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.layer * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
                        continue;
                    }

                    switch (backgroundObject.reactiveType)
                    {
                        case DataManager.GameData.BackgroundObject.ReactiveType.LOW:
                            backgroundObject.reactiveSize = new Vector2(__instance.sampleLow, __instance.sampleLow) * backgroundObject.reactiveScale;
                            break;
                        case DataManager.GameData.BackgroundObject.ReactiveType.MID:
                            backgroundObject.reactiveSize = new Vector2(__instance.sampleMid, __instance.sampleMid) * backgroundObject.reactiveScale;
                            break;
                        case DataManager.GameData.BackgroundObject.ReactiveType.HIGH:
                            backgroundObject.reactiveSize = new Vector2(__instance.sampleHigh, __instance.sampleHigh) * backgroundObject.reactiveScale;
                            break;
                        case (DataManager.GameData.BackgroundObject.ReactiveType)3:
                            {
                                float xr = Updater.GetSample(backgroundObject.reactiveScaSamples[0], backgroundObject.reactiveScaIntensity[0]);
                                float yr = Updater.GetSample(backgroundObject.reactiveScaSamples[1], backgroundObject.reactiveScaIntensity[1]);

                                backgroundObject.reactiveSize = new Vector2(xr, yr) * backgroundObject.reactiveScale;
                                break;
                            }
                    }

                    float x = Updater.GetSample(backgroundObject.reactivePosSamples[0], backgroundObject.reactivePosIntensity[0]);
                    float y = Updater.GetSample(backgroundObject.reactivePosSamples[1], backgroundObject.reactivePosIntensity[1]);
                    float z = Updater.GetSample(backgroundObject.reactiveZSample, backgroundObject.reactiveZIntensity);

                    float rot = Updater.GetSample(backgroundObject.reactiveRotSample, backgroundObject.reactiveRotIntensity);

                    gameObject.transform.localPosition =
                        new Vector3(backgroundObject.pos.x + x,
                        backgroundObject.pos.y + y,
                        32f + backgroundObject.layer * 10f + z + backgroundObject.zposition) + backgroundObject.positionOffset;
                    gameObject.transform.localScale =
                        new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                        new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                    gameObject.transform.localRotation = Quaternion.Euler(
                        new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
                        backgroundObject.rot + rot) + backgroundObject.rotationOffset);
                }
            }

            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgrounds))]
        [HarmonyPrefix]
        static bool UpdateBackgrounds()
        {
            Updater.UpdateBackgroundObjects();
            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.LoadBackground))]
        [HarmonyPrefix]
        static bool LoadBackgroundPrefix(ref IEnumerator __result)
        {
            __result = LoadBackgrounds();
            return false;
        }

        static IEnumerator LoadBackgrounds()
        {
            while (!GameData.Current || GameManager.inst.gameState != GameManager.State.Playing)
                yield return null;

            Instance.audio = AudioManager.inst.CurrentAudioSource;
            Instance.samples = new float[256];
            if (Instance.audio.clip != null)
                Instance.audio.clip.GetData(Instance.samples, 0);

            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                Instance.CreateBackgroundObject(backgroundObject);

            yield break;
        }
    }
}
