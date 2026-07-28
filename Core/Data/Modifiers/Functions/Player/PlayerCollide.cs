using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerCollide : PlayerTriggerBase
    {
        #region Constructors

        public PlayerCollide(Requirement requirement, bool isGroup) : base(requirement)
        {
            this.isGroup = isGroup;
            Name = "playerCollide";
            if (requirement != Requirement.Nearest)
                Name += requirement.ToString();
            if (isGroup)
                Name += "Other";
            SetupModifier();
            if (isGroup)
                Modifier.values.Add("Object Group");
            if (requirement == Requirement.Index)
                Modifier.values.Add("0");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : requirement == Requirement.Index ? ModifierCompatibility.BeatmapObjectCompatible : base.Compatibility;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override bool CheckPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            var optimized = false; // maybe add this as a value?
            if (isGroup)
            {
                if (!player.RuntimePlayer)
                    return false;

                var prefabable = modifierLoop.reference.AsPrefabable();
                if (prefabable == null)
                    return false;

                var tag = modifier.GetValue(0, modifierLoop.variables);

                var cache = modifier.GetResultOrDefault(() => new GenericGroupCache<BeatmapObject>(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag)));
                if (cache.tag != tag)
                    cache.UpdateCache(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag));

                for (int i = 0; i < cache.group.Count; i++)
                {
                    var otherRuntimeObject = cache.group[i]?.runtimeObject;
                    if (!otherRuntimeObject || !otherRuntimeObject.visualObject || !otherRuntimeObject.visualObject.collider)
                        continue;

                    if (otherRuntimeObject.visualObject is SolidObject solidObject && !solidObject.forceCollisionEnabled)
                    {
                        solidObject.forceCollisionEnabled = true;
                        solidObject.UpdateCollider();
                    }

                    var collider = otherRuntimeObject.visualObject.collider;

                    var colliderCheck = optimized ? player.RuntimePlayer.collisionState.Collider : player.RuntimePlayer.CurrentCollider;
                    if (!colliderCheck)
                        return false;

                    if (optimized ? collider == colliderCheck : colliderCheck.IsTouching(collider))
                        return true;
                }
                return false;
            }

            if (!player.RuntimePlayer)
                return false;

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return false;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                if (runtimeObject.visualObject is SolidObject solidObject && !solidObject.forceCollisionEnabled)
                {
                    solidObject.forceCollisionEnabled = true;
                    solidObject.UpdateCollider();
                }

                var collider = runtimeObject.visualObject.collider;

                var colliderCheck = optimized ? player.RuntimePlayer.collisionState.Collider : player.RuntimePlayer.CurrentCollider;
                if (!colliderCheck)
                    return false;

                if (optimized ? collider == colliderCheck : colliderCheck.IsTouching(collider))
                    return true;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            }
            if (requirement == Requirement.Index)
                modifierCard.IntegerGenerator(modifier, reference, "Index", isGroup ? 1 : 0);
        }

        #endregion
    }
}
