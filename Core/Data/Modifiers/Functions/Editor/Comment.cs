using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Comment : ModifierActionBase
    {
        #region Constructors

        public Comment() => SetupModifier(
            "Default comment.", // comment
            "False", // lock
            "126" /// height
            );

        #endregion

        #region Values

        public override string Name => "comment";

        public override CategoryType Category => CategoryType.Editor;

        public override Sprite Icon => EditorSprites.EditSprite;

        public override bool IsEditorModifier => true;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) { }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var height = Mathf.Clamp(modifier.GetFloat(2, 126f), 20f, 512f);
            modifierCard.layout.AsRT().sizeDelta = new Vector2(340f, height);
            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(modifierCard.layout, "Input");
            input.transform.localScale = Vector2.one;
            input.transform.AsRT().sizeDelta = new Vector2(340f, height);
            input.transform.Find("Text").AsRT().sizeDelta = Vector2.zero;
            var inputField = input.GetComponent<InputField>();
            inputField.textComponent.alignment = TextAnchor.UpperLeft;
            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.interactable = !modifier.GetBool(1, false);
            inputField.SetTextWithoutNotify(modifier.GetValue(0));
            inputField.onValueChanged.NewListener(_val =>
            {
                modifier.SetValue(0, _val);
                modifierCard.Update(modifier, reference);
            });

            EditorThemeManager.ApplyInputField(inputField);

            EditorContextMenu.AddContextMenu(input,
                new ButtonElement(() => modifier.GetBool(1, false) ? "Unlock comment" : "Lock comment", () =>
                {
                    modifier.SetValue(1, (!modifier.GetBool(1, false)).ToString());
                    modifierCard.Update(modifier, reference);

                    if (inputField)
                        inputField.interactable = !modifier.GetBool(1, false);
                }),
                new LabelElement("Height"),
                new NumberInputElement(() => modifier.GetFloat(2, 126f).ToString(), _val =>
                {
                    var height = Mathf.Clamp(Parser.TryParse(_val, 126f), 20f, 512f);
                    modifier.SetValue(2, height.ToString());
                    modifierCard.Update(modifier, reference);

                    if (input)
                        input.transform.AsRT().sizeDelta = new Vector2(340f, height);
                    if (modifierCard.layout)
                    {
                        modifierCard.layout.AsRT().sizeDelta = new Vector2(340f, height);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(modifierCard.layout.AsRT());
                        LayoutRebuilder.ForceRebuildLayoutImmediate(modifierCard.layout.parent.AsRT());
                    }
                }, new NumberInputElement.ArrowHandlerFloat()
                {
                    min = 20f,
                    max = 512f,
                }),
                new ButtonElement("Reset Height", () =>
                {
                    modifier.SetValue(2, "126");
                    modifierCard.Update(modifier, reference);

                    if (input)
                        input.transform.AsRT().sizeDelta = new Vector2(340f, 126f);
                    if (modifierCard.layout)
                    {
                        modifierCard.layout.AsRT().sizeDelta = new Vector2(340f, 126f);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(modifierCard.layout.AsRT());
                        LayoutRebuilder.ForceRebuildLayoutImmediate(modifierCard.layout.parent.AsRT());
                    }
                }));
        }

        #endregion
    }
}
