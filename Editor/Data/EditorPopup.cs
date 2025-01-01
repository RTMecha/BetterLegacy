using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    public class Popup
    {
        public string Name { get; set; }
        public GameObject GameObject { get; set; }
        public Button Close { get; set; }
        public InputField SearchField { get; set; }
        public Transform Content { get; set; }
        public GridLayoutGroup Grid { get; set; }
        public RectTransform TopPanel { get; set; }
    }

}
