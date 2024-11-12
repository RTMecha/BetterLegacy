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
        public float selectedOpacity;

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
            allowOriginalHoverMethods = orig.allowOriginalHoverMethods,

            #endregion
        };

        #region JSON

        public static new MenuButton Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            var element = new MenuButton();
            element.Read(jnElement, j, loop, spriteAssets);
            return element;
        }

        public override void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            #region Base

            id = jnElement["id"] == null ? LSText.randomNumString(16) : jnElement["id"];
            name = jnElement["name"];
            parentLayout = jnElement["parent_layout"];
            parent = jnElement["parent"];
            siblingIndex = jnElement["sibling_index"] == null ? -1 : jnElement["sibling_index"].AsInt;

            #endregion

            #region Spawning

            regenerate = jnElement["regen"] == null ? true : jnElement["regen"].AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            text = FontManager.TextTranslater.ReplaceProperties(jnElement["text"]);
            selectionPosition = new Vector2Int(jnElement["select"]["x"].AsInt, jnElement["select"]["y"].AsInt);
            autoAlignSelectionPosition = jnElement["align_select"].AsBool;
            icon = jnElement["icon"] != null ? spriteAssets != null && spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null;
            rect = RectValues.TryParse(jnElement["rect"], RectValues.Default);
            textRect = RectValues.TryParse(jnElement["text_rect"], RectValues.FullAnchored);
            iconRect = RectValues.TryParse(jnElement["icon_rect"], RectValues.Default);
            rounded = jnElement["rounded"] == null ? 1 : jnElement["rounded"].AsInt; // roundness can be prevented by setting rounded to 0.
            roundedSide = jnElement["rounded_side"] == null ? SpriteHelper.RoundedSide.W : (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt; // default side should be Whole.
            mask = jnElement["mask"].AsBool;
            reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j);
            alignment = Parser.TryParse(jnElement["alignment"], TMPro.TextAlignmentOptions.Left);
            enableWordWrapping = jnElement["word_wrap"].AsBool;
            overflowMode = Parser.TryParse(jnElement["overflow_mode"], TMPro.TextOverflowModes.Masking);

            #endregion

            #region Color

            hideBG = jnElement["hide_bg"].AsBool;
            color = jnElement["col"].AsInt;
            opacity = jnElement["opacity"] == null ? 1f : jnElement["opacity"].AsFloat;
            hue = jnElement["hue"].AsFloat;
            sat = jnElement["sat"].AsFloat;
            val = jnElement["val"].AsFloat;
            textColor = jnElement["text_col"].AsInt;
            textHue = jnElement["text_hue"].AsFloat;
            textSat = jnElement["text_sat"].AsFloat;
            textVal = jnElement["text_val"].AsFloat;

            selectedColor = jnElement["sel_col"].AsInt;
            selectedOpacity = jnElement["sel_opacity"] == null ? 1f : jnElement["sel_opacity"].AsFloat;
            selectedHue = jnElement["sel_hue"].AsFloat;
            selectedSat = jnElement["sel_sat"].AsFloat;
            selectedVal = jnElement["sel_val"].AsFloat;
            selectedTextColor = jnElement["sel_text_col"].AsInt;
            selectedTextHue = jnElement["sel_text_hue"].AsFloat;
            selectedTextSat = jnElement["sel_text_sat"].AsFloat;
            selectedTextVal = jnElement["sel_text_val"].AsFloat;

            overrideColor = jnElement["override_col"] == null ? Color.white : LSColors.HexToColorAlpha(jnElement["override_col"]);
            overrideTextColor = jnElement["override_text_col"] == null ? Color.white : LSColors.HexToColorAlpha(jnElement["override_text_col"]);
            overrideSelectedColor = jnElement["override_sel_col"] == null ? Color.white : LSColors.HexToColorAlpha(jnElement["override_sel_col"]);
            overrideSelectedTextColor = jnElement["override_sel_text_col"] == null ? Color.white : LSColors.HexToColorAlpha(jnElement["override_sel_text_col"]);
            useOverrideColor = jnElement["override_col"] != null;
            useOverrideTextColor = jnElement["override_text_col"] != null;
            useOverrideSelectedColor = jnElement["override_sel_col"] != null;
            useOverrideSelectedTextColor = jnElement["override_sel_text_col"] != null;

            #endregion

            #region Anim

            length = jnElement["anim_length"].AsFloat;
            wait = jnElement["wait"] == null ? true : jnElement["wait"].AsBool;

            #endregion

            #region Func

            playBlipSound = jnElement["play_blip_sound"].AsBool;
            funcJSON = jnElement["func"]; // function to run when the element is clicked.
            spawnFuncJSON = jnElement["spawn_func"]; // function to run when the element spawns.
            enterFuncJSON = jnElement["enter_func"]; // function to run when the element is hovered over.
            exitFuncJSON = jnElement["exit_func"]; // function to run when the element is hovered over.
            allowOriginalHoverMethods = jnElement["allow_original_hover_func"].AsBool;

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
