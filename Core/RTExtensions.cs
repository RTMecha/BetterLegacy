using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
            return e;
        }

        public static bool TryFind(this Transform tf, string find, out Transform result)
        {
            var e = tf.Find(find);
            result = e;
            return e;
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
            var copy = gameObject.Duplicate(parent, name);
            copy.transform.SetSiblingIndex(index);
            return copy;
        }

        public static RectTransform AsRT(this Transform transform) => (RectTransform)transform;

        public static void SetPosition(this Transform transform, int axis, float value)
        {
            switch (axis)
            {
                case 0: transform.SetPositionX(value); break;
                case 1: transform.SetPositionY(value); break;
                case 2: transform.SetPositionZ(value); break;
            }
        }

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

        public static void SetLocalPosition(this Transform transform, int axis, float value)
        {
            switch (axis)
            {
                case 0: transform.SetLocalPositionX(value); break;
                case 1: transform.SetLocalPositionY(value); break;
                case 2: transform.SetLocalPositionZ(value); break;
            }
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

        public static void SetLocalScale(this Transform transform, int axis, float value)
        {
            switch (axis)
            {
                case 0: transform.SetLocalScaleX(value); break;
                case 1: transform.SetLocalScaleY(value); break;
                case 2: transform.SetLocalScaleZ(value); break;
            }
        }

        public static void SetLocalScaleX(this Transform transform, float x)
        {
            var sca = transform.localScale;
            sca.x = x;
            transform.localScale = sca;
        }
        
        public static void SetLocalScaleY(this Transform transform, float y)
        {
            var sca = transform.localScale;
            sca.y = y;
            transform.localScale = sca;
        }
        
        public static void SetLocalScaleZ(this Transform transform, float z)
        {
            var sca = transform.localScale;
            sca.z = z;
            transform.localScale = sca;
        }

        public static void SetLocalRotationEuler(this Transform transform, int axis, float value)
        {
            switch (axis)
            {
                case 0: transform.SetLocalRotationEulerX(value); break;
                case 1: transform.SetLocalRotationEulerY(value); break;
                case 2: transform.SetLocalRotationEulerZ(value); break;
            }
        }

        public static void SetLocalRotationEulerX(this Transform transform, float x)
        {
            var rot = transform.localRotation.eulerAngles;
            rot.x = x;
            transform.localRotation = Quaternion.Euler(rot);
        }
        
        public static void SetLocalRotationEulerY(this Transform transform, float y)
        {
            var rot = transform.localRotation.eulerAngles;
            rot.y = y;
            transform.localRotation = Quaternion.Euler(rot);
        }
        
        public static void SetLocalRotationEulerZ(this Transform transform, float z)
        {
            var rot = transform.localRotation.eulerAngles;
            rot.z = z;
            transform.localRotation = Quaternion.Euler(rot);
        }

        public static RectValues GetRectValues(this RectTransform rectTransform) => RectValues.FromRectTransform(rectTransform);

        public static bool TryGetComponent<T>(this GameObject gameObject, out T result) where T : Component
        {
            var t = gameObject.GetComponent<T>();
            result = t;
            return t;
        }

        #endregion

        #region Coroutines

        public static void Start(this IEnumerator routine) => CoreHelper.StartCoroutine(routine);
        public static Coroutine StartCoroutine(this IEnumerator routine) => CoreHelper.StartCoroutine(routine);
        public static void StartAsync(this IEnumerator routine) => CoreHelper.StartCoroutineAsync(routine);
        public static Coroutine StartCoroutineAsync(this IEnumerator routine) => CoreHelper.StartCoroutineAsync(routine);

        #endregion

        #region Data

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

        public static T GetDefault<T>(this List<T> list, int index, T defaultValue) => index >= 0 && index < list.Count - 1 ? list[index] : defaultValue;

        public static bool TryFind<T>(this List<T> ts, Predicate<T> match, out T item)
        {
            var t = ts.Find(match);
            item = t;
            return t != null;
        }

        public static bool TryFindIndex<T>(this List<T> ts, Predicate<T> match, out int index)
        {
            var i = ts.FindIndex(match);
            index = i;
            return index >= 0 && index < ts.Count;
        }

        public static bool TryFindAll<T>(this List<T> ts, Predicate<T> match, out List<T> findAll)
        {
            findAll = ts.FindAll(match);
            return findAll.Count > 0;
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List</typeparam>
        /// <param name="t">Object reference.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> ts, T t, int moveTo)
        {
            var index = ts.IndexOf(t);
            if (index < 0 || index == moveTo)
                return;

            var result = ts[index];
            ts.RemoveAt(index);
            ts.Insert(Mathf.Clamp(moveTo, 0, ts.Count), result);
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List.</typeparam>
        /// <param name="match">Object predicate.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> ts, Predicate<T> match, int moveTo)
        {
            var index = ts.FindIndex(match);
            if (index < 0 || index == moveTo)
                return;

            var result = ts[index];
            ts.RemoveAt(index);
            ts.Insert(Mathf.Clamp(moveTo, 0, ts.Count), result);
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List.</typeparam>
        /// <param name="index">Index of an object.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> ts, int index, int moveTo)
        {
            if (index < 0 || index == moveTo)
                return;

            var result = ts[index];
            ts.RemoveAt(index);
            ts.Insert(Mathf.Clamp(moveTo, 0, ts.Count), result);
        }

        /// <summary>
        /// Checks if a list has a specific item.
        /// </summary>
        /// <typeparam name="T">The type of the list.</typeparam>
        /// <param name="predicate">Predicate to find an item.</param>
        /// <returns>Returns true if an item is found, otherwise returns false.</returns>
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

        /// <summary>
        /// Fills a list with an item.
        /// </summary>
        /// <typeparam name="T">The type of the list and item.</typeparam>
        /// <param name="count">The amount to fill.</param>
        /// <param name="obj">The item to fill the list with.</param>
        public static void Fill<T>(this List<T> ts, int count, T obj)
        {
            for (int i = 0; i < count; i++)
                ts.Add(obj);
        }
        
        public static string[] GetLines(this string str) => str.Split(new string[] { "\n", "\n\r", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        public static string Remove(this string str, string remove) => str.Replace(remove, "");

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

        public static void AssignTexture(this Image image, Texture2D texture2D) => image.sprite = SpriteHelper.CreateSprite(texture2D);

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

        public static void ClearAll(this Button.ButtonClickedEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this InputField.OnChangeEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this InputField.SubmitEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this Toggle.ToggleEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this Dropdown.DropdownEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this Slider.SliderEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void ClearAll(this Scrollbar.ScrollEvent uiEvent)
        {
            uiEvent.m_Calls.m_ExecutingCalls.Clear();
            uiEvent.m_Calls.m_PersistentCalls.Clear();
            uiEvent.m_Calls.Clear();
            uiEvent.m_PersistentCalls.m_Calls.Clear();
            uiEvent.m_PersistentCalls.Clear();
            uiEvent.RemoveAllListeners();
        }

        public static void NewOnClickListener(this Button b, UnityAction unityAction)
        {
            b.onClick.ClearAll();
            b.onClick.AddListener(unityAction);
        }

        public static void NewListener(this Button.ButtonClickedEvent b, Action action)
        {
            b.ClearAll();
            b.AddListener(() => { action?.Invoke(); });
        }

        public static void NewListener(this InputField.OnChangeEvent s, Action<string> action)
        {
            s.ClearAll();
            s.AddListener(x => { action?.Invoke(x); });
        }
        
        public static void NewListener(this InputField.SubmitEvent s, Action<string> action)
        {
            s.ClearAll();
            s.AddListener(x => { action?.Invoke(x); });
        }
        
        public static void NewListener(this Toggle.ToggleEvent t, Action<bool> action)
        {
            t.ClearAll();
            t.AddListener(x => { action?.Invoke(x); });
        }
        
        public static void NewListener(this Dropdown.DropdownEvent d, Action<int> action)
        {
            d.ClearAll();
            d.AddListener(x => { action?.Invoke(x); });
        }
        
        public static void NewListener(this Slider.SliderEvent s, Action<float> action)
        {
            s.ClearAll();
            s.AddListener(x => { action?.Invoke(x); });
        }
        
        public static void NewListener(this Scrollbar.ScrollEvent s, Action<float> action)
        {
            s.ClearAll();
            s.AddListener(x => { action?.Invoke(x); });
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

        public static void Save(this Sprite sprite, string path) => SpriteHelper.SaveSprite(sprite, path);

        public static Vector3 X(this Vector3 vector3) => new Vector3(vector3.x, 0f, 0f);
        public static Vector3 Y(this Vector3 vector3) => new Vector3(0f, vector3.y, 0f);
        public static Vector3 Z(this Vector3 vector3) => new Vector3(0f, 0f, vector3.z);

        public static Vector2 X(this Vector2 vector3) => new Vector2(vector3.x, 0f);
        public static Vector2 Y(this Vector2 vector3) => new Vector2(0f, vector3.y);

        #endregion
    }
}
