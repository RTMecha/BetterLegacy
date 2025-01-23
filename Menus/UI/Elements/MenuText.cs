using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

using TMPro;
using SimpleJSON;
using BetterLegacy.Core;
using LSFunctions;
using BetterLegacy.Core.Data;

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
        public string text = "";

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

        public static new MenuText Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            var element = new MenuText();
            element.Read(jnElement, j, loop, spriteAssets);
            element.parsed = true;
            return element;
        }

        public override void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            #region Base

            if (!string.IsNullOrEmpty(jnElement["id"]))
                id = jnElement["id"];
            if (string.IsNullOrEmpty(id))
                id = LSText.randomNumString(16);
            if (!string.IsNullOrEmpty(jnElement["name"]))
                name = jnElement["name"];
            if (!string.IsNullOrEmpty(jnElement["parent_layout"]))
                parentLayout = jnElement["parent_layout"];
            if (!string.IsNullOrEmpty(jnElement["parent"]))
                parent = jnElement["parent"];
            if (jnElement["sibling_index"] != null)
                siblingIndex = jnElement["sibling_index"].AsInt;

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = jnElement["regen"].AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            if (jnElement["speeds"] != null)
            {
                textSpeeds = new List<Speed>();
                for (int i = 0; i < jnElement["speeds"].Count; i++)
                    textSpeeds.Add(new Speed(jnElement["speeds"][i]["position"].AsInt, jnElement["speeds"][i]["speed"].AsFloat));
            }

            if (!string.IsNullOrEmpty(jnElement["text"]))
                text = InterfaceManager.inst.ParseText(Lang.Parse(jnElement["text"]));
            if (!string.IsNullOrEmpty(jnElement["icon"]))
                icon = jnElement["icon"] != null ? spriteAssets != null && spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null;
            if (!string.IsNullOrEmpty(jnElement["icon_path"]))
                iconPath = RTFile.ParsePaths(jnElement["icon_path"]);
            if (jnElement["rect"] != null)
            rect = RectValues.TryParse(jnElement["rect"], RectValues.Default);
            if (jnElement["text_rect"] != null)
                textRect = RectValues.TryParse(jnElement["text_rect"], RectValues.FullAnchored);
            if (jnElement["icon_rect"] != null)
                iconRect = RectValues.TryParse(jnElement["icon_rect"], RectValues.Default);
            if (jnElement["rounded"] != null)
                rounded = jnElement["rounded"].AsInt; // roundness can be prevented by setting rounded to 0.
            if (jnElement["rounded_side"] != null)
                roundedSide = (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt; // default side should be Whole.
            if (jnElement["mask"] != null)
                mask = jnElement["mask"].AsBool;
            if (jnElement["reactive"] != null)
                reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j);
            if (jnElement["alignment"] != null)
                alignment = Parser.TryParse(jnElement["alignment"], TextAlignmentOptions.Left);
            if (jnElement["word_wrap"] != null)
                enableWordWrapping = jnElement["word_wrap"].AsBool;
            if (jnElement["overflow_mode"] != null)
                overflowMode = Parser.TryParse(jnElement["overflow_mode"], TextOverflowModes.Masking);
            if (jnElement["update_text"] != null)
                updateTextOnTick = jnElement["update_text"].AsBool;
            if (jnElement["run_animations_end"] != null)
                runAnimationsOnEnd = jnElement["run_animations_end"].AsBool;

            #endregion

            #region Color

            if (jnElement["hide_bg"] != null)
                hideBG = jnElement["hide_bg"].AsBool;
            if (jnElement["col"] != null)
                color = jnElement["col"].AsInt;
            if (jnElement["opacity"] != null)
                opacity = jnElement["opacity"].AsFloat;
            if (jnElement["hue"] != null)
                hue = jnElement["hue"].AsFloat;
            if (jnElement["sat"] != null)
                sat = jnElement["sat"].AsFloat;
            if (jnElement["val"] != null)
                val = jnElement["val"].AsFloat;
            if (jnElement["text_col"] != null)
                textColor = jnElement["text_col"].AsInt;
            if (jnElement["text_hue"] != null)
                textHue = jnElement["text_hue"].AsFloat;
            if (jnElement["text_sat"] != null)
                textSat = jnElement["text_sat"].AsFloat;
            if (jnElement["text_val"] != null)
                textVal = jnElement["text_val"].AsFloat;
            if (jnElement["override_col"] != null)
                overrideColor = LSColors.HexToColorAlpha(jnElement["override_col"]);
            if (jnElement["override_text_col"] != null)
                overrideTextColor = LSColors.HexToColorAlpha(jnElement["override_text_col"]);
            useOverrideColor = jnElement["override_col"] != null;
            useOverrideTextColor = jnElement["override_text_col"] != null;

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = jnElement["wait"].AsBool;
            if (jnElement["anim_length"] != null)
                length = jnElement["anim_length"].AsFloat;
            else if (!parsed)
                length = 0f;
            if (jnElement["play_sound"] != null)
                playSound = jnElement["play_sound"].AsBool;
            if (jnElement["text_sound"] != null)
                textSound = jnElement["text_sound"];
            if (jnElement["text_sound_volume"] != null)
                textSoundVolume = jnElement["text_sound_volume"].AsFloat;
            if (jnElement["text_sound_pitch"] != null)
                textSoundPitch = jnElement["text_sound_pitch"].AsFloat;
            if (jnElement["text_sound_pitch_vary"] != null)
                textSoundPitchVary = jnElement["text_sound_pitch_vary"].AsFloat;
            if (jnElement["text_sound_repeat"] != null)
                textSoundRepeat = jnElement["text_sound_repeat"].AsInt;
            if (jnElement["text_sound_ranges"] != null)
            {
                textSoundRanges = new List<Vector2Int>();
                for (int i = 0; i < jnElement["text_sound_ranges"].Count; i++)
                    textSoundRanges.Add(Parser.TryParse(jnElement["text_sound_ranges"][i], Vector2Int.zero));
            }

            #endregion

            #region Func

            if (jnElement["play_blip_sound"] != null)
                playBlipSound = jnElement["play_blip_sound"].AsBool;
            if (jnElement["func"] != null)
                funcJSON = jnElement["func"]; // function to run when the element is clicked.
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = jnElement["on_scroll_up_func"]; // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = jnElement["on_scroll_down_func"]; // function to run when the element is scrolled on.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = jnElement["spawn_func"]; // function to run when the element spawns.
            if (jnElement["on_wait_end_func"] != null)
                onWaitEndFuncJSON = jnElement["on_wait_end_func"];
            if (jnElement["tick_func"] != null)
                tickFuncJSON = jnElement["tick_func"];

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
                textWithoutFormatting = textWithoutFormatting.Replace(match.Groups[0].ToString(), "");
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

                var pitch = textSoundPitch + UnityEngine.Random.Range(-textSoundPitchVary, textSoundPitchVary);
                if (!string.IsNullOrEmpty(textSound))
                {
                    if (SoundManager.inst.TryGetSound(textSound, out AudioClip audioClip))
                        SoundManager.inst.PlaySound(audioClip, textSoundVolume, pitch);
                    else if (cachedTextSound == null && RTFile.FileExists(textSound))
                    {
                        CoreHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{textSound}", RTFile.GetAudioType(textSound), audioClip =>
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

            if (textInterpolation != null)
                time = textInterpolation.Time;
            else
                time = (Time.time - timeOffset) * length * (text.Length / TEXT_LENGTH_DIVISION) * InterfaceManager.InterfaceSpeed * currentSpeed;
            
            if (cachedQuickElements == null)
                return;

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
