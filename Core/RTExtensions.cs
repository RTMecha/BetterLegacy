using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;
using Object = UnityEngine.Object;

namespace BetterLegacy.Core
{
    public static class RTExtensions
    {
        #region Scene

        public static bool TryFind(string find, out GameObject result)
        {
            var e = GameObject.Find(find);
            result = e;
            return e != null;
        }

        public static bool TryFind(this Transform tf, string find, out Transform result)
        {
            var e = tf.Find(find);
            result = e;
            return e != null;
        }

        public static List<Transform> ChildList(this Transform transform)
        {
            var list = new List<Transform>();
            foreach (var obj in transform)
                list.Add((Transform)obj);
            return list;
        }

        public static void DeleteChildren(this Transform tf, bool instant = false) => LSHelpers.DeleteChildren(tf, instant);

        public static GameObject Duplicate(this GameObject gameObject, Transform parent)
        {
            var copy = Object.Instantiate(gameObject);
            copy.transform.SetParent(parent);
            copy.transform.localPosition = gameObject.transform.localPosition;
            copy.transform.localScale = gameObject.transform.localScale;

            return copy;
        }

        public static GameObject Duplicate(this GameObject gameObject, Transform parent, string name)
        {
            var copy = gameObject.Duplicate(parent);
            copy.name = name;
            return copy;
        }

        public static GameObject Duplicate(this GameObject gameObject, Transform parent, string name, int index)
        {
            var copy = gameObject.Duplicate(parent);
            copy.name = name;
            copy.transform.SetSiblingIndex(index);
            return copy;
        }

        public static RectTransform AsRT(this Transform transform) => (RectTransform)transform;

        public static void SetPositionX(this Transform transform, float x)
        {
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        public static void SetPositionY(this Transform transform, float y)
        {
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        public static void SetPositionZ(this Transform transform, float z)
        {
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        public static void SetLocalPositionX(this Transform transform, float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }

        public static void SetLocalPositionY(this Transform transform, float y)
        {
            var pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }

        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            var pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }

        public static void SetLocalRotationEulerZ(this Transform transform, float z)
        {
            var rot = transform.localRotation.eulerAngles;
            rot.z = z;
            transform.localRotation = Quaternion.Euler(rot);
        }

        #endregion

        #region Data

        /// <summary>
        /// Gets the entire parent chain, including the beatmap object itself.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>List of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public static List<BaseBeatmapObject> GetParentChain(this BaseBeatmapObject beatmapObject)
        {
            var beatmapObjects = new List<BaseBeatmapObject>();

            if (beatmapObject != null)
            {
                var orig = beatmapObject;
                beatmapObjects.Add(orig);

                while (!string.IsNullOrEmpty(orig.parent))
                {
                    if (orig == null || DataManager.inst.gameData.beatmapObjects.Find(x => x.id == orig.parent) == null)
                        break;
                    var select = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == orig.parent);
                    beatmapObjects.Add(select);
                    orig = select;
                }
            }

            return beatmapObjects;
        }

        public static List<BaseBeatmapObject> GetParentChainSimple(this BaseBeatmapObject beatmapObject)
        {
            var beatmapObjects = new List<BaseBeatmapObject>();

            var orig = beatmapObject;
            beatmapObjects.Add(orig);

            while (!string.IsNullOrEmpty(orig.parent))
            {
                orig = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == orig.parent);
                beatmapObjects.Add(orig);
            }

            return beatmapObjects;
        }

        /// <summary>
        /// Gets the every child connected to the beatmap object.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>A full list tree with every child object.</returns>
        public static List<List<BaseBeatmapObject>> GetChildChain(this BaseBeatmapObject beatmapObject)
        {
            var lists = new List<List<BaseBeatmapObject>>();
            for (int i = 0; i < DataManager.inst.gameData.beatmapObjects.Count; i++)
            {
                var parentChain = DataManager.inst.gameData.beatmapObjects[i].GetParentChain();

                if (parentChain == null || parentChain.Count < 1)
                    continue;

                if (parentChain.Has(x => x.id == beatmapObject.id))
                    lists.Add(parentChain);

                //foreach (var parent in parentChain)
                //{
                //	if (parent.id == beatmapObject.id)
                //	{
                //		lists.Add(parentChain);
                //	}
                //}
            }

            return lists;
        }

        /// <summary>
        /// Checks whether the current time is within the objects' lifespan / if the object is alive.
        /// </summary>
        /// <returns>If alive returns true, otherwise returns false.</returns>
        public static bool TimeWithinLifespan(this BaseBeatmapObject beatmapObject)
        {
            var time = AudioManager.inst.CurrentAudioSource.time;
            var st = beatmapObject.StartTime;
            var akt = beatmapObject.autoKillType;
            var ako = beatmapObject.autoKillOffset;
            var l = beatmapObject.GetObjectLifeLength(_oldStyle: true);
            return time >= st && (time <= l + st && akt != AutoKillType.OldStyleNoAutokill && akt != AutoKillType.SongTime || akt == AutoKillType.OldStyleNoAutokill || time < ako && beatmapObject.autoKillType == AutoKillType.SongTime);
        }

        /// <summary>
        /// Gets beatmap object by id from any beatmap object list.
        /// </summary>
        /// <param name="beatmapObjects"></param>
        /// <param name="_id"></param>
        /// <returns>Beatmap object from list.</returns>
        public static BaseBeatmapObject ID(this List<BaseBeatmapObject> beatmapObjects, string _id) => beatmapObjects.Find(x => x.id == _id);

        /// <summary>
        /// Gets the parent of the beatmap object.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <returns>Parent of the beatmap object.</returns>
        public static BaseBeatmapObject GetParent(this BaseBeatmapObject beatmapObject) => DataManager.inst.gameData.beatmapObjects.Find(x => x.id == beatmapObject.parent);

        public static BasePrefab GetPrefab(this BasePrefabObject prefabObject) => DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabObject.prefabID);

        /// <summary>
        /// Creates a new list with all the same element instances as the parent list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>Returns cloned list.</returns>
        public static List<T> Clone<T>(this List<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array);
            return array.ToList();
        }

        public static T[] Copy<T>(this T[] ts)
        {
            var array = new T[ts.Length];
            for (int i = 0; i < ts.Length; i++)
                array[i] = ts[i];
            return array;
        }

        public static float Interpolate(this BaseBeatmapObject beatmapObject, int type, int value)
        {
            var time = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

            var nextKFIndex = beatmapObject.events[type].FindIndex(x => x.eventTime > time);

            if (nextKFIndex >= 0)
            {
                var prevKFIndex = nextKFIndex - 1;
                if (prevKFIndex < 0)
                    prevKFIndex = 0;

                var nextKF = beatmapObject.events[type][nextKFIndex];
                var prevKF = beatmapObject.events[type][prevKFIndex];

                var next = nextKF.eventValues[value];
                var prev = prevKF.eventValues[value];

                if (float.IsNaN(prev))
                    prev = 0f;

                if (float.IsNaN(next))
                    next = 0f;

                var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time)));

                if (prevKFIndex == nextKFIndex || float.IsNaN(x) || float.IsInfinity(x))
                    x = next;

                return x;
            }
            else
            {
                var x = beatmapObject.events[type][beatmapObject.events[type].Count - 1].eventValues[value];

                if (float.IsNaN(x))
                    x = 0f;

                return x;
            }
        }

        #endregion

        #region Event Keyframes

        public static BaseEventKeyframe GetEventKeyframe(this List<List<BaseEventKeyframe>> eventKeyframes, int type, int index) => eventKeyframes[RTMath.Clamp(type, 0, eventKeyframes.Count - 1)].GetEventKeyframe(index);
        public static BaseEventKeyframe GetEventKeyframe(this List<BaseEventKeyframe> eventKeyframes, int index) => eventKeyframes[RTMath.Clamp(index, 0, eventKeyframes.Count - 1)];

        public static BaseEventKeyframe ClosestEventKeyframe(int _type, object n = null) => DataManager.inst.gameData.eventObjects.allEvents[_type][ClosestEventKeyframe(_type)];

        /// <summary>
        /// Gets closest event keyframe to current time.
        /// </summary>
        /// <param name="_type">Event Keyframe Type</param>
        /// <returns>Event Keyframe Index</returns>
        public static int ClosestEventKeyframe(int _type)
        {
            var allEvents = DataManager.inst.gameData.eventObjects.allEvents;
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (allEvents[_type].Has(x => x.eventTime > time))
            {
                var nextKFE = allEvents[_type].Find(x => x.eventTime > time);
                var nextKF = allEvents[_type].IndexOf(nextKFE);
                var prevKF = nextKF - 1;

                if (nextKF == 0)
                {
                    prevKF = 0;
                }
                else
                {
                    var v1 = new Vector2(allEvents[_type][prevKF].eventTime, 0f);
                    var v2 = new Vector2(allEvents[_type][nextKF].eventTime, 0f);

                    float dis = Vector2.Distance(v1, v2) / 2f;

                    bool prevClose = time > dis + allEvents[_type][prevKF].eventTime;
                    bool nextClose = time < allEvents[_type][nextKF].eventTime - dis;

                    if (!prevClose)
                    {
                        return prevKF;
                    }
                    if (!nextClose)
                    {
                        return nextKF;
                    }
                }
            }
            return 0;
        }

        public static BaseEventKeyframe ClosestKeyframe(this BaseBeatmapObject beatmapObject, int _type, object n = null) => beatmapObject.events[_type][beatmapObject.ClosestKeyframe(_type)];

        /// <summary>
        /// Gets closest event keyframe to current time within a beatmap object.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <param name="_type">Event Keyframe Type</param>
        /// <returns>Event Keyframe Index</returns>
        public static int ClosestKeyframe(this BaseBeatmapObject beatmapObject, int _type)
        {
            if (beatmapObject.events[_type].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime) != null)
            {
                var nextKFE = beatmapObject.events[_type].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime);
                var nextKF = beatmapObject.events[_type].IndexOf(nextKFE);
                var prevKF = nextKF - 1;

                if (prevKF < 0)
                    prevKF = 0;

                var prevKFE = beatmapObject.events[_type][prevKF];

                if (nextKF == 0)
                {
                    prevKF = 0;
                }
                else
                {
                    var v1 = new Vector2(beatmapObject.events[_type][prevKF].eventTime, 0f);
                    var v2 = new Vector2(beatmapObject.events[_type][nextKF].eventTime, 0f);

                    float dis = Vector2.Distance(v1, v2);
                    float time = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    bool prevClose = time > dis + beatmapObject.events[_type][prevKF].eventTime / 2f;
                    bool nextClose = time < beatmapObject.events[_type][nextKF].eventTime - dis / 2f;

                    if (!prevClose)
                    {
                        return prevKF;
                    }
                    if (!nextClose)
                    {
                        return nextKF;
                    }
                }
                {
                    var dis = RTMath.Distance(nextKFE.eventTime, prevKFE.eventTime);
                    var time = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    var prevClose = time > dis + prevKFE.eventTime / 2f;
                    var nextClose = time < nextKFE.eventTime - dis / 2f;


                }
            }
            return 0;
        }

        public static bool TryGetValue(this BaseEventKeyframe eventKeyframe, int index, out float result)
        {
            result = eventKeyframe.eventValues.Length > index ? eventKeyframe.eventValues[index] : 0f;
            return eventKeyframe.eventValues.Length > index;
        }

        #endregion

        #region Data Extensions

        public static bool Has<T>(this List<T> ts, Predicate<T> predicate) => ts.Find(predicate) != null;

        public static void For<T>(this T[] ts, Action<T, int> action)
        {
            for (int i = 0; i < ts.Length; i++)
                action?.Invoke(ts[i], i);
        }

        public static void For<T>(this List<T> ts, Action<T, int> action)
        {
            for (int i = 0; i < ts.Count; i++)
                action?.Invoke(ts[i], i);
        }

        public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(this List<T> ts, Func<T, TKey> key, Func<T, TValue> value)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            var keys = ts.Select(key);
            var values = ts.Select(value);

            for (int i = 0; i < keys.Count(); i++)
            {
                var k = keys.ElementAt(i);
                if (!dictionary.ContainsKey(k))
                    dictionary.Add(k, values.ElementAt(i));
            }

            return dictionary;
        }

        static void Test()
        {
            DataManager.inst.gameData.beatmapObjects.ToDictionary(x => x.id, x => x);

            DataManager.inst.gameData.beatmapObjects.ToDictionary(x => x.id);

            var dictionary = new Dictionary<string, object>();

            dictionary.Get<string, Component>("test");
        }

        public static string[] GetLinesArray(this string str) => str.Split(new string[] { "\n", "\n\r", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        public static List<string> GetLines(this string str) => str.Split(new string[] { "\n", "\n\r", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        public static T Get<TKey, T>(this Dictionary<TKey, object> keyValuePairs, TKey key) => (T)keyValuePairs[key];

        public static Vector2 ToVector2(this Vector3 _v) => new Vector2(_v.x, _v.y);

        #endregion

        #region JSON

        public static JSONNode ToJSON(this Vector2Int vector2)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector2.x.ToString();
            jn["y"] = vector2.y.ToString();

            return jn;
        }
        
        public static JSONNode ToJSON(this Vector2 vector2)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector2.x.ToString();
            jn["y"] = vector2.y.ToString();

            return jn;
        }

        public static JSONNode ToJSON(this Vector3Int vector3)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector3.x.ToString();
            jn["y"] = vector3.y.ToString();
            jn["z"] = vector3.z.ToString();

            return jn;
        }
        
        public static JSONNode ToJSON(this Vector3 vector3)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector3.x.ToString();
            jn["y"] = vector3.y.ToString();
            jn["z"] = vector3.z.ToString();

            return jn;
        }

        public static JSONNode ToJSON(this Vector4 vector4)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector4.x.ToString();
            jn["y"] = vector4.y.ToString();
            jn["z"] = vector4.z.ToString();
            jn["w"] = vector4.w.ToString();

            return jn;
        }

        public static Vector2 AsVector2(this JSONNode jn) => new Vector2(jn["x"].AsFloat, jn["y"].AsFloat);

        public static Vector3 AsVector3(this JSONNode jn) => new Vector3(jn["x"].AsFloat, jn["y"].AsFloat, jn["z"].AsFloat);

        public static Vector3 AsVector4(this JSONNode jn) => new Vector4(jn["x"].AsFloat, jn["y"].AsFloat, jn["z"].AsFloat, jn["w"].AsFloat);

        #endregion

        #region UI

        public static ColorBlock SetColorBlock(this ColorBlock cb, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, float fade = 0.2f)
        {
            cb.normalColor = normal;
            cb.highlightedColor = highlighted;
            cb.pressedColor = pressed;
            cb.selectedColor = selected;
            cb.disabledColor = disabled;
            cb.fadeDuration = fade;
            return cb;
        }

        public static void SetColor(this Material material, Color color) => material.color = color;

        public static void SetText(this Text text, string str) => text.text = str;

        public static void SetColor(this Image image, Color color) => image.color = color;

        public static void SetText(this InputField inputField, string str) => inputField.text = str;

        public static void SetIsOn(this Toggle toggle, bool on) => toggle.isOn = on;

        public static void SetValue(this Dropdown dropdown, int value) => dropdown.value = value;

        public static void SetSlider(this Slider slider, float value) => slider.value = value;

        public static Text PlaceholderText(this InputField inputField) => (Text)inputField.placeholder;

        public static void ClearAll(this Button.ButtonClickedEvent b)
        {
            b.m_Calls.m_ExecutingCalls.Clear();
            b.m_Calls.m_PersistentCalls.Clear();
            b.m_PersistentCalls.m_Calls.Clear();
            b.RemoveAllListeners();
        }

        public static void ClearAll(this InputField.OnChangeEvent i)
        {
            i.m_Calls.m_ExecutingCalls.Clear();
            i.m_Calls.m_PersistentCalls.Clear();
            i.m_PersistentCalls.m_Calls.Clear();
            i.RemoveAllListeners();
        }

        public static void ClearAll(this InputField.SubmitEvent s)
        {
            s.m_Calls.m_ExecutingCalls.Clear();
            s.m_Calls.m_PersistentCalls.Clear();
            s.m_PersistentCalls.m_Calls.Clear();
            s.RemoveAllListeners();
        }

        public static void ClearAll(this Toggle.ToggleEvent i)
        {
            i.m_Calls.m_ExecutingCalls.Clear();
            i.m_Calls.m_PersistentCalls.Clear();
            i.m_PersistentCalls.m_Calls.Clear();
            i.RemoveAllListeners();
        }

        public static void ClearAll(this Dropdown.DropdownEvent d)
        {
            d.m_Calls.m_ExecutingCalls.Clear();
            d.m_Calls.m_PersistentCalls.Clear();
            d.m_PersistentCalls.m_Calls.Clear();
            d.RemoveAllListeners();
        }

        public static void ClearAll(this Slider.SliderEvent s)
        {
            s.m_Calls.m_ExecutingCalls.Clear();
            s.m_Calls.m_PersistentCalls.Clear();
            s.m_PersistentCalls.m_Calls.Clear();
            s.RemoveAllListeners();
        }

        public static void ClearAll(this Scrollbar.ScrollEvent s)
        {
            s.m_Calls.m_ExecutingCalls.Clear();
            s.m_Calls.m_PersistentCalls.Clear();
            s.m_PersistentCalls.m_Calls.Clear();
            s.RemoveAllListeners();
        }

        public static void NewOnClickListener(this Button b, UnityAction unityAction)
        {
            b.onClick.ClearAll();
            b.onClick.AddListener(unityAction);
        }

        public static void NewValueChangedListener(this InputField i, string value, UnityAction<string> unityAction)
        {
            i.onValueChanged.ClearAll();
            i.text = value;
            i.onValueChanged.AddListener(unityAction);
        }

        public static void NewValueChangedListener(this Toggle i, bool value, UnityAction<bool> unityAction)
        {
            i.onValueChanged.ClearAll();
            i.isOn = value;
            i.onValueChanged.AddListener(unityAction);
        }

        public static void NewValueChangedListener(this Dropdown d, int value, UnityAction<int> unityAction)
        {
            d.onValueChanged.ClearAll();
            d.value = value;
            d.onValueChanged.AddListener(unityAction);
        }

        public static void NewValueChangedListener(this Slider slider, float value, UnityAction<float> unityAction)
        {
            slider.onValueChanged.ClearAll();
            slider.value = value;
            slider.onValueChanged.AddListener(unityAction);
        }

        #endregion

        #region Misc

        public static int ClosestPlayer(GameObject _gm)
        {
            if (InputDataManager.inst.players.Count > 0)
            {
                float distance = float.MaxValue;
                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1));

                        if (Vector2.Distance(player.Find("Player").position, _gm.transform.position) < distance)
                        {
                            distance = Vector2.Distance(player.Find("Player").position, _gm.transform.position);
                        }
                    }
                }
                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1));

                        if (Vector2.Distance(player.Find("Player").position, _gm.transform.position) < distance)
                        {
                            return i;
                        }
                    }
                }
            }

            return 0;
        }

        public static bool IsTouchingPlayer(this BaseBeatmapObject beatmapObject)
        {
            var list = new List<bool>();

            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject.Collider)
            {
                if (levelObject.visualObject.Collider)
                {
                    var collider = levelObject.visualObject.Collider;

                    for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                    {
                        if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                        {
                            var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                            list.Add(player.GetComponent<Collider2D>().IsTouching(collider));
                        }
                    }
                }
            }

            return list.Any(x => x == true);
        }

        public static void SetTransform(this BeatmapObject beatmapObject, int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0:
                    {
                        if (toAxis == 0)
                            beatmapObject.positionOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.positionOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.positionOffset.z = value;

                        break;
                    }
                case 1:
                    {
                        if (toAxis == 0)
                            beatmapObject.scaleOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.scaleOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.scaleOffset.z = value;

                        break;
                    }
                case 2:
                    {
                        if (toAxis == 0)
                            beatmapObject.rotationOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.rotationOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.rotationOffset.z = value;

                        break;
                    }
            }
        }

        public static void CreateCollider(this PolygonCollider2D polygonCollider, MeshFilter meshFilter)
        {
            if (meshFilter.mesh == null)
                return;

            // Get triangles and vertices from mesh
            var triangles = meshFilter.mesh.triangles;
            var vertices = meshFilter.mesh.vertices;

            // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
            var edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int vert1 = triangles[i + e];
                    int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                    string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);

                    if (edges.ContainsKey(edge))
                        edges.Remove(edge);
                    else
                        edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                }
            }

            // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
            var lookup = new Dictionary<int, int>();
            foreach (var edge in edges.Values)
            {
                if (!lookup.ContainsKey(edge.Key))
                    lookup.Add(edge.Key, edge.Value);
            }

            // Create empty polygon collider
            polygonCollider.pathCount = 0;

            // Loop through edge vertices in order
            var startVert = 0;
            var nextVert = startVert;
            var highestVert = startVert;
            var colliderPath = new List<Vector2>();
            while (true)
            {
                // Add vertex to collider path
                colliderPath.Add(vertices[nextVert]);

                // Get next vertex
                nextVert = lookup[nextVert];

                // Store highest vertex (to know what shape to move to next)
                if (nextVert > highestVert)
                    highestVert = nextVert;

                // Shape complete
                if (nextVert == startVert)
                {
                    // Add path to polygon collider
                    polygonCollider.pathCount++;
                    polygonCollider.SetPath(polygonCollider.pathCount - 1, colliderPath.ToArray());
                    colliderPath.Clear();

                    // Go to next shape if one exists
                    if (lookup.ContainsKey(highestVert + 1))
                    {
                        // Set starting and next vertices
                        startVert = highestVert + 1;
                        nextVert = startVert;

                        // Continue to next loop
                        continue;
                    }

                    // No more verts
                    break;
                }
            }
        }

        public static void Save(this Sprite sprite, string path) => SpriteManager.SaveSprite(sprite, path);

        public static bool TryGetComponent<T>(this GameObject gameObject, out T result)
        {
            var t = gameObject.GetComponent<T>();
            result = t;
            return t != null;
        }

        public static T GetDefault<T>(this List<T> list, int index, T defaultValue) => index >= 0 && index < list.Count - 1 ? list[index] : defaultValue;

        public static void AddSet<TKey, TValue>(this Dictionary<TKey, TValue> keyValuePairs, TKey key, TValue value)
        {
            if (!keyValuePairs.ContainsKey(key))
                keyValuePairs.Add(key, value);
            else
                keyValuePairs[key] = value;
        }

        public static bool TryFind<T>(this List<T> ts, Predicate<T> match, out T item)
        {
            var t = ts.Find(match);
            item = t;
            return t != null;
        }

        public static Vector3 X(this Vector3 vector3) => new Vector3(vector3.x, 0f, 0f);
        public static Vector3 Y(this Vector3 vector3) => new Vector3(0f, vector3.y, 0f);
        public static Vector3 Z(this Vector3 vector3) => new Vector3(0f, 0f, vector3.z);

        public static Vector2 X(this Vector2 vector3) => new Vector2(vector3.x, 0f);
        public static Vector2 Y(this Vector2 vector3) => new Vector2(0f, vector3.y);

        #endregion
    }
}
