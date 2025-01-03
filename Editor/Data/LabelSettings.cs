using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles information for a set of editor labels.
    /// </summary>
    public class LabelSettings
    {
        public LabelSettings(string text) => this.text = text;

        public LabelSettings(string text, int fontSize) : this(text) => this.fontSize = fontSize;

        public LabelSettings(string text, int fontSize, FontStyle fontStyle) : this(text, fontSize) => this.fontStyle = fontStyle;

        public LabelSettings(string text, int fontSize, FontStyle fontStyle, TextAnchor alignment) : this(text, fontSize, fontStyle) => this.alignment = alignment;

        public TextAnchor alignment = TextAnchor.MiddleLeft;
        public string text;
        public int fontSize = 20;
        public FontStyle fontStyle = FontStyle.Normal;
        public HorizontalWrapMode horizontalWrap = HorizontalWrapMode.Overflow;
        public VerticalWrapMode verticalWrap = VerticalWrapMode.Truncate;

        public void Apply(Text text)
        {
            text.text = this;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = horizontalWrap;
            text.verticalOverflow = verticalWrap;
        }

        public static implicit operator string(LabelSettings labelSettings) => labelSettings.text;
    }
}
