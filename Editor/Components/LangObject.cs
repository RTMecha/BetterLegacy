using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component used for setting a lang key onto a text component.
    /// </summary>
    public class LangObject : MonoBehaviour
    {
        #region Values

        /// <summary>
        /// Lang key or display text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Default text to display.
        /// </summary>
        public string DefaultText { get; set; }

        /// <summary>
        /// Text to display.
        /// </summary>
        public string DisplayText => Lang.Current.GetOrDefault(Text, DefaultText ?? Text);

        Text textComponent;

        /// <summary>
        /// Text component that the display text is set to.
        /// </summary>
        public Text TextComponent
        {
            get => textComponent;
            set => textComponent = value;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes a <see cref="LangObject"/> onto the <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Unity Game Object reference.</param>
        /// <param name="text">Lang key or display text.</param>
        public static void Init(GameObject gameObject, string text, string defaultText = null)
        {
            if (gameObject)
                Init(gameObject.GetComponent<Text>(), text, defaultText);
        }

        /// <summary>
        /// Initializes a <see cref="LangObject"/> onto the <paramref name="textComponent"/>.
        /// </summary>
        /// <param name="textComponent">Text component.</param>
        /// <param name="text">Lang key or display text.</param>
        public static void Init(Text textComponent, string text, string defaultText = null)
        {
            if (!textComponent)
                return;

            var langObject = textComponent.gameObject.AddComponent<LangObject>();
            langObject.textComponent = textComponent;
            langObject.Text = text;
            langObject.DefaultText = defaultText;
        }

        void OnEnable() => UpdateDisplayText();

        /// <summary>
        /// Updates the display text.
        /// </summary>
        public void UpdateDisplayText()
        {
            if (!textComponent)
                textComponent = GetComponent<Text>();

            if (textComponent)
                textComponent.text = DisplayText;
        }

        /// <summary>
        /// Renders all lang objects.
        /// </summary>
        public static void RenderLangObjects()
        {
            var objs = Resources.FindObjectsOfTypeAll<LangObject>();
            for (int i = 0; i < objs.Length; i++)
                objs[i].UpdateDisplayText();
        }

        #endregion
    }
}
