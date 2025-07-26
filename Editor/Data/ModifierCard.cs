using System;
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
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a modifier in the editor.
    /// </summary>
    public class ModifierCard : Exists
    {
        public ModifierCard(Modifier modifier, int index, ModifiersEditorDialog dialog)
        {
            Modifier = modifier;
            this.index = index;
            this.dialog = dialog;
        }

        public GameObject gameObject;

        public Modifier Modifier { get; set; }

        public Transform layout;

        public int index;

        public ModifiersEditorDialog dialog;

        public void RenderModifier<T>(T reference = default)
        {
            if (Modifier is not Modifier modifier)
                return;

            if (reference is not IModifyable modifyable)
                return;

            if (!dialog)
                return;

            var name = modifier.Name;
            var content = dialog.Content;
            var scrollbar = dialog.Scrollbar;

            var gameObject = this.gameObject;

            if (gameObject)
                CoreHelper.Delete(gameObject);

            gameObject = ModifiersEditor.inst.modifierCardPrefab.Duplicate(content, name, index);
            this.gameObject = gameObject;

            TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {(name + " (" + modifier.type.ToString() + ")")}");
            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

            gameObject.transform.localScale = Vector3.one;
            var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
            modifierTitle.text = name;
            EditorThemeManager.ApplyLightText(modifierTitle);

            var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
            collapse.SetIsOnWithoutNotify(modifier.collapse);
            collapse.onValueChanged.NewListener(_val => Collapse(_val, reference));

            TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");
            EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

            for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() => Delete(reference));

            TooltipHelper.AssignTooltip(delete.gameObject, "Delete Modifier");
            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
            copy.button.onClick.NewListener(() => Copy(reference));

            TooltipHelper.AssignTooltip(copy.gameObject, "Copy Modifier");
            EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

            var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
            notifier.modifier = modifier;
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
                    new ButtonFunction("Add", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, dialog: dialog)),
                    new ButtonFunction("Add Above", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, index, dialog)),
                    new ButtonFunction("Add Below", () => ModifiersEditor.inst.OpenDefaultModifiersList(modifyable.ReferenceType, modifyable, index + 1, dialog)),
                    new ButtonFunction("Delete", () => Delete(reference)),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy", () => Copy(reference)),
                    new ButtonFunction("Copy All", () =>
                    {
                        var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                        if (copiedModifiers == null)
                            return;
                        copiedModifiers.Clear();
                        copiedModifiers.AddRange(modifyable.Modifiers.Select(x => x.Copy()));

                        ModifiersEditor.inst.PasteGenerator(modifyable, dialog);
                        EditorManager.inst.DisplayNotification("Copied Modifiers!", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste", () =>
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
                    new ButtonFunction("Paste Above", () =>
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
                    new ButtonFunction("Paste Below", () =>
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
                    new ButtonFunction(true),
                };

                if (!modifyable.OrderModifiers)
                    buttonFunctions.Add(new ButtonFunction("Sort Modifiers", () =>
                    {
                        modifyable.Modifiers = modifyable.Modifiers.OrderBy(x => x.type == Modifier.Type.Action).ToList();

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }));

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

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }),
                    new ButtonFunction("Move Down", () =>
                    {
                        if (index >= modifyable.Modifiers.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                            return;
                        }

                        modifyable.Modifiers.Move(index, index + 1);

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }),
                    new ButtonFunction("Move to Start", () =>
                    {
                        modifyable.Modifiers.Move(index, 0);

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }),
                    new ButtonFunction("Move to End", () =>
                    {
                        modifyable.Modifiers.Move(index, modifyable.Modifiers.Count - 1);

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Update Modifier", () => Update(modifier, reference)),
                    new ButtonFunction(true),
                    new ButtonFunction("Collapse", () => Collapse(true, reference)),
                    new ButtonFunction("Unollapse", () => Collapse(false, reference)),
                    new ButtonFunction("Collapse All", () =>
                    {
                        foreach (var mod in modifyable.Modifiers)
                            mod.collapse = true;

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    }),
                    new ButtonFunction("Uncollapse All", () =>
                    {
                        foreach (var mod in modifyable.Modifiers)
                            mod.collapse = false;

                        CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                    })
                });

                if (ModCompatibility.UnityExplorerInstalled)
                {
                    buttonFunctions.Add(new ButtonFunction(true));
                    buttonFunctions.Add(new ButtonFunction("Inspect", () => ModCompatibility.Inspect(modifier)));
                }

                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
            };

            if (modifier.collapse)
                return;

            layout = gameObject.transform.Find("Layout");

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
                    modifier.Inactive?.Invoke(modifier, reference as IModifierReference, null);
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

            if (modifier.type == Modifier.Type.Trigger)
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
                case nameof(ModifierActions.setActive): {
                        BoolGenerator(modifier, reference, "Active", 0, false);

                        break;
                    }
                case nameof(ModifierActions.setActiveOther): {
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

                case nameof(ModifierActions.setPitch): {
                        SingleGenerator(modifier, reference, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.addPitch): {
                        SingleGenerator(modifier, reference, "Pitch", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setPitchMath): {
                        StringGenerator(modifier, reference, "Pitch", 0);

                        break;
                    }
                case nameof(ModifierActions.addPitchMath): {
                        StringGenerator(modifier, reference, "Pitch", 0);

                        break;
                    }

                case nameof(ModifierActions.setMusicTime): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setMusicTimeMath): {
                        StringGenerator(modifier, reference, "Time", 0);

                        break;
                    }
                //case "setMusicTimeStartTime): {
                //        break;
                //    }
                //case "setMusicTimeAutokill): {
                //        break;
                //    }
                case nameof(ModifierActions.setMusicPlaying): {
                        BoolGenerator(modifier, reference, "Playing", 0, false);

                        break;
                    }

                case nameof(ModifierActions.playSound): {
                        var str = StringGenerator(modifier, reference, "Path", 0);
                        var search = str.transform.Find("Input").gameObject.AddComponent<ContextClickable>();
                        search.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Use Local Browser", () =>
                                {
                                    var global = modifier.GetBool(1, false);
                                    var directory = global && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (global && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                                    if (string.IsNullOrEmpty(result))
                                        return;


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

                                    var global = modifier.GetBool(1, false);
                                    var directory = global && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                    RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                    if (global && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                    {
                                        EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        return;
                                    }

                                    RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                                    {
                                        var global = modifier.GetBool(1, false);
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
                        BoolGenerator(modifier, reference, "Global", 1, false);
                        SingleGenerator(modifier, reference, "Pitch", 2, 1f);
                        SingleGenerator(modifier, reference, "Volume", 3, 1f);
                        BoolGenerator(modifier, reference, "Loop", 4, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 5);

                        break;
                    }
                case nameof(ModifierActions.playSoundOnline): {
                        StringGenerator(modifier, reference, "URL", 0);
                        SingleGenerator(modifier, reference, "Pitch", 1, 1f);
                        SingleGenerator(modifier, reference, "Volume", 2, 1f);
                        BoolGenerator(modifier, reference, "Loop", 3, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 4);
                        break;
                    }
                case nameof(ModifierActions.playDefaultSound): {
                        var dd = ModifiersEditor.inst.dropdownBar.Duplicate(layout, "Sound");
                        dd.transform.localScale = Vector3.one;
                        var labelText = dd.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Sound";

                        CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        CoreHelper.Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

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

                        SingleGenerator(modifier, reference, "Pitch", 1, 1f);
                        SingleGenerator(modifier, reference, "Volume", 2, 1f);
                        BoolGenerator(modifier, reference, "Loop", 3, false);
                        SingleGenerator(modifier, reference, "Pan Stereo", 4);

                        break;
                    }
                case nameof(ModifierActions.audioSource): {
                        var str = StringGenerator(modifier, reference, "Path", 0);
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

                #endregion

                #region Level

                case nameof(ModifierActions.loadLevel): {
                        StringGenerator(modifier, reference, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.loadLevelID): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierActions.loadLevelInternal): {
                        StringGenerator(modifier, reference, "Inner Path", 0);

                        break;
                    }
                //case "loadLevelPrevious): {
                //        break;
                //    }
                //case "loadLevelHub): {
                //        break;
                //    }
                case nameof(ModifierActions.loadLevelInCollection): {
                        StringGenerator(modifier, reference, "ID", 0);

                        break;
                    }
                case nameof(ModifierActions.downloadLevel): {
                        StringGenerator(modifier, reference, "Arcade ID", 0);
                        StringGenerator(modifier, reference, "Server ID", 1);
                        StringGenerator(modifier, reference, "Workshop ID", 2);
                        StringGenerator(modifier, reference, "Song Title", 3);
                        StringGenerator(modifier, reference, "Level Name", 4);
                        BoolGenerator(modifier, reference, "Play Level", 5, true);

                        break;
                    }
                case nameof(ModifierActions.endLevel): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, reference, "End Level Function", 0, options);
                        StringGenerator(modifier, reference, "End Level Data", 1);
                        BoolGenerator(modifier, reference, "Save Player Data", 2, true);

                        break;
                    }
                case nameof(ModifierActions.setAudioTransition): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setIntroFade): {
                        BoolGenerator(modifier, reference, "Should Fade", 0, true);

                        break;
                    }
                case nameof(ModifierActions.setLevelEndFunc): {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        DropdownGenerator(modifier, reference, "End Level Function", 0, options);
                        StringGenerator(modifier, reference, "End Level Data", 1);
                        BoolGenerator(modifier, reference, "Save Player Data", 2, true);

                        break;
                    }

                #endregion

                #region Component

                case nameof(ModifierActions.blur): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Use Opacity", 1, false);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurVariable): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 1, false);

                        break;
                    }
                case nameof(ModifierActions.blurVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurColored): {
                        SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
                        BoolGenerator(modifier, reference, "Use Opacity", 1, false);
                        BoolGenerator(modifier, reference, "Set Back to Normal", 2, false);

                        break;
                    }
                case nameof(ModifierActions.blurColoredOther): {
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
                case nameof(ModifierActions.particleSystem): {
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
                        }, _val =>
                        {
                            var shapeType = (ShapeType)_val;
                            if (shapeType == ShapeType.Text || shapeType == ShapeType.Image || shapeType == ShapeType.Polygon)
                                modifier.SetValue(1, "0");

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
                case nameof(ModifierActions.trailRenderer): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);
                        SingleGenerator(modifier, reference, "Start Width", 1, 1f);
                        SingleGenerator(modifier, reference, "End Width", 2, 0f);
                        ColorGenerator(modifier, reference, "Start Color", 3);
                        SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
                        ColorGenerator(modifier, reference, "End Color", 5);
                        SingleGenerator(modifier, reference, "End Opacity", 6, 0f);

                        break;
                    }
                case nameof(ModifierActions.trailRendererHex): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);
                        SingleGenerator(modifier, reference, "Start Width", 1, 1f);
                        SingleGenerator(modifier, reference, "End Width", 2, 0f);
                        StringGenerator(modifier, reference, "Start Color", 3);
                        StringGenerator(modifier, reference, "End Color", 4);

                        break;
                    }
                case nameof(ModifierActions.rigidbody): {
                        SingleGenerator(modifier, reference, "Gravity", 1, 0f);

                        DropdownGenerator(modifier, reference, "Collision Mode", 2, CoreHelper.StringToOptionData("Discrete", "Continuous"));

                        SingleGenerator(modifier, reference, "Drag", 3, 0f);
                        SingleGenerator(modifier, reference, "Velocity X", 4, 0f);
                        SingleGenerator(modifier, reference, "Velocity Y", 5, 0f);

                        DropdownGenerator(modifier, reference, "Body Type", 6, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));

                        break;
                    }
                case nameof(ModifierActions.rigidbodyOther): {
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
                case nameof(ModifierActions.setRenderType): {
                        DropdownGenerator(modifier, reference, "Render Type", 0, CoreHelper.ToOptionData<BeatmapObject.RenderLayerType>());
                        break;
                    }
                case nameof(ModifierActions.setRenderTypeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        DropdownGenerator(modifier, reference, "Render Type", 1, CoreHelper.ToOptionData<BeatmapObject.RenderLayerType>());
                        break;
                    }

                #endregion

                #region Player

                case nameof(ModifierActions.playerHit): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHitIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        IntegerGenerator(modifier, reference, "Hit Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHitAll): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0, 0);

                        break;
                    }

                case nameof(ModifierActions.playerHeal): {
                        IntegerGenerator(modifier, reference, "Heal Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHealIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        IntegerGenerator(modifier, reference, "Heal Amount", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.playerHealAll): {
                        IntegerGenerator(modifier, reference, "Heal Amount", 0, 0);

                        break;
                    }

                //case "playerKill): {
                //        break;
                //    }
                case nameof(ModifierActions.playerKillIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerKillAll): {
                //        break;
                //    }

                //case "playerRespawn): {
                //        break;
                //    }
                case nameof(ModifierActions.playerRespawnIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

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

                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "X", 1, 0f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);

                        SingleGenerator(modifier, reference, "Duration", 3, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 5, false);

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

                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveX): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveXIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "X", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveXAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveY): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveYIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerMoveYAll): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotate): {
                        SingleGenerator(modifier, reference, "Rotation", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotateIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        SingleGenerator(modifier, reference, "Rotation", 1, 0f);

                        SingleGenerator(modifier, reference, "Duration", 2, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 3, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 4, false);

                        break;
                    }
                case nameof(ModifierActions.playerRotateAll): {
                        SingleGenerator(modifier, reference, "Rotation", 0, 0f);

                        SingleGenerator(modifier, reference, "Duration", 1, 1f);

                        DropdownGenerator(modifier, reference, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Relative", 3, false);

                        break;
                    }

                //case "playerMoveToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveAllToObject): {
                //        break;
                //    }
                //case "playerMoveXToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveXIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveXAllToObject): {
                //        break;
                //    }
                //case "playerMoveYToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerMoveYIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerMoveYAllToObject): {
                //        break;
                //    }
                //case "playerRotateToObject): {
                //        break;
                //    }
                case nameof(ModifierActions.playerRotateIndexToObject): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerRotateAllToObject): {
                //        break;
                //    }

                case nameof(ModifierActions.playerDrag): {
                        BoolGenerator(modifier, reference, "Use Position", 0);
                        BoolGenerator(modifier, reference, "Use Scale", 1);
                        BoolGenerator(modifier, reference, "Use Rotation", 2);

                        break;
                    }

                case nameof(ModifierActions.playerBoost): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                case nameof(ModifierActions.playerBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1);
                        SingleGenerator(modifier, reference, "Y", 2);

                        break;
                    }
                case nameof(ModifierActions.playerBoostAll): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                    
                //case nameof(ModifierActions.playerCancelBoost): {

                //        break;
                //    }
                case nameof(ModifierActions.playerCancelBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case nameof(ModifierActions.playerCancelBoostAll): {

                //        break;
                //    }

                //case "playerDisableBoost): {
                //        break;
                //    }
                case nameof(ModifierActions.playerDisableBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);

                        break;
                    }
                //case "playerDisableBoostAll): {
                //        break;
                //    }
                
                case nameof(ModifierActions.playerEnableBoost): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableBoostIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Enabled", 1, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableBoostAll): {
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableMove): {
                        BoolGenerator(modifier, reference, "Can Move", 0, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 1, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableMoveIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        BoolGenerator(modifier, reference, "Can Move", 1, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 2, true);

                        break;
                    }
                case nameof(ModifierActions.playerEnableMoveAll): {
                        BoolGenerator(modifier, reference, "Can Move", 0, true);
                        BoolGenerator(modifier, reference, "Can Rotate", 1, true);

                        break;
                    }

                case nameof(ModifierActions.playerSpeed): {
                        SingleGenerator(modifier, reference, "Global Speed", 0, 1f);

                        break;
                    }

                case nameof(ModifierActions.playerVelocity): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1, 0f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                    
                case nameof(ModifierActions.playerVelocityX): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityXIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "X", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityXAll): {
                        SingleGenerator(modifier, reference, "X", 0, 0f);

                        break;
                    }
                    
                case nameof(ModifierActions.playerVelocityY): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityYIndex): {
                        IntegerGenerator(modifier, reference, "Player Index", 0, 0);
                        SingleGenerator(modifier, reference, "Y", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.playerVelocityYAll): {
                        SingleGenerator(modifier, reference, "Y", 0, 0f);

                        break;
                    }

                case nameof(ModifierActions.setPlayerModel): {
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: 3);
                        var modelID = StringGenerator(modifier, reference, "Model ID", 0);
                        var contextClickable = modelID.transform.Find("Input").gameObject.GetOrAddComponent<ContextClickable>();
                        contextClickable.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Select model", () =>
                                {
                                    PlayerEditor.inst.ModelsPopup.Open();
                                    CoroutineHelper.StartCoroutine(PlayerEditor.inst.RefreshModels(model =>
                                    {
                                        contextClickable.GetComponent<InputField>().text = model.basePart.id;
                                    }));
                                }));
                        };

                        break;
                    }
                case nameof(ModifierActions.setGameMode): {
                        DropdownGenerator(modifier, reference, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));

                        break;
                    }
                    
                case nameof(ModifierActions.gameMode): {
                        DropdownGenerator(modifier, reference, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));
                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);

                        break;
                    }

                case nameof(ModifierActions.blackHole): {
                        SingleGenerator(modifier, reference, "Value", 0, 1f);
                        BoolGenerator(modifier, reference, "Use Opacity", 1, false);

                        break;
                    }

                #endregion

                #region Mouse Cursor

                case nameof(ModifierActions.showMouse): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Enabled", 0, true);
                        break;
                    }
                //case "hideMouse): {
                //        break;
                //    }
                case nameof(ModifierActions.setMousePosition): {
                        IntegerGenerator(modifier, reference, "Position X", 1, 0);
                        IntegerGenerator(modifier, reference, "Position Y", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.followMousePosition): {
                        SingleGenerator(modifier, reference, "Position Focus", 0, 1f);
                        SingleGenerator(modifier, reference, "Rotation Delay", 1, 1f);

                        break;
                    }

                #endregion

                #region Variable
                    
                case nameof(ModifierActions.getToggle): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        BoolGenerator(modifier, reference, "Value", 1, false);
                        BoolGenerator(modifier, reference, "Invert Value", 2, false);

                        break;
                    }
                case nameof(ModifierActions.getFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Value", 1, 0f);

                        break;
                    }
                case nameof(ModifierActions.getInt): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Value", 1, 0);

                        break;
                    }
                case nameof(ModifierActions.getString): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getStringLower): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getStringUpper): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        ColorGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getEnum): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        var options = new List<string>();
                        for (int i = 3; i < modifier.commands.Count; i += 2)
                            options.Add(modifier.commands[i]);

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
                        for (int i = 3; i < modifier.commands.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Enum Value {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 2; j++)
                                    modifier.commands.RemoveAt(groupIndex);
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
                            modifier.commands.Add($"Enum {a}");
                            modifier.commands.Add(a.ToString());
                        });

                        break;
                    }
                case nameof(ModifierActions.getTag): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Index", 1, 0);

                        break;
                    }

                case nameof(ModifierActions.getAxis): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

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
                case nameof(ModifierActions.getPitch): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getMusicTime): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getMath): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getNearestPlayer): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getCollidingPlayers): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getPlayerHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerPosX): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerPosY): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getPlayerRot): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);

                        break;
                    }
                case nameof(ModifierActions.getEventValue): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Axis", 2, 0);

                        SingleGenerator(modifier, reference, "Delay", 3, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                        SingleGenerator(modifier, reference, "Offset", 5, 0f);
                        SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        SingleGenerator(modifier, reference, "Loop", 8, 99999f);

                        break;
                    }
                case nameof(ModifierActions.getSample): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        SingleGenerator(modifier, reference, "Intensity", 2, 0f);

                        break;
                    }
                case nameof(ModifierActions.getText): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        BoolGenerator(modifier, reference, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierActions.getTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 2);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Variable Name", 0);
                        BoolGenerator(modifier, reference, "Use Visual", 1, false);

                        break;
                    }
                case nameof(ModifierActions.getCurrentKey): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.getColorSlotHexCode): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
                        SingleGenerator(modifier, reference, "Hue", 3);
                        SingleGenerator(modifier, reference, "Saturation", 4);
                        SingleGenerator(modifier, reference, "Value", 5);

                        break;
                    }
                case nameof(ModifierActions.getFloatFromHexCode): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Hex Code", 1);

                        break;
                    }
                case nameof(ModifierActions.getHexCodeFromFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Value", 1, 0f, max: 1f);

                        break;
                    }
                case nameof(ModifierActions.getMixedColors): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        int a = 0;
                        for (int i = 1; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Color {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Color Hex Code", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Color Value", () =>
                        {
                            modifier.commands.Add(RTColors.ColorToHexOptional(RTColors.errorColor));
                        });

                        break;
                    }
                case nameof(ModifierActions.getModifiedColor): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Hex Color", 1);

                        SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
                        SingleGenerator(modifier, reference, "Hue", 3);
                        SingleGenerator(modifier, reference, "Saturation", 4);
                        SingleGenerator(modifier, reference, "Value", 5);

                        break;
                    }
                case nameof(ModifierActions.getVisualColor): {
                        StringGenerator(modifier, reference, "Color 1 Var Name", 0);
                        StringGenerator(modifier, reference, "Color 2 Var Name", 1);

                        break;
                    }
                case nameof(ModifierActions.getJSONString): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.getJSONFloat): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.getJSON): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "JSON", 1);
                        StringGenerator(modifier, reference, "JSON Value", 2);

                        break;
                    }
                case nameof(ModifierActions.getSubString): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        IntegerGenerator(modifier, reference, "Start Index", 1, 0);
                        IntegerGenerator(modifier, reference, "Length", 2, 0);

                        break;
                    }
                case nameof(ModifierActions.getStringLength): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Text", 1);

                        break;
                    }
                case nameof(ModifierActions.getParsedString): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value", 1);

                        break;
                    }
                case nameof(ModifierActions.getSplitString): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Variable {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add String Value", () =>
                        {
                            modifier.commands.Add($"SPLITSTRING_VAR_{a}");
                        });

                        break;
                    }
                case nameof(ModifierActions.getSplitStringAt): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);
                        StringGenerator(modifier, reference, "Variable Name", 2);

                        break;
                    }
                case nameof(ModifierActions.getSplitStringCount): {
                        StringGenerator(modifier, reference, "Text", 0);
                        StringGenerator(modifier, reference, "Character", 1);
                        StringGenerator(modifier, reference, "Variable Name", 2);

                        break;
                    }
                case nameof(ModifierActions.getRegex): {
                        StringGenerator(modifier, reference, "Regex", 0);
                        StringGenerator(modifier, reference, "Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator(a == 0 ? "- Whole Match Variable" : $"- Match Variable {a}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Regex Value", () =>
                        {
                            modifier.commands.Add($"REGEX_VAR_{a}");
                        });

                        break;
                    }
                case nameof(ModifierActions.getFormatVariable): { // whaaaaaaat (found i could use nameof() here)
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Format Text", 1);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Text Arg {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

                            var groupName = StringGenerator(modifier, reference, "Variable Name", i);
                            EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Text Value", () =>
                        {
                            modifier.commands.Add($"Text");
                        });

                        break;
                    }
                case nameof(ModifierActions.getComparison): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Compare From", 1);
                        StringGenerator(modifier, reference, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierActions.getComparisonMath): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Compare From", 1);
                        StringGenerator(modifier, reference, "Compare To", 2);

                        break;
                    }
                case nameof(ModifierActions.getSignaledVariables): {
                        BoolGenerator(modifier, reference, "Clear", 0, true);

                        break;
                    }
                case nameof(ModifierActions.signalLocalVariables): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
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
                            PrefabGroupOnly(modifier, reference);
                        IntegerGenerator(modifier, reference, "Value", 0, 0);

                        if (isGroup)
                        {
                            var str = StringGenerator(modifier, reference, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        }

                        break;
                    }
                case nameof(ModifierActions.animateVariableOther): {
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

                case nameof(ModifierActions.clampVariable): {
                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);

                        break;
                    }
                case nameof(ModifierActions.clampVariableOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);

                        break;
                    }

                #endregion

                #region Enable

                case nameof(ModifierActions.enableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Enabled", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);
                        break;
                    }
                case nameof(ModifierActions.enableObjectTree): {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        BoolGenerator(modifier, reference, "Enabled", 2, true);
                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Enabled", 2, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectTreeOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Enabled", 3, true);
                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 2, true);

                        break;
                    }
                case nameof(ModifierActions.enableObjectGroup): {
                        PrefabGroupOnly(modifier, reference);
                        BoolGenerator(modifier, reference, "Enabled", 0, true);

                        var options = new List<string>() { "All" };
                        for (int i = 2; i < modifier.commands.Count; i++)
                            options.Add(modifier.commands[i]);

                        DropdownGenerator(modifier, reference, "Value", 1, options);

                        int a = 0;
                        for (int i = 2; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

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
                            modifier.commands.Add($"Object Group");
                        });

                        break;
                    }

                case nameof(ModifierActions.disableObject): {
                        if (modifier.GetValue(0) == "0")
                            modifier.SetValue(0, "True");

                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectTree): {
                        if (modifier.value == "0")
                            modifier.value = "False";

                        BoolGenerator(modifier, reference, "Use Self", 0, true);
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Reset", 1, true);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }
                case nameof(ModifierActions.disableObjectTreeOther): {
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

                case nameof(ModifierActions.saveFloat): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        SingleGenerator(modifier, reference, "Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.saveString): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        StringGenerator(modifier, reference, "Value", 0);

                        break;
                    }
                case nameof(ModifierActions.saveText): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.saveVariable): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.loadVariable): {
                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        break;
                    }
                case nameof(ModifierActions.loadVariableOther): {
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

                case nameof(ModifierActions.reactivePos): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveSca): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveRot): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }
                case nameof(ModifierActions.reactiveCol): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0);
                        ColorGenerator(modifier, reference, "Color", 2);

                        break;
                    }
                case nameof(ModifierActions.reactiveColLerp): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0);
                        ColorGenerator(modifier, reference, "Color", 2);

                        break;
                    }
                case nameof(ModifierActions.reactivePosChain): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveScaChain): {
                        SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);

                        IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);

                        SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.reactiveRotChain): {
                        SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);

                        break;
                    }

                #endregion

                #region Events

                case nameof(ModifierActions.eventOffset): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Offset Value", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetVariable): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Multiply Variable", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetMath): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        StringGenerator(modifier, reference, "Evaluation", 0);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetAnimate): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Offset Value", 0, 0f);

                        SingleGenerator(modifier, reference, "Time", 3, 1f);
                        DropdownGenerator(modifier, reference, "Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                        BoolGenerator(modifier, reference, "Relative", 5, false);

                        break;
                    }
                case nameof(ModifierActions.eventOffsetCopyAxis): {
                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
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

                case nameof(ModifierActions.addColor): {
                        ColorGenerator(modifier, reference, "Color", 1);

                        SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        SingleGenerator(modifier, reference, "Value", 4, 0f);

                        SingleGenerator(modifier, reference, "Add Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.addColorOther): {
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
                case nameof(ModifierActions.lerpColor): {
                        ColorGenerator(modifier, reference, "Color", 1);

                        SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        SingleGenerator(modifier, reference, "Value", 4, 0f);

                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.lerpColorOther): {
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
                case nameof(ModifierActions.addColorPlayerDistance): {
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);
                        SingleGenerator(modifier, reference, "Offset", 2, 10f);

                        break;
                    }
                case nameof(ModifierActions.lerpColorPlayerDistance): {
                        ColorGenerator(modifier, reference, "Color", 1);
                        SingleGenerator(modifier, reference, "Multiply", 0, 1f);
                        SingleGenerator(modifier, reference, "Offset", 2, 10f);

                        SingleGenerator(modifier, reference, "Opacity", 3, 1f);
                        SingleGenerator(modifier, reference, "Hue", 4, 0f);
                        SingleGenerator(modifier, reference, "Saturation", 5, 0f);
                        SingleGenerator(modifier, reference, "Value", 6, 0f);

                        break;
                    }
                case nameof(ModifierActions.setOpacity): {
                        SingleGenerator(modifier, reference, "Amount", 0, 1f);

                        break;
                    }
                case nameof(ModifierActions.setOpacityOther): {
                        SingleGenerator(modifier, reference, "Amount", 0, 1f);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.copyColor): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, reference, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierActions.copyColorOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Apply Color 1", 1, true);
                        BoolGenerator(modifier, reference, "Apply Color 2", 2, true);

                        break;
                    }
                case nameof(ModifierActions.applyColorGroup): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        break;
                    }
                case nameof(ModifierActions.setColorHex): {
                        var primaryHexCode = StringGenerator(modifier, reference, "Primary Hex Code", 0);
                        var primaryHexCodeContextMenu = primaryHexCode.AddComponent<ContextClickable>();
                        primaryHexCodeContextMenu.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            var inputField = primaryHexCode.transform.Find("Input").GetComponent<InputField>();
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Edit Color", () =>
                                {
                                    RTColorPicker.inst.Show(RTColors.HexToColor(modifier.GetValue(index)),
                                        (col, hex) =>
                                        {
                                            inputField.SetTextWithoutNotify(hex);
                                        },
                                        (col, hex) =>
                                        {
                                            CoreHelper.Log($"Set timeline object color: {hex}");
                                            // set the input field's text empty so it notices there was a change
                                            inputField.SetTextWithoutNotify(string.Empty);
                                            inputField.text = hex;
                                        }, () =>
                                        {
                                            inputField.SetTextWithoutNotify(modifier.GetValue(index));
                                        });
                                }),
                                new ButtonFunction("Clear", () =>
                                {
                                    inputField.text = string.Empty;
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction("VG Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED;
                                }),
                                new ButtonFunction("VG Red Green", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_GREEN;
                                }),
                                new ButtonFunction("VG Green", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN;
                                }),
                                new ButtonFunction("VG Green Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN_BLUE;
                                }),
                                new ButtonFunction("VG Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.BLUE;
                                }),
                                new ButtonFunction("VG Blue Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_BLUE;
                                }));
                        };

                        var secondaryHexCode = StringGenerator(modifier, reference, "Secondary Hex Code", 1);
                        var secondaryHexCodeContextMenu = secondaryHexCode.AddComponent<ContextClickable>();
                        secondaryHexCodeContextMenu.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            var inputField = secondaryHexCode.transform.Find("Input").GetComponent<InputField>();
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Edit Color", () =>
                                {
                                    RTColorPicker.inst.Show(RTColors.HexToColor(modifier.GetValue(index)),
                                        (col, hex) =>
                                        {
                                            inputField.SetTextWithoutNotify(hex);
                                        },
                                        (col, hex) =>
                                        {
                                            CoreHelper.Log($"Set timeline object color: {hex}");
                                            // set the input field's text empty so it notices there was a change
                                            inputField.SetTextWithoutNotify(string.Empty);
                                            inputField.text = hex;
                                        }, () =>
                                        {
                                            inputField.SetTextWithoutNotify(modifier.GetValue(index));
                                        });
                                }),
                                new ButtonFunction("Clear", () =>
                                {
                                    inputField.text = string.Empty;
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction("VG Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED;
                                }),
                                new ButtonFunction("VG Red Green", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_GREEN;
                                }),
                                new ButtonFunction("VG Green", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN;
                                }),
                                new ButtonFunction("VG Green Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN_BLUE;
                                }),
                                new ButtonFunction("VG Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.BLUE;
                                }),
                                new ButtonFunction("VG Blue Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_BLUE;
                                }));
                        };
                        break;
                    }
                case nameof(ModifierActions.setColorHexOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        var primaryHexCode = StringGenerator(modifier, reference, "Primary Hex Code", 0);
                        var primaryHexCodeContextMenu = primaryHexCode.AddComponent<ContextClickable>();
                        primaryHexCodeContextMenu.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            var inputField = primaryHexCode.transform.Find("Input").GetComponent<InputField>();
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Edit Color", () =>
                                {
                                    RTColorPicker.inst.Show(RTColors.HexToColor(modifier.GetValue(index)),
                                        (col, hex) =>
                                        {
                                            inputField.SetTextWithoutNotify(hex);
                                        },
                                        (col, hex) =>
                                        {
                                            CoreHelper.Log($"Set timeline object color: {hex}");
                                            // set the input field's text empty so it notices there was a change
                                            inputField.SetTextWithoutNotify(string.Empty);
                                            inputField.text = hex;
                                        }, () =>
                                        {
                                            inputField.SetTextWithoutNotify(modifier.GetValue(index));
                                        });
                                }),
                                new ButtonFunction("Clear", () =>
                                {
                                    inputField.text = string.Empty;
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction("VG Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED;
                                }),
                                new ButtonFunction("VG Red Green", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_GREEN;
                                }),
                                new ButtonFunction("VG Green", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN;
                                }),
                                new ButtonFunction("VG Green Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN_BLUE;
                                }),
                                new ButtonFunction("VG Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.BLUE;
                                }),
                                new ButtonFunction("VG Blue Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_BLUE;
                                }));
                        };

                        var secondaryHexCode = StringGenerator(modifier, reference, "Secondary Hex Code", 2);
                        var secondaryHexCodeContextMenu = secondaryHexCode.AddComponent<ContextClickable>();
                        secondaryHexCodeContextMenu.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button != PointerEventData.InputButton.Right)
                                return;

                            var inputField = secondaryHexCode.transform.Find("Input").GetComponent<InputField>();
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction("Edit Color", () =>
                                {
                                    RTColorPicker.inst.Show(RTColors.HexToColor(modifier.GetValue(index)),
                                        (col, hex) =>
                                        {
                                            inputField.SetTextWithoutNotify(hex);
                                        },
                                        (col, hex) =>
                                        {
                                            CoreHelper.Log($"Set timeline object color: {hex}");
                                            // set the input field's text empty so it notices there was a change
                                            inputField.SetTextWithoutNotify(string.Empty);
                                            inputField.text = hex;
                                        }, () =>
                                        {
                                            inputField.SetTextWithoutNotify(modifier.GetValue(index));
                                        });
                                }),
                                new ButtonFunction("Clear", () =>
                                {
                                    inputField.text = string.Empty;
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction("VG Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED;
                                }),
                                new ButtonFunction("VG Red Green", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_GREEN;
                                }),
                                new ButtonFunction("VG Green", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN;
                                }),
                                new ButtonFunction("VG Green Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.GREEN_BLUE;
                                }),
                                new ButtonFunction("VG Blue", () =>
                                {
                                    inputField.text = ObjectEditorData.BLUE;
                                }),
                                new ButtonFunction("VG Blue Red", () =>
                                {
                                    inputField.text = ObjectEditorData.RED_BLUE;
                                }));
                        };

                        break;
                    }
                case nameof(ModifierActions.setColorRGBA): {
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
                case nameof(ModifierActions.setColorRGBAOther): {
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
                case nameof(ModifierActions.animateColorKF): {
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
                        for (int i = 12; i < modifier.commands.Count; i += 14)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Keyframe {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 14; j++)
                                    modifier.commands.RemoveAt(groupIndex);
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
                            DropdownGenerator(modifier, reference, "Easing", i + 13, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
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
                        });

                        break;
                    }
                case nameof(ModifierActions.animateColorKFHex): {
                        SingleGenerator(modifier, reference, "Time", 0);

                        StringGenerator(modifier, reference, "Color 1", 1);
                        StringGenerator(modifier, reference, "Color 2", 2);

                        int a = 0;
                        for (int i = 3; i < modifier.commands.Count; i += 6)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Keyframe {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 6; j++)
                                    modifier.commands.RemoveAt(groupIndex);
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
                            DropdownGenerator(modifier, reference, "Easing", i + 5, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                            BoolGenerator(modifier, reference, "Relative", i + 4, true);

                            StringGenerator(modifier, reference, "Color 1", i + 2);
                            StringGenerator(modifier, reference, "Color 2", i + 3);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Keyframe", () =>
                        {
                            modifier.commands.Add("False"); // collapse keyframe
                            modifier.commands.Add("0"); // keyframe time
                            modifier.commands.Add("0"); // color 1
                            modifier.commands.Add("0"); // color 2
                            modifier.commands.Add("True"); // relative
                            modifier.commands.Add("0"); // easing
                        });

                        break;
                    }

                #endregion

                #region Shape

                case nameof(ModifierActions.setShape): {
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
                case nameof(ModifierActions.setPolygonShape): {
                        SingleGenerator(modifier, reference, "Radius", 0);
                        IntegerGenerator(modifier, reference, "Sides", 1, min: 3, max: 32);
                        SingleGenerator(modifier, reference, "Roundness", 2, max: 1f);
                        SingleGenerator(modifier, reference, "Thickness", 3, max: 1f);
                        SingleGenerator(modifier, reference, "Thick Offset X", 5);
                        SingleGenerator(modifier, reference, "Thick Offset Y", 6);
                        SingleGenerator(modifier, reference, "Thick Scale X", 7);
                        SingleGenerator(modifier, reference, "Thick Scale Y", 8);
                        IntegerGenerator(modifier, reference, "Slices", 4, max: 32);
                        SingleGenerator(modifier, reference, "Angle", 9);

                        break;
                    }
                case nameof(ModifierActions.setPolygonShapeOther): {
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
                        IntegerGenerator(modifier, reference, "Slices", 5, max: 32);
                        SingleGenerator(modifier, reference, "Angle", 10);

                        break;
                    }
                case nameof(ModifierActions.actorFrameTexture): {
                        DropdownGenerator(modifier, reference, "Camera", 0, CoreHelper.StringToOptionData("Foreground", "Background"));
                        IntegerGenerator(modifier, reference, "Width", 1, 512);
                        IntegerGenerator(modifier, reference, "Height", 2, 512);
                        SingleGenerator(modifier, reference, "Pos X", 3, 0f);
                        SingleGenerator(modifier, reference, "Pos Y", 4, 0f);

                        break;
                    }
                case nameof(ModifierActions.setImage): {
                        StringGenerator(modifier, reference, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.setImageOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Path", 0);

                        break;
                    }
                case nameof(ModifierActions.setText): {
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.setTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.addText): {
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.addTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        StringGenerator(modifier, reference, "Text", 0);

                        break;
                    }
                case nameof(ModifierActions.removeText): {
                        IntegerGenerator(modifier, reference, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, reference, "Remove Amount", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextAt): {
                        IntegerGenerator(modifier, reference, "Remove At", 0, 0);

                        break;
                    }
                case nameof(ModifierActions.removeTextOtherAt): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        IntegerGenerator(modifier, reference, "Remove At", 0, 0);

                        break;
                    }
                //case "formatText): {
                //        break;
                //    }
                case nameof(ModifierActions.textSequence): {
                        SingleGenerator(modifier, reference, "Length", 0, 1f);
                        BoolGenerator(modifier, reference, "Display Glitch", 1, true);
                        BoolGenerator(modifier, reference, "Play Sound", 2, true);
                        BoolGenerator(modifier, reference, "Custom Sound", 3, false);
                        StringGenerator(modifier, reference, "Sound Path", 4);
                        BoolGenerator(modifier, reference, "Global", 5, false);

                        SingleGenerator(modifier, reference, "Pitch", 6, 1f);
                        SingleGenerator(modifier, reference, "Volume", 7, 1f);
                        SingleGenerator(modifier, reference, "Pitch Vary", 8, 0f);
                        var str = StringGenerator(modifier, reference, "Custom Text", 9);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
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
                case nameof(ModifierActions.translateShape): {
                        SingleGenerator(modifier, reference, "Pos X", 1, 0f);
                        SingleGenerator(modifier, reference, "Pos Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Sca X", 3, 0f);
                        SingleGenerator(modifier, reference, "Sca Y", 4, 0f);
                        SingleGenerator(modifier, reference, "Rot", 5, 0f, 15f, 3f);

                        break;
                    }

                #endregion

                #region Animation

                case nameof(ModifierActions.animateObject): {
                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Apply Delta Time", 7, true);

                        break;
                    }
                case nameof(ModifierActions.animateObjectOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Apply Delta Time", 8, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignal): {
                        PrefabGroupOnly(modifier, reference);

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        StringGenerator(modifier, reference, "Signal Group", 7);
                        SingleGenerator(modifier, reference, "Signal Delay", 8, 0f);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 9, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 10, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignalOther): {
                        PrefabGroupOnly(modifier, reference);

                        SingleGenerator(modifier, reference, "Time", 0, 1f);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        SingleGenerator(modifier, reference, "X", 2, 0f);
                        SingleGenerator(modifier, reference, "Y", 3, 0f);
                        SingleGenerator(modifier, reference, "Z", 4, 0f);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Signal Group", 8);
                        SingleGenerator(modifier, reference, "Signal Delay", 9, 0f);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 10, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 11, true);

                        break;
                    }
                    
                case nameof(ModifierActions.animateObjectMath): {
                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Apply Delta Time", 7, true);

                        break;
                    }
                case nameof(ModifierActions.animateObjectMathOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        BoolGenerator(modifier, reference, "Apply Delta Time", 8, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignalMath): {
                        PrefabGroupOnly(modifier, reference);

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        StringGenerator(modifier, reference, "Signal Group", 7);
                        StringGenerator(modifier, reference, "Signal Delay", 8);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 9, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 10, true);

                        break;
                    }
                case nameof(ModifierActions.animateSignalMathOther): {
                        PrefabGroupOnly(modifier, reference);

                        StringGenerator(modifier, reference, "Time", 0);

                        DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                        StringGenerator(modifier, reference, "X", 2);
                        StringGenerator(modifier, reference, "Y", 3);
                        StringGenerator(modifier, reference, "Z", 4);

                        BoolGenerator(modifier, reference, "Relative", 5, true);

                        DropdownGenerator(modifier, reference, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                        var str = StringGenerator(modifier, reference, "Object Group", 7);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        StringGenerator(modifier, reference, "Signal Group", 8);
                        StringGenerator(modifier, reference, "Signal Delay", 9);
                        BoolGenerator(modifier, reference, "Signal Deactivate", 10, true);

                        BoolGenerator(modifier, reference, "Apply Delta Time", 11, true);

                        break;
                    }

                case nameof(ModifierActions.gravity): {
                        SingleGenerator(modifier, reference, "X", 1, -1f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, reference, "Curve", 4, 2);

                        break;
                    }
                case nameof(ModifierActions.gravityOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        SingleGenerator(modifier, reference, "X", 1, -1f);
                        SingleGenerator(modifier, reference, "Y", 2, 0f);
                        SingleGenerator(modifier, reference, "Time Multiply", 3, 1f);
                        IntegerGenerator(modifier, reference, "Curve", 4, 2);

                        break;
                    }

                case nameof(ModifierActions.copyAxis): {
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
                case nameof(ModifierActions.copyAxisMath): {
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
                case nameof(ModifierActions.copyAxisGroup): {
                        PrefabGroupOnly(modifier, reference);
                        StringGenerator(modifier, reference, "Expression", 0);

                        DropdownGenerator(modifier, reference, "To Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        DropdownGenerator(modifier, reference, "To Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        int a = 0;
                        for (int i = 3; i < modifier.commands.Count; i += 8)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 8; j++)
                                    modifier.commands.RemoveAt(groupIndex);
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
                            var lastIndex = modifier.commands.Count - 1;

                            modifier.commands.Add($"var_{a}");
                            modifier.commands.Add("Object Group");
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                            modifier.commands.Add("-9999");
                            modifier.commands.Add("9999");
                            modifier.commands.Add("False");
                        });

                        break;
                    }
                case nameof(ModifierActions.copyPlayerAxis): {
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

                case nameof(ModifierActions.legacyTail): {
                        SingleGenerator(modifier, reference, "Total Time", 0, 200f);

                        var path = ModifiersEditor.inst.stringInput.Duplicate(layout, "usage");
                        path.transform.localScale = Vector3.one;
                        var labelText = path.transform.Find("Text").GetComponent<Text>();
                        labelText.text = "Update Object to Update Modifier";
                        path.transform.Find("Text").AsRT().sizeDelta = new Vector2(350f, 32f);
                        CoreHelper.Destroy(path.transform.Find("Input").gameObject);

                        int a = 0;
                        for (int i = 1; i < modifier.commands.Count; i += 3)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Tail Group {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 3; j++)
                                    modifier.commands.RemoveAt(groupIndex);
                            });

                            var str = StringGenerator(modifier, reference, "Object Group", i);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            SingleGenerator(modifier, reference, "Distance", i + 1, 2f);
                            SingleGenerator(modifier, reference, "Time", i + 2, 12f);
                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Group", () =>
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
                        });

                        break;
                    }

                case nameof(ModifierActions.applyAnimationFrom):
                case nameof(ModifierActions.applyAnimationTo):
                case nameof(ModifierActions.applyAnimation): {
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
                case nameof(ModifierActions.applyAnimationFromMath):
                case nameof(ModifierActions.applyAnimationToMath):
                case nameof(ModifierActions.applyAnimationMath): {
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

                case nameof(ModifierActions.spawnPrefab):
                case nameof(ModifierActions.spawnPrefabOffset):
                case nameof(ModifierActions.spawnPrefabOffsetOther):
                case nameof(ModifierActions.spawnMultiPrefab):
                case nameof(ModifierActions.spawnMultiPrefabOffset):
                case nameof(ModifierActions.spawnMultiPrefabOffsetOther): {
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
                            BoolGenerator(modifier, reference, "Permanent", 9, false);

                        SingleGenerator(modifier, reference, "Time", valueIndex, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", valueIndex + 1, true);

                        break;
                    }

                case nameof(ModifierActions.spawnMultiPrefabCopy): 
                case nameof(ModifierActions.spawnPrefabCopy): {
                        var isMulti = name.Contains("Multi");

                        PrefabGroupOnly(modifier, reference);

                        DropdownGenerator(modifier, reference, "Search Prefab Using", 4, CoreHelper.StringToOptionData("Index", "Name", "ID"));
                        StringGenerator(modifier, reference, "Prefab Reference", 0);

                        var str = StringGenerator(modifier, reference, "Prefab Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        if (!isMulti)
                            BoolGenerator(modifier, reference, "Permanent", 5, false);

                        SingleGenerator(modifier, reference, "Time", 2, 0f);
                        BoolGenerator(modifier, reference, "Time Relative", 3, true);

                        break;
                    }

                case nameof(ModifierActions.clearSpawnedPrefabs): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.setPrefabTime): {
                        SingleGenerator(modifier, reference, "Time", 0);
                        BoolGenerator(modifier, reference, "Use Custom Time", 1);

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
                case nameof(ModifierActions.addDeath): {
                        BoolGenerator(modifier, reference, "Use Self Position", 0, true);
                        StringGenerator(modifier, reference, "Time", 1);

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
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.setParent): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.setParentOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, reference, "Clear Parent", 1, false);
                        var str2 = StringGenerator(modifier, reference, "Parent Group To", 2);
                        EditorHelper.AddInputFieldContextMenu(str2.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierActions.detachParent): {
                        BoolGenerator(modifier, reference, "Detach", 0, false);

                        break;
                    }
                case nameof(ModifierActions.detachParentOther): {
                        BoolGenerator(modifier, reference, "Detach", 0, false);

                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                #endregion

                #region Physics

                case nameof(ModifierActions.setCollision): {
                        BoolGenerator(modifier, reference, "On", 0, false);
                        break;
                    }
                case nameof(ModifierActions.setCollisionOther): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        BoolGenerator(modifier, reference, "On", 0, false);
                        break;
                    }

                #endregion

                #region Checkpoints
                    
                case nameof(ModifierActions.createCheckpoint): {
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
                        for (int i = 9; i < modifier.commands.Count; i += 2)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Position {a + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 2; j++)
                                    modifier.commands.RemoveAt(groupIndex);
                            });

                            SingleGenerator(modifier, reference, "Pos X", i);
                            SingleGenerator(modifier, reference, "Pos Y", i + 1);

                            a++;
                        }

                        AddGenerator(modifier, reference, "Add Position Value", () =>
                        {
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                        });

                        break;
                    }
                case nameof(ModifierActions.resetCheckpoint): {
                        BoolGenerator(modifier, reference, "Reset to Previous", 0);
                        break;
                    }

                #endregion

                #region Interfaces

                case nameof(ModifierActions.loadInterface): {
                        StringGenerator(modifier, reference, "Path", 0);
                        BoolGenerator(modifier, reference, "Pause Level", 1);
                        BoolGenerator(modifier, reference, "Pass Variables", 2);

                        break;
                    }
                //case nameof(ModifierActions.exitInterface): {
                //        break;
                //    }
                //case nameof(ModifierActions.pauseLevel): {
                //        break;
                //    }
                //case nameof(ModifierActions.quitToMenu): {
                //        break;
                //    }
                //case nameof(ModifierActions.quitToArcade): {
                //        break;
                //    }

                #endregion

                #region Player Only

                case nameof(ModifierActions.PlayerActions.setCustomObjectActive): {
                        StringGenerator(modifier, reference, "ID", 1);
                        BoolGenerator(modifier, reference, "Enabled", 0);
                        BoolGenerator(modifier, reference, "Reset", 2);

                        break;
                    }
                case nameof(ModifierActions.PlayerActions.setIdleAnimation): {
                        StringGenerator(modifier, reference, "ID", 0);
                        StringGenerator(modifier, reference, "Reference ID", 1);

                        break;
                    }
                case nameof(ModifierActions.PlayerActions.playAnimation): {
                        StringGenerator(modifier, reference, "ID", 0);
                        StringGenerator(modifier, reference, "Reference ID", 1);

                        break;
                    }
                //case nameof(ModifierActions.PlayerActions.kill): {
                //        break;
                //    }
                case nameof(ModifierActions.PlayerActions.hit): {
                        IntegerGenerator(modifier, reference, "Hit Amount", 0);
                        break;
                    }
                case nameof(ModifierActions.PlayerActions.boost): {
                        SingleGenerator(modifier, reference, "X", 0);
                        SingleGenerator(modifier, reference, "Y", 1);

                        break;
                    }
                //case nameof(ModifierActions.PlayerActions.shoot): {
                //        break;
                //    }
                //case nameof(ModifierActions.PlayerActions.pulse): {
                //        break;
                //    }
                //case nameof(ModifierActions.PlayerActions.jump): {
                //        break;
                //    }
                case nameof(ModifierActions.PlayerActions.getHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.PlayerActions.getMaxHealth): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }
                case nameof(ModifierActions.PlayerActions.getIndex): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

                        break;
                    }

                #endregion

                #region Misc

                case nameof(ModifierActions.setBGActive): {
                        BoolGenerator(modifier, reference, "Active", 0, false);
                        var str = StringGenerator(modifier, reference, "BG Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierActions.signalModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, reference, "Delay", 0, 0f);

                        break;
                    }
                case nameof(ModifierActions.activateModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        BoolGenerator(modifier, reference, "Do Multiple", 1, true);
                        IntegerGenerator(modifier, reference, "Singlular Index", 2, 0);

                        for (int i = 3; i < modifier.commands.Count; i++)
                        {
                            int groupIndex = i;
                            var label = LabelGenerator($"- Name {i + 1}");

                            DeleteGenerator(modifier, reference, label.transform, () => modifier.commands.RemoveAt(groupIndex));

                            StringGenerator(modifier, reference, "Modifier Name", groupIndex);
                        }

                        AddGenerator(modifier, reference, "Add Modifier Ref", () =>
                        {
                            modifier.commands.Add("modifierName");
                        });

                        break;
                    }

                case nameof(ModifierActions.editorNotify): {
                        StringGenerator(modifier, reference, "Text", 0);
                        SingleGenerator(modifier, reference, "Time", 1, 0.5f);
                        DropdownGenerator(modifier, reference, "Notify Type", 2, CoreHelper.StringToOptionData("Info", "Success", "Error", "Warning"));

                        break;
                    }
                case nameof(ModifierActions.setWindowTitle): {
                        StringGenerator(modifier, reference, "Title", 0);

                        break;
                    }
                case nameof(ModifierActions.setDiscordStatus): {
                        StringGenerator(modifier, reference, "State", 0);
                        StringGenerator(modifier, reference, "Details", 1);
                        DropdownGenerator(modifier, reference, "Sub Icon", 2, CoreHelper.StringToOptionData("Arcade", "Editor", "Play", "Menu"));
                        DropdownGenerator(modifier, reference, "Icon", 3, CoreHelper.StringToOptionData("PA Logo White", "PA Logo Black"));

                        break;
                    }

                case nameof(ModifierActions.callModifierBlock): {
                        StringGenerator(modifier, reference, "Function Name", 0);

                        break;
                    }

                case "forLoop": {
                        StringGenerator(modifier, reference, "Variable Name", 0);
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

                case nameof(ModifierTriggers.localVariableContains): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Contains", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableStartsWith): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Starts With", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableEndsWith): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Ends With", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Compare To", 1);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableLesserEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableGreaterEquals): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableLesser): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableGreater): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        SingleGenerator(modifier, reference, "Compare To", 1, 0);

                        break;
                    }
                case nameof(ModifierTriggers.localVariableExists): {
                        StringGenerator(modifier, reference, "Variable Name", 0);

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
                        SingleGenerator(modifier, reference, "Value", 0, 1f);

                        break;
                    }

                case nameof(ModifierTriggers.musicTimeGreater):
                case nameof(ModifierTriggers.musicTimeLesser): {
                        SingleGenerator(modifier, reference, "Time", 0, 0f);
                        BoolGenerator(modifier, reference, "Offset From Start Time", 1, false);

                        break;
                    }

                #endregion

                #region String

                case nameof(ModifierTriggers.usernameEquals): {
                        StringGenerator(modifier, reference, "Username", 0);
                        break;
                    }
                case nameof(ModifierTriggers.objectCollide): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }
                case nameof(ModifierTriggers.levelPathExists):
                case nameof(ModifierTriggers.realTimeDayWeekEquals): {
                        StringGenerator(modifier, reference, name == "realTimeDayWeekEquals" ? "Day" : "Path", 0);

                        break;
                    }
                case nameof(ModifierTriggers.levelUnlocked):
                case nameof(ModifierTriggers.levelCompletedOther):
                case nameof(ModifierTriggers.levelExists): {
                        StringGenerator(modifier, reference, "ID", 0);

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

                case nameof(ModifierTriggers.keyPressDown):
                case nameof(ModifierTriggers.keyPress):
                case nameof(ModifierTriggers.keyPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<KeyCode>();
                        DropdownGenerator(modifier, reference, "Key", 0, dropdownData.Key, dropdownData.Value);

                        break;
                    }

                case nameof(ModifierTriggers.controlPressDown):
                case nameof(ModifierTriggers.controlPress):
                case nameof(ModifierTriggers.controlPressUp): {
                        var dropdownData = CoreHelper.ToDropdownData<PlayerInputControlType>();
                        DropdownGenerator(modifier, reference, "Button", 0, dropdownData.Key, dropdownData.Value);

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
                        if (name == "loadEquals" && modifier.commands.Count < 5)
                            modifier.commands.Add("0");

                        if (name == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 0 && !float.TryParse(modifier.value, out float abcdef))
                            modifier.value = "0";

                        StringGenerator(modifier, reference, "Path", 1);
                        StringGenerator(modifier, reference, "JSON 1", 2);
                        StringGenerator(modifier, reference, "JSON 2", 3);

                        if (name != "loadExists" && (name != "loadEquals" || Parser.TryParse(modifier.commands[4], 0) == 0))
                            SingleGenerator(modifier, reference, "Value", 0, 0f);

                        if (name == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 1)
                            StringGenerator(modifier, reference, "Value", 0);

                        if (name == "loadEquals")
                            DropdownGenerator(modifier, reference, "Type", 4, CoreHelper.StringToOptionData("Number", "Text"));

                        break;
                    }

                #endregion

                #region Signal

                case nameof(ModifierTriggers.mouseOverSignalModifier): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 1);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                        SingleGenerator(modifier, reference, "Delay", 0, 0f);

                        LabelGenerator(ModifiersHelper.DEPRECATED_MESSAGE);
                        break;
                    }

                #endregion

                #region Random

                case nameof(ModifierTriggers.randomGreater):
                case nameof(ModifierTriggers.randomLesser):
                case nameof(ModifierTriggers.randomEquals): {
                        IntegerGenerator(modifier, reference, "Minimum", 1, 0);
                        IntegerGenerator(modifier, reference, "Maximum", 2, 0);
                        IntegerGenerator(modifier, reference, "Compare To", 0, 0);

                        break;
                    }

                #endregion

                #region Animate

                case nameof(ModifierTriggers.axisEquals):
                case nameof(ModifierTriggers.axisLesserEquals):
                case nameof(ModifierTriggers.axisGreaterEquals):
                case nameof(ModifierTriggers.axisLesser):
                case nameof(ModifierTriggers.axisGreater): {
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

                case nameof(ModifierTriggers.eventEquals):
                case nameof(ModifierTriggers.eventLesserEquals):
                case nameof(ModifierTriggers.eventGreaterEquals):
                case nameof(ModifierTriggers.eventLesser):
                case nameof(ModifierTriggers.eventGreater): {
                        DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                        IntegerGenerator(modifier, reference, "Value Index", 2, 0);
                        SingleGenerator(modifier, reference, "Time", 0, 0f);
                        SingleGenerator(modifier, reference, "Equals", 3, 0f);

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
                        if (name.Contains("Other"))
                            StringGenerator(modifier, reference, "ID", 1);

                        DropdownGenerator(modifier, reference, "Rank", 0, Rank.Null.GetNames().ToList());

                        break;
                    }

                #endregion

                #region Math

                case nameof(ModifierTriggers.mathEquals):
                case nameof(ModifierTriggers.mathLesserEquals):
                case nameof(ModifierTriggers.mathGreaterEquals):
                case nameof(ModifierTriggers.mathLesser):
                case nameof(ModifierTriggers.mathGreater): {
                        StringGenerator(modifier, reference, "First", 0);
                        StringGenerator(modifier, reference, "Second", 1);

                        break;
                    }

                #endregion

                #region Misc

                case nameof(ModifierTriggers.playerCollideIndex): {
                        IntegerGenerator(modifier, reference, "Index", 0);

                        break;
                    }

                case nameof(ModifierTriggers.containsTag): {
                        StringGenerator(modifier, reference, "Tag", 0);

                        break;
                    }
                case nameof(ModifierTriggers.objectAlive):
                case nameof(ModifierTriggers.objectSpawned): {
                        PrefabGroupOnly(modifier, reference);
                        var str = StringGenerator(modifier, reference, "Object Group", 0);
                        EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                        break;
                    }

                case nameof(ModifierTriggers.languageEquals): {
                        var options = new List<Dropdown.OptionData>();

                        var languages = Enum.GetValues(typeof(Language));

                        for (int i = 0; i < languages.Length; i++)
                            options.Add(new Dropdown.OptionData(Enum.GetName(typeof(Language), i) ?? "Invalid Value"));

                        DropdownGenerator(modifier, reference, "Language", 0, options);

                        break;
                    }

                #endregion

                #endregion

                #region Dev Only

                case nameof(ModifierActions.loadSceneDEVONLY): {
                        StringGenerator(modifier, reference, "Scene", 0);
                        if (modifier.commands.Count > 1)
                            BoolGenerator(modifier, reference, "Show Loading", 1, true);

                        break;
                    }
                case nameof(ModifierActions.loadStoryLevelDEVONLY): {
                        IntegerGenerator(modifier, reference, "Chapter", 1, 0);
                        IntegerGenerator(modifier, reference, "Level", 2, 0);
                        BoolGenerator(modifier, reference, "Bonus", 0, false);
                        BoolGenerator(modifier, reference, "Skip Cutscene", 3, false);

                        break;
                    }
                case nameof(ModifierActions.storySaveBoolDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        BoolGenerator(modifier, reference, "Value", 1, false);
                        break;
                    }
                case nameof(ModifierActions.storySaveIntDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        IntegerGenerator(modifier, reference, "Value", 1, 0);
                        break;
                    }
                case nameof(ModifierActions.storySaveFloatDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        SingleGenerator(modifier, reference, "Value", 1, 0f);
                        break;
                    }
                case nameof(ModifierActions.storySaveStringDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        StringGenerator(modifier, reference, "Value", 1);
                        break;
                    }
                case nameof(ModifierActions.storySaveIntVariableDEVONLY): {
                        StringGenerator(modifier, reference, "Save", 0);
                        break;
                    }
                case nameof(ModifierActions.getStorySaveBoolDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        BoolGenerator(modifier, reference, "Default Value", 2, false);
                        break;
                    }
                case nameof(ModifierActions.getStorySaveIntDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        IntegerGenerator(modifier, reference, "Default Value", 2, 0);
                        break;
                    }
                case nameof(ModifierActions.getStorySaveFloatDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
                        StringGenerator(modifier, reference, "Value Name", 1);
                        SingleGenerator(modifier, reference, "Default Value", 2, 0f);
                        break;
                    }
                case nameof(ModifierActions.getStorySaveStringDEVONLY): {
                        StringGenerator(modifier, reference, "Variable Name", 0);
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

                case nameof(ModifierActions.exampleEnableDEVONLY): {
                        BoolGenerator(modifier, reference, "Active", 0, false);
                        break;
                    }
                case nameof(ModifierActions.exampleSayDEVONLY): {
                        StringGenerator(modifier, reference, "Dialogue", 0);
                        break;
                    }

                    #endregion
            }
        }

        #region Functions

        public void Collapse<T>(bool collapse, T reference)
        {
            Modifier.collapse = collapse;
            RenderModifier(reference);
            CoroutineHelper.PerformAtEndOfFrame(() => LayoutRebuilder.ForceRebuildLayoutImmediate(dialog.Content.AsRT()));
        }

        public void Delete<T>(T reference)
        {
            if (reference is not IModifyable modifyable)
                return;

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

        public void Copy<T>(T reference)
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

        public void Update<T>(T reference) => Update(Modifier as Modifier, reference);

        public void Update<T>(Modifier modifier, T reference)
        {
            if (!modifier)
                return;

            modifier.active = false;
            modifier.runCount = 0;
            modifier.Inactive?.Invoke(modifier, reference as IModifierReference, null);
        }

        #endregion

        #region Generators

        public void PrefabGroupOnly<T>(Modifier modifier, T reference)
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

            TriggerHelper.InversableField(inputField);
            result = inputField;
            return single;
        }

        public GameObject SingleGenerator<T>(Modifier modifier, T reference, string label, int type, float defaultValue = 0f, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f)
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
                            if (reference is IModifyable modifyable)
                                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                            RTEditor.inst.HideNameEditor();
                            Update(modifier, reference);
                        });
                    }));
            };

            return single;
        }

        public GameObject IntegerGenerator<T>(Modifier modifier, T reference, string label, int type, int defaultValue = 0, int amount = 1, int min = 0, int max = 0)
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
                            if (reference is IModifyable modifyable)
                                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                            RTEditor.inst.HideNameEditor();
                            Update(modifier, reference);
                        });
                    }));
            };

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
            globalToggle.onValueChanged.ClearAll();
            globalToggle.isOn = value;
            globalToggle.onValueChanged.AddListener(_val => action?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyToggle(globalToggle);

            toggle = globalToggle;
            return global;
        }

        public GameObject BoolGenerator<T>(Modifier modifier, T reference, string label, int type, bool defaultValue = false)
        {
            var global = BoolGenerator(label, modifier.GetBool(type, defaultValue), _val =>
            {
                modifier.SetValue(type, _val.ToString());

                Update(modifier, reference);
            }, out Toggle globalToggle);
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
                            if (reference is IModifyable modifyable)
                                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                            RTEditor.inst.HideNameEditor();
                            Update(modifier, reference);
                        });
                    }));
            };

            return global;
        }

        public GameObject StringGenerator(Transform layout, string label, string value, Action<string> onValueChanged, Action<string> onEndEdit = null)
        {
            var path = ModifiersEditor.inst.stringInput.Duplicate(layout, label);
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
            buttonStorage.button.onClick.NewListener(() => RTTextEditor.inst.SetInputField(pathInputField));
            RectValues.Default.AnchoredPosition(154f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(buttonStorage.baseImage.rectTransform);

            return path;
        }

        public GameObject StringGenerator<T>(Modifier modifier, T reference, string label, int type, Action<string> onEndEdit = null) => StringGenerator(layout, label, modifier.GetValue(type), _val =>
        {
            modifier.SetValue(type, _val);

            Update(modifier, reference);
        }, onEndEdit);

        public void SetObjectColors<T>(Toggle[] toggles, int index, int currentValue, Modifier modifier, T reference, List<Color> colors)
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

        public GameObject ColorGenerator<T>(Modifier modifier, T reference, string label, int type, int colorSource = 0)
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
                                if (reference is IModifyable modifyable)
                                    CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                                RTEditor.inst.HideNameEditor();
                                Update(modifier, reference);
                            });
                        }));
                };
            }

            CoreHelper.Delete(colorPrefab);

            EditorThemeManager.ApplyLightText(labelText);
            SetObjectColors(toggles, type, modifier.GetInt(type, -1), modifier, reference, colors);

            return startColorBase;
        }

        public GameObject DropdownGenerator<T>(Modifier modifier, T reference, string label, int type, List<string> options, Action<int> onSelect = null) => DropdownGenerator(modifier, reference, label, type, options.Select(x => new Dropdown.OptionData(x)).ToList(), null, onSelect);

        public GameObject DropdownGenerator<T>(Modifier modifier, T reference, string label, int type, List<Dropdown.OptionData> options, List<bool> disabledOptions = null, Action<int> onSelect = null)
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
            dropdown.onValueChanged.ClearAll();
            dropdown.options.Clear();
            dropdown.options = options;
            dropdown.value = modifier.GetInt(type, 0);
            dropdown.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val.ToString());
                onSelect?.Invoke(_val);

                Update(modifier, reference);
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
                            if (reference is IModifyable modifyable)
                                CoroutineHelper.StartCoroutine(dialog.RenderModifiers(modifyable));
                            RTEditor.inst.HideNameEditor();
                            Update(modifier, reference);
                        });
                    }));
            };

            return dd;
        }

        public GameObject DeleteGenerator<T>(Modifier modifier, T reference, Transform parent, Action onDelete)
        {
            var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(parent, "delete");
            deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
            var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
            deleteGroupButton.button.onClick.NewListener(() =>
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

            EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);
            return deleteGroup;
        }

        public GameObject AddGenerator<T>(Modifier modifier, T reference, string text, Action onAdd)
        {
            var baseAdd = Creator.NewUIObject("add", layout);
            baseAdd.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(baseAdd.transform, "add");
            var addText = add.transform.GetChild(0).GetComponent<Text>();
            addText.text = text;
            RectValues.Default.AnchoredPosition(105f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(add.transform.AsRT());

            var addButton = add.GetComponent<Button>();
            addButton.onClick.NewListener(() =>
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

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
            return baseAdd;
        }

        #endregion
    }
}
