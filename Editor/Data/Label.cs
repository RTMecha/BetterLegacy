using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles information for a set of editor labels.
    /// </summary>
    public class Label
    {
        public Label(string text) => this.text = text;

        public Label(string text, Vector2 sizeDelta) : this(text) => this.sizeDelta = sizeDelta;

        public Label(string text, int fontSize) : this(text) => this.fontSize = fontSize;

        public Label(string text, int fontSize, FontStyle fontStyle) : this(text, fontSize) => this.fontStyle = fontStyle;

        public Label(string text, int fontSize, FontStyle fontStyle, TextAnchor alignment) : this(text, fontSize, fontStyle) => this.alignment = alignment;

        public Label(string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 sizeDelta) : this(text, fontSize, fontStyle, alignment) => this.sizeDelta = sizeDelta;

        public TextAnchor alignment = TextAnchor.MiddleLeft;
        public string text;
        public int fontSize = 20;
        public FontStyle fontStyle = FontStyle.Normal;
        public HorizontalWrapMode horizontalWrap = HorizontalWrapMode.Wrap;
        public VerticalWrapMode verticalWrap = VerticalWrapMode.Overflow;

        public Vector2? sizeDelta;

        public void Apply(Text text)
        {
            text.text = this;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = horizontalWrap;
            text.verticalOverflow = verticalWrap;

            if (sizeDelta != null && sizeDelta.HasValue)
                text.rectTransform.sizeDelta = sizeDelta.Value;
        }

        public static implicit operator string(Label labelSettings) => labelSettings.text;
        public static implicit operator Label(string text) => new Label(text);
    }
}
