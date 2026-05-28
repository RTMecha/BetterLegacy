using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Menus;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents an animatable text element.
    /// </summary>
    public class QuickElement : PAObject<QuickElement>
    {
        #region Constructors

        public QuickElement() : base() { }

        public QuickElement(string name) : this() => this.name = name;

        public QuickElement(global::QuickElement quickElement) : this(quickElement.name)
        {
            if (quickElement.keyframes != null)
                quickElement.keyframes.ForLoop(keyframe => keyframes.Add(new TextKeyframe(keyframe)));
            if (quickElement.effects != null && quickElement.effects.Has(x => x.name == "loop"))
                loop = true;
        }

        #endregion

        #region Values

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

        #endregion

        #region Functions

        public override void CopyData(QuickElement orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name;
            loop = orig.loop;
            keyframes = new List<TextKeyframe>(orig.keyframes.Select(x => x.Copy(false)));
        }

        public override void ReadJSON(JSONNode jn)
        {
            name = jn["name"];
            if (jn["effects"] != null)
                loop = true;
            else
                loop = jn["loop"].AsBool;

            for (int i = 0; i < jn["keys"].Count; i++)
                keyframes.Add(TextKeyframe.Parse(jn["keys"][i]));
        }

        public override JSONNode ToJSON() => base.ToJSON();

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

        #endregion

    }

    /// <summary>
    /// Represents a text keyframe.
    /// </summary>
    public class TextKeyframe : PAObject<TextKeyframe>
    {
        #region Constructors

        public TextKeyframe() : base() { }

        public TextKeyframe(string text, float time) : this()
        {
            this.text = text;
            this.time = time;
        }

        public TextKeyframe(global::QuickElement.Keyframe keyframe) : this(keyframe.text, keyframe.time) { }

        #endregion

        #region Values

        /// <summary>
        /// Text to display.
        /// </summary>
        public string text;

        /// <summary>
        /// Time of the text keyframe.
        /// </summary>
        public float time;

        /// <summary>
        /// Text keyframe function.
        /// </summary>
        public Action func;

        /// <summary>
        /// Text keyframe function.
        /// </summary>
        public JSONNode funcJSON;

        #endregion

        #region Functions

        public override void CopyData(TextKeyframe orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            text = orig.text;
            time = orig.time;
            funcJSON = orig.funcJSON;
            func = orig.func;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            text = jn["text"];
            time = jn["time"];

            funcJSON = jn["func"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["text"] = text ?? string.Empty;
            jn["time"] = time;

            if (funcJSON != null)
                jn["func"] = funcJSON;

            return jn;
        }

        /// <summary>
        /// Runs the text keyframe function.
        /// </summary>
        public void Run()
        {
            func?.Invoke();
            if (funcJSON != null)
                InterfaceManager.inst.ParseFunction(funcJSON);
        }

        #endregion
    }
}
