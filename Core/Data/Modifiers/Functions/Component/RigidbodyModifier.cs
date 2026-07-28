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

        public override CategoryType Category => CategoryType.Component;

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

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));
                if (list.IsEmpty())
                    return;

                foreach (var other in list)
                    Apply(other, gravity, collisionMode, drag, bodyType, velocityX, velocityY);
                return;
            }

            if (modifierLoop.reference is BeatmapObject beatmapObject)
                Apply(beatmapObject, gravity, collisionMode, drag, bodyType, velocityX, velocityY);
        }

        int GetValueIndex(int index) => isGroup ? index + 1 : index;

        void Apply(BeatmapObject beatmapObject, float gravity, int collisionMode, float drag, int bodyType, float velocityX, float velocityY)
        {
            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (!beatmapObject.rigidbody)
                beatmapObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

            beatmapObject.rigidbody.gravityScale = gravity;
            beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
            beatmapObject.rigidbody.drag = drag;

            beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

            beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
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

        #endregion
    }
}
