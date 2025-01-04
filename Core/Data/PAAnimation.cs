using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using LSFunctions;

using SimpleJSON;

using BetterLegacy.Editor.Managers;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data
{
    // TODO: figure out how to get this working properly
    public class PAAnimation : Exists
    {
        public PAAnimation()
        {
            id = LSText.randomNumString(16);
            objects = new List<AnimationObject>();
            markers = new List<Marker>();
        }

        public PAAnimation(string name, string desc, float startTime, List<AnimationObject> objects, List<Marker> markers, AnimationReferenceType animationReferenceType = AnimationReferenceType.Player)
        {
            id = LSText.randomNumString(16);
            this.name = name;
            this.desc = desc;
            StartTime = startTime;
            this.objects = objects;
            this.markers = markers;
            this.animationReferenceType = animationReferenceType;
        }

        #region Fields

        /// <summary>
        /// ID of the animation.
        /// </summary>
        public string id;
        /// <summary>
        /// Name of the animation.
        /// </summary>
        public string name = "Anim";
        /// <summary>
        /// Description of the animation.
        /// </summary>
        public string desc = "This is the default description!";
        float startTime;
        /// <summary>
        /// Time the animation starts at.
        /// </summary>
        public float StartTime { get => Mathf.Clamp(startTime, 0f, float.MaxValue); set => startTime = Mathf.Clamp(value, 0f, float.MaxValue); }
        /// <summary>
        /// Markers to render in the <see cref="AnimationEditor"/>
        /// </summary>
        public List<Marker> markers;

        public List<AnimationObject> objects;

        /// <summary>
        /// What type of object should the animation be applied to.
        /// </summary>
        public AnimationReferenceType animationReferenceType;

        #endregion

        #region Methods

        public float GetLength(int current)
        {
            if (current >= objects.Count || current < 0)
                return 0f;

            var objectsLength = objects[current].animationBins.Max(x => x.events.Max(x => x.eventTime));
            var markersLength = markers.Max(x => x.time);

            return objectsLength > markersLength ? objectsLength : markersLength;
        }

        public static PAAnimation Parse(JSONNode jn)
        {
            var markers = new List<Marker>();
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
                    animationBin.transformType = Parser.TryParse(jn["objs"][i]["bins"][j]["type"], AnimationBin.TransformType.Null);
                    var defaultValueCount = jn["objs"][i]["bins"][j]["defval"].AsInt;
                    for (int k = 0; k < jn["objs"][i]["bins"][j]["kf"].Count; k++)
                        animationBin.events.Add(EventKeyframe.Parse(jn["objs"][i]["bins"][j]["kf"][k], 0, defaultValueCount));
                }

                list.Add(animationObject);
            }

            return new PAAnimation(jn["name"], jn["desc"], jn["st"].AsFloat, list, markers, Parser.TryParse(jn["ref_type"], AnimationReferenceType.Player))
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
            jn["ref_type"] = animationReferenceType.ToString();
            for (int i = 0; i < markers.Count; i++)
                jn["markers"][i] = markers[i].ToJSON();

            for (int i = 0; i < objects.Count; i++)
            {
                for (int j = 0; j < objects[i].animationBins.Count; j++)
                {
                    jn["objs"][i]["bins"][j]["name"] = objects[i].animationBins[j].name;
                    jn["objs"][i]["bins"][j]["type"] = objects[i].animationBins[j].transformType.ToString();
                    jn["objs"][i]["bins"][j]["defval"] = objects[i].animationBins[j].events[0].eventValues.Length;

                    for (int k = 0; k < objects[i].animationBins[j].events.Count; k++)
                        jn["objs"][i]["bins"][j]["kf"][k] = objects[i].animationBins[j].events[k].ToJSON();
                }
            }

            return jn;
        }

        public RTAnimation ToRTAnimation(object reference)
        {
            switch (animationReferenceType)
            {
                case AnimationReferenceType.Player:
                    {
                        if (reference is not CustomPlayer customPlayer || !customPlayer.Player)
                            break;

                        var animation = new RTAnimation($"PA Animation [ {name} ]");
                        animation.animationHandlers = new List<AnimationHandlerBase>();
                        for (int i = 0; i < objects.Count; i++)
                        {
                            var animationObject = objects[i];
                            switch (animationObject.ReferenceID)
                            {
                                case "HEAD":
                                    {
                                        for (int j = 0; j < animationObject.animationBins.Count; j++)
                                        {
                                            var animationBin = animationObject.animationBins[j];

                                            switch (animationBin.transformType)
                                            {
                                                case AnimationBin.TransformType.Position:
                                                    {
                                                        var sequence = Updater.levelProcessor.converter.GetVector3Sequence(animationBin.events.Select(x => x as DataManager.GameData.EventKeyframe).ToList(), new Vector3Keyframe(0f, Vector3.zero, Ease.Linear));
                                                        animation.animationHandlers.Add(new AnimationHandler<Vector3>(sequence, vector3 => customPlayer.Player.head.gameObject.transform.localPosition = vector3));

                                                        break;
                                                    }
                                                case AnimationBin.TransformType.Scale:
                                                    {
                                                        var sequence = Updater.levelProcessor.converter.GetVector2Sequence(animationBin.events.Select(x => x as DataManager.GameData.EventKeyframe).ToList(), new Vector2Keyframe(0f, Vector2.one, Ease.Linear));
                                                        animation.animationHandlers.Add(new AnimationHandler<Vector2>(sequence, vector2 => customPlayer.Player.head.gameObject.transform.localScale = new Vector3(vector2.x, vector2.y, 1f)));

                                                        break;
                                                    }
                                                case AnimationBin.TransformType.Rotation:
                                                    {
                                                        var sequence = Updater.levelProcessor.converter.GetFloatSequence(animationBin.events.Select(x => x as DataManager.GameData.EventKeyframe).ToList(), 0, new FloatKeyframe(0f, 0f, Ease.Linear), null, false);
                                                        animation.animationHandlers.Add(new AnimationHandler<float>(sequence, x => customPlayer.Player.head.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, x)));

                                                        break;
                                                    }
                                            }
                                        }

                                        break;
                                    }
                            }
                        }

                        return animation;
                    }
            }

            return null;
        }

        #endregion

        #region Sub Types

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
            public TransformType transformType;

            public enum TransformType
            {
                Null,
                Position,
                Scale,
                Rotation,
                Color
            }
        }

        /// <summary>
        /// What type of object should the animation be applied to.
        /// </summary>
        public enum AnimationReferenceType
        {
            /// <summary>
            /// Applies animation to players.
            /// </summary>
            Player,
            /// <summary>
            /// Applies animation to <see cref="BeatmapObject"/>.
            /// </summary>
            BeatmapObject,
            /// <summary>
            /// Applies animation to <see cref="BackgroundObject"/>.
            /// </summary>
            BackgroundObject,
            /// <summary>
            /// Applies animation to the game timeline.
            /// </summary>
            Timeline,
        }

        #endregion

        public static Dictionary<string, string[]> DefaultBinNames { get; set; } = new Dictionary<string, string[]>
        {
            { "Default", new string[] { "Position", "Scale", "Rotation" } }
        };
    }
}
