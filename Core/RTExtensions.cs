using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using LSFunctions;

using ILMath;

using SimpleJSON;
using SteamworksFacepunch.Ugc;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;

using Object = UnityEngine.Object;

namespace BetterLegacy.Core
{
    /// <summary>
    /// BetterLegacy extension class.
    /// </summary>
    public static class RTExtensions
    {
        #region Scene

        /// <summary>
        /// Tries to find a child from a parent.
        /// </summary>
        /// <param name="find">Child to find. To search through the chain, do "object 1/object 2/object 3"</param>
        /// <param name="result">Output child.</param>
        /// <returns>Returns true if a child was found, otherwise returns false.</returns>
        public static bool TryFind(this Transform tf, string find, out Transform result)
        {
            result = tf.Find(find);
            return result;
        }

        /// <summary>
        /// Gets the children of a <see cref="Transform"/>.
        /// </summary>
        /// <returns>Returns a <see cref="List{T}"/> of the <see cref="Transform"/>s children.</returns>
        public static List<Transform> ChildList(this Transform transform)
        {
            var list = new List<Transform>();
            foreach (var obj in transform)
                list.Add((Transform)obj);
            return list;
        }

        /// <summary>
        /// Gets the children of a <see cref="Transform"/>.
        /// </summary>
        /// <returns>Returns an <see cref="IEnumerable{T}"/> of the <see cref="Transform"/>s children.</returns>
        public static IEnumerable<Transform> GetChildren(this Transform transform)
        {
            foreach (Transform child in transform)
                yield return child;
        }

        /// <summary>
        /// Destroys all the children of the <see cref="GameObject"/>.
        /// </summary>
        /// <param name="instant">True if it should be instant, otherwise put onto the Unity scheduler.</param>
        public static void DeleteChildren(this Transform tf, bool instant = false) => LSHelpers.DeleteChildren(tf, instant);

        /// <summary>
        /// Duplicates a <see cref="GameObject"/>, sets a parent and ensures the same local position and scale. Wraps the <see cref="Object.Instantiate(Object)"/> method.
        /// </summary>
        /// <param name="parent">Parent to set.</param>
        /// <returns>Returns a duplicated <see cref="GameObject"/>.</returns>
        public static GameObject Duplicate(this GameObject gameObject, Transform parent)
        {
            var copy = Object.Instantiate(gameObject);
            copy.transform.SetParent(parent);
            copy.transform.localPosition = gameObject.transform.localPosition;
            copy.transform.localScale = gameObject.transform.localScale;

            return copy;
        }

        /// <summary>
        /// Duplicates a <see cref="GameObject"/>, sets a parent, name and ensures the same local position and scale. Wraps the <see cref="Object.Instantiate(Object)"/> method.
        /// </summary>
        /// <param name="parent">Parent to set.</param>
        /// <param name="name">Name of the <see cref="GameObject"/>.</param>
        /// <returns>Returns a duplicated <see cref="GameObject"/>.</returns>
        public static GameObject Duplicate(this GameObject gameObject, Transform parent, string name)
        {
            var copy = gameObject.Duplicate(parent);
            copy.name = name;
            return copy;
        }

        /// <summary>
        /// Duplicates a <see cref="GameObject"/>, sets a parent & sibling index, name and ensures the same local position and scale. Wraps the <see cref="Object.Instantiate(Object)"/> method.
        /// </summary>
        /// <param name="parent">Parent to set.</param>
        /// <param name="name">Name of the <see cref="GameObject"/>.</param>
        /// <param name="index">Sets the sibling index of the <see cref="GameObject"/>.</param>
        /// <returns>Returns a duplicated <see cref="GameObject"/>.</returns>
        public static GameObject Duplicate(this GameObject gameObject, Transform parent, string name, int index)
        {
            var copy = gameObject.Duplicate(parent, name);
            if (index >= 0)
                copy.transform.SetSiblingIndex(index);
            return copy;
        }

        /// <summary>
        /// Casts a <see cref="Transform"/> into a <see cref="RectTransform"/>. For ease of access with Unity UI.
        /// </summary>
        /// <param name="transform">Transform to cast.</param>
        /// <returns>Returns a casted <see cref="Transform"/>.</returns>
        public static RectTransform AsRT(this Transform transform) => (RectTransform)transform;

        public static Vector3 GetLocalVector(this Transform transform, int type) => type switch
        {
            0 => transform.localPosition,
            1 => transform.localScale,
            2 => transform.localRotation.eulerAngles,
            _ => Vector3.zero,
        };

        public static Vector3 GetVector(this Transform transform, int type) => type switch
        {
            0 => transform.position,
            1 => transform.lossyScale,
            2 => transform.rotation.eulerAngles,
            _ => Vector3.zero,
        };

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

        public static void SetAnchoredPositionX(this RectTransform transform, float x)
        {
            var pos = transform.anchoredPosition;
            pos.x = x;
            transform.anchoredPosition = pos;
        }

        public static void SetAnchoredPositionY(this RectTransform transform, float y)
        {
            var pos = transform.anchoredPosition;
            pos.y = y;
            transform.anchoredPosition = pos;
        }

        public static void SetSizeDeltaX(this RectTransform transform, float x)
        {
            var size = transform.sizeDelta;
            size.x = x;
            transform.sizeDelta = size;
        }
        
        public static void SetSizeDeltaY(this RectTransform transform, float y)
        {
            var size = transform.sizeDelta;
            size.y = y;
            transform.sizeDelta = size;
        }

        /// <summary>
        /// Gets a <see cref="RectValues"/> from a <see cref="RectTransform"/>.
        /// </summary>
        /// <param name="rectTransform"><see cref="RectTransform"/> to get.</param>
        /// <returns>Returns <see cref="RectValues"/> based on the <see cref="RectTransform"/>.</returns>
        public static RectValues GetRectValues(this RectTransform rectTransform) => RectValues.FromRectTransform(rectTransform);

        /// <summary>
        /// Tries to get a component from a <see cref="GameObject"/> and outputs it.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="gameObject">GameObject to get a component from.</param>
        /// <param name="result">Output component.</param>
        /// <returns>Returns true if a component was found, otherwise returns false.</returns>
        public static bool TryGetComponent<T>(this GameObject gameObject, out T result) where T : Component
        {
            result = gameObject.GetComponent<T>();
            return result;
        }

        /// <summary>
        /// Add the component if doesn't exist in the game object, otherwise get the component.
        /// </summary>
        /// <typeparam name="T">Type of the Component.</typeparam>
        /// <returns>Returns a component.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component => gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

        #endregion

        #region Coroutines

        /// <summary>
        /// Starts an <see cref="IEnumerator"/> as a <see cref="Coroutine"/> without returning the coroutine.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        public static void Start(this IEnumerator routine) => CoroutineHelper.StartCoroutine(routine);
        /// <summary>
        /// Starts an <see cref="IEnumerator"/> as a <see cref="Coroutine"/>.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public static Coroutine StartCoroutine(this IEnumerator routine) => CoroutineHelper.StartCoroutine(routine);
        /// <summary>
        /// Starts an <see cref="IEnumerator"/> asynchronously as a <see cref="Coroutine"/> without returning the coroutine.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        public static void StartAsync(this IEnumerator routine) => CoroutineHelper.StartCoroutineAsync(routine);
        /// <summary>
        /// Starts an <see cref="IEnumerator"/> asynchronously as a <see cref="Coroutine"/>.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated coroutine.</returns>
        public static Coroutine StartCoroutineAsync(this IEnumerator routine) => CoroutineHelper.StartCoroutineAsync(routine);

        #endregion

        #region Data

        /// <summary>
        /// Splits a string into an array.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <param name="split">Strings to split by.</param>
        /// <returns>Returns an array of strings split from the input based on provided strings.</returns>
        public static string[] Split(this string input, params string[] split) => input.Split(split, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Removes a specified string from the input string.
        /// </summary>
        /// <param name="remove">String to remove.</param>
        /// <returns>Returns a string with the specified string removed.</returns>
        public static string Remove(this string input, string remove) => input.Replace(remove, "");

        /// <summary>
        /// Gets a vector value at an index.
        /// </summary>
        /// <param name="index">Index of the axis.</param>
        /// <returns>Returns an axis.</returns>
        public static float At(this Vector2 vector2, int index) => vector2[Mathf.Clamp(index, 0, 1)];

        /// <summary>
        /// Gets a vector value at an index.
        /// </summary>
        /// <param name="index">Index of the axis.</param>
        /// <returns>Returns an axis.</returns>
        public static float At(this Vector3 vector3, int index) => vector3[Mathf.Clamp(index, 0, 2)];

        /// <summary>
        /// Gets a vector value at an index.
        /// </summary>
        /// <param name="index">Index of the axis.</param>
        /// <returns>Returns an axis.</returns>
        public static float At(this Vector4 vector4, int index) => vector4[Mathf.Clamp(index, 0, 3)];

        /// <summary>
        /// Gets a vector value at an index.
        /// </summary>
        /// <param name="index">Index of the axis.</param>
        /// <returns>Returns an axis.</returns>
        public static float At(this Vector2Int vector2, int index) => vector2[Mathf.Clamp(index, 0, 1)];

        /// <summary>
        /// Gets a vector value at an index.
        /// </summary>
        /// <param name="index">Index of the axis.</param>
        /// <returns>Returns an axis.</returns>
        public static float At(this Vector3Int vector3, int index) => vector3[Mathf.Clamp(index, 0, 2)];

        /// <summary>
        /// Gets a color channel at an index.
        /// </summary>
        /// <param name="index">Index of the color channel.</param>
        /// <returns>Returns a color channel.</returns>
        public static float At(this Color color, int index) => color[Mathf.Clamp(index, 0, 3)];

        public static Transform GetPlayer(this Animation.Keyframe.IHomingKeyframe homingKeyframe)
        {
            var player = PlayerManager.GetClosestPlayer(homingKeyframe.GetPosition());
            if (player && player.RuntimePlayer)
                return player.RuntimePlayer.transform.Find("Player");
            return null;
        }
        
        public static Transform GetPlayer(this Animation.Keyframe.IHomingKeyframe homingKeyframe, float time)
        {
            var player = PlayerManager.GetClosestPlayer(homingKeyframe.GetPosition(time));
            if (player && player.RuntimePlayer)
                return player.RuntimePlayer.transform.Find("Player");
            return null;
        }

        #endregion

        #region Collection

        /// <summary>
        /// Creates a new list with all the same element instances as the parent list.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <returns>Returns cloned list.</returns>
        public static List<T> Clone<T>(this List<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array);
            return array.ToList();
        }

        /// <summary>
        /// Copies the items from an array to a new array.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <returns>Returns a copied array.</returns>
        public static T[] Copy<T>(this T[] array)
        {
            var newArray = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
                newArray[i] = array[i];
            return newArray;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs the first occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IEnumerable{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="result">The item found.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFind<T>(this IEnumerable<T> collection, Predicate<T> match, out T result)
        {
            foreach (var item in collection)
            {
                if (match(item))
                {
                    result = item;
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs the first occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IEnumerable{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="result">The item found.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFind<T>(this T[] array, Predicate<T> match, out T result)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];
                if (match(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs the first occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="result">The item found.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFind<T>(this List<T> list, Predicate<T> match, out T result)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                {
                    result = list[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs the last occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="result">The item found.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFindLast<T>(this List<T> list, Predicate<T> match, out T result)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (match(list[i]))
                {
                    result = list[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs an index of the first occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="index">Index of the match.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFindIndex<T>(this List<T> list, Predicate<T> match, out int index)
        {
            index = list.FindIndex(match);
            return index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs an index of the last occurance of the match.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="index">Index of the match.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFindLastIndex<T>(this List<T> list, Predicate<T> match, out int index)
        {
            index = list.FindLastIndex(match);
            return index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Tries to find a match in the list and outputs a list containing all matches.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="match">Match to find.</param>
        /// <param name="findAll">List of matches.</param>
        /// <returns>Returns true if any matches were found, otherwise returns false.</returns>
        public static bool TryFindAll<T>(this List<T> list, Predicate<T> match, out List<T> findAll)
        {
            bool found = false;
            var newList = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                {
                    newList.Add(list[i]);
                    found = true;
                }
            }
            findAll = newList;
            return found;
        }

        /// <summary>
        /// Tries to get an item at an index if the index is in range.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="index">Index of the item to get.</param>
        /// <param name="item">Output item.</param>
        /// <returns>Returns true if the index is inside the range of the list, otherwise returns false.</returns>
        public static bool TryGetAt<T>(this List<T> list, int index, out T item)
        {
            var exists = index >= 0 && index < list.Count;
            item = exists ? list[index] : default;
            return exists;
        }

        /// <summary>
        /// Tries to get an item at an index if the index is in range.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="index">Index of the item to get.</param>
        /// <param name="item">Output item.</param>
        /// <returns>Returns true if the index is inside the range of the array, otherwise returns false.</returns>
        public static bool TryGetAt<T>(this T[] array, int index, out T item)
        {
            var exists = index >= 0 && index < array.Length;
            item = exists ? array[index] : default;
            return exists;
        }

        /// <summary>
        /// Gets an item at an index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="index">Index of the item to get.</param>
        /// <returns>If the index is in the range of the list, returns the item at the index, otherwise returns default.</returns>
        public static T GetAt<T>(this List<T> list, int index)
        {
            list.TryGetAt(index, out T result);
            return result;
        }

        /// <summary>
        /// Gets an item at an index.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="index">Index of the item to get.</param>
        /// <returns>If the index is in the range of the array, returns the item at the index, otherwise returns default.</returns>
        public static T GetAt<T>(this T[] array, int index)
        {
            array.TryGetAt(index, out T result);
            return result;
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List</typeparam>
        /// <param name="t">Object reference.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> list, T t, int moveTo)
        {
            var index = list.IndexOf(t);
            if (index < 0 || index == moveTo)
                return;

            var result = list[index];
            list.RemoveAt(index);
            list.Insert(Mathf.Clamp(moveTo, 0, list.Count), result);
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List.</typeparam>
        /// <param name="match">Object predicate.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> list, Predicate<T> match, int moveTo)
        {
            var index = list.FindIndex(match);
            if (index < 0 || index == moveTo)
                return;

            var result = list[index];
            list.RemoveAt(index);
            list.Insert(Mathf.Clamp(moveTo, 0, list.Count), result);
        }

        /// <summary>
        /// Moves an item in a list to a different index.
        /// </summary>
        /// <typeparam name="T">The type of the List.</typeparam>
        /// <param name="index">Index of an object.</param>
        /// <param name="moveTo">Index to move the item to.</param>
        public static void Move<T>(this List<T> list, int index, int moveTo)
        {
            if (index < 0 || index == moveTo)
                return;

            var result = list[index];
            list.RemoveAt(index);
            list.Insert(Mathf.Clamp(moveTo, 0, list.Count), result);
        }

        /// <summary>
        /// Shuffles a list randomly.
        /// </summary>
        /// <typeparam name="T">The type of the List.</typeparam>
        /// <returns>Returns a shuffled list.</returns>
        public static List<T> Shuffle<T>(this List<T> list) => list.Order(x => x?.GetHashCode() + UnityEngine.Random.Range(-100, 100) * UnityEngine.Random.Range(0, 100), false);

        /// <summary>
        /// Checks if an array has a specific item.
        /// </summary>
        /// <typeparam name="T">The type of the list.</typeparam>
        /// <param name="match">Predicate to find an item.</param>
        /// <returns>Returns true if an item is found, otherwise returns false.</returns>
        public static bool Has<T>(this T[] array, Predicate<T> match)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a list has a specific item.
        /// </summary>
        /// <typeparam name="T">The type of the list.</typeparam>
        /// <param name="match">Predicate to find an item.</param>
        /// <returns>Returns true if an item is found, otherwise returns false.</returns>
        public static bool Has<T>(this List<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Performs a for loop to the array. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoop<T>(this T[] array, Action<T, int> action)
        {
            var length = array.Length;
            if (length == 0)
                return;
            if (length == 1) // we do this because using a for loop on an array of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(array[0], 0);
                return;
            }

            for (int i = 0; i < length; i++)
                action?.Invoke(array[i], i);
        }

        /// <summary>
        /// Performs a for loop to the list. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoop<T>(this List<T> list, Action<T, int> action)
        {
            var count = list.Count;
            if (count == 0)
                return;
            if (count == 1) // we do this because using a for loop on a list of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(list[0], 0);
                return;
            }

            for (int i = 0; i < count; i++)
                action?.Invoke(list[i], i);
        }

        /// <summary>
        /// Performs a for loop to the array. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoop<T>(this T[] array, Action<T> action)
        {
            var length = array.Length;
            if (length == 0)
                return;
            if (length == 1) // we do this because using a for loop on an array of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(array[0]);
                return;
            }

            for (int i = 0; i < length; i++)
                action?.Invoke(array[i]);
        }

        /// <summary>
        /// Performs a for loop to the list. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoop<T>(this List<T> list, Action<T> action)
        {
            var count = list.Count;
            if (count == 0)
                return;
            if (count == 1) // we do this because using a for loop on a list of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(list[0]);
                return;
            }

            for (int i = 0; i < count; i++)
                action?.Invoke(list[i]);
        }

        /// <summary>
        /// Performs a for loop to the array. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoopReverse<T>(this T[] array, Action<T, int> action)
        {
            var length = array.Length;
            if (length == 0)
                return;
            if (length == 1) // we do this because using a for loop on an array of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(array[0], 0);
                return;
            }

            for (int i = length - 1; i >= 0; i--)
                action?.Invoke(array[i], i);
        }

        /// <summary>
        /// Performs a for loop to the list. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoopReverse<T>(this List<T> list, Action<T, int> action)
        {
            var count = list.Count;
            if (count == 0)
                return;
            if (count == 1) // we do this because using a for loop on a list of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(list[0], 0);
                return;
            }

            for (int i = count - 1; i >= 0; i--)
                action?.Invoke(list[i], i);
        }

        /// <summary>
        /// Performs a for loop to the array. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoopReverse<T>(this T[] array, Action<T> action)
        {
            var length = array.Length;
            if (length == 0)
                return;
            if (length == 1) // we do this because using a for loop on an array of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(array[0]);
                return;
            }

            for (int i = length - 1; i >= 0; i--)
                action?.Invoke(array[i]);
        }

        /// <summary>
        /// Performs a for loop to the list. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public static void ForLoopReverse<T>(this List<T> list, Action<T> action)
        {
            var count = list.Count;
            if (count == 0)
                return;
            if (count == 1) // we do this because using a for loop on a list of 1 item seems to be slower than just accessing the index of 0
            {
                action.Invoke(list[0]);
                return;
            }

            for (int i = count - 1; i >= 0; i--)
                action?.Invoke(list[i]);
        }

        /// <summary>
        /// Sorts a list with a custom selector and descending.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <typeparam name="TKey">Type to compare.</typeparam>
        /// <param name="selector">Comparer.</param>
        /// <param name="descending">If the list should sort by descending or not.</param>
        /// <returns>Returns a sorted list.</returns>
        public static List<T> Order<T, TKey>(this List<T> list, Func<T, TKey> selector, bool descending) => (descending ? list.OrderByDescending(selector) : list.OrderBy(selector)).ToList();

        /// <summary>
        /// Fills a list with an item.
        /// </summary>
        /// <typeparam name="T">The type of the list and item.</typeparam>
        /// <param name="count">The amount to fill.</param>
        /// <param name="obj">The item to fill the list with.</param>
        public static List<T> Fill<T>(this List<T> list, int count, T obj)
        {
            for (int i = 0; i < count; i++)
                list.Add(obj);
            return list;
        }

        /// <summary>
        /// Checks if a list contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <returns>Returns true if the list doesn't contain elements.</returns>
        public static bool IsEmpty<T>(this List<T> list) => list.Count < 1;

        /// <summary>
        /// Checks if a queue contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Queue{T}"/>.</typeparam>
        /// <returns>Returns true if the queue doesn't contain elements.</returns>
        public static bool IsEmpty<T>(this Queue<T> queue) => queue.Count < 1;

        /// <summary>
        /// Checks if a collection contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IEnumerable{T}"/>.</typeparam>
        /// <returns>Returns true if the collection doesn't contain elements.</returns>
        public static bool IsEmpty<T>(this IEnumerable<T> collection) => collection.Count() < 1;

        /// <summary>
        /// Checks if a dictionary contains no elements.
        /// </summary>
        /// <typeparam name="TKey">Type of the keys in <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
        /// <typeparam name="TValue">Type of the values in <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
        /// <returns>Returns true if the dictionary doesn't contain elements.</returns>
        public static bool IsEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) => dictionary.Count < 1;

        /// <summary>
        /// Checks if an array contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <returns>Returns true if the array doesn't contain elements.</returns>
        public static bool IsEmpty<T>(this T[] array) => array.Length < 1;

        /// <summary>
        /// Checks if an index is in the range of a list.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="index">Index to verify.</param>
        /// <returns>Returns true if the <paramref name="index"/> is in the range of the list, otherwise returns false.</returns>
        public static bool InRange<T>(this List<T> list, int index) => index >= 0 && index < list.Count;

        /// <summary>
        /// Checks if an index is in the range of an array.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="index">Index to verify.</param>
        /// <returns>Returns true if the <paramref name="index"/> is in the range of the array, otherwise returns false.</returns>
        public static bool InRange<T>(this T[] array, int index) => index >= 0 && index < array.Length;

        /// <summary>
        /// Creates a copy of the list from an indexed range.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="list"></param>
        /// <param name="startIndex">Start index to get.</param>
        /// <param name="endIndex">End index to get.</param>
        /// <returns>Returns a copy of the list from the index range.</returns>
        public static List<T> GetIndexRange<T>(this List<T> list, int startIndex, int endIndex) => list.GetRange(startIndex, endIndex - startIndex);

        /// <summary>
        /// Runs a for loop where iterations may run parallel.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="action">Function to run for each item in the list.</param>
        /// <returns>Returns a result of the loop.</returns>
        public static ParallelLoopResult ParallelFor<T>(this List<T> list, Action<int> action) => Parallel.For(0, list.Count, action);

        /// <summary>
        /// Runs a for loop where iterations may run parallel.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IEnumerable{T}"/>.</typeparam>
        /// <param name="action">Function to run for each item in the collection.</param>
        /// <returns>Returns a result of the loop.</returns>
        public static ParallelLoopResult ParallelForEach<T>(this IEnumerable<T> enumerable, Action<T> action) => Parallel.ForEach(enumerable, action);

        /// <summary>
        /// Removes an item matching the predicate.
        /// </summary>
        /// <typeparam name="T">Type of the list.</typeparam>
        /// <param name="predicate">Predicate to match.</param>
        /// <returns>Returns true if an item was successfully removed, otherwise returns false.</returns>
        public static bool Remove<T>(this List<T> list, Predicate<T> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts the collection into an indexed collection.
        /// </summary>
        /// <typeparam name="T">Type of the collection.</typeparam>
        /// <returns>Returns a collection of indexed items.</returns>
        public static IEnumerable<Indexer<T>> ToIndexer<T>(this IEnumerable<T> collection)
        {
            int index = 0;
            foreach (var item in collection)
            {
                yield return new Indexer<T>(index, item);
                index++;
            }
        }

        /// <summary>
        /// Converts the list into an indexed list.
        /// </summary>
        /// <typeparam name="T">Type of the list.</typeparam>
        /// <returns>Returns a list of indexed items.</returns>
        public static List<Indexer<T>> ToIndexerList<T>(this List<T> list)
        {
            var indexers = new List<Indexer<T>>();
            for (int i = 0; i < list.Count; i++)
                indexers.Add(new Indexer<T>(i, list[i]));
            return indexers;
        }

        /// <summary>
        /// Gets the value associated with the specified key. If no value was found, returns the default.
        /// </summary>
        /// <typeparam name="TKey">Key of the item.</typeparam>
        /// <typeparam name="TValue">Value of the item.</typeparam>
        /// <param name="key">Key to search the dictionary for.</param>
        /// <param name="defaultValue">Default value to return if no entry was found.</param>
        /// <returns>Returns the found entry if one was found, otherwise returns the default value.</returns>
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) => dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;

        /// <summary>
        /// Inserts a range of key value pairs from one dictionary to another.
        /// </summary>
        /// <typeparam name="TKey">Key of the dictionary.</typeparam>
        /// <typeparam name="TValue">Value of the dictionary.</typeparam>
        /// <param name="a">Dictionary to pass the elements to.</param>
        /// <param name="b">Dictionary to pass the elements from.</param>
        public static void InsertRange<TKey, TValue>(this Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
        {
            foreach (var keyValuePair in b)
                a[keyValuePair.Key] = keyValuePair.Value;
        }

        #endregion

        #region JSON

        public static bool Empty(this JSONNode jn) => jn == null || jn.IsArray ? jn.Count < 1 : jn.Children.Count() < 1;

        public static bool Contains(this JSONNode jn, int index, string key) => jn != null && jn.IsArray ? jn[index] != null : jn[key] != null;

        /// <summary>
        /// If the JSON is an array, gets the JSON node at the index, otherwise gets the JSON node with the key.
        /// </summary>
        /// <param name="index">Index to get if JSON is an array.</param>
        /// <param name="key">Key to get if the JSON is an object.</param>
        /// <returns>Returns a JSON node.</returns>
        public static JSONNode Get(this JSONNode jn, int index, string key) => jn.IsArray ? index >= 0 && index < jn.Count ? jn[index] : new JSONNull() : jn[key];

        /// <summary>
        /// If the JSON is an array, gets the JSON node at the index, otherwise gets the JSON node with the key.
        /// </summary>
        /// <param name="index">Index to get if JSON is an array.</param>
        /// <param name="key">Key to get if the JSON is an object.</param>
        /// <param name="defaultJN">Default JSON node to return if no items are found.</param>
        /// <returns>Returns a JSON node.</returns>
        public static JSONNode GetOrDefault(this JSONNode jn, int index, string key, JSONNode defaultJN) => jn.IsArray ? index >= 0 && index < jn.Count ? jn[index] ?? defaultJN : defaultJN : jn[key] ?? defaultJN;

        public static bool TryGet(this JSONNode jn, int index, string key, out JSONNode result)
        {
            if (jn.IsArray && index >= 0 && index < jn.Count)
            {
                result = jn[index];
                return true;
            }

            if (jn.IsObject && jn[key] != null)
            {
                result = jn[key];
                return true;
            }

            result = new JSONNull();
            return false;
        }

        public static JSONNode ToJSONArray(this Vector2Int vector2)
        {
            var jn = Parser.NewJSONArray();
            jn[0] = vector2.x;
            jn[1] = vector2.y;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector2 vector2)
        {
            var jn = Parser.NewJSONArray();
            jn[0] = vector2.x;
            jn[1] = vector2.y;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector3Int vector3)
        {
            var jn = Parser.NewJSONArray();
            jn[0] = vector3.x;
            jn[1] = vector3.y;
            jn[2] = vector3.z;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector3 vector3)
        {
            var jn = Parser.NewJSONArray();
            jn[0] = vector3.x;
            jn[1] = vector3.y;
            jn[2] = vector3.z;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector4 vector4)
        {
            var jn = Parser.NewJSONArray();
            jn[0] = vector4.x;
            jn[1] = vector4.y;
            jn[2] = vector4.z;
            jn[3] = vector4.w;
            return jn;
        }

        public static JSONNode ToJSON(this Vector2Int vector2)
        {
            var jn = Parser.NewJSONObject();
            jn["x"] = vector2.x.ToString();
            jn["y"] = vector2.y.ToString();
            return jn;
        }
        
        public static JSONNode ToJSON(this Vector2 vector2)
        {
            var jn = Parser.NewJSONObject();
            jn["x"] = vector2.x;
            jn["y"] = vector2.y;
            return jn;
        }

        public static JSONNode ToJSON(this Vector3Int vector3)
        {
            var jn = Parser.NewJSONObject();
            jn["x"] = vector3.x.ToString();
            jn["y"] = vector3.y.ToString();
            jn["z"] = vector3.z.ToString();
            return jn;
        }
        
        public static JSONNode ToJSON(this Vector3 vector3)
        {
            var jn = Parser.NewJSONObject();
            jn["x"] = vector3.x.ToString();
            jn["y"] = vector3.y.ToString();
            jn["z"] = vector3.z.ToString();
            return jn;
        }

        public static JSONNode ToJSON(this Vector4 vector4)
        {
            var jn = Parser.NewJSONObject();
            jn["x"] = vector4.x.ToString();
            jn["y"] = vector4.y.ToString();
            jn["z"] = vector4.z.ToString();
            jn["w"] = vector4.w.ToString();
            return jn;
        }

        public static Vector2 AsVector2(this JSONNode jn) => Parser.TryParse(jn, Vector2.zero);

        public static Vector3 AsVector3(this JSONNode jn) => new Vector3(jn["x"].AsFloat, jn["y"].AsFloat, jn["z"].AsFloat);

        public static Vector3 AsVector4(this JSONNode jn) => new Vector4(jn["x"].AsFloat, jn["y"].AsFloat, jn["z"].AsFloat, jn["w"].AsFloat);

        #endregion

        #region UI

        /// <summary>
        /// Assigns a <see cref="Texture2D"/> to an <see cref="Image"/>.
        /// </summary>
        /// <param name="texture2D"><see cref="Texture2D"/> to assign.</param>
        public static void AssignTexture(this Image image, Texture2D texture2D) => image.sprite = SpriteHelper.CreateSprite(texture2D);

        /// <summary>
        /// Sets the colors of <see cref="Selectable.colors"/>.
        /// </summary>
        /// <param name="normal">The normal color.</param>
        /// <param name="highlighted">The highlighted color.</param>
        /// <param name="pressed">The pressed color.</param>
        /// <param name="selected">The selected color.</param>
        /// <param name="disabled">The disabled color.</param>
        /// <param name="fade">The fade duration between each color.</param>
        public static void SetColorBlock(this Selectable selectable, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, float fade = 0.2f) => selectable.colors = selectable.colors.SetColorBlock(normal, highlighted, pressed, selected, disabled, fade);

        /// <summary>
        /// Sets the colors of a <see cref="ColorBlock"/>.
        /// </summary>
        /// <param name="normal">The normal color.</param>
        /// <param name="highlighted">The highlighted color.</param>
        /// <param name="pressed">The pressed color.</param>
        /// <param name="selected">The selected color.</param>
        /// <param name="disabled">The disabled color.</param>
        /// <param name="fade">The fade duration between each color.</param>
        /// <returns>Returns the <see cref="ColorBlock"/>.</returns>
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

        /// <summary>
        /// Sets the color of a <see cref="Material"/>.
        /// </summary>
        /// <param name="color">Color to set.</param>
        public static void SetColor(this Material material, Color color) => material.color = color;

        /// <summary>
        /// Sets the text of a <see cref="Text"/> component.
        /// </summary>
        /// <param name="input">The text to set.</param>
        public static void SetText(this Text text, string input) => text.text = input;

        /// <summary>
        /// Sets the color of an <see cref="Image"/> component.
        /// </summary>
        /// <param name="color">Color to set.</param>
        public static void SetColor(this Image image, Color color) => image.color = color;

        /// <summary>
        /// Sets the text of an <see cref="InputField"/> component.
        /// </summary>
        /// <param name="input">The text to set.</param>
        public static void SetText(this InputField inputField, string input) => inputField.text = input;

        /// <summary>
        /// Sets a <see cref="Toggle"/> on / off.
        /// </summary>
        /// <param name="on">The <see cref="Toggle.isOn"/> value.</param>
        public static void SetIsOn(this Toggle toggle, bool on) => toggle.isOn = on;

        /// <summary>
        /// Sets the value of a <see cref="Dropdown"/>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public static void SetValue(this Dropdown dropdown, int value) => dropdown.value = value;

        /// <summary>
        /// Sets the value of a <see cref="Slider"/>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public static void SetSlider(this Slider slider, float value) => slider.value = value;

        /// <summary>
        /// Gets the Placeholder of an <see cref="InputField"/> and casts it to a <see cref="Text"/>, since it's always a <see cref="Text"/> component in Project Arrhythmia.
        /// </summary>
        /// <returns>Returns a casted <see cref="InputField.placeholder"/>.</returns>
        public static Text GetPlaceholderText(this InputField inputField) => (Text)inputField.placeholder;

        /// <summary>
        /// Clears all functions from a <see cref="UnityEventBase"/>, including persistent. This is specifically for weird cases where functions aren't removed with <see cref="UnityEventBase.RemoveAllListeners"/>.
        /// </summary>
        public static void ClearAll(this UnityEventBase unityEvent)
        {
            unityEvent.m_Calls.m_ExecutingCalls.Clear();
            unityEvent.m_Calls.m_PersistentCalls.Clear();
            unityEvent.m_Calls.Clear();
            unityEvent.m_PersistentCalls.m_Calls.Clear();
            unityEvent.m_PersistentCalls.Clear();
            unityEvent.RemoveAllListeners();
        }

        public static void NewListener<T>(this UnityEvent<T> unityEvent, Action<T> action)
        {
            unityEvent.ClearAll();
            unityEvent.AddListener(x => action?.Invoke(x));
        }

        public static void NewListener(this UnityEvent unityEvent, Action action)
        {
            unityEvent.ClearAll();
            unityEvent.AddListener(() => action?.Invoke());
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

        #region Enums

        /// <summary>
        /// Gets the <see cref="Animation.EaseFunction"/> in the easing dictionary.
        /// </summary>
        /// <returns>Returns an <see cref="Animation.EaseFunction"/>.</returns>
        public static Animation.EaseFunction GetFunction(this Easing easing) => Animation.Ease.GetEaseFunction(easing.ToString());

        /// <summary>
        /// Gets the actual registered scene name.
        /// </summary>
        /// <returns>Returns a scene name string from <see cref="SceneName"/>.</returns>
        public static string ToName(this SceneName sceneName) => sceneName == SceneName.post_level ? sceneName.ToString() : sceneName.ToString().Replace("_", " ");

        /// <summary>
        /// Converts the file format to a string and appends a . to the start, to make it a proper file extension.
        /// </summary>
        /// <returns>Returns a file extension.</returns>
        public static string Dot(this FileFormat fileFormat) => "." + fileFormat.ToName();

        /// <summary>
        /// Converts the file format to a string pattern, used for getting files from a directory.
        /// </summary>
        /// <returns>Returns a pattern. Example: *.json</returns>
        public static string ToPattern(this FileFormat fileFormat) => "*" + fileFormat.Dot();

        /// <summary>
        /// Converts the file format to a lowercased string.
        /// </summary>
        /// <returns>Returns a lowercase string representing the <see cref="FileFormat"/>.</returns>
        public static string ToName(this FileFormat fileFormat) => fileFormat.ToString().ToLower();

        /// <summary>
        /// Converts the file format to an <see cref="ArrhythmiaType"/>.
        /// </summary>
        /// <returns>Returns an <see cref="ArrhythmiaType"/> based on the <see cref="FileFormat"/>.<br></br>Example: <see cref="FileFormat.LSB"/> > <see cref="ArrhythmiaType.LS"/> or <see cref="FileFormat.VGD"/> to <see cref="ArrhythmiaType.VG"/>.</returns>
        public static ArrhythmiaType ToArrhythmiaType(this FileFormat fileFormat) => fileFormat.ToName().Contains("ls") ? ArrhythmiaType.LS : fileFormat.ToName().Contains("vg") ? ArrhythmiaType.VG : ArrhythmiaType.NULL;

        /// <summary>
        /// Converts Unity's <see cref="AudioType"/> to the BetterLegacy <see cref="FileFormat"/> enum.
        /// </summary>
        /// <returns>Returns an <see cref="AudioType"/>.</returns>
        public static AudioType ToAudioType(this FileFormat fileFormat) => fileFormat switch
        {
            FileFormat.OGG => AudioType.OGGVORBIS,
            FileFormat.WAV => AudioType.WAV,
            FileFormat.MP3 => AudioType.MPEG,
            _ => AudioType.UNKNOWN,
        };

        /// <summary>
        /// Converts the BetterLegacy <see cref="FileFormat"/> to Unity's <see cref="AudioType"/> enum.
        /// </summary>
        /// <returns>Returns a <see cref="FileFormat"/>.</returns>
        public static FileFormat ToFileFormat(this AudioType audioType) => audioType switch
        {
            AudioType.OGGVORBIS => FileFormat.OGG,
            AudioType.WAV => FileFormat.WAV,
            AudioType.MPEG => FileFormat.MP3,
            _ => FileFormat.NULL,
        };

        /// <summary>
        /// Sorts a query.
        /// </summary>
        /// <param name="query">Query to sort.</param>
        /// <returns>Returns a sorted query based on the <see cref="QuerySort"/>.</returns>
        public static Query SortQuery(this QuerySort querySort, Query query) => querySort switch
        {
            QuerySort.UploadDate => query.RankedByPublicationDate(),
            QuerySort.Votes => query.RankedByVote(),
            QuerySort.VotesUp => query.RankedByVotesUp(),
            QuerySort.TotalVotes => query.RankedByTotalVotesAsc(),
            QuerySort.TotalSubs => query.RankedByTotalUniqueSubscriptions(),
            QuerySort.Trend => query.RankedByTrend(),
            _ => query,
        };

        /// <summary>
        /// Sorts a query.
        /// </summary>
        /// <param name="querySort">Query sorting.</param>
        /// <returns>Returns a sorted query based on the <see cref="QuerySort"/>.</returns>
        public static Query Sort(this Query query, QuerySort querySort) => querySort.SortQuery(query);

        public static Type ToType(this ValueType valueType) => valueType switch
        {
            ValueType.Bool => typeof(bool),
            ValueType.Int => typeof(int),
            ValueType.Float => typeof(float),
            ValueType.String => typeof(string),
            ValueType.Vector2 => typeof(Vector2),
            ValueType.Vector2Int => typeof(Vector2Int),
            ValueType.Vector3 => typeof(Vector3),
            ValueType.Vector3Int => typeof(Vector3Int),
            ValueType.Color => typeof(Color),
            _ => null,
        };

        public static ValueType ToValueType(this Type type)
        {
            if (type.IsEnum)
                return ValueType.Enum;
            if (type == typeof(bool))
                return ValueType.Bool;
            if (type == typeof(int))
                return ValueType.Int;
            if (type == typeof(float))
                return ValueType.Float;
            if (type == typeof(string))
                return ValueType.String;
            if (type == typeof(Vector2))
                return ValueType.Vector2;
            if (type == typeof(Vector2Int))
                return ValueType.Vector2Int;
            if (type == typeof(Vector3))
                return ValueType.Vector3;
            if (type == typeof(Vector3Int))
                return ValueType.Vector3Int;
            if (type == typeof(Color))
                return ValueType.Color;
            if (type == typeof(Delegate))
                return ValueType.Function;
            return ValueType.Unrecognized;
        }

        /// <summary>
        /// Gets the directory of the menu music load mode.
        /// </summary>
        /// <param name="menuMusic">Menu music to get a directory from.</param>
        /// <returns>Returns a directory to use for the custom menu music.</returns>
        public static string GetDirectory(this MenuMusicLoadMode menuMusic) => menuMusic switch
        {
            MenuMusicLoadMode.ArcadeFolder => $"{RTFile.ApplicationDirectory}{LevelManager.ListPath}",
            MenuMusicLoadMode.StoryFolder => $"{RTFile.ApplicationDirectory}beatmaps/story",
            MenuMusicLoadMode.EditorFolder => $"{RTFile.ApplicationDirectory}beatmaps/editor",
            MenuMusicLoadMode.InterfacesFolder => $"{RTFile.ApplicationDirectory}beatmaps/interfaces/music",
            MenuMusicLoadMode.GlobalFolder => MenuConfig.Instance.MusicGlobalPath.Value,
            _ => RTFile.ApplicationDirectory + "settings/menus",
        };

        /// <summary>
        /// Converts the reference type into a compatibility check.
        /// </summary>
        /// <param name="referenceType">Reference Type.</param>
        /// <returns>Returns the associated reference type.</returns>
        public static ModifierCompatibility ToCompatibility(this ModifierReferenceType referenceType) => ModifierCompatibility.FromType(referenceType);

        #endregion

        #region Interfaces

        /// <summary>
        /// Removes the prefab references.
        /// </summary>
        public static void RemovePrefabReference(this IPrefabable instance)
        {
            instance.PrefabID = string.Empty;
            instance.PrefabInstanceID = string.Empty;
        }

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object reference.</param>
        public static void SetPrefabReference(this IPrefabable instance, PrefabObject prefabObject)
        {
            instance.PrefabID = prefabObject.prefabID;
            instance.PrefabInstanceID = prefabObject.id;
        }

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from a prefabable.
        /// </summary>
        /// <param name="prefabable">Prefabable object reference.</param>
        public static void SetPrefabReference(this IPrefabable instance, IPrefabable prefabable)
        {
            instance.PrefabID = prefabable.PrefabID;
            instance.PrefabInstanceID = prefabable.PrefabInstanceID;
        }

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public static Prefab GetPrefab(this IPrefabable instance) => instance.CachedPrefab ? instance.CachedPrefab : GameData.Current.prefabs.Find(x => x.id == instance.PrefabID);

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public static Prefab GetPrefab(this IPrefabable instance, List<Prefab> prefabs) => prefabs.Find(x => x.id == instance.PrefabID);

        /// <summary>
        /// Tries to get the Prefab Object reference.
        /// </summary>
        /// <param name="result">Output object.</param>
        /// <returns>Returns true if a Prefab Object was found, otherwise returns false.</returns>
        public static bool TryGetPrefabObject(this IPrefabable instance, out PrefabObject result)
        {
            result = instance.GetPrefabObject();
            return result;
        }

        /// <summary>
        /// Gets the prefab object reference.
        /// </summary>
        public static PrefabObject GetPrefabObject(this IPrefabable instance) => instance.CachedPrefabObject ? instance.CachedPrefabObject : GameData.Current.prefabObjects.Find(x => x.id == instance.PrefabInstanceID);

        /// <summary>
        /// Checks if a prefabable object is from the same Prefab.
        /// </summary>
        /// <param name="prefabable">Prefabable object.</param>
        /// <returns>Returns true if the Prefab is the same.</returns>
        public static bool SamePrefab(this IPrefabable instance, IPrefabable prefabable) => instance.PrefabID == prefabable.PrefabID;

        /// <summary>
        /// Checks if a prefabable object is from the same Prefab and same Prefab instance.
        /// </summary>
        /// <param name="prefabable">Prefabable object.</param>
        /// <returns>Returns true if the Prefab instance is the same.</returns>
        public static bool SamePrefabInstance(this IPrefabable instance, IPrefabable prefabable) => instance.PrefabInstanceID == prefabable.PrefabInstanceID;

        /// <summary>
        /// Checks if a prefabable object is from the same Prefab and same spawned Prefab instance.
        /// </summary>
        /// <param name="prefabable">Prefabable object.</param>
        /// <returns>Returns true if the spawned Prefab instance is the same.</returns>
        public static bool SamePrefabInstanceSpawned(this IPrefabable instance, IPrefabable prefabable) => instance.SamePrefabInstance(prefabable) && instance.FromPrefab == prefabable.FromPrefab;

        /// <summary>
        /// Gets variables from the evaluatable object.
        /// </summary>
        /// <returns>Returns a dictionary containing variables from the evaluatable object.</returns>
        public static Dictionary<string, float> GetObjectVariables(this IEvaluatable variableContainer)
        {
            var variables = new Dictionary<string, float>();
            variableContainer.SetObjectVariables(variables);
            return variables;
        }

        /// <summary>
        /// Gets variables from the evaluatable object.
        /// </summary>
        /// <returns>Returns a dictionary containing variables from the evaluatable object.</returns>
        public static Dictionary<string, float> GetOtherObjectVariables(this IEvaluatable variableContainer)
        {
            var variables = new Dictionary<string, float>();
            variableContainer.SetOtherObjectVariables(variables);
            return variables;
        }

        /// <summary>
        /// Gets functions from the evaluatable object.
        /// </summary>
        /// <returns>Returns a dictionary containing functionss from the evaluatable object.</returns>
        public static Dictionary<string, MathFunction> GetObjectFunctions(this IEvaluatable variableContainer)
        {
            var functions = new Dictionary<string, MathFunction>();
            variableContainer.SetObjectFunctions(functions);
            return functions;
        }

        /// <summary>
        /// Gets the parent toggle value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent toggle to get.</param>
        /// <returns>Returns a parent toggle value.</returns>
        public static bool GetParentType(this IParentable parentable, int index) => parentable.ParentType[index] == '1';

        /// <summary>
        /// Sets the parent toggle value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">The new value to set.</param>
        public static void SetParentType(this IParentable parentable, int index, bool val)
        {
            var stringBuilder = new StringBuilder(parentable.ParentType);
            stringBuilder[index] = (val ? '1' : '0');
            parentable.ParentType = stringBuilder.ToString();
            CoreHelper.Log($"Set Parent Type: {parentable.ParentType}");
        }

        /// <summary>
        /// Gets the parent delay value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent delay to get.</param>
        /// <returns>Returns a parent delay value.</returns>
        public static float GetParentOffset(this IParentable parentable, int index) => parentable.ParentOffsets.TryGetAt(index, out float offset) ? offset : 0f;

        /// <summary>
        /// Sets the parent delay value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">the new value to set.</param>
        public static void SetParentOffset(this IParentable parentable, int index, float val)
        {
            if (parentable.ParentOffsets.InRange(index))
                parentable.ParentOffsets[index] = val;
        }

        /// <summary>
        /// Gets the parent additive value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent additive to get.</param>
        /// <returns>Returns a parent additive value.</returns>
        public static bool GetParentAdditive(this IParentable parentable, int index) => parentable.ParentAdditive[index] == '1';

        /// <summary>
        /// Sets the parent additive value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">The new value to set.</param>
        public static void SetParentAdditive(this IParentable parentable, int index, bool val)
        {
            var stringBuilder = new StringBuilder(parentable.ParentAdditive);
            stringBuilder[index] = val ? '1' : '0';
            parentable.ParentAdditive = stringBuilder.ToString();
            CoreHelper.Log($"Set Parent Additive: {parentable.ParentAdditive}");
        }

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="beatmapObjectToParentTo">Object to try parenting to.</param>
        /// <param name="renderParent">If parent editor should render.</param>
        /// <returns>Returns true if the <see cref="BeatmapObject"/> was successfully parented, otherwise returns false.</returns>
        public static bool TrySetParent(this IParentable parentable, BeatmapObject beatmapObjectToParentTo, bool renderParent = true) => parentable.TrySetParent(beatmapObjectToParentTo.id, renderParent);

        /// <summary>
        /// Sets the parent of the object.
        /// </summary>
        /// <param name="parentable">Parentable reference.</param>
        /// <param name="beatmapObjectToParentTo">Beatmap Object to parent <paramref name="parentable"/> to.</param>
        /// <param name="renderParent">If the editor should render.</param>
        public static void SetParent(this IParentable parentable, string beatmapObjectToParentTo, bool renderParent = true) => parentable.TrySetParent(beatmapObjectToParentTo, renderParent);

        /// <summary>
        /// Sets the parent of the object.
        /// </summary>
        /// <param name="parentable">Parentable reference.</param>
        /// <param name="beatmapObjectToParentTo">Beatmap Object to parent <paramref name="parentable"/> to.</param>
        /// <param name="renderParent">If the editor should render.</param>
        public static void SetParent(this IParentable parentable, BeatmapObject beatmapObjectToParentTo, bool renderParent = true) => parentable.TrySetParent(beatmapObjectToParentTo.id, renderParent);

        /// <summary>
        /// Checks if another object can be parented to this object.
        /// </summary>
        /// <param name="obj">Object to check the parent compatibility of.</param>
        /// <returns>Returns true if <paramref name="obj"/> can be parented to <paramref name="parentable"/>.</returns>
        public static bool CanParent(this IParentable parentable, BeatmapObject obj)
        {
            if (string.IsNullOrEmpty(obj.Parent))
                return true;

            return parentable.CanParent(obj, GameData.Current.beatmapObjects);
        }

        /// <summary>
        /// Gets the parent runtime of the object.
        /// </summary>
        /// <param name="reference">Modifier object reference.</param>
        /// <returns>Returns the parent runtime.</returns>
        public static RTLevelBase GetParentRuntime(this IModifierReference reference) => (reference.ParentRuntime ?? RTLevel.Current);

        /// <summary>
        /// Gets the parent runtime of the object.
        /// </summary>
        /// <param name="reference">Runtime object reference.</param>
        /// <returns>Returns the parent runtime.</returns>
        public static RTLevelBase GetParentRuntime(this IRTObject reference) => (reference.ParentRuntime ?? RTLevel.Current);

        /// <summary>
        /// Gets a <see cref="ObjectTransform"/> from a transformable object.
        /// </summary>
        /// <param name="transformable">Transformable reference.</param>
        /// <returns>Returns a transform cache.</returns>
        public static ObjectTransform GetObjectTransform(this ITransformable transformable) => new ObjectTransform(transformable.GetFullPosition(), transformable.GetFullScale(), transformable.GetFullRotation(true).z);

        /// <summary>
        /// Gets all transformables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of transformables.</returns>
        public static IEnumerable<ITransformable> GetTransformables(this IBeatmap beatmap)
        {
            foreach (var beatmapObject in beatmap.BeatmapObjects)
                yield return beatmapObject;
            foreach (var backgroundObject in beatmap.BackgroundObjects)
                yield return backgroundObject;
            foreach (var prefabObject in beatmap.PrefabObjects)
                yield return prefabObject;
        }

        /// <summary>
        /// Gets all modifyables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of modifyables.</returns>
        public static IEnumerable<IModifyable> GetModifyables(this IBeatmap beatmap)
        {
            foreach (var beatmapObject in beatmap.BeatmapObjects)
                yield return beatmapObject;
            foreach (var backgroundObject in beatmap.BackgroundObjects)
                yield return backgroundObject;
            foreach (var prefabObject in beatmap.PrefabObjects)
                yield return prefabObject;
        }

        /// <summary>
        /// Gets all prefabables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of prefabables.</returns>
        public static List<IPrefabable> GetPrefabablesList(this IBeatmap beatmap)
        {
            var prefabables = new List<IPrefabable>();

            prefabables.AddRange(beatmap.BeatmapObjects);
            prefabables.AddRange(beatmap.BackgroundObjects);
            prefabables.AddRange(beatmap.BackgroundLayers);
            prefabables.AddRange(beatmap.PrefabObjects);
            prefabables.AddRange(beatmap.Prefabs);

            return prefabables;
        }

        /// <summary>
        /// Gets all prefabables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of prefabables.</returns>
        public static IEnumerable<IPrefabable> GetPrefabables(this IBeatmap beatmap)
        {
            foreach (var beatmapObject in beatmap.BeatmapObjects)
                yield return beatmapObject;
            foreach (var backgroundObject in beatmap.BackgroundObjects)
                yield return backgroundObject;
            foreach (var backgroundLayer in beatmap.BackgroundLayers)
                yield return backgroundLayer;
            foreach (var prefabObject in beatmap.PrefabObjects)
                yield return prefabObject;
            foreach (var prefab in beatmap.Prefabs)
                yield return prefab;
        }

        /// <summary>
        /// Gets all parentables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of parentables.</returns>
        public static IEnumerable<IParentable> GetParentables(this IBeatmap beatmap)
        {
            foreach (var beatmapObject in beatmap.BeatmapObjects)
                yield return beatmapObject;
            foreach (var prefabObject in beatmap.PrefabObjects)
                yield return prefabObject;
        }

        /// <summary>
        /// Gets all editables from a package.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        /// <returns>Returns a collection of editables.</returns>
        public static IEnumerable<IEditable> GetEditables(this IBeatmap beatmap)
        {
            foreach (var beatmapObject in beatmap.BeatmapObjects)
                yield return beatmapObject;
            foreach (var backgroundObject in beatmap.BackgroundObjects)
                yield return backgroundObject;
            foreach (var prefabObject in beatmap.PrefabObjects)
                yield return prefabObject;
        }

        /// <summary>
        /// Copies parent data from another parentable object.
        /// </summary>
        /// <param name="orig">Parentable object to copy and apply from.</param>
        public static void CopyParentData(this IParentable parentable, IParentable orig)
        {
            parentable.Parent = orig.Parent;
            parentable.ParentType = orig.ParentType;
            parentable.ParentOffsets = orig.ParentOffsets.Copy();
            parentable.ParentAdditive = orig.ParentAdditive;
            parentable.ParentParallax = orig.ParentParallax.Copy();
            parentable.ParentDesync = orig.ParentDesync;
            parentable.ParentDesyncOffset = orig.ParentDesyncOffset;
        }

        /// <summary>
        /// Copies parent data from another modifyable object.
        /// </summary>
        /// <typeparam name="T">Type of the modifyable.</typeparam>
        /// <param name="orig">Modifyable object to copy and apply from.</param>
        public static void CopyModifyableData(this IModifyable modifyable, IModifyable orig)
        {
            modifyable.Tags = orig.Tags != null && !orig.Tags.IsEmpty() ? orig.Tags.Clone() : new List<string>();
            modifyable.IgnoreLifespan = orig.IgnoreLifespan;
            modifyable.OrderModifiers = orig.OrderModifiers;

            modifyable.Modifiers.Clear();
            for (int i = 0; i < orig.Modifiers.Count; i++)
                modifyable.Modifiers.Add(orig.Modifiers[i].Copy());
        }

        /// <summary>
        /// Reads <see cref="IShapeable"/> data from JSON.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        /// <param name="jn">JSON to read from.</param>
        public static void ReadShapeJSON(this IShapeable shapeable, JSONNode jn)
        {
            if (jn["s"] != null)
                shapeable.Shape = jn["s"].AsInt;

            if (jn["shape"] != null)
                shapeable.Shape = jn["shape"].AsInt;

            if (jn["so"] != null)
                shapeable.ShapeOption = jn["so"].AsInt;

            if (jn["csp"] != null)
                shapeable.Polygon = PolygonShape.Parse(jn["csp"]);

            if (jn["text"] != null)
                shapeable.Text = ((string)jn["text"]).Replace("{{colon}}", ":");

            if (jn["ata"] != null)
                shapeable.AutoTextAlign = jn["ata"];
        }

        /// <summary>
        /// Writes <see cref="IShapeable"/> data to JSON.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        /// <param name="jn">JSON to write to.</param>
        public static void WriteShapeJSON(this IShapeable shapeable, JSONNode jn)
        {
            if (shapeable.Shape != 0)
                jn["s"] = shapeable.Shape;

            if (shapeable.ShapeOption != 0)
                jn["so"] = shapeable.ShapeOption;

            if (shapeable.ShapeType == ShapeType.Polygon && shapeable.Polygon != null)
                jn["csp"] = shapeable.Polygon.ToJSON();

            if (!string.IsNullOrEmpty(shapeable.Text))
                jn["text"] = shapeable.Text;

            if (shapeable.AutoTextAlign)
                jn["ata"] = shapeable.AutoTextAlign;
        }

        /// <summary>
        /// If <see cref="IShapeable"/> data should be serialized to JSON.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        /// <returns>Returns true if all the shapeable's values are not default, otherwise returns false.</returns>
        public static bool ShouldSerializeShape(this IShapeable shapeable) =>
            shapeable.Shape != 0 ||
            shapeable.ShapeOption != 0 ||
            !string.IsNullOrEmpty(shapeable.Text) ||
            shapeable.AutoTextAlign;

        /// <summary>
        /// Copies <see cref="IShapeable"/> data from another <see cref="IShapeable"/>.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        /// <param name="orig">Original to copy data from.</param>
        public static void CopyShapeableData(this IShapeable shapeable, IShapeable orig)
        {
            shapeable.Shape = orig.Shape;
            shapeable.ShapeOption = orig.ShapeOption;
            shapeable.Polygon = orig.Polygon.Copy();
            shapeable.Text = orig.Text;
            shapeable.AutoTextAlign = orig.AutoTextAlign;
        }

        /// <summary>
        /// Reads <see cref="IParentable"/> data from JSON.
        /// </summary>
        /// <param name="parentable">Parentable object reference.</param>
        /// <param name="jn">JSON to read from.</param>
        public static void ReadParentJSON(this IParentable parentable, JSONNode jn)
        {
            if (jn["p"] != null)
                parentable.Parent = jn["p"];

            if (jn["pd"] != null && !string.IsNullOrEmpty(parentable.Parent))
                parentable.ParentDesync = jn["pd"].AsBool;

            if (jn["desync"] != null && !string.IsNullOrEmpty(parentable.Parent))
                parentable.ParentDesync = jn["desync"].AsBool;

            if (jn["pt"] != null)
                parentable.ParentType = jn["pt"];

            if (jn["po"] != null)
                for (int i = 0; i < parentable.ParentOffsets.Length; i++)
                    if (jn["po"].Count > i && jn["po"][i] != null)
                        parentable.ParentOffsets[i] = jn["po"][i].AsFloat;

            if (jn["ps"] != null)
            {
                for (int i = 0; i < parentable.ParentParallax.Length; i++)
                {
                    if (jn["ps"].Count > i && jn["ps"][i] != null)
                        parentable.ParentParallax[i] = jn["ps"][i].AsFloat;
                }
            }

            if (jn["pa"] != null)
                parentable.ParentAdditive = jn["pa"];
        }

        /// <summary>
        /// Writes <see cref="IParentable"/> data to JSON.
        /// </summary>
        /// <param name="parentable">Parentable object reference.</param>
        /// <param name="jn">JSON to write to.</param>
        public static void WriteParentJSON(this IParentable parentable, JSONNode jn)
        {
            if (!string.IsNullOrEmpty(parentable.Parent))
                jn["p"] = parentable.Parent;

            if (parentable.ParentDesync && !string.IsNullOrEmpty(parentable.Parent))
                jn["pd"] = parentable.ParentDesync;

            if (parentable.ParentType != BeatmapObject.DEFAULT_PARENT_TYPE)
                jn["pt"] = parentable.ParentType;

            if (parentable.ParentOffsets.Any(x => x != 0f))
                for (int i = 0; i < parentable.ParentOffsets.Length; i++)
                    jn["po"][i] = parentable.ParentOffsets[i];

            if (parentable.ParentAdditive != BeatmapObject.DEFAULT_PARENT_ADDITIVE)
                jn["pa"] = parentable.ParentAdditive;

            if (parentable.ParentParallax.Any(x => x != 1f))
                for (int i = 0; i < parentable.ParentParallax.Length; i++)
                    jn["ps"][i] = parentable.ParentParallax[i];
        }

        /// <summary>
        /// Reads <see cref="IPrefabable"/> data from JSON.
        /// </summary>
        /// <param name="prefabable">Prefabable object reference.</param>
        /// <param name="jn">JSON to read from.</param>
        public static void ReadPrefabJSON(this IPrefabable prefabable, JSONNode jn)
        {
            if (jn["piid"] != null)
                prefabable.PrefabInstanceID = jn["piid"];

            if (jn["pid"] != null)
                prefabable.PrefabID = jn["pid"];
        }

        /// <summary>
        /// Writes <see cref="IPrefabable"/> data to JSON.
        /// </summary>
        /// <param name="prefabable">Prefabable object reference.</param>
        /// <param name="jn">JSON to write to.</param>
        public static void WritePrefabJSON(this IPrefabable prefabable, JSONNode jn)
        {
            var prefabID = prefabable.PrefabID;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pid"] = prefabID;

            var prefabInstanceID = prefabable.PrefabInstanceID;
            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["piid"] = prefabInstanceID;
        }

        /// <summary>
        /// Reads <see cref="IModifyable{T}"/> data from JSON.
        /// </summary>
        /// <typeparam name="T">Type of the modifyable.</typeparam>
        /// <param name="modifyable">Modifyable object reference.</param>
        /// <param name="jn">JSON to read from.</param>
        /// <param name="defaultModifiers">Default modifiers list to validate from.</param>
        public static void ReadModifiersJSON(this IModifyable modifyable, JSONNode jn, bool handleOutdatedPageModifiers = false)
        {
            modifyable.Tags.Clear();
            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    modifyable.Tags.Add(jn["tags"][i]);

            if (jn["iglif"] != null)
                modifyable.IgnoreLifespan = jn["iglif"].AsBool;

            if (jn["ordmod"] != null)
                modifyable.OrderModifiers = jn["ordmod"].AsBool;

            modifyable.Modifiers.Clear();
            if (handleOutdatedPageModifiers)
            {
                var modifiersCount = jn["modifiers"].Count;
                for (int i = 0; i < modifiersCount; i++)
                {
                    var modifierJN = jn["modifiers"][i];

                    // this is for backwards compatibility due to BG modifiers having multiple lists previously.
                    if (modifierJN.IsArray)
                    {
                        //var wasOrderModifiers = orderModifiers;

                        modifyable.OrderModifiers = true;
                        var list = new List<Modifier>();
                        for (int j = 0; j < jn["modifiers"][i].Count; j++)
                        {
                            var modifier = Modifier.Parse(jn["modifiers"][i][j]);
                            if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.inst.modifiers))
                                list.Add(modifier);
                        }

                        //if (!wasOrderModifiers)
                        list.Sort((a, b) => a.type.CompareTo(b.type));
                        //else if (i != modifiersCount - 1 && ModifiersManager.defaultBackgroundObjectModifiers.TryFind(x => x.Name == "break", out ModifierBase breakModifierBase) && breakModifierBase is Modifier<BackgroundObject> breakModifier)
                        //    list.Add(breakModifier.Copy(this));

                        modifyable.Modifiers.AddRange(list);
                    }
                    else
                    {
                        var modifier = Modifier.Parse(jn["modifiers"][i]);
                        if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.inst.modifiers))
                            modifyable.Modifiers.Add(modifier);
                    }
                }

                return;
            }

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                var modifier = Modifier.Parse(jn["modifiers"][i]);
                if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.inst.modifiers))
                    modifyable.Modifiers.Add(modifier);
            }
        }

        /// <summary>
        /// Writes <see cref="IModifyable{T}"/> data to JSON.
        /// </summary>
        /// <typeparam name="T">Type of the modifyable.</typeparam>
        /// <param name="modifyable">Modifyable object reference.</param>
        /// <param name="jn">JSON to write to.</param>
        public static void WriteModifiersJSON(this IModifyable modifyable, JSONNode jn)
        {
            if (modifyable.Tags != null)
                for (int i = 0; i < modifyable.Tags.Count; i++)
                    jn["tags"][i] = modifyable.Tags[i];

            if (modifyable.IgnoreLifespan)
                jn["iglif"] = modifyable.IgnoreLifespan;
            if (modifyable.OrderModifiers)
                jn["ordmod"] = modifyable.OrderModifiers;
            for (int i = 0; i < modifyable.Modifiers.Count; i++)
                jn["modifiers"][i] = modifyable.Modifiers[i].ToJSON();
        }

        #region Animation

        /// <summary>
        /// Updates all animation controllers.
        /// </summary>
        public static void UpdateAnimations(this IAnimationController animationController)
        {
            var animations = animationController.Animations;
            var speed = animationController.Speed;
            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].playing)
                {
                    animations[i].globalSpeed = speed;
                    animations[i].Update();
                }
            }
        }

        /// <summary>
        /// Adds an animation to the update list and plays it.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        public static void Play(this IAnimationController animationController, RTAnimation animation)
        {
            var animations = animationController.Animations;
            if (!animations.Has(x => x.id == animation.id))
                animations.Add(animation);
            animation.Start();
        }

        /// <summary>
        /// Stops all the animations in the animation list and clears it.
        /// </summary>
        public static void StopAll(this IAnimationController animationController)
        {
            var animations = animationController.Animations;
            for (int i = 0; i < animations.Count; i++)
            {
                var anim = animations[i];
                anim.Stop();
            }
            animations.Clear();
        }

        #region Remove

        /// <summary>
        /// Removes all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animations to remove.</param>
        public static void RemoveName(this IAnimationController animationController, string name) => animationController.Animations.RemoveAll(x => x.name == name);

        /// <summary>
        /// Removes all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animations to remove.</param>
        public static void Remove(this IAnimationController animationController, string id) => animationController.Animations.RemoveAll(x => x.id == id);

        /// <summary>
        /// Removes all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        public static void Remove(this IAnimationController animationController, Predicate<RTAnimation> predicate) => animationController.Animations.RemoveAll(predicate);

        #endregion

        #region Search methods

        /// <summary>
        /// Finds an animation with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public static RTAnimation FindAnimationByName(this IAnimationController animationController, string name) => animationController.Animations.Find(x => x.name == name);

        /// <summary>
        /// Finds an animation with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public static RTAnimation FindAnimation(this IAnimationController animationController, string id) => animationController.Animations.Find(x => x.id == id);

        /// <summary>
        /// Finds a list of all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animations to find.</param>
        /// <returns>Returns a list of animations.</returns>
        public static List<RTAnimation> FindAnimationsByName(this IAnimationController animationController, string name) => animationController.Animations.FindAll(x => x.name == name);

        /// <summary>
        /// Finds a list of all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animations to find.</param>
        /// <returns>Returns a list of animations.</returns>
        public static List<RTAnimation> FindAnimations(this IAnimationController animationController, string id) => animationController.Animations.FindAll(x => x.id == id);

        /// <summary>
        /// Finds a list of all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animation to match.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public static RTAnimation FindAnimation(this IAnimationController animationController, Predicate<RTAnimation> predicate) => animationController.Animations.Find(predicate);

        /// <summary>
        /// Finds a list of all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        /// <returns>Returns a list of animations.</returns>
        public static List<RTAnimation> FindAnimations(this IAnimationController animationController, Predicate<RTAnimation> predicate) => animationController.Animations.FindAll(predicate);

        /// <summary>
        /// Tries to find an animation with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public static bool TryFindAnimationByName(this IAnimationController animationController, string name, out RTAnimation animation) => animationController.Animations.TryFind(x => x.name == name, out animation);

        /// <summary>
        /// Tries to find an animation with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public static bool TryFindAnimation(this IAnimationController animationController, string id, out RTAnimation animation) => animationController.Animations.TryFind(x => x.id == id, out animation);

        /// <summary>
        /// Tries to find all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public static bool TryFindAnimationsByName(this IAnimationController animationController, string name, out List<RTAnimation> animations) => animationController.Animations.TryFindAll(x => x.name == name, out animations);

        /// <summary>
        /// Tries to find all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public static bool TryFindAnimations(this IAnimationController animationController, string id, out List<RTAnimation> animations) => animationController.Animations.TryFindAll(x => x.id == id, out animations);

        /// <summary>
        /// Tries to find an animation via a predicate.
        /// </summary>
        /// <param name="predicate">Animation to match.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public static bool TryFindAnimation(this IAnimationController animationController, Predicate<RTAnimation> predicate, out RTAnimation animation) => animationController.Animations.TryFind(predicate, out animation);

        /// <summary>
        /// Tries to find all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public static bool TryFindAnimations(this IAnimationController animationController, Predicate<RTAnimation> predicate, out List<RTAnimation> animations) => animationController.Animations.TryFindAll(predicate, out animations);

        #endregion

        #endregion

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
