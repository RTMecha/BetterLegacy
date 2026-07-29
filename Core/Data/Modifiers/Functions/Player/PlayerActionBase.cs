using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public abstract class PlayerActionBase : ModifierActionBase
    {
        #region Constructors

        public PlayerActionBase() { }

        public PlayerActionBase(Selector selector) => this.selector = selector;

        public PlayerActionBase(string name, Selector selector, params string[] values)
        {
            this.selector = selector;
            Name = name;
            if (selector != Selector.Nearest)
                Name += selector.ToString();
            SetupModifier(values);
            if (selector == Selector.Index)
                Modifier.values.Insert(0, "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => selector switch
        {
            Selector.Nearest => ModifierCompatibility.BeatmapObjectCompatible,
            _ => ModifierCompatibility.LevelControlCompatible,
        };

        public readonly Selector selector;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (selector)
            {
                case Selector.Nearest: {
                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            break;
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var pos = beatmapObject.GetFullPosition();
                            var player = PlayerManager.GetClosestPlayer(pos);
                            RunOnPlayer(modifier, modifierLoop, player);
                        });
                        break;
                    }
                case Selector.Index: {
                        if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, modifierLoop.variables), out PAPlayer player))
                            RunOnPlayer(modifier, modifierLoop, player);
                        break;
                    }
                case Selector.All: {
                        foreach (var player in PlayerManager.Players)
                            RunOnPlayer(modifier, modifierLoop, player);
                        break;
                    }
            }
        }

        public abstract void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (selector == Selector.Index)
                modifierCard.IntegerGenerator(modifier, reference, "Player Index", 0, 0);
        }

        public int Index(int index) => selector == Selector.Index ? index + 1 : index;

        #endregion

        #region Sub Classes

        public enum Selector
        {
            Nearest,
            Index,
            All,
        }

        #endregion
    }
}
