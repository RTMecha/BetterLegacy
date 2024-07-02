using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using LSFunctions;

using SimpleJSON;

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

        public static PAAnimation Parse(JSONNode jn)
        {
            var markers = new List<Marker>();
            for (int i = 0; i < jn["markers"].Count; i++)
            {
                markers.Add(ProjectData.Reader.ParseMarker(jn["marker"][i]));
            }

            var list = new List<List<EventKeyframe>>();
            string[] binNames = new string[jn["events"].Count];
            for (int i = 0; i < jn["events"].Count; i++)
            {
                list.Add(new List<EventKeyframe>());

                binNames[i] = jn["events"][i]["bin"];
                var defaultValueCount = jn["events"][i]["defval"].AsInt;

                for (int j = 0; j < jn["events"][i]["kf"].Count; j++)
                    list[i].Add(EventKeyframe.Parse(jn["events"][i]["kf"][j], 0, defaultValueCount));
            }

            return new PAAnimation(jn["name"], jn["desc"], jn["st"].AsFloat, list, binNames, markers)
            {
                id = jn["id"] ?? LSText.randomNumString(16),
            };
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["id"] = id;
            for (int i = 0; i < markers.Count; i++)
            {
                
            }

            return jn;
        }
    }
}
