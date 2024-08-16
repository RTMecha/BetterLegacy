using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;

using TMPro;
using SimpleJSON;
using BetterLegacy.Core;
using BetterLegacy.Configs;

namespace BetterLegacy.Menus.UI.Elements
{
    public class MenuText : MenuImage
    {
        public override void Spawn()
        {
            var text = this.text;
            var matches = Regex.Matches(text, "<(.*?)>");
            foreach (var obj in matches)
            {
                var match = (Match)obj;
                text = text.Replace(match.Groups[0].ToString(), "");
            }

            isSpawning = true;
            textInterpolation = new RTAnimation("Text Interpolation");
            textInterpolation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(length * (text.Length / 64f), text.Length, Ease.Linear),
                }, Interpolate),
            };
            textInterpolation.onComplete = () =>
            {
                AnimationManager.inst.RemoveID(textInterpolation.id);
                Interpolate(text.Length);
                isSpawning = false;
                textInterpolation = null;
            };
            AnimationManager.inst.Play(textInterpolation);
        }

        public static float textLengthDivision = 126f;

        public override void Update()
        {
            textInterpolation?.animationHandlers[0]?.SetKeyframeTime(1, InputDataManager.inst.menuActions.Submit.IsPressed ? length * (text.Length / textLengthDivision) * 0.3f : length * (text.Length / textLengthDivision));
        }

        void Interpolate(float x)
        {
            var val = (int)x;

            if (textUI.maxVisibleCharacters != val)
                AudioManager.inst.PlaySound("Click");

            textUI.maxVisibleCharacters = val;
        }

        public Image iconUI;

        public bool hideBG;

        public JSONNode iconRectJSON;
        public JSONNode textRectJSON;

        public RTAnimation textInterpolation;

        public int textColor;
        public string text = "";
        public TextMeshProUGUI textUI;
    }
}
