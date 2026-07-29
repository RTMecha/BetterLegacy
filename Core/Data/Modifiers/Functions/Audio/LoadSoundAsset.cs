using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadSoundAsset : ModifierActionBase
    {
        #region Constructors

        public LoadSoundAsset() => SetupModifier("audio.ogg", "True", "False", "1", "1", "False", "0");

        #endregion

        #region Values

        public override string Name => "loadSoundAsset";

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant && modifier.TryGetResult(out AudioSource cache) && cache)
            {
                cache.UnPause();
                return;
            }

            var name = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var soundAsset = GameData.Current.assets.sounds.Find(x => x.name == name);
            if (!soundAsset)
                return;

            if (modifier.GetBool(1, true, modifierLoop.variables))
            {
                if (soundAsset.audio)
                    return;

                var play = modifier.GetBool(2, false, modifierLoop.variables);
                var pitch = modifier.GetFloat(3, 1f, modifierLoop.variables);
                var vol = modifier.GetFloat(4, 1f, modifierLoop.variables);
                var loop = modifier.GetBool(5, false, modifierLoop.variables);
                var panStereo = modifier.GetFloat(6, 0f, modifierLoop.variables);

                CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                {
                    if (play)
                        modifier.Result = SoundManager.inst.PlaySound(soundAsset.audio, vol, pitch, loop, panStereo);
                }));
            }
            else
                soundAsset.UnloadAudioClip();
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
            modifierCard.StringGenerator(modifier, reference, "Asset Name", 0);
            modifierCard.BoolGenerator(modifier, reference, "Load", 1);
            modifierCard.BoolGenerator(modifier, reference, "Play", 2);
            modifierCard.SingleGenerator(modifier, reference, "Pitch", 3);
            modifierCard.SingleGenerator(modifier, reference, "Volume", 4);
            modifierCard.BoolGenerator(modifier, reference, "Loop", 5);
            modifierCard.SingleGenerator(modifier, reference, "Pan Stereo", 6);
        }

        #endregion
    }
}
