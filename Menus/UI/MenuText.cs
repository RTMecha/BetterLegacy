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

namespace BetterLegacy.Menus.UI
{
    public class MenuText
    {
        public void Spawn()
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
                    new FloatKeyframe(length / (text.Length / 48f), text.Length, Ease.Linear),
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

        void Interpolate(float x)
        {
            var val = (int)x;

            if (textUI.maxVisibleCharacters != val)
                AudioManager.inst.PlaySound("Click");

            textUI.maxVisibleCharacters = val;
        }

        public GameObject gameObject;

        public string parentLayout;

        public float length = 1f;

        public bool playBlipSound;

        public string name;
        public Vector2 pos;
        public Vector2 size;

        public Sprite icon;
        public Image iconUI;

        public bool hideBG;

        public JSONNode iconRectJSON;
        public JSONNode textRectJSON;
        public JSONNode rectJSON;
        public JSONNode funcJSON;

        public RTAnimation textInterpolation;

        public Clickable clickable;
        public Image image;
        public string text = "";
        public TextMeshProUGUI textUI;

        public int color;
        public int textColor;
        public int selectedColor;
        public int selectedTextColor;

        public bool isSpawning;

        public void ParseFunction(JSONNode jn)
        {
            var parameters = jn["params"];

            string name = jn["name"];
            switch (name)
            {
                case "LoadScene":
                    {
                        if (parameters == null)
                            break;

                        if (parameters.Count >= 2)
                            SceneManager.inst.LoadScene(parameters[0], Parser.TryParse(parameters[1], true));
                        else if (parameters.Count >= 1)
                            SceneManager.inst.LoadScene(parameters[0]);

                        break;
                    }
                case "Log":
                    {
                        if (parameters != null && parameters.Count >= 1)
                            CoreHelper.Log(parameters[0]);

                        break;
                    }
                case "ExitGame":
                    {
                        Application.Quit();
                        break;
                    }
                case "Config":
                    {
                        ConfigManager.inst.Show();
                        break;
                    }
                case "SetCurrentInterface":
                    {
                        if (parameters != null && parameters.Count >= 1)
                            NewMenuManager.inst.SetCurrentInterface(parameters[0]);

                        break;
                    }
            }
        }
    }
}
