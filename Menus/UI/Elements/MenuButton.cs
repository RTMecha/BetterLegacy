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

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling button elements in the interface. Based on <see cref="MenuText"/>.
    /// </summary>
    public class MenuButton : MenuText
    {
        /// <summary>
        /// True if the element is hovered, otherwise false.
        /// </summary>
        public bool isHovered;

        /// <summary>
        /// To be used for where the current selection is. If it's at 0, 0 then it's the default selection. This is compared against <see cref="MenuBase.selected"/> to see if it is selected.
        /// </summary>
        public Vector2Int selectionPosition;


        RTAnimation enterAnimation;
        RTAnimation exitAnimation;

        /// <summary>
        /// Opacity of the image when the element is selected.
        /// </summary>
        public float selectedOpacity;

        /// <summary>
        /// Color of the image when the element is selected.
        /// </summary>
        public int selectedColor;

        /// <summary>
        /// Color of the text when the element is selected.
        /// </summary>
        public int selectedTextColor;

        /// <summary>
        /// Plays the Enter animation when the element is selected. Currently is not customizable.
        /// </summary>
        public void OnEnter()
        {
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
        /// Plays the Exit animation when the element is no longer selected. Currently is not customizable.
        /// </summary>
        public void OnExit()
        {
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
    }
}
