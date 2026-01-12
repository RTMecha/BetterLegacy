using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the section of an editor dialog where the objects' modifiers can be edited.
    /// </summary>
    public class ModifiersEditorDialog : Exists
    {
        public ModifiersEditorDialog() => showModifiers = EditorConfig.Instance.ShowModifiersDefault.Value;

        #region Values

        /// <summary>
        /// Label of the modifiers editor.
        /// </summary>
        public Text Label { get; set; }

        /// <summary>
        /// Button to copy the modifier block.
        /// </summary>
        public Button CopyButton { get; set; }

        /// <summary>
        /// Button to delete the modifier block.
        /// </summary>
        public Button DeleteButton { get; set; }

        /// <summary>
        /// Name input field of the modifiers editor.
        /// </summary>
        public InputField NameField { get; set; }

        /// <summary>
        /// Text that displays the objects' current integer variable.
        /// </summary>
        public Text IntVariableUI { get; set; }

        /// <summary>
        /// Toggle for if the modifiers list is expanded.
        /// </summary>
        public Toggle ActiveToggle { get; set; }

        /// <summary>
        /// Toggle for if the object's lifespan is ignored when activating modifiers.
        /// </summary>
        public Toggle IgnoreToggle { get; set; }

        /// <summary>
        /// Toggle for if the modifiers' order matters.
        /// </summary>
        public Toggle OrderToggle { get; set; }

        /// <summary>
        /// Content parent of the UI.
        /// </summary>
        public Transform Content { get; set; }

        /// <summary>
        /// Base scroll view of the UI.
        /// </summary>
        public Transform ScrollView { get; set; }

        /// <summary>
        /// Scrollbar of the UI.
        /// </summary>
        public Scrollbar Scrollbar { get; set; }

        /// <summary>
        /// If modifiers should be displayed.
        /// </summary>
        public bool showModifiers;

        /// <summary>
        /// List of modifier cards.
        /// </summary>
        public List<ModifierCard> modifierCards = new List<ModifierCard>();

        /// <summary>
        /// Cached paste modifier button.
        /// </summary>
        public GameObject pasteModifier;

        /// <summary>
        /// Function to run when the modifiers list is toggled on.
        /// </summary>
        public Action<bool> showModifiersFunc;

        public Func<IModifierReference> getReference;

        #endregion

        #region Functions

        public void InitLabel(Transform parent)
        {
            var labels = new Labels(Labels.InitSettings.Default.Parent(parent).Name("label").ApplyThemes(false), "Modifiers");
            Label = labels.GameObject.transform.GetChild(0).GetComponent<Text>();
            EditorThemeManager.ApplyLightText(Label);
        }

        public void InitCopy(Transform parent)
        {
            var copy = EditorPrefabHolder.Instance.DeleteButton.Duplicate(parent, "copy");
            var copyStorage = copy.GetComponent<DeleteButtonStorage>();
            copyStorage.Sprite = EditorSprites.CopySprite;

            CopyButton = copyStorage.button;
            EditorThemeManager.ApplyGraphic(copyStorage.baseImage, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(copyStorage.image, ThemeGroup.Copy_Text);
        }

        public void InitDelete(Transform parent)
        {
            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(parent, "delete");
            var deleteStorage = delete.GetComponent<DeleteButtonStorage>();

            DeleteButton = deleteStorage.button;
            EditorThemeManager.ApplyDeleteButton(deleteStorage);
        }

        public void InitName(Transform parent)
        {
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(parent, "block name");
            NameField = name.GetComponent<InputField>();
            EditorThemeManager.ApplyInputField(NameField);
        }

        public void InitIntegerVariable(Transform parent)
        {
            var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, "int_variable");

            IntVariableUI = label.transform.GetChild(0).GetComponent<Text>();
            IntVariableUI.text = "Integer Variable: [ null ]";
            IntVariableUI.fontSize = 18;
            EditorThemeManager.ApplyLightText(IntVariableUI);
            label.AddComponent<Image>().color = LSColors.transparent;
            TooltipHelper.AssignTooltip(label, "Modifiers Integer Variable");
        }

        public void InitIgnoreLifespan(Transform parent)
        {
            var ignoreLifespan = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "ignore life");
            var ignoreLifespanToggleButton = ignoreLifespan.GetComponent<ToggleButtonStorage>();
            ignoreLifespanToggleButton.label.text = "Ignore Lifespan";

            IgnoreToggle = ignoreLifespanToggleButton.toggle;

            EditorThemeManager.ApplyToggle(IgnoreToggle, graphic: ignoreLifespanToggleButton.label);
            TooltipHelper.AssignTooltip(ignoreLifespan, "Modifiers Ignore Lifespan");
        }

        public void InitOrderMatters(Transform parent)
        {
            var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "order modifiers");
            var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
            orderMattersToggleButton.label.text = "Order Matters";

            OrderToggle = orderMattersToggleButton.toggle;

            EditorThemeManager.ApplyToggle(OrderToggle, graphic: orderMattersToggleButton.label);
            TooltipHelper.AssignTooltip(orderMatters, "Modifiers Order Matters");
        }

        public void InitActive(Transform parent)
        {
            var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "active");
            var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
            showModifiersToggleButton.label.text = "Show Modifiers";

            ActiveToggle = showModifiersToggleButton.toggle;

            EditorThemeManager.ApplyToggle(ActiveToggle, graphic: showModifiersToggleButton.label);
            TooltipHelper.AssignTooltip(showModifiers, "Show Modifiers");
        }

        public void InitScrollView(Transform parent)
        {
            var scrollObj = EditorPrefabHolder.Instance.ScrollView.Duplicate(parent, "Modifiers Scroll View");

            ScrollView = scrollObj.transform;

            ScrollView.localScale = Vector3.one;

            Content = ScrollView.Find("Viewport/Content");

            ScrollView.gameObject.SetActive(showModifiers);
            Scrollbar = ScrollView.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
        }

        /// <summary>
        /// Initializes the Modifier Editor UI.
        /// </summary>
        /// <param name="parent">Parent to set the Modifier Editor UI to.</param>
        public void Init(Transform parent, bool doLabel = true, bool doIntegerVariable = true, bool doIgnoreLifespan = true, bool doName = false)
        {
            if (doLabel)
                InitLabel(parent);

            if (doName)
                InitName(parent);

            if (doIntegerVariable)
                InitIntegerVariable(parent);

            if (doIgnoreLifespan)
                InitIgnoreLifespan(parent);

            InitOrderMatters(parent);
            InitActive(parent);
            InitScrollView(parent);
        }

        /// <summary>
        /// Renders the modifier list.
        /// </summary>
        /// <typeparam name="T">Type of the modifyable object.</typeparam>
        /// <param name="modifyable">Object that is modifyable.</param>
        public IEnumerator RenderModifiers(IModifyable modifyable)
        {
            if (modifyable == null)
                yield break;

            if (Label)
                Label.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (IntVariableUI)
                IntVariableUI.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (IgnoreToggle)
                IgnoreToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (OrderToggle)
                OrderToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (NameField)
            {
                NameField.gameObject.SetActive(RTEditor.ShowModdedUI);
                if (modifyable is ModifierBlock modifierBlock)
                {
                    NameField.SetTextWithoutNotify(modifierBlock.Name);
                    NameField.onValueChanged.NewListener(_val => modifierBlock.Name = _val);
                }
            }

            if (!RTEditor.ShowModdedUI)
                showModifiers = false;

            ScrollView.gameObject.SetActive(showModifiers);

            if (ActiveToggle)
            {
                ActiveToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
                ActiveToggle.SetIsOnWithoutNotify(showModifiers);
                ActiveToggle.onValueChanged.NewListener(_val =>
                {
                    showModifiers = _val;
                    CoroutineHelper.StartCoroutine(RenderModifiers(modifyable));
                    showModifiersFunc?.Invoke(_val);
                });
            }

            if (!RTEditor.ShowModdedUI)
                yield break;

            if (IgnoreToggle)
            {
                IgnoreToggle.SetIsOnWithoutNotify(modifyable.IgnoreLifespan);
                IgnoreToggle.onValueChanged.NewListener(_val =>
                {
                    modifyable.IgnoreLifespan = _val;
                    if (modifyable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                    if (modifyable is BackgroundObject backgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.MODIFIERS);
                    if (modifyable is PrefabObject prefabObject)
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.MODIFIERS);
                });
            }

            if (OrderToggle)
            {
                OrderToggle.SetIsOnWithoutNotify(modifyable.OrderModifiers);
                OrderToggle.onValueChanged.NewListener(_val =>
                {
                    modifyable.OrderModifiers = _val;
                    if (modifyable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                    if (modifyable is BackgroundObject backgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.MODIFIERS);
                });
            }

            if (!showModifiers)
                yield break;

            var value = Scrollbar ? Scrollbar.value : 0f;

            LSHelpers.DeleteChildren(Content);
            modifierCards.Clear();

            Content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 500f);

            int num = 0;
            bool inCollapsedRegion = false;
            int subRegion = 0;
            foreach (var modifier in modifyable.Modifiers)
            {
                int index = num;
                var modifierCard = new ModifierCard(modifier, index, inCollapsedRegion, this);
                modifierCards.Add(modifierCard);
                modifierCard.RenderModifier(modifyable);
                if (modifier.Name == "endregion")
                {
                    if (subRegion > 0)
                        subRegion--;
                    if (subRegion == 0)
                        inCollapsedRegion = false;
                }

                if (modifier.Name == "region")
                {
                    if (modifier.collapse)
                        inCollapsedRegion = true;
                    subRegion++;
                }
                num++;
            }

            // Add Modifier
            {
                var gameObject = ModifiersEditor.inst.modifierAddPrefab.Duplicate(Content, "add modifier");
                TooltipHelper.AssignTooltip(gameObject, "Add Modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, dialog: this));

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            ModifiersEditor.inst.PasteGenerator(modifyable, this);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content.AsRT());

            CoroutineHelper.PerformAtNextFrame(() =>
            {
                if (Scrollbar)
                    Scrollbar.value = value;
            });

            yield break;
        }

        /// <summary>
        /// Ticks the Modifier Editor Dialog.
        /// </summary>
        public void Tick()
        {
            try
            {
                if (!RTEditor.ShowModdedUI)
                    return;

                if (getReference == null)
                    return;

                var reference = getReference.Invoke();
                if (reference == null)
                    return;

                if (IntVariableUI)
                    IntVariableUI.text = $"Integer Variable: [ {reference.IntVariable} ]";

                for (int i = 0; i < modifierCards.Count; i++)
                    modifierCards[i].Tick(reference);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Clears the modifiers editor.
        /// </summary>
        public void Clear()
        {
            if (Label)
                CoreHelper.Delete(Label.transform.parent);
            CoreHelper.Delete(NameField);
            CoreHelper.Delete(IntVariableUI);
            CoreHelper.Delete(ActiveToggle);
            CoreHelper.Delete(IgnoreToggle);
            CoreHelper.Delete(OrderToggle);
            CoreHelper.Delete(ScrollView);
            modifierCards.Clear();
            showModifiersFunc = null;
        }

        #endregion
    }
}
