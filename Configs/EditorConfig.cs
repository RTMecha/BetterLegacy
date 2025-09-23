using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Editor Config for PA Legacy. Based on the EditorManagement mod.
    /// </summary>
    public class EditorConfig : BaseConfig
    {
        public static EditorConfig Instance { get; set; }

        public EditorConfig() : base(nameof(EditorConfig))
        {
            Instance = this;
            BindSettings();

            SelectGUI.DragGUI = DragUI.Value;
            ObjectEditor.TimelineObjectHoverSize = TimelineObjectHoverSize.Value;
            ObjectEditor.HideVisualElementsWhenObjectIsEmpty = HideVisualElementsWhenObjectIsEmpty.Value;
            RTPrefabEditor.ImportPrefabsDirectly = ImportPrefabsDirectly.Value;
            RTThemeEditor.themesPerPage = ThemesPerPage.Value;
            RTThemeEditor.eventThemesPerPage = ThemesEventKeyframePerPage.Value;
            RTEditor.DraggingPlaysSound = DraggingPlaysSound.Value;
            RTEditor.DraggingPlaysSoundBPM = DraggingPlaysSoundOnlyWithBPM.Value;
            RTEditor.ShowModdedUI = EditorComplexity.Value == Complexity.Advanced;
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;

            SetPreviewConfig();
            SetupSettingChanged();
        }

        public override string TabName => "Editor";
        public override Color TabColor => new Color(0.5694f, 0.3f, 1f, 1f);
        public override string TabDesc => "The PA editor.";

        #region Settings

        #region General

        public Setting<bool> Debug { get; set; }
        public Setting<bool> ResetHealthInEditor { get; set; }
        public Setting<bool> ApplyGameSettingsInPreviewMode { get; set; }
        public Setting<bool> DraggingPlaysSound { get; set; }
        public Setting<bool> DraggingPlaysSoundOnlyWithBPM { get; set; }
        public Setting<bool> ShowCollapsePrefabWarning { get; set; }
        public Setting<bool> UpdateHomingKeyframesDrag { get; set; }
        public Setting<bool> RoundToNearest { get; set; }
        public Setting<bool> ScrollOnEasing { get; set; }
        public Setting<bool> PrefabExampleTemplate { get; set; }
        public Setting<bool> PasteOffset { get; set; }
        public Setting<bool> BringToSelection { get; set; }
        public Setting<bool> SelectPasted { get; set; }
        public Setting<bool> AllowEditorKeybindsWithEditorCam { get; set; }
        public Setting<bool> RotationEventKeyframeResets { get; set; }
        public Setting<bool> RememberLastKeyframeType { get; set; }

        #endregion

        #region Timeline

        public Setting<bool> ClampedTimelineDrag { get; set; }
        public Setting<bool> DraggingMainCursorPausesLevel { get; set; }
        public Setting<bool> DraggingMainCursorFix { get; set; }
        public Setting<bool> UseMouseAsZoomPoint { get; set; }
        public Setting<Color> TimelineCursorColor { get; set; }
        public Setting<Color> KeyframeCursorColor { get; set; }
        public Setting<Color> ObjectSelectionColor { get; set; }
        public Setting<Color> TimelineObjectBaseColor { get; set; }
        public Setting<Color> TimelineObjectTextColor { get; set; }
        public Setting<Color> TimelineObjectMarkColor { get; set; }
        public Setting<bool> PrioritzePrefabTypeColor { get; set; }
        public Setting<Vector2> MainZoomBounds { get; set; }
        public Setting<Vector2> KeyframeZoomBounds { get; set; }
        public Setting<float> MainZoomAmount { get; set; }
        public Setting<float> KeyframeZoomAmount { get; set; }
        public Setting<KeyCode> BinControlKey { get; set; }
        public Setting<BinSliderControlActive> BinControlActiveBehavior { get; set; }
        public Setting<float> BinControlScrollAmount { get; set; }
        public Setting<bool> BinControlsPlaysSounds { get; set; }
        public Setting<BinClamp> BinClampBehavior { get; set; }
        public Setting<bool> MoveToChangedBin { get; set; }
        public Setting<float> KeyframeEndLengthOffset { get; set; }
        public Setting<float> TimelineObjectCollapseLength { get; set; }
        public Setting<bool> TimelineObjectPrefabTypeIcon { get; set; }
        public Setting<bool> TimelineObjectPrefabIcon { get; set; }
        public Setting<bool> EventLabelsRenderLeft { get; set; }
        public Setting<bool> EventKeyframesRenderBinColor { get; set; }
        public Setting<bool> ObjectKeyframesRenderBinColor { get; set; }
        public Setting<bool> WaveformGenerate { get; set; }
        public Setting<bool> WaveformSaves { get; set; }
        public Setting<bool> WaveformRerender { get; set; }
        public Setting<WaveformType> WaveformMode { get; set; }
        public Setting<Color> WaveformBGColor { get; set; }
        public Setting<Color> WaveformTopColor { get; set; }
        public Setting<Color> WaveformBottomColor { get; set; }
        public Setting<TextureFormat> WaveformTextureFormat { get; set; }
        public Setting<bool> TimelineGridEnabled { get; set; }
        public Setting<Color> TimelineGridColor { get; set; }
        public Setting<float> TimelineGridThickness { get; set; }

        #endregion

        #region Data

        public Setting<int> AutosaveLimit { get; set; }
        public Setting<float> AutosaveLoopTime { get; set; }
        public Setting<bool> UploadDeleteOnLogin { get; set; }
        /// <summary>
        /// If searching should automatically run when sort, ascend, etc is changed.
        /// </summary>
        public Setting<bool> AutoSearch { get; set; }
        public Setting<bool> SaveAsync { get; set; }
        public Setting<bool> LevelLoadsLastTime { get; set; }
        public Setting<bool> LevelPausesOnStart { get; set; }
        public Setting<bool> BackupPreviousLoadedLevel { get; set; }
        public Setting<bool> SettingPathReloads { get; set; }
        public Setting<Rank> EditorRank { get; set; }
        public Setting<bool> CopyPasteGlobal { get; set; }
        public Setting<bool> PasteBackgroundObjectsOverwrites { get; set; }
        public Setting<bool> RetainCopiedPrefabInstanceData { get; set; }
        public Setting<ArrhythmiaType> CombinerOutputFormat { get; set; }
        public Setting<bool> SavingSavesThemeOpacity { get; set; }
        public Setting<bool> AutoPolygonRadius { get; set; }
        public Setting<bool> UpdatePrefabListOnFilesChanged { get; set; }
        public Setting<bool> UpdateThemeListOnFilesChanged { get; set; }
        public Setting<bool> ShowFoldersInLevelList { get; set; }
        public Setting<string> ZIPLevelExportPath { get; set; }
        public Setting<string> ConvertLevelLSToVGExportPath { get; set; }
        public Setting<string> ConvertPrefabLSToVGExportPath { get; set; }
        public Setting<string> ConvertThemeLSToVGExportPath { get; set; }
        public Setting<float> FileBrowserAudioPreviewLength { get; set; }
        public Setting<bool> ThemeSavesIndents { get; set; }
        public Setting<bool> FileBrowserRemembersLocation { get; set; }
        public Setting<bool> ShowDefaultThemes { get; set; }
        public Setting<int> ImageSequenceFPS { get; set; }
        public Setting<bool> OverwriteImportedImages { get; set; }
        public Setting<bool> LoadSoundAssetOnClick { get; set; }

        #endregion

        #region Editor GUI

        public Setting<bool> DragUI { get; set; }
        public Setting<EditorThemeType> EditorTheme { get; set; }
        public Setting<EditorFont> EditorFont { get; set; }
        public Setting<bool> RoundedUI { get; set; }
        public Setting<Complexity> EditorComplexity { get; set; }
        public Setting<bool> ShowExperimental { get; set; }
        public Setting<UserPreferenceType> UserPreference { get; set; }
        public Setting<bool> HoverUIPlaySound { get; set; }
        public Setting<bool> ImportPrefabsDirectly { get; set; }
        public Setting<int> ThemesPerPage { get; set; }
        public Setting<int> ThemesEventKeyframePerPage { get; set; }
        public Setting<bool> ShowHelpOnStartup { get; set; }
        public Setting<bool> MouseTooltipDisplay { get; set; }
        public Setting<bool> MouseTooltipRequiresHelp { get; set; }
        public Setting<float> MouseTooltipHoverTime { get; set; }
        public Setting<bool> HideMouseTooltipOnExit { get; set; }
        public Setting<float> MouseTooltipDisplayTime { get; set; }
        public Setting<float> NotificationDisplayTime { get; set; }
        public Setting<float> NotificationWidth { get; set; }
        public Setting<float> NotificationSize { get; set; }
        public Setting<VerticalDirection> NotificationDirection { get; set; }
        public Setting<bool> NotificationsDisplay { get; set; }
        public Setting<bool> ShowDropdownOnHover { get; set; }
        public Setting<bool> HideVisualElementsWhenObjectIsEmpty { get; set; }
        public Setting<Vector2Int> RenderDepthRange { get; set; }
        public Setting<bool> OpenNewLevelCreatorIfNoLevels { get; set; }

        public Setting<Vector2> OpenLevelPosition { get; set; }
        public Setting<Vector2> OpenLevelScale { get; set; }
        public Setting<Vector2> OpenLevelEditorPathPos { get; set; }
        public Setting<float> OpenLevelEditorPathLength { get; set; }
        public Setting<Vector2> OpenLevelListRefreshPosition { get; set; }
        public Setting<Vector2> OpenLevelTogglePosition { get; set; }
        public Setting<Vector2> OpenLevelDropdownPosition { get; set; }
        public Setting<Vector2> OpenLevelCellSize { get; set; }
        public Setting<GridLayoutGroup.Constraint> OpenLevelCellConstraintType { get; set; }
        public Setting<int> OpenLevelCellConstraintCount { get; set; }
        public Setting<Vector2> OpenLevelCellSpacing { get; set; }
        public Setting<HorizontalWrapMode> OpenLevelTextHorizontalWrap { get; set; }
        public Setting<VerticalWrapMode> OpenLevelTextVerticalWrap { get; set; }
        public Setting<int> OpenLevelTextFontSize { get; set; }

        public Setting<int> OpenLevelFolderNameMax { get; set; }
        public Setting<int> OpenLevelSongNameMax { get; set; }
        public Setting<int> OpenLevelArtistNameMax { get; set; }
        public Setting<int> OpenLevelCreatorNameMax { get; set; }
        public Setting<int> OpenLevelDescriptionMax { get; set; }
        public Setting<int> OpenLevelDateMax { get; set; }
        public Setting<string> OpenLevelTextFormatting { get; set; }

        public Setting<float> OpenLevelButtonHoverSize { get; set; }
        public Setting<Vector2> OpenLevelCoverPosition { get; set; }
        public Setting<Vector2> OpenLevelCoverScale { get; set; }

        public Setting<bool> ChangesRefreshLevelList { get; set; }
        public Setting<bool> OpenLevelShowDeleteButton { get; set; }

        public Setting<float> TimelineObjectHoverSize { get; set; }
        public Setting<float> KeyframeHoverSize { get; set; }
        public Setting<float> TimelineBarButtonsHoverSize { get; set; }
        public Setting<float> PrefabButtonHoverSize { get; set; }

        //Prefab Internal
        public Setting<Vector2> PrefabInternalPopupPos { get; set; }
        public Setting<Vector2> PrefabInternalPopupSize { get; set; }
        public Setting<bool> PrefabInternalHorizontalScroll { get; set; }
        public Setting<Vector2> PrefabInternalCellSize { get; set; }
        public Setting<GridLayoutGroup.Constraint> PrefabInternalConstraintMode { get; set; }
        public Setting<int> PrefabInternalConstraint { get; set; }
        public Setting<Vector2> PrefabInternalSpacing { get; set; }
        public Setting<GridLayoutGroup.Axis> PrefabInternalStartAxis { get; set; }
        public Setting<Vector2> PrefabInternalDeleteButtonPos { get; set; }
        public Setting<Vector2> PrefabInternalDeleteButtonSca { get; set; }
        public Setting<HorizontalWrapMode> PrefabInternalNameHorizontalWrap { get; set; }
        public Setting<VerticalWrapMode> PrefabInternalNameVerticalWrap { get; set; }
        public Setting<int> PrefabInternalNameFontSize { get; set; }
        public Setting<HorizontalWrapMode> PrefabInternalTypeHorizontalWrap { get; set; }
        public Setting<VerticalWrapMode> PrefabInternalTypeVerticalWrap { get; set; }
        public Setting<int> PrefabInternalTypeFontSize { get; set; }

        //Prefab External
        public Setting<Vector2> PrefabExternalPopupPos { get; set; }
        public Setting<Vector2> PrefabExternalPopupSize { get; set; }
        public Setting<Vector2> PrefabExternalPrefabPathPos { get; set; }
        public Setting<float> PrefabExternalPrefabPathLength { get; set; }
        public Setting<Vector2> PrefabExternalPrefabRefreshPos { get; set; }
        public Setting<bool> PrefabExternalHorizontalScroll { get; set; }
        public Setting<Vector2> PrefabExternalCellSize { get; set; }
        public Setting<GridLayoutGroup.Constraint> PrefabExternalConstraintMode { get; set; }
        public Setting<int> PrefabExternalConstraint { get; set; }
        public Setting<Vector2> PrefabExternalSpacing { get; set; }
        public Setting<GridLayoutGroup.Axis> PrefabExternalStartAxis { get; set; }
        public Setting<Vector2> PrefabExternalDeleteButtonPos { get; set; }
        public Setting<Vector2> PrefabExternalDeleteButtonSca { get; set; }
        public Setting<HorizontalWrapMode> PrefabExternalNameHorizontalWrap { get; set; }
        public Setting<VerticalWrapMode> PrefabExternalNameVerticalWrap { get; set; }
        public Setting<int> PrefabExternalNameFontSize { get; set; }
        public Setting<HorizontalWrapMode> PrefabExternalTypeHorizontalWrap { get; set; }
        public Setting<VerticalWrapMode> PrefabExternalTypeVerticalWrap { get; set; }
        public Setting<int> PrefabExternalTypeFontSize { get; set; }

        #endregion

        #region Markers

        public Setting<bool> ShowMarkers { get; set; }
        public Setting<bool> ShowMarkersInObjectEditor { get; set; }
        public Setting<int> MarkerDefaultColor { get; set; }
        public Setting<PointerEventData.InputButton> MarkerDragButton { get; set; }
        public Setting<bool> MarkerShowContextMenu { get; set; }
        public Setting<MarkerLoopBehavior> MarkerLoopBehavior { get; set; }
        public Setting<Color> MarkerLineColor { get; set; }
        public Setting<float> MarkerLineWidth { get; set; }
        public Setting<float> MarkerTextWidth { get; set; }
        public Setting<bool> MarkerLineDotted { get; set; }
        public Setting<Color> ObjectMarkerLineColor { get; set; }
        public Setting<float> ObjectMarkerLineWidth { get; set; }
        public Setting<float> ObjectMarkerTextWidth { get; set; }
        public Setting<bool> ObjectMarkerLineDotted { get; set; }

        #endregion

        #region BPM

        public Setting<bool> AnalyzeBPMOnLevelLoad { get; set; }
        public Setting<bool> BPMSnapsObjects { get; set; }
        public Setting<bool> BPMSnapsObjectKeyframes { get; set; }
        public Setting<bool> BPMSnapsKeyframes { get; set; }
        public Setting<bool> BPMSnapsCheckpoints { get; set; }
        public Setting<bool> BPMSnapsMarkers { get; set; }
        public Setting<bool> BPMSnapsCreated { get; set; }
        public Setting<bool> BPMSnapsPasted { get; set; }
        public Setting<bool> BPMSnapsPrefabImport { get; set; }

        #endregion

        #region Fields

        public Setting<KeyCode> ScrollwheelLargeAmountKey { get; set; }
        public Setting<KeyCode> ScrollwheelSmallAmountKey { get; set; }
        public Setting<KeyCode> ScrollwheelRegularAmountKey { get; set; }
        public Setting<KeyCode> ScrollwheelVector2LargeAmountKey { get; set; }
        public Setting<KeyCode> ScrollwheelVector2SmallAmountKey { get; set; }
        public Setting<KeyCode> ScrollwheelVector2RegularAmountKey { get; set; }

        public Setting<float> ObjectPositionScroll { get; set; }
        public Setting<float> ObjectPositionScrollMultiply { get; set; }
        public Setting<float> ObjectScaleScroll { get; set; }
        public Setting<float> ObjectScaleScrollMultiply { get; set; }
        public Setting<float> ObjectRotationScroll { get; set; }
        public Setting<float> ObjectRotationScrollMultiply { get; set; }

        public Setting<bool> ShowModifiedColors { get; set; }
        public Setting<string> ThemeTemplateName { get; set; }
        public Setting<Color> ThemeTemplateGUI { get; set; }
        public Setting<Color> ThemeTemplateTail { get; set; }
        public Setting<Color> ThemeTemplateBG { get; set; }
        public Setting<Color> ThemeTemplatePlayer1 { get; set; }
        public Setting<Color> ThemeTemplatePlayer2 { get; set; }
        public Setting<Color> ThemeTemplatePlayer3 { get; set; }
        public Setting<Color> ThemeTemplatePlayer4 { get; set; }
        public Setting<Color> ThemeTemplateOBJ1 { get; set; }
        public Setting<Color> ThemeTemplateOBJ2 { get; set; }
        public Setting<Color> ThemeTemplateOBJ3 { get; set; }
        public Setting<Color> ThemeTemplateOBJ4 { get; set; }
        public Setting<Color> ThemeTemplateOBJ5 { get; set; }
        public Setting<Color> ThemeTemplateOBJ6 { get; set; }
        public Setting<Color> ThemeTemplateOBJ7 { get; set; }
        public Setting<Color> ThemeTemplateOBJ8 { get; set; }
        public Setting<Color> ThemeTemplateOBJ9 { get; set; }
        public Setting<Color> ThemeTemplateOBJ10 { get; set; }
        public Setting<Color> ThemeTemplateOBJ11 { get; set; }
        public Setting<Color> ThemeTemplateOBJ12 { get; set; }
        public Setting<Color> ThemeTemplateOBJ13 { get; set; }
        public Setting<Color> ThemeTemplateOBJ14 { get; set; }
        public Setting<Color> ThemeTemplateOBJ15 { get; set; }
        public Setting<Color> ThemeTemplateOBJ16 { get; set; }
        public Setting<Color> ThemeTemplateOBJ17 { get; set; }
        public Setting<Color> ThemeTemplateOBJ18 { get; set; }
        public Setting<Color> ThemeTemplateBG1 { get; set; }
        public Setting<Color> ThemeTemplateBG2 { get; set; }
        public Setting<Color> ThemeTemplateBG3 { get; set; }
        public Setting<Color> ThemeTemplateBG4 { get; set; }
        public Setting<Color> ThemeTemplateBG5 { get; set; }
        public Setting<Color> ThemeTemplateBG6 { get; set; }
        public Setting<Color> ThemeTemplateBG7 { get; set; }
        public Setting<Color> ThemeTemplateBG8 { get; set; }
        public Setting<Color> ThemeTemplateBG9 { get; set; }
        public Setting<Color> ThemeTemplateFX1 { get; set; }
        public Setting<Color> ThemeTemplateFX2 { get; set; }
        public Setting<Color> ThemeTemplateFX3 { get; set; }
        public Setting<Color> ThemeTemplateFX4 { get; set; }
        public Setting<Color> ThemeTemplateFX5 { get; set; }
        public Setting<Color> ThemeTemplateFX6 { get; set; }
        public Setting<Color> ThemeTemplateFX7 { get; set; }
        public Setting<Color> ThemeTemplateFX8 { get; set; }
        public Setting<Color> ThemeTemplateFX9 { get; set; }
        public Setting<Color> ThemeTemplateFX10 { get; set; }
        public Setting<Color> ThemeTemplateFX11 { get; set; }
        public Setting<Color> ThemeTemplateFX12 { get; set; }
        public Setting<Color> ThemeTemplateFX13 { get; set; }
        public Setting<Color> ThemeTemplateFX14 { get; set; }
        public Setting<Color> ThemeTemplateFX15 { get; set; }
        public Setting<Color> ThemeTemplateFX16 { get; set; }
        public Setting<Color> ThemeTemplateFX17 { get; set; }
        public Setting<Color> ThemeTemplateFX18 { get; set; }

        #endregion

        #region Animations

        public Setting<bool> PlayEditorAnimations { get; set; }

        #endregion

        #region Preview

        public Setting<bool> OnlyObjectsOnCurrentLayerVisible { get; set; }
        public Setting<float> VisibleObjectOpacity { get; set; }
        public Setting<bool> ShowEmpties { get; set; }
        public Setting<bool> OnlyShowDamagable { get; set; }
        public Setting<bool> HighlightObjects { get; set; }
        public Setting<Color> ObjectHighlightAmount { get; set; }
        public Setting<Color> ObjectHighlightDoubleAmount { get; set; }

        public Setting<bool> ObjectDraggerHelper { get; set; }
        public Setting<TransformType> ObjectDraggerHelperType { get; set; }
        public Setting<float> ObjectDraggerHelperSize { get; set; }
        public Setting<Color> ObjectDraggerHelperColor { get; set; }
        public Setting<float> ObjectDraggerHelperOutlineSize { get; set; }
        public Setting<Color> ObjectDraggerHelperOutlineColor { get; set; }

        public Setting<bool> ObjectDraggerEnabled { get; set; }
        public Setting<bool> PrefabObjectDraggerEnabled { get; set; }
        public Setting<bool> ObjectDraggerCreatesKeyframe { get; set; }
        public Setting<float> ObjectDraggerRotatorRadius { get; set; }
        public Setting<float> ObjectDraggerScalerOffset { get; set; }
        public Setting<float> ObjectDraggerScalerScale { get; set; }
        public Setting<bool> SelectTextObjectsInPreview { get; set; }
        public Setting<bool> SelectImageObjectsInPreview { get; set; }

        public Setting<bool> PreviewGridEnabled { get; set; }
        public Setting<float> PreviewGridSize { get; set; }
        public Setting<float> PreviewGridThickness { get; set; }
        public Setting<Color> PreviewGridColor { get; set; }

        public Setting<bool> EnableEditorCameraOnDrag { get; set; }

        #endregion

        #region Modifiers

        /// <summary>
        /// Any modifiers with the "loadLevel" function will load the level whilst in the editor. This is only to prevent the loss of progress.
        /// </summary>
        public Setting<bool> ModifiersCanLoadLevels { get; set; }

        /// <summary>
        /// The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.
        /// </summary>
        public Setting<bool> ModifiersSavesBackup { get; set; }

        /// <summary>
        /// If the achievement modifiers display the achievement they unlock in the editor regardless of unlock state.
        /// </summary>
        public Setting<bool> ModifiersDisplayAchievements { get; set; }

        #endregion

        #region Creation

        public Setting<ObjectCreationClickBehaviorType> ObjectCreationClickBehavior { get; set; }
        public Setting<bool> CreateObjectsatCameraCenter { get; set; }
        public Setting<bool> SpawnPrefabsAtCameraCenter { get; set; }
        public Setting<bool> CreateObjectPositionParentDefault { get; set; }
        public Setting<bool> CreateObjectScaleParentDefault { get; set; }
        public Setting<bool> CreateObjectRotationParentDefault { get; set; }
        public Setting<bool> CreateObjectModifierOrderDefault { get; set; }
        public Setting<bool> CreateObjectOpacityCollisionDefault { get; set; }
        public Setting<bool> CreateObjectAutoTextAlignDefault { get; set; }
        public Setting<bool> CreateObjectPositionKFRelativeDefault { get; set; }
        public Setting<bool> CreateObjectScaleKFRelativeDefault { get; set; }
        public Setting<bool> CreateObjectRotationKFRelativeDefault { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            Debug = Bind(this, GENERAL, "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
            ResetHealthInEditor = Bind(this, GENERAL, "Reset Health In Editor View", true, "If on, the player's health will reset when the creator exits Preview Mode.");
            ApplyGameSettingsInPreviewMode = Bind(this, GENERAL, "Apply Game Settings In Preview Mode", true, "If game settings (such as challenge mode and game speed) should apply in preview mode.");
            DraggingPlaysSound = Bind(this, GENERAL, "Dragging Plays Sound", true, "If dragging an object plays a sound.");
            DraggingPlaysSoundOnlyWithBPM = Bind(this, GENERAL, "Dragging Plays Sound Only With BPM", true, "If dragging an object plays a sound ONLY when BPM Snap is active.");
            ShowCollapsePrefabWarning = Bind(this, GENERAL, "Show Collapse Prefab Warning", true, "If a warning should popup when the user is trying to apply a prefab. Can be good for accidental Apply Prefab button clicks.");
            UpdateHomingKeyframesDrag = Bind(this, GENERAL, "Update Homing Keyframes on Drag", true, "If all homing keyframes should retarget when the audio position is changed via the timeline cursor.");
            RoundToNearest = Bind(this, GENERAL, "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).");
            ScrollOnEasing = Bind(this, GENERAL, "Scroll on Easing Changes Value", true, "If Scolling on an easing dropdown changes the easing.");
            PrefabExampleTemplate = Bind(this, GENERAL, "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.");
            PasteOffset = Bind(this, GENERAL, "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.");
            BringToSelection = Bind(this, GENERAL, "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.");
            SelectPasted = Bind(this, GENERAL, "Select Pasted Keyframes", false, "Select a pasted keyframe.");
            AllowEditorKeybindsWithEditorCam = Bind(this, GENERAL, "Allow Editor Keybinds With Editor Cam", true, "Allows keybinds to be used if EventsCore editor camera is on.");
            RotationEventKeyframeResets = Bind(this, GENERAL, "Rotation Event Keyframe Resets", true, "When an Event / Check rotation keyframe is created, it resets the value to 0.");
            RememberLastKeyframeType = Bind(this, GENERAL, "Remember Last Keyframe Type", false, "When an object is selected for the first time, it selects the previous objects' keyframe selection type. For example, say you had a color keyframe selected, this newly selected object will select the first color keyframe.");

            #endregion

            #region Timeline

            ClampedTimelineDrag = Bind(this, TIMELINE, "Clamped Timeline Drag", true, "If dragging objects around in the timeline prevents their start times from going outside the range of the song. Turning this off can be good for having objects spawn at the start of the level.");
            DraggingMainCursorPausesLevel = Bind(this, TIMELINE, "Dragging Main Cursor Pauses Level", true, "If dragging the cursor pauses the level.");
            DraggingMainCursorFix = Bind(this, TIMELINE, "Dragging Main Cursor Fix", true, "If the main cursor should act like the object cursor.");
            UseMouseAsZoomPoint = Bind(this, TIMELINE, "Use Mouse As Zooming Point", false, "If zooming in should use your mouse as a point instead of the timeline cursor. Applies to both main and object timelines..");
            TimelineCursorColor = Bind(this, TIMELINE, "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.");
            KeyframeCursorColor = Bind(this, TIMELINE, "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.");
            ObjectSelectionColor = Bind(this, TIMELINE, "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.");
            TimelineObjectBaseColor = Bind(this, TIMELINE, "Timeline Object Base Color", RTColors.HexToColor("F5F5F5"), "Color of the base of timeline objects.");
            TimelineObjectTextColor = Bind(this, TIMELINE, "Timeline Object Text Color", RTColors.HexToColor("FFFFFF"), "Color of the text of timeline objects.");
            TimelineObjectMarkColor = Bind(this, TIMELINE, "Timeline Object Mark Color", RTColors.HexToColor("000000aa"), "Color of the mark (text BG) of timeline objects.");
            PrioritzePrefabTypeColor = Bind(this, TIMELINE, "Prioritize Prefab Type Color", true, "If the objects' Prefab Type reference color should be prioritized over other colors.");
            MainZoomBounds = Bind(this, TIMELINE, "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.");
            KeyframeZoomBounds = Bind(this, TIMELINE, "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.");
            MainZoomAmount = Bind(this, TIMELINE, "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.");
            KeyframeZoomAmount = Bind(this, TIMELINE, "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.");
            BinControlKey = BindEnum(this, TIMELINE, "Bin Control Key", KeyCode.Tab, "The key to be held when you want to enable bin controls.");
            BinControlActiveBehavior = BindEnum(this, TIMELINE, "Bin Control Active Behavior", BinSliderControlActive.KeyToggled, "How the visibility of the Bin Slider should be treated.");
            BinControlScrollAmount = Bind(this, TIMELINE, "Bin Control Scroll Amount", 0.02f, "How much the bins should scroll.");
            BinControlsPlaysSounds = Bind(this, TIMELINE, "Bin Controls Plays Sounds", true, "If using the bin controls can play sounds.");
            BinClampBehavior = BindEnum(this, TIMELINE, "Bin Clamp Behavior", BinClamp.Clamp, "How timeline objects bin should be handled when dragging past the limits.");
            MoveToChangedBin = Bind(this, TIMELINE, "Move To Changed Bin", true, "If the timeline should move to the bottom of the bin count when a bin is added / removed.");
            KeyframeEndLengthOffset = Bind(this, TIMELINE, "Keyframe End Length Offset", 2f, "Sets the amount of space you have after the last keyframe in an object.");
            TimelineObjectCollapseLength = Bind(this, TIMELINE, "Timeline Object Collapse Length", 0.2f, "How small a collapsed timeline object ends up.", 0.05f, 1f);
            TimelineObjectPrefabTypeIcon = Bind(this, TIMELINE, "Timeline Object Prefab Type Icon", true, "Shows the object's Prefab Type's icon.");
            TimelineObjectPrefabIcon = Bind(this, TIMELINE, "Timeline Object Prefab Icon", false, "If the Prefab icon should be prioritized.");
            EventLabelsRenderLeft = Bind(this, TIMELINE, "Event Labels Render Left", false, "If the Event Layer labels should render on the left side or not.");
            EventKeyframesRenderBinColor = Bind(this, TIMELINE, "Event Keyframes Use Bin Color", true, "If the Event Keyframes should use the bin color when not selected or not.");
            ObjectKeyframesRenderBinColor = Bind(this, TIMELINE, "Object Keyframes Use Bin Color", true, "If the Object Keyframes should use the bin color when not selected or not.");
            WaveformGenerate = Bind(this, TIMELINE, "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)");
            WaveformSaves = Bind(this, TIMELINE, "Waveform Saves", true, "Turn off if you don't want the timeline waveform to save.");
            WaveformRerender = Bind(this, TIMELINE, "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.");
            WaveformMode = BindEnum(this, TIMELINE, "Waveform Mode", WaveformType.Split, "The mode of the timeline waveform.");
            WaveformBGColor = Bind(this, TIMELINE, "Waveform BG Color", Color.clear, "Color of the background for the waveform.");
            WaveformTopColor = Bind(this, TIMELINE, "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.");
            WaveformBottomColor = Bind(this, TIMELINE, "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.");
            WaveformTextureFormat = BindEnum(this, TIMELINE, "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.");
            TimelineGridEnabled = Bind(this, TIMELINE, "Timeline Grid Enabled", true, "If the timeline grid renders.");
            TimelineGridColor = Bind(this, TIMELINE, "Timeline Grid Color", new Color(0.2157f, 0.2157f, 0.2196f, 1f), "The color of the timeline grid.");
            TimelineGridThickness = Bind(this, TIMELINE, "Timeline Grid Thickness", 2f, "The size of each line of the timeline grid.");

            #endregion

            #region Data

            AutosaveLimit = Bind(this, DATA, "Autosave Limit", 7, "If autosave count reaches this number, delete the first autosave.");
            AutosaveLoopTime = Bind(this, DATA, "Autosave Loop Time", 600f, "The repeat time of autosave.");
            UploadDeleteOnLogin = Bind(this, DATA, "Retry Upload or Delete on Login", false, "If the game should try uploading / deleting again after you have logged in.");
            AutoSearch = Bind(this, DATA, "Auto Search", true, "If searching should automatically run when sort, ascend, etc is changed.");
            SaveAsync = Bind(this, DATA, "Save Async", true, "If saving levels should run asynchronously. Having this on will not show logs in the BepInEx log console.");
            LevelLoadsLastTime = Bind(this, DATA, "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
            LevelPausesOnStart = Bind(this, DATA, "Level Pauses on Start", false, "Editor pauses on level load.");
            BackupPreviousLoadedLevel = Bind(this, DATA, "Backup Previous Loaded Level", false, "Saves the previously loaded level when loading a different level to a level-previous.lsb file.");
            SettingPathReloads = Bind(this, DATA, "Setting Path Reloads", true, "With this setting on, update the list for levels, prefabs and themes when changing the directory.");
            EditorRank = Bind(this, DATA, "Editor Rank", Rank.Null, "What rank should be used when displaying / calculating level rank while in the editor.");
            CopyPasteGlobal = Bind(this, DATA, "Copy Paste From Global Folder", false, "If copied objects & event keyframes are saved to a global file for any instance of Project Arrhythmia to load when pasting. Turn off if copy & paste is breaking.");
            PasteBackgroundObjectsOverwrites = Bind(this, DATA, "Paste Background Objects Overwrites", true, "If pasting the entire copied set of BG objects overwrites the current list of BG objects.");
            RetainCopiedPrefabInstanceData = Bind(this, DATA, "Retain Copied Prefab Instance Data", false, "If the copied Prefab instance data should be kept when loading a different level.");
            CombinerOutputFormat = BindEnum(this, DATA, "Combiner Output Format", ArrhythmiaType.LS, "Which PA file type the level combiner outputs.");
            SavingSavesThemeOpacity = Bind(this, DATA, "Saving Saves Theme Opacity", false, "Turn this off if you don't want themes to break in unmodded PA.");
            AutoPolygonRadius = Bind(this, DATA, "Auto Polygon Radius", true, "If polygon shapes' should have their radius automatically modified to fit the way VG handles them.");
            UpdatePrefabListOnFilesChanged = Bind(this, DATA, "Update Prefab List on Files Changed", false, "When you add a prefab to your prefab path, the editor will automatically update the prefab list for you.");
            UpdateThemeListOnFilesChanged = Bind(this, DATA, "Update Theme List on Files Changed", false, "When you add a theme to your theme path, the editor will automatically update the theme list for you.");
            ShowFoldersInLevelList = Bind(this, DATA, "Show Folders In Level List", true, "If folders should appear in the level list UI. This allows you to quickly navigate level folders.");
            ZIPLevelExportPath = Bind(this, DATA, "ZIP Level Export Path", "", "The custom path to export a zipped level to. If no path is set then it will export to beatmaps/exports.");
            ConvertLevelLSToVGExportPath = Bind(this, DATA, "Convert Level LS to VG Export Path", "", "The custom path to export a level to. If no path is set then it will export to beatmaps/exports.");
            ConvertPrefabLSToVGExportPath = Bind(this, DATA, "Convert Prefab LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ConvertThemeLSToVGExportPath = Bind(this, DATA, "Convert Theme LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            FileBrowserAudioPreviewLength = Bind(this, DATA, "File Browser Audio Preview Length", 3f, "How long the file browser audio preview should be.");
            ThemeSavesIndents = Bind(this, DATA, "Theme Saves Indents", false, "If .lst files should save with multiple lines and indents.");
            FileBrowserRemembersLocation = Bind(this, DATA, "File Browser Remembers Location", true, "If the in-editor File Browser should retain the previous path that was set.");
            ShowDefaultThemes = Bind(this, DATA, "Show Default Themes", true, "If the default beatmap themes should appear in the theme list.");
            ImageSequenceFPS = Bind(this, DATA, "Image Sequence FPS", 24, "FPS of a generated image sequence. Image sequences can be created by dragging in a collection of images or a folder that only contains images.");
            OverwriteImportedImages = Bind(this, DATA, "Overwrite Imported Images", false, "If imported images to image objects should overwrite the file.");
            LoadSoundAssetOnClick = Bind(this, DATA, "Load Sound Asset On Click", true, "If sound assets should load the audio clip when clicked.");

            #endregion

            #region Editor GUI

            DragUI = Bind(this, EDITOR_GUI, "Drag UI", true, "Specific UI popups can be dragged around (such as the parent selector, etc).");
            EditorTheme = Bind(this, EDITOR_GUI, "Editor Theme", EditorThemeType.Legacy, "The current theme the editor uses.");
            EditorFont = BindEnum(this, EDITOR_GUI, "Editor Font", BetterLegacy.EditorFont.Inconsolata_Variable, "The current font the editor uses.");
            RoundedUI = Bind(this, EDITOR_GUI, "Rounded UI", false, "If all elements that can be rounded should be so.");
            EditorComplexity = BindEnum(this, EDITOR_GUI, "Editor Complexity", Complexity.Advanced, "What features show in the editor.");
            ShowExperimental = Bind(this, EDITOR_GUI, "Show Experimental Features", false, "If experimental features should display. These features are not gauranteed to always work and have a chance to be changed in future updates.");
            UserPreference = BindEnum(this, EDITOR_GUI, "User Preference", UserPreferenceType.None, "Change this to whatever config preset you want to use. THIS WILL CHANGE A LOT OF SETTINGS, SO USE WITH CAUTION.");
            HoverUIPlaySound = Bind(this, EDITOR_GUI, "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.");
            ImportPrefabsDirectly = Bind(this, EDITOR_GUI, "Import Prefabs Directly", false, "When clicking on an External Prefab, instead of importing it directly it'll bring up a Prefab External View Dialog if this config is off.");
            ThemesPerPage = Bind(this, EDITOR_GUI, "Themes Per Page", 10, "How many themes are shown per page in the Beatmap Themes popup.");
            ThemesEventKeyframePerPage = Bind(this, EDITOR_GUI, "Themes (Event Keyframe) Per Page", 30, "How many themes are shown per page in the theme event keyframe.");
            ShowHelpOnStartup = Bind(this, EDITOR_GUI, "Show Help on Startup", true, "If the help info box should appear on startup.");
            MouseTooltipDisplay = Bind(this, EDITOR_GUI, "Mouse Tooltip Display", true, "If the mouse tooltip should display.");
            MouseTooltipRequiresHelp = Bind(this, EDITOR_GUI, "Mouse Tooltip Requires Help", false, "If the mouse tooltip should only display when the help info box is active.");
            MouseTooltipHoverTime = Bind(this, EDITOR_GUI, "Mouse Tooltip Hover Time", 0.45f, "How long you have to hover your cursor over an element to show the mouse tooltip.", 0f, 20f);
            HideMouseTooltipOnExit = Bind(this, EDITOR_GUI, "Hide Mouse Tooltip on Exit", true, "If the mouse tooltip should hide when the mouse exits the element.");
            MouseTooltipDisplayTime = Bind(this, EDITOR_GUI, "Mouse Tooltip Display Time", 1f, "The multiplied length of time mouse tooltips stay on screen for.", 0f, 20f);
            NotificationDisplayTime = Bind(this, EDITOR_GUI, "Notification Display Time", 1f, "The multiplied length of time notifications stay on screen for.", 0f, 10f);
            NotificationWidth = Bind(this, EDITOR_GUI, "Notification Width", 221f, "Width of the notifications.");
            NotificationSize = Bind(this, EDITOR_GUI, "Notification Size", 1f, "Total size of the notifications.");
            NotificationDirection = BindEnum(this, EDITOR_GUI, "Notification Direction", VerticalDirection.Down, "Direction the notifications popup from.");
            NotificationsDisplay = Bind(this, EDITOR_GUI, "Notifications Display", true, "If the notifications should display. Does not include the help box.");
            ShowDropdownOnHover = Bind(this, EDITOR_GUI, "Show Dropdowns on Hover", false, "If your mouse enters a dropdown bar, it will automatically show the dropdown list.");
            HideVisualElementsWhenObjectIsEmpty = Bind(this, EDITOR_GUI, "Hide Visual Elements When Object Is Empty", true, "If the Beatmap Object is empty, anything related to the visuals of the object doesn't show.");
            RenderDepthRange = Bind(this, EDITOR_GUI, "Render Depth Range", new Vector2Int(219, -98), "The range the Render Depth slider will show. Vanilla Legacy range is 30 and 0.");
            OpenNewLevelCreatorIfNoLevels = Bind(this, EDITOR_GUI, "Open New Level Creator If No Levels", false, "If the New Level Creator popup should open when there are no levels in a level folder.");

            OpenLevelPosition = Bind(this, EDITOR_GUI, "Open Level Position", Vector2.zero, "The position of the Open Level popup.");
            OpenLevelScale = Bind(this, EDITOR_GUI, "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.");
            OpenLevelEditorPathPos = Bind(this, EDITOR_GUI, "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.");
            OpenLevelEditorPathLength = Bind(this, EDITOR_GUI, "Open Level Editor Path Length", 104f, "The length of the editor path input field.");
            OpenLevelListRefreshPosition = Bind(this, EDITOR_GUI, "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.");
            OpenLevelTogglePosition = Bind(this, EDITOR_GUI, "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.");
            OpenLevelDropdownPosition = Bind(this, EDITOR_GUI, "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.");
            OpenLevelCellSize = Bind(this, EDITOR_GUI, "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.");
            OpenLevelCellConstraintType = BindEnum(this, EDITOR_GUI, "Open Level Cell Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.");
            OpenLevelCellConstraintCount = Bind(this, EDITOR_GUI, "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
            OpenLevelCellSpacing = Bind(this, EDITOR_GUI, "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.");
            OpenLevelTextHorizontalWrap = BindEnum(this, EDITOR_GUI, "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
            OpenLevelTextVerticalWrap = BindEnum(this, EDITOR_GUI, "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
            OpenLevelTextFontSize = Bind(this, EDITOR_GUI, "Open Level Text Font Size", 20, "Font size of the folder button text.", 1, 40);

            OpenLevelFolderNameMax = Bind(this, EDITOR_GUI, "Open Level Folder Name Max", 14, "Limited length of the folder name.", 1, 40);
            OpenLevelSongNameMax = Bind(this, EDITOR_GUI, "Open Level Song Name Max", 22, "Limited length of the song name.", 1, 40);
            OpenLevelArtistNameMax = Bind(this, EDITOR_GUI, "Open Level Artist Name Max", 16, "Limited length of the artist name.", 1, 40);
            OpenLevelCreatorNameMax = Bind(this, EDITOR_GUI, "Open Level Creator Name Max", 16, "Limited length of the creator name.", 1, 40);
            OpenLevelDescriptionMax = Bind(this, EDITOR_GUI, "Open Level Description Max", 16, "Limited length of the description.", 1, 40);
            OpenLevelDateMax = Bind(this, EDITOR_GUI, "Open Level Date Max", 16, "Limited length of the date.", 1, 40);
            OpenLevelTextFormatting = Bind(this, EDITOR_GUI, "Open Level Text Formatting", ".  /{0} : {1} by {2}",
                    "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

            OpenLevelButtonHoverSize = Bind(this, EDITOR_GUI, "Open Level Button Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            OpenLevelCoverPosition = Bind(this, EDITOR_GUI, "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
            OpenLevelCoverScale = Bind(this, EDITOR_GUI, "Open Level Cover Scale", new Vector2(26f, 26f), "Size of the level cover.");

            ChangesRefreshLevelList = Bind(this, EDITOR_GUI, "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");
            OpenLevelShowDeleteButton = Bind(this, EDITOR_GUI, "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

            TimelineObjectHoverSize = Bind(this, EDITOR_GUI, "Timeline Object Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            KeyframeHoverSize = Bind(this, EDITOR_GUI, "Keyframe Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            TimelineBarButtonsHoverSize = Bind(this, EDITOR_GUI, "Timeline Bar Buttons Hover Size", 1.05f, "How big the button gets when hovered.", 0.7f, 1.4f);
            PrefabButtonHoverSize = Bind(this, EDITOR_GUI, "Prefab Button Hover Size", 1.05f, "How big the button gets when hovered.", 0.7f, 1.4f);

            PrefabInternalPopupPos = Bind(this, EDITOR_GUI, "Prefab Internal Popup Pos", new Vector2(-80f, -16f), "Position of the internal prefabs popup.");
            PrefabInternalPopupSize = Bind(this, EDITOR_GUI, "Prefab Internal Popup Size", new Vector2(500f, -32f), "Scale of the internal prefabs popup.");
            PrefabInternalHorizontalScroll = Bind(this, EDITOR_GUI, "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabInternalCellSize = Bind(this, EDITOR_GUI, "Prefab Internal Cell Size", new Vector2(483f, 32f), "Size of each Prefab Item.");
            PrefabInternalConstraintMode = BindEnum(this, EDITOR_GUI, "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabInternalConstraint = Bind(this, EDITOR_GUI, "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabInternalSpacing = Bind(this, EDITOR_GUI, "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabInternalStartAxis = BindEnum(this, EDITOR_GUI, "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabInternalDeleteButtonPos = Bind(this, EDITOR_GUI, "Prefab Internal Delete Button Pos", new Vector2(467f, -16f), "Position of the Delete Button.");
            PrefabInternalDeleteButtonSca = Bind(this, EDITOR_GUI, "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabInternalNameHorizontalWrap = BindEnum(this, EDITOR_GUI, "Prefab Internal Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameVerticalWrap = BindEnum(this, EDITOR_GUI, "Prefab Internal Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameFontSize = Bind(this, EDITOR_GUI, "Prefab Internal Name Font Size", 20, "Size of the text font.", 1, 40);
            PrefabInternalTypeHorizontalWrap = BindEnum(this, EDITOR_GUI, "Prefab Internal Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeVerticalWrap = BindEnum(this, EDITOR_GUI, "Prefab Internal Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeFontSize = Bind(this, EDITOR_GUI, "Prefab Internal Type Font Size", 20, "Size of the text font.", 1, 40);

            PrefabExternalPopupPos = Bind(this, EDITOR_GUI, "Prefab External Popup Pos", new Vector2(60f, -16f), "Position of the external prefabs popup.");
            PrefabExternalPopupSize = Bind(this, EDITOR_GUI, "Prefab External Popup Size", new Vector2(500f, -32f), "Scale of the external prefabs popup.");
            PrefabExternalPrefabPathPos = Bind(this, EDITOR_GUI, "Prefab External Prefab Path Pos", new Vector2(325f, 16f), "Position of the prefab path input field.");
            PrefabExternalPrefabPathLength = Bind(this, EDITOR_GUI, "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
            PrefabExternalPrefabRefreshPos = Bind(this, EDITOR_GUI, "Prefab External Prefab Refresh Pos", new Vector2(310f, 450f), "Position of the prefab refresh button.");
            PrefabExternalHorizontalScroll = Bind(this, EDITOR_GUI, "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabExternalCellSize = Bind(this, EDITOR_GUI, "Prefab External Cell Size", new Vector2(483f, 32f), "Size of each Prefab Item.");
            PrefabExternalConstraintMode = BindEnum(this, EDITOR_GUI, "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabExternalConstraint = Bind(this, EDITOR_GUI, "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabExternalSpacing = Bind(this, EDITOR_GUI, "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabExternalStartAxis = BindEnum(this, EDITOR_GUI, "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabExternalDeleteButtonPos = Bind(this, EDITOR_GUI, "Prefab External Delete Button Pos", new Vector2(467f, -16f), "Position of the Delete Button.");
            PrefabExternalDeleteButtonSca = Bind(this, EDITOR_GUI, "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabExternalNameHorizontalWrap = BindEnum(this, EDITOR_GUI, "Prefab External Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameVerticalWrap = BindEnum(this, EDITOR_GUI, "Prefab External Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameFontSize = Bind(this, EDITOR_GUI, "Prefab External Name Font Size", 20, "Size of the text font.", 1, 40);
            PrefabExternalTypeHorizontalWrap = BindEnum(this, EDITOR_GUI, "Prefab External Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeVerticalWrap = BindEnum(this, EDITOR_GUI, "Prefab External Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeFontSize = Bind(this, EDITOR_GUI, "Prefab External Type Font Size", 20, "Size of the text font.", 1, 40);

            #endregion

            #region Markers

            ShowMarkers = Bind(this, MARKERS, "Show Markers", true, "If markers should show in the editor timeline.");
            ShowMarkersInObjectEditor = Bind(this, MARKERS, "Show Markers in Object Editor", false, "If markers should display in the object editor.");
            MarkerDefaultColor = Bind(this, MARKERS, "Marker Default Color", 0, "The default color assigned to a new marker.");
            MarkerDragButton = BindEnum(this, MARKERS, "Marker Drag Button", PointerEventData.InputButton.Middle, "The mouse button to click and hold to drag a marker.");
            MarkerShowContextMenu = Bind(this, MARKERS, "Marker Show Context Menu", false, "If a context menu should show instead of deleting a marker when you right click a marker.");
            MarkerLoopBehavior = Bind(this, MARKERS, "Marker Loop Behavior", BetterLegacy.MarkerLoopBehavior.Loop, "How marker looping should behave.");
            MarkerLineColor = Bind(this, MARKERS, "Marker Line Color", new Color(1f, 1f, 1f, 0.7843f), "The color of the marker lines.");
            MarkerLineWidth = Bind(this, MARKERS, "Marker Line Width", 2f, "The width of the marker lines.");
            MarkerTextWidth = Bind(this, MARKERS, "Marker Text Width", 64f, "The width of the markers' text. If the text is longer than this width, then it doesn't display the symbols after the width.");
            MarkerLineDotted = Bind(this, MARKERS, "Marker Line Dotted", false, "If the markers' line should be dotted.");
            ObjectMarkerLineColor = Bind(this, MARKERS, "Object Marker Line Color", new Color(1f, 1f, 1f, 0.5f), "The color of the marker lines.");
            ObjectMarkerLineWidth = Bind(this, MARKERS, "Object Marker Line Width", 4f, "The width of the marker lines.");
            ObjectMarkerTextWidth = Bind(this, MARKERS, "Object Marker Text Width", 64f, "The width of the markers' text. If the text is longer than this width, then it doesn't display the symbols after the width.");
            ObjectMarkerLineDotted = Bind(this, MARKERS, "Object Marker Line Dotted", false, "If the markers' line should be dotted.");

            #endregion

            #region BPM

            AnalyzeBPMOnLevelLoad = Bind(this, BPM, "Analyze BPM On Level Load", false, "If the BPM should be analyzed when the level loads.");
            BPMSnapsObjects = Bind(this, BPM, "Snap Objects", true, "Makes the start time of objects snap to the BPM.");
            BPMSnapsObjectKeyframes = Bind(this, BPM, "Snap Object Keyframes", false, "Makes the time of object keyframes snap to the BPM.");
            BPMSnapsKeyframes = Bind(this, BPM, "Snap Keyframes", true, "Makes the time of event keyframes snap to the BPM.");
            BPMSnapsCheckpoints = Bind(this, BPM, "Snap Checkpoints", true, "Makes the time of checkpoints snap to the BPM.");
            BPMSnapsMarkers = Bind(this, BPM, "Snap Markers", true, "Makes the time of markers snap to the BPM.");
            BPMSnapsCreated = Bind(this, BPM, "Snap Created", true, "If created objects should snap to the BPM.");
            BPMSnapsPasted = Bind(this, BPM, "Snap Pasted", true, "Makes pasted objects snap to the BPM at an offset from the first object, so any objects that spawn after don't get snapped.");
            BPMSnapsPrefabImport = Bind(this, BPM, "Snap Prefab Import", true, "Makes prefabs imported to the level snap to the BPM.");

            #endregion

            #region Fields

            ScrollwheelLargeAmountKey = BindEnum(this, FIELDS, "Scrollwheel Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelSmallAmountKey = BindEnum(this, FIELDS, "Scrollwheel Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelRegularAmountKey = BindEnum(this, FIELDS, "Scrollwheel Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2LargeAmountKey = BindEnum(this, FIELDS, "Scrollwheel Vector2 Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2SmallAmountKey = BindEnum(this, FIELDS, "Scrollwheel Vector2 Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2RegularAmountKey = BindEnum(this, FIELDS, "Scrollwheel Vector2 Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");

            ObjectPositionScroll = Bind(this, FIELDS, "Object Position Scroll", 0.1f, "The amount that is added when the Object Keyframe Editor position fields are scrolled on.");
            ObjectPositionScrollMultiply = Bind(this, FIELDS, "Object Position Scroll Multiply", 10f, "The amount that is added (multiply) when the Object Keyframe Editor position fields are scrolled on.");
            ObjectScaleScroll = Bind(this, FIELDS, "Object Scale Scroll", 0.1f, "The amount that is added when the Object Keyframe Editor scale fields are scrolled on.");
            ObjectScaleScrollMultiply = Bind(this, FIELDS, "Object Scale Scroll Multiply", 10f, "The amount that is added (multiply) when the Object Keyframe Editor scale fields are scrolled on.");
            ObjectRotationScroll = Bind(this, FIELDS, "Object Rotation Scroll", 15f, "The amount that is added when the Object Keyframe Editor rotation fields are scrolled on.");
            ObjectRotationScrollMultiply = Bind(this, FIELDS, "Object Rotation Scroll Multiply", 3f, "The amount that is added (multiply) when the Object Keyframe Editor rotation fields are scrolled on.");

            ShowModifiedColors = Bind(this, FIELDS, "Show Modified Colors", true, "Keyframe colors show any modifications done (such as hue, saturation and value).");
            ThemeTemplateName = Bind(this, FIELDS, "Theme Template Name", "New Theme", "Name of the template theme.");
            ThemeTemplateGUI = Bind(this, FIELDS, "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
            ThemeTemplateTail = Bind(this, FIELDS, "Theme Template Tail", LSColors.white, "Tail Color of the template theme.");
            ThemeTemplateBG = Bind(this, FIELDS, "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
            ThemeTemplatePlayer1 = Bind(this, FIELDS, "Theme Template Player 1", LSColors.HexToColor(BeatmapTheme.PLAYER_1_COLOR), "Player 1 Color of the template theme.");
            ThemeTemplatePlayer2 = Bind(this, FIELDS, "Theme Template Player 2", LSColors.HexToColor(BeatmapTheme.PLAYER_2_COLOR), "Player 2 Color of the template theme.");
            ThemeTemplatePlayer3 = Bind(this, FIELDS, "Theme Template Player 3", LSColors.HexToColor(BeatmapTheme.PLAYER_3_COLOR), "Player 3 Color of the template theme.");
            ThemeTemplatePlayer4 = Bind(this, FIELDS, "Theme Template Player 4", LSColors.HexToColor(BeatmapTheme.PLAYER_4_COLOR), "Player 4 Color of the template theme.");
            ThemeTemplateOBJ1 = Bind(this, FIELDS, "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
            ThemeTemplateOBJ2 = Bind(this, FIELDS, "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
            ThemeTemplateOBJ3 = Bind(this, FIELDS, "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
            ThemeTemplateOBJ4 = Bind(this, FIELDS, "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
            ThemeTemplateOBJ5 = Bind(this, FIELDS, "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
            ThemeTemplateOBJ6 = Bind(this, FIELDS, "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
            ThemeTemplateOBJ7 = Bind(this, FIELDS, "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
            ThemeTemplateOBJ8 = Bind(this, FIELDS, "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
            ThemeTemplateOBJ9 = Bind(this, FIELDS, "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
            ThemeTemplateOBJ10 = Bind(this, FIELDS, "Theme Template OBJ 10", LSColors.gray100, "OBJ 10 Color of the template theme.");
            ThemeTemplateOBJ11 = Bind(this, FIELDS, "Theme Template OBJ 11", LSColors.gray200, "OBJ 11 Color of the template theme.");
            ThemeTemplateOBJ12 = Bind(this, FIELDS, "Theme Template OBJ 12", LSColors.gray300, "OBJ 12 Color of the template theme.");
            ThemeTemplateOBJ13 = Bind(this, FIELDS, "Theme Template OBJ 13", LSColors.gray400, "OBJ 13 Color of the template theme.");
            ThemeTemplateOBJ14 = Bind(this, FIELDS, "Theme Template OBJ 14", LSColors.gray500, "OBJ 14 Color of the template theme.");
            ThemeTemplateOBJ15 = Bind(this, FIELDS, "Theme Template OBJ 15", LSColors.gray600, "OBJ 15 Color of the template theme.");
            ThemeTemplateOBJ16 = Bind(this, FIELDS, "Theme Template OBJ 16", LSColors.gray700, "OBJ 16 Color of the template theme.");
            ThemeTemplateOBJ17 = Bind(this, FIELDS, "Theme Template OBJ 17", LSColors.gray800, "OBJ 17 Color of the template theme.");
            ThemeTemplateOBJ18 = Bind(this, FIELDS, "Theme Template OBJ 18", LSColors.gray900, "OBJ 18 Color of the template theme.");
            ThemeTemplateBG1 = Bind(this, FIELDS, "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
            ThemeTemplateBG2 = Bind(this, FIELDS, "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
            ThemeTemplateBG3 = Bind(this, FIELDS, "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
            ThemeTemplateBG4 = Bind(this, FIELDS, "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
            ThemeTemplateBG5 = Bind(this, FIELDS, "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
            ThemeTemplateBG6 = Bind(this, FIELDS, "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
            ThemeTemplateBG7 = Bind(this, FIELDS, "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
            ThemeTemplateBG8 = Bind(this, FIELDS, "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
            ThemeTemplateBG9 = Bind(this, FIELDS, "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");
            ThemeTemplateFX1 = Bind(this, FIELDS, "Theme Template FX 1", LSColors.gray100, "FX 1 Color of the template theme.");
            ThemeTemplateFX2 = Bind(this, FIELDS, "Theme Template FX 2", LSColors.gray200, "FX 2 Color of the template theme.");
            ThemeTemplateFX3 = Bind(this, FIELDS, "Theme Template FX 3", LSColors.gray300, "FX 3 Color of the template theme.");
            ThemeTemplateFX4 = Bind(this, FIELDS, "Theme Template FX 4", LSColors.gray400, "FX 4 Color of the template theme.");
            ThemeTemplateFX5 = Bind(this, FIELDS, "Theme Template FX 5", LSColors.gray500, "FX 5 Color of the template theme.");
            ThemeTemplateFX6 = Bind(this, FIELDS, "Theme Template FX 6", LSColors.gray600, "FX 6 Color of the template theme.");
            ThemeTemplateFX7 = Bind(this, FIELDS, "Theme Template FX 7", LSColors.gray700, "FX 7 Color of the template theme.");
            ThemeTemplateFX8 = Bind(this, FIELDS, "Theme Template FX 8", LSColors.gray800, "FX 8 Color of the template theme.");
            ThemeTemplateFX9 = Bind(this, FIELDS, "Theme Template FX 9", LSColors.gray900, "FX 9 Color of the template theme.");
            ThemeTemplateFX10 = Bind(this, FIELDS, "Theme Template FX 10", LSColors.gray100, "FX 10 Color of the template theme.");
            ThemeTemplateFX11 = Bind(this, FIELDS, "Theme Template FX 11", LSColors.gray200, "FX 11 Color of the template theme.");
            ThemeTemplateFX12 = Bind(this, FIELDS, "Theme Template FX 12", LSColors.gray300, "FX 12 Color of the template theme.");
            ThemeTemplateFX13 = Bind(this, FIELDS, "Theme Template FX 13", LSColors.gray400, "FX 13 Color of the template theme.");
            ThemeTemplateFX14 = Bind(this, FIELDS, "Theme Template FX 14", LSColors.gray500, "FX 14 Color of the template theme.");
            ThemeTemplateFX15 = Bind(this, FIELDS, "Theme Template FX 15", LSColors.gray600, "FX 15 Color of the template theme.");
            ThemeTemplateFX16 = Bind(this, FIELDS, "Theme Template FX 16", LSColors.gray700, "FX 16 Color of the template theme.");
            ThemeTemplateFX17 = Bind(this, FIELDS, "Theme Template FX 17", LSColors.gray800, "FX 17 Color of the template theme.");
            ThemeTemplateFX18 = Bind(this, FIELDS, "Theme Template FX 18", LSColors.gray900, "FX 18 Color of the template theme.");

            #endregion

            #region Animations

            PlayEditorAnimations = Bind(this, ANIMATIONS, "Play Editor Animations", true, "If popups should be animated.");

            RegisterPopupAnimation(EditorPopup.OPEN_FILE_POPUP);
            RegisterPopupAnimation(EditorPopup.NEW_FILE_POPUP);
            RegisterPopupAnimation(EditorPopup.SAVE_AS_POPUP);
            RegisterPopupAnimation(EditorPopup.QUICK_ACTIONS_POPUP);
            RegisterPopupAnimation(EditorPopup.PARENT_SELECTOR);
            RegisterPopupAnimation(EditorPopup.PREFAB_POPUP);
            RegisterPopupAnimation(EditorPopup.OBJECT_OPTIONS_POPUP, new EditorAnimatonDefaults
            {
                active = true,
                animatePosition = true,
                openPos = new Vector2(-35f, 57f),
                closePos = new Vector2(-35f, 22f),
                openPosDuration = new Vector2(0f, 0.6f),
                closePosDuration = new Vector2(0f, 0.1f),
                openPosXEase = Easing.OutElastic,
                openPosYEase = Easing.OutElastic,
                closePosXEase = Easing.InCirc,
                closePosYEase = Easing.InCirc,
                animateScale = true,
                openSca = Vector2.one,
                closeSca = new Vector2(1f, 0f),
                openScaDuration = new Vector2(0.6f, 0.6f),
                closeScaDuration = new Vector2(0.1f, 0.1f),
                openScaXEase = Easing.OutElastic,
                openScaYEase = Easing.OutElastic,
                closeScaXEase = Easing.InCirc,
                closeScaYEase = Easing.InCirc,
            });
            RegisterPopupAnimation(EditorPopup.BG_OPTIONS_POPUP, new EditorAnimatonDefaults
            {
                active = true,
                animatePosition = true,
                openPos = new Vector2(0f, 57f),
                closePos = new Vector2(0f, 22f),
                openPosDuration = new Vector2(0f, 0.6f),
                closePosDuration = new Vector2(0f, 0.1f),
                openPosXEase = Easing.OutElastic,
                openPosYEase = Easing.OutElastic,
                closePosXEase = Easing.InCirc,
                closePosYEase = Easing.InCirc,
                animateScale = true,
                openSca = Vector2.one,
                closeSca = new Vector2(1f, 0f),
                openScaDuration = new Vector2(0.6f, 0.6f),
                closeScaDuration = new Vector2(0.1f, 0.1f),
                openScaXEase = Easing.OutElastic,
                openScaYEase = Easing.OutElastic,
                closeScaXEase = Easing.InCirc,
                closeScaYEase = Easing.InCirc,
            });
            RegisterPopupAnimation(EditorPopup.BROWSER_POPUP);
            RegisterPopupAnimation(EditorPopup.OBJECT_SEARCH_POPUP);
            RegisterPopupAnimation(EditorPopup.OBJECT_TEMPLATES_POPUP);
            RegisterPopupAnimation(EditorPopup.WARNING_POPUP);
            RegisterPopupAnimation(EditorPopup.FOLDER_CREATOR_POPUP);
            RegisterPopupAnimation(EditorPopup.TEXT_EDITOR);
            RegisterPopupAnimation(EditorPopup.DOCUMENTATION_POPUP);
            RegisterPopupAnimation(EditorPopup.DEBUGGER_POPUP);
            RegisterPopupAnimation(EditorPopup.AUTOSAVE_POPUP);
            RegisterPopupAnimation(EditorPopup.DEFAULT_MODIFIERS_POPUP);
            RegisterPopupAnimation(EditorPopup.KEYBIND_LIST_POPUP);
            RegisterPopupAnimation(EditorPopup.KEYBIND_FUNCTION_POPUP);
            RegisterPopupAnimation(EditorPopup.THEME_POPUP);
            RegisterPopupAnimation(EditorPopup.PREFAB_TYPES_POPUP);
            RegisterPopupAnimation(EditorPopup.FONT_SELECTOR_POPUP);
            RegisterPopupAnimation(EditorPopup.PINNED_EDITOR_LAYER_POPUP);
            RegisterPopupAnimation(EditorPopup.COLOR_PICKER);
            RegisterPopupAnimation(EditorPopup.DEFAULT_TAGS_POPUP);
            RegisterPopupAnimation(EditorPopup.LEVEL_COLLECTION_POPUP);
            RegisterPopupAnimation(EditorPopup.ASSET_POPUP);
            RegisterPopupAnimation(EditorPopup.ANIMATIONS_POPUP);
            RegisterPopupAnimation(EditorPopup.ACHIEVEMENTS_POPUP);
            RegisterPopupAnimation(EditorPopup.PROGRESS_POPUP);
            RegisterPopupAnimation(EditorPopup.PLAYER_MODELS_POPUP);
            RegisterPopupAnimation(EditorPopup.USER_SEARCH_POPUP);

            RegisterDropdownAnimation(EditorHelper.FILE_DROPDOWN);
            RegisterDropdownAnimation(EditorHelper.EDIT_DROPDOWN);
            RegisterDropdownAnimation(EditorHelper.VIEW_DROPDOWN);
            RegisterDropdownAnimation(EditorHelper.SETTINGS_DROPDOWN);
            RegisterDropdownAnimation(EditorHelper.UPLOAD_DROPDOWN);
            RegisterDropdownAnimation(EditorHelper.HELP_DROPDOWN);

            #endregion

            #region Preview

            OnlyObjectsOnCurrentLayerVisible = Bind(this, PREVIEW, "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent");
            VisibleObjectOpacity = Bind(this, PREVIEW, "Visible Object Opacity", 0.2f, "Opacity of the objects not on the current layer.");
            ShowEmpties = Bind(this, PREVIEW, "Show Empties", false, "If enabled, show all objects that are set to the empty object type.");
            OnlyShowDamagable = Bind(this, PREVIEW, "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
            HighlightObjects = Bind(this, PREVIEW, "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
            ObjectHighlightAmount = Bind(this, PREVIEW, "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
            ObjectHighlightDoubleAmount = Bind(this, PREVIEW, "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.");
            ObjectDraggerHelper = Bind(this, PREVIEW, "Object Drag Helper Enabled", true, "If the object dragger helper should be enabled. This displays empty objects, the true origin of an object and allows dragging other types of objects.");
            ObjectDraggerHelperType = BindEnum(this, PREVIEW, "Object Drag Helper Type", TransformType.Position, "Transform type to drag.");
            ObjectDraggerHelperSize = Bind(this, PREVIEW, "Object Drag Helper Size", 16f, "Size of the Object Drag Helper.");
            ObjectDraggerHelperColor = Bind(this, PREVIEW, "Object Drag Helper Color", Color.white, "Size of the Object Drag Helper outline.");
            ObjectDraggerHelperOutlineSize = Bind(this, PREVIEW, "Object Drag Helper Outline Size", 4f, "Size of the Object Drag Helper outline.");
            ObjectDraggerHelperOutlineColor = Bind(this, PREVIEW, "Object Drag Helper Outline Color", Color.black, "Size of the Object Drag Helper outline.");

            ObjectDraggerEnabled = Bind(this, PREVIEW, "Object Dragging Enabled", false, "If an object can be dragged around.");
            PrefabObjectDraggerEnabled = Bind(this, PREVIEW, "Prefab Object Dragging Enabled", true, "If a Prefab object can be dragged around.");
            ObjectDraggerCreatesKeyframe = Bind(this, PREVIEW, "Object Dragging Creates Keyframe", false, "When an object is dragged, create a keyframe.");
            ObjectDraggerRotatorRadius = Bind(this, PREVIEW, "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.");
            ObjectDraggerScalerOffset = Bind(this, PREVIEW, "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.");
            ObjectDraggerScalerScale = Bind(this, PREVIEW, "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.");

            SelectTextObjectsInPreview = Bind(this, PREVIEW, "Select Text Objects", false, "If text objects can be selected in the preview area.");
            SelectImageObjectsInPreview = Bind(this, PREVIEW, "Select Image Objects", true, "If image objects can be selected in the preview area.");

            PreviewGridEnabled = Bind(this, PREVIEW, "Grid Enabled", false, "If the preview grid should be enabled.");
            PreviewGridSize = Bind(this, PREVIEW, "Grid Size", 0.5f, "The overall size of the preview grid.");
            PreviewGridThickness = Bind(this, PREVIEW, "Grid Thickness", 0.1f, "The line thickness of the preview grid.", 0.01f, 2f);
            PreviewGridColor = Bind(this, PREVIEW, "Grid Color", Color.white, "The color of the preview grid.");

            EnableEditorCameraOnDrag = Bind(this, PREVIEW, "Enable Editor Camera On Drag", true, "If the editor camera should enable on preview drag.");

            #endregion

            #region Modifiers

            ModifiersCanLoadLevels = Bind(this, MODIFIERS, "Modifiers Can Load Levels", true, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            ModifiersSavesBackup = Bind(this, MODIFIERS, "Modifiers Saves Backup", true, "The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.");
            ModifiersDisplayAchievements = Bind(this, MODIFIERS, "Modifiers Display Achievements", true, "If the achievement modifiers display the achievement they unlock in the editor regardless of unlock state.");

            #endregion

            #region Creation

            ObjectCreationClickBehavior = BindEnum(this, CREATION, "Object Creation Click Behavior", ObjectCreationClickBehaviorType.OpenOptionsPopup, "How clicking the Object button should behave.");

            CreateObjectsatCameraCenter = Bind(this, CREATION, "Create Object at Camera Center", false, "When an object is created, its position will be set to that of the camera's.");
            SpawnPrefabsAtCameraCenter = Bind(this, CREATION, "Spawn Prefabs at Camera Center", false, "When a Prefab object is placed into a level, its position will be set to that of the camera's.");

            CreateObjectPositionParentDefault = Bind(this, CREATION, "Object Position Parent Default", true, "The default value for new Beatmap Objects' Position Parent.");
            CreateObjectScaleParentDefault = Bind(this, CREATION, "Object Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.");
            CreateObjectRotationParentDefault = Bind(this, CREATION, "Object Rotation Parent Default", true, "The default value for new Beatmap Objects' Rotation Parent.");
            CreateObjectModifierOrderDefault = Bind(this, CREATION, "Object Modifier Order Default", true, "The default value for new objects' Order Matters toggle.");
            CreateObjectOpacityCollisionDefault = Bind(this, CREATION, "Object Opacity Collision Default", false, "The default value for new objects' Opacity Collision toggle.");
            CreateObjectAutoTextAlignDefault = Bind(this, CREATION, "Object Auto Text Align Default", false, "The default value for new objects' Auto Text Align toggle.");
            CreateObjectPositionKFRelativeDefault = Bind(this, CREATION, "Object Position KF Relative Default", false, "The default value for new objects' position keyframe relative toggle.");
            CreateObjectScaleKFRelativeDefault = Bind(this, CREATION, "Object Scale KF Relative Default", false, "The default value for new objects' scale keyframe relative toggle.");
            CreateObjectRotationKFRelativeDefault = Bind(this, CREATION, "Object Rotation KF Relative Default", true, "The default value for new objects' rotation keyframe relative toggle.");

            #endregion

            Save();
        }

        public List<EditorAnimation> editorAnimations = new List<EditorAnimation>();

        struct EditorAnimatonDefaults
        {
            public static EditorAnimatonDefaults Default => new EditorAnimatonDefaults
            {
                active = true,
                animatePosition = false,
                animateScale = true,
                openSca = Vector2.one,
                openScaDuration = new Vector2(0.6f, 0.6f),
                closeScaDuration = new Vector2(0.1f, 0.1f),
                openScaXEase = Easing.OutElastic,
                openScaYEase = Easing.OutElastic,
                closeScaXEase = Easing.InCirc,
                closeScaYEase = Easing.InCirc,
                animateRotation = false,
            };

            public bool active;

            #region Position

            public bool animatePosition;

            public Vector2 openPos;
            public Vector2 closePos;
            public Vector2 openPosDuration;
            public Vector2 closePosDuration;
            public Easing openPosXEase;
            public Easing openPosYEase;
            public Easing closePosXEase;
            public Easing closePosYEase;

            #endregion

            #region Scale

            public bool animateScale;

            public Vector2 openSca;
            public Vector2 closeSca;
            public Vector2 openScaDuration;
            public Vector2 closeScaDuration;
            public Easing openScaXEase;
            public Easing openScaYEase;
            public Easing closeScaXEase;
            public Easing closeScaYEase;

            #endregion

            #region Rotation

            public bool animateRotation;

            public float openRot;
            public float closeRot;
            public float openRotDuration;
            public float closeRotDuration;
            public Easing openRotEase;
            public Easing closeRotEase;

            #endregion
        }

        void RegisterPopupAnimation(string name) => RegisterPopupAnimation(name, EditorAnimatonDefaults.Default);

        void RegisterPopupAnimation(string name, EditorAnimatonDefaults defaults)
        {
            var editorAnimation = new EditorAnimation(name);

            editorAnimation.ActiveConfig = Bind(this, ANIMATIONS, $"{name} Active", defaults.active, "If the popup animation should play.");

            #region Position

            editorAnimation.PosActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Position", defaults.animatePosition, "If position should be animated.");
            editorAnimation.PosOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Position", defaults.openPos, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.PosCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Position", defaults.closePos, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.PosOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Position Duration", defaults.openPosDuration, "The duration of opening.");
            editorAnimation.PosCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Position Duration", defaults.closePosDuration, "The duration of closing.");
            editorAnimation.PosXOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Position X Ease", defaults.openPosXEase, "The easing of opening.");
            editorAnimation.PosXCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Position X Ease", defaults.closePosXEase, "The easing of closing.");
            editorAnimation.PosYOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Position Y Ease", defaults.openPosYEase, "The easing of opening.");
            editorAnimation.PosYCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Position Y Ease", defaults.closePosYEase, "The easing of closing.");

            #endregion

            #region Scale

            editorAnimation.ScaActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Scale", defaults.animateScale, "If scale should be animated.");
            editorAnimation.ScaOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Scale", defaults.openSca, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.ScaCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Scale", defaults.closeSca, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.ScaOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Scale Duration", defaults.openScaDuration, "The duration of opening.");
            editorAnimation.ScaCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Scale Duration", defaults.closeScaDuration, "The duration of closing.");
            editorAnimation.ScaXOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Scale X Ease", defaults.openScaXEase, "The easing of opening.");
            editorAnimation.ScaXCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Scale X Ease", defaults.closeScaXEase, "The easing of closing.");
            editorAnimation.ScaYOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Scale Y Ease", defaults.openScaYEase, "The easing of opening.");
            editorAnimation.ScaYCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Scale Y Ease", defaults.closeScaYEase, "The easing of closing.");

            #endregion

            #region Rotation

            editorAnimation.RotActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Rotation", defaults.animateRotation, "If rotation should be animated.");
            editorAnimation.RotOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Rotation", defaults.openRot, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.RotCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Rotation", defaults.closeRot, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.RotOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Rotation Duration", defaults.openRotDuration, "The duration of opening.");
            editorAnimation.RotCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Rotation Duration", defaults.closeRotDuration, "The duration of closing.");
            editorAnimation.RotOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Rotation Ease", defaults.openRotEase, "The easing of opening.");
            editorAnimation.RotCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Rotation Ease", defaults.closeRotEase, "The easing of closing.");

            #endregion

            editorAnimations.Add(editorAnimation);
        }

        void RegisterDropdownAnimation(string name)
        {
            name += " Dropdown";

            var editorAnimation = new EditorAnimation(name);

            editorAnimation.ActiveConfig = Bind(this, ANIMATIONS, $"{name} Active", true, "If the popup animation should play.");

            editorAnimation.PosActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Position", false, "If position should be animated.");
            editorAnimation.PosOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.PosCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.PosOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Position Duration", Vector2.zero, "The duration of opening.");
            editorAnimation.PosCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Position Duration", Vector2.zero, "The duration of closing.");
            editorAnimation.PosXOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Position X Ease", Easing.Linear, "The easing of opening.");
            editorAnimation.PosXCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Position X Ease", Easing.Linear, "The easing of opening.");
            editorAnimation.PosYOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Position Y Ease", Easing.Linear, "The easing of opening.");
            editorAnimation.PosYCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Position Y Ease", Easing.Linear, "The easing of opening.");

            editorAnimation.ScaActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Scale", true, "If scale should be animated.");
            editorAnimation.ScaOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.ScaCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.ScaOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            editorAnimation.ScaCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            editorAnimation.ScaXOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            editorAnimation.ScaXCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            editorAnimation.ScaYOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            editorAnimation.ScaYCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            editorAnimation.RotActiveConfig = Bind(this, ANIMATIONS, $"{name} Animate Rotation", false, "If rotation should be animated.");
            editorAnimation.RotOpenConfig = Bind(this, ANIMATIONS, $"{name} Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            editorAnimation.RotCloseConfig = Bind(this, ANIMATIONS, $"{name} Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            editorAnimation.RotOpenDurationConfig = Bind(this, ANIMATIONS, $"{name} Open Rotation Duration", 0f, "The duration of opening.");
            editorAnimation.RotCloseDurationConfig = Bind(this, ANIMATIONS, $"{name} Close Rotation Duration", 0f, "The duration of closing.");
            editorAnimation.RotOpenEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Open Rotation Ease", Easing.Linear, "The easing of opening.");
            editorAnimation.RotCloseEaseConfig = BindEnum(this, ANIMATIONS, $"{name} Close Rotation Ease", Easing.Linear, "The easing of opening.");

            editorAnimations.Add(editorAnimation);
        }

        #region Setting Changed

        public static Action UpdateEditorComplexity { get; set; }

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;

            TimelineGridEnabled.SettingChanged += TimelineGridChanged;
            TimelineGridThickness.SettingChanged += TimelineGridChanged;
            TimelineGridColor.SettingChanged += TimelineGridChanged;

            DragUI.SettingChanged += DragUIChanged;

            AutoPolygonRadius.SettingChanged += ObjectEditorChanged;
            HideVisualElementsWhenObjectIsEmpty.SettingChanged += ObjectEditorChanged;
            KeyframeZoomBounds.SettingChanged += ObjectEditorChanged;
            ObjectSelectionColor.SettingChanged += ObjectEditorChanged;
            KeyframeEndLengthOffset.SettingChanged += ObjectEditorChanged;

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
            WaveformMode.SettingChanged += TimelineWaveformChanged;
            WaveformBGColor.SettingChanged += TimelineWaveformChanged;
            WaveformBottomColor.SettingChanged += TimelineWaveformChanged;
            WaveformTopColor.SettingChanged += TimelineWaveformChanged;
            WaveformTextureFormat.SettingChanged += TimelineWaveformChanged;

            DraggingPlaysSound.SettingChanged += DraggingChanged;
            DraggingPlaysSoundOnlyWithBPM.SettingChanged += DraggingChanged;

            EditorComplexity.SettingChanged += ModdedEditorChanged;
            ShowExperimental.SettingChanged += ModdedEditorChanged;

            MarkerLineColor.SettingChanged += MarkerChanged;
            MarkerLineWidth.SettingChanged += MarkerChanged;
            MarkerTextWidth.SettingChanged += MarkerChanged;

            EditorTheme.SettingChanged += EditorThemeChanged;
            RoundedUI.SettingChanged += EditorThemeChanged;

            AutosaveLoopTime.SettingChanged += AutosaveChanged;

            TimelineObjectCollapseLength.SettingChanged += TimelineCollapseLengthChanged;

            PreviewGridEnabled.SettingChanged += PreviewGridChanged;
            PreviewGridSize.SettingChanged += PreviewGridChanged;
            PreviewGridThickness.SettingChanged += PreviewGridChanged;
            PreviewGridColor.SettingChanged += PreviewGridChanged;

            PrefabInternalPopupPos.SettingChanged += PrefabPopupsChanged;
            PrefabInternalPopupSize.SettingChanged += PrefabPopupsChanged;
            PrefabInternalSpacing.SettingChanged += PrefabPopupsChanged;
            PrefabInternalCellSize.SettingChanged += PrefabPopupsChanged;
            PrefabInternalConstraintMode.SettingChanged += PrefabPopupsChanged;
            PrefabInternalConstraint.SettingChanged += PrefabPopupsChanged;
            PrefabInternalStartAxis.SettingChanged += PrefabPopupsChanged;
            PrefabInternalHorizontalScroll.SettingChanged += PrefabPopupsChanged;

            PrefabInternalDeleteButtonPos.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalDeleteButtonSca.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalNameFontSize.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalNameHorizontalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalNameVerticalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalTypeFontSize.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalTypeHorizontalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabInternalTypeVerticalWrap.SettingChanged += PrefabPopupsItemsChanged;

            PrefabExternalPopupPos.SettingChanged += PrefabPopupsChanged;
            PrefabExternalPopupSize.SettingChanged += PrefabPopupsChanged;
            PrefabExternalSpacing.SettingChanged += PrefabPopupsChanged;
            PrefabExternalCellSize.SettingChanged += PrefabPopupsChanged;
            PrefabExternalConstraintMode.SettingChanged += PrefabPopupsChanged;
            PrefabExternalConstraint.SettingChanged += PrefabPopupsChanged;
            PrefabExternalStartAxis.SettingChanged += PrefabPopupsChanged;
            PrefabExternalHorizontalScroll.SettingChanged += PrefabPopupsChanged;
            PrefabExternalPrefabRefreshPos.SettingChanged += PrefabPopupsChanged;
            PrefabExternalPrefabPathPos.SettingChanged += PrefabPopupsChanged;
            PrefabExternalPrefabPathLength.SettingChanged += PrefabPopupsChanged;

            PrefabExternalDeleteButtonPos.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalDeleteButtonSca.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalNameFontSize.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalNameHorizontalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalNameVerticalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalTypeFontSize.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalTypeHorizontalWrap.SettingChanged += PrefabPopupsItemsChanged;
            PrefabExternalTypeVerticalWrap.SettingChanged += PrefabPopupsItemsChanged;

            UserPreference.SettingChanged += UserPreferenceChanged;
            ShowMarkers.SettingChanged += ShowMarkersChanged;

            NotificationWidth.SettingChanged += NotificationChanged;
            NotificationSize.SettingChanged += NotificationChanged;
            NotificationDirection.SettingChanged += NotificationChanged;

            TimelineCursorColor.SettingChanged += TimelineColorsChanged;
            KeyframeCursorColor.SettingChanged += TimelineColorsChanged;

            MarkerLineDotted.SettingChanged += MarkerLineDottedChanged;
        }

        void MarkerLineDottedChanged()
        {
            if (!CoreHelper.InEditor)
                return;

            for (int i = 0; i < RTMarkerEditor.inst.timelineMarkers.Count; i++)
                RTMarkerEditor.inst.timelineMarkers[i].RenderLine();
        }

        void NotificationChanged()
        {
            if (CoreHelper.InEditor)
                RTEditor.inst.UpdateNotificationConfig();
        }

        void ShowMarkersChanged()
        {
            if (EditorManager.inst && EditorManager.inst.markerTimeline)
                EditorManager.inst.markerTimeline.SetActive(ShowMarkers.Value);
        }

        void UserPreferenceChanged() => CoreHelper.SetConfigPreset(UserPreference.Value);

        void PrefabPopupsItemsChanged()
        {
            if (PrefabEditor.inst.internalPrefabDialog.gameObject.activeInHierarchy)
                CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());

            if (!PrefabEditor.inst.externalPrefabDialog.gameObject.activeInHierarchy)
                return;

            for (int i = 0; i < RTPrefabEditor.inst.PrefabPanels.Count; i++)
            {
                var prefabPanel = RTPrefabEditor.inst.PrefabPanels[i];
                prefabPanel.DeleteButton.transform.AsRT().anchoredPosition = PrefabExternalDeleteButtonPos.Value;
                prefabPanel.DeleteButton.transform.AsRT().sizeDelta = PrefabExternalDeleteButtonSca.Value;

                prefabPanel.Label.fontSize = PrefabExternalNameFontSize.Value;
                prefabPanel.Label.horizontalOverflow = PrefabExternalNameHorizontalWrap.Value;
                prefabPanel.Label.verticalOverflow = PrefabExternalNameVerticalWrap.Value;

                prefabPanel.TypeText.fontSize = PrefabExternalTypeFontSize.Value;
                prefabPanel.TypeText.horizontalOverflow = PrefabExternalTypeHorizontalWrap.Value;
                prefabPanel.TypeText.verticalOverflow = PrefabExternalTypeVerticalWrap.Value;
            }
        }

        void PrefabPopupsChanged()
        {
            try
            {
                //Internal Config
                {
                    var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                    var internalPrefabGLG = internalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                    internalPrefabGLG.spacing = PrefabInternalSpacing.Value;
                    internalPrefabGLG.cellSize = PrefabInternalCellSize.Value;
                    internalPrefabGLG.constraint = PrefabInternalConstraintMode.Value;
                    internalPrefabGLG.constraintCount = PrefabInternalConstraint.Value;
                    internalPrefabGLG.startAxis = PrefabInternalStartAxis.Value;

                    internalPrefab.AsRT().anchoredPosition = PrefabInternalPopupPos.Value;
                    internalPrefab.AsRT().sizeDelta = PrefabInternalPopupSize.Value;

                    internalPrefab.GetComponent<ScrollRect>().horizontal = PrefabInternalHorizontalScroll.Value;
                }

                //External Config
                {
                    var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                    var externalPrefabGLG = externalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                    externalPrefabGLG.spacing = PrefabExternalSpacing.Value;
                    externalPrefabGLG.cellSize = PrefabExternalCellSize.Value;
                    externalPrefabGLG.constraint = PrefabExternalConstraintMode.Value;
                    externalPrefabGLG.constraintCount = PrefabExternalConstraint.Value;
                    externalPrefabGLG.startAxis = PrefabExternalStartAxis.Value;

                    externalPrefab.AsRT().anchoredPosition = PrefabExternalPopupPos.Value;
                    externalPrefab.AsRT().sizeDelta = PrefabExternalPopupSize.Value;

                    externalPrefab.GetComponent<ScrollRect>().horizontal = PrefabExternalHorizontalScroll.Value;

                    RTEditor.inst.prefabPathField.transform.AsRT().anchoredPosition = PrefabExternalPrefabPathPos.Value;
                    RTEditor.inst.prefabPathField.transform.AsRT().sizeDelta = new Vector2(PrefabExternalPrefabPathLength.Value, 32f);

                    PrefabEditor.inst.externalPrefabDialog.Find("reload prefabs").AsRT().anchoredPosition = PrefabExternalPrefabRefreshPos.Value;

                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't update Prefab Popups.\nException: {ex}");
            }
        }

        void PreviewGridChanged()
        {
            if (RTEditor.inst && RTEditor.inst.previewGrid)
            {
                RTEditor.inst.UpdateGrid();
            }
        }

        void TimelineCollapseLengthChanged()
        {
            if (EditorTimeline.inst)
                EditorTimeline.inst.RenderTimelineObjects();
        }

        void ThemeEventKeyframeChanged()
        {
            RTThemeEditor.eventThemesPerPage = ThemesEventKeyframePerPage.Value;

            if (!CoreHelper.InEditor)
                return;

            var p = Mathf.Clamp(RTThemeEditor.inst.Dialog.Page, 0, ThemeManager.inst.ThemeCount / RTThemeEditor.eventThemesPerPage).ToString();

            if (RTThemeEditor.inst.Dialog.PageField.Text != p)
            {
                RTThemeEditor.inst.Dialog.PageField.Text = p;
                return;
            }


            if (RTThemeEditor.inst.Dialog.GameObject.activeInHierarchy)
                RTThemeEditor.inst.RenderThemeList();
        }

        void AutosaveChanged()
        {
            if (CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel)
                EditorLevelManager.inst.SetAutosave();
        }

        void EditorThemeChanged()
        {
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;
            CoroutineHelper.StartCoroutine(EditorThemeManager.RenderElements());
        }

        void MarkerChanged() => RTMarkerEditor.inst?.RenderMarkers();

        void ModdedEditorChanged()
        {
            RTEditor.ShowModdedUI = EditorComplexity.Value == Complexity.Advanced;

            UpdateEditorComplexity?.Invoke();
            AdjustPositionInputsChanged?.Invoke();

            if (EditorTimeline.inst && EditorTimeline.inst.SelectedObjectCount == 1 && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());

            if (RTEditor.inst && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderEventsDialog();
            }

            if (RTPrefabEditor.inst && EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectDialog(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());

            if (RTBackgroundEditor.inst && EditorTimeline.inst.CurrentSelection.isBackgroundObject)
                RTBackgroundEditor.inst.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>());
        }

        void DraggingChanged()
        {
            RTEditor.DraggingPlaysSound = DraggingPlaysSound.Value;
            RTEditor.DraggingPlaysSoundBPM = DraggingPlaysSoundOnlyWithBPM.Value;
        }

        void TimelineWaveformChanged()
        {
            if (!RTEditor.inst)
                return;

            if (WaveformRerender.Value)
                RTEditor.inst.StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(AudioManager.inst.CurrentAudioSource.clip));
        }

        void ThemePopupChanged()
        {
            RTThemeEditor.themesPerPage = ThemesPerPage.Value;
        }

        void ObjectEditorChanged()
        {
            ObjectEditor.HideVisualElementsWhenObjectIsEmpty = HideVisualElementsWhenObjectIsEmpty.Value;

            if (!ObjEditor.inst)
                return;

            ObjEditor.inst.zoomBounds = KeyframeZoomBounds.Value;
            ObjEditor.inst.ObjectLengthOffset = KeyframeEndLengthOffset.Value;
            ObjEditor.inst.SelectedColor = ObjectSelectionColor.Value;

            if (EditorTimeline.inst.SelectedObjectCount == 1 && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
        }

        void TimelineGridChanged()
        {
            if (!RTEditor.inst)
                return;

            EditorTimeline.inst.timelineGridRenderer.enabled = false;

            EditorTimeline.inst.timelineGridRenderer.color = TimelineGridColor.Value;
            EditorTimeline.inst.timelineGridRenderer.thickness = TimelineGridThickness.Value;

            EditorTimeline.inst.SetTimelineGridSize();
        }

        void ThemeTemplateChanged() => UpdateDefaultThemeValues();

        void DragUIChanged() => SelectGUI.DragGUI = DragUI.Value;

        public static Action AdjustPositionInputsChanged { get; set; }

        void UpdateSettings()
        {
            SetPreviewConfig();

            ObjectEditor.TimelineObjectHoverSize = TimelineObjectHoverSize.Value;
            RTPrefabEditor.ImportPrefabsDirectly = ImportPrefabsDirectly.Value;

            if (!CoreHelper.InEditor)
                return;

            EditorManager.inst.zoomBounds = MainZoomBounds.Value;

            AdjustPositionInputsChanged?.Invoke();

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                RTEventEditor.inst.RenderLayerBins();
        }

        void SetPreviewConfig()
        {
            SelectObject.Enabled = ObjectDraggerEnabled.Value;
            SelectObject.CreateKeyframe = ObjectDraggerCreatesKeyframe.Value;
            SelectObject.HighlightColor = ObjectHighlightAmount.Value;
            SelectObject.HighlightDoubleColor = ObjectHighlightDoubleAmount.Value;
            SelectObject.HighlightObjects = HighlightObjects.Value;
            SelectObject.ShowObjectsOnlyOnLayer = OnlyObjectsOnCurrentLayerVisible.Value;
            SelectObject.LayerOpacity = VisibleObjectOpacity.Value;

            SelectObjectRotator.RotatorRadius = ObjectDraggerRotatorRadius.Value;
            SelectObjectScaler.ScalerOffset = ObjectDraggerScalerOffset.Value;
            SelectObjectScaler.ScalerScale = ObjectDraggerScalerScale.Value;

            if ((showEmpties != ShowEmpties.Value || OnlyShowDamagable.Value != OnlyShowDamagable.Value) && CoreHelper.InEditor)
                RTLevel.Reinit();

            showEmpties = ShowEmpties.Value;
        }

        static bool showEmpties;

        void TimelineColorsChanged()
        {
            if (!CoreHelper.InEditor)
                return;

            EditorTimeline.inst.UpdateTimelineColors();

            KeyframeTimeline.AllTimelines.ForLoop(keyframeTimeline => keyframeTimeline?.SetCursorColor(KeyframeCursorColor.Value));
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

        #endregion

        #region Sections

        public const string GENERAL = "General";
        public const string TIMELINE = "Timeline";
        public const string DATA = "Data";
        public const string EDITOR_GUI = "Editor GUI";
        public const string MARKERS = "Markers";
        public const string BPM = "BPM";
        public const string FIELDS = "Fields";
        public const string ANIMATIONS = "Animations";
        public const string PREVIEW = "Preview";
        public const string MODIFIERS = "Modifiers";
        public const string CREATION = "Creation";

        #endregion
    }
}
