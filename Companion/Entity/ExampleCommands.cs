using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's commands. The user can use this to talk to Example.
    /// </summary>
    public class ExampleCommands : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default discussion.
        /// </summary>
        public static ExampleCommands Default
        {
            get
            {
                var discussion = new ExampleCommands();
                discussion.InitDefault();
                return discussion;
            }
        }

        public override void InitDefault()
        {
            RegisterCommands();
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

            chatterBase = Creator.NewUIObject("Discussion Base", reference.model.baseCanvas.transform).transform.AsRT();
            chatterBase.transform.AsRT().anchoredPosition = Vector2.zero;
            chatterBase.transform.AsRT().sizeDelta = new Vector2(800f, 96f);
            chatterBase.transform.localScale = Vector2.one;

            var chatterImage = chatterBase.gameObject.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(chatterImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var draggable = chatterBase.gameObject.AddComponent<SelectGUI>();
            draggable.OverrideDrag = true;
            draggable.target = chatterBase;
            draggable.ogPos = chatterBase.anchoredPosition;

            var title = UIManager.GenerateUIText("Title", chatterBase.transform);
            var titleText = (Text)title["Text"];
            titleText.text = "Example Commands";
            titleText.rectTransform.anchoredPosition = new Vector2(8f, -16f);
            titleText.rectTransform.sizeDelta = new Vector2(800f, 100f);

            var uiField = UIManager.GenerateUIInputField("Discussion", chatterBase);

            var chatterField = ((GameObject)uiField["GameObject"]).transform.AsRT();
            chatter = (InputField)uiField["InputField"];
            chatter.image = chatter.GetComponent<Image>();

            chatterField.AsRT().anchoredPosition = new Vector2(0f, -32);
            chatterField.AsRT().sizeDelta = new Vector2(800f, 64f);

            chatter.textComponent.alignment = TextAnchor.MiddleLeft;
            chatter.textComponent.fontSize = 40;

            chatter.onValueChanged.AddListener(SearchCommandAutocomplete);

            chatter.onEndEdit.AddListener(_val => HandleChatting());

            EditorThemeManager.ApplyInputField(chatter);

            autocomplete = Creator.NewUIObject("Autocomplete", chatterBase);
            UIManager.SetRectTransform(autocomplete.transform.AsRT(), new Vector2(-16f, -64f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(768f, 300f));

            EditorThemeManager.ApplyGraphic(autocomplete.AddComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            var scrollrect = autocomplete.AddComponent<ScrollRect>();
            scrollrect.decelerationRate = 0.135f;
            scrollrect.elasticity = 0.1f;
            scrollrect.horizontal = false;
            scrollrect.movementType = ScrollRect.MovementType.Elastic;
            scrollrect.scrollSensitivity = 20f;

            var scrollbar = Creator.NewUIObject("Scrollbar", autocomplete.transform);
            var scrollbarImage = scrollbar.AddComponent<Image>();
            UIManager.SetRectTransform(scrollbar.transform.AsRT(), Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f));

            var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = Creator.NewUIObject("Sliding Area", scrollbar.transform);
            UIManager.SetRectTransform(slidingArea.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

            var handle = Creator.NewUIObject("Handle", slidingArea.transform);
            UIManager.SetRectTransform(handle.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
            var handleImage = handle.AddComponent<Image>();
            scrollbarComponent.handleRect = handle.transform.AsRT();
            scrollbarComponent.targetGraphic = handleImage;

            scrollrect.verticalScrollbar = scrollbarComponent;

            EditorThemeManager.ApplyScrollbar(scrollbarComponent);

            var mask = Creator.NewUIObject("Mask", autocomplete.transform);
            UIManager.SetRectTransform(mask.transform.AsRT(), new Vector2(0f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            var maskImage = mask.AddComponent<Image>();
            var maskComponent = mask.AddComponent<Mask>();
            maskComponent.showMaskGraphic = false;

            var content = Creator.NewUIObject("Content", mask.transform);
            UIManager.SetRectTransform(content.transform.AsRT(), new Vector2(0f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var gridLayoutGroup = content.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.cellSize = new Vector2(768f, 86f);
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 1;
            gridLayoutGroup.spacing = new Vector2(0f, 8f);
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Vertical;
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;

            autocompleteContent = content.transform.AsRT();
            scrollrect.content = autocompleteContent;

            commandAutocompletePrefab = Creator.NewUIObject("Autocomplete Prefab", chatterBase);
            commandAutocompletePrefab.SetActive(false);
            var commandAutocompletePrefabImage = commandAutocompletePrefab.AddComponent<Image>();

            commandAutocompletePrefab.AddComponent<Button>().image = commandAutocompletePrefabImage;

            var commandAutocompletePrefabName = UIManager.GenerateUIText("Name", commandAutocompletePrefab.transform);
            UIManager.SetRectTransform((RectTransform)commandAutocompletePrefabName["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            var commandAutocompletePrefabDesc = UIManager.GenerateUIText("Desc", commandAutocompletePrefab.transform);
            UIManager.SetRectTransform((RectTransform)commandAutocompletePrefabDesc["RectTransform"], new Vector2(0f, -16f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f));

            CoreHelper.StartCoroutine(SetupCommandsAutocomplete());

            chatterBase.gameObject.SetActive(false);

            yield break;
        }

        IEnumerator SetupCommandsAutocomplete()
        {
            LSHelpers.DeleteChildren(autocompleteContent);

            foreach (var command in commands.Where(x => x.autocomplete))
            {
                var autocomplete = commandAutocompletePrefab.Duplicate(autocompleteContent, "Autocomplete");
                autocomplete.SetActive(true);

                var autocompleteButton = autocomplete.GetComponent<Button>();
                autocompleteButton.onClick.ClearAll();
                autocompleteButton.onClick.AddListener(() =>
                {
                    chatter.text = command.name;
                    command.CheckResponse(command.name);

                    SearchCommandAutocomplete(command.name);
                });

                EditorThemeManager.ApplySelectable(autocompleteButton, ThemeGroup.List_Button_1);

                var autocompleteName = autocomplete.transform.Find("Name").GetComponent<Text>();
                autocompleteName.text = command.name.ToUpper();
                autocompleteName.fontSize = 28;
                autocompleteName.fontStyle = FontStyle.Bold;
                EditorThemeManager.ApplyLightText(autocompleteName);
                var autocompleteDesc = autocomplete.transform.Find("Desc").GetComponent<Text>();
                autocompleteDesc.text = command.desc;
                EditorThemeManager.ApplyLightText(autocompleteDesc);
            }

            yield break;
        }

        public override void Tick()
        {
            if (autocomplete && chatter)
                autocomplete.SetActive(!string.IsNullOrEmpty(chatter.text));
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion

        #region Commands

        /// <summary>
        /// List of commands with responses.
        /// </summary>
        public List<ExampleCommand> commands = new List<ExampleCommand>();

        /// <summary>
        /// Registers commands.
        /// </summary>
        public virtual void RegisterCommands()
        {
            RegisterChat("Greet Example", ExampleChatBubble.Dialogues.GREETING,
                new List<ExampleCommand.Phrase>
                {
                    new ExampleCommand.Phrase("hello example"),
                    new ExampleCommand.Phrase("hello buddy"),
                    new ExampleCommand.Phrase("hello friend"),
                    new ExampleCommand.Phrase("hello"),
                    new ExampleCommand.Phrase("hey example"),
                    new ExampleCommand.Phrase("hey buddy"),
                    new ExampleCommand.Phrase("hey friend"),
                    new ExampleCommand.Phrase("hey"),
                    new ExampleCommand.Phrase("hi example"),
                    new ExampleCommand.Phrase("hi buddy"),
                    new ExampleCommand.Phrase("hi friend"),
                    new ExampleCommand.Phrase("hi"),
                });
            RegisterChat("Love Example", ExampleChatBubble.Dialogues.LOVE,
                new List<ExampleCommand.Phrase>
                {
                    new ExampleCommand.Phrase("i love example"),
                    new ExampleCommand.Phrase("i love you"),
                    new ExampleCommand.Phrase("i like example"),
                    new ExampleCommand.Phrase("i like you"),
                });
            RegisterChat("Hate Example", ExampleChatBubble.Dialogues.HATE,
                new List<ExampleCommand.Phrase>
                {
                    new ExampleCommand.Phrase("i hate example"),
                    new ExampleCommand.Phrase("i hate you"),
                    new ExampleCommand.Phrase("i dislike example"),
                    new ExampleCommand.Phrase("i dislike you"),
                });

            commands.Add(new ExampleCommand("evaluate (1 + 1)", "Evaluates a math expression", true,
                response =>
                {
                    try
                    {
                        reference.chatBubble.Say(RTMath.Parse(response).ToString());
                    }
                    catch
                    {
                        reference.chatBubble.Say("Couldn't calculate that, sorry...");
                    }
                }, true,
                new List<ExampleCommand.Phrase>
                {
                    new ExampleCommand.Phrase("evaluate \\((.*?)\\)", true),
                }));

            commands.Add(new ExampleCommand("Select all objects", "Selects every Beatmap Object and Prefab Object in the current level.", true,
                response => EditorHelper.SelectAllObjects()));
            commands.Add(new ExampleCommand("Select all objects on current layer", "Selects every Beatmap Object and Prefab Object in the current level's currently viewed editor layer.", true,
                response => EditorHelper.SelectAllObjectsOnCurrentLayer()));
            commands.Add(new ExampleCommand("Mirror Selection", "Horizontally mirrors selected objects.", true,
                response => EditorHelper.MirrorSelectedObjects()));
            commands.Add(new ExampleCommand("Flip Selection", "Vertically flips selected objects.", true,
                response => EditorHelper.MirrorSelectedObjects()));
            commands.Add(new ExampleCommand("Refresh selected objects animations", "Updates just the animation of all selected objects.", true,
                response => EditorHelper.RefreshKeyframesFromSelection()));
            commands.Add(new ExampleCommand("Give a random idea", "Example outputs a random idea.", true,
                response => reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.RANDOM_IDEA)));
        }

        /// <summary>
        /// Registers a command that acts as a chat message to Example.
        /// </summary>
        /// <param name="key">Key of the message.</param>
        /// <param name="dialogue">Dialogue key to respond with.</param>
        /// <param name="phrases">Phrases to match.</param>
        public void RegisterChat(string key, string dialogue, List<ExampleCommand.Phrase> phrases)
        {
            commands.Add(new ExampleCommand(key, key, false,
                response => reference.interactions.Interact(ExampleInteractions.Interactions.CHAT, new ChatInteractParameters(dialogue, response)), phrases));
        }

        /// <summary>
        /// Searches for a command.
        /// </summary>
        /// <param name="searchTerm">Search term.</param>
        public void SearchCommandAutocomplete(string searchTerm)
        {
            CoreHelper.Log($"Typing: {searchTerm}");
            int num = 0;
            for (int i = 0; i < commands.Count; i++)
            {
                if (!commands[i].autocomplete)
                    continue;

                try
                {
                    autocompleteContent.GetChild(num).gameObject.SetActive(RTString.SearchString(searchTerm, commands[i].name));
                }
                catch
                {

                }
                num++;
            }
        }

        void HandleChatting()
        {
            if (chatter == null)
                return;

            reference?.interactions?.Interact(ExampleInteractions.Interactions.CHAT);

            for (int i = 0; i < commands.Count; i++)
                commands[i].CheckResponse(chatter.text);

            AchievementManager.inst.UnlockAchievement("example_chat");
        }

        #endregion

        #region UI

        /// <summary>
        /// If the commands window is visible.
        /// </summary>
        public bool Active => chatterBase && chatterBase.gameObject.activeSelf;

        /// <summary>
        /// Shows the commands window.
        /// </summary>
        public void Show()
        {
            if (chatterBase)
                chatterBase.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the commands window.
        /// </summary>
        public void Hide()
        {
            if (chatterBase)
                chatterBase.gameObject.SetActive(false);
        }

        /// <summary>
        /// Toggles the commands window.
        /// </summary>
        public void Toggle()
        {
            if (chatterBase)
                chatterBase.gameObject.SetActive(!chatterBase.gameObject.activeSelf);
        }

        GameObject commandAutocompletePrefab;

        GameObject autocomplete;
        InputField chatter;
        RectTransform chatterBase;
        RectTransform autocompleteContent;

        #endregion
    }
}
