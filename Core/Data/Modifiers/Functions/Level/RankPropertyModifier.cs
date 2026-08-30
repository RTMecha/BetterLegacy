using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RankPropertyModifier : ModifierActionBase
    {
        #region Constructors

        public RankPropertyModifier(Action action, Property property)
        {
            this.action = action;
            this.property = property;
            Name = action.ToString().ToLower() + property.ToString();
            if (action == Action.Clear)
                Name += 's';
            SetupModifier();
            if (action == Action.Add)
            {
                Modifier.values.Add("True"); // Use Self Position
                Modifier.values.Add(string.Empty); // Time
            }
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly Action action;

        readonly Property property;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var list = property switch
            {
                Property.Hit => RTBeatmap.Current.hits,
                Property.Death => RTBeatmap.Current.deaths,
                _ => null,
            };
            if (list == null)
                return;

            switch (action)
            {
                case Action.Clear: {
                        list.Clear();
                        break;
                    }
                case Action.Add: {
                        var vector = Vector3.zero;
                        if (modifierLoop.reference is BeatmapObject beatmapObject)
                        {
                            if (modifier.GetBool(0, true, modifierLoop.variables))
                                vector = beatmapObject.GetFullPosition();
                            else
                            {
                                var player = PlayerManager.inst.GetClosestPlayer(beatmapObject.GetFullPosition());
                                if (player && player.RuntimePlayer)
                                    vector = player.RuntimePlayer.rb.position;
                            }
                        }

                        var timeValue = modifier.GetValue(1, modifierLoop.variables);
                        float time = AudioManager.inst.CurrentAudioSource.time;
                        if (!string.IsNullOrEmpty(timeValue) && modifierLoop.reference is IEvaluatable evaluatable)
                        {
                            var numberVariables = evaluatable.GetObjectVariables();
                            ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                            time = RTMath.Parse(timeValue, RTLevel.Current?.evaluationContext, numberVariables, evaluatable.GetObjectFunctions());
                        }

                        list.Add(new PlayerDataPoint(vector, time));
                        break;
                    }
                case Action.Sub: {
                        if (!RTBeatmap.Current.hits.IsEmpty())
                            RTBeatmap.Current.hits.RemoveAt(RTBeatmap.Current.hits.Count - 1);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (action != Action.Add)
                return;
            modifierCard.BoolGenerator(modifier, reference, "Use Self Position", 0, true);
            modifierCard.StringGenerator(modifier, reference, "Time", 1);
        }

        #endregion

        #region Sub Classes

        public enum Action
        {
            Clear,
            Add,
            Sub,
        }

        public enum Property
        {
            Hit,
            Death,
        }

        #endregion
    }
}
