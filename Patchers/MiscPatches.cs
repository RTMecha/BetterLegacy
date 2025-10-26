using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using HarmonyLib;

using MP3Sharp;
using MP3Sharp.Decoding.Decoders;
using MP3Sharp.Decoding.Decoders.LayerIII;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(Dropdown))]
    public class DropdownPatch
    {
        [HarmonyPatch(nameof(Dropdown.Show))]
        [HarmonyPostfix]
        static void ShowPostfix(Dropdown __instance)
        {
            if (__instance.gameObject && __instance.transform && __instance.gameObject.TryGetComponent(out HideDropdownOptions hideDropdownOptions))
            {
                var content = __instance.transform.Find("Dropdown List/Viewport/Content");
                for (int i = 0; i < hideDropdownOptions.DisabledOptions.Count; i++)
                {
                    if (!(content.childCount > i + 1))
                        continue;

                    if (hideDropdownOptions.remove && hideDropdownOptions.DisabledOptions[i])
                        content.GetChild(i + 1).gameObject.SetActive(false);
                    else
                        content.GetChild(i + 1).GetComponent<Toggle>().interactable = !hideDropdownOptions.DisabledOptions[i];

                    content.GetChild(i + 1).AsRT().sizeDelta = new Vector2(0f, 32f);
                }
            }
        }

        [HarmonyPatch(nameof(Dropdown.OnPointerClick))]
        [HarmonyPrefix]
        static bool OnPointerClickPrefix(Dropdown __instance, PointerEventData __0)
        {
            if (__0.button == PointerEventData.InputButton.Left) // require left click so it doesn't get in the way of context menus
                __instance.Show();
            return false;
        }
    }

    [HarmonyPatch(typeof(HideDropdownOptions))]
    public class HideDropdownOptionsPatch
    {
        [HarmonyPatch(nameof(HideDropdownOptions.OnPointerClick))]
        [HarmonyPrefix]
        static bool OnPointerClickPrefix() => false;
    }

    [HarmonyPatch(typeof(DropdownHovered))]
    public class DropdownHoveredPatch
    {
        [HarmonyPatch(nameof(DropdownHovered.OnPointerEnter))]
        [HarmonyPrefix]
        static bool OnPointerEnterPrefix(DropdownHovered __instance, PointerEventData __0)
        {
            var dropdown = __instance.GetComponent<Dropdown>();
            if (!dropdown || !EditorConfig.Instance.ShowDropdownOnHover.Value)
                return false;

            dropdown.Show();
            return false;
        }
    }

    [HarmonyPatch(typeof(LSHelpers))]
    public class LSHelpersPatch
    {
        [HarmonyPatch(nameof(LSHelpers.IsUsingInputField))]
        [HarmonyPrefix]
        static bool IsUsingInputFieldPrefix(ref bool __result)
        {
            __result = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>());
            return false;
        }
    }

    [HarmonyPatch(typeof(LSAudio))]
    public class LSAudioPatch
    {
        [HarmonyPatch(nameof(LSAudio.CreateAudioClipUsingMP3File))]
        [HarmonyPrefix]
        static bool CreateAudioClipUsingMP3FilePrefix(ref AudioClip __result, string __0)
        {
            __result = CreateAudioClipUsingMP3File(__0);
            return false;
        }

        static AudioClip CreateAudioClipUsingMP3File(string path)
        {
            var buffer = new byte[4096];
            var data = new List<float>();
            using var mp3Stream = new MP3Stream(path);
            int num = 4096;
            while (num == 4096 && mp3Stream.Length - mp3Stream.Position >= 1000L)
            {
                num = mp3Stream.Read(buffer, 0, 4096);
                for (int i = 0; i < num; i += 2)
                {
                    byte b = buffer[i];
                    data.Add(((short)((buffer[i + 1] << 8) | b)) * 1f / 32767f);
                }
            }
            int frequency = mp3Stream.Frequency;
            short channelCount = mp3Stream.ChannelCount;
            int num3 = data.Count / channelCount;
            var audioClip = AudioClip.Create("audio", num3, channelCount, frequency, false);
            audioClip.SetData(data.ToArray(), 0);
            return audioClip;
        }
    }

    [HarmonyPatch(typeof(LSText))]
    public class LSTextPatch
    {
        [HarmonyPatch(nameof(LSText.ClampString))]
        [HarmonyPrefix]
        static bool ClampStringPrefix(ref string __result, string __0, int __1)
        {
            var input = __0;
            var maxLength = __1;

            if (string.IsNullOrEmpty(input))
            {
                __result = input;
                return false;
            }

            if (input.Length > maxLength)
            {
                __result = input.Substring(0, maxLength - 3) + "...";
                return false;
            }
            if (input.Length < maxLength)
            {
                string text = string.Empty;
                for (int i = 0; i < maxLength - input.Length; i++)
                    text += " ";
                __result = input + text;
                return false;
            }
            __result = input;
            return false;
        }
    }

    [HarmonyPatch(typeof(LayerIIIDecoder))]
    public class LayerDecoderPatch
    {
        [HarmonyPatch("dequantize_sample")]
        [HarmonyPrefix]
        static bool dequantize_samplePrefix(LayerIIIDecoder __instance, float[][] xr, int ch, int gr)
        {
            try
            {
                GranuleInfo granuleInfo = __instance.m_SideInfo.Channels[ch].Granules[gr];
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                int num4 = 0;
                int num5;
                if (granuleInfo.WindowSwitchingFlag != 0 && granuleInfo.BlockType == 2)
                {
                    if (granuleInfo.MixedBlockFlag != 0)
                    {
                        num5 = __instance.sfBandIndex[__instance.sfreq].l[1];
                    }
                    else
                    {
                        num3 = __instance.sfBandIndex[__instance.sfreq].s[1];
                        num5 = (num3 << 2) - num3;
                        num2 = 0;
                    }
                }
                else
                {
                    num5 = __instance.sfBandIndex[__instance.sfreq].l[1];
                }

                // problematic area
                float num6 = (float)Math.Pow(2.0, 0.25 * ((double)granuleInfo.GlobalGain - 210.0));
                for (int i = 0; i < __instance.nonzero[ch]; i++)
                {
                    int num7 = i % 18;
                    int num8 = (i - num7) / 18;
                    if (__instance.is_1d[i] == 0)
                    {
                        xr[num8][num7] = 0f;
                    }
                    else
                    {
                        int num9 = __instance.is_1d[i];
                        if (__instance.is_1d[i] > 0)
                        {
                            xr[num8][num7] = num6 * LayerIIIDecoder.t_43[num9];
                        }
                        else
                        {
                            xr[num8][num7] = -num6 * LayerIIIDecoder.t_43[-num9];
                        }
                    }
                }

                // dunno if the problem is after this
                for (int i = 0; i < __instance.nonzero[ch]; i++)
                {
                    int num10 = i % 18;
                    int num11 = (i - num10) / 18;
                    if (num4 == num5)
                    {
                        if (granuleInfo.WindowSwitchingFlag != 0 && granuleInfo.BlockType == 2)
                        {
                            if (granuleInfo.MixedBlockFlag != 0)
                            {
                                if (num4 == __instance.sfBandIndex[__instance.sfreq].l[8])
                                {
                                    num5 = __instance.sfBandIndex[__instance.sfreq].s[4];
                                    num5 = (num5 << 2) - num5;
                                    num = 3;
                                    num3 = __instance.sfBandIndex[__instance.sfreq].s[4] - __instance.sfBandIndex[__instance.sfreq].s[3];
                                    num2 = __instance.sfBandIndex[__instance.sfreq].s[3];
                                    num2 = (num2 << 2) - num2;
                                }
                                else if (num4 < __instance.sfBandIndex[__instance.sfreq].l[8])
                                {
                                    num5 = __instance.sfBandIndex[__instance.sfreq].l[++num + 1];
                                }
                                else
                                {
                                    num5 = __instance.sfBandIndex[__instance.sfreq].s[++num + 1];
                                    num5 = (num5 << 2) - num5;
                                    num2 = __instance.sfBandIndex[__instance.sfreq].s[num];
                                    num3 = __instance.sfBandIndex[__instance.sfreq].s[num + 1] - num2;
                                    num2 = (num2 << 2) - num2;
                                }
                            }
                            else
                            {
                                num5 = __instance.sfBandIndex[__instance.sfreq].s[++num + 1];
                                num5 = (num5 << 2) - num5;
                                num2 = __instance.sfBandIndex[__instance.sfreq].s[num];
                                num3 = __instance.sfBandIndex[__instance.sfreq].s[num + 1] - num2;
                                num2 = (num2 << 2) - num2;
                            }
                        }
                        else
                        {
                            num5 = __instance.sfBandIndex[__instance.sfreq].l[++num + 1];
                        }
                    }
                    if (granuleInfo.WindowSwitchingFlag != 0 && ((granuleInfo.BlockType == 2 && granuleInfo.MixedBlockFlag == 0) || (granuleInfo.BlockType == 2 && granuleInfo.MixedBlockFlag != 0 && i >= 36)))
                    {
                        int num12 = (num4 - num2) / num3;
                        int num13 = __instance.scalefac[ch].s[num12][num] << granuleInfo.ScaleFacScale;
                        num13 += granuleInfo.SubblockGain[num12] << 2;
                        xr[num11][num10] *= LayerIIIDecoder.two_to_negative_half_pow[num13];
                    }
                    else
                    {
                        int num14 = __instance.scalefac[ch].l[num];
                        if (granuleInfo.Preflag != 0)
                        {
                            num14 += LayerIIIDecoder.pretab[num];
                        }
                        num14 <<= granuleInfo.ScaleFacScale;
                        xr[num11][num10] *= LayerIIIDecoder.two_to_negative_half_pow[num14];
                    }
                    num4++;
                }
                for (int i = __instance.nonzero[ch]; i < 576; i++)
                {
                    int num15 = i % 18;
                    int num16 = (i - num15) / 18;
                    if (num15 < 0)
                    {
                        num15 = 0;
                    }
                    if (num16 < 0)
                    {
                        num16 = 0;
                    }
                    xr[num16][num15] = 0f;
                }
            }
            catch
            {

            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HoverTooltip))]
    public class HoverTooltipPatch
    {
        [HarmonyPatch(nameof(HoverTooltip.OnPointerEnter))]
        [HarmonyPrefix]
        static bool OnPointerEnterPrefix(HoverTooltip __instance)
        {
            var index = (int)CoreConfig.Instance.Language.Value;

            var tooltip = __instance.tooltipLangauges.Find(x => (int)x.language == index);
            var hasTooltip = tooltip != null;

            EditorManager.inst.SetTooltip(hasTooltip ? tooltip.keys : new List<string>(), hasTooltip ? tooltip.desc : "No tooltip added yet!", hasTooltip ? tooltip.hint : __instance.gameObject.name);
            return false;
        }

        [HarmonyPatch(nameof(HoverTooltip.OnPointerExit))]
        [HarmonyPrefix]
        static bool OnPointerExitPrefix() => false; // Don't want to have the actual mouse tooltip to disappear when I don't want it to.
    }

    [HarmonyPatch(typeof(InputField))]
    public class InputFieldPatch
    {
        [HarmonyPatch("Append", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool AppendPrefix(InputField __instance, ref bool ___m_ReadOnly, ref bool ___m_TouchKeyboardAllowsInPlaceEditing, string __0)
        {
            if (___m_ReadOnly)
                return false;
            CoroutineHelper.StartCoroutine(IAppend(__instance, ___m_ReadOnly, ___m_TouchKeyboardAllowsInPlaceEditing, __0));
            return false;
        }

        static IEnumerator IAppend(InputField __instance, bool ___m_ReadOnly, bool ___m_TouchKeyboardAllowsInPlaceEditing, string __0)
        {
            if (!TouchScreenKeyboard.isSupported || ___m_TouchKeyboardAllowsInPlaceEditing)
            {
                int i = 0;
                int length = __0.Length;
                while (i < length)
                {
                    char c = __0[i];
                    if (c >= ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\n')
                    {
                        Append(__instance, ___m_ReadOnly, ___m_TouchKeyboardAllowsInPlaceEditing, c, i == length - 1);
                    }
                    i++;
                }
            }
            yield break;
        }

        static void Append(InputField __instance, bool m_ReadOnly, bool m_TouchKeyboardAllowsInPlaceEditing, char input, bool sendOnValueChanged)
        {
            if (char.IsSurrogate(input))
                return;

            var type = typeof(InputField);
            if (!m_ReadOnly /*&& __instance.text.Length < 16382*/)
            {
                if (!TouchScreenKeyboard.isSupported || m_TouchKeyboardAllowsInPlaceEditing)
                {
                    int num = Math.Min(__instance.selectionFocusPosition, __instance.selectionAnchorPosition);
                    if (__instance.onValidateInput != null)
                    {
                        input = __instance.onValidateInput(__instance.text, num, input);
                    }
                    else if (__instance.characterValidation != InputField.CharacterValidation.None)
                    {
                        input = (char)AccessTools.Method(type, "Validate").Invoke(__instance, new object[] { __instance.text, num, input });
                    }
                    if (input != '\0')
                    {
                        Insert(type, __instance, m_ReadOnly, input, sendOnValueChanged);
                    }
                }
            }
        }

        static void Insert(Type inputFieldType, InputField __instance, bool m_ReadOnly, char c, bool sendOnValueChanged)
        {
            if (m_ReadOnly)
                return;

            string text = c.ToString();
            AccessTools.Method(inputFieldType, "Delete").Invoke(__instance, new object[] { });
            if (__instance.characterLimit <= 0 || __instance.text.Length < __instance.characterLimit)
            {
                AccessTools.Field(inputFieldType, "m_Text").SetValue(__instance, __instance.text.Insert((int)AccessTools.Field(inputFieldType, "m_CaretPosition").GetValue(__instance), text));

                var caretSelectedPosition = (__instance.selectionAnchorPosition += text.Length);
                ClampPos(__instance, ref caretSelectedPosition);
                AccessTools.Field(inputFieldType, "m_CaretSelectPosition").SetValue(__instance, caretSelectedPosition);

                // Probably not needed.
                //__instance.UpdateTouchKeyboardFromEditChanges();
                if (sendOnValueChanged)
                    AccessTools.Method(inputFieldType, "SendOnValueChanged").Invoke(__instance, new object[] { });
            }
        }

        static void ClampPos(InputField __instance, ref int pos)
        {
            if (pos < 0)
            {
                pos = 0;
            }
            else if (pos > __instance.text.Length)
            {
                pos = __instance.text.Length;
            }
        }
    }

    [HarmonyPatch(typeof(DiscordRpc.RichPresence))]
    public class DiscordRichPresencePatch
    {
        [HarmonyPatch(nameof(DiscordRpc.RichPresence.StrClampBytes))]
        [HarmonyPrefix]
        static bool StrClampBytesPrefix(ref string __result, string __0, int __1)
        {
            string text = DiscordRpc.RichPresence.StrToUtf8NullTerm(__0);
            __result = text;
            return false;
        }

        [HarmonyPatch(nameof(DiscordRpc.RichPresence.FreeMem))]
        [HarmonyPrefix]
        static bool FreeMemPrefix(DiscordRpc.RichPresence __instance)
        {
            try
            {
                if (__instance._buffers.Count > 0)
                    for (int i = __instance._buffers.Count - 1; i >= 0; i--)
                    {
                        Marshal.FreeHGlobal(__instance._buffers[i]);
                        __instance._buffers.RemoveAt(i);
                    }
            }
            catch
            {

            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ColorPicker))]
    public class ColorPickerPatch
    {
        [HarmonyPatch(nameof(ColorPicker.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;
    }

    [HarmonyPatch(typeof(History))]
    public class HistoryPatch
    {
        [HarmonyPatch(nameof(History.Undo))]
        [HarmonyPrefix]
        static bool UndoPrefix(History __instance)
        {
            if (__instance.commands.IsEmpty())
                return false;

            if (!__instance.commands.TryGetAt(__instance.LastExecuted, out History.Command command))
            {
                EditorManager.inst.DisplayNotification("Nothing to undo!", 1f, EditorManager.NotificationType.Error);
                return false;
            }

            command.Undo?.Invoke();
            EditorManager.inst.DisplayNotification($"Undo: {command.CommandName}", 1f, EditorManager.NotificationType.Warning);
            __instance.lastExecuted--;
            return false;
        }

        [HarmonyPatch(nameof(History.Redo))]
        [HarmonyPrefix]
        static bool RedoPrefix(History __instance)
        {
            if (__instance.commands.IsEmpty())
                return false;

            if (!__instance.commands.TryGetAt(__instance.LastExecuted + 1, out History.Command command))
            {
                EditorManager.inst.DisplayNotification("Nothing to redo!", 1f, EditorManager.NotificationType.Error);
                return false;
            }

            command.Do?.Invoke();
            EditorManager.inst.DisplayNotification($"Redo: {command.CommandName}", 1f, EditorManager.NotificationType.Warning); // wrong command name displayed in vanilla?
            __instance.lastExecuted++;
            return false;
        }
    }
}
