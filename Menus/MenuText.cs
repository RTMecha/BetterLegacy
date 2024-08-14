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

namespace BetterLegacy.Menus
{
    public class MenuText : MonoBehaviour
    {
        public void Spawn()
        {
            isSpawning = true;
            textInterpolation = new RTAnimation("Text Interpolation");
            textInterpolation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(1f, text.Length, Ease.Linear),
                }, Interpolate),
            };
            textInterpolation.onComplete = () =>
            {
                AnimationManager.inst.RemoveID(textInterpolation.id);
                Interpolate(1f);
                isSpawning = false;
                textInterpolation = null;
            };
            AnimationManager.inst.Play(textInterpolation);
        }

        void Interpolate(float x)
        {
            var interpolation = CoreHelper.InterpolateString(text, x);
            if (interpolation.Length != textUI.text.Length)
                AudioManager.inst.PlaySound("Click");

            textUI.text = interpolation;
        }

        public RTAnimation textInterpolation;

        public Clickable clickable;
        public Image image;
        public string text;
        public TextMeshProUGUI textUI;

        public int color;

        public bool isSpawning;
    }
}
