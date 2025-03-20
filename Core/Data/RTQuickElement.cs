using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Menus;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents an animatable text element.
    /// </summary>
    public class RTQuickElement : Exists
    {
        public RTQuickElement(string name) => this.name = name;

        public RTQuickElement(QuickElement quickElement)
        {
            name = quickElement.name;
            if (quickElement.keyframes != null)
                quickElement.keyframes.ForLoop(keyframe => keyframes.Add(new TextKeyframe(keyframe)));
            if (quickElement.effects != null && quickElement.effects.Has(x => x.name == "loop"))
                loop = true;
        }

        /// <summary>
        /// Name to use when searching for the text animation.
        /// </summary>
        public string name;

        /// <summary>
        /// If the text animation should loop.
        /// </summary>
        public bool loop;

        /// <summary>
        /// List of text keyframes.
        /// </summary>
        public List<TextKeyframe> keyframes = new List<TextKeyframe>();

        int activeIndex;

        void SetActive(int index)
        {
            if (index != activeIndex && index >= 0 && index < keyframes.Count)
            {
                activeIndex = index;
                keyframes[index].Run();
            }
        }

        /// <summary>
        /// Interpolates the text animation.
        /// </summary>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns the specific frame of the QuickElement.</returns>
        public string Interpolate(float t)
        {
            if (keyframes == null || keyframes.IsEmpty())
                return string.Empty;

            var index = keyframes.FindIndex(x => x.time > (loop ? t % 0f : t)) - 1;
            if (t < 0f)
                index = 0;

            SetActive(index);

            return index >= 0 && index < keyframes.Count ? keyframes[index].text : keyframes.Count > 0 ? keyframes[keyframes.Count - 1].text : string.Empty;
        }

        public static RTQuickElement Parse(JSONNode jn)
        {
            var quickElement = new RTQuickElement(jn["name"]);

            if (jn["effects"] != null)
                quickElement.loop = true;

            for (int i = 0; i < jn["keys"].Count; i++)
                quickElement.keyframes.Add(TextKeyframe.Parse(jn["keys"][i]));

            return quickElement;
        }
    }

    public class TextKeyframe : Exists
    {
        public TextKeyframe(string text, float time)
        {
            this.text = text;
            this.time = time;
        }

        public TextKeyframe(QuickElement.Keyframe keyframe) : this(keyframe.text, keyframe.time) { }

        public string text;
        public float time;
        public Action func;
        public JSONNode funcJSON;

        public void Run()
        {
            func?.Invoke();
            if (funcJSON != null)
                InterfaceManager.inst.ParseFunction(funcJSON);
        }

        public static TextKeyframe Parse(JSONNode jn)
        {
            var textKeyframe = new TextKeyframe(jn["text"], jn["time"].AsFloat);
            textKeyframe.funcJSON = jn["func"];

            return textKeyframe;
        }
    }
}
