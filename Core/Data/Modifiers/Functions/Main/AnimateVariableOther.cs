using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AnimateVariableOther : ModifierActionBase
    {
        #region Constructors

        public AnimateVariableOther()
        {
            SetupModifier("Object Group", "0", "0", "0", "1", "0", "-99999", "99999", "99999");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "animateVariableOther";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
            var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);
            var delay = modifier.GetFloat(3, 0, modifierLoop.variables);
            var multiply = modifier.GetFloat(4, 0, modifierLoop.variables);
            var offset = modifier.GetFloat(5, 0, modifierLoop.variables);
            var min = modifier.GetFloat(6, -9999f, modifierLoop.variables);
            var max = modifier.GetFloat(7, 9999f, modifierLoop.variables);
            var loop = modifier.GetFloat(8, 9999f, modifierLoop.variables);

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));
            if (list.IsEmpty())
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                var cachedSequences = beatmapObject.cachedSequences;
                var time = AudioManager.inst.CurrentAudioSource.time;

                fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

                if (!cachedSequences)
                    continue;

                switch (fromType)
                {
                    // To Type Position
                    // To Axis X
                    // From Type Position
                    case 0: {
                            var sequence = cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((sequence.At(fromAxis) % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Scale
                    case 1: {
                            var sequence = cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((sequence.At(fromAxis) % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Rotation
                    case 2: {
                            var sequence = cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay) * multiply;

                            beatmapObject.integerVariable = (int)Mathf.Clamp((sequence.At(fromAxis) % loop) - offset, min, max);
                            break;
                        }
                }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);

            modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
            modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.SingleGenerator(modifier, reference, "Delay", 3, 0f);

            modifierCard.SingleGenerator(modifier, reference, "Multiply", 4, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Offset", 5, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Min", 6, -99999f);
            modifierCard.SingleGenerator(modifier, reference, "Max", 7, 99999f);
            modifierCard.SingleGenerator(modifier, reference, "Loop", 8, 99999f);
        }

        #endregion
    }
}
