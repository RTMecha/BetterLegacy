using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BetterLegacy.Companion.Entity
{
    public class ExampleOptions : ExampleModule
    {
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
                var active = reference.discussion.chatterBase.gameObject.activeSelf;
                if (!active)
                    reference.chatBubble.Say("What do you want to talk about?");

                reference.discussion.chatterBase.gameObject.SetActive(!active);
            }));
            options.Add(new Option("Tutorials", () =>
            {
                // tutorials module goes here
                reference.chatBubble.Say("I can't do that yet, sorry.");
            }));
            options.Add(new Option("Cya later", () =>
            {

            }));
        }

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

        #region Options

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

        public static bool optionsActive;
        public Transform optionsLayout;
        public Transform optionsBase;

        public List<Option> options = new List<Option>();

        public class Option
        {
            public Option(string text, UnityAction action, int index = -1)
            {
                this.text = text;
                this.action = action;
                this.index = index;
            }

            public string text;
            public UnityAction action;
            public int index = -1;
        }

        #endregion
    }
}
