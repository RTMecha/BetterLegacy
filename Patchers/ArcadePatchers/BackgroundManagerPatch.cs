using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundManager))]
    public class BackgroundManagerPatch : MonoBehaviour
    {
        public static AudioSource Audio => AudioManager.inst.CurrentAudioSource;

        public static BackgroundManager Instance { get => BackgroundManager.inst; set => BackgroundManager.inst = value; }

        [HarmonyPatch(nameof(BackgroundManager.Update))]
        [HarmonyPrefix]
        static void UpdatePrefix()
        {
            if (!GameData.IsValid || GameData.Current.backgroundObjects == null)
                return;

            var list = GameData.Current.backgroundObjects.FindAll(x => x.modifiers.Count > 0);

            if (CoreHelper.Playing)
                for (int i = 0; i < list.Count; i++)
                {
                    var backgroundObject = list[i];

                    for (int j = 0; j < backgroundObject.modifiers.Count; j++)
                    {
                        var modifiers = backgroundObject.modifiers[j];

                        modifiers.Where(x => x.Action == null || x.Trigger == null || x.Inactive == null).ToList().ForEach(delegate (Modifier<BackgroundObject> modifier)
                        {
                            modifier.Action = ModifiersHelper.BGAction;
                            modifier.Trigger = ModifiersHelper.BGTrigger;
                            modifier.Inactive = ModifiersHelper.BGInactive;
                        });

                        var actions = modifiers.Where(x => x.type == ModifierBase.Type.Action);
                        var triggers = modifiers.Where(x => x.type == ModifierBase.Type.Trigger);

                        if (triggers.Count() > 0)
                        {
                            if (triggers.All(x => !x.active && (x.Trigger(x) && !x.not || !x.Trigger(x) && x.not)))
                            {
                                foreach (var act in actions)
                                {
                                    if (!act.active)
                                    {
                                        if (!act.constant)
                                            act.active = true;

                                        act.Action?.Invoke(act);
                                    }
                                }

                                foreach (var trig in triggers)
                                {
                                    if (!trig.constant)
                                        trig.active = true;
                                }
                            }
                            else
                            {
                                foreach (var act in actions)
                                {
                                    act.active = false;
                                    act.Inactive?.Invoke(act);
                                }
                            }
                        }
                        else
                        {
                            foreach (var act in actions)
                            {
                                if (!act.active)
                                {
                                    if (!act.constant)
                                    {
                                        act.active = true;
                                    }
                                    act.Action?.Invoke(act);
                                }
                            }
                        }
                    }
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
            if ((CoreHelper.Playing || LevelManager.LevelEnded && CoreConfig.Instance.ReplayLevel.Value) && BackgroundManager.inst?.backgroundParent?.gameObject)
            {
                __instance.sampleLow = Updater.samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
                __instance.sampleMid = Updater.samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
                __instance.sampleHigh = Updater.samples.Skip(156).Take(100).Average((float a) => a) * 6000f;

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                for (int bg = 0; bg < GameData.Current.backgroundObjects.Count; bg++)
                {
                    var backgroundObject = GameData.Current.backgroundObjects[bg];

                    if (backgroundObject.active)
                        backgroundObject.BaseObject?.SetActive(backgroundObject.Enabled);

                    if (!backgroundObject.active || !backgroundObject.Enabled || !backgroundObject.BaseObject)
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
                                Updater.samples[Mathf.Clamp(backgroundObject.reactiveColSample, 0, Updater.samples.Length - 1)] * backgroundObject.reactiveColIntensity);

                    mainColor.a = 1f;

                    var fadeColor =
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.FadeColor), backgroundObject.fadeHue, backgroundObject.fadeSaturation, backgroundObject.fadeValue);

                    if (CoreHelper.ColorMatch(fadeColor, beatmapTheme.backgroundColor, 0.05f))
                        fadeColor = bgColorToLerp;
                    fadeColor.a = 1f;

                    for (int i = 0; i < backgroundObject.renderers.Count; i++)
                    {
                        var renderer = backgroundObject.renderers[i];
                        if (i == 0)
                        {
                            renderer.material.color = mainColor;
                            continue;
                        }

                        int layer = backgroundObject.depth - backgroundObject.layer;
                        float t = 1f / (float)layer * (float)i;

                        renderer.material.color = Color.Lerp(Color.Lerp(mainColor, fadeColor, t), fadeColor, t);
                    }

                    if (backgroundObject.reactive)
                    {
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
                                    float xr = Updater.samples[Mathf.Clamp(backgroundObject.reactiveScaSamples[0], 0, Updater.samples.Length - 1)];
                                    float yr = Updater.samples[Mathf.Clamp(backgroundObject.reactiveScaSamples[1], 0, Updater.samples.Length - 1)];

                                    backgroundObject.reactiveSize =
                                        new Vector2(xr * backgroundObject.reactiveScaIntensity[0], yr * backgroundObject.reactiveScaIntensity[1]) * backgroundObject.reactiveScale;
                                    break;
                                }
                        }

                        float x = Updater.samples[Mathf.Clamp(backgroundObject.reactivePosSamples[0], 0, Updater.samples.Length - 1)];
                        float y = Updater.samples[Mathf.Clamp(backgroundObject.reactivePosSamples[1], 0, Updater.samples.Length - 1)];

                        float rot = Updater.samples[Mathf.Clamp(backgroundObject.reactiveRotSample, 0, Updater.samples.Length - 1)];

                        var gameObject = backgroundObject.BaseObject;

                        float z = Updater.samples[Mathf.Clamp(backgroundObject.reactiveZSample, 0, Updater.samples.Length - 1)];

                        gameObject.transform.localPosition =
                            new Vector3(backgroundObject.pos.x + (x * backgroundObject.reactivePosIntensity[0]),
                            backgroundObject.pos.y + (y * backgroundObject.reactivePosIntensity[1]),
                            32f + backgroundObject.layer * 10f + (z * backgroundObject.reactiveZIntensity) + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale =
                            new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                            new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation = Quaternion.Euler(
                            new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
                            backgroundObject.rot + (rot * backgroundObject.reactiveRotIntensity)) + backgroundObject.rotationOffset);
                    }
                    else
                    {
                        backgroundObject.reactiveSize = Vector2.zero;

                        var gameObject = backgroundObject.BaseObject;

                        gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.layer * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
                    }
                }
            }

            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgrounds))]
        [HarmonyPrefix]
        static bool UpdateBackgrounds()
        {
            foreach (var gameObject in Instance.backgroundObjects)
                CoreHelper.Destroy(gameObject);
            Instance.backgroundObjects.Clear();

            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                Instance.CreateBackgroundObject(backgroundObject);
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
            while (!GameData.IsValid || GameManager.inst.gameState != GameManager.State.Playing)
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
