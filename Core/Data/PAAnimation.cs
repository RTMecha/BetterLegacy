using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using LSFunctions;

using Marker = DataManager.GameData.BeatmapData.Marker;

namespace BetterLegacy.Core.Data
{
    public class PAAnimation : Exists
    {
        public PAAnimation()
        {
            id = LSText.randomNumString(16);
            eventKeyframes = new List<List<EventKeyframe>>();
            markers = new List<Marker>();
        }

        public PAAnimation(string name, string desc, float startTime, List<List<EventKeyframe>> eventKeyframes, string[] binNames, List<Marker> markers)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.desc = desc;
            StartTime = startTime;
            this.eventKeyframes = eventKeyframes;
            this.markers = markers;
            this.binNames = binNames;
        }

        public string id;
        public string name = "Anim";
        public string desc = "This is the default description!";
        float startTime;
        public float StartTime { get => Mathf.Clamp(startTime, 0f, float.MaxValue); set => startTime = Mathf.Clamp(value, 0f, float.MaxValue); }
        public List<List<EventKeyframe>> eventKeyframes;
        public List<Marker> markers;
        public string[] binNames;
    }
}
