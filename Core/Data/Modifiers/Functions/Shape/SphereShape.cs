using UnityEngine;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SphereShape : ModifierActionBase
    {
        #region Constructors

        public SphereShape() => SetupModifier(false, "0");

        #endregion

        #region Values

        public override string Name => "sphereShape";

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var option = modifier.GetInt(0, 0, modifierLoop.variables);

            runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = option switch
            {
                1 => LegacyResources.halfSphereMesh,
                2 => LegacyResources.quarterSphereMesh,
                3 => LegacyResources.eighthSphereMesh,
                _ => GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh,
            };
            modifier.Result = "frick";
            runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Option", 0, CoreHelper.StringToOptionData("Full", "Half", "Quarter", "Eighth"));
        }

        #endregion
    }
}
