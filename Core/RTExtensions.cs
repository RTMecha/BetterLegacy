using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using LSFunctions;

using ILMath;

using SimpleJSON;
using SteamworksFacepunch.Ugc;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

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
        /// <returns>Returns true if the list contains elements.</returns>
        public static bool IsEmpty<T>(this List<T> list) => list.Count < 1;

        /// <summary>
        /// Checks if a queue contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Queue{T}"/>.</typeparam>
        /// <returns>Returns true if the queue contains elements.</returns>
        public static bool IsEmpty<T>(this Queue<T> queue) => queue.Count < 1;

        /// <summary>
        /// Checks if an array contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <returns>Returns true if the array contains elements.</returns>
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
        public static JSONNode Get(this JSONNode jn, int index, string key) => jn.IsArray ? jn[index] : jn[key];

        public static JSONNode ToJSONArray(this Vector2Int vector2)
        {
            var jn = JSON.Parse("{}");
            jn[0] = vector2.x;
            jn[1] = vector2.y;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector2 vector2)
        {
            var jn = JSON.Parse("{}");
            jn[0] = vector2.x;
            jn[1] = vector2.y;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector3Int vector3)
        {
            var jn = JSON.Parse("{}");
            jn[0] = vector3.x;
            jn[1] = vector3.y;
            jn[2] = vector3.z;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector3 vector3)
        {
            var jn = JSON.Parse("{}");
            jn[0] = vector3.x;
            jn[1] = vector3.y;
            jn[2] = vector3.z;
            return jn;
        }
        
        public static JSONNode ToJSONArray(this Vector4 vector4)
        {
            var jn = JSON.Parse("{}");
            jn[0] = vector4.x;
            jn[1] = vector4.y;
            jn[2] = vector4.z;
            jn[3] = vector4.w;
            return jn;
        }

        public static JSONNode ToJSON(this Vector2Int vector2)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector2.x.ToString();
            jn["y"] = vector2.y.ToString();

            return jn;
        }
        
        public static JSONNode ToJSON(this Vector2 vector2, bool toString = true)
        {
            var jn = JSON.Parse("{}");

            jn["x"] = vector2.x;
            jn["y"] = vector2.y;

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

        public static Rank GetEnum(this DataManager.LevelRank levelRank) => Enum.TryParse(levelRank.name, out Rank rank) ? rank : Rank.Null;

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
        public static Prefab GetPrefab(this IPrefabable instance) => GameData.Current.prefabs.Find(x => x.id == instance.PrefabID);

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
        public static PrefabObject GetPrefabObject(this IPrefabable instance) => GameData.Current.prefabObjects.Find(x => x.id == instance.PrefabInstanceID);

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
