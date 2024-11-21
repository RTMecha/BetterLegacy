using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;
using BetterLegacy.Menus.UI.Interfaces;

using TMPro;
using SimpleJSON;
using LSFunctions;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Data;
using BetterLegacy.Core;

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

            #endregion

            #region Func

            playBlipSound = orig.playBlipSound,
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

            if (!string.IsNullOrEmpty(jnElement["text"]))
                text = ParseText(FontManager.TextTranslater.ReplaceProperties(jnElement["text"]));
            if (jnElement["select"] != null)
                selectionPosition = Parser.TryParse(jnElement["select"], Vector2Int.zero);
            if (jnElement["align_select"] != null)
                autoAlignSelectionPosition = jnElement["align_select"].AsBool;
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

            if (jnElement["sel_col"] != null)
                selectedColor = jnElement["sel_col"].AsInt;
            if (jnElement["sel_opacity"] != null)
                selectedOpacity = jnElement["sel_opacity"].AsFloat;
            if (jnElement["sel_hue"] != null)
                selectedHue = jnElement["sel_hue"].AsFloat;
            if (jnElement["sel_sat"] != null)
                selectedSat = jnElement["sel_sat"].AsFloat;
            if (jnElement["sel_val"] != null)
                selectedVal = jnElement["sel_val"].AsFloat;
            if (jnElement["sel_text_col"] != null)
                selectedTextColor = jnElement["sel_text_col"].AsInt;
            if (jnElement["sel_text_hue"] != null)
                selectedTextHue = jnElement["sel_text_hue"].AsFloat;
            if (jnElement["sel_text_sat"] != null)
                selectedTextSat = jnElement["sel_text_sat"].AsFloat;
            if (jnElement["sel_text_val"] != null)
                selectedTextVal = jnElement["sel_text_val"].AsFloat;

            if (jnElement["override_col"] != null)
                overrideColor = LSColors.HexToColorAlpha(jnElement["override_col"]);
            if (jnElement["override_text_col"] != null)
                overrideTextColor = LSColors.HexToColorAlpha(jnElement["override_text_col"]);
            if (jnElement["override_sel_col"] != null)
                overrideSelectedColor = LSColors.HexToColorAlpha(jnElement["override_sel_col"]);
            if (jnElement["override_sel_text_col"] != null)
                overrideSelectedTextColor = LSColors.HexToColorAlpha(jnElement["override_sel_text_col"]);
            useOverrideColor = jnElement["override_col"] != null;
            useOverrideTextColor = jnElement["override_text_col"] != null;
            useOverrideSelectedColor = jnElement["override_sel_col"] != null;
            useOverrideSelectedTextColor = jnElement["override_sel_text_col"] != null;

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = jnElement["wait"].AsBool;
            if (jnElement["anim_length"] != null)
                length = jnElement["anim_length"].AsFloat;
            else if (!parsed)
                length = 0f;
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
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = jnElement["spawn_func"]; // function to run when the element spawns.
            if (jnElement["enter_func"] != null)
                enterFuncJSON = jnElement["enter_func"]; // function to run when the element is hovered over.
            if (jnElement["exit_func"] != null)
                exitFuncJSON = jnElement["exit_func"]; // function to run when the element is hovered over.
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = jnElement["on_scroll_up_func"]; // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = jnElement["on_scroll_down_func"]; // function to run when the element is scrolled on.
            if (jnElement["allow_original_hover_func"] != null)
                allowOriginalHoverMethods = jnElement["allow_original_hover_func"].AsBool;
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
                ParseFunction(enterFuncJSON);
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
                ParseFunction(exitFuncJSON);
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
