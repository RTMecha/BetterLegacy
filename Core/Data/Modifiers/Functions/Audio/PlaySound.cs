using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlaySound : ModifierActionBase
    {
        #region Constructors

        public PlaySound(SoundSource soundSource)
        {
            this.soundSource = soundSource;
            Name = "play";
            if (soundSource != SoundSource.Regular)
                Name += soundSource.ToString();
            Name += "Sound";
            SetupModifier(false, soundSource == SoundSource.Default ? "blip" : "sounds/audio.wav", "1", "1", "False", "0");
            if (soundSource == SoundSource.Regular)
                Modifier.version = 1;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        readonly SoundSource soundSource;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (soundSource == SoundSource.Regular && modifier.version == 0)
            {
                modifier.values.RemoveAt(1);
                modifier.version++;
            }
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var path = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var pitch = modifier.GetFloat(1, 1f, modifierLoop.variables);
            var volume = modifier.GetFloat(2, 1f, modifierLoop.variables);
            var loop = modifier.GetBool(3, false, modifierLoop.variables);
            var panStereo = modifier.GetFloat(4, 0f, modifierLoop.variables);

            var id = modifierLoop.reference is PAObjectBase obj ? obj.id : modifierLoop.reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            switch (soundSource)
            {
                case SoundSource.Regular: {
                        if (GameData.Current && GameData.Current.assets.sounds.TryFind(x => x.name == path, out SoundAsset soundAsset))
                        {
                            if (!soundAsset.audio)
                            {
                                CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                                {
                                    if (soundAsset.audio)
                                        modifier.Result = ModifiersHelper.PlaySound(id, soundAsset.audio, pitch, volume, loop, panStereo);
                                }));
                                break;
                            }

                            modifier.Result = ModifiersHelper.PlaySound(id, soundAsset.audio, pitch, volume, loop, panStereo);
                            break;
                        }

                        var fullPath = AssetPack.TryGetFile(path, out string assetFile) ? assetFile : RTFile.CombinePaths(RTFile.BasePath, path);
                        var audioDotFormats = RTFile.AudioDotFormats;
                        for (int i = 0; i < audioDotFormats.Length; i++)
                        {
                            var audioDotFormat = audioDotFormats[i];
                            if (!path.Contains(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                                fullPath += audioDotFormat;
                        }

                        if (!RTFile.FileExists(fullPath))
                            break;

                        if (fullPath.EndsWith(FileFormat.MP3.Dot()))
                        {
                            modifier.Result = ModifiersHelper.PlaySound(id, LSAudio.CreateAudioClipUsingMP3File(fullPath), pitch, volume, loop, panStereo);
                            break;
                        }
                        CoroutineHelper.StartCoroutine(ModifiersHelper.LoadMusicFileRaw(fullPath,
                            callback: audioClip => modifier.Result = ModifiersHelper.PlaySound(id, audioClip, pitch, volume, loop, panStereo)));
                        break;
                    }
                case SoundSource.Online: {
                        var audioType = RTFile.GetAudioType(path);

                        if (audioType != AudioType.UNKNOWN)
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip(path, audioType,
                                callback: audioClip => modifier.Result = ModifiersHelper.PlaySound(id, audioClip, pitch, volume, loop, panStereo),
                                onError: (string onError, long responseCode, string errorMsg) => CoreHelper.Log($"Error! Could not download audioclip.\n{onError}")));
                        break;
                    }
                case SoundSource.Default: {
                        if (LegacyResources.soundClips.TryFind(x => x.id == path, out SoundGroup soundGroup))
                            ModifiersHelper.PlaySound(id, soundGroup.GetClip(), pitch, volume, loop, panStereo);
                        break;
                    }
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
                cache.Pause();
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.TryGetResult(out AudioSource cache) && cache)
                CoreHelper.Destroy(cache);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (soundSource)
            {
                case SoundSource.Regular: {
                        var str = modifierCard.StringGenerator(modifier, reference, "Path", 0);
                        EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                            EditorContextMenu.GetModifierSoundPathFunctions(_val => modifier.SetValue(0, _val)));
                        break;
                    }
                case SoundSource.Online: {
                        modifierCard.StringGenerator(modifier, reference, "URL", 0);
                        break;
                    }
                case SoundSource.Default: {
                        modifierCard.DropdownGenerator(modifier, reference, "Sound",
                            getValue: () =>
                            {
                                var values = EnumHelper.GetNames<DefaultSounds>();
                                int soundIndex = -1;
                                for (int i = 0; i < values.Length; i++)
                                {
                                    if (values[i] == modifier.GetValue(0))
                                    {
                                        soundIndex = i;
                                        break;
                                    }
                                }
                                return (soundIndex >= 0 ? soundIndex : 0).ToString();
                            },
                            setValue: _val =>
                            {
                                if (System.Enum.TryParse(_val, out DefaultSounds defaultSounds))
                                    modifier.SetValue(0, defaultSounds.ToString());
                                else
                                    modifier.SetValue(0, _val);
                                modifierCard.Update(modifier, reference);
                            },
                            options: CoreHelper.ToOptionData<DefaultSounds>(), null);
                        break;
                    }
            }
            modifierCard.SingleGenerator(modifier, reference, "Pitch", 1, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Volume", 2, 1f);
            modifierCard.BoolGenerator(modifier, reference, "Loop", 3, false);
            modifierCard.SingleGenerator(modifier, reference, "Pan Stereo", 4);
        }

        #endregion

        #region Sub Classes

        public enum SoundSource
        {
            Regular,
            Online,
            Default,
        }

        #endregion
    }
}
