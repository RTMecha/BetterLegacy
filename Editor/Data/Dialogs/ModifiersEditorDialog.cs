using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the section of an editor dialog where the objects' modifiers can be edited.
    /// </summary>
    public class ModifiersEditorDialog : Exists
    {
        /// <summary>
        /// Label of the modifiers editor.
        /// </summary>
        public Text Label { get; set; }

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
        /// Initializes the Modifier Editor UI.
        /// </summary>
        /// <param name="parent">Parent to set the Modifier Editor UI to.</param>
        public void Init(Transform parent, bool doLabel = true, bool doIntegerVariable = true, bool doIgnoreLifespan = true)
        {
            // Label
            if (doLabel)
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, "label");

                Label = label.transform.GetChild(0).GetComponent<Text>();
                Label.text = "Modifiers";
                EditorThemeManager.AddLightText(Label);
            }

            // Integer variable
            if (doIntegerVariable)
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, "int_variable");

                IntVariableUI = label.transform.GetChild(0).GetComponent<Text>();
                IntVariableUI.text = "Integer Variable: [ null ]";
                IntVariableUI.fontSize = 18;
                EditorThemeManager.AddLightText(IntVariableUI);
                label.AddComponent<Image>().color = LSColors.transparent;
                TooltipHelper.AssignTooltip(label, "Modifiers Integer Variable");
            }

            // Ignored Lifespan
            if (doIgnoreLifespan)
            {
                var ignoreLifespan = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "ignore life");
                var ignoreLifespanToggleButton = ignoreLifespan.GetComponent<ToggleButtonStorage>();
                ignoreLifespanToggleButton.label.text = "Ignore Lifespan";

                IgnoreToggle = ignoreLifespanToggleButton.toggle;

                EditorThemeManager.AddToggle(IgnoreToggle, graphic: ignoreLifespanToggleButton.label);
                TooltipHelper.AssignTooltip(ignoreLifespan, "Modifiers Ignore Lifespan");
            }

            // Order Modifiers
            {
                var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "order modifiers");
                var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
                orderMattersToggleButton.label.text = "Order Matters";

                OrderToggle = orderMattersToggleButton.toggle;

                EditorThemeManager.AddToggle(OrderToggle, graphic: orderMattersToggleButton.label);
                TooltipHelper.AssignTooltip(orderMatters, "Modifiers Order Matters");
            }

            // Active
            {
                var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "active");
                var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
                showModifiersToggleButton.label.text = "Show Modifiers";

                ActiveToggle = showModifiersToggleButton.toggle;

                EditorThemeManager.AddToggle(ActiveToggle, graphic: showModifiersToggleButton.label);
                TooltipHelper.AssignTooltip(showModifiers, "Show Modifiers");
            }

            var scrollObj = EditorPrefabHolder.Instance.ScrollView.Duplicate(parent, "Modifiers Scroll View");

            ScrollView = scrollObj.transform;

            ScrollView.localScale = Vector3.one;

            Content = ScrollView.Find("Viewport/Content");

            ScrollView.gameObject.SetActive(showModifiers);
            Scrollbar = ScrollView.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
        }

        /// <summary>
        /// Renders the modifier list.
        /// </summary>
        /// <typeparam name="T">Type of the modifyable object.</typeparam>
        /// <param name="modifyable">Object that is modifyable.</param>
        public IEnumerator RenderModifiers(IModifyable modifyable)
        {
            if (Label)
                Label.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (IntVariableUI)
                IntVariableUI.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (IgnoreToggle)
                IgnoreToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            if (OrderToggle)
                OrderToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

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
            foreach (var modifier in modifyable.Modifiers)
            {
                int index = num;
                var modifierCard = new ModifierCard(modifier, index, this);
                modifierCards.Add(modifierCard);
                modifierCard.RenderModifier(modifyable);
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

                if (EditorTimeline.inst.CurrentSelection.TryGetData(out IModifierReference reference))
                    IntVariableUI.text = $"Integer Variable: [ {reference.IntVariable} ]";
            }
            catch
            {

            }
        }
    }
}
