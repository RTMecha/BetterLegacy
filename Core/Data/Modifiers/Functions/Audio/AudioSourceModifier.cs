using LSFunctions;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AudioSourceModifier : ModifierActionBase
    {
        #region Constructors

        public AudioSourceModifier() => SetupModifier(1, "sounds/audio.wav", "1", "1", "False", "0", "True", "0", "True", "0");

        #endregion

        #region Values

        public override string Name => "audioSource";

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version == 0)
            {
                modifier.values.RemoveAt(1);
                modifier.version++;
            }
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (modifier.TryGetResult(out AudioModifier audioModifier))
            {
                audioModifier.pitch = modifier.GetFloat(1, 1f, modifierLoop.variables);
                audioModifier.volume = modifier.GetFloat(2, 1f, modifierLoop.variables);
                audioModifier.loop = modifier.GetBool(3, true, modifierLoop.variables);
                audioModifier.timeOffset = modifier.GetBool(5, true, modifierLoop.variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(4, 0f, modifierLoop.variables) : modifier.GetFloat(4, 0f, modifierLoop.variables);
                audioModifier.lengthOffset = modifier.GetFloat(6, 0f, modifierLoop.variables);
                audioModifier.playing = modifier.GetBool(7, true, modifierLoop.variables);
                audioModifier.panStereo = modifier.GetFloat(8, 0f, modifierLoop.variables);
                audioModifier.Tick();
                return;
            }

            var path = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

            string fullPath = AssetPack.TryGetFile(path, out string assetFile) ? assetFile : RTFile.CombinePaths(RTFile.BasePath, path);

            var audioDotFormats = RTFile.AudioDotFormats;
            for (int i = 0; i < audioDotFormats.Length; i++)
            {
                var audioDotFormat = audioDotFormats[i];
                if (!path.EndsWith(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                    fullPath += audioDotFormat;
            }

            if (!RTFile.FileExists(fullPath))
            {
                CoreHelper.LogError($"File does not exist {fullPath}");
                return;
            }

            if (fullPath.EndsWith(FileFormat.MP3.Dot()))
            {
                modifier.Result = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), beatmapObject, modifier);
                return;
            }

            CoroutineHelper.StartCoroutine(ModifiersHelper.LoadMusicFileRaw(fullPath, audioClip =>
            {
                if (!audioClip)
                {
                    CoreHelper.LogError($"Failed to load audio {fullPath}");
                    return;
                }

                audioClip.name = path;

                if (!runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                    return;

                var audioModifier = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                modifier.Result = audioModifier;
                audioModifier.Init(audioClip, beatmapObject, modifier);
                audioModifier.pitch = modifier.GetFloat(1, 1f, modifierLoop.variables);
                audioModifier.volume = modifier.GetFloat(2, 1f, modifierLoop.variables);
                audioModifier.loop = modifier.GetBool(3, true, modifierLoop.variables);
                audioModifier.timeOffset = modifier.GetBool(5, true, modifierLoop.variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(4, 0f, modifierLoop.variables) : modifier.GetFloat(4, 0f, modifierLoop.variables);
                audioModifier.lengthOffset = modifier.GetFloat(6, 0f, modifierLoop.variables);
                audioModifier.playing = modifier.GetBool(7, true, modifierLoop.variables);
                audioModifier.panStereo = modifier.GetFloat(8, 0f, modifierLoop.variables);
                audioModifier.Tick();
            }));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var str = modifierCard.StringGenerator(modifier, reference, "Path", 0);
            EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                EditorContextMenu.GetModifierSoundPathFunctions(_val => modifier.SetValue(0, _val)));
            modifierCard.SingleGenerator(modifier, reference, "Pitch", 1, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Volume", 2, 1f);
            modifierCard.BoolGenerator(modifier, reference, "Loop", 3, true);

            modifierCard.SingleGenerator(modifier, reference, "Time", 4, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Time Relative", 5, true);
            modifierCard.SingleGenerator(modifier, reference, "Length Offset", 6, 0f);

            modifierCard.BoolGenerator(modifier, reference, "Playing", 7, true);

            modifierCard.SingleGenerator(modifier, reference, "Pan Stereo", 8);
        }

        #endregion
    }
}
