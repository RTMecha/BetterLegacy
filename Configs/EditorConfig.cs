using BepInEx.Configuration;
using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Components.Player;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Editor Config for PA Legacy. Based on the EditorManagement mod.
    /// </summary>
    public class EditorConfig : BaseConfig
    {
        public static EditorConfig Instance { get; private set; }

        public override ConfigFile Config { get; set; }

        public static AcceptableValueRange<int> FontSizeLimit { get; } = new AcceptableValueRange<int>(1, 40);
        public static AcceptableValueRange<float> HoverScaleLimit { get; } = new AcceptableValueRange<float>(0.7f, 1.4f);

        public EditorConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            #region General

            Debug = Config.Bind("Editor - General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
            EditorZenMode = Config.Bind("Editor - General", "Editor Zen Mode", false, "If on, the player will not take damage in Preview Mode.");
            ResetHealthInEditor = Config.Bind("Editor - General", "Reset Health In Editor View", true, "If on, the player's health will reset when the creator exits Preview Mode.");
            BPMSnapsKeyframes = Config.Bind("Editor - General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.");
            BPMSnapDivisions = Config.Bind("Editor - General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.");
            DraggingPlaysSound = Config.Bind("Editor - General", "Dragging Plays Sound", true, "If dragging an object plays a sound.");
            DraggingPlaysSoundOnlyWithBPM = Config.Bind("Editor - General", "Dragging Plays Sound Only With BPM", true, "If dragging an object plays a sound ONLY when BPM Snap is active.");
            ShowCollapsePrefabWarning = Config.Bind("Editor - General", "Show Collapse Prefab Warning", true, "If a warning should popup when the user is trying to apply a prefab. Can be good for accidental Apply Prefab button clicks.");
            RoundToNearest = Config.Bind("Editor - General", "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).");
            ScrollOnEasing = Config.Bind("Editor - General", "Scroll on Easing Changes Value", true, "If Scolling on an easing dropdown changes the easing.");
            PrefabExampleTemplate = Config.Bind("Editor - General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.");
            PasteOffset = Config.Bind("Editor - General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.");
            BringToSelection = Config.Bind("Editor - General", "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.");
            SelectPasted = Config.Bind("Editor - General", "Select Pasted Keyframes", false, "Select a pasted keyframe.");
            CreateObjectsatCameraCenter = Config.Bind("Editor - General", "Create Objects at Camera Center", true, "When an object is created, its position will be set to that of the camera's.");
            CreateObjectsScaleParentDefault = Config.Bind("Editor - General", "Create Objects Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.");
            AllowEditorKeybindsWithEditorCam = Config.Bind("Editor - General", "Allow Editor Keybinds With Editor Cam", true, "Allows keybinds to be used if EventsCore editor camera is on.");
            RotationEventKeyframeResets = Config.Bind("Editor - General", "Rotation Event Keyframe Resets", true, "When an Event / Check rotation keyframe is created, it resets the value to 0.");
            RememberLastKeyframeType = Config.Bind("Editor - General", "Remember Last Keyframe Type", false, "When an object is selected for the first time, it selects the previous objects' keyframe selection type. For example, say you had a color keyframe selected, this newly selected object will select the first color keyframe.");

            #endregion

            #region Timeline

            DraggingMainCursorPausesLevel = Config.Bind("Editor - Timeline", "Dragging Main Cursor Pauses Level", true, "If dragging the cursor pauses the level.");
            DraggingMainCursorFix = Config.Bind("Editor - Timeline", "Dragging Main Cursor Fix", true, "If the main cursor should act like the object cursor.");
            TimelineCursorColor = Config.Bind("Editor - Timeline", "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.");
            KeyframeCursorColor = Config.Bind("Editor - Timeline", "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.");
            ObjectSelectionColor = Config.Bind("Editor - Timeline", "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.");
            MainZoomBounds = Config.Bind("Editor - Timeline", "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.");
            KeyframeZoomBounds = Config.Bind("Editor - Timeline", "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.");
            MainZoomAmount = Config.Bind("Editor - Timeline", "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.");
            KeyframeZoomAmount = Config.Bind("Editor - Timeline", "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.");
            KeyframeEndLengthOffset = Config.Bind("Editor - Timeline", "Keyframe End Length Offset", 2f, "Sets the amount of space you have after the last keyframe in an object.");
            TimelineObjectPrefabTypeIcon = Config.Bind("Editor - Timeline", "Timeline Object Prefab Type Icon", true, "Shows the object's prefab type's icon.");
            EventLabelsRenderLeft = Config.Bind("Editor - Timeline", "Event Labels Render Left", false, "If the Event Layer labels should render on the left side or not.");
            EventKeyframesRenderBinColor = Config.Bind("Editor - Timeline", "Event Keyframes Use Bin Color", true, "If the Event Keyframes should use the bin color when not selected or not.");
            ObjectKeyframesRenderBinColor = Config.Bind("Editor - Timeline", "Object Keyframes Use Bin Color", true, "If the Object Keyframes should use the bin color when not selected or not.");
            WaveformGenerate = Config.Bind("Editor - Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)");
            WaveformSaves = Config.Bind("Editor - Timeline", "Waveform Saves", true, "Turn off if you don't want the timeline waveform to save.");
            WaveformRerender = Config.Bind("Editor - Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.");
            WaveformMode = Config.Bind("Editor - Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
            WaveformBGColor = Config.Bind("Editor - Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.");
            WaveformTopColor = Config.Bind("Editor - Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.");
            WaveformBottomColor = Config.Bind("Editor - Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.");
            WaveformTextureFormat = Config.Bind("Editor - Timeline", "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.");
            TimelineGridEnabled = Config.Bind("Editor - Timeline", "Timeline Grid Enabled", true, "If the timeline grid renders.");
            TimelineGridColor = Config.Bind("Editor - Timeline", "Timeline Grid Color", new Color(0.2157f, 0.2157f, 0.2196f, 1f), "The color of the timeline grid.");
            TimelineGridThickness = Config.Bind("Editor - Timeline", "Timeline Grid Thickness", 2f, "The size of each line of the timeline grid.");
            MarkerLineColor = Config.Bind("Editor - Timeline", "Marker Line Color", new Color(1f, 1f, 1f, 0.7843f), "The color of the marker lines.");
            MarkerLineWidth = Config.Bind("Editor - Timeline", "Marker Line Width", 2f, "The width of the marker lines.");
            MarkerTextWidth = Config.Bind("Editor - Timeline", "Marker Text Width", 64f, "The width of the markers' text. If the text is longer than this width, then it doesn't display the symbols after the width.");
            MarkerLoopActive = Config.Bind("Editor - Timeline", "Marker Loop Active", false, "If the marker should loop between markers.");
            MarkerLoopBegin = Config.Bind("Editor - Timeline", "Marker Loop Begin", 0, "Audio time gets set to this marker.");
            MarkerLoopEnd = Config.Bind("Editor - Timeline", "Marker Loop End", 1, "If the audio time gets to the set marker time, it will loop to the beginning marker.");
            MarkerDefaultColor = Config.Bind("Editor - Timeline", "Marker Default Color", 0, "The default color assigned to a new marker.");

            #endregion

            #region Data

            AutosaveLimit = Config.Bind("Editor - Data", "Autosave Limit", 7, "If autosave count reaches this number, delete the first autosave.");
            AutosaveLoopTime = Config.Bind("Editor - Data", "Autosave Loop Time", 600f, "The repeat time of autosave.");
            LevelLoadsLastTime = Config.Bind("Editor - Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
            LevelPausesOnStart = Config.Bind("Editor - Data", "Level Pauses on Start", false, "Editor pauses on level load.");
            BackupPreviousLoadedLevel = Config.Bind("Editor - Data", "Backup Previous Loaded Level", false, "Saves the previously loaded level when loading a different level to a level-previous.lsb file.");
            SettingPathReloads = Config.Bind("Editor - Data", "Setting Path Reloads", true, "With this setting on, update the list for levels, prefabs and themes when changing the directory.");
            SavingSavesThemeOpacity = Config.Bind("Editor - Data", "Saving Saves Theme Opacity", false, "Turn this off if you don't want themes to break in unmodded PA.");
            UpdatePrefabListOnFilesChanged = Config.Bind("Editor - Data", "Update Prefab List on Files Changed", false, "When you add a prefab to your prefab path, the editor will automatically update the prefab list for you.");
            UpdateThemeListOnFilesChanged = Config.Bind("Editor - Data", "Update Theme List on Files Changed", false, "When you add a theme to your theme path, the editor will automatically update the theme list for you.");
            ShowLevelsWithoutCoverNotification = Config.Bind("Editor - Data", "Show Levels Without Cover Notification", false, "Sends an error notification for what levels don't have covers.");
            ZIPLevelExportPath = Config.Bind("Editor - Data", "ZIP Level Export Path", "", "The custom path to export a zipped level to. If no path is set then it will export to beatmaps/exports.");
            ConvertLevelLSToVGExportPath = Config.Bind("Editor - Data", "Convert Level LS to VG Export Path", "", "The custom path to export a level to. If no path is set then it will export to beatmaps/exports.");
            ConvertPrefabLSToVGExportPath = Config.Bind("Editor - Data", "Convert Prefab LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ConvertThemeLSToVGExportPath = Config.Bind("Editor - Data", "Convert Theme LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ThemeSavesIndents = Config.Bind("Editor - Data", "Theme Saves Indents", false, "If .lst files should save with multiple lines and indents.");

            #endregion

            #region Editor GUI

            DragUI = Config.Bind("Editor - Editor GUI", "Drag UI", true, "Specific UI popups can be dragged around (such as the parent selector, etc).");
            EditorTheme = Config.Bind("Editor - Editor GUI", "Editor Theme", BetterLegacy.EditorTheme.Legacy, "The current theme the editor uses.");
            EditorFont = Config.Bind("Editor - Editor GUI", "Editor Font", BetterLegacy.EditorFont.Inconsolata_Variable, "The current font the editor uses.");
            RoundedUI = Config.Bind("Editor - Editor GUI", "Rounded UI", false, "If all elements that can be rounded should be so.");
            ShowModdedFeaturesInEditor = Config.Bind("Editor - Editor GUI", "Show Modded Features in Editor", true, "Z axis, 10-18 color slots, homing keyframes, etc get set active / inactive with this on / off respectively");
            HoverUIPlaySound = Config.Bind("Editor - Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.");
            ImportPrefabsDirectly = Config.Bind("Editor - Editor GUI", "Import Prefabs Directly", false, "When clicking on an External Prefab, instead of importing it directly it'll bring up a Prefab External View Dialog if this config is off.");
            ThemesPerPage = Config.Bind("Editor - Editor GUI", "Themes Per Page", 10, "How many themes are shown per page in the Beatmap Themes popup.");
            ThemesEventKeyframePerPage = Config.Bind("Editor - Editor GUI", "Themes (Event Keyframe) Per Page", 30, "How many themes are shown per page in the theme event keyframe.");
            MouseTooltipDisplay = Config.Bind("Editor - Editor GUI", "Mouse Tooltip Display", true, "If the mouse tooltip should display.");
            NotificationWidth = Config.Bind("Editor - Editor GUI", "Notification Width", 221f, "Width of the notifications.");
            NotificationSize = Config.Bind("Editor - Editor GUI", "Notification Size", 1f, "Total size of the notifications.");
            NotificationDirection = Config.Bind("Editor - Editor GUI", "Notification Direction", Direction.Down, "Direction the notifications popup from.");
            NotificationsDisplay = Config.Bind("Editor - Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.");
            AdjustPositionInputs = Config.Bind("Editor - Editor GUI", "Adjust Position Inputs", true, "If position keyframe input fields should be adjusted so they're in a proper row rather than having Z Axis below X Axis without a label. Drawback with doing this is it makes the fields smaller than normal.");
            ShowDropdownOnHover = Config.Bind("Editor - Editor GUI", "Show Dropdowns on Hover", false, "If your mouse enters a dropdown bar, it will automatically show the dropdown list.");
            HideVisualElementsWhenObjectIsEmpty = Config.Bind("Editor - Editor GUI", "Hide Visual Elements When Object Is Empty", true, "If the Beatmap Object is empty, anything related to the visuals of the object doesn't show.");
            OpenLevelPosition = Config.Bind("Editor - Editor GUI", "Open Level Position", Vector2.zero, "The position of the Open Level popup.");
            OpenLevelScale = Config.Bind("Editor - Editor GUI", "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.");
            OpenLevelEditorPathPos = Config.Bind("Editor - Editor GUI", "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.");
            OpenLevelEditorPathLength = Config.Bind("Editor - Editor GUI", "Open Level Editor Path Length", 104f, "The length of the editor path input field.");
            OpenLevelListRefreshPosition = Config.Bind("Editor - Editor GUI", "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.");
            OpenLevelTogglePosition = Config.Bind("Editor - Editor GUI", "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.");
            OpenLevelDropdownPosition = Config.Bind("Editor - Editor GUI", "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.");
            OpenLevelCellSize = Config.Bind("Editor - Editor GUI", "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.");
            OpenLevelCellConstraintType = Config.Bind("Editor - Editor GUI", "Open Level Cell Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.");
            OpenLevelCellConstraintCount = Config.Bind("Editor - Editor GUI", "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
            OpenLevelCellSpacing = Config.Bind("Editor - Editor GUI", "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.");
            OpenLevelTextHorizontalWrap = Config.Bind("Editor - Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
            OpenLevelTextVerticalWrap = Config.Bind("Editor - Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
            OpenLevelTextFontSize = Config.Bind("Editor - Editor GUI", "Open Level Text Font Size", 20, new ConfigDescription("Font size of the folder button text.", FontSizeLimit));

            OpenLevelFolderNameMax = Config.Bind("Editor - Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.");
            OpenLevelSongNameMax = Config.Bind("Editor - Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.");
            OpenLevelArtistNameMax = Config.Bind("Editor - Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.");
            OpenLevelCreatorNameMax = Config.Bind("Editor - Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.");
            OpenLevelDescriptionMax = Config.Bind("Editor - Editor GUI", "Open Level Description Max", 16, "Limited length of the description.");
            OpenLevelDateMax = Config.Bind("Editor - Editor GUI", "Open Level Date Max", 16, "Limited length of the date.");
            OpenLevelTextFormatting = Config.Bind("Editor - Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}",
                    "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

            OpenLevelButtonHoverSize = Config.Bind("Editor - Editor GUI", "Open Level Button Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            OpenLevelCoverPosition = Config.Bind("Editor - Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
            OpenLevelCoverScale = Config.Bind("Editor - Editor GUI", "Open Level Cover Scale", new Vector2(26f, 26f), "Size of the level cover.");

            ChangesRefreshLevelList = Config.Bind("Editor - Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");
            OpenLevelShowDeleteButton = Config.Bind("Editor - Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

            TimelineObjectHoverSize = Config.Bind("Editor - Editor GUI", "Timeline Object Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            KeyframeHoverSize = Config.Bind("Editor - Editor GUI", "Keyframe Hover Size", 1f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            TimelineBarButtonsHoverSize = Config.Bind("Editor - Editor GUI", "Timeline Bar Buttons Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));
            PrefabButtonHoverSize = Config.Bind("Editor - Editor GUI", "Prefab Button Hover Size", 1.05f, new ConfigDescription("How big the button gets when hovered.", HoverScaleLimit));

            PrefabInternalPopupPos = Config.Bind("Editor - Editor GUI", "Prefab Internal Popup Pos", new Vector2(0f, -16f), "Position of the internal prefabs popup.");
            PrefabInternalPopupSize = Config.Bind("Editor - Editor GUI", "Prefab Internal Popup Size", new Vector2(400f, -32f), "Scale of the internal prefabs popup.");
            PrefabInternalHorizontalScroll = Config.Bind("Editor - Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabInternalCellSize = Config.Bind("Editor - Editor GUI", "Prefab Internal Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.");
            PrefabInternalConstraintMode = Config.Bind("Editor - Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabInternalConstraint = Config.Bind("Editor - Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabInternalSpacing = Config.Bind("Editor - Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabInternalStartAxis = Config.Bind("Editor - Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabInternalDeleteButtonPos = Config.Bind("Editor - Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
            PrefabInternalDeleteButtonSca = Config.Bind("Editor - Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabInternalNameHorizontalWrap = Config.Bind("Editor - Editor GUI", "Prefab Internal Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameVerticalWrap = Config.Bind("Editor - Editor GUI", "Prefab Internal Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameFontSize = Config.Bind("Editor - Editor GUI", "Prefab Internal Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));
            PrefabInternalTypeHorizontalWrap = Config.Bind("Editor - Editor GUI", "Prefab Internal Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeVerticalWrap = Config.Bind("Editor - Editor GUI", "Prefab Internal Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeFontSize = Config.Bind("Editor - Editor GUI", "Prefab Internal Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));

            PrefabExternalPopupPos = Config.Bind("Editor - Editor GUI", "Prefab External Popup Pos", new Vector2(0f, -16f), "Position of the external prefabs popup.");
            PrefabExternalPopupSize = Config.Bind("Editor - Editor GUI", "Prefab External Popup Size", new Vector2(400f, -32f), "Scale of the external prefabs popup.");
            PrefabExternalPrefabPathPos = Config.Bind("Editor - Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 16f), "Position of the prefab path input field.");
            PrefabExternalPrefabPathLength = Config.Bind("Editor - Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
            PrefabExternalPrefabRefreshPos = Config.Bind("Editor - Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(210f, 450f), "Position of the prefab refresh button.");
            PrefabExternalHorizontalScroll = Config.Bind("Editor - Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabExternalCellSize = Config.Bind("Editor - Editor GUI", "Prefab External Cell Size", new Vector2(383f, 32f), "Size of each Prefab Item.");
            PrefabExternalConstraintMode = Config.Bind("Editor - Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabExternalConstraint = Config.Bind("Editor - Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabExternalSpacing = Config.Bind("Editor - Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabExternalStartAxis = Config.Bind("Editor - Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabExternalDeleteButtonPos = Config.Bind("Editor - Editor GUI", "Prefab External Delete Button Pos", new Vector2(367f, -16f), "Position of the Delete Button.");
            PrefabExternalDeleteButtonSca = Config.Bind("Editor - Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabExternalNameHorizontalWrap = Config.Bind("Editor - Editor GUI", "Prefab External Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameVerticalWrap = Config.Bind("Editor - Editor GUI", "Prefab External Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameFontSize = Config.Bind("Editor - Editor GUI", "Prefab External Name Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));
            PrefabExternalTypeHorizontalWrap = Config.Bind("Editor - Editor GUI", "Prefab External Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeVerticalWrap = Config.Bind("Editor - Editor GUI", "Prefab External Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeFontSize = Config.Bind("Editor - Editor GUI", "Prefab External Type Font Size", 20, new ConfigDescription("Size of the text font.", FontSizeLimit));

            #endregion

            #region Fields

            ScrollwheelLargeAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelSmallAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelRegularAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2LargeAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Vector2 Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2SmallAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Vector2 Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2RegularAmountKey = Config.Bind("Editor - Fields", "Scrollwheel Vector2 Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ShowModifiedColors = Config.Bind("Editor - Fields", "Show Modified Colors", true, "Keyframe colors show any modifications done (such as hue, saturation and value).");
            ThemeTemplateName = Config.Bind("Editor - Fields", "Theme Template Name", "New Theme", "Name of the template theme.");
            ThemeTemplateGUI = Config.Bind("Editor - Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
            ThemeTemplateTail = Config.Bind("Editor - Fields", "Theme Template Tail", LSColors.white, "Tail Color of the template theme.");
            ThemeTemplateBG = Config.Bind("Editor - Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
            ThemeTemplatePlayer1 = Config.Bind("Editor - Fields", "Theme Template Player 1", LSColors.HexToColor("E57373"), "Player 1 Color of the template theme.");
            ThemeTemplatePlayer2 = Config.Bind("Editor - Fields", "Theme Template Player 2", LSColors.HexToColor("64B5F6"), "Player 2 Color of the template theme.");
            ThemeTemplatePlayer3 = Config.Bind("Editor - Fields", "Theme Template Player 3", LSColors.HexToColor("81C784"), "Player 3 Color of the template theme.");
            ThemeTemplatePlayer4 = Config.Bind("Editor - Fields", "Theme Template Player 4", LSColors.HexToColor("FFB74D"), "Player 4 Color of the template theme.");
            ThemeTemplateOBJ1 = Config.Bind("Editor - Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
            ThemeTemplateOBJ2 = Config.Bind("Editor - Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
            ThemeTemplateOBJ3 = Config.Bind("Editor - Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
            ThemeTemplateOBJ4 = Config.Bind("Editor - Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
            ThemeTemplateOBJ5 = Config.Bind("Editor - Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
            ThemeTemplateOBJ6 = Config.Bind("Editor - Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
            ThemeTemplateOBJ7 = Config.Bind("Editor - Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
            ThemeTemplateOBJ8 = Config.Bind("Editor - Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
            ThemeTemplateOBJ9 = Config.Bind("Editor - Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
            ThemeTemplateOBJ10 = Config.Bind("Editor - Fields", "Theme Template OBJ 10", LSColors.gray100, "OBJ 10 Color of the template theme.");
            ThemeTemplateOBJ11 = Config.Bind("Editor - Fields", "Theme Template OBJ 11", LSColors.gray200, "OBJ 11 Color of the template theme.");
            ThemeTemplateOBJ12 = Config.Bind("Editor - Fields", "Theme Template OBJ 12", LSColors.gray300, "OBJ 12 Color of the template theme.");
            ThemeTemplateOBJ13 = Config.Bind("Editor - Fields", "Theme Template OBJ 13", LSColors.gray400, "OBJ 13 Color of the template theme.");
            ThemeTemplateOBJ14 = Config.Bind("Editor - Fields", "Theme Template OBJ 14", LSColors.gray500, "OBJ 14 Color of the template theme.");
            ThemeTemplateOBJ15 = Config.Bind("Editor - Fields", "Theme Template OBJ 15", LSColors.gray600, "OBJ 15 Color of the template theme.");
            ThemeTemplateOBJ16 = Config.Bind("Editor - Fields", "Theme Template OBJ 16", LSColors.gray700, "OBJ 16 Color of the template theme.");
            ThemeTemplateOBJ17 = Config.Bind("Editor - Fields", "Theme Template OBJ 17", LSColors.gray800, "OBJ 17 Color of the template theme.");
            ThemeTemplateOBJ18 = Config.Bind("Editor - Fields", "Theme Template OBJ 18", LSColors.gray900, "OBJ 18 Color of the template theme.");
            ThemeTemplateBG1 = Config.Bind("Editor - Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
            ThemeTemplateBG2 = Config.Bind("Editor - Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
            ThemeTemplateBG3 = Config.Bind("Editor - Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
            ThemeTemplateBG4 = Config.Bind("Editor - Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
            ThemeTemplateBG5 = Config.Bind("Editor - Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
            ThemeTemplateBG6 = Config.Bind("Editor - Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
            ThemeTemplateBG7 = Config.Bind("Editor - Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
            ThemeTemplateBG8 = Config.Bind("Editor - Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
            ThemeTemplateBG9 = Config.Bind("Editor - Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");
            ThemeTemplateFX1 = Config.Bind("Editor - Fields", "Theme Template FX 1", LSColors.gray100, "FX 1 Color of the template theme.");
            ThemeTemplateFX2 = Config.Bind("Editor - Fields", "Theme Template FX 2", LSColors.gray200, "FX 2 Color of the template theme.");
            ThemeTemplateFX3 = Config.Bind("Editor - Fields", "Theme Template FX 3", LSColors.gray300, "FX 3 Color of the template theme.");
            ThemeTemplateFX4 = Config.Bind("Editor - Fields", "Theme Template FX 4", LSColors.gray400, "FX 4 Color of the template theme.");
            ThemeTemplateFX5 = Config.Bind("Editor - Fields", "Theme Template FX 5", LSColors.gray500, "FX 5 Color of the template theme.");
            ThemeTemplateFX6 = Config.Bind("Editor - Fields", "Theme Template FX 6", LSColors.gray600, "FX 6 Color of the template theme.");
            ThemeTemplateFX7 = Config.Bind("Editor - Fields", "Theme Template FX 7", LSColors.gray700, "FX 7 Color of the template theme.");
            ThemeTemplateFX8 = Config.Bind("Editor - Fields", "Theme Template FX 8", LSColors.gray800, "FX 8 Color of the template theme.");
            ThemeTemplateFX9 = Config.Bind("Editor - Fields", "Theme Template FX 9", LSColors.gray900, "FX 9 Color of the template theme.");
            ThemeTemplateFX10 = Config.Bind("Editor - Fields", "Theme Template FX 10", LSColors.gray100, "FX 10 Color of the template theme.");
            ThemeTemplateFX11 = Config.Bind("Editor - Fields", "Theme Template FX 11", LSColors.gray200, "FX 11 Color of the template theme.");
            ThemeTemplateFX12 = Config.Bind("Editor - Fields", "Theme Template FX 12", LSColors.gray300, "FX 12 Color of the template theme.");
            ThemeTemplateFX13 = Config.Bind("Editor - Fields", "Theme Template FX 13", LSColors.gray400, "FX 13 Color of the template theme.");
            ThemeTemplateFX14 = Config.Bind("Editor - Fields", "Theme Template FX 14", LSColors.gray500, "FX 14 Color of the template theme.");
            ThemeTemplateFX15 = Config.Bind("Editor - Fields", "Theme Template FX 15", LSColors.gray600, "FX 15 Color of the template theme.");
            ThemeTemplateFX16 = Config.Bind("Editor - Fields", "Theme Template FX 16", LSColors.gray700, "FX 16 Color of the template theme.");
            ThemeTemplateFX17 = Config.Bind("Editor - Fields", "Theme Template FX 17", LSColors.gray800, "FX 17 Color of the template theme.");
            ThemeTemplateFX18 = Config.Bind("Editor - Fields", "Theme Template FX 18", LSColors.gray900, "FX 18 Color of the template theme.");

            #endregion

            #region Animations

            PlayEditorAnimations = Config.Bind("Editor - Animations", "Play Editor Animations", false, "If popups should be animated.");

            #region Open File Popup

            OpenFilePopupActive = Config.Bind("Editor - Animations", "Open File Popup Active", true, "If the popup animation should play.");

            OpenFilePopupPosActive = Config.Bind("Editor - Animations", "Open File Popup Animate Position", false, "If position should be animated.");
            OpenFilePopupPosOpen = Config.Bind("Editor - Animations", "Open File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupPosClose = Config.Bind("Editor - Animations", "Open File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupPosOpenDuration = Config.Bind("Editor - Animations", "Open File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            OpenFilePopupPosCloseDuration = Config.Bind("Editor - Animations", "Open File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            OpenFilePopupPosXOpenEase = Config.Bind("Editor - Animations", "Open File Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosXCloseEase = Config.Bind("Editor - Animations", "Open File Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosYOpenEase = Config.Bind("Editor - Animations", "Open File Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosYCloseEase = Config.Bind("Editor - Animations", "Open File Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            OpenFilePopupScaActive = Config.Bind("Editor - Animations", "Open File Popup Animate Scale", true, "If scale should be animated.");
            OpenFilePopupScaOpen = Config.Bind("Editor - Animations", "Open File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupScaClose = Config.Bind("Editor - Animations", "Open File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupScaOpenDuration = Config.Bind("Editor - Animations", "Open File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            OpenFilePopupScaCloseDuration = Config.Bind("Editor - Animations", "Open File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            OpenFilePopupScaXOpenEase = Config.Bind("Editor - Animations", "Open File Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            OpenFilePopupScaXCloseEase = Config.Bind("Editor - Animations", "Open File Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            OpenFilePopupScaYOpenEase = Config.Bind("Editor - Animations", "Open File Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            OpenFilePopupScaYCloseEase = Config.Bind("Editor - Animations", "Open File Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            OpenFilePopupRotActive = Config.Bind("Editor - Animations", "Open File Popup Animate Rotation", false, "If rotation should be animated.");
            OpenFilePopupRotOpen = Config.Bind("Editor - Animations", "Open File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupRotClose = Config.Bind("Editor - Animations", "Open File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupRotOpenDuration = Config.Bind("Editor - Animations", "Open File Popup Open Rotation Duration", 0f, "The duration of opening.");
            OpenFilePopupRotCloseDuration = Config.Bind("Editor - Animations", "Open File Popup Close Rotation Duration", 0f, "The duration of closing.");
            OpenFilePopupRotOpenEase = Config.Bind("Editor - Animations", "Open File Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupRotCloseEase = Config.Bind("Editor - Animations", "Open File Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region New File Popup

            NewFilePopupActive = Config.Bind("Editor - Animations", "New File Popup Active", true, "If the popup animation should play.");

            NewFilePopupPosActive = Config.Bind("Editor - Animations", "New File Popup Animate Position", false, "If position should be animated.");
            NewFilePopupPosOpen = Config.Bind("Editor - Animations", "New File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupPosClose = Config.Bind("Editor - Animations", "New File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupPosOpenDuration = Config.Bind("Editor - Animations", "New File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            NewFilePopupPosCloseDuration = Config.Bind("Editor - Animations", "New File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            NewFilePopupPosXOpenEase = Config.Bind("Editor - Animations", "New File Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosXCloseEase = Config.Bind("Editor - Animations", "New File Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosYOpenEase = Config.Bind("Editor - Animations", "New File Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosYCloseEase = Config.Bind("Editor - Animations", "New File Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            NewFilePopupScaActive = Config.Bind("Editor - Animations", "New File Popup Animate Scale", true, "If scale should be animated.");
            NewFilePopupScaOpen = Config.Bind("Editor - Animations", "New File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupScaClose = Config.Bind("Editor - Animations", "New File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupScaOpenDuration = Config.Bind("Editor - Animations", "New File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            NewFilePopupScaCloseDuration = Config.Bind("Editor - Animations", "New File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            NewFilePopupScaXOpenEase = Config.Bind("Editor - Animations", "New File Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            NewFilePopupScaXCloseEase = Config.Bind("Editor - Animations", "New File Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            NewFilePopupScaYOpenEase = Config.Bind("Editor - Animations", "New File Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            NewFilePopupScaYCloseEase = Config.Bind("Editor - Animations", "New File Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            NewFilePopupRotActive = Config.Bind("Editor - Animations", "New File Popup Animate Rotation", false, "If rotation should be animated.");
            NewFilePopupRotOpen = Config.Bind("Editor - Animations", "New File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupRotClose = Config.Bind("Editor - Animations", "New File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupRotOpenDuration = Config.Bind("Editor - Animations", "New File Popup Open Rotation Duration", 0f, "The duration of opening.");
            NewFilePopupRotCloseDuration = Config.Bind("Editor - Animations", "New File Popup Close Rotation Duration", 0f, "The duration of closing.");
            NewFilePopupRotOpenEase = Config.Bind("Editor - Animations", "New File Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupRotCloseEase = Config.Bind("Editor - Animations", "New File Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Save As Popup

            SaveAsPopupActive = Config.Bind("Editor - Animations", "Save As Popup Active", true, "If the popup animation should play.");

            SaveAsPopupPosActive = Config.Bind("Editor - Animations", "Save As Popup Animate Position", false, "If position should be animated.");
            SaveAsPopupPosOpen = Config.Bind("Editor - Animations", "Save As Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupPosClose = Config.Bind("Editor - Animations", "Save As Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupPosOpenDuration = Config.Bind("Editor - Animations", "Save As Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            SaveAsPopupPosCloseDuration = Config.Bind("Editor - Animations", "Save As Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            SaveAsPopupPosXOpenEase = Config.Bind("Editor - Animations", "Save As Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosXCloseEase = Config.Bind("Editor - Animations", "Save As Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosYOpenEase = Config.Bind("Editor - Animations", "Save As Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosYCloseEase = Config.Bind("Editor - Animations", "Save As Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            SaveAsPopupScaActive = Config.Bind("Editor - Animations", "Save As Popup Animate Scale", true, "If scale should be animated.");
            SaveAsPopupScaOpen = Config.Bind("Editor - Animations", "Save As Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupScaClose = Config.Bind("Editor - Animations", "Save As Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupScaOpenDuration = Config.Bind("Editor - Animations", "Save As Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SaveAsPopupScaCloseDuration = Config.Bind("Editor - Animations", "Save As Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SaveAsPopupScaXOpenEase = Config.Bind("Editor - Animations", "Save As Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            SaveAsPopupScaXCloseEase = Config.Bind("Editor - Animations", "Save As Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            SaveAsPopupScaYOpenEase = Config.Bind("Editor - Animations", "Save As Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            SaveAsPopupScaYCloseEase = Config.Bind("Editor - Animations", "Save As Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            SaveAsPopupRotActive = Config.Bind("Editor - Animations", "Save As Popup Animate Rotation", false, "If rotation should be animated.");
            SaveAsPopupRotOpen = Config.Bind("Editor - Animations", "Save As Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupRotClose = Config.Bind("Editor - Animations", "Save As Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupRotOpenDuration = Config.Bind("Editor - Animations", "Save As Popup Open Rotation Duration", 0f, "The duration of opening.");
            SaveAsPopupRotCloseDuration = Config.Bind("Editor - Animations", "Save As Popup Close Rotation Duration", 0f, "The duration of closing.");
            SaveAsPopupRotOpenEase = Config.Bind("Editor - Animations", "Save As Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupRotCloseEase = Config.Bind("Editor - Animations", "Save As Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Quick Actions Popup

            QuickActionsPopupActive = Config.Bind("Editor - Animations", "Quick Actions Popup Active", true, "If the popup animation should play.");

            QuickActionsPopupPosActive = Config.Bind("Editor - Animations", "Quick Actions Popup Animate Position", false, "If position should be animated.");
            QuickActionsPopupPosOpen = Config.Bind("Editor - Animations", "Quick Actions Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupPosClose = Config.Bind("Editor - Animations", "Quick Actions Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupPosOpenDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            QuickActionsPopupPosCloseDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            QuickActionsPopupPosXOpenEase = Config.Bind("Editor - Animations", "Quick Actions Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosXCloseEase = Config.Bind("Editor - Animations", "Quick Actions Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosYOpenEase = Config.Bind("Editor - Animations", "Quick Actions Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosYCloseEase = Config.Bind("Editor - Animations", "Quick Actions Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            QuickActionsPopupScaActive = Config.Bind("Editor - Animations", "Quick Actions Popup Animate Scale", true, "If scale should be animated.");
            QuickActionsPopupScaOpen = Config.Bind("Editor - Animations", "Quick Actions Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupScaClose = Config.Bind("Editor - Animations", "Quick Actions Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupScaOpenDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            QuickActionsPopupScaCloseDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            QuickActionsPopupScaXOpenEase = Config.Bind("Editor - Animations", "Quick Actions Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            QuickActionsPopupScaXCloseEase = Config.Bind("Editor - Animations", "Quick Actions Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            QuickActionsPopupScaYOpenEase = Config.Bind("Editor - Animations", "Quick Actions Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            QuickActionsPopupScaYCloseEase = Config.Bind("Editor - Animations", "Quick Actions Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            QuickActionsPopupRotActive = Config.Bind("Editor - Animations", "Quick Actions Popup Animate Rotation", false, "If rotation should be animated.");
            QuickActionsPopupRotOpen = Config.Bind("Editor - Animations", "Quick Actions Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupRotClose = Config.Bind("Editor - Animations", "Quick Actions Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupRotOpenDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Open Rotation Duration", 0f, "The duration of opening.");
            QuickActionsPopupRotCloseDuration = Config.Bind("Editor - Animations", "Quick Actions Popup Close Rotation Duration", 0f, "The duration of closing.");
            QuickActionsPopupRotOpenEase = Config.Bind("Editor - Animations", "Quick Actions Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupRotCloseEase = Config.Bind("Editor - Animations", "Quick Actions Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Parent Selector

            ParentSelectorPopupActive = Config.Bind("Editor - Animations", "Parent Selector Popup Active", true, "If the popup animation should play.");

            ParentSelectorPopupPosActive = Config.Bind("Editor - Animations", "Parent Selector Popup Animate Position", false, "If position should be animated.");
            ParentSelectorPopupPosOpen = Config.Bind("Editor - Animations", "Parent Selector Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupPosClose = Config.Bind("Editor - Animations", "Parent Selector Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupPosOpenDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ParentSelectorPopupPosCloseDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ParentSelectorPopupPosXOpenEase = Config.Bind("Editor - Animations", "Parent Selector Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosXCloseEase = Config.Bind("Editor - Animations", "Parent Selector Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosYOpenEase = Config.Bind("Editor - Animations", "Parent Selector Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosYCloseEase = Config.Bind("Editor - Animations", "Parent Selector Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ParentSelectorPopupScaActive = Config.Bind("Editor - Animations", "Parent Selector Popup Animate Scale", true, "If scale should be animated.");
            ParentSelectorPopupScaOpen = Config.Bind("Editor - Animations", "Parent Selector Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupScaClose = Config.Bind("Editor - Animations", "Parent Selector Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupScaOpenDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ParentSelectorPopupScaCloseDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ParentSelectorPopupScaXOpenEase = Config.Bind("Editor - Animations", "Parent Selector Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaXCloseEase = Config.Bind("Editor - Animations", "Parent Selector Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ParentSelectorPopupScaYOpenEase = Config.Bind("Editor - Animations", "Parent Selector Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaYCloseEase = Config.Bind("Editor - Animations", "Parent Selector Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ParentSelectorPopupRotActive = Config.Bind("Editor - Animations", "Parent Selector Popup Animate Rotation", false, "If rotation should be animated.");
            ParentSelectorPopupRotOpen = Config.Bind("Editor - Animations", "Parent Selector Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupRotClose = Config.Bind("Editor - Animations", "Parent Selector Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupRotOpenDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Open Rotation Duration", 0f, "The duration of opening.");
            ParentSelectorPopupRotCloseDuration = Config.Bind("Editor - Animations", "Parent Selector Popup Close Rotation Duration", 0f, "The duration of closing.");
            ParentSelectorPopupRotOpenEase = Config.Bind("Editor - Animations", "Parent Selector Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupRotCloseEase = Config.Bind("Editor - Animations", "Parent Selector Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Prefab Popup

            PrefabPopupActive = Config.Bind("Editor - Animations", "Prefab Popup Active", true, "If the popup animation should play.");

            PrefabPopupPosActive = Config.Bind("Editor - Animations", "Prefab Popup Animate Position", false, "If position should be animated.");
            PrefabPopupPosOpen = Config.Bind("Editor - Animations", "Prefab Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupPosClose = Config.Bind("Editor - Animations", "Prefab Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupPosOpenDuration = Config.Bind("Editor - Animations", "Prefab Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabPopupPosCloseDuration = Config.Bind("Editor - Animations", "Prefab Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabPopupPosXOpenEase = Config.Bind("Editor - Animations", "Prefab Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosXCloseEase = Config.Bind("Editor - Animations", "Prefab Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosYOpenEase = Config.Bind("Editor - Animations", "Prefab Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosYCloseEase = Config.Bind("Editor - Animations", "Prefab Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            PrefabPopupScaActive = Config.Bind("Editor - Animations", "Prefab Popup Animate Scale", true, "If scale should be animated.");
            PrefabPopupScaOpen = Config.Bind("Editor - Animations", "Prefab Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupScaClose = Config.Bind("Editor - Animations", "Prefab Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupScaOpenDuration = Config.Bind("Editor - Animations", "Prefab Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabPopupScaCloseDuration = Config.Bind("Editor - Animations", "Prefab Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabPopupScaXOpenEase = Config.Bind("Editor - Animations", "Prefab Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            PrefabPopupScaXCloseEase = Config.Bind("Editor - Animations", "Prefab Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            PrefabPopupScaYOpenEase = Config.Bind("Editor - Animations", "Prefab Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            PrefabPopupScaYCloseEase = Config.Bind("Editor - Animations", "Prefab Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            PrefabPopupRotActive = Config.Bind("Editor - Animations", "Prefab Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabPopupRotOpen = Config.Bind("Editor - Animations", "Prefab Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupRotClose = Config.Bind("Editor - Animations", "Prefab Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupRotOpenDuration = Config.Bind("Editor - Animations", "Prefab Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabPopupRotCloseDuration = Config.Bind("Editor - Animations", "Prefab Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabPopupRotOpenEase = Config.Bind("Editor - Animations", "Prefab Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupRotCloseEase = Config.Bind("Editor - Animations", "Prefab Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Object Options Popup

            ObjectOptionsPopupActive = Config.Bind("Editor - Animations", "Object Options Popup Active", true, "If the popup animation should play.");

            ObjectOptionsPopupPosActive = Config.Bind("Editor - Animations", "Object Options Popup Animate Position", true, "If position should be animated.");
            ObjectOptionsPopupPosOpen = Config.Bind("Editor - Animations", "Object Options Popup Open Position", new Vector2(-35f, 22f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupPosClose = Config.Bind("Editor - Animations", "Object Options Popup Close Position", new Vector2(-35f, 57f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupPosOpenDuration = Config.Bind("Editor - Animations", "Object Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupPosCloseDuration = Config.Bind("Editor - Animations", "Object Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupPosXOpenEase = Config.Bind("Editor - Animations", "Object Options Popup Open Position X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosXCloseEase = Config.Bind("Editor - Animations", "Object Options Popup Close Position X Ease", Easings.InCirc, "The easing of opening.");
            ObjectOptionsPopupPosYOpenEase = Config.Bind("Editor - Animations", "Object Options Popup Open Position Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosYCloseEase = Config.Bind("Editor - Animations", "Object Options Popup Close Position Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectOptionsPopupScaActive = Config.Bind("Editor - Animations", "Object Options Popup Animate Scale", true, "If scale should be animated.");
            ObjectOptionsPopupScaOpen = Config.Bind("Editor - Animations", "Object Options Popup Open Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupScaClose = Config.Bind("Editor - Animations", "Object Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupScaOpenDuration = Config.Bind("Editor - Animations", "Object Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupScaCloseDuration = Config.Bind("Editor - Animations", "Object Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupScaXOpenEase = Config.Bind("Editor - Animations", "Object Options Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaXCloseEase = Config.Bind("Editor - Animations", "Object Options Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ObjectOptionsPopupScaYOpenEase = Config.Bind("Editor - Animations", "Object Options Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaYCloseEase = Config.Bind("Editor - Animations", "Object Options Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectOptionsPopupRotActive = Config.Bind("Editor - Animations", "Object Options Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectOptionsPopupRotOpen = Config.Bind("Editor - Animations", "Object Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupRotClose = Config.Bind("Editor - Animations", "Object Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupRotOpenDuration = Config.Bind("Editor - Animations", "Object Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectOptionsPopupRotCloseDuration = Config.Bind("Editor - Animations", "Object Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectOptionsPopupRotOpenEase = Config.Bind("Editor - Animations", "Object Options Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ObjectOptionsPopupRotCloseEase = Config.Bind("Editor - Animations", "Object Options Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region BG Options Popup

            BGOptionsPopupActive = Config.Bind("Editor - Animations", "BG Options Popup Active", true, "If the popup animation should play.");

            BGOptionsPopupPosActive = Config.Bind("Editor - Animations", "BG Options Popup Animate Position", true, "If position should be animated.");
            BGOptionsPopupPosOpen = Config.Bind("Editor - Animations", "BG Options Popup Open Position", new Vector2(0f, 57f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupPosClose = Config.Bind("Editor - Animations", "BG Options Popup Close Position", new Vector2(0f, 22f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupPosOpenDuration = Config.Bind("Editor - Animations", "BG Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            BGOptionsPopupPosCloseDuration = Config.Bind("Editor - Animations", "BG Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            BGOptionsPopupPosXOpenEase = Config.Bind("Editor - Animations", "BG Options Popup Open Position X Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupPosXCloseEase = Config.Bind("Editor - Animations", "BG Options Popup Close Position X Ease", Easings.InCirc, "The easing of opening.");
            BGOptionsPopupPosYOpenEase = Config.Bind("Editor - Animations", "BG Options Popup Open Position Y Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupPosYCloseEase = Config.Bind("Editor - Animations", "BG Options Popup Close Position Y Ease", Easings.InCirc, "The easing of opening.");

            BGOptionsPopupScaActive = Config.Bind("Editor - Animations", "BG Options Popup Animate Scale", true, "If scale should be animated.");
            BGOptionsPopupScaOpen = Config.Bind("Editor - Animations", "BG Options Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupScaClose = Config.Bind("Editor - Animations", "BG Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupScaOpenDuration = Config.Bind("Editor - Animations", "BG Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BGOptionsPopupScaCloseDuration = Config.Bind("Editor - Animations", "BG Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BGOptionsPopupScaXOpenEase = Config.Bind("Editor - Animations", "BG Options Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupScaXCloseEase = Config.Bind("Editor - Animations", "BG Options Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            BGOptionsPopupScaYOpenEase = Config.Bind("Editor - Animations", "BG Options Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupScaYCloseEase = Config.Bind("Editor - Animations", "BG Options Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            BGOptionsPopupRotActive = Config.Bind("Editor - Animations", "BG Options Popup Animate Rotation", false, "If rotation should be animated.");
            BGOptionsPopupRotOpen = Config.Bind("Editor - Animations", "BG Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupRotClose = Config.Bind("Editor - Animations", "BG Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupRotOpenDuration = Config.Bind("Editor - Animations", "BG Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            BGOptionsPopupRotCloseDuration = Config.Bind("Editor - Animations", "BG Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            BGOptionsPopupRotOpenEase = Config.Bind("Editor - Animations", "BG Options Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            BGOptionsPopupRotCloseEase = Config.Bind("Editor - Animations", "BG Options Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Browser Popup

            BrowserPopupActive = Config.Bind("Editor - Animations", "Browser Popup Active", true, "If the popup animation should play.");

            BrowserPopupPosActive = Config.Bind("Editor - Animations", "Browser Popup Animate Position", false, "If position should be animated.");
            BrowserPopupPosOpen = Config.Bind("Editor - Animations", "Browser Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupPosClose = Config.Bind("Editor - Animations", "Browser Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupPosOpenDuration = Config.Bind("Editor - Animations", "Browser Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            BrowserPopupPosCloseDuration = Config.Bind("Editor - Animations", "Browser Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            BrowserPopupPosXOpenEase = Config.Bind("Editor - Animations", "Browser Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosXCloseEase = Config.Bind("Editor - Animations", "Browser Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosYOpenEase = Config.Bind("Editor - Animations", "Browser Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosYCloseEase = Config.Bind("Editor - Animations", "Browser Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            BrowserPopupScaActive = Config.Bind("Editor - Animations", "Browser Popup Animate Scale", true, "If scale should be animated.");
            BrowserPopupScaOpen = Config.Bind("Editor - Animations", "Browser Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupScaClose = Config.Bind("Editor - Animations", "Browser Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupScaOpenDuration = Config.Bind("Editor - Animations", "Browser Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BrowserPopupScaCloseDuration = Config.Bind("Editor - Animations", "Browser Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BrowserPopupScaXOpenEase = Config.Bind("Editor - Animations", "Browser Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            BrowserPopupScaXCloseEase = Config.Bind("Editor - Animations", "Browser Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            BrowserPopupScaYOpenEase = Config.Bind("Editor - Animations", "Browser Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            BrowserPopupScaYCloseEase = Config.Bind("Editor - Animations", "Browser Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            BrowserPopupRotActive = Config.Bind("Editor - Animations", "Browser Popup Animate Rotation", false, "If rotation should be animated.");
            BrowserPopupRotOpen = Config.Bind("Editor - Animations", "Browser Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupRotClose = Config.Bind("Editor - Animations", "Browser Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupRotOpenDuration = Config.Bind("Editor - Animations", "Browser Popup Open Rotation Duration", 0f, "The duration of opening.");
            BrowserPopupRotCloseDuration = Config.Bind("Editor - Animations", "Browser Popup Close Rotation Duration", 0f, "The duration of closing.");
            BrowserPopupRotOpenEase = Config.Bind("Editor - Animations", "Browser Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupRotCloseEase = Config.Bind("Editor - Animations", "Browser Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Object Search Popup

            ObjectSearchPopupActive = Config.Bind("Editor - Animations", "Object Search Popup Active", true, "If the popup animation should play.");

            ObjectSearchPopupPosActive = Config.Bind("Editor - Animations", "Object Search Popup Animate Position", false, "If position should be animated.");
            ObjectSearchPopupPosOpen = Config.Bind("Editor - Animations", "Object Search Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupPosClose = Config.Bind("Editor - Animations", "Object Search Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupPosOpenDuration = Config.Bind("Editor - Animations", "Object Search Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ObjectSearchPopupPosCloseDuration = Config.Bind("Editor - Animations", "Object Search Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ObjectSearchPopupPosXOpenEase = Config.Bind("Editor - Animations", "Object Search Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosXCloseEase = Config.Bind("Editor - Animations", "Object Search Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosYOpenEase = Config.Bind("Editor - Animations", "Object Search Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosYCloseEase = Config.Bind("Editor - Animations", "Object Search Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ObjectSearchPopupScaActive = Config.Bind("Editor - Animations", "Object Search Popup Animate Scale", true, "If scale should be animated.");
            ObjectSearchPopupScaOpen = Config.Bind("Editor - Animations", "Object Search Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupScaClose = Config.Bind("Editor - Animations", "Object Search Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupScaOpenDuration = Config.Bind("Editor - Animations", "Object Search Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectSearchPopupScaCloseDuration = Config.Bind("Editor - Animations", "Object Search Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectSearchPopupScaXOpenEase = Config.Bind("Editor - Animations", "Object Search Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaXCloseEase = Config.Bind("Editor - Animations", "Object Search Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ObjectSearchPopupScaYOpenEase = Config.Bind("Editor - Animations", "Object Search Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaYCloseEase = Config.Bind("Editor - Animations", "Object Search Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectSearchPopupRotActive = Config.Bind("Editor - Animations", "Object Search Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectSearchPopupRotOpen = Config.Bind("Editor - Animations", "Object Search Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupRotClose = Config.Bind("Editor - Animations", "Object Search Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupRotOpenDuration = Config.Bind("Editor - Animations", "Object Search Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectSearchPopupRotCloseDuration = Config.Bind("Editor - Animations", "Object Search Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectSearchPopupRotOpenEase = Config.Bind("Editor - Animations", "Object Search Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupRotCloseEase = Config.Bind("Editor - Animations", "Object Search Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Warning Popup

            WarningPopupActive = Config.Bind("Editor - Animations", "Warning Popup Active", true, "If the popup animation should play.");

            WarningPopupPosActive = Config.Bind("Editor - Animations", "Warning Popup Animate Position", false, "If position should be animated.");
            WarningPopupPosOpen = Config.Bind("Editor - Animations", "Warning Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupPosClose = Config.Bind("Editor - Animations", "Warning Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupPosOpenDuration = Config.Bind("Editor - Animations", "Warning Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            WarningPopupPosCloseDuration = Config.Bind("Editor - Animations", "Warning Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            WarningPopupPosXOpenEase = Config.Bind("Editor - Animations", "Warning Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosXCloseEase = Config.Bind("Editor - Animations", "Warning Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosYOpenEase = Config.Bind("Editor - Animations", "Warning Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosYCloseEase = Config.Bind("Editor - Animations", "Warning Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            WarningPopupScaActive = Config.Bind("Editor - Animations", "Warning Popup Animate Scale", true, "If scale should be animated.");
            WarningPopupScaOpen = Config.Bind("Editor - Animations", "Warning Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupScaClose = Config.Bind("Editor - Animations", "Warning Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupScaOpenDuration = Config.Bind("Editor - Animations", "Warning Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            WarningPopupScaCloseDuration = Config.Bind("Editor - Animations", "Warning Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            WarningPopupScaXOpenEase = Config.Bind("Editor - Animations", "Warning Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            WarningPopupScaXCloseEase = Config.Bind("Editor - Animations", "Warning Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            WarningPopupScaYOpenEase = Config.Bind("Editor - Animations", "Warning Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            WarningPopupScaYCloseEase = Config.Bind("Editor - Animations", "Warning Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            WarningPopupRotActive = Config.Bind("Editor - Animations", "Warning Popup Animate Rotation", false, "If rotation should be animated.");
            WarningPopupRotOpen = Config.Bind("Editor - Animations", "Warning Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupRotClose = Config.Bind("Editor - Animations", "Warning Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupRotOpenDuration = Config.Bind("Editor - Animations", "Warning Popup Open Rotation Duration", 0f, "The duration of opening.");
            WarningPopupRotCloseDuration = Config.Bind("Editor - Animations", "Warning Popup Close Rotation Duration", 0f, "The duration of closing.");
            WarningPopupRotOpenEase = Config.Bind("Editor - Animations", "Warning Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            WarningPopupRotCloseEase = Config.Bind("Editor - Animations", "Warning Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region REPL Editor Popup

            REPLEditorPopupActive = Config.Bind("Editor - Animations", "REPL Editor Popup Active", true, "If the popup animation should play.");

            REPLEditorPopupPosActive = Config.Bind("Editor - Animations", "REPL Editor Popup Animate Position", false, "If position should be animated.");
            REPLEditorPopupPosOpen = Config.Bind("Editor - Animations", "REPL Editor Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            REPLEditorPopupPosClose = Config.Bind("Editor - Animations", "REPL Editor Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            REPLEditorPopupPosOpenDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            REPLEditorPopupPosCloseDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            REPLEditorPopupPosXOpenEase = Config.Bind("Editor - Animations", "REPL Editor Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            REPLEditorPopupPosXCloseEase = Config.Bind("Editor - Animations", "REPL Editor Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            REPLEditorPopupPosYOpenEase = Config.Bind("Editor - Animations", "REPL Editor Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            REPLEditorPopupPosYCloseEase = Config.Bind("Editor - Animations", "REPL Editor Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            REPLEditorPopupScaActive = Config.Bind("Editor - Animations", "REPL Editor Popup Animate Scale", true, "If scale should be animated.");
            REPLEditorPopupScaOpen = Config.Bind("Editor - Animations", "REPL Editor Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            REPLEditorPopupScaClose = Config.Bind("Editor - Animations", "REPL Editor Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            REPLEditorPopupScaOpenDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            REPLEditorPopupScaCloseDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            REPLEditorPopupScaXOpenEase = Config.Bind("Editor - Animations", "REPL Editor Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            REPLEditorPopupScaXCloseEase = Config.Bind("Editor - Animations", "REPL Editor Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            REPLEditorPopupScaYOpenEase = Config.Bind("Editor - Animations", "REPL Editor Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            REPLEditorPopupScaYCloseEase = Config.Bind("Editor - Animations", "REPL Editor Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            REPLEditorPopupRotActive = Config.Bind("Editor - Animations", "REPL Editor Popup Animate Rotation", false, "If rotation should be animated.");
            REPLEditorPopupRotOpen = Config.Bind("Editor - Animations", "REPL Editor Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            REPLEditorPopupRotClose = Config.Bind("Editor - Animations", "REPL Editor Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            REPLEditorPopupRotOpenDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Open Rotation Duration", 0f, "The duration of opening.");
            REPLEditorPopupRotCloseDuration = Config.Bind("Editor - Animations", "REPL Editor Popup Close Rotation Duration", 0f, "The duration of closing.");
            REPLEditorPopupRotOpenEase = Config.Bind("Editor - Animations", "REPL Editor Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            REPLEditorPopupRotCloseEase = Config.Bind("Editor - Animations", "REPL Editor Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Editor Properties Popup

            EditorPropertiesPopupActive = Config.Bind("Editor - Animations", "Editor Properties Popup Active", true, "If the popup animation should play.");

            EditorPropertiesPopupPosActive = Config.Bind("Editor - Animations", "Editor Properties Popup Animate Position", false, "If position should be animated.");
            EditorPropertiesPopupPosOpen = Config.Bind("Editor - Animations", "Editor Properties Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditorPropertiesPopupPosClose = Config.Bind("Editor - Animations", "Editor Properties Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditorPropertiesPopupPosOpenDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            EditorPropertiesPopupPosCloseDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            EditorPropertiesPopupPosXOpenEase = Config.Bind("Editor - Animations", "Editor Properties Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            EditorPropertiesPopupPosXCloseEase = Config.Bind("Editor - Animations", "Editor Properties Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            EditorPropertiesPopupPosYOpenEase = Config.Bind("Editor - Animations", "Editor Properties Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            EditorPropertiesPopupPosYCloseEase = Config.Bind("Editor - Animations", "Editor Properties Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            EditorPropertiesPopupScaActive = Config.Bind("Editor - Animations", "Editor Properties Popup Animate Scale", true, "If scale should be animated.");
            EditorPropertiesPopupScaOpen = Config.Bind("Editor - Animations", "Editor Properties Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditorPropertiesPopupScaClose = Config.Bind("Editor - Animations", "Editor Properties Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditorPropertiesPopupScaOpenDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            EditorPropertiesPopupScaCloseDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            EditorPropertiesPopupScaXOpenEase = Config.Bind("Editor - Animations", "Editor Properties Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            EditorPropertiesPopupScaXCloseEase = Config.Bind("Editor - Animations", "Editor Properties Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            EditorPropertiesPopupScaYOpenEase = Config.Bind("Editor - Animations", "Editor Properties Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            EditorPropertiesPopupScaYCloseEase = Config.Bind("Editor - Animations", "Editor Properties Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            EditorPropertiesPopupRotActive = Config.Bind("Editor - Animations", "Editor Properties Popup Animate Rotation", false, "If rotation should be animated.");
            EditorPropertiesPopupRotOpen = Config.Bind("Editor - Animations", "Editor Properties Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditorPropertiesPopupRotClose = Config.Bind("Editor - Animations", "Editor Properties Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditorPropertiesPopupRotOpenDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Open Rotation Duration", 0f, "The duration of opening.");
            EditorPropertiesPopupRotCloseDuration = Config.Bind("Editor - Animations", "Editor Properties Popup Close Rotation Duration", 0f, "The duration of closing.");
            EditorPropertiesPopupRotOpenEase = Config.Bind("Editor - Animations", "Editor Properties Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            EditorPropertiesPopupRotCloseEase = Config.Bind("Editor - Animations", "Editor Properties Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Documentation Popup

            DocumentationPopupActive = Config.Bind("Editor - Animations", "Documentation Popup Active", true, "If the popup animation should play.");

            DocumentationPopupPosActive = Config.Bind("Editor - Animations", "Documentation Popup Animate Position", false, "If position should be animated.");
            DocumentationPopupPosOpen = Config.Bind("Editor - Animations", "Documentation Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupPosClose = Config.Bind("Editor - Animations", "Documentation Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupPosOpenDuration = Config.Bind("Editor - Animations", "Documentation Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DocumentationPopupPosCloseDuration = Config.Bind("Editor - Animations", "Documentation Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DocumentationPopupPosXOpenEase = Config.Bind("Editor - Animations", "Documentation Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosXCloseEase = Config.Bind("Editor - Animations", "Documentation Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosYOpenEase = Config.Bind("Editor - Animations", "Documentation Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosYCloseEase = Config.Bind("Editor - Animations", "Documentation Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DocumentationPopupScaActive = Config.Bind("Editor - Animations", "Documentation Popup Animate Scale", true, "If scale should be animated.");
            DocumentationPopupScaOpen = Config.Bind("Editor - Animations", "Documentation Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupScaClose = Config.Bind("Editor - Animations", "Documentation Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupScaOpenDuration = Config.Bind("Editor - Animations", "Documentation Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DocumentationPopupScaCloseDuration = Config.Bind("Editor - Animations", "Documentation Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DocumentationPopupScaXOpenEase = Config.Bind("Editor - Animations", "Documentation Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DocumentationPopupScaXCloseEase = Config.Bind("Editor - Animations", "Documentation Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DocumentationPopupScaYOpenEase = Config.Bind("Editor - Animations", "Documentation Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DocumentationPopupScaYCloseEase = Config.Bind("Editor - Animations", "Documentation Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DocumentationPopupRotActive = Config.Bind("Editor - Animations", "Documentation Popup Animate Rotation", false, "If rotation should be animated.");
            DocumentationPopupRotOpen = Config.Bind("Editor - Animations", "Documentation Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupRotClose = Config.Bind("Editor - Animations", "Documentation Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupRotOpenDuration = Config.Bind("Editor - Animations", "Documentation Popup Open Rotation Duration", 0f, "The duration of opening.");
            DocumentationPopupRotCloseDuration = Config.Bind("Editor - Animations", "Documentation Popup Close Rotation Duration", 0f, "The duration of closing.");
            DocumentationPopupRotOpenEase = Config.Bind("Editor - Animations", "Documentation Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupRotCloseEase = Config.Bind("Editor - Animations", "Documentation Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Debugger Popup

            DebuggerPopupActive = Config.Bind("Editor - Animations", "Debugger Popup Active", true, "If the popup animation should play.");

            DebuggerPopupPosActive = Config.Bind("Editor - Animations", "Debugger Popup Animate Position", false, "If position should be animated.");
            DebuggerPopupPosOpen = Config.Bind("Editor - Animations", "Debugger Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupPosClose = Config.Bind("Editor - Animations", "Debugger Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupPosOpenDuration = Config.Bind("Editor - Animations", "Debugger Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DebuggerPopupPosCloseDuration = Config.Bind("Editor - Animations", "Debugger Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DebuggerPopupPosXOpenEase = Config.Bind("Editor - Animations", "Debugger Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosXCloseEase = Config.Bind("Editor - Animations", "Debugger Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosYOpenEase = Config.Bind("Editor - Animations", "Debugger Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosYCloseEase = Config.Bind("Editor - Animations", "Debugger Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DebuggerPopupScaActive = Config.Bind("Editor - Animations", "Debugger Popup Animate Scale", true, "If scale should be animated.");
            DebuggerPopupScaOpen = Config.Bind("Editor - Animations", "Debugger Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupScaClose = Config.Bind("Editor - Animations", "Debugger Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupScaOpenDuration = Config.Bind("Editor - Animations", "Debugger Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DebuggerPopupScaCloseDuration = Config.Bind("Editor - Animations", "Debugger Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DebuggerPopupScaXOpenEase = Config.Bind("Editor - Animations", "Debugger Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DebuggerPopupScaXCloseEase = Config.Bind("Editor - Animations", "Debugger Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DebuggerPopupScaYOpenEase = Config.Bind("Editor - Animations", "Debugger Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DebuggerPopupScaYCloseEase = Config.Bind("Editor - Animations", "Debugger Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DebuggerPopupRotActive = Config.Bind("Editor - Animations", "Debugger Popup Animate Rotation", false, "If rotation should be animated.");
            DebuggerPopupRotOpen = Config.Bind("Editor - Animations", "Debugger Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupRotClose = Config.Bind("Editor - Animations", "Debugger Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupRotOpenDuration = Config.Bind("Editor - Animations", "Debugger Popup Open Rotation Duration", 0f, "The duration of opening.");
            DebuggerPopupRotCloseDuration = Config.Bind("Editor - Animations", "Debugger Popup Close Rotation Duration", 0f, "The duration of closing.");
            DebuggerPopupRotOpenEase = Config.Bind("Editor - Animations", "Debugger Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupRotCloseEase = Config.Bind("Editor - Animations", "Debugger Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Autosaves Popup

            AutosavesPopupActive = Config.Bind("Editor - Animations", "Autosaves Popup Active", true, "If the popup animation should play.");

            AutosavesPopupPosActive = Config.Bind("Editor - Animations", "Autosaves Popup Animate Position", false, "If position should be animated.");
            AutosavesPopupPosOpen = Config.Bind("Editor - Animations", "Autosaves Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupPosClose = Config.Bind("Editor - Animations", "Autosaves Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupPosOpenDuration = Config.Bind("Editor - Animations", "Autosaves Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            AutosavesPopupPosCloseDuration = Config.Bind("Editor - Animations", "Autosaves Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            AutosavesPopupPosXOpenEase = Config.Bind("Editor - Animations", "Autosaves Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosXCloseEase = Config.Bind("Editor - Animations", "Autosaves Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosYOpenEase = Config.Bind("Editor - Animations", "Autosaves Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosYCloseEase = Config.Bind("Editor - Animations", "Autosaves Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            AutosavesPopupScaActive = Config.Bind("Editor - Animations", "Autosaves Popup Animate Scale", true, "If scale should be animated.");
            AutosavesPopupScaOpen = Config.Bind("Editor - Animations", "Autosaves Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupScaClose = Config.Bind("Editor - Animations", "Autosaves Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupScaOpenDuration = Config.Bind("Editor - Animations", "Autosaves Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            AutosavesPopupScaCloseDuration = Config.Bind("Editor - Animations", "Autosaves Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            AutosavesPopupScaXOpenEase = Config.Bind("Editor - Animations", "Autosaves Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            AutosavesPopupScaXCloseEase = Config.Bind("Editor - Animations", "Autosaves Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            AutosavesPopupScaYOpenEase = Config.Bind("Editor - Animations", "Autosaves Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            AutosavesPopupScaYCloseEase = Config.Bind("Editor - Animations", "Autosaves Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            AutosavesPopupRotActive = Config.Bind("Editor - Animations", "Autosaves Popup Animate Rotation", false, "If rotation should be animated.");
            AutosavesPopupRotOpen = Config.Bind("Editor - Animations", "Autosaves Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupRotClose = Config.Bind("Editor - Animations", "Autosaves Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupRotOpenDuration = Config.Bind("Editor - Animations", "Autosaves Popup Open Rotation Duration", 0f, "The duration of opening.");
            AutosavesPopupRotCloseDuration = Config.Bind("Editor - Animations", "Autosaves Popup Close Rotation Duration", 0f, "The duration of closing.");
            AutosavesPopupRotOpenEase = Config.Bind("Editor - Animations", "Autosaves Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupRotCloseEase = Config.Bind("Editor - Animations", "Autosaves Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Default Modifiers Popup

            DefaultModifiersPopupActive = Config.Bind("Editor - Animations", "Default Modifiers Popup Active", true, "If the popup animation should play.");

            DefaultModifiersPopupPosActive = Config.Bind("Editor - Animations", "Default Modifiers Popup Animate Position", false, "If position should be animated.");
            DefaultModifiersPopupPosOpen = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupPosClose = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupPosOpenDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DefaultModifiersPopupPosCloseDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DefaultModifiersPopupPosXOpenEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosXCloseEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYOpenEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYCloseEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DefaultModifiersPopupScaActive = Config.Bind("Editor - Animations", "Default Modifiers Popup Animate Scale", true, "If scale should be animated.");
            DefaultModifiersPopupScaOpen = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupScaClose = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupScaOpenDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DefaultModifiersPopupScaCloseDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DefaultModifiersPopupScaXOpenEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaXCloseEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DefaultModifiersPopupScaYOpenEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaYCloseEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DefaultModifiersPopupRotActive = Config.Bind("Editor - Animations", "Default Modifiers Popup Animate Rotation", false, "If rotation should be animated.");
            DefaultModifiersPopupRotOpen = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupRotClose = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupRotOpenDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Rotation Duration", 0f, "The duration of opening.");
            DefaultModifiersPopupRotCloseDuration = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Rotation Duration", 0f, "The duration of closing.");
            DefaultModifiersPopupRotOpenEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupRotCloseEase = Config.Bind("Editor - Animations", "Default Modifiers Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Keybind List Popup

            KeybindListPopupActive = Config.Bind("Editor - Animations", "Keybind List Popup Active", true, "If the popup animation should play.");

            KeybindListPopupPosActive = Config.Bind("Editor - Animations", "Keybind List Popup Animate Position", false, "If position should be animated.");
            KeybindListPopupPosOpen = Config.Bind("Editor - Animations", "Keybind List Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupPosClose = Config.Bind("Editor - Animations", "Keybind List Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupPosOpenDuration = Config.Bind("Editor - Animations", "Keybind List Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            KeybindListPopupPosCloseDuration = Config.Bind("Editor - Animations", "Keybind List Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            KeybindListPopupPosXOpenEase = Config.Bind("Editor - Animations", "Keybind List Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosXCloseEase = Config.Bind("Editor - Animations", "Keybind List Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosYOpenEase = Config.Bind("Editor - Animations", "Keybind List Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosYCloseEase = Config.Bind("Editor - Animations", "Keybind List Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            KeybindListPopupScaActive = Config.Bind("Editor - Animations", "Keybind List Popup Animate Scale", true, "If scale should be animated.");
            KeybindListPopupScaOpen = Config.Bind("Editor - Animations", "Keybind List Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupScaClose = Config.Bind("Editor - Animations", "Keybind List Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupScaOpenDuration = Config.Bind("Editor - Animations", "Keybind List Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            KeybindListPopupScaCloseDuration = Config.Bind("Editor - Animations", "Keybind List Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            KeybindListPopupScaXOpenEase = Config.Bind("Editor - Animations", "Keybind List Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            KeybindListPopupScaXCloseEase = Config.Bind("Editor - Animations", "Keybind List Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            KeybindListPopupScaYOpenEase = Config.Bind("Editor - Animations", "Keybind List Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            KeybindListPopupScaYCloseEase = Config.Bind("Editor - Animations", "Keybind List Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            KeybindListPopupRotActive = Config.Bind("Editor - Animations", "Keybind List Popup Animate Rotation", false, "If rotation should be animated.");
            KeybindListPopupRotOpen = Config.Bind("Editor - Animations", "Keybind List Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupRotClose = Config.Bind("Editor - Animations", "Keybind List Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupRotOpenDuration = Config.Bind("Editor - Animations", "Keybind List Popup Open Rotation Duration", 0f, "The duration of opening.");
            KeybindListPopupRotCloseDuration = Config.Bind("Editor - Animations", "Keybind List Popup Close Rotation Duration", 0f, "The duration of closing.");
            KeybindListPopupRotOpenEase = Config.Bind("Editor - Animations", "Keybind List Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupRotCloseEase = Config.Bind("Editor - Animations", "Keybind List Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Theme Popup

            ThemePopupActive = Config.Bind("Editor - Animations", "Theme Popup Active", true, "If the popup animation should play.");

            ThemePopupPosActive = Config.Bind("Editor - Animations", "Theme Popup Animate Position", false, "If position should be animated.");
            ThemePopupPosOpen = Config.Bind("Editor - Animations", "Theme Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupPosClose = Config.Bind("Editor - Animations", "Theme Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupPosOpenDuration = Config.Bind("Editor - Animations", "Theme Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ThemePopupPosCloseDuration = Config.Bind("Editor - Animations", "Theme Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ThemePopupPosXOpenEase = Config.Bind("Editor - Animations", "Theme Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosXCloseEase = Config.Bind("Editor - Animations", "Theme Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosYOpenEase = Config.Bind("Editor - Animations", "Theme Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosYCloseEase = Config.Bind("Editor - Animations", "Theme Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ThemePopupScaActive = Config.Bind("Editor - Animations", "Theme Popup Animate Scale", true, "If scale should be animated.");
            ThemePopupScaOpen = Config.Bind("Editor - Animations", "Theme Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupScaClose = Config.Bind("Editor - Animations", "Theme Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupScaOpenDuration = Config.Bind("Editor - Animations", "Theme Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ThemePopupScaCloseDuration = Config.Bind("Editor - Animations", "Theme Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ThemePopupScaXOpenEase = Config.Bind("Editor - Animations", "Theme Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ThemePopupScaXCloseEase = Config.Bind("Editor - Animations", "Theme Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ThemePopupScaYOpenEase = Config.Bind("Editor - Animations", "Theme Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ThemePopupScaYCloseEase = Config.Bind("Editor - Animations", "Theme Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ThemePopupRotActive = Config.Bind("Editor - Animations", "Theme Popup Animate Rotation", false, "If rotation should be animated.");
            ThemePopupRotOpen = Config.Bind("Editor - Animations", "Theme Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupRotClose = Config.Bind("Editor - Animations", "Theme Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupRotOpenDuration = Config.Bind("Editor - Animations", "Theme Popup Open Rotation Duration", 0f, "The duration of opening.");
            ThemePopupRotCloseDuration = Config.Bind("Editor - Animations", "Theme Popup Close Rotation Duration", 0f, "The duration of closing.");
            ThemePopupRotOpenEase = Config.Bind("Editor - Animations", "Theme Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ThemePopupRotCloseEase = Config.Bind("Editor - Animations", "Theme Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Prefab Types Popup

            PrefabTypesPopupActive = Config.Bind("Editor - Animations", "Prefab Types Popup Active", true, "If the popup animation should play.");

            PrefabTypesPopupPosActive = Config.Bind("Editor - Animations", "Prefab Types Popup Animate Position", false, "If position should be animated.");
            PrefabTypesPopupPosOpen = Config.Bind("Editor - Animations", "Prefab Types Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupPosClose = Config.Bind("Editor - Animations", "Prefab Types Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupPosOpenDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabTypesPopupPosCloseDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabTypesPopupPosXOpenEase = Config.Bind("Editor - Animations", "Prefab Types Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosXCloseEase = Config.Bind("Editor - Animations", "Prefab Types Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosYOpenEase = Config.Bind("Editor - Animations", "Prefab Types Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosYCloseEase = Config.Bind("Editor - Animations", "Prefab Types Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            PrefabTypesPopupScaActive = Config.Bind("Editor - Animations", "Prefab Types Popup Animate Scale", true, "If scale should be animated.");
            PrefabTypesPopupScaOpen = Config.Bind("Editor - Animations", "Prefab Types Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupScaClose = Config.Bind("Editor - Animations", "Prefab Types Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupScaOpenDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabTypesPopupScaCloseDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabTypesPopupScaXOpenEase = Config.Bind("Editor - Animations", "Prefab Types Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaXCloseEase = Config.Bind("Editor - Animations", "Prefab Types Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            PrefabTypesPopupScaYOpenEase = Config.Bind("Editor - Animations", "Prefab Types Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaYCloseEase = Config.Bind("Editor - Animations", "Prefab Types Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            PrefabTypesPopupRotActive = Config.Bind("Editor - Animations", "Prefab Types Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabTypesPopupRotOpen = Config.Bind("Editor - Animations", "Prefab Types Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupRotClose = Config.Bind("Editor - Animations", "Prefab Types Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupRotOpenDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabTypesPopupRotCloseDuration = Config.Bind("Editor - Animations", "Prefab Types Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabTypesPopupRotOpenEase = Config.Bind("Editor - Animations", "Prefab Types Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupRotCloseEase = Config.Bind("Editor - Animations", "Prefab Types Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region File Dropdown

            FileDropdownActive = Config.Bind("Editor - Animations", "File Dropdown Active", true, "If the popup animation should play.");

            FileDropdownPosActive = Config.Bind("Editor - Animations", "File Dropdown Animate Position", false, "If position should be animated.");
            FileDropdownPosOpen = Config.Bind("Editor - Animations", "File Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownPosClose = Config.Bind("Editor - Animations", "File Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownPosOpenDuration = Config.Bind("Editor - Animations", "File Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            FileDropdownPosCloseDuration = Config.Bind("Editor - Animations", "File Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            FileDropdownPosXOpenEase = Config.Bind("Editor - Animations", "File Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosXCloseEase = Config.Bind("Editor - Animations", "File Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosYOpenEase = Config.Bind("Editor - Animations", "File Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosYCloseEase = Config.Bind("Editor - Animations", "File Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            FileDropdownScaActive = Config.Bind("Editor - Animations", "File Dropdown Animate Scale", true, "If scale should be animated.");
            FileDropdownScaOpen = Config.Bind("Editor - Animations", "File Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownScaClose = Config.Bind("Editor - Animations", "File Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownScaOpenDuration = Config.Bind("Editor - Animations", "File Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FileDropdownScaCloseDuration = Config.Bind("Editor - Animations", "File Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FileDropdownScaXOpenEase = Config.Bind("Editor - Animations", "File Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            FileDropdownScaXCloseEase = Config.Bind("Editor - Animations", "File Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            FileDropdownScaYOpenEase = Config.Bind("Editor - Animations", "File Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            FileDropdownScaYCloseEase = Config.Bind("Editor - Animations", "File Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            FileDropdownRotActive = Config.Bind("Editor - Animations", "File Dropdown Animate Rotation", false, "If rotation should be animated.");
            FileDropdownRotOpen = Config.Bind("Editor - Animations", "File Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownRotClose = Config.Bind("Editor - Animations", "File Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownRotOpenDuration = Config.Bind("Editor - Animations", "File Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            FileDropdownRotCloseDuration = Config.Bind("Editor - Animations", "File Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            FileDropdownRotOpenEase = Config.Bind("Editor - Animations", "File Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            FileDropdownRotCloseEase = Config.Bind("Editor - Animations", "File Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Edit Dropdown

            EditDropdownActive = Config.Bind("Editor - Animations", "Edit Dropdown Active", true, "If the popup animation should play.");

            EditDropdownPosActive = Config.Bind("Editor - Animations", "Edit Dropdown Animate Position", false, "If position should be animated.");
            EditDropdownPosOpen = Config.Bind("Editor - Animations", "Edit Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownPosClose = Config.Bind("Editor - Animations", "Edit Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownPosOpenDuration = Config.Bind("Editor - Animations", "Edit Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            EditDropdownPosCloseDuration = Config.Bind("Editor - Animations", "Edit Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            EditDropdownPosXOpenEase = Config.Bind("Editor - Animations", "Edit Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosXCloseEase = Config.Bind("Editor - Animations", "Edit Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosYOpenEase = Config.Bind("Editor - Animations", "Edit Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosYCloseEase = Config.Bind("Editor - Animations", "Edit Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            EditDropdownScaActive = Config.Bind("Editor - Animations", "Edit Dropdown Animate Scale", true, "If scale should be animated.");
            EditDropdownScaOpen = Config.Bind("Editor - Animations", "Edit Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownScaClose = Config.Bind("Editor - Animations", "Edit Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownScaOpenDuration = Config.Bind("Editor - Animations", "Edit Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            EditDropdownScaCloseDuration = Config.Bind("Editor - Animations", "Edit Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            EditDropdownScaXOpenEase = Config.Bind("Editor - Animations", "Edit Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            EditDropdownScaXCloseEase = Config.Bind("Editor - Animations", "Edit Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            EditDropdownScaYOpenEase = Config.Bind("Editor - Animations", "Edit Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            EditDropdownScaYCloseEase = Config.Bind("Editor - Animations", "Edit Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            EditDropdownRotActive = Config.Bind("Editor - Animations", "Edit Dropdown Animate Rotation", false, "If rotation should be animated.");
            EditDropdownRotOpen = Config.Bind("Editor - Animations", "Edit Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownRotClose = Config.Bind("Editor - Animations", "Edit Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownRotOpenDuration = Config.Bind("Editor - Animations", "Edit Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            EditDropdownRotCloseDuration = Config.Bind("Editor - Animations", "Edit Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            EditDropdownRotOpenEase = Config.Bind("Editor - Animations", "Edit Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            EditDropdownRotCloseEase = Config.Bind("Editor - Animations", "Edit Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region View Dropdown

            ViewDropdownActive = Config.Bind("Editor - Animations", "View Dropdown Active", true, "If the popup animation should play.");

            ViewDropdownPosActive = Config.Bind("Editor - Animations", "View Dropdown Animate Position", false, "If position should be animated.");
            ViewDropdownPosOpen = Config.Bind("Editor - Animations", "View Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownPosClose = Config.Bind("Editor - Animations", "View Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownPosOpenDuration = Config.Bind("Editor - Animations", "View Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            ViewDropdownPosCloseDuration = Config.Bind("Editor - Animations", "View Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            ViewDropdownPosXOpenEase = Config.Bind("Editor - Animations", "View Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosXCloseEase = Config.Bind("Editor - Animations", "View Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosYOpenEase = Config.Bind("Editor - Animations", "View Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosYCloseEase = Config.Bind("Editor - Animations", "View Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ViewDropdownScaActive = Config.Bind("Editor - Animations", "View Dropdown Animate Scale", true, "If scale should be animated.");
            ViewDropdownScaOpen = Config.Bind("Editor - Animations", "View Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownScaClose = Config.Bind("Editor - Animations", "View Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownScaOpenDuration = Config.Bind("Editor - Animations", "View Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ViewDropdownScaCloseDuration = Config.Bind("Editor - Animations", "View Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ViewDropdownScaXOpenEase = Config.Bind("Editor - Animations", "View Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ViewDropdownScaXCloseEase = Config.Bind("Editor - Animations", "View Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ViewDropdownScaYOpenEase = Config.Bind("Editor - Animations", "View Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ViewDropdownScaYCloseEase = Config.Bind("Editor - Animations", "View Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ViewDropdownRotActive = Config.Bind("Editor - Animations", "View Dropdown Animate Rotation", false, "If rotation should be animated.");
            ViewDropdownRotOpen = Config.Bind("Editor - Animations", "View Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownRotClose = Config.Bind("Editor - Animations", "View Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownRotOpenDuration = Config.Bind("Editor - Animations", "View Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            ViewDropdownRotCloseDuration = Config.Bind("Editor - Animations", "View Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            ViewDropdownRotOpenEase = Config.Bind("Editor - Animations", "View Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownRotCloseEase = Config.Bind("Editor - Animations", "View Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Steam Dropdown

            SteamDropdownActive = Config.Bind("Editor - Animations", "Steam Dropdown Active", true, "If the popup animation should play.");

            SteamDropdownPosActive = Config.Bind("Editor - Animations", "Steam Dropdown Animate Position", false, "If position should be animated.");
            SteamDropdownPosOpen = Config.Bind("Editor - Animations", "Steam Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownPosClose = Config.Bind("Editor - Animations", "Steam Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownPosOpenDuration = Config.Bind("Editor - Animations", "Steam Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            SteamDropdownPosCloseDuration = Config.Bind("Editor - Animations", "Steam Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            SteamDropdownPosXOpenEase = Config.Bind("Editor - Animations", "Steam Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosXCloseEase = Config.Bind("Editor - Animations", "Steam Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosYOpenEase = Config.Bind("Editor - Animations", "Steam Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosYCloseEase = Config.Bind("Editor - Animations", "Steam Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            SteamDropdownScaActive = Config.Bind("Editor - Animations", "Steam Dropdown Animate Scale", true, "If scale should be animated.");
            SteamDropdownScaOpen = Config.Bind("Editor - Animations", "Steam Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownScaClose = Config.Bind("Editor - Animations", "Steam Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownScaOpenDuration = Config.Bind("Editor - Animations", "Steam Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SteamDropdownScaCloseDuration = Config.Bind("Editor - Animations", "Steam Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SteamDropdownScaXOpenEase = Config.Bind("Editor - Animations", "Steam Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            SteamDropdownScaXCloseEase = Config.Bind("Editor - Animations", "Steam Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            SteamDropdownScaYOpenEase = Config.Bind("Editor - Animations", "Steam Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            SteamDropdownScaYCloseEase = Config.Bind("Editor - Animations", "Steam Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            SteamDropdownRotActive = Config.Bind("Editor - Animations", "Steam Dropdown Animate Rotation", false, "If rotation should be animated.");
            SteamDropdownRotOpen = Config.Bind("Editor - Animations", "Steam Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownRotClose = Config.Bind("Editor - Animations", "Steam Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownRotOpenDuration = Config.Bind("Editor - Animations", "Steam Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            SteamDropdownRotCloseDuration = Config.Bind("Editor - Animations", "Steam Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            SteamDropdownRotOpenEase = Config.Bind("Editor - Animations", "Steam Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownRotCloseEase = Config.Bind("Editor - Animations", "Steam Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Help Dropdown

            HelpDropdownActive = Config.Bind("Editor - Animations", "Help Dropdown Active", true, "If the popup animation should play.");

            HelpDropdownPosActive = Config.Bind("Editor - Animations", "Help Dropdown Animate Position", false, "If position should be animated.");
            HelpDropdownPosOpen = Config.Bind("Editor - Animations", "Help Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownPosClose = Config.Bind("Editor - Animations", "Help Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownPosOpenDuration = Config.Bind("Editor - Animations", "Help Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            HelpDropdownPosCloseDuration = Config.Bind("Editor - Animations", "Help Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            HelpDropdownPosXOpenEase = Config.Bind("Editor - Animations", "Help Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosXCloseEase = Config.Bind("Editor - Animations", "Help Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosYOpenEase = Config.Bind("Editor - Animations", "Help Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosYCloseEase = Config.Bind("Editor - Animations", "Help Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            HelpDropdownScaActive = Config.Bind("Editor - Animations", "Help Dropdown Animate Scale", true, "If scale should be animated.");
            HelpDropdownScaOpen = Config.Bind("Editor - Animations", "Help Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownScaClose = Config.Bind("Editor - Animations", "Help Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownScaOpenDuration = Config.Bind("Editor - Animations", "Help Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            HelpDropdownScaCloseDuration = Config.Bind("Editor - Animations", "Help Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            HelpDropdownScaXOpenEase = Config.Bind("Editor - Animations", "Help Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            HelpDropdownScaXCloseEase = Config.Bind("Editor - Animations", "Help Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            HelpDropdownScaYOpenEase = Config.Bind("Editor - Animations", "Help Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            HelpDropdownScaYCloseEase = Config.Bind("Editor - Animations", "Help Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            HelpDropdownRotActive = Config.Bind("Editor - Animations", "Help Dropdown Animate Rotation", false, "If rotation should be animated.");
            HelpDropdownRotOpen = Config.Bind("Editor - Animations", "Help Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownRotClose = Config.Bind("Editor - Animations", "Help Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownRotOpenDuration = Config.Bind("Editor - Animations", "Help Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            HelpDropdownRotCloseDuration = Config.Bind("Editor - Animations", "Help Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            HelpDropdownRotOpenEase = Config.Bind("Editor - Animations", "Help Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownRotCloseEase = Config.Bind("Editor - Animations", "Help Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #endregion

            #region Preview

            OnlyObjectsOnCurrentLayerVisible = Config.Bind("Editor - Preview", "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent");
            VisibleObjectOpacity = Config.Bind("Editor - Preview", "Visible Object Opacity", 0.2f, "Opacity of the objects not on the current layer.");
            ShowEmpties = Config.Bind("Editor - Preview", "Show Empties", false, "If enabled, show all objects that are set to the empty object type.");
            OnlyShowDamagable = Config.Bind("Editor - Preview", "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
            HighlightObjects = Config.Bind("Editor - Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
            ObjectHighlightAmount = Config.Bind("Editor - Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
            ObjectHighlightDoubleAmount = Config.Bind("Editor - Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.");
            ObjectDraggerEnabled = Config.Bind("Editor - Preview", "Object Dragger Enabled", false, "If an object can be dragged around.");
            ObjectDraggerCreatesKeyframe = Config.Bind("Editor - Preview", "Object Dragger Creates Keyframe", false, "When an object is dragged, create a keyframe.");
            ObjectDraggerRotatorRadius = Config.Bind("Editor - Preview", "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.");
            ObjectDraggerScalerOffset = Config.Bind("Editor - Preview", "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.");
            ObjectDraggerScalerScale = Config.Bind("Editor - Preview", "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.");

            #endregion

            SelectGUI.DragGUI = DragUI.Value;
            ObjectEditor.RenderPrefabTypeIcon = TimelineObjectPrefabTypeIcon.Value;
            ObjectEditor.TimelineObjectHoverSize = TimelineObjectHoverSize.Value;
            ObjectEditor.HideVisualElementsWhenObjectIsEmpty = HideVisualElementsWhenObjectIsEmpty.Value;
            KeybindManager.AllowKeys = AllowEditorKeybindsWithEditorCam.Value;
            PrefabEditorManager.ImportPrefabsDirectly = ImportPrefabsDirectly.Value;
            ThemeEditorManager.themesPerPage = ThemesPerPage.Value;
            ThemeEditorManager.eventThemesPerPage = ThemesEventKeyframePerPage.Value;
            RTEditor.DraggingPlaysSound = DraggingPlaysSound.Value;
            RTEditor.DraggingPlaysSoundBPM = DraggingPlaysSoundOnlyWithBPM.Value;
            RTEditor.ShowModdedUI = ShowModdedFeaturesInEditor.Value;
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;

            SetPreviewConfig();
            SetupSettingChanged();
        }

        #region General

        public ConfigEntry<bool> Debug { get; set; }
        public ConfigEntry<bool> EditorZenMode { get; set; }
        public ConfigEntry<bool> ResetHealthInEditor { get; set; }
        public ConfigEntry<bool> BPMSnapsKeyframes { get; set; }
        public ConfigEntry<float> BPMSnapDivisions { get; set; }
        public ConfigEntry<bool> DraggingPlaysSound { get; set; }
        public ConfigEntry<bool> DraggingPlaysSoundOnlyWithBPM { get; set; }
        public ConfigEntry<bool> ShowCollapsePrefabWarning { get; set; }
        public ConfigEntry<bool> RoundToNearest { get; set; }
        public ConfigEntry<bool> ScrollOnEasing { get; set; }
        public ConfigEntry<bool> PrefabExampleTemplate { get; set; }
        public ConfigEntry<bool> PasteOffset { get; set; }
        public ConfigEntry<bool> BringToSelection { get; set; }
        public ConfigEntry<bool> SelectPasted { get; set; }
        public ConfigEntry<bool> CreateObjectsatCameraCenter { get; set; }
        public ConfigEntry<bool> CreateObjectsScaleParentDefault { get; set; }
        public ConfigEntry<bool> AllowEditorKeybindsWithEditorCam { get; set; }
        public ConfigEntry<bool> RotationEventKeyframeResets { get; set; }
        public ConfigEntry<bool> RememberLastKeyframeType { get; set; }

        #endregion

        #region Timeline

        public ConfigEntry<bool> DraggingMainCursorPausesLevel { get; set; }
        public ConfigEntry<bool> DraggingMainCursorFix { get; set; }
        public ConfigEntry<Color> TimelineCursorColor { get; set; }
        public ConfigEntry<Color> KeyframeCursorColor { get; set; }
        public ConfigEntry<Color> ObjectSelectionColor { get; set; }
        public ConfigEntry<Vector2> MainZoomBounds { get; set; }
        public ConfigEntry<Vector2> KeyframeZoomBounds { get; set; }
        public ConfigEntry<float> MainZoomAmount { get; set; }
        public ConfigEntry<float> KeyframeZoomAmount { get; set; }
        public ConfigEntry<float> KeyframeEndLengthOffset { get; set; }
        public ConfigEntry<bool> TimelineObjectPrefabTypeIcon { get; set; }
        public ConfigEntry<bool> EventLabelsRenderLeft { get; set; }
        public ConfigEntry<bool> EventKeyframesRenderBinColor { get; set; }
        public ConfigEntry<bool> ObjectKeyframesRenderBinColor { get; set; }
        public ConfigEntry<bool> WaveformGenerate { get; set; }
        public ConfigEntry<bool> WaveformSaves { get; set; }
        public ConfigEntry<bool> WaveformRerender { get; set; }
        public ConfigEntry<WaveformType> WaveformMode { get; set; }
        public ConfigEntry<Color> WaveformBGColor { get; set; }
        public ConfigEntry<Color> WaveformTopColor { get; set; }
        public ConfigEntry<Color> WaveformBottomColor { get; set; }
        public ConfigEntry<TextureFormat> WaveformTextureFormat { get; set; }
        public ConfigEntry<bool> TimelineGridEnabled { get; set; }
        public ConfigEntry<Color> TimelineGridColor { get; set; }
        public ConfigEntry<float> TimelineGridThickness { get; set; }
        public ConfigEntry<Color> MarkerLineColor { get; set; }
        public ConfigEntry<float> MarkerLineWidth { get; set; }
        public ConfigEntry<float> MarkerTextWidth { get; set; }
        public ConfigEntry<bool> MarkerLoopActive { get; set; }
        public ConfigEntry<int> MarkerLoopBegin { get; set; }
        public ConfigEntry<int> MarkerLoopEnd { get; set; }
        public ConfigEntry<int> MarkerDefaultColor { get; set; }

        #endregion

        #region Data

        public ConfigEntry<int> AutosaveLimit { get; set; }
        public ConfigEntry<float> AutosaveLoopTime { get; set; }
        public ConfigEntry<bool> LevelLoadsLastTime { get; set; }
        public ConfigEntry<bool> LevelPausesOnStart { get; set; }
        public ConfigEntry<bool> BackupPreviousLoadedLevel { get; set; }
        public ConfigEntry<bool> SettingPathReloads { get; set; }
        public ConfigEntry<bool> SavingSavesThemeOpacity { get; set; }
        public ConfigEntry<bool> UpdatePrefabListOnFilesChanged { get; set; }
        public ConfigEntry<bool> UpdateThemeListOnFilesChanged { get; set; }
        public ConfigEntry<bool> ShowLevelsWithoutCoverNotification { get; set; }
        public ConfigEntry<string> ZIPLevelExportPath { get; set; }
        public ConfigEntry<string> ConvertLevelLSToVGExportPath { get; set; }
        public ConfigEntry<string> ConvertPrefabLSToVGExportPath { get; set; }
        public ConfigEntry<string> ConvertThemeLSToVGExportPath { get; set; }
        public ConfigEntry<bool> ThemeSavesIndents { get; set; }

        #endregion

        #region Editor GUI

        public ConfigEntry<bool> DragUI { get; set; }
        public ConfigEntry<EditorTheme> EditorTheme { get; set; }
        public ConfigEntry<EditorFont> EditorFont { get; set; }
        public ConfigEntry<bool> RoundedUI { get; set; }
        public ConfigEntry<bool> ShowModdedFeaturesInEditor { get; set; }
        public ConfigEntry<bool> HoverUIPlaySound { get; set; }
        public ConfigEntry<bool> ImportPrefabsDirectly { get; set; }
        public ConfigEntry<int> ThemesPerPage { get; set; }
        public ConfigEntry<int> ThemesEventKeyframePerPage { get; set; }
        public ConfigEntry<bool> MouseTooltipDisplay { get; set; }
        public ConfigEntry<float> NotificationWidth { get; set; }
        public ConfigEntry<float> NotificationSize { get; set; }
        public ConfigEntry<Direction> NotificationDirection { get; set; }
        public ConfigEntry<bool> NotificationsDisplay { get; set; }
        public ConfigEntry<bool> AdjustPositionInputs { get; set; }
        public ConfigEntry<bool> ShowDropdownOnHover { get; set; }
        public ConfigEntry<bool> HideVisualElementsWhenObjectIsEmpty { get; set; }
        public ConfigEntry<Vector2> OpenLevelPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelScale { get; set; }
        public ConfigEntry<Vector2> OpenLevelEditorPathPos { get; set; }
        public ConfigEntry<float> OpenLevelEditorPathLength { get; set; }
        public ConfigEntry<Vector2> OpenLevelListRefreshPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelTogglePosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelDropdownPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> OpenLevelCellConstraintType { get; set; }
        public ConfigEntry<int> OpenLevelCellConstraintCount { get; set; }
        public ConfigEntry<Vector2> OpenLevelCellSpacing { get; set; }
        public ConfigEntry<HorizontalWrapMode> OpenLevelTextHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> OpenLevelTextVerticalWrap { get; set; }
        public ConfigEntry<int> OpenLevelTextFontSize { get; set; }

        public ConfigEntry<int> OpenLevelFolderNameMax { get; set; }
        public ConfigEntry<int> OpenLevelSongNameMax { get; set; }
        public ConfigEntry<int> OpenLevelArtistNameMax { get; set; }
        public ConfigEntry<int> OpenLevelCreatorNameMax { get; set; }
        public ConfigEntry<int> OpenLevelDescriptionMax { get; set; }
        public ConfigEntry<int> OpenLevelDateMax { get; set; }
        public ConfigEntry<string> OpenLevelTextFormatting { get; set; }

        public ConfigEntry<float> OpenLevelButtonHoverSize { get; set; }
        public ConfigEntry<Vector2> OpenLevelCoverPosition { get; set; }
        public ConfigEntry<Vector2> OpenLevelCoverScale { get; set; }

        public ConfigEntry<bool> ChangesRefreshLevelList { get; set; }
        public ConfigEntry<bool> OpenLevelShowDeleteButton { get; set; }

        public ConfigEntry<float> TimelineObjectHoverSize { get; set; }
        public ConfigEntry<float> KeyframeHoverSize { get; set; }
        public ConfigEntry<float> TimelineBarButtonsHoverSize { get; set; }
        public ConfigEntry<float> PrefabButtonHoverSize { get; set; }

        //Prefab Internal
        public ConfigEntry<Vector2> PrefabInternalPopupPos { get; set; }
        public ConfigEntry<Vector2> PrefabInternalPopupSize { get; set; }
        public ConfigEntry<bool> PrefabInternalHorizontalScroll { get; set; }
        public ConfigEntry<Vector2> PrefabInternalCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> PrefabInternalConstraintMode { get; set; }
        public ConfigEntry<int> PrefabInternalConstraint { get; set; }
        public ConfigEntry<Vector2> PrefabInternalSpacing { get; set; }
        public ConfigEntry<GridLayoutGroup.Axis> PrefabInternalStartAxis { get; set; }
        public ConfigEntry<Vector2> PrefabInternalDeleteButtonPos { get; set; }
        public ConfigEntry<Vector2> PrefabInternalDeleteButtonSca { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabInternalNameHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabInternalNameVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabInternalNameFontSize { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabInternalTypeHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabInternalTypeVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabInternalTypeFontSize { get; set; }

        //Prefab External
        public ConfigEntry<Vector2> PrefabExternalPopupPos { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPopupSize { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPrefabPathPos { get; set; }
        public ConfigEntry<float> PrefabExternalPrefabPathLength { get; set; }
        public ConfigEntry<Vector2> PrefabExternalPrefabRefreshPos { get; set; }
        public ConfigEntry<bool> PrefabExternalHorizontalScroll { get; set; }
        public ConfigEntry<Vector2> PrefabExternalCellSize { get; set; }
        public ConfigEntry<GridLayoutGroup.Constraint> PrefabExternalConstraintMode { get; set; }
        public ConfigEntry<int> PrefabExternalConstraint { get; set; }
        public ConfigEntry<Vector2> PrefabExternalSpacing { get; set; }
        public ConfigEntry<GridLayoutGroup.Axis> PrefabExternalStartAxis { get; set; }
        public ConfigEntry<Vector2> PrefabExternalDeleteButtonPos { get; set; }
        public ConfigEntry<Vector2> PrefabExternalDeleteButtonSca { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabExternalNameHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabExternalNameVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabExternalNameFontSize { get; set; }
        public ConfigEntry<HorizontalWrapMode> PrefabExternalTypeHorizontalWrap { get; set; }
        public ConfigEntry<VerticalWrapMode> PrefabExternalTypeVerticalWrap { get; set; }
        public ConfigEntry<int> PrefabExternalTypeFontSize { get; set; }

        #endregion

        #region Fields

        public ConfigEntry<KeyCode> ScrollwheelLargeAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelSmallAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelRegularAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2LargeAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2SmallAmountKey { get; set; }
        public ConfigEntry<KeyCode> ScrollwheelVector2RegularAmountKey { get; set; }
        public ConfigEntry<bool> ShowModifiedColors { get; set; }
        public ConfigEntry<string> ThemeTemplateName { get; set; }
        public ConfigEntry<Color> ThemeTemplateGUI { get; set; }
        public ConfigEntry<Color> ThemeTemplateTail { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer1 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer2 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer3 { get; set; }
        public ConfigEntry<Color> ThemeTemplatePlayer4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ10 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ11 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ12 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ13 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ14 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ15 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ16 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ17 { get; set; }
        public ConfigEntry<Color> ThemeTemplateOBJ18 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateBG9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX1 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX2 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX3 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX4 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX5 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX6 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX7 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX8 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX9 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX10 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX11 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX12 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX13 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX14 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX15 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX16 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX17 { get; set; }
        public ConfigEntry<Color> ThemeTemplateFX18 { get; set; }

        #endregion

        #region Animations

        public ConfigEntry<bool> PlayEditorAnimations { get; set; }

        #region Open File Popup

        public ConfigEntry<bool> OpenFilePopupActive { get; set; }

        public ConfigEntry<bool> OpenFilePopupPosActive { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupPosOpen { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupPosClose { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> OpenFilePopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> OpenFilePopupScaActive { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupScaOpen { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupScaClose { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> OpenFilePopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> OpenFilePopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> OpenFilePopupRotActive { get; set; }
        public ConfigEntry<float> OpenFilePopupRotOpen { get; set; }
        public ConfigEntry<float> OpenFilePopupRotClose { get; set; }
        public ConfigEntry<float> OpenFilePopupRotOpenDuration { get; set; }
        public ConfigEntry<float> OpenFilePopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> OpenFilePopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> OpenFilePopupRotCloseEase { get; set; }

        #endregion

        #region New File Popup

        public ConfigEntry<bool> NewFilePopupActive { get; set; }

        public ConfigEntry<bool> NewFilePopupPosActive { get; set; }
        public ConfigEntry<Vector2> NewFilePopupPosOpen { get; set; }
        public ConfigEntry<Vector2> NewFilePopupPosClose { get; set; }
        public ConfigEntry<Vector2> NewFilePopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> NewFilePopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> NewFilePopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> NewFilePopupScaActive { get; set; }
        public ConfigEntry<Vector2> NewFilePopupScaOpen { get; set; }
        public ConfigEntry<Vector2> NewFilePopupScaClose { get; set; }
        public ConfigEntry<Vector2> NewFilePopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> NewFilePopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> NewFilePopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> NewFilePopupRotActive { get; set; }
        public ConfigEntry<float> NewFilePopupRotOpen { get; set; }
        public ConfigEntry<float> NewFilePopupRotClose { get; set; }
        public ConfigEntry<float> NewFilePopupRotOpenDuration { get; set; }
        public ConfigEntry<float> NewFilePopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> NewFilePopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> NewFilePopupRotCloseEase { get; set; }

        #endregion

        #region Save As Popup

        public ConfigEntry<bool> SaveAsPopupActive { get; set; }

        public ConfigEntry<bool> SaveAsPopupPosActive { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupPosClose { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> SaveAsPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> SaveAsPopupScaActive { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupScaClose { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> SaveAsPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> SaveAsPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> SaveAsPopupRotActive { get; set; }
        public ConfigEntry<float> SaveAsPopupRotOpen { get; set; }
        public ConfigEntry<float> SaveAsPopupRotClose { get; set; }
        public ConfigEntry<float> SaveAsPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> SaveAsPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> SaveAsPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> SaveAsPopupRotCloseEase { get; set; }

        #endregion

        #region Quick Actions Popup

        public ConfigEntry<bool> QuickActionsPopupActive { get; set; }

        public ConfigEntry<bool> QuickActionsPopupPosActive { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupPosClose { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> QuickActionsPopupScaActive { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupScaClose { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> QuickActionsPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> QuickActionsPopupRotActive { get; set; }
        public ConfigEntry<float> QuickActionsPopupRotOpen { get; set; }
        public ConfigEntry<float> QuickActionsPopupRotClose { get; set; }
        public ConfigEntry<float> QuickActionsPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> QuickActionsPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> QuickActionsPopupRotCloseEase { get; set; }

        #endregion

        #region Parent Selector Popup

        public ConfigEntry<bool> ParentSelectorPopupActive { get; set; }

        public ConfigEntry<bool> ParentSelectorPopupPosActive { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupPosClose { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> ParentSelectorPopupScaActive { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupScaClose { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> ParentSelectorPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> ParentSelectorPopupRotActive { get; set; }
        public ConfigEntry<float> ParentSelectorPopupRotOpen { get; set; }
        public ConfigEntry<float> ParentSelectorPopupRotClose { get; set; }
        public ConfigEntry<float> ParentSelectorPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> ParentSelectorPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> ParentSelectorPopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Popup

        public ConfigEntry<bool> PrefabPopupActive { get; set; }

        public ConfigEntry<bool> PrefabPopupPosActive { get; set; }
        public ConfigEntry<Vector2> PrefabPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> PrefabPopupPosClose { get; set; }
        public ConfigEntry<Vector2> PrefabPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> PrefabPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> PrefabPopupScaActive { get; set; }
        public ConfigEntry<Vector2> PrefabPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> PrefabPopupScaClose { get; set; }
        public ConfigEntry<Vector2> PrefabPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> PrefabPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> PrefabPopupRotActive { get; set; }
        public ConfigEntry<float> PrefabPopupRotOpen { get; set; }
        public ConfigEntry<float> PrefabPopupRotClose { get; set; }
        public ConfigEntry<float> PrefabPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> PrefabPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabPopupRotCloseEase { get; set; }

        #endregion

        #region Object Options Popup

        public ConfigEntry<bool> ObjectOptionsPopupActive { get; set; }

        public ConfigEntry<bool> ObjectOptionsPopupPosActive { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupPosClose { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> ObjectOptionsPopupScaActive { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupScaClose { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> ObjectOptionsPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> ObjectOptionsPopupRotActive { get; set; }
        public ConfigEntry<float> ObjectOptionsPopupRotOpen { get; set; }
        public ConfigEntry<float> ObjectOptionsPopupRotClose { get; set; }
        public ConfigEntry<float> ObjectOptionsPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> ObjectOptionsPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region BG Options Popup

        public ConfigEntry<bool> BGOptionsPopupActive { get; set; }

        public ConfigEntry<bool> BGOptionsPopupPosActive { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupPosClose { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> BGOptionsPopupScaActive { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupScaClose { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> BGOptionsPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> BGOptionsPopupRotActive { get; set; }
        public ConfigEntry<float> BGOptionsPopupRotOpen { get; set; }
        public ConfigEntry<float> BGOptionsPopupRotClose { get; set; }
        public ConfigEntry<float> BGOptionsPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> BGOptionsPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> BGOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region Browser Popup

        public ConfigEntry<bool> BrowserPopupActive { get; set; }

        public ConfigEntry<bool> BrowserPopupPosActive { get; set; }
        public ConfigEntry<Vector2> BrowserPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> BrowserPopupPosClose { get; set; }
        public ConfigEntry<Vector2> BrowserPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> BrowserPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> BrowserPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> BrowserPopupScaActive { get; set; }
        public ConfigEntry<Vector2> BrowserPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> BrowserPopupScaClose { get; set; }
        public ConfigEntry<Vector2> BrowserPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> BrowserPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> BrowserPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> BrowserPopupRotActive { get; set; }
        public ConfigEntry<float> BrowserPopupRotOpen { get; set; }
        public ConfigEntry<float> BrowserPopupRotClose { get; set; }
        public ConfigEntry<float> BrowserPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> BrowserPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> BrowserPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> BrowserPopupRotCloseEase { get; set; }

        #endregion

        #region Object Search Popup

        public ConfigEntry<bool> ObjectSearchPopupActive { get; set; }

        public ConfigEntry<bool> ObjectSearchPopupPosActive { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupPosClose { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> ObjectSearchPopupScaActive { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupScaClose { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> ObjectSearchPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> ObjectSearchPopupRotActive { get; set; }
        public ConfigEntry<float> ObjectSearchPopupRotOpen { get; set; }
        public ConfigEntry<float> ObjectSearchPopupRotClose { get; set; }
        public ConfigEntry<float> ObjectSearchPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> ObjectSearchPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> ObjectSearchPopupRotCloseEase { get; set; }

        #endregion

        #region Warning Popup

        public ConfigEntry<bool> WarningPopupActive { get; set; }

        public ConfigEntry<bool> WarningPopupPosActive { get; set; }
        public ConfigEntry<Vector2> WarningPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> WarningPopupPosClose { get; set; }
        public ConfigEntry<Vector2> WarningPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> WarningPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> WarningPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> WarningPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> WarningPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> WarningPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> WarningPopupScaActive { get; set; }
        public ConfigEntry<Vector2> WarningPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> WarningPopupScaClose { get; set; }
        public ConfigEntry<Vector2> WarningPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> WarningPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> WarningPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> WarningPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> WarningPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> WarningPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> WarningPopupRotActive { get; set; }
        public ConfigEntry<float> WarningPopupRotOpen { get; set; }
        public ConfigEntry<float> WarningPopupRotClose { get; set; }
        public ConfigEntry<float> WarningPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> WarningPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> WarningPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> WarningPopupRotCloseEase { get; set; }

        #endregion

        #region REPL Editor Popup

        public ConfigEntry<bool> REPLEditorPopupActive { get; set; }

        public ConfigEntry<bool> REPLEditorPopupPosActive { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupPosClose { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> REPLEditorPopupScaActive { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupScaClose { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> REPLEditorPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> REPLEditorPopupRotActive { get; set; }
        public ConfigEntry<float> REPLEditorPopupRotOpen { get; set; }
        public ConfigEntry<float> REPLEditorPopupRotClose { get; set; }
        public ConfigEntry<float> REPLEditorPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> REPLEditorPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> REPLEditorPopupRotCloseEase { get; set; }

        #endregion

        #region Editor Properties Popup

        public ConfigEntry<bool> EditorPropertiesPopupActive { get; set; }

        public ConfigEntry<bool> EditorPropertiesPopupPosActive { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupPosClose { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> EditorPropertiesPopupScaActive { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupScaClose { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> EditorPropertiesPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> EditorPropertiesPopupRotActive { get; set; }
        public ConfigEntry<float> EditorPropertiesPopupRotOpen { get; set; }
        public ConfigEntry<float> EditorPropertiesPopupRotClose { get; set; }
        public ConfigEntry<float> EditorPropertiesPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> EditorPropertiesPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> EditorPropertiesPopupRotCloseEase { get; set; }

        #endregion

        #region Documentation Popup

        public ConfigEntry<bool> DocumentationPopupActive { get; set; }

        public ConfigEntry<bool> DocumentationPopupPosActive { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupPosClose { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> DocumentationPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> DocumentationPopupScaActive { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupScaClose { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> DocumentationPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> DocumentationPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> DocumentationPopupRotActive { get; set; }
        public ConfigEntry<float> DocumentationPopupRotOpen { get; set; }
        public ConfigEntry<float> DocumentationPopupRotClose { get; set; }
        public ConfigEntry<float> DocumentationPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> DocumentationPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> DocumentationPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> DocumentationPopupRotCloseEase { get; set; }

        #endregion

        #region Debugger Popup

        public ConfigEntry<bool> DebuggerPopupActive { get; set; }

        public ConfigEntry<bool> DebuggerPopupPosActive { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupPosClose { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> DebuggerPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> DebuggerPopupScaActive { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupScaClose { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> DebuggerPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> DebuggerPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> DebuggerPopupRotActive { get; set; }
        public ConfigEntry<float> DebuggerPopupRotOpen { get; set; }
        public ConfigEntry<float> DebuggerPopupRotClose { get; set; }
        public ConfigEntry<float> DebuggerPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> DebuggerPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> DebuggerPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> DebuggerPopupRotCloseEase { get; set; }

        #endregion

        #region Autosaves Popup

        public ConfigEntry<bool> AutosavesPopupActive { get; set; }

        public ConfigEntry<bool> AutosavesPopupPosActive { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupPosClose { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> AutosavesPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> AutosavesPopupScaActive { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupScaClose { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> AutosavesPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> AutosavesPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> AutosavesPopupRotActive { get; set; }
        public ConfigEntry<float> AutosavesPopupRotOpen { get; set; }
        public ConfigEntry<float> AutosavesPopupRotClose { get; set; }
        public ConfigEntry<float> AutosavesPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> AutosavesPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> AutosavesPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> AutosavesPopupRotCloseEase { get; set; }

        #endregion

        #region Default Modifiers Popup

        public ConfigEntry<bool> DefaultModifiersPopupActive { get; set; }

        public ConfigEntry<bool> DefaultModifiersPopupPosActive { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupPosClose { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> DefaultModifiersPopupScaActive { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupScaClose { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> DefaultModifiersPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> DefaultModifiersPopupRotActive { get; set; }
        public ConfigEntry<float> DefaultModifiersPopupRotOpen { get; set; }
        public ConfigEntry<float> DefaultModifiersPopupRotClose { get; set; }
        public ConfigEntry<float> DefaultModifiersPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> DefaultModifiersPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> DefaultModifiersPopupRotCloseEase { get; set; }

        #endregion

        #region Keybind List Popup

        public ConfigEntry<bool> KeybindListPopupActive { get; set; }

        public ConfigEntry<bool> KeybindListPopupPosActive { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupPosClose { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> KeybindListPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> KeybindListPopupScaActive { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupScaClose { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> KeybindListPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> KeybindListPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> KeybindListPopupRotActive { get; set; }
        public ConfigEntry<float> KeybindListPopupRotOpen { get; set; }
        public ConfigEntry<float> KeybindListPopupRotClose { get; set; }
        public ConfigEntry<float> KeybindListPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> KeybindListPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> KeybindListPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> KeybindListPopupRotCloseEase { get; set; }

        #endregion

        #region Theme Popup

        public ConfigEntry<bool> ThemePopupActive { get; set; }

        public ConfigEntry<bool> ThemePopupPosActive { get; set; }
        public ConfigEntry<Vector2> ThemePopupPosOpen { get; set; }
        public ConfigEntry<Vector2> ThemePopupPosClose { get; set; }
        public ConfigEntry<Vector2> ThemePopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> ThemePopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> ThemePopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> ThemePopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> ThemePopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> ThemePopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> ThemePopupScaActive { get; set; }
        public ConfigEntry<Vector2> ThemePopupScaOpen { get; set; }
        public ConfigEntry<Vector2> ThemePopupScaClose { get; set; }
        public ConfigEntry<Vector2> ThemePopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> ThemePopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> ThemePopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> ThemePopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> ThemePopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> ThemePopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> ThemePopupRotActive { get; set; }
        public ConfigEntry<float> ThemePopupRotOpen { get; set; }
        public ConfigEntry<float> ThemePopupRotClose { get; set; }
        public ConfigEntry<float> ThemePopupRotOpenDuration { get; set; }
        public ConfigEntry<float> ThemePopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> ThemePopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> ThemePopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Types Popup

        public ConfigEntry<bool> PrefabTypesPopupActive { get; set; }

        public ConfigEntry<bool> PrefabTypesPopupPosActive { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupPosOpen { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupPosClose { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupPosCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupPosXOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupPosXCloseEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupPosYOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupPosYCloseEase { get; set; }

        public ConfigEntry<bool> PrefabTypesPopupScaActive { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupScaOpen { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupScaClose { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> PrefabTypesPopupScaCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupScaXOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupScaXCloseEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupScaYOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupScaYCloseEase { get; set; }

        public ConfigEntry<bool> PrefabTypesPopupRotActive { get; set; }
        public ConfigEntry<float> PrefabTypesPopupRotOpen { get; set; }
        public ConfigEntry<float> PrefabTypesPopupRotClose { get; set; }
        public ConfigEntry<float> PrefabTypesPopupRotOpenDuration { get; set; }
        public ConfigEntry<float> PrefabTypesPopupRotCloseDuration { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupRotOpenEase { get; set; }
        public ConfigEntry<Easings> PrefabTypesPopupRotCloseEase { get; set; }

        #endregion

        #region File Dropdown

        public ConfigEntry<bool> FileDropdownActive { get; set; }

        public ConfigEntry<bool> FileDropdownPosActive { get; set; }
        public ConfigEntry<Vector2> FileDropdownPosOpen { get; set; }
        public ConfigEntry<Vector2> FileDropdownPosClose { get; set; }
        public ConfigEntry<Vector2> FileDropdownPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> FileDropdownPosCloseDuration { get; set; }
        public ConfigEntry<Easings> FileDropdownPosXOpenEase { get; set; }
        public ConfigEntry<Easings> FileDropdownPosXCloseEase { get; set; }
        public ConfigEntry<Easings> FileDropdownPosYOpenEase { get; set; }
        public ConfigEntry<Easings> FileDropdownPosYCloseEase { get; set; }

        public ConfigEntry<bool> FileDropdownScaActive { get; set; }
        public ConfigEntry<Vector2> FileDropdownScaOpen { get; set; }
        public ConfigEntry<Vector2> FileDropdownScaClose { get; set; }
        public ConfigEntry<Vector2> FileDropdownScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> FileDropdownScaCloseDuration { get; set; }
        public ConfigEntry<Easings> FileDropdownScaXOpenEase { get; set; }
        public ConfigEntry<Easings> FileDropdownScaXCloseEase { get; set; }
        public ConfigEntry<Easings> FileDropdownScaYOpenEase { get; set; }
        public ConfigEntry<Easings> FileDropdownScaYCloseEase { get; set; }

        public ConfigEntry<bool> FileDropdownRotActive { get; set; }
        public ConfigEntry<float> FileDropdownRotOpen { get; set; }
        public ConfigEntry<float> FileDropdownRotClose { get; set; }
        public ConfigEntry<float> FileDropdownRotOpenDuration { get; set; }
        public ConfigEntry<float> FileDropdownRotCloseDuration { get; set; }
        public ConfigEntry<Easings> FileDropdownRotOpenEase { get; set; }
        public ConfigEntry<Easings> FileDropdownRotCloseEase { get; set; }

        #endregion

        #region Edit Dropdown

        public ConfigEntry<bool> EditDropdownActive { get; set; }

        public ConfigEntry<bool> EditDropdownPosActive { get; set; }
        public ConfigEntry<Vector2> EditDropdownPosOpen { get; set; }
        public ConfigEntry<Vector2> EditDropdownPosClose { get; set; }
        public ConfigEntry<Vector2> EditDropdownPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> EditDropdownPosCloseDuration { get; set; }
        public ConfigEntry<Easings> EditDropdownPosXOpenEase { get; set; }
        public ConfigEntry<Easings> EditDropdownPosXCloseEase { get; set; }
        public ConfigEntry<Easings> EditDropdownPosYOpenEase { get; set; }
        public ConfigEntry<Easings> EditDropdownPosYCloseEase { get; set; }

        public ConfigEntry<bool> EditDropdownScaActive { get; set; }
        public ConfigEntry<Vector2> EditDropdownScaOpen { get; set; }
        public ConfigEntry<Vector2> EditDropdownScaClose { get; set; }
        public ConfigEntry<Vector2> EditDropdownScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> EditDropdownScaCloseDuration { get; set; }
        public ConfigEntry<Easings> EditDropdownScaXOpenEase { get; set; }
        public ConfigEntry<Easings> EditDropdownScaXCloseEase { get; set; }
        public ConfigEntry<Easings> EditDropdownScaYOpenEase { get; set; }
        public ConfigEntry<Easings> EditDropdownScaYCloseEase { get; set; }

        public ConfigEntry<bool> EditDropdownRotActive { get; set; }
        public ConfigEntry<float> EditDropdownRotOpen { get; set; }
        public ConfigEntry<float> EditDropdownRotClose { get; set; }
        public ConfigEntry<float> EditDropdownRotOpenDuration { get; set; }
        public ConfigEntry<float> EditDropdownRotCloseDuration { get; set; }
        public ConfigEntry<Easings> EditDropdownRotOpenEase { get; set; }
        public ConfigEntry<Easings> EditDropdownRotCloseEase { get; set; }

        #endregion

        #region View Dropdown

        public ConfigEntry<bool> ViewDropdownActive { get; set; }

        public ConfigEntry<bool> ViewDropdownPosActive { get; set; }
        public ConfigEntry<Vector2> ViewDropdownPosOpen { get; set; }
        public ConfigEntry<Vector2> ViewDropdownPosClose { get; set; }
        public ConfigEntry<Vector2> ViewDropdownPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> ViewDropdownPosCloseDuration { get; set; }
        public ConfigEntry<Easings> ViewDropdownPosXOpenEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownPosXCloseEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownPosYOpenEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownPosYCloseEase { get; set; }

        public ConfigEntry<bool> ViewDropdownScaActive { get; set; }
        public ConfigEntry<Vector2> ViewDropdownScaOpen { get; set; }
        public ConfigEntry<Vector2> ViewDropdownScaClose { get; set; }
        public ConfigEntry<Vector2> ViewDropdownScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> ViewDropdownScaCloseDuration { get; set; }
        public ConfigEntry<Easings> ViewDropdownScaXOpenEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownScaXCloseEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownScaYOpenEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownScaYCloseEase { get; set; }

        public ConfigEntry<bool> ViewDropdownRotActive { get; set; }
        public ConfigEntry<float> ViewDropdownRotOpen { get; set; }
        public ConfigEntry<float> ViewDropdownRotClose { get; set; }
        public ConfigEntry<float> ViewDropdownRotOpenDuration { get; set; }
        public ConfigEntry<float> ViewDropdownRotCloseDuration { get; set; }
        public ConfigEntry<Easings> ViewDropdownRotOpenEase { get; set; }
        public ConfigEntry<Easings> ViewDropdownRotCloseEase { get; set; }

        #endregion

        #region Steam Dropdown

        public ConfigEntry<bool> SteamDropdownActive { get; set; }

        public ConfigEntry<bool> SteamDropdownPosActive { get; set; }
        public ConfigEntry<Vector2> SteamDropdownPosOpen { get; set; }
        public ConfigEntry<Vector2> SteamDropdownPosClose { get; set; }
        public ConfigEntry<Vector2> SteamDropdownPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> SteamDropdownPosCloseDuration { get; set; }
        public ConfigEntry<Easings> SteamDropdownPosXOpenEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownPosXCloseEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownPosYOpenEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownPosYCloseEase { get; set; }

        public ConfigEntry<bool> SteamDropdownScaActive { get; set; }
        public ConfigEntry<Vector2> SteamDropdownScaOpen { get; set; }
        public ConfigEntry<Vector2> SteamDropdownScaClose { get; set; }
        public ConfigEntry<Vector2> SteamDropdownScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> SteamDropdownScaCloseDuration { get; set; }
        public ConfigEntry<Easings> SteamDropdownScaXOpenEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownScaXCloseEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownScaYOpenEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownScaYCloseEase { get; set; }

        public ConfigEntry<bool> SteamDropdownRotActive { get; set; }
        public ConfigEntry<float> SteamDropdownRotOpen { get; set; }
        public ConfigEntry<float> SteamDropdownRotClose { get; set; }
        public ConfigEntry<float> SteamDropdownRotOpenDuration { get; set; }
        public ConfigEntry<float> SteamDropdownRotCloseDuration { get; set; }
        public ConfigEntry<Easings> SteamDropdownRotOpenEase { get; set; }
        public ConfigEntry<Easings> SteamDropdownRotCloseEase { get; set; }

        #endregion

        #region Help Dropdown

        public ConfigEntry<bool> HelpDropdownActive { get; set; }

        public ConfigEntry<bool> HelpDropdownPosActive { get; set; }
        public ConfigEntry<Vector2> HelpDropdownPosOpen { get; set; }
        public ConfigEntry<Vector2> HelpDropdownPosClose { get; set; }
        public ConfigEntry<Vector2> HelpDropdownPosOpenDuration { get; set; }
        public ConfigEntry<Vector2> HelpDropdownPosCloseDuration { get; set; }
        public ConfigEntry<Easings> HelpDropdownPosXOpenEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownPosXCloseEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownPosYOpenEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownPosYCloseEase { get; set; }

        public ConfigEntry<bool> HelpDropdownScaActive { get; set; }
        public ConfigEntry<Vector2> HelpDropdownScaOpen { get; set; }
        public ConfigEntry<Vector2> HelpDropdownScaClose { get; set; }
        public ConfigEntry<Vector2> HelpDropdownScaOpenDuration { get; set; }
        public ConfigEntry<Vector2> HelpDropdownScaCloseDuration { get; set; }
        public ConfigEntry<Easings> HelpDropdownScaXOpenEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownScaXCloseEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownScaYOpenEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownScaYCloseEase { get; set; }

        public ConfigEntry<bool> HelpDropdownRotActive { get; set; }
        public ConfigEntry<float> HelpDropdownRotOpen { get; set; }
        public ConfigEntry<float> HelpDropdownRotClose { get; set; }
        public ConfigEntry<float> HelpDropdownRotOpenDuration { get; set; }
        public ConfigEntry<float> HelpDropdownRotCloseDuration { get; set; }
        public ConfigEntry<Easings> HelpDropdownRotOpenEase { get; set; }
        public ConfigEntry<Easings> HelpDropdownRotCloseEase { get; set; }

        #endregion

        #endregion

        #region Preview

        public ConfigEntry<bool> OnlyObjectsOnCurrentLayerVisible { get; set; }
        public ConfigEntry<float> VisibleObjectOpacity { get; set; }
        public ConfigEntry<bool> ShowEmpties { get; set; }
        public ConfigEntry<bool> OnlyShowDamagable { get; set; }
        public ConfigEntry<bool> HighlightObjects { get; set; }
        public ConfigEntry<Color> ObjectHighlightAmount { get; set; }
        public ConfigEntry<Color> ObjectHighlightDoubleAmount { get; set; }
        public ConfigEntry<bool> ObjectDraggerEnabled { get; set; }
        public ConfigEntry<bool> ObjectDraggerCreatesKeyframe { get; set; }
        public ConfigEntry<float> ObjectDraggerRotatorRadius { get; set; }
        public ConfigEntry<float> ObjectDraggerScalerOffset { get; set; }
        public ConfigEntry<float> ObjectDraggerScalerScale { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

            TimelineGridEnabled.SettingChanged += TimelineGridChanged;
            TimelineGridThickness.SettingChanged += TimelineGridChanged;
            TimelineGridColor.SettingChanged += TimelineGridChanged;

            BPMSnapDivisions.SettingChanged += TimelineGridChanged;
            DragUI.SettingChanged += DragUIChanged;

            HideVisualElementsWhenObjectIsEmpty.SettingChanged += ObjectEditorChanged;
            KeyframeZoomBounds.SettingChanged += ObjectEditorChanged;
            ObjectSelectionColor.SettingChanged += ObjectEditorChanged;

            ThemeTemplateName.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateGUI.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateTail.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG.SettingChanged += ThemeTemplateChanged;

            ThemeTemplatePlayer1.SettingChanged += ThemeTemplateChanged;
            ThemeTemplatePlayer2.SettingChanged += ThemeTemplateChanged;
            ThemeTemplatePlayer3.SettingChanged += ThemeTemplateChanged;
            ThemeTemplatePlayer4.SettingChanged += ThemeTemplateChanged;

            ThemeTemplateOBJ1.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ2.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ3.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ4.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ5.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ6.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ7.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ8.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ9.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ10.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ11.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ12.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ13.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ14.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ15.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ16.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ17.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateOBJ18.SettingChanged += ThemeTemplateChanged;

            ThemeTemplateBG1.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG2.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG3.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG4.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG5.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG6.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG7.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG8.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateBG9.SettingChanged += ThemeTemplateChanged;

            ThemeTemplateFX1.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX2.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX3.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX4.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX5.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX6.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX7.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX8.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX9.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX10.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX11.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX12.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX13.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX14.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX15.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX16.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX17.SettingChanged += ThemeTemplateChanged;
            ThemeTemplateFX18.SettingChanged += ThemeTemplateChanged;

            ThemesPerPage.SettingChanged += ThemePopupChanged;
            ThemesEventKeyframePerPage.SettingChanged += ThemeEventKeyframeChanged;

            WaveformRerender.SettingChanged += TimelineWaveformChanged;

            DraggingPlaysSound.SettingChanged += DraggingChanged;
            DraggingPlaysSoundOnlyWithBPM.SettingChanged += DraggingChanged;

            ShowModdedFeaturesInEditor.SettingChanged += ModdedEditorChanged;

            MarkerLineColor.SettingChanged += MarkerChanged;
            MarkerLineWidth.SettingChanged += MarkerChanged;
            MarkerTextWidth.SettingChanged += MarkerChanged;

            EditorTheme.SettingChanged += EditorThemeChanged;
            RoundedUI.SettingChanged += EditorThemeChanged;

            AutosaveLoopTime.SettingChanged += AutosaveChanged;
        }

        void ThemeEventKeyframeChanged(object sender, EventArgs e)
        {
            ThemeEditorManager.eventThemesPerPage = ThemesEventKeyframePerPage.Value;

            if (!EditorManager.inst)
                return;

            var p = Mathf.Clamp(ThemeEditorManager.inst.eventThemePage, 0, DataManager.inst.AllThemes.Count / ThemeEditorManager.eventThemesPerPage).ToString();

            if (ThemeEditorManager.eventPageStorage.inputField.text != p)
            {
                ThemeEditorManager.eventPageStorage.inputField.text = p;
                return;
            }


            var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
            if (dialogTmp.gameObject.activeInHierarchy)
                CoreHelper.StartCoroutine(ThemeEditorManager.inst.RenderThemeList(
                    dialogTmp.Find("theme-search").GetComponent<InputField>().text));
        }

        void AutosaveChanged(object sender, EventArgs e)
        {
            if (EditorManager.inst && EditorManager.inst.hasLoadedLevel)
                RTEditor.inst.SetAutoSave();
        }

        void EditorThemeChanged(object sender, EventArgs e)
        {
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;
            CoreHelper.StartCoroutine(EditorThemeManager.RenderElements());
        }

        void MarkerChanged(object sender, EventArgs e)
        {
            MarkerEditor.inst?.RenderMarkers();
        }

        void ModdedEditorChanged(object sender, EventArgs e)
        {
            RTEditor.ShowModdedUI = ShowModdedFeaturesInEditor.Value;

            AdjustPositionInputsChanged?.Invoke();

            if (ObjectEditor.inst && ObjectEditor.inst.SelectedObjectCount == 1 && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
            {
                CoreHelper.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
            }

            if (RTEditor.inst && RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderEventsDialog();
            }

            if (PrefabEditorManager.inst)
            {
                var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

                if (!prefabSelectorLeft.gameObject.activeInHierarchy)
                    PrefabEditorManager.inst.UpdateModdedVisbility();
                else if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                    PrefabEditorManager.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());
            }
        }

        void DraggingChanged(object sender, EventArgs e)
        {
            RTEditor.DraggingPlaysSound = DraggingPlaysSound.Value;
            RTEditor.DraggingPlaysSoundBPM = DraggingPlaysSoundOnlyWithBPM.Value;
        }

        void TimelineWaveformChanged(object sender, EventArgs e)
        {
            if (WaveformRerender.Value)
            {
                RTEditor.inst.StartCoroutine(RTEditor.inst.AssignTimelineTexture());
            }
        }

        void ThemePopupChanged(object sender, EventArgs e)
        {
            ThemeEditorManager.themesPerPage = ThemesPerPage.Value;
        }

        void ObjectEditorChanged(object sender, EventArgs e)
        {
            ObjectEditor.HideVisualElementsWhenObjectIsEmpty = HideVisualElementsWhenObjectIsEmpty.Value;

            if (ObjEditor.inst)
            {
                ObjEditor.inst.zoomBounds = KeyframeZoomBounds.Value;
                ObjEditor.inst.ObjectLengthOffset = KeyframeEndLengthOffset.Value;
                ObjEditor.inst.SelectedColor = ObjectSelectionColor.Value;
            }
        }

        void TimelineGridChanged(object sender, EventArgs e)
        {
            RTEditor.inst.timelineGridRenderer.enabled = false;

            RTEditor.inst.timelineGridRenderer.color = TimelineGridColor.Value;
            RTEditor.inst.timelineGridRenderer.thickness = TimelineGridThickness.Value;

            RTEditor.inst.SetTimelineGridSize();
        }

        void ThemeTemplateChanged(object sender, EventArgs e) => UpdateDefaultThemeValues();

        void DragUIChanged(object sender, EventArgs e) => SelectGUI.DragGUI = DragUI.Value;

        public static Action AdjustPositionInputsChanged { get; set; }

        void UpdateSettings(object sender, SettingChangedEventArgs e)
        {
            SetPreviewConfig();

            KeybindManager.AllowKeys = AllowEditorKeybindsWithEditorCam.Value;
            ObjectEditor.RenderPrefabTypeIcon = TimelineObjectPrefabTypeIcon.Value;
            ObjectEditor.TimelineObjectHoverSize = TimelineObjectHoverSize.Value;
            PrefabEditorManager.ImportPrefabsDirectly = ImportPrefabsDirectly.Value;

            if (!EditorManager.inst)
                return;

            SetNotificationProperties();
            EditorManager.inst.zoomBounds = MainZoomBounds.Value;

            SetTimelineColors();
            AdjustPositionInputsChanged?.Invoke();

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
            }
        }

        void SetPreviewConfig()
        {
            try
            {
                ModCompatibility.sharedFunctions.AddSet("ShowEmpties", ShowEmpties.Value);
                ModCompatibility.sharedFunctions.AddSet("ShowDamagable", OnlyShowDamagable.Value);

                RTObject.Enabled = ObjectDraggerEnabled.Value;
                RTObject.CreateKeyframe = ObjectDraggerCreatesKeyframe.Value;
                RTObject.HighlightColor = ObjectHighlightAmount.Value;
                RTObject.HighlightDoubleColor = ObjectHighlightDoubleAmount.Value;
                RTObject.HighlightObjects = HighlightObjects.Value;
                RTObject.ShowObjectsOnlyOnLayer = OnlyObjectsOnCurrentLayerVisible.Value;
                RTObject.LayerOpacity = VisibleObjectOpacity.Value;

                RTRotator.RotatorRadius = ObjectDraggerRotatorRadius.Value;
                RTScaler.ScalerOffset = ObjectDraggerScalerOffset.Value;
                RTScaler.ScalerScale = ObjectDraggerScalerScale.Value;

                RTPlayer.ZenModeInEditor = EditorZenMode.Value;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"SharedFunctions Error{ex}");
            }
        }

        void SetTimelineColors()
        {
            var timelineCursorColor = TimelineCursorColor.Value;
            var keyframeCursorColor = KeyframeCursorColor.Value;

            RTEditor.inst.timelineSliderHandle.color = timelineCursorColor;

            RTEditor.inst.timelineSliderRuler.color = timelineCursorColor;

            RTEditor.inst.keyframeTimelineSliderHandle.color = keyframeCursorColor;
            RTEditor.inst.keyframeTimelineSliderRuler.color = keyframeCursorColor;
        }

        void SetNotificationProperties()
        {
            CoreHelper.Log("Setting Notification values");
            var notifyRT = EditorManager.inst.notification.transform.AsRT();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(NotificationSize.Value, NotificationSize.Value,
                    1f);

            var direction = NotificationDirection.Value;

            notifyRT.anchoredPosition = new Vector2(8f, direction == Direction.Up ? 408f : 410f);
            notifyGroup.childAlignment = direction != Direction.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
        }

        void UpdateDefaultThemeValues()
        {
            BeatmapTheme.DefaultName = ThemeTemplateName.Value;
            BeatmapTheme.DefaultGUIColor = ThemeTemplateGUI.Value;
            BeatmapTheme.DefaultTailColor = ThemeTemplateTail.Value;
            BeatmapTheme.DefaultBGColor = ThemeTemplateBG.Value;

            try
            {
                BeatmapTheme.DefaultPlayerColors[0] = ThemeTemplatePlayer1.Value;
                BeatmapTheme.DefaultPlayerColors[1] = ThemeTemplatePlayer2.Value;
                BeatmapTheme.DefaultPlayerColors[2] = ThemeTemplatePlayer3.Value;
                BeatmapTheme.DefaultPlayerColors[3] = ThemeTemplatePlayer4.Value;

                BeatmapTheme.DefaultObjectColors[0] = ThemeTemplateOBJ1.Value;
                BeatmapTheme.DefaultObjectColors[1] = ThemeTemplateOBJ2.Value;
                BeatmapTheme.DefaultObjectColors[2] = ThemeTemplateOBJ3.Value;
                BeatmapTheme.DefaultObjectColors[3] = ThemeTemplateOBJ4.Value;
                BeatmapTheme.DefaultObjectColors[4] = ThemeTemplateOBJ5.Value;
                BeatmapTheme.DefaultObjectColors[5] = ThemeTemplateOBJ6.Value;
                BeatmapTheme.DefaultObjectColors[6] = ThemeTemplateOBJ7.Value;
                BeatmapTheme.DefaultObjectColors[7] = ThemeTemplateOBJ8.Value;
                BeatmapTheme.DefaultObjectColors[8] = ThemeTemplateOBJ9.Value;
                BeatmapTheme.DefaultObjectColors[9] = ThemeTemplateOBJ10.Value;
                BeatmapTheme.DefaultObjectColors[10] = ThemeTemplateOBJ11.Value;
                BeatmapTheme.DefaultObjectColors[11] = ThemeTemplateOBJ12.Value;
                BeatmapTheme.DefaultObjectColors[12] = ThemeTemplateOBJ13.Value;
                BeatmapTheme.DefaultObjectColors[13] = ThemeTemplateOBJ14.Value;
                BeatmapTheme.DefaultObjectColors[14] = ThemeTemplateOBJ15.Value;
                BeatmapTheme.DefaultObjectColors[15] = ThemeTemplateOBJ16.Value;
                BeatmapTheme.DefaultObjectColors[16] = ThemeTemplateOBJ17.Value;
                BeatmapTheme.DefaultObjectColors[17] = ThemeTemplateOBJ18.Value;

                BeatmapTheme.DefaulBackgroundColors[0] = ThemeTemplateBG1.Value;
                BeatmapTheme.DefaulBackgroundColors[1] = ThemeTemplateBG2.Value;
                BeatmapTheme.DefaulBackgroundColors[2] = ThemeTemplateBG3.Value;
                BeatmapTheme.DefaulBackgroundColors[3] = ThemeTemplateBG4.Value;
                BeatmapTheme.DefaulBackgroundColors[4] = ThemeTemplateBG5.Value;
                BeatmapTheme.DefaulBackgroundColors[5] = ThemeTemplateBG6.Value;
                BeatmapTheme.DefaulBackgroundColors[6] = ThemeTemplateBG7.Value;
                BeatmapTheme.DefaulBackgroundColors[7] = ThemeTemplateBG8.Value;
                BeatmapTheme.DefaulBackgroundColors[8] = ThemeTemplateBG9.Value;

                BeatmapTheme.DefaultEffectColors[0] = ThemeTemplateFX1.Value;
                BeatmapTheme.DefaultEffectColors[1] = ThemeTemplateFX2.Value;
                BeatmapTheme.DefaultEffectColors[2] = ThemeTemplateFX3.Value;
                BeatmapTheme.DefaultEffectColors[3] = ThemeTemplateFX4.Value;
                BeatmapTheme.DefaultEffectColors[4] = ThemeTemplateFX5.Value;
                BeatmapTheme.DefaultEffectColors[5] = ThemeTemplateFX6.Value;
                BeatmapTheme.DefaultEffectColors[6] = ThemeTemplateFX7.Value;
                BeatmapTheme.DefaultEffectColors[7] = ThemeTemplateFX8.Value;
                BeatmapTheme.DefaultEffectColors[8] = ThemeTemplateFX9.Value;
                BeatmapTheme.DefaultEffectColors[9] = ThemeTemplateFX10.Value;
                BeatmapTheme.DefaultEffectColors[10] = ThemeTemplateFX11.Value;
                BeatmapTheme.DefaultEffectColors[11] = ThemeTemplateFX12.Value;
                BeatmapTheme.DefaultEffectColors[12] = ThemeTemplateFX13.Value;
                BeatmapTheme.DefaultEffectColors[13] = ThemeTemplateFX14.Value;
                BeatmapTheme.DefaultEffectColors[14] = ThemeTemplateFX15.Value;
                BeatmapTheme.DefaultEffectColors[15] = ThemeTemplateFX16.Value;
                BeatmapTheme.DefaultEffectColors[16] = ThemeTemplateFX17.Value;
                BeatmapTheme.DefaultEffectColors[17] = ThemeTemplateFX18.Value;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"{nameof(UpdateDefaultThemeValues)} had an error! \n{ex}");
            }
        }

        public override string ToString() => "Editor Config";
    }
}
