using System.Collections.Generic;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the base event editor dialog which contains all event keyframe editors.
    /// </summary>
    public class EventEditorDialog : EditorDialog
    {
        public EventEditorDialog() : base(EVENT_EDITOR) { }

        /// <summary>
        /// The currently open event keyframe editor.
        /// </summary>
        public KeyframeDialog CurrentKeyframeDialog { get; set; }

        /// <summary>
        /// A list containing all the event keyframe editors.
        /// </summary>
        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            for (int i = 0; i < EventLibrary.displayNames.Length; i++)
            {
                KeyframeDialog keyframeDialog = i switch
                {
                    EventLibrary.Indexes.MOVE => new Vector2KeyframeDialog(i),
                    EventLibrary.Indexes.ZOOM => new FloatKeyframeDialog(i, min: -9999f, max: 9999f, onValueChanged: _val =>
                    {
                        if (_val < 0f)
                            AchievementManager.inst.UnlockAchievement("editor_zoom_break");
                    }),
                    EventLibrary.Indexes.ROTATE => new FloatKeyframeDialog(i, 15f, 3f),
                    EventLibrary.Indexes.SHAKE => new ShakeKeyframeDialog(),
                    EventLibrary.Indexes.THEME => new ThemeKeyframeDialog(),
                    EventLibrary.Indexes.CHROMA => new FloatKeyframeDialog(i, max: float.PositiveInfinity, allowNegative: false),
                    EventLibrary.Indexes.BLOOM => new BloomKeyframeDialog(),
                    EventLibrary.Indexes.VIGNETTE => new VignetteKeyframeDialog(),
                    EventLibrary.Indexes.LENS => new LensKeyframeDialog(),
                    EventLibrary.Indexes.GRAIN => new GrainKeyframeDialog(),
                    EventLibrary.Indexes.COLORGRADING => new ColorGradingKeyframeDialog(),
                    EventLibrary.Indexes.RIPPLES => new RipplesKeyframeDialog(),
                    EventLibrary.Indexes.RADIALBLUR => new BlurKeyframeDialog(i, 20),
                    EventLibrary.Indexes.COLORSPLIT => new FloatKeyframeModeDialog(i, "Offset", CoreHelper.StringToOptionData("Single", "Single Box Filtered", "Double", "Double Box Filtered")),
                    EventLibrary.Indexes.MOVE_OFFSET => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Offset X", "Offset Y")),
                    EventLibrary.Indexes.GRADIENT => new GradientKeyframeDialog(),
                    EventLibrary.Indexes.DOUBLEVISION => new FloatKeyframeModeDialog(i, "Intensity", CoreHelper.StringToOptionData("Split", "Edges")),
                    EventLibrary.Indexes.SCANLINES => new ScanlinesKeyframeDialog(),
                    EventLibrary.Indexes.BLUR => new BlurKeyframeDialog(i, 12),
                    EventLibrary.Indexes.PIXELIZE => new FloatKeyframeDialog(i, "Amount", max: 0.99f),
                    EventLibrary.Indexes.BG => new BGKeyframeDialog(),
                    EventLibrary.Indexes.INVERT => new FloatKeyframeDialog(i, "Invert Amount", max: 1f),
                    EventLibrary.Indexes.TIMELINE => new TimelineKeyframeDialog(),
                    EventLibrary.Indexes.PLAYER => new PlayerKeyframeDialog(),
                    EventLibrary.Indexes.FOLLOW_PLAYER => new FollowPlayerKeyframeDialog(),
                    EventLibrary.Indexes.AUDIO => new AudioKeyframeDialog(),
                    EventLibrary.Indexes.VIDEO_PARENT => new VideoKeyframeDialog(i, false),
                    EventLibrary.Indexes.VIDEO => new VideoKeyframeDialog(i, true),
                    EventLibrary.Indexes.SHARPEN => new FloatKeyframeDialog(i, "Intensity"),
                    EventLibrary.Indexes.BARS => new FloatKeyframeModeDialog(i, "Intensity", "Direction", CoreHelper.StringToOptionData("Horizontal", "Vertical")),
                    EventLibrary.Indexes.DANGER => new DangerKeyframeDialog(),
                    EventLibrary.Indexes.DEPTH_ROTATION => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Rotation X", "Rotation Y")) { increase = 15f, multiply = 3f, },
                    EventLibrary.Indexes.CAMERA_DEPTH => new CameraDepthKeyframeDialog(),
                    EventLibrary.Indexes.WINDOW_BASE => new WindowBaseKeyframeDialog(),
                    EventLibrary.Indexes.WINDOW_POSITION_X => new FloatKeyframeDialog(i, "Position X"),
                    EventLibrary.Indexes.WINDOW_POSITION_Y => new FloatKeyframeDialog(i, "Position Y"),
                    EventLibrary.Indexes.PLAYER_FORCE => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Force X", "Force Y")),
                    EventLibrary.Indexes.MOSAIC => new FloatKeyframeDialog(i, "Amount"),
                    EventLibrary.Indexes.ANALOG_GLITCH => new AnalogGlitchKeyframeDialog(),
                    EventLibrary.Indexes.DIGITAL_GLITCH => new FloatKeyframeDialog(i, "Intensity"),
                    _ => new KeyframeDialog(i),
                };
                var dialog = EventEditor.inst.dialogRight.TryGetChild(i);
                if (dialog)
                    keyframeDialog.GameObject = dialog.gameObject;
                try
                {
                    keyframeDialog.Init();
                }
                catch (System.Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                keyframeDialogs.Add(keyframeDialog);
            }
        }

        /// <summary>
        /// Opens an event keyframe editor.
        /// </summary>
        /// <param name="type">The type of event keyframe.</param>
        public void OpenKeyframeDialog(int type)
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
            {
                var active = i == type;
                keyframeDialogs[i].SetActive(active);
                if (active)
                    CurrentKeyframeDialog = keyframeDialogs[i];
            }
        }

        /// <summary>
        /// Checks if <see cref="CurrentKeyframeDialog"/> is of a specific keyframe type.
        /// </summary>
        /// <param name="type">The type of event keyframe.</param>
        /// <returns>Returns true if the current keyframe dialog type matches the specific type, otherwise returns false.</returns>
        public bool IsCurrentKeyframeType(int type) => CurrentKeyframeDialog && CurrentKeyframeDialog.type == type;

        /// <summary>
        /// Closes the keyframe dialogs.
        /// </summary>
        public void CloseKeyframeDialogs()
        {
            for (int i = 0; i < keyframeDialogs.Count; i++)
                keyframeDialogs[i].SetActive(false);
            CurrentKeyframeDialog = null;
        }
    }
}
