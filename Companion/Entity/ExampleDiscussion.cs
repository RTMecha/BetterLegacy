using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Companion.Entity
{
    public class ExampleDiscussion : ExampleModule
    {
        public static ExampleDiscussion Default
        {
            get
            {
                var discussion = new ExampleDiscussion();
                discussion.InitDefault();
                return discussion;
            }
        }

        void InitDefault()
        {

        }

        public override void Build()
        {
            LoadCommands();
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

        public override void Tick()
        {
            if (autocomplete && chatter)
                autocomplete.SetActive(!string.IsNullOrEmpty(chatter.text));
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #region Commands

        public void HandleChatting()
        {
            if (chatter == null)
                return;

            reference?.brain?.Interact(ExampleInteractions.CHAT);

            for (int i = 0; i < commands.Count; i++)
                commands[i].CheckResponse(chatter.text);

            AchievementManager.inst.UnlockAchievement("example_chat");
        }

        void LoadCommands()
        {
            commands.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/commands.json"));

            for (int i = 0; i < jn["commands"].Count; i++)
                commands.Add(ExampleCommand.Parse(jn["commands"][i]));
        }

        public List<ExampleCommand> commands = new List<ExampleCommand>();

        public GameObject commandAutocompletePrefab;

        public GameObject autocomplete;
        public InputField chatter;
        public RectTransform chatterBase;
        public RectTransform autocompleteContent;

        public bool chatting = false;

        #endregion
    }
}
