using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Components;
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
        public Setting<bool> CreateObjectsatCameraCenter { get; set; }
        public Setting<bool> SpawnPrefabsAtCameraCenter { get; set; }
        public Setting<bool> CreateObjectsScaleParentDefault { get; set; }
        public Setting<bool> CreateObjectModifierOrderDefault { get; set; }
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
        public Setting<bool> PasteBackgroundObjectsOverwrites { get; set; }
        public Setting<bool> ShowDefaultThemes { get; set; }
        public Setting<int> ImageSequenceFPS { get; set; }
        public Setting<bool> OverwriteImportedImages { get; set; }

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

        #region BPM

        public Setting<bool> BPMSnapsObjects { get; set; }
        public Setting<bool> BPMSnapsObjectKeyframes { get; set; }
        public Setting<bool> BPMSnapsKeyframes { get; set; }
        public Setting<bool> BPMSnapsCheckpoints { get; set; }
        public Setting<bool> BPMSnapsMarkers { get; set; }
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

        #region Open File Popup

        public Setting<bool> OpenFilePopupActive { get; set; }

        public Setting<bool> OpenFilePopupPosActive { get; set; }
        public Setting<Vector2> OpenFilePopupPosOpen { get; set; }
        public Setting<Vector2> OpenFilePopupPosClose { get; set; }
        public Setting<Vector2> OpenFilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> OpenFilePopupPosCloseDuration { get; set; }
        public Setting<Easing> OpenFilePopupPosXOpenEase { get; set; }
        public Setting<Easing> OpenFilePopupPosXCloseEase { get; set; }
        public Setting<Easing> OpenFilePopupPosYOpenEase { get; set; }
        public Setting<Easing> OpenFilePopupPosYCloseEase { get; set; }

        public Setting<bool> OpenFilePopupScaActive { get; set; }
        public Setting<Vector2> OpenFilePopupScaOpen { get; set; }
        public Setting<Vector2> OpenFilePopupScaClose { get; set; }
        public Setting<Vector2> OpenFilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> OpenFilePopupScaCloseDuration { get; set; }
        public Setting<Easing> OpenFilePopupScaXOpenEase { get; set; }
        public Setting<Easing> OpenFilePopupScaXCloseEase { get; set; }
        public Setting<Easing> OpenFilePopupScaYOpenEase { get; set; }
        public Setting<Easing> OpenFilePopupScaYCloseEase { get; set; }

        public Setting<bool> OpenFilePopupRotActive { get; set; }
        public Setting<float> OpenFilePopupRotOpen { get; set; }
        public Setting<float> OpenFilePopupRotClose { get; set; }
        public Setting<float> OpenFilePopupRotOpenDuration { get; set; }
        public Setting<float> OpenFilePopupRotCloseDuration { get; set; }
        public Setting<Easing> OpenFilePopupRotOpenEase { get; set; }
        public Setting<Easing> OpenFilePopupRotCloseEase { get; set; }

        #endregion

        #region New File Popup

        public Setting<bool> NewFilePopupActive { get; set; }

        public Setting<bool> NewFilePopupPosActive { get; set; }
        public Setting<Vector2> NewFilePopupPosOpen { get; set; }
        public Setting<Vector2> NewFilePopupPosClose { get; set; }
        public Setting<Vector2> NewFilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> NewFilePopupPosCloseDuration { get; set; }
        public Setting<Easing> NewFilePopupPosXOpenEase { get; set; }
        public Setting<Easing> NewFilePopupPosXCloseEase { get; set; }
        public Setting<Easing> NewFilePopupPosYOpenEase { get; set; }
        public Setting<Easing> NewFilePopupPosYCloseEase { get; set; }

        public Setting<bool> NewFilePopupScaActive { get; set; }
        public Setting<Vector2> NewFilePopupScaOpen { get; set; }
        public Setting<Vector2> NewFilePopupScaClose { get; set; }
        public Setting<Vector2> NewFilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> NewFilePopupScaCloseDuration { get; set; }
        public Setting<Easing> NewFilePopupScaXOpenEase { get; set; }
        public Setting<Easing> NewFilePopupScaXCloseEase { get; set; }
        public Setting<Easing> NewFilePopupScaYOpenEase { get; set; }
        public Setting<Easing> NewFilePopupScaYCloseEase { get; set; }

        public Setting<bool> NewFilePopupRotActive { get; set; }
        public Setting<float> NewFilePopupRotOpen { get; set; }
        public Setting<float> NewFilePopupRotClose { get; set; }
        public Setting<float> NewFilePopupRotOpenDuration { get; set; }
        public Setting<float> NewFilePopupRotCloseDuration { get; set; }
        public Setting<Easing> NewFilePopupRotOpenEase { get; set; }
        public Setting<Easing> NewFilePopupRotCloseEase { get; set; }

        #endregion

        #region Save As Popup

        public Setting<bool> SaveAsPopupActive { get; set; }

        public Setting<bool> SaveAsPopupPosActive { get; set; }
        public Setting<Vector2> SaveAsPopupPosOpen { get; set; }
        public Setting<Vector2> SaveAsPopupPosClose { get; set; }
        public Setting<Vector2> SaveAsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> SaveAsPopupPosCloseDuration { get; set; }
        public Setting<Easing> SaveAsPopupPosXOpenEase { get; set; }
        public Setting<Easing> SaveAsPopupPosXCloseEase { get; set; }
        public Setting<Easing> SaveAsPopupPosYOpenEase { get; set; }
        public Setting<Easing> SaveAsPopupPosYCloseEase { get; set; }

        public Setting<bool> SaveAsPopupScaActive { get; set; }
        public Setting<Vector2> SaveAsPopupScaOpen { get; set; }
        public Setting<Vector2> SaveAsPopupScaClose { get; set; }
        public Setting<Vector2> SaveAsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> SaveAsPopupScaCloseDuration { get; set; }
        public Setting<Easing> SaveAsPopupScaXOpenEase { get; set; }
        public Setting<Easing> SaveAsPopupScaXCloseEase { get; set; }
        public Setting<Easing> SaveAsPopupScaYOpenEase { get; set; }
        public Setting<Easing> SaveAsPopupScaYCloseEase { get; set; }

        public Setting<bool> SaveAsPopupRotActive { get; set; }
        public Setting<float> SaveAsPopupRotOpen { get; set; }
        public Setting<float> SaveAsPopupRotClose { get; set; }
        public Setting<float> SaveAsPopupRotOpenDuration { get; set; }
        public Setting<float> SaveAsPopupRotCloseDuration { get; set; }
        public Setting<Easing> SaveAsPopupRotOpenEase { get; set; }
        public Setting<Easing> SaveAsPopupRotCloseEase { get; set; }

        #endregion

        #region Quick Actions Popup

        public Setting<bool> QuickActionsPopupActive { get; set; }

        public Setting<bool> QuickActionsPopupPosActive { get; set; }
        public Setting<Vector2> QuickActionsPopupPosOpen { get; set; }
        public Setting<Vector2> QuickActionsPopupPosClose { get; set; }
        public Setting<Vector2> QuickActionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> QuickActionsPopupPosCloseDuration { get; set; }
        public Setting<Easing> QuickActionsPopupPosXOpenEase { get; set; }
        public Setting<Easing> QuickActionsPopupPosXCloseEase { get; set; }
        public Setting<Easing> QuickActionsPopupPosYOpenEase { get; set; }
        public Setting<Easing> QuickActionsPopupPosYCloseEase { get; set; }

        public Setting<bool> QuickActionsPopupScaActive { get; set; }
        public Setting<Vector2> QuickActionsPopupScaOpen { get; set; }
        public Setting<Vector2> QuickActionsPopupScaClose { get; set; }
        public Setting<Vector2> QuickActionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> QuickActionsPopupScaCloseDuration { get; set; }
        public Setting<Easing> QuickActionsPopupScaXOpenEase { get; set; }
        public Setting<Easing> QuickActionsPopupScaXCloseEase { get; set; }
        public Setting<Easing> QuickActionsPopupScaYOpenEase { get; set; }
        public Setting<Easing> QuickActionsPopupScaYCloseEase { get; set; }

        public Setting<bool> QuickActionsPopupRotActive { get; set; }
        public Setting<float> QuickActionsPopupRotOpen { get; set; }
        public Setting<float> QuickActionsPopupRotClose { get; set; }
        public Setting<float> QuickActionsPopupRotOpenDuration { get; set; }
        public Setting<float> QuickActionsPopupRotCloseDuration { get; set; }
        public Setting<Easing> QuickActionsPopupRotOpenEase { get; set; }
        public Setting<Easing> QuickActionsPopupRotCloseEase { get; set; }

        #endregion

        #region Parent Selector Popup

        public Setting<bool> ParentSelectorPopupActive { get; set; }

        public Setting<bool> ParentSelectorPopupPosActive { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosOpen { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosClose { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ParentSelectorPopupPosCloseDuration { get; set; }
        public Setting<Easing> ParentSelectorPopupPosXOpenEase { get; set; }
        public Setting<Easing> ParentSelectorPopupPosXCloseEase { get; set; }
        public Setting<Easing> ParentSelectorPopupPosYOpenEase { get; set; }
        public Setting<Easing> ParentSelectorPopupPosYCloseEase { get; set; }

        public Setting<bool> ParentSelectorPopupScaActive { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaOpen { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaClose { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ParentSelectorPopupScaCloseDuration { get; set; }
        public Setting<Easing> ParentSelectorPopupScaXOpenEase { get; set; }
        public Setting<Easing> ParentSelectorPopupScaXCloseEase { get; set; }
        public Setting<Easing> ParentSelectorPopupScaYOpenEase { get; set; }
        public Setting<Easing> ParentSelectorPopupScaYCloseEase { get; set; }

        public Setting<bool> ParentSelectorPopupRotActive { get; set; }
        public Setting<float> ParentSelectorPopupRotOpen { get; set; }
        public Setting<float> ParentSelectorPopupRotClose { get; set; }
        public Setting<float> ParentSelectorPopupRotOpenDuration { get; set; }
        public Setting<float> ParentSelectorPopupRotCloseDuration { get; set; }
        public Setting<Easing> ParentSelectorPopupRotOpenEase { get; set; }
        public Setting<Easing> ParentSelectorPopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Popup

        public Setting<bool> PrefabPopupActive { get; set; }

        public Setting<bool> PrefabPopupPosActive { get; set; }
        public Setting<Vector2> PrefabPopupPosOpen { get; set; }
        public Setting<Vector2> PrefabPopupPosClose { get; set; }
        public Setting<Vector2> PrefabPopupPosOpenDuration { get; set; }
        public Setting<Vector2> PrefabPopupPosCloseDuration { get; set; }
        public Setting<Easing> PrefabPopupPosXOpenEase { get; set; }
        public Setting<Easing> PrefabPopupPosXCloseEase { get; set; }
        public Setting<Easing> PrefabPopupPosYOpenEase { get; set; }
        public Setting<Easing> PrefabPopupPosYCloseEase { get; set; }

        public Setting<bool> PrefabPopupScaActive { get; set; }
        public Setting<Vector2> PrefabPopupScaOpen { get; set; }
        public Setting<Vector2> PrefabPopupScaClose { get; set; }
        public Setting<Vector2> PrefabPopupScaOpenDuration { get; set; }
        public Setting<Vector2> PrefabPopupScaCloseDuration { get; set; }
        public Setting<Easing> PrefabPopupScaXOpenEase { get; set; }
        public Setting<Easing> PrefabPopupScaXCloseEase { get; set; }
        public Setting<Easing> PrefabPopupScaYOpenEase { get; set; }
        public Setting<Easing> PrefabPopupScaYCloseEase { get; set; }

        public Setting<bool> PrefabPopupRotActive { get; set; }
        public Setting<float> PrefabPopupRotOpen { get; set; }
        public Setting<float> PrefabPopupRotClose { get; set; }
        public Setting<float> PrefabPopupRotOpenDuration { get; set; }
        public Setting<float> PrefabPopupRotCloseDuration { get; set; }
        public Setting<Easing> PrefabPopupRotOpenEase { get; set; }
        public Setting<Easing> PrefabPopupRotCloseEase { get; set; }

        #endregion

        #region Object Options Popup

        public Setting<bool> ObjectOptionsPopupActive { get; set; }

        public Setting<bool> ObjectOptionsPopupPosActive { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosOpen { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosClose { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectOptionsPopupPosCloseDuration { get; set; }
        public Setting<Easing> ObjectOptionsPopupPosXOpenEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupPosXCloseEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupPosYOpenEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectOptionsPopupScaActive { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaOpen { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaClose { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectOptionsPopupScaCloseDuration { get; set; }
        public Setting<Easing> ObjectOptionsPopupScaXOpenEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupScaXCloseEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupScaYOpenEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectOptionsPopupRotActive { get; set; }
        public Setting<float> ObjectOptionsPopupRotOpen { get; set; }
        public Setting<float> ObjectOptionsPopupRotClose { get; set; }
        public Setting<float> ObjectOptionsPopupRotOpenDuration { get; set; }
        public Setting<float> ObjectOptionsPopupRotCloseDuration { get; set; }
        public Setting<Easing> ObjectOptionsPopupRotOpenEase { get; set; }
        public Setting<Easing> ObjectOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region BG Options Popup

        public Setting<bool> BGOptionsPopupActive { get; set; }

        public Setting<bool> BGOptionsPopupPosActive { get; set; }
        public Setting<Vector2> BGOptionsPopupPosOpen { get; set; }
        public Setting<Vector2> BGOptionsPopupPosClose { get; set; }
        public Setting<Vector2> BGOptionsPopupPosOpenDuration { get; set; }
        public Setting<Vector2> BGOptionsPopupPosCloseDuration { get; set; }
        public Setting<Easing> BGOptionsPopupPosXOpenEase { get; set; }
        public Setting<Easing> BGOptionsPopupPosXCloseEase { get; set; }
        public Setting<Easing> BGOptionsPopupPosYOpenEase { get; set; }
        public Setting<Easing> BGOptionsPopupPosYCloseEase { get; set; }

        public Setting<bool> BGOptionsPopupScaActive { get; set; }
        public Setting<Vector2> BGOptionsPopupScaOpen { get; set; }
        public Setting<Vector2> BGOptionsPopupScaClose { get; set; }
        public Setting<Vector2> BGOptionsPopupScaOpenDuration { get; set; }
        public Setting<Vector2> BGOptionsPopupScaCloseDuration { get; set; }
        public Setting<Easing> BGOptionsPopupScaXOpenEase { get; set; }
        public Setting<Easing> BGOptionsPopupScaXCloseEase { get; set; }
        public Setting<Easing> BGOptionsPopupScaYOpenEase { get; set; }
        public Setting<Easing> BGOptionsPopupScaYCloseEase { get; set; }

        public Setting<bool> BGOptionsPopupRotActive { get; set; }
        public Setting<float> BGOptionsPopupRotOpen { get; set; }
        public Setting<float> BGOptionsPopupRotClose { get; set; }
        public Setting<float> BGOptionsPopupRotOpenDuration { get; set; }
        public Setting<float> BGOptionsPopupRotCloseDuration { get; set; }
        public Setting<Easing> BGOptionsPopupRotOpenEase { get; set; }
        public Setting<Easing> BGOptionsPopupRotCloseEase { get; set; }

        #endregion

        #region Browser Popup

        public Setting<bool> BrowserPopupActive { get; set; }

        public Setting<bool> BrowserPopupPosActive { get; set; }
        public Setting<Vector2> BrowserPopupPosOpen { get; set; }
        public Setting<Vector2> BrowserPopupPosClose { get; set; }
        public Setting<Vector2> BrowserPopupPosOpenDuration { get; set; }
        public Setting<Vector2> BrowserPopupPosCloseDuration { get; set; }
        public Setting<Easing> BrowserPopupPosXOpenEase { get; set; }
        public Setting<Easing> BrowserPopupPosXCloseEase { get; set; }
        public Setting<Easing> BrowserPopupPosYOpenEase { get; set; }
        public Setting<Easing> BrowserPopupPosYCloseEase { get; set; }

        public Setting<bool> BrowserPopupScaActive { get; set; }
        public Setting<Vector2> BrowserPopupScaOpen { get; set; }
        public Setting<Vector2> BrowserPopupScaClose { get; set; }
        public Setting<Vector2> BrowserPopupScaOpenDuration { get; set; }
        public Setting<Vector2> BrowserPopupScaCloseDuration { get; set; }
        public Setting<Easing> BrowserPopupScaXOpenEase { get; set; }
        public Setting<Easing> BrowserPopupScaXCloseEase { get; set; }
        public Setting<Easing> BrowserPopupScaYOpenEase { get; set; }
        public Setting<Easing> BrowserPopupScaYCloseEase { get; set; }

        public Setting<bool> BrowserPopupRotActive { get; set; }
        public Setting<float> BrowserPopupRotOpen { get; set; }
        public Setting<float> BrowserPopupRotClose { get; set; }
        public Setting<float> BrowserPopupRotOpenDuration { get; set; }
        public Setting<float> BrowserPopupRotCloseDuration { get; set; }
        public Setting<Easing> BrowserPopupRotOpenEase { get; set; }
        public Setting<Easing> BrowserPopupRotCloseEase { get; set; }

        #endregion

        #region Object Search Popup

        public Setting<bool> ObjectSearchPopupActive { get; set; }

        public Setting<bool> ObjectSearchPopupPosActive { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosOpen { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosClose { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectSearchPopupPosCloseDuration { get; set; }
        public Setting<Easing> ObjectSearchPopupPosXOpenEase { get; set; }
        public Setting<Easing> ObjectSearchPopupPosXCloseEase { get; set; }
        public Setting<Easing> ObjectSearchPopupPosYOpenEase { get; set; }
        public Setting<Easing> ObjectSearchPopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectSearchPopupScaActive { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaOpen { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaClose { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectSearchPopupScaCloseDuration { get; set; }
        public Setting<Easing> ObjectSearchPopupScaXOpenEase { get; set; }
        public Setting<Easing> ObjectSearchPopupScaXCloseEase { get; set; }
        public Setting<Easing> ObjectSearchPopupScaYOpenEase { get; set; }
        public Setting<Easing> ObjectSearchPopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectSearchPopupRotActive { get; set; }
        public Setting<float> ObjectSearchPopupRotOpen { get; set; }
        public Setting<float> ObjectSearchPopupRotClose { get; set; }
        public Setting<float> ObjectSearchPopupRotOpenDuration { get; set; }
        public Setting<float> ObjectSearchPopupRotCloseDuration { get; set; }
        public Setting<Easing> ObjectSearchPopupRotOpenEase { get; set; }
        public Setting<Easing> ObjectSearchPopupRotCloseEase { get; set; }

        #endregion

        #region Object Template Popup

        public Setting<bool> ObjectTemplatePopupActive { get; set; }

        public Setting<bool> ObjectTemplatePopupPosActive { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosOpen { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosClose { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosOpenDuration { get; set; }
        public Setting<Vector2> ObjectTemplatePopupPosCloseDuration { get; set; }
        public Setting<Easing> ObjectTemplatePopupPosXOpenEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupPosXCloseEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupPosYOpenEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupPosYCloseEase { get; set; }

        public Setting<bool> ObjectTemplatePopupScaActive { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaOpen { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaClose { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaOpenDuration { get; set; }
        public Setting<Vector2> ObjectTemplatePopupScaCloseDuration { get; set; }
        public Setting<Easing> ObjectTemplatePopupScaXOpenEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupScaXCloseEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupScaYOpenEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupScaYCloseEase { get; set; }

        public Setting<bool> ObjectTemplatePopupRotActive { get; set; }
        public Setting<float> ObjectTemplatePopupRotOpen { get; set; }
        public Setting<float> ObjectTemplatePopupRotClose { get; set; }
        public Setting<float> ObjectTemplatePopupRotOpenDuration { get; set; }
        public Setting<float> ObjectTemplatePopupRotCloseDuration { get; set; }
        public Setting<Easing> ObjectTemplatePopupRotOpenEase { get; set; }
        public Setting<Easing> ObjectTemplatePopupRotCloseEase { get; set; }

        #endregion

        #region Warning Popup

        public Setting<bool> WarningPopupActive { get; set; }

        public Setting<bool> WarningPopupPosActive { get; set; }
        public Setting<Vector2> WarningPopupPosOpen { get; set; }
        public Setting<Vector2> WarningPopupPosClose { get; set; }
        public Setting<Vector2> WarningPopupPosOpenDuration { get; set; }
        public Setting<Vector2> WarningPopupPosCloseDuration { get; set; }
        public Setting<Easing> WarningPopupPosXOpenEase { get; set; }
        public Setting<Easing> WarningPopupPosXCloseEase { get; set; }
        public Setting<Easing> WarningPopupPosYOpenEase { get; set; }
        public Setting<Easing> WarningPopupPosYCloseEase { get; set; }

        public Setting<bool> WarningPopupScaActive { get; set; }
        public Setting<Vector2> WarningPopupScaOpen { get; set; }
        public Setting<Vector2> WarningPopupScaClose { get; set; }
        public Setting<Vector2> WarningPopupScaOpenDuration { get; set; }
        public Setting<Vector2> WarningPopupScaCloseDuration { get; set; }
        public Setting<Easing> WarningPopupScaXOpenEase { get; set; }
        public Setting<Easing> WarningPopupScaXCloseEase { get; set; }
        public Setting<Easing> WarningPopupScaYOpenEase { get; set; }
        public Setting<Easing> WarningPopupScaYCloseEase { get; set; }

        public Setting<bool> WarningPopupRotActive { get; set; }
        public Setting<float> WarningPopupRotOpen { get; set; }
        public Setting<float> WarningPopupRotClose { get; set; }
        public Setting<float> WarningPopupRotOpenDuration { get; set; }
        public Setting<float> WarningPopupRotCloseDuration { get; set; }
        public Setting<Easing> WarningPopupRotOpenEase { get; set; }
        public Setting<Easing> WarningPopupRotCloseEase { get; set; }

        #endregion

        #region File Popup

        public Setting<bool> FilePopupActive { get; set; }

        public Setting<bool> FilePopupPosActive { get; set; }
        public Setting<Vector2> FilePopupPosOpen { get; set; }
        public Setting<Vector2> FilePopupPosClose { get; set; }
        public Setting<Vector2> FilePopupPosOpenDuration { get; set; }
        public Setting<Vector2> FilePopupPosCloseDuration { get; set; }
        public Setting<Easing> FilePopupPosXOpenEase { get; set; }
        public Setting<Easing> FilePopupPosXCloseEase { get; set; }
        public Setting<Easing> FilePopupPosYOpenEase { get; set; }
        public Setting<Easing> FilePopupPosYCloseEase { get; set; }

        public Setting<bool> FilePopupScaActive { get; set; }
        public Setting<Vector2> FilePopupScaOpen { get; set; }
        public Setting<Vector2> FilePopupScaClose { get; set; }
        public Setting<Vector2> FilePopupScaOpenDuration { get; set; }
        public Setting<Vector2> FilePopupScaCloseDuration { get; set; }
        public Setting<Easing> FilePopupScaXOpenEase { get; set; }
        public Setting<Easing> FilePopupScaXCloseEase { get; set; }
        public Setting<Easing> FilePopupScaYOpenEase { get; set; }
        public Setting<Easing> FilePopupScaYCloseEase { get; set; }

        public Setting<bool> FilePopupRotActive { get; set; }
        public Setting<float> FilePopupRotOpen { get; set; }
        public Setting<float> FilePopupRotClose { get; set; }
        public Setting<float> FilePopupRotOpenDuration { get; set; }
        public Setting<float> FilePopupRotCloseDuration { get; set; }
        public Setting<Easing> FilePopupRotOpenEase { get; set; }
        public Setting<Easing> FilePopupRotCloseEase { get; set; }

        #endregion

        #region Text Editor

        public Setting<bool> TextEditorActive { get; set; }

        public Setting<bool> TextEditorPosActive { get; set; }
        public Setting<Vector2> TextEditorPosOpen { get; set; }
        public Setting<Vector2> TextEditorPosClose { get; set; }
        public Setting<Vector2> TextEditorPosOpenDuration { get; set; }
        public Setting<Vector2> TextEditorPosCloseDuration { get; set; }
        public Setting<Easing> TextEditorPosXOpenEase { get; set; }
        public Setting<Easing> TextEditorPosXCloseEase { get; set; }
        public Setting<Easing> TextEditorPosYOpenEase { get; set; }
        public Setting<Easing> TextEditorPosYCloseEase { get; set; }

        public Setting<bool> TextEditorScaActive { get; set; }
        public Setting<Vector2> TextEditorScaOpen { get; set; }
        public Setting<Vector2> TextEditorScaClose { get; set; }
        public Setting<Vector2> TextEditorScaOpenDuration { get; set; }
        public Setting<Vector2> TextEditorScaCloseDuration { get; set; }
        public Setting<Easing> TextEditorScaXOpenEase { get; set; }
        public Setting<Easing> TextEditorScaXCloseEase { get; set; }
        public Setting<Easing> TextEditorScaYOpenEase { get; set; }
        public Setting<Easing> TextEditorScaYCloseEase { get; set; }

        public Setting<bool> TextEditorRotActive { get; set; }
        public Setting<float> TextEditorRotOpen { get; set; }
        public Setting<float> TextEditorRotClose { get; set; }
        public Setting<float> TextEditorRotOpenDuration { get; set; }
        public Setting<float> TextEditorRotCloseDuration { get; set; }
        public Setting<Easing> TextEditorRotOpenEase { get; set; }
        public Setting<Easing> TextEditorRotCloseEase { get; set; }

        #endregion

        #region Documentation Popup

        public Setting<bool> DocumentationPopupActive { get; set; }

        public Setting<bool> DocumentationPopupPosActive { get; set; }
        public Setting<Vector2> DocumentationPopupPosOpen { get; set; }
        public Setting<Vector2> DocumentationPopupPosClose { get; set; }
        public Setting<Vector2> DocumentationPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DocumentationPopupPosCloseDuration { get; set; }
        public Setting<Easing> DocumentationPopupPosXOpenEase { get; set; }
        public Setting<Easing> DocumentationPopupPosXCloseEase { get; set; }
        public Setting<Easing> DocumentationPopupPosYOpenEase { get; set; }
        public Setting<Easing> DocumentationPopupPosYCloseEase { get; set; }

        public Setting<bool> DocumentationPopupScaActive { get; set; }
        public Setting<Vector2> DocumentationPopupScaOpen { get; set; }
        public Setting<Vector2> DocumentationPopupScaClose { get; set; }
        public Setting<Vector2> DocumentationPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DocumentationPopupScaCloseDuration { get; set; }
        public Setting<Easing> DocumentationPopupScaXOpenEase { get; set; }
        public Setting<Easing> DocumentationPopupScaXCloseEase { get; set; }
        public Setting<Easing> DocumentationPopupScaYOpenEase { get; set; }
        public Setting<Easing> DocumentationPopupScaYCloseEase { get; set; }

        public Setting<bool> DocumentationPopupRotActive { get; set; }
        public Setting<float> DocumentationPopupRotOpen { get; set; }
        public Setting<float> DocumentationPopupRotClose { get; set; }
        public Setting<float> DocumentationPopupRotOpenDuration { get; set; }
        public Setting<float> DocumentationPopupRotCloseDuration { get; set; }
        public Setting<Easing> DocumentationPopupRotOpenEase { get; set; }
        public Setting<Easing> DocumentationPopupRotCloseEase { get; set; }

        #endregion

        #region Debugger Popup

        public Setting<bool> DebuggerPopupActive { get; set; }

        public Setting<bool> DebuggerPopupPosActive { get; set; }
        public Setting<Vector2> DebuggerPopupPosOpen { get; set; }
        public Setting<Vector2> DebuggerPopupPosClose { get; set; }
        public Setting<Vector2> DebuggerPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DebuggerPopupPosCloseDuration { get; set; }
        public Setting<Easing> DebuggerPopupPosXOpenEase { get; set; }
        public Setting<Easing> DebuggerPopupPosXCloseEase { get; set; }
        public Setting<Easing> DebuggerPopupPosYOpenEase { get; set; }
        public Setting<Easing> DebuggerPopupPosYCloseEase { get; set; }

        public Setting<bool> DebuggerPopupScaActive { get; set; }
        public Setting<Vector2> DebuggerPopupScaOpen { get; set; }
        public Setting<Vector2> DebuggerPopupScaClose { get; set; }
        public Setting<Vector2> DebuggerPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DebuggerPopupScaCloseDuration { get; set; }
        public Setting<Easing> DebuggerPopupScaXOpenEase { get; set; }
        public Setting<Easing> DebuggerPopupScaXCloseEase { get; set; }
        public Setting<Easing> DebuggerPopupScaYOpenEase { get; set; }
        public Setting<Easing> DebuggerPopupScaYCloseEase { get; set; }

        public Setting<bool> DebuggerPopupRotActive { get; set; }
        public Setting<float> DebuggerPopupRotOpen { get; set; }
        public Setting<float> DebuggerPopupRotClose { get; set; }
        public Setting<float> DebuggerPopupRotOpenDuration { get; set; }
        public Setting<float> DebuggerPopupRotCloseDuration { get; set; }
        public Setting<Easing> DebuggerPopupRotOpenEase { get; set; }
        public Setting<Easing> DebuggerPopupRotCloseEase { get; set; }

        #endregion

        #region Autosaves Popup

        public Setting<bool> AutosavesPopupActive { get; set; }

        public Setting<bool> AutosavesPopupPosActive { get; set; }
        public Setting<Vector2> AutosavesPopupPosOpen { get; set; }
        public Setting<Vector2> AutosavesPopupPosClose { get; set; }
        public Setting<Vector2> AutosavesPopupPosOpenDuration { get; set; }
        public Setting<Vector2> AutosavesPopupPosCloseDuration { get; set; }
        public Setting<Easing> AutosavesPopupPosXOpenEase { get; set; }
        public Setting<Easing> AutosavesPopupPosXCloseEase { get; set; }
        public Setting<Easing> AutosavesPopupPosYOpenEase { get; set; }
        public Setting<Easing> AutosavesPopupPosYCloseEase { get; set; }

        public Setting<bool> AutosavesPopupScaActive { get; set; }
        public Setting<Vector2> AutosavesPopupScaOpen { get; set; }
        public Setting<Vector2> AutosavesPopupScaClose { get; set; }
        public Setting<Vector2> AutosavesPopupScaOpenDuration { get; set; }
        public Setting<Vector2> AutosavesPopupScaCloseDuration { get; set; }
        public Setting<Easing> AutosavesPopupScaXOpenEase { get; set; }
        public Setting<Easing> AutosavesPopupScaXCloseEase { get; set; }
        public Setting<Easing> AutosavesPopupScaYOpenEase { get; set; }
        public Setting<Easing> AutosavesPopupScaYCloseEase { get; set; }

        public Setting<bool> AutosavesPopupRotActive { get; set; }
        public Setting<float> AutosavesPopupRotOpen { get; set; }
        public Setting<float> AutosavesPopupRotClose { get; set; }
        public Setting<float> AutosavesPopupRotOpenDuration { get; set; }
        public Setting<float> AutosavesPopupRotCloseDuration { get; set; }
        public Setting<Easing> AutosavesPopupRotOpenEase { get; set; }
        public Setting<Easing> AutosavesPopupRotCloseEase { get; set; }

        #endregion

        #region Default Modifiers Popup

        public Setting<bool> DefaultModifiersPopupActive { get; set; }

        public Setting<bool> DefaultModifiersPopupPosActive { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosOpen { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosClose { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosOpenDuration { get; set; }
        public Setting<Vector2> DefaultModifiersPopupPosCloseDuration { get; set; }
        public Setting<Easing> DefaultModifiersPopupPosXOpenEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupPosXCloseEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupPosYOpenEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupPosYCloseEase { get; set; }

        public Setting<bool> DefaultModifiersPopupScaActive { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaOpen { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaClose { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaOpenDuration { get; set; }
        public Setting<Vector2> DefaultModifiersPopupScaCloseDuration { get; set; }
        public Setting<Easing> DefaultModifiersPopupScaXOpenEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupScaXCloseEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupScaYOpenEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupScaYCloseEase { get; set; }

        public Setting<bool> DefaultModifiersPopupRotActive { get; set; }
        public Setting<float> DefaultModifiersPopupRotOpen { get; set; }
        public Setting<float> DefaultModifiersPopupRotClose { get; set; }
        public Setting<float> DefaultModifiersPopupRotOpenDuration { get; set; }
        public Setting<float> DefaultModifiersPopupRotCloseDuration { get; set; }
        public Setting<Easing> DefaultModifiersPopupRotOpenEase { get; set; }
        public Setting<Easing> DefaultModifiersPopupRotCloseEase { get; set; }

        #endregion

        #region Keybind List Popup

        public Setting<bool> KeybindListPopupActive { get; set; }

        public Setting<bool> KeybindListPopupPosActive { get; set; }
        public Setting<Vector2> KeybindListPopupPosOpen { get; set; }
        public Setting<Vector2> KeybindListPopupPosClose { get; set; }
        public Setting<Vector2> KeybindListPopupPosOpenDuration { get; set; }
        public Setting<Vector2> KeybindListPopupPosCloseDuration { get; set; }
        public Setting<Easing> KeybindListPopupPosXOpenEase { get; set; }
        public Setting<Easing> KeybindListPopupPosXCloseEase { get; set; }
        public Setting<Easing> KeybindListPopupPosYOpenEase { get; set; }
        public Setting<Easing> KeybindListPopupPosYCloseEase { get; set; }

        public Setting<bool> KeybindListPopupScaActive { get; set; }
        public Setting<Vector2> KeybindListPopupScaOpen { get; set; }
        public Setting<Vector2> KeybindListPopupScaClose { get; set; }
        public Setting<Vector2> KeybindListPopupScaOpenDuration { get; set; }
        public Setting<Vector2> KeybindListPopupScaCloseDuration { get; set; }
        public Setting<Easing> KeybindListPopupScaXOpenEase { get; set; }
        public Setting<Easing> KeybindListPopupScaXCloseEase { get; set; }
        public Setting<Easing> KeybindListPopupScaYOpenEase { get; set; }
        public Setting<Easing> KeybindListPopupScaYCloseEase { get; set; }

        public Setting<bool> KeybindListPopupRotActive { get; set; }
        public Setting<float> KeybindListPopupRotOpen { get; set; }
        public Setting<float> KeybindListPopupRotClose { get; set; }
        public Setting<float> KeybindListPopupRotOpenDuration { get; set; }
        public Setting<float> KeybindListPopupRotCloseDuration { get; set; }
        public Setting<Easing> KeybindListPopupRotOpenEase { get; set; }
        public Setting<Easing> KeybindListPopupRotCloseEase { get; set; }

        #endregion

        #region Theme Popup

        public Setting<bool> ThemePopupActive { get; set; }

        public Setting<bool> ThemePopupPosActive { get; set; }
        public Setting<Vector2> ThemePopupPosOpen { get; set; }
        public Setting<Vector2> ThemePopupPosClose { get; set; }
        public Setting<Vector2> ThemePopupPosOpenDuration { get; set; }
        public Setting<Vector2> ThemePopupPosCloseDuration { get; set; }
        public Setting<Easing> ThemePopupPosXOpenEase { get; set; }
        public Setting<Easing> ThemePopupPosXCloseEase { get; set; }
        public Setting<Easing> ThemePopupPosYOpenEase { get; set; }
        public Setting<Easing> ThemePopupPosYCloseEase { get; set; }

        public Setting<bool> ThemePopupScaActive { get; set; }
        public Setting<Vector2> ThemePopupScaOpen { get; set; }
        public Setting<Vector2> ThemePopupScaClose { get; set; }
        public Setting<Vector2> ThemePopupScaOpenDuration { get; set; }
        public Setting<Vector2> ThemePopupScaCloseDuration { get; set; }
        public Setting<Easing> ThemePopupScaXOpenEase { get; set; }
        public Setting<Easing> ThemePopupScaXCloseEase { get; set; }
        public Setting<Easing> ThemePopupScaYOpenEase { get; set; }
        public Setting<Easing> ThemePopupScaYCloseEase { get; set; }

        public Setting<bool> ThemePopupRotActive { get; set; }
        public Setting<float> ThemePopupRotOpen { get; set; }
        public Setting<float> ThemePopupRotClose { get; set; }
        public Setting<float> ThemePopupRotOpenDuration { get; set; }
        public Setting<float> ThemePopupRotCloseDuration { get; set; }
        public Setting<Easing> ThemePopupRotOpenEase { get; set; }
        public Setting<Easing> ThemePopupRotCloseEase { get; set; }

        #endregion

        #region Prefab Types Popup

        public Setting<bool> PrefabTypesPopupActive { get; set; }

        public Setting<bool> PrefabTypesPopupPosActive { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosOpen { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosClose { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosOpenDuration { get; set; }
        public Setting<Vector2> PrefabTypesPopupPosCloseDuration { get; set; }
        public Setting<Easing> PrefabTypesPopupPosXOpenEase { get; set; }
        public Setting<Easing> PrefabTypesPopupPosXCloseEase { get; set; }
        public Setting<Easing> PrefabTypesPopupPosYOpenEase { get; set; }
        public Setting<Easing> PrefabTypesPopupPosYCloseEase { get; set; }

        public Setting<bool> PrefabTypesPopupScaActive { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaOpen { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaClose { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaOpenDuration { get; set; }
        public Setting<Vector2> PrefabTypesPopupScaCloseDuration { get; set; }
        public Setting<Easing> PrefabTypesPopupScaXOpenEase { get; set; }
        public Setting<Easing> PrefabTypesPopupScaXCloseEase { get; set; }
        public Setting<Easing> PrefabTypesPopupScaYOpenEase { get; set; }
        public Setting<Easing> PrefabTypesPopupScaYCloseEase { get; set; }

        public Setting<bool> PrefabTypesPopupRotActive { get; set; }
        public Setting<float> PrefabTypesPopupRotOpen { get; set; }
        public Setting<float> PrefabTypesPopupRotClose { get; set; }
        public Setting<float> PrefabTypesPopupRotOpenDuration { get; set; }
        public Setting<float> PrefabTypesPopupRotCloseDuration { get; set; }
        public Setting<Easing> PrefabTypesPopupRotOpenEase { get; set; }
        public Setting<Easing> PrefabTypesPopupRotCloseEase { get; set; }

        #endregion

        #region Font Selector Popup

        public Setting<bool> FontSelectorPopupActive { get; set; }

        public Setting<bool> FontSelectorPopupPosActive { get; set; }
        public Setting<Vector2> FontSelectorPopupPosOpen { get; set; }
        public Setting<Vector2> FontSelectorPopupPosClose { get; set; }
        public Setting<Vector2> FontSelectorPopupPosOpenDuration { get; set; }
        public Setting<Vector2> FontSelectorPopupPosCloseDuration { get; set; }
        public Setting<Easing> FontSelectorPopupPosXOpenEase { get; set; }
        public Setting<Easing> FontSelectorPopupPosXCloseEase { get; set; }
        public Setting<Easing> FontSelectorPopupPosYOpenEase { get; set; }
        public Setting<Easing> FontSelectorPopupPosYCloseEase { get; set; }

        public Setting<bool> FontSelectorPopupScaActive { get; set; }
        public Setting<Vector2> FontSelectorPopupScaOpen { get; set; }
        public Setting<Vector2> FontSelectorPopupScaClose { get; set; }
        public Setting<Vector2> FontSelectorPopupScaOpenDuration { get; set; }
        public Setting<Vector2> FontSelectorPopupScaCloseDuration { get; set; }
        public Setting<Easing> FontSelectorPopupScaXOpenEase { get; set; }
        public Setting<Easing> FontSelectorPopupScaXCloseEase { get; set; }
        public Setting<Easing> FontSelectorPopupScaYOpenEase { get; set; }
        public Setting<Easing> FontSelectorPopupScaYCloseEase { get; set; }

        public Setting<bool> FontSelectorPopupRotActive { get; set; }
        public Setting<float> FontSelectorPopupRotOpen { get; set; }
        public Setting<float> FontSelectorPopupRotClose { get; set; }
        public Setting<float> FontSelectorPopupRotOpenDuration { get; set; }
        public Setting<float> FontSelectorPopupRotCloseDuration { get; set; }
        public Setting<Easing> FontSelectorPopupRotOpenEase { get; set; }
        public Setting<Easing> FontSelectorPopupRotCloseEase { get; set; }

        #endregion

        #region Pinned Editor Layer Popup

        public Setting<bool> PinnedEditorLayerPopupActive { get; set; }

        public Setting<bool> PinnedEditorLayerPopupPosActive { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupPosOpen { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupPosClose { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupPosOpenDuration { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupPosCloseDuration { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupPosXOpenEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupPosXCloseEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupPosYOpenEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupPosYCloseEase { get; set; }

        public Setting<bool> PinnedEditorLayerPopupScaActive { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupScaOpen { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupScaClose { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupScaOpenDuration { get; set; }
        public Setting<Vector2> PinnedEditorLayerPopupScaCloseDuration { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupScaXOpenEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupScaXCloseEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupScaYOpenEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupScaYCloseEase { get; set; }

        public Setting<bool> PinnedEditorLayerPopupRotActive { get; set; }
        public Setting<float> PinnedEditorLayerPopupRotOpen { get; set; }
        public Setting<float> PinnedEditorLayerPopupRotClose { get; set; }
        public Setting<float> PinnedEditorLayerPopupRotOpenDuration { get; set; }
        public Setting<float> PinnedEditorLayerPopupRotCloseDuration { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupRotOpenEase { get; set; }
        public Setting<Easing> PinnedEditorLayerPopupRotCloseEase { get; set; }

        #endregion

        #region Color Picker Popup

        public Setting<bool> ColorPickerPopupActive { get; set; }

        public Setting<bool> ColorPickerPopupPosActive { get; set; }
        public Setting<Vector2> ColorPickerPopupPosOpen { get; set; }
        public Setting<Vector2> ColorPickerPopupPosClose { get; set; }
        public Setting<Vector2> ColorPickerPopupPosOpenDuration { get; set; }
        public Setting<Vector2> ColorPickerPopupPosCloseDuration { get; set; }
        public Setting<Easing> ColorPickerPopupPosXOpenEase { get; set; }
        public Setting<Easing> ColorPickerPopupPosXCloseEase { get; set; }
        public Setting<Easing> ColorPickerPopupPosYOpenEase { get; set; }
        public Setting<Easing> ColorPickerPopupPosYCloseEase { get; set; }

        public Setting<bool> ColorPickerPopupScaActive { get; set; }
        public Setting<Vector2> ColorPickerPopupScaOpen { get; set; }
        public Setting<Vector2> ColorPickerPopupScaClose { get; set; }
        public Setting<Vector2> ColorPickerPopupScaOpenDuration { get; set; }
        public Setting<Vector2> ColorPickerPopupScaCloseDuration { get; set; }
        public Setting<Easing> ColorPickerPopupScaXOpenEase { get; set; }
        public Setting<Easing> ColorPickerPopupScaXCloseEase { get; set; }
        public Setting<Easing> ColorPickerPopupScaYOpenEase { get; set; }
        public Setting<Easing> ColorPickerPopupScaYCloseEase { get; set; }

        public Setting<bool> ColorPickerPopupRotActive { get; set; }
        public Setting<float> ColorPickerPopupRotOpen { get; set; }
        public Setting<float> ColorPickerPopupRotClose { get; set; }
        public Setting<float> ColorPickerPopupRotOpenDuration { get; set; }
        public Setting<float> ColorPickerPopupRotCloseDuration { get; set; }
        public Setting<Easing> ColorPickerPopupRotOpenEase { get; set; }
        public Setting<Easing> ColorPickerPopupRotCloseEase { get; set; }

        #endregion

        #region File Dropdown

        public Setting<bool> FileDropdownActive { get; set; }

        public Setting<bool> FileDropdownPosActive { get; set; }
        public Setting<Vector2> FileDropdownPosOpen { get; set; }
        public Setting<Vector2> FileDropdownPosClose { get; set; }
        public Setting<Vector2> FileDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> FileDropdownPosCloseDuration { get; set; }
        public Setting<Easing> FileDropdownPosXOpenEase { get; set; }
        public Setting<Easing> FileDropdownPosXCloseEase { get; set; }
        public Setting<Easing> FileDropdownPosYOpenEase { get; set; }
        public Setting<Easing> FileDropdownPosYCloseEase { get; set; }

        public Setting<bool> FileDropdownScaActive { get; set; }
        public Setting<Vector2> FileDropdownScaOpen { get; set; }
        public Setting<Vector2> FileDropdownScaClose { get; set; }
        public Setting<Vector2> FileDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> FileDropdownScaCloseDuration { get; set; }
        public Setting<Easing> FileDropdownScaXOpenEase { get; set; }
        public Setting<Easing> FileDropdownScaXCloseEase { get; set; }
        public Setting<Easing> FileDropdownScaYOpenEase { get; set; }
        public Setting<Easing> FileDropdownScaYCloseEase { get; set; }

        public Setting<bool> FileDropdownRotActive { get; set; }
        public Setting<float> FileDropdownRotOpen { get; set; }
        public Setting<float> FileDropdownRotClose { get; set; }
        public Setting<float> FileDropdownRotOpenDuration { get; set; }
        public Setting<float> FileDropdownRotCloseDuration { get; set; }
        public Setting<Easing> FileDropdownRotOpenEase { get; set; }
        public Setting<Easing> FileDropdownRotCloseEase { get; set; }

        #endregion

        #region Edit Dropdown

        public Setting<bool> EditDropdownActive { get; set; }

        public Setting<bool> EditDropdownPosActive { get; set; }
        public Setting<Vector2> EditDropdownPosOpen { get; set; }
        public Setting<Vector2> EditDropdownPosClose { get; set; }
        public Setting<Vector2> EditDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> EditDropdownPosCloseDuration { get; set; }
        public Setting<Easing> EditDropdownPosXOpenEase { get; set; }
        public Setting<Easing> EditDropdownPosXCloseEase { get; set; }
        public Setting<Easing> EditDropdownPosYOpenEase { get; set; }
        public Setting<Easing> EditDropdownPosYCloseEase { get; set; }

        public Setting<bool> EditDropdownScaActive { get; set; }
        public Setting<Vector2> EditDropdownScaOpen { get; set; }
        public Setting<Vector2> EditDropdownScaClose { get; set; }
        public Setting<Vector2> EditDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> EditDropdownScaCloseDuration { get; set; }
        public Setting<Easing> EditDropdownScaXOpenEase { get; set; }
        public Setting<Easing> EditDropdownScaXCloseEase { get; set; }
        public Setting<Easing> EditDropdownScaYOpenEase { get; set; }
        public Setting<Easing> EditDropdownScaYCloseEase { get; set; }

        public Setting<bool> EditDropdownRotActive { get; set; }
        public Setting<float> EditDropdownRotOpen { get; set; }
        public Setting<float> EditDropdownRotClose { get; set; }
        public Setting<float> EditDropdownRotOpenDuration { get; set; }
        public Setting<float> EditDropdownRotCloseDuration { get; set; }
        public Setting<Easing> EditDropdownRotOpenEase { get; set; }
        public Setting<Easing> EditDropdownRotCloseEase { get; set; }

        #endregion

        #region View Dropdown

        public Setting<bool> ViewDropdownActive { get; set; }

        public Setting<bool> ViewDropdownPosActive { get; set; }
        public Setting<Vector2> ViewDropdownPosOpen { get; set; }
        public Setting<Vector2> ViewDropdownPosClose { get; set; }
        public Setting<Vector2> ViewDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> ViewDropdownPosCloseDuration { get; set; }
        public Setting<Easing> ViewDropdownPosXOpenEase { get; set; }
        public Setting<Easing> ViewDropdownPosXCloseEase { get; set; }
        public Setting<Easing> ViewDropdownPosYOpenEase { get; set; }
        public Setting<Easing> ViewDropdownPosYCloseEase { get; set; }

        public Setting<bool> ViewDropdownScaActive { get; set; }
        public Setting<Vector2> ViewDropdownScaOpen { get; set; }
        public Setting<Vector2> ViewDropdownScaClose { get; set; }
        public Setting<Vector2> ViewDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> ViewDropdownScaCloseDuration { get; set; }
        public Setting<Easing> ViewDropdownScaXOpenEase { get; set; }
        public Setting<Easing> ViewDropdownScaXCloseEase { get; set; }
        public Setting<Easing> ViewDropdownScaYOpenEase { get; set; }
        public Setting<Easing> ViewDropdownScaYCloseEase { get; set; }

        public Setting<bool> ViewDropdownRotActive { get; set; }
        public Setting<float> ViewDropdownRotOpen { get; set; }
        public Setting<float> ViewDropdownRotClose { get; set; }
        public Setting<float> ViewDropdownRotOpenDuration { get; set; }
        public Setting<float> ViewDropdownRotCloseDuration { get; set; }
        public Setting<Easing> ViewDropdownRotOpenEase { get; set; }
        public Setting<Easing> ViewDropdownRotCloseEase { get; set; }

        #endregion

        #region Settings Dropdown

        public Setting<bool> SettingsDropdownActive { get; set; }

        public Setting<bool> SettingsDropdownPosActive { get; set; }
        public Setting<Vector2> SettingsDropdownPosOpen { get; set; }
        public Setting<Vector2> SettingsDropdownPosClose { get; set; }
        public Setting<Vector2> SettingsDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> SettingsDropdownPosCloseDuration { get; set; }
        public Setting<Easing> SettingsDropdownPosXOpenEase { get; set; }
        public Setting<Easing> SettingsDropdownPosXCloseEase { get; set; }
        public Setting<Easing> SettingsDropdownPosYOpenEase { get; set; }
        public Setting<Easing> SettingsDropdownPosYCloseEase { get; set; }

        public Setting<bool> SettingsDropdownScaActive { get; set; }
        public Setting<Vector2> SettingsDropdownScaOpen { get; set; }
        public Setting<Vector2> SettingsDropdownScaClose { get; set; }
        public Setting<Vector2> SettingsDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> SettingsDropdownScaCloseDuration { get; set; }
        public Setting<Easing> SettingsDropdownScaXOpenEase { get; set; }
        public Setting<Easing> SettingsDropdownScaXCloseEase { get; set; }
        public Setting<Easing> SettingsDropdownScaYOpenEase { get; set; }
        public Setting<Easing> SettingsDropdownScaYCloseEase { get; set; }

        public Setting<bool> SettingsDropdownRotActive { get; set; }
        public Setting<float> SettingsDropdownRotOpen { get; set; }
        public Setting<float> SettingsDropdownRotClose { get; set; }
        public Setting<float> SettingsDropdownRotOpenDuration { get; set; }
        public Setting<float> SettingsDropdownRotCloseDuration { get; set; }
        public Setting<Easing> SettingsDropdownRotOpenEase { get; set; }
        public Setting<Easing> SettingsDropdownRotCloseEase { get; set; }

        #endregion

        #region Steam Dropdown

        public Setting<bool> SteamDropdownActive { get; set; }

        public Setting<bool> SteamDropdownPosActive { get; set; }
        public Setting<Vector2> SteamDropdownPosOpen { get; set; }
        public Setting<Vector2> SteamDropdownPosClose { get; set; }
        public Setting<Vector2> SteamDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> SteamDropdownPosCloseDuration { get; set; }
        public Setting<Easing> SteamDropdownPosXOpenEase { get; set; }
        public Setting<Easing> SteamDropdownPosXCloseEase { get; set; }
        public Setting<Easing> SteamDropdownPosYOpenEase { get; set; }
        public Setting<Easing> SteamDropdownPosYCloseEase { get; set; }

        public Setting<bool> SteamDropdownScaActive { get; set; }
        public Setting<Vector2> SteamDropdownScaOpen { get; set; }
        public Setting<Vector2> SteamDropdownScaClose { get; set; }
        public Setting<Vector2> SteamDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> SteamDropdownScaCloseDuration { get; set; }
        public Setting<Easing> SteamDropdownScaXOpenEase { get; set; }
        public Setting<Easing> SteamDropdownScaXCloseEase { get; set; }
        public Setting<Easing> SteamDropdownScaYOpenEase { get; set; }
        public Setting<Easing> SteamDropdownScaYCloseEase { get; set; }

        public Setting<bool> SteamDropdownRotActive { get; set; }
        public Setting<float> SteamDropdownRotOpen { get; set; }
        public Setting<float> SteamDropdownRotClose { get; set; }
        public Setting<float> SteamDropdownRotOpenDuration { get; set; }
        public Setting<float> SteamDropdownRotCloseDuration { get; set; }
        public Setting<Easing> SteamDropdownRotOpenEase { get; set; }
        public Setting<Easing> SteamDropdownRotCloseEase { get; set; }

        #endregion

        #region Help Dropdown

        public Setting<bool> HelpDropdownActive { get; set; }

        public Setting<bool> HelpDropdownPosActive { get; set; }
        public Setting<Vector2> HelpDropdownPosOpen { get; set; }
        public Setting<Vector2> HelpDropdownPosClose { get; set; }
        public Setting<Vector2> HelpDropdownPosOpenDuration { get; set; }
        public Setting<Vector2> HelpDropdownPosCloseDuration { get; set; }
        public Setting<Easing> HelpDropdownPosXOpenEase { get; set; }
        public Setting<Easing> HelpDropdownPosXCloseEase { get; set; }
        public Setting<Easing> HelpDropdownPosYOpenEase { get; set; }
        public Setting<Easing> HelpDropdownPosYCloseEase { get; set; }

        public Setting<bool> HelpDropdownScaActive { get; set; }
        public Setting<Vector2> HelpDropdownScaOpen { get; set; }
        public Setting<Vector2> HelpDropdownScaClose { get; set; }
        public Setting<Vector2> HelpDropdownScaOpenDuration { get; set; }
        public Setting<Vector2> HelpDropdownScaCloseDuration { get; set; }
        public Setting<Easing> HelpDropdownScaXOpenEase { get; set; }
        public Setting<Easing> HelpDropdownScaXCloseEase { get; set; }
        public Setting<Easing> HelpDropdownScaYOpenEase { get; set; }
        public Setting<Easing> HelpDropdownScaYCloseEase { get; set; }

        public Setting<bool> HelpDropdownRotActive { get; set; }
        public Setting<float> HelpDropdownRotOpen { get; set; }
        public Setting<float> HelpDropdownRotClose { get; set; }
        public Setting<float> HelpDropdownRotOpenDuration { get; set; }
        public Setting<float> HelpDropdownRotCloseDuration { get; set; }
        public Setting<Easing> HelpDropdownRotOpenEase { get; set; }
        public Setting<Easing> HelpDropdownRotCloseEase { get; set; }

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
        public Setting<bool> ObjectDraggerHelper { get; set; }
        public Setting<TransformType> ObjectDraggerHelperType { get; set; }
        public Setting<bool> ObjectDraggerEnabled { get; set; }
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
            CreateObjectsatCameraCenter = Bind(this, GENERAL, "Create Objects at Camera Center", true, "When an object is created, its position will be set to that of the camera's.");
            SpawnPrefabsAtCameraCenter = Bind(this, GENERAL, "Spawn Prefabs at Camera Center", true, "When a Prefab object is placed into a level, its position will be set to that of the camera's.");
            CreateObjectsScaleParentDefault = Bind(this, GENERAL, "Create Objects Scale Parent Default", true, "The default value for new Beatmap Objects' Scale Parent.");
            CreateObjectModifierOrderDefault = Bind(this, GENERAL, "Create Objects Modifier Order Default", true, "The default value for new objects' Order Matters toggle.");
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
            TimelineCollapseLength = Bind(this, TIMELINE, "Timeline Collapse Length", 0.4f, "How small a collapsed timeline object ends up.", 0.05f, 1f);
            TimelineObjectPrefabTypeIcon = Bind(this, TIMELINE, "Timeline Object Prefab Type Icon", true, "Shows the object's prefab type's icon.");
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
            SaveAsync = Bind(this, DATA, "Save Async", true, "If saving levels should run asynchronously. Having this on will not show logs in the BepInEx log console.");
            LevelLoadsLastTime = Bind(this, DATA, "Level Loads Last Time", true, "Sets the editor position (audio time, layer, etc) to the last saved editor position on level load.");
            LevelPausesOnStart = Bind(this, DATA, "Level Pauses on Start", false, "Editor pauses on level load.");
            BackupPreviousLoadedLevel = Bind(this, DATA, "Backup Previous Loaded Level", false, "Saves the previously loaded level when loading a different level to a level-previous.lsb file.");
            SettingPathReloads = Bind(this, DATA, "Setting Path Reloads", true, "With this setting on, update the list for levels, prefabs and themes when changing the directory.");
            EditorRank = Bind(this, DATA, "Editor Rank", Rank.Null, "What rank should be used when displaying / calculating level rank while in the editor.");
            CopyPasteGlobal = Bind(this, DATA, "Copy Paste From Global Folder", false, "If copied objects & event keyframes are saved to a global file for any instance of Project Arrhythmia to load when pasting. Turn off if copy & paste is breaking.");
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
            PasteBackgroundObjectsOverwrites = Bind(this, DATA, "Paste Background Objects Overwrites", true, "If pasting the entire copied set of BG objects overwrites the current list of BG objects.");
            ShowDefaultThemes = Bind(this, DATA, "Show Default Themes", true, "If the default beatmap themes should appear in the theme list.");
            ImageSequenceFPS = Bind(this, DATA, "Image Sequence FPS", 24, "FPS of a generated image sequence. Image sequences can be created by dragging in a collection of images or a folder that only contains images.");
            OverwriteImportedImages = Bind(this, DATA, "Overwrite Imported Images", false, "If imported images to image objects should overwrite the file.");

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
            AdjustPositionInputs = Bind(this, EDITOR_GUI, "Adjust Position Inputs", true, "If position keyframe input fields should be adjusted so they're in a proper row rather than having Z Axis below X Axis without a label. Drawback with doing this is it makes the fields smaller than normal.");
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
            PrefabExternalDeleteButtonPos = Bind(this, EDITOR_GUI, "Prefab External Delete Button Pos", new Vector2(567f, -16f), "Position of the Delete Button.");
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

            BPMSnapsObjects = Bind(this, BPM, "Snap Objects", true, "Makes the start time of objects snap to the BPM.");
            BPMSnapsObjectKeyframes = Bind(this, BPM, "Snap Object Keyframes", false, "Makes the time of object keyframes snap to the BPM.");
            BPMSnapsKeyframes = Bind(this, BPM, "Snap Keyframes", true, "Makes the time of event keyframes snap to the BPM.");
            BPMSnapsCheckpoints = Bind(this, BPM, "Snap Checkpoints", true, "Makes the time of checkpoints snap to the BPM.");
            BPMSnapsMarkers = Bind(this, BPM, "Snap Markers", true, "Makes the time of markers snap to the BPM.");
            BPMSnapsPasted = Bind(this, BPM, "Snaps Pasted", true, "Makes pasted objects snap to the BPM at an offset from the first object, so any objects that spawn after don't get snapped.");
            BPMSnapsPrefabImport = Bind(this, BPM, "Snaps Prefab Import", true, "Makes prefabs imported to the level snap to the BPM.");

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

            PlayEditorAnimations = Bind(this, ANIMATIONS, "Play Editor Animations", false, "If popups should be animated.");

            #region Open File Popup

            OpenFilePopupActive = Bind(this, ANIMATIONS, "Open File Popup Active", true, "If the popup animation should play.");

            OpenFilePopupPosActive = Bind(this, ANIMATIONS, "Open File Popup Animate Position", false, "If position should be animated.");
            OpenFilePopupPosOpen = Bind(this, ANIMATIONS, "Open File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupPosClose = Bind(this, ANIMATIONS, "Open File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupPosOpenDuration = Bind(this, ANIMATIONS, "Open File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            OpenFilePopupPosCloseDuration = Bind(this, ANIMATIONS, "Open File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            OpenFilePopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Open File Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            OpenFilePopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Open File Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            OpenFilePopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Open File Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            OpenFilePopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Open File Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            OpenFilePopupScaActive = Bind(this, ANIMATIONS, "Open File Popup Animate Scale", true, "If scale should be animated.");
            OpenFilePopupScaOpen = Bind(this, ANIMATIONS, "Open File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupScaClose = Bind(this, ANIMATIONS, "Open File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupScaOpenDuration = Bind(this, ANIMATIONS, "Open File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            OpenFilePopupScaCloseDuration = Bind(this, ANIMATIONS, "Open File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            OpenFilePopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Open File Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            OpenFilePopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Open File Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            OpenFilePopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Open File Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            OpenFilePopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Open File Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            OpenFilePopupRotActive = Bind(this, ANIMATIONS, "Open File Popup Animate Rotation", false, "If rotation should be animated.");
            OpenFilePopupRotOpen = Bind(this, ANIMATIONS, "Open File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            OpenFilePopupRotClose = Bind(this, ANIMATIONS, "Open File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            OpenFilePopupRotOpenDuration = Bind(this, ANIMATIONS, "Open File Popup Open Rotation Duration", 0f, "The duration of opening.");
            OpenFilePopupRotCloseDuration = Bind(this, ANIMATIONS, "Open File Popup Close Rotation Duration", 0f, "The duration of closing.");
            OpenFilePopupRotOpenEase = BindEnum(this, ANIMATIONS, "Open File Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            OpenFilePopupRotCloseEase = BindEnum(this, ANIMATIONS, "Open File Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region New File Popup

            NewFilePopupActive = Bind(this, ANIMATIONS, "New File Popup Active", true, "If the popup animation should play.");

            NewFilePopupPosActive = Bind(this, ANIMATIONS, "New File Popup Animate Position", false, "If position should be animated.");
            NewFilePopupPosOpen = Bind(this, ANIMATIONS, "New File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupPosClose = Bind(this, ANIMATIONS, "New File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupPosOpenDuration = Bind(this, ANIMATIONS, "New File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            NewFilePopupPosCloseDuration = Bind(this, ANIMATIONS, "New File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            NewFilePopupPosXOpenEase = BindEnum(this, ANIMATIONS, "New File Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            NewFilePopupPosXCloseEase = BindEnum(this, ANIMATIONS, "New File Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            NewFilePopupPosYOpenEase = BindEnum(this, ANIMATIONS, "New File Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            NewFilePopupPosYCloseEase = BindEnum(this, ANIMATIONS, "New File Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            NewFilePopupScaActive = Bind(this, ANIMATIONS, "New File Popup Animate Scale", true, "If scale should be animated.");
            NewFilePopupScaOpen = Bind(this, ANIMATIONS, "New File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupScaClose = Bind(this, ANIMATIONS, "New File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupScaOpenDuration = Bind(this, ANIMATIONS, "New File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            NewFilePopupScaCloseDuration = Bind(this, ANIMATIONS, "New File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            NewFilePopupScaXOpenEase = BindEnum(this, ANIMATIONS, "New File Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            NewFilePopupScaXCloseEase = BindEnum(this, ANIMATIONS, "New File Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            NewFilePopupScaYOpenEase = BindEnum(this, ANIMATIONS, "New File Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            NewFilePopupScaYCloseEase = BindEnum(this, ANIMATIONS, "New File Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            NewFilePopupRotActive = Bind(this, ANIMATIONS, "New File Popup Animate Rotation", false, "If rotation should be animated.");
            NewFilePopupRotOpen = Bind(this, ANIMATIONS, "New File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            NewFilePopupRotClose = Bind(this, ANIMATIONS, "New File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            NewFilePopupRotOpenDuration = Bind(this, ANIMATIONS, "New File Popup Open Rotation Duration", 0f, "The duration of opening.");
            NewFilePopupRotCloseDuration = Bind(this, ANIMATIONS, "New File Popup Close Rotation Duration", 0f, "The duration of closing.");
            NewFilePopupRotOpenEase = BindEnum(this, ANIMATIONS, "New File Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            NewFilePopupRotCloseEase = BindEnum(this, ANIMATIONS, "New File Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Save As Popup

            SaveAsPopupActive = Bind(this, ANIMATIONS, "Save As Popup Active", true, "If the popup animation should play.");

            SaveAsPopupPosActive = Bind(this, ANIMATIONS, "Save As Popup Animate Position", false, "If position should be animated.");
            SaveAsPopupPosOpen = Bind(this, ANIMATIONS, "Save As Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupPosClose = Bind(this, ANIMATIONS, "Save As Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupPosOpenDuration = Bind(this, ANIMATIONS, "Save As Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            SaveAsPopupPosCloseDuration = Bind(this, ANIMATIONS, "Save As Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            SaveAsPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Save As Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            SaveAsPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Save As Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            SaveAsPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Save As Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            SaveAsPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Save As Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            SaveAsPopupScaActive = Bind(this, ANIMATIONS, "Save As Popup Animate Scale", true, "If scale should be animated.");
            SaveAsPopupScaOpen = Bind(this, ANIMATIONS, "Save As Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupScaClose = Bind(this, ANIMATIONS, "Save As Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupScaOpenDuration = Bind(this, ANIMATIONS, "Save As Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SaveAsPopupScaCloseDuration = Bind(this, ANIMATIONS, "Save As Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SaveAsPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Save As Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            SaveAsPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Save As Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            SaveAsPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Save As Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            SaveAsPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Save As Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            SaveAsPopupRotActive = Bind(this, ANIMATIONS, "Save As Popup Animate Rotation", false, "If rotation should be animated.");
            SaveAsPopupRotOpen = Bind(this, ANIMATIONS, "Save As Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SaveAsPopupRotClose = Bind(this, ANIMATIONS, "Save As Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SaveAsPopupRotOpenDuration = Bind(this, ANIMATIONS, "Save As Popup Open Rotation Duration", 0f, "The duration of opening.");
            SaveAsPopupRotCloseDuration = Bind(this, ANIMATIONS, "Save As Popup Close Rotation Duration", 0f, "The duration of closing.");
            SaveAsPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Save As Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            SaveAsPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Save As Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Quick Actions Popup

            QuickActionsPopupActive = Bind(this, ANIMATIONS, "Quick Actions Popup Active", true, "If the popup animation should play.");

            QuickActionsPopupPosActive = Bind(this, ANIMATIONS, "Quick Actions Popup Animate Position", false, "If position should be animated.");
            QuickActionsPopupPosOpen = Bind(this, ANIMATIONS, "Quick Actions Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupPosClose = Bind(this, ANIMATIONS, "Quick Actions Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupPosOpenDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            QuickActionsPopupPosCloseDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            QuickActionsPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            QuickActionsPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            QuickActionsPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            QuickActionsPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            QuickActionsPopupScaActive = Bind(this, ANIMATIONS, "Quick Actions Popup Animate Scale", true, "If scale should be animated.");
            QuickActionsPopupScaOpen = Bind(this, ANIMATIONS, "Quick Actions Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupScaClose = Bind(this, ANIMATIONS, "Quick Actions Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupScaOpenDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            QuickActionsPopupScaCloseDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            QuickActionsPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            QuickActionsPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            QuickActionsPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            QuickActionsPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            QuickActionsPopupRotActive = Bind(this, ANIMATIONS, "Quick Actions Popup Animate Rotation", false, "If rotation should be animated.");
            QuickActionsPopupRotOpen = Bind(this, ANIMATIONS, "Quick Actions Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            QuickActionsPopupRotClose = Bind(this, ANIMATIONS, "Quick Actions Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            QuickActionsPopupRotOpenDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Open Rotation Duration", 0f, "The duration of opening.");
            QuickActionsPopupRotCloseDuration = Bind(this, ANIMATIONS, "Quick Actions Popup Close Rotation Duration", 0f, "The duration of closing.");
            QuickActionsPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            QuickActionsPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Quick Actions Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Parent Selector

            ParentSelectorPopupActive = Bind(this, ANIMATIONS, "Parent Selector Popup Active", true, "If the popup animation should play.");

            ParentSelectorPopupPosActive = Bind(this, ANIMATIONS, "Parent Selector Popup Animate Position", false, "If position should be animated.");
            ParentSelectorPopupPosOpen = Bind(this, ANIMATIONS, "Parent Selector Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupPosClose = Bind(this, ANIMATIONS, "Parent Selector Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupPosOpenDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ParentSelectorPopupPosCloseDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ParentSelectorPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            ParentSelectorPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            ParentSelectorPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ParentSelectorPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ParentSelectorPopupScaActive = Bind(this, ANIMATIONS, "Parent Selector Popup Animate Scale", true, "If scale should be animated.");
            ParentSelectorPopupScaOpen = Bind(this, ANIMATIONS, "Parent Selector Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupScaClose = Bind(this, ANIMATIONS, "Parent Selector Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupScaOpenDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ParentSelectorPopupScaCloseDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ParentSelectorPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ParentSelectorPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ParentSelectorPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ParentSelectorPopupRotActive = Bind(this, ANIMATIONS, "Parent Selector Popup Animate Rotation", false, "If rotation should be animated.");
            ParentSelectorPopupRotOpen = Bind(this, ANIMATIONS, "Parent Selector Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ParentSelectorPopupRotClose = Bind(this, ANIMATIONS, "Parent Selector Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ParentSelectorPopupRotOpenDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Open Rotation Duration", 0f, "The duration of opening.");
            ParentSelectorPopupRotCloseDuration = Bind(this, ANIMATIONS, "Parent Selector Popup Close Rotation Duration", 0f, "The duration of closing.");
            ParentSelectorPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ParentSelectorPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Parent Selector Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Prefab Popup

            PrefabPopupActive = Bind(this, ANIMATIONS, "Prefab Popup Active", true, "If the popup animation should play.");

            PrefabPopupPosActive = Bind(this, ANIMATIONS, "Prefab Popup Animate Position", false, "If position should be animated.");
            PrefabPopupPosOpen = Bind(this, ANIMATIONS, "Prefab Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupPosClose = Bind(this, ANIMATIONS, "Prefab Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupPosOpenDuration = Bind(this, ANIMATIONS, "Prefab Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabPopupPosCloseDuration = Bind(this, ANIMATIONS, "Prefab Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Prefab Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            PrefabPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Prefab Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            PrefabPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Prefab Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            PrefabPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Prefab Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            PrefabPopupScaActive = Bind(this, ANIMATIONS, "Prefab Popup Animate Scale", true, "If scale should be animated.");
            PrefabPopupScaOpen = Bind(this, ANIMATIONS, "Prefab Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupScaClose = Bind(this, ANIMATIONS, "Prefab Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupScaOpenDuration = Bind(this, ANIMATIONS, "Prefab Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabPopupScaCloseDuration = Bind(this, ANIMATIONS, "Prefab Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Prefab Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            PrefabPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Prefab Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            PrefabPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Prefab Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            PrefabPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Prefab Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            PrefabPopupRotActive = Bind(this, ANIMATIONS, "Prefab Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabPopupRotOpen = Bind(this, ANIMATIONS, "Prefab Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabPopupRotClose = Bind(this, ANIMATIONS, "Prefab Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabPopupRotOpenDuration = Bind(this, ANIMATIONS, "Prefab Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabPopupRotCloseDuration = Bind(this, ANIMATIONS, "Prefab Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Prefab Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            PrefabPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Prefab Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Object Options Popup

            ObjectOptionsPopupActive = Bind(this, ANIMATIONS, "Object Options Popup Active", true, "If the popup animation should play.");

            ObjectOptionsPopupPosActive = Bind(this, ANIMATIONS, "Object Options Popup Animate Position", true, "If position should be animated.");
            ObjectOptionsPopupPosOpen = Bind(this, ANIMATIONS, "Object Options Popup Open Position", new Vector2(-35f, 22f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupPosClose = Bind(this, ANIMATIONS, "Object Options Popup Close Position", new Vector2(-35f, 57f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupPosOpenDuration = Bind(this, ANIMATIONS, "Object Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupPosCloseDuration = Bind(this, ANIMATIONS, "Object Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Object Options Popup Open Position X Ease", Easing.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Object Options Popup Close Position X Ease", Easing.InCirc, "The easing of opening.");
            ObjectOptionsPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Object Options Popup Open Position Y Ease", Easing.OutElastic, "The easing of opening.");
            ObjectOptionsPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Object Options Popup Close Position Y Ease", Easing.InCirc, "The easing of opening.");

            ObjectOptionsPopupScaActive = Bind(this, ANIMATIONS, "Object Options Popup Animate Scale", true, "If scale should be animated.");
            ObjectOptionsPopupScaOpen = Bind(this, ANIMATIONS, "Object Options Popup Open Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupScaClose = Bind(this, ANIMATIONS, "Object Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupScaOpenDuration = Bind(this, ANIMATIONS, "Object Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectOptionsPopupScaCloseDuration = Bind(this, ANIMATIONS, "Object Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectOptionsPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Object Options Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Object Options Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ObjectOptionsPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Object Options Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ObjectOptionsPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Object Options Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ObjectOptionsPopupRotActive = Bind(this, ANIMATIONS, "Object Options Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectOptionsPopupRotOpen = Bind(this, ANIMATIONS, "Object Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectOptionsPopupRotClose = Bind(this, ANIMATIONS, "Object Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectOptionsPopupRotOpenDuration = Bind(this, ANIMATIONS, "Object Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectOptionsPopupRotCloseDuration = Bind(this, ANIMATIONS, "Object Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectOptionsPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Object Options Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ObjectOptionsPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Object Options Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region BG Options Popup

            BGOptionsPopupActive = Bind(this, ANIMATIONS, "BG Options Popup Active", true, "If the popup animation should play.");

            BGOptionsPopupPosActive = Bind(this, ANIMATIONS, "BG Options Popup Animate Position", true, "If position should be animated.");
            BGOptionsPopupPosOpen = Bind(this, ANIMATIONS, "BG Options Popup Open Position", new Vector2(0f, 57f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupPosClose = Bind(this, ANIMATIONS, "BG Options Popup Close Position", new Vector2(0f, 22f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupPosOpenDuration = Bind(this, ANIMATIONS, "BG Options Popup Open Position Duration", new Vector2(0f, 0.6f), "The duration of opening.");
            BGOptionsPopupPosCloseDuration = Bind(this, ANIMATIONS, "BG Options Popup Close Position Duration", new Vector2(0f, 0.1f), "The duration of closing.");
            BGOptionsPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "BG Options Popup Open Position X Ease", Easing.OutElastic, "The easing of opening.");
            BGOptionsPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "BG Options Popup Close Position X Ease", Easing.InCirc, "The easing of opening.");
            BGOptionsPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "BG Options Popup Open Position Y Ease", Easing.OutElastic, "The easing of opening.");
            BGOptionsPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "BG Options Popup Close Position Y Ease", Easing.InCirc, "The easing of opening.");

            BGOptionsPopupScaActive = Bind(this, ANIMATIONS, "BG Options Popup Animate Scale", true, "If scale should be animated.");
            BGOptionsPopupScaOpen = Bind(this, ANIMATIONS, "BG Options Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupScaClose = Bind(this, ANIMATIONS, "BG Options Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupScaOpenDuration = Bind(this, ANIMATIONS, "BG Options Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BGOptionsPopupScaCloseDuration = Bind(this, ANIMATIONS, "BG Options Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BGOptionsPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "BG Options Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            BGOptionsPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "BG Options Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            BGOptionsPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "BG Options Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            BGOptionsPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "BG Options Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            BGOptionsPopupRotActive = Bind(this, ANIMATIONS, "BG Options Popup Animate Rotation", false, "If rotation should be animated.");
            BGOptionsPopupRotOpen = Bind(this, ANIMATIONS, "BG Options Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BGOptionsPopupRotClose = Bind(this, ANIMATIONS, "BG Options Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BGOptionsPopupRotOpenDuration = Bind(this, ANIMATIONS, "BG Options Popup Open Rotation Duration", 0f, "The duration of opening.");
            BGOptionsPopupRotCloseDuration = Bind(this, ANIMATIONS, "BG Options Popup Close Rotation Duration", 0f, "The duration of closing.");
            BGOptionsPopupRotOpenEase = BindEnum(this, ANIMATIONS, "BG Options Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            BGOptionsPopupRotCloseEase = BindEnum(this, ANIMATIONS, "BG Options Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Browser Popup

            BrowserPopupActive = Bind(this, ANIMATIONS, "Browser Popup Active", true, "If the popup animation should play.");

            BrowserPopupPosActive = Bind(this, ANIMATIONS, "Browser Popup Animate Position", false, "If position should be animated.");
            BrowserPopupPosOpen = Bind(this, ANIMATIONS, "Browser Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupPosClose = Bind(this, ANIMATIONS, "Browser Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupPosOpenDuration = Bind(this, ANIMATIONS, "Browser Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            BrowserPopupPosCloseDuration = Bind(this, ANIMATIONS, "Browser Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            BrowserPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Browser Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            BrowserPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Browser Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            BrowserPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Browser Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            BrowserPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Browser Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            BrowserPopupScaActive = Bind(this, ANIMATIONS, "Browser Popup Animate Scale", true, "If scale should be animated.");
            BrowserPopupScaOpen = Bind(this, ANIMATIONS, "Browser Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupScaClose = Bind(this, ANIMATIONS, "Browser Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupScaOpenDuration = Bind(this, ANIMATIONS, "Browser Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            BrowserPopupScaCloseDuration = Bind(this, ANIMATIONS, "Browser Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            BrowserPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Browser Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            BrowserPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Browser Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            BrowserPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Browser Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            BrowserPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Browser Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            BrowserPopupRotActive = Bind(this, ANIMATIONS, "Browser Popup Animate Rotation", false, "If rotation should be animated.");
            BrowserPopupRotOpen = Bind(this, ANIMATIONS, "Browser Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            BrowserPopupRotClose = Bind(this, ANIMATIONS, "Browser Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            BrowserPopupRotOpenDuration = Bind(this, ANIMATIONS, "Browser Popup Open Rotation Duration", 0f, "The duration of opening.");
            BrowserPopupRotCloseDuration = Bind(this, ANIMATIONS, "Browser Popup Close Rotation Duration", 0f, "The duration of closing.");
            BrowserPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Browser Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            BrowserPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Browser Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Object Search Popup

            ObjectSearchPopupActive = Bind(this, ANIMATIONS, "Object Search Popup Active", true, "If the popup animation should play.");

            ObjectSearchPopupPosActive = Bind(this, ANIMATIONS, "Object Search Popup Animate Position", false, "If position should be animated.");
            ObjectSearchPopupPosOpen = Bind(this, ANIMATIONS, "Object Search Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupPosClose = Bind(this, ANIMATIONS, "Object Search Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupPosOpenDuration = Bind(this, ANIMATIONS, "Object Search Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ObjectSearchPopupPosCloseDuration = Bind(this, ANIMATIONS, "Object Search Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ObjectSearchPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Object Search Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            ObjectSearchPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Object Search Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            ObjectSearchPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Object Search Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ObjectSearchPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Object Search Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ObjectSearchPopupScaActive = Bind(this, ANIMATIONS, "Object Search Popup Animate Scale", true, "If scale should be animated.");
            ObjectSearchPopupScaOpen = Bind(this, ANIMATIONS, "Object Search Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupScaClose = Bind(this, ANIMATIONS, "Object Search Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupScaOpenDuration = Bind(this, ANIMATIONS, "Object Search Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectSearchPopupScaCloseDuration = Bind(this, ANIMATIONS, "Object Search Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectSearchPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Object Search Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Object Search Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ObjectSearchPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Object Search Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ObjectSearchPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Object Search Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ObjectSearchPopupRotActive = Bind(this, ANIMATIONS, "Object Search Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectSearchPopupRotOpen = Bind(this, ANIMATIONS, "Object Search Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectSearchPopupRotClose = Bind(this, ANIMATIONS, "Object Search Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectSearchPopupRotOpenDuration = Bind(this, ANIMATIONS, "Object Search Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectSearchPopupRotCloseDuration = Bind(this, ANIMATIONS, "Object Search Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectSearchPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Object Search Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ObjectSearchPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Object Search Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Object Template Popup

            ObjectTemplatePopupActive = Bind(this, ANIMATIONS, "Object Template Popup Active", true, "If the popup animation should play.");

            ObjectTemplatePopupPosActive = Bind(this, ANIMATIONS, "Object Template Popup Animate Position", false, "If position should be animated.");
            ObjectTemplatePopupPosOpen = Bind(this, ANIMATIONS, "Object Template Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupPosClose = Bind(this, ANIMATIONS, "Object Template Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupPosOpenDuration = Bind(this, ANIMATIONS, "Object Template Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ObjectTemplatePopupPosCloseDuration = Bind(this, ANIMATIONS, "Object Template Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ObjectTemplatePopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Object Template Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            ObjectTemplatePopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Object Template Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            ObjectTemplatePopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Object Template Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ObjectTemplatePopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Object Template Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ObjectTemplatePopupScaActive = Bind(this, ANIMATIONS, "Object Template Popup Animate Scale", true, "If scale should be animated.");
            ObjectTemplatePopupScaOpen = Bind(this, ANIMATIONS, "Object Template Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupScaClose = Bind(this, ANIMATIONS, "Object Template Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupScaOpenDuration = Bind(this, ANIMATIONS, "Object Template Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ObjectTemplatePopupScaCloseDuration = Bind(this, ANIMATIONS, "Object Template Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ObjectTemplatePopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Object Template Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ObjectTemplatePopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Object Template Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ObjectTemplatePopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Object Template Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ObjectTemplatePopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Object Template Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ObjectTemplatePopupRotActive = Bind(this, ANIMATIONS, "Object Template Popup Animate Rotation", false, "If rotation should be animated.");
            ObjectTemplatePopupRotOpen = Bind(this, ANIMATIONS, "Object Template Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ObjectTemplatePopupRotClose = Bind(this, ANIMATIONS, "Object Template Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ObjectTemplatePopupRotOpenDuration = Bind(this, ANIMATIONS, "Object Template Popup Open Rotation Duration", 0f, "The duration of opening.");
            ObjectTemplatePopupRotCloseDuration = Bind(this, ANIMATIONS, "Object Template Popup Close Rotation Duration", 0f, "The duration of closing.");
            ObjectTemplatePopupRotOpenEase = BindEnum(this, ANIMATIONS, "Object Template Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ObjectTemplatePopupRotCloseEase = BindEnum(this, ANIMATIONS, "Object Template Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Warning Popup

            WarningPopupActive = Bind(this, ANIMATIONS, "Warning Popup Active", true, "If the popup animation should play.");

            WarningPopupPosActive = Bind(this, ANIMATIONS, "Warning Popup Animate Position", false, "If position should be animated.");
            WarningPopupPosOpen = Bind(this, ANIMATIONS, "Warning Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupPosClose = Bind(this, ANIMATIONS, "Warning Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupPosOpenDuration = Bind(this, ANIMATIONS, "Warning Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            WarningPopupPosCloseDuration = Bind(this, ANIMATIONS, "Warning Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            WarningPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Warning Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            WarningPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Warning Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            WarningPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Warning Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            WarningPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Warning Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            WarningPopupScaActive = Bind(this, ANIMATIONS, "Warning Popup Animate Scale", true, "If scale should be animated.");
            WarningPopupScaOpen = Bind(this, ANIMATIONS, "Warning Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupScaClose = Bind(this, ANIMATIONS, "Warning Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupScaOpenDuration = Bind(this, ANIMATIONS, "Warning Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            WarningPopupScaCloseDuration = Bind(this, ANIMATIONS, "Warning Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            WarningPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Warning Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            WarningPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Warning Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            WarningPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Warning Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            WarningPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Warning Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            WarningPopupRotActive = Bind(this, ANIMATIONS, "Warning Popup Animate Rotation", false, "If rotation should be animated.");
            WarningPopupRotOpen = Bind(this, ANIMATIONS, "Warning Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            WarningPopupRotClose = Bind(this, ANIMATIONS, "Warning Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            WarningPopupRotOpenDuration = Bind(this, ANIMATIONS, "Warning Popup Open Rotation Duration", 0f, "The duration of opening.");
            WarningPopupRotCloseDuration = Bind(this, ANIMATIONS, "Warning Popup Close Rotation Duration", 0f, "The duration of closing.");
            WarningPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Warning Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            WarningPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Warning Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Warning Popup

            FilePopupActive = Bind(this, ANIMATIONS, "File Popup Active", true, "If the popup animation should play.");

            FilePopupPosActive = Bind(this, ANIMATIONS, "File Popup Animate Position", false, "If position should be animated.");
            FilePopupPosOpen = Bind(this, ANIMATIONS, "File Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupPosClose = Bind(this, ANIMATIONS, "File Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupPosOpenDuration = Bind(this, ANIMATIONS, "File Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            FilePopupPosCloseDuration = Bind(this, ANIMATIONS, "File Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            FilePopupPosXOpenEase = BindEnum(this, ANIMATIONS, "File Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            FilePopupPosXCloseEase = BindEnum(this, ANIMATIONS, "File Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            FilePopupPosYOpenEase = BindEnum(this, ANIMATIONS, "File Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            FilePopupPosYCloseEase = BindEnum(this, ANIMATIONS, "File Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            FilePopupScaActive = Bind(this, ANIMATIONS, "File Popup Animate Scale", true, "If scale should be animated.");
            FilePopupScaOpen = Bind(this, ANIMATIONS, "File Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupScaClose = Bind(this, ANIMATIONS, "File Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupScaOpenDuration = Bind(this, ANIMATIONS, "File Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FilePopupScaCloseDuration = Bind(this, ANIMATIONS, "File Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FilePopupScaXOpenEase = BindEnum(this, ANIMATIONS, "File Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            FilePopupScaXCloseEase = BindEnum(this, ANIMATIONS, "File Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            FilePopupScaYOpenEase = BindEnum(this, ANIMATIONS, "File Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            FilePopupScaYCloseEase = BindEnum(this, ANIMATIONS, "File Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            FilePopupRotActive = Bind(this, ANIMATIONS, "File Popup Animate Rotation", false, "If rotation should be animated.");
            FilePopupRotOpen = Bind(this, ANIMATIONS, "File Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FilePopupRotClose = Bind(this, ANIMATIONS, "File Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FilePopupRotOpenDuration = Bind(this, ANIMATIONS, "File Popup Open Rotation Duration", 0f, "The duration of opening.");
            FilePopupRotCloseDuration = Bind(this, ANIMATIONS, "File Popup Close Rotation Duration", 0f, "The duration of closing.");
            FilePopupRotOpenEase = BindEnum(this, ANIMATIONS, "File Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            FilePopupRotCloseEase = BindEnum(this, ANIMATIONS, "File Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Text Editor

            TextEditorActive = Bind(this, ANIMATIONS, "Text Editor Active", true, "If the popup animation should play.");

            TextEditorPosActive = Bind(this, ANIMATIONS, "Text Editor Animate Position", false, "If position should be animated.");
            TextEditorPosOpen = Bind(this, ANIMATIONS, "Text Editor Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorPosClose = Bind(this, ANIMATIONS, "Text Editor Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorPosOpenDuration = Bind(this, ANIMATIONS, "Text Editor Open Position Duration", Vector2.zero, "The duration of opening.");
            TextEditorPosCloseDuration = Bind(this, ANIMATIONS, "Text Editor Close Position Duration", Vector2.zero, "The duration of closing.");
            TextEditorPosXOpenEase = BindEnum(this, ANIMATIONS, "Text Editor Open Position X Ease", Easing.Linear, "The easing of opening.");
            TextEditorPosXCloseEase = BindEnum(this, ANIMATIONS, "Text Editor Close Position X Ease", Easing.Linear, "The easing of opening.");
            TextEditorPosYOpenEase = BindEnum(this, ANIMATIONS, "Text Editor Open Position Y Ease", Easing.Linear, "The easing of opening.");
            TextEditorPosYCloseEase = BindEnum(this, ANIMATIONS, "Text Editor Close Position Y Ease", Easing.Linear, "The easing of opening.");

            TextEditorScaActive = Bind(this, ANIMATIONS, "Text Editor Animate Scale", true, "If scale should be animated.");
            TextEditorScaOpen = Bind(this, ANIMATIONS, "Text Editor Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorScaClose = Bind(this, ANIMATIONS, "Text Editor Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorScaOpenDuration = Bind(this, ANIMATIONS, "Text Editor Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            TextEditorScaCloseDuration = Bind(this, ANIMATIONS, "Text Editor Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            TextEditorScaXOpenEase = BindEnum(this, ANIMATIONS, "Text Editor Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            TextEditorScaXCloseEase = BindEnum(this, ANIMATIONS, "Text Editor Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            TextEditorScaYOpenEase = BindEnum(this, ANIMATIONS, "Text Editor Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            TextEditorScaYCloseEase = BindEnum(this, ANIMATIONS, "Text Editor Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            TextEditorRotActive = Bind(this, ANIMATIONS, "Text Editor Animate Rotation", false, "If rotation should be animated.");
            TextEditorRotOpen = Bind(this, ANIMATIONS, "Text Editor Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            TextEditorRotClose = Bind(this, ANIMATIONS, "Text Editor Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            TextEditorRotOpenDuration = Bind(this, ANIMATIONS, "Text Editor Open Rotation Duration", 0f, "The duration of opening.");
            TextEditorRotCloseDuration = Bind(this, ANIMATIONS, "Text Editor Close Rotation Duration", 0f, "The duration of closing.");
            TextEditorRotOpenEase = BindEnum(this, ANIMATIONS, "Text Editor Open Rotation Ease", Easing.Linear, "The easing of opening.");
            TextEditorRotCloseEase = BindEnum(this, ANIMATIONS, "Text Editor Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Documentation Popup

            DocumentationPopupActive = Bind(this, ANIMATIONS, "Documentation Popup Active", true, "If the popup animation should play.");

            DocumentationPopupPosActive = Bind(this, ANIMATIONS, "Documentation Popup Animate Position", false, "If position should be animated.");
            DocumentationPopupPosOpen = Bind(this, ANIMATIONS, "Documentation Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupPosClose = Bind(this, ANIMATIONS, "Documentation Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupPosOpenDuration = Bind(this, ANIMATIONS, "Documentation Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DocumentationPopupPosCloseDuration = Bind(this, ANIMATIONS, "Documentation Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DocumentationPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Documentation Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            DocumentationPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Documentation Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            DocumentationPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Documentation Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            DocumentationPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Documentation Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            DocumentationPopupScaActive = Bind(this, ANIMATIONS, "Documentation Popup Animate Scale", true, "If scale should be animated.");
            DocumentationPopupScaOpen = Bind(this, ANIMATIONS, "Documentation Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupScaClose = Bind(this, ANIMATIONS, "Documentation Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupScaOpenDuration = Bind(this, ANIMATIONS, "Documentation Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DocumentationPopupScaCloseDuration = Bind(this, ANIMATIONS, "Documentation Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DocumentationPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Documentation Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            DocumentationPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Documentation Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            DocumentationPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Documentation Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            DocumentationPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Documentation Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            DocumentationPopupRotActive = Bind(this, ANIMATIONS, "Documentation Popup Animate Rotation", false, "If rotation should be animated.");
            DocumentationPopupRotOpen = Bind(this, ANIMATIONS, "Documentation Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DocumentationPopupRotClose = Bind(this, ANIMATIONS, "Documentation Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DocumentationPopupRotOpenDuration = Bind(this, ANIMATIONS, "Documentation Popup Open Rotation Duration", 0f, "The duration of opening.");
            DocumentationPopupRotCloseDuration = Bind(this, ANIMATIONS, "Documentation Popup Close Rotation Duration", 0f, "The duration of closing.");
            DocumentationPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Documentation Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            DocumentationPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Documentation Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Debugger Popup

            DebuggerPopupActive = Bind(this, ANIMATIONS, "Debugger Popup Active", true, "If the popup animation should play.");

            DebuggerPopupPosActive = Bind(this, ANIMATIONS, "Debugger Popup Animate Position", false, "If position should be animated.");
            DebuggerPopupPosOpen = Bind(this, ANIMATIONS, "Debugger Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupPosClose = Bind(this, ANIMATIONS, "Debugger Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupPosOpenDuration = Bind(this, ANIMATIONS, "Debugger Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DebuggerPopupPosCloseDuration = Bind(this, ANIMATIONS, "Debugger Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DebuggerPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Debugger Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            DebuggerPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Debugger Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            DebuggerPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Debugger Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            DebuggerPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Debugger Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            DebuggerPopupScaActive = Bind(this, ANIMATIONS, "Debugger Popup Animate Scale", true, "If scale should be animated.");
            DebuggerPopupScaOpen = Bind(this, ANIMATIONS, "Debugger Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupScaClose = Bind(this, ANIMATIONS, "Debugger Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupScaOpenDuration = Bind(this, ANIMATIONS, "Debugger Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DebuggerPopupScaCloseDuration = Bind(this, ANIMATIONS, "Debugger Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DebuggerPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Debugger Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            DebuggerPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Debugger Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            DebuggerPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Debugger Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            DebuggerPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Debugger Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            DebuggerPopupRotActive = Bind(this, ANIMATIONS, "Debugger Popup Animate Rotation", false, "If rotation should be animated.");
            DebuggerPopupRotOpen = Bind(this, ANIMATIONS, "Debugger Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DebuggerPopupRotClose = Bind(this, ANIMATIONS, "Debugger Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DebuggerPopupRotOpenDuration = Bind(this, ANIMATIONS, "Debugger Popup Open Rotation Duration", 0f, "The duration of opening.");
            DebuggerPopupRotCloseDuration = Bind(this, ANIMATIONS, "Debugger Popup Close Rotation Duration", 0f, "The duration of closing.");
            DebuggerPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Debugger Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            DebuggerPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Debugger Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Autosaves Popup

            AutosavesPopupActive = Bind(this, ANIMATIONS, "Autosaves Popup Active", true, "If the popup animation should play.");

            AutosavesPopupPosActive = Bind(this, ANIMATIONS, "Autosaves Popup Animate Position", false, "If position should be animated.");
            AutosavesPopupPosOpen = Bind(this, ANIMATIONS, "Autosaves Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupPosClose = Bind(this, ANIMATIONS, "Autosaves Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupPosOpenDuration = Bind(this, ANIMATIONS, "Autosaves Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            AutosavesPopupPosCloseDuration = Bind(this, ANIMATIONS, "Autosaves Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            AutosavesPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            AutosavesPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            AutosavesPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            AutosavesPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            AutosavesPopupScaActive = Bind(this, ANIMATIONS, "Autosaves Popup Animate Scale", true, "If scale should be animated.");
            AutosavesPopupScaOpen = Bind(this, ANIMATIONS, "Autosaves Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupScaClose = Bind(this, ANIMATIONS, "Autosaves Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupScaOpenDuration = Bind(this, ANIMATIONS, "Autosaves Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            AutosavesPopupScaCloseDuration = Bind(this, ANIMATIONS, "Autosaves Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            AutosavesPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            AutosavesPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            AutosavesPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            AutosavesPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            AutosavesPopupRotActive = Bind(this, ANIMATIONS, "Autosaves Popup Animate Rotation", false, "If rotation should be animated.");
            AutosavesPopupRotOpen = Bind(this, ANIMATIONS, "Autosaves Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            AutosavesPopupRotClose = Bind(this, ANIMATIONS, "Autosaves Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            AutosavesPopupRotOpenDuration = Bind(this, ANIMATIONS, "Autosaves Popup Open Rotation Duration", 0f, "The duration of opening.");
            AutosavesPopupRotCloseDuration = Bind(this, ANIMATIONS, "Autosaves Popup Close Rotation Duration", 0f, "The duration of closing.");
            AutosavesPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            AutosavesPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Autosaves Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Default Modifiers Popup

            DefaultModifiersPopupActive = Bind(this, ANIMATIONS, "Default Modifiers Popup Active", true, "If the popup animation should play.");

            DefaultModifiersPopupPosActive = Bind(this, ANIMATIONS, "Default Modifiers Popup Animate Position", false, "If position should be animated.");
            DefaultModifiersPopupPosOpen = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupPosClose = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupPosOpenDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            DefaultModifiersPopupPosCloseDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            DefaultModifiersPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            DefaultModifiersPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            DefaultModifiersPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            DefaultModifiersPopupScaActive = Bind(this, ANIMATIONS, "Default Modifiers Popup Animate Scale", true, "If scale should be animated.");
            DefaultModifiersPopupScaOpen = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupScaClose = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupScaOpenDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            DefaultModifiersPopupScaCloseDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            DefaultModifiersPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            DefaultModifiersPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            DefaultModifiersPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            DefaultModifiersPopupRotActive = Bind(this, ANIMATIONS, "Default Modifiers Popup Animate Rotation", false, "If rotation should be animated.");
            DefaultModifiersPopupRotOpen = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            DefaultModifiersPopupRotClose = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            DefaultModifiersPopupRotOpenDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Open Rotation Duration", 0f, "The duration of opening.");
            DefaultModifiersPopupRotCloseDuration = Bind(this, ANIMATIONS, "Default Modifiers Popup Close Rotation Duration", 0f, "The duration of closing.");
            DefaultModifiersPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            DefaultModifiersPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Default Modifiers Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Keybind List Popup

            KeybindListPopupActive = Bind(this, ANIMATIONS, "Keybind List Popup Active", true, "If the popup animation should play.");

            KeybindListPopupPosActive = Bind(this, ANIMATIONS, "Keybind List Popup Animate Position", false, "If position should be animated.");
            KeybindListPopupPosOpen = Bind(this, ANIMATIONS, "Keybind List Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupPosClose = Bind(this, ANIMATIONS, "Keybind List Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupPosOpenDuration = Bind(this, ANIMATIONS, "Keybind List Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            KeybindListPopupPosCloseDuration = Bind(this, ANIMATIONS, "Keybind List Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            KeybindListPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            KeybindListPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            KeybindListPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            KeybindListPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            KeybindListPopupScaActive = Bind(this, ANIMATIONS, "Keybind List Popup Animate Scale", true, "If scale should be animated.");
            KeybindListPopupScaOpen = Bind(this, ANIMATIONS, "Keybind List Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupScaClose = Bind(this, ANIMATIONS, "Keybind List Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupScaOpenDuration = Bind(this, ANIMATIONS, "Keybind List Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            KeybindListPopupScaCloseDuration = Bind(this, ANIMATIONS, "Keybind List Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            KeybindListPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            KeybindListPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            KeybindListPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            KeybindListPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            KeybindListPopupRotActive = Bind(this, ANIMATIONS, "Keybind List Popup Animate Rotation", false, "If rotation should be animated.");
            KeybindListPopupRotOpen = Bind(this, ANIMATIONS, "Keybind List Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            KeybindListPopupRotClose = Bind(this, ANIMATIONS, "Keybind List Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            KeybindListPopupRotOpenDuration = Bind(this, ANIMATIONS, "Keybind List Popup Open Rotation Duration", 0f, "The duration of opening.");
            KeybindListPopupRotCloseDuration = Bind(this, ANIMATIONS, "Keybind List Popup Close Rotation Duration", 0f, "The duration of closing.");
            KeybindListPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            KeybindListPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Keybind List Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Theme Popup

            ThemePopupActive = Bind(this, ANIMATIONS, "Theme Popup Active", true, "If the popup animation should play.");

            ThemePopupPosActive = Bind(this, ANIMATIONS, "Theme Popup Animate Position", false, "If position should be animated.");
            ThemePopupPosOpen = Bind(this, ANIMATIONS, "Theme Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupPosClose = Bind(this, ANIMATIONS, "Theme Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupPosOpenDuration = Bind(this, ANIMATIONS, "Theme Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ThemePopupPosCloseDuration = Bind(this, ANIMATIONS, "Theme Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ThemePopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Theme Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            ThemePopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Theme Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            ThemePopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Theme Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ThemePopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Theme Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ThemePopupScaActive = Bind(this, ANIMATIONS, "Theme Popup Animate Scale", true, "If scale should be animated.");
            ThemePopupScaOpen = Bind(this, ANIMATIONS, "Theme Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupScaClose = Bind(this, ANIMATIONS, "Theme Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupScaOpenDuration = Bind(this, ANIMATIONS, "Theme Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ThemePopupScaCloseDuration = Bind(this, ANIMATIONS, "Theme Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ThemePopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Theme Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ThemePopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Theme Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ThemePopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Theme Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ThemePopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Theme Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ThemePopupRotActive = Bind(this, ANIMATIONS, "Theme Popup Animate Rotation", false, "If rotation should be animated.");
            ThemePopupRotOpen = Bind(this, ANIMATIONS, "Theme Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ThemePopupRotClose = Bind(this, ANIMATIONS, "Theme Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ThemePopupRotOpenDuration = Bind(this, ANIMATIONS, "Theme Popup Open Rotation Duration", 0f, "The duration of opening.");
            ThemePopupRotCloseDuration = Bind(this, ANIMATIONS, "Theme Popup Close Rotation Duration", 0f, "The duration of closing.");
            ThemePopupRotOpenEase = BindEnum(this, ANIMATIONS, "Theme Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ThemePopupRotCloseEase = BindEnum(this, ANIMATIONS, "Theme Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Prefab Types Popup

            PrefabTypesPopupActive = Bind(this, ANIMATIONS, "Prefab Types Popup Active", true, "If the popup animation should play.");

            PrefabTypesPopupPosActive = Bind(this, ANIMATIONS, "Prefab Types Popup Animate Position", false, "If position should be animated.");
            PrefabTypesPopupPosOpen = Bind(this, ANIMATIONS, "Prefab Types Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupPosClose = Bind(this, ANIMATIONS, "Prefab Types Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupPosOpenDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PrefabTypesPopupPosCloseDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PrefabTypesPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            PrefabTypesPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            PrefabTypesPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            PrefabTypesPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            PrefabTypesPopupScaActive = Bind(this, ANIMATIONS, "Prefab Types Popup Animate Scale", true, "If scale should be animated.");
            PrefabTypesPopupScaOpen = Bind(this, ANIMATIONS, "Prefab Types Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupScaClose = Bind(this, ANIMATIONS, "Prefab Types Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupScaOpenDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PrefabTypesPopupScaCloseDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PrefabTypesPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            PrefabTypesPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            PrefabTypesPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            PrefabTypesPopupRotActive = Bind(this, ANIMATIONS, "Prefab Types Popup Animate Rotation", false, "If rotation should be animated.");
            PrefabTypesPopupRotOpen = Bind(this, ANIMATIONS, "Prefab Types Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PrefabTypesPopupRotClose = Bind(this, ANIMATIONS, "Prefab Types Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PrefabTypesPopupRotOpenDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Open Rotation Duration", 0f, "The duration of opening.");
            PrefabTypesPopupRotCloseDuration = Bind(this, ANIMATIONS, "Prefab Types Popup Close Rotation Duration", 0f, "The duration of closing.");
            PrefabTypesPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            PrefabTypesPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Prefab Types Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion
            
            #region Font Selector Popup

            FontSelectorPopupActive = Bind(this, ANIMATIONS, "Font Selector Popup Active", true, "If the popup animation should play.");

            FontSelectorPopupPosActive = Bind(this, ANIMATIONS, "Font Selector Popup Animate Position", false, "If position should be animated.");
            FontSelectorPopupPosOpen = Bind(this, ANIMATIONS, "Font Selector Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupPosClose = Bind(this, ANIMATIONS, "Font Selector Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupPosOpenDuration = Bind(this, ANIMATIONS, "Font Selector Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            FontSelectorPopupPosCloseDuration = Bind(this, ANIMATIONS, "Font Selector Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            FontSelectorPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            FontSelectorPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            FontSelectorPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            FontSelectorPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            FontSelectorPopupScaActive = Bind(this, ANIMATIONS, "Font Selector Popup Animate Scale", true, "If scale should be animated.");
            FontSelectorPopupScaOpen = Bind(this, ANIMATIONS, "Font Selector Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupScaClose = Bind(this, ANIMATIONS, "Font Selector Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupScaOpenDuration = Bind(this, ANIMATIONS, "Font Selector Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FontSelectorPopupScaCloseDuration = Bind(this, ANIMATIONS, "Font Selector Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FontSelectorPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            FontSelectorPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            FontSelectorPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            FontSelectorPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            FontSelectorPopupRotActive = Bind(this, ANIMATIONS, "Font Selector Popup Animate Rotation", false, "If rotation should be animated.");
            FontSelectorPopupRotOpen = Bind(this, ANIMATIONS, "Font Selector Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FontSelectorPopupRotClose = Bind(this, ANIMATIONS, "Font Selector Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FontSelectorPopupRotOpenDuration = Bind(this, ANIMATIONS, "Font Selector Popup Open Rotation Duration", 0f, "The duration of opening.");
            FontSelectorPopupRotCloseDuration = Bind(this, ANIMATIONS, "Font Selector Popup Close Rotation Duration", 0f, "The duration of closing.");
            FontSelectorPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            FontSelectorPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Font Selector Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Pinned Editor Layer Popup

            PinnedEditorLayerPopupActive = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Active", true, "If the popup animation should play.");

            PinnedEditorLayerPopupPosActive = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Animate Position", false, "If position should be animated.");
            PinnedEditorLayerPopupPosOpen = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PinnedEditorLayerPopupPosClose = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PinnedEditorLayerPopupPosOpenDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            PinnedEditorLayerPopupPosCloseDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            PinnedEditorLayerPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            PinnedEditorLayerPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            PinnedEditorLayerPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            PinnedEditorLayerPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            PinnedEditorLayerPopupScaActive = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Animate Scale", true, "If scale should be animated.");
            PinnedEditorLayerPopupScaOpen = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PinnedEditorLayerPopupScaClose = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PinnedEditorLayerPopupScaOpenDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            PinnedEditorLayerPopupScaCloseDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            PinnedEditorLayerPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            PinnedEditorLayerPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            PinnedEditorLayerPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            PinnedEditorLayerPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            PinnedEditorLayerPopupRotActive = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Animate Rotation", false, "If rotation should be animated.");
            PinnedEditorLayerPopupRotOpen = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            PinnedEditorLayerPopupRotClose = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            PinnedEditorLayerPopupRotOpenDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Open Rotation Duration", 0f, "The duration of opening.");
            PinnedEditorLayerPopupRotCloseDuration = Bind(this, ANIMATIONS, "Pinned Editor Layer Popup Close Rotation Duration", 0f, "The duration of closing.");
            PinnedEditorLayerPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            PinnedEditorLayerPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Pinned Editor Layer Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Color Picker Popup

            ColorPickerPopupActive = Bind(this, ANIMATIONS, "Color Picker Popup Active", true, "If the popup animation should play.");

            ColorPickerPopupPosActive = Bind(this, ANIMATIONS, "Color Picker Popup Animate Position", false, "If position should be animated.");
            ColorPickerPopupPosOpen = Bind(this, ANIMATIONS, "Color Picker Popup Open Position", Vector2.zero, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ColorPickerPopupPosClose = Bind(this, ANIMATIONS, "Color Picker Popup Close Position", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ColorPickerPopupPosOpenDuration = Bind(this, ANIMATIONS, "Color Picker Popup Open Position Duration", Vector2.zero, "The duration of opening.");
            ColorPickerPopupPosCloseDuration = Bind(this, ANIMATIONS, "Color Picker Popup Close Position Duration", Vector2.zero, "The duration of closing.");
            ColorPickerPopupPosXOpenEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Open Position X Ease", Easing.Linear, "The easing of opening.");
            ColorPickerPopupPosXCloseEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Close Position X Ease", Easing.Linear, "The easing of opening.");
            ColorPickerPopupPosYOpenEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ColorPickerPopupPosYCloseEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ColorPickerPopupScaActive = Bind(this, ANIMATIONS, "Color Picker Popup Animate Scale", true, "If scale should be animated.");
            ColorPickerPopupScaOpen = Bind(this, ANIMATIONS, "Color Picker Popup Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ColorPickerPopupScaClose = Bind(this, ANIMATIONS, "Color Picker Popup Close Scale", Vector2.zero, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ColorPickerPopupScaOpenDuration = Bind(this, ANIMATIONS, "Color Picker Popup Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ColorPickerPopupScaCloseDuration = Bind(this, ANIMATIONS, "Color Picker Popup Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ColorPickerPopupScaXOpenEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ColorPickerPopupScaXCloseEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ColorPickerPopupScaYOpenEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ColorPickerPopupScaYCloseEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ColorPickerPopupRotActive = Bind(this, ANIMATIONS, "Color Picker Popup Animate Rotation", false, "If rotation should be animated.");
            ColorPickerPopupRotOpen = Bind(this, ANIMATIONS, "Color Picker Popup Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ColorPickerPopupRotClose = Bind(this, ANIMATIONS, "Color Picker Popup Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ColorPickerPopupRotOpenDuration = Bind(this, ANIMATIONS, "Color Picker Popup Open Rotation Duration", 0f, "The duration of opening.");
            ColorPickerPopupRotCloseDuration = Bind(this, ANIMATIONS, "Color Picker Popup Close Rotation Duration", 0f, "The duration of closing.");
            ColorPickerPopupRotOpenEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ColorPickerPopupRotCloseEase = BindEnum(this, ANIMATIONS, "Color Picker Popup Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region File Dropdown

            FileDropdownActive = Bind(this, ANIMATIONS, "File Dropdown Active", true, "If the popup animation should play.");

            FileDropdownPosActive = Bind(this, ANIMATIONS, "File Dropdown Animate Position", false, "If position should be animated.");
            FileDropdownPosOpen = Bind(this, ANIMATIONS, "File Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownPosClose = Bind(this, ANIMATIONS, "File Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownPosOpenDuration = Bind(this, ANIMATIONS, "File Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            FileDropdownPosCloseDuration = Bind(this, ANIMATIONS, "File Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            FileDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "File Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            FileDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "File Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            FileDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "File Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            FileDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "File Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            FileDropdownScaActive = Bind(this, ANIMATIONS, "File Dropdown Animate Scale", true, "If scale should be animated.");
            FileDropdownScaOpen = Bind(this, ANIMATIONS, "File Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownScaClose = Bind(this, ANIMATIONS, "File Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownScaOpenDuration = Bind(this, ANIMATIONS, "File Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            FileDropdownScaCloseDuration = Bind(this, ANIMATIONS, "File Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            FileDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "File Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            FileDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "File Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            FileDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "File Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            FileDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "File Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            FileDropdownRotActive = Bind(this, ANIMATIONS, "File Dropdown Animate Rotation", false, "If rotation should be animated.");
            FileDropdownRotOpen = Bind(this, ANIMATIONS, "File Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            FileDropdownRotClose = Bind(this, ANIMATIONS, "File Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            FileDropdownRotOpenDuration = Bind(this, ANIMATIONS, "File Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            FileDropdownRotCloseDuration = Bind(this, ANIMATIONS, "File Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            FileDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "File Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            FileDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "File Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Edit Dropdown

            EditDropdownActive = Bind(this, ANIMATIONS, "Edit Dropdown Active", true, "If the popup animation should play.");

            EditDropdownPosActive = Bind(this, ANIMATIONS, "Edit Dropdown Animate Position", false, "If position should be animated.");
            EditDropdownPosOpen = Bind(this, ANIMATIONS, "Edit Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownPosClose = Bind(this, ANIMATIONS, "Edit Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownPosOpenDuration = Bind(this, ANIMATIONS, "Edit Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            EditDropdownPosCloseDuration = Bind(this, ANIMATIONS, "Edit Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            EditDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            EditDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            EditDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            EditDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            EditDropdownScaActive = Bind(this, ANIMATIONS, "Edit Dropdown Animate Scale", true, "If scale should be animated.");
            EditDropdownScaOpen = Bind(this, ANIMATIONS, "Edit Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownScaClose = Bind(this, ANIMATIONS, "Edit Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownScaOpenDuration = Bind(this, ANIMATIONS, "Edit Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            EditDropdownScaCloseDuration = Bind(this, ANIMATIONS, "Edit Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            EditDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            EditDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            EditDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            EditDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            EditDropdownRotActive = Bind(this, ANIMATIONS, "Edit Dropdown Animate Rotation", false, "If rotation should be animated.");
            EditDropdownRotOpen = Bind(this, ANIMATIONS, "Edit Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            EditDropdownRotClose = Bind(this, ANIMATIONS, "Edit Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            EditDropdownRotOpenDuration = Bind(this, ANIMATIONS, "Edit Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            EditDropdownRotCloseDuration = Bind(this, ANIMATIONS, "Edit Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            EditDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            EditDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "Edit Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region View Dropdown

            ViewDropdownActive = Bind(this, ANIMATIONS, "View Dropdown Active", true, "If the popup animation should play.");

            ViewDropdownPosActive = Bind(this, ANIMATIONS, "View Dropdown Animate Position", false, "If position should be animated.");
            ViewDropdownPosOpen = Bind(this, ANIMATIONS, "View Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownPosClose = Bind(this, ANIMATIONS, "View Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownPosOpenDuration = Bind(this, ANIMATIONS, "View Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            ViewDropdownPosCloseDuration = Bind(this, ANIMATIONS, "View Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            ViewDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "View Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            ViewDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "View Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            ViewDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "View Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            ViewDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "View Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            ViewDropdownScaActive = Bind(this, ANIMATIONS, "View Dropdown Animate Scale", true, "If scale should be animated.");
            ViewDropdownScaOpen = Bind(this, ANIMATIONS, "View Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownScaClose = Bind(this, ANIMATIONS, "View Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownScaOpenDuration = Bind(this, ANIMATIONS, "View Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            ViewDropdownScaCloseDuration = Bind(this, ANIMATIONS, "View Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            ViewDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "View Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            ViewDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "View Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            ViewDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "View Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            ViewDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "View Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            ViewDropdownRotActive = Bind(this, ANIMATIONS, "View Dropdown Animate Rotation", false, "If rotation should be animated.");
            ViewDropdownRotOpen = Bind(this, ANIMATIONS, "View Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            ViewDropdownRotClose = Bind(this, ANIMATIONS, "View Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            ViewDropdownRotOpenDuration = Bind(this, ANIMATIONS, "View Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            ViewDropdownRotCloseDuration = Bind(this, ANIMATIONS, "View Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            ViewDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "View Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            ViewDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "View Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Settings Dropdown

            SettingsDropdownActive = Bind(this, ANIMATIONS, "Settings Dropdown Active", true, "If the popup animation should play.");

            SettingsDropdownPosActive = Bind(this, ANIMATIONS, "Settings Dropdown Animate Position", false, "If position should be animated.");
            SettingsDropdownPosOpen = Bind(this, ANIMATIONS, "Settings Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SettingsDropdownPosClose = Bind(this, ANIMATIONS, "Settings Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SettingsDropdownPosOpenDuration = Bind(this, ANIMATIONS, "Settings Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            SettingsDropdownPosCloseDuration = Bind(this, ANIMATIONS, "Settings Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            SettingsDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            SettingsDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            SettingsDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            SettingsDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            SettingsDropdownScaActive = Bind(this, ANIMATIONS, "Settings Dropdown Animate Scale", true, "If scale should be animated.");
            SettingsDropdownScaOpen = Bind(this, ANIMATIONS, "Settings Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SettingsDropdownScaClose = Bind(this, ANIMATIONS, "Settings Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SettingsDropdownScaOpenDuration = Bind(this, ANIMATIONS, "Settings Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SettingsDropdownScaCloseDuration = Bind(this, ANIMATIONS, "Settings Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SettingsDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            SettingsDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            SettingsDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            SettingsDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            SettingsDropdownRotActive = Bind(this, ANIMATIONS, "Settings Dropdown Animate Rotation", false, "If rotation should be animated.");
            SettingsDropdownRotOpen = Bind(this, ANIMATIONS, "Settings Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SettingsDropdownRotClose = Bind(this, ANIMATIONS, "Settings Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SettingsDropdownRotOpenDuration = Bind(this, ANIMATIONS, "Settings Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            SettingsDropdownRotCloseDuration = Bind(this, ANIMATIONS, "Settings Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            SettingsDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            SettingsDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "Settings Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Steam Dropdown

            SteamDropdownActive = Bind(this, ANIMATIONS, "Steam Dropdown Active", true, "If the popup animation should play.");

            SteamDropdownPosActive = Bind(this, ANIMATIONS, "Steam Dropdown Animate Position", false, "If position should be animated.");
            SteamDropdownPosOpen = Bind(this, ANIMATIONS, "Steam Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownPosClose = Bind(this, ANIMATIONS, "Steam Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownPosOpenDuration = Bind(this, ANIMATIONS, "Steam Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            SteamDropdownPosCloseDuration = Bind(this, ANIMATIONS, "Steam Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            SteamDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            SteamDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            SteamDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            SteamDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            SteamDropdownScaActive = Bind(this, ANIMATIONS, "Steam Dropdown Animate Scale", true, "If scale should be animated.");
            SteamDropdownScaOpen = Bind(this, ANIMATIONS, "Steam Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownScaClose = Bind(this, ANIMATIONS, "Steam Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownScaOpenDuration = Bind(this, ANIMATIONS, "Steam Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            SteamDropdownScaCloseDuration = Bind(this, ANIMATIONS, "Steam Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            SteamDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            SteamDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            SteamDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            SteamDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            SteamDropdownRotActive = Bind(this, ANIMATIONS, "Steam Dropdown Animate Rotation", false, "If rotation should be animated.");
            SteamDropdownRotOpen = Bind(this, ANIMATIONS, "Steam Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            SteamDropdownRotClose = Bind(this, ANIMATIONS, "Steam Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            SteamDropdownRotOpenDuration = Bind(this, ANIMATIONS, "Steam Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            SteamDropdownRotCloseDuration = Bind(this, ANIMATIONS, "Steam Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            SteamDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            SteamDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "Steam Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #region Help Dropdown

            HelpDropdownActive = Bind(this, ANIMATIONS, "Help Dropdown Active", true, "If the popup animation should play.");

            HelpDropdownPosActive = Bind(this, ANIMATIONS, "Help Dropdown Animate Position", false, "If position should be animated.");
            HelpDropdownPosOpen = Bind(this, ANIMATIONS, "Help Dropdown Open Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownPosClose = Bind(this, ANIMATIONS, "Help Dropdown Close Position", new Vector2(-37.5f, -16f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownPosOpenDuration = Bind(this, ANIMATIONS, "Help Dropdown Open Position Duration", Vector2.zero, "The duration of opening.");
            HelpDropdownPosCloseDuration = Bind(this, ANIMATIONS, "Help Dropdown Close Position Duration", Vector2.zero, "The duration of closing.");
            HelpDropdownPosXOpenEase = BindEnum(this, ANIMATIONS, "Help Dropdown Open Position X Ease", Easing.Linear, "The easing of opening.");
            HelpDropdownPosXCloseEase = BindEnum(this, ANIMATIONS, "Help Dropdown Close Position X Ease", Easing.Linear, "The easing of opening.");
            HelpDropdownPosYOpenEase = BindEnum(this, ANIMATIONS, "Help Dropdown Open Position Y Ease", Easing.Linear, "The easing of opening.");
            HelpDropdownPosYCloseEase = BindEnum(this, ANIMATIONS, "Help Dropdown Close Position Y Ease", Easing.Linear, "The easing of opening.");

            HelpDropdownScaActive = Bind(this, ANIMATIONS, "Help Dropdown Animate Scale", true, "If scale should be animated.");
            HelpDropdownScaOpen = Bind(this, ANIMATIONS, "Help Dropdown Open Scale", Vector2.one, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownScaClose = Bind(this, ANIMATIONS, "Help Dropdown Close Scale", new Vector2(1f, 0f), "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownScaOpenDuration = Bind(this, ANIMATIONS, "Help Dropdown Open Scale Duration", new Vector2(0.6f, 0.6f), "The duration of opening.");
            HelpDropdownScaCloseDuration = Bind(this, ANIMATIONS, "Help Dropdown Close Scale Duration", new Vector2(0.1f, 0.1f), "The duration of closing.");
            HelpDropdownScaXOpenEase = BindEnum(this, ANIMATIONS, "Help Dropdown Open Scale X Ease", Easing.OutElastic, "The easing of opening.");
            HelpDropdownScaXCloseEase = BindEnum(this, ANIMATIONS, "Help Dropdown Close Scale X Ease", Easing.InCirc, "The easing of opening.");
            HelpDropdownScaYOpenEase = BindEnum(this, ANIMATIONS, "Help Dropdown Open Scale Y Ease", Easing.OutElastic, "The easing of opening.");
            HelpDropdownScaYCloseEase = BindEnum(this, ANIMATIONS, "Help Dropdown Close Scale Y Ease", Easing.InCirc, "The easing of opening.");

            HelpDropdownRotActive = Bind(this, ANIMATIONS, "Help Dropdown Animate Rotation", false, "If rotation should be animated.");
            HelpDropdownRotOpen = Bind(this, ANIMATIONS, "Help Dropdown Open Rotation", 0f, "Where the animation starts when the popup is closing and ends when the popup is opening.");
            HelpDropdownRotClose = Bind(this, ANIMATIONS, "Help Dropdown Close Rotation", 0f, "Where the animation starts when the popup is opening and ends when the popup is closing.");
            HelpDropdownRotOpenDuration = Bind(this, ANIMATIONS, "Help Dropdown Open Rotation Duration", 0f, "The duration of opening.");
            HelpDropdownRotCloseDuration = Bind(this, ANIMATIONS, "Help Dropdown Close Rotation Duration", 0f, "The duration of closing.");
            HelpDropdownRotOpenEase = BindEnum(this, ANIMATIONS, "Help Dropdown Open Rotation Ease", Easing.Linear, "The easing of opening.");
            HelpDropdownRotCloseEase = BindEnum(this, ANIMATIONS, "Help Dropdown Close Rotation Ease", Easing.Linear, "The easing of opening.");

            #endregion

            #endregion

            #region Preview

            OnlyObjectsOnCurrentLayerVisible = Bind(this, PREVIEW, "Only Objects on Current Layer Visible", false, "If enabled, all objects not on current layer will be set to transparent");
            VisibleObjectOpacity = Bind(this, PREVIEW, "Visible Object Opacity", 0.2f, "Opacity of the objects not on the current layer.");
            ShowEmpties = Bind(this, PREVIEW, "Show Empties", false, "If enabled, show all objects that are set to the empty object type.");
            OnlyShowDamagable = Bind(this, PREVIEW, "Only Show Damagable", false, "If enabled, only objects that can damage the player will be shown.");
            HighlightObjects = Bind(this, PREVIEW, "Highlight Objects", true, "If enabled and if cursor hovers over an object, it will be highlighted.");
            ObjectHighlightAmount = Bind(this, PREVIEW, "Object Highlight Amount", new Color(0.1f, 0.1f, 0.1f), "If an object is hovered, it adds this amount of color to the hovered object.");
            ObjectHighlightDoubleAmount = Bind(this, PREVIEW, "Object Highlight Double Amount", new Color(0.5f, 0.5f, 0.5f), "If an object is hovered and shift is held, it adds this amount of color to the hovered object.");
            ObjectDraggerHelper = Bind(this, PREVIEW, "Object Dragger Helper", true, "If the object dragger helper should be enabled. This displays empty objects, the true origin of an object and allows dragging other types of objects.");
            ObjectDraggerHelperType = BindEnum(this, PREVIEW, "Drag Type", TransformType.Position, "Transform type to drag.");
            ObjectDraggerEnabled = Bind(this, PREVIEW, "Object Dragger Enabled", false, "If an object can be dragged around.");
            ObjectDraggerCreatesKeyframe = Bind(this, PREVIEW, "Object Dragger Creates Keyframe", false, "When an object is dragged, create a keyframe.");
            ObjectDraggerRotatorRadius = Bind(this, PREVIEW, "Object Dragger Rotator Radius", 22f, "The size of the Object Draggers' rotation ring.");
            ObjectDraggerScalerOffset = Bind(this, PREVIEW, "Object Dragger Scaler Offset", 6f, "The distance of the Object Draggers' scale arrows.");
            ObjectDraggerScalerScale = Bind(this, PREVIEW, "Object Dragger Scaler Scale", 1.6f, "The size of the Object Draggers' scale arrows.");

            SelectTextObjectsInPreview = Bind(this, PREVIEW, "Select Text Objects", false, "If text objects can be selected in the preview area.");
            SelectImageObjectsInPreview = Bind(this, PREVIEW, "Select Image Objects", true, "If image objects can be selected in the preview area.");

            PreviewGridEnabled = Bind(this, PREVIEW, "Grid Enabled", false, "If the preview grid should be enabled.");
            PreviewGridSize = Bind(this, PREVIEW, "Grid Size", 0.5f, "The overall size of the preview grid.");
            PreviewGridThickness = Bind(this, PREVIEW, "Grid Thickness", 0.1f, "The line thickness of the preview grid.", 0.01f, 2f);
            PreviewGridColor = Bind(this, PREVIEW, "Grid Color", Color.white, "The color of the preview grid.");

            #endregion

            #region Modifiers

            ModifiersCanLoadLevels = Bind(this, MODIFIERS, "Modifiers Can Load Levels", true, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            ModifiersSavesBackup = Bind(this, MODIFIERS, "Modifiers Saves Backup", true, "The current level will have a backup saved before a level is loaded using a loadLevel modifier or before the game has been quit.");

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
            ObjectEditor.TimelineCollapseLength = TimelineCollapseLength.Value;
            if (EditorTimeline.inst)
                EditorTimeline.inst.RenderTimelineObjects();
        }

        void ThemeEventKeyframeChanged()
        {
            RTThemeEditor.eventThemesPerPage = ThemesEventKeyframePerPage.Value;

            if (!CoreHelper.InEditor)
                return;

            var p = Mathf.Clamp(RTThemeEditor.inst.eventThemePage, 0, ThemeManager.inst.ThemeCount / RTThemeEditor.eventThemesPerPage).ToString();

            if (RTThemeEditor.eventPageStorage.inputField.text != p)
            {
                RTThemeEditor.eventPageStorage.inputField.text = p;
                return;
            }


            if (RTThemeEditor.inst.Dialog.GameObject.activeInHierarchy)
                CoroutineHelper.StartCoroutine(RTThemeEditor.inst.RenderThemeList(RTThemeEditor.inst.Dialog.SearchTerm));
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

            if ((showEmpties != ShowEmpties.Value || OnlyShowDamagable.Value != OnlyShowDamagable.Value) && CoreHelper.InEditor)
                RTLevel.Reinit();

            showEmpties = ShowEmpties.Value;
        }

        static bool showEmpties;

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

        #endregion
    }
}
