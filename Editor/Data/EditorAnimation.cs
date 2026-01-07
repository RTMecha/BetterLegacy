using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles editor UI's animation.
    /// </summary>
    public class EditorAnimation : Exists
    {
        public EditorAnimation(string name) => this.name = name;

        #region Values

        /// <summary>
        /// Name of the editor element.
        /// </summary>
        public string name;

        #region Configs

        public Setting<bool> ActiveConfig { get; set; }

        #region Position

        public Setting<bool> PosActiveConfig { get; set; }
        public Setting<Vector2> PosOpenConfig { get; set; }
        public Setting<Vector2> PosCloseConfig { get; set; }
        public Setting<Vector2> PosOpenDurationConfig { get; set; }
        public Setting<Vector2> PosCloseDurationConfig { get; set; }
        public Setting<Easing> PosXOpenEaseConfig { get; set; }
        public Setting<Easing> PosXCloseEaseConfig { get; set; }
        public Setting<Easing> PosYOpenEaseConfig { get; set; }
        public Setting<Easing> PosYCloseEaseConfig { get; set; }

        #endregion

        #region Scale

        public Setting<bool> ScaActiveConfig { get; set; }
        public Setting<Vector2> ScaOpenConfig { get; set; }
        public Setting<Vector2> ScaCloseConfig { get; set; }
        public Setting<Vector2> ScaOpenDurationConfig { get; set; }
        public Setting<Vector2> ScaCloseDurationConfig { get; set; }
        public Setting<Easing> ScaXOpenEaseConfig { get; set; }
        public Setting<Easing> ScaXCloseEaseConfig { get; set; }
        public Setting<Easing> ScaYOpenEaseConfig { get; set; }
        public Setting<Easing> ScaYCloseEaseConfig { get; set; }

        #endregion

        #region Rotation

        public Setting<bool> RotActiveConfig { get; set; }
        public Setting<float> RotOpenConfig { get; set; }
        public Setting<float> RotCloseConfig { get; set; }
        public Setting<float> RotOpenDurationConfig { get; set; }
        public Setting<float> RotCloseDurationConfig { get; set; }
        public Setting<Easing> RotOpenEaseConfig { get; set; }
        public Setting<Easing> RotCloseEaseConfig { get; set; }

        #endregion

        #endregion

        #region Wrapper Values

        public bool Active => ActiveConfig.Value;

        #region Position

        public bool PosActive => PosActiveConfig.Value;
        public Vector2 PosStart => PosCloseConfig.Value;
        public Vector2 PosEnd => PosOpenConfig.Value;
        public float PosXStartDuration => PosOpenDurationConfig.Value.x;
        public float PosXEndDuration => PosCloseDurationConfig.Value.x;
        public Easing PosXStartEase => PosXOpenEaseConfig.Value;
        public Easing PosXEndEase => PosXCloseEaseConfig.Value;
        public float PosYStartDuration => PosOpenDurationConfig.Value.y;
        public float PosYEndDuration => PosCloseDurationConfig.Value.y;
        public Easing PosYStartEase => PosYOpenEaseConfig.Value;
        public Easing PosYEndEase => PosYCloseEaseConfig.Value;

        #endregion

        #region Scale

        public bool ScaActive => ScaActiveConfig.Value;
        public Vector2 ScaStart => ScaCloseConfig.Value;
        public Vector2 ScaEnd => ScaOpenConfig.Value;
        public float ScaXStartDuration => ScaOpenDurationConfig.Value.x;
        public float ScaXEndDuration => ScaCloseDurationConfig.Value.x;
        public Easing ScaXStartEase => ScaXOpenEaseConfig.Value;
        public Easing ScaXEndEase => ScaXCloseEaseConfig.Value;
        public float ScaYStartDuration => ScaOpenDurationConfig.Value.y;
        public float ScaYEndDuration => ScaCloseDurationConfig.Value.y;
        public Easing ScaYStartEase => ScaYOpenEaseConfig.Value;
        public Easing ScaYEndEase => ScaYCloseEaseConfig.Value;

        #endregion

        #region Rotation

        public bool RotActive => RotActiveConfig.Value;
        public float RotStart => RotCloseConfig.Value;
        public float RotEnd => RotOpenConfig.Value;
        public float RotStartDuration => RotOpenDurationConfig.Value;
        public float RotEndDuration => RotCloseDurationConfig.Value;
        public Easing RotStartEase => RotOpenEaseConfig.Value;
        public Easing RotEndEase => RotCloseEaseConfig.Value;

        #endregion

        #endregion

        #endregion
    }

    /// <summary>
    /// Provides a custom way of animating an editor element.
    /// </summary>
    public class CustomEditorAnimation : Exists
    {
        public CustomEditorAnimation(string name) => this.name = name;

        #region Values

        /// <summary>
        /// Name of the editor element.
        /// </summary>
        public string name;

        /// <summary>
        /// Active config reference.
        /// </summary>
        public Setting<bool> ActiveConfig { get; set; }

        /// <summary>
        /// If the animation is active.
        /// </summary>
        public bool Active => ActiveConfig.Value;

        /// <summary>
        /// Open animation.
        /// </summary>
        public PAAnimation OpenAnimation { get; set; }

        /// <summary>
        /// Close animation.
        /// </summary>
        public PAAnimation CloseAnimation { get; set; }

        #endregion

        #region Functions

        /// <summary>
        /// Plays the animation based on the active state.
        /// </summary>
        /// <param name="active">Active state.</param>
        /// <param name="editorPopup">Editor popup to play the animation on.</param>
        /// <returns>Returns the generated <see cref="RTAnimation"/>.</returns>
        public RTAnimation Play(bool active, EditorPopup editorPopup) => active ? PlayOpen(editorPopup) : PlayClose(editorPopup);

        /// <summary>
        /// Plays the open animation.
        /// </summary>
        /// <param name="editorPopup">Editor popup to play the animation on.</param>
        /// <returns>Returns the generated <see cref="RTAnimation"/>.</returns>
        public RTAnimation PlayOpen(EditorPopup editorPopup)
        {
            editorPopup.SetActive(true);
            if (!OpenAnimation)
                return null;

            var runtimeAnim = OpenAnimation.ToRTAnimation(editorPopup.GameObject.transform);
            runtimeAnim.onComplete = () => AnimationManager.inst.Remove(runtimeAnim.id);
            AnimationManager.inst.Play(runtimeAnim);
            return runtimeAnim;
        }

        /// <summary>
        /// Plays the close animation.
        /// </summary>
        /// <param name="editorPopup">Editor popup to play the animation on.</param>
        /// <returns>Returns the generated <see cref="RTAnimation"/>.</returns>
        public RTAnimation PlayClose(EditorPopup editorPopup)
        {
            if (!CloseAnimation)
            {
                editorPopup.SetActive(false);
                return null;
            }

            var runtimeAnim = CloseAnimation.ToRTAnimation(editorPopup.GameObject.transform);
            runtimeAnim.onComplete = () =>
            {
                AnimationManager.inst.Remove(runtimeAnim.id);
                editorPopup.SetActive(false);
            };
            AnimationManager.inst.Play(runtimeAnim);
            return runtimeAnim;
        }

        #endregion
    }
}
