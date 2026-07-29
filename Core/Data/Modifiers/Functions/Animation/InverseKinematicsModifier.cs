using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: please figure out how to get this to work.
    // there needs to be a way to specify bone length for each object.
    public class InverseKinematicsModifier : ModifierActionBase
    {
        #region Constructors

        public InverseKinematicsModifier()
        {
            SetupModifier("Object Group", "Object Group", "-1");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "inverseKinematics";

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        public override bool DisplayInEditor => false; // wip

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var targetTag = modifier.GetValue(0, modifierLoop.variables);
            var baseTag = modifier.GetValue(1, modifierLoop.variables);
            var parentCount = modifier.GetInt(2, 1, modifierLoop.variables);

            var cache = modifier.GetResultOrDefault(() =>
            {
                var cache = new Cache();
                cache.UpdateCache(modifier, beatmapObject, targetTag, baseTag);
                cache.UpdateParents(beatmapObject, parentCount);
                return cache;
            });
            if (cache.targetTag != targetTag || cache.baseTag != baseTag)
            {
                cache.UpdateCache(modifier, beatmapObject, targetTag, baseTag);
                modifier.Result = cache;
            }

            if (!cache.baseObject || !cache.targetObject)
                return;

            if (!cache.ik)
            {
                cache.ik = new InverseKinematics();
                cache.ik.bones = cache.parents.Select(x => new BeatmapObjectBone(x)).ToArray();
            }

            cache.ik.Set(cache.baseObject.GetFullPosition(), cache.targetObject.GetFullPosition());
            cache.ik.UpdateIK();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Target Group", 0);
            modifierCard.GroupFieldGenerator(modifier, reference, "Base Group", 1);
            modifierCard.IntegerGenerator(modifier, reference, "Parent Count", 2);
        }

        #endregion

        #region Sub Classes

        public class BeatmapObjectBone : InverseKinematics.Bone
        {
            public BeatmapObjectBone(BeatmapObject beatmapObject)
            {
                this.beatmapObject = beatmapObject;
                length = beatmapObject.boneLength;
            }

            public BeatmapObject beatmapObject;

            public override void Apply()
            {
                beatmapObject.PositionOffset = position;
                beatmapObject.RotationOffset = rotation.eulerAngles;
            }
        }

        public class Cache
        {
            public Cache() { }

            public InverseKinematics ik;

            public List<BeatmapObject> parents;

            public string baseTag;

            public BeatmapObject baseObject;

            public string targetTag;

            public BeatmapObject targetObject;

            public void UpdateCache(Modifier modifier, IPrefabable prefabable, string targetTag, string baseTag)
            {
                this.targetTag = targetTag;
                this.baseTag = baseTag;
                if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, targetTag, out BeatmapObject targetObject))
                    this.targetObject = targetObject;
                if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, baseTag, out BeatmapObject baseObject))
                    this.baseObject = baseObject;
            }

            public void UpdateParents(BeatmapObject beatmapObject, int parentCount)
            {
                var self = beatmapObject;
                var parents = new List<BeatmapObject>();
                parents.Add(self);

                for (int i = 0; i < (parentCount < 0 ? int.MaxValue : parentCount); i++)
                {
                    var parent = self.GetParent();
                    if (!parent)
                        break;

                    parents.Add(parent);
                    self = parent;
                }

                this.parents = parents;
            }
        }

        #endregion
    }
}
