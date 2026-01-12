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
    public class ModifierCard : Exists
    {
        public ModifierCard(Modifier modifier, int index, bool inCollapsedRegion, ModifiersEditorDialog dialog)
        {
            Modifier = modifier;
            this.index = index;
            this.inCollapsedRegion = inCollapsedRegion;
            this.dialog = dialog;
        }

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

            if (!ModifiersHelper.IsEditorModifier(name))
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

            if (name != "else" && modifier.type == Modifier.Type.Trigger)
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

            if (!modifier.verified)
            {
                modifier.verified = true;
                if (!name.Contains("DEVONLY"))
                    modifier.VerifyModifier(ModifiersManager.inst.modifiers);
            }

            if (string.IsNullOrEmpty(name))
            {
                EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            switch (name)
            {
                case "comment": {
                        var height = Mathf.Clamp(modifier.GetFloat(2, 126f), 20f, 512f);
                        layout.AsRT().sizeDelta = new Vector2(340f, height);
                        var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(layout, "Input");
                        input.transform.localScale = Vector2.one;
                        input.transform.AsRT().sizeDelta = new Vector2(340f, height);
                        input.transform.Find("Text").AsRT().sizeDelta = Vector2.zero;
                        var inputField = input.GetComponent<InputField>();
                        inputField.textComponent.alignment = TextAnchor.UpperLeft;
                        inputField.lineType = InputField.LineType.MultiLineNewline;
                        inputField.interactable = !modifier.GetBool(1, false);
                        inputField.SetTextWithoutNotify(modifier.GetValue(0));
                        inputField.onValueChanged.NewListener(_val =>
                        {
                            modifier.SetValue(0, _val);
                            Update(modifier, reference);
                        });

                        EditorThemeManager.ApplyInputField(inputField);

                        EditorContextMenu.AddContextMenu(input,
                            new ButtonElement(() => modifier.GetBool(1, false) ? "Unlock comment" : "Lock comment", () =>
                            {
                                modifier.SetValue(1, (!modifier.GetBool(1, false)).ToString());
                                Update(modifier, reference);

                                if (inputField)
                                    inputField.interactable = !modifier.GetBool(1, false);
                            }),
                            new LabelElement("Height"),
                            new NumberInputElement(() => modifier.GetFloat(2, 126f).ToString(), _val =>
                            {
                                var height = Mathf.Clamp(Parser.TryParse(_val, 126f), 20f, 512f);
                                modifier.SetValue(2, height.ToString());
                                Update(modifier, reference);

                                if (input)
                                    input.transform.AsRT().sizeDelta = new Vector2(340f, height);
                                if (layout)
                                {
                                    layout.AsRT().sizeDelta = new Vector2(340f, height);
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(layout.AsRT());
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(layout.parent.AsRT());
                                }
                            }, new NumberInputElement.ArrowHandlerFloat()
                            {
                                min = 20f,
                                max = 512f,
                            }),
                            new ButtonElement("Reset Height", () =>
                            {
                                modifier.SetValue(2, "126");
                                Update(modifier, reference);

                                if (input)
                                    input.transform.AsRT().sizeDelta = new Vector2(340f, 126f);
                                if (layout)
                                {
                                    layout.AsRT().sizeDelta = new Vector2(340f, 126f);
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(layout.AsRT());
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(layout.parent.AsRT());
                                }
                            }));

                        break;
                    }

                case nameof(ModifierFunctions.setActive): {
                        BoolGenerator(modifier, reference, "Active", 0, false);

                        break;
                    }
                case nameof(ModifierFunctions.setActiveOther): {
                        BoolGenerator(modifier, reference, "Active", 0, false);
                        StringGenerator(modifier, reference, "BG Group", 1);

                        break;
                    }

                case "timeLesserEquals":
                case "timeGreaterEquals":
                case "timeLesser":
                case "timeGreater": {
                        SingleGenerator(modifier, reference, "Time", 0, 0f);

                        break;
                    }

                #region Actions

                #region Audio

                case nameof(ModifierFunctions.setPitch): {
                        SingleGenerator(modifier, reference, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.addPitch): {
                        SingleGenerator(modifier, reference, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setPitchMath): {
                        StringGenerator(modifier, reference, "Pitch", 0);

                        break;
                    }
                case nameof(ModifierFunctions.addPitchMath): {
                        StringGenerator(modifier, reference, "Pitch", 0);

                        break;
                    }

                case nameof(ModifierFunctions.setMusicTime): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setMusicTimeMath): {
                        StringGenerator(modifier, reference, "Time", 0);

                        break;
                    }
                //case "setMusicTimeStartTime): {
                //        break;
                //    }
                //case "setMusicTimeAutokill): {
                //        break;
                //    }
                case nameof(ModifierFunctions.setMusicPlaying): {
                        BoolGenerator(modifier, reference, "Playing", 0, false);

                        break;
                    }

                case nameof(ModifierFunctions.playSound): {
                        var str = StringGenerator(modifier, reference, "Path", 0);
                        EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                            EditorContextMenu.GetModifierSoundPathFunctions(() => modifier.GetBool(1, false), _val => modifier.SetValue(0, _val)));
                        BoolGenerator(modifier, reference, "Global", 1, false);
                        SingleGenerator(modifier, reference, "Pitch", 2, 1f);
                        SingleGenerator(modifier, reference, "Volume", 3, 1f);
                        BoolGenerator(modifier, reference, "Loop", 4, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 5);

                        break;
                    }
                case nameof(ModifierFunctions.playSoundOnline): {
                        StringGenerator(modifier, reference, "URL", 0);
                        SingleGenerator(modifier, reference, "Pitch", 1, 1f);
                        SingleGenerator(modifier, reference, "Volume", 2, 1f);
                        BoolGenerator(modifier, reference, "Loop", 3, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 4);
                        break;
                    }
                case nameof(ModifierFunctions.playOnlineSound): {
                        StringGenerator(modifier, reference, "URL", 0);
                        SingleGenerator(modifier, reference, "Pitch", 1, 1f);
                        SingleGenerator(modifier, reference, "Volume", 2, 1f);
                        BoolGenerator(modifier, reference, "Loop", 3, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 4);
                        break;
                    }
                case nameof(ModifierFunctions.playDefaultSound): {
                        var dd = ModifiersEditor.inst.dropdownBar.Duplicate(layout, "Sound");
                        dd.transform.localScale = Vector3.one;
                        var labelText = dd.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Sound";

                        CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                        var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                        var sounds = Enum.GetNames(typeof(DefaultSounds));
                        d.options = CoreHelper.StringToOptionData(sounds);

                        int soundIndex = -1;
                        for (int i = 0; i < sounds.Length; i++)
                        {
                            if (sounds[i] == modifier.GetValue(0))
                            {
                                soundIndex = i;
                                break;
                            }
                        }

                        if (soundIndex >= 0)
                            d.SetValueWithoutNotify(soundIndex);

                        d.onValueChanged.NewListener(_val =>
                        {
                            modifier.SetValue(0, sounds[_val]);
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyDropdown(d);

                        SingleGenerator(modifier, reference, "Pitch", 1, 1f);
                        SingleGenerator(modifier, reference, "Volume", 2, 1f);
                        BoolGenerator(modifier, reference, "Loop", 3, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 4);

                        break;
                    }
                case nameof(ModifierFunctions.audioSource): {
                        var str = StringGenerator(modifier, reference, "Path", 0);
                        EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                            EditorContextMenu.GetModifierSoundPathFunctions(() => modifier.GetBool(1, false), _val => modifier.SetValue(0, _val)));
                        BoolGenerator(modifier, reference, "Global", 1, false);
                        SingleGenerator(modifier, reference, "Pitch", 2, 1f);
                        SingleGenerator(modifier, reference, "Volume", 3, 1f);
                        BoolGenerator(modifier, reference, "Loop", 4, true);

                        SingleGenerator(modifier, reference, "Time", 5, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", 6, true);
                        SingleGenerator(modifier, reference, "Length Offset", 7, 0f);

                        BoolGenerator(modifier, reference, "Playing", 8, true);

                        SingleGenerator(modifier, reference, "Pan Stereo", 9);

                        break;
                    }
                case nameof(ModifierFunctions.loadSoundAsset): {
                        StringGenerator(modifier, reference, "Asset Name", 0);
                        BoolGenerator(modifier, reference, "Load", 1);
                        BoolGenerator(modifier, reference, "Play", 2);
                        SingleGenerator(modifier, reference, "Pitch", 3);
                        SingleGenerator(modifier, reference, "Volume", 4);
                        BoolGenerator(modifier, reference, "Loop", 5);
                        SingleGenerator(modifier, reference, "Pan Stereo", 6);

                        break;
                    }

                #endregion

                #region Level

                case nameof(ModifierFunctions.loadLevel): {
                        StringGenerator(modifier, reference, "Path", 0);

                        break;
                    }
                case nameof(ModifierFunctions.loadLevelID): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierFunctions.loadLevelInternal): {
                        StringGenerator(modifier, reference, "Inner Path", 0);

                        break;
                    }
                //case "loadLevelPrevious): {
                //        break;
                //    }
                //case "loadLevelHub): {
                //        break;
                //    }
                case nameof(ModifierFunctions.loadLevelInCollection): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierFunctions.loadLevelCollection): {
                        StringGenerator(modifier, reference, "Collection ID", 0);
                        StringGenerator(modifier, reference, "Level ID", 1);

                        break;
                    }
                case nameof(ModifierFunctions.downloadLevel): {
                        StringGenerator(modifier, reference, "Arcade ID", 0);
                        StringGenerator(modifier, reference, "Server ID", 1);
                        StringGenerator(modifier, reference, "Workshop ID", 2);
                        StringGenerator(modifier, reference, "Song Title", 3);
                        StringGenerator(modifier, reference, "Level Name", 4);
                        BoolGenerator(modifier, reference, "Play Level", 5, true);

                        break;
                    }
                case nameof(ModifierFunctions.endLevel): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, reference, "End Level Function", 0, options);
                        StringGenerator(modifier, reference, "End Level Data", 1);
                        BoolGenerator(modifier, reference, "Save Player Data", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.setAudioTransition): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setIntroFade): {
                        BoolGenerator(modifier, reference, "Should Fade", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.setLevelEndFunc): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, reference, "End Level Function", 0, options);
                        StringGenerator(modifier, reference, "End Level Data", 1);
                        BoolGenerator(modifier, reference, "Save Player Data", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.getCurrentLevelID): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getCurrentLevelName): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getCurrentLevelRank): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getLevelVariable): {
                        StringGenerator(modifier, reference, "ID", 0);
                        StringGenerator(modifier, reference, "Level Variable Name", 1);
                        StringGenerator(modifier, reference, "Default Value", 2);
                        StringGenerator(modifier, reference, "Variable Name", 3, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.setLevelVariable): {
                        StringGenerator(modifier, reference, "ID", 0);
                        StringGenerator(modifier, reference, "Level Variable Name", 1);
                        StringGenerator(modifier, reference, "Value", 2);

                        break;
                    }
                case nameof(ModifierFunctions.removeLevelVariable): {
                        StringGenerator(modifier, reference, "ID", 0);
                        StringGenerator(modifier, reference, "Level Variable Name", 1);

                        break;
                    }
                case nameof(ModifierFunctions.clearLevelVariables): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierFunctions.getCurrentLevelVariable): {
                        StringGenerator(modifier, reference, "Level Variable Name", 0);
                        StringGenerator(modifier, reference, "Default Value", 1);
                        StringGenerator(modifier, reference, "Variable Name", 2, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.setCurrentLevelVariable): {
                        StringGenerator(modifier, reference, "Level Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.removeCurrentLevelVariable): {
                        StringGenerator(modifier, reference, "Level Variable Name", 0);

                        break;
                    }
                //case nameof(ModifierFunctions.clearCurrentLevelVariables): {

                //        break;
                //    }

                #endregion

                #region Component

                case nameof(ModifierFunctions.blur): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Use Opacity", 1, false);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierFunctions.blurOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierFunctions.blurVariable): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 1, false);

                        break;
                    }
                case nameof(ModifierFunctions.blurVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierFunctions.blurColored): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Use Opacity", 1, false);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierFunctions.blurColoredOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                //case "doubleSided): {
                //        break;
                //    }
                case nameof(ModifierFunctions.particleSystem): {
                        SingleGenerator(modifier, reference, "Life Time", 0, 5f);

                        DropdownGenerator(modifier, reference, "Shape", 1, ShapeManager.inst.Shapes2D.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), new List<bool>
                        {
                            false, // square
                            false, // circle
                            false, // triangle
                            false, // arrow
                            true, // text
                            false, // hexagon
                            true, // image
                            false, // pentagon
                            false, // misc
                            true, // polygon
                        },
                        _val =>
                        {
                            var shapeType = (ShapeType)_val;
                            if (shapeType == ShapeType.Text || shapeType == ShapeType.Image || shapeType == ShapeType.Polygon)
                                modifier.SetValue(1, "0");
                            else
                                modifier.SetValue(1, _val.ToString());
                            modifier.SetValue(2, "0");
                            RenderModifier(reference);
                            Update(modifier, reference);
                        });

                        var shape = modifier.GetInt(1, 0);
                        DropdownGenerator(modifier, reference, "Shape Option", 2, ShapeManager.inst.Shapes2D[shape].shapes.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), null);

                        ColorGenerator(modifier, reference, "Color", 3);
                        SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
                        SingleGenerator(modifier, reference, "End Opacity", 5, 0f);
                        SingleGenerator(modifier, reference, "Start Scale", 6, 1f);
                        SingleGenerator(modifier, reference, "End Scale", 7, 0f);
                        SingleGenerator(modifier, reference, "Rotation", 8, 0f);
                        SingleGenerator(modifier, reference, "Speed", 9, 5f);
                        SingleGenerator(modifier, reference, "Amount", 10, 1f);
                        SingleGenerator(modifier, reference, "Duration", 11, 1f);
                        SingleGenerator(modifier, reference, "Force X", 12, 0f);
                        SingleGenerator(modifier, reference, "Force Y", 13, 0f);
                        BoolGenerator(modifier, reference, "Emit Trail", 14, false);
                        SingleGenerator(modifier, reference, "Angle", 15, 0f);
                        IntegerGenerator(modifier, reference, "Burst Count", 16, 0);

                        break;
                    }
                case nameof(ModifierFunctions.particleSystemHex): {
                        SingleGenerator(modifier, reference, "Life Time", 0, 5f);

                        DropdownGenerator(modifier, reference, "Shape", 1, ShapeManager.inst.Shapes2D.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), new List<bool>
                        {
                            false, // square
                            false, // circle
                            false, // triangle
                            false, // arrow
                            true, // text
                            false, // hexagon
                            true, // image
                            false, // pentagon
                            false, // misc
                            true, // polygon
                        },
                        _val =>
                        {
                            var shapeType = (ShapeType)_val;
                            if (shapeType == ShapeType.Text || shapeType == ShapeType.Image || shapeType == ShapeType.Polygon)
                                modifier.SetValue(1, "0");
                            else
                                modifier.SetValue(1, _val.ToString());
                            modifier.SetValue(2, "0");
                            RenderModifier(reference);
                            Update(modifier, reference);
                        });

                        var shape = modifier.GetInt(1, 0);
                        DropdownGenerator(modifier, reference, "Shape Option", 2, ShapeManager.inst.Shapes2D[shape].shapes.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), null);

                        StringGenerator(modifier, reference, "Color", 3);
                        SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
                        SingleGenerator(modifier, reference, "End Opacity", 5, 0f);
                        SingleGenerator(modifier, reference, "Start Scale", 6, 1f);
                        SingleGenerator(modifier, reference, "End Scale", 7, 0f);
                        SingleGenerator(modifier, reference, "Rotation", 8, 0f);
                        SingleGenerator(modifier, reference, "Speed", 9, 5f);
                        SingleGenerator(modifier, reference, "Amount", 10, 1f);
                        SingleGenerator(modifier, reference, "Duration", 11, 1f);
                        SingleGenerator(modifier, reference, "Force X", 12, 0f);
                        SingleGenerator(modifier, reference, "Force Y", 13, 0f);
                        BoolGenerator(modifier, reference, "Emit Trail", 14, false);
                        SingleGenerator(modifier, reference, "Angle", 15, 0f);
                        IntegerGenerator(modifier, reference, "Burst Count", 16, 0);

                        break;
                    }
                case nameof(ModifierFunctions.trailRenderer): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);
                        SingleGenerator(modifier, reference, "Start Width", 1, 1f);
                        SingleGenerator(modifier, reference, "End Width", 2, 0f);
                        ColorGenerator(modifier, reference, "Start Color", 3);
                        SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
                        ColorGenerator(modifier, reference, "End Color", 5);
                        SingleGenerator(modifier, reference, "End Opacity", 6, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.trailRendererHex): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);
                        SingleGenerator(modifier, reference, "Start Width", 1, 1f);
                        SingleGenerator(modifier, reference, "End Width", 2, 0f);
                        StringGenerator(modifier, reference, "Start Color", 3);
                        StringGenerator(modifier, reference, "End Color", 4);

                        break;
                    }
                case nameof(ModifierFunctions.rigidbody): {
                        SingleGenerator(modifier, reference, "Gravity", 1, 0f);

                        DropdownGenerator(modifier, reference, "Collision Mode", 2, CoreHelper.StringToOptionData("Discrete", "Continuous"));

                        SingleGenerator(modifier, reference, "Drag", 3, 0f);
                        SingleGenerator(modifier, reference, "Velocity X", 4, 0f);
                        SingleGenerator(modifier, reference, "Velocity Y", 5, 0f);

                        DropdownGenerator(modifier, reference, "Body Type", 6, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));

                        break;
                    }
                case nameof(ModifierFunctions.rigidbodyOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "Gravity", 1, 0f);

                        DropdownGenerator(modifier, reference, "Collision Mode", 2, CoreHelper.StringToOptionData("Discrete", "Continuous"));

                        SingleGenerator(modifier, reference, "Drag", 3, 0f);
                        SingleGenerator(modifier, reference, "Velocity X", 4, 0f);
                        SingleGenerator(modifier, reference, "Velocity Y", 5, 0f);

                        DropdownGenerator(modifier, reference, "Body Type", 6, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));

                        break;
                    }
                case nameof(ModifierFunctions.setRenderType): {
                        DropdownGenerator(modifier, reference, "Render Type", 0, CoreHelper.ToOptionData<BeatmapObject.RenderLayerType>());
                        break;
                    }
                case nameof(ModifierFunctions.setRenderTypeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "Render Type", 1, CoreHelper.ToOptionData<BeatmapObject.RenderLayerType>());
                        break;
                    }
                case nameof(ModifierFunctions.setRendering): {
                        BoolGenerator(modifier, reference, "Double Sided", 0);
                        DropdownGenerator(modifier, reference, "Gradient Type", 1, CoreHelper.ToOptionData<GradientType>());
                        SingleGenerator(modifier, reference, "Gradient Scale", 3, 1f);
                        SingleGenerator(modifier, reference, "Gradient Rotation", 4, 0, 15f, 3f);
                        DropdownGenerator(modifier, reference, "Color Blend Mode", 2, CoreHelper.ToOptionData<ColorBlendMode>());

                        break;
                    }
                case nameof(ModifierFunctions.setOutline): {
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Behind Object", "Behind All"));
                        SingleGenerator(modifier, reference, "Width", 2, 0.1f);

                        ColorGenerator(modifier, reference, "Color", 3);
                        SingleGenerator(modifier, reference, "Opacity", 4, 0.5f);
                        SingleGenerator(modifier, reference, "Hue", 5, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 6, 0f);
                        SingleGenerator(modifier, reference, "Value", 7, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.setOutlineOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, reference, "Enabled", 1);
                        DropdownGenerator(modifier, reference, "Type", 2, CoreHelper.StringToOptionData("Behind Object", "Behind All"));
                        SingleGenerator(modifier, reference, "Width", 3, 0.1f);

                        ColorGenerator(modifier, reference, "Color", 4);
                        SingleGenerator(modifier, reference, "Opacity", 5, 0.5f);
                        SingleGenerator(modifier, reference, "Hue", 6, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 7, 0f);
                        SingleGenerator(modifier, reference, "Value", 8, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.setOutlineHex): {
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Behind Object", "Behind All"));
                        SingleGenerator(modifier, reference, "Width", 2, 0.1f);

                        var hexCode = StringGenerator(modifier, reference, "Hex Code", 3);
                        EditorContextMenu.AddContextMenu(hexCode,
                            EditorContextMenu.GetEditorColorFunctions(hexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(3)));

                        break;
                    }
                case nameof(ModifierFunctions.setOutlineHexOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Enabled", 1);
                        DropdownGenerator(modifier, reference, "Type", 2, CoreHelper.StringToOptionData("Behind Object", "Behind All"));
                        SingleGenerator(modifier, reference, "Width", 3, 0.1f);

                        var hexCode = StringGenerator(modifier, reference, "Hex Code", 4);
                        EditorContextMenu.AddContextMenu(hexCode,
                            EditorContextMenu.GetEditorColorFunctions(hexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(4)));

                        break;
                    }
                case nameof(ModifierFunctions.setDepthOffset): {
                        IntegerGenerator(modifier, reference, "Depth Offset", 0, max: int.MaxValue);
                        BoolGenerator(modifier, reference, "Inverse", 1);

                        break;
                    }

                #endregion

                #region Player

                case nameof(ModifierFunctions.playerBoostingIndex): {
                        SingleGenerator(modifier, reference, "Index", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerJumpingIndex): {
                        SingleGenerator(modifier, reference, "Index", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerAliveIndex): {
                        SingleGenerator(modifier, reference, "Index", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerInput): {
                        var str = StringGenerator(modifier, reference, "Action Name", 0);
                        var inputField = str.transform.Find("Input").GetComponent<InputField>();
                        var elements = new EditorElement[Core.Data.Player.PlayerInput.Names.AllNames.Length + 1];
                        elements[0] = new LabelElement("Action Names");
                        for (int i = 1; i < elements.Length; i++)
                        {
                            var n = Core.Data.Player.PlayerInput.Names.AllNames[i - 1];
                            elements[i] = new ButtonElement(n, () => inputField.text = n);
                        }
                        EditorContextMenu.AddContextMenu(inputField.gameObject, elements);
                        DropdownGenerator(modifier, reference, "Held Type", 1, CoreHelper.StringToOptionData("Was Pressed", "Is Pressed", "Was Released"));
                        break;
                    }
                case nameof(ModifierFunctions.playerInputIndex): {
                        IntegerGenerator(modifier, reference, "Index", 0);
                        var str = StringGenerator(modifier, reference, "Action Name", 1);
                        var inputField = str.transform.Find("Input").GetComponent<InputField>();
                        var elements = new EditorElement[Core.Data.Player.PlayerInput.Names.AllNames.Length + 1];
                        elements[0] = new LabelElement("Action Names");
                        for (int i = 1; i < elements.Length; i++)
                        {
                            var n = Core.Data.Player.PlayerInput.Names.AllNames[i - 1];
                            elements[i] = new ButtonElement(n, () => inputField.text = n);
                        }
                        EditorContextMenu.AddContextMenu(inputField.gameObject, elements);
                        DropdownGenerator(modifier, reference, "Held Type", 2, CoreHelper.StringToOptionData("Was Pressed", "Is Pressed", "Was Released"));
                        break;
                    }

                case nameof(ModifierFunctions.playerHit): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierFunctions.playerHitIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        IntegerGenerator(modifier, reference, "Hit Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.playerHitAll): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0, 0);

                        break;
                    }

                case nameof(ModifierFunctions.playerHeal): {
                        IntegerGenerator(modifier, reference, "Heal Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierFunctions.playerHealIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        IntegerGenerator(modifier, reference, "Heal Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.playerHealAll): {
                        IntegerGenerator(modifier, reference, "Heal Amount", 0, 0);

                        break;
                    }

                //case nameof(ModifierFunctions.playerKill): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerKillIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerKillAll): {
                //        break;
                //    }

                //case nameof(ModifierFunctions.playerRespawn): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerRespawnIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerRespawnAll): {
                //        break;
                //    }

                case nameof(ModifierFunctions.playerLockX): {
                        BoolGenerator(modifier, reference, "Lock X", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerLockXIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0);
                        BoolGenerator(modifier, reference, "Lock X", 1);
                        break;
                    }
                case nameof(ModifierFunctions.playerLockXAll): {
                        BoolGenerator(modifier, reference, "Lock X", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerLockY): {
                        BoolGenerator(modifier, reference, "Lock Y", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerLockYIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0);
                        BoolGenerator(modifier, reference, "Lock Y", 1);
                        break;
                    }
                case nameof(ModifierFunctions.playerLockYAll): {
                        BoolGenerator(modifier, reference, "Lock Y", 0);
                        break;
                    }

                case nameof(ModifierFunctions.playerEnable): {
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        break;
                    }
                case nameof(ModifierFunctions.playerEnableIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0);
                        BoolGenerator(modifier, reference, "Enabled", 1);
                        break;
                    }
                case nameof(ModifierFunctions.playerEnableAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        break;
                    }

                case nameof(ModifierFunctions.playerMove): {
                        var value = modifier.GetValue(0);

                        if (value.Contains(','))
                        {
                            var axis = value.Split(',');
                            modifier.SetValue(0, axis[0]);
                            modifier.values.RemoveAt(modifier.values.Count - 1);
                            modifier.values.Insert(1, axis[1]);
                        }

                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        EaseGenerator(modifier, reference, 3);

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "X", 1, 0f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);

                        SingleGenerator(modifier, reference, "Duration", 3, 1f);

                        EaseGenerator(modifier, reference, 4);

                        BoolGenerator(modifier, reference, "Relative", 5, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveAll): {
                        var value = modifier.GetValue(0);

                        if (value.Contains(','))
                        {
                            var axis = value.Split(',');
                            modifier.SetValue(0, axis[0]);
                            modifier.values.RemoveAt(modifier.values.Count - 1);
                            modifier.values.Insert(1, axis[1]);
                        }

                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        EaseGenerator(modifier, reference, 3);

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveX): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveXIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "X", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        EaseGenerator(modifier, reference, 3);

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveXAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveY): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveYIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        EaseGenerator(modifier, reference, 3);

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerMoveYAll): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerRotate): {
                        SingleGenerator(modifier, reference, "Rotation", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerRotateIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "Rotation", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        EaseGenerator(modifier, reference, 3);

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierFunctions.playerRotateAll): {
                        SingleGenerator(modifier, reference, "Rotation", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        EaseGenerator(modifier, reference, 2);

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }

                //case nameof(ModifierFunctions.playerMoveToObject): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerMoveIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerMoveAllToObject): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.playerMoveXToObject): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerMoveXIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerMoveXAllToObject): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.playerMoveYToObject): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerMoveYIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveYAllToObject): {
                //        break;
                //    }
                //case "playerRotateToObject): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerRotateIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerRotateAllToObject): {
                //        break;
                //    }

                case nameof(ModifierFunctions.playerDrag): {
                        BoolGenerator(modifier, reference, "Use Position", 0);
                        BoolGenerator(modifier, reference, "Use Scale", 1);
                        BoolGenerator(modifier, reference, "Use Rotation", 2);

                        break;
                    }

                case nameof(ModifierFunctions.playerBoost): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                case nameof(ModifierFunctions.playerBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1);
                        SingleGenerator(modifier, reference, "Y", 2);

                        break;
                    }
                case nameof(ModifierFunctions.playerBoostAll): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                    
                //case nameof(ModifierFunctions.playerCancelBoost): {

                //        break;
                //    }
                case nameof(ModifierFunctions.playerCancelBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerCancelBoostAll): {

                //        break;
                //    }

                //case nameof(ModifierFunctions.playerDisableBoost): {
                //        break;
                //    }
                case nameof(ModifierFunctions.playerDisableBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierFunctions.playerDisableBoostAll): {
                //        break;
                //    }

                case nameof(ModifierFunctions.playerEnableBoost): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableBoostAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableMove): {
                        BoolGenerator(modifier, reference, "Can Move", 0, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableMoveIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Can Move", 1, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableMoveAll): {
                        BoolGenerator(modifier, reference, "Can Move", 0, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 1, true);

                        break;
                    }

                case nameof(ModifierFunctions.playerSpeed): {
                        SingleGenerator(modifier, reference, "Global Speed", 0, 1f);

                        break;
                    }

                case nameof(ModifierFunctions.playerVelocity): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1, 0f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                    
                case nameof(ModifierFunctions.playerVelocityX): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityXIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityXAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        break;
                    }
                    
                case nameof(ModifierFunctions.playerVelocityY): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityYIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.playerVelocityYAll): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        break;
                    }

                case nameof(ModifierFunctions.playerEnableDamage): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableDamageIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableDamageAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                    
                case nameof(ModifierFunctions.playerEnableJump): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableJumpIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableJumpAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                    
                case nameof(ModifierFunctions.playerEnableReversedJump): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableReversedJumpIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableReversedJumpAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                    
                case nameof(ModifierFunctions.playerEnableWallJump): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableWallJumpIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.playerEnableWallJumpAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                    
                case nameof(ModifierFunctions.setPlayerJumpGravity): {
                        SingleGenerator(modifier, reference, "Gravity", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setPlayerJumpGravityIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Gravity", 1, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setPlayerJumpGravityAll): {
                        SingleGenerator(modifier, reference, "Gravity", 0, 1f);

                        break;
                    }
                    
                case nameof(ModifierFunctions.setPlayerJumpIntensity): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setPlayerJumpIntensityIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Intensity", 1, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setPlayerJumpIntensityAll): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);

                        break;
                    }

                case nameof(ModifierFunctions.setPlayerModel): {
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: 3);
                        var modelID = StringGenerator(modifier, reference, "Model ID", 0);
                        EditorContextMenu.AddContextMenu(modelID.transform.Find("Input").gameObject,
                            new ButtonElement("Select model", () => PlayerEditor.inst.OpenModelsPopup(model => SetValue(0, model.basePart.id, reference))));

                        break;
                    }
                case nameof(ModifierFunctions.setGameMode): {
                        DropdownGenerator(modifier, reference, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));

                        break;
                    }
                    
                case nameof(ModifierFunctions.gameMode): {
                        DropdownGenerator(modifier, reference, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));
                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);

                        break;
                    }

                case nameof(ModifierFunctions.blackHole): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.blackHoleIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Value", 1, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.blackHoleAll): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.whiteHole): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.whiteHoleIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Value", 1, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.whiteHoleAll): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }

                #endregion

                #region Mouse Cursor

                case nameof(ModifierFunctions.showMouse): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Enabled", 0, true);
                        break;
                    }
                //case "hideMouse): {
                //        break;
                //    }
                case nameof(ModifierFunctions.setMousePosition): {
                        IntegerGenerator(modifier, reference, "Position X", 1, 0);
                        IntegerGenerator(modifier, reference, "Position Y", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.followMousePosition): {
                        SingleGenerator(modifier, reference, "Position Focus", 0, 1f);
                        SingleGenerator(modifier, reference, "Rotation Delay", 1, 1f);

                        break;
                    }

                #endregion

                #region Variable
                    
                case nameof(ModifierFunctions.getToggle): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Value", 1, false);
                        BoolGenerator(modifier, reference, "Invert Value", 2, false);

                        break;
                    }
                case nameof(ModifierFunctions.getFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Value", 1, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.getInt): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Value", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.getString): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getStringLower): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getStringUpper): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        ColorGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getEnum): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        var options = new List<string>();
                        for (int i = 3; i < modifier.values.Count; i += 2)
                            options.Add(modifier.values[i]);

                        if (!options.IsEmpty())
                            DropdownGenerator(modifier, reference, "Value", 1, options);

                        var collapseValue = modifier.GetBool(2, false);
                        BoolGenerator("Collapse Enum Editor", collapseValue, _val =>
                        {
                            modifier.SetValue(2, _val.ToString());
                            var value = scrollbar ? scrollbar.value : 0f;
                            RenderModifier(reference);
                            CoroutineHelper.PerformAtNextFrame(() =>
                            {
                                if (scrollbar)
                                    scrollbar.value = value;
                            });
                        });

                        if (collapseValue)
                            break;

                        int a = 0;
                        for (int i = 3; i < modifier.values.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Enum Value {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 2; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var groupName = StringGenerator(modifier, reference, "Name", i, _val =>
                            {
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(reference);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());
                            var value = StringGenerator(modifier, reference, "Value", i + 1);
                            EditorHelper.AddInputFieldContextMenu(value.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Enum Value", () =>
                        {
                            modifier.values.Add($"Enum {a}");
                            modifier.values.Add(a.ToString());
                        });

                        break;
                    }
                case nameof(ModifierFunctions.getTag): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Index", 1, 0);

                        break;
                    }

                case nameof(ModifierFunctions.getAxis): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 10);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                        SingleGenerator(modifier, reference, "Offset", 5, 0f);
                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        SingleGenerator(modifier, reference, "Loop", 9, 99999f);
                        BoolGenerator(modifier, reference, "Use Visual", 8, false);

                        break;
                    }
                case nameof(ModifierFunctions.getAxisMath): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 5);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        BoolGenerator(modifier, reference, "Use Visual", 4, false);

                        StringGenerator(modifier, reference, "Expression", 6);

                        break;
                    }
                case nameof(ModifierFunctions.getAnimateVariable): {
                        StringGenerator(modifier, reference, "Variable Name", 1, renderVariables: false);

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        SingleGenerator(modifier, reference, "Value", 2, 0f);

                        BoolGenerator(modifier, reference, "Relative", 3, true);

                        EaseGenerator(modifier, reference, 4);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 5, true);

                        break;
                    }
                case nameof(ModifierFunctions.getAnimateVariableMath): {
                        StringGenerator(modifier, reference, "Variable Name", 1, renderVariables: false);

                        StringGenerator(modifier, reference, "Time", 0);

                        StringGenerator(modifier, reference, "Value", 2);

                        BoolGenerator(modifier, reference, "Relative", 3, true);

                        EaseGenerator(modifier, reference, 4);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 5, true);

                        break;
                    }
                case nameof(ModifierFunctions.getPitch): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getMusicTime): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getMath): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getNearestPlayer): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getCollidingPlayers): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getPlayerHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierFunctions.getPlayerLives): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierFunctions.getPlayerPosX): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierFunctions.getPlayerPosY): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierFunctions.getPlayerRot): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierFunctions.getEventValue): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Axis", 2, 0);

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                        SingleGenerator(modifier, reference, "Offset", 5, 0f);
                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        SingleGenerator(modifier, reference, "Loop", 8, 99999f);

                        break;
                    }
                case nameof(ModifierFunctions.getSample): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        SingleGenerator(modifier, reference, "Intensity", 2, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.getText): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierFunctions.getTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 2);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierFunctions.getCurrentKey): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getColorSlotHexCode): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
                        SingleGenerator(modifier, reference, "Hue", 3);
                        SingleGenerator(modifier, reference, "Saturation", 4);
                        SingleGenerator(modifier, reference, "Value", 5);

                        break;
                    }
                case nameof(ModifierFunctions.getFloatFromHexCode): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Hex Code", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getHexCodeFromFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Value", 1, 0f, max: 1f);

                        break;
                    }
                case nameof(ModifierFunctions.getMixedColors): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        int a = 0;
                        for (int i = 1; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Color {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Color Hex Code", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Color Value", () =>
                        {
                            modifier.values.Add(RTColors.ColorToHexOptional(RTColors.errorColor));
                        });

                        break;
                    }
                case nameof(ModifierFunctions.getModifiedColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Hex Color", 1);

                        SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
                        SingleGenerator(modifier, reference, "Hue", 3);
                        SingleGenerator(modifier, reference, "Saturation", 4);
                        SingleGenerator(modifier, reference, "Value", 5);

                        break;
                    }
                case nameof(ModifierFunctions.getLerpColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Hex Color 1", 1);
                        StringGenerator(modifier, reference, "Hex Color 2", 2);
                        SingleGenerator(modifier, reference, "Multiply", 3);

                        break;
                    }
                case nameof(ModifierFunctions.getAddColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Hex Color 1", 1);
                        StringGenerator(modifier, reference, "Hex Color 2", 2);
                        SingleGenerator(modifier, reference, "Add Amount", 3);

                        break;
                    }
                case nameof(ModifierFunctions.getVisualColor): {
                        StringGenerator(modifier, reference, "Color 1 Var Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 2 Var Name", 1, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getVisualColorRGBA): {
                        StringGenerator(modifier, reference, "Color 1 R Var Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 1 G Var Name", 1, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 1 B Var Name", 2, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 1 A Var Name", 3, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 2 R Var Name", 4, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 2 G Var Name", 5, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 2 B Var Name", 6, renderVariables: false);
                        StringGenerator(modifier, reference, "Color 2 A Var Name", 7, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getVisualOpacity): {
                        StringGenerator(modifier, reference, "Opacity 1 Var Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Opacity 2 Var Name", 1, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getJSONString): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierFunctions.getJSONFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierFunctions.getJSON): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "JSON", 1);
                        StringGenerator(modifier, reference, "JSON Value", 2);

                        break;
                    }
                case nameof(ModifierFunctions.getSubString): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Start Index", 1, 0);
                        IntegerGenerator(modifier, reference, "Length", 2, 0);

                        break;
                    }
                case nameof(ModifierFunctions.getStringLength): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Text", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getParsedString): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getSplitString): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Variable {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add String Value", () =>
                        {
                            modifier.values.Add($"SPLITSTRING_VAR_{a}");
                        });

                        break;
                    }
                case nameof(ModifierFunctions.getSplitStringAt): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);
                        StringGenerator(modifier, reference, "Variable Name", 2, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getSplitStringCount): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);
                        StringGenerator(modifier, reference, "Variable Name", 2, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getRegex): {
                        StringGenerator(modifier, reference, "Regex", 0);
                        StringGenerator(modifier, reference, "Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator(a == 0 ? "- Whole Match Variable" : $"- Match Variable {a}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Regex Value", () =>
                        {
                            modifier.values.Add($"REGEX_VAR_{a}");
                        });

                        break;
                    }
                case nameof(ModifierFunctions.getFormatVariable): { // whaaaaaaat (found i could use nameof() here)
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Format Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Text Arg {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i, renderVariables: false);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Text Value", () =>
                        {
                            modifier.values.Add($"Text");
                        });

                        break;
                    }
                case nameof(ModifierFunctions.getComparison): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Compare From", 1);
                        StringGenerator(modifier, reference, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierFunctions.getComparisonMath): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Compare From", 1);
                        StringGenerator(modifier, reference, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierFunctions.getEditorBin): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Use Prefab Object Bin", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getEditorLayer): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Use Prefab Object Bin", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getObjectName): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getSignaledVariables): {
                        BoolGenerator(modifier, reference, "Clear", 0, true);

                        break;
                    }
                case nameof(ModifierFunctions.signalLocalVariables): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                //case "clearLocalVariables): {
                //        break;
                //    }

                case nameof(ModifierFunctions.addVariable):
                case nameof(ModifierFunctions.subVariable):
                case nameof(ModifierFunctions.setVariable):
                case nameof(ModifierFunctions.addVariableOther):
                case nameof(ModifierFunctions.subVariableOther):
                case nameof(ModifierFunctions.setVariableOther): {
                        var isGroup = modifier.values.Count == 2;
                        if (isGroup)
                            PrefabGroupOnly(modifier, reference);
                        IntegerGenerator(modifier, reference, "Value", 0, 0);

                        if (isGroup)
                        {
                            var str = StringGenerator(modifier, reference, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }
                case nameof(ModifierFunctions.animateVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                        SingleGenerator(modifier, reference, "Offset", 5, 0f);
                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        SingleGenerator(modifier, reference, "Loop", 8, 99999f);

                        break;
                    }

                case nameof(ModifierFunctions.clampVariable): {
                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);

                        break;
                    }
                case nameof(ModifierFunctions.clampVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);

                        break;
                    }

                #endregion

                #region Enable

                case nameof(ModifierFunctions.enableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Enabled", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);
                        break;
                    }
                case nameof(ModifierFunctions.enableObjectTree): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "False");

                        BoolGenerator(modifier, reference, "Enabled", 2, true);
                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.enableObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Enabled", 2, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.enableObjectTreeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Enabled", 3, true);
                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.enableObjectGroup): {
                        PrefabGroupOnly(modifier, reference);
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        var options = new List<string>() { "All" };
                        for (int i = 2; i < modifier.values.Count; i++)
                            options.Add(modifier.values[i]);

                        DropdownGenerator(modifier, reference, "Value", 1, options);

                        int a = 0;
                        for (int i = 2; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Object Group", i, _val =>
                            {
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(reference);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Group", () =>
                        {
                            modifier.values.Add($"Object Group");
                        });

                        break;
                    }

                case nameof(ModifierFunctions.disableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierFunctions.disableObjectTree): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "False");

                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierFunctions.disableObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierFunctions.disableObjectTreeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 2, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }

                #endregion

                #region JSON

                case nameof(ModifierFunctions.saveFloat): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        SingleGenerator(modifier, reference, "Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.saveString): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        StringGenerator(modifier, reference, "Value", 0);

                        break;
                    }
                case nameof(ModifierFunctions.saveText): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierFunctions.saveVariable): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierFunctions.loadVariable): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierFunctions.loadVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }

                #endregion

                #region Reactive

                case nameof(ModifierFunctions.reactivePos): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveSca): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveRot): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveCol): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0);
                        ColorGenerator(modifier, reference, "Color", 2);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveColLerp): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0);
                        ColorGenerator(modifier, reference, "Color", 2);

                        break;
                    }
                case nameof(ModifierFunctions.reactivePosChain): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveScaChain): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveRotChain): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }
                case nameof(ModifierFunctions.reactiveIterations): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Offset", 2);
                        BoolGenerator(modifier, reference, "Inverse", 3);

                        break;
                    }

                #endregion

                #region Events

                case nameof(ModifierFunctions.eventOffset): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Offset Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.eventOffsetVariable): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Multiply Variable", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.eventOffsetMath): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        StringGenerator(modifier, reference, "Evaluation", 0);

                        break;
                    }
                case nameof(ModifierFunctions.eventOffsetAnimate): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Offset Value", 0, 0f);

                        SingleGenerator(modifier, reference, "Time", 3, 1f);
                        EaseGenerator(modifier, reference, 4);
                        BoolGenerator(modifier, reference, "Relative", 5, false);

                        break;
                    }
                case nameof(ModifierFunctions.eventOffsetCopyAxis): {
                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "To Axis", 4, 0);

                        SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 6, 1f);
                        SingleGenerator(modifier, reference, "Offset", 7, 0f);
                        SingleGenerator(modifier, reference, "Min", 8, -99999f);
                        SingleGenerator(modifier, reference, "Max", 9, 99999f);

                        SingleGenerator(modifier, reference, "Loop", 10, 99999f);
                        BoolGenerator(modifier, reference, "Use Visual", 11, false);

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
                    
                case nameof(ModifierFunctions.setTheme): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierFunctions.lerpTheme): {
                        StringGenerator(modifier, reference, "Previous ID", 0);
                        StringGenerator(modifier, reference, "Next ID", 1);
                        SingleGenerator(modifier, reference, "Time", 2);

                        break;
                    }
                case nameof(ModifierFunctions.addColor): {
                        ColorGenerator(modifier, reference, "Color", 1);

                        SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        SingleGenerator(modifier, reference, "Value", 4, 0f);

                        SingleGenerator(modifier, reference, "Add Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.addColorOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        ColorGenerator(modifier, reference, "Color", 2);

                        SingleGenerator(modifier, reference, "Hue", 3, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 4, 0f);
                        SingleGenerator(modifier, reference, "Value", 5, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.lerpColor): {
                        ColorGenerator(modifier, reference, "Color", 1);

                        SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        SingleGenerator(modifier, reference, "Value", 4, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.lerpColorOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        ColorGenerator(modifier, reference, "Color", 2);

                        SingleGenerator(modifier, reference, "Hue", 3, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 4, 0f);
                        SingleGenerator(modifier, reference, "Value", 5, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.addColorPlayerDistance): {
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);
                        SingleGenerator(modifier, reference, "Offset", 2, 10f);

                        break;
                    }
                case nameof(ModifierFunctions.lerpColorPlayerDistance): {
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);
                        SingleGenerator(modifier, reference, "Offset", 2, 10f);

                        SingleGenerator(modifier, reference, "Opacity", 3, 1f);
                        SingleGenerator(modifier, reference, "Hue", 4, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 5, 0f);
                        SingleGenerator(modifier, reference, "Value", 6, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.setOpacity): {
                        SingleGenerator(modifier, reference, "Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setOpacityOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 1f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.copyColor): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, reference, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.copyColorOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, reference, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierFunctions.applyColorGroup): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));
                        BoolGenerator(modifier, reference, "Override Start Opacity", 3, true);
                        BoolGenerator(modifier, reference, "Override End Opacity", 4, true);

                        break;
                    }
                case nameof(ModifierFunctions.setColorHex): {
                        var primaryHexCode = StringGenerator(modifier, reference, "Primary Hex Code", 0);
                        EditorContextMenu.AddContextMenu(primaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(primaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(0)));

                        var secondaryHexCode = StringGenerator(modifier, reference, "Secondary Hex Code", 1);
                        EditorContextMenu.AddContextMenu(secondaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(secondaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(1)));
                        break;
                    }
                case nameof(ModifierFunctions.setColorHexOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        var primaryHexCode = StringGenerator(modifier, reference, "Primary Hex Code", 0);
                        EditorContextMenu.AddContextMenu(primaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(primaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(0)));

                        var secondaryHexCode = StringGenerator(modifier, reference, "Secondary Hex Code", 2);
                        EditorContextMenu.AddContextMenu(secondaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(secondaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(2)));

                        break;
                    }
                case nameof(ModifierFunctions.setColorRGBA): {
                        SingleGenerator(modifier, reference, "Red 1", 0, 1f);
                        SingleGenerator(modifier, reference, "Green 1", 1, 1f);
                        SingleGenerator(modifier, reference, "Blue 1", 2, 1f);
                        SingleGenerator(modifier, reference, "Opacity 1", 3, 1f);

                        SingleGenerator(modifier, reference, "Red 2", 4, 1f);
                        SingleGenerator(modifier, reference, "Green 2", 5, 1f);
                        SingleGenerator(modifier, reference, "Blue 2", 6, 1f);
                        SingleGenerator(modifier, reference, "Opacity 2", 7, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setColorRGBAOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 8);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "Red 1", 0, 1f);
                        SingleGenerator(modifier, reference, "Green 1", 1, 1f);
                        SingleGenerator(modifier, reference, "Blue 1", 2, 1f);
                        SingleGenerator(modifier, reference, "Opacity 1", 3, 1f);

                        SingleGenerator(modifier, reference, "Red 2", 4, 1f);
                        SingleGenerator(modifier, reference, "Green 2", 5, 1f);
                        SingleGenerator(modifier, reference, "Blue 2", 6, 1f);
                        SingleGenerator(modifier, reference, "Opacity 2", 7, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.animateColorKF): {
                        SingleGenerator(modifier, reference, "Time", 0);
                        DropdownGenerator(modifier, reference, "Color Source", 1, CoreHelper.StringToOptionData("Objects", "BG Objects", "Effects"), onSelect: _val => RenderModifier(reference));

                        var colorSource = modifier.GetInt(1, 0);

                        ColorGenerator(modifier, reference, "Color 1 Start", 2, colorSource);
                        SingleGenerator(modifier, reference, "Opacity 1 Start", 3, 1f);
                        SingleGenerator(modifier, reference, "Hue 1 Start", 4, 0f);
                        SingleGenerator(modifier, reference, "Saturation 1 Start", 5, 0f);
                        SingleGenerator(modifier, reference, "Value 1 Start", 6, 0f);

                        ColorGenerator(modifier, reference, "Color 2 Start", 7, colorSource);
                        SingleGenerator(modifier, reference, "Opacity 2 Start", 8, 1f);
                        SingleGenerator(modifier, reference, "Hue 2 Start", 9, 0f);
                        SingleGenerator(modifier, reference, "Saturation 2 Start", 10, 0f);
                        SingleGenerator(modifier, reference, "Value 2 Start", 11, 0f);

                        int a = 0;
                        for (int i = 12; i < modifier.values.Count; i += 14)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Keyframe {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 14; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var collapseValue = modifier.GetBool(i, false);
                            BoolGenerator("Collapse Keyframe Editor", collapseValue, _val =>
                            {
                                modifier.SetValue(groupIndex, _val.ToString());
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(reference);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            if (collapseValue)
                                continue;

                            SingleGenerator(modifier, reference, "Keyframe Time", i + 1);
                            EaseGenerator(modifier, reference, i + 13);

                            BoolGenerator(modifier, reference, "Relative", i + 12, true);

                            ColorGenerator(modifier, reference, "Color 1", i + 2, colorSource);
                            SingleGenerator(modifier, reference, "Opacity 1", i + 3, 1f);
                            SingleGenerator(modifier, reference, "Hue 1", i + 4, 0f);
                            SingleGenerator(modifier, reference, "Saturation 1", i + 5, 0f);
                            SingleGenerator(modifier, reference, "Value 1", i + 6, 0f);

                            ColorGenerator(modifier, reference, "Color 2", i + 7, colorSource);
                            SingleGenerator(modifier, reference, "Opacity 2", i + 8, 1f);
                            SingleGenerator(modifier, reference, "Hue 2", i + 9, 0f);
                            SingleGenerator(modifier, reference, "Saturation 2", i + 10, 0f);
                            SingleGenerator(modifier, reference, "Value 2", i + 11, 0f);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Keyframe", () =>
                        {
                            modifier.values.Add("False"); // collapse keyframe
                            modifier.values.Add("0"); // keyframe time
                            modifier.values.Add("0"); // color slot 1
                            modifier.values.Add("1"); // opacity 1
                            modifier.values.Add("0"); // hue 1
                            modifier.values.Add("0"); // saturation 1
                            modifier.values.Add("0"); // value 1
                            modifier.values.Add("0"); // color slot 2
                            modifier.values.Add("1"); // opacity 2
                            modifier.values.Add("0"); // hue 2
                            modifier.values.Add("0"); // saturation 2
                            modifier.values.Add("0"); // value 2
                            modifier.values.Add("True"); // relative
                            modifier.values.Add("Linear"); // easing
                        });

                        break;
                    }
                case nameof(ModifierFunctions.animateColorKFHex): {
                        SingleGenerator(modifier, reference, "Time", 0);

                        StringGenerator(modifier, reference, "Color 1", 1);
                        StringGenerator(modifier, reference, "Color 2", 2);

                        int a = 0;
                        for (int i = 3; i < modifier.values.Count; i += 6)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Keyframe {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 6; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var collapseValue = modifier.GetBool(i, false);
                            BoolGenerator("Collapse Keyframe Editor", collapseValue, _val =>
                            {
                                modifier.SetValue(groupIndex, _val.ToString());
                                var value = scrollbar ? scrollbar.value : 0f;
                                RenderModifier(reference);
                                CoroutineHelper.PerformAtNextFrame(() =>
                                {
                                    if (scrollbar)
                                        scrollbar.value = value;
                                });
                            });

                            if (collapseValue)
                                continue;

                            SingleGenerator(modifier, reference, "Keyframe Time", i + 1);
                            EaseGenerator(modifier, reference, i + 5);

                            BoolGenerator(modifier, reference, "Relative", i + 4, true);

                            StringGenerator(modifier, reference, "Color 1", i + 2);
                            StringGenerator(modifier, reference, "Color 2", i + 3);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Keyframe", () =>
                        {
                            modifier.values.Add("False"); // collapse keyframe
                            modifier.values.Add("0"); // keyframe time
                            modifier.values.Add("0"); // color 1
                            modifier.values.Add("0"); // color 2
                            modifier.values.Add("True"); // relative
                            modifier.values.Add("Linear"); // easing
                        });

                        break;
                    }

                #endregion

                #region Shape

                case nameof(ModifierFunctions.setShape): {
                        var isBG = modifyable.ReferenceType == ModifierReferenceType.BackgroundObject;

                        DropdownGenerator(modifier, reference, "Shape", 0, ShapeManager.inst.Shapes2D.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), new List<bool>
                        {
                            false, // square
                            false, // circle
                            false, // triangle
                            false, // arrow
                            !isBG, // text
                            false, // hexagon
                            !isBG, // image
                            false, // pentagon
                            false, // misc
                            true, // polygon
                        }, _val =>
                        {
                            var shapeType = (ShapeType)_val;
                            if (isBG && (shapeType == ShapeType.Text || shapeType == ShapeType.Image) || shapeType == ShapeType.Polygon)
                                modifier.SetValue(0, "0");

                            modifier.SetValue(1, "0");
                            RenderModifier(reference);
                            Update(modifier, reference);
                        });

                        var shape = modifier.GetInt(0, 0);
                        DropdownGenerator(modifier, reference, "Shape Option", 1, ShapeManager.inst.Shapes2D[shape].shapes.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), null);

                        break;
                    }
                case nameof(ModifierFunctions.setPolygonShape): {
                        SingleGenerator(modifier, reference, "Radius", 0);
                        IntegerGenerator(modifier, reference, "Sides", 1, min: 3, max: 32);
                        SingleGenerator(modifier, reference, "Roundness", 2, max: 1f);
                        SingleGenerator(modifier, reference, "Thickness", 3, max: 1f);
                        SingleGenerator(modifier, reference, "Thick Offset X", 5);
                        SingleGenerator(modifier, reference, "Thick Offset Y", 6);
                        SingleGenerator(modifier, reference, "Thick Scale X", 7);
                        SingleGenerator(modifier, reference, "Thick Scale Y", 8);
                        SingleGenerator(modifier, reference, "Thick Angle", 10);
                        IntegerGenerator(modifier, reference, "Slices", 4, max: 32);
                        SingleGenerator(modifier, reference, "Angle", 9);

                        break;
                    }
                case nameof(ModifierFunctions.setPolygonShapeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "Radius", 1, max: 1f);
                        IntegerGenerator(modifier, reference, "Sides", 2, min: 3, max: 32);
                        SingleGenerator(modifier, reference, "Roundness", 3, max: 1f);
                        SingleGenerator(modifier, reference, "Thickness", 4, max: 1f);
                        SingleGenerator(modifier, reference, "Thick Offset X", 6);
                        SingleGenerator(modifier, reference, "Thick Offset Y", 7);
                        SingleGenerator(modifier, reference, "Thick Scale X", 8);
                        SingleGenerator(modifier, reference, "Thick Scale Y", 9);
                        SingleGenerator(modifier, reference, "Thick Angle", 11);
                        IntegerGenerator(modifier, reference, "Slices", 5, max: 32);
                        SingleGenerator(modifier, reference, "Angle", 10);

                        break;
                    }

                case nameof(ModifierFunctions.setImage): {
                        StringGenerator(modifier, reference, "Path", 0);
                        SingleGenerator(modifier, reference, "Texture Offset X", 1);
                        SingleGenerator(modifier, reference, "Texture Offset Y", 2);
                        SingleGenerator(modifier, reference, "Texture Scale X", 3, 1f);
                        SingleGenerator(modifier, reference, "Texture Scale Y", 4, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.setImageOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Path", 0);
                        SingleGenerator(modifier, reference, "Texture Offset X", 2);
                        SingleGenerator(modifier, reference, "Texture Offset Y", 3);
                        SingleGenerator(modifier, reference, "Texture Scale X", 4, 1f);
                        SingleGenerator(modifier, reference, "Texture Scale Y", 5, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.actorFrameTexture): {
                        DropdownGenerator(modifier, reference, "Camera", 0, CoreHelper.StringToOptionData("Foreground", "Background", "UI"));
                        BoolGenerator(modifier, reference, "All Cameras", 7);
                        IntegerGenerator(modifier, reference, "Width", 1, 512);
                        IntegerGenerator(modifier, reference, "Height", 2, 512);
                        SingleGenerator(modifier, reference, "Pos X", 3, 0f);
                        SingleGenerator(modifier, reference, "Pos Y", 4, 0f);
                        BoolGenerator(modifier, reference, "Calculate Zoom", 10, true);
                        SingleGenerator(modifier, reference, "Zoom", 5, 1f);
                        SingleGenerator(modifier, reference, "Rotate", 6, 0f, 15f, 3f);

                        SingleGenerator(modifier, reference, "Texture Offset X", 11);
                        SingleGenerator(modifier, reference, "Texture Offset Y", 12);
                        SingleGenerator(modifier, reference, "Texture Scale X", 13, 1f);
                        SingleGenerator(modifier, reference, "Texture Scale Y", 14, 1f);
                        BoolGenerator(modifier, reference, "Clear Texture", 8);

                        var primaryHexCode = StringGenerator(modifier, reference, "BG Color", 9);
                        EditorContextMenu.AddContextMenu(primaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(primaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(9)));

                        BoolGenerator(modifier, reference, "Hide Players", 15);

                        break;
                    }

                case nameof(ModifierFunctions.setText): {
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierFunctions.setTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierFunctions.addText): {
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierFunctions.addTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierFunctions.removeText): {
                        IntegerGenerator(modifier, reference, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierFunctions.removeTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, reference, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierFunctions.removeTextAt): {
                        IntegerGenerator(modifier, reference, "Remove At", 0, 0);

                        break;
                    }
                case nameof(ModifierFunctions.removeTextOtherAt): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, reference, "Remove At", 0, 0);

                        break;
                    }
                //case "formatText): {
                //        break;
                //    }
                case nameof(ModifierFunctions.textSequence): {
                        SingleGenerator(modifier, reference, "Length", 0, 1f);
                        BoolGenerator(modifier, reference, "Display Glitch", 1, true);
                        BoolGenerator(modifier, reference, "Play Sound", 2, true);
                        BoolGenerator(modifier, reference, "Custom Sound", 3, false);
                        var str = StringGenerator(modifier, reference, "Sound Path", 4);
                        EditorContextMenu.AddContextMenu(str.transform.Find("Input").gameObject,
                            EditorContextMenu.GetModifierSoundPathFunctions(() => modifier.GetBool(5, false), _val => modifier.SetValue(4, _val)));
                        BoolGenerator(modifier, reference, "Global", 5, false);

                        SingleGenerator(modifier, reference, "Pitch", 6, 1f);
                        SingleGenerator(modifier, reference, "Volume", 7, 1f);
                        SingleGenerator(modifier, reference, "Pitch Vary", 8, 0f);
                        var customText = StringGenerator(modifier, reference, "Custom Text", 9);
                        EditorHelper.AddInputFieldContextMenu(customText.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, reference, "Time Offset", 10, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", 11, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 12);

                        break;
                    }
                //case "backgroundShape): {
                //        break;
                //    }
                //case "sphereShape): {
                //        break;
                //    }
                case nameof(ModifierFunctions.translateShape): {
                        SingleGenerator(modifier, reference, "Pos X", 1, 0f);
                        SingleGenerator(modifier, reference, "Pos Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Sca X", 3, 0f);
                        SingleGenerator(modifier, reference, "Sca Y", 4, 0f);
                        SingleGenerator(modifier, reference, "Rot", 5, 0f, 15f, 3f);

                        break;
                    }
                case nameof(ModifierFunctions.translateShape3D): {
                        SingleGenerator(modifier, reference, "Pos X", 1, 0f);
                        SingleGenerator(modifier, reference, "Pos Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Pos Z", 3, 0f);
                        SingleGenerator(modifier, reference, "Sca X", 4, 0f);
                        SingleGenerator(modifier, reference, "Sca Y", 5, 0f);
                        SingleGenerator(modifier, reference, "Sca Z", 6, 0f);
                        SingleGenerator(modifier, reference, "Rot X", 7, 0f, 15f, 3f);
                        SingleGenerator(modifier, reference, "Rot Y", 8, 0f, 15f, 3f);
                        SingleGenerator(modifier, reference, "Rot Z", 9, 0f, 15f, 3f);

                        break;
                    }

                #endregion

                #region Animation

                case nameof(ModifierFunctions.animateObject): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 7, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 8, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateSignal): {
                        PrefabGroupOnly(modifier, reference);

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        StringGenerator(modifier, reference, "Signal Group", 7);
                        SingleGenerator(modifier, reference, "Signal Delay", 8, 0f);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 9, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 10, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateSignalOther): {
                        PrefabGroupOnly(modifier, reference);

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Signal Group", 8);
                        SingleGenerator(modifier, reference, "Signal Delay", 9, 0f);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 10, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 11, true);

                        break;
                    }
                    
                case nameof(ModifierFunctions.animateObjectMath): {
                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 7, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateObjectMathOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 8, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateSignalMath): {
                        PrefabGroupOnly(modifier, reference);

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        StringGenerator(modifier, reference, "Signal Group", 7);
                        StringGenerator(modifier, reference, "Signal Delay", 8);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 9, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 10, true);

                        break;
                    }
                case nameof(ModifierFunctions.animateSignalMathOther): {
                        PrefabGroupOnly(modifier, reference);

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        EaseGenerator(modifier, reference, 6);

                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Signal Group", 8);
                        StringGenerator(modifier, reference, "Signal Delay", 9);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 10, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 11, true);

                        break;
                    }

                case nameof(ModifierFunctions.gravity): {
                        SingleGenerator(modifier, reference, "X", 1, -1f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, reference, "Curve", 4, 2);

                        break;
                    }
                case nameof(ModifierFunctions.gravityOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "X", 1, -1f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, reference, "Curve", 4, 2);

                        break;
                    }

                case nameof(ModifierFunctions.copyAxis): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 6, 1f);
                        SingleGenerator(modifier, reference, "Offset", 7, 0f);
                        SingleGenerator(modifier, reference, "Min", 8, -99999f);
                        SingleGenerator(modifier, reference, "Max", 9, 99999f);

                        SingleGenerator(modifier, reference, "Loop", 10, 99999f);
                        BoolGenerator(modifier, reference, "Use Visual", 11, false);

                        break;
                    }
                case nameof(ModifierFunctions.copyAxisMath): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        BoolGenerator(modifier, reference, "Use Visual", 9, false);
                        StringGenerator(modifier, reference, "Expression", 8);

                        break;
                    }
                case nameof(ModifierFunctions.copyAxisGroup): {
                        PrefabGroupOnly(modifier, reference);
                        StringGenerator(modifier, reference, "Expression", 0);

                        DropdownGenerator(modifier, reference, "To Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "To Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        int a = 0;
                        for (int i = 3; i < modifier.values.Count; i += 8)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 8; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var groupName = StringGenerator(modifier, reference, "Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());
                            var str = StringGenerator(modifier, reference, "Object Group", i + 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            DropdownGenerator(modifier, reference, "From Type", i + 2, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color", "Variable"));
                            DropdownGenerator(modifier, reference, "From Axis", i + 3, CoreHelper.StringToOptionData("X", "Y", "Z"));
                            SingleGenerator(modifier, reference, "Delay", i + 4, 0f);
                            SingleGenerator(modifier, reference, "Min", i + 5, -9999f);
                            SingleGenerator(modifier, reference, "Max", i + 6, 9999f);
                            BoolGenerator(modifier, reference, "Use Visual", 7, false);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Group", () =>
                        {
                            var lastIndex = modifier.values.Count - 1;

                            modifier.values.Add($"var_{a}");
                            modifier.values.Add("Object Group");
                            modifier.values.Add("0");
                            modifier.values.Add("0");
                            modifier.values.Add("0");
                            modifier.values.Add("-9999");
                            modifier.values.Add("9999");
                            modifier.values.Add("False");
                        });

                        break;
                    }
                case nameof(ModifierFunctions.copyPlayerAxis): {
                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Multiply", 6, 1f);
                        SingleGenerator(modifier, reference, "Offset", 7, 0f);
                        SingleGenerator(modifier, reference, "Min", 8, -99999f);
                        SingleGenerator(modifier, reference, "Max", 9, 99999f);

                        break;
                    }

                case nameof(ModifierFunctions.legacyTail): {
                        SingleGenerator(modifier, reference, "Total Time", 0, 200f);

                        var path = ModifiersEditor.inst.stringInput.Duplicate(layout, "usage");
                        path.transform.localScale = Vector3.one;
                        var labelText = path.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Update Object to Update Modifier";
                        path.transform.Find("Text").AsRT().sizeDelta = new Vector2(350f, 32f);
                        CoreHelper.Destroy(path.transform.Find("Input").gameObject);

                        int a = 0;
                        for (int i = 1; i < modifier.values.Count; i += 3)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Tail Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 3; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var str = StringGenerator(modifier, reference, "Object Group", i);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            SingleGenerator(modifier, reference, "Distance", i + 1, 2f);
                            SingleGenerator(modifier, reference, "Time", i + 2, 12f);
                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Group", () =>
                        {
                            var lastIndex = modifier.values.Count - 1;
                            var length = "2";
                            var time = "12";
                            if (lastIndex - 1 > 2)
                            {
                                length = modifier.values[lastIndex - 1];
                                time = modifier.values[lastIndex];
                            }

                            modifier.values.Add("Object Group");
                            modifier.values.Add(length);
                            modifier.values.Add(time);
                        });

                        break;
                    }

                case nameof(ModifierFunctions.applyAnimationFrom):
                case nameof(ModifierFunctions.applyAnimationTo):
                case nameof(ModifierFunctions.applyAnimation): {
                        PrefabGroupOnly(modifier, reference);
                        if (name != "applyAnimation")
                        {
                            var str = StringGenerator(modifier, reference, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }
                        else
                        {
                            var from = StringGenerator(modifier, reference, "From Group", 0);
                            EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                            var to = StringGenerator(modifier, reference, "To Group", 10);
                            EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                        }

                        BoolGenerator(modifier, reference, "Animate Position", 1, true);
                        BoolGenerator(modifier, reference, "Animate Scale", 2, true);
                        BoolGenerator(modifier, reference, "Animate Rotation", 3, true);
                        SingleGenerator(modifier, reference, "Delay Position", 4, 0f);
                        SingleGenerator(modifier, reference, "Delay Scale", 5, 0f);
                        SingleGenerator(modifier, reference, "Delay Rotation", 6, 0f);
                        BoolGenerator(modifier, reference, "Use Visual", 7, false);
                        SingleGenerator(modifier, reference, "Length", 8, 1f);
                        SingleGenerator(modifier, reference, "Speed", 9, 1f);

                        break;
                    }
                case nameof(ModifierFunctions.applyAnimationFromMath):
                case nameof(ModifierFunctions.applyAnimationToMath):
                case nameof(ModifierFunctions.applyAnimationMath): {
                        PrefabGroupOnly(modifier, reference);
                        if (name != "applyAnimationMath")
                        {
                            var str = StringGenerator(modifier, reference, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }
                        else
                        {
                            var from = StringGenerator(modifier, reference, "From Group", 0);
                            EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                            var to = StringGenerator(modifier, reference, "To Group", 10);
                            EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                        }

                        BoolGenerator(modifier, reference, "Animate Position", 1, true);
                        BoolGenerator(modifier, reference, "Animate Scale", 2, true);
                        BoolGenerator(modifier, reference, "Animate Rotation", 3, true);
                        StringGenerator(modifier, reference, "Delay Position", 4);
                        StringGenerator(modifier, reference, "Delay Scale", 5);
                        StringGenerator(modifier, reference, "Delay Rotation", 6);
                        BoolGenerator(modifier, reference, "Use Visual", 7, false);
                        StringGenerator(modifier, reference, "Length", 8);
                        StringGenerator(modifier, reference, "Speed", 9);
                        StringGenerator(modifier, reference, "Time", name != "applyAnimationMath" ? 10 : 11);

                        break;
                    }

                #endregion

                #region Prefab

                case nameof(ModifierFunctions.spawnPrefab):
                case nameof(ModifierFunctions.spawnPrefabOffset):
                case nameof(ModifierFunctions.spawnPrefabOffsetOther):
                case nameof(ModifierFunctions.spawnMultiPrefab):
                case nameof(ModifierFunctions.spawnMultiPrefabOffset):
                case nameof(ModifierFunctions.spawnMultiPrefabOffsetOther): {
                        var isMulti = name.Contains("Multi");
                        var isOther = name.Contains("Other");
                        if (isOther)
                        {
                            PrefabGroupOnly(modifier, reference);
                            var str = StringGenerator(modifier, reference, "Object Group", isMulti ? 9 : 10);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        int valueIndex = 10;
                        if (isOther)
                            valueIndex++;
                        if (isMulti)
                            valueIndex--;

                        DropdownGenerator(modifier, reference, "Search Prefab Using", valueIndex + 2, CoreHelper.StringToOptionData("Index", "Name", "ID"));
                        StringGenerator(modifier, reference, "Prefab Reference", 0);

                        SingleGenerator(modifier, reference, "Position X", 1, 0f);
                        SingleGenerator(modifier, reference, "Position Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Scale X", 3, 0f);
                        SingleGenerator(modifier, reference, "Scale Y", 4, 0f);
                        SingleGenerator(modifier, reference, "Rotation", 5, 0f, 15f, 3f);

                        IntegerGenerator(modifier, reference, "Repeat Count", 6, 0);
                        SingleGenerator(modifier, reference, "Repeat Offset Time", 7, 0f);
                        SingleGenerator(modifier, reference, "Speed", 8, 1f);

                        if (!isMulti)
                            BoolGenerator(modifier, reference, "Don't Despawn On Inactive", 9, false);

                        SingleGenerator(modifier, reference, "Time", valueIndex, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", valueIndex + 1, true);

                        BoolGenerator(modifier, reference, "Remove After Despawn", valueIndex + 3);

                        break;
                    }

                case nameof(ModifierFunctions.spawnMultiPrefabCopy): 
                case nameof(ModifierFunctions.spawnPrefabCopy): {
                        var isMulti = name.Contains("Multi");

                        PrefabGroupOnly(modifier, reference);

                        DropdownGenerator(modifier, reference, "Search Prefab Using", 4, CoreHelper.StringToOptionData("Index", "Name", "ID"));
                        StringGenerator(modifier, reference, "Prefab Reference", 0);

                        var str = StringGenerator(modifier, reference, "Prefab Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        if (!isMulti)
                            BoolGenerator(modifier, reference, "Don't Despawn On Inactive", 5, false);

                        SingleGenerator(modifier, reference, "Time", 2, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", 3, true);

                        BoolGenerator(modifier, reference, "Remove After Despawn", isMulti ? 5 : 6);

                        break;
                    }

                case nameof(ModifierFunctions.clearSpawnedPrefabs): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.setPrefabTime): {
                        SingleGenerator(modifier, reference, "Time", 0);
                        BoolGenerator(modifier, reference, "Use Custom Time", 1);

                        break;
                    }
                case nameof(ModifierFunctions.enablePrefab): {
                        BoolGenerator(modifier, reference, "Enabled", 0);

                        break;
                    }
                case nameof(ModifierFunctions.updatePrefab): {
                        BoolGenerator(modifier, reference, "Respawn", 0);

                        break;
                    }
                case nameof(ModifierFunctions.spawnClone): {
                        IntegerGenerator(modifier, reference, "Start Index", 0);
                        IntegerGenerator(modifier, reference, "End Count", 1);
                        IntegerGenerator(modifier, reference, "Increment", 2, 1);
                        SingleGenerator(modifier, reference, "Pos X", 3);
                        SingleGenerator(modifier, reference, "Pos Y", 4);
                        SingleGenerator(modifier, reference, "Pos Z", 5);
                        SingleGenerator(modifier, reference, "Sca X", 6);
                        SingleGenerator(modifier, reference, "Sca Y", 7);
                        SingleGenerator(modifier, reference, "Rot", 8, amount: 15f, multiply: 3f);
                        SingleGenerator(modifier, reference, "Time Offset", 9);
                        StringGenerator(modifier, reference, "Disabled Array", 10);
                        BoolGenerator(modifier, reference, "Use Prefab Offsets", 11);
                        BoolGenerator(modifier, reference, "Copy Offsets", 12);
                        BoolGenerator(modifier, reference, "Disable Self", 13);

                        break;
                    }
                case nameof(ModifierFunctions.spawnCloneMath): {
                        IntegerGenerator(modifier, reference, "Start Index", 0);
                        IntegerGenerator(modifier, reference, "End Count", 1);
                        IntegerGenerator(modifier, reference, "Increment", 2, 1);
                        StringGenerator(modifier, reference, "Pos X Eval", 3);
                        StringGenerator(modifier, reference, "Pos Y Eval", 4);
                        StringGenerator(modifier, reference, "Pos Z Eval", 5);
                        StringGenerator(modifier, reference, "Sca X Eval", 6);
                        StringGenerator(modifier, reference, "Sca Y Eval", 7);
                        StringGenerator(modifier, reference, "Rot Eval", 8);
                        StringGenerator(modifier, reference, "Time Eval", 9);
                        StringGenerator(modifier, reference, "Disabled Array", 10);
                        BoolGenerator(modifier, reference, "Use Prefab Offsets", 11);
                        BoolGenerator(modifier, reference, "Copy Offsets", 12);
                        BoolGenerator(modifier, reference, "Disable Self", 13);

                        break;
                    }

                #endregion

                #region Ranking
                    
                case nameof(ModifierFunctions.unlockAchievement): {
                        var id = StringGenerator(modifier, reference, "ID", 0);
                        EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                            new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => SetValue(0, achievement.id, reference))));

                        break;
                    }
                case nameof(ModifierFunctions.lockAchievement): {
                        var id = StringGenerator(modifier, reference, "ID", 0);
                        EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                            new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => SetValue(0, achievement.id, reference))));

                        break;
                    }
                case nameof(ModifierFunctions.getAchievementUnlocked): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        var id = StringGenerator(modifier, reference, "ID", 1);
                        EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                            new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => SetValue(1, achievement.id, reference))));
                        BoolGenerator(modifier, reference, "Global", 2);

                        break;
                    }
                case nameof(ModifierFunctions.achievementUnlocked): {
                        var id = StringGenerator(modifier, reference, "ID", 0);
                        EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                            new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => SetValue(0, achievement.id, reference))));
                        BoolGenerator(modifier, reference, "Global", 1);

                        break;
                    }

                //case "saveLevelRank): {
                //        break;
                //    }
                //case "clearHits): {
                //        break;
                //    }
                case nameof(ModifierFunctions.addHit): {
                        BoolGenerator(modifier, reference, "Use Self Position", 0, true);
                        StringGenerator(modifier, reference, "Time", 1);

                        break;
                    }
                //case "subHit): {
                //        break;
                //    }
                //case "clearDeaths): {
                //        break;
                //    }
                case nameof(ModifierFunctions.addDeath): {
                        BoolGenerator(modifier, reference, "Use Self Position", 0, true);
                        StringGenerator(modifier, reference, "Time", 1);

                        break;
                    }
                //case "subDeath): {
                //        break;
                //    }

                case nameof(ModifierFunctions.getHitCount): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierFunctions.getDeathCount): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }

                #endregion

                #region Updates

                //case "updateObjects): {
                //        break;
                //    }
                case nameof(ModifierFunctions.updateObject): {
                        BoolGenerator(modifier, reference, "Respawn", 0);
                        BoolGenerator(modifier, reference, "Retain Modifiers", 1);

                        break;
                    }
                case nameof(ModifierFunctions.updateObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Respawn", 1);
                        BoolGenerator(modifier, reference, "Retain Modifiers", 2);

                        break;
                    }
                case nameof(ModifierFunctions.setParent): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.setParentOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, reference, "Clear Parent", 1, false);
                        var str2 = StringGenerator(modifier, reference, "Parent Group To", 2);
                        EditorHelper.AddInputFieldContextMenu(str2.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.detachParent): {
                        BoolGenerator(modifier, reference, "Detach", 0, false);

                        break;
                    }
                case nameof(ModifierFunctions.detachParentOther): {
                        BoolGenerator(modifier, reference, "Detach", 0, false);

                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.setSeed): {
                        StringGenerator(modifier, reference, "Seed", 0);

                        break;
                    }

                #endregion

                #region Physics

                case nameof(ModifierFunctions.setCollision): {
                        BoolGenerator(modifier, reference, "On", 0, false);
                        break;
                    }
                case nameof(ModifierFunctions.setCollisionOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, reference, "On", 0, false);
                        break;
                    }

                #endregion

                #region Checkpoints

                case nameof(ModifierFunctions.getActiveCheckpointIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getLastCheckpointIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getNextCheckpointIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getLastMarkerIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getNextMarkerIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getCheckpointCount): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getMarkerCount): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
                case nameof(ModifierFunctions.getCheckpointTime): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Checkpoint Index", 1);
                        break;
                    }
                case nameof(ModifierFunctions.getMarkerTime): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Marker Index", 1);
                        break;
                    }
                case nameof(ModifierFunctions.createCheckpoint): {
                        SingleGenerator(modifier, reference, "Time", 0);
                        BoolGenerator(modifier, reference, "Time Relative", 1);

                        SingleGenerator(modifier, reference, "Pos X", 2);
                        SingleGenerator(modifier, reference, "Pos Y", 3);

                        BoolGenerator(modifier, reference, "Heal", 4);
                        BoolGenerator(modifier, reference, "Respawn", 5, true);
                        BoolGenerator(modifier, reference, "Reverse On Death", 6, true);
                        BoolGenerator(modifier, reference, "Set Time On Death", 7, true);
                        DropdownGenerator(modifier, reference, "Spawn Position Type", 8, CoreHelper.ToOptionData<Checkpoint.SpawnPositionType>());

                        int a = 0;
                        for (int i = 9; i < modifier.values.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Position {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 2; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            SingleGenerator(modifier, reference, "Pos X", i);
                            SingleGenerator(modifier, reference, "Pos Y", i + 1);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Position Value", () =>
                        {
                            modifier.values.Add("0");
                            modifier.values.Add("0");
                        });

                        break;
                    }
                case nameof(ModifierFunctions.resetCheckpoint): {
                        BoolGenerator(modifier, reference, "Reset to Previous", 0);
                        break;
                    }
                case nameof(ModifierFunctions.setCurrentCheckpoint): {
                        IntegerGenerator(modifier, reference, "Checkpoint Index", 0);
                        break;
                    }

                #endregion

                #region Interfaces

                case nameof(ModifierFunctions.loadInterface): {
                        StringGenerator(modifier, reference, "Path", 0);
                        BoolGenerator(modifier, reference, "Pause Level", 1);
                        BoolGenerator(modifier, reference, "Pass Variables", 2);

                        break;
                    }
                //case nameof(ModifierFunctions.exitInterface): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.pauseLevel): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.quitToMenu): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.quitToArcade): {
                //        break;
                //    }

                #endregion

                #region Player Only

                case nameof(ModifierFunctions.setCustomObjectActive): {
                        StringGenerator(modifier, reference, "ID", 1);
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        BoolGenerator(modifier, reference, "Reset", 2);

                        break;
                    }
                case nameof(ModifierFunctions.setCustomObjectIdle): {
                        StringGenerator(modifier, reference, "ID", 0);
                        BoolGenerator(modifier, reference, "Idle", 1);

                        break;
                    }
                case nameof(ModifierFunctions.setIdleAnimation): {
                        StringGenerator(modifier, reference, "ID", 0);
                        var referenceID = StringGenerator(modifier, reference, "Reference ID", 1);
                        var customPlayerObject = PlayerEditor.inst.CurrentCustomObject;
                        if (!customPlayerObject)
                            break;

                        ITransformable transformable = null;
                        var player = PlayerEditor.inst.CurrentPlayer;
                        if (player && player.RuntimePlayer)
                        {
                            var id = modifier.GetValue(0);
                            if (!string.IsNullOrEmpty(id))
                                transformable = player.RuntimePlayer.customObjects.Find(x => x.id == id);
                        }

                        EditorContextMenu.AddContextMenu(referenceID,
                            new ButtonElement("Select Animation", () => AnimationEditor.inst.OpenPopup(customPlayerObject.animations, PlayerEditor.inst.PlayAnimation, animation =>
                            {
                                referenceID.transform.Find("Input").GetComponent<InputField>().text = animation.ReferenceID;
                            }, transformable)));

                        break;
                    }
                case nameof(ModifierFunctions.playAnimation): {
                        StringGenerator(modifier, reference, "ID", 0);
                        var referenceID = StringGenerator(modifier, reference, "Reference ID", 1);
                        var customPlayerObject = PlayerEditor.inst.CurrentCustomObject;
                        if (!customPlayerObject)
                            break;

                        ITransformable transformable = null;
                        var player = PlayerEditor.inst.CurrentPlayer;
                        if (player && player.RuntimePlayer)
                        {
                            var id = modifier.GetValue(0);
                            if (!string.IsNullOrEmpty(id))
                                transformable = player.RuntimePlayer.customObjects.Find(x => x.id == id);
                        }

                        EditorContextMenu.AddContextMenu(referenceID,
                            new ButtonElement("Select Animation", () => AnimationEditor.inst.OpenPopup(customPlayerObject.animations, PlayerEditor.inst.PlayAnimation, animation =>
                            {
                                referenceID.transform.Find("Input").GetComponent<InputField>().text = animation.ReferenceID;
                            }, transformable)));

                        break;
                    }
                //case nameof(ModifierFunctions.PlayerActions.kill): {
                //        break;
                //    }
                case nameof(ModifierFunctions.hit): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0);
                        break;
                    }
                case nameof(ModifierFunctions.boost): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                //case nameof(ModifierFunctions.shoot): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.pulse): {
                //        break;
                //    }
                //case nameof(ModifierFunctions.jump): {
                //        break;
                //    }
                case nameof(ModifierFunctions.getHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getLives): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getMaxHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getMaxLives): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }
                case nameof(ModifierFunctions.getMove): {
                        StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Y Variable Name", 1, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 2);

                        break;
                    }
                case nameof(ModifierFunctions.getMoveX): {
                        StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getMoveY): {
                        StringGenerator(modifier, reference, "Y Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getLook): {
                        StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Y Variable Name", 1, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 2);

                        break;
                    }
                case nameof(ModifierFunctions.getLookX): {
                        StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 1);

                        break;
                    }
                case nameof(ModifierFunctions.getLookY): {
                        StringGenerator(modifier, reference, "Y Variable Name", 0, renderVariables: false);
                        BoolGenerator(modifier, reference, "Normalize", 1);

                        break;
                    }

                #endregion

                #region Misc

                case nameof(ModifierFunctions.setBGActive): {
                        BoolGenerator(modifier, reference, "Active", 0, false);
                        var str = StringGenerator(modifier, reference, "BG Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierFunctions.signalModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, reference, "Delay", 0, 0f);

                        break;
                    }
                case nameof(ModifierFunctions.activateModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Do Multiple", 1, true);
                        IntegerGenerator(modifier, reference, "Singlular Index", 2, 0);

                        for (int i = 3; i < modifier.values.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Name {i + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                            StringGenerator(modifier, reference, "Modifier Name", groupIndex);
                        }

                        AddGenerator(modifier, reference, "Add Modifier Ref", () =>
                        {
                            modifier.values.Add("modifierName");
                        });

                        break;
                    }

                case nameof(ModifierFunctions.editorNotify): {
                        StringGenerator(modifier, reference, "Text", 0);
                        SingleGenerator(modifier, reference, "Time", 1, 0.5f);
                        DropdownGenerator(modifier, reference, "Notify Type", 2, CoreHelper.StringToOptionData("Info", "Success", "Error", "Warning"));

                        break;
                    }
                case nameof(ModifierFunctions.setWindowTitle): {
                        StringGenerator(modifier, reference, "Title", 0);

                        break;
                    }
                case nameof(ModifierFunctions.setDiscordStatus): {
                        StringGenerator(modifier, reference, "State", 0);
                        StringGenerator(modifier, reference, "Details", 1);
                        DropdownGenerator(modifier, reference, "Sub Icon", 2, CoreHelper.StringToOptionData("Arcade", "Editor", "Play", "Menu"));
                        DropdownGenerator(modifier, reference, "Icon", 3, CoreHelper.StringToOptionData("PA Logo White", "PA Logo Black"));

                        break;
                    }

                case nameof(ModifierFunctions.callModifierBlock): {
                        StringGenerator(modifier, reference, "Function Name", 0);

                        break;
                    }
                case nameof(ModifierFunctions.callModifierBlockTrigger): {
                        StringGenerator(modifier, reference, "Function Name", 0);

                        break;
                    }
                case nameof(ModifierFunctions.callModifiers): {
                        StringGenerator(modifier, reference, "Object Group", 0);

                        break;
                    }
                case nameof(ModifierFunctions.callModifiersTrigger): {
                        StringGenerator(modifier, reference, "Object Group", 0);

                        break;
                    }

                case nameof(ModifierFunctions.forLoop): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        IntegerGenerator(modifier, reference, "Start Index", 1, 0);
                        IntegerGenerator(modifier, reference, "End Count", 2, 10);
                        IntegerGenerator(modifier, reference, "Increment", 3, 1);

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

                case nameof(ModifierFunctions.localVariableContains): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Contains", 1);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableStartsWith): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Starts With", 1);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableEndsWith): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Ends With", 1);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Compare To", 1);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableLesserEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableGreaterEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableLesser): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableGreater): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierFunctions.localVariableExists): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

                        break;
                    }

                #region Float

                case nameof(ModifierFunctions.pitchEquals):
                case nameof(ModifierFunctions.pitchLesserEquals):
                case nameof(ModifierFunctions.pitchGreaterEquals):
                case nameof(ModifierFunctions.pitchLesser):
                case nameof(ModifierFunctions.pitchGreater):
                case nameof(ModifierFunctions.playerDistanceLesser):
                case nameof(ModifierFunctions.playerDistanceGreater): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }

                case nameof(ModifierFunctions.musicTimeGreater):
                case nameof(ModifierFunctions.musicTimeLesser): {
                        SingleGenerator(modifier, reference, "Time", 0, 0f);
                        BoolGenerator(modifier, reference, "Offset From Start Time", 1, false);

                        break;
                    }

                #endregion

                #region String

                case nameof(ModifierFunctions.usernameEquals): {
                        StringGenerator(modifier, reference, "Username", 0);
                        break;
                    }
                case nameof(ModifierFunctions.objectCollide): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.levelPathExists):
                case nameof(ModifierFunctions.realTimeDayWeekEquals): {
                        StringGenerator(modifier, reference, name == "realTimeDayWeekEquals" ? "Day" : "Path", 0);

                        break;
                    }
                case nameof(ModifierFunctions.levelUnlocked):
                case nameof(ModifierFunctions.levelCompletedOther):
                case nameof(ModifierFunctions.levelExists): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }

                #endregion

                #region Integer

                case nameof(ModifierFunctions.mouseButtonDown):
                case nameof(ModifierFunctions.mouseButton):
                case nameof(ModifierFunctions.mouseButtonUp):
                case nameof(ModifierFunctions.playerCountEquals):
                case nameof(ModifierFunctions.playerCountLesserEquals):
                case nameof(ModifierFunctions.playerCountGreaterEquals):
                case nameof(ModifierFunctions.playerCountLesser):
                case nameof(ModifierFunctions.playerCountGreater):
                case nameof(ModifierFunctions.playerHealthEquals):
                case nameof(ModifierFunctions.playerHealthLesserEquals):
                case nameof(ModifierFunctions.playerHealthGreaterEquals):
                case nameof(ModifierFunctions.playerHealthLesser):
                case nameof(ModifierFunctions.playerHealthGreater):
                case nameof(ModifierFunctions.playerDeathsEquals):
                case nameof(ModifierFunctions.playerDeathsLesserEquals):
                case nameof(ModifierFunctions.playerDeathsGreaterEquals):
                case nameof(ModifierFunctions.playerDeathsLesser):
                case nameof(ModifierFunctions.playerDeathsGreater):
                case nameof(ModifierFunctions.variableEquals):
                case nameof(ModifierFunctions.variableLesserEquals):
                case nameof(ModifierFunctions.variableGreaterEquals):
                case nameof(ModifierFunctions.variableLesser):
                case nameof(ModifierFunctions.variableGreater):
                case nameof(ModifierFunctions.variableOtherEquals):
                case nameof(ModifierFunctions.variableOtherLesserEquals):
                case nameof(ModifierFunctions.variableOtherGreaterEquals):
                case nameof(ModifierFunctions.variableOtherLesser):
                case nameof(ModifierFunctions.variableOtherGreater):
                case nameof(ModifierFunctions.playerBoostEquals):
                case nameof(ModifierFunctions.playerBoostLesserEquals):
                case nameof(ModifierFunctions.playerBoostGreaterEquals):
                case nameof(ModifierFunctions.playerBoostLesser):
                case nameof(ModifierFunctions.playerBoostGreater):
                case nameof(ModifierFunctions.realTimeSecondEquals):
                case nameof(ModifierFunctions.realTimeSecondLesserEquals):
                case nameof(ModifierFunctions.realTimeSecondGreaterEquals):
                case nameof(ModifierFunctions.realTimeSecondLesser):
                case nameof(ModifierFunctions.realTimeSecondGreater):
                case nameof(ModifierFunctions.realTimeMinuteEquals):
                case nameof(ModifierFunctions.realTimeMinuteLesserEquals):
                case nameof(ModifierFunctions.realTimeMinuteGreaterEquals):
                case nameof(ModifierFunctions.realTimeMinuteLesser):
                case nameof(ModifierFunctions.realTimeMinuteGreater):
                case nameof(ModifierFunctions.realTime12HourEquals):
                case nameof(ModifierFunctions.realTime12HourLesserEquals):
                case nameof(ModifierFunctions.realTime12HourGreaterEquals):
                case nameof(ModifierFunctions.realTime12HourLesser):
                case nameof(ModifierFunctions.realTime12HourGreater):
                case nameof(ModifierFunctions.realTime24HourEquals):
                case nameof(ModifierFunctions.realTime24HourLesserEquals):
                case nameof(ModifierFunctions.realTime24HourGreaterEquals):
                case nameof(ModifierFunctions.realTime24HourLesser):
                case nameof(ModifierFunctions.realTime24HourGreater):
                case nameof(ModifierFunctions.realTimeDayEquals):
                case nameof(ModifierFunctions.realTimeDayLesserEquals):
                case nameof(ModifierFunctions.realTimeDayGreaterEquals):
                case nameof(ModifierFunctions.realTimeDayLesser):
                case nameof(ModifierFunctions.realTimeDayGreater):
                case nameof(ModifierFunctions.realTimeMonthEquals):
                case nameof(ModifierFunctions.realTimeMonthLesserEquals):
                case nameof(ModifierFunctions.realTimeMonthGreaterEquals):
                case nameof(ModifierFunctions.realTimeMonthLesser):
                case nameof(ModifierFunctions.realTimeMonthGreater):
                case nameof(ModifierFunctions.realTimeYearEquals):
                case nameof(ModifierFunctions.realTimeYearLesserEquals):
                case nameof(ModifierFunctions.realTimeYearGreaterEquals):
                case nameof(ModifierFunctions.realTimeYearLesser):
                case nameof(ModifierFunctions.realTimeYearGreater): {
                        var isGroup = name.Contains("variableOther");
                        if (isGroup)
                            PrefabGroupOnly(modifier, reference);

                        IntegerGenerator(modifier, reference, "Value", 0, 0);

                        if (isGroup)
                        {
                            var str = StringGenerator(modifier, reference, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }

                #endregion

                #region Key

                case nameof(ModifierFunctions.keyPressDown):
                case nameof(ModifierFunctions.keyPress):
                case nameof(ModifierFunctions.keyPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<KeyCode>();
                        DropdownGenerator(modifier, reference, "Key", 0, dropdownData.Key, dropdownData.Value);

                        break;
                    }

                case nameof(ModifierFunctions.controlPressDown):
                case nameof(ModifierFunctions.controlPress):
                case nameof(ModifierFunctions.controlPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<PlayerInputControlType>();
                        DropdownGenerator(modifier, reference, "Button", 0, dropdownData.Key, dropdownData.Value);

                        break;
                    }

                #endregion

                #region Save / Load JSON

                case nameof(ModifierFunctions.loadEquals):
                case nameof(ModifierFunctions.loadLesserEquals):
                case nameof(ModifierFunctions.loadGreaterEquals):
                case nameof(ModifierFunctions.loadLesser):
                case nameof(ModifierFunctions.loadGreater):
                case nameof(ModifierFunctions.loadExists): {
                        if (name == "loadEquals" && modifier.values.Count < 5)
                            modifier.values.Add("0");

                        if (name == "loadEquals" && modifier.GetInt(4, 0) == 0 && !float.TryParse(modifier.GetValue(0), out float abcdef))
                            modifier.SetValue(0, "0");

                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        if (name != "loadExists" && (name != "loadEquals" || modifier.GetInt(4, 0) == 0))
                            SingleGenerator(modifier, reference, "Value", 0, 0f);

                        if (name == "loadEquals" && modifier.GetInt(4, 0) == 1)
                            StringGenerator(modifier, reference, "Value", 0);

                        if (name == "loadEquals")
                            DropdownGenerator(modifier, reference, "Type", 4, CoreHelper.StringToOptionData("Number", "Text"));

                        break;
                    }

                #endregion

                #region Signal

                case nameof(ModifierFunctions.mouseOverSignalModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, reference, "Delay", 0, 0f);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }

                #endregion

                #region Random

                case nameof(ModifierFunctions.randomGreater):
                case nameof(ModifierFunctions.randomLesser):
                case nameof(ModifierFunctions.randomEquals): {
                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);
                        IntegerGenerator(modifier, reference, "Compare To", 0, 0);

                        break;
                    }

                #endregion

                #region Animate

                case nameof(ModifierFunctions.axisEquals):
                case nameof(ModifierFunctions.axisLesserEquals):
                case nameof(ModifierFunctions.axisGreaterEquals):
                case nameof(ModifierFunctions.axisLesser):
                case nameof(ModifierFunctions.axisGreater): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                        SingleGenerator(modifier, reference, "Offset", 5, 0f);
                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        SingleGenerator(modifier, reference, "Loop", 10, 99999f);
                        BoolGenerator(modifier, reference, "Use Visual", 9, false);

                        SingleGenerator(modifier, reference, "Equals", 8, 1f);

                        break;
                    }

                case nameof(ModifierFunctions.eventEquals):
                case nameof(ModifierFunctions.eventLesserEquals):
                case nameof(ModifierFunctions.eventGreaterEquals):
                case nameof(ModifierFunctions.eventLesser):
                case nameof(ModifierFunctions.eventGreater): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Time", 0, 0f);
                        SingleGenerator(modifier, reference, "Equals", 3, 0f);

                        break;
                    }

                #endregion

                #region Level Rank

                case nameof(ModifierFunctions.levelRankEquals):
                case nameof(ModifierFunctions.levelRankLesserEquals):
                case nameof(ModifierFunctions.levelRankGreaterEquals):
                case nameof(ModifierFunctions.levelRankLesser):
                case nameof(ModifierFunctions.levelRankGreater):
                case nameof(ModifierFunctions.levelRankOtherEquals):
                case nameof(ModifierFunctions.levelRankOtherLesserEquals):
                case nameof(ModifierFunctions.levelRankOtherGreaterEquals):
                case nameof(ModifierFunctions.levelRankOtherLesser):
                case nameof(ModifierFunctions.levelRankOtherGreater):
                case nameof(ModifierFunctions.levelRankCurrentEquals):
                case nameof(ModifierFunctions.levelRankCurrentLesserEquals):
                case nameof(ModifierFunctions.levelRankCurrentGreaterEquals):
                case nameof(ModifierFunctions.levelRankCurrentLesser):
                case nameof(ModifierFunctions.levelRankCurrentGreater): {
                        if (name.Contains("Other"))
                            StringGenerator(modifier, reference, "ID", 1);

                        DropdownGenerator(modifier, reference, "Rank", 0, Rank.Null.GetNames().ToList());

                        break;
                    }

                #endregion

                #region Math

                case nameof(ModifierFunctions.mathEquals):
                case nameof(ModifierFunctions.mathLesserEquals):
                case nameof(ModifierFunctions.mathGreaterEquals):
                case nameof(ModifierFunctions.mathLesser):
                case nameof(ModifierFunctions.mathGreater): {
                        StringGenerator(modifier, reference, "First", 0);
                        StringGenerator(modifier, reference, "Second", 1);

                        break;
                    }

                #endregion

                #region Misc

                case nameof(ModifierFunctions.playerCollideIndex): {
                        IntegerGenerator(modifier, reference, "Index", 0);

                        break;
                    }
                case nameof(ModifierFunctions.playerCollideOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierFunctions.playerCollideIndexOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, reference, "Index", 1);

                        break;
                    }

                case nameof(ModifierFunctions.await): {
                        IntegerGenerator(modifier, reference, "Start Time", 0);
                        SingleGenerator(modifier, reference, "Trigger Time", 1);
                        BoolGenerator(modifier, reference, "Use Real Time", 2);

                        break;
                    }
                case nameof(ModifierFunctions.awaitCounter): {
                        IntegerGenerator(modifier, reference, "Start", 0);
                        IntegerGenerator(modifier, reference, "End", 1);
                        IntegerGenerator(modifier, reference, "Amount", 2);

                        break;
                    }

                case nameof(ModifierFunctions.containsTag): {
                        StringGenerator(modifier, reference, "Tag", 0);

                        break;
                    }
                case nameof(ModifierFunctions.objectAlive):
                case nameof(ModifierFunctions.objectSpawned): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierFunctions.languageEquals): {
                        DropdownGenerator(modifier, reference, "Language", 0, CoreHelper.ToOptionData<Language>());

                        break;
                    }

                case nameof(ModifierFunctions.onMarker): {
                        StringGenerator(modifier, reference, "Name", 0);
                        ColorGenerator(modifier, reference, "Color", 1, MarkerEditor.inst.markerColors);
                        IntegerGenerator(modifier, reference, "Layer", 2);

                        break;
                    }
                case nameof(ModifierFunctions.onCheckpoint): {
                        StringGenerator(modifier, reference, "Name", 0);

                        break;
                    }
                case nameof(ModifierFunctions.realTimeEquals): {
                        StringGenerator(modifier, reference, "Format", 0);
                        StringGenerator(modifier, reference, "Equals", 1);

                        break;
                    }
                case nameof(ModifierFunctions.realTimeLesserEquals): 
                case nameof(ModifierFunctions.realTimeGreaterEquals): 
                case nameof(ModifierFunctions.realTimeLesser): 
                case nameof(ModifierFunctions.realTimeGreater): {
                        DropdownGenerator(modifier, reference, "Type", 0, CoreHelper.StringToOptionData("Millisecond", "Second", "Minute", "12 Hour", "24 Hour", "Day", "Month", "Year", "Day of Week", "Day of Year", "Ticks"));
                        IntegerGenerator(modifier, reference, "Compare To", 1);

                        break;
                    }

                #endregion

                #endregion

                #region Dev Only

                case nameof(ModifierFunctions.loadSceneDEVONLY): {
                        StringGenerator(modifier, reference, "Scene", 0);
                        if (modifier.values.Count > 1)
                            BoolGenerator(modifier, reference, "Show Loading", 1, true);

                        break;
                    }
                case nameof(ModifierFunctions.loadStoryLevelDEVONLY): {
                        IntegerGenerator(modifier, reference, "Chapter", 1, 0);
                        IntegerGenerator(modifier, reference, "Level", 2, 0);
                        BoolGenerator(modifier, reference, "Bonus", 0, false);
                        BoolGenerator(modifier, reference, "Skip Cutscene", 3, false);

                        break;
                    }
                case nameof(ModifierFunctions.storySaveBoolDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        BoolGenerator(modifier, reference, "Value", 1, false);
                        break;
                    }
                case nameof(ModifierFunctions.storySaveIntDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        IntegerGenerator(modifier, reference, "Value", 1, 0);
                        break;
                    }
                case nameof(ModifierFunctions.storySaveFloatDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        SingleGenerator(modifier, reference, "Value", 1, 0f);
                        break;
                    }
                case nameof(ModifierFunctions.storySaveStringDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        StringGenerator(modifier, reference, "Value", 1);
                        break;
                    }
                case nameof(ModifierFunctions.storySaveIntVariableDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        break;
                    }
                case nameof(ModifierFunctions.getStorySaveBoolDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        BoolGenerator(modifier, reference, "Default Value", 2, false);
                        break;
                    }
                case nameof(ModifierFunctions.getStorySaveIntDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        IntegerGenerator(modifier, reference, "Default Value", 2, 0);
                        break;
                    }
                case nameof(ModifierFunctions.getStorySaveFloatDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        SingleGenerator(modifier, reference, "Default Value", 2, 0f);
                        break;
                    }
                case nameof(ModifierFunctions.getStorySaveStringDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        StringGenerator(modifier, reference, "Default Value", 2);
                        break;
                    }

                case "storyLoadIntEqualsDEVONLY":
                case "storyLoadIntLesserEqualsDEVONLY":
                case "storyLoadIntGreaterEqualsDEVONLY":
                case "storyLoadIntLesserDEVONLY":
                case "storyLoadIntGreaterDEVONLY": {
                        StringGenerator(modifier, reference, "Load", 0);
                        IntegerGenerator(modifier, reference, "Default", 1, 0);
                        IntegerGenerator(modifier, reference, "Equals", 2, 0);

                        break;
                    }
                case "storyLoadBoolDEVONLY": {
                        StringGenerator(modifier, reference, "Load", 0);
                        BoolGenerator(modifier, reference, "Default", 1, false);

                        break;
                    }

                case nameof(ModifierFunctions.exampleEnableDEVONLY): {
                        BoolGenerator(modifier, reference, "Active", 0, false);
                        break;
                    }
                case nameof(ModifierFunctions.exampleSayDEVONLY): {
                        StringGenerator(modifier, reference, "Dialogue", 0);
                        break;
                    }

                    #endregion
            }
        }

        #region Functions

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
            CoreHelper.Delete(gameObject);
            dialog.modifierCards.RemoveAt(index);
            for (int i = 0; i < dialog.modifierCards.Count; i++)
                dialog.modifierCards[i].index = i;

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

        public void Update(IModifierReference reference) => Update(Modifier, reference);

        public void Update(Modifier modifier, IModifierReference reference)
        {
            if (!modifier)
                return;

            modifier.active = false;
            modifier.runCount = 0;
            modifier.RunInactive(modifier, reference as IModifierReference);
            ModifiersHelper.OnRemoveCache(modifier);
            modifier.Result = default;
        }

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

            var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
            pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
            pathInputField.SetTextWithoutNotify(value);
            pathInputField.onValueChanged.NewListener(_val => onValueChanged?.Invoke(_val));
            pathInputField.onEndEdit.NewListener(_val => onEndEdit?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(pathInputField);

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(path.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.Sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(pathInputField));
            RectValues.Default.AnchoredPosition(154f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);

            return new StringInputElement
            {
                GameObject = path,
                inputField = pathInputField,
                labelsElement = new LabelElement() { GameObject = labelText.gameObject, uiText = labelText },
            };
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

            var startColors = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject.Duplicate(startColorBase.transform, "color");

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

        public GameObject DropdownGenerator(Modifier modifier, IModifierReference reference, string label, Func<string> getValue, Action<string> setValue, List<Dropdown.OptionData> options, List<bool> disabledOptions, Action<int> onSelect = null)
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

        #endregion

        public abstract class Value : Exists
        {
            public Value(int valueIndex) => this.valueIndex = valueIndex;

            public int valueIndex;

            public bool hovered;

            public abstract void Tick(ModifierCard modifierCard, IModifierReference reference);

            public void InitHover(GameObject gameObject)
            {
                var hoverNotifier = gameObject.AddComponent<HoverNotifier>();
                hoverNotifier.notifier = (hovered, pointerEventData) => this.hovered = hovered;
            }
        }

        public class StringValue : Value
        {
            public StringValue(int valueIndex, InputField inputField) : base(valueIndex)
            {
                this.inputField = inputField;
                InitHover(inputField.gameObject);
            }

            public InputField inputField;

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
        }

        public class BoolValue : Value
        {
            public BoolValue(int valueIndex, Toggle toggle) : base(valueIndex)
            {
                this.toggle = toggle;
                InitHover(toggle.gameObject);
            }

            public Toggle toggle;
            public bool defaultValue;

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
        }

        public class DropdownValue : Value
        {
            public DropdownValue(int valueIndex, Dropdown dropdown) : base(valueIndex)
            {
                this.dropdown = dropdown;
                InitHover(dropdown.gameObject);
            }

            public Dropdown dropdown;

            public Func<string, int> getValue;

            public int defaultValue;

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
        }

        public class ColorSlotsValue : Value
        {
            public ColorSlotsValue(int valueIndex, GameObject gameObject, Toggle[] toggles) : base(valueIndex)
            {
                this.toggles = toggles;
                InitHover(gameObject);
            }

            public Toggle[] toggles;
            bool cachedHover;

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
        }
    }
}
