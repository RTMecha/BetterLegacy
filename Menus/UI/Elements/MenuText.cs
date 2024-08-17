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
using BetterLegacy.Core.Managers;

using TMPro;
using SimpleJSON;
using BetterLegacy.Core;
using BetterLegacy.Configs;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling text elements in the interface. Based on <see cref="MenuImage"/>.
    /// </summary>
    public class MenuText : MenuImage
    {
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
                    new FloatKeyframe(length * (text.Length / 64f), textWithoutFormatting.Length, Ease.Linear),
                                             // ^ Regular spawn length is sometimes too slow for text interpolation so it's sped up relative to the text length. 
                                             // For example, if the text is 32 characters long, it'd be fine. But if it were just 3 letters, it'd be really slow looking.
                }, Interpolate),
            };
            textInterpolation.onComplete = () =>
            {
                AnimationManager.inst.RemoveID(textInterpolation.id);
                Interpolate(textWithoutFormatting.Length);
                isSpawning = false;
                textInterpolation = null;
            };
            AnimationManager.inst.Play(textInterpolation);
        }

        public static float textLengthDivision = 126f;

        public override void UpdateSpawnCondition()
        {
            // Speeds up the text interpolation if a Submit key is being held.
            textInterpolation?.animationHandlers[0]?.SetKeyframeTime(1, InputDataManager.inst.menuActions.Submit.IsPressed ? length * (text.Length / textLengthDivision) * 0.3f : length * (text.Length / textLengthDivision));
        }

        /// <summary>
        /// Sets <see cref="TMP_Text.maxVisibleCharacters"/> so we can have a typewriter effect. Also plays a sound every time a character is placed.
        /// </summary>
        /// <param name="x"></param>
        void Interpolate(float x)
        {
            var val = (int)x;

            if (textUI.maxVisibleCharacters != val)
                AudioManager.inst.PlaySound("Click"); // TODO: Maybe look into custom sound fonts?

            textUI.maxVisibleCharacters = val;
        }

        /// <summary>
        /// Updates <see cref="textUI"/>s' formatting, so it can utilize QuickElements.
        /// </summary>
        public void UpdateText()
        {
            time = Time.time - timeOffset * (InputDataManager.inst.menuActions.Submit.IsPressed ? length * 0.3f : length);

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

        /// <summary>
        /// A sub-image that spawns if <see cref="MenuImage.icon"/> exists.
        /// </summary>
        public Image iconUI;

        /// <summary>
        /// If the background shouldn't display.
        /// </summary>
        public bool hideBG;

        /// <summary>
        /// RectTransform values for the icon in a JSON format. If left null, the RectTransform values will be set to their defaults.
        /// </summary>
        public JSONNode iconRectJSON;

        /// <summary>
        /// RectTransform values for the text in a JSON format. If left null, the RectTransform values will be set to their defaults.
        /// </summary>
        public JSONNode textRectJSON;

        /// <summary>
        /// The animation of the text typewriter effect.
        /// </summary>
        public RTAnimation textInterpolation;

        /// <summary>
        /// Theme color slot for the text to use.
        /// </summary>
        public int textColor;

        /// <summary>
        /// The elements' text.
        /// </summary>
        public string text = "";

        /// <summary>
        /// The text component of the element.
        /// </summary>
        public TextMeshProUGUI textUI;

        #region Private Fields
        
        string textWithoutFormatting;

        List<Tuple<float, Match, QuickElement>> cachedQuickElements;

        #endregion
    }
}
