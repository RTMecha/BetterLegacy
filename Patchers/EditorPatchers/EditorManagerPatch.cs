using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Example;
using BetterLegacy.Menus;
using CielaSpike;
using Crosstales.FB;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(EditorManager))]
    public class EditorManagerPatch : MonoBehaviour
    {
        static EditorManager Instance { get => EditorManager.inst; set => EditorManager.inst = value; }

        static bool April => CoreHelper.AprilFools;

        [HarmonyPatch(nameof(EditorManager.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(EditorManager __instance)
        {
            if (!Instance)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            FontManager.inst.ChangeAllFontsInEditor();

            InputDataManager.inst.BindMenuKeys();
            InputDataManager.inst.BindEditorKeys();
            __instance.ScreenScale = Screen.width / 1920f;
            __instance.ScreenScaleInverse = 1f / __instance.ScreenScale;
            __instance.RefreshDialogDictionary();

            var easingDropdowns = (from x in Resources.FindObjectsOfTypeAll<Dropdown>()
                                   where x.gameObject != null && x.gameObject.name == "curves"
                                   select x).ToList();

            RTEditor.EasingDropdowns.Clear();

            var easings = __instance.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

            foreach (var dropdown in easingDropdowns)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(easings);

                TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                {
                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                        return;

                    var pointerEventData = (PointerEventData)baseEventData;
                    if (pointerEventData.scrollDelta.y > 0f)
                        dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
                    if (pointerEventData.scrollDelta.y < 0f)
                        dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
                }));
            }

            RTEditor.EasingDropdowns = easingDropdowns;

            if (Updater.levelProcessor)
            {
                Updater.levelProcessor.Dispose();
                Updater.levelProcessor = null;
            }

            Menus.InterfaceManager.inst.StopMusic();
            Menus.InterfaceManager.inst.CloseMenus();
            Menus.InterfaceManager.inst.Clear();
            CoreHelper.InStory = false;
            if (ExampleManager.inst)
                ExampleManager.inst.SetActive(true); // if Example was disabled

            #region Editor Theme Setup

            EditorThemeManager.AddGraphic(EditorManager.inst.timeline.transform.parent.Find("Panel 2").GetComponent<Image>(), ThemeGroup.Timeline_Background);

            var openFilePopup = __instance.GetDialog("Open File Popup").Dialog.gameObject;
            EditorThemeManager.AddGraphic(openFilePopup.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

            var openFilePopupPanel = openFilePopup.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(openFilePopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var openFilePopupClose = openFilePopupPanel.transform.Find("x").gameObject;
            var openFilePopupCloseButton = openFilePopupClose.GetComponent<Button>();
            openFilePopupCloseButton.onClick.AddListener(() => { RTEditor.inst.choosingLevelTemplate = false; });
            EditorThemeManager.AddSelectable(openFilePopupCloseButton, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(openFilePopupClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(openFilePopupPanel.transform.Find("Text").GetComponent<Text>());

            EditorThemeManager.AddScrollbar(openFilePopup.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            EditorThemeManager.AddInputField(openFilePopup.transform.Find("search-box/search").GetComponent<InputField>(), ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

            EditorThemeManager.AddGraphic(EditorManager.inst.dialogs.GetComponent<Image>(), ThemeGroup.Background_1);

            var titleBar = EditorManager.inst.GUIMain.transform.Find("TitleBar").gameObject;
            EditorThemeManager.AddGraphic(titleBar.GetComponent<Image>(), ThemeGroup.Background_1);

            for (int i = 0; i < titleBar.transform.childCount; i++)
            {
                var child = titleBar.transform.GetChild(i).gameObject;
                EditorThemeManager.AddSelectable(child.GetComponent<Button>(), ThemeGroup.Title_Bar_Button, false);

                var text = child.transform.GetChild(0).gameObject;
                EditorThemeManager.AddGraphic(child.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Title_Bar_Text);

                if (child.transform.childCount > 1)
                {
                    var dropdownBase = child.transform.GetChild(1).gameObject;

                    dropdownBase.AddComponent<Mask>();

                    EditorThemeManager.AddGraphic(dropdownBase.GetComponent<Image>(), ThemeGroup.Title_Bar_Dropdown_Normal, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

                    for (int j = 0; j < dropdownBase.transform.childCount; j++)
                    {
                        var childB = dropdownBase.transform.GetChild(j).gameObject;
                        EditorThemeManager.AddSelectable(childB.GetComponent<Button>(), ThemeGroup.Title_Bar_Dropdown, false);

                        var text2 = childB.transform.GetChild(0).gameObject;
                        EditorThemeManager.AddGraphic(childB.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Title_Bar_Text);

                        var image = childB.transform.Find("Image").gameObject;
                        EditorThemeManager.AddGraphic(childB.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Title_Bar_Text);
                    }
                }
            }

            var saveAsPopup = __instance.GetDialog("Save As Popup").Dialog.GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(saveAsPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var saveAsPopupPanel = saveAsPopup.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(saveAsPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var saveAsPopupClose = saveAsPopupPanel.transform.Find("x").gameObject;
            Destroy(saveAsPopupClose.GetComponent<Animator>());
            var saveAsPopupCloseButton = saveAsPopupClose.GetComponent<Button>();
            saveAsPopupCloseButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(saveAsPopupCloseButton, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(saveAsPopupClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(saveAsPopupPanel.transform.Find("Text").GetComponent<Text>());
            EditorThemeManager.AddLightText(saveAsPopup.transform.Find("Level Name").GetComponent<Text>());

            EditorThemeManager.AddInputField(saveAsPopup.transform.Find("level-name").GetComponent<InputField>(), ThemeGroup.Input_Field);

            var create = saveAsPopup.transform.Find("submit").GetComponent<Button>();
            Destroy(create.GetComponent<Animator>());
            create.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddGraphic(create.image, ThemeGroup.Add, true);

            var createText = create.transform.Find("text").gameObject;
            EditorThemeManager.AddGraphic(create.transform.Find("text").GetComponent<Text>(), ThemeGroup.Add_Text);

            var fileInfoPopup = __instance.GetDialog("File Info Popup").Dialog.gameObject;
            EditorThemeManager.AddGraphic(fileInfoPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);

            EditorThemeManager.AddLightText(fileInfoPopup.transform.Find("title").GetComponent<Text>());

            EditorThemeManager.AddLightText(fileInfoPopup.transform.Find("text").GetComponent<Text>());

            var parentSelectorPopup = EditorManager.inst.GetDialog("Parent Selector").Dialog;
            EditorThemeManager.AddGraphic(parentSelectorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var parentSelectorPopupPanel = parentSelectorPopup.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(parentSelectorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(parentSelectorPopupPanel.transform.Find("x").GetComponent<Button>(), ThemeGroup.Close);

            var parentSelectorPopupCloseX = parentSelectorPopupPanel.transform.Find("x").GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(parentSelectorPopupPanel.transform.Find("x").GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(parentSelectorPopupPanel.transform.Find("Text").GetComponent<Text>());

            EditorThemeManager.AddScrollbar(parentSelectorPopup.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            EditorThemeManager.AddInputField(parentSelectorPopup.transform.Find("search-box/search").GetComponent<InputField>(), ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

            #endregion

            // Add function button storage to folder button.
            var levelButtonPrefab = __instance.folderButtonPrefab.Duplicate(__instance.transform, __instance.folderButtonPrefab.name);
            var levelButtonPrefabStorage = levelButtonPrefab.AddComponent<FunctionButtonStorage>();
            levelButtonPrefabStorage.button = levelButtonPrefab.GetComponent<Button>();
            levelButtonPrefabStorage.text = levelButtonPrefab.transform.GetChild(0).GetComponent<Text>();
            __instance.folderButtonPrefab = levelButtonPrefab;

            RTEditor.Init(__instance);

            try
            {
                GameManager.inst.playerGUI.transform.Find("Interface").gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not set interface inactive.\n{ex}");
            }

            // New Level Name input field contains text but newLevelName does not, so people might end up making an empty named level if they don't name it anything else.
            __instance.newLevelName = "New Awesome Beatmap";

            __instance.hasLoadedLevel = false;
            __instance.loading = false;

            ExampleManager.onEditorAwake?.Invoke(__instance);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.GetLevelList();

            CoreHelper.UpdateDiscordStatus("", "In Editor (Selecting level)", "editor");

            Instance.SetDialogStatus("Timeline", true, true);

            InputDataManager.inst.players.Clear();
            InputDataManager.inst.players.Add(PlayerManager.CreateDefaultPlayer());

            Instance.GUI.SetActive(false);
            Instance.canEdit = DataManager.inst.GetSettingBool("CanEdit", false);

            Instance.isEditing = true;
            SteamWrapper.inst.user.displayName = CoreConfig.Instance.DisplayName.Value;
            Instance.SetCreatorName(SteamWrapper.inst.user.displayName);
            Instance.SetShowHelp(EditorConfig.Instance.ShowHelpOnStartup.Value);

            RTEditor.inst.mouseTooltip?.SetActive(false);

            LoadBaseLevelPrefix();
            Instance.Zoom = 0.05f;
            Instance.SetLayer(0);
            Instance.DisplayNotification("Base Level Loaded", 2f, EditorManager.NotificationType.Info);

            InputDataManager.inst.editorActions.Cut.ClearBindings();
            InputDataManager.inst.editorActions.Copy.ClearBindings();
            InputDataManager.inst.editorActions.Paste.ClearBindings();
            InputDataManager.inst.editorActions.Duplicate.ClearBindings();
            InputDataManager.inst.editorActions.Delete.ClearBindings();
            InputDataManager.inst.editorActions.Undo.ClearBindings();
            InputDataManager.inst.editorActions.Redo.ClearBindings();
            InputDataManager.inst.editorActions.CreateMarker.ClearBindings();

            //Set Editor Zoom cap
            Instance.zoomBounds = EditorConfig.Instance.MainZoomBounds.Value;

            try
            {
                CoreHelper.StartCoroutine(RTEditor.inst.AssignTimelineTexture());
                Instance.UpdateTimelineSizes();
                Instance.firstOpened = true;

                RTEventEditor.inst.CreateEventObjects();
                CheckpointEditor.inst.CreateGhostCheckpoints();
                GameManager.inst.UpdateTimeline();
                CheckpointEditor.inst.SetCurrentCheckpoint(0);
                if (!April)
                    Instance.TogglePlayingSong();
                else
                    Instance.DisplayNotification("Welcome to the 3.0.0 update!\njk, April Fools!", 6f, EditorManager.NotificationType.Error);
                Instance.ClearDialogs();

                AchievementManager.inst.UnlockAchievement("editor");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Instance.className}First opened error!{ex}");
            }

            ArcadeHelper.ResetModifiedStates();
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            Instance.ScreenScale = Screen.width / 1920f;
            Instance.ScreenScaleInverse = 1f / Instance.ScreenScale;

            if (GameManager.inst.gameState == GameManager.State.Playing)
            {
                if (Instance.canEdit)
                {
                    if (InputDataManager.inst.editorActions.ToggleEditor.WasPressed && !CoreHelper.IsUsingInputField || Input.GetKeyDown(KeyCode.Escape) && !Instance.isEditing)
                        Instance.ToggleEditor();

                    if (Instance.isEditing && !CoreHelper.IsUsingInputField)
                        Instance.handleViewShortcuts();

                    if (Instance.OpenedEditor)
                    {
                        GameManager.inst.ResetCheckpoints(true);
                        GameManager.inst.playerGUI.SetActive(false);
                        LSHelpers.ShowCursor();
                        Instance.GUI.SetActive(true);
                        Instance.ShowGUI();
                        Instance.SetPlayersInvinsible(true);
                        Instance.SetEditRenderArea();
                        GameManager.inst.UpdateTimeline();

                        if (EditorConfig.Instance.ResetHealthInEditor.Value && InputDataManager.inst.players.Count > 0)
                        {
                            try
                            {
                                if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x is CustomPlayer))
                                    foreach (var player in PlayerManager.Players)
                                    {
                                        if (player.PlayerModel != null && player.PlayerModel.basePart != null)
                                            player.Health = player.PlayerModel.basePart.health;
                                    }
                            }
                            catch (Exception ex)
                            {
                                CoreHelper.LogError($"Resetting player health error.\n{ex}");
                            }
                        }
                    }
                    else if (Instance.ClosedEditor)
                    {
                        GameManager.inst.playerGUI.SetActive(true);
                        LSHelpers.HideCursor();
                        Instance.GUI.SetActive(false);
                        AudioManager.inst.CurrentAudioSource.Play();
                        Instance.SetNormalRenderArea();
                        GameManager.inst.UpdateTimeline();

                        EventSystem.current.SetSelectedGameObject(null);
                    }

                    Instance.updatePointer();
                    Instance.UpdateTooltip();
                    Instance.UpdateEditButtons();

                    if (RTEditor.inst.timelineTime)
                        RTEditor.inst.timelineTime.text = string.Format("{0:0}:{1:00}.{2:000}",
                            Mathf.Floor(Instance.CurrentAudioPos / 60f),
                            Mathf.Floor(Instance.CurrentAudioPos % 60f),
                            Mathf.Floor(AudioManager.inst.CurrentAudioSource.time * 1000f % 1000f));

                    Instance.wasEditing = Instance.isEditing;
                }
                else if (!Instance.canEdit && Instance.isEditing)
                {
                    Instance.GUI.SetActive(false);
                    AudioManager.inst.SetPitch(1f);
                    Instance.SetNormalRenderArea();
                    Instance.isEditing = false;
                }
            }

            if (Instance.GUI.activeSelf == true && Instance.isEditing == true)
            {
                if (RTEditor.inst.timeField && !RTEditor.inst.timeField.isFocused)
                    RTEditor.inst.timeField.text = AudioManager.inst.CurrentAudioSource.time.ToString();

                if (RTEditor.inst.pitchField && !RTEditor.inst.pitchField.isFocused)
                    RTEditor.inst.pitchField.text = RTEventManager.inst ?
                        RTEventManager.inst.pitchOffset.ToString() : AudioManager.inst.pitch.ToString();

            }

            Instance.prevAudioTime = AudioManager.inst.CurrentAudioSource.time;
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.TogglePlayingSong))]
        [HarmonyPrefix]
        static bool TogglePlayingSongPrefix()
        {
            if (April || Instance.hasLoadedLevel)
            {
                if (AudioManager.inst.CurrentAudioSource.isPlaying)
                    AudioManager.inst.CurrentAudioSource.Pause();
                else
                    AudioManager.inst.CurrentAudioSource.Play();
                Instance.UpdatePlayButton();
            }
            else
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                Instance.UpdatePlayButton();
            }
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenGuides))]
        [HarmonyPrefix]
        static bool OpenGuidesPrefix()
        {
            Application.OpenURL("https://steamcommunity.com/app/440310/guides");
            Instance.DisplayNotification("Guides Link will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenSteamWorkshop))]
        [HarmonyPrefix]
        static bool OpenSteamWorkshopPrefix()
        {
            Application.OpenURL("https://steamcommunity.com/workshop/browse/?appid=440310&requiredtags[]=level");
            Instance.DisplayNotification("Steam will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenDiscord))]
        [HarmonyPrefix]
        static bool OpenDiscordPrefix()
        {
            Application.OpenURL(CoreHelper.MOD_DISCORD_LINK);
            Instance.DisplayNotification("Modders' Discord will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenTutorials))]
        [HarmonyPrefix]
        static bool OpenTutorialsPrefix()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlX89xw2z6hUFF3meXFXz9DL");
            Instance.DisplayNotification("PA Mod Showcases playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenVerifiedSongs))]
        [HarmonyPrefix]
        static bool OpenVerifiedSongsPrefix()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLMHuUok_ojlX89xw2z6hUFF3meXFXz9DL");
            Instance.DisplayNotification("PA Mod Showcases playlist will open in your browser!", 2f, EditorManager.NotificationType.Success);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SnapToBPM))]
        [HarmonyPrefix]
        static bool SnapToBPMPrefix(ref float __result, float __0)
        {
            __result = RTEditor.SnapToBPM(__0);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetLayer))]
        [HarmonyPrefix]
        static bool SetLayerPrefix(int __0)
        {
            RTEditor.inst.SetLayer(__0);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.GetLevelList))]
        [HarmonyPrefix]
        static bool GetLevelListPrefix()
        {
            CoreHelper.StartCoroutine(RTEditor.inst.LoadLevels());
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.LoadBaseLevel))]
        [HarmonyPrefix]
        static bool LoadBaseLevelPrefix()
        {
            GameManager.inst.ResetCheckpoints(true);
            if (!April)
            {
                AssignGameData();
                return false;
            }

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadJSONFile("https://drive.google.com/uc?export=download&id=1QJUeviLerCX1tZXW7QxpBC6K1BjtG1KT", json =>
            {
                GameData.Current = GameData.Parse(JSON.Parse(json));

                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip("https://drive.google.com/uc?export=download&id=1BDrRqX1IDk7bKo2hhYDqDqWLncMy7FkP", AudioType.OGGVORBIS, audioClip =>
                {
                    AudioManager.inst.PlayMusic(null, audioClip, true, 0f);
                    GameManager.inst.gameState = GameManager.State.Playing;

                    CoreHelper.StartCoroutine(Updater.IUpdateObjects(true));
                }, onError => { AssignGameData(); }));
            }, onError => { AssignGameData(); }));

            return false;
        }

        static void AssignGameData()
        {
            GameData.Current = RTEditor.inst.CreateBaseBeatmap();
            AudioManager.inst.PlayMusic(null, Instance.baseSong, true, 0f);
            GameManager.inst.gameState = GameManager.State.Playing;
        }

        [HarmonyPatch(nameof(EditorManager.SetFileInfoPopupText))]
        [HarmonyPrefix]
        static bool SetFileInfoPopupTextPrefix(string __0)
        {
            RTEditor.inst.fileInfoText?.SetText(__0);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.LoadLevel))]
        [HarmonyPrefix]
        static bool LoadLevelPrefix(ref IEnumerator __result, string __0)
        {
            __result = RTEditor.inst.LoadLevel($"{RTFile.ApplicationDirectory}{RTEditor.editorListSlash}{__0}");
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.DisplayNotification))]
        [HarmonyPrefix]
        static bool DisplayNotificationPrefix(string __0, float __1, EditorManager.NotificationType __2 = EditorManager.NotificationType.Info, bool __3 = false)
        {
            RTEditor.inst.DisplayNotification(__0, __0, __1, __2);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Copy))]
        [HarmonyPrefix]
        static bool CopyPrefix(bool _cut = false, bool _dup = false)
        {
            RTEditor.inst.Copy(_cut, _dup);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Paste))]
        [HarmonyPrefix]
        static bool PastePrefix(float __0)
        {
            RTEditor.inst.Paste(__0);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Delete))]
        [HarmonyPrefix]
        static bool DeletePrefix()
        {
            RTEditor.inst.Delete();

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.handleViewShortcuts))]
        [HarmonyPrefix]
        static bool handleViewShortcutsPrefix()
        {
            var config = EditorConfig.Instance;
            float multiply = Input.GetKey(KeyCode.LeftControl) ? 2f : Input.GetKey(KeyCode.LeftShift) ? 0.1f : 1f;

            if (Instance.IsCurrentDialog(EditorManager.EditorDialog.DialogType.Object)
                && Instance.IsOverObjTimeline
                && !CoreHelper.IsUsingInputField
                && !RTEditor.inst.isOverMainTimeline)
            {

                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat + config.KeyframeZoomAmount.Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    ObjEditor.inst.Zoom = ObjEditor.inst.zoomFloat - config.KeyframeZoomAmount.Value * multiply;
            }

            if (!Instance.IsOverObjTimeline && RTEditor.inst.isOverMainTimeline)
            {
                if (InputDataManager.inst.editorActions.ZoomIn.WasPressed)
                    Instance.Zoom = Instance.zoomFloat + config.MainZoomAmount.Value * multiply;
                if (InputDataManager.inst.editorActions.ZoomOut.WasPressed)
                    Instance.Zoom = Instance.zoomFloat - config.MainZoomAmount.Value * multiply;
            }

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.updatePointer))]
        [HarmonyPrefix]
        static bool updatePointerPrefix()
        {
            if (EditorConfig.Instance.DraggingMainCursorFix.Value && RTEditor.inst && RTEditor.inst.timelineSlider)
            {
                var slider = RTEditor.inst.timelineSlider;
                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom;
                Instance.audioTimeForSlider = slider.value;
                Instance.timelineSlider.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom, 25f);
                return false;
            }

            var point = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            var rect = new Rect(0f, 0.305f * (float)Screen.height, (float)Screen.width, (float)Screen.height * 0.025f);
            if (Instance.updateAudioTime && Input.GetMouseButtonUp(0) && rect.Contains(point))
            {
                AudioManager.inst.SetMusicTime(Instance.audioTimeForSlider / Instance.Zoom);
                Instance.updateAudioTime = false;
            }
            if (Input.GetMouseButton(0) && rect.Contains(point) && RTEditor.inst && RTEditor.inst.timelineSlider)
            {
                var slider = RTEditor.inst.timelineSlider;
                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom;
                Instance.audioTimeForSlider = slider.value;
                Instance.updateAudioTime = true;
                Instance.wasDraggingPointer = true;
                if (Mathf.Abs(Instance.audioTimeForSlider / Instance.Zoom - Instance.prevAudioTime) < 2f)
                {
                    if (EditorConfig.Instance.DraggingMainCursorPausesLevel.Value)
                    {
                        AudioManager.inst.CurrentAudioSource.Pause();
                        Instance.UpdatePlayButton();
                    }
                    AudioManager.inst.SetMusicTime(Instance.audioTimeForSlider / Instance.Zoom);
                }
            }
            else if (Instance.updateAudioTime && Instance.wasDraggingPointer && !rect.Contains(point))
            {
                AudioManager.inst.SetMusicTime(Instance.audioTimeForSlider / Instance.Zoom);
                Instance.updateAudioTime = false;
                Instance.wasDraggingPointer = false;
            }
            else if (RTEditor.inst && RTEditor.inst.timelineSlider)
            {
                var slider = RTEditor.inst.timelineSlider;

                slider.minValue = 0f;
                slider.maxValue = AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom;
                slider.value = AudioManager.inst.CurrentAudioSource.time * Instance.Zoom;
                Instance.audioTimeForSlider = AudioManager.inst.CurrentAudioSource.time * Instance.Zoom;
            }
            Instance.timelineSlider.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * Instance.Zoom, 25f);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.AddToPitch))]
        [HarmonyPrefix]
        static bool AddToPitchPrefix(float __0)
        {
            AudioManager.inst.SetPitch(AudioManager.inst.pitch + __0 * (Input.GetKey(KeyCode.LeftControl) ? 10f : Input.GetKey(KeyCode.LeftAlt) ? 0.1f : 1f));

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.ToggleEditor))]
        [HarmonyPostfix]
        static void ToggleEditorPostfix()
        {
            if (Instance.isEditing)
                Instance.UpdatePlayButton();
            GameManager.inst.ResetCheckpoints();

            ExampleManager.onEditorToggle?.Invoke(Instance.isEditing);
            if (ExampleManager.inst)
                ExampleManager.inst.UpdateActive();
        }

        [HarmonyPatch(nameof(EditorManager.CloseOpenBeatmapPopup))]
        [HarmonyPrefix]
        static bool CloseOpenBeatmapPopupPrefix()
        {
            Instance.HideDialog("Open File Popup");
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SaveBeatmap))]
        [HarmonyPrefix]
        static bool SaveBeatmapPrefix()
        {
            if (!Instance.hasLoadedLevel)
            {
                Instance.DisplayNotification("Beatmap can't be saved until you load a level.", 5f, EditorManager.NotificationType.Error);
                return false;
            }
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Attempting to save beatmap already, please wait!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            _ = RTFile.CopyFile(RTFile.CombinePaths(RTFile.BasePath, Level.LEVEL_LSB), RTFile.CombinePaths(RTFile.BasePath, $"level-previous{FileFormat.LSB.Dot()}"));

            DataManager.inst.SaveMetadata(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB));
            CoreHelper.StartCoroutine(SaveData(GameManager.inst.path));
            PlayerManager.SaveLocalModels();

            RTEditor.inst.SaveSettings();

            return false;
        }

        public static IEnumerator SaveData(string _path)
        {
            if (Instance != null)
            {
                Instance.DisplayNotification("Saving Beatmap!", 1f, EditorManager.NotificationType.Warning);
                Instance.savingBeatmap = true;
            }

            var gameData = GameData.Current;
            if (gameData.beatmapData is LevelBeatmapData levelBeatmapData && levelBeatmapData.levelData is LevelData levelData)
                levelData.modVersion = LegacyPlugin.ModVersion.ToString();

            if (EditorConfig.Instance.SaveAsync.Value)
                yield return CoreHelper.StartCoroutineAsync(gameData.ISaveData(_path));
            else
                yield return CoreHelper.StartCoroutine(gameData.ISaveData(_path));

            yield return new WaitForSeconds(0.5f);
            if (Instance != null)
            {
                Instance.DisplayNotification("Saved Beatmap!", 2f, EditorManager.NotificationType.Success);
                Instance.savingBeatmap = false;
            }
            yield break;
        }

        [HarmonyPatch(nameof(EditorManager.OpenSaveAs))]
        [HarmonyPrefix]
        static bool OpenSaveAsPrefix()
        {
            if (Instance.hasLoadedLevel)
            {
                Instance.ClearDialogs(EditorManager.EditorDialog.DialogType.Popup);
                Instance.ShowDialog("Save As Popup");
                return false;
            }
            Instance.DisplayNotification("Beatmap can't be saved as until you load a level.", 5f, EditorManager.NotificationType.Error);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SaveBeatmapAs), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SaveBeatmapAsPrefix(string __0)
        {
            if (Instance.hasLoadedLevel)
            {
                string str = RTFile.ApplicationDirectory + RTEditor.editorListSlash + __0;
                if (!RTFile.DirectoryExists(str))
                    Directory.CreateDirectory(str);

                var files = Directory.GetFiles(RTFile.ApplicationDirectory + RTEditor.editorListSlash + GameManager.inst.levelName);

                foreach (var file in files)
                {
                    if (!RTFile.DirectoryExists(Path.GetDirectoryName(file)))
                        Directory.CreateDirectory(Path.GetDirectoryName(file));

                    string saveTo = file.Replace("\\", "/").Replace(RTFile.ApplicationDirectory + RTEditor.editorListSlash + GameManager.inst.levelName, str);
                    File.Copy(file, saveTo, RTFile.FileExists(saveTo));
                }

                GameData.Current.SaveData(str + "/level.lsb", () =>
                {
                    Instance.DisplayNotification($"Saved beatmap to {__0}", 3f, EditorManager.NotificationType.Success);
                });
                return false;
            }
            Instance.DisplayNotification("Beatmap can't be saved as until you load a level.", 3f, EditorManager.NotificationType.Error);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenBeatmapPopup))]
        [HarmonyPrefix]
        static bool OpenBeatmapPopupPrefix()
        {
            CoreHelper.Log("Open Beatmap Popup");
            var component = Instance.GetDialog("Open File Popup").Dialog.Find("search-box/search").GetComponent<InputField>();
            if (Instance.openFileSearch == null)
                Instance.openFileSearch = "";

            RTEditor.inst.choosingLevelTemplate = false;
            component.text = Instance.openFileSearch;
            Instance.ClearDialogs(EditorManager.EditorDialog.DialogType.Popup);
            Instance.RenderOpenBeatmapPopup();
            Instance.ShowDialog("Open File Popup");

            var config = EditorConfig.Instance;

            try
            {
                //Create Local Variables
                var openLevel = Instance.GetDialog("Open File Popup").Dialog.gameObject;
                var openTLevel = openLevel.transform;
                var openRTLevel = openLevel.GetComponent<RectTransform>();
                var openGridLVL = openTLevel.Find("mask/content").GetComponent<GridLayoutGroup>();

                //Set Open File Popup RectTransform
                openRTLevel.anchoredPosition = config.OpenLevelPosition.Value;
                openRTLevel.sizeDelta = config.OpenLevelScale.Value;

                //Set Open FIle Popup content GridLayoutGroup
                openGridLVL.cellSize = config.OpenLevelCellSize.Value;
                openGridLVL.constraint = config.OpenLevelCellConstraintType.Value;
                openGridLVL.constraintCount = config.OpenLevelCellConstraintCount.Value;
                openGridLVL.spacing = config.OpenLevelCellSpacing.Value;
            }
            catch (Exception ex)
            {
                Debug.Log($"OpenBeatmapPopup {ex}");
            }

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.AssignWaveformTextures))]
        [HarmonyPrefix]
        static bool AssignWaveformTexturesPrefix() => false;

        [HarmonyPatch(nameof(EditorManager.RenderOpenBeatmapPopup))]
        [HarmonyPrefix]
        static bool RenderOpenBeatmapPopupPrefix()
        {
            RTEditor.inst.StartCoroutine(RTEditor.inst.RefreshLevelList());
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.RenderParentSearchList))]
        [HarmonyPrefix]
        static bool RenderParentSearchListPrefix()
        {
            RTEditor.inst.RefreshParentSearch(Instance, ObjectEditor.inst.CurrentSelection);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenAlbumArtSelector))]
        [HarmonyPrefix]
        static bool OpenAlbumArtSelectorPrefix()
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            Debug.Log("Selected file: " + jpgFile);
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = RTFile.ApplicationDirectory + RTEditor.editorListSlash + Instance.currentLoadedLevel + "/level.jpg";
                CoreHelper.StartCoroutine(Instance.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
                {
                    File.Copy(jpgFile, jpgFileLocation, true);
                    Instance.GetDialog("Metadata Editor").Dialog.transform.Find("Scroll View/Viewport/Content/creator/cover_art/image").GetComponent<Image>().sprite = cover;
                    MetadataEditor.inst.currentLevelCover = cover;
                }, errorFile =>
                {
                    Instance.DisplayNotification("Please resize your image to be less then or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error);
                }));
            }
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.RenderTimeline))]
        [HarmonyPrefix]
        static bool RenderTimelinePrefix()
        {
            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                EventEditor.inst.RenderEventObjects();
            else
                ObjectEditor.inst.RenderTimelineObjectsPositions();

            CheckpointEditor.inst.RenderCheckpoints();
            RTMarkerEditor.inst.RenderMarkers();

            Instance.UpdateTimelineSizes();

            RTEditor.inst.SetTimelineGridSize();

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.UpdateTimelineSizes))]
        [HarmonyPrefix]
        static bool UpdateTimelineSizesPrefix()
        {
            if (AudioManager.inst.CurrentAudioSource.clip == null)
                return false;

            var zoom = Instance.Zoom;
            Instance.markerTimeline.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * zoom, Instance.markerTimeline.transform.AsRT().sizeDelta.y);
            Instance.timeline.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * zoom, Instance.timeline.transform.AsRT().sizeDelta.y);
            Instance.timelineWaveformOverlay.transform.AsRT().sizeDelta = new Vector2(AudioManager.inst.CurrentAudioSource.clip.length * zoom, Instance.timeline.transform.AsRT().sizeDelta.y);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.QuitToMenu))]
        [HarmonyPrefix]
        static bool QuitToMenuPrefix()
        {
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                return false;
            }

            RTEditor.inst.ShowWarningPopup("Are you sure you want to quit to the main menu? Any unsaved progress will be lost!", () =>
            {
                if (Instance.savingBeatmap)
                {
                    Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                DG.Tweening.DOTween.KillAll(false);
                DG.Tweening.DOTween.Clear(true);
                Instance.loadedLevels.Clear();
                GameData.Current = null;
                GameData.Current = new GameData();
                DiscordController.inst.OnIconChange("");
                DiscordController.inst.OnStateChange("");
                Debug.Log($"{Instance.className}Quit to Main Menu");
                InputDataManager.inst.players.Clear();
                SceneHelper.LoadScene(SceneName.Main_Menu);
            }, RTEditor.inst.HideWarningPopup);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.QuitGame))]
        [HarmonyPrefix]
        static bool QuitGamePrefix()
        {
            if (Instance.savingBeatmap)
            {
                Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error, false);
                return false;
            }

            RTEditor.inst.ShowWarningPopup("Are you sure you want to quit the game? Any unsaved progress will be lost!", () =>
            {
                if (Instance.savingBeatmap)
                {
                    Instance.DisplayNotification("Please wait until the beatmap finishes saving!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                DiscordController.inst.OnIconChange("");
                DiscordController.inst.OnStateChange("");
                Debug.Log($"{Instance.className}Quit Game");
                Application.Quit();
            }, RTEditor.inst.HideWarningPopup);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.CreateNewLevel))]
        [HarmonyPrefix]
        static bool CreateNewLevelPrefix()
        {
            RTEditor.inst.CreateNewLevel();
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenLevelFolder))]
        [HarmonyPrefix]
        static bool OpenLevelFolder()
        {
            if (RTFile.DirectoryExists(RTFile.BasePath))
            {
                RTFile.OpenInFileBrowser.Open(RTFile.BasePath);
                return false;
            }
            RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.editorListPath));
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetEditRenderArea))]
        [HarmonyPrefix]
        static bool SetEditRenderAreaPrefix()
        {
            if (Instance.hasLoadedLevel && RTEventManager.windowPositionResolutionChanged)
                WindowController.ResetResolution();

            if (InterfaceManager.inst && InterfaceManager.inst.CurrentInterface)
                InterfaceManager.inst.CloseMenus();

            EventManager.inst.cam.rect = new Rect(0f, 0.3708f, 0.601f, 0.601f);
            EventManager.inst.camPer.rect = new Rect(0f, 0.3708f, 0.602f, 0.601f);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetNormalRenderArea))]
        [HarmonyPrefix]
        static bool SetNormalRenderAreaPrefix()
        {
            EventManager.inst.cam.rect = new Rect(0f, 0f, 1f, 1f);
            EventManager.inst.camPer.rect = new Rect(0f, 0f, 1f, 1f);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetDialogStatus))]
        [HarmonyPrefix]
        static bool SetDialogStatusPrefix(string __0, bool __1, bool __2 = true)
        {
            RTEditor.inst.SetDialogStatus(__0, __1, __2);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.ClearDialogs))]
        [HarmonyPrefix]
        static bool ClearDialogsPrefix(params EditorManager.EditorDialog.DialogType[] __0)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            var editorDialogs = Instance.EditorDialogs;
            for (int i = 0; i < editorDialogs.Count; i++)
            {
                var editorDialog = editorDialogs[i];
                if (__0.Length == 0)
                {
                    if (editorDialog.Type != EditorManager.EditorDialog.DialogType.Timeline)
                    {
                        if (play)
                            RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
                        else
                            editorDialog.Dialog.gameObject.SetActive(false);

                        Instance.ActiveDialogs.Remove(editorDialog);
                    }
                }
                else if (__0.Contains(editorDialog.Type))
                {
                    if (play)
                        RTEditor.inst.PlayDialogAnimation(editorDialog.Dialog.gameObject, editorDialog.Name, false);
                    else
                        editorDialog.Dialog.gameObject.SetActive(false);

                    Instance.ActiveDialogs.Remove(editorDialog);
                }
            }

            Instance.currentDialog = Instance.ActiveDialogs.Last();

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.ToggleDropdown))]
        [HarmonyPrefix]
        static bool ToggleDropdownPrefix(GameObject __0)
        {
            bool flag = !__0.activeSelf;
            foreach (var gameObject in Instance.DropdownMenus)
            {
                RTEditor.inst.PlayDialogAnimation(gameObject, gameObject.name, false);
            }

            RTEditor.inst.PlayDialogAnimation(__0, __0.name, flag);

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.HideAllDropdowns))]
        [HarmonyPrefix]
        static bool HideAllDropdownsPrefix()
        {
            foreach (var gameObject in Instance.DropdownMenus)
            {
                RTEditor.inst.PlayDialogAnimation(gameObject, gameObject.name, false);
            }
            EventSystem.current.SetSelectedGameObject(null);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.GetTimelineTime))]
        [HarmonyPrefix]
        static bool GetTimelineTimePrefix(ref float __result, float __0)
        {
            __result = RTEditor.inst.GetTimelineTime(__0);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetTooltip))]
        [HarmonyPrefix]
        static bool SetTooltipPrefix(List<string> __0, string __1, string __2)
        {
            if (Instance.toolTipRoutine != null)
                Instance.StopCoroutine(Instance.toolTipRoutine);

            Instance.tooltipString = Instance.TooltipConverter(__0, __1, __2);
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.TooltipConverter))]
        [HarmonyPrefix]
        static bool TooltipConverterPrefix(ref string __result, List<string> __0, string __1, string __2)
        {
            string text = "";

            if (__0 != null && __0.Count > 0)
                text += $"<size=16>[{string.Join(", ", __0.ToArray())}]</size><br>";
            if (!string.IsNullOrEmpty(__1))
                text += $"<b><size=18>{__1}</size></b>";
            if (!string.IsNullOrEmpty(__2))
                text += $"<br><size=18>{__2}</size>";

            __result = text;
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.SetShowHelp))]
        [HarmonyPrefix]
        static bool SetShowHelpPrefix(bool __0)
        {
            Instance.showHelp = __0;
            Instance.tooltip.transform.parent.gameObject.SetActive(__0);

            if (__0)
                RTEditor.inst.RebuildNotificationLayout();
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.UpdateTooltip))]
        [HarmonyPrefix]
        static bool UpdateTooltipPrefix()
        {
            if (!RTEditor.inst.tooltipText)
                EditorManager.inst.tooltip.GetComponent<TextMeshProUGUI>().text = Instance.tooltipString;

            RTEditor.inst.tooltipText?.SetText(Instance.tooltipString);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Instance.tooltip.transform.AsRT());

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenMetadata))]
        [HarmonyPrefix]
        static bool OpenMetadataPrefix()
        {
            var inst = Instance;

            if (!inst.hasLoadedLevel)
            {
                inst.DisplayNotification("Load a level first before trying to upload!", 5f, EditorManager.NotificationType.Error);
                return false;
            }

            inst.ClearDialogs();
            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{RTFile.CombinePaths(RTFile.BasePath, Level.LEVEL_JPG)}", x =>
            {
                var cover = SpriteHelper.CreateSprite(x);
                inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/creator/cover_art/image").GetComponent<Image>().sprite = cover;
                MetadataEditor.inst.currentLevelCover = cover;
            }, onError =>
            {
                inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content/creator/cover_art/image").GetComponent<Image>().sprite = LegacyPlugin.AtanPlaceholder;
                MetadataEditor.inst.currentLevelCover = LegacyPlugin.AtanPlaceholder;
            }));

            MetadataEditor.inst.Render();
            MetadataEditor.inst.OpenDialog();
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.GetSprite))]
        [HarmonyPrefix]
        static bool GetSpritePrefix(ref IEnumerator __result, string __0, EditorManager.SpriteLimits __1, Action<Sprite> __2, Action<string> __3)
        {
            __result = GetSprite(__0, __1, __2, __3);
            return false;
        }

        public static IEnumerator GetSprite(string _path, EditorManager.SpriteLimits _limits, Action<Sprite> callback, Action<string> onError)
        {
            yield return CoreHelper.StartCoroutine(FileManager.inst.LoadImageFileRaw(_path, _texture =>
            {
                if ((_texture.texture.width > _limits.size.x && _limits.size.x > 0f) || (_texture.texture.height > _limits.size.y && _limits.size.y > 0f))
                {
                    onError?.Invoke(_path);
                    return;
                }
                callback?.Invoke(_texture);
            }, error => { onError?.Invoke(_path); }));
            yield break;
        }

        [HarmonyPatch(nameof(EditorManager.SetMainTimelineZoom))]
        [HarmonyPrefix]
        static bool SetMainTimelineZoomPrefix(float __0, bool __1 = true)
        {
            if (__1)
                Instance.RenderTimeline();

            Instance.timelineScrollRectBar.value = (EditorConfig.Instance.UseMouseAsZoomPoint.Value ? RTEditor.inst.GetTimelineTime() : AudioManager.inst.CurrentAudioSource.time) / AudioManager.inst.CurrentAudioSource.clip.length;
            Debug.LogFormat("{0}Set Timeline Zoom -> [{1}]", new object[] { Instance.className, Instance.Zoom });
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.LoadingIconUpdate))]
        [HarmonyPrefix]
        static bool LoadingIconUpdatePrefix()
        {
            var inst = Instance;
            if (inst.currentLoadingSprite >= inst.loadingSprites.Length)
                inst.currentLoadingSprite = 0;

            var sprite = inst.loadingSprites[inst.currentLoadingSprite];
            if (RTEditor.inst.doggoImage)
                RTEditor.inst.doggoImage.sprite = sprite;
            inst.loadingImage.sprite = sprite;
            inst.currentLoadingSprite++;

            return false;
        }

        [HarmonyPatch(nameof(EditorManager.OpenedLevel), MethodType.Getter)]
        [HarmonyPrefix]
        static bool OpenedLevelPrefix(EditorManager __instance, ref bool __result)
        {
            __result = __instance.wasOpenLevel && __instance.hasLoadedLevel;
            return false;
        }

        [HarmonyPatch(nameof(EditorManager.Zoom), MethodType.Setter)]
        [HarmonyPrefix]
        static bool ZoomSetterPrefix(ref float value)
        {
            RTEditor.inst.SetTimeline(value);
            return false;
        }
    }
}
