using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public abstract class PlayerTriggerBase : ModifierTriggerBase
    {
        #region Constructors

        public PlayerTriggerBase(Requirement requirement)
        {
            this.requirement = requirement;
        }

        #endregion

        #region Values

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => requirement == Requirement.Nearest ? ModifierCompatibility.BeatmapObjectCompatible : base.Compatibility;

        public Requirement requirement;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (requirement)
            {
                case Requirement.Nearest: {
                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return false;

                        var runtimeObject = beatmapObject.runtimeObject;
                        if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                        {
                            var player = PlayerManager.inst.GetClosestPlayer(beatmapObject.GetFullPosition());

                            if (CheckPlayer(modifier, modifierLoop, player))
                                return true;
                        }
                        break;
                    }
                case Requirement.Index: {
                        var index = modifier.GetInt(0, 0, modifierLoop.variables);
                        if (PlayerManager.inst.players.TryGetAt(index, out PAPlayer player) && CheckPlayer(modifier, modifierLoop, player))
                            return true;
                        break;
                    }
                case Requirement.Any: {
                        for (int i = 0; i < PlayerManager.inst.players.Count; i++)
                        {
                            var player = PlayerManager.inst.players[i];
                            if (CheckPlayer(modifier, modifierLoop, player))
                                return true;
                        }
                        break;
                    }
                case Requirement.All: return PlayerManager.inst.players.All(x => CheckPlayer(modifier, modifierLoop, x));
            }
            return false;
        }

        public abstract bool CheckPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (requirement == Requirement.Index)
                modifierCard.IntegerGenerator(modifier, reference, "Index", 0);
        }

        #endregion

        #region Sub Classes

        public enum Requirement
        {
            Nearest,
            Index,
            Any,
            All,
        }

        #endregion
    }
}
