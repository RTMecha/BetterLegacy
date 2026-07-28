using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CreateCheckpoint : ModifierActionBase
    {
        #region Constructors

        public CreateCheckpoint() => SetupModifier(false, "0", "True", "0", "0", "False", "True", "True", "True", "0");

        #endregion

        #region Values

        public override string Name => "createCheckpoint";

        public override CategoryType Category => CategoryType.Checkpoints;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // if active checpoints matches the stored checkpoint, do not create a new checkpoint.
            if (modifier.TryGetResult(out Checkpoint prevCheckpoint) && prevCheckpoint.id == RTBeatmap.Current.ActiveCheckpoint.id)
                return;

            var checkpoint = new Checkpoint();
            checkpoint.time = modifier.GetBool(1, true, modifierLoop.variables) ? modifierLoop.reference.GetParentRuntime().FixedTime + modifier.GetFloat(0, 0f, modifierLoop.variables) : modifier.GetFloat(0, 0f, modifierLoop.variables);
            checkpoint.pos = new Vector2(modifier.GetFloat(2, 0f, modifierLoop.variables), modifier.GetFloat(3, 0f, modifierLoop.variables));
            checkpoint.heal = modifier.GetBool(4, false, modifierLoop.variables);
            checkpoint.respawn = modifier.GetBool(5, true, modifierLoop.variables);
            checkpoint.reverse = modifier.GetBool(6, true, modifierLoop.variables);
            checkpoint.setTime = modifier.GetBool(7, true, modifierLoop.variables);
            checkpoint.spawnType = (Checkpoint.SpawnPositionType)modifier.GetInt(8, 0, modifierLoop.variables);
            for (int i = 9; i < modifier.values.Count; i += 2)
                checkpoint.positions.Add(new Vector2(modifier.GetFloat(i, 0f, modifierLoop.variables), modifier.GetFloat(i + 1, 0f, modifierLoop.variables)));

            RTBeatmap.Current.SetCheckpoint(checkpoint);
            modifier.Result = checkpoint;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Time", 0);
            modifierCard.BoolGenerator(modifier, reference, "Time Relative", 1);

            modifierCard.SingleGenerator(modifier, reference, "Pos X", 2);
            modifierCard.SingleGenerator(modifier, reference, "Pos Y", 3);

            modifierCard.BoolGenerator(modifier, reference, "Heal", 4);
            modifierCard.BoolGenerator(modifier, reference, "Respawn", 5, true);
            modifierCard.BoolGenerator(modifier, reference, "Reverse On Death", 6, true);
            modifierCard.BoolGenerator(modifier, reference, "Set Time On Death", 7, true);
            modifierCard.DropdownGenerator(modifier, reference, "Spawn Position Type", 8, CoreHelper.ToOptionData<Checkpoint.SpawnPositionType>());

            int a = 0;
            for (int i = 9; i < modifier.values.Count; i += 2)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Position {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int j = 0; j < 2; j++)
                        modifier.values.RemoveAt(groupIndex);
                });

                modifierCard.SingleGenerator(modifier, reference, "Pos X", i);
                modifierCard.SingleGenerator(modifier, reference, "Pos Y", i + 1);

                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Position Value", () =>
            {
                modifier.values.Add("0");
                modifier.values.Add("0");
            });
        }

        #endregion
    }
}
