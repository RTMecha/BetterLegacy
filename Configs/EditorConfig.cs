using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            ObjectEditor.RenderPrefabTypeIcon = TimelineObjectPrefabTypeIcon.Value;
            ObjectEditor.TimelineObjectHoverSize = TimelineObjectHoverSize.Value;
            ObjectEditor.HideVisualElementsWhenObjectIsEmpty = HideVisualElementsWhenObjectIsEmpty.Value;
            KeybindEditor.AllowKeys = AllowEditorKeybindsWithEditorCam.Value;
            RTPrefabEditor.ImportPrefabsDirectly = ImportPrefabsDirectly.Value;
            RTThemeEditor.themesPerPage = ThemesPerPage.Value;
            RTThemeEditor.eventThemesPerPage = ThemesEventKeyframePerPage.Value;
            RTEditor.DraggingPlaysSound = DraggingPlaysSound.Value;
            RTEditor.DraggingPlaysSoundBPM = DraggingPlaysSoundOnlyWithBPM.Value;
            RTEditor.ShowModdedUI = EditorComplexity.Value == Complexity.Advanced;
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;
            ObjectEditor.TimelineCollapseLength = TimelineCollapseLength.Value;

            SetPreviewConfig();
            SetupSettingChanged();
        }

        #region Settings

        #region General

        public Setting<bool> Debug { get; set; }
        public Setting<bool> EditorZenMode { get; set; }
        public Setting<bool> ResetHealthInEditor { get; set; }
        public Setting<bool> BPMSnapsKeyframes { get; set; }
        public Setting<float> BPMSnapDivisions { get; set; }
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
        public Setting<bool> CreateObjectsatCameraCenter { get; set; }
        public Setting<bool> SpawnPrefabsAtCameraCenter { get; set; }
        public Setting<bool> CreateObjectsScaleParentDefault { get; set; }
        public Setting<bool> CreateObjectModifierOrderDefault { get; set; }
        public Setting<bool> AllowEditorKeybindsWithEditorCam { get; set; }
        public Setting<bool> RotationEventKeyframeResets { get; set; }
        public Setting<bool> RememberLastKeyframeType { get; set; }

        #endregion

        #region Timeline

        public Setting<bool> DraggingMainCursorPausesLevel { get; set; }
        public Setting<bool> DraggingMainCursorFix { get; set; }
        public Setting<bool> UseMouseAsZoomPoint { get; set; }
        public Setting<Color> TimelineCursorColor { get; set; }
        public Setting<Color> KeyframeCursorColor { get; set; }
        public Setting<Color> ObjectSelectionColor { get; set; }
        public Setting<Vector2> MainZoomBounds { get; set; }
        public Setting<Vector2> KeyframeZoomBounds { get; set; }
        public Setting<float> MainZoomAmount { get; set; }
        public Setting<float> KeyframeZoomAmount { get; set; }
        public Setting<KeyCode> BinControlKey { get; set; }
        public Setting<BinSliderControlActive> BinControlActiveBehavior { get; set; }
        public Setting<float> BinControlScrollAmount { get; set; }
        public Setting<bool> BinControlsPlaysSounds { get; set; }
        public Setting<bool> TimelineObjectRetainsBinOnDrag { get; set; }
        public Setting<bool> MoveToChangedBin { get; set; }
        public Setting<float> KeyframeEndLengthOffset { get; set; }
        public Setting<float> TimelineCollapseLength { get; set; }
        public Setting<bool> TimelineObjectPrefabTypeIcon { get; set; }
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
        public Setting<bool> SaveAsync { get; set; }
        public Setting<bool> LevelLoadsLastTime { get; set; }
        public Setting<bool> LevelPausesOnStart { get; set; }
        public Setting<bool> BackupPreviousLoadedLevel { get; set; }
        public Setting<bool> SettingPathReloads { get; set; }
        public Setting<Rank> EditorRank { get; set; }
        public Setting<bool> CopyPasteGlobal { get; set; }
        public Setting<YieldType> ExpandObjectsYieldMode { get; set; }
        public Setting<YieldType> UpdateExpandedObjectsYieldMode { get; set; }
        public Setting<YieldType> PasteObjectsYieldMode { get; set; }
        public Setting<YieldType> UpdatePastedObjectsYieldMode { get; set; }
        public Setting<ArrhythmiaType> CombinerOutputFormat { get; set; }
        public Setting<bool> SavingSavesThemeOpacity { get; set; }
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
        public Setting<bool> PasteBackgroundObjectsOverwrites { get; set; }

        #endregion

        #region Editor GUI

        public Setting<bool> DragUI { get; set; }
        public Setting<EditorTheme> EditorTheme { get; set; }
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
        public Setting<bool> AdjustPositionInputs { get; set; }
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
        public Setting<Color> MarkerLineColor { get; set; }
        public Setting<float> MarkerLineWidth { get; set; }
        public Setting<float> MarkerTextWidth { get; set; }
        public Setting<bool> MarkerLineDotted { get; set; }
        public Setting<Color> ObjectMarkerLineColor { get; set; }
        public Setting<float> ObjectMarkerLineWidth { get; set; }
        public Setting<float> ObjectMarkerTextWidth { get; set; }
        public Setting<bool> ObjectMarkerLineDotted { get; set; }

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

        #region Open File Popup

        public Setting<bool> OpenFilePopupActive { get; set; }

        public Setting<bool> OpenFilePopupPosActive { get; set; }
        public Setting<Vector2> OpenFilePopupPosOpen { get; set; }
        public Setting<Vector2> OpenFilePopupPosClose { get; set; }
        public Setting<Vector2> OpenFilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> OpenFilePopupPosCloseDuration { get; set; }
        public Setting<Easings> OpenFilePopupPosXOpenEase { get; set; }
        public Setting<Easings> OpenFilePopupPosXCloseEase { get; set; }
        public Setting<Easings> OpenFilePopupPosYOpenEase { get; set; }
        public Setting<Easings> OpenFilePopupPosYCloseEase { get; set; }

        public Setting<bool> OpenFilePopupScaActive { get; set; }
        public Setting<Vector2> OpenFilePopupScaOpen { get; set; }
        public Setting<Vector2> OpenFilePopupScaClose { get; set; }
        public Setting<Vector2> OpenFilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> OpenFilePopupScaCloseDuration { get; set; }
        public Setting<Easings> OpenFilePopupScaXOpenEase { get; set; }
        public Setting<Easings> OpenFilePopupScaXCloseEase { get; set; }
        public Setting<Easings> OpenFilePopupScaYOpenEase { get; set; }
        public Setting<Easings> OpenFilePopupScaYCloseEase { get; set; }

        public Setting<bool> OpenFilePopupRotActive { get; set; }
        public Setting<float> OpenFilePopupRotOpen { get; set; }
        public Setting<float> OpenFilePopupRotClose { get; set; }
        public Setting<float> OpenFilePopupRotOpenDuration { get; set; }
        public Setting<float> OpenFilePopupRotCloseDuration { get; set; }
        public Setting<Easings> OpenFilePopupRotOpenEase { get; set; }
        public Setting<Easings> OpenFilePopupRotCloseEase { get; set; }

        #endregion

        #region New File Popup

        public Setting<bool> NewFilePopupActive { get; set; }

        public Setting<bool> NewFilePopupPosActive { get; set; }
        public Setting<Vector2> NewFilePopupPosOpen { get; set; }
        public Setting<Vector2> NewFilePopupPosClose { get; set; }
        public Setting<Vector2> NewFilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> NewFilePopupPosCloseDuration { get; set; }
        public Setting<Easings> NewFilePopupPosXOpenEase { get; set; }
        public Setting<Easings> NewFilePopupPosXCloseEase { get; set; }
        public Setting<Easings> NewFilePopupPosYOpenEase { get; set; }
        public Setting<Easings> NewFilePopupPosYCloseEase { get; set; }

        public Setting<bool> NewFilePopupScaActive { get; set; }
        public Setting<Vector2> NewFilePopupScaOpen { get; set; }
        public Setting<Vector2> NewFilePopupScaClose { get; set; }
        public Setting<Vector2> NewFilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> NewFilePopupScaCloseDuration { get; set; }
        public Setting<Easings> NewFilePopupScaXOpenEase { get; set; }
        public Setting<Easings> NewFilePopupScaXCloseEase { get; set; }
        public Setting<Easings> NewFilePopupScaYOpenEase { get; set; }
        public Setting<Easings> NewFilePopupScaYCloseEase { get; set; }

        public Setting<bool> NewFilePopupRotActive { get; set; }
        public Setting<float> NewFilePopupRotOpen { get; set; }
        public Setting<float> NewFilePopupRotClose { get; set; }
        public Setting<float> NewFilePopupRotOpenDuration { get; set; }
        public Setting<float> NewFilePopupRotCloseDuration { get; set; }
        public Setting<Easings> NewFilePopupRotOpenEase { get; set; }
        public Setting<Easings> NewFilePopupRotCloseEase { get; set; }

        #endregion

        #region Save As Popup

        public Setting<bool> SaveAsPopupActive { get; set; }

        public Setting<bool> SaveAsPopupPosActive { get; set; }
        public Setting<Vector2> SaveAsPopupPosOpen { get; set; }
        public Setting<Vector2> SaveAsPopupPosClose { get; set; }
        public Setting<Vector2> SaveAsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> SaveAsPopupPosCloseDuration { get; set; }
        public Setting<Easings> SaveAsPopupPosXOpenEase { get; set; }
        public Setting<Easings> SaveAsPopupPosXCloseEase { get; set; }
        public Setting<Easings> SaveAsPopupPosYOpenEase { get; set; }
        public Setting<Easings> SaveAsPopupPosYCloseEase { get; set; }

        public Setting<bool> SaveAsPopupScaActive { get; set; }
        public Setting<Vector2> SaveAsPopupScaOpen { get; set; }
        public Setting<Vector2> SaveAsPopupScaClose { get; set; }
        public Setting<Vector2> SaveAsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> SaveAsPopupScaCloseDuration { get; set; }
        public Setting<Easings> SaveAsPopupScaXOpenEase { get; set; }
        public Setting<Easings> SaveAsPopupScaXCloseEase { get; set; }
        public Setting<Easings> SaveAsPopupScaYOpenEase { get; set; }
        public Setting<Easings> SaveAsPopupScaYCloseEase { get; set; }

        public Setting<bool> SaveAsPopupRotActive { get; set; }
        public Setting<float> SaveAsPopupRotOpen { get; set; }
        public Setting<float> SaveAsPopupRotClose { get; set; }
        public Setting<float> SaveAsPopupRotOpenDuration { get; set; }
        public Setting<float> SaveAsPopupRotCloseDuration { get; set; }
        public Setting<Easings> SaveAsPopupRotOpenEase { get; set; }
        public Setting<Easings> SaveAsPopupRotCloseEase { get; set; }

        #endregion

        #region Quick Actions Popup

        public Setting<bool> QuickActionsPopupActive { get; set; }

        public Setting<bool> QuickActionsPopupPosActive { get; set; }
        public Setting<Vector2> QuickActionsPopupPosOpen { get; set; }
        public Setting<Vector2> QuickActionsPopupPosClose { get; set; }
        public Setting<Vector2> QuickActionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> QuickActionsPopupPosCloseDuration { get; set; }
        public Setting<Easings> QuickActionsPopupPosXOpenEase { get; set; }
        public Setting<Easings> QuickActionsPopupPosXCloseEase { get; set; }
        public Setting<Easings> QuickActionsPopupPosYOpenEase { get; set; }
        public Setting<Easings> QuickActionsPopupPosYCloseEase { get; set; }

        public Setting<bool> QuickActionsPopupScaActive { get; set; }
        public Setting<Vector2> QuickActionsPopupScaOpen { get; set; }
        public Setting<Vector2> QuickActionsPopupScaClose { get; set; }
        public Setting<Vector2> QuickActionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> QuickActionsPopupScaCloseDuration { get; set; }
        public Setting<Easings> QuickActionsPopupScaXOpenEase { get; set; }
        public Setting<Easings> QuickActionsPopupScaXCloseEase { get; set; }
        public Setting<Easings> QuickActionsPopupScaYOpenEase { get; set; }
        public Setting<Easings> QuickActionsPopupScaYCloseEase { get; set; }

        public Setting<bool> QuickActionsPopupRotActive { get; set; }
        public Setting<float> QuickActionsPopupRotOpen { get; set; }
        public Setting<float> QuickActionsPopupRotClose { get; set; }
        public Setting<float> QuickActionsPopupRotOpenDuration { get; set; }
        public Setting<float> QuickActionsPopupRotCloseDuration { get; set; }
        public Setting<Easings> QuickActionsPopupRotOpenEase { get; set; }
        public Setting<Easings> QuickActionsPopupRotCloseEase { get; set; }

        #endregion

        #region Parent Selector Popup

        public Setting<bool> ParentSelectorPopupActive { get; set; }

        public Setting<bool> ParentSelectorPopupPosActive { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosOpen { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosClose { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosCloseDuration { get; set; }
        public Setting<Easings> ParentSelectorPopupPosXOpenEase { get; set; }
        public Setting<Easings> ParentSelectorPopupPosXCloseEase { get; set; }
        public Setting<Easings> ParentSelectorPopupPosYOpenEase { get; set; }
        public Setting<Easings> ParentSelectorPopupPosYCloseEase { get; set; }

        public Setting<bool> ParentSelectorPopupScaActive { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaOpen { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaClose { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaCloseDuration { get; set; }
        public Setting<Easings> ParentSelectorPopupScaXOpenEase { get; set; }
        public Setting<Easings> ParentSelectorPopupScaXCloseEase { get; set; }
        public Setting<Easings> ParentSelectorPopupScaYOpenEase { get; set; }
        public Setting<Easings> ParentSelectorPopupScaYCloseEase { get; set; }

        public Setting<bool> ParentSelectorPopupRotActive { get; set; }
        public Setting<float> ParentSelectorPopupRotOpen { get; set; }
        public Setting<float> ParentSelectorPopupRotClose { get; set; }
        public Setting<float> ParentSelectorPopupRotOpenDuration { get; set; }
        public Setting<float> ParentSelectorPopupRotCloseDuration { get; set; }
        public Setting<Easings> ParentSelectorPopupRotOpenEase { get; set; }
        public Setting<Easings> ParentSelectorPopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Popup

        public Setting<bool> PrefabPopupActive { get; set; }

        public Setting<bool> PrefabPopupPosActive { get; set; }
        public Setting<Vector2> PrefabPopupPosOpen { get; set; }
        public Setting<Vector2> PrefabPopupPosClose { get; set; }
        public Setting<Vector2> PrefabPopupPosOpenDuration { get; set; }
        public Setting<Vector2> PrefabPopupPosCloseDuration { get; set; }
        public Setting<Easings> PrefabPopupPosXOpenEase { get; set; }
        public Setting<Easings> PrefabPopupPosXCloseEase { get; set; }
        public Setting<Easings> PrefabPopupPosYOpenEase { get; set; }
        public Setting<Easings> PrefabPopupPosYCloseEase { get; set; }

        public Setting<bool> PrefabPopupScaActive { get; set; }
        public Setting<Vector2> PrefabPopupScaOpen { get; set; }
        public Setting<Vector2> PrefabPopupScaClose { get; set; }
        public Setting<Vector2> PrefabPopupScaOpenDuration { get; set; }
        public Setting<Vector2> PrefabPopupScaCloseDuration { get; set; }
        public Setting<Easings> PrefabPopupScaXOpenEase { get; set; }
        public Setting<Easings> PrefabPopupScaXCloseEase { get; set; }
        public Setting<Easings> PrefabPopupScaYOpenEase { get; set; }
        public Setting<Easings> PrefabPopupScaYCloseEase { get; set; }

        public Setting<bool> PrefabPopupRotActive { get; set; }
        public Setting<float> PrefabPopupRotOpen { get; set; }
        public Setting<float> PrefabPopupRotClose { get; set; }
        public Setting<float> PrefabPopupRotOpenDuration { get; set; }
        public Setting<float> PrefabPopupRotCloseDuration { get; set; }
        public Setting<Easings> PrefabPopupRotOpenEase { get; set; }
        public Setting<Easings> PrefabPopupRotCloseEase { get; set; }

        #endregion

        #region Object Options Popup

        public Setting<bool> ObjectOptionsPopupActive { get; set; }

        public Setting<bool> ObjectOptionsPopupPosActive { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosOpen { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosClose { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosCloseDuration { get; set; }
        public Setting<Easings> ObjectOptionsPopupPosXOpenEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupPosXCloseEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupPosYOpenEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectOptionsPopupScaActive { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaOpen { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaClose { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaCloseDuration { get; set; }
        public Setting<Easings> ObjectOptionsPopupScaXOpenEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupScaXCloseEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupScaYOpenEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectOptionsPopupRotActive { get; set; }
        public Setting<float> ObjectOptionsPopupRotOpen { get; set; }
        public Setting<float> ObjectOptionsPopupRotClose { get; set; }
        public Setting<float> ObjectOptionsPopupRotOpenDuration { get; set; }
        public Setting<float> ObjectOptionsPopupRotCloseDuration { get; set; }
        public Setting<Easings> ObjectOptionsPopupRotOpenEase { get; set; }
        public Setting<Easings> ObjectOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region BG Options Popup

        public Setting<bool> BGOptionsPopupActive { get; set; }

        public Setting<bool> BGOptionsPopupPosActive { get; set; }
        public Setting<Vector2> BGOptionsPopupPosOpen { get; set; }
        public Setting<Vector2> BGOptionsPopupPosClose { get; set; }
        public Setting<Vector2> BGOptionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> BGOptionsPopupPosCloseDuration { get; set; }
        public Setting<Easings> BGOptionsPopupPosXOpenEase { get; set; }
        public Setting<Easings> BGOptionsPopupPosXCloseEase { get; set; }
        public Setting<Easings> BGOptionsPopupPosYOpenEase { get; set; }
        public Setting<Easings> BGOptionsPopupPosYCloseEase { get; set; }

        public Setting<bool> BGOptionsPopupScaActive { get; set; }
        public Setting<Vector2> BGOptionsPopupScaOpen { get; set; }
        public Setting<Vector2> BGOptionsPopupScaClose { get; set; }
        public Setting<Vector2> BGOptionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> BGOptionsPopupScaCloseDuration { get; set; }
        public Setting<Easings> BGOptionsPopupScaXOpenEase { get; set; }
        public Setting<Easings> BGOptionsPopupScaXCloseEase { get; set; }
        public Setting<Easings> BGOptionsPopupScaYOpenEase { get; set; }
        public Setting<Easings> BGOptionsPopupScaYCloseEase { get; set; }

        public Setting<bool> BGOptionsPopupRotActive { get; set; }
        public Setting<float> BGOptionsPopupRotOpen { get; set; }
        public Setting<float> BGOptionsPopupRotClose { get; set; }
        public Setting<float> BGOptionsPopupRotOpenDuration { get; set; }
        public Setting<float> BGOptionsPopupRotCloseDuration { get; set; }
        public Setting<Easings> BGOptionsPopupRotOpenEase { get; set; }
        public Setting<Easings> BGOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region Browser Popup

        public Setting<bool> BrowserPopupActive { get; set; }

        public Setting<bool> BrowserPopupPosActive { get; set; }
        public Setting<Vector2> BrowserPopupPosOpen { get; set; }
        public Setting<Vector2> BrowserPopupPosClose { get; set; }
        public Setting<Vector2> BrowserPopupPosOpenDuration { get; set; }
        public Setting<Vector2> BrowserPopupPosCloseDuration { get; set; }
        public Setting<Easings> BrowserPopupPosXOpenEase { get; set; }
        public Setting<Easings> BrowserPopupPosXCloseEase { get; set; }
        public Setting<Easings> BrowserPopupPosYOpenEase { get; set; }
        public Setting<Easings> BrowserPopupPosYCloseEase { get; set; }

        public Setting<bool> BrowserPopupScaActive { get; set; }
        public Setting<Vector2> BrowserPopupScaOpen { get; set; }
        public Setting<Vector2> BrowserPopupScaClose { get; set; }
        public Setting<Vector2> BrowserPopupScaOpenDuration { get; set; }
        public Setting<Vector2> BrowserPopupScaCloseDuration { get; set; }
        public Setting<Easings> BrowserPopupScaXOpenEase { get; set; }
        public Setting<Easings> BrowserPopupScaXCloseEase { get; set; }
        public Setting<Easings> BrowserPopupScaYOpenEase { get; set; }
        public Setting<Easings> BrowserPopupScaYCloseEase { get; set; }

        public Setting<bool> BrowserPopupRotActive { get; set; }
        public Setting<float> BrowserPopupRotOpen { get; set; }
        public Setting<float> BrowserPopupRotClose { get; set; }
        public Setting<float> BrowserPopupRotOpenDuration { get; set; }
        public Setting<float> BrowserPopupRotCloseDuration { get; set; }
        public Setting<Easings> BrowserPopupRotOpenEase { get; set; }
        public Setting<Easings> BrowserPopupRotCloseEase { get; set; }

        #endregion

        #region Object Search Popup

        public Setting<bool> ObjectSearchPopupActive { get; set; }

        public Setting<bool> ObjectSearchPopupPosActive { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosOpen { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosClose { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosCloseDuration { get; set; }
        public Setting<Easings> ObjectSearchPopupPosXOpenEase { get; set; }
        public Setting<Easings> ObjectSearchPopupPosXCloseEase { get; set; }
        public Setting<Easings> ObjectSearchPopupPosYOpenEase { get; set; }
        public Setting<Easings> ObjectSearchPopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectSearchPopupScaActive { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaOpen { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaClose { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaCloseDuration { get; set; }
        public Setting<Easings> ObjectSearchPopupScaXOpenEase { get; set; }
        public Setting<Easings> ObjectSearchPopupScaXCloseEase { get; set; }
        public Setting<Easings> ObjectSearchPopupScaYOpenEase { get; set; }
        public Setting<Easings> ObjectSearchPopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectSearchPopupRotActive { get; set; }
        public Setting<float> ObjectSearchPopupRotOpen { get; set; }
        public Setting<float> ObjectSearchPopupRotClose { get; set; }
        public Setting<float> ObjectSearchPopupRotOpenDuration { get; set; }
        public Setting<float> ObjectSearchPopupRotCloseDuration { get; set; }
        public Setting<Easings> ObjectSearchPopupRotOpenEase { get; set; }
        public Setting<Easings> ObjectSearchPopupRotCloseEase { get; set; }

        #endregion

        #region Object Template Popup

        public Setting<bool> ObjectTemplatePopupActive { get; set; }

        public Setting<bool> ObjectTemplatePopupPosActive { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosOpen { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosClose { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosCloseDuration { get; set; }
        public Setting<Easings> ObjectTemplatePopupPosXOpenEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupPosXCloseEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupPosYOpenEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectTemplatePopupScaActive { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaOpen { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaClose { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaCloseDuration { get; set; }
        public Setting<Easings> ObjectTemplatePopupScaXOpenEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupScaXCloseEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupScaYOpenEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectTemplatePopupRotActive { get; set; }
        public Setting<float> ObjectTemplatePopupRotOpen { get; set; }
        public Setting<float> ObjectTemplatePopupRotClose { get; set; }
        public Setting<float> ObjectTemplatePopupRotOpenDuration { get; set; }
        public Setting<float> ObjectTemplatePopupRotCloseDuration { get; set; }
        public Setting<Easings> ObjectTemplatePopupRotOpenEase { get; set; }
        public Setting<Easings> ObjectTemplatePopupRotCloseEase { get; set; }

        #endregion

        #region Warning Popup

        public Setting<bool> WarningPopupActive { get; set; }

        public Setting<bool> WarningPopupPosActive { get; set; }
        public Setting<Vector2> WarningPopupPosOpen { get; set; }
        public Setting<Vector2> WarningPopupPosClose { get; set; }
        public Setting<Vector2> WarningPopupPosOpenDuration { get; set; }
        public Setting<Vector2> WarningPopupPosCloseDuration { get; set; }
        public Setting<Easings> WarningPopupPosXOpenEase { get; set; }
        public Setting<Easings> WarningPopupPosXCloseEase { get; set; }
        public Setting<Easings> WarningPopupPosYOpenEase { get; set; }
        public Setting<Easings> WarningPopupPosYCloseEase { get; set; }

        public Setting<bool> WarningPopupScaActive { get; set; }
        public Setting<Vector2> WarningPopupScaOpen { get; set; }
        public Setting<Vector2> WarningPopupScaClose { get; set; }
        public Setting<Vector2> WarningPopupScaOpenDuration { get; set; }
        public Setting<Vector2> WarningPopupScaCloseDuration { get; set; }
        public Setting<Easings> WarningPopupScaXOpenEase { get; set; }
        public Setting<Easings> WarningPopupScaXCloseEase { get; set; }
        public Setting<Easings> WarningPopupScaYOpenEase { get; set; }
        public Setting<Easings> WarningPopupScaYCloseEase { get; set; }

        public Setting<bool> WarningPopupRotActive { get; set; }
        public Setting<float> WarningPopupRotOpen { get; set; }
        public Setting<float> WarningPopupRotClose { get; set; }
        public Setting<float> WarningPopupRotOpenDuration { get; set; }
        public Setting<float> WarningPopupRotCloseDuration { get; set; }
        public Setting<Easings> WarningPopupRotOpenEase { get; set; }
        public Setting<Easings> WarningPopupRotCloseEase { get; set; }

        #endregion

        #region File Popup

        public Setting<bool> FilePopupActive { get; set; }

        public Setting<bool> FilePopupPosActive { get; set; }
        public Setting<Vector2> FilePopupPosOpen { get; set; }
        public Setting<Vector2> FilePopupPosClose { get; set; }
        public Setting<Vector2> FilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> FilePopupPosCloseDuration { get; set; }
        public Setting<Easings> FilePopupPosXOpenEase { get; set; }
        public Setting<Easings> FilePopupPosXCloseEase { get; set; }
        public Setting<Easings> FilePopupPosYOpenEase { get; set; }
        public Setting<Easings> FilePopupPosYCloseEase { get; set; }

        public Setting<bool> FilePopupScaActive { get; set; }
        public Setting<Vector2> FilePopupScaOpen { get; set; }
        public Setting<Vector2> FilePopupScaClose { get; set; }
        public Setting<Vector2> FilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> FilePopupScaCloseDuration { get; set; }
        public Setting<Easings> FilePopupScaXOpenEase { get; set; }
        public Setting<Easings> FilePopupScaXCloseEase { get; set; }
        public Setting<Easings> FilePopupScaYOpenEase { get; set; }
        public Setting<Easings> FilePopupScaYCloseEase { get; set; }

        public Setting<bool> FilePopupRotActive { get; set; }
        public Setting<float> FilePopupRotOpen { get; set; }
        public Setting<float> FilePopupRotClose { get; set; }
        public Setting<float> FilePopupRotOpenDuration { get; set; }
        public Setting<float> FilePopupRotCloseDuration { get; set; }
        public Setting<Easings> FilePopupRotOpenEase { get; set; }
        public Setting<Easings> FilePopupRotCloseEase { get; set; }

        #endregion

        #region Text Editor

        public Setting<bool> TextEditorActive { get; set; }

        public Setting<bool> TextEditorPosActive { get; set; }
        public Setting<Vector2> TextEditorPosOpen { get; set; }
        public Setting<Vector2> TextEditorPosClose { get; set; }
        public Setting<Vector2> TextEditorPosOpenDuration { get; set; }
        public Setting<Vector2> TextEditorPosCloseDuration { get; set; }
        public Setting<Easings> TextEditorPosXOpenEase { get; set; }
        public Setting<Easings> TextEditorPosXCloseEase { get; set; }
        public Setting<Easings> TextEditorPosYOpenEase { get; set; }
        public Setting<Easings> TextEditorPosYCloseEase { get; set; }

        public Setting<bool> TextEditorScaActive { get; set; }
        public Setting<Vector2> TextEditorScaOpen { get; set; }
        public Setting<Vector2> TextEditorScaClose { get; set; }
        public Setting<Vector2> TextEditorScaOpenDuration { get; set; }
        public Setting<Vector2> TextEditorScaCloseDuration { get; set; }
        public Setting<Easings> TextEditorScaXOpenEase { get; set; }
        public Setting<Easings> TextEditorScaXCloseEase { get; set; }
        public Setting<Easings> TextEditorScaYOpenEase { get; set; }
        public Setting<Easings> TextEditorScaYCloseEase { get; set; }

        public Setting<bool> TextEditorRotActive { get; set; }
        public Setting<float> TextEditorRotOpen { get; set; }
        public Setting<float> TextEditorRotClose { get; set; }
        public Setting<float> TextEditorRotOpenDuration { get; set; }
        public Setting<float> TextEditorRotCloseDuration { get; set; }
        public Setting<Easings> TextEditorRotOpenEase { get; set; }
        public Setting<Easings> TextEditorRotCloseEase { get; set; }

        #endregion

        #region Documentation Popup

        public Setting<bool> DocumentationPopupActive { get; set; }

        public Setting<bool> DocumentationPopupPosActive { get; set; }
        public Setting<Vector2> DocumentationPopupPosOpen { get; set; }
        public Setting<Vector2> DocumentationPopupPosClose { get; set; }
        public Setting<Vector2> DocumentationPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DocumentationPopupPosCloseDuration { get; set; }
        public Setting<Easings> DocumentationPopupPosXOpenEase { get; set; }
        public Setting<Easings> DocumentationPopupPosXCloseEase { get; set; }
        public Setting<Easings> DocumentationPopupPosYOpenEase { get; set; }
        public Setting<Easings> DocumentationPopupPosYCloseEase { get; set; }

        public Setting<bool> DocumentationPopupScaActive { get; set; }
        public Setting<Vector2> DocumentationPopupScaOpen { get; set; }
        public Setting<Vector2> DocumentationPopupScaClose { get; set; }
        public Setting<Vector2> DocumentationPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DocumentationPopupScaCloseDuration { get; set; }
        public Setting<Easings> DocumentationPopupScaXOpenEase { get; set; }
        public Setting<Easings> DocumentationPopupScaXCloseEase { get; set; }
        public Setting<Easings> DocumentationPopupScaYOpenEase { get; set; }
        public Setting<Easings> DocumentationPopupScaYCloseEase { get; set; }

        public Setting<bool> DocumentationPopupRotActive { get; set; }
        public Setting<float> DocumentationPopupRotOpen { get; set; }
        public Setting<float> DocumentationPopupRotClose { get; set; }
        public Setting<float> DocumentationPopupRotOpenDuration { get; set; }
        public Setting<float> DocumentationPopupRotCloseDuration { get; set; }
        public Setting<Easings> DocumentationPopupRotOpenEase { get; set; }
        public Setting<Easings> DocumentationPopupRotCloseEase { get; set; }

        #endregion

        #region Debugger Popup

        public Setting<bool> DebuggerPopupActive { get; set; }

        public Setting<bool> DebuggerPopupPosActive { get; set; }
        public Setting<Vector2> DebuggerPopupPosOpen { get; set; }
        public Setting<Vector2> DebuggerPopupPosClose { get; set; }
        public Setting<Vector2> DebuggerPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DebuggerPopupPosCloseDuration { get; set; }
        public Setting<Easings> DebuggerPopupPosXOpenEase { get; set; }
        public Setting<Easings> DebuggerPopupPosXCloseEase { get; set; }
        public Setting<Easings> DebuggerPopupPosYOpenEase { get; set; }
        public Setting<Easings> DebuggerPopupPosYCloseEase { get; set; }

        public Setting<bool> DebuggerPopupScaActive { get; set; }
        public Setting<Vector2> DebuggerPopupScaOpen { get; set; }
        public Setting<Vector2> DebuggerPopupScaClose { get; set; }
        public Setting<Vector2> DebuggerPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DebuggerPopupScaCloseDuration { get; set; }
        public Setting<Easings> DebuggerPopupScaXOpenEase { get; set; }
        public Setting<Easings> DebuggerPopupScaXCloseEase { get; set; }
        public Setting<Easings> DebuggerPopupScaYOpenEase { get; set; }
        public Setting<Easings> DebuggerPopupScaYCloseEase { get; set; }

        public Setting<bool> DebuggerPopupRotActive { get; set; }
        public Setting<float> DebuggerPopupRotOpen { get; set; }
        public Setting<float> DebuggerPopupRotClose { get; set; }
        public Setting<float> DebuggerPopupRotOpenDuration { get; set; }
        public Setting<float> DebuggerPopupRotCloseDuration { get; set; }
        public Setting<Easings> DebuggerPopupRotOpenEase { get; set; }
        public Setting<Easings> DebuggerPopupRotCloseEase { get; set; }

        #endregion

        #region Autosaves Popup

        public Setting<bool> AutosavesPopupActive { get; set; }

        public Setting<bool> AutosavesPopupPosActive { get; set; }
        public Setting<Vector2> AutosavesPopupPosOpen { get; set; }
        public Setting<Vector2> AutosavesPopupPosClose { get; set; }
        public Setting<Vector2> AutosavesPopupPosOpenDuration { get; set; }
        public Setting<Vector2> AutosavesPopupPosCloseDuration { get; set; }
        public Setting<Easings> AutosavesPopupPosXOpenEase { get; set; }
        public Setting<Easings> AutosavesPopupPosXCloseEase { get; set; }
        public Setting<Easings> AutosavesPopupPosYOpenEase { get; set; }
        public Setting<Easings> AutosavesPopupPosYCloseEase { get; set; }

        public Setting<bool> AutosavesPopupScaActive { get; set; }
        public Setting<Vector2> AutosavesPopupScaOpen { get; set; }
        public Setting<Vector2> AutosavesPopupScaClose { get; set; }
        public Setting<Vector2> AutosavesPopupScaOpenDuration { get; set; }
        public Setting<Vector2> AutosavesPopupScaCloseDuration { get; set; }
        public Setting<Easings> AutosavesPopupScaXOpenEase { get; set; }
        public Setting<Easings> AutosavesPopupScaXCloseEase { get; set; }
        public Setting<Easings> AutosavesPopupScaYOpenEase { get; set; }
        public Setting<Easings> AutosavesPopupScaYCloseEase { get; set; }

        public Setting<bool> AutosavesPopupRotActive { get; set; }
        public Setting<float> AutosavesPopupRotOpen { get; set; }
        public Setting<float> AutosavesPopupRotClose { get; set; }
        public Setting<float> AutosavesPopupRotOpenDuration { get; set; }
        public Setting<float> AutosavesPopupRotCloseDuration { get; set; }
        public Setting<Easings> AutosavesPopupRotOpenEase { get; set; }
        public Setting<Easings> AutosavesPopupRotCloseEase { get; set; }

        #endregion

        #region Default Modifiers Popup

        public Setting<bool> DefaultModifiersPopupActive { get; set; }

        public Setting<bool> DefaultModifiersPopupPosActive { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosOpen { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosClose { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosCloseDuration { get; set; }
        public Setting<Easings> DefaultModifiersPopupPosXOpenEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupPosXCloseEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupPosYOpenEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupPosYCloseEase { get; set; }

        public Setting<bool> DefaultModifiersPopupScaActive { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaOpen { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaClose { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaCloseDuration { get; set; }
        public Setting<Easings> DefaultModifiersPopupScaXOpenEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupScaXCloseEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupScaYOpenEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupScaYCloseEase { get; set; }

        public Setting<bool> DefaultModifiersPopupRotActive { get; set; }
        public Setting<float> DefaultModifiersPopupRotOpen { get; set; }
        public Setting<float> DefaultModifiersPopupRotClose { get; set; }
        public Setting<float> DefaultModifiersPopupRotOpenDuration { get; set; }
        public Setting<float> DefaultModifiersPopupRotCloseDuration { get; set; }
        public Setting<Easings> DefaultModifiersPopupRotOpenEase { get; set; }
        public Setting<Easings> DefaultModifiersPopupRotCloseEase { get; set; }

        #endregion

        #region Keybind List Popup

        public Setting<bool> KeybindListPopupActive { get; set; }

        public Setting<bool> KeybindListPopupPosActive { get; set; }
        public Setting<Vector2> KeybindListPopupPosOpen { get; set; }
        public Setting<Vector2> KeybindListPopupPosClose { get; set; }
        public Setting<Vector2> KeybindListPopupPosOpenDuration { get; set; }
        public Setting<Vector2> KeybindListPopupPosCloseDuration { get; set; }
        public Setting<Easings> KeybindListPopupPosXOpenEase { get; set; }
        public Setting<Easings> KeybindListPopupPosXCloseEase { get; set; }
        public Setting<Easings> KeybindListPopupPosYOpenEase { get; set; }
        public Setting<Easings> KeybindListPopupPosYCloseEase { get; set; }

        public Setting<bool> KeybindListPopupScaActive { get; set; }
        public Setting<Vector2> KeybindListPopupScaOpen { get; set; }
        public Setting<Vector2> KeybindListPopupScaClose { get; set; }
        public Setting<Vector2> KeybindListPopupScaOpenDuration { get; set; }
        public Setting<Vector2> KeybindListPopupScaCloseDuration { get; set; }
        public Setting<Easings> KeybindListPopupScaXOpenEase { get; set; }
        public Setting<Easings> KeybindListPopupScaXCloseEase { get; set; }
        public Setting<Easings> KeybindListPopupScaYOpenEase { get; set; }
        public Setting<Easings> KeybindListPopupScaYCloseEase { get; set; }

        public Setting<bool> KeybindListPopupRotActive { get; set; }
        public Setting<float> KeybindListPopupRotOpen { get; set; }
        public Setting<float> KeybindListPopupRotClose { get; set; }
        public Setting<float> KeybindListPopupRotOpenDuration { get; set; }
        public Setting<float> KeybindListPopupRotCloseDuration { get; set; }
        public Setting<Easings> KeybindListPopupRotOpenEase { get; set; }
        public Setting<Easings> KeybindListPopupRotCloseEase { get; set; }

        #endregion

        #region Theme Popup

        public Setting<bool> ThemePopupActive { get; set; }

        public Setting<bool> ThemePopupPosActive { get; set; }
        public Setting<Vector2> ThemePopupPosOpen { get; set; }
        public Setting<Vector2> ThemePopupPosClose { get; set; }
        public Setting<Vector2> ThemePopupPosOpenDuration { get; set; }
        public Setting<Vector2> ThemePopupPosCloseDuration { get; set; }
        public Setting<Easings> ThemePopupPosXOpenEase { get; set; }
        public Setting<Easings> ThemePopupPosXCloseEase { get; set; }
        public Setting<Easings> ThemePopupPosYOpenEase { get; set; }
        public Setting<Easings> ThemePopupPosYCloseEase { get; set; }

        public Setting<bool> ThemePopupScaActive { get; set; }
        public Setting<Vector2> ThemePopupScaOpen { get; set; }
        public Setting<Vector2> ThemePopupScaClose { get; set; }
        public Setting<Vector2> ThemePopupScaOpenDuration { get; set; }
        public Setting<Vector2> ThemePopupScaCloseDuration { get; set; }
        public Setting<Easings> ThemePopupScaXOpenEase { get; set; }
        public Setting<Easings> ThemePopupScaXCloseEase { get; set; }
        public Setting<Easings> ThemePopupScaYOpenEase { get; set; }
        public Setting<Easings> ThemePopupScaYCloseEase { get; set; }

        public Setting<bool> ThemePopupRotActive { get; set; }
        public Setting<float> ThemePopupRotOpen { get; set; }
        public Setting<float> ThemePopupRotClose { get; set; }
        public Setting<float> ThemePopupRotOpenDuration { get; set; }
        public Setting<float> ThemePopupRotCloseDuration { get; set; }
        public Setting<Easings> ThemePopupRotOpenEase { get; set; }
        public Setting<Easings> ThemePopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Types Popup

        public Setting<bool> PrefabTypesPopupActive { get; set; }

        public Setting<bool> PrefabTypesPopupPosActive { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosOpen { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosClose { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosOpenDuration { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosCloseDuration { get; set; }
        public Setting<Easings> PrefabTypesPopupPosXOpenEase { get; set; }
        public Setting<Easings> PrefabTypesPopupPosXCloseEase { get; set; }
        public Setting<Easings> PrefabTypesPopupPosYOpenEase { get; set; }
        public Setting<Easings> PrefabTypesPopupPosYCloseEase { get; set; }

        public Setting<bool> PrefabTypesPopupScaActive { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaOpen { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaClose { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaOpenDuration { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaCloseDuration { get; set; }
        public Setting<Easings> PrefabTypesPopupScaXOpenEase { get; set; }
        public Setting<Easings> PrefabTypesPopupScaXCloseEase { get; set; }
        public Setting<Easings> PrefabTypesPopupScaYOpenEase { get; set; }
        public Setting<Easings> PrefabTypesPopupScaYCloseEase { get; set; }

        public Setting<bool> PrefabTypesPopupRotActive { get; set; }
        public Setting<float> PrefabTypesPopupRotOpen { get; set; }
        public Setting<float> PrefabTypesPopupRotClose { get; set; }
        public Setting<float> PrefabTypesPopupRotOpenDuration { get; set; }
        public Setting<float> PrefabTypesPopupRotCloseDuration { get; set; }
        public Setting<Easings> PrefabTypesPopupRotOpenEase { get; set; }
        public Setting<Easings> PrefabTypesPopupRotCloseEase { get; set; }

        #endregion

        #region Font Selector Popup

        public Setting<bool> FontSelectorPopupActive { get; set; }

        public Setting<bool> FontSelectorPopupPosActive { get; set; }
        public Setting<Vector2> FontSelectorPopupPosOpen { get; set; }
        public Setting<Vector2> FontSelectorPopupPosClose { get; set; }
        public Setting<Vector2> FontSelectorPopupPosOpenDuration { get; set; }
        public Setting<Vector2> FontSelectorPopupPosCloseDuration { get; set; }
        public Setting<Easings> FontSelectorPopupPosXOpenEase { get; set; }
        public Setting<Easings> FontSelectorPopupPosXCloseEase { get; set; }
        public Setting<Easings> FontSelectorPopupPosYOpenEase { get; set; }
        public Setting<Easings> FontSelectorPopupPosYCloseEase { get; set; }

        public Setting<bool> FontSelectorPopupScaActive { get; set; }
        public Setting<Vector2> FontSelectorPopupScaOpen { get; set; }
        public Setting<Vector2> FontSelectorPopupScaClose { get; set; }
        public Setting<Vector2> FontSelectorPopupScaOpenDuration { get; set; }
        public Setting<Vector2> FontSelectorPopupScaCloseDuration { get; set; }
        public Setting<Easings> FontSelectorPopupScaXOpenEase { get; set; }
        public Setting<Easings> FontSelectorPopupScaXCloseEase { get; set; }
        public Setting<Easings> FontSelectorPopupScaYOpenEase { get; set; }
        public Setting<Easings> FontSelectorPopupScaYCloseEase { get; set; }

        public Setting<bool> FontSelectorPopupRotActive { get; set; }
        public Setting<float> FontSelectorPopupRotOpen { get; set; }
        public Setting<float> FontSelectorPopupRotClose { get; set; }
        public Setting<float> FontSelectorPopupRotOpenDuration { get; set; }
        public Setting<float> FontSelectorPopupRotCloseDuration { get; set; }
        public Setting<Easings> FontSelectorPopupRotOpenEase { get; set; }
        public Setting<Easings> FontSelectorPopupRotCloseEase { get; set; }

        #endregion

        #region File Dropdown

        public Setting<bool> FileDropdownActive { get; set; }

        public Setting<bool> FileDropdownPosActive { get; set; }
        public Setting<Vector2> FileDropdownPosOpen { get; set; }
        public Setting<Vector2> FileDropdownPosClose { get; set; }
        public Setting<Vector2> FileDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> FileDropdownPosCloseDuration { get; set; }
        public Setting<Easings> FileDropdownPosXOpenEase { get; set; }
        public Setting<Easings> FileDropdownPosXCloseEase { get; set; }
        public Setting<Easings> FileDropdownPosYOpenEase { get; set; }
        public Setting<Easings> FileDropdownPosYCloseEase { get; set; }

        public Setting<bool> FileDropdownScaActive { get; set; }
        public Setting<Vector2> FileDropdownScaOpen { get; set; }
        public Setting<Vector2> FileDropdownScaClose { get; set; }
        public Setting<Vector2> FileDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> FileDropdownScaCloseDuration { get; set; }
        public Setting<Easings> FileDropdownScaXOpenEase { get; set; }
        public Setting<Easings> FileDropdownScaXCloseEase { get; set; }
        public Setting<Easings> FileDropdownScaYOpenEase { get; set; }
        public Setting<Easings> FileDropdownScaYCloseEase { get; set; }

        public Setting<bool> FileDropdownRotActive { get; set; }
        public Setting<float> FileDropdownRotOpen { get; set; }
        public Setting<float> FileDropdownRotClose { get; set; }
        public Setting<float> FileDropdownRotOpenDuration { get; set; }
        public Setting<float> FileDropdownRotCloseDuration { get; set; }
        public Setting<Easings> FileDropdownRotOpenEase { get; set; }
        public Setting<Easings> FileDropdownRotCloseEase { get; set; }

        #endregion

        #region Edit Dropdown

        public Setting<bool> EditDropdownActive { get; set; }

        public Setting<bool> EditDropdownPosActive { get; set; }
        public Setting<Vector2> EditDropdownPosOpen { get; set; }
        public Setting<Vector2> EditDropdownPosClose { get; set; }
        public Setting<Vector2> EditDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> EditDropdownPosCloseDuration { get; set; }
        public Setting<Easings> EditDropdownPosXOpenEase { get; set; }
        public Setting<Easings> EditDropdownPosXCloseEase { get; set; }
        public Setting<Easings> EditDropdownPosYOpenEase { get; set; }
        public Setting<Easings> EditDropdownPosYCloseEase { get; set; }

        public Setting<bool> EditDropdownScaActive { get; set; }
        public Setting<Vector2> EditDropdownScaOpen { get; set; }
        public Setting<Vector2> EditDropdownScaClose { get; set; }
        public Setting<Vector2> EditDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> EditDropdownScaCloseDuration { get; set; }
        public Setting<Easings> EditDropdownScaXOpenEase { get; set; }
        public Setting<Easings> EditDropdownScaXCloseEase { get; set; }
        public Setting<Easings> EditDropdownScaYOpenEase { get; set; }
        public Setting<Easings> EditDropdownScaYCloseEase { get; set; }

        public Setting<bool> EditDropdownRotActive { get; set; }
        public Setting<float> EditDropdownRotOpen { get; set; }
        public Setting<float> EditDropdownRotClose { get; set; }
        public Setting<float> EditDropdownRotOpenDuration { get; set; }
        public Setting<float> EditDropdownRotCloseDuration { get; set; }
        public Setting<Easings> EditDropdownRotOpenEase { get; set; }
        public Setting<Easings> EditDropdownRotCloseEase { get; set; }

        #endregion

        #region View Dropdown

        public Setting<bool> ViewDropdownActive { get; set; }

        public Setting<bool> ViewDropdownPosActive { get; set; }
        public Setting<Vector2> ViewDropdownPosOpen { get; set; }
        public Setting<Vector2> ViewDropdownPosClose { get; set; }
        public Setting<Vector2> ViewDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> ViewDropdownPosCloseDuration { get; set; }
        public Setting<Easings> ViewDropdownPosXOpenEase { get; set; }
        public Setting<Easings> ViewDropdownPosXCloseEase { get; set; }
        public Setting<Easings> ViewDropdownPosYOpenEase { get; set; }
        public Setting<Easings> ViewDropdownPosYCloseEase { get; set; }

        public Setting<bool> ViewDropdownScaActive { get; set; }
        public Setting<Vector2> ViewDropdownScaOpen { get; set; }
        public Setting<Vector2> ViewDropdownScaClose { get; set; }
        public Setting<Vector2> ViewDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> ViewDropdownScaCloseDuration { get; set; }
        public Setting<Easings> ViewDropdownScaXOpenEase { get; set; }
        public Setting<Easings> ViewDropdownScaXCloseEase { get; set; }
        public Setting<Easings> ViewDropdownScaYOpenEase { get; set; }
        public Setting<Easings> ViewDropdownScaYCloseEase { get; set; }

        public Setting<bool> ViewDropdownRotActive { get; set; }
        public Setting<float> ViewDropdownRotOpen { get; set; }
        public Setting<float> ViewDropdownRotClose { get; set; }
        public Setting<float> ViewDropdownRotOpenDuration { get; set; }
        public Setting<float> ViewDropdownRotCloseDuration { get; set; }
        public Setting<Easings> ViewDropdownRotOpenEase { get; set; }
        public Setting<Easings> ViewDropdownRotCloseEase { get; set; }

        #endregion

        #region Steam Dropdown

        public Setting<bool> SteamDropdownActive { get; set; }

        public Setting<bool> SteamDropdownPosActive { get; set; }
        public Setting<Vector2> SteamDropdownPosOpen { get; set; }
        public Setting<Vector2> SteamDropdownPosClose { get; set; }
        public Setting<Vector2> SteamDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> SteamDropdownPosCloseDuration { get; set; }
        public Setting<Easings> SteamDropdownPosXOpenEase { get; set; }
        public Setting<Easings> SteamDropdownPosXCloseEase { get; set; }
        public Setting<Easings> SteamDropdownPosYOpenEase { get; set; }
        public Setting<Easings> SteamDropdownPosYCloseEase { get; set; }

        public Setting<bool> SteamDropdownScaActive { get; set; }
        public Setting<Vector2> SteamDropdownScaOpen { get; set; }
        public Setting<Vector2> SteamDropdownScaClose { get; set; }
        public Setting<Vector2> SteamDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> SteamDropdownScaCloseDuration { get; set; }
        public Setting<Easings> SteamDropdownScaXOpenEase { get; set; }
        public Setting<Easings> SteamDropdownScaXCloseEase { get; set; }
        public Setting<Easings> SteamDropdownScaYOpenEase { get; set; }
        public Setting<Easings> SteamDropdownScaYCloseEase { get; set; }

        public Setting<bool> SteamDropdownRotActive { get; set; }
        public Setting<float> SteamDropdownRotOpen { get; set; }
        public Setting<float> SteamDropdownRotClose { get; set; }
        public Setting<float> SteamDropdownRotOpenDuration { get; set; }
        public Setting<float> SteamDropdownRotCloseDuration { get; set; }
        public Setting<Easings> SteamDropdownRotOpenEase { get; set; }
        public Setting<Easings> SteamDropdownRotCloseEase { get; set; }

        #endregion

        #region Help Dropdown

        public Setting<bool> HelpDropdownActive { get; set; }

        public Setting<bool> HelpDropdownPosActive { get; set; }
        public Setting<Vector2> HelpDropdownPosOpen { get; set; }
        public Setting<Vector2> HelpDropdownPosClose { get; set; }
        public Setting<Vector2> HelpDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> HelpDropdownPosCloseDuration { get; set; }
        public Setting<Easings> HelpDropdownPosXOpenEase { get; set; }
        public Setting<Easings> HelpDropdownPosXCloseEase { get; set; }
        public Setting<Easings> HelpDropdownPosYOpenEase { get; set; }
        public Setting<Easings> HelpDropdownPosYCloseEase { get; set; }

        public Setting<bool> HelpDropdownScaActive { get; set; }
        public Setting<Vector2> HelpDropdownScaOpen { get; set; }
        public Setting<Vector2> HelpDropdownScaClose { get; set; }
        public Setting<Vector2> HelpDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> HelpDropdownScaCloseDuration { get; set; }
        public Setting<Easings> HelpDropdownScaXOpenEase { get; set; }
        public Setting<Easings> HelpDropdownScaXCloseEase { get; set; }
        public Setting<Easings> HelpDropdownScaYOpenEase { get; set; }
        public Setting<Easings> HelpDropdownScaYCloseEase { get; set; }

        public Setting<bool> HelpDropdownRotActive { get; set; }
        public Setting<float> HelpDropdownRotOpen { get; set; }
        public Setting<float> HelpDropdownRotClose { get; set; }
        public Setting<float> HelpDropdownRotOpenDuration { get; set; }
        public Setting<float> HelpDropdownRotCloseDuration { get; set; }
        public Setting<Easings> HelpDropdownRotOpenEase { get; set; }
        public Setting<Easings> HelpDropdownRotCloseEase { get; set; }

        #endregion

        #endregion

        #region Preview

        public Setting<bool> OnlyObjectsOnCurrentLayerVisible { get; set; }
        public Setting<float> VisibleObjectOpacity { get; set; }
        public Setting<bool> ShowEmpties { get; set; }
        public Setting<bool> OnlyShowDamagable { get; set; }
        public Setting<bool> HighlightObjects { get; set; }
        public Setting<Color> ObjectHighlightAmount { get; set; }
        public Setting<Color> ObjectHighlightDoubleAmount { get; set; }
        public Setting<bool> ObjectDraggerEnabled { get; set; }
        public Setting<bool> ObjectDraggerCreatesKeyframe { get; set; }
        public Setting<float> ObjectDraggerRotatorRadius { get; set; }
        public Setting<float> ObjectDraggerScalerOffset { get; set; }
        public Setting<float> ObjectDraggerScalerScale { get; set; }

        public Setting<bool> PreviewGridEnabled { get; set; }
        public Setting<float> PreviewGridSize { get; set; }
        public Setting<float> PreviewGridThickness { get; set; }
        public Setting<Color> PreviewGridColor { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            Debug = Bind(this, "General", "Debug", false, "If enabled, specific debugging functions for the editor will be enabled.");
            EditorZenMode = Bind(this, "General", "Editor Zen Mode", false, "If on, the player will not take damage in Preview Mode.");
            ResetHealthInEditor = Bind(this, "General", "Reset Health In Editor View", true, "If on, the player's health will reset when the creator exits Preview Mode.");
            BPMSnapsKeyframes = Bind(this, "General", "BPM Snaps Keyframes", false, "Makes object's keyframes snap if Snap BPM is enabled.");
            BPMSnapDivisions = Bind(this, "General", "BPM Snap Divisions", 4f, "How many times the snap is divided into. Can be good for songs that don't do 4 divisions.");
            DraggingPlaysSound = Bind(this, "General", "Dragging Plays Sound", true, "If dragging an object plays a sound.");
            DraggingPlaysSoundOnlyWithBPM = Bind(this, "General", "Dragging Plays Sound Only With BPM", true, "If dragging an object plays a sound ONLY when BPM Snap is active.");
            ShowCollapsePrefabWarning = Bind(this, "General", "Show Collapse Prefab Warning", true, "If a warning should popup when the user is trying to apply a prefab. Can be good for accidental Apply Prefab button clicks.");
            UpdateHomingKeyframesDrag = Bind(this, "General", "Update Homing Keyframes on Drag", true, "If all homing keyframes should retarget when the audio position is changed via the timeline cursor.");
            RoundToNearest = Bind(this, "General", "Round To Nearest", true, "If numbers should be rounded up to 3 decimal points (for example, 0.43321245 into 0.433).");
            ScrollOnEasing = Bind(this, "General", "Scroll on Easing Changes Value", true, "If Scolling on an easing dropdown changes the easing.");
            PrefabExampleTemplate = Bind(this, "General", "Prefab Example Template", true, "Example Template prefab will always be generated into the internal prefabs for you to use.");
            PasteOffset = Bind(this, "General", "Paste Offset", false, "When enabled objects that are pasted will be pasted at an offset based on the distance between the audio time and the copied object. Otherwise, the objects will be pasted at the earliest objects start time.");
            BringToSelection = Bind(this, "General", "Bring To Selection", false, "When an object is selected (whether it be a regular object, a marker, etc), it will move the layer and audio time to that object.");
            SelectPasted = Bind(this, "General", "Select Pasted Keyframes", false, "Select a pasted keyframe.");
            CreateObjectsatCameraCenter = Bind(this, "General", "Create Objects at Camera Center", true, "When an object is created, its position will be set to that of the camera's.");
            SpawnPrefabsAtCameraCenter = Bind(this, "General", "Spawn Prefabs at Camera Center", true, "When a Prefab object is placed into a level, its position will be set to that of the camera's.");
            CreateObjectsScaleParentDefault = Bind(this, "General", "Create Objects Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.");
            CreateObjectModifierOrderDefault = Bind(this, "General", "Create Object Modifier Order Default", false, "The default value for new objects' Order Matters toggle.");
            AllowEditorKeybindsWithEditorCam = Bind(this, "General", "Allow Editor Keybinds With Editor Cam", true, "Allows keybinds to be used if EventsCore editor camera is on.");
            RotationEventKeyframeResets = Bind(this, "General", "Rotation Event Keyframe Resets", true, "When an Event / Check rotation keyframe is created, it resets the value to 0.");
            RememberLastKeyframeType = Bind(this, "General", "Remember Last Keyframe Type", false, "When an object is selected for the first time, it selects the previous objects' keyframe selection type. For example, say you had a color keyframe selected, this newly selected object will select the first color keyframe.");

            #endregion

            #region Timeline

            DraggingMainCursorPausesLevel = Bind(this, "Timeline", "Dragging Main Cursor Pauses Level", true, "If dragging the cursor pauses the level.");
            DraggingMainCursorFix = Bind(this, "Timeline", "Dragging Main Cursor Fix", true, "If the main cursor should act like the object cursor.");
            UseMouseAsZoomPoint = Bind(this, "Timeline", "Use Mouse As Zooming Point", false, "If zooming in should use your mouse as a point instead of the timeline cursor. Applies to both main and object timelines..");
            TimelineCursorColor = Bind(this, "Timeline", "Timeline Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the main timeline cursor.");
            KeyframeCursorColor = Bind(this, "Timeline", "Keyframe Cursor Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of the object timeline cursor.");
            ObjectSelectionColor = Bind(this, "Timeline", "Object Selection Color", new Color(0.251f, 0.4627f, 0.8745f, 1f), "Color of selected objects.");
            MainZoomBounds = Bind(this, "Timeline", "Main Zoom Bounds", new Vector2(16f, 512f), "The limits of the main timeline zoom.");
            KeyframeZoomBounds = Bind(this, "Timeline", "Keyframe Zoom Bounds", new Vector2(1f, 512f), "The limits of the keyframe timeline zoom.");
            MainZoomAmount = Bind(this, "Timeline", "Main Zoom Amount", 0.05f, "Sets the zoom in & out amount for the main timeline.");
            KeyframeZoomAmount = Bind(this, "Timeline", "Keyframe Zoom Amount", 0.05f, "Sets the zoom in & out amount for the keyframe timeline.");
            BinControlKey = BindEnum(this, "Timeline", "Bin Control Key", KeyCode.Tab, "The key to be held when you want to enable bin controls.");
            BinControlActiveBehavior = BindEnum(this, "Timeline", "Bin Control Active Behavior", BinSliderControlActive.KeyToggled, "How the visibility of the Bin Slider should be treated.");
            BinControlScrollAmount = Bind(this, "Timeline", "Bin Control Scroll Amount", 0.02f, "How much the bins should scroll.");
            BinControlsPlaysSounds = Bind(this, "Timeline", "Bin Controls Plays Sounds", true, "If using the bin controls can play sounds.");
            TimelineObjectRetainsBinOnDrag = Bind(this, "Timeline", "Timeline Object Retains Bin On Drag", true, "If timeline object should retain their bin value when dragged. Having this on can prevent objects from collapsing when bins are removed.");
            MoveToChangedBin = Bind(this, "Timeline", "Move To Changed Bin", true, "If the timeline should move to the bottom of the bin count when a bin is added / removed.");
            KeyframeEndLengthOffset = Bind(this, "Timeline", "Keyframe End Length Offset", 2f, "Sets the amount of space you have after the last keyframe in an object.");
            TimelineCollapseLength = Bind(this, "Timeline", "Timeline Collapse Length", 0.4f, "How small a collapsed timeline object ends up.", 0.05f, 1f);
            TimelineObjectPrefabTypeIcon = Bind(this, "Timeline", "Timeline Object Prefab Type Icon", true, "Shows the object's prefab type's icon.");
            EventLabelsRenderLeft = Bind(this, "Timeline", "Event Labels Render Left", false, "If the Event Layer labels should render on the left side or not.");
            EventKeyframesRenderBinColor = Bind(this, "Timeline", "Event Keyframes Use Bin Color", true, "If the Event Keyframes should use the bin color when not selected or not.");
            ObjectKeyframesRenderBinColor = Bind(this, "Timeline", "Object Keyframes Use Bin Color", true, "If the Object Keyframes should use the bin color when not selected or not.");
            WaveformGenerate = Bind(this, "Timeline", "Waveform Generate", true, "Allows the timeline waveform to generate. (Waveform might not show on some devices and will increase level load times)");
            WaveformSaves = Bind(this, "Timeline", "Waveform Saves", true, "Turn off if you don't want the timeline waveform to save.");
            WaveformRerender = Bind(this, "Timeline", "Waveform Re-render", false, "If the timeline waveform should update when a value is changed.");
            WaveformMode = BindEnum(this, "Timeline", "Waveform Mode", WaveformType.Legacy, "The mode of the timeline waveform.");
            WaveformBGColor = Bind(this, "Timeline", "Waveform BG Color", Color.clear, "Color of the background for the waveform.");
            WaveformTopColor = Bind(this, "Timeline", "Waveform Top Color", LSColors.red300, "If waveform mode is Legacy, this will be the top color. Otherwise, it will be the regular color.");
            WaveformBottomColor = Bind(this, "Timeline", "Waveform Bottom Color", LSColors.blue300, "If waveform is Legacy, this will be the bottom color. Otherwise, it will be unused.");
            WaveformTextureFormat = BindEnum(this, "Timeline", "Waveform Texture Format", TextureFormat.ARGB32, "What format the waveform's texture should render under.");
            TimelineGridEnabled = Bind(this, "Timeline", "Timeline Grid Enabled", true, "If the timeline grid renders.");
            TimelineGridColor = Bind(this, "Timeline", "Timeline Grid Color", new Color(0.2157f, 0.2157f, 0.2196f, 1f), "The color of the timeline grid.");
            TimelineGridThickness = Bind(this, "Timeline", "Timeline Grid Thickness", 2f, "The size of each line of the timeline grid.");

            #endregion

            #region Data

            AutosaveLimit = Bind(this, "Data", "Autosave Limit", 7, "If autosave count reaches this number, delete the first autosave.");
            AutosaveLoopTime = Bind(this, "Data", "Autosave Loop Time", 600f, "The repeat time of autosave.");
            UploadDeleteOnLogin = Bind(this, "Data", "Retry Upload or Delete on Login", false, "If the game should try uploading / deleting again after you have logged in.");
            SaveAsync = Bind(this, "Data", "Save Async", true, "If saving levels should run asynchronously. Having this on will not show logs in the BepInEx log console.");
            LevelLoadsLastTime = Bind(this, "Data", "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
            LevelPausesOnStart = Bind(this, "Data", "Level Pauses on Start", false, "Editor pauses on level load.");
            BackupPreviousLoadedLevel = Bind(this, "Data", "Backup Previous Loaded Level", false, "Saves the previously loaded level when loading a different level to a level-previous.lsb file.");
            SettingPathReloads = Bind(this, "Data", "Setting Path Reloads", true, "With this setting on, update the list for levels, prefabs and themes when changing the directory.");
            EditorRank = BindEnum(this, "Data", "Editor Rank", Rank.Null, "What rank should be used when displaying / calculating level rank while in the editor.");
            CopyPasteGlobal = Bind(this, "Data", "Copy Paste From Global Folder", false, "If copied objects & event keyframes are saved to a global file for any instance of Project Arrhythmia to load when pasting. Turn off if copy & paste is breaking.");
            ExpandObjectsYieldMode = BindEnum(this, "Data", "Expand Objects Yield Mode", YieldType.None, $"The yield instruction used for spacing out expanding objects from a Prefab Object. {CoreHelper.DefaultYieldInstructionDescription}");
            UpdateExpandedObjectsYieldMode = BindEnum(this, "Data", "Update Expand Objects Yield Mode", YieldType.None, $"The yield instruction used for spacing out updating the expanded objects of a Prefab Object. {CoreHelper.DefaultYieldInstructionDescription}");
            PasteObjectsYieldMode = BindEnum(this, "Data", "Paste Objects Yield Mode", YieldType.None, $"The yield instruction used for spacing out pasting objects. {CoreHelper.DefaultYieldInstructionDescription}");
            UpdatePastedObjectsYieldMode = BindEnum(this, "Data", "Update Pasted Objects Yield Mode", YieldType.None, $"The yield instruction used for spacing out updating pasted objects. {CoreHelper.DefaultYieldInstructionDescription}");
            CombinerOutputFormat = BindEnum(this, "Data", "Combiner Output Format", ArrhythmiaType.LS, "Which PA file type the level combiner outputs.");
            SavingSavesThemeOpacity = Bind(this, "Data", "Saving Saves Theme Opacity", false, "Turn this off if you don't want themes to break in unmodded PA.");
            UpdatePrefabListOnFilesChanged = Bind(this, "Data", "Update Prefab List on Files Changed", false, "When you add a prefab to your prefab path, the editor will automatically update the prefab list for you.");
            UpdateThemeListOnFilesChanged = Bind(this, "Data", "Update Theme List on Files Changed", false, "When you add a theme to your theme path, the editor will automatically update the theme list for you.");
            ShowFoldersInLevelList = Bind(this, "Data", "Show Folders In Level List", true, "If folders should appear in the level list UI. This allows you to quickly navigate level folders.");
            ZIPLevelExportPath = Bind(this, "Data", "ZIP Level Export Path", "", "The custom path to export a zipped level to. If no path is set then it will export to beatmaps/exports.");
            ConvertLevelLSToVGExportPath = Bind(this, "Data", "Convert Level LS to VG Export Path", "", "The custom path to export a level to. If no path is set then it will export to beatmaps/exports.");
            ConvertPrefabLSToVGExportPath = Bind(this, "Data", "Convert Prefab LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            ConvertThemeLSToVGExportPath = Bind(this, "Data", "Convert Theme LS to VG Export Path", "", "The custom path to export a prefab to. If no path is set then it will export to beatmaps/exports.");
            FileBrowserAudioPreviewLength = Bind(this, "Data", "File Browser Audio Preview Length", 3f, "How long the file browser audio preview should be.");
            ThemeSavesIndents = Bind(this, "Data", "Theme Saves Indents", false, "If .lst files should save with multiple lines and indents.");
            FileBrowserRemembersLocation = Bind(this, "Data", "File Browser Remembers Location", true, "If the in-editor File Browser should retain the previous path that was set.");
            PasteBackgroundObjectsOverwrites = Bind(this, "Data", "Paste Background Objects Overwrites", true, "If pasting the entire copied set of BG objects overwrites the current list of BG objects.");

            #endregion

            #region Editor GUI

            DragUI = Bind(this, "Editor GUI", "Drag UI", true, "Specific UI popups can be dragged around (such as the parent selector, etc).");
            EditorTheme = BindEnum(this, "Editor GUI", "Editor Theme", BetterLegacy.EditorTheme.Legacy, "The current theme the editor uses.");
            EditorFont = BindEnum(this, "Editor GUI", "Editor Font", BetterLegacy.EditorFont.Inconsolata_Variable, "The current font the editor uses.");
            RoundedUI = Bind(this, "Editor GUI", "Rounded UI", false, "If all elements that can be rounded should be so.");
            EditorComplexity = BindEnum(this, "Editor GUI", "Editor Complexity", Complexity.Advanced, "What features show in the editor.");
            ShowExperimental = Bind(this, "Editor GUI", "Show Experimental Features", false, "If experimental features should display. These features are not gauranteed to always work and have a chance to be changed in future updates.");
            UserPreference = BindEnum(this, "Editor GUI", "User Preference", UserPreferenceType.None, "Change this to whatever config preset you want to use. THIS WILL CHANGE A LOT OF SETTINGS, SO USE WITH CAUTION.");
            HoverUIPlaySound = Bind(this, "Editor GUI", "Hover UI Play Sound", false, "Plays a sound when the hover UI element is hovered over.");
            ImportPrefabsDirectly = Bind(this, "Editor GUI", "Import Prefabs Directly", false, "When clicking on an External Prefab, instead of importing it directly it'll bring up a Prefab External View Dialog if this config is off.");
            ThemesPerPage = Bind(this, "Editor GUI", "Themes Per Page", 10, "How many themes are shown per page in the Beatmap Themes popup.");
            ThemesEventKeyframePerPage = Bind(this, "Editor GUI", "Themes (Event Keyframe) Per Page", 30, "How many themes are shown per page in the theme event keyframe.");
            ShowHelpOnStartup = Bind(this, "Editor GUI", "Show Help on Startup", true, "If the help info box should appear on startup.");
            MouseTooltipDisplay = Bind(this, "Editor GUI", "Mouse Tooltip Display", true, "If the mouse tooltip should display.");
            MouseTooltipRequiresHelp = Bind(this, "Editor GUI", "Mouse Tooltip Requires Help", false, "If the mouse tooltip should only display when the help info box is active.");
            MouseTooltipHoverTime = Bind(this, "Editor GUI", "Mouse Tooltip Hover Time", 0.45f, "How long you have to hover your cursor over an element to show the mouse tooltip.", 0f, 20f);
            HideMouseTooltipOnExit = Bind(this, "Editor GUI", "Hide Mouse Tooltip on Exit", true, "If the mouse tooltip should hide when the mouse exits the element.");
            MouseTooltipDisplayTime = Bind(this, "Editor GUI", "Mouse Tooltip Display Time", 1f, "The multiplied length of time mouse tooltips stay on screen for.", 0f, 20f);
            NotificationDisplayTime = Bind(this, "Editor GUI", "Notification Display Time", 1f, "The multiplied length of time notifications stay on screen for.", 0f, 10f);
            NotificationWidth = Bind(this, "Editor GUI", "Notification Width", 221f, "Width of the notifications.");
            NotificationSize = Bind(this, "Editor GUI", "Notification Size", 1f, "Total size of the notifications.");
            NotificationDirection = BindEnum(this, "Editor GUI", "Notification Direction", VerticalDirection.Down, "Direction the notifications popup from.");
            NotificationsDisplay = Bind(this, "Editor GUI", "Notifications Display", true, "If the notifications should display. Does not include the help box.");
            AdjustPositionInputs = Bind(this, "Editor GUI", "Adjust Position Inputs", true, "If position keyframe input fields should be adjusted so they're in a proper row rather than having Z Axis below X Axis without a label. Drawback with doing this is it makes the fields smaller than normal.");
            ShowDropdownOnHover = Bind(this, "Editor GUI", "Show Dropdowns on Hover", false, "If your mouse enters a dropdown bar, it will automatically show the dropdown list.");
            HideVisualElementsWhenObjectIsEmpty = Bind(this, "Editor GUI", "Hide Visual Elements When Object Is Empty", true, "If the Beatmap Object is empty, anything related to the visuals of the object doesn't show.");
            RenderDepthRange = Bind(this, "Editor GUI", "Render Depth Range", new Vector2Int(219, -98), "The range the Render Depth slider will show. Vanilla Legacy range is 30 and 0.");
            OpenNewLevelCreatorIfNoLevels = Bind(this, "Editor GUI", "Open New Level Creator If No Levels", false, "If the New Level Creator popup should open when there are no levels in a level folder.");

            OpenLevelPosition = Bind(this, "Editor GUI", "Open Level Position", Vector2.zero, "The position of the Open Level popup.");
            OpenLevelScale = Bind(this, "Editor GUI", "Open Level Scale", new Vector2(600f, 400f), "The size of the Open Level popup.");
            OpenLevelEditorPathPos = Bind(this, "Editor GUI", "Open Level Editor Path Pos", new Vector2(275f, 16f), "The position of the editor path input field.");
            OpenLevelEditorPathLength = Bind(this, "Editor GUI", "Open Level Editor Path Length", 104f, "The length of the editor path input field.");
            OpenLevelListRefreshPosition = Bind(this, "Editor GUI", "Open Level List Refresh Position", new Vector2(330f, 432f), "The position of the refresh button.");
            OpenLevelTogglePosition = Bind(this, "Editor GUI", "Open Level Toggle Position", new Vector2(600f, 16f), "The position of the descending toggle.");
            OpenLevelDropdownPosition = Bind(this, "Editor GUI", "Open Level Dropdown Position", new Vector2(501f, 416f), "The position of the sort dropdown.");
            OpenLevelCellSize = Bind(this, "Editor GUI", "Open Level Cell Size", new Vector2(584f, 32f), "Size of each cell.");
            OpenLevelCellConstraintType = BindEnum(this, "Editor GUI", "Open Level Cell Constraint Type", GridLayoutGroup.Constraint.FixedColumnCount, "How the cells are layed out.");
            OpenLevelCellConstraintCount = Bind(this, "Editor GUI", "Open Level Cell Constraint Count", 1, "How many rows / columns there are, depending on Constraint Type.");
            OpenLevelCellSpacing = Bind(this, "Editor GUI", "Open Level Cell Spacing", new Vector2(0f, 8f), "The space between each cell.");
            OpenLevelTextHorizontalWrap = BindEnum(this, "Editor GUI", "Open Level Text Horizontal Wrap", HorizontalWrapMode.Wrap, "Horizontal Wrap Mode of the folder button text.");
            OpenLevelTextVerticalWrap = BindEnum(this, "Editor GUI", "Open Level Text Vertical Wrap", VerticalWrapMode.Truncate, "Vertical Wrap Mode of the folder button text.");
            OpenLevelTextFontSize = Bind(this, "Editor GUI", "Open Level Text Font Size", 20, "Font size of the folder button text.", 1, 40);

            OpenLevelFolderNameMax = Bind(this, "Editor GUI", "Open Level Folder Name Max", 14, "Limited length of the folder name.", 1, 40);
            OpenLevelSongNameMax = Bind(this, "Editor GUI", "Open Level Song Name Max", 22, "Limited length of the song name.", 1, 40);
            OpenLevelArtistNameMax = Bind(this, "Editor GUI", "Open Level Artist Name Max", 16, "Limited length of the artist name.", 1, 40);
            OpenLevelCreatorNameMax = Bind(this, "Editor GUI", "Open Level Creator Name Max", 16, "Limited length of the creator name.", 1, 40);
            OpenLevelDescriptionMax = Bind(this, "Editor GUI", "Open Level Description Max", 16, "Limited length of the description.", 1, 40);
            OpenLevelDateMax = Bind(this, "Editor GUI", "Open Level Date Max", 16, "Limited length of the date.", 1, 40);
            OpenLevelTextFormatting = Bind(this, "Editor GUI", "Open Level Text Formatting", ".  /{0} : {1} by {2}",
                    "The way the text is formatted for each level. {0} is folder, {1} is song, {2} is artist, {3} is creator, {4} is difficulty, {5} is description and {6} is last edited.");

            OpenLevelButtonHoverSize = Bind(this, "Editor GUI", "Open Level Button Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            OpenLevelCoverPosition = Bind(this, "Editor GUI", "Open Level Cover Position", new Vector2(-276f, 0f), "Position of the level cover.");
            OpenLevelCoverScale = Bind(this, "Editor GUI", "Open Level Cover Scale", new Vector2(26f, 26f), "Size of the level cover.");

            ChangesRefreshLevelList = Bind(this, "Editor GUI", "Changes Refresh Level List", false, "If the level list reloads whenever a change is made.");
            OpenLevelShowDeleteButton = Bind(this, "Editor GUI", "Open Level Show Delete Button", false, "Shows a delete button that can be used to move levels to a recycling folder.");

            TimelineObjectHoverSize = Bind(this, "Editor GUI", "Timeline Object Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            KeyframeHoverSize = Bind(this, "Editor GUI", "Keyframe Hover Size", 1f, "How big the button gets when hovered.", 0.7f, 1.4f);
            TimelineBarButtonsHoverSize = Bind(this, "Editor GUI", "Timeline Bar Buttons Hover Size", 1.05f, "How big the button gets when hovered.", 0.7f, 1.4f);
            PrefabButtonHoverSize = Bind(this, "Editor GUI", "Prefab Button Hover Size", 1.05f, "How big the button gets when hovered.", 0.7f, 1.4f);

            PrefabInternalPopupPos = Bind(this, "Editor GUI", "Prefab Internal Popup Pos", new Vector2(-80f, -16f), "Position of the internal prefabs popup.");
            PrefabInternalPopupSize = Bind(this, "Editor GUI", "Prefab Internal Popup Size", new Vector2(500f, -32f), "Scale of the internal prefabs popup.");
            PrefabInternalHorizontalScroll = Bind(this, "Editor GUI", "Prefab Internal Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabInternalCellSize = Bind(this, "Editor GUI", "Prefab Internal Cell Size", new Vector2(483f, 32f), "Size of each Prefab Item.");
            PrefabInternalConstraintMode = BindEnum(this, "Editor GUI", "Prefab Internal Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabInternalConstraint = Bind(this, "Editor GUI", "Prefab Internal Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabInternalSpacing = Bind(this, "Editor GUI", "Prefab Internal Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabInternalStartAxis = BindEnum(this, "Editor GUI", "Prefab Internal Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabInternalDeleteButtonPos = Bind(this, "Editor GUI", "Prefab Internal Delete Button Pos", new Vector2(467f, -16f), "Position of the Delete Button.");
            PrefabInternalDeleteButtonSca = Bind(this, "Editor GUI", "Prefab Internal Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabInternalNameHorizontalWrap = BindEnum(this, "Editor GUI", "Prefab Internal Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameVerticalWrap = BindEnum(this, "Editor GUI", "Prefab Internal Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalNameFontSize = Bind(this, "Editor GUI", "Prefab Internal Name Font Size", 20, "Size of the text font.", 1, 40);
            PrefabInternalTypeHorizontalWrap = BindEnum(this, "Editor GUI", "Prefab Internal Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeVerticalWrap = BindEnum(this, "Editor GUI", "Prefab Internal Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabInternalTypeFontSize = Bind(this, "Editor GUI", "Prefab Internal Type Font Size", 20, "Size of the text font.", 1, 40);

            PrefabExternalPopupPos = Bind(this, "Editor GUI", "Prefab External Popup Pos", new Vector2(60f, -16f), "Position of the external prefabs popup.");
            PrefabExternalPopupSize = Bind(this, "Editor GUI", "Prefab External Popup Size", new Vector2(500f, -32f), "Scale of the external prefabs popup.");
            PrefabExternalPrefabPathPos = Bind(this, "Editor GUI", "Prefab External Prefab Path Pos", new Vector2(325f, 16f), "Position of the prefab path input field.");
            PrefabExternalPrefabPathLength = Bind(this, "Editor GUI", "Prefab External Prefab Path Length", 150f, "Length of the prefab path input field.");
            PrefabExternalPrefabRefreshPos = Bind(this, "Editor GUI", "Prefab External Prefab Refresh Pos", new Vector2(310f, 450f), "Position of the prefab refresh button.");
            PrefabExternalHorizontalScroll = Bind(this, "Editor GUI", "Prefab External Horizontal Scroll", false, "If you can scroll left / right or not.");
            PrefabExternalCellSize = Bind(this, "Editor GUI", "Prefab External Cell Size", new Vector2(483f, 32f), "Size of each Prefab Item.");
            PrefabExternalConstraintMode = BindEnum(this, "Editor GUI", "Prefab External Constraint Mode", GridLayoutGroup.Constraint.FixedColumnCount, "Which direction the prefab list goes.");
            PrefabExternalConstraint = Bind(this, "Editor GUI", "Prefab External Constraint", 1, "How many columns the prefabs are divided into.");
            PrefabExternalSpacing = Bind(this, "Editor GUI", "Prefab External Spacing", new Vector2(8f, 8f), "Distance between each Prefab Cell.");
            PrefabExternalStartAxis = BindEnum(this, "Editor GUI", "Prefab External Start Axis", GridLayoutGroup.Axis.Horizontal, "Start axis of the prefab list.");
            PrefabExternalDeleteButtonPos = Bind(this, "Editor GUI", "Prefab External Delete Button Pos", new Vector2(567f, -16f), "Position of the Delete Button.");
            PrefabExternalDeleteButtonSca = Bind(this, "Editor GUI", "Prefab External Delete Button Sca", new Vector2(32f, 32f), "Scale of the Delete Button.");
            PrefabExternalNameHorizontalWrap = BindEnum(this, "Editor GUI", "Prefab External Name Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameVerticalWrap = BindEnum(this, "Editor GUI", "Prefab External Name Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalNameFontSize = Bind(this, "Editor GUI", "Prefab External Name Font Size", 20, "Size of the text font.", 1, 40);
            PrefabExternalTypeHorizontalWrap = BindEnum(this, "Editor GUI", "Prefab External Type Horizontal Wrap", HorizontalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeVerticalWrap = BindEnum(this, "Editor GUI", "Prefab External Type Vertical Wrap", VerticalWrapMode.Overflow, "If the text overflows into another line or keeps going.");
            PrefabExternalTypeFontSize = Bind(this, "Editor GUI", "Prefab External Type Font Size", 20, "Size of the text font.", 1, 40);

            #endregion

            #region Markers

            ShowMarkers = Bind(this, "Markers", "Show Markers", true, "If markers should show in the editor timeline.");
            ShowMarkersInObjectEditor = Bind(this, "Markers", "Show Markers in Object Editor", false, "If markers should display in the object editor.");
            MarkerDefaultColor = Bind(this, "Markers", "Marker Default Color", 0, "The default color assigned to a new marker.");
            MarkerDragButton = BindEnum(this, "Markers", "Marker Drag Button", PointerEventData.InputButton.Middle, "The mouse button to click and hold to drag a marker.");
            MarkerShowContextMenu = Bind(this, "Markers", "Marker Show Context Menu", false, "If a context menu should show instead of deleting a marker when you right click a marker.");
            MarkerLineColor = Bind(this, "Markers", "Marker Line Color", new Color(1f, 1f, 1f, 0.7843f), "The color of the marker lines.");
            MarkerLineWidth = Bind(this, "Markers", "Marker Line Width", 2f, "The width of the marker lines.");
            MarkerTextWidth = Bind(this, "Markers", "Marker Text Width", 64f, "The width of the markers' text. If the text is longer than this width, then it doesn't display the symbols after the width.");
            MarkerLineDotted = Bind(this, "Markers", "Marker Line Dotted", false, "If the markers' line should be dotted.");
            ObjectMarkerLineColor = Bind(this, "Markers", "Object Marker Line Color", new Color(1f, 1f, 1f, 0.5f), "The color of the marker lines.");
            ObjectMarkerLineWidth = Bind(this, "Markers", "Object Marker Line Width", 4f, "The width of the marker lines.");
            ObjectMarkerTextWidth = Bind(this, "Markers", "Object Marker Text Width", 64f, "The width of the markers' text. If the text is longer than this width, then it doesn't display the symbols after the width.");
            ObjectMarkerLineDotted = Bind(this, "Markers", "Object Marker Line Dotted", false, "If the markers' line should be dotted.");

            #endregion

            #region Fields

            ScrollwheelLargeAmountKey = BindEnum(this, "Fields", "Scrollwheel Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelSmallAmountKey = BindEnum(this, "Fields", "Scrollwheel Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelRegularAmountKey = BindEnum(this, "Fields", "Scrollwheel Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2LargeAmountKey = BindEnum(this, "Fields", "Scrollwheel Vector2 Large Amount Key", KeyCode.LeftControl, "If this key is being held while you are scrolling over a number field, the number will change by a large amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2SmallAmountKey = BindEnum(this, "Fields", "Scrollwheel Vector2 Small Amount Key", KeyCode.LeftAlt, "If this key is being held while you are scrolling over a number field, the number will change by a small amount. If the key is set to None, you will not need to hold a key.");
            ScrollwheelVector2RegularAmountKey = BindEnum(this, "Fields", "Scrollwheel Vector2 Regular Amount Key", KeyCode.None, "If this key is being held while you are scrolling over a number field, the number will change by the regular amount. If the key is set to None, you will not need to hold a key.");

            ObjectPositionScroll = Bind(this, "Fields", "Object Position Scroll", 0.1f, "The amount that is added when the Object Keyframe Editor position fields are scrolled on.");
            ObjectPositionScrollMultiply = Bind(this, "Fields", "Object Position Scroll Multiply", 10f, "The amount that is added (multiply) when the Object Keyframe Editor position fields are scrolled on.");
            ObjectScaleScroll = Bind(this, "Fields", "Object Scale Scroll", 0.1f, "The amount that is added when the Object Keyframe Editor scale fields are scrolled on.");
            ObjectScaleScrollMultiply = Bind(this, "Fields", "Object Scale Scroll Multiply", 10f, "The amount that is added (multiply) when the Object Keyframe Editor scale fields are scrolled on.");
            ObjectRotationScroll = Bind(this, "Fields", "Object Rotation Scroll", 15f, "The amount that is added when the Object Keyframe Editor rotation fields are scrolled on.");
            ObjectRotationScrollMultiply = Bind(this, "Fields", "Object Rotation Scroll Multiply", 3f, "The amount that is added (multiply) when the Object Keyframe Editor rotation fields are scrolled on.");

            ShowModifiedColors = Bind(this, "Fields", "Show Modified Colors", true, "Keyframe colors show any modifications done (such as hue, saturation and value).");
            ThemeTemplateName = Bind(this, "Fields", "Theme Template Name", "New Theme", "Name of the template theme.");
            ThemeTemplateGUI = Bind(this, "Fields", "Theme Template GUI", LSColors.white, "GUI Color of the template theme.");
            ThemeTemplateTail = Bind(this, "Fields", "Theme Template Tail", LSColors.white, "Tail Color of the template theme.");
            ThemeTemplateBG = Bind(this, "Fields", "Theme Template BG", LSColors.gray900, "BG Color of the template theme.");
            ThemeTemplatePlayer1 = Bind(this, "Fields", "Theme Template Player 1", LSColors.HexToColor(BeatmapTheme.PLAYER_1_COLOR), "Player 1 Color of the template theme.");
            ThemeTemplatePlayer2 = Bind(this, "Fields", "Theme Template Player 2", LSColors.HexToColor(BeatmapTheme.PLAYER_2_COLOR), "Player 2 Color of the template theme.");
            ThemeTemplatePlayer3 = Bind(this, "Fields", "Theme Template Player 3", LSColors.HexToColor(BeatmapTheme.PLAYER_3_COLOR), "Player 3 Color of the template theme.");
            ThemeTemplatePlayer4 = Bind(this, "Fields", "Theme Template Player 4", LSColors.HexToColor(BeatmapTheme.PLAYER_4_COLOR), "Player 4 Color of the template theme.");
            ThemeTemplateOBJ1 = Bind(this, "Fields", "Theme Template OBJ 1", LSColors.gray100, "OBJ 1 Color of the template theme.");
            ThemeTemplateOBJ2 = Bind(this, "Fields", "Theme Template OBJ 2", LSColors.gray200, "OBJ 2 Color of the template theme.");
            ThemeTemplateOBJ3 = Bind(this, "Fields", "Theme Template OBJ 3", LSColors.gray300, "OBJ 3 Color of the template theme.");
            ThemeTemplateOBJ4 = Bind(this, "Fields", "Theme Template OBJ 4", LSColors.gray400, "OBJ 4 Color of the template theme.");
            ThemeTemplateOBJ5 = Bind(this, "Fields", "Theme Template OBJ 5", LSColors.gray500, "OBJ 5 Color of the template theme.");
            ThemeTemplateOBJ6 = Bind(this, "Fields", "Theme Template OBJ 6", LSColors.gray600, "OBJ 6 Color of the template theme.");
            ThemeTemplateOBJ7 = Bind(this, "Fields", "Theme Template OBJ 7", LSColors.gray700, "OBJ 7 Color of the template theme.");
            ThemeTemplateOBJ8 = Bind(this, "Fields", "Theme Template OBJ 8", LSColors.gray800, "OBJ 8 Color of the template theme.");
            ThemeTemplateOBJ9 = Bind(this, "Fields", "Theme Template OBJ 9", LSColors.gray900, "OBJ 9 Color of the template theme.");
            ThemeTemplateOBJ10 = Bind(this, "Fields", "Theme Template OBJ 10", LSColors.gray100, "OBJ 10 Color of the template theme.");
            ThemeTemplateOBJ11 = Bind(this, "Fields", "Theme Template OBJ 11", LSColors.gray200, "OBJ 11 Color of the template theme.");
            ThemeTemplateOBJ12 = Bind(this, "Fields", "Theme Template OBJ 12", LSColors.gray300, "OBJ 12 Color of the template theme.");
            ThemeTemplateOBJ13 = Bind(this, "Fields", "Theme Template OBJ 13", LSColors.gray400, "OBJ 13 Color of the template theme.");
            ThemeTemplateOBJ14 = Bind(this, "Fields", "Theme Template OBJ 14", LSColors.gray500, "OBJ 14 Color of the template theme.");
            ThemeTemplateOBJ15 = Bind(this, "Fields", "Theme Template OBJ 15", LSColors.gray600, "OBJ 15 Color of the template theme.");
            ThemeTemplateOBJ16 = Bind(this, "Fields", "Theme Template OBJ 16", LSColors.gray700, "OBJ 16 Color of the template theme.");
            ThemeTemplateOBJ17 = Bind(this, "Fields", "Theme Template OBJ 17", LSColors.gray800, "OBJ 17 Color of the template theme.");
            ThemeTemplateOBJ18 = Bind(this, "Fields", "Theme Template OBJ 18", LSColors.gray900, "OBJ 18 Color of the template theme.");
            ThemeTemplateBG1 = Bind(this, "Fields", "Theme Template BG 1", LSColors.pink100, "BG 1 Color of the template theme.");
            ThemeTemplateBG2 = Bind(this, "Fields", "Theme Template BG 2", LSColors.pink200, "BG 2 Color of the template theme.");
            ThemeTemplateBG3 = Bind(this, "Fields", "Theme Template BG 3", LSColors.pink300, "BG 3 Color of the template theme.");
            ThemeTemplateBG4 = Bind(this, "Fields", "Theme Template BG 4", LSColors.pink400, "BG 4 Color of the template theme.");
            ThemeTemplateBG5 = Bind(this, "Fields", "Theme Template BG 5", LSColors.pink500, "BG 5 Color of the template theme.");
            ThemeTemplateBG6 = Bind(this, "Fields", "Theme Template BG 6", LSColors.pink600, "BG 6 Color of the template theme.");
            ThemeTemplateBG7 = Bind(this, "Fields", "Theme Template BG 7", LSColors.pink700, "BG 7 Color of the template theme.");
            ThemeTemplateBG8 = Bind(this, "Fields", "Theme Template BG 8", LSColors.pink800, "BG 8 Color of the template theme.");
            ThemeTemplateBG9 = Bind(this, "Fields", "Theme Template BG 9", LSColors.pink900, "BG 9 Color of the template theme.");
            ThemeTemplateFX1 = Bind(this, "Fields", "Theme Template FX 1", LSColors.gray100, "FX 1 Color of the template theme.");
            ThemeTemplateFX2 = Bind(this, "Fields", "Theme Template FX 2", LSColors.gray200, "FX 2 Color of the template theme.");
            ThemeTemplateFX3 = Bind(this, "Fields", "Theme Template FX 3", LSColors.gray300, "FX 3 Color of the template theme.");
            ThemeTemplateFX4 = Bind(this, "Fields", "Theme Template FX 4", LSColors.gray400, "FX 4 Color of the template theme.");
            ThemeTemplateFX5 = Bind(this, "Fields", "Theme Template FX 5", LSColors.gray500, "FX 5 Color of the template theme.");
            ThemeTemplateFX6 = Bind(this, "Fields", "Theme Template FX 6", LSColors.gray600, "FX 6 Color of the template theme.");
            ThemeTemplateFX7 = Bind(this, "Fields", "Theme Template FX 7", LSColors.gray700, "FX 7 Color of the template theme.");
            ThemeTemplateFX8 = Bind(this, "Fields", "Theme Template FX 8", LSColors.gray800, "FX 8 Color of the template theme.");
            ThemeTemplateFX9 = Bind(this, "Fields", "Theme Template FX 9", LSColors.gray900, "FX 9 Color of the template theme.");
            ThemeTemplateFX10 = Bind(this, "Fields", "Theme Template FX 10", LSColors.gray100, "FX 10 Color of the template theme.");
            ThemeTemplateFX11 = Bind(this, "Fields", "Theme Template FX 11", LSColors.gray200, "FX 11 Color of the template theme.");
            ThemeTemplateFX12 = Bind(this, "Fields", "Theme Template FX 12", LSColors.gray300, "FX 12 Color of the template theme.");
            ThemeTemplateFX13 = Bind(this, "Fields", "Theme Template FX 13", LSColors.gray400, "FX 13 Color of the template theme.");
            ThemeTemplateFX14 = Bind(this, "Fields", "Theme Template FX 14", LSColors.gray500, "FX 14 Color of the template theme.");
            ThemeTemplateFX15 = Bind(this, "Fields", "Theme Template FX 15", LSColors.gray600, "FX 15 Color of the template theme.");
            ThemeTemplateFX16 = Bind(this, "Fields", "Theme Template FX 16", LSColors.gray700, "FX 16 Color of the template theme.");
            ThemeTemplateFX17 = Bind(this, "Fields", "Theme Template FX 17", LSColors.gray800, "FX 17 Color of the template theme.");
            ThemeTemplateFX18 = Bind(this, "Fields", "Theme Template FX 18", LSColors.gray900, "FX 18 Color of the template theme.");

            #endregion

            #region Animations

            PlayEditorAnimations = Bind(this, "Animations", "Play Editor Animations", false, "If popups should be animated.");

            #region Open File Popup

            OpenFilePopupActive = Bind(this, "Animations", "Open File Popup Active", true, "If the popup animation should play.");

            OpenFilePopupPosActive = Bind(this, "Animations", "Open File Popup Animate Position", false, "If position should be animated.");
            OpenFilePopupPosOpen = Bind(this, "Animations", "Open File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupPosClose = Bind(this, "Animations", "Open File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupPosOpenDuration = Bind(this, "Animations", "Open File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            OpenFilePopupPosCloseDuration = Bind(this, "Animations", "Open File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            OpenFilePopupPosXOpenEase = BindEnum(this, "Animations", "Open File Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosXCloseEase = BindEnum(this, "Animations", "Open File Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosYOpenEase = BindEnum(this, "Animations", "Open File Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupPosYCloseEase = BindEnum(this, "Animations", "Open File Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            OpenFilePopupScaActive = Bind(this, "Animations", "Open File Popup Animate Scale", true, "If scale should be animated.");
            OpenFilePopupScaOpen = Bind(this, "Animations", "Open File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupScaClose = Bind(this, "Animations", "Open File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupScaOpenDuration = Bind(this, "Animations", "Open File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            OpenFilePopupScaCloseDuration = Bind(this, "Animations", "Open File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            OpenFilePopupScaXOpenEase = BindEnum(this, "Animations", "Open File Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            OpenFilePopupScaXCloseEase = BindEnum(this, "Animations", "Open File Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            OpenFilePopupScaYOpenEase = BindEnum(this, "Animations", "Open File Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            OpenFilePopupScaYCloseEase = BindEnum(this, "Animations", "Open File Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            OpenFilePopupRotActive = Bind(this, "Animations", "Open File Popup Animate Rotation", false, "If rotation should be animated.");
            OpenFilePopupRotOpen = Bind(this, "Animations", "Open File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupRotClose = Bind(this, "Animations", "Open File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupRotOpenDuration = Bind(this, "Animations", "Open File Popup Open Rotation Duration", 0f, "The duration of opening.");
            OpenFilePopupRotCloseDuration = Bind(this, "Animations", "Open File Popup Close Rotation Duration", 0f, "The duration of closing.");
            OpenFilePopupRotOpenEase = BindEnum(this, "Animations", "Open File Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            OpenFilePopupRotCloseEase = BindEnum(this, "Animations", "Open File Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region New File Popup

            NewFilePopupActive = Bind(this, "Animations", "New File Popup Active", true, "If the popup animation should play.");

            NewFilePopupPosActive = Bind(this, "Animations", "New File Popup Animate Position", false, "If position should be animated.");
            NewFilePopupPosOpen = Bind(this, "Animations", "New File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupPosClose = Bind(this, "Animations", "New File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupPosOpenDuration = Bind(this, "Animations", "New File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            NewFilePopupPosCloseDuration = Bind(this, "Animations", "New File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            NewFilePopupPosXOpenEase = BindEnum(this, "Animations", "New File Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosXCloseEase = BindEnum(this, "Animations", "New File Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosYOpenEase = BindEnum(this, "Animations", "New File Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupPosYCloseEase = BindEnum(this, "Animations", "New File Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            NewFilePopupScaActive = Bind(this, "Animations", "New File Popup Animate Scale", true, "If scale should be animated.");
            NewFilePopupScaOpen = Bind(this, "Animations", "New File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupScaClose = Bind(this, "Animations", "New File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupScaOpenDuration = Bind(this, "Animations", "New File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            NewFilePopupScaCloseDuration = Bind(this, "Animations", "New File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            NewFilePopupScaXOpenEase = BindEnum(this, "Animations", "New File Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            NewFilePopupScaXCloseEase = BindEnum(this, "Animations", "New File Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            NewFilePopupScaYOpenEase = BindEnum(this, "Animations", "New File Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            NewFilePopupScaYCloseEase = BindEnum(this, "Animations", "New File Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            NewFilePopupRotActive = Bind(this, "Animations", "New File Popup Animate Rotation", false, "If rotation should be animated.");
            NewFilePopupRotOpen = Bind(this, "Animations", "New File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupRotClose = Bind(this, "Animations", "New File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupRotOpenDuration = Bind(this, "Animations", "New File Popup Open Rotation Duration", 0f, "The duration of opening.");
            NewFilePopupRotCloseDuration = Bind(this, "Animations", "New File Popup Close Rotation Duration", 0f, "The duration of closing.");
            NewFilePopupRotOpenEase = BindEnum(this, "Animations", "New File Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            NewFilePopupRotCloseEase = BindEnum(this, "Animations", "New File Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Save As Popup

            SaveAsPopupActive = Bind(this, "Animations", "Save As Popup Active", true, "If the popup animation should play.");

            SaveAsPopupPosActive = Bind(this, "Animations", "Save As Popup Animate Position", false, "If position should be animated.");
            SaveAsPopupPosOpen = Bind(this, "Animations", "Save As Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupPosClose = Bind(this, "Animations", "Save As Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupPosOpenDuration = Bind(this, "Animations", "Save As Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            SaveAsPopupPosCloseDuration = Bind(this, "Animations", "Save As Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            SaveAsPopupPosXOpenEase = BindEnum(this, "Animations", "Save As Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosXCloseEase = BindEnum(this, "Animations", "Save As Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosYOpenEase = BindEnum(this, "Animations", "Save As Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupPosYCloseEase = BindEnum(this, "Animations", "Save As Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            SaveAsPopupScaActive = Bind(this, "Animations", "Save As Popup Animate Scale", true, "If scale should be animated.");
            SaveAsPopupScaOpen = Bind(this, "Animations", "Save As Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupScaClose = Bind(this, "Animations", "Save As Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupScaOpenDuration = Bind(this, "Animations", "Save As Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SaveAsPopupScaCloseDuration = Bind(this, "Animations", "Save As Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SaveAsPopupScaXOpenEase = BindEnum(this, "Animations", "Save As Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            SaveAsPopupScaXCloseEase = BindEnum(this, "Animations", "Save As Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            SaveAsPopupScaYOpenEase = BindEnum(this, "Animations", "Save As Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            SaveAsPopupScaYCloseEase = BindEnum(this, "Animations", "Save As Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            SaveAsPopupRotActive = Bind(this, "Animations", "Save As Popup Animate Rotation", false, "If rotation should be animated.");
            SaveAsPopupRotOpen = Bind(this, "Animations", "Save As Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupRotClose = Bind(this, "Animations", "Save As Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupRotOpenDuration = Bind(this, "Animations", "Save As Popup Open Rotation Duration", 0f, "The duration of opening.");
            SaveAsPopupRotCloseDuration = Bind(this, "Animations", "Save As Popup Close Rotation Duration", 0f, "The duration of closing.");
            SaveAsPopupRotOpenEase = BindEnum(this, "Animations", "Save As Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            SaveAsPopupRotCloseEase = BindEnum(this, "Animations", "Save As Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Quick Actions Popup

            QuickActionsPopupActive = Bind(this, "Animations", "Quick Actions Popup Active", true, "If the popup animation should play.");

            QuickActionsPopupPosActive = Bind(this, "Animations", "Quick Actions Popup Animate Position", false, "If position should be animated.");
            QuickActionsPopupPosOpen = Bind(this, "Animations", "Quick Actions Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupPosClose = Bind(this, "Animations", "Quick Actions Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupPosOpenDuration = Bind(this, "Animations", "Quick Actions Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            QuickActionsPopupPosCloseDuration = Bind(this, "Animations", "Quick Actions Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            QuickActionsPopupPosXOpenEase = BindEnum(this, "Animations", "Quick Actions Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosXCloseEase = BindEnum(this, "Animations", "Quick Actions Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosYOpenEase = BindEnum(this, "Animations", "Quick Actions Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupPosYCloseEase = BindEnum(this, "Animations", "Quick Actions Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            QuickActionsPopupScaActive = Bind(this, "Animations", "Quick Actions Popup Animate Scale", true, "If scale should be animated.");
            QuickActionsPopupScaOpen = Bind(this, "Animations", "Quick Actions Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupScaClose = Bind(this, "Animations", "Quick Actions Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupScaOpenDuration = Bind(this, "Animations", "Quick Actions Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            QuickActionsPopupScaCloseDuration = Bind(this, "Animations", "Quick Actions Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            QuickActionsPopupScaXOpenEase = BindEnum(this, "Animations", "Quick Actions Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            QuickActionsPopupScaXCloseEase = BindEnum(this, "Animations", "Quick Actions Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            QuickActionsPopupScaYOpenEase = BindEnum(this, "Animations", "Quick Actions Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            QuickActionsPopupScaYCloseEase = BindEnum(this, "Animations", "Quick Actions Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            QuickActionsPopupRotActive = Bind(this, "Animations", "Quick Actions Popup Animate Rotation", false, "If rotation should be animated.");
            QuickActionsPopupRotOpen = Bind(this, "Animations", "Quick Actions Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupRotClose = Bind(this, "Animations", "Quick Actions Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupRotOpenDuration = Bind(this, "Animations", "Quick Actions Popup Open Rotation Duration", 0f, "The duration of opening.");
            QuickActionsPopupRotCloseDuration = Bind(this, "Animations", "Quick Actions Popup Close Rotation Duration", 0f, "The duration of closing.");
            QuickActionsPopupRotOpenEase = BindEnum(this, "Animations", "Quick Actions Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            QuickActionsPopupRotCloseEase = BindEnum(this, "Animations", "Quick Actions Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Parent Selector

            ParentSelectorPopupActive = Bind(this, "Animations", "Parent Selector Popup Active", true, "If the popup animation should play.");

            ParentSelectorPopupPosActive = Bind(this, "Animations", "Parent Selector Popup Animate Position", false, "If position should be animated.");
            ParentSelectorPopupPosOpen = Bind(this, "Animations", "Parent Selector Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupPosClose = Bind(this, "Animations", "Parent Selector Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupPosOpenDuration = Bind(this, "Animations", "Parent Selector Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ParentSelectorPopupPosCloseDuration = Bind(this, "Animations", "Parent Selector Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ParentSelectorPopupPosXOpenEase = BindEnum(this, "Animations", "Parent Selector Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosXCloseEase = BindEnum(this, "Animations", "Parent Selector Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosYOpenEase = BindEnum(this, "Animations", "Parent Selector Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupPosYCloseEase = BindEnum(this, "Animations", "Parent Selector Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ParentSelectorPopupScaActive = Bind(this, "Animations", "Parent Selector Popup Animate Scale", true, "If scale should be animated.");
            ParentSelectorPopupScaOpen = Bind(this, "Animations", "Parent Selector Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupScaClose = Bind(this, "Animations", "Parent Selector Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupScaOpenDuration = Bind(this, "Animations", "Parent Selector Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ParentSelectorPopupScaCloseDuration = Bind(this, "Animations", "Parent Selector Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ParentSelectorPopupScaXOpenEase = BindEnum(this, "Animations", "Parent Selector Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaXCloseEase = BindEnum(this, "Animations", "Parent Selector Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ParentSelectorPopupScaYOpenEase = BindEnum(this, "Animations", "Parent Selector Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaYCloseEase = BindEnum(this, "Animations", "Parent Selector Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ParentSelectorPopupRotActive = Bind(this, "Animations", "Parent Selector Popup Animate Rotation", false, "If rotation should be animated.");
            ParentSelectorPopupRotOpen = Bind(this, "Animations", "Parent Selector Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupRotClose = Bind(this, "Animations", "Parent Selector Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupRotOpenDuration = Bind(this, "Animations", "Parent Selector Popup Open Rotation Duration", 0f, "The duration of opening.");
            ParentSelectorPopupRotCloseDuration = Bind(this, "Animations", "Parent Selector Popup Close Rotation Duration", 0f, "The duration of closing.");
            ParentSelectorPopupRotOpenEase = BindEnum(this, "Animations", "Parent Selector Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ParentSelectorPopupRotCloseEase = BindEnum(this, "Animations", "Parent Selector Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Prefab Popup

            PrefabPopupActive = Bind(this, "Animations", "Prefab Popup Active", true, "If the popup animation should play.");

            PrefabPopupPosActive = Bind(this, "Animations", "Prefab Popup Animate Position", false, "If position should be animated.");
            PrefabPopupPosOpen = Bind(this, "Animations", "Prefab Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupPosClose = Bind(this, "Animations", "Prefab Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupPosOpenDuration = Bind(this, "Animations", "Prefab Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabPopupPosCloseDuration = Bind(this, "Animations", "Prefab Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabPopupPosXOpenEase = BindEnum(this, "Animations", "Prefab Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosXCloseEase = BindEnum(this, "Animations", "Prefab Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosYOpenEase = BindEnum(this, "Animations", "Prefab Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupPosYCloseEase = BindEnum(this, "Animations", "Prefab Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            PrefabPopupScaActive = Bind(this, "Animations", "Prefab Popup Animate Scale", true, "If scale should be animated.");
            PrefabPopupScaOpen = Bind(this, "Animations", "Prefab Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupScaClose = Bind(this, "Animations", "Prefab Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupScaOpenDuration = Bind(this, "Animations", "Prefab Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabPopupScaCloseDuration = Bind(this, "Animations", "Prefab Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabPopupScaXOpenEase = BindEnum(this, "Animations", "Prefab Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            PrefabPopupScaXCloseEase = BindEnum(this, "Animations", "Prefab Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            PrefabPopupScaYOpenEase = BindEnum(this, "Animations", "Prefab Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            PrefabPopupScaYCloseEase = BindEnum(this, "Animations", "Prefab Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            PrefabPopupRotActive = Bind(this, "Animations", "Prefab Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabPopupRotOpen = Bind(this, "Animations", "Prefab Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupRotClose = Bind(this, "Animations", "Prefab Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupRotOpenDuration = Bind(this, "Animations", "Prefab Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabPopupRotCloseDuration = Bind(this, "Animations", "Prefab Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabPopupRotOpenEase = BindEnum(this, "Animations", "Prefab Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            PrefabPopupRotCloseEase = BindEnum(this, "Animations", "Prefab Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Object Options Popup

            ObjectOptionsPopupActive = Bind(this, "Animations", "Object Options Popup Active", true, "If the popup animation should play.");

            ObjectOptionsPopupPosActive = Bind(this, "Animations", "Object Options Popup Animate Position", true, "If position should be animated.");
            ObjectOptionsPopupPosOpen = Bind(this, "Animations", "Object Options Popup Open Position", new Vector2(-35f, 22f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupPosClose = Bind(this, "Animations", "Object Options Popup Close Position", new Vector2(-35f, 57f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupPosOpenDuration = Bind(this, "Animations", "Object Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupPosCloseDuration = Bind(this, "Animations", "Object Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupPosXOpenEase = BindEnum(this, "Animations", "Object Options Popup Open Position X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosXCloseEase = BindEnum(this, "Animations", "Object Options Popup Close Position X Ease", Easings.InCirc, "The easing of opening.");
            ObjectOptionsPopupPosYOpenEase = BindEnum(this, "Animations", "Object Options Popup Open Position Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosYCloseEase = BindEnum(this, "Animations", "Object Options Popup Close Position Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectOptionsPopupScaActive = Bind(this, "Animations", "Object Options Popup Animate Scale", true, "If scale should be animated.");
            ObjectOptionsPopupScaOpen = Bind(this, "Animations", "Object Options Popup Open Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupScaClose = Bind(this, "Animations", "Object Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupScaOpenDuration = Bind(this, "Animations", "Object Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupScaCloseDuration = Bind(this, "Animations", "Object Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupScaXOpenEase = BindEnum(this, "Animations", "Object Options Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaXCloseEase = BindEnum(this, "Animations", "Object Options Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ObjectOptionsPopupScaYOpenEase = BindEnum(this, "Animations", "Object Options Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaYCloseEase = BindEnum(this, "Animations", "Object Options Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectOptionsPopupRotActive = Bind(this, "Animations", "Object Options Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectOptionsPopupRotOpen = Bind(this, "Animations", "Object Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupRotClose = Bind(this, "Animations", "Object Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupRotOpenDuration = Bind(this, "Animations", "Object Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectOptionsPopupRotCloseDuration = Bind(this, "Animations", "Object Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectOptionsPopupRotOpenEase = BindEnum(this, "Animations", "Object Options Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ObjectOptionsPopupRotCloseEase = BindEnum(this, "Animations", "Object Options Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region BG Options Popup

            BGOptionsPopupActive = Bind(this, "Animations", "BG Options Popup Active", true, "If the popup animation should play.");

            BGOptionsPopupPosActive = Bind(this, "Animations", "BG Options Popup Animate Position", true, "If position should be animated.");
            BGOptionsPopupPosOpen = Bind(this, "Animations", "BG Options Popup Open Position", new Vector2(0f, 57f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupPosClose = Bind(this, "Animations", "BG Options Popup Close Position", new Vector2(0f, 22f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupPosOpenDuration = Bind(this, "Animations", "BG Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            BGOptionsPopupPosCloseDuration = Bind(this, "Animations", "BG Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            BGOptionsPopupPosXOpenEase = BindEnum(this, "Animations", "BG Options Popup Open Position X Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupPosXCloseEase = BindEnum(this, "Animations", "BG Options Popup Close Position X Ease", Easings.InCirc, "The easing of opening.");
            BGOptionsPopupPosYOpenEase = BindEnum(this, "Animations", "BG Options Popup Open Position Y Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupPosYCloseEase = BindEnum(this, "Animations", "BG Options Popup Close Position Y Ease", Easings.InCirc, "The easing of opening.");

            BGOptionsPopupScaActive = Bind(this, "Animations", "BG Options Popup Animate Scale", true, "If scale should be animated.");
            BGOptionsPopupScaOpen = Bind(this, "Animations", "BG Options Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupScaClose = Bind(this, "Animations", "BG Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupScaOpenDuration = Bind(this, "Animations", "BG Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BGOptionsPopupScaCloseDuration = Bind(this, "Animations", "BG Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BGOptionsPopupScaXOpenEase = BindEnum(this, "Animations", "BG Options Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupScaXCloseEase = BindEnum(this, "Animations", "BG Options Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            BGOptionsPopupScaYOpenEase = BindEnum(this, "Animations", "BG Options Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            BGOptionsPopupScaYCloseEase = BindEnum(this, "Animations", "BG Options Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            BGOptionsPopupRotActive = Bind(this, "Animations", "BG Options Popup Animate Rotation", false, "If rotation should be animated.");
            BGOptionsPopupRotOpen = Bind(this, "Animations", "BG Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupRotClose = Bind(this, "Animations", "BG Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupRotOpenDuration = Bind(this, "Animations", "BG Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            BGOptionsPopupRotCloseDuration = Bind(this, "Animations", "BG Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            BGOptionsPopupRotOpenEase = BindEnum(this, "Animations", "BG Options Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            BGOptionsPopupRotCloseEase = BindEnum(this, "Animations", "BG Options Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Browser Popup

            BrowserPopupActive = Bind(this, "Animations", "Browser Popup Active", true, "If the popup animation should play.");

            BrowserPopupPosActive = Bind(this, "Animations", "Browser Popup Animate Position", false, "If position should be animated.");
            BrowserPopupPosOpen = Bind(this, "Animations", "Browser Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupPosClose = Bind(this, "Animations", "Browser Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupPosOpenDuration = Bind(this, "Animations", "Browser Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            BrowserPopupPosCloseDuration = Bind(this, "Animations", "Browser Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            BrowserPopupPosXOpenEase = BindEnum(this, "Animations", "Browser Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosXCloseEase = BindEnum(this, "Animations", "Browser Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosYOpenEase = BindEnum(this, "Animations", "Browser Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupPosYCloseEase = BindEnum(this, "Animations", "Browser Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            BrowserPopupScaActive = Bind(this, "Animations", "Browser Popup Animate Scale", true, "If scale should be animated.");
            BrowserPopupScaOpen = Bind(this, "Animations", "Browser Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupScaClose = Bind(this, "Animations", "Browser Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupScaOpenDuration = Bind(this, "Animations", "Browser Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BrowserPopupScaCloseDuration = Bind(this, "Animations", "Browser Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BrowserPopupScaXOpenEase = BindEnum(this, "Animations", "Browser Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            BrowserPopupScaXCloseEase = BindEnum(this, "Animations", "Browser Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            BrowserPopupScaYOpenEase = BindEnum(this, "Animations", "Browser Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            BrowserPopupScaYCloseEase = BindEnum(this, "Animations", "Browser Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            BrowserPopupRotActive = Bind(this, "Animations", "Browser Popup Animate Rotation", false, "If rotation should be animated.");
            BrowserPopupRotOpen = Bind(this, "Animations", "Browser Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupRotClose = Bind(this, "Animations", "Browser Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupRotOpenDuration = Bind(this, "Animations", "Browser Popup Open Rotation Duration", 0f, "The duration of opening.");
            BrowserPopupRotCloseDuration = Bind(this, "Animations", "Browser Popup Close Rotation Duration", 0f, "The duration of closing.");
            BrowserPopupRotOpenEase = BindEnum(this, "Animations", "Browser Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            BrowserPopupRotCloseEase = BindEnum(this, "Animations", "Browser Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Object Search Popup

            ObjectSearchPopupActive = Bind(this, "Animations", "Object Search Popup Active", true, "If the popup animation should play.");

            ObjectSearchPopupPosActive = Bind(this, "Animations", "Object Search Popup Animate Position", false, "If position should be animated.");
            ObjectSearchPopupPosOpen = Bind(this, "Animations", "Object Search Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupPosClose = Bind(this, "Animations", "Object Search Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupPosOpenDuration = Bind(this, "Animations", "Object Search Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ObjectSearchPopupPosCloseDuration = Bind(this, "Animations", "Object Search Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ObjectSearchPopupPosXOpenEase = BindEnum(this, "Animations", "Object Search Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosXCloseEase = BindEnum(this, "Animations", "Object Search Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosYOpenEase = BindEnum(this, "Animations", "Object Search Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupPosYCloseEase = BindEnum(this, "Animations", "Object Search Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ObjectSearchPopupScaActive = Bind(this, "Animations", "Object Search Popup Animate Scale", true, "If scale should be animated.");
            ObjectSearchPopupScaOpen = Bind(this, "Animations", "Object Search Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupScaClose = Bind(this, "Animations", "Object Search Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupScaOpenDuration = Bind(this, "Animations", "Object Search Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectSearchPopupScaCloseDuration = Bind(this, "Animations", "Object Search Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectSearchPopupScaXOpenEase = BindEnum(this, "Animations", "Object Search Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaXCloseEase = BindEnum(this, "Animations", "Object Search Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ObjectSearchPopupScaYOpenEase = BindEnum(this, "Animations", "Object Search Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaYCloseEase = BindEnum(this, "Animations", "Object Search Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectSearchPopupRotActive = Bind(this, "Animations", "Object Search Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectSearchPopupRotOpen = Bind(this, "Animations", "Object Search Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupRotClose = Bind(this, "Animations", "Object Search Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupRotOpenDuration = Bind(this, "Animations", "Object Search Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectSearchPopupRotCloseDuration = Bind(this, "Animations", "Object Search Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectSearchPopupRotOpenEase = BindEnum(this, "Animations", "Object Search Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ObjectSearchPopupRotCloseEase = BindEnum(this, "Animations", "Object Search Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Object Template Popup

            ObjectTemplatePopupActive = Bind(this, "Animations", "Object Template Popup Active", true, "If the popup animation should play.");

            ObjectTemplatePopupPosActive = Bind(this, "Animations", "Object Template Popup Animate Position", false, "If position should be animated.");
            ObjectTemplatePopupPosOpen = Bind(this, "Animations", "Object Template Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupPosClose = Bind(this, "Animations", "Object Template Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupPosOpenDuration = Bind(this, "Animations", "Object Template Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ObjectTemplatePopupPosCloseDuration = Bind(this, "Animations", "Object Template Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ObjectTemplatePopupPosXOpenEase = BindEnum(this, "Animations", "Object Template Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectTemplatePopupPosXCloseEase = BindEnum(this, "Animations", "Object Template Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ObjectTemplatePopupPosYOpenEase = BindEnum(this, "Animations", "Object Template Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ObjectTemplatePopupPosYCloseEase = BindEnum(this, "Animations", "Object Template Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ObjectTemplatePopupScaActive = Bind(this, "Animations", "Object Template Popup Animate Scale", true, "If scale should be animated.");
            ObjectTemplatePopupScaOpen = Bind(this, "Animations", "Object Template Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupScaClose = Bind(this, "Animations", "Object Template Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupScaOpenDuration = Bind(this, "Animations", "Object Template Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectTemplatePopupScaCloseDuration = Bind(this, "Animations", "Object Template Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectTemplatePopupScaXOpenEase = BindEnum(this, "Animations", "Object Template Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ObjectTemplatePopupScaXCloseEase = BindEnum(this, "Animations", "Object Template Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ObjectTemplatePopupScaYOpenEase = BindEnum(this, "Animations", "Object Template Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ObjectTemplatePopupScaYCloseEase = BindEnum(this, "Animations", "Object Template Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ObjectTemplatePopupRotActive = Bind(this, "Animations", "Object Template Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectTemplatePopupRotOpen = Bind(this, "Animations", "Object Template Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupRotClose = Bind(this, "Animations", "Object Template Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupRotOpenDuration = Bind(this, "Animations", "Object Template Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectTemplatePopupRotCloseDuration = Bind(this, "Animations", "Object Template Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectTemplatePopupRotOpenEase = BindEnum(this, "Animations", "Object Template Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ObjectTemplatePopupRotCloseEase = BindEnum(this, "Animations", "Object Template Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Warning Popup

            WarningPopupActive = Bind(this, "Animations", "Warning Popup Active", true, "If the popup animation should play.");

            WarningPopupPosActive = Bind(this, "Animations", "Warning Popup Animate Position", false, "If position should be animated.");
            WarningPopupPosOpen = Bind(this, "Animations", "Warning Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupPosClose = Bind(this, "Animations", "Warning Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupPosOpenDuration = Bind(this, "Animations", "Warning Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            WarningPopupPosCloseDuration = Bind(this, "Animations", "Warning Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            WarningPopupPosXOpenEase = BindEnum(this, "Animations", "Warning Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosXCloseEase = BindEnum(this, "Animations", "Warning Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosYOpenEase = BindEnum(this, "Animations", "Warning Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            WarningPopupPosYCloseEase = BindEnum(this, "Animations", "Warning Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            WarningPopupScaActive = Bind(this, "Animations", "Warning Popup Animate Scale", true, "If scale should be animated.");
            WarningPopupScaOpen = Bind(this, "Animations", "Warning Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupScaClose = Bind(this, "Animations", "Warning Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupScaOpenDuration = Bind(this, "Animations", "Warning Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            WarningPopupScaCloseDuration = Bind(this, "Animations", "Warning Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            WarningPopupScaXOpenEase = BindEnum(this, "Animations", "Warning Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            WarningPopupScaXCloseEase = BindEnum(this, "Animations", "Warning Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            WarningPopupScaYOpenEase = BindEnum(this, "Animations", "Warning Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            WarningPopupScaYCloseEase = BindEnum(this, "Animations", "Warning Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            WarningPopupRotActive = Bind(this, "Animations", "Warning Popup Animate Rotation", false, "If rotation should be animated.");
            WarningPopupRotOpen = Bind(this, "Animations", "Warning Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupRotClose = Bind(this, "Animations", "Warning Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupRotOpenDuration = Bind(this, "Animations", "Warning Popup Open Rotation Duration", 0f, "The duration of opening.");
            WarningPopupRotCloseDuration = Bind(this, "Animations", "Warning Popup Close Rotation Duration", 0f, "The duration of closing.");
            WarningPopupRotOpenEase = BindEnum(this, "Animations", "Warning Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            WarningPopupRotCloseEase = BindEnum(this, "Animations", "Warning Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Warning Popup

            FilePopupActive = Bind(this, "Animations", "File Popup Active", true, "If the popup animation should play.");

            FilePopupPosActive = Bind(this, "Animations", "File Popup Animate Position", false, "If position should be animated.");
            FilePopupPosOpen = Bind(this, "Animations", "File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupPosClose = Bind(this, "Animations", "File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupPosOpenDuration = Bind(this, "Animations", "File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            FilePopupPosCloseDuration = Bind(this, "Animations", "File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            FilePopupPosXOpenEase = BindEnum(this, "Animations", "File Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            FilePopupPosXCloseEase = BindEnum(this, "Animations", "File Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            FilePopupPosYOpenEase = BindEnum(this, "Animations", "File Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            FilePopupPosYCloseEase = BindEnum(this, "Animations", "File Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            FilePopupScaActive = Bind(this, "Animations", "File Popup Animate Scale", true, "If scale should be animated.");
            FilePopupScaOpen = Bind(this, "Animations", "File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupScaClose = Bind(this, "Animations", "File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupScaOpenDuration = Bind(this, "Animations", "File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FilePopupScaCloseDuration = Bind(this, "Animations", "File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FilePopupScaXOpenEase = BindEnum(this, "Animations", "File Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            FilePopupScaXCloseEase = BindEnum(this, "Animations", "File Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            FilePopupScaYOpenEase = BindEnum(this, "Animations", "File Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            FilePopupScaYCloseEase = BindEnum(this, "Animations", "File Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            FilePopupRotActive = Bind(this, "Animations", "File Popup Animate Rotation", false, "If rotation should be animated.");
            FilePopupRotOpen = Bind(this, "Animations", "File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupRotClose = Bind(this, "Animations", "File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupRotOpenDuration = Bind(this, "Animations", "File Popup Open Rotation Duration", 0f, "The duration of opening.");
            FilePopupRotCloseDuration = Bind(this, "Animations", "File Popup Close Rotation Duration", 0f, "The duration of closing.");
            FilePopupRotOpenEase = BindEnum(this, "Animations", "File Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            FilePopupRotCloseEase = BindEnum(this, "Animations", "File Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Text Editor

            TextEditorActive = Bind(this, "Animations", "Text Editor Active", true, "If the popup animation should play.");

            TextEditorPosActive = Bind(this, "Animations", "Text Editor Animate Position", false, "If position should be animated.");
            TextEditorPosOpen = Bind(this, "Animations", "Text Editor Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorPosClose = Bind(this, "Animations", "Text Editor Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorPosOpenDuration = Bind(this, "Animations", "Text Editor Open Position Duration", Vector2.zero, "The duration of opening.");
            TextEditorPosCloseDuration = Bind(this, "Animations", "Text Editor Close Position Duration", Vector2.zero, "The duration of closing.");
            TextEditorPosXOpenEase = BindEnum(this, "Animations", "Text Editor Open Position X Ease", Easings.Linear, "The easing of opening.");
            TextEditorPosXCloseEase = BindEnum(this, "Animations", "Text Editor Close Position X Ease", Easings.Linear, "The easing of opening.");
            TextEditorPosYOpenEase = BindEnum(this, "Animations", "Text Editor Open Position Y Ease", Easings.Linear, "The easing of opening.");
            TextEditorPosYCloseEase = BindEnum(this, "Animations", "Text Editor Close Position Y Ease", Easings.Linear, "The easing of opening.");

            TextEditorScaActive = Bind(this, "Animations", "Text Editor Animate Scale", true, "If scale should be animated.");
            TextEditorScaOpen = Bind(this, "Animations", "Text Editor Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorScaClose = Bind(this, "Animations", "Text Editor Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorScaOpenDuration = Bind(this, "Animations", "Text Editor Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            TextEditorScaCloseDuration = Bind(this, "Animations", "Text Editor Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            TextEditorScaXOpenEase = BindEnum(this, "Animations", "Text Editor Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            TextEditorScaXCloseEase = BindEnum(this, "Animations", "Text Editor Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            TextEditorScaYOpenEase = BindEnum(this, "Animations", "Text Editor Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            TextEditorScaYCloseEase = BindEnum(this, "Animations", "Text Editor Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            TextEditorRotActive = Bind(this, "Animations", "Text Editor Animate Rotation", false, "If rotation should be animated.");
            TextEditorRotOpen = Bind(this, "Animations", "Text Editor Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorRotClose = Bind(this, "Animations", "Text Editor Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorRotOpenDuration = Bind(this, "Animations", "Text Editor Open Rotation Duration", 0f, "The duration of opening.");
            TextEditorRotCloseDuration = Bind(this, "Animations", "Text Editor Close Rotation Duration", 0f, "The duration of closing.");
            TextEditorRotOpenEase = BindEnum(this, "Animations", "Text Editor Open Rotation Ease", Easings.Linear, "The easing of opening.");
            TextEditorRotCloseEase = BindEnum(this, "Animations", "Text Editor Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Documentation Popup

            DocumentationPopupActive = Bind(this, "Animations", "Documentation Popup Active", true, "If the popup animation should play.");

            DocumentationPopupPosActive = Bind(this, "Animations", "Documentation Popup Animate Position", false, "If position should be animated.");
            DocumentationPopupPosOpen = Bind(this, "Animations", "Documentation Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupPosClose = Bind(this, "Animations", "Documentation Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupPosOpenDuration = Bind(this, "Animations", "Documentation Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DocumentationPopupPosCloseDuration = Bind(this, "Animations", "Documentation Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DocumentationPopupPosXOpenEase = BindEnum(this, "Animations", "Documentation Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosXCloseEase = BindEnum(this, "Animations", "Documentation Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosYOpenEase = BindEnum(this, "Animations", "Documentation Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupPosYCloseEase = BindEnum(this, "Animations", "Documentation Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DocumentationPopupScaActive = Bind(this, "Animations", "Documentation Popup Animate Scale", true, "If scale should be animated.");
            DocumentationPopupScaOpen = Bind(this, "Animations", "Documentation Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupScaClose = Bind(this, "Animations", "Documentation Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupScaOpenDuration = Bind(this, "Animations", "Documentation Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DocumentationPopupScaCloseDuration = Bind(this, "Animations", "Documentation Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DocumentationPopupScaXOpenEase = BindEnum(this, "Animations", "Documentation Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DocumentationPopupScaXCloseEase = BindEnum(this, "Animations", "Documentation Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DocumentationPopupScaYOpenEase = BindEnum(this, "Animations", "Documentation Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DocumentationPopupScaYCloseEase = BindEnum(this, "Animations", "Documentation Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DocumentationPopupRotActive = Bind(this, "Animations", "Documentation Popup Animate Rotation", false, "If rotation should be animated.");
            DocumentationPopupRotOpen = Bind(this, "Animations", "Documentation Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupRotClose = Bind(this, "Animations", "Documentation Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupRotOpenDuration = Bind(this, "Animations", "Documentation Popup Open Rotation Duration", 0f, "The duration of opening.");
            DocumentationPopupRotCloseDuration = Bind(this, "Animations", "Documentation Popup Close Rotation Duration", 0f, "The duration of closing.");
            DocumentationPopupRotOpenEase = BindEnum(this, "Animations", "Documentation Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DocumentationPopupRotCloseEase = BindEnum(this, "Animations", "Documentation Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Debugger Popup

            DebuggerPopupActive = Bind(this, "Animations", "Debugger Popup Active", true, "If the popup animation should play.");

            DebuggerPopupPosActive = Bind(this, "Animations", "Debugger Popup Animate Position", false, "If position should be animated.");
            DebuggerPopupPosOpen = Bind(this, "Animations", "Debugger Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupPosClose = Bind(this, "Animations", "Debugger Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupPosOpenDuration = Bind(this, "Animations", "Debugger Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DebuggerPopupPosCloseDuration = Bind(this, "Animations", "Debugger Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DebuggerPopupPosXOpenEase = BindEnum(this, "Animations", "Debugger Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosXCloseEase = BindEnum(this, "Animations", "Debugger Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosYOpenEase = BindEnum(this, "Animations", "Debugger Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupPosYCloseEase = BindEnum(this, "Animations", "Debugger Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DebuggerPopupScaActive = Bind(this, "Animations", "Debugger Popup Animate Scale", true, "If scale should be animated.");
            DebuggerPopupScaOpen = Bind(this, "Animations", "Debugger Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupScaClose = Bind(this, "Animations", "Debugger Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupScaOpenDuration = Bind(this, "Animations", "Debugger Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DebuggerPopupScaCloseDuration = Bind(this, "Animations", "Debugger Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DebuggerPopupScaXOpenEase = BindEnum(this, "Animations", "Debugger Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DebuggerPopupScaXCloseEase = BindEnum(this, "Animations", "Debugger Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DebuggerPopupScaYOpenEase = BindEnum(this, "Animations", "Debugger Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DebuggerPopupScaYCloseEase = BindEnum(this, "Animations", "Debugger Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DebuggerPopupRotActive = Bind(this, "Animations", "Debugger Popup Animate Rotation", false, "If rotation should be animated.");
            DebuggerPopupRotOpen = Bind(this, "Animations", "Debugger Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupRotClose = Bind(this, "Animations", "Debugger Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupRotOpenDuration = Bind(this, "Animations", "Debugger Popup Open Rotation Duration", 0f, "The duration of opening.");
            DebuggerPopupRotCloseDuration = Bind(this, "Animations", "Debugger Popup Close Rotation Duration", 0f, "The duration of closing.");
            DebuggerPopupRotOpenEase = BindEnum(this, "Animations", "Debugger Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DebuggerPopupRotCloseEase = BindEnum(this, "Animations", "Debugger Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Autosaves Popup

            AutosavesPopupActive = Bind(this, "Animations", "Autosaves Popup Active", true, "If the popup animation should play.");

            AutosavesPopupPosActive = Bind(this, "Animations", "Autosaves Popup Animate Position", false, "If position should be animated.");
            AutosavesPopupPosOpen = Bind(this, "Animations", "Autosaves Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupPosClose = Bind(this, "Animations", "Autosaves Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupPosOpenDuration = Bind(this, "Animations", "Autosaves Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            AutosavesPopupPosCloseDuration = Bind(this, "Animations", "Autosaves Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            AutosavesPopupPosXOpenEase = BindEnum(this, "Animations", "Autosaves Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosXCloseEase = BindEnum(this, "Animations", "Autosaves Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosYOpenEase = BindEnum(this, "Animations", "Autosaves Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupPosYCloseEase = BindEnum(this, "Animations", "Autosaves Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            AutosavesPopupScaActive = Bind(this, "Animations", "Autosaves Popup Animate Scale", true, "If scale should be animated.");
            AutosavesPopupScaOpen = Bind(this, "Animations", "Autosaves Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupScaClose = Bind(this, "Animations", "Autosaves Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupScaOpenDuration = Bind(this, "Animations", "Autosaves Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            AutosavesPopupScaCloseDuration = Bind(this, "Animations", "Autosaves Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            AutosavesPopupScaXOpenEase = BindEnum(this, "Animations", "Autosaves Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            AutosavesPopupScaXCloseEase = BindEnum(this, "Animations", "Autosaves Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            AutosavesPopupScaYOpenEase = BindEnum(this, "Animations", "Autosaves Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            AutosavesPopupScaYCloseEase = BindEnum(this, "Animations", "Autosaves Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            AutosavesPopupRotActive = Bind(this, "Animations", "Autosaves Popup Animate Rotation", false, "If rotation should be animated.");
            AutosavesPopupRotOpen = Bind(this, "Animations", "Autosaves Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupRotClose = Bind(this, "Animations", "Autosaves Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupRotOpenDuration = Bind(this, "Animations", "Autosaves Popup Open Rotation Duration", 0f, "The duration of opening.");
            AutosavesPopupRotCloseDuration = Bind(this, "Animations", "Autosaves Popup Close Rotation Duration", 0f, "The duration of closing.");
            AutosavesPopupRotOpenEase = BindEnum(this, "Animations", "Autosaves Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            AutosavesPopupRotCloseEase = BindEnum(this, "Animations", "Autosaves Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Default Modifiers Popup

            DefaultModifiersPopupActive = Bind(this, "Animations", "Default Modifiers Popup Active", true, "If the popup animation should play.");

            DefaultModifiersPopupPosActive = Bind(this, "Animations", "Default Modifiers Popup Animate Position", false, "If position should be animated.");
            DefaultModifiersPopupPosOpen = Bind(this, "Animations", "Default Modifiers Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupPosClose = Bind(this, "Animations", "Default Modifiers Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupPosOpenDuration = Bind(this, "Animations", "Default Modifiers Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DefaultModifiersPopupPosCloseDuration = Bind(this, "Animations", "Default Modifiers Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DefaultModifiersPopupPosXOpenEase = BindEnum(this, "Animations", "Default Modifiers Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosXCloseEase = BindEnum(this, "Animations", "Default Modifiers Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYOpenEase = BindEnum(this, "Animations", "Default Modifiers Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYCloseEase = BindEnum(this, "Animations", "Default Modifiers Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            DefaultModifiersPopupScaActive = Bind(this, "Animations", "Default Modifiers Popup Animate Scale", true, "If scale should be animated.");
            DefaultModifiersPopupScaOpen = Bind(this, "Animations", "Default Modifiers Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupScaClose = Bind(this, "Animations", "Default Modifiers Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupScaOpenDuration = Bind(this, "Animations", "Default Modifiers Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DefaultModifiersPopupScaCloseDuration = Bind(this, "Animations", "Default Modifiers Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DefaultModifiersPopupScaXOpenEase = BindEnum(this, "Animations", "Default Modifiers Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaXCloseEase = BindEnum(this, "Animations", "Default Modifiers Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            DefaultModifiersPopupScaYOpenEase = BindEnum(this, "Animations", "Default Modifiers Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaYCloseEase = BindEnum(this, "Animations", "Default Modifiers Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            DefaultModifiersPopupRotActive = Bind(this, "Animations", "Default Modifiers Popup Animate Rotation", false, "If rotation should be animated.");
            DefaultModifiersPopupRotOpen = Bind(this, "Animations", "Default Modifiers Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupRotClose = Bind(this, "Animations", "Default Modifiers Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupRotOpenDuration = Bind(this, "Animations", "Default Modifiers Popup Open Rotation Duration", 0f, "The duration of opening.");
            DefaultModifiersPopupRotCloseDuration = Bind(this, "Animations", "Default Modifiers Popup Close Rotation Duration", 0f, "The duration of closing.");
            DefaultModifiersPopupRotOpenEase = BindEnum(this, "Animations", "Default Modifiers Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            DefaultModifiersPopupRotCloseEase = BindEnum(this, "Animations", "Default Modifiers Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Keybind List Popup

            KeybindListPopupActive = Bind(this, "Animations", "Keybind List Popup Active", true, "If the popup animation should play.");

            KeybindListPopupPosActive = Bind(this, "Animations", "Keybind List Popup Animate Position", false, "If position should be animated.");
            KeybindListPopupPosOpen = Bind(this, "Animations", "Keybind List Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupPosClose = Bind(this, "Animations", "Keybind List Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupPosOpenDuration = Bind(this, "Animations", "Keybind List Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            KeybindListPopupPosCloseDuration = Bind(this, "Animations", "Keybind List Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            KeybindListPopupPosXOpenEase = BindEnum(this, "Animations", "Keybind List Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosXCloseEase = BindEnum(this, "Animations", "Keybind List Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosYOpenEase = BindEnum(this, "Animations", "Keybind List Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupPosYCloseEase = BindEnum(this, "Animations", "Keybind List Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            KeybindListPopupScaActive = Bind(this, "Animations", "Keybind List Popup Animate Scale", true, "If scale should be animated.");
            KeybindListPopupScaOpen = Bind(this, "Animations", "Keybind List Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupScaClose = Bind(this, "Animations", "Keybind List Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupScaOpenDuration = Bind(this, "Animations", "Keybind List Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            KeybindListPopupScaCloseDuration = Bind(this, "Animations", "Keybind List Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            KeybindListPopupScaXOpenEase = BindEnum(this, "Animations", "Keybind List Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            KeybindListPopupScaXCloseEase = BindEnum(this, "Animations", "Keybind List Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            KeybindListPopupScaYOpenEase = BindEnum(this, "Animations", "Keybind List Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            KeybindListPopupScaYCloseEase = BindEnum(this, "Animations", "Keybind List Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            KeybindListPopupRotActive = Bind(this, "Animations", "Keybind List Popup Animate Rotation", false, "If rotation should be animated.");
            KeybindListPopupRotOpen = Bind(this, "Animations", "Keybind List Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupRotClose = Bind(this, "Animations", "Keybind List Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupRotOpenDuration = Bind(this, "Animations", "Keybind List Popup Open Rotation Duration", 0f, "The duration of opening.");
            KeybindListPopupRotCloseDuration = Bind(this, "Animations", "Keybind List Popup Close Rotation Duration", 0f, "The duration of closing.");
            KeybindListPopupRotOpenEase = BindEnum(this, "Animations", "Keybind List Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            KeybindListPopupRotCloseEase = BindEnum(this, "Animations", "Keybind List Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Theme Popup

            ThemePopupActive = Bind(this, "Animations", "Theme Popup Active", true, "If the popup animation should play.");

            ThemePopupPosActive = Bind(this, "Animations", "Theme Popup Animate Position", false, "If position should be animated.");
            ThemePopupPosOpen = Bind(this, "Animations", "Theme Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupPosClose = Bind(this, "Animations", "Theme Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupPosOpenDuration = Bind(this, "Animations", "Theme Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ThemePopupPosCloseDuration = Bind(this, "Animations", "Theme Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ThemePopupPosXOpenEase = BindEnum(this, "Animations", "Theme Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosXCloseEase = BindEnum(this, "Animations", "Theme Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosYOpenEase = BindEnum(this, "Animations", "Theme Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ThemePopupPosYCloseEase = BindEnum(this, "Animations", "Theme Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ThemePopupScaActive = Bind(this, "Animations", "Theme Popup Animate Scale", true, "If scale should be animated.");
            ThemePopupScaOpen = Bind(this, "Animations", "Theme Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupScaClose = Bind(this, "Animations", "Theme Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupScaOpenDuration = Bind(this, "Animations", "Theme Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ThemePopupScaCloseDuration = Bind(this, "Animations", "Theme Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ThemePopupScaXOpenEase = BindEnum(this, "Animations", "Theme Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ThemePopupScaXCloseEase = BindEnum(this, "Animations", "Theme Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ThemePopupScaYOpenEase = BindEnum(this, "Animations", "Theme Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ThemePopupScaYCloseEase = BindEnum(this, "Animations", "Theme Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ThemePopupRotActive = Bind(this, "Animations", "Theme Popup Animate Rotation", false, "If rotation should be animated.");
            ThemePopupRotOpen = Bind(this, "Animations", "Theme Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupRotClose = Bind(this, "Animations", "Theme Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupRotOpenDuration = Bind(this, "Animations", "Theme Popup Open Rotation Duration", 0f, "The duration of opening.");
            ThemePopupRotCloseDuration = Bind(this, "Animations", "Theme Popup Close Rotation Duration", 0f, "The duration of closing.");
            ThemePopupRotOpenEase = BindEnum(this, "Animations", "Theme Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ThemePopupRotCloseEase = BindEnum(this, "Animations", "Theme Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Prefab Types Popup

            PrefabTypesPopupActive = Bind(this, "Animations", "Prefab Types Popup Active", true, "If the popup animation should play.");

            PrefabTypesPopupPosActive = Bind(this, "Animations", "Prefab Types Popup Animate Position", false, "If position should be animated.");
            PrefabTypesPopupPosOpen = Bind(this, "Animations", "Prefab Types Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupPosClose = Bind(this, "Animations", "Prefab Types Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupPosOpenDuration = Bind(this, "Animations", "Prefab Types Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabTypesPopupPosCloseDuration = Bind(this, "Animations", "Prefab Types Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabTypesPopupPosXOpenEase = BindEnum(this, "Animations", "Prefab Types Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosXCloseEase = BindEnum(this, "Animations", "Prefab Types Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosYOpenEase = BindEnum(this, "Animations", "Prefab Types Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupPosYCloseEase = BindEnum(this, "Animations", "Prefab Types Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            PrefabTypesPopupScaActive = Bind(this, "Animations", "Prefab Types Popup Animate Scale", true, "If scale should be animated.");
            PrefabTypesPopupScaOpen = Bind(this, "Animations", "Prefab Types Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupScaClose = Bind(this, "Animations", "Prefab Types Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupScaOpenDuration = Bind(this, "Animations", "Prefab Types Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabTypesPopupScaCloseDuration = Bind(this, "Animations", "Prefab Types Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabTypesPopupScaXOpenEase = BindEnum(this, "Animations", "Prefab Types Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaXCloseEase = BindEnum(this, "Animations", "Prefab Types Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            PrefabTypesPopupScaYOpenEase = BindEnum(this, "Animations", "Prefab Types Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaYCloseEase = BindEnum(this, "Animations", "Prefab Types Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            PrefabTypesPopupRotActive = Bind(this, "Animations", "Prefab Types Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabTypesPopupRotOpen = Bind(this, "Animations", "Prefab Types Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupRotClose = Bind(this, "Animations", "Prefab Types Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupRotOpenDuration = Bind(this, "Animations", "Prefab Types Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabTypesPopupRotCloseDuration = Bind(this, "Animations", "Prefab Types Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabTypesPopupRotOpenEase = BindEnum(this, "Animations", "Prefab Types Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            PrefabTypesPopupRotCloseEase = BindEnum(this, "Animations", "Prefab Types Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion
            
            #region Font Selector Popup

            FontSelectorPopupActive = Bind(this, "Animations", "Font Selector Popup Active", true, "If the popup animation should play.");

            FontSelectorPopupPosActive = Bind(this, "Animations", "Font Selector Popup Animate Position", false, "If position should be animated.");
            FontSelectorPopupPosOpen = Bind(this, "Animations", "Font Selector Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupPosClose = Bind(this, "Animations", "Font Selector Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupPosOpenDuration = Bind(this, "Animations", "Font Selector Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            FontSelectorPopupPosCloseDuration = Bind(this, "Animations", "Font Selector Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            FontSelectorPopupPosXOpenEase = BindEnum(this, "Animations", "Font Selector Popup Open Position X Ease", Easings.Linear, "The easing of opening.");
            FontSelectorPopupPosXCloseEase = BindEnum(this, "Animations", "Font Selector Popup Close Position X Ease", Easings.Linear, "The easing of opening.");
            FontSelectorPopupPosYOpenEase = BindEnum(this, "Animations", "Font Selector Popup Open Position Y Ease", Easings.Linear, "The easing of opening.");
            FontSelectorPopupPosYCloseEase = BindEnum(this, "Animations", "Font Selector Popup Close Position Y Ease", Easings.Linear, "The easing of opening.");

            FontSelectorPopupScaActive = Bind(this, "Animations", "Font Selector Popup Animate Scale", true, "If scale should be animated.");
            FontSelectorPopupScaOpen = Bind(this, "Animations", "Font Selector Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupScaClose = Bind(this, "Animations", "Font Selector Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupScaOpenDuration = Bind(this, "Animations", "Font Selector Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FontSelectorPopupScaCloseDuration = Bind(this, "Animations", "Font Selector Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FontSelectorPopupScaXOpenEase = BindEnum(this, "Animations", "Font Selector Popup Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            FontSelectorPopupScaXCloseEase = BindEnum(this, "Animations", "Font Selector Popup Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            FontSelectorPopupScaYOpenEase = BindEnum(this, "Animations", "Font Selector Popup Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            FontSelectorPopupScaYCloseEase = BindEnum(this, "Animations", "Font Selector Popup Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            FontSelectorPopupRotActive = Bind(this, "Animations", "Font Selector Popup Animate Rotation", false, "If rotation should be animated.");
            FontSelectorPopupRotOpen = Bind(this, "Animations", "Font Selector Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupRotClose = Bind(this, "Animations", "Font Selector Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupRotOpenDuration = Bind(this, "Animations", "Font Selector Popup Open Rotation Duration", 0f, "The duration of opening.");
            FontSelectorPopupRotCloseDuration = Bind(this, "Animations", "Font Selector Popup Close Rotation Duration", 0f, "The duration of closing.");
            FontSelectorPopupRotOpenEase = BindEnum(this, "Animations", "Font Selector Popup Open Rotation Ease", Easings.Linear, "The easing of opening.");
            FontSelectorPopupRotCloseEase = BindEnum(this, "Animations", "Font Selector Popup Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region File Dropdown

            FileDropdownActive = Bind(this, "Animations", "File Dropdown Active", true, "If the popup animation should play.");

            FileDropdownPosActive = Bind(this, "Animations", "File Dropdown Animate Position", false, "If position should be animated.");
            FileDropdownPosOpen = Bind(this, "Animations", "File Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownPosClose = Bind(this, "Animations", "File Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownPosOpenDuration = Bind(this, "Animations", "File Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            FileDropdownPosCloseDuration = Bind(this, "Animations", "File Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            FileDropdownPosXOpenEase = BindEnum(this, "Animations", "File Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosXCloseEase = BindEnum(this, "Animations", "File Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosYOpenEase = BindEnum(this, "Animations", "File Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            FileDropdownPosYCloseEase = BindEnum(this, "Animations", "File Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            FileDropdownScaActive = Bind(this, "Animations", "File Dropdown Animate Scale", true, "If scale should be animated.");
            FileDropdownScaOpen = Bind(this, "Animations", "File Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownScaClose = Bind(this, "Animations", "File Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownScaOpenDuration = Bind(this, "Animations", "File Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FileDropdownScaCloseDuration = Bind(this, "Animations", "File Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FileDropdownScaXOpenEase = BindEnum(this, "Animations", "File Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            FileDropdownScaXCloseEase = BindEnum(this, "Animations", "File Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            FileDropdownScaYOpenEase = BindEnum(this, "Animations", "File Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            FileDropdownScaYCloseEase = BindEnum(this, "Animations", "File Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            FileDropdownRotActive = Bind(this, "Animations", "File Dropdown Animate Rotation", false, "If rotation should be animated.");
            FileDropdownRotOpen = Bind(this, "Animations", "File Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownRotClose = Bind(this, "Animations", "File Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownRotOpenDuration = Bind(this, "Animations", "File Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            FileDropdownRotCloseDuration = Bind(this, "Animations", "File Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            FileDropdownRotOpenEase = BindEnum(this, "Animations", "File Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            FileDropdownRotCloseEase = BindEnum(this, "Animations", "File Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Edit Dropdown

            EditDropdownActive = Bind(this, "Animations", "Edit Dropdown Active", true, "If the popup animation should play.");

            EditDropdownPosActive = Bind(this, "Animations", "Edit Dropdown Animate Position", false, "If position should be animated.");
            EditDropdownPosOpen = Bind(this, "Animations", "Edit Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownPosClose = Bind(this, "Animations", "Edit Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownPosOpenDuration = Bind(this, "Animations", "Edit Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            EditDropdownPosCloseDuration = Bind(this, "Animations", "Edit Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            EditDropdownPosXOpenEase = BindEnum(this, "Animations", "Edit Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosXCloseEase = BindEnum(this, "Animations", "Edit Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosYOpenEase = BindEnum(this, "Animations", "Edit Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            EditDropdownPosYCloseEase = BindEnum(this, "Animations", "Edit Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            EditDropdownScaActive = Bind(this, "Animations", "Edit Dropdown Animate Scale", true, "If scale should be animated.");
            EditDropdownScaOpen = Bind(this, "Animations", "Edit Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownScaClose = Bind(this, "Animations", "Edit Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownScaOpenDuration = Bind(this, "Animations", "Edit Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            EditDropdownScaCloseDuration = Bind(this, "Animations", "Edit Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            EditDropdownScaXOpenEase = BindEnum(this, "Animations", "Edit Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            EditDropdownScaXCloseEase = BindEnum(this, "Animations", "Edit Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            EditDropdownScaYOpenEase = BindEnum(this, "Animations", "Edit Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            EditDropdownScaYCloseEase = BindEnum(this, "Animations", "Edit Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            EditDropdownRotActive = Bind(this, "Animations", "Edit Dropdown Animate Rotation", false, "If rotation should be animated.");
            EditDropdownRotOpen = Bind(this, "Animations", "Edit Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownRotClose = Bind(this, "Animations", "Edit Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownRotOpenDuration = Bind(this, "Animations", "Edit Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            EditDropdownRotCloseDuration = Bind(this, "Animations", "Edit Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            EditDropdownRotOpenEase = BindEnum(this, "Animations", "Edit Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            EditDropdownRotCloseEase = BindEnum(this, "Animations", "Edit Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region View Dropdown

            ViewDropdownActive = Bind(this, "Animations", "View Dropdown Active", true, "If the popup animation should play.");

            ViewDropdownPosActive = Bind(this, "Animations", "View Dropdown Animate Position", false, "If position should be animated.");
            ViewDropdownPosOpen = Bind(this, "Animations", "View Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownPosClose = Bind(this, "Animations", "View Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownPosOpenDuration = Bind(this, "Animations", "View Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            ViewDropdownPosCloseDuration = Bind(this, "Animations", "View Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            ViewDropdownPosXOpenEase = BindEnum(this, "Animations", "View Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosXCloseEase = BindEnum(this, "Animations", "View Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosYOpenEase = BindEnum(this, "Animations", "View Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownPosYCloseEase = BindEnum(this, "Animations", "View Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            ViewDropdownScaActive = Bind(this, "Animations", "View Dropdown Animate Scale", true, "If scale should be animated.");
            ViewDropdownScaOpen = Bind(this, "Animations", "View Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownScaClose = Bind(this, "Animations", "View Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownScaOpenDuration = Bind(this, "Animations", "View Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ViewDropdownScaCloseDuration = Bind(this, "Animations", "View Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ViewDropdownScaXOpenEase = BindEnum(this, "Animations", "View Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            ViewDropdownScaXCloseEase = BindEnum(this, "Animations", "View Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            ViewDropdownScaYOpenEase = BindEnum(this, "Animations", "View Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            ViewDropdownScaYCloseEase = BindEnum(this, "Animations", "View Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            ViewDropdownRotActive = Bind(this, "Animations", "View Dropdown Animate Rotation", false, "If rotation should be animated.");
            ViewDropdownRotOpen = Bind(this, "Animations", "View Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownRotClose = Bind(this, "Animations", "View Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownRotOpenDuration = Bind(this, "Animations", "View Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            ViewDropdownRotCloseDuration = Bind(this, "Animations", "View Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            ViewDropdownRotOpenEase = BindEnum(this, "Animations", "View Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            ViewDropdownRotCloseEase = BindEnum(this, "Animations", "View Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Steam Dropdown

            SteamDropdownActive = Bind(this, "Animations", "Steam Dropdown Active", true, "If the popup animation should play.");

            SteamDropdownPosActive = Bind(this, "Animations", "Steam Dropdown Animate Position", false, "If position should be animated.");
            SteamDropdownPosOpen = Bind(this, "Animations", "Steam Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownPosClose = Bind(this, "Animations", "Steam Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownPosOpenDuration = Bind(this, "Animations", "Steam Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            SteamDropdownPosCloseDuration = Bind(this, "Animations", "Steam Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            SteamDropdownPosXOpenEase = BindEnum(this, "Animations", "Steam Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosXCloseEase = BindEnum(this, "Animations", "Steam Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosYOpenEase = BindEnum(this, "Animations", "Steam Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownPosYCloseEase = BindEnum(this, "Animations", "Steam Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            SteamDropdownScaActive = Bind(this, "Animations", "Steam Dropdown Animate Scale", true, "If scale should be animated.");
            SteamDropdownScaOpen = Bind(this, "Animations", "Steam Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownScaClose = Bind(this, "Animations", "Steam Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownScaOpenDuration = Bind(this, "Animations", "Steam Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SteamDropdownScaCloseDuration = Bind(this, "Animations", "Steam Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SteamDropdownScaXOpenEase = BindEnum(this, "Animations", "Steam Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            SteamDropdownScaXCloseEase = BindEnum(this, "Animations", "Steam Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            SteamDropdownScaYOpenEase = BindEnum(this, "Animations", "Steam Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            SteamDropdownScaYCloseEase = BindEnum(this, "Animations", "Steam Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            SteamDropdownRotActive = Bind(this, "Animations", "Steam Dropdown Animate Rotation", false, "If rotation should be animated.");
            SteamDropdownRotOpen = Bind(this, "Animations", "Steam Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownRotClose = Bind(this, "Animations", "Steam Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownRotOpenDuration = Bind(this, "Animations", "Steam Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            SteamDropdownRotCloseDuration = Bind(this, "Animations", "Steam Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            SteamDropdownRotOpenEase = BindEnum(this, "Animations", "Steam Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            SteamDropdownRotCloseEase = BindEnum(this, "Animations", "Steam Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #region Help Dropdown

            HelpDropdownActive = Bind(this, "Animations", "Help Dropdown Active", true, "If the popup animation should play.");

            HelpDropdownPosActive = Bind(this, "Animations", "Help Dropdown Animate Position", false, "If position should be animated.");
            HelpDropdownPosOpen = Bind(this, "Animations", "Help Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownPosClose = Bind(this, "Animations", "Help Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownPosOpenDuration = Bind(this, "Animations", "Help Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            HelpDropdownPosCloseDuration = Bind(this, "Animations", "Help Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            HelpDropdownPosXOpenEase = BindEnum(this, "Animations", "Help Dropdown Open Position X Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosXCloseEase = BindEnum(this, "Animations", "Help Dropdown Close Position X Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosYOpenEase = BindEnum(this, "Animations", "Help Dropdown Open Position Y Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownPosYCloseEase = BindEnum(this, "Animations", "Help Dropdown Close Position Y Ease", Easings.Linear, "The easing of opening.");

            HelpDropdownScaActive = Bind(this, "Animations", "Help Dropdown Animate Scale", true, "If scale should be animated.");
            HelpDropdownScaOpen = Bind(this, "Animations", "Help Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownScaClose = Bind(this, "Animations", "Help Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownScaOpenDuration = Bind(this, "Animations", "Help Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            HelpDropdownScaCloseDuration = Bind(this, "Animations", "Help Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            HelpDropdownScaXOpenEase = BindEnum(this, "Animations", "Help Dropdown Open Scale X Ease", Easings.OutElastic, "The easing of opening.");
            HelpDropdownScaXCloseEase = BindEnum(this, "Animations", "Help Dropdown Close Scale X Ease", Easings.InCirc, "The easing of opening.");
            HelpDropdownScaYOpenEase = BindEnum(this, "Animations", "Help Dropdown Open Scale Y Ease", Easings.OutElastic, "The easing of opening.");
            HelpDropdownScaYCloseEase = BindEnum(this, "Animations", "Help Dropdown Close Scale Y Ease", Easings.InCirc, "The easing of opening.");

            HelpDropdownRotActive = Bind(this, "Animations", "Help Dropdown Animate Rotation", false, "If rotation should be animated.");
            HelpDropdownRotOpen = Bind(this, "Animations", "Help Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownRotClose = Bind(this, "Animations", "Help Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownRotOpenDuration = Bind(this, "Animations", "Help Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            HelpDropdownRotCloseDuration = Bind(this, "Animations", "Help Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            HelpDropdownRotOpenEase = BindEnum(this, "Animations", "Help Dropdown Open Rotation Ease", Easings.Linear, "The easing of opening.");
            HelpDropdownRotCloseEase = BindEnum(this, "Animations", "Help Dropdown Close Rotation Ease", Easings.Linear, "The easing of opening.");

            #endregion

            #endregion

            #region Preview

            OnlyObjectsOnCurrentLayerVisible = Bind(this, "Preview", "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent");
            VisibleObjectOpacity = Bind(this, "Preview", "Visible Object Opacity", 0.2f, "Opacity of the objects not on the current layer.");
            ShowEmpties = Bind(this, "Preview", "Show Empties", false, "If enabled, show all objects that are set to the empty object type.");
            OnlyShowDamagable = Bind(this, "Preview", "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
            HighlightObjects = Bind(this, "Preview", "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
            ObjectHighlightAmount = Bind(this, "Preview", "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
            ObjectHighlightDoubleAmount = Bind(this, "Preview", "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.");
            ObjectDraggerEnabled = Bind(this, "Preview", "Object Dragger Enabled", false, "If an object can be dragged around.");
            ObjectDraggerCreatesKeyframe = Bind(this, "Preview", "Object Dragger Creates Keyframe", false, "When an object is dragged, create a keyframe.");
            ObjectDraggerRotatorRadius = Bind(this, "Preview", "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.");
            ObjectDraggerScalerOffset = Bind(this, "Preview", "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.");
            ObjectDraggerScalerScale = Bind(this, "Preview", "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.");

            PreviewGridEnabled = Bind(this, "Preview", "Grid Enabled", false, "If the preview grid should be enabled.");
            PreviewGridSize = Bind(this, "Preview", "Grid Size", 0.5f, "The overall size of the preview grid.");
            PreviewGridThickness = Bind(this, "Preview", "Grid Thickness", 0.1f, "The line thickness of the preview grid.", 0.01f, 2f);
            PreviewGridColor = Bind(this, "Preview", "Grid Color", Color.white, "The color of the preview grid.");

            #endregion

            Save();
        }

        #region Setting Changed

        public static Action UpdateEditorComplexity { get; set; }

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;

            TimelineGridEnabled.SettingChanged += TimelineGridChanged;
            TimelineGridThickness.SettingChanged += TimelineGridChanged;
            TimelineGridColor.SettingChanged += TimelineGridChanged;

            BPMSnapDivisions.SettingChanged += TimelineGridChanged;
            DragUI.SettingChanged += DragUIChanged;

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

            TimelineCollapseLength.SettingChanged += TimelineCollapseLengthChanged;

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
            {
                PrefabEditor.inst.ReloadInternalPrefabsInPopup();
            }
            if (PrefabEditor.inst.externalPrefabDialog.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < RTPrefabEditor.inst.PrefabPanels.Count; i++)
                {
                    var prefabPanel = RTPrefabEditor.inst.PrefabPanels[i];
                    prefabPanel.DeleteButton.transform.AsRT().anchoredPosition = PrefabExternalDeleteButtonPos.Value;
                    prefabPanel.DeleteButton.transform.AsRT().sizeDelta = PrefabExternalDeleteButtonSca.Value;

                    prefabPanel.Name.fontSize = PrefabExternalNameFontSize.Value;
                    prefabPanel.Name.horizontalOverflow = PrefabExternalNameHorizontalWrap.Value;
                    prefabPanel.Name.verticalOverflow = PrefabExternalNameVerticalWrap.Value;

                    prefabPanel.TypeText.fontSize = PrefabExternalTypeFontSize.Value;
                    prefabPanel.TypeText.horizontalOverflow = PrefabExternalTypeHorizontalWrap.Value;
                    prefabPanel.TypeText.verticalOverflow = PrefabExternalTypeVerticalWrap.Value;
                }
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
            ObjectEditor.TimelineCollapseLength = TimelineCollapseLength.Value;
            if (EditorTimeline.inst)
                EditorTimeline.inst.RenderTimelineObjects();
        }

        void ThemeEventKeyframeChanged()
        {
            RTThemeEditor.eventThemesPerPage = ThemesEventKeyframePerPage.Value;

            if (!CoreHelper.InEditor)
                return;

            var p = Mathf.Clamp(RTThemeEditor.inst.eventThemePage, 0, DataManager.inst.AllThemes.Count / RTThemeEditor.eventThemesPerPage).ToString();

            if (RTThemeEditor.eventPageStorage.inputField.text != p)
            {
                RTThemeEditor.eventPageStorage.inputField.text = p;
                return;
            }


            var dialogTmp = EventEditor.inst.dialogRight.GetChild(4);
            if (dialogTmp.gameObject.activeInHierarchy)
                CoreHelper.StartCoroutine(RTThemeEditor.inst.RenderThemeList(
                    dialogTmp.Find("theme-search").GetComponent<InputField>().text));
        }

        void AutosaveChanged()
        {
            if (CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel)
                RTEditor.inst.SetAutosave();
        }

        void EditorThemeChanged()
        {
            EditorThemeManager.currentTheme = (int)EditorTheme.Value;
            CoreHelper.StartCoroutine(EditorThemeManager.RenderElements());
        }

        void MarkerChanged() => RTMarkerEditor.inst?.RenderMarkers();

        void ModdedEditorChanged()
        {
            RTEditor.ShowModdedUI = EditorComplexity.Value == Complexity.Advanced;

            UpdateEditorComplexity?.Invoke();
            AdjustPositionInputsChanged?.Invoke();

            if (EditorTimeline.inst && EditorTimeline.inst.SelectedObjectCount == 1 && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                CoreHelper.StartCoroutine(ObjectEditor.inst.RefreshObjectGUI(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>()));

            if (RTEditor.inst && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderEventsDialog();
            }

            if (!RTPrefabEditor.inst)
                return;

            var prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");

            if (!prefabSelectorLeft.gameObject.activeInHierarchy)
                RTPrefabEditor.inst.UpdateModdedVisbility();
            else if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectDialog(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
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
                RTEditor.inst.StartCoroutine(EditorTimeline.inst.AssignTimelineTexture());
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
                CoreHelper.StartCoroutine(ObjectEditor.inst.RefreshObjectGUI(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>()));
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

            KeybindEditor.AllowKeys = AllowEditorKeybindsWithEditorCam.Value;
            ObjectEditor.RenderPrefabTypeIcon = TimelineObjectPrefabTypeIcon.Value;
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

            RTPlayer.ZenModeInEditor = EditorZenMode.Value;

            ObjectConverter.ShowEmpties = ShowEmpties.Value;
            ObjectConverter.ShowDamagable = OnlyShowDamagable.Value;
        }

        void TimelineColorsChanged()
        {
            if (CoreHelper.InEditor)
                EditorTimeline.inst.UpdateTimelineColors();
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
    }
}
