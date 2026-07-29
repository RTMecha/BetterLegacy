using UnityEngine.UI;

using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetIdleAnimation : ModifierActionBase
    {
        #region Constructors

        public SetIdleAnimation() => Modifier = CreateModifier(Name, false, "0", "boost");

        #endregion

        #region Values

        public override string Name => "setIdleAnimation";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullPlayerCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = modifier.GetValue(0, modifierLoop.variables);
            var referenceID = modifier.GetValue(1, modifierLoop.variables);
            var customPlayerObject = modifierLoop.reference as RTPlayer.RTCustomPlayerObject;
            var player = customPlayerObject ? customPlayerObject.Player.Core : modifierLoop.reference as PAPlayer;

            if (!player || !player.RuntimePlayer)
                return;

            var customObject = string.IsNullOrEmpty(id) && customPlayerObject ? customPlayerObject : player.RuntimePlayer.customObjects.Find(x => x.id == id);

            if (customObject && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
                customObject.currentIdleAnimation = animation.ReferenceID;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "ID", 0);
            var referenceID = modifierCard.StringGenerator(modifier, reference, "Reference ID", 1);
            var customPlayerObject = PlayerEditor.inst.CurrentCustomObject;
            if (!customPlayerObject)
                return;

            ITransformable transformable = null;
            var player = PlayerEditor.inst.CurrentPlayer;
            if (player && player.RuntimePlayer)
            {
                var id = modifier.GetValue(0);
                if (!string.IsNullOrEmpty(id))
                    transformable = player.RuntimePlayer.customObjects.Find(x => x.id == id);
            }

            EditorContextMenu.AddContextMenu(referenceID,
                new ButtonElement("Select Animation", () => AnimationEditor.inst.OpenPopup(customPlayerObject.animations, PlayerEditor.inst.PlayAnimation, animation =>
                {
                    referenceID.transform.Find("Input").GetComponent<InputField>().text = animation.ReferenceID;
                }, transformable)));
        }

        #endregion
    }
}
