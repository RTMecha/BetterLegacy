using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling text elements in the interface. Based on <see cref="MenuImage"/>.
    /// </summary>
    public class MenuText : MenuImage
    {
        public const float TEXT_LENGTH_DIVISION = 126f;

        #region Public Fields

        /// <summary>
        /// Dynamic changing speed.
        /// </summary>
        public List<Speed> textSpeeds;

        /// <summary>
        /// A sub-image that spawns if <see cref="MenuImage.icon"/> exists.
        /// </summary>
        public Image iconUI;

        /// <summary>
        /// If the background shouldn't display.
        /// </summary>
        public bool hideBG;

        /// <summary>
        /// RectTransform values for the icon..
        /// </summary>
        public RectValues iconRect = RectValues.Default;

        /// <summary>
        /// RectTransform values for the text.
        /// </summary>
        public RectValues textRect = RectValues.FullAnchored;

        /// <summary>
        /// The animation of the text typewriter effect.
        /// </summary>
        public RTAnimation textInterpolation;

        /// <summary>
        /// Theme color slot for the text to use.
        /// </summary>
        public int textColor;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float textHue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float textSat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float textVal;

        /// <summary>
        /// If the current text color should use <see cref="overrideTextColor"/> instead of a color slot based on <see cref="textColor"/>.
        /// </summary>
        public bool useOverrideTextColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overrideTextColor;

        /// <summary>
        /// The elements' text.
        /// </summary>
        public string text = string.Empty;

        /// <summary>
        /// If the text should be interpolated.
        /// </summary>
        public bool interpolateText = true;

        /// <summary>
        /// If the text should wrap when it overflows the sides of the element.
        /// </summary>
        public bool enableWordWrapping = false;
        
        /// <summary>
        /// The alignment of the text.
        /// </summary>
        public TextAlignmentOptions alignment = TextAlignmentOptions.Left;

        /// <summary>
        /// How text overflow should be handled.
        /// </summary>
        public TextOverflowModes overflowMode = TextOverflowModes.Masking;

        /// <summary>
        /// The text component of the element.
        /// </summary>
        public TextMeshProUGUI textUI;

        /// <summary>
        /// Sound path / name to to play when text interpolates.
        /// </summary>
        public string textSound;

        /// <summary>
        /// Text interpolation sound volume.
        /// </summary>
        public float textSoundVolume = 1f;

        /// <summary>
        /// Text interpolation sound pitch.
        /// </summary>
        public float textSoundPitch = 1f;

        /// <summary>
        /// How varied the text interpolation sound's pitch is.
        /// </summary>
        public float textSoundPitchVary = 0.1f;

        /// <summary>
        /// How often the text interpolation sound should play. If it's 0, it'll loop.
        /// </summary>
        public int textSoundRepeat = 0;

        /// <summary>
        /// Where the text interpolation should play the text interpolation sound.
        /// </summary>
        public List<Vector2Int> textSoundRanges;

        /// <summary>
        /// If text interpolation sound should play.
        /// </summary>
        public bool playSound = true;

        /// <summary>
        /// If text should be updated per-tick.
        /// </summary>
        public bool updateTextOnTick;

        /// <summary>
        /// Text to set per-tick.
        /// </summary>
        public JSONNode tickTextFunc;

        /// <summary>
        /// If quick element animations should play at the end of interpolation instead of during it.
        /// </summary>
        public bool runAnimationsOnEnd;

        #endregion

        #region Private Fields

        /// <summary>
        /// Cached text interpolation sound when an external sound is loaded.
        /// </summary>
        AudioClip cachedTextSound;

        float currentSpeed = 1f;

        string textWithoutFormatting;

        List<Tuple<float, Match, QuickElement>> cachedQuickElements;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new MenuText element with all the same values as <paramref name="orig"/>.
        /// </summary>
        /// <param name="orig">The element to copy.</param>
        /// <param name="newID">If a new ID should be generated.</param>
        /// <returns>Returns a copied MenuText element.</returns>
        public static MenuText DeepCopy(MenuText orig, bool newID = true) => new MenuText
        {
            #region Base

            id = newID ? LSText.randomNumString(16) : orig.id,
            name = orig.name,
            parentLayout = orig.parentLayout,
            parent = orig.parent,
            siblingIndex = orig.siblingIndex,

            #endregion

            #region Spawning

            regenerate = orig.regenerate,
            fromLoop = false, // if element has been spawned from the loop or if its the first / only of its kind.
            loop = orig.loop,

            #endregion

            #region UI

            textSpeeds = orig.textSpeeds?.Select(x => Speed.DeepCopy(x))?.ToList(),
            text = orig.text,
            icon = orig.icon,
            iconPath = orig.iconPath,
            interpolateText = orig.interpolateText,
            rect = orig.rect,
            textRect = orig.textRect,
            iconRect = orig.iconRect,
            rounded = orig.rounded, // roundness can be prevented by setting rounded to 0.
            roundedSide = orig.roundedSide, // default side should be Whole.
            mask = orig.mask,
            reactiveSetting = orig.reactiveSetting,
            alignment = orig.alignment,
            enableWordWrapping = orig.enableWordWrapping,
            overflowMode = orig.overflowMode,
            updateTextOnTick = orig.updateTextOnTick,
            runAnimationsOnEnd = orig.runAnimationsOnEnd,

            #endregion

            #region Color

            hideBG = orig.hideBG,
            color = orig.color,
            opacity = orig.opacity,
            hue = orig.hue,
            sat = orig.sat,
            val = orig.val,
            textColor = orig.textColor,
            textHue = orig.textHue,
            textSat = orig.textSat,
            textVal = orig.textVal,

            overrideColor = orig.overrideColor,
            overrideTextColor = orig.overrideTextColor,
            useOverrideColor = orig.useOverrideColor,
            useOverrideTextColor = orig.useOverrideTextColor,

            #endregion

            #region Anim

            wait = orig.wait,
            length = orig.length,
            playSound = orig.playSound,
            textSound = orig.textSound,
            textSoundVolume = orig.textSoundVolume,
            textSoundPitch = orig.textSoundPitch,
            textSoundPitchVary = orig.textSoundPitchVary,
            textSoundRepeat = orig.textSoundRepeat,
            textSoundRanges = orig.textSoundRanges != null ? new List<Vector2Int>(orig.textSoundRanges) : null,

            #endregion

            #region Func

            playBlipSound = orig.playBlipSound,
            selectable = orig.selectable,
            funcJSON = orig.funcJSON, // function to run when the element is clicked.
            onScrollUpFuncJSON = orig.onScrollUpFuncJSON,
            onScrollDownFuncJSON = orig.onScrollDownFuncJSON,
            spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
            onWaitEndFuncJSON = orig.onWaitEndFuncJSON,
            tickFunc = orig.tickFunc,
            func = orig.func,
            onScrollUpFunc = orig.onScrollUpFunc,
            onScrollDownFunc = orig.onScrollDownFunc,
            spawnFunc = orig.spawnFunc,
            onWaitEndFunc = orig.onWaitEndFunc,
            tickFuncJSON = orig.tickFuncJSON,

            #endregion
        };

        #region JSON

        /// <summary>
        /// Parses a <see cref="MenuText"/> interface element from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to parse.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns a parsed interface element.</returns>
        public static new MenuText Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            var element = new MenuText();
            element.Read(jnElement, j, loop, spriteAssets, customVariables);
            element.parsed = true;
            return element;
        }

        /// <summary>
        /// Reads interface element data from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to read.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public override void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            base.Read(jnElement, j, loop, spriteAssets, customVariables);

            #region UI

            var jnSpeeds = InterfaceManager.inst.ParseVarFunction(jnElement["speeds"], this, customVariables);
            if (jnSpeeds != null)
            {
                textSpeeds = new List<Speed>();
                for (int i = 0; i < jnSpeeds.Count; i++)
                {
                    var jnSpeed = InterfaceManager.inst.ParseVarFunction(jnSpeeds[i], this, customVariables);

                    if (jnSpeed == null)
                        continue;

                    var position = InterfaceManager.inst.ParseVarFunction(jnSpeed["position"], this, customVariables);
                    var speedValue = InterfaceManager.inst.ParseVarFunction(jnSpeed["speed"], this, customVariables);
                    if (position == null || speedValue == null)
                        continue;

                    textSpeeds.Add(new Speed(position.AsInt, speedValue.AsFloat));
                }
            }

            var jnText = InterfaceManager.inst.ParseVarFunction(jnElement["text"], this, customVariables);
            if (jnText != null)
                text = jnText;
            var jnTextFunc = InterfaceManager.inst.ParseVarFunction(jnElement["tick_text_func"], this, customVariables);
            if (jnTextFunc != null)
                tickTextFunc = jnTextFunc;
            var jnTextRect = InterfaceManager.inst.ParseVarFunction(jnElement["text_rect"], this, customVariables);
            if (jnTextRect != null)
                textRect = RectValues.TryParse(jnTextRect, RectValues.FullAnchored);
            var jnIconRect = InterfaceManager.inst.ParseVarFunction(jnElement["icon_rect"]);
            if (jnIconRect != null)
                iconRect = RectValues.TryParse(jnIconRect, RectValues.Default);
            var jnAlignment = InterfaceManager.inst.ParseVarFunction(jnElement["alignment"], this, customVariables);
            if (jnAlignment != null)
                alignment = Parser.TryParse(jnAlignment, TextAlignmentOptions.Left);
            var jnWordWrap = InterfaceManager.inst.ParseVarFunction(jnElement["word_wrap"]);
            if (jnWordWrap != null)
                enableWordWrapping = jnWordWrap.AsBool;
            var jnOverflowMode = InterfaceManager.inst.ParseVarFunction(jnElement["overflow_mode"], this, customVariables);
            if (jnOverflowMode != null)
                overflowMode = Parser.TryParse(jnOverflowMode, TextOverflowModes.Masking);
            var jnUpdateText = InterfaceManager.inst.ParseVarFunction(jnElement["update_text"], this, customVariables);
            if (jnUpdateText != null)
                updateTextOnTick = jnUpdateText.AsBool;
            var jnRunAnimationsEnd = InterfaceManager.inst.ParseVarFunction(jnElement["run_animations_end"], this, customVariables);
            if (jnRunAnimationsEnd != null)
                runAnimationsOnEnd = jnRunAnimationsEnd.AsBool;

            #endregion

            #region Color

            var jnHideBG = InterfaceManager.inst.ParseVarFunction(jnElement["hide_bg"], this, customVariables);
            if (jnHideBG != null)
                hideBG = jnHideBG.AsBool;
            var jnTextCol = InterfaceManager.inst.ParseVarFunction(jnElement["text_col"], this, customVariables);
            if (jnTextCol != null)
                textColor = jnTextCol.AsInt;
            var jnTextHue = InterfaceManager.inst.ParseVarFunction(jnElement["text_hue"], this, customVariables);
            if (jnTextHue != null)
                textHue = jnTextHue.AsFloat;
            var jnTextSat = InterfaceManager.inst.ParseVarFunction(jnElement["text_sat"], this, customVariables);
            if (jnTextSat != null)
                textSat = jnTextSat.AsFloat;
            var jnTextVal = InterfaceManager.inst.ParseVarFunction(jnElement["text_val"], this, customVariables);
            if (jnTextVal != null)
                textVal = jnTextVal.AsFloat;
            var jnOverrideTextCol = InterfaceManager.inst.ParseVarFunction(jnElement["override_text_col"], this, customVariables);
            useOverrideTextColor = jnOverrideTextCol != null;
            if (useOverrideTextColor)
                overrideTextColor = RTColors.HexToColor(jnOverrideTextCol);

            #endregion

            #region Anim

            var jnPlaySound = InterfaceManager.inst.ParseVarFunction(jnElement["play_sound"], this, customVariables);
            if (jnPlaySound != null)
                playSound = jnPlaySound.AsBool;
            var jnTextSound = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound"], this, customVariables);
            if (jnTextSound != null)
                textSound = jnTextSound;
            var jnTextSoundVolume = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_volume"], this, customVariables);
            if (jnTextSoundVolume != null)
                textSoundVolume = jnTextSoundVolume.AsFloat;
            var jnTextSoundPitch = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_pitch"], this, customVariables);
            if (jnTextSoundPitch != null)
                textSoundPitch = jnTextSoundPitch.AsFloat;
            var jnTextSoundPitchVary = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_pitch_vary"], this, customVariables);
            if (jnTextSoundPitchVary != null)
                textSoundPitchVary = jnTextSoundPitchVary.AsFloat;
            var jnTextSoundRepeat = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_repeat"], this, customVariables);
            if (jnTextSoundRepeat != null)
                textSoundRepeat = jnTextSoundRepeat.AsInt;
            var jnTextSoundRanges = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_ranges"], this, customVariables);
            if (jnTextSoundRanges != null)
            {
                textSoundRanges = new List<Vector2Int>();
                for (int i = 0; i < jnTextSoundRanges.Count; i++)
                    textSoundRanges.Add(Parser.TryParse(InterfaceManager.inst.ParseVarFunction(jnTextSoundRanges[i], this, customVariables), Vector2Int.zero));
            }

            #endregion
        }

        #endregion

        public override void Spawn()
        {
            timeOffset = Time.time;

            textWithoutFormatting = text;

            // Here we replace every instance of <formatting> in the text. Examples include <b>, <i>, <color=#FFFFFF>, etc. We do this to ensure the maxVisibleCharacters value is correct.
            var matches = Regex.Matches(text, "<(.*?)>");
            foreach (var obj in matches)
            {
                var match = (Match)obj;
                textWithoutFormatting = textWithoutFormatting.Remove(match.Groups[0].ToString());
            }

            // Regular formatting should be ignored.
            var quickElementMatches = Regex.Matches(textWithoutFormatting, "{{QuickElement=(.*?)}}");

            foreach (var obj in quickElementMatches)
            {
                var match = (Match)obj;

                if (cachedQuickElements == null)
                    cachedQuickElements = new List<Tuple<float, Match, QuickElement>>(); // if there aren't any matches, there isn't any need for the cachedQuickElements list.

                if (QuickElementManager.AllQuickElements.TryGetValue(match.Groups[1].ToString(), out QuickElement quickElement))
                    cachedQuickElements.Add(new Tuple<float, Match, QuickElement>(-1f, match, quickElement));
            }

            if (!interpolateText)
                return;

            isSpawning = true;
            textInterpolation = new RTAnimation("Text Interpolation");
            textInterpolation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(length * (textWithoutFormatting.Length / TEXT_LENGTH_DIVISION), textWithoutFormatting.Length, Ease.Linear),
                                             // ^ Regular spawn length is sometimes too slow for text interpolation so it's sped up relative to the text length. 
                                             // For example, if the text is 32 characters long, it'd be fine. But if it were just 3 letters, it'd be really slow looking.
                }, Interpolate),
            };
            textInterpolation.onComplete = () =>
            {
                AnimationManager.inst.Remove(textInterpolation.id);
                Interpolate(textWithoutFormatting.Length);
                isSpawning = false;
                textInterpolation = null;
            };
            AnimationManager.inst.Play(textInterpolation);
        }

        public override void UpdateSpawnCondition()
        {
            if (textSpeeds != null && textSpeeds.TryFindLast(x => x.position <= ((int)(length * TEXT_LENGTH_DIVISION) == 0 ? 0 : (int)(textInterpolation.Time / length * TEXT_LENGTH_DIVISION)), out Speed speed))
                currentSpeed = speed.speed;

            // Speeds up the text interpolation if a Submit key is being held.
            if (textInterpolation != null)
                textInterpolation.speed = InterfaceManager.InterfaceSpeed * currentSpeed;
        }

        /// <summary>
        /// Sets <see cref="TMP_Text.maxVisibleCharacters"/> so we can have a typewriter effect. Also plays a sound every time a character is placed.
        /// </summary>
        /// <param name="x"></param>
        void Interpolate(float x)
        {
            if (!interpolateText)
                return;

            var val = (int)x;

            if (playSound && textUI.maxVisibleCharacters != val && (textSoundRepeat == 0 || val % textSoundRepeat == textSoundRepeat - 1) && textWithoutFormatting[Mathf.Clamp(val, 0, textWithoutFormatting.Length - 1)] != ' ')
            {
                // x is min
                // y is max
                if (textSoundRanges != null && textSoundRanges.Count > 0 && !textSoundRanges.Has(x => x.y >= val && x.x <= val))
                {
                    textUI.maxVisibleCharacters = val;
                    return;
                }

                var pitch = textSoundPitch + UnityRandom.Range(-textSoundPitchVary, textSoundPitchVary);
                if (!string.IsNullOrEmpty(textSound))
                {
                    if (SoundManager.inst.TryGetSound(textSound, out AudioClip audioClip))
                        SoundManager.inst.PlaySound(audioClip, textSoundVolume, pitch);
                    else if (cachedTextSound == null && RTFile.FileExists(textSound))
                    {
                        CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{textSound}", RTFile.GetAudioType(textSound), audioClip =>
                        {
                            cachedTextSound = audioClip;
                            SoundManager.inst.PlaySound(audioClip, textSoundVolume, pitch);
                        }));
                    }
                    else if (cachedTextSound != null)
                        SoundManager.inst.PlaySound(cachedTextSound, textSoundVolume, pitch);
                }
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Click);
            }

            textUI.maxVisibleCharacters = val;
        }

        /// <summary>
        /// Updates <see cref="textUI"/>s' formatting, so it can utilize QuickElements.
        /// </summary>
        public void UpdateText()
        {
            var text = this.text;
            if (updateTextOnTick)
                text = InterfaceManager.inst.ParseTickText(text);
            var tickTextFunc = InterfaceManager.inst.ParseVarFunction(this.tickTextFunc, this, cachedVariables);
            if (tickTextFunc != null && tickTextFunc.IsString)
                text = tickTextFunc;

            if (textInterpolation != null)
                time = textInterpolation.Time;
            else
                time = (Time.time - timeOffset) * length * (text.Length / TEXT_LENGTH_DIVISION) * InterfaceManager.InterfaceSpeed * currentSpeed;
            
            if (cachedQuickElements == null)
            {
                if (isSpawning || !textUI)
                    return;

                textUI.maxVisibleCharacters = text.Length;
                textUI.text = text;
                return;
            }

            for (int i = 0; i < cachedQuickElements.Count; i++)
            {
                var matchPair = cachedQuickElements[i];
                var time = matchPair.Item1 < 0f ? this.time + 1f : matchPair.Item1;
                var match = matchPair.Item2;
                var quickElement = matchPair.Item3;

                if (textUI.maxVisibleCharacters > match.Index + match.Groups[1].ToString().Length && matchPair.Item1 < 0f)
                {
                    cachedQuickElements[i] = new Tuple<float, Match, QuickElement>(this.time, match, quickElement);
                    time = this.time;
                }

                textUI.text = text.Replace(match.Groups[0].ToString(), QuickElementManager.ConvertQuickElement(quickElement, this.time - time));
            }
        }

        public override void Clear()
        {
            if (textInterpolation != null)
            {
                AnimationManager.inst.Remove(textInterpolation.id);
                textInterpolation = null;
            }

            base.Clear();
        }

        #endregion

        public class Speed
        {
            public Speed(int position, float speed)
            {
                this.position = position;
                this.speed = speed;
            }

            public int position;
            public float speed = 1f;

            public static Speed DeepCopy(Speed orig) => new Speed(orig.position, orig.speed);
        }
    }
}
