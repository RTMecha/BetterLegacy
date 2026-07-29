using UnityEngine;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetAudio : ModifierActionBase
    {
        #region Constructors

        public SetAudio() => SetupModifier(false, "audio", "1", string.Empty, "True");

        #endregion

        #region Values

        public override string Name => "setAudio";

        public override ModifierCategoryType Category => ModifierCategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var currentLevel = ProjectArrhythmia.State.InEditor ? EditorLevelManager.inst.CurrentLevel : LevelManager.CurrentLevel;

            if (!MetaData.Current || !MetaData.Current.package || !currentLevel)
                return;

            if (!currentLevel.tracks.TryGetValue(modifier.GetValue(0, modifierLoop.variables), out AudioClip audioClip))
                return;

            AudioManager.inst.PlayMusic(null, audioClip, true, modifier.GetFloat(1, 1f, modifierLoop.variables), false);
            var t = modifier.GetValue(2, modifierLoop.variables);
            if (!string.IsNullOrEmpty(t) && float.TryParse(t, out float time))
                AudioManager.inst.SetMusicTime(time);
            GameManager.inst.songLength = audioClip.length;

            if (modifier.GetBool(3, true, modifierLoop.variables) && !ProjectArrhythmia.State.InEditor)
                RTGameManager.inst.PlayIntro();
            if (!ProjectArrhythmia.State.InEditor)
                return;

            if (EditorConfig.Instance.WaveformGenerate.Value)
            {
                CoreHelper.Log("Assigning waveform textures...");
                CoroutineHelper.StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(audioClip, true));
            }
            else
            {
                CoreHelper.Log("Skipping waveform textures...");
                EditorTimeline.inst.SetTimelineSprite(null);
            }

            EditorTimeline.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();

            var timeField = RTEditor.inst.timeField;
            TriggerHelper.AddEventTriggers(timeField.gameObject, TriggerHelper.ScrollDelta(timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "File ID", 0);
            modifierCard.SingleGenerator(modifier, reference, "Transition Time", 1, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Set Song Time", 2);
            modifierCard.BoolGenerator(modifier, reference, "Show Info", 3);
        }

        #endregion
    }
}
