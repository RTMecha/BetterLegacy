using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor
{
    public class TimelineMarker : Exists
    {
        /// <summary>
        /// Index of the marker.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The marker data.
        /// </summary>
        public Marker Marker { get; set; }

        /// <summary>
        /// The GameObject of the Marker.
        /// </summary>
        public GameObject GameObject { get; set; }
        public RectTransform RectTransform { get; set; }

        public Text Text { get; set; }
        public Image Handle { get; set; }
        public Image Line { get; set; }

        public bool dragging;
    }
}
