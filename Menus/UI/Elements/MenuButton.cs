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

        #endregion

        #region Private Fields

        RTAnimation enterAnimation;
        RTAnimation exitAnimation;

        #endregion

        #region Methods

        /// <summary>
        /// Plays the Enter animation when the element is selected.
        /// </summary>
        public void OnEnter()
        {
            if (enterFuncJSON != null)
            {
                ParseFunction(enterFuncJSON);
                return;
            }

            if (enterFunc != null)
            {
                enterFunc();
                return;
            }

            if (exitAnimation != null)
            {
                AnimationManager.inst.RemoveID(exitAnimation.id);
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
                AnimationManager.inst.RemoveID(enterAnimation.id);
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
                return;
            }

            if (exitFunc != null)
            {
                exitFunc();
                return;
            }

            if (enterAnimation != null)
            {
                AnimationManager.inst.RemoveID(enterAnimation.id);
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
            exitAnimation.onComplete = () => { AnimationManager.inst.RemoveID(exitAnimation.id); };
            AnimationManager.inst.Play(exitAnimation);
        }

        public static MenuButton DeepCopy(MenuButton orig, bool newID = true, bool newSelectionPosition = false, Vector2Int selectionPosition = default)
        {
            return new MenuButton
            {
                id = newID ? LSText.randomNumString(16) : orig.id,
                name = orig.name,
                parentLayout = orig.parentLayout,
                parent = orig.parent,
                siblingIndex = orig.siblingIndex,
                icon = orig.icon,
                rectJSON = orig.rectJSON,
                color = orig.color,
                opacity = orig.opacity,
                hue = orig.hue,
                sat = orig.sat,
                val = orig.val,
                length = orig.length,
                playBlipSound = orig.playBlipSound,
                rounded = orig.rounded, // roundness can be prevented by setting rounded to 0.
                roundedSide = orig.roundedSide, // default side should be Whole.
                funcJSON = orig.funcJSON, // function to run when the element is clicked.
                spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
                reactiveSetting = orig.reactiveSetting,
                fromLoop = false, // if element has been spawned from the loop or if its the first / only of its kind.
                loop = orig.loop,
                func = orig.func,
                spawnFunc = orig.spawnFunc,
                text = orig.text,
                hideBG = orig.hideBG,
                iconRectJSON = orig.iconRectJSON,
                textRectJSON = orig.textRectJSON,
                textColor = orig.textColor,
                textHue = orig.textHue,
                textSat = orig.textSat,
                textVal = orig.textVal,

                selectionPosition = newSelectionPosition ? selectionPosition : orig.selectionPosition,
                autoAlignSelectionPosition = orig.autoAlignSelectionPosition,
                enterFunc = orig.enterFunc,
                exitFunc = orig.exitFunc,
                enterFuncJSON = orig.enterFuncJSON,
                exitFuncJSON = orig.exitFuncJSON,
                selectedColor = orig.selectedColor,
                selectedOpacity = orig.selectedOpacity,
                selectedHue = orig.selectedHue,
                selectedSat = orig.selectedSat,
                selectedVal = orig.selectedVal,
                selectedTextColor = orig.selectedTextColor,
                selectedTextHue = orig.selectedTextHue,
                selectedTextSat = orig.selectedTextSat,
                selectedTextVal = orig.selectedTextVal
            };
        }

        #endregion
    }
}
