using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
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

        /// <summary>
        /// Parses a <see cref="MenuButton"/> interface element from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to parse.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns a parsed interface element.</returns>
        public static new MenuButton Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            var element = new MenuButton();
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

            #region Color

            var jnSelCol = InterfaceManager.inst.ParseVarFunction(jnElement["sel_col"], this, customVariables);
            if (jnSelCol != null)
                selectedColor = jnSelCol.AsInt;
            var jnSelOpacity = InterfaceManager.inst.ParseVarFunction(jnElement["sel_opacity"], this, customVariables);
            if (jnSelOpacity != null)
                selectedOpacity = jnSelOpacity.AsFloat;
            var selHue = InterfaceManager.inst.ParseVarFunction(jnElement["sel_hue"], this, customVariables);
            if (selHue != null)
                selectedHue = selHue.AsFloat;
            var selSat = InterfaceManager.inst.ParseVarFunction(jnElement["sel_sat"], this, customVariables);
            if (selSat != null)
                selectedSat = selSat.AsFloat;
            var selVal = InterfaceManager.inst.ParseVarFunction(jnElement["sel_val"], this, customVariables);
            if (selVal != null)
                selectedVal = selVal.AsFloat;
            var selTextCol = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_col"], this, customVariables);
            if (selTextCol != null)
                selectedTextColor = selTextCol.AsInt;
            var selTextHue = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_hue"], this, customVariables);
            if (selTextHue != null)
                selectedTextHue = selTextHue.AsFloat;
            var selTextSat = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_sat"], this, customVariables);
            if (selTextSat != null)
                selectedTextSat = selTextSat.AsFloat;
            var selTextVal = InterfaceManager.inst.ParseVarFunction(jnElement["sel_text_val"], this, customVariables);
            if (selTextVal != null)
                selectedTextVal = selTextVal.AsFloat;

            var jnOverrideSelCol = InterfaceManager.inst.ParseVarFunction(jnElement["override_sel_col"], this, customVariables);
            var jnOverrideSelTextCol = InterfaceManager.inst.ParseVarFunction(jnElement["override_sel_text_col"], this, customVariables);
            useOverrideSelectedColor = jnOverrideSelCol != null;
            useOverrideSelectedTextColor = jnOverrideSelTextCol != null;
            if (useOverrideSelectedColor)
                overrideSelectedColor = RTColors.HexToColor(jnOverrideSelCol);
            if (useOverrideSelectedTextColor)
                overrideSelectedTextColor = RTColors.HexToColor(jnOverrideSelTextCol);

            #endregion

            #region Func

            var jnSelect = InterfaceManager.inst.ParseVarFunction(jnElement["select"], this, customVariables);
            if (jnSelect != null)
                selectionPosition = CustomMenu.ParseVector2Int(jnSelect, Vector2Int.zero, this, customVariables);
            var jnAlignSelect = InterfaceManager.inst.ParseVarFunction(jnElement["align_select"], this, customVariables);
            if (jnElement["align_select"] != null)
                autoAlignSelectionPosition = jnAlignSelect.AsBool;

            var jnEnterFunc = InterfaceManager.inst.ParseVarFunction(jnElement["enter_func"], this, customVariables);
            if (jnEnterFunc != null)
                enterFuncJSON = jnEnterFunc; // function to run when the element is hovered over.
            var jnExitFunc = InterfaceManager.inst.ParseVarFunction(jnElement["exit_func"], this, customVariables);
            if (jnExitFunc != null)
                exitFuncJSON = jnExitFunc; // function to run when the element is hovered over.
            var jnAllowOriginalHoverFunc = InterfaceManager.inst.ParseVarFunction(jnElement["allow_original_hover_func"], this, customVariables);
            if (jnAllowOriginalHoverFunc != null)
                allowOriginalHoverMethods = jnAllowOriginalHoverFunc.AsBool;
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
