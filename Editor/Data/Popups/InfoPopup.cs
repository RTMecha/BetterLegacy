using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Popups
{
    /// <summary>
    /// Represents a popup that displays loading information.
    /// </summary>
    public class InfoPopup : EditorPopup
    {
        public InfoPopup(string name) : base(name) { }

        /// <summary>
        /// Text that displays the current state of level loading.
        /// </summary>
        public Text Info { get; set; }

        /// <summary>
        /// Dog that bops.
        /// </summary>
        public Image Doggo { get; set; }

        public override void Assign(GameObject popup)
        {
            GameObject = popup;
            Title = popup.transform.Find("title").GetComponent<Text>();
            Info = popup.transform.Find("text").GetComponent<Text>();
        }

        /// <summary>
        /// Sets the currently displayed info.
        /// </summary>
        /// <param name="text">Text to set.</param>
        public void SetInfo(string text)
        {
            if (Info)
                Info.text = text;
        }
    }
}
