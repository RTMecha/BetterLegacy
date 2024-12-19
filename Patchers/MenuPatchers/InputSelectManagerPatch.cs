using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InputSelectManager))]
    public class InputSelectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(InputSelectManager.Start))]
        [HarmonyPrefix]
        static void StartPrefix()
        {
            if (Menus.InterfaceManager.inst)
                Menus.InterfaceManager.inst.CurrentInterface = null; // clear menu for now until Input Select scene is reworked to use the new menu system.

            InputDataManager.inst.ClearInputs();
            LSHelpers.HideCursor();
            ArcadeHelper.fromLevel = false;

            if (MenuConfig.Instance.PlayInputSelectMusic.Value)
                SoundManager.inst.PlayMusic(DefaultMusic.loading);
        }

        [HarmonyPatch(nameof(InputSelectManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix(InputSelectManager __instance)
        {
            if (!__instance.ic.screenGlitch)
            {
                string[] array = new string[8];
                for (int i = 0; i < array.Length; i++)
                {
                    if (InputDataManager.inst.players.Count - 1 >= i)
                    {
                        var customPlayer = InputDataManager.inst.players[i];
                        if (customPlayer.active)
                        {
                            string text = "black";
                            if (customPlayer.index < __instance.playerColors.Count)
                            {
                                text = "#" + LSColors.ColorToHex(__instance.playerColors[customPlayer.index]);
                            }
                            string text2 = customPlayer.deviceType.ToString();
                            if (customPlayer.deviceType.ToString() != customPlayer.deviceModel)
                            {
                                text2 = customPlayer.deviceType.ToString() + " (" + customPlayer.deviceModel + ")";
                            }

                            array[i] = customPlayer.index < 4 ?
                                $"<color={text}><size=200%>■</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {customPlayer.index + 1}    <b>Input Device:</b> {text2}" :
                                $"<color={text}><size=200%>●</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {customPlayer.index + 1}    <b>Input Device:</b> {text2}";
                        }
                    }
                    else
                    {
                        string text3 = (__instance.randomStrings2.Count > i) ? __instance.randomStrings2[i] : "#666666";

                        array[i] = i < 4 ?
                            $"<color={text3}><size=200%>■</color><voffset=0.25em><size=100%> {((__instance.randomStrings.Count > i) ? __instance.randomStrings[i] : "")}" :
                            $"<color={text3}><size=200%>●</color><voffset=0.25em><size=100%> {((__instance.randomStrings.Count > i) ? __instance.randomStrings[i] : "")}";
                    }
                }

                int num = 7;
                foreach (string text4 in array)
                {
                    __instance.ic.Replaceline(num, text4);
                    num++;
                }

                __instance.ic.Replaceline(3, "[BACK] or [ESCAPE] to return to previous menu.");
                __instance.ic.Replaceline(4, "[PHASE] or [SPACE] to add a simulation.");
                if (InputDataManager.inst.players.Count > 0)
                {
                    if (LevelManager.IsArcade)
                        __instance.ic.Replaceline(5, "[START] or [ENTER] to choose arcade simulation.");
                    else
                        __instance.ic.Replaceline(5, "[START] or [ENTER] to start story simulation.");
                }
            }

            if (!CoreHelper.IsUsingInputField && InputDataManager.inst.players.Count > 0 && InputDataManager.inst.menuActions.Start.WasPressed)
                __instance.LoadLevel();

            return false;
        }


        [HarmonyPatch(nameof(InputSelectManager.loadStrings))]
        [HarmonyPrefix]
        static bool loadStringsPrefix(InputSelectManager __instance, ref int ___randomLength)
        {
            __instance.randomStrings.Clear();
            for (int i = 0; i < 8; i++)
            {
                __instance.randomStrings.Add($"<color={LSText.randomHex("666666")}>{LSText.randomString(___randomLength)}</color>");
                __instance.randomStrings2.Add(LSText.randomHex("666666"));
            }
            return false;
        }

        [HarmonyPatch(nameof(InputSelectManager.canChange))]
        [HarmonyPrefix]
        static bool canChangePrefix(InputSelectManager __instance, ref IEnumerator __result, ref int ___randomLength)
        {
            __result = CanChange(__instance, ___randomLength);
            return false;
        }

        static IEnumerator CanChange(InputSelectManager __instance, int ___randomLength)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Random.value < 0.5f)
                {
                    __instance.randomStrings[i] = $"<color={LSText.randomHex("666666")}>{LSText.randomString(___randomLength)}</color>";
                    __instance.randomStrings2[i] = LSText.randomHex("666666");
                }
            }
            yield return new WaitForSeconds(Random.Range(0f, 0.4f));
            __instance.StartCoroutine(CanChange(__instance, ___randomLength));
            yield break;
        }

        [HarmonyPatch(nameof(InputSelectManager.LoadLevel))]
        [HarmonyPrefix]
        static bool LoadLevelPrefix()
        {
            InputDataManager.inst.playersCanJoin = false;

            LevelManager.OnInputsSelected?.Invoke();

            if (LevelManager.OnInputsSelected != null) // if we want to run a custom function instead of doing the normal methods.
            {
                LevelManager.OnInputsSelected = null;
                return false;
            }

            if (LevelManager.IsArcade)
            {
                SceneHelper.LoadScene(SceneName.Arcade_Select, false);
                return false;
            }
            SaveManager.inst.LoadCurrentStoryLevel();
            return false;
        }
    }
}
