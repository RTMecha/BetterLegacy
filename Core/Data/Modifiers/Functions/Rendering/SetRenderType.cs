using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetRenderType : ModifierActionBase
    {
        #region Constructors

        public SetRenderType(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setRenderType";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "0");
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Rendering;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));
                if (list.IsEmpty())
                    return;

                var renderType = modifier.GetInt(1, 0, modifierLoop.variables);
                foreach (var other in list)
                {
                    if (other.runtimeObject && other.runtimeObject.visualObject)
                        other.runtimeObject.visualObject.SetRenderType(renderType);
                }
                return;
            }

            if (modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                beatmapObject.runtimeObject.visualObject.SetRenderType(modifier.GetInt(0, 0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", 0).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));
                modifierCard.DropdownGenerator(modifier, reference, "Render Type", 1, CoreHelper.ToOptionData<RenderLayerType>());
                return;
            }
            modifierCard.DropdownGenerator(modifier, reference, "Render Type", 0, CoreHelper.ToOptionData<RenderLayerType>());
        }

        #endregion
    }
}
