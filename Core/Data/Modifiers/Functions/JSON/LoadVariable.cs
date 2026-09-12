using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadVariable : ModifierActionBase
    {
        public LoadVariable(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "loadVariable";
            if (isGroup)
                Name += "Other";
            IsGroup = isGroup;
        }

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.JSON;

        readonly bool isGroup;

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, modifierLoop.variables) + FileFormat.SES.Dot());
                if (!RTFile.FileExists(path))
                    return;

                string json = RTFile.ReadFromFile(path);

                if (string.IsNullOrEmpty(json))
                    return;

                var jn = JSON.Parse(json);
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));
                var fjn = jn[modifier.GetValue(2, modifierLoop.variables)][modifier.GetValue(3, modifierLoop.variables)]["float"];

                if (list.Count > 0 && !string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                    foreach (var bm in list)
                        bm.integerVariable = (int)eq;
            }
            else
            {
                var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, modifierLoop.variables) + FileFormat.SES.Dot());
                if (!RTFile.FileExists(path))
                    return;

                string json = RTFile.ReadFromFile(path);

                if (string.IsNullOrEmpty(json))
                    return;

                var jn = JSON.Parse(json);

                var fjn = jn[modifier.GetValue(2, modifierLoop.variables)][modifier.GetValue(3, modifierLoop.variables)]["float"];
                if (!string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                    modifierLoop.reference.IntVariable = (int)eq;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            }

            modifierCard.StringGenerator(modifier, reference, "Path", 1);
            modifierCard.StringGenerator(modifier, reference, "JSON 1", 2);
            modifierCard.StringGenerator(modifier, reference, "JSON 2", 3);
        }
    }
}
