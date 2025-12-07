using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public abstract class EditorElement
    {
        public RectValues rect;
        public string tooltipGroup;

        public virtual float ContextMenuHeight => 0f;

        public abstract void Init(InitSettings initSettings);

        public void InitTooltip(GameObject gameObject)
        {
            if (!string.IsNullOrEmpty(tooltipGroup))
                TooltipHelper.AssignTooltip(gameObject, tooltipGroup);
        }

        public struct InitSettings
        {
            public Transform parent;

            public InitSettings Parent(Transform parent)
            {
                this.parent = parent;
                return this;
            }
        }
    }

    public class SpacerElement : EditorElement
    {
        public Vector2 size = new Vector2(0f, 4f);

        public override float ContextMenuHeight => 6f;

        public override void Init(InitSettings initSettings)
        {
            if (!initSettings.parent)
                return;

            var gameObject = Creator.NewUIObject("sp", initSettings.parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = size;
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
        }
    }

    public class ButtonElement : EditorElement
    {
        public string name;
        public Action action;

        public override float ContextMenuHeight => 37f;

        public override void Init(InitSettings initSettings)
        {
            if (!initSettings.parent)
                return;

            var gameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(initSettings.parent);
            var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();

            buttonStorage.OnClick.NewListener(() => action?.Invoke());
            buttonStorage.label.alignment = TextAnchor.MiddleLeft;
            buttonStorage.Text = name;
            buttonStorage.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

            InitTooltip(gameObject);

            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Function_2_Text);
        }
    }

    /// <summary>
    /// Handles a buttons' UI in the editor.
    /// </summary>
    public class ButtonFunction
    {
        public ButtonFunction(bool isSpacer, float spacerSize = 4f)
        {
            IsSpacer = isSpacer;
            SpacerSize = spacerSize;
        }

        public ButtonFunction(string name, Action action, string tooltipGroup = null, ThemeGroup? buttonThemeGroup = null, ThemeGroup? labelThemeGroup = null)
        {
            Name = name;
            Action = action;
            TooltipGroup = tooltipGroup;

            ButtonThemeGroup = buttonThemeGroup;
            LabelThemeGroup = labelThemeGroup;
        }

        public ButtonFunction(string name, Action<PointerEventData> onClick, string tooltipGroup = null, ThemeGroup? buttonThemeGroup = null, ThemeGroup? labelThemeGroup = null)
        {
            Name = name;
            OnClick = onClick;
            TooltipGroup = tooltipGroup;

            ButtonThemeGroup = buttonThemeGroup;
            LabelThemeGroup = labelThemeGroup;
        }

        public bool IsSpacer { get; set; }
        public float SpacerSize { get; set; } = 4f;
        public string Name { get; set; }
        public int FontSize { get; set; } = 20;
        public Action Action { get; set; }
        public Action<PointerEventData> OnClick { get; set; }

        public ThemeGroup? ButtonThemeGroup { get; set; }
        public ThemeGroup? LabelThemeGroup { get; set; }

        public string TooltipGroup { get; set; }
    }
}
