using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RigidbodyModifier : ModifierActionBase
    {
        #region Constructors

        public RigidbodyModifier(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "rigidbody";
            if (isGroup)
                Name += "Other";
            SetupModifier(new string[]
            {
                "0", // Gravity
                "0", // Collision Mode
                "0", // Drag
                "0", // Velocity X
                "0", // Velocity Y
                "0", // Body Type
            });
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Component;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var gravity = modifier.GetFloat(GetValueIndex(0), 0f, modifierLoop.variables);
            var collisionMode = modifier.GetInt(GetValueIndex(1), 0, modifierLoop.variables);
            var drag = modifier.GetFloat(GetValueIndex(2), 0f, modifierLoop.variables);
            var velocityX = modifier.GetFloat(GetValueIndex(3), 0f, modifierLoop.variables);
            var velocityY = modifier.GetFloat(GetValueIndex(4), 0f, modifierLoop.variables);
            var bodyType = modifier.GetInt(GetValueIndex(5), 0, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var tag = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
                var cache = modifier.GetResultOrDefault(() => new GenericGroupCache<BeatmapObject>(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag)));
                if (cache.tag != tag)
                    cache.UpdateCache(tag, GameData.Current.FindObjectsWithTag(modifier, prefabable, tag));
                var list = cache.group;
                if (list.IsEmpty())
                    return;

                foreach (var other in list)
                    Apply(other, gravity, collisionMode, drag, bodyType, velocityX, velocityY);
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;
            modifier.Result = beatmapObject;
            Apply(beatmapObject, gravity, collisionMode, drag, bodyType, velocityX, velocityY);
        }

        int GetValueIndex(int index) => isGroup ? index + 1 : index;

        void Apply(BeatmapObject beatmapObject, float gravity, int collisionMode, float drag, int bodyType, float velocityX, float velocityY)
        {
            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (!runtimeObject.visualObject.rigidbody)
                runtimeObject.visualObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

            runtimeObject.visualObject.rigidbody.gravityScale = gravity;
            runtimeObject.visualObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
            runtimeObject.visualObject.rigidbody.drag = drag;

            runtimeObject.visualObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

            runtimeObject.visualObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            int index = 0;
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", 0).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));
                index++;
            }

            modifierCard.SingleGenerator(modifier, reference, "Gravity", index, 0f);
            index++;

            modifierCard.DropdownGenerator(modifier, reference, "Collision Mode", index, CoreHelper.StringToOptionData("Discrete", "Continuous"));
            index++;

            modifierCard.SingleGenerator(modifier, reference, "Drag", index, 0f);
            index++;
            modifierCard.SingleGenerator(modifier, reference, "Velocity X", index, 0f);
            index++;
            modifierCard.SingleGenerator(modifier, reference, "Velocity Y", index, 0f);
            index++;

            modifierCard.DropdownGenerator(modifier, reference, "Body Type", index, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.TryGetResult(out BeatmapObject beatmapObject) && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
            {
                CoreHelper.Destroy(beatmapObject.runtimeObject.visualObject.rigidbody);
                beatmapObject.runtimeObject.visualObject.rigidbody = null;
            }
            else if (modifier.TryGetResult(out GenericGroupCache<BeatmapObject> cache))
                foreach (var other in cache.group)
                {
                    if (!other.runtimeObject || !other.runtimeObject.visualObject)
                        continue;
                    CoreHelper.Destroy(other.runtimeObject.visualObject.rigidbody);
                    other.runtimeObject.visualObject.rigidbody = null;
                }
        }

        #endregion
    }
}
