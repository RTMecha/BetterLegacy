using BetterLegacy.Core.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor
{
    public class ThemePanel
    {
        public List<Image> Colors { get; set; } = new List<Image>();

        public GameObject GameObject { get; set; }
        public Components.ContextClickable ContextClickable { get; set; }
        public Button UseButton { get; set; }
        public Button EditButton { get; set; }
        public Button DeleteButton { get; set; }
        public Image BaseImage { get; set; }

        public Text Name { get; set; }

        public void SetActive(bool active) => GameObject?.SetActive(active);

        public BeatmapTheme Theme { get; set; }

        public string FilePath { get; set; }

        public string OriginalID { get; set; }

        public bool isDuplicate;

        public bool isFolder;
    }
}
