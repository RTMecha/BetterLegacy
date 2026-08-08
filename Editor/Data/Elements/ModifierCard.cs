using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Represents a modifier in the editor.
    /// </summary>
    public class ModifierCard : Exists, ISelectable
    {
        #region Constructors

        public ModifierCard(Modifier modifier)
        {
            Modifier = modifier;
            Modifier.card = this;
        }

        public ModifierCard(Modifier modifier, int index, bool inCollapsedRegion, ModifiersEditorDialog dialog)
        {
            Modifier = modifier;
            Modifier.card = this;
            this.index = index;
            this.inCollapsedRegion = inCollapsedRegion;
            this.dialog = dialog;
        }

        #endregion

        #region Values

        /// <summary>
        /// Modifier reference.
        /// </summary>
        public Modifier Modifier { get; set; }

        /// <summary>
        /// Unity Game Object of the Modifier Card.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Value layout parent.
        /// </summary>
        public Transform layout;

        /// <summary>
        /// Index of the modifier.
        /// </summary>
        public int index;

        /// <summary>
        /// If the modifier is in a collapsed region.
        /// </summary>
        public bool inCollapsedRegion;

        /// <summary>
        /// Parent dialog reference.
        /// </summary>
        public ModifiersEditorDialog dialog;

        /// <summary>
        /// List of values to update.
        /// </summary>
        public List<Value> values = new List<Value>();

        /// <summary>
        /// Value of the parent dialogs' scrollbar.
        /// </summary>
        public float DialogScrollbarValue
        {
            get => dialog && dialog.Scrollbar ? dialog.Scrollbar.value : 0f;
            set
            {
                if (dialog && dialog.Scrollbar)
                    dialog.Scrollbar.value = value;
            }
        }

        public bool Selected { get; set; }

        #endregion

        #region Functions

        /// <summary>
        /// Updates the modifier card per-frame.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        public void Tick(IModifierReference reference)
        {
            if (reference == default)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                var value = values[i];
                value.Tick(this, reference);
            }
        }

        /// <summary>
        /// Renders the modifier card.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        public void RenderModifier(IModifierReference reference)
        {
            if (reference is IModifyable modifyable)
                RenderModifier(reference, modifyable);
        }

        /// <summary>
        /// Renders the modifier card.
        /// </summary>
        /// <param name="modifyable">Object reference.</param>
        public void RenderModifier(IModifyable modifyable)
        {
            if (modifyable is IModifierReference reference)
                RenderModifier(reference, modifyable);
        }

        /// <summary>
        /// Renders the modifier card.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        /// <param name="modifyable">Object reference.</param>
        public void RenderModifier(IModifierReference reference, IModifyable modifyable)
        {
            var modifier = Modifier;
            if (!modifier || reference == default || modifyable == default)
                return;

            if (!dialog)
                return;

            values.Clear();

            var name = modifier.Name;
            var content = dialog.Content;
            var scrollbar = dialog.Scrollbar;

            var gameObject = this.gameObject;

            if (gameObject)
                CoreHelper.Delete(gameObject);

            gameObject = ModifiersEditor.inst.modifierCardPrefab.Duplicate(content, name, index);
            this.gameObject = gameObject;
            gameObject.SetActive(!inCollapsedRegion);

            if (inCollapsedRegion)
                return;

            if (!string.IsNullOrEmpty(modifier.description))
                TooltipHelper.AddHoverTooltip(gameObject, modifier.DisplayName, modifier.description);

            TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {(name + " (" + modifier.type.ToString() + ")")}");
            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

            gameObject.transform.localScale = Vector3.one;
            var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
            modifierTitle.text = modifier.DisplayName;
            EditorThemeManager.ApplyLightText(modifierTitle);

            var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
            collapse.interactable = name != "endregion";
            collapse.SetIsOnWithoutNotify(modifier.collapse);
            collapse.onValueChanged.NewListener(_val => Collapse(_val, reference));

            TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");
            EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

            for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => Delete(reference));

            TooltipHelper.AssignTooltip(delete.gameObject, "Delete Modifier");
            EditorThemeManager.ApplyDeleteButton(delete);

            var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
            copy.OnClick.NewListener(() => Copy(reference));

            TooltipHelper.AssignTooltip(copy.gameObject, "Copy Modifier");
            EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

            var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
            notifier.modifier = modifier;
            notifier.notifier = gameObject.transform.Find("Label/Notifier").gameObject.GetComponent<Image>();
            TooltipHelper.AssignTooltip(notifier.notifier.gameObject, "Notifier Modifier");
            EditorThemeManager.ApplyGraphic(notifier.notifier, ThemeGroup.Warning_Confirm, true);

            gameObject.AddComponent<Button>();
            var buttonFunctions = new List<EditorElement>()
            {
                new ButtonElement("Add", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, dialog: dialog)),
                new ButtonElement("Add Above", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, index, dialog)),
                new ButtonElement("Add Below", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, index + 1, dialog)),
                new ButtonElement("Delete", () => Delete(reference)),
                new SpacerElement(),
                new ButtonElement("Copy", () => Copy(reference)),
                new ButtonElement("Copy All", () =>
                {
                    var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                    if (copiedModifiers == null)
                        return;
                    copiedModifiers.Clear();
                    copiedModifiers.AddRange(modifyable.Modifiers.Select(x => x.Copy()));

                    ModifiersEditor.inst.PasteGenerator(modifyable, dialog);
                    EditorManager.inst.DisplayNotification("Copied Modifiers!", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste", () =>
                {
                    var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                    if (copiedModifiers == null || copiedModifiers.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                        return;
                    }

                    modifyable.Modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));

                    if (modifyable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                    if (modifyable is BackgroundObject backgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, ObjectContext.MODIFIERS);

                    EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste Above", () =>
                {
                    var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                    if (copiedModifiers == null || copiedModifiers.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                        return;
                    }

                    modifyable.Modifiers.InsertRange(index, copiedModifiers.Select(x => x.Copy()));

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));

                    if (modifyable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                    if (modifyable is BackgroundObject backgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, ObjectContext.MODIFIERS);

                    EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Paste Below", () =>
                {
                    var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                    if (copiedModifiers == null || copiedModifiers.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                        return;
                    }

                    modifyable.Modifiers.InsertRange(index + 1, copiedModifiers.Select(x => x.Copy()));

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));

                    if (modifyable is BeatmapObject beatmapObject)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                    if (modifyable is BackgroundObject backgroundObject)
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, ObjectContext.MODIFIERS);

                    EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                }),
                new SpacerElement(),
                new ButtonElement("Sort Modifiers", () =>
                {
                    modifyable.Modifiers = modifyable.Modifiers.OrderBy(x => x.type == Modifier.Type.Action).ToList();

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                }, shouldGenerate: () => !modifyable.OrderModifiers),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(modifyable.Modifiers, index, () => CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable))));

            buttonFunctions.AddRange(new List<EditorElement>()
            {
                new SpacerElement(),
                new ButtonElement("Update Modifier", () => Update(modifier, reference)),
                new SpacerElement(),
                new ButtonElement(modifier.collapse ? "Uncollapse" : "Collapse", () => Collapse(!modifier.collapse, reference), shouldGenerate: () => name != "endregion"),
                new ButtonElement("Collapse All", () =>
                {
                    foreach (var mod in modifyable.Modifiers)
                    {
                        if (mod.Name != "endregion")
                            mod.collapse = true;
                    }

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                }),
                new ButtonElement("Uncollapse All", () =>
                {
                    foreach (var mod in modifyable.Modifiers)
                    {
                        if (mod.Name != "endregion")
                            mod.collapse = false;
                    }

                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                }),
                new SpacerElement(),
                new ButtonElement("Set Custom Name", () => RTEditor.inst.ShowNameEditor("Set Custom Name", "Custom name", string.IsNullOrEmpty(modifier.customName) ? "modifierName" : modifier.customName, "Set", () =>
                {
                    modifier.customName = RTEditor.inst.folderCreatorName.text;
                    RenderModifier(reference);
                    RTEditor.inst.HideNameEditor();
                })),
                new ButtonElement("Set Description", () => RTEditor.inst.ShowNameEditor("Set Description", "Description", string.IsNullOrEmpty(modifier.description) ? "This modifier does..." : modifier.description, "Set", () =>
                {
                    modifier.description = RTEditor.inst.folderCreatorName.text;
                    RenderModifier(reference);
                    RTEditor.inst.HideNameEditor();
                })),
                new SpacerElement(() => ModCompatibility.UnityExplorerInstalled),
                new ButtonElement("Inspect", () => ModCompatibility.Inspect(modifier), shouldGenerate: () => ModCompatibility.UnityExplorerInstalled),
            });

            EditorContextMenu.AddContextMenu(gameObject, buttonFunctions);

            if (modifier.collapse)
                return;

            layout = gameObject.transform.Find("Layout");

            if (modifier.function && !modifier.function.IsEditorModifier)
            {
                var constant = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Constant");
                constant.transform.localScale = Vector3.one;

                var constantText = constant.transform.Find("Text").GetComponent<Text>();
                constantText.text = "Constant";

                var constantToggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                constantToggle.SetIsOnWithoutNotify(modifier.constant);
                constantToggle.onValueChanged.NewListener(_val =>
                {
                    modifier.constant = _val;
                    Update(modifier, reference);
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
                        modifier.RunInactive(modifier, reference as IModifierReference);
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
            }

            if (modifier.function && name != "else" && modifier.type == Modifier.Type.Trigger)
            {
                var not = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Not");
                not.transform.localScale = Vector3.one;
                var notText = not.transform.Find("Text").GetComponent<Text>();
                notText.text = "Not";

                var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                notToggle.SetIsOnWithoutNotify(modifier.not);
                notToggle.onValueChanged.NewListener(_val =>
                {
                    modifier.not = _val;
                    Update(modifier, reference);
                });

                TooltipHelper.AssignTooltip(notToggle.gameObject, "Trigger Not Modifier");
                EditorThemeManager.ApplyLightText(notText);
                EditorThemeManager.ApplyToggle(notToggle);

                var elseIf = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Not");
                elseIf.transform.localScale = Vector3.one;
                var elseIfText = elseIf.transform.Find("Text").GetComponent<Text>();
                elseIfText.text = "Else If";

                var elseIfToggle = elseIf.transform.Find("Toggle").GetComponent<Toggle>();
                elseIfToggle.SetIsOnWithoutNotify(modifier.elseIf);
                elseIfToggle.onValueChanged.NewListener(_val =>
                {
                    modifier.elseIf = _val;
                    Update(modifier, reference);
                });

                TooltipHelper.AssignTooltip(elseIfToggle.gameObject, "Trigger Else If Modifier");
                EditorThemeManager.ApplyLightText(elseIfText);
                EditorThemeManager.ApplyToggle(elseIfToggle);
            }

            if (string.IsNullOrEmpty(name))
            {
                EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            modifier.function?.RenderModifierCard(modifier, this, reference, modifyable);
        }

        #region Functions

        /// <summary>
        /// Sets the collapse state of the modifier card.
        /// </summary>
        /// <param name="collapse">Collapse state to set.</param>
        /// <param name="reference">Object reference.</param>
        public void Collapse(bool collapse, IModifierReference reference)
        {
            if (Modifier.Name == "endregion")
                return;

            Modifier.collapse = collapse;
            if (Modifier.Name == "region")
            {
                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(reference as IModifyable));
                return;
            }

            RenderModifier(reference);
            CoroutineHelper.PerformAtEndOfFrame(() => LayoutRebuilder.ForceRebuildLayoutImmediate(dialog.Content.AsRT()));
        }

        /// <summary>
        /// Deletes the modifier.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        public void Delete(IModifierReference reference)
        {
            if (reference is not IModifyable modifyable)
                return;

            // remove cache and set inactive state before deleting just in case
            try
            {
                Update(reference);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            modifyable.Modifiers.RemoveAt(index);
            if (Modifier.Name == "region" || Modifier.Name == "endregion")
                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
            else
            {
                CoreHelper.Delete(gameObject);
                dialog.modifierCards.RemoveAt(index);
                for (int i = 0; i < dialog.modifierCards.Count; i++)
                    dialog.modifierCards[i].index = i;
            }

            switch (modifyable.ReferenceType)
            {
                case ModifierReferenceType.BeatmapObject: {
                        var beatmapObject = modifyable as BeatmapObject;
                        beatmapObject.reactivePositionOffset = Vector3.zero;
                        beatmapObject.reactiveScaleOffset = Vector3.zero;
                        beatmapObject.reactiveRotationOffset = 0f;
                        RTLevel.Current?.UpdateObject(beatmapObject);
                        break;
                    }
                case ModifierReferenceType.BackgroundObject: {
                        var backgroundObject = modifyable as BackgroundObject;
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                        break;
                    }
                case ModifierReferenceType.PrefabObject: {
                        var prefabObject = modifyable as PrefabObject;
                        RTLevel.Current?.UpdatePrefab(prefabObject);
                        break;
                    }
            }
        }

        /// <summary>
        /// Copies the modifier.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        public void Copy(IModifierReference reference)
        {
            if (Modifier is not Modifier modifier)
                return;

            if (reference is not IModifyable modifyable)
                return;

            var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
            if (copiedModifiers == null)
                return;
            copiedModifiers.Clear();
            copiedModifiers.Add(modifier.Copy());

            ModifiersEditor.inst.PasteGenerator(modifyable, dialog);
            EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Updates the modifier.
        /// </summary>
        /// <param name="reference">Object reference.</param>
        public void Update(IModifierReference reference) => Update(Modifier, reference);

        /// <summary>
        /// Updates the modifier.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="reference">Object reference.</param>
        public void Update(Modifier modifier, IModifierReference reference)
        {
            if (!modifier)
                return;

            modifier.active = false;
            modifier.runCount = 0;
            modifier.RunInactive(modifier, reference);
            modifier.OnRemoveCache();
            modifier.Result = default;
        }

        /// <summary>
        /// Sets a value at an index.
        /// </summary>
        /// <param name="index">Index of the value to set.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="reference">Object reference.</param>
        public void SetValue(int index, string value, IModifierReference reference)
        {
            Modifier.SetValue(index, value);
            var scrollbar = dialog.Scrollbar;
            var scrollValue = scrollbar ? scrollbar.value : 0f;
            RenderModifier(reference);
            CoroutineHelper.PerformAtNextFrame(() =>
            {
                if (scrollbar)
                    scrollbar.value = scrollValue;
            });
            Update(Modifier, reference);
        }

        #endregion

        #region Generators

        public void PrefabGroupOnly(Modifier modifier, IModifierReference reference)
        {
            var prefabInstance = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Prefab");
            prefabInstance.transform.localScale = Vector3.one;
            var prefabInstanceText = prefabInstance.transform.Find("Text").GetComponent<Text>();
            prefabInstanceText.text = "Prefab Group Only";

            var prefabInstanceToggle = prefabInstance.transform.Find("Toggle").GetComponent<Toggle>();
            prefabInstanceToggle.SetIsOnWithoutNotify(modifier.prefabInstanceOnly);
            prefabInstanceToggle.onValueChanged.NewListener(_val =>
            {
                modifier.prefabInstanceOnly = _val;
                modifier.active = false;
            });

            TooltipHelper.AssignTooltip(prefabInstance, "Prefab Instance Group Modifier");
            EditorThemeManager.ApplyLightText(prefabInstanceText);
            EditorThemeManager.ApplyToggle(prefabInstanceToggle);

            var groupAlive = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Prefab");
            groupAlive.transform.localScale = Vector3.one;
            var groupAliveText = groupAlive.transform.Find("Text").GetComponent<Text>();
            groupAliveText.text = "Require Group Alive";

            var groupAliveToggle = groupAlive.transform.Find("Toggle").GetComponent<Toggle>();
            groupAliveToggle.SetIsOnWithoutNotify(modifier.groupAlive);
            groupAliveToggle.onValueChanged.NewListener(_val =>
            {
                modifier.groupAlive = _val;
                modifier.active = false;
            });

            TooltipHelper.AssignTooltip(groupAlive, "Group Alive Modifier");
            EditorThemeManager.ApplyLightText(groupAliveText);
            EditorThemeManager.ApplyToggle(groupAliveToggle);

            if (reference is PrefabObject)
            {
                var subPrefab = ModifiersEditor.inst.booleanBar.Duplicate(layout, "Sub Prefab");
                subPrefab.transform.localScale = Vector3.one;
                var subPrefabText = subPrefab.transform.Find("Text").GetComponent<Text>();
                subPrefabText.text = "Search in Prefab";

                var subPrefabToggle = subPrefab.transform.Find("Toggle").GetComponent<Toggle>();
                subPrefabToggle.SetIsOnWithoutNotify(modifier.subPrefab);
                subPrefabToggle.onValueChanged.NewListener(_val =>
                {
                    modifier.subPrefab = _val;
                    Update(modifier, reference);
                });

                TooltipHelper.AssignTooltip(subPrefab, "Sub Prefab Modifier");
                EditorThemeManager.ApplyLightText(subPrefabText);
                EditorThemeManager.ApplyToggle(subPrefabToggle);
            }
        }

        public GameObject LabelGenerator(string label)
        {
            var gameObject = ModifiersEditor.inst.stringInput.Duplicate(layout, "group label");
            gameObject.transform.localScale = Vector3.one;
            var groupLabel = gameObject.transform.Find("Text").GetComponent<Text>();
            groupLabel.text = label;
            gameObject.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
            CoreHelper.Delete(gameObject.transform.Find("Input").gameObject);
            return gameObject;
        }

        public GameObject NumberGenerator(Transform layout, string label, string text, Action<string> action, out InputField result)
        {
            var single = ModifiersEditor.inst.numberInput.Duplicate(layout, label);
            single.transform.localScale = Vector3.one;
            var labelText = single.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = single.transform.Find("Input").GetComponent<InputField>();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.SetTextWithoutNotify(text);
            inputField.onValueChanged.NewListener(_val => action?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(inputField);
            var leftButton = single.transform.Find("<").GetComponent<Button>();
            var rightButton = single.transform.Find(">").GetComponent<Button>();
            leftButton.transition = Selectable.Transition.ColorTint;
            rightButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

            TriggerHelper.InversableField(inputField);
            result = inputField;
            return single;
        }

        public GameObject SingleGenerator(Modifier modifier, IModifierReference reference, string label, int type, float defaultValue = 0f, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f)
        {
            var single = NumberGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                if (float.TryParse(_val, out float num))
                    _val = RTMath.ClampZero(num, min, max).ToString();

                modifier.SetValue(type, _val);
                Update(modifier, reference);
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtons(inputField, amount, multiply, min, max, single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, amount, multiply, min, max));

            EditorContextMenu.AddContextMenu(inputField.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));

            values.Add(new StringValue(type, inputField));

            return single;
        }

        public GameObject IntegerGenerator(Modifier modifier, IModifierReference reference, string label, int type, int defaultValue = 0, int amount = 1, int min = 0, int max = 0)
        {
            var single = NumberGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                if (int.TryParse(_val, out int num))
                    _val = RTMath.ClampZero(num, min, max).ToString();

                modifier.SetValue(type, _val);

                Update(modifier, reference);
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtonsInt(inputField, amount, min, max, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField, amount, min, max));

            EditorContextMenu.AddContextMenu(inputField.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));

            values.Add(new StringValue(type, inputField));

            return single;
        }

        public GameObject BoolGenerator(string label, bool value, Action<bool> action) => BoolGenerator(label, value, action, out Toggle toggle);

        public GameObject BoolGenerator(string label, bool value, Action<bool> action, out Toggle toggle)
        {
            var global = ModifiersEditor.inst.booleanBar.Duplicate(layout, label);
            global.transform.localScale = Vector3.one;
            var labelText = global.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
            globalToggle.SetIsOnWithoutNotify(value);
            globalToggle.onValueChanged.NewListener(_val => action?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyToggle(globalToggle);

            toggle = globalToggle;
            return global;
        }

        public GameObject BoolGenerator(Modifier modifier, IModifierReference reference, string label, int type, bool defaultValue = false)
        {
            var gameObject = BoolGenerator(label, modifier.GetBool(type, defaultValue), _val =>
            {
                modifier.SetValue(type, _val.ToString());

                Update(modifier, reference);
            }, out Toggle toggle);
            EditorContextMenu.AddContextMenu(toggle.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));
            values.Add(new BoolValue(type, toggle));

            return gameObject;
        }

        public StringInputElement StringGenerator(Transform layout, string label, string value, Action<string> onValueChanged, Action<string> onEndEdit = null)
        {
            var path = ModifiersEditor.inst.stringInput.Duplicate(layout, label);
            path.transform.localScale = Vector3.one;
            var labelText = path.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = path.transform.Find("Input").GetComponent<InputField>();
            inputField.textComponent.alignment = TextAnchor.MiddleLeft;
            inputField.SetTextWithoutNotify(value);
            inputField.onValueChanged.NewListener(_val => onValueChanged?.Invoke(_val));
            inputField.onEndEdit.NewListener(_val => onEndEdit?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(inputField);

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(path.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.Sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(inputField));
            RectValues.Default.AnchoredPosition(154f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);

            return new StringInputElement
            {
                GameObject = path,
                inputField = inputField,
                labelsElement = new LabelElement() { GameObject = labelText.gameObject, uiText = labelText },
            };
        }

        public StringInputElement GroupFieldGenerator(Modifier modifier, IModifierReference reference, string label, int type, Action<string> onEndEdit = null, bool renderVariables = true)
        {
            var editorElement = StringGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                modifier.SetValue(type, _val);
                Update(modifier, reference);
            }, onEndEdit);
            if (renderVariables)
                values.Add(new StringValue(type, editorElement.inputField));
            EditorContextMenu.AddContextMenu(editorElement.GameObject, EditorContextMenu.GetNameFunctions(editorElement.inputField));
            return editorElement;
        }

        public GameObject StringGenerator(Modifier modifier, IModifierReference reference, string label, int type, Action<string> onEndEdit = null, bool renderVariables = true)
        {
            var editorElement = StringGenerator(layout, label, modifier.GetValue(type), _val =>
            {
                modifier.SetValue(type, _val);
                Update(modifier, reference);
            }, onEndEdit);
            if (renderVariables)
                values.Add(new StringValue(type, editorElement.inputField));
            return editorElement.GameObject;
        }

        public void SetObjectColors(Toggle[] toggles, int index, int currentValue, Modifier modifier, IModifierReference reference, List<Color> colors)
        {
            int num = 0;
            foreach (var toggle in toggles)
            {
                int toggleIndex = num;
                toggle.SetIsOnWithoutNotify(num == currentValue);
                toggle.onValueChanged.NewListener(_val =>
                {
                    modifier.SetValue(index, toggleIndex.ToString());

                    SetObjectColors(toggles, index, toggleIndex, modifier, reference, colors);
                    Update(modifier, reference);
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

        public GameObject ColorGenerator(Modifier modifier, IModifierReference reference, string label, int type, ThemeSource source) => ColorGenerator(modifier, reference, label, type, source switch
        {
            ThemeSource.GUI => new List<Color>() { CoreHelper.CurrentBeatmapTheme.guiColor },
            ThemeSource.Background => new List<Color>() { CoreHelper.CurrentBeatmapTheme.backgroundColor },
            ThemeSource.Player => CoreHelper.CurrentBeatmapTheme.playerColors,
            ThemeSource.PlayerTail => new List<Color>() { CoreHelper.CurrentBeatmapTheme.guiAccentColor },
            ThemeSource.BackgroundObjects => CoreHelper.CurrentBeatmapTheme.backgroundColors,
            ThemeSource.Effects => CoreHelper.CurrentBeatmapTheme.effectColors,
            _ => CoreHelper.CurrentBeatmapTheme.objectColors,
        });

        public GameObject ColorGenerator(Modifier modifier, IModifierReference reference, string label, int type, int colorSource = 0) => ColorGenerator(modifier, reference, label, type, colorSource switch
        {
            0 => CoreHelper.CurrentBeatmapTheme.objectColors,
            1 => CoreHelper.CurrentBeatmapTheme.backgroundColors,
            2 => CoreHelper.CurrentBeatmapTheme.effectColors,
            _ => null,
        });

        public GameObject ColorGenerator(Modifier modifier, IModifierReference reference, string label, int type, List<Color> colors)
        {
            var startColorBase = ModifiersEditor.inst.numberInput.Duplicate(layout, label);
            startColorBase.transform.localScale = Vector3.one;

            var labelText = startColorBase.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            CoreHelper.Delete(startColorBase.transform.Find("Input").gameObject);
            CoreHelper.Delete(startColorBase.transform.Find(">").gameObject);
            CoreHelper.Delete(startColorBase.transform.Find("<").gameObject);

            var startColors = EditorPrefabHolder.Instance.ColorsLayout.Duplicate(startColorBase.transform, "color");
            startColors.SetActive(true);

            if (startColors.TryGetComponent(out GridLayoutGroup scglg))
            {
                scglg.cellSize = new Vector2(16f, 16f);
                scglg.spacing = new Vector2(4.66f, 2.5f);
            }

            startColors.transform.AsRT().sizeDelta = new Vector2(183f, 32f);

            var colorPrefab = startColors.transform.GetChild(0).gameObject;
            colorPrefab.transform.SetParent(ModifiersEditor.inst.transform);

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

                EditorContextMenu.AddContextMenu(toggle.gameObject,
                    new ButtonElement("Edit Raw Value", () =>
                    {
                        RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                        RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                        {
                            modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                            if (reference is IModifyable modifyable)
                                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                            RTEditor.inst.HideNameEditor();
                            Update(modifier, reference);
                        });
                    }));
            }

            CoreHelper.Delete(colorPrefab);

            EditorThemeManager.ApplyLightText(labelText);
            SetObjectColors(toggles, type, modifier.GetInt(type, -1), modifier, reference, colors);

            values.Add(new ColorSlotsValue(type, startColorBase, toggles));

            return startColorBase;
        }

        //public GameObject EaseGenerator<T>(Modifier modifier, T reference, int type) => DropdownGenerator(modifier, reference, "Easing",
        //        () => modifier.GetValue(type),
        //        _val =>
        //        {
        //            modifier.SetValue(type, _val);
        //        },
        //        RTEditor.inst.GetEaseOptions(), null,
        //        _val =>
        //        {
        //            modifier.SetValue(type, Core.Animation.Ease.EaseReferences.GetAtOrDefault(_val, Core.Animation.Ease.EaseReferences[0]).Name);
        //        });
        
        public GameObject EaseGenerator(Modifier modifier, IModifierReference reference, int type)
        {
            var dd = ModifiersEditor.inst.easingBar.Duplicate(layout, "Easing");
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = "Easing";

            CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

            var hideOptions = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
            CoreHelper.Destroy(hideOptions);

            var dropdown = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            RTEditor.inst.SetupEaseDropdown(dropdown);
            dropdown.SetValueWithoutNotify(RTEditor.inst.GetEaseIndex(modifier.GetValue(type)));
            dropdown.onValueChanged.NewListener(_val =>
            {
                modifier.SetValue(type, RTEditor.inst.GetEasing(_val).ToString());

                Update(modifier, reference);
            });

            //if (dropdown.template)
            //    dropdown.template.sizeDelta = new Vector2(80f, 192f);

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyDropdown(dropdown);

            EditorContextMenu.AddContextMenu(dropdown.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));

            values.Add(new DropdownValue(type, dropdown)
            {
                getValue = _val => RTEditor.inst.GetEaseIndex(_val),
            });

            return dd;
        }

        public GameObject DropdownGenerator(Modifier modifier, IModifierReference reference, string label, int type, List<string> options, Action<int> onSelect = null) => DropdownGenerator(modifier, reference, label, type, options.Select(x => new Dropdown.OptionData(x)).ToList(), null, onSelect);

        public GameObject DropdownGenerator(Modifier modifier, IModifierReference reference, string label, int type, List<Dropdown.OptionData> options, Action<int> onSelect = null) => DropdownGenerator(modifier, reference, label, type, options, null, onSelect);

        public GameObject DropdownGenerator(Modifier modifier, IModifierReference reference, string label, int type, List<Dropdown.OptionData> options, List<bool> disabledOptions, Action<int> onSelect = null)
        {
            var dd = ModifiersEditor.inst.dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

            var hideOptions = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
            if (disabledOptions == null)
                CoreHelper.Destroy(hideOptions);
            else
            {
                if (!hideOptions)
                    hideOptions = dd.transform.Find("Dropdown").gameObject.AddComponent<HideDropdownOptions>();

                hideOptions.DisabledOptions = disabledOptions;
                hideOptions.remove = true;
            }

            var dropdown = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.options = options;
            dropdown.SetValueWithoutNotify(modifier.GetInt(type, 0));
            dropdown.onValueChanged.NewListener(_val =>
            {
                if (onSelect == null)
                    modifier.SetValue(type, _val.ToString());
                onSelect?.Invoke(_val);

                Update(modifier, reference);
            });

            if (dropdown.template)
                dropdown.template.sizeDelta = new Vector2(80f, 192f);

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyDropdown(dropdown);

            EditorContextMenu.AddContextMenu(dropdown.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(modifier.GetValue(type));
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        modifier.SetValue(type, RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));

            values.Add(new DropdownValue(type, dropdown));

            return dd;
        }

        public GameObject DropdownGenerator(Modifier modifier, IModifierReference reference, string label, Func<string> getValue, Action<string> setValue, List<Dropdown.OptionData> options, List<bool> disabledOptions)
        {
            var dd = ModifiersEditor.inst.dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

            var hideOptions = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
            if (disabledOptions == null)
                CoreHelper.Destroy(hideOptions);
            else
            {
                if (!hideOptions)
                    hideOptions = dd.transform.Find("Dropdown").gameObject.AddComponent<HideDropdownOptions>();

                hideOptions.DisabledOptions = disabledOptions;
                hideOptions.remove = true;
            }

            var dropdown = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.options = options;
            dropdown.SetValueWithoutNotify(Parser.TryParse(getValue?.Invoke(), 0));
            dropdown.onValueChanged.NewListener(_val =>
            {
                setValue?.Invoke(_val.ToString());
                Update(modifier, reference);
            });

            if (dropdown.template)
                dropdown.template.sizeDelta = new Vector2(80f, 192f);

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyDropdown(dropdown);

            EditorContextMenu.AddContextMenu(dropdown.gameObject,
                new ButtonElement("Edit Raw Value", () =>
                {
                    RTEditor.inst.folderCreatorName.SetTextWithoutNotify(getValue?.Invoke());
                    RTEditor.inst.ShowNameEditor("Field Editor", "Edit Field", "Submit", () =>
                    {
                        setValue?.Invoke(RTEditor.inst.folderCreatorName.text);
                        if (reference is IModifyable modifyable)
                            CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                        RTEditor.inst.HideNameEditor();
                        Update(modifier, reference);
                    });
                }));

            return dd;
        }

        public GameObject DeleteGenerator(Modifier modifier, IModifierReference reference, Transform parent, Action onDelete)
        {
            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(parent, "delete");
            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
            deleteGroupButton.OnClick.NewListener(() =>
            {
                onDelete?.Invoke();

                if (reference is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject);
                if (reference is BackgroundObject backgroundObject)
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                var scrollbar = dialog.Scrollbar;
                var value = scrollbar ? scrollbar.value : 0f;
                RenderModifier(reference);
                CoroutineHelper.PerformAtNextFrame(() =>
                {
                    if (scrollbar)
                        scrollbar.value = value;
                });
                Update(modifier, reference);
            });
            EditorThemeManager.ApplyDeleteButton(deleteGroupButton);
            return deleteGroup;
        }

        public GameObject AddGenerator(Modifier modifier, IModifierReference reference, string text, Action onAdd)
        {
            var add = EditorPrefabHolder.Instance.CreateAddButton(layout);
            add.Text = text;
            add.OnClick.NewListener(() =>
            {
                onAdd?.Invoke();

                if (reference is BeatmapObject beatmapObject)
                    RTLevel.Current?.UpdateObject(beatmapObject);
                if (reference is BackgroundObject backgroundObject)
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                var scrollbar = dialog.Scrollbar;
                var value = scrollbar ? scrollbar.value : 0f;
                RenderModifier(reference);
                CoroutineHelper.PerformAtNextFrame(() =>
                {
                    if (scrollbar)
                        scrollbar.value = value;
                });
                Update(modifier, reference);
            });
            return add.gameObject;
        }

        #endregion

        public override string ToString() => Modifier ? Modifier.ToString() : base.ToString();

        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents the base value that displays in a modifier card.
        /// </summary>
        public abstract class Value : Exists
        {
            #region Constructors

            public Value(int valueIndex) => this.valueIndex = valueIndex;

            #endregion

            #region Values

            /// <summary>
            /// Index of the value.
            /// </summary>
            public int valueIndex;

            /// <summary>
            /// If the mouse cursor is hovering over the modifier value, meaning the raw value should display if possible.
            /// </summary>
            public bool hovered;

            #endregion

            #region Functions

            /// <summary>
            /// Updates the value display per tick.
            /// </summary>
            /// <param name="modifierCard">Modifier card reference.</param>
            /// <param name="reference">Object reference.</param>
            public abstract void Tick(ModifierCard modifierCard, IModifierReference reference);

            /// <summary>
            /// Initializes the hover notifier.
            /// </summary>
            /// <param name="gameObject">Game object reference.</param>
            public void InitHover(GameObject gameObject) => gameObject.AddComponent<HoverNotifier>().notifier = (hovered, pointerEventData) => this.hovered = hovered;

            #endregion
        }

        public class StringValue : Value
        {
            #region Constructors

            public StringValue(int valueIndex, InputField inputField) : base(valueIndex)
            {
                this.inputField = inputField;
                InitHover(inputField.gameObject);
            }

            #endregion

            #region Values

            /// <summary>
            /// Input field reference.
            /// </summary>
            public InputField inputField;

            #endregion

            #region Functions

            public override void Tick(ModifierCard modifierCard, IModifierReference reference)
            {
                if (!inputField || inputField.isFocused)
                    return;

                if (!modifierCard || !modifierCard.Modifier)
                    return;
                var modifierLoop = reference.GetModifierLoop();
                if (!modifierLoop)
                    return;

                if (!modifierLoop.variables.TryGetValue(modifierCard.Modifier.GetValue(valueIndex), out string value))
                    return;

                if (hovered)
                    inputField.SetTextWithoutNotify(modifierCard.Modifier.GetValue(valueIndex));
                else
                    inputField.SetTextWithoutNotify(value);
            }

            #endregion
        }

        public class BoolValue : Value
        {
            #region Constructors

            public BoolValue(int valueIndex, Toggle toggle) : base(valueIndex)
            {
                this.toggle = toggle;
                InitHover(toggle.gameObject);
            }

            #endregion

            #region Values

            /// <summary>
            /// Toggle reference.
            /// </summary>
            public Toggle toggle;

            /// <summary>
            /// The default value to display if value is in an incorrect format.
            /// </summary>
            public bool defaultValue;

            #endregion

            #region Functions

            public override void Tick(ModifierCard modifierCard, IModifierReference reference)
            {
                if (!toggle)
                    return;

                if (!modifierCard || !modifierCard.Modifier)
                    return;
                var modifierLoop = reference.GetModifierLoop();
                if (!modifierLoop)
                    return;

                if (!modifierLoop.variables.TryGetValue(modifierCard.Modifier.GetValue(valueIndex), out string value))
                    return;

                if (hovered)
                    toggle.SetIsOnWithoutNotify(modifierCard.Modifier.GetBool(valueIndex, defaultValue));
                else if (bool.TryParse(value, out bool isOn))
                    toggle.SetIsOnWithoutNotify(isOn);
            }

            #endregion
        }

        public class DropdownValue : Value
        {
            #region Constructors

            public DropdownValue(int valueIndex, Dropdown dropdown) : base(valueIndex)
            {
                this.dropdown = dropdown;
                InitHover(dropdown.gameObject);
            }

            #endregion

            #region Values

            /// <summary>
            /// Dropdown reference.
            /// </summary>
            public Dropdown dropdown;

            /// <summary>
            /// Function that gets the value for the dropdown to display.
            /// </summary>
            public Func<string, int> getValue;

            /// <summary>
            /// The default value to display if value is in an incorrect format.
            /// </summary>
            public int defaultValue;

            #endregion

            #region Functions

            public override void Tick(ModifierCard modifierCard, IModifierReference reference)
            {
                if (!dropdown || dropdown.m_Blocker) // m_Blocker means the dropdown is currently being selected
                    return;

                if (!modifierCard || !modifierCard.Modifier)
                    return;
                var modifierLoop = reference.GetModifierLoop();
                if (!modifierLoop)
                    return;

                if (!modifierLoop.variables.TryGetValue(modifierCard.Modifier.GetValue(valueIndex), out string value))
                    return;

                if (hovered)
                {
                    if (getValue != null)
                        dropdown.SetValueWithoutNotify(getValue.Invoke(modifierCard.Modifier.GetValue(valueIndex)));
                    else
                        dropdown.SetValueWithoutNotify(modifierCard.Modifier.GetInt(valueIndex, defaultValue));
                    return;
                }

                if (getValue != null)
                    dropdown.SetValueWithoutNotify(getValue.Invoke(value));
                else if (int.TryParse(value, out int num))
                    dropdown.SetValueWithoutNotify(num);
            }

            #endregion
        }

        public class ColorSlotsValue : Value
        {
            #region Constructors

            public ColorSlotsValue(int valueIndex, GameObject gameObject, Toggle[] toggles) : base(valueIndex)
            {
                this.toggles = toggles;
                InitHover(gameObject);
            }

            #endregion

            #region Values

            /// <summary>
            /// Array of toggles.
            /// </summary>
            public Toggle[] toggles;
            bool cachedHover;

            #endregion

            #region Functions

            public override void Tick(ModifierCard modifierCard, IModifierReference reference)
            {
                if (toggles == null)
                    return;

                if (!modifierCard || !modifierCard.Modifier)
                    return;
                var modifierLoop = reference.GetModifierLoop();
                if (!modifierLoop)
                    return;

                if (!modifierLoop.variables.TryGetValue(modifierCard.Modifier.GetValue(valueIndex), out string value))
                    return;

                if (cachedHover != hovered)
                {
                    cachedHover = hovered;
                    if (hovered)
                    {
                        SetValue(modifierCard.Modifier.GetInt(valueIndex, 0));
                        return;
                    }
                }

                if (!hovered && int.TryParse(value, out int slot))
                    SetValue(slot);
            }

            void SetValue(int slot)
            {
                for (int i = 0; i < toggles.Length; i++)
                    toggles[i].SetIsOnWithoutNotify(i == slot);
            }

            #endregion
        }

        #endregion
    }
}
