using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling button elements in the interface. Based on <see cref="MenuText"/>.
    /// </summary>
    public class MenuButton : MenuText
    {
        #region Public Fields

        /// <summary>
        /// True if the element is hovered, otherwise false.
        /// </summary>
        public bool isHovered;

        /// <summary>
        /// To be used for where the current selection is. If it's at 0, 0 then it's the default selection. This is compared against <see cref="MenuBase.selected"/> to see if it is selected.
        /// </summary>
        public Vector2Int selectionPosition;

        /// <summary>
        /// Opacity of the image when the element is selected.
        /// </summary>
        public float selectedOpacity = 1f;

        /// <summary>
        /// Color of the image when the element is selected.
        /// </summary>
        public int selectedColor;

        /// <summary>
        /// Hue color offset when the element is selected.
        /// </summary>
        public float selectedHue;

        /// <summary>
        /// Saturation color offset when the element is selected.
        /// </summary>
        public float selectedSat;

        /// <summary>
        /// Value color offset when the element is selected.
        /// </summary>
        public float selectedVal;

        public bool useOverrideSelectedColor;
        public Color overrideSelectedColor;

        /// <summary>
        /// Color of the text when the element is selected.
        /// </summary>
        public int selectedTextColor;

        /// <summary>
        /// Texts' hue color offset when the element is selected.
        /// </summary>
        public float selectedTextHue;

        /// <summary>
        /// Texts' saturation color offset when the element is selected.
        /// </summary>
        public float selectedTextSat;

        /// <summary>
        /// Texts' value color offset when the element is selected.
        /// </summary>
        public float selectedTextVal;

        public bool useOverrideSelectedTextColor;
        public Color overrideSelectedTextColor;

        /// <summary>
        /// Function JSON to parse when the mouse enters the element.
        /// </summary>
        public JSONNode enterFuncJSON;

        /// <summary>
        /// Function JSON to parse when the mouse exits the element.
        /// </summary>
        public JSONNode exitFuncJSON;

        /// <summary>
        /// Function called when the mouse enters the element.
        /// </summary>
        public Action enterFunc;

        /// <summary>
        /// Function called when the mouse exits the element.
        /// </summary>
        public Action exitFunc;

        /// <summary>
        /// If <see cref="selectionPosition"/> should be automatically aligned with the buttons' layout parent.
        /// </summary>
        public bool autoAlignSelectionPosition;

        /// <summary>
        /// If the original hover functions should still run despite <see cref="enterFuncJSON"/> or <see cref="enterFunc"/> existing.
        /// </summary>
        public bool allowOriginalHoverMethods = false;

        #endregion

        #region Private Fields

        RTAnimation enterAnimation;
        RTAnimation exitAnimation;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new MenuButton element with all the same values as <paramref name="orig"/>.
        /// </summary>
        /// <param name="orig">The element to copy.</param>
        /// <param name="newID">If a new ID should be generated.</param>
        /// <returns>Returns a copied MenuButton element.</returns>
        public static MenuButton DeepCopy(MenuButton orig, bool newID = true, bool newSelectionPosition = false, Vector2Int selectionPosition = default) => new MenuButton
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

            text = orig.text,
            selectionPosition = newSelectionPosition ? selectionPosition : orig.selectionPosition,
            autoAlignSelectionPosition = orig.autoAlignSelectionPosition,
            icon = orig.icon,
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

            selectedColor = orig.selectedColor,
            selectedOpacity = orig.selectedOpacity,
            selectedHue = orig.selectedHue,
            selectedSat = orig.selectedSat,
            selectedVal = orig.selectedVal,
            selectedTextColor = orig.selectedTextColor,
            selectedTextHue = orig.selectedTextHue,
            selectedTextSat = orig.selectedTextSat,
            selectedTextVal = orig.selectedTextVal,

            overrideColor = orig.overrideColor,
            overrideTextColor = orig.overrideTextColor,
            overrideSelectedColor = orig.overrideSelectedColor,
            overrideSelectedTextColor = orig.overrideSelectedTextColor,
            useOverrideColor = orig.useOverrideColor,
            useOverrideTextColor = orig.useOverrideTextColor,
            useOverrideSelectedColor = orig.useOverrideSelectedColor,
            useOverrideSelectedTextColor = orig.useOverrideSelectedTextColor,

            #endregion

            #region Anim

            length = orig.length,
            wait = orig.wait,
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
            spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
            func = orig.func,
            spawnFunc = orig.spawnFunc,
            enterFunc = orig.enterFunc,
            exitFunc = orig.exitFunc,
            enterFuncJSON = orig.enterFuncJSON,
            exitFuncJSON = orig.exitFuncJSON,
            onScrollUpFuncJSON = orig.onScrollUpFuncJSON,
            onScrollDownFuncJSON = orig.onScrollDownFuncJSON,
            allowOriginalHoverMethods = orig.allowOriginalHoverMethods,

            #endregion
        };

        #region JSON

        public static new MenuButton Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            var element = new MenuButton();
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
                name = InterfaceManager.inst.ParseVarFunction(jnElement["name"], this);
            if (!string.IsNullOrEmpty(jnElement["parent_layout"]))
                parentLayout = InterfaceManager.inst.ParseVarFunction(jnElement["parent_layout"], this);
            if (!string.IsNullOrEmpty(jnElement["parent"]))
                parent = InterfaceManager.inst.ParseVarFunction(jnElement["parent"], this);
            if (jnElement["sibling_index"] != null)
                siblingIndex = InterfaceManager.inst.ParseVarFunction(jnElement["sibling_index"], this).AsInt;

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = InterfaceManager.inst.ParseVarFunction(jnElement["regen"], this).AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            if (jnElement["speeds"] != null)
            {
                var jnSpeeds = InterfaceManager.inst.ParseVarFunction(jnElement["speeds"], this);
                textSpeeds = new List<Speed>();
                for (int i = 0; i < jnSpeeds.Count; i++)
                {
                    var jnSpeed = InterfaceManager.inst.ParseVarFunction(jnSpeeds[i], this);

                    textSpeeds.Add(new Speed(InterfaceManager.inst.ParseVarFunction(jnSpeed["position"], this).AsInt, InterfaceManager.inst.ParseVarFunction(jnSpeed["speed"], this).AsFloat));
                }
            }

            if (jnElement["text"] != null)
                text = InterfaceManager.inst.ParseVarFunction(jnElement["text"], this);
            if (!string.IsNullOrEmpty(jnElement["icon"]))
            {
                var jnIcon = InterfaceManager.inst.ParseVarFunction(jnElement["icon"], this);
                icon = jnIcon != null ? spriteAssets != null && spriteAssets.TryGetValue(jnIcon, out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnIcon) : null;
            }
            if (!string.IsNullOrEmpty(jnElement["icon_path"]))
                iconPath = InterfaceManager.inst.ParseVarFunction(jnElement["icon_path"], this);
            if (jnElement["rect"] != null)
                rect = RectValues.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["rect"], this), RectValues.Default);
            if (jnElement["text_rect"] != null)
                textRect = RectValues.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["text_rect"], this), RectValues.FullAnchored);
            if (jnElement["icon_rect"] != null)
                iconRect = RectValues.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["icon_rect"]), RectValues.Default);
            if (jnElement["rounded"] != null)
                rounded = InterfaceManager.inst.ParseVarFunction(jnElement["rounded"], this).AsInt; // roundness can be prevented by setting rounded to 0.
            if (jnElement["rounded_side"] != null)
                roundedSide = (SpriteHelper.RoundedSide)InterfaceManager.inst.ParseVarFunction(jnElement["rounded_side"], this).AsInt; // default side should be Whole.
            if (jnElement["mask"] != null)
                mask = InterfaceManager.inst.ParseVarFunction(jnElement["mask"], this).AsBool;
            if (jnElement["reactive"] != null)
                reactiveSetting = ReactiveSetting.Parse(InterfaceManager.inst.ParseVarFunction(jnElement["reactive"], this), j);
            if (jnElement["alignment"] != null)
                alignment = Parser.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["alignment"], this), TextAlignmentOptions.Left);
            if (jnElement["word_wrap"] != null)
                enableWordWrapping = InterfaceManager.inst.ParseVarFunction(jnElement["word_wrap"]).AsBool;
            if (jnElement["overflow_mode"] != null)
                overflowMode = Parser.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["overflow_mode"], this), TextOverflowModes.Masking);
            if (jnElement["update_text"] != null)
                updateTextOnTick = InterfaceManager.inst.ParseVarFunction(jnElement["update_text"], this).AsBool;
            if (jnElement["run_animations_end"] != null)
                runAnimationsOnEnd = InterfaceManager.inst.ParseVarFunction(jnElement["run_animations_end"], this).AsBool;

            #endregion

            #region Color

            if (jnElement["hide_bg"] != null)
                hideBG = InterfaceManager.inst.ParseVarFunction(jnElement["hide_bg"], this).AsBool;
            if (jnElement["col"] != null)
                color = InterfaceManager.inst.ParseVarFunction(jnElement["col"], this).AsInt;
            if (jnElement["opacity"] != null)
                opacity = InterfaceManager.inst.ParseVarFunction(jnElement["opacity"], this).AsFloat;
            if (jnElement["hue"] != null)
                hue = InterfaceManager.inst.ParseVarFunction(jnElement["hue"], this).AsFloat;
            if (jnElement["sat"] != null)
                sat = InterfaceManager.inst.ParseVarFunction(jnElement["sat"], this).AsFloat;
            if (jnElement["val"] != null)
                val = InterfaceManager.inst.ParseVarFunction(jnElement["val"], this).AsFloat;
            if (jnElement["text_col"] != null)
                textColor = InterfaceManager.inst.ParseVarFunction(jnElement["text_col"], this).AsInt;
            if (jnElement["text_hue"] != null)
                textHue = InterfaceManager.inst.ParseVarFunction(jnElement["text_hue"], this).AsFloat;
            if (jnElement["text_sat"] != null)
                textSat = InterfaceManager.inst.ParseVarFunction(jnElement["text_sat"], this).AsFloat;
            if (jnElement["text_val"] != null)
                textVal = InterfaceManager.inst.ParseVarFunction(jnElement["text_val"], this).AsFloat;
            
            if (jnElement["sel_col"] != null)
                selectedColor = InterfaceManager.inst.ParseVarFunction(jnElement["sel_col"], this).AsInt;
            if (jnElement["sel_opacity"] != null)
                selectedOpacity = InterfaceManager.inst.ParseVarFunction(jnElement["sel_opacity"], this).AsFloat;
            if (jnElement["sel_hue"] != null)
                selectedHue = InterfaceManager.inst.ParseVarFunction(jnElement["sel_hue"], this).AsFloat;
            if (jnElement["sel_sat"] != null)
                selectedSat = InterfaceManager.inst.ParseVarFunction(jnElement["sel_sat"], this).AsFloat;
            if (jnElement["sel_val"] != null)
                selectedVal = InterfaceManager.inst.ParseVarFunction(jnElement["sel_val"], this).AsFloat;
            if (jnElement["sel_text_col"] != null)
                selectedTextColor = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_col"], this).AsInt;
            if (jnElement["sel_text_hue"] != null)
                selectedTextHue = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_hue"], this).AsFloat;
            if (jnElement["sel_text_sat"] != null)
                selectedTextSat = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_sat"], this).AsFloat;
            if (jnElement["sel_text_val"] != null)
                selectedTextVal = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_val"], this).AsFloat;

            useOverrideColor = jnElement["override_col"] != null;
            useOverrideTextColor = jnElement["override_text_col"] != null;
            useOverrideSelectedColor = jnElement["override_sel_col"] != null;
            useOverrideSelectedTextColor = jnElement["override_sel_text_col"] != null;
            if (useOverrideColor)
                overrideColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_col"], this));
            if (useOverrideTextColor)
                overrideTextColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_text_col"], this));
            if (useOverrideSelectedColor)
                overrideSelectedColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_sel_col"], this));
            if (useOverrideSelectedTextColor)
                overrideSelectedTextColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_sel_text_col"], this));

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = InterfaceManager.inst.ParseVarFunction(jnElement["wait"], this).AsBool;
            if (jnElement["anim_length"] != null)
                length = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"], this).AsFloat;
            else if (!parsed)
                length = 0f;
            if (jnElement["play_sound"] != null)
                playSound = InterfaceManager.inst.ParseVarFunction(jnElement["play_sound"], this).AsBool;
            if (jnElement["text_sound"] != null)
                textSound = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound"], this);
            if (jnElement["text_sound_volume"] != null)
                textSoundVolume = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_volume"], this).AsFloat;
            if (jnElement["text_sound_pitch"] != null)
                textSoundPitch = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_pitch"], this).AsFloat;
            if (jnElement["text_sound_pitch_vary"] != null)
                textSoundPitchVary = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_pitch_vary"], this).AsFloat;
            if (jnElement["text_sound_repeat"] != null)
                textSoundRepeat = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_repeat"], this).AsInt;
            if (jnElement["text_sound_ranges"] != null)
            {
                var jnTextSoundRanges = InterfaceManager.inst.ParseVarFunction(jnElement["text_sound_ranges"], this);
                textSoundRanges = new List<Vector2Int>();
                for (int i = 0; i < jnTextSoundRanges.Count; i++)
                    textSoundRanges.Add(Parser.TryParse(jnTextSoundRanges[i], Vector2Int.zero));
            }

            #endregion

            #region Func

            if (jnElement["select"] != null)
                selectionPosition = Parser.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["select"], this), Vector2Int.zero);
            if (jnElement["align_select"] != null)
                autoAlignSelectionPosition = InterfaceManager.inst.ParseVarFunction(jnElement["align_select"], this).AsBool;

            if (jnElement["play_blip_sound"] != null)
                playBlipSound = InterfaceManager.inst.ParseVarFunction(jnElement["play_blip_sound"], this).AsBool;
            if (jnElement["selectable"] != null)
                selectable = InterfaceManager.inst.ParseVarFunction(jnElement["selectable"], this).AsBool;
            if (jnElement["func"] != null)
                funcJSON = InterfaceManager.inst.ParseVarFunction(jnElement["func"], this); // function to run when the element is clicked.
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_up_func"], this); // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_down_func"], this); // function to run when the element is scrolled on.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_func"], this); // function to run when the element spawns.
            if (jnElement["on_wait_end_func"] != null)
                onWaitEndFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_wait_end_func"], this);
            if (jnElement["tick_func"] != null)
                tickFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["tick_func"], this);

            if (jnElement["enter_func"] != null)
                enterFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["enter_func"], this); // function to run when the element is hovered over.
            if (jnElement["exit_func"] != null)
                exitFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["exit_func"], this); // function to run when the element is hovered over.
            if (jnElement["allow_original_hover_func"] != null)
                allowOriginalHoverMethods = InterfaceManager.inst.ParseVarFunction(jnElement["allow_original_hover_func"], this).AsBool;
            else if (!parsed)
                allowOriginalHoverMethods = true;

            #endregion
        }

        #endregion

        /// <summary>
        /// Plays the Enter animation when the element is selected.
        /// </summary>
        public void OnEnter()
        {
            if (enterFuncJSON != null)
            {
                InterfaceManager.inst.ParseFunction(enterFuncJSON, this);
                if (!allowOriginalHoverMethods)
                    return;
            }

            if (enterFunc != null)
            {
                enterFunc();
                if (!allowOriginalHoverMethods)
                    return;
            }

            if (exitAnimation != null)
            {
                AnimationManager.inst.Remove(exitAnimation.id);
                exitAnimation = null;
            }

            enterAnimation = new RTAnimation("Interface Element Enter Animation");
            enterAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1f, Ease.Linear),
                    new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                    new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                }, x =>
                {
                    if (gameObject != null)
                        gameObject.transform.localScale = new Vector3(x, x, 1f);
                }),
            };
            enterAnimation.onComplete = () =>
            {
                AnimationManager.inst.Remove(enterAnimation.id);
                enterAnimation = null;
            };
            AnimationManager.inst.Play(enterAnimation);
        }

        /// <summary>
        /// Plays the Exit animation when the element is no longer selected.
        /// </summary>
        public void OnExit()
        {
            if (exitFuncJSON != null)
            {
                InterfaceManager.inst.ParseFunction(exitFuncJSON, this);
                if (!allowOriginalHoverMethods)
                    return;
            }

            if (exitFunc != null)
            {
                exitFunc();
                if (!allowOriginalHoverMethods)
                    return;
            }

            if (enterAnimation != null)
            {
                AnimationManager.inst.Remove(enterAnimation.id);
                enterAnimation = null;
            }

            exitAnimation = new RTAnimation("Interface Element Exit Animation");
            exitAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1.1f, Ease.Linear),
                    new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                    new FloatKeyframe(0.31f, 1f, Ease.Linear),
                }, x =>
                {
                    if (gameObject)
                        gameObject.transform.localScale = new Vector3(x, x, 1f);
                }),
            };
            exitAnimation.onComplete = () => { AnimationManager.inst.Remove(exitAnimation.id); };
            AnimationManager.inst.Play(exitAnimation);
        }

        #endregion
    }
}
