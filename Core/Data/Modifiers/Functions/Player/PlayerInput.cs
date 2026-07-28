using UnityEngine.UI;

using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerInputTrigger : PlayerTriggerBase
    {
        #region Constructors

        public PlayerInputTrigger(Requirement requirement) : base(requirement)
        {
            Name = "playerInput";
            if (requirement != Requirement.Nearest)
                Name += requirement.ToString();
            SetupModifier("Boost", "0");
            if (requirement == Requirement.Index)
                Modifier.values.Insert(0, "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        #endregion

        #region Functions

        public override bool CheckPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            var name = modifier.GetValue(requirement == Requirement.Index ? 1 : 0, modifierLoop.variables);
            var type = modifier.GetInt(requirement == Requirement.Index ? 2 : 1, 0, modifierLoop.variables);
            if (player && player.Input && player.Input.TryGetPlayerAction(name, out InControl.PlayerAction playerAction))
                return type switch
                {
                    0 => playerAction.WasPressed,
                    1 => playerAction.IsPressed,
                    2 => playerAction.WasReleased,
                    _ => false,
                };
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            var str = modifierCard.StringGenerator(modifier, reference, "Action Name", requirement == Requirement.Index ? 1 : 0);
            var inputField = str.transform.Find("Input").GetComponent<InputField>();
            var elements = new EditorElement[PlayerInput.Names.AllNames.Length + 1];
            elements[0] = new LabelElement("Action Names");
            for (int i = 1; i < elements.Length; i++)
            {
                var n = PlayerInput.Names.AllNames[i - 1];
                elements[i] = new ButtonElement(n, () => inputField.text = n);
            }
            EditorContextMenu.AddContextMenu(inputField.gameObject, elements);
            modifierCard.DropdownGenerator(modifier, reference, "Held Type", requirement == Requirement.Index ? 2 : 1, CoreHelper.StringToOptionData("Was Pressed", "Is Pressed", "Was Released"));
        }

        #endregion
    }
}
