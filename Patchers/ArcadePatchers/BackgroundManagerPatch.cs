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

        [HarmonyPatch(nameof(BackgroundManager.Update))]
        [HarmonyPrefix]
        static void UpdatePrefix()
        {
            if (!CoreConfig.Instance.ShowBackgroundObjects.Value || !CoreHelper.Playing || !GameData.Current || GameData.Current.backgroundObjects == null)
                return;

            var list = GameData.Current.backgroundObjects;

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
        }

        [HarmonyPatch(nameof(BackgroundManager.CreateBackgroundObject))]
        [HarmonyPrefix]
        static bool CreateBackgroundObjectPrefix(ref GameObject __result)
        {
            __result = null;
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
                // idk if there's a better solution for this
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

                    var reactive = backgroundObject.IsReactive;

                    if (reactive)
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
                        CoreHelper.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.fadeColor), backgroundObject.fadeHue, backgroundObject.fadeSaturation, backgroundObject.fadeValue);

                    if (CoreHelper.ColorMatch(fadeColor, beatmapTheme.backgroundColor, 0.05f))
                        fadeColor = bgColorToLerp;
                    fadeColor.a = 1f;

                    int layer = backgroundObject.iterations - backgroundObject.depth;
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

                    if (!reactive)
                    {
                        backgroundObject.reactiveSize = Vector2.zero;

                        gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
                        continue;
                    }

                    backgroundObject.reactiveSize = backgroundObject.reactiveType switch
                    {
                        BackgroundObject.ReactiveType.Bass => new Vector2(__instance.sampleLow, __instance.sampleLow) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Mids => new Vector2(__instance.sampleMid, __instance.sampleMid) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Treble => new Vector2(__instance.sampleHigh, __instance.sampleHigh) * backgroundObject.reactiveScale,
                        BackgroundObject.ReactiveType.Custom => new Vector2(Updater.GetSample(backgroundObject.reactiveScaSamples.x, backgroundObject.reactiveScaIntensity.x), Updater.GetSample(backgroundObject.reactiveScaSamples.y, backgroundObject.reactiveScaIntensity.y)) * backgroundObject.reactiveScale,
                        _ => Vector2.zero,
                    };

                    if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                    {
                        gameObject.transform.localPosition =
                            new Vector3(backgroundObject.pos.x,
                            backgroundObject.pos.y,
                            32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                        gameObject.transform.localScale =
                            new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                            new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                        gameObject.transform.localRotation =
                            Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);

                        continue;
                    }

                    float x = Updater.GetSample(backgroundObject.reactivePosSamples.x, backgroundObject.reactivePosIntensity.x);
                    float y = Updater.GetSample(backgroundObject.reactivePosSamples.y, backgroundObject.reactivePosIntensity.y);
                    float z = Updater.GetSample(backgroundObject.reactiveZSample, backgroundObject.reactiveZIntensity);

                    float rot = Updater.GetSample(backgroundObject.reactiveRotSample, backgroundObject.reactiveRotIntensity);

                    gameObject.transform.localPosition =
                        new Vector3(backgroundObject.pos.x + x,
                        backgroundObject.pos.y + y,
                        32f + backgroundObject.depth * 10f + z + backgroundObject.zposition) + backgroundObject.positionOffset;
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
                Updater.CreateBackgroundObject(backgroundObject);

            yield break;
        }
    }
}
