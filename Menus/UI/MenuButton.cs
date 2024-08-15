using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;

using TMPro;

namespace BetterLegacy.Menus.UI
{
    public class MenuButton : MenuText
    {
        public bool isHovered;

        public Vector2Int selectionPosition;
        public RTAnimation enterAnimation;
        public RTAnimation exitAnimation;

        public float selectedOpacity;
        public int selectedColor;
        public int selectedTextColor;

        public void OnEnter()
        {
            if (exitAnimation != null)
            {
                AnimationManager.inst.RemoveID(exitAnimation.id);
                exitAnimation = null;
            }

            enterAnimation = new RTAnimation("Enter Animation");
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

        public void OnExit()
        {
            if (enterAnimation != null)
            {
                AnimationManager.inst.RemoveID(enterAnimation.id);
                enterAnimation = null;
            }

            exitAnimation = new RTAnimation("Exit Animation");
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
