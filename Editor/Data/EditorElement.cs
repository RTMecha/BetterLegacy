using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
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
        public Func<bool> shouldGenerate;
        public bool ShouldGenerate => shouldGenerate == null || shouldGenerate.Invoke();

        /// <summary>
        /// Height of the element in the context menu.
        /// </summary>
        public virtual float ContextMenuHeight => 0f;

        /// <summary>
        /// Initializes the editor element.
        /// </summary>
        /// <param name="initSettings">Initialize settings.</param>
        public abstract void Init(InitSettings initSettings);

        /// <summary>
        /// Initializes the tooltip group.
        /// </summary>
        /// <param name="gameObject">Game object to set the tooltip to.</param>
        public void InitTooltip(GameObject gameObject)
        {
            if (!string.IsNullOrEmpty(tooltipGroup))
                TooltipHelper.AssignTooltip(gameObject, tooltipGroup);
        }

        /// <summary>
        /// Initialize settings for <see cref="EditorElement"/>.
        /// </summary>
        public struct InitSettings
        {
            /// <summary>
            /// Parent of the editor element.
            /// </summary>
            public Transform parent;

            /// <summary>
            /// On click function of the editor element.
            /// </summary>
            public Action<PointerEventData> onClick;

            /// <summary>
            /// Sets the parent of the editor element.
            /// </summary>
            /// <param name="parent">Parent to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings Parent(Transform parent)
            {
                this.parent = parent;
                return this;
            }

            /// <summary>
            /// Sets the on click function of the editor element.
            /// </summary>
            /// <param name="onClick">Function to set.</param>
            /// <returns>Returns the current <see cref="InitSettings"/></returns>
            public InitSettings OnClick(Action<PointerEventData> onClick)
            {
                this.onClick = onClick;
                return this;
            }
        }
    }

    public class SpacerElement : EditorElement
    {
        public SpacerElement() { }

        public Vector2 size = new Vector2(0f, 4f);

        public override float ContextMenuHeight => 6f;

        public override void Init(InitSettings initSettings)
        {
            if (!initSettings.parent)
                return;

            var gameObject = Creator.NewUIObject("spacer element", initSettings.parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = size;
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
        }
    }

    public class ButtonElement : EditorElement
    {
        public ButtonElement(string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null)
        {
            this.name = name;
            this.action = action;
            this.tooltipGroup = tooltipGroup;
            this.shouldGenerate = shouldGenerate;
        }

        public ButtonElement(string name, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null)
        {
            this.name = name;
            this.onClick = onClick;
            this.tooltipGroup = tooltipGroup;
            this.shouldGenerate = shouldGenerate;
        }
        
        public ButtonElement(Func<string> getName, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null)
        {
            this.getName = getName;
            this.action = action;
            this.tooltipGroup = tooltipGroup;
            this.shouldGenerate = shouldGenerate;
        }

        public ButtonElement(Func<string> getName, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null)
        {
            this.getName = getName;
            this.onClick = onClick;
            this.tooltipGroup = tooltipGroup;
            this.shouldGenerate = shouldGenerate;
        }

        /// <summary>
        /// Name to display on the button.
        /// </summary>
        public string name;
        /// <summary>
        /// Action to run on click.
        /// </summary>
        public Action action;
        public Action<PointerEventData> onClick;
        public Func<string> getName;
        public string Name => getName != null ? getName.Invoke() : name;

        public override float ContextMenuHeight => 37f;

        public override void Init(InitSettings initSettings)
        {
            if (!initSettings.parent)
                return;

            var gameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(initSettings.parent);
            var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();

            if (onClick != null)
            {
                buttonStorage.OnClick.ClearAll();
                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = pointerEventData =>
                {
                    onClick.Invoke(pointerEventData);
                    initSettings.onClick?.Invoke(pointerEventData);
                };
            }
            else
                buttonStorage.OnClick.NewListener(() =>
                {
                    action?.Invoke();
                    initSettings.onClick?.Invoke(null);
                });

            buttonStorage.label.alignment = TextAnchor.MiddleLeft;
            buttonStorage.Text = Name;
            buttonStorage.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

            InitTooltip(gameObject);

            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Function_2_Text);
        }

        public static ButtonElement ToggleButton(string name, Func<bool> onState, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => $"{name} [{(onState?.Invoke() ?? false ? "On" : "Off")}]", action, tooltipGroup, shouldGenerate);

        public static ButtonElement ToggleButton(string name, Func<bool> onState, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => $"{name} [{(onState?.Invoke() ?? false ? "On" : "Off")}]", onClick, tooltipGroup, shouldGenerate);

        public static ButtonElement SelectionButton(Func<bool> selectedState, string name, Action action, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => (selectedState?.Invoke() ?? false ? "> " : string.Empty) + name, action, tooltipGroup, shouldGenerate);

        public static ButtonElement SelectionButton(Func<bool> selectedState, string name, Action<PointerEventData> onClick, string tooltipGroup = null, Func<bool> shouldGenerate = null) => new ButtonElement(() => (selectedState?.Invoke() ?? false ? "> " : string.Empty) + name, onClick, tooltipGroup, shouldGenerate);
    }
}
