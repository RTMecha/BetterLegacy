using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's list of options that pops up when right clicked.
    /// </summary>
    public class ExampleOptions : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default options.
        /// </summary>
        public static ExampleOptions Default
        {
            get
            {
                var options = new ExampleOptions();
                options.InitDefault();
                return options;
            }
        }

        public override void InitDefault()
        {
            options.Clear();
            options.Add(new Option("Chat", () =>
            {
                var active = reference.discussion.Active;
                if (!active)
                    reference.chatBubble.Say("What do you want to talk about?");

                reference.discussion.Toggle();
            }));
            options.Add(new Option("Tutorials", () =>
            {
                // tutorials module goes here
                reference.chatBubble.Say("I can't do that yet, sorry.");
            }));
            if (ModCompatibility.UnityExplorerInstalled)
                options.Add(new Option("Inspect", () => ModCompatibility.Inspect(reference)));
            options.Add(new Option("Cya later", () =>
            {
                reference?.Exit();
            }));
        }

        #endregion

        #region Core

        public override void Build()
        {
            CoreHelper.StartCoroutine(IBuild());
        }

        // wait until fonts have loaded
        IEnumerator IBuild()
        {
            while (!FontManager.inst || !FontManager.inst.loadedFiles)
                yield return null;

            var optionsBase = Creator.NewUIObject("Options Base", reference.model.baseCanvas.transform);

            this.optionsBase = optionsBase.transform;
            optionsBase.SetActive(optionsActive);

            var options = Creator.NewUIObject("Options", optionsBase.transform);

            var optionsImage = options.AddComponent<Image>();
            optionsImage.rectTransform.anchoredPosition = Vector2.zero;
            optionsImage.rectTransform.sizeDelta = new Vector2(200f, 250f);

            EditorThemeManager.ApplyGraphic(optionsImage, ThemeGroup.Background_2, true);

            var optionsLayout = Creator.NewUIObject("Layout", options.transform);
            this.optionsLayout = optionsLayout.transform;
            var optionsVLG = optionsLayout.AddComponent<VerticalLayoutGroup>();
            optionsVLG.childControlHeight = false;
            optionsVLG.childForceExpandHeight = false;
            optionsVLG.spacing = 4f;
            UIManager.SetRectTransform(optionsLayout.transform.AsRT(), Vector2.zero, new Vector2(0.95f, 0.95f), new Vector2(0.05f, 0.05f), new Vector2(0.5f, 0.5f), Vector2.zero);

            try
            {
                for (int i = 0; i < this.options.Count; i++)
                {
                    var option = this.options[i];
                    SetupOptionButton(option.text, option.action, option.index);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error in generating buttons {ex}");
            }

            yield break;
        }

        public override void Tick()
        {
            if (!optionsActive || !optionsBase || !reference || !reference.model)
                return;

            float addToOptionsX = -222f;
            if (reference.model.position.x < -640f)
                addToOptionsX = 222f;

            // Same situation as the dialogue box.
            optionsBase.localPosition = new Vector3(reference.model.position.x + addToOptionsX, reference.model.position.y);
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion

        #region Options

        /// <summary>
        /// Shows the options menu.
        /// </summary>
        public void Show()
        {
            optionsActive = true;
            Render();
        }

        /// <summary>
        /// Hides the options menu.
        /// </summary>
        public void Hide()
        {
            optionsActive = false;
            Render();
        }

        /// <summary>
        /// Toggles the options menu.
        /// </summary>
        public void Toggle()
        {
            optionsActive = !optionsActive;
            Render();
        }

        /// <summary>
        /// Renders the options menu's active state.
        /// </summary>
        public void Render()
        {
            if (optionsBase)
                optionsBase.gameObject.SetActive(optionsActive);
        }

        /// <summary>
        /// Sets up an option.
        /// </summary>
        /// <param name="name">Name to display on the button.</param>
        /// <param name="action">Function to run when clicked.</param>
        /// <param name="index">Index of the option.</param>
        public void SetupOptionButton(string name, UnityAction action, int index = -1)
        {
            var buttonObject = Creator.NewUIObject(name, optionsLayout);
            if (index >= 0 && index < optionsLayout.childCount)
                buttonObject.transform.SetSiblingIndex(index);

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.rectTransform.sizeDelta = new Vector2(180f, 32f);

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            button.image = buttonImage;

            var textObject = Creator.NewUIObject("Text", buttonObject.transform);
            var text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 16;
            text.font = FontManager.inst.DefaultFont;
            text.text = name;
            UIManager.SetRectTransform(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            EditorThemeManager.ApplySelectable(button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Function_2_Text);
        }

        /// <summary>
        /// Active state of the options menu.
        /// </summary>
        public static bool optionsActive;

        /// <summary>
        /// List of options to show.
        /// </summary>
        public List<Option> options = new List<Option>();

        /// <summary>
        /// Represents an option in the options menu.
        /// </summary>
        public class Option
        {
            public Option(string text, UnityAction action, int index = -1)
            {
                this.text = text;
                this.action = action;
                this.index = index;
            }

            /// <summary>
            /// Name to display on the button.
            /// </summary>
            public string text;

            /// <summary>
            /// Function to run when clicked.
            /// </summary>
            public UnityAction action;

            /// <summary>
            /// Index of the option.
            /// </summary>
            public int index = -1;
        }

        Transform optionsLayout;
        Transform optionsBase;

        #endregion
    }
}
