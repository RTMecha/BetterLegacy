using System.Linq;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using HarmonyLib;

using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundManager))]
    public class BackgroundManagerPatch : MonoBehaviour
    {
		public static AudioSource Audio => AudioManager.inst.CurrentAudioSource;

		public static BackgroundManager Instance { get => BackgroundManager.inst; set => BackgroundManager.inst = value; }

		[HarmonyPatch(typeof(BackgroundManager), "Update")]
		[HarmonyPrefix]
		static void BackgroundManagerUpdatePostfix(BackgroundManager __instance)
		{
			var list = DataManager.inst.gameData is GameData gameData && gameData.backgroundObjects != null ? gameData.BackgroundObjects.Where(x => x.modifiers.Count > 0).ToList() : null;

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

		[HarmonyPatch("CreateBackgroundObject")]
		[HarmonyPrefix]
		static bool CreateBackgroundObject(ref GameObject __result, DataManager.GameData.BackgroundObject __0)
		{
			__result = Updater.CreateBackgroundObject((BackgroundObject)__0);
			return false;
		}

		public static Color bgColorToLerp;

		[HarmonyPatch("UpdateBackgroundObjects")]
		[HarmonyPrefix]
		static bool UpdateBackgroundObjects(BackgroundManager __instance)
		{
			if ((GameManager.inst.gameState == GameManager.State.Playing || LevelManager.LevelEnded && CoreConfig.Instance.ReplayLevel.Value) && BackgroundManager.inst?.backgroundParent?.gameObject)
			{
				var lerp = CoreConfig.Instance.BGReactiveLerp.Value;
				Audio?.GetSpectrumData(__instance.samples, 0, FFTWindow.Rectangular);
				__instance.sampleLow = __instance.samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
				__instance.sampleMid = __instance.samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
				__instance.sampleHigh = __instance.samples.Skip(156).Take(100).Average((float a) => a) * 6000f;

				//foreach (var bg in DataManager.inst.gameData.backgroundObjects)
				for (int bg = 0; bg < DataManager.inst.gameData.backgroundObjects.Count; bg++)
                {
					var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[bg];

					if (backgroundObject.active)
					{
						backgroundObject.BaseObject?.SetActive(backgroundObject.Enabled);
					}

					if (!backgroundObject.active || !backgroundObject.Enabled || !backgroundObject.BaseObject)
						continue;

					var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

					Color a;

					if (backgroundObject.reactive)
					{
						if (lerp)
						{
							a = RTMath.Lerp(beatmapTheme.GetBGColor(backgroundObject.color), beatmapTheme.GetBGColor(backgroundObject.reactiveCol), __instance.samples[Mathf.Clamp(backgroundObject.reactiveColSample, 0, __instance.samples.Length - 1)] * backgroundObject.reactiveColIntensity);
						}
						else
						{
							a = beatmapTheme.GetBGColor(backgroundObject.color);
							a += beatmapTheme.GetBGColor(backgroundObject.reactiveCol) * __instance.samples[Mathf.Clamp(backgroundObject.reactiveColSample, 0, __instance.samples.Length - 1)] * backgroundObject.reactiveColIntensity;
						}
					}
					else
					{
						a = beatmapTheme.GetBGColor(backgroundObject.color);
					}

					a.a = 1f;

					for (int i = 0; i < backgroundObject.renderers.Count; i++)
					{
						var renderer = backgroundObject.renderers[i];
						if (i == 0)
						{
							renderer.material.color = a;
						}
						else
						{
							int layer = backgroundObject.depth - backgroundObject.layer;
							float t = a.a / (float)layer * (float)i;
							Color b = beatmapTheme.GetBGColor(backgroundObject.FadeColor);

							if (CoreHelper.ColorMatch(b, beatmapTheme.backgroundColor, 0.05f))
							{
								b = bgColorToLerp;
								b.a = 1f;
								renderer.material.color = Color.Lerp(Color.Lerp(a, b, t), b, t);
							}
							else
							{
								b.a = 1f;
								renderer.material.color = Color.Lerp(Color.Lerp(a, b, t), b, t);
							}
						}
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
									float xr = __instance.samples[Mathf.Clamp(backgroundObject.reactiveScaSamples[0], 0, __instance.samples.Length - 1)];
									float yr = __instance.samples[Mathf.Clamp(backgroundObject.reactiveScaSamples[1], 0, __instance.samples.Length - 1)];

									backgroundObject.reactiveSize =
										new Vector2(xr * backgroundObject.reactiveScaIntensity[0], yr * backgroundObject.reactiveScaIntensity[1]) * backgroundObject.reactiveScale;
									break;
								}
						}

						float x = __instance.samples[Mathf.Clamp(backgroundObject.reactivePosSamples[0], 0, __instance.samples.Length - 1)];
						float y = __instance.samples[Mathf.Clamp(backgroundObject.reactivePosSamples[1], 0, __instance.samples.Length - 1)];

						float rot = __instance.samples[Mathf.Clamp(backgroundObject.reactiveRotSample, 0, __instance.samples.Length - 1)];

						var gameObject = backgroundObject.BaseObject;

						float z = __instance.samples[Mathf.Clamp(backgroundObject.reactiveZSample, 0, __instance.samples.Length - 1)];

						gameObject.transform.localPosition =
							new Vector3(backgroundObject.pos.x + x * backgroundObject.reactivePosIntensity[0],
							backgroundObject.pos.y + y * backgroundObject.reactivePosIntensity[1],
							32f + backgroundObject.layer * 10f + z * backgroundObject.reactiveZIntensity) + backgroundObject.positionOffset;
						gameObject.transform.localScale =
							new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
							new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
						gameObject.transform.localRotation = Quaternion.Euler(
							new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
							backgroundObject.rot + rot * backgroundObject.reactiveRotIntensity) + backgroundObject.rotationOffset);
					}
					else
					{
						backgroundObject.reactiveSize = Vector2.zero;

						var gameObject = backgroundObject.BaseObject;

						gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.layer * 10f) + backgroundObject.positionOffset;
						gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
						gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
					}
				}
			}

			return false;
		}
	}
}
