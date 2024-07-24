using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using LSFunctions;

using SimpleJSON;

using BaseMarker = DataManager.GameData.BeatmapData.Marker;

namespace BetterLegacy.Core.Data
{
    public class PAAnimation : Exists
    {
        public PAAnimation()
        {
            id = LSText.randomNumString(16);
            objects = new List<AnimationObject>();
            markers = new List<BaseMarker>();
        }

        public PAAnimation(string name, string desc, float startTime, List<AnimationObject> objects, List<BaseMarker> markers)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.desc = desc;
            StartTime = startTime;
            this.objects = objects;
            this.markers = markers;
        }

        public string id;
        public string name = "Anim";
        public string desc = "This is the default description!";
        float startTime;
        public float StartTime { get => Mathf.Clamp(startTime, 0f, float.MaxValue); set => startTime = Mathf.Clamp(value, 0f, float.MaxValue); }
        public List<BaseMarker> markers;

        public List<AnimationObject> objects;

        public static PAAnimation Parse(JSONNode jn)
        {
            var markers = new List<BaseMarker>();
            for (int i = 0; i < jn["markers"].Count; i++)
                markers.Add(Marker.Parse(jn["markers"][i]));

            var list = new List<AnimationObject>();
            for (int i = 0; i < jn["objs"].Count; i++)
            {
                var animationObject = new AnimationObject();

                for (int j = 0; j < jn["objs"][i]["bins"].Count; j++)
                {
                    var animationBin = new AnimationBin();
                    animationBin.name = jn["objs"][i]["bins"][j]["name"];
                    var defaultValueCount = jn["objs"][i]["bins"][j]["defval"].AsInt;
                    for (int k = 0; k < jn["objs"][i]["bins"][j]["kf"].Count; k++)
                        animationBin.events.Add(EventKeyframe.Parse(jn["objs"][i]["bins"][j]["kf"][k], 0, defaultValueCount));
                }

                list.Add(animationObject);
            }

            return new PAAnimation(jn["name"], jn["desc"], jn["st"].AsFloat, list, markers)
            {
                id = jn["id"] ?? LSText.randomNumString(16),
            };
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["id"] = id;
            jn["name"] = name;
            jn["st"] = StartTime.ToString();
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];

                if (!string.IsNullOrEmpty(marker.name))
                    jn["markers"][i]["name"] = marker.name.ToString();

                if (!string.IsNullOrEmpty(marker.desc) && marker.desc != "Description")
                    jn["markers"][i]["desc"] = marker.desc.ToString();

                if (marker.color != 0)
                    jn["markers"][i]["col"] = marker.color.ToString();

                jn["markers"][i]["t"] = marker.time.ToString();
            }

            for (int i = 0; i < objects.Count; i++)
            {
                for (int j = 0; j < objects[i].animationBins.Count; j++)
                {
                    jn["objs"][i]["bins"][j]["name"] = objects[i].animationBins[j].name;
                    jn["objs"][i]["bins"][j]["defval"] = objects[i].animationBins[j].events[0].eventValues.Length;

                    for (int k = 0; k < objects[i].animationBins[j].events.Count; k++)
                        jn["objs"][i]["bins"][j]["kf"][k] = objects[i].animationBins[j].events[k].ToJSON();
                }
            }

            return jn;
        }

        public class AnimationObject
        {
            /// <summary>
            /// ID used for comparing objects with their associated animations.
            /// </summary>
            public string ReferenceID { get; set; }
            public List<AnimationBin> animationBins = new List<AnimationBin>();
        }

        public class AnimationBin
        {
            public string name;
            public List<EventKeyframe> events = new List<EventKeyframe>();
        }
    }
}
