using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: please fix this modifier, it's kinda stupid
    // It needs the ability to pause at specified parts of the text.
    public class TextSequence : ModifierActionBase
    {
        #region Constructors

        public TextSequence() => SetupModifier(1, new string[]
        {
            "1", // 0: Length
            "True", // 1: Display Glitch
            "True", // 2: Play Sound
            "False", // 3: Custom Sound
            "Path", // 4: Path
            "1", // 5: Pitch
            "1", // 6: Volume
            "0", // 7: Pitch Vary
            string.Empty, // 8: Custom Text
            "0", // 9: Time Offset
            "False", // 10: Time Relative
            "0", // 11: Pan Stereo
        });

        #endregion

        #region Values

        public override string Name => "textSequence";

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            if (modifier.version == 0)
            {
                modifier.values.RemoveAt(5); // removes global
                modifier.version++;
            }
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var cache = modifier.GetResultOrDefault(() => new Cache());

            var value = modifier.GetValue(8, modifierLoop.variables);
            var text = !string.IsNullOrEmpty(value) ? value : beatmapObject.text;
            text = FormatStringVariables(text, modifierLoop.variables);

            if (cache.text != text)
            {
                cache.text = text;
                cache.textWithoutFormatting = text;
                RTString.RegexMatches(text, new Regex(@"<(.*?)>"), match =>
                {
                    cache.textWithoutFormatting = cache.textWithoutFormatting.Remove(match.Groups[0].ToString());
                    cache.tagLocations.Add(new Vector2Int(match.Index, match.Length - 1));
                });
            }

            if (!cache.setTimer)
            {
                cache.setTimer = true;
                cache.startTime = AudioManager.inst.CurrentAudioSource.time;
            }

            var offsetTime = cache.startTime;
            if (!modifier.GetBool(10, false, modifierLoop.variables))
                offsetTime = beatmapObject.StartTime;

            var time = AudioManager.inst.CurrentAudioSource.time - offsetTime + modifier.GetFloat(9, 0f, modifierLoop.variables);
            var length = modifier.GetFloat(0, 1f, modifierLoop.variables);
            var glitch = modifier.GetBool(1, true, modifierLoop.variables);

            var p = time / length;

            var stringLength = (int)Mathf.Lerp(0, cache.textWithoutFormatting.Length, p);
            textObject.textMeshPro.maxVisibleCharacters = stringLength;

            if (glitch && (int)RTMath.Lerp(0, cache.textWithoutFormatting.Length, p) <= cache.textWithoutFormatting.Length)
            {
                int insert = Mathf.Clamp(stringLength - 1, 0, text.Length);
                for (int i = 0; i < cache.tagLocations.Count; i++)
                {
                    var tagLocation = cache.tagLocations[i];
                    if (insert >= tagLocation.x)
                        insert += tagLocation.y + 1;
                }

                text = text.Insert(insert, LSText.randomString(1));
            }

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;

            if (cache.current != stringLength && cache.textWithoutFormatting[Mathf.Clamp(stringLength - 1, 0, cache.textWithoutFormatting.Length - 1)] != ' ')
            {
                cache.current = stringLength;
                var pitch = modifier.GetFloat(5, 1f, modifierLoop.variables);
                var volume = modifier.GetFloat(6, 1f, modifierLoop.variables);
                var pitchVary = modifier.GetFloat(7, 0f, modifierLoop.variables);
                var panStereo = modifier.GetFloat(11, 0f, modifierLoop.variables);

                if (pitchVary != 0f)
                    pitch += UnityRandom.Range(-pitchVary, pitchVary);

                // Don't play any sounds.
                if (!modifier.GetBool(2, true, modifierLoop.variables))
                    return;

                // Don't play custom sound.
                if (!modifier.GetBool(3, false, modifierLoop.variables))
                {
                    SoundManager.inst.PlaySound(DefaultSounds.Click, volume, volume, panStereo: panStereo);
                    return;
                }

                var soundName = modifier.GetValue(4, modifierLoop.variables);
                if (string.IsNullOrEmpty(soundName))
                    return;

                if (GameData.Current.assets.sounds.TryFind(x => x.name == soundName, out SoundAsset soundAsset) && soundAsset.audio)
                    SoundManager.inst.PlaySound(soundAsset.audio, volume, pitch, panStereo: panStereo);
                else if (SoundManager.inst.TryGetSound(soundName, out AudioClip audioClip))
                    SoundManager.inst.PlaySound(audioClip, volume, pitch, panStereo: panStereo);
                else
                {
                    var fullPath = AssetPack.TryGetFile(soundName, out string assetFile) ? assetFile : RTFile.CombinePaths(RTFile.BasePath, soundName);
                    var audioDotFormats = RTFile.AudioDotFormats;
                    for (int i = 0; i < audioDotFormats.Length; i++)
                    {
                        var audioDotFormat = audioDotFormats[i];
                        if (!soundName.Contains(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                            fullPath += audioDotFormat;
                    }

                    if (!RTFile.FileExists(fullPath))
                        return;

                    if (fullPath.EndsWith(FileFormat.MP3.Dot()))
                    {
                        modifier.Result = ModifiersHelper.PlaySound(beatmapObject.id, LSAudio.CreateAudioClipUsingMP3File(fullPath), pitch, volume, false, panStereo: panStereo);
                        return;
                    }
                    CoroutineHelper.StartCoroutine(ModifiersHelper.LoadMusicFileRaw(fullPath,
                        callback: audioClip => modifier.Result = ModifiersHelper.PlaySound(beatmapObject.id, audioClip, pitch, volume, false, panStereo: panStereo)));
                }
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.TryGetResult(out Cache cache))
                cache.setTimer = false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Length", 0, 1f);
            modifierCard.BoolGenerator(modifier, reference, "Display Glitch", 1, true);
            modifierCard.BoolGenerator(modifier, reference, "Play Sound", 2, true);
            modifierCard.BoolGenerator(modifier, reference, "Custom Sound", 3, false);
            var str = modifierCard.StringGenerator(modifier, reference, "Sound Path", 4);
            EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                EditorContextMenu.GetModifierSoundPathFunctions(_val => modifier.SetValue(4, _val)));

            modifierCard.SingleGenerator(modifier, reference, "Pitch", 5, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Volume", 6, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Pitch Vary", 7, 0f);
            var customText = modifierCard.StringGenerator(modifier, reference, "Custom Text", 8).transform.Find("Input").GetComponent<InputField>();
            EditorContextMenu.AddContextMenu(customText.gameObject, EditorContextMenu.GetNameFunctions(customText));
            modifierCard.SingleGenerator(modifier, reference, "Time Offset", 9, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Time Relative", 10, false);
            modifierCard.SingleGenerator(modifier, reference, "Pan Stereo", 11);
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public bool setTimer;
            public float startTime;
            public int current;
            public string text = string.Empty;
            public string textWithoutFormatting = string.Empty;
            public List<Vector2Int> tagLocations = new List<Vector2Int>();
        }

        #endregion
    }
}
