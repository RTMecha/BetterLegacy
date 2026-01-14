using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Companion.Data;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's commands. The user can use this to talk to Example.
    /// </summary>
    public class ExampleCommands : ExampleModule<ExampleCommands>
    {
        #region Default Instance

        public ExampleCommands() { }

        public override void InitDefault()
        {
            RegisterFunctions();
            RegisterCommands();
        }

        #endregion

        #region Core

        public override void Build() => CoroutineHelper.StartCoroutine(IBuild());

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

            var draggable = chatterBase.gameObject.AddComponent<DraggableUI>();
            draggable.mode = DraggableUI.DragMode.RequiredDrag;
            draggable.target = chatterBase;
            draggable.ogPos = chatterBase.anchoredPosition;

            var title = Creator.NewUIObject("Title", chatterBase.transform);
            var titleText = title.AddComponent<Text>();
            titleText.font = Font.GetDefault();
            titleText.fontSize = 20;
            titleText.text = "Example Commands";
            titleText.rectTransform.anchoredPosition = new Vector2(8f, -16f);
            titleText.rectTransform.sizeDelta = new Vector2(800f, 100f);

            chatter = UIManager.GenerateInputField("Discussion", chatterBase);
            RectValues.Default.AnchoredPosition(-32f, -32f).SizeDelta(736f, 64f).AssignToRectTransform(chatter.image.rectTransform);

            chatter.textComponent.alignment = TextAnchor.MiddleLeft;
            chatter.textComponent.fontSize = 20;

            chatter.onValueChanged.AddListener(SearchCommandAutocomplete);
            chatter.onEndEdit.AddListener(_val => CompanionManager.Log($"Typing: {_val}"));

            EditorThemeManager.ApplyInputField(chatter);

            var submit = Creator.NewUIObject("Submit", chatterBase);
            RectValues.Default.AnchoredPosition(368f, -32f).SizeDelta(64f, 64f).AssignToRectTransform(submit.transform.AsRT());
            var submitImage = submit.AddComponent<Image>();
            submitButton = submit.AddComponent<Button>();
            submitButton.image = submitImage;
            submitButton.onClick.NewListener(() =>
            {
                try
                {
                    HandleChatting(chatter.text);
                }
                catch (Exception ex)
                {
                    reference?.chatBubble?.Say("Uh oh, something went wrong!");
                    CoreHelper.LogException(ex);
                }
            });

            EditorThemeManager.ApplyGraphic(submitImage, ThemeGroup.Function_1, true);

            var submitIcon = Creator.NewUIObject("Icon", submit.transform.AsRT());
            RectValues.FullAnchored.AssignToRectTransform(submitIcon.transform.AsRT());
            var submitIconImage = submitIcon.AddComponent<Image>();
            submitIconImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/play.png"));

            EditorThemeManager.ApplyGraphic(submitIconImage, ThemeGroup.Function_1_Text);

            autocomplete = Creator.NewUIObject("Autocomplete", chatterBase);
            RectValues.Default.AnchoredPosition(-16f, -64f).Pivot(0.5f, 1f).SizeDelta(768f, 300f).AssignToRectTransform(autocomplete.transform.AsRT());

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
            gridLayoutGroup.cellSize = new Vector2(768f, 140f);
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

            var commandAutocompletePrefabButton = commandAutocompletePrefab.AddComponent<Button>();
            commandAutocompletePrefabButton.image = commandAutocompletePrefabImage;

            var commandAutocompletePrefabName = Creator.NewUIObject("Name", commandAutocompletePrefab.transform);
            var commandAutocompletePrefabNameText = commandAutocompletePrefabName.AddComponent<Text>();
            commandAutocompletePrefabNameText.font = Font.GetDefault();
            commandAutocompletePrefabNameText.fontSize = 28;
            commandAutocompletePrefabNameText.fontStyle = FontStyle.Bold;
            UIManager.SetRectTransform(commandAutocompletePrefabName.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            var commandAutocompletePrefabDesc = Creator.NewUIObject("Desc", commandAutocompletePrefab.transform);
            var commandAutocompletePrefabDescText = commandAutocompletePrefabDesc.AddComponent<Text>();
            commandAutocompletePrefabDescText.font = Font.GetDefault();
            commandAutocompletePrefabDescText.fontSize = 20;
            UIManager.SetRectTransform(commandAutocompletePrefabDesc.transform.AsRT(), new Vector2(0f, -48f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f));

            EditorThemeManager.ApplySelectable(commandAutocompletePrefabButton, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(commandAutocompletePrefabNameText);
            EditorThemeManager.ApplyLightText(commandAutocompletePrefabDescText);

            CoroutineHelper.StartCoroutine(ISetupCommandsAutocomplete());

            chatterBase.gameObject.SetActive(false);

            yield break;
        }

        public void SetupCommandsAutocomplete() => CoroutineHelper.StartCoroutine(ISetupCommandsAutocomplete());

        public IEnumerator ISetupCommandsAutocomplete()
        {
            LSHelpers.DeleteChildren(autocompleteContent);
            autocompletes.Clear();
            foreach (var command in ExampleCommand.commands.Where(x => x.Autocomplete))
            {
                var autocomplete = new CommandAutocomplete();
                autocomplete.command = command;
                autocomplete.gameObject = commandAutocompletePrefab.Duplicate(autocompleteContent, "Autocomplete");
                autocomplete.gameObject.SetActive(true);

                var autocompleteButton = autocomplete.gameObject.GetComponent<Button>();
                autocompleteButton.onClick.NewListener(() => chatter.text = command.AddToAutocomplete);

                EditorThemeManager.ApplySelectable(autocompleteButton, ThemeGroup.List_Button_1);

                var autocompleteName = autocomplete.gameObject.transform.Find("Name").GetComponent<Text>();
                autocompleteName.text = command.Name;
                if (command.Pattern != command.Name)
                    autocompleteName.text += $" ({command.Pattern})";
                EditorThemeManager.ApplyLightText(autocompleteName);
                var autocompleteDesc = autocomplete.gameObject.transform.Find("Desc").GetComponent<Text>();
                autocompleteDesc.text = command.Description;
                EditorThemeManager.ApplyLightText(autocompleteDesc);

                autocompletes.Add(autocomplete);
            }
            SearchCommandAutocomplete(chatter.text);
            yield break;
        }

        public override void Tick()
        {
            if (autocomplete && chatter)
                autocomplete.SetActive(ExampleConfig.Instance.ShowAutocompleteWithEmptyInput.Value || !string.IsNullOrEmpty(chatter.text));

            try
            {
                if (onTickJSON != null)
                    functions.ParseFunction(onTickJSON, this, GetVariables());
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public override void Clear()
        {
            base.Clear();
            autocompletes.Clear();
            commandAutocompletePrefab = null;
            autocomplete = null;
            chatter = null;
            chatterBase = null;
            autocompleteContent = null;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Registers commands.
        /// </summary>
        public virtual void RegisterCommands() { }

        /// <summary>
        /// Searches for a command.
        /// </summary>
        /// <param name="searchTerm">Search term.</param>
        public void SearchCommandAutocomplete(string searchTerm)
        {
            var origSearch = searchTerm;
            string firstWord = null;
            string[] split = null;
            string mode = null;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                split = searchTerm.Split(' ');
                if (!split.IsEmpty())
                {
                    firstWord = split[0];
                    searchTerm = split[split.Length - 1];
                    for (int i = 0; i < split.Length; i++)
                    {
                        var s = split[i];
                        if (s == "->")
                            mode = "action";
                        if (s == "edit")
                            mode = "edit";
                        if (s == "select")
                        {
                            mode = "select";
                            firstWord = mode;
                        }
                    }
                }
            }
            for (int i = 0; i < parameterAutocompletes.Count; i++)
                CoreHelper.Delete(parameterAutocompletes[i].gameObject);
            parameterAutocompletes.Clear();
            CompanionManager.Log($"Command: {searchTerm}\nTyping: {origSearch}");
            for (int i = 0; i < autocompletes.Count; i++)
            {
                var autocomplete = autocompletes[i];
                if (!autocomplete.gameObject)
                    continue;

                if (string.IsNullOrEmpty(firstWord) || split == null || split.Length <= 1)
                {
                    autocomplete.gameObject.SetActive(autocomplete.command.Usable && RTString.SearchString(searchTerm, autocomplete.command.Name));
                    continue;
                }

                // search extra autocompletes here
                autocomplete.gameObject.SetActive(false);
                if (firstWord.ToLower() != autocomplete.command.Name)
                    continue;

                var parameters = autocomplete.command.GetParameters();
                if (autocomplete.command is ExampleCommand.SelectCommand selectCommand)
                {
                    if (mode == "action")
                        parameters = selectCommand.actionParameters;
                    if (mode == "edit")
                        parameters = new ExampleCommand.EditCommand().GetParameters();
                }
                if (parameters == null)
                    continue;

                foreach (var parameter in parameters)
                {
                    if (!RTString.SearchString(searchTerm, parameter.Name) || !string.IsNullOrEmpty(searchTerm) && searchTerm.ToLower() == parameter.Name.ToLower())
                        continue;

                    var parameterAutocomplete = new ParameterAutocomplete();
                    parameterAutocomplete.parameter = parameter;
                    parameterAutocomplete.gameObject = commandAutocompletePrefab.Duplicate(autocompleteContent, "Autocomplete");
                    parameterAutocomplete.gameObject.SetActive(true);

                    var autocompleteButton = parameterAutocomplete.gameObject.GetComponent<Button>();
                    autocompleteButton.onClick.NewListener(() =>
                    {
                        var text = chatter.text;
                        text = text.Substring(0, text.LastIndexOf(' ')) + " " + parameter.AddToAutocomplete;
                        chatter.text = text;
                    });

                    EditorThemeManager.ApplySelectable(autocompleteButton, ThemeGroup.List_Button_1);

                    var autocompleteName = parameterAutocomplete.gameObject.transform.Find("Name").GetComponent<Text>();
                    autocompleteName.text = parameter.Name;
                    EditorThemeManager.ApplyLightText(autocompleteName);
                    var autocompleteDesc = parameterAutocomplete.gameObject.transform.Find("Desc").GetComponent<Text>();
                    autocompleteDesc.text = parameter.Description;
                    EditorThemeManager.ApplyLightText(autocompleteDesc);

                    parameterAutocompletes.Add(parameterAutocomplete);
                }
            }
        }

        void HandleChatting(string chat)
        {
            if (chatter == null || string.IsNullOrEmpty(chat))
                return;

            try
            {
                ExampleCommand.Run(chat);
                reference?.brain?.Interact(ExampleBrain.Interactions.CHAT);
                AchievementManager.inst.UnlockAchievement("example_chat");
            }
            catch (Exception ex)
            {
                CompanionManager.LogError($"Command line failed to parse due to the exception: {ex}");
            }
        }

        #endregion

        #region UI

        /// <summary>
        /// If the commands window is visible.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Shows the commands window.
        /// </summary>
        public void Show()
        {
            Active = true;
            Render();
        }

        /// <summary>
        /// Hides the commands window.
        /// </summary>
        public void Hide()
        {
            Active = true;
            Render();
        }

        /// <summary>
        /// Toggles the commands window.
        /// </summary>
        public void Toggle()
        {
            Active = !Active;
            Render();
        }

        /// <summary>
        /// Renders the commands window's active state.
        /// </summary>
        public void Render()
        {
            if (chatterBase)
                chatterBase.gameObject.SetActive(Active);
        }

        GameObject commandAutocompletePrefab;

        GameObject autocomplete;
        InputField chatter;
        RectTransform chatterBase;
        Button submitButton;
        RectTransform autocompleteContent;

        #endregion

        #region JSON Functions

        public override void RegisterFunctions()
        {
            functions = new Functions();
            functions.LoadCustomJSONFunctions("companion/commands/functions.json");

            if (AssetPack.TryReadFromFile("companion/commands/behavior.json", out string behaviorFile))
                onTickJSON = JSON.Parse(behaviorFile)["on_tick"];
        }

        public override Dictionary<string, JSONNode> GetVariables() => new Dictionary<string, JSONNode>();

        public class Functions : JSONFunctionParser<ExampleCommands>
        {
            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, ExampleCommands thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, ExampleCommands thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "SetAttribute": {
                            if (parameters == null || !thisElement)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (!Parser.IsCompatibleString(id))
                                return;

                            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables).AsDouble;
                            var min = ParseVarFunction(parameters.Get(2, "min"), thisElement, customVariables).AsDouble;
                            var max = ParseVarFunction(parameters.Get(3, "max"), thisElement, customVariables).AsDouble;

                            thisElement.GetAttribute(id, value, min, max).Value = value;
                            return;
                        }
                    case "SetAttributeOperation": {
                            if (parameters == null || !thisElement)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (!Parser.IsCompatibleString(id))
                                return;

                            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables).AsDouble;
                            var operation = Parser.TryParse(ParseVarFunction(parameters.Get(2, "operation"), thisElement, customVariables), MathOperation.Addition);
                            thisElement.SetAttribute(id, value, operation);
                            return;
                        }
                }

                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, ExampleCommands thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }

        #endregion

        #region Autocomplete

        /// <summary>
        /// List of regular commands that should appear in the autocomplete.
        /// </summary>
        public List<CommandAutocomplete> autocompletes = new List<CommandAutocomplete>();

        /// <summary>
        /// List of command parameters that should appear in the autocomplete.
        /// </summary>
        public List<ParameterAutocomplete> parameterAutocompletes = new List<ParameterAutocomplete>();

        /// <summary>
        /// Represents a command in the autocomplete.
        /// </summary>
        public class CommandAutocomplete
        {
            /// <summary>
            /// The command reference.
            /// </summary>
            public ExampleCommand command;
            /// <summary>
            /// The Unity Game Object reference.
            /// </summary>
            public GameObject gameObject;
        }

        /// <summary>
        /// Represents a parameter in the autocomplete.
        /// </summary>
        public class ParameterAutocomplete
        {
            /// <summary>
            /// The parameter reference.
            /// </summary>
            public ExampleCommand.ParameterBase parameter;
            /// <summary>
            /// The Unity Game Object reference.
            /// </summary>
            public GameObject gameObject;
        }

        #endregion
    }
}
