using System;
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
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class ModifiersEditor : MonoBehaviour
    {
        public static ModifiersEditor inst;

        public Transform content;
        public Transform scrollView;
        public Scrollbar scrollbar;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public static void Init() => Creator.NewGameObject(nameof(ModifiersEditor), EditorManager.inst.transform.parent).AddComponent<ModifiersEditor>();

        void Awake()
        {
            inst = this;

            CreateModifiersOnAwake();
            DefaultModifiersPopup = RTEditor.inst.GeneratePopup(EditorPopup.DEFAULT_MODIFIERS_POPUP, "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), _val =>
            {
                searchTerm = _val;
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    RefreshDefaultModifiersList(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), addIndex);
            }, placeholderText: "Search for default Modifier...");
        }

        float time;
        float timeOffset;
        bool setTime;

        void Update()
        {
            if (!setTime)
            {
                timeOffset = Time.time;
                setTime = true;
            }

            time = timeOffset - Time.time;
            timeOffset = Time.time;

            try
            {
                if (RTEditor.ShowModdedUI && EditorTimeline.inst.SelectedObjectCount == 1 && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    intVariable.text = $"Integer Variable: [ {EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().integerVariable} ]";
            }
            catch
            {

            }
        }

        public Text modifiersLabel;

        public Text intVariable;

        public Toggle activeToggle;

        public Toggle ignoreToggle;

        public Toggle orderToggle;

        public bool renderingModifiers;

        public void CreateModifiersOnAwake()
        {
            // Label
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.ObjectView.transform, "label");

                modifiersLabel = label.transform.GetChild(0).GetComponent<Text>();
                modifiersLabel.text = "Modifiers";
                EditorThemeManager.AddLightText(modifiersLabel);
            }

            // Integer variable
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.ObjectView.transform, "int_variable");

                intVariable = label.transform.GetChild(0).GetComponent<Text>();
                intVariable.text = "Integer Variable: [ null ]";
                intVariable.fontSize = 18;
                EditorThemeManager.AddLightText(intVariable);
                label.AddComponent<Image>().color = LSColors.transparent;
                TooltipHelper.AssignTooltip(label, "Modifiers Integer Variable");
            }

            // Ignored Lifespan
            {
                var ignoreLifespan = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "ignore life");
                var ignoreLifespanToggleButton = ignoreLifespan.GetComponent<ToggleButtonStorage>();
                ignoreLifespanToggleButton.label.text = "Ignore Lifespan";

                ignoreToggle = ignoreLifespanToggleButton.toggle;

                EditorThemeManager.AddToggle(ignoreToggle, graphic: ignoreLifespanToggleButton.label);
                TooltipHelper.AssignTooltip(ignoreLifespan, "Modifiers Ignore Lifespan");
            }

            // Order Modifiers
            {
                var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "order modifiers");
                var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
                orderMattersToggleButton.label.text = "Order Matters";

                orderToggle = orderMattersToggleButton.toggle;

                EditorThemeManager.AddToggle(orderToggle, graphic: orderMattersToggleButton.label);
                TooltipHelper.AssignTooltip(orderMatters, "Modifiers Order Matters");
            }

            // Active
            {
                var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "active");
                var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
                showModifiersToggleButton.label.text = "Show Modifiers";

                activeToggle = showModifiersToggleButton.toggle;

                EditorThemeManager.AddToggle(activeToggle, graphic: showModifiersToggleButton.label);
                TooltipHelper.AssignTooltip(showModifiers, "Show Modifiers");
            }

            var scrollObj = EditorPrefabHolder.Instance.ScrollView.Duplicate(ObjEditor.inst.ObjectView.transform, "Modifiers Scroll View");

            scrollView = scrollObj.transform;

            scrollView.localScale = Vector3.one;

            content = scrollView.Find("Viewport/Content");

            scrollView.gameObject.SetActive(showModifiers);
            try
            {
                scrollbar = scrollView.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

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
        }

        public List<ModifierBase> GetCopiedModifiers(ModifierReferenceType referenceType) => referenceType switch
        {
            ModifierReferenceType.BeatmapObject => copiedBeatmapObjectModifiers,
            ModifierReferenceType.BackgroundObject => copiedBackgroundObjectModifiers,
            ModifierReferenceType.CustomPlayer => copiedPlayerModifiers,
            ModifierReferenceType.GameData => copiedLevelModifiers,
            _ => null,
        };

        public List<ModifierBase> copiedBeatmapObjectModifiers = new List<ModifierBase>();

        public List<ModifierBase> copiedBackgroundObjectModifiers = new List<ModifierBase>();

        public List<ModifierBase> copiedPlayerModifiers = new List<ModifierBase>();

        public List<ModifierBase> copiedLevelModifiers = new List<ModifierBase>();

        public IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            modifiersLabel.gameObject.SetActive(RTEditor.ShowModdedUI);
            intVariable.gameObject.SetActive(RTEditor.ShowModdedUI);
            ignoreToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            orderToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                showModifiers = false;

            activeToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            activeToggle.onValueChanged.ClearAll();
            activeToggle.isOn = showModifiers;
            activeToggle.onValueChanged.AddListener(_val =>
            {
                showModifiers = _val;
                scrollView.gameObject.SetActive(showModifiers);
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    ObjectEditor.inst.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            });

            if (!RTEditor.ShowModdedUI)
                yield break;

            ignoreToggle.onValueChanged.ClearAll();
            ignoreToggle.isOn = beatmapObject.ignoreLifespan;
            ignoreToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.ignoreLifespan = _val;
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
            });

            orderToggle.onValueChanged.ClearAll();
            orderToggle.isOn = beatmapObject.orderModifiers;
            orderToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.orderModifiers = _val;
                RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
            });

            if (!showModifiers)
                yield break;

            renderingModifiers = true;

            var value = scrollbar ? scrollbar.value : 0f;

            LSHelpers.DeleteChildren(content);
            modifierCards.Clear();

            content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 500f);

            int num = 0;
            foreach (var modifier in beatmapObject.modifiers)
            {
                int index = num;
                RenderModifier(modifier, index);
                num++;
            }

            // Add Modifier
            {
                var gameObject = modifierAddPrefab.Duplicate(content, "add modifier");
                TooltipHelper.AssignTooltip(gameObject, "Add Modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    DefaultModifiersPopup.Open();
                    RefreshDefaultModifiersList(beatmapObject);
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            PasteGenerator(beatmapObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());

            CoroutineHelper.PerformAtNextFrame(() =>
            {
                if (scrollbar)
                    scrollbar.value = value;
            });

            yield break;
        }

        public void SetObjectColors<T>(Toggle[] toggles, int index, int currentValue, Modifier<T> modifier, List<Color> colors)
        {
            int num = 0;
            foreach (var toggle in toggles)
            {
                int toggleIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == currentValue;
                toggle.onValueChanged.AddListener(_val =>
                {
                    modifier.SetValue(index, toggleIndex.ToString());

                    try
                    {
                        modifier.Inactive?.Invoke(modifier, null);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    }
                    modifier.active = false;

                    SetObjectColors(toggles, index, toggleIndex, modifier, colors);
                });

                toggle.GetComponent<Image>().color = colors.GetAt(toggleIndex);

                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
                num++;
            }
        }

        // temporary solution
        public List<ModifierCard> modifierCards = new List<ModifierCard>();

        public void RenderModifier<T>(Modifier<T> modifier, int index)
        {
            if (modifier.reference is not IModifyable<T> modifyable)
                return;

            var name = modifier.Name;
            var isBeatmapObject = modifier.reference is BeatmapObject;
            var content = isBeatmapObject ? this.content : RTBackgroundEditor.inst.content;
            var modifierCards = isBeatmapObject ? this.modifierCards : RTBackgroundEditor.inst.modifierCards;

            var gameObject = modifierCardPrefab.Duplicate(content, name);
            var modifierCard = modifierCards.InRange(index) ? modifierCards[index] : new ModifierCard(gameObject, modifier, index);
            if (!modifierCards.InRange(index))
                modifierCards.Add(modifierCard);
            else if (!modifierCard.GameObject)
                modifierCard.GameObject = gameObject;
            else
            {
                CoreHelper.Delete(modifierCard.GameObject);
                gameObject.transform.SetSiblingIndex(modifierCard.index);
                modifierCard.GameObject = gameObject;
            }

            TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {(name + " (" + modifier.type.ToString() + ")")}");
            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

            gameObject.transform.localScale = Vector3.one;
            var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
            modifierTitle.text = name;
            EditorThemeManager.ApplyLightText(modifierTitle);

            var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
            collapse.onValueChanged.ClearAll();
            collapse.isOn = modifier.collapse;
            collapse.onValueChanged.AddListener(_val =>
            {
                modifier.collapse = _val;
                RenderModifier(modifier, modifierCard.index);
                CoroutineHelper.PerformAtEndOfFrame(() =>
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());
                });
            });

            TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");
            EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

            for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                modifyable.Modifiers.RemoveAt(modifierCard.index);
                CoreHelper.Delete(gameObject);
                modifierCards.RemoveAt(modifierCard.index);
                for (int i = 0; i < modifierCards.Count; i++)
                    modifierCards[i].index = i;

                if (modifier.reference is BeatmapObject beatmapObject)
                {
                    beatmapObject.reactivePositionOffset = Vector3.zero;
                    beatmapObject.reactiveScaleOffset = Vector3.zero;
                    beatmapObject.reactiveRotationOffset = 0f;
                    RTLevel.Current?.UpdateObject(beatmapObject);
                }
                if (modifier.reference is BackgroundObject backgroundObject)
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });

            TooltipHelper.AssignTooltip(delete.gameObject, "Delete Modifier");
            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
            copy.button.onClick.NewListener(() =>
            {
                var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                copiedModifiers.Clear();
                copiedModifiers.Add(modifier.Copy(default));

                PasteGenerator(modifyable);
                EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
            });

            TooltipHelper.AssignTooltip(copy.gameObject, "Copy Modifier");
            EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

            var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
            notifier.modifierBase = modifier;
            notifier.notifier = gameObject.transform.Find("Label/Notifier").gameObject.GetComponent<Image>();
            TooltipHelper.AssignTooltip(notifier.notifier.gameObject, "Notifier Modifier");
            EditorThemeManager.ApplyGraphic(notifier.notifier, ThemeGroup.Warning_Confirm, true);

            gameObject.AddComponent<Button>();
            var modifierContextMenu = gameObject.AddComponent<ContextClickable>();
            modifierContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                var buttonFunctions = new List<ButtonFunction>()
                {
                    new ButtonFunction("Add", () =>
                    {
                        if (modifier.reference is BeatmapObject beatmapObject)
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(beatmapObject);
                        }
                        if (modifier.reference is BackgroundObject backgroundObject)
                        {
                            RTBackgroundEditor.inst.DefaultModifiersPopup.Open();
                            RTBackgroundEditor.inst.RefreshDefaultModifiersList(backgroundObject);
                        }
                    }),
                    new ButtonFunction("Add Above", () =>
                    {
                        if (modifier.reference is BeatmapObject beatmapObject)
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(beatmapObject, modifierCard.index);
                        }
                        if (modifier.reference is BackgroundObject backgroundObject)
                        {
                            RTBackgroundEditor.inst.DefaultModifiersPopup.Open();
                            RTBackgroundEditor.inst.RefreshDefaultModifiersList(backgroundObject, modifierCard.index);
                        }
                    }),
                    new ButtonFunction("Add Below", () =>
                    {
                        if (modifier.reference is BeatmapObject beatmapObject)
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(beatmapObject, modifierCard.index + 1);
                        }
                        if (modifier.reference is BackgroundObject backgroundObject)
                        {
                            RTBackgroundEditor.inst.DefaultModifiersPopup.Open();
                            RTBackgroundEditor.inst.RefreshDefaultModifiersList(backgroundObject, modifierCard.index + 1);
                        }
                    }),
                    new ButtonFunction("Delete", () =>
                    {
                        modifyable.Modifiers.RemoveAt(modifierCard.index);
                        CoreHelper.Delete(gameObject);
                        modifierCards.RemoveAt(modifierCard.index);
                        for (int i = 0; i < modifierCards.Count; i++)
                            modifierCards[i].index = i;

                        if (modifier.reference is BeatmapObject beatmapObject)
                        {
                            beatmapObject.reactivePositionOffset = Vector3.zero;
                            beatmapObject.reactiveScaleOffset = Vector3.zero;
                            beatmapObject.reactiveRotationOffset = 0f;
                            RTLevel.Current?.UpdateObject(beatmapObject);
                        }
                        if (modifier.reference is BackgroundObject backgroundObject)
                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy", () =>
                    {
                        var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                        copiedModifiers.Clear();
                        copiedModifiers.Add(modifier.Copy(default));

                        PasteGenerator(modifyable);
                        EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Copy All", () =>
                    {
                        var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                        copiedModifiers.Clear();
                        copiedModifiers.AddRange(modifyable.Modifiers.Select(x => x.Copy(default)));

                        PasteGenerator(modifyable);
                        EditorManager.inst.DisplayNotification("Copied Modifiers!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste", () =>
                    {
                        var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                        if (copiedModifiers == null || copiedModifiers.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.AddRange(copiedModifiers.Select(x => (x as Modifier<T>).Copy((T)modifyable)));

                        if (modifyable is BeatmapObject beatmapObject)
                        {
                            StartCoroutine(RenderModifiers(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
                        }
                        if (modifyable is BackgroundObject backgroundObject)
                        {
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.ObjectContext.MODIFIERS);
                        }

                        EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste Above", () =>
                    {
                        var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                        if (copiedModifiers == null || copiedModifiers.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.InsertRange(modifierCard.index, copiedModifiers.Select(x => (x as Modifier<T>).Copy((T)modifyable)));

                        if (modifyable is BeatmapObject beatmapObject)
                        {
                            StartCoroutine(RenderModifiers(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
                        }
                        if (modifyable is BackgroundObject backgroundObject)
                        {
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.ObjectContext.MODIFIERS);
                        }

                        EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste Below", () =>
                    {
                        var copiedModifiers = GetCopiedModifiers(modifier.referenceType);
                        if (copiedModifiers == null || copiedModifiers.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.InsertRange(modifierCard.index + 1, copiedModifiers.Select(x => (x as Modifier<T>).Copy((T)modifyable)));

                        if (modifyable is BeatmapObject beatmapObject)
                        {
                            StartCoroutine(RenderModifiers(beatmapObject));
                            RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
                        }
                        if (modifyable is BackgroundObject backgroundObject)
                        {
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.ObjectContext.MODIFIERS);
                        }

                        EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction(true),
                };

                if (!modifyable.OrderModifiers)
                {
                    buttonFunctions.Add(new ButtonFunction("Sort Modifiers", () =>
                    {
                        modifyable.Modifiers = modifyable.Modifiers.OrderBy(x => x.type == ModifierBase.Type.Action).ToList();

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }));
                }
                buttonFunctions.AddRange(new List<ButtonFunction>()
                {
                    new ButtonFunction("Move Up", () =>
                    {
                        if (index <= 0)
                        {
                            EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.Move(index, index - 1);

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }),
                    new ButtonFunction("Move Down", () =>
                    {
                        if (index >= modifyable.Modifiers.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.Move(index, index + 1);

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }),
                    new ButtonFunction("Move to Start", () =>
                    {
                        modifyable.Modifiers.Move(index, 0);

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }),
                    new ButtonFunction("Move to End", () =>
                    {
                        modifyable.Modifiers.Move(index, modifyable.Modifiers.Count - 1);

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Update Modifier", () =>
                    {
                        modifier.active = false;
                        modifier.runCount = 0;
                        modifier.Inactive?.Invoke(modifier, null);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Collapse", () =>
                    {
                        modifier.collapse = true;
                        RenderModifier(modifier, modifierCard.index);
                        CoroutineHelper.PerformAtEndOfFrame(() =>
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());
                        });
                    }),
                    new ButtonFunction("Unollapse", () =>
                    {
                        modifier.collapse = false;
                        RenderModifier(modifier, modifierCard.index);
                        CoroutineHelper.PerformAtEndOfFrame(() =>
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());
                        });
                    }),
                    new ButtonFunction("Collapse All", () =>
                    {
                        foreach (var mod in modifyable.Modifiers)
                            mod.collapse = true;

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    }),
                    new ButtonFunction("Uncollapse All", () =>
                    {
                        foreach (var mod in modifyable.Modifiers)
                            mod.collapse = false;

                        if (modifier.reference is BeatmapObject beatmapObject)
                            StartCoroutine(RenderModifiers(beatmapObject));
                        if (modifier.reference is BackgroundObject backgroundObject)
                            StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    })
                });
                if (ModCompatibility.UnityExplorerInstalled)
                    buttonFunctions.Add(new ButtonFunction("Inspect", () => ModCompatibility.Inspect(modifier)));

                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
            };

            if (modifier.collapse)
                return;

            var layout = gameObject.transform.Find("Layout");

            var constant = booleanBar.Duplicate(layout, "Constant");
            constant.transform.localScale = Vector3.one;

            var constantText = constant.transform.Find("Text").GetComponent<Text>();
            constantText.text = "Constant";

            var constantToggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
            constantToggle.onValueChanged.ClearAll();
            constantToggle.isOn = modifier.constant;
            constantToggle.onValueChanged.AddListener(_val =>
            {
                modifier.constant = _val;
                modifier.active = false;
            });

            TooltipHelper.AssignTooltip(constantToggle.gameObject, "Constant Modifier");
            EditorThemeManager.ApplyLightText(constantText);
            EditorThemeManager.ApplyToggle(constantToggle);

            var count = NumberGenerator(layout, "Run Count", modifier.triggerCount.ToString(), _val =>
            {
                if (int.TryParse(_val, out int num))
                    modifier.triggerCount = Mathf.Clamp(num, 0, int.MaxValue);

                modifier.runCount = 0;

                try
                {
                    modifier.Inactive?.Invoke(modifier, null);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            }, out InputField countField);

            TooltipHelper.AssignTooltip(countField.gameObject, "Run Count Modifier");
            TriggerHelper.IncreaseDecreaseButtonsInt(countField, 1, 0, int.MaxValue, count.transform);
            TriggerHelper.AddEventTriggers(countField.gameObject, TriggerHelper.ScrollDeltaInt(countField, 1, 0, int.MaxValue));

            if (modifier.type == ModifierBase.Type.Trigger)
            {
                var not = booleanBar.Duplicate(layout, "Not");
                not.transform.localScale = Vector3.one;
                var notText = not.transform.Find("Text").GetComponent<Text>();
                notText.text = "Not";

                var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                notToggle.onValueChanged.ClearAll();
                notToggle.isOn = modifier.not;
                notToggle.onValueChanged.AddListener(_val =>
                {
                    modifier.not = _val;
                    modifier.active = false;
                });

                TooltipHelper.AssignTooltip(notToggle.gameObject, "Trigger Not Modifier");
                EditorThemeManager.ApplyLightText(notText);
                EditorThemeManager.ApplyToggle(notToggle);

                var elseIf = booleanBar.Duplicate(layout, "Not");
                elseIf.transform.localScale = Vector3.one;
                var elseIfText = elseIf.transform.Find("Text").GetComponent<Text>();
                elseIfText.text = "Else If";

                var elseIfToggle = elseIf.transform.Find("Toggle").GetComponent<Toggle>();
                elseIfToggle.onValueChanged.ClearAll();
                elseIfToggle.isOn = modifier.elseIf;
                elseIfToggle.onValueChanged.AddListener(_val =>
                {
                    modifier.elseIf = _val;
                    modifier.active = false;
                });

                TooltipHelper.AssignTooltip(elseIfToggle.gameObject, "Trigger Else If Modifier");
                EditorThemeManager.ApplyLightText(elseIfText);
                EditorThemeManager.ApplyToggle(elseIfToggle);
            }

            if (!modifier.verified)
            {
                modifier.verified = true;
                if (!name.Contains("DEVONLY"))
                {
                    if (modifier.referenceType == ModifierReferenceType.BeatmapObject)
                        (modifier as Modifier<BeatmapObject>).VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
                    if (modifier.referenceType == ModifierReferenceType.BackgroundObject)
                        (modifier as Modifier<BackgroundObject>).VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var cmd = modifier.Name;
            switch (cmd)
            {
                case "setActive": {
                        BoolGenerator(modifier, layout, "Active", 0, false);

                        break;
                    }
                case "setActiveOther": {
                        BoolGenerator(modifier, layout, "Active", 0, false);
                        StringGenerator(modifier, layout, "BG Group", 1);

                        break;
                    }
                case "timeLesserEquals":
                case "timeGreaterEquals":
                case "timeLesser":
                case "timeGreater": {
                        SingleGenerator(modifier, layout, "Time", 0, 0f);

                        break;
                    }

                #region Actions

                #region Audio

                case nameof(ModifierActions.setPitch): {
                        SingleGenerator(modifier, layout, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.addPitch): {
                        SingleGenerator(modifier, layout, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setPitchMath): {
                        StringGenerator(modifier, layout, "Pitch", 0);

                        break;
                    }
                case nameof(ModifierActions.addPitchMath): {
                        StringGenerator(modifier, layout, "Pitch", 0);

                        break;
                    }

                case nameof(ModifierActions.setMusicTime): {
                        SingleGenerator(modifier, layout, "Time", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setMusicTimeMath): {
                        StringGenerator(modifier, layout, "Time", 0);

                        break;
                    }
                //case "setMusicTimeStartTime): {
                //        break;
                //    }
                //case "setMusicTimeAutokill): {
                //        break;
                //    }
                case nameof(ModifierActions.setMusicPlaying): {
                        BoolGenerator(modifier, layout, "Playing", 0, false);

                        break;
                    }

                case nameof(ModifierActions.playSound): {
                        var str = StringGenerator(modifier, layout, "Path", 0);
                        var search = str.transform.Find("Input").gameObject.AddComponent<ContextClickable>();
                        search.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Use Local Browser", () =>
                                {
                                    var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                    var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                                    if (string.IsNullOrEmpty(result))
                                        return;

                                    var global = Parser.TryParse(modifier.commands[1], false);

                                    result = RTFile.ReplaceSlash(result);
                                    if (result.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                    {
                                        str.transform.Find("Input").GetComponent<InputField>().text =
                                            result.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                        RTEditor.inst.BrowserPopup.Close();
                                        return;
                                    }

                                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                }),
                                new ButtonFunction("Use In-game Browser", () =>
                                {
                                    RTEditor.inst.BrowserPopup.Open();

                                    var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                    var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                                    {
                                        var global = Parser.TryParse(modifier.commands[1], false);
                                        _val = RTFile.ReplaceSlash(_val);
                                        if (_val.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                            RTEditor.inst.BrowserPopup.Close();
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    });
                                })
                                );
                        };
                        BoolGenerator(modifier, layout, "Global", 1, false);
                        SingleGenerator(modifier, layout, "Pitch", 2, 1f);
                        SingleGenerator(modifier, layout, "Volume", 3, 1f);
                        BoolGenerator(modifier, layout, "Loop", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playSoundOnline): {
                        StringGenerator(modifier, layout, "URL", 0);
                        SingleGenerator(modifier, layout, "Pitch", 1, 1f);
                        SingleGenerator(modifier, layout, "Volume", 2, 1f);
                        BoolGenerator(modifier, layout, "Loop", 3, false);
                        break;
                    }
                case nameof(ModifierActions.playDefaultSound): {
                        var dd = dropdownBar.Duplicate(layout, "Sound");
                        dd.transform.localScale = Vector3.one;
                        var labelText = dd.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Sound";

                        Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                        var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                        d.onValueChanged.ClearAll();
                        d.options.Clear();
                        var sounds = Enum.GetNames(typeof(DefaultSounds));
                        d.options = CoreHelper.StringToOptionData(sounds);

                        int soundIndex = -1;
                        for (int i = 0; i < sounds.Length; i++)
                        {
                            if (sounds[i] == modifier.value)
                            {
                                soundIndex = i;
                                break;
                            }
                        }

                        if (soundIndex >= 0)
                            d.value = soundIndex;

                        d.onValueChanged.AddListener(_val =>
                        {
                            modifier.value = sounds[_val];
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyDropdown(d);

                        SingleGenerator(modifier, layout, "Pitch", 1, 1f);
                        SingleGenerator(modifier, layout, "Volume", 2, 1f);
                        BoolGenerator(modifier, layout, "Loop", 3, false);

                        break;
                    }
                case nameof(ModifierActions.audioSource): {
                        var str = StringGenerator(modifier, layout, "Path", 0);
                        var search = str.transform.Find("Input").gameObject.AddComponent<Clickable>();
                        search.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Use Local Browser", () =>
                                {
                                    var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                    var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                                    if (string.IsNullOrEmpty(result))
                                        return;

                                    var global = Parser.TryParse(modifier.commands[1], false);
                                    result = RTFile.ReplaceSlash(result);
                                    if (result.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                    {
                                        str.transform.Find("Input").GetComponent<InputField>().text = result.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                        RTEditor.inst.BrowserPopup.Close();
                                        return;
                                    }

                                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                }),
                                new ButtonFunction("Use In-game Browser", () =>
                                {
                                    RTEditor.inst.BrowserPopup.Open();

                                    var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                    var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                                    {
                                        var global = Parser.TryParse(modifier.commands[1], false);
                                        _val = RTFile.ReplaceSlash(_val);
                                        if (_val.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                            RTEditor.inst.BrowserPopup.Close();
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    });
                                })
                                );
                        };
                        BoolGenerator(modifier, layout, "Global", 1, false);

                        break;
                    }

                #endregion

                #region Level

                case nameof(ModifierActions.loadLevel): {
                        StringGenerator(modifier, layout, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.loadLevelID): {
                        StringGenerator(modifier, layout, "ID", 0);

                        break;
                    }
                case nameof(ModifierActions.loadLevelInternal): {
                        StringGenerator(modifier, layout, "Inner Path", 0);

                        break;
                    }
                //case "loadLevelPrevious): {
                //        break;
                //    }
                //case "loadLevelHub): {
                //        break;
                //    }
                case nameof(ModifierActions.loadLevelInCollection): {
                        StringGenerator(modifier, layout, "ID", 0);

                        break;
                    }
                case nameof(ModifierActions.downloadLevel): {
                        StringGenerator(modifier, layout, "Arcade ID", 0);
                        StringGenerator(modifier, layout, "Server ID", 1);
                        StringGenerator(modifier, layout, "Workshop ID", 2);
                        StringGenerator(modifier, layout, "Song Title", 3);
                        StringGenerator(modifier, layout, "Level Name", 4);
                        BoolGenerator(modifier, layout, "Play Level", 5, true);

                        break;
                    }
                case nameof(ModifierActions.endLevel): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, layout, "End Level Function", 0, options);
                        StringGenerator(modifier, layout, "End Level Data", 1);
                        BoolGenerator(modifier, layout, "Save Player Data", 2, true);

                        break;
                    }
                case nameof(ModifierActions.setAudioTransition): {
                        SingleGenerator(modifier, layout, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setIntroFade): {
                        BoolGenerator(modifier, layout, "Should Fade", 0, true);

                        break;
                    }
                case nameof(ModifierActions.setLevelEndFunc): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, layout, "End Level Function", 0, options);
                        StringGenerator(modifier, layout, "End Level Data", 1);
                        BoolGenerator(modifier, layout, "Save Player Data", 2, true);

                        break;
                    }

                #endregion

                #region Component

                case nameof(ModifierActions.blur): {
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, layout, "Use Opacity", 1, false);
                        BoolGenerator(modifier, layout, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurOther): {
                        PrefabGroupOnly(modifier, layout);
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurVariable): {
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, layout, "Set Back to Normal", 1, false);

                        break;
                    }
                case nameof(ModifierActions.blurVariableOther): {
                        PrefabGroupOnly(modifier, layout);
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurColored): {
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, layout, "Use Opacity", 1, false);
                        BoolGenerator(modifier, layout, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurColoredOther): {
                        PrefabGroupOnly(modifier, layout);
                        SingleGenerator(modifier, layout, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Set Back to Normal", 2, false);

                        break;
                    }
                //case "doubleSided): {
                //        break;
                //    }
                case nameof(ModifierActions.particleSystem): {
                        SingleGenerator(modifier, layout, "Life Time", 0, 5f);

                        // Shape
                        {
                            var dd = dropdownBar.Duplicate(layout, "Shape");
                            dd.transform.localScale = Vector3.one;
                            var labelText = dd.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Shape";

                            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                            Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                            d.onValueChanged.ClearAll();
                            d.options = CoreHelper.StringToOptionData("Square", "Circle", "Triangle", "Arrow", "Text", "Hexagon", "Image", "Pentagon", "Misc");

                            d.value = Parser.TryParse(modifier.commands[1], 0);

                            d.onValueChanged.AddListener(_val =>
                            {
                                if (_val == 4 || _val == 6)
                                {
                                    EditorManager.inst.DisplayNotification("Shape type not available for particle system.", 1.5f, EditorManager.NotificationType.Warning);
                                    d.value = Parser.TryParse(modifier.commands[1], 0);
                                    return;
                                }

                                modifier.commands[1] = Mathf.Clamp(_val, 0, ShapeManager.inst.Shapes2D.Count - 1).ToString();
                                modifier.active = false;

                                RenderModifier(modifier, modifierCard.index);
                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyDropdown(d);

                            TriggerHelper.AddEventTriggers(d.gameObject, TriggerHelper.ScrollDelta(d));
                        }

                        // Shape Option
                        {
                            var dd = dropdownBar.Duplicate(layout, "Shape");
                            dd.transform.localScale = Vector3.one;
                            var labelText = dd.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Shape";

                            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                            Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                            d.onValueChanged.ClearAll();
                            d.options.Clear();

                            var type = Parser.TryParse(modifier.commands[1], 0);
                            for (int i = 0; i < ShapeManager.inst.Shapes2D[type].Count; i++)
                            {
                                var shape = ShapeManager.inst.Shapes2D[type][i].name.Replace("_", " ");
                                d.options.Add(new Dropdown.OptionData(shape, ShapeManager.inst.Shapes2D[type][i].icon));
                            }

                            d.value = Parser.TryParse(modifier.commands[2], 0);

                            d.onValueChanged.AddListener(_val =>
                            {
                                modifier.commands[2] = Mathf.Clamp(_val, 0, ShapeManager.inst.Shapes2D[type].Count - 1).ToString();
                                modifier.active = false;

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyDropdown(d);

                            TriggerHelper.AddEventTriggers(d.gameObject, TriggerHelper.ScrollDelta(d));
                        }

                        ColorGenerator(modifier, layout, "Color", 3);
                        SingleGenerator(modifier, layout, "Start Opacity", 4, 1f);
                        SingleGenerator(modifier, layout, "End Opacity", 5, 0f);
                        SingleGenerator(modifier, layout, "Start Scale", 6, 1f);
                        SingleGenerator(modifier, layout, "End Scale", 7, 0f);
                        SingleGenerator(modifier, layout, "Rotation", 8, 0f);
                        SingleGenerator(modifier, layout, "Speed", 9, 5f);
                        SingleGenerator(modifier, layout, "Amount", 10, 1f);
                        SingleGenerator(modifier, layout, "Duration", 11, 1f);
                        SingleGenerator(modifier, layout, "Force X", 12, 0f);
                        SingleGenerator(modifier, layout, "Force Y", 13, 0f);
                        BoolGenerator(modifier, layout, "Emit Trail", 14, false);
                        SingleGenerator(modifier, layout, "Angle", 15, 0f);

                        break;
                    }
                case nameof(ModifierActions.trailRenderer): {
                        SingleGenerator(modifier, layout, "Time", 0, 1f);
                        SingleGenerator(modifier, layout, "Start Width", 1, 1f);
                        SingleGenerator(modifier, layout, "End Width", 2, 0f);
                        ColorGenerator(modifier, layout, "Start Color", 3);
                        SingleGenerator(modifier, layout, "Start Opacity", 4, 1f);
                        ColorGenerator(modifier, layout, "End Color", 5);
                        SingleGenerator(modifier, layout, "End Opacity", 6, 0f);

                        break;
                    }
                case nameof(ModifierActions.rigidbody):
                case nameof(ModifierActions.rigidbodyOther): {
                        if (cmd == "rigidbodyOther")
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        SingleGenerator(modifier, layout, "Gravity", 1, 0f);

                        DropdownGenerator(modifier, layout, "Collision Mode", 2, CoreHelper.StringToOptionData("Discrete", "Continuous"));

                        SingleGenerator(modifier, layout, "Drag", 3, 0f);
                        SingleGenerator(modifier, layout, "Velocity X", 4, 0f);
                        SingleGenerator(modifier, layout, "Velocity Y", 5, 0f);

                        DropdownGenerator(modifier, layout, "Body Type", 6, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));

                        break;
                    }

                #endregion

                #region Player

                case nameof(ModifierActions.playerHit): {
                        IntegerGenerator(modifier, layout, "Hit Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHitIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        IntegerGenerator(modifier, layout, "Hit Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHitAll): {
                        IntegerGenerator(modifier, layout, "Hit Amount", 0, 0);

                        break;
                    }

                case nameof(ModifierActions.playerHeal): {
                        IntegerGenerator(modifier, layout, "Heal Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHealIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        IntegerGenerator(modifier, layout, "Heal Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHealAll): {
                        IntegerGenerator(modifier, layout, "Heal Amount", 0, 0);

                        break;
                    }

                //case "playerKill): {
                //        break;
                //    }
                case nameof(ModifierActions.playerKillIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerKillAll): {
                //        break;
                //    }

                //case "playerRespawn): {
                //        break;
                //    }
                case nameof(ModifierActions.playerRespawnIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerRespawnAll): {
                //        break;
                //    }

                case nameof(ModifierActions.playerMove): {
                        var value = modifier.GetValue(0);

                        if (value.Contains(','))
                        {
                            var axis = modifier.value.Split(',');
                            modifier.SetValue(0, axis[0]);
                            modifier.commands.RemoveAt(modifier.commands.Count - 1);
                            modifier.commands.Insert(1, axis[1]);
                        }

                        SingleGenerator(modifier, layout, "X", 0, 0f);
                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        SingleGenerator(modifier, layout, "Duration", 2, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        SingleGenerator(modifier, layout, "X", 1, 0f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);

                        SingleGenerator(modifier, layout, "Duration", 3, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 5, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveAll): {
                        var value = modifier.GetValue(0);

                        if (value.Contains(','))
                        {
                            var axis = modifier.value.Split(',');
                            modifier.SetValue(0, axis[0]);
                            modifier.commands.RemoveAt(modifier.commands.Count - 1);
                            modifier.commands.Insert(1, axis[1]);
                        }

                        SingleGenerator(modifier, layout, "X", 0, 0f);
                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        SingleGenerator(modifier, layout, "Duration", 2, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveX): {
                        SingleGenerator(modifier, layout, "X", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveXIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        SingleGenerator(modifier, layout, "X", 1, 0f);

                        SingleGenerator(modifier, layout, "Duration", 2, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveXAll): {
                        SingleGenerator(modifier, layout, "X", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveY): {
                        SingleGenerator(modifier, layout, "Y", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveYIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        SingleGenerator(modifier, layout, "Duration", 2, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveYAll): {
                        SingleGenerator(modifier, layout, "Y", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotate): {
                        SingleGenerator(modifier, layout, "Rotation", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotateIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        SingleGenerator(modifier, layout, "Rotation", 1, 0f);

                        SingleGenerator(modifier, layout, "Duration", 2, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotateAll): {
                        SingleGenerator(modifier, layout, "Rotation", 0, 0f);

                        SingleGenerator(modifier, layout, "Duration", 1, 1f);

                        DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, layout, "Relative", 3, false);

                        break;
                    }

                //case "playerMoveToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveIndexToObject): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveAllToObject): {
                //        break;
                //    }
                //case "playerMoveXToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveXIndexToObject): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveXAllToObject): {
                //        break;
                //    }
                //case "playerMoveYToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveYIndexToObject): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveYAllToObject): {
                //        break;
                //    }
                //case "playerRotateToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerRotateIndexToObject): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerRotateAllToObject): {
                //        break;
                //    }

                case nameof(ModifierActions.playerBoost): {
                        SingleGenerator(modifier, layout, "X", 0);
                        SingleGenerator(modifier, layout, "Y", 1);

                        break;
                    }
                case nameof(ModifierActions.playerBoostIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        SingleGenerator(modifier, layout, "X", 1);
                        SingleGenerator(modifier, layout, "Y", 2);

                        break;
                    }
                case nameof(ModifierActions.playerBoostAll): {
                        SingleGenerator(modifier, layout, "X", 0);
                        SingleGenerator(modifier, layout, "Y", 1);

                        break;
                    }

                //case "playerDisableBoost): {
                //        break;
                //    }
                case nameof(ModifierActions.playerDisableBoostIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);

                        break;
                    }
                //case "playerDisableBoostAll): {
                //        break;
                //    }
                
                case nameof(ModifierActions.playerEnableBoost): {
                        BoolGenerator(modifier, layout, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableBoostIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        BoolGenerator(modifier, layout, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableBoostAll): {
                        BoolGenerator(modifier, layout, "Enabled", 0, true);

                        break;
                    }

                case nameof(ModifierActions.playerSpeed): {
                        SingleGenerator(modifier, layout, "Global Speed", 0, 1f);

                        break;
                    }

                case nameof(ModifierActions.playerVelocity): {
                        SingleGenerator(modifier, layout, "X", 1, 0f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        SingleGenerator(modifier, layout, "X", 1, 0f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityAll): {
                        SingleGenerator(modifier, layout, "X", 1, 0f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);

                        break;
                    }
                    
                case nameof(ModifierActions.playerVelocityX): {
                        SingleGenerator(modifier, layout, "X", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityXIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        SingleGenerator(modifier, layout, "X", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityXAll): {
                        SingleGenerator(modifier, layout, "X", 1, 0f);

                        break;
                    }
                    
                case nameof(ModifierActions.playerVelocityY): {
                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityYIndex): {
                        IntegerGenerator(modifier, layout, "Player Index", 0, 0);
                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityYAll): {
                        SingleGenerator(modifier, layout, "Y", 1, 0f);

                        break;
                    }

                case nameof(ModifierActions.setPlayerModel): {
                        IntegerGenerator(modifier, layout, "Player Index", 1, 0, max: 3);
                        var modelID = StringGenerator(modifier, layout, "Model ID", 0);
                        var contextClickable = modelID.transform.Find("Input").gameObject.GetOrAddComponent<ContextClickable>();
                        contextClickable.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Select model", () =>
                                {
                                    RTEditor.inst.PlayerModelsPopup.Open();
                                    CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshModels(model =>
                                    {
                                        contextClickable.GetComponent<InputField>().text = model.basePart.id;
                                    }));
                                }));
                        };

                        break;
                    }
                case nameof(ModifierActions.setGameMode): {
                        DropdownGenerator(modifier, layout, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));

                        break;
                    }
                    
                case nameof(ModifierActions.gameMode): {
                        DropdownGenerator(modifier, layout, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));
                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);

                        break;
                    }

                case nameof(ModifierActions.blackHole): {
                        SingleGenerator(modifier, layout, "Value", 0, 1f);
                        BoolGenerator(modifier, layout, "Use Opacity", 1, false);

                        break;
                    }

                #endregion

                #region Mouse Cursor

                case nameof(ModifierActions.showMouse): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, layout, "Enabled", 0, true);
                        break;
                    }
                //case "hideMouse): {
                //        break;
                //    }
                case nameof(ModifierActions.setMousePosition): {
                        IntegerGenerator(modifier, layout, "Position X", 1, 0);
                        IntegerGenerator(modifier, layout, "Position Y", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.followMousePosition): {
                        SingleGenerator(modifier, layout, "Position Focus", 0, 1f);
                        SingleGenerator(modifier, layout, "Rotation Delay", 1, 1f);

                        break;
                    }

                #endregion

                #region Variable
                    
                case nameof(ModifierActions.getToggle): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        BoolGenerator(modifier, layout, "Value", 1, false);

                        break;
                    }
                case nameof(ModifierActions.getFloat): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Value", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.getInt): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Value", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.getString): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getStringLower): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getStringUpper): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getColor): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        ColorGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getEnum): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        var options = new List<string>();
                        for (int i = 3; i < modifier.commands.Count; i += 2)
                            options.Add(modifier.commands[i]);

                        if (!options.IsEmpty())
                            DropdownGenerator(modifier, layout, "Value", 1, options);

                        var collapseEnum = booleanBar.Duplicate(layout, "Collapse");
                        collapseEnum.transform.localScale = Vector3.one;
                        var collapseEnumText = collapseEnum.transform.Find("Text").GetComponent<Text>();
                        collapseEnumText.text = "Collapse Enum Editor";

                        var collapseEnumToggle = collapseEnum.transform.Find("Toggle").GetComponent<Toggle>();
                        collapseEnumToggle.onValueChanged.ClearAll();
                        collapseEnumToggle.isOn = modifier.GetBool(2, false);
                        collapseEnumToggle.onValueChanged.AddListener(_val =>
                        {
                            modifier.SetValue(2, _val.ToString());
                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        EditorThemeManager.ApplyLightText(collapseEnumText);
                        EditorThemeManager.ApplyToggle(collapseEnumToggle);

                        if (modifier.GetBool(2, false))
                            break;

                        int a = 0;
                        for (int i = 3; i < modifier.commands.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Group {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Name", i, _val =>
                            {
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());
                            var value = StringGenerator(modifier, layout, "Value", i + 1);
                            EditorHelper.AddInputFieldContextMenu(value.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Enum Value", () =>
                        {
                            modifier.commands.Add($"Enum {a}");
                            modifier.commands.Add(a.ToString());

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }

                case nameof(ModifierActions.getAxis): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 10);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, layout, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Delay", 3, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                        SingleGenerator(modifier, layout, "Offset", 5, 0f);
                        SingleGenerator(modifier, layout, "Min", 6, -99999f);
                        SingleGenerator(modifier, layout, "Max", 7, 99999f);
                        SingleGenerator(modifier, layout, "Loop", 9, 99999f);
                        BoolGenerator(modifier, layout, "Use Visual", 8, false);

                        break;
                    }
                case nameof(ModifierActions.getPitch): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getMusicTime): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getMath): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getNearestPlayer): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getCollidingPlayers): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getPlayerHealth): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerPosX): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerPosY): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerRot): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getEventValue): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "Axis", 2, 0);

                        SingleGenerator(modifier, layout, "Delay", 3, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                        SingleGenerator(modifier, layout, "Offset", 5, 0f);
                        SingleGenerator(modifier, layout, "Min", 6, -99999f);
                        SingleGenerator(modifier, layout, "Max", 7, 99999f);
                        SingleGenerator(modifier, layout, "Loop", 8, 99999f);

                        break;
                    }
                case nameof(ModifierActions.getSample): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        IntegerGenerator(modifier, layout, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        SingleGenerator(modifier, layout, "Intensity", 2, 0f);

                        break;
                    }
                case nameof(ModifierActions.getText): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        BoolGenerator(modifier, layout, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierActions.getTextOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 2);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Variable Name", 0);
                        BoolGenerator(modifier, layout, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierActions.getCurrentKey): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getColorSlotHexCode): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        ColorGenerator(modifier, layout, "Color", 1);
                        SingleGenerator(modifier, layout, "Opacity", 2, 1f);
                        SingleGenerator(modifier, layout, "Hue", 3);
                        SingleGenerator(modifier, layout, "Saturation", 4);
                        SingleGenerator(modifier, layout, "Value", 5);

                        break;
                    }
                case nameof(ModifierActions.getFloatFromHexCode): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Hex Code", 1);

                        break;
                    }
                case nameof(ModifierActions.getHexCodeFromFloat): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Value", 1, 0f, max: 1f);

                        break;
                    }
                case nameof(ModifierActions.getJSONString): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.getJSONFloat): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.getJSON): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "JSON", 1);
                        StringGenerator(modifier, layout, "JSON Value", 2);

                        break;
                    }
                case nameof(ModifierActions.getSubString): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Start Index", 1, 0);
                        IntegerGenerator(modifier, layout, "Length", 2, 0);

                        break;
                    }
                case nameof(ModifierActions.getStringLength): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Text", 1);

                        break;
                    }
                case nameof(ModifierActions.getParsedString): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getSplitString): {
                        StringGenerator(modifier, layout, "Text", 0);
                        StringGenerator(modifier, layout, "Character", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Group {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add String Value", () =>
                        {
                            modifier.commands.Add($"SPLITSTRING_VAR_{a}");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.getSplitStringAt): {
                        StringGenerator(modifier, layout, "Text", 0);
                        StringGenerator(modifier, layout, "Character", 1);
                        StringGenerator(modifier, layout, "Variable Name", 2);

                        break;
                    }
                case nameof(ModifierActions.getSplitStringCount): {
                        StringGenerator(modifier, layout, "Text", 0);
                        StringGenerator(modifier, layout, "Character", 1);
                        StringGenerator(modifier, layout, "Variable Name", 2);

                        break;
                    }
                case nameof(ModifierActions.getRegex): {
                        StringGenerator(modifier, layout, "Regex", 0);
                        StringGenerator(modifier, layout, "Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Group {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Regex Value", () =>
                        {
                            modifier.commands.Add($"REGEX_VAR_{a}");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.getFormatVariable): { // whaaaaaaat
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Format Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Text {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Text Value", () =>
                        {
                            modifier.commands.Add($"Text");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.getComparison): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Compare From", 1);
                        StringGenerator(modifier, layout, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierActions.getComparisonMath): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Compare From", 1);
                        StringGenerator(modifier, layout, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierActions.getMixedColors): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        int a = 0;
                        for (int i = 1; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Color {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Color Hex Code", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Color Value", () =>
                        {
                            modifier.commands.Add(RTColors.ColorToHexOptional(LSColors.pink500));

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.getSignaledVariables): {
                        BoolGenerator(modifier, layout, "Clear", 0, true);

                        break;
                    }
                case nameof(ModifierActions.signalLocalVariables): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                //case "clearLocalVariables): {
                //        break;
                //    }

                case nameof(ModifierActions.addVariable):
                case nameof(ModifierActions.subVariable):
                case nameof(ModifierActions.setVariable):
                case nameof(ModifierActions.addVariableOther):
                case nameof(ModifierActions.subVariableOther):
                case nameof(ModifierActions.setVariableOther): {
                        var isGroup = modifier.commands.Count == 2;
                        if (isGroup)
                            PrefabGroupOnly(modifier, layout);
                        IntegerGenerator(modifier, layout, "Value", 0, 0);

                        if (isGroup)
                        {
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }
                case nameof(ModifierActions.animateVariableOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Delay", 3, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                        SingleGenerator(modifier, layout, "Offset", 5, 0f);
                        SingleGenerator(modifier, layout, "Min", 6, -99999f);
                        SingleGenerator(modifier, layout, "Max", 7, 99999f);
                        SingleGenerator(modifier, layout, "Loop", 8, 99999f);

                        break;
                    }

                case nameof(ModifierActions.clampVariable): {
                        IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                        IntegerGenerator(modifier, layout, "Maximum", 2, 0);

                        break;
                    }
                case nameof(ModifierActions.clampVariableOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                        IntegerGenerator(modifier, layout, "Maximum", 2, 0);

                        break;
                    }

                #endregion

                #region Enable / Disable

                case nameof(ModifierActions.enableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, layout, "Enabled", 0, true);
                        BoolGenerator(modifier, layout, "Reset", 1, true);
                        break;
                    }
                case nameof(ModifierActions.enableObjectTree): {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        BoolGenerator(modifier, layout, "Enabled", 2, true);
                        BoolGenerator(modifier, layout, "Use Self", 0, true);
                        BoolGenerator(modifier, layout, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Enabled", 2, true);
                        BoolGenerator(modifier, layout, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectTreeOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Enabled", 3, true);
                        BoolGenerator(modifier, layout, "Use Self", 0, true);
                        BoolGenerator(modifier, layout, "Reset", 2, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectGroup): {
                        PrefabGroupOnly(modifier, layout);
                        BoolGenerator(modifier, layout, "Enabled", 0, true);

                        var options = new List<string>() { "All" };
                        for (int i = 2; i < modifier.commands.Count; i++)
                            options.Add(modifier.commands[i]);

                        DropdownGenerator(modifier, layout, "Value", 1, options);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Group {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Object Group", i, _val =>
                            {
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Enum Value", () =>
                        {
                            modifier.commands.Add($"Object Group");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }

                case nameof(ModifierActions.disableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, layout, "Reset", 1, true);

                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectTree): {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        BoolGenerator(modifier, layout, "Use Self", 0, true);
                        BoolGenerator(modifier, layout, "Reset", 1, true);

                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Reset", 1, true);

                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectTreeOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Use Self", 0, true);
                        BoolGenerator(modifier, layout, "Reset", 2, true);

                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }

                #endregion

                #region JSON

                case nameof(ModifierActions.saveFloat): {
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        SingleGenerator(modifier, layout, "Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.saveString): {
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        StringGenerator(modifier, layout, "Value", 0);

                        break;
                    }
                case nameof(ModifierActions.saveText): {
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.saveVariable): {
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.loadVariable): {
                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.loadVariableOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        break;
                    }

                #endregion

                #region Reactive

                case nameof(ModifierActions.reactivePos): {
                        SingleGenerator(modifier, layout, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, layout, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, layout, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, layout, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, layout, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveSca): {
                        SingleGenerator(modifier, layout, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, layout, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, layout, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, layout, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, layout, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveRot): {
                        SingleGenerator(modifier, layout, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, layout, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }
                case nameof(ModifierActions.reactiveCol): {
                        SingleGenerator(modifier, layout, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, layout, "Sample", 1, 0);
                        ColorGenerator(modifier, layout, "Color", 2);

                        break;
                    }
                case nameof(ModifierActions.reactiveColLerp): {
                        SingleGenerator(modifier, layout, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, layout, "Sample", 1, 0);
                        ColorGenerator(modifier, layout, "Color", 2);

                        break;
                    }
                case nameof(ModifierActions.reactivePosChain): {
                        SingleGenerator(modifier, layout, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, layout, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, layout, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, layout, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, layout, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveScaChain): {
                        SingleGenerator(modifier, layout, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, layout, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, layout, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, layout, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, layout, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveRotChain): {
                        SingleGenerator(modifier, layout, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, layout, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }

                #endregion

                #region Events

                case nameof(ModifierActions.eventOffset): {
                        DropdownGenerator(modifier, layout, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "Value Index", 2, 0);
                        SingleGenerator(modifier, layout, "Offset Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetVariable): {
                        DropdownGenerator(modifier, layout, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "Value Index", 2, 0);
                        SingleGenerator(modifier, layout, "Multiply Variable", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetMath): {
                        DropdownGenerator(modifier, layout, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "Value Index", 2, 0);
                        StringGenerator(modifier, layout, "Evaluation", 0);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetAnimate): {
                        DropdownGenerator(modifier, layout, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "Value Index", 2, 0);
                        SingleGenerator(modifier, layout, "Offset Value", 0, 0f);

                        SingleGenerator(modifier, layout, "Time", 3, 1f);
                        DropdownGenerator(modifier, layout, "Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                        BoolGenerator(modifier, layout, "Relative", 5, false);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetCopyAxis): {
                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, layout, "To Axis", 4, 0);

                        SingleGenerator(modifier, layout, "Delay", 5, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                        SingleGenerator(modifier, layout, "Offset", 7, 0f);
                        SingleGenerator(modifier, layout, "Min", 8, -99999f);
                        SingleGenerator(modifier, layout, "Max", 9, 99999f);

                        SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                        BoolGenerator(modifier, layout, "Use Visual", 11, false);

                        break;
                    }
                //case "vignetteTracksPlayer): {
                //        break;
                //    }
                //case "lensTracksPlayer): {
                //        break;
                //    }

                #endregion

                #region Color

                case nameof(ModifierActions.addColor): {
                        ColorGenerator(modifier, layout, "Color", 1);

                        SingleGenerator(modifier, layout, "Hue", 2, 0f);
                        SingleGenerator(modifier, layout, "Saturation", 3, 0f);
                        SingleGenerator(modifier, layout, "Value", 4, 0f);

                        SingleGenerator(modifier, layout, "Add Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.addColorOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        ColorGenerator(modifier, layout, "Color", 2);

                        SingleGenerator(modifier, layout, "Hue", 3, 0f);
                        SingleGenerator(modifier, layout, "Saturation", 4, 0f);
                        SingleGenerator(modifier, layout, "Value", 5, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.lerpColor): {
                        ColorGenerator(modifier, layout, "Color", 1);

                        SingleGenerator(modifier, layout, "Hue", 2, 0f);
                        SingleGenerator(modifier, layout, "Saturation", 3, 0f);
                        SingleGenerator(modifier, layout, "Value", 4, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.lerpColorOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        ColorGenerator(modifier, layout, "Color", 2);

                        SingleGenerator(modifier, layout, "Hue", 3, 0f);
                        SingleGenerator(modifier, layout, "Saturation", 4, 0f);
                        SingleGenerator(modifier, layout, "Value", 5, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.addColorPlayerDistance): {
                        ColorGenerator(modifier, layout, "Color", 1);
                        SingleGenerator(modifier, layout, "Multiply", 0, 1f);
                        SingleGenerator(modifier, layout, "Offset", 2, 10f);

                        break;
                    }
                case nameof(ModifierActions.lerpColorPlayerDistance): {
                        ColorGenerator(modifier, layout, "Color", 1);
                        SingleGenerator(modifier, layout, "Multiply", 0, 1f);
                        SingleGenerator(modifier, layout, "Offset", 2, 10f);

                        if (cmd == "lerpColorPlayerDistance")
                        {
                            SingleGenerator(modifier, layout, "Opacity", 3, 1f);
                            SingleGenerator(modifier, layout, "Hue", 4, 0f);
                            SingleGenerator(modifier, layout, "Saturation", 5, 0f);
                            SingleGenerator(modifier, layout, "Value", 6, 0f);
                        }

                        break;
                    }
                case nameof(ModifierActions.setOpacity): {
                        SingleGenerator(modifier, layout, "Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setOpacityOther): {
                        SingleGenerator(modifier, layout, "Amount", 0, 1f);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.copyColor): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, layout, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierActions.copyColorOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, layout, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierActions.applyColorGroup): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        break;
                    }
                case nameof(ModifierActions.setColorHex): {
                        StringGenerator(modifier, layout, "Hex Code", 0);
                        StringGenerator(modifier, layout, "Hex Gradient Color", 1);
                        break;
                    }
                case nameof(ModifierActions.setColorHexOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Hex Code", 0);
                        StringGenerator(modifier, layout, "Hex Gradient Color", 2);
                        break;
                    }
                case nameof(ModifierActions.setColorRGBA): {
                        SingleGenerator(modifier, layout, "Red 1", 0, 1f);
                        SingleGenerator(modifier, layout, "Green 1", 1, 1f);
                        SingleGenerator(modifier, layout, "Blue 1", 2, 1f);
                        SingleGenerator(modifier, layout, "Opacity 1", 3, 1f);

                        SingleGenerator(modifier, layout, "Red 2", 4, 1f);
                        SingleGenerator(modifier, layout, "Green 2", 5, 1f);
                        SingleGenerator(modifier, layout, "Blue 2", 6, 1f);
                        SingleGenerator(modifier, layout, "Opacity 2", 7, 1f);

                        break;
                    }
                case nameof(ModifierActions.setColorRGBAOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 8);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, layout, "Red 1", 0, 1f);
                        SingleGenerator(modifier, layout, "Green 1", 1, 1f);
                        SingleGenerator(modifier, layout, "Blue 1", 2, 1f);
                        SingleGenerator(modifier, layout, "Opacity 1", 3, 1f);

                        SingleGenerator(modifier, layout, "Red 2", 4, 1f);
                        SingleGenerator(modifier, layout, "Green 2", 5, 1f);
                        SingleGenerator(modifier, layout, "Blue 2", 6, 1f);
                        SingleGenerator(modifier, layout, "Opacity 2", 7, 1f);

                        break;
                    }
                case nameof(ModifierActions.animateColorKF): {
                        SingleGenerator(modifier, layout, "Time", 0);
                        DropdownGenerator(modifier, layout, "Color Source", 1, CoreHelper.StringToOptionData("Objects", "BG Objects", "Effects"), onSelect: _val => RenderModifier(modifier, modifierCard.index));

                        var colorSource = modifier.GetInt(1, 0);

                        ColorGenerator(modifier, layout, "Color 1 Start", 2, colorSource);
                        SingleGenerator(modifier, layout, "Opacity 1 Start", 3, 1f);
                        SingleGenerator(modifier, layout, "Hue 1 Start", 4, 0f);
                        SingleGenerator(modifier, layout, "Saturation 1 Start", 5, 0f);
                        SingleGenerator(modifier, layout, "Value 1 Start", 6, 0f);

                        ColorGenerator(modifier, layout, "Color 2 Start", 7, colorSource);
                        SingleGenerator(modifier, layout, "Opacity 2 Start", 8, 1f);
                        SingleGenerator(modifier, layout, "Hue 2 Start", 9, 0f);
                        SingleGenerator(modifier, layout, "Saturation 2 Start", 10, 0f);
                        SingleGenerator(modifier, layout, "Value 2 Start", 11, 0f);

                        int a = 0;
                        for (int i = 12; i < modifier.commands.Count; i += 14)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Keyframe {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex); // collapse keyframe
                                modifier.commands.RemoveAt(groupIndex); // keyframe time
                                modifier.commands.RemoveAt(groupIndex); // color slot 1
                                modifier.commands.RemoveAt(groupIndex); // opacity 1
                                modifier.commands.RemoveAt(groupIndex); // hue 1
                                modifier.commands.RemoveAt(groupIndex); // saturation 1
                                modifier.commands.RemoveAt(groupIndex); // value 1
                                modifier.commands.RemoveAt(groupIndex); // color slot 2
                                modifier.commands.RemoveAt(groupIndex); // opacity 2
                                modifier.commands.RemoveAt(groupIndex); // hue 2
                                modifier.commands.RemoveAt(groupIndex); // saturation 2
                                modifier.commands.RemoveAt(groupIndex); // value 2
                                modifier.commands.RemoveAt(groupIndex); // relative
                                modifier.commands.RemoveAt(groupIndex); // easing

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var collapseEnum = booleanBar.Duplicate(layout, "Collapse");
                            collapseEnum.transform.localScale = Vector3.one;
                            var collapseEnumText = collapseEnum.transform.Find("Text").GetComponent<Text>();
                            collapseEnumText.text = "Collapse Keyframe Editor";

                            var collapseEnumToggle = collapseEnum.transform.Find("Toggle").GetComponent<Toggle>();
                            collapseEnumToggle.onValueChanged.ClearAll();
                            collapseEnumToggle.isOn = modifier.GetBool(i, false);
                            collapseEnumToggle.onValueChanged.AddListener(_val =>
                            {
                                modifier.SetValue(groupIndex, _val.ToString());
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyLightText(collapseEnumText);
                            EditorThemeManager.ApplyToggle(collapseEnumToggle);

                            if (modifier.GetBool(i, false))
                                continue;

                            SingleGenerator(modifier, layout, "Keyframe Time", i + 1);
                            DropdownGenerator(modifier, layout, "Easing", i + 13, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                            BoolGenerator(modifier, layout, "Relative", i + 12, true);

                            ColorGenerator(modifier, layout, "Color 1", i + 2, colorSource);
                            SingleGenerator(modifier, layout, "Opacity 1", i + 3, 1f);
                            SingleGenerator(modifier, layout, "Hue 1", i + 4, 0f);
                            SingleGenerator(modifier, layout, "Saturation 1", i + 5, 0f);
                            SingleGenerator(modifier, layout, "Value 1", i + 6, 0f);

                            ColorGenerator(modifier, layout, "Color 2", i + 7, colorSource);
                            SingleGenerator(modifier, layout, "Opacity 2", i + 8, 1f);
                            SingleGenerator(modifier, layout, "Hue 2", i + 9, 0f);
                            SingleGenerator(modifier, layout, "Saturation 2", i + 10, 0f);
                            SingleGenerator(modifier, layout, "Value 2", i + 11, 0f);

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Keyframe", () =>
                        {
                            modifier.commands.Add("False"); // collapse keyframe
                            modifier.commands.Add("0"); // keyframe time
                            modifier.commands.Add("0"); // color slot 1
                            modifier.commands.Add("1"); // opacity 1
                            modifier.commands.Add("0"); // hue 1
                            modifier.commands.Add("0"); // saturation 1
                            modifier.commands.Add("0"); // value 1
                            modifier.commands.Add("0"); // color slot 2
                            modifier.commands.Add("1"); // opacity 2
                            modifier.commands.Add("0"); // hue 2
                            modifier.commands.Add("0"); // saturation 2
                            modifier.commands.Add("0"); // value 2
                            modifier.commands.Add("True"); // relative
                            modifier.commands.Add("0"); // easing

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.animateColorKFHex): {
                        SingleGenerator(modifier, layout, "Time", 0);

                        StringGenerator(modifier, layout, "Color 1", 1);
                        StringGenerator(modifier, layout, "Color 2", 2);

                        int a = 0;
                        for (int i = 3; i < modifier.commands.Count; i += 6)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Keyframe {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex); // collapse keyframe
                                modifier.commands.RemoveAt(groupIndex); // keyframe time
                                modifier.commands.RemoveAt(groupIndex); // color slot 1
                                modifier.commands.RemoveAt(groupIndex); // color slot 2
                                modifier.commands.RemoveAt(groupIndex); // relative
                                modifier.commands.RemoveAt(groupIndex); // easing

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var collapseEnum = booleanBar.Duplicate(layout, "Collapse");
                            collapseEnum.transform.localScale = Vector3.one;
                            var collapseEnumText = collapseEnum.transform.Find("Text").GetComponent<Text>();
                            collapseEnumText.text = "Collapse Keyframe Editor";

                            var collapseEnumToggle = collapseEnum.transform.Find("Toggle").GetComponent<Toggle>();
                            collapseEnumToggle.onValueChanged.ClearAll();
                            collapseEnumToggle.isOn = modifier.GetBool(i, false);
                            collapseEnumToggle.onValueChanged.AddListener(_val =>
                            {
                                modifier.SetValue(groupIndex, _val.ToString());
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyLightText(collapseEnumText);
                            EditorThemeManager.ApplyToggle(collapseEnumToggle);

                            if (modifier.GetBool(i, false))
                                break;

                            SingleGenerator(modifier, layout, "Keyframe Time", i + 1);
                            DropdownGenerator(modifier, layout, "Easing", i + 5, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                            BoolGenerator(modifier, layout, "Relative", i + 4, true);

                            StringGenerator(modifier, layout, "Color 1", i + 2);
                            StringGenerator(modifier, layout, "Color 2", i + 3);

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Keyframe", () =>
                        {
                            modifier.commands.Add("False"); // collapse keyframe
                            modifier.commands.Add("0"); // keyframe time
                            modifier.commands.Add("0"); // color 1
                            modifier.commands.Add("0"); // color 2
                            modifier.commands.Add("True"); // relative
                            modifier.commands.Add("0"); // easing

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }

                #endregion

                #region Shape

                case nameof(ModifierActions.actorFrameTexture): {
                        DropdownGenerator(modifier, layout, "Camera", 0, CoreHelper.StringToOptionData("Foreground", "Background"));
                        IntegerGenerator(modifier, layout, "Width", 1, 512);
                        IntegerGenerator(modifier, layout, "Height", 2, 512);
                        SingleGenerator(modifier, layout, "Pos X", 3, 0f);
                        SingleGenerator(modifier, layout, "Pos Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.setImage): {
                        StringGenerator(modifier, layout, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.setImageOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, layout, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.setText): {
                        StringGenerator(modifier, layout, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.setTextOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, layout, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.addText): {
                        StringGenerator(modifier, layout, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.addTextOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, layout, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.removeText): {
                        IntegerGenerator(modifier, layout, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, layout, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextAt): {
                        IntegerGenerator(modifier, layout, "Remove At", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextOtherAt): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, layout, "Remove At", 0, 0);

                        break;
                    }
                //case "formatText): {
                //        break;
                //    }
                case nameof(ModifierActions.textSequence): {
                        SingleGenerator(modifier, layout, "Length", 0, 1f);
                        BoolGenerator(modifier, layout, "Display Glitch", 1, true);
                        BoolGenerator(modifier, layout, "Play Sound", 2, true);
                        BoolGenerator(modifier, layout, "Custom Sound", 3, false);
                        StringGenerator(modifier, layout, "Sound Path", 4);
                        BoolGenerator(modifier, layout, "Global", 5, false);

                        SingleGenerator(modifier, layout, "Pitch", 6, 1f);
                        SingleGenerator(modifier, layout, "Volume", 7, 1f);
                        SingleGenerator(modifier, layout, "Pitch Vary", 8, 0f);
                        var str = StringGenerator(modifier, layout, "Custom Text", 9);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, layout, "Time Offset", 10, 0f);
                        BoolGenerator(modifier, layout, "Time Relative", 11, false);

                        break;
                    }
                //case "backgroundShape): {
                //        break;
                //    }
                //case "sphereShape): {
                //        break;
                //    }
                case nameof(ModifierActions.translateShape): {
                        SingleGenerator(modifier, layout, "Pos X", 1, 0f);
                        SingleGenerator(modifier, layout, "Pos Y", 2, 0f);
                        SingleGenerator(modifier, layout, "Sca X", 3, 0f);
                        SingleGenerator(modifier, layout, "Sca Y", 4, 0f);
                        SingleGenerator(modifier, layout, "Rot", 5, 0f, 15f, 3f);

                        break;
                    }

                #endregion

                #region Animation

                case nameof(ModifierActions.animateObject): {
                        SingleGenerator(modifier, layout, "Time", 0, 1f);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, layout, "X", 2, 0f);
                        SingleGenerator(modifier, layout, "Y", 3, 0f);
                        SingleGenerator(modifier, layout, "Z", 4, 0f);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        break;
                    }
                case nameof(ModifierActions.animateObjectOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, layout, "Time", 0, 1f);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, layout, "X", 2, 0f);
                        SingleGenerator(modifier, layout, "Y", 3, 0f);
                        SingleGenerator(modifier, layout, "Z", 4, 0f);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());


                        break;
                    }
                case nameof(ModifierActions.animateSignal): {
                        PrefabGroupOnly(modifier, layout);

                        SingleGenerator(modifier, layout, "Time", 0, 1f);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, layout, "X", 2, 0f);
                        SingleGenerator(modifier, layout, "Y", 3, 0f);
                        SingleGenerator(modifier, layout, "Z", 4, 0f);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        StringGenerator(modifier, layout, "Signal Group", 7);
                        SingleGenerator(modifier, layout, "Signal Delay", 8, 0f);
                        BoolGenerator(modifier, layout, "Signal Deactivate", 9, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignalOther): {
                        PrefabGroupOnly(modifier, layout);

                        SingleGenerator(modifier, layout, "Time", 0, 1f);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, layout, "X", 2, 0f);
                        SingleGenerator(modifier, layout, "Y", 3, 0f);
                        SingleGenerator(modifier, layout, "Z", 4, 0f);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        var str = StringGenerator(modifier, layout, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Signal Group", 8);
                        SingleGenerator(modifier, layout, "Signal Delay", 9, 0f);
                        BoolGenerator(modifier, layout, "Signal Deactivate", 10, true);

                        break;
                    }
                    
                case nameof(ModifierActions.animateObjectMath): {
                        StringGenerator(modifier, layout, "Time", 0);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, layout, "X", 2);
                        StringGenerator(modifier, layout, "Y", 3);
                        StringGenerator(modifier, layout, "Z", 4);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        break;
                    }
                case nameof(ModifierActions.animateObjectMathOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Time", 0);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, layout, "X", 2);
                        StringGenerator(modifier, layout, "Y", 3);
                        StringGenerator(modifier, layout, "Z", 4);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        break;
                    }
                case nameof(ModifierActions.animateSignalMath): {
                        PrefabGroupOnly(modifier, layout);

                        StringGenerator(modifier, layout, "Time", 0);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, layout, "X", 2);
                        StringGenerator(modifier, layout, "Y", 3);
                        StringGenerator(modifier, layout, "Z", 4);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        StringGenerator(modifier, layout, "Signal Group", 7);
                        StringGenerator(modifier, layout, "Signal Delay", 8);
                        BoolGenerator(modifier, layout, "Signal Deactivate", 9, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignalMathOther): {
                        PrefabGroupOnly(modifier, layout);

                        StringGenerator(modifier, layout, "Time", 0);

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, layout, "X", 2);
                        StringGenerator(modifier, layout, "Y", 3);
                        StringGenerator(modifier, layout, "Z", 4);

                        BoolGenerator(modifier, layout, "Relative", 5, true);

                        DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        var str = StringGenerator(modifier, layout, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, layout, "Signal Group", 8);
                        StringGenerator(modifier, layout, "Signal Delay", 9);
                        BoolGenerator(modifier, layout, "Signal Deactivate", 10, true);

                        break;
                    }

                case nameof(ModifierActions.gravity): {
                        SingleGenerator(modifier, layout, "X", 1, -1f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);
                        SingleGenerator(modifier, layout, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, layout, "Curve", 4, 2);

                        break;
                    }
                case nameof(ModifierActions.gravityOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, layout, "X", 1, -1f);
                        SingleGenerator(modifier, layout, "Y", 2, 0f);
                        SingleGenerator(modifier, layout, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, layout, "Curve", 4, 2);

                        break;
                    }

                case nameof(ModifierActions.copyAxis): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Delay", 5, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                        SingleGenerator(modifier, layout, "Offset", 7, 0f);
                        SingleGenerator(modifier, layout, "Min", 8, -99999f);
                        SingleGenerator(modifier, layout, "Max", 9, 99999f);

                        SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                        BoolGenerator(modifier, layout, "Use Visual", 11, false);

                        break;
                    }
                case nameof(ModifierActions.copyAxisMath): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Delay", 5, 0f);

                        SingleGenerator(modifier, layout, "Min", 6, -99999f);
                        SingleGenerator(modifier, layout, "Max", 7, 99999f);
                        BoolGenerator(modifier, layout, "Use Visual", 9, false);
                        StringGenerator(modifier, layout, "Expression", 8);

                        break;
                    }
                case nameof(ModifierActions.copyAxisGroup): {
                        PrefabGroupOnly(modifier, layout);
                        StringGenerator(modifier, layout, "Expression", 0);

                        DropdownGenerator(modifier, layout, "To Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, layout, "To Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        int a = 0;
                        for (int i = 3; i < modifier.commands.Count; i += 8)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Group {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                for (int j = 0; j < 8; j++)
                                    modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var groupName = StringGenerator(modifier, layout, "Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());
                            var str = StringGenerator(modifier, layout, "Object Group", i + 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            DropdownGenerator(modifier, layout, "From Type", i + 2, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color", "Variable"));
                            DropdownGenerator(modifier, layout, "From Axis", i + 3, CoreHelper.StringToOptionData("X", "Y", "Z"));
                            SingleGenerator(modifier, layout, "Delay", i + 4, 0f);
                            SingleGenerator(modifier, layout, "Min", i + 5, -9999f);
                            SingleGenerator(modifier, layout, "Max", i + 6, 9999f);
                            BoolGenerator(modifier, layout, "Use Visual", 7, false);

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Group", () =>
                        {
                            var lastIndex = modifier.commands.Count - 1;

                            modifier.commands.Add($"var_{a}");
                            modifier.commands.Add("Object Group");
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                            modifier.commands.Add("-9999");
                            modifier.commands.Add("9999");
                            modifier.commands.Add("False");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.copyPlayerAxis): {
                        DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                        SingleGenerator(modifier, layout, "Offset", 7, 0f);
                        SingleGenerator(modifier, layout, "Min", 8, -99999f);
                        SingleGenerator(modifier, layout, "Max", 9, 99999f);

                        break;
                    }

                case nameof(ModifierActions.legacyTail): {
                        SingleGenerator(modifier, layout, "Total Time", 0, 200f);

                        var path = stringInput.Duplicate(layout, "usage");
                        path.transform.localScale = Vector3.one;
                        var labelText = path.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Update Object to Update Modifier";
                        path.transform.Find("Text").AsRT().sizeDelta = new Vector2(350f, 32f);
                        Destroy(path.transform.Find("Input").gameObject);

                        for (int i = 1; i < modifier.commands.Count; i += 3)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $" Tail Group {(i + 2) / 3}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                for (int j = 0; j < 3; j++)
                                    modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            var str = StringGenerator(modifier, layout, "Object Group", i);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            SingleGenerator(modifier, layout, "Distance", i + 1, 2f);
                            SingleGenerator(modifier, layout, "Time", i + 2, 12f);
                        }

                        AddGenerator(modifier, layout, "Add Group", () =>
                        {
                            var lastIndex = modifier.commands.Count - 1;
                            var length = "2";
                            var time = "12";
                            if (lastIndex - 1 > 2)
                            {
                                length = modifier.commands[lastIndex - 1];
                                time = modifier.commands[lastIndex];
                            }

                            modifier.commands.Add("Object Group");
                            modifier.commands.Add(length);
                            modifier.commands.Add(time);

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }

                case nameof(ModifierActions.applyAnimationFrom):
                case nameof(ModifierActions.applyAnimationTo):
                case nameof(ModifierActions.applyAnimation): {
                        PrefabGroupOnly(modifier, layout);
                        if (cmd != "applyAnimation")
                        {
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }
                        else
                        {
                            var from = StringGenerator(modifier, layout, "From Group", 0);
                            EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                            var to = StringGenerator(modifier, layout, "To Group", 10);
                            EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                        }

                        BoolGenerator(modifier, layout, "Animate Position", 1, true);
                        BoolGenerator(modifier, layout, "Animate Scale", 2, true);
                        BoolGenerator(modifier, layout, "Animate Rotation", 3, true);
                        SingleGenerator(modifier, layout, "Delay Position", 4, 0f);
                        SingleGenerator(modifier, layout, "Delay Scale", 5, 0f);
                        SingleGenerator(modifier, layout, "Delay Rotation", 6, 0f);
                        BoolGenerator(modifier, layout, "Use Visual", 7, false);
                        SingleGenerator(modifier, layout, "Length", 8, 1f);
                        SingleGenerator(modifier, layout, "Speed", 9, 1f);

                        break;
                    }
                case nameof(ModifierActions.applyAnimationFromMath):
                case nameof(ModifierActions.applyAnimationToMath):
                case nameof(ModifierActions.applyAnimationMath): {
                        PrefabGroupOnly(modifier, layout);
                        if (cmd != "applyAnimationMath")
                        {
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }
                        else
                        {
                            var from = StringGenerator(modifier, layout, "From Group", 0);
                            EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                            var to = StringGenerator(modifier, layout, "To Group", 10);
                            EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                        }

                        BoolGenerator(modifier, layout, "Animate Position", 1, true);
                        BoolGenerator(modifier, layout, "Animate Scale", 2, true);
                        BoolGenerator(modifier, layout, "Animate Rotation", 3, true);
                        StringGenerator(modifier, layout, "Delay Position", 4);
                        StringGenerator(modifier, layout, "Delay Scale", 5);
                        StringGenerator(modifier, layout, "Delay Rotation", 6);
                        BoolGenerator(modifier, layout, "Use Visual", 7, false);
                        StringGenerator(modifier, layout, "Length", 8);
                        StringGenerator(modifier, layout, "Speed", 9);
                        StringGenerator(modifier, layout, "Time", cmd != "applyAnimationMath" ? 10 : 11);

                        break;
                    }

                #endregion

                #region Prefab

                case nameof(ModifierActions.spawnPrefab):
                case nameof(ModifierActions.spawnPrefabOffset):
                case nameof(ModifierActions.spawnPrefabOffsetOther):
                case nameof(ModifierActions.spawnMultiPrefab):
                case nameof(ModifierActions.spawnMultiPrefabOffset):
                case nameof(ModifierActions.spawnMultiPrefabOffsetOther): {
                        var isMulti = cmd.Contains("Multi");
                        var isOther = cmd.Contains("Other");
                        if (isOther)
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", isMulti ? 9 : 10);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        int valueIndex = 10;
                        if (isOther)
                            valueIndex++;
                        if (isMulti)
                            valueIndex--;

                        DropdownGenerator(modifier, layout, "Search Prefab Using", valueIndex + 2, CoreHelper.StringToOptionData("Index", "ID", "Name"));
                        StringGenerator(modifier, layout, "Prefab Reference", 0);

                        SingleGenerator(modifier, layout, "Position X", 1, 0f);
                        SingleGenerator(modifier, layout, "Position Y", 2, 0f);
                        SingleGenerator(modifier, layout, "Scale X", 3, 0f);
                        SingleGenerator(modifier, layout, "Scale Y", 4, 0f);
                        SingleGenerator(modifier, layout, "Rotation", 5, 0f, 15f, 3f);

                        IntegerGenerator(modifier, layout, "Repeat Count", 6, 0);
                        SingleGenerator(modifier, layout, "Repeat Offset Time", 7, 0f);
                        SingleGenerator(modifier, layout, "Speed", 8, 1f);

                        if (!isMulti)
                            BoolGenerator(modifier, layout, "Permanent", 9, false);

                        SingleGenerator(modifier, layout, "Time", valueIndex, 0f);
                        BoolGenerator(modifier, layout, "Time Relative", valueIndex + 1, true);

                        break;
                    }

                case nameof(ModifierActions.clearSpawnedPrefabs): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                #endregion

                #region Ranking

                //case "saveLevelRank): {
                //        break;
                //    }
                //case "clearHits): {
                //        break;
                //    }
                case nameof(ModifierActions.addHit): {
                        BoolGenerator(modifier, layout, "Use Self Position", 0, true);
                        StringGenerator(modifier, layout, "Time", 1);

                        break;
                    }
                //case "subHit): {
                //        break;
                //    }
                //case "clearDeaths): {
                //        break;
                //    }
                case nameof(ModifierActions.addDeath): {
                        BoolGenerator(modifier, layout, "Use Self Position", 0, true);
                        StringGenerator(modifier, layout, "Time", 1);

                        break;
                    }
                //case "subDeath): {
                //        break;
                //    }

                #endregion

                #region Updates

                //case "updateObjects): {
                //        break;
                //    }
                case nameof(ModifierActions.updateObject): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.setParent): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.setParentOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, layout, "Clear Parent", 1, false);
                        var str2 = StringGenerator(modifier, layout, "Parent Group To", 2);
                        EditorHelper.AddInputFieldContextMenu(str2.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.detachParent): {
                        BoolGenerator(modifier, layout, "Detach", 0, false);

                        break;
                    }
                case nameof(ModifierActions.detachParentOther): {
                        BoolGenerator(modifier, layout, "Detach", 0, false);

                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                #endregion

                #region Physics

                case nameof(ModifierActions.setCollision): {
                        BoolGenerator(modifier, layout, "On", 0, false);
                        break;
                    }
                case nameof(ModifierActions.setCollisionOther): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, layout, "On", 0, false);
                        break;
                    }

                #endregion

                #region Checkpoints
                    
                case nameof(ModifierActions.createCheckpoint): {
                        SingleGenerator(modifier, layout, "Time", 0);
                        BoolGenerator(modifier, layout, "Time Relative", 1);

                        SingleGenerator(modifier, layout, "Pos X", 2);
                        SingleGenerator(modifier, layout, "Pos Y", 3);

                        BoolGenerator(modifier, layout, "Heal", 4);
                        BoolGenerator(modifier, layout, "Respawn", 5, true);
                        BoolGenerator(modifier, layout, "Reverse On Death", 6, true);
                        BoolGenerator(modifier, layout, "Set Time On Death", 7, true);
                        DropdownGenerator(modifier, layout, "Spawn Position Type", 8, CoreHelper.ToOptionData<Checkpoint.SpawnPositionType>());

                        int a = 0;
                        for (int i = 9; i < modifier.commands.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $"Position {a + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroupButton.button.onClick.NewListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(modifier, modifierCard.index);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                            SingleGenerator(modifier, layout, "Pos X", i);
                            SingleGenerator(modifier, layout, "Pos Y", i + 1);

                            a++;
                        }

                        AddGenerator(modifier, layout, "Add Position Value", () =>
                        {
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(modifier, modifierCard.index);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        break;
                    }
                case nameof(ModifierActions.resetCheckpoint): {
                        BoolGenerator(modifier, layout, "Reset to Previous", 0);
                        break;
                    }

                #endregion

                #region Interfaces

                case nameof(ModifierActions.loadInterface): {
                        StringGenerator(modifier, layout, "Path", 0);

                        break;
                    }
                //case "pauseLevel): {
                //        break;
                //    }
                //case "quitToMenu): {
                //        break;
                //    }
                //case "quitToArcade): {
                //        break;
                //    }

                #endregion

                #region Misc

                case nameof(ModifierActions.setBGActive): {
                        BoolGenerator(modifier, layout, "Active", 0, false);
                        var str = StringGenerator(modifier, layout, "BG Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierActions.signalModifier): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, layout, "Delay", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.activateModifier): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, layout, "Do Multiple", 1, true);
                        IntegerGenerator(modifier, layout, "Singlular Index", 2, 0);

                        for (int i = 3; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = stringInput.Duplicate(layout, "group label");
                            label.transform.localScale = Vector3.one;
                            var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                            groupLabel.text = $" Name {i + 1}";
                            label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                            Destroy(label.transform.Find("Input").gameObject);

                            StringGenerator(modifier, layout, "Modifier Name", groupIndex);

                            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                            deleteGroupButton.button.onClick.AddListener(() =>
                            {
                                modifier.commands.RemoveAt(groupIndex);

                                if (modifier.reference is BeatmapObject beatmapObject)
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                if (modifier.reference is BackgroundObject backgroundObject)
                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                                RenderModifier(modifier, modifierCard.index);
                            });

                            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);
                        }

                        AddGenerator(modifier, layout, "Add Group", () =>
                        {
                            modifier.commands.Add("modifierName");

                            if (modifier.reference is BeatmapObject beatmapObject)
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            if (modifier.reference is BackgroundObject backgroundObject)
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            RenderModifier(modifier, modifierCard.index);
                        });

                        break;
                    }

                case nameof(ModifierActions.editorNotify): {
                        StringGenerator(modifier, layout, "Text", 0);
                        SingleGenerator(modifier, layout, "Time", 1, 0.5f);
                        DropdownGenerator(modifier, layout, "Notify Type", 2, CoreHelper.StringToOptionData("Info", "Success", "Error", "Warning"));

                        break;
                    }
                case nameof(ModifierActions.setWindowTitle): {
                        StringGenerator(modifier, layout, "Title", 0);

                        break;
                    }
                case nameof(ModifierActions.setDiscordStatus): {
                        StringGenerator(modifier, layout, "State", 0);
                        StringGenerator(modifier, layout, "Details", 1);
                        DropdownGenerator(modifier, layout, "Sub Icon", 2, CoreHelper.StringToOptionData("Arcade", "Editor", "Play", "Menu"));
                        DropdownGenerator(modifier, layout, "Icon", 3, CoreHelper.StringToOptionData("PA Logo White", "PA Logo Black"));

                        break;
                    }

                case "forLoop": {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        IntegerGenerator(modifier, layout, "Start Index", 1, 0);
                        IntegerGenerator(modifier, layout, "End Count", 2, 10);
                        IntegerGenerator(modifier, layout, "Increment", 3, 1);

                        break;
                    }
                //case "continue): {
                //        break;
                //    }
                //case "return): {
                //        break;
                //    }

                #endregion

                #endregion

                #region Triggers

                case nameof(ModifierTriggers.localVariableContains): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Contains", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableStartsWith): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Starts With", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableEndsWith): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Ends With", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableEquals): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        StringGenerator(modifier, layout, "Compare To", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableLesserEquals): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableGreaterEquals): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableLesser): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableGreater): {
                        StringGenerator(modifier, layout, "Variable Name", 0);
                        SingleGenerator(modifier, layout, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableExists): {
                        StringGenerator(modifier, layout, "Variable Name", 0);

                        break;
                    }

                #region Float

                case nameof(ModifierTriggers.pitchEquals):
                case nameof(ModifierTriggers.pitchLesserEquals):
                case nameof(ModifierTriggers.pitchGreaterEquals):
                case nameof(ModifierTriggers.pitchLesser):
                case nameof(ModifierTriggers.pitchGreater):
                case nameof(ModifierTriggers.playerDistanceLesser):
                case nameof(ModifierTriggers.playerDistanceGreater): {
                        if (cmd.Contains("Other"))
                            PrefabGroupOnly(modifier, layout);

                        SingleGenerator(modifier, layout, "Value", 0, 1f);

                        if (cmd == "setAlphaOther")
                        {
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        if (cmd == "blackHole")
                            BoolGenerator(modifier, layout, "Use Opacity", 1, false);

                        break;
                    }

                case nameof(ModifierTriggers.musicTimeGreater):
                case nameof(ModifierTriggers.musicTimeLesser): {
                        SingleGenerator(modifier, layout, "Time", 0, 0f);
                        BoolGenerator(modifier, layout, "Offset From Start Time", 1, false);

                        break;
                    }

                #endregion

                #region String

                case nameof(ModifierTriggers.usernameEquals): {
                        StringGenerator(modifier, layout, "Username", 0);
                        break;
                    }
                case nameof(ModifierTriggers.objectCollide): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        if (cmd == "setParentOther")
                        {
                            BoolGenerator(modifier, layout, "Clear Parent", 1, false);
                            var str2 = StringGenerator(modifier, layout, "Parent Group To", 2);
                            EditorHelper.AddInputFieldContextMenu(str2.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }
                case nameof(ModifierTriggers.levelPathExists):
                case nameof(ModifierTriggers.realTimeDayWeekEquals): {
                        StringGenerator(modifier, layout, cmd == "setText" || cmd == "addText" ? "Text" :
                            cmd == "setWindowTitle" ? "Title" :
                            cmd == "realTimeDayWeekEquals" ? "Day" :
                            "Path", 0);

                        break;
                    }
                case nameof(ModifierTriggers.levelUnlocked):
                case nameof(ModifierTriggers.levelCompletedOther):
                case nameof(ModifierTriggers.levelExists): {
                        StringGenerator(modifier, layout, "ID", 0);

                        break;
                    }

                #endregion

                #region Integer

                case nameof(ModifierTriggers.mouseButtonDown):
                case nameof(ModifierTriggers.mouseButton):
                case nameof(ModifierTriggers.mouseButtonUp):
                case nameof(ModifierTriggers.playerCountEquals):
                case nameof(ModifierTriggers.playerCountLesserEquals):
                case nameof(ModifierTriggers.playerCountGreaterEquals):
                case nameof(ModifierTriggers.playerCountLesser):
                case nameof(ModifierTriggers.playerCountGreater):
                case nameof(ModifierTriggers.playerHealthEquals):
                case nameof(ModifierTriggers.playerHealthLesserEquals):
                case nameof(ModifierTriggers.playerHealthGreaterEquals):
                case nameof(ModifierTriggers.playerHealthLesser):
                case nameof(ModifierTriggers.playerHealthGreater):
                case nameof(ModifierTriggers.playerDeathsEquals):
                case nameof(ModifierTriggers.playerDeathsLesserEquals):
                case nameof(ModifierTriggers.playerDeathsGreaterEquals):
                case nameof(ModifierTriggers.playerDeathsLesser):
                case nameof(ModifierTriggers.playerDeathsGreater):
                case nameof(ModifierTriggers.variableEquals):
                case nameof(ModifierTriggers.variableLesserEquals):
                case nameof(ModifierTriggers.variableGreaterEquals):
                case nameof(ModifierTriggers.variableLesser):
                case nameof(ModifierTriggers.variableGreater):
                case nameof(ModifierTriggers.variableOtherEquals):
                case nameof(ModifierTriggers.variableOtherLesserEquals):
                case nameof(ModifierTriggers.variableOtherGreaterEquals):
                case nameof(ModifierTriggers.variableOtherLesser):
                case nameof(ModifierTriggers.variableOtherGreater):
                case nameof(ModifierTriggers.playerBoostEquals):
                case nameof(ModifierTriggers.playerBoostLesserEquals):
                case nameof(ModifierTriggers.playerBoostGreaterEquals):
                case nameof(ModifierTriggers.playerBoostLesser):
                case nameof(ModifierTriggers.playerBoostGreater):
                case nameof(ModifierTriggers.realTimeSecondEquals):
                case nameof(ModifierTriggers.realTimeSecondLesserEquals):
                case nameof(ModifierTriggers.realTimeSecondGreaterEquals):
                case nameof(ModifierTriggers.realTimeSecondLesser):
                case nameof(ModifierTriggers.realTimeSecondGreater):
                case nameof(ModifierTriggers.realTimeMinuteEquals):
                case nameof(ModifierTriggers.realTimeMinuteLesserEquals):
                case nameof(ModifierTriggers.realTimeMinuteGreaterEquals):
                case nameof(ModifierTriggers.realTimeMinuteLesser):
                case nameof(ModifierTriggers.realTimeMinuteGreater):
                case nameof(ModifierTriggers.realTime12HourEquals):
                case nameof(ModifierTriggers.realTime12HourLesserEquals):
                case nameof(ModifierTriggers.realTime12HourGreaterEquals):
                case nameof(ModifierTriggers.realTime12HourLesser):
                case nameof(ModifierTriggers.realTime12HourGreater):
                case nameof(ModifierTriggers.realTime24HourEquals):
                case nameof(ModifierTriggers.realTime24HourLesserEquals):
                case nameof(ModifierTriggers.realTime24HourGreaterEquals):
                case nameof(ModifierTriggers.realTime24HourLesser):
                case nameof(ModifierTriggers.realTime24HourGreater):
                case nameof(ModifierTriggers.realTimeDayEquals):
                case nameof(ModifierTriggers.realTimeDayLesserEquals):
                case nameof(ModifierTriggers.realTimeDayGreaterEquals):
                case nameof(ModifierTriggers.realTimeDayLesser):
                case nameof(ModifierTriggers.realTimeDayGreater):
                case nameof(ModifierTriggers.realTimeMonthEquals):
                case nameof(ModifierTriggers.realTimeMonthLesserEquals):
                case nameof(ModifierTriggers.realTimeMonthGreaterEquals):
                case nameof(ModifierTriggers.realTimeMonthLesser):
                case nameof(ModifierTriggers.realTimeMonthGreater):
                case nameof(ModifierTriggers.realTimeYearEquals):
                case nameof(ModifierTriggers.realTimeYearLesserEquals):
                case nameof(ModifierTriggers.realTimeYearGreaterEquals):
                case nameof(ModifierTriggers.realTimeYearLesser):
                case nameof(ModifierTriggers.realTimeYearGreater): {
                        var isGroup = cmd.Contains("variableOther") || cmd == "setAlphaOther" || cmd == "removeTextOther" || cmd == "removeTextOtherAt";
                        if (isGroup)
                            PrefabGroupOnly(modifier, layout);
                        IntegerGenerator(modifier, layout, "Value", 0, 0);

                        if (isGroup)
                        {
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }

                #endregion

                #region Key

                case nameof(ModifierTriggers.keyPressDown):
                case nameof(ModifierTriggers.keyPress):
                case nameof(ModifierTriggers.keyPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<KeyCode>();
                        DropdownGenerator(modifier, layout, "Key", 0, dropdownData.Key, dropdownData.Value);

                        break;
                    }

                case nameof(ModifierTriggers.controlPressDown):
                case nameof(ModifierTriggers.controlPress):
                case nameof(ModifierTriggers.controlPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<PlayerInputControlType>();
                        DropdownGenerator(modifier, layout, "Button", 0, dropdownData.Key, dropdownData.Value);

                        break;
                    }

                #endregion

                #region Save / Load JSON

                case nameof(ModifierTriggers.loadEquals):
                case nameof(ModifierTriggers.loadLesserEquals):
                case nameof(ModifierTriggers.loadGreaterEquals):
                case nameof(ModifierTriggers.loadLesser):
                case nameof(ModifierTriggers.loadGreater):
                case nameof(ModifierTriggers.loadExists): {
                        if (cmd == "loadEquals" && modifier.commands.Count < 5)
                            modifier.commands.Add("0");

                        if (cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 0 && !float.TryParse(modifier.value, out float abcdef))
                            modifier.value = "0";

                        StringGenerator(modifier, layout, "Path", 1);
                        StringGenerator(modifier, layout, "JSON 1", 2);
                        StringGenerator(modifier, layout, "JSON 2", 3);

                        if (cmd != "saveVariable" && cmd != "saveText" && cmd != "loadExists" && cmd != "saveString" && (cmd != "loadEquals" || Parser.TryParse(modifier.commands[4], 0) == 0))
                            SingleGenerator(modifier, layout, "Value", 0, 0f);

                        if (cmd == "saveString" || cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 1)
                            StringGenerator(modifier, layout, "Value", 0);

                        if (cmd == "loadEquals")
                            DropdownGenerator(modifier, layout, "Type", 4, CoreHelper.StringToOptionData("Number", "Text"));

                        break;
                    }

                #endregion

                #region Signal

                case nameof(ModifierTriggers.mouseOverSignalModifier): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, layout, "Delay", 0, 0f);

                        MessageGenerator(layout, ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }

                #endregion

                #region Random

                case nameof(ModifierTriggers.randomGreater):
                case nameof(ModifierTriggers.randomLesser):
                case nameof(ModifierTriggers.randomEquals): {
                        IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                        IntegerGenerator(modifier, layout, "Maximum", 2, 0);
                        IntegerGenerator(modifier, layout, "Compare To", 0, 0);

                        break;
                    }

                #endregion

                #region Animate

                case nameof(ModifierTriggers.axisEquals):
                case nameof(ModifierTriggers.axisLesserEquals):
                case nameof(ModifierTriggers.axisGreaterEquals):
                case nameof(ModifierTriggers.axisLesser):
                case nameof(ModifierTriggers.axisGreater): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, layout, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, layout, "Delay", 3, 0f);

                        SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                        SingleGenerator(modifier, layout, "Offset", 5, 0f);
                        SingleGenerator(modifier, layout, "Min", 6, -99999f);
                        SingleGenerator(modifier, layout, "Max", 7, 99999f);
                        SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                        BoolGenerator(modifier, layout, "Use Visual", 9, false);

                        SingleGenerator(modifier, layout, "Equals", 8, 1f);

                        break;
                    }

                #endregion

                #region Level Rank

                case nameof(ModifierTriggers.levelRankEquals):
                case nameof(ModifierTriggers.levelRankLesserEquals):
                case nameof(ModifierTriggers.levelRankGreaterEquals):
                case nameof(ModifierTriggers.levelRankLesser):
                case nameof(ModifierTriggers.levelRankGreater):
                case nameof(ModifierTriggers.levelRankOtherEquals):
                case nameof(ModifierTriggers.levelRankOtherLesserEquals):
                case nameof(ModifierTriggers.levelRankOtherGreaterEquals):
                case nameof(ModifierTriggers.levelRankOtherLesser):
                case nameof(ModifierTriggers.levelRankOtherGreater):
                case nameof(ModifierTriggers.levelRankCurrentEquals):
                case nameof(ModifierTriggers.levelRankCurrentLesserEquals):
                case nameof(ModifierTriggers.levelRankCurrentGreaterEquals):
                case nameof(ModifierTriggers.levelRankCurrentLesser):
                case nameof(ModifierTriggers.levelRankCurrentGreater): {
                        if (cmd.Contains("Other"))
                            StringGenerator(modifier, layout, "ID", 1);

                        DropdownGenerator(modifier, layout, "Rank", 0, DataManager.inst.levelRanks.Select(x => x.name).ToList());

                        break;
                    }

                #endregion

                #region Math

                case nameof(ModifierTriggers.mathEquals):
                case nameof(ModifierTriggers.mathLesserEquals):
                case nameof(ModifierTriggers.mathGreaterEquals):
                case nameof(ModifierTriggers.mathLesser):
                case nameof(ModifierTriggers.mathGreater): {
                        StringGenerator(modifier, layout, "First", 0);
                        StringGenerator(modifier, layout, "Second", 1);

                        break;
                    }

                #endregion

                #region Misc

                case nameof(ModifierTriggers.objectAlive):
                case nameof(ModifierTriggers.objectSpawned): {
                        PrefabGroupOnly(modifier, layout);
                        var str = StringGenerator(modifier, layout, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierTriggers.languageEquals): {
                        var options = new List<Dropdown.OptionData>();

                        var languages = Enum.GetValues(typeof(Language));

                        for (int i = 0; i < languages.Length; i++)
                            options.Add(new Dropdown.OptionData(Enum.GetName(typeof(Language), i) ?? "Invalid Value"));

                        DropdownGenerator(modifier, layout, "Language", 0, options);

                        break;
                    }

                #endregion

                #endregion

                #region Dev Only

                case "loadSceneDEVONLY":
                    {
                        StringGenerator(modifier, layout, "Scene", 0);
                        if (modifier.commands.Count > 1)
                            BoolGenerator(modifier, layout, "Show Loading", 1, true);

                        break;
                    }
                case "loadStoryLevelDEVONLY":
                    {
                        IntegerGenerator(modifier, layout, "Chapter", 1, 0);
                        IntegerGenerator(modifier, layout, "Level", 2, 0);
                        BoolGenerator(modifier, layout, "Bonus", 0, false);
                        BoolGenerator(modifier, layout, "Skip Cutscene", 3, false);

                        break;
                    }
                case "storySaveIntVariableDEVONLY":
                case "storySaveIntDEVONLY":
                    {
                        StringGenerator(modifier, layout, "Save", 0);
                        if (cmd == "storySaveIntDEVONLY")
                            IntegerGenerator(modifier, layout, "Value", 1, 0);
                        break;
                    }
                case "storySaveBoolDEVONLY":
                    {
                        StringGenerator(modifier, layout, "Save", 0);
                        BoolGenerator(modifier, layout, "Value", 1, false);
                        break;
                    }
                case "storyLoadIntEqualsDEVONLY":
                case "storyLoadIntLesserEqualsDEVONLY":
                case "storyLoadIntGreaterEqualsDEVONLY":
                case "storyLoadIntLesserDEVONLY":
                case "storyLoadIntGreaterDEVONLY":
                    {
                        StringGenerator(modifier, layout, "Load", 0);
                        IntegerGenerator(modifier, layout, "Default", 1, 0);
                        IntegerGenerator(modifier, layout, "Equals", 2, 0);

                        break;
                    }
                case "storyLoadBoolDEVONLY":
                    {
                        StringGenerator(modifier, layout, "Load", 0);
                        BoolGenerator(modifier, layout, "Default", 1, false);

                        break;
                    }
                case "enableExampleDEVONLY":
                    {
                        BoolGenerator(modifier, layout, "Active", 0, false);
                        break;
                    }

                    #endregion
            }
        }

        #region Generators

        public void PrefabGroupOnly<T>(Modifier<T> modifier, Transform layout)
        {
            var prefabInstance = booleanBar.Duplicate(layout, "Prefab");
            prefabInstance.transform.localScale = Vector3.one;
            var prefabInstanceText = prefabInstance.transform.Find("Text").GetComponent<Text>();
            prefabInstanceText.text = "Prefab Group Only";

            var prefabInstanceToggle = prefabInstance.transform.Find("Toggle").GetComponent<Toggle>();
            prefabInstanceToggle.onValueChanged.ClearAll();
            prefabInstanceToggle.isOn = modifier.prefabInstanceOnly;
            prefabInstanceToggle.onValueChanged.AddListener(_val =>
            {
                modifier.prefabInstanceOnly = _val;
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(prefabInstanceText);
            EditorThemeManager.ApplyToggle(prefabInstanceToggle);

            var groupAlive = booleanBar.Duplicate(layout, "Prefab");
            groupAlive.transform.localScale = Vector3.one;
            var groupAliveText = groupAlive.transform.Find("Text").GetComponent<Text>();
            groupAliveText.text = "Require Group Alive";

            var groupAliveToggle = groupAlive.transform.Find("Toggle").GetComponent<Toggle>();
            groupAliveToggle.onValueChanged.ClearAll();
            groupAliveToggle.isOn = modifier.groupAlive;
            groupAliveToggle.onValueChanged.AddListener(_val =>
            {
                modifier.groupAlive = _val;
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(groupAliveText);
            EditorThemeManager.ApplyToggle(groupAliveToggle);
        }

        public GameObject MessageGenerator(Transform layout, string label)
        {
            var gameObject = stringInput.Duplicate(layout, "group label");
            gameObject.transform.localScale = Vector3.one;
            var groupLabel = gameObject.transform.Find("Text").GetComponent<Text>();
            groupLabel.text = label;
            groupLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            gameObject.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
            Destroy(gameObject.transform.Find("Input").gameObject);
            return gameObject;
        }

        public GameObject NumberGenerator(Transform layout, string label, string text, Action<string> action, out InputField result)
        {
            var single = numberInput.Duplicate(layout, label);
            single.transform.localScale = Vector3.one;
            var labelText = single.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = single.transform.Find("Input").GetComponent<InputField>();
            inputField.onValueChanged.ClearAll();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = text;
            inputField.onValueChanged.AddListener(_val => action?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(inputField);
            var leftButton = single.transform.Find("<").GetComponent<Button>();
            var rightButton = single.transform.Find(">").GetComponent<Button>();
            leftButton.transition = Selectable.Transition.ColorTint;
            rightButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

            var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
            inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
            result = inputField;
            return single;
        }

        public GameObject SingleGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, float defaultValue = 0f, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f)
        {
            var single = NumberGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                if (float.TryParse(_val, out float num))
                    _val = RTMath.ClampZero(num, min, max).ToString();

                modifier.SetValue(type, _val);

                try
                {
                    modifier.Inactive?.Invoke(modifier, null);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtons(inputField, amount, multiply, min, max, single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, amount, multiply, min, max));

            var contextClickable = inputField.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Raw Value", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                        {
                            modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                            if (modifier.reference is BeatmapObject beatmapObject)
                                CoroutineHelper.StartCoroutine(RenderModifiers(beatmapObject));
                            RTEditor.inst.HideNameEditor();
                        });
                    }));
            };

            return single;
        }

        public GameObject IntegerGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, int defaultValue = 0, int amount = 1, int min = 0, int max = 0)
        {
            var single = NumberGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                if (int.TryParse(_val, out int num))
                    _val = RTMath.ClampZero(num, min, max).ToString();

                modifier.SetValue(type, _val);

                try
                {
                    modifier.Inactive?.Invoke(modifier, null);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtonsInt(inputField, amount, min, max, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField, amount, min, max));

            var contextClickable = inputField.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Raw Value", () =>
                    {
                        RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                        {
                            modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                            if (modifier.reference is BeatmapObject beatmapObject)
                                CoroutineHelper.StartCoroutine(RenderModifiers(beatmapObject));
                            RTEditor.inst.HideNameEditor();
                        });
                    }));
            };

            return single;
        }

        public GameObject BoolGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, bool defaultValue = false)
        {
            var global = booleanBar.Duplicate(layout, label);
            global.transform.localScale = Vector3.one;
            var labelText = global.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
            globalToggle.onValueChanged.ClearAll();
            globalToggle.isOn = modifier.GetBool(type, defaultValue);
            globalToggle.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val.ToString());

                try
                {
                    modifier.Inactive?.Invoke(modifier, null);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyToggle(globalToggle);

            var contextClickable = globalToggle.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Raw Value", () =>
                    {
                        RTEditor.inst.folderCreatorName.text = modifier.GetValue(type);
                        RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                        {
                            modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                            if (modifier.reference is BeatmapObject beatmapObject)
                                CoroutineHelper.StartCoroutine(RenderModifiers(beatmapObject));
                            RTEditor.inst.HideNameEditor();
                        });
                    }));
            };

            return global;
        }

        public GameObject StringGenerator(Transform layout, string label, string value, Action<string> onValueChanged, Action<string> onEndEdit = null)
        {
            var path = stringInput.Duplicate(layout, label);
            path.transform.localScale = Vector3.one;
            var labelText = path.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
            pathInputField.onValueChanged.ClearAll();
            pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
            pathInputField.text = value;
            pathInputField.onValueChanged.AddListener(_val => onValueChanged?.Invoke(_val));
            pathInputField.onEndEdit.NewListener(_val => onEndEdit?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(pathInputField);

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(path.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.image.sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.NewListener(() => TextEditor.inst.SetInputField(pathInputField));
            RectValues.Default.AnchoredPosition(154f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);

            return path;
        }

        public GameObject StringGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, Action<string> onEndEdit = null) => StringGenerator(layout, label, modifier.GetValue(type), _val =>
        {
            modifier.SetValue(type, _val);

            try
            {
                modifier.Inactive?.Invoke(modifier, null);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            modifier.active = false;
        }, onEndEdit);

        public GameObject ColorGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, int colorSource = 0)
        {
            var startColorBase = numberInput.Duplicate(layout, label);
            startColorBase.transform.localScale = Vector3.one;

            var labelText = startColorBase.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(startColorBase.transform.Find("Input").gameObject);
            Destroy(startColorBase.transform.Find(">").gameObject);
            Destroy(startColorBase.transform.Find("<").gameObject);

            var startColors = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject.Duplicate(startColorBase.transform, "color");

            if (startColors.TryGetComponent(out GridLayoutGroup scglg))
            {
                scglg.cellSize = new Vector2(16f, 16f);
                scglg.spacing = new Vector2(4.66f, 2.5f);
            }

            startColors.transform.AsRT().sizeDelta = new Vector2(183f, 32f);

            var colorPrefab = startColors.transform.GetChild(0).gameObject;
            colorPrefab.transform.SetParent(transform);
            var colors = colorSource switch
            {
                0 => CoreHelper.CurrentBeatmapTheme.objectColors,
                1 => CoreHelper.CurrentBeatmapTheme.backgroundColors,
                2 => CoreHelper.CurrentBeatmapTheme.effectColors,
                _ => null,
            };

            CoreHelper.DestroyChildren(startColors.transform);

            var toggles = new Toggle[colors.Count];

            for (int i = 0; i < colors.Count; i++)
            {
                var color = colors[i];

                var gameObject = colorPrefab.Duplicate(startColors.transform);
                var toggle = gameObject.GetComponent<Toggle>();
                toggles[i] = toggle;

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.List_Button_1_Normal);

                var contextClickable = toggle.gameObject.GetOrAddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Edit Raw Value", () =>
                        {
                            RTEditor.inst.folderCreatorName.text = modifier.GetValue(type);
                            RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                            {
                                modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                                if (modifier.reference is BeatmapObject beatmapObject)
                                    CoroutineHelper.StartCoroutine(RenderModifiers(beatmapObject));
                                RTEditor.inst.HideNameEditor();
                            });
                        }));
                };
            }

            CoreHelper.Delete(colorPrefab);

            EditorThemeManager.ApplyLightText(labelText);
            SetObjectColors(toggles, type, modifier.GetInt(type, -1), modifier, colors);

            return startColorBase;
        }

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<string> options, Action<int> onSelect = null) => DropdownGenerator(modifier, layout, label, type, options.Select(x => new Dropdown.OptionData(x)).ToList(), null, onSelect);

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<Dropdown.OptionData> options, List<bool> disabledOptions = null, Action<int> onSelect = null)
        {
            var dd = dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

            var hideOptions = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
            if (disabledOptions == null)
                Destroy(hideOptions);
            else
            {
                if (!hideOptions)
                    hideOptions = dd.transform.Find("Dropdown").gameObject.AddComponent<HideDropdownOptions>();

                hideOptions.DisabledOptions = disabledOptions;
                hideOptions.remove = true;
            }

            var dropdown = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.onValueChanged.ClearAll();
            dropdown.options.Clear();
            dropdown.options = options;
            dropdown.value = modifier.GetInt(type, 0);
            dropdown.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val.ToString());
                onSelect?.Invoke(_val);

                try
                {
                    modifier.Inactive?.Invoke(modifier, null);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            });

            if (dropdown.template)
                dropdown.template.sizeDelta = new Vector2(80f, 192f);

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyDropdown(dropdown);

            var contextClickable = dropdown.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Raw Value", () =>
                    {
                        RTEditor.inst.folderCreatorName.text = modifier.GetValue(type);
                        RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                        {
                            modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                            if (modifier.reference is BeatmapObject beatmapObject)
                                CoroutineHelper.StartCoroutine(RenderModifiers(beatmapObject));
                            RTEditor.inst.HideNameEditor();
                        });
                    }));
            };

            return dd;
        }

        public GameObject AddGenerator<T>(Modifier<T> modifier, Transform layout, string text, Action onAdd)
        {
            var baseAdd = Creator.NewUIObject("add", layout);
            baseAdd.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(baseAdd.transform, "add");
            var addText = add.transform.GetChild(0).GetComponent<Text>();
            addText.text = text;
            RectValues.Default.AnchoredPosition(105f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(add.transform.AsRT());

            var addButton = add.GetComponent<Button>();
            addButton.onClick.NewListener(() => onAdd?.Invoke());

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
            return baseAdd;
        }

        GameObject pasteModifier;
        public void PasteGenerator<T>(IModifyable<T> modifyable)
        {
            var isBeatmapObject = modifyable is BeatmapObject;

            var copiedModifiers = GetCopiedModifiers(isBeatmapObject ? ModifierReferenceType.BeatmapObject : ModifierReferenceType.BackgroundObject);

            if (copiedModifiers == null || copiedModifiers.IsEmpty())
                return; 

            var pasteModifier = isBeatmapObject ? this.pasteModifier : RTBackgroundEditor.inst.pasteModifier;

            if (pasteModifier)
                CoreHelper.Destroy(pasteModifier);

            pasteModifier = EditorPrefabHolder.Instance.Function1Button.Duplicate(isBeatmapObject ? content : RTBackgroundEditor.inst.content, "paste modifier");
            pasteModifier.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var buttonStorage = pasteModifier.GetComponent<FunctionButtonStorage>();
            buttonStorage.label.text = "Paste";
            buttonStorage.button.onClick.NewListener(() =>
            {
                modifyable.Modifiers.AddRange(copiedModifiers.Select(x => (x as Modifier<T>).Copy((T)modifyable)));

                if (modifyable is BeatmapObject beatmapObject)
                {
                    StartCoroutine(RenderModifiers(beatmapObject));
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
                }
                if (modifyable is BackgroundObject backgroundObject)
                {
                    StartCoroutine(RTBackgroundEditor.inst.RenderModifiers(backgroundObject));
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.ObjectContext.MODIFIERS);
                }

                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
            });

            TooltipHelper.AssignTooltip(pasteModifier, "Paste Modifier");
            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Paste_Text);

            if (isBeatmapObject)
                this.pasteModifier = pasteModifier;
            else
                RTBackgroundEditor.inst.pasteModifier = pasteModifier;
        }

        #endregion

        #region Default Modifiers

        public ContentPopup DefaultModifiersPopup { get; set; }

        public string searchTerm;
        public int addIndex = -1;
        public void RefreshDefaultModifiersList(BeatmapObject beatmapObject, int addIndex = -1)
        {
            this.addIndex = addIndex;
            var defaultModifiers = ModifiersManager.defaultBeatmapObjectModifiers;

            DefaultModifiersPopup.ClearContent();

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                var defaultModifier = defaultModifiers[i];
                if (!SearchModifier(searchTerm, defaultModifier))
                    continue;

                var name = $"{defaultModifier.Name} ({defaultModifier.type})";

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(DefaultModifiersPopup.Content, name);

                TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {name}");

                var modifierName = gameObject.transform.GetChild(0).GetComponent<Text>();
                modifierName.text = name;

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    var cmd = defaultModifier.Name;
                    if (cmd.Contains("Text") && !cmd.Contains("Other") && beatmapObject.shape != 4)
                    {
                        EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be a Text Object.", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    if (cmd.Contains("Image") && !cmd.Contains("Other") && beatmapObject.shape != 6)
                    {
                        EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be an Image Object.", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var modifier = defaultModifier.Copy(true, beatmapObject);
                    if (addIndex == -1)
                        beatmapObject.modifiers.Add(modifier);
                    else
                        beatmapObject.modifiers.Insert(Mathf.Clamp(addIndex, 0, beatmapObject.modifiers.Count), modifier);
                    ObjectEditor.inst.RenderDialog(beatmapObject);
                    DefaultModifiersPopup.Close();
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.MODIFIERS);
                });

                EditorThemeManager.ApplyLightText(modifierName);
                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            }
        }

        public bool SearchModifier(string searchTerm, ModifierBase modifier) =>
            string.IsNullOrEmpty(searchTerm) ||
            RTString.SearchString(searchTerm, modifier.Name) ||
            searchTerm.ToLower() == "action" && modifier.type == ModifierBase.Type.Action ||
            searchTerm.ToLower() == "trigger" && modifier.type == ModifierBase.Type.Trigger;

        #endregion

        #region UI Part Handlers

        GameObject booleanBar;

        GameObject numberInput;

        GameObject stringInput;

        GameObject dropdownBar;

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
