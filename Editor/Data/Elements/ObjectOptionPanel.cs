using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    public class ObjectOptionPanel : EditorPanel<ObjectOption>
    {
        public ObjectOptionPanel() { }

        public ObjectOptionPanel(bool isDefault, Transform parent)
        {
            this.isDefault = isDefault;
            Parent = parent;
        }

        #region Values

        public bool isDefault;

        public Transform Parent { get; set; }

        public List<ObjectOptionPanel> SubOptions { get; set; }

        public Vector2 cellSize;
        public Vector2 spacing;

        public override string DisplayName => Item.name;

        #endregion

        #region Methods

        public override void Init(ObjectOption item)
        {
            Item = item;

            var name = item.name;
            var hint = item.hint;

            CoreHelper.Delete(GameObject);

            if (isDefault)
            {
                GameObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(Parent, name);
                var buttonStorage = GameObject.GetComponent<FunctionButtonStorage>();

                GameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new Tooltip { desc = name, hint = hint });

                buttonStorage.OnClick.NewListener(item.Create);
                buttonStorage.Text = name;

                Label = buttonStorage.label;

                EditorThemeManager.AddGraphic(buttonStorage.button.image, ThemeGroup.Function_3, true);
                EditorThemeManager.AddGraphic(buttonStorage.label, ThemeGroup.Function_3_Text);

                var icon = item.GetIcon();

                if (icon)
                {
                    Label.gameObject.SetActive(false);

                    var iconObject = Creator.NewUIObject("icon", GameObject.transform);
                    RectValues.FullAnchored.AssignToRectTransform(iconObject.transform.AsRT());
                    var iconImage = iconObject.AddComponent<Image>();
                    iconImage.sprite = icon;

                    EditorThemeManager.AddGraphic(iconImage, ThemeGroup.Function_3_Text);
                }
            }
            else
            {
                GameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Parent, name);
                var buttonStorage = GameObject.GetComponent<FunctionButtonStorage>();

                GameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new Tooltip { desc = name, hint = hint });

                buttonStorage.OnClick.NewListener(item.Create);
                buttonStorage.Text = name;

                Label = buttonStorage.label;

                EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(buttonStorage.label);
            }

            Render();
        }

        public void Init(List<ObjectOption> options, Vector2 cellSize, Vector2 spacing)
        {
            if (SubOptions == null)
                SubOptions = new List<ObjectOptionPanel>();

            SubOptions.Clear();

            CoreHelper.Delete(GameObject);

            this.cellSize = cellSize;
            this.spacing = spacing;

            GameObject = Creator.NewUIObject("group", Parent);
            var gridLayoutGroup = GameObject.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.cellSize = cellSize;
            gridLayoutGroup.spacing = spacing;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var optionPanel = new ObjectOptionPanel(isDefault, GameObject.transform);
                optionPanel.Init(option);
                SubOptions.Add(optionPanel);
            }
        }

        public override void Render()
        {
            RenderLabel();
        }

        public override void RenderLabel(string text)
        {
            if (Label)
                Label.text = text;
        }

        #endregion
    }
}
