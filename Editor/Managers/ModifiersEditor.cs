﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class ModifiersEditor : MonoBehaviour
    {
        public static ModifiersEditor inst;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public static void Init() => Creator.NewGameObject(nameof(ModifiersEditor), EditorManager.inst.transform.parent).AddComponent<ModifiersEditor>();

        void Awake()
        {
            inst = this;

            #region Prefabs

            modifierCardPrefab = Creator.NewUIObject("Modifier Prefab", transform);
            modifierCardPrefab.transform.AsRT().sizeDelta = new Vector2(336f, 128f);

            var mcpImage = modifierCardPrefab.AddComponent<Image>();
            mcpImage.color = new Color(1f, 1f, 1f, 0.03f);

            var mcpVLG = modifierCardPrefab.AddComponent<VerticalLayoutGroup>();
            mcpVLG.childControlHeight = false;
            mcpVLG.childControlWidth = false;
            mcpVLG.childForceExpandWidth = false;

            var mcpCSF = modifierCardPrefab.AddComponent<ContentSizeFitter>();
            mcpCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerTop = Creator.NewUIObject("Spacer Top", modifierCardPrefab.transform);
            mcpSpacerTop.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var mcpLabel = Creator.NewUIObject("Label", modifierCardPrefab.transform);
            RectValues.LeftAnchored.AnchoredPosition(0f, -8f).SizeDelta(352f, 32f).AssignToRectTransform(mcpLabel.transform.AsRT());

            var mcpText = Creator.NewUIObject("Text", mcpLabel.transform);
            RectValues.LeftAnchored.SizeDelta(300f, 32f).AssignToRectTransform(mcpText.transform.AsRT());

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mcpTextText.font = FontManager.inst.DefaultFont;
            mcpTextText.fontSize = 19;

            var collapse = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(mcpLabel.transform, "Collapse");
            collapse.transform.localScale = Vector3.one;
            var collapseLayoutElement = collapse.GetComponent<LayoutElement>() ?? collapse.AddComponent<LayoutElement>();
            collapseLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(70f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(collapse.transform.AsRT());

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Delete");
            delete.transform.localScale = Vector3.one;
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.AddComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(140f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());

            var copy = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Copy");
            copy.transform.localScale = Vector3.one;
            var duplicateLayoutElement = copy.GetComponent<LayoutElement>() ?? copy.AddComponent<LayoutElement>();
            duplicateLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(106f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(copy.transform.AsRT());

            copy.GetComponent<DeleteButtonStorage>().image.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"copy{FileFormat.PNG.Dot()}"));

            var notifier = Creator.NewUIObject("Notifier", mcpLabel.transform);
            var notifierImage = notifier.AddComponent<Image>();
            RectValues.Default.AnchoredPosition(44f, 0f).SizeDelta(8f, 32f).AssignToRectTransform(notifierImage.rectTransform);

            var mcpSpacerMid = Creator.NewUIObject("Spacer Middle", modifierCardPrefab.transform);
            mcpSpacerMid.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var layout = Creator.NewUIObject("Layout", modifierCardPrefab.transform);
            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();
            layoutVLG.childControlHeight = false;
            layoutVLG.childForceExpandHeight = false;
            layoutVLG.spacing = 4f;

            var layoutCSF = layout.AddComponent<ContentSizeFitter>();
            layoutCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerBot = Creator.NewUIObject("Spacer Bottom", modifierCardPrefab.transform);
            mcpSpacerBot.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();

            #endregion

            DefaultModifiersPopup = RTEditor.inst.GeneratePopup(EditorPopup.DEFAULT_MODIFIERS_POPUP, "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), _val =>
            {
                searchTerm = _val;
            }, placeholderText: "Search for default Modifier...");
        }

        public List<Modifier> GetCopiedModifiers(ModifierReferenceType referenceType) => copiedModifiers.TryGetValue(referenceType, out List<Modifier> list) ? list : null;

        public Dictionary<ModifierReferenceType, List<Modifier>> copiedModifiers = new Dictionary<ModifierReferenceType, List<Modifier>>()
        {
            { ModifierReferenceType.BeatmapObject, new List<Modifier>() },
            { ModifierReferenceType.BackgroundObject, new List<Modifier>() },
            { ModifierReferenceType.PrefabObject, new List<Modifier>() },
            { ModifierReferenceType.PAPlayer, new List<Modifier>() },
            { ModifierReferenceType.GameData, new List<Modifier>() },
        };

        #region Default Modifiers

        public ContentPopup DefaultModifiersPopup { get; set; }

        public void OpenDefaultModifiersList(ModifierReferenceType referenceType, IModifyable modifyable, int addIndex = -1, ModifiersEditorDialog dialog = null)
        {
            DefaultModifiersPopup.Open();
            RefreshDefaultModifiersList(referenceType, modifyable, addIndex, dialog);
        }

        public void RefreshDefaultModifiersList(ModifierReferenceType referenceType, IModifyable modifyable, int addIndex = -1, ModifiersEditorDialog dialog = null)
        {
            DefaultModifiersPopup.SearchField.onValueChanged.NewListener(_val =>
            {
                searchTerm = _val;
                RefreshDefaultModifiersList(referenceType, modifyable, addIndex, dialog);
            });

            int shape = modifyable is IShapeable shapeable ? shapeable.Shape : 0;

            DefaultModifiersPopup.ClearContent();

            var modifiersEditorDialog = dialog;

            foreach (var defaultModifier in ModifiersManager.inst.modifiers)
            {
                if (!SearchModifier(searchTerm, defaultModifier) || !defaultModifier.compatibility.CompareType(referenceType) || defaultModifier.hideInEditor)
                    continue;

                var name = $"{defaultModifier.Name} ({defaultModifier.type})";

                var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(DefaultModifiersPopup.Content, name);
                var spriteFunctionButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();

                TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {name}");

                spriteFunctionButton.label.text = name;
                spriteFunctionButton.button.onClick.NewListener(() =>
                {
                    var name = defaultModifier.Name;

                    if (name.Contains("Text") && !name.Contains("Other") && shape != 4)
                    {
                        EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be a Text Object.", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    if (name.Contains("Image") && !name.Contains("Other") && shape != 6)
                    {
                        EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be an Image Object.", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var modifier = defaultModifier.Copy();
                    if (addIndex == -1)
                        modifyable.Modifiers.Add(modifier);
                    else
                        modifyable.Modifiers.Insert(Mathf.Clamp(addIndex, 0, modifyable.Modifiers.Count), modifier);

                    CoroutineHelper.StartCoroutine(modifiersEditorDialog.RenderModifiers(modifyable));
                    DefaultModifiersPopup.Close();
                    switch (referenceType)
                    {
                        case ModifierReferenceType.BeatmapObject: {
                                RTLevel.Current?.UpdateObject(modifyable as BeatmapObject, ObjectContext.MODIFIERS);
                                break;
                            }
                        case ModifierReferenceType.BackgroundObject: {
                                RTLevel.Current?.UpdateBackgroundObject(modifyable as BackgroundObject, BackgroundObjectContext.MODIFIERS);
                                break;
                            }
                        case ModifierReferenceType.PrefabObject: {
                                RTLevel.Current?.UpdatePrefab(modifyable as PrefabObject, PrefabObjectContext.MODIFIERS);
                                break;
                            }
                    }
                });
                spriteFunctionButton.image.sprite = GetSprite(defaultModifier);

                EditorThemeManager.ApplyLightText(spriteFunctionButton.label);
                EditorThemeManager.ApplySelectable(spriteFunctionButton.button, ThemeGroup.List_Button_1);
            }
        }

        Sprite GetSprite(Modifier modifier) => modifier.Name.StartsWith("get") ? EditorSprites.DownArrow : modifier.type == Modifier.Type.Trigger ? EditorSprites.QuestionSprite : EditorSprites.ExclaimSprite;

        public string searchTerm;

        public bool SearchModifier(string searchTerm, Modifier modifier) =>
            string.IsNullOrEmpty(searchTerm) ||
            RTString.SearchString(searchTerm, modifier.Name) ||
            searchTerm.ToLower() == "action" && modifier.type == Modifier.Type.Action ||
            searchTerm.ToLower() == "trigger" && modifier.type == Modifier.Type.Trigger;

        #endregion

        #region UI Part Handlers

        public void PasteGenerator(IModifyable modifyable, ModifiersEditorDialog dialog)
        {
            var referenceType = modifyable.ReferenceType;

            if (referenceType == ModifierReferenceType.Null)
            {
                EditorManager.inst.DisplayNotification($"Incompatible modifier.", 3f, EditorManager.NotificationType.Error);
                return;
            }

            var copiedModifiers = GetCopiedModifiers(referenceType);
            if (copiedModifiers == null || copiedModifiers.IsEmpty())
                return;

            var pasteModifier = dialog.pasteModifier;

            if (pasteModifier)
                CoreHelper.Destroy(pasteModifier);

            var content = dialog.Content;

            pasteModifier = EditorPrefabHolder.Instance.Function1Button.Duplicate(content, "paste modifier");
            pasteModifier.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var buttonStorage = pasteModifier.GetComponent<FunctionButtonStorage>();
            buttonStorage.label.text = "Paste";
            buttonStorage.button.onClick.NewListener(() =>
            {
                modifyable.Modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));

                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                if (modifyable is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                if (modifyable is BackgroundObject backgroundObject)
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, ObjectContext.MODIFIERS);

                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
            });

            TooltipHelper.AssignTooltip(pasteModifier, "Paste Modifier");
            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Paste_Text);

            dialog.pasteModifier = pasteModifier;
        }

        public GameObject booleanBar;

        public GameObject numberInput;

        public GameObject stringInput;

        public GameObject dropdownBar;

        GameObject Base(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(transform);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;

            var text = new GameObject("Text");
            text.transform.SetParent(rectTransform);
            text.transform.localScale = Vector3.one;
            var textRT = text.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(10f, -5f);
            textRT.anchorMax = Vector2.one;
            textRT.anchorMin = Vector2.zero;
            textRT.pivot = new Vector2(0f, 1f);
            textRT.sizeDelta = new Vector2(296f, 32f);

            var textText = text.AddComponent<Text>();
            textText.alignment = TextAnchor.MiddleLeft;
            textText.font = FontManager.inst.DefaultFont;
            textText.fontSize = 19;
            textText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            return gameObject;
        }

        GameObject Boolean()
        {
            var gameObject = Base("Bool");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(266f, 32f);

            var toggleBase = new GameObject("Toggle");
            toggleBase.transform.SetParent(rectTransform);
            toggleBase.transform.localScale = Vector3.one;

            var toggleBaseRT = toggleBase.AddComponent<RectTransform>();

            toggleBaseRT.anchorMax = Vector2.one;
            toggleBaseRT.anchorMin = Vector2.zero;
            toggleBaseRT.sizeDelta = new Vector2(32f, 32f);

            var toggle = toggleBase.AddComponent<Toggle>();

            var background = new GameObject("Background");
            background.transform.SetParent(toggleBaseRT);
            background.transform.localScale = Vector3.one;

            var backgroundRT = background.AddComponent<RectTransform>();
            backgroundRT.anchoredPosition = Vector3.zero;
            backgroundRT.anchorMax = new Vector2(0f, 1f);
            backgroundRT.anchorMin = new Vector2(0f, 1f);
            backgroundRT.pivot = new Vector2(0f, 1f);
            backgroundRT.sizeDelta = new Vector2(32f, 32f);
            var backgroundImage = background.AddComponent<Image>();

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(backgroundRT);
            checkmark.transform.localScale = Vector3.one;

            var checkmarkRT = checkmark.AddComponent<RectTransform>();
            checkmarkRT.anchoredPosition = Vector3.zero;
            checkmarkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRT.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRT.sizeDelta = new Vector2(20f, 20f);
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_checkmark.png");
            checkmarkImage.color = new Color(0.1294f, 0.1294f, 0.1294f);

            toggle.image = backgroundImage;
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return gameObject;
        }

        GameObject NumberInput()
        {
            var gameObject = Base("Number");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            var buttonL = Button("<", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_left_small.png"));
            buttonL.transform.SetParent(rectTransform);
            buttonL.transform.localScale = Vector3.one;

            ((RectTransform)buttonL.transform).sizeDelta = new Vector2(16f, 32f);

            var buttonR = Button(">", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_right_small.png"));
            buttonR.transform.SetParent(rectTransform);
            buttonR.transform.localScale = Vector3.one;

            ((RectTransform)buttonR.transform).sizeDelta = new Vector2(16f, 32f);

            return gameObject;
        }

        GameObject StringInput()
        {
            var gameObject = Base("String");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            input.transform.AsRT().anchoredPosition = new Vector2(210f, -16f);
            input.transform.AsRT().sizeDelta = new Vector2(120, 32f);
            input.transform.Find("Text").AsRT().sizeDelta = Vector2.zero;

            return gameObject;
        }

        GameObject Dropdown()
        {
            var gameObject = Base("Dropdown");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var dropdownInput = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(rectTransform, "Dropdown");
            dropdownInput.transform.localScale = Vector2.one;

            return gameObject;
        }

        GameObject Button(string name, Sprite sprite)
        {
            var gameObject = new GameObject(name);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.localScale = Vector2.one;

            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8784f, 0.8784f, 0.8784f);
            image.sprite = sprite;

            var button = gameObject.AddComponent<Button>();
            button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.898f, 0.451f, 0.451f, 1f), Color.white, Color.white, Color.red);

            return gameObject;
        }

        #endregion
    }
}
