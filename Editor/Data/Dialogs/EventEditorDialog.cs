using System.Collections.Generic;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Events;
using BetterLegacy.Editor.Managers;

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

            for (int i = 0; i < RTEventEditor.EventTypes.Length; i++)
            {
                KeyframeDialog keyframeDialog = i switch
                {
                    EventEngine.MOVE => new Vector2KeyframeDialog(i),
                    EventEngine.ZOOM => new FloatKeyframeDialog(i, min: -9999f, max: 9999f, onValueChanged: _val =>
                    {
                        if (_val < 0f)
                            AchievementManager.inst.UnlockAchievement("editor_zoom_break");
                    }),
                    EventEngine.ROTATE => new FloatKeyframeDialog(i, 15f, 3f),
                    EventEngine.SHAKE => new ShakeKeyframeDialog(),
                    EventEngine.THEME => new ThemeKeyframeDialog(),
                    EventEngine.CHROMA => new FloatKeyframeDialog(i, max: float.PositiveInfinity, allowNegative: false),
                    EventEngine.BLOOM => new BloomKeyframeDialog(),
                    EventEngine.VIGNETTE => new VignetteKeyframeDialog(),
                    EventEngine.LENS => new LensKeyframeDialog(),
                    EventEngine.GRAIN => new GrainKeyframeDialog(),
                    EventEngine.COLORGRADING => new ColorGradingKeyframeDialog(),
                    EventEngine.RIPPLES => new RipplesKeyframeDialog(),
                    EventEngine.RADIALBLUR => new BlurKeyframeDialog(i, 20),
                    EventEngine.COLORSPLIT => new FloatKeyframeModeDialog(i, "Offset", CoreHelper.StringToOptionData("Single", "Single Box Filtered", "Double", "Double Box Filtered")),
                    EventEngine.OFFSET => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Offset X", "Offset Y")),
                    EventEngine.GRADIENT => new GradientKeyframeDialog(),
                    EventEngine.DOUBLEVISION => new FloatKeyframeModeDialog(i, "Intensity", CoreHelper.StringToOptionData("Split", "Edges")),
                    EventEngine.SCANLINES => new ScanlinesKeyframeDialog(),
                    EventEngine.BLUR => new BlurKeyframeDialog(i, 12),
                    EventEngine.PIXEL => new FloatKeyframeDialog(i, "Amount", max: 0.99f),
                    EventEngine.BG => new BGKeyframeDialog(),
                    EventEngine.INVERT => new FloatKeyframeDialog(i, "Invert Amount", max: 1f),
                    EventEngine.TIMELINE => new TimelineKeyframeDialog(),
                    EventEngine.PLAYER => new PlayerKeyframeDialog(),
                    EventEngine.FOLLOW_PLAYER => new FollowPlayerKeyframeDialog(),
                    EventEngine.AUDIO => new AudioKeyframeDialog(),
                    EventEngine.VIDEO_PARENT => new VideoKeyframeDialog(i, false),
                    EventEngine.VIDEO => new VideoKeyframeDialog(i, true),
                    EventEngine.SHARPNESS => new FloatKeyframeDialog(i, "Intensity"),
                    EventEngine.BARS => new FloatKeyframeModeDialog(i, "Intensity", "Direction", CoreHelper.StringToOptionData("Horizontal", "Vertical")),
                    EventEngine.DANGER => new DangerKeyframeDialog(),
                    EventEngine.ROTATION => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Rotation X", "Rotation Y")) { increase = 15f, multiply = 3f, },
                    EventEngine.CAMERA_DEPTH => new CameraDepthKeyframeDialog(),
                    EventEngine.WINDOW_BASE => new WindowBaseKeyframeDialog(),
                    EventEngine.WINDOW_POSITION_X => new FloatKeyframeDialog(i, "Position X"),
                    EventEngine.WINDOW_POSITION_Y => new FloatKeyframeDialog(i, "Position Y"),
                    EventEngine.PLAYER_FORCE => new Vector2KeyframeDialog(i, new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Force X", "Force Y")),
                    EventEngine.MOSAIC => new FloatKeyframeDialog(i, "Amount"),
                    EventEngine.ANALOG_GLITCH => new AnalogGlitchKeyframeDialog(),
                    EventEngine.DIGITAL_GLITCH => new FloatKeyframeDialog(i, "Intensity"),
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
