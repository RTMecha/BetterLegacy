using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

using LSFunctions;

using InControl;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;

namespace BetterLegacy.Core.Helpers
{
    public static class CoreHelper
    {
        #region Properties

        /// <summary>
        /// The multiplied screen scale, multiplied by a base resolution of 1920. To be used for fixing UI scale issues.
        /// </summary>
        public static float ScreenScale => Screen.width / 1920f;

        /// <summary>
        /// Inverses the Screen Scale.
        /// </summary>
        public static float ScreenScaleInverse => 1f / ScreenScale;

        /// <summary>
        /// For checking if the user is in the editor preview or in game.
        /// </summary>
        public static bool InEditorPreview => !EditorManager.inst || !EditorManager.inst.isEditing;

        /// <summary>
        /// For checking if the user is just in the editor.
        /// </summary>
        public static bool IsEditing => EditorManager.inst && EditorManager.inst.isEditing;

        /// <summary>
        /// If the user is in the editor.
        /// </summary>
        public static bool InEditor => EditorManager.inst;

        /// <summary>
        /// If the user is in game. Can include editor or arcade.
        /// </summary>
        public static bool InGame => GameManager.inst;

        /// <summary>
        /// If InterfaceController exists.
        /// </summary>
        public static bool InMenu => InterfaceManager.inst.CurrentInterface;

        /// <summary>
        /// If the player is in the Classic Arrhythmia story mode.
        /// </summary>
        public static bool InStory { get; set; }

        /// <summary>
        /// The currently open level.
        /// </summary>
        public static Level CurrentLevel => InEditor ? EditorLevelManager.inst.CurrentLevel : LevelManager.CurrentLevel;

        /// <summary>
        /// If the game is loading.
        /// </summary>
        public static bool Loading => GameManager.inst && GameManager.inst.gameState == GameManager.State.Loading;

        /// <summary>
        /// If the game is parsing.
        /// </summary>
        public static bool Parsing => GameManager.inst && GameManager.inst.gameState == GameManager.State.Parsing;

        /// <summary>
        /// If the game is playing.
        /// </summary>
        public static bool Playing => GameManager.inst && GameManager.inst.gameState == GameManager.State.Playing;

        /// <summary>
        /// If the game is reversing to checkpoint.
        /// </summary>
        public static bool Reversing => GameManager.inst && GameManager.inst.gameState == GameManager.State.Reversing;

        /// <summary>
        /// If the game is paused.
        /// </summary>
        public static bool Paused => GameManager.inst && GameManager.inst.gameState == GameManager.State.Paused;

        /// <summary>
        /// If the game is finished.
        /// </summary>
        public static bool Finished => GameManager.inst && GameManager.inst.gameState == GameManager.State.Finish;

        /// <summary>
        /// The normalized time for relative animation consistency.
        /// </summary>
        public static float TimeFrame => Time.deltaTime * 300f;

        /// <summary>
        /// Takes the current pitch and always makes sure it's a valid value to be used for DelayTracker components.
        /// </summary>
        public static float ForwardPitch
        {
            get
            {
                float pitch = AudioManager.inst.CurrentAudioSource.pitch;
                if (pitch < 0f)
                    pitch = -pitch;

                if (pitch == 0f)
                    pitch = 0.0001f;

                return pitch;
            }
        }

        /// <summary>
        /// Gets the current interpolated theme or if the user is in the theme editor, the preview theme.
        /// </summary>
        public static BeatmapTheme CurrentBeatmapTheme => InEditor && EventEditor.inst.showTheme ? RTThemeEditor.inst.PreviewTheme : ThemeManager.inst.Current;

        /// <summary>
        /// Gets the current resolution as a Vector2Int based on Core Config's resolution value.
        /// </summary>
        public static Vector2 CurrentResolution => CoreConfig.Instance.Resolution.Value.Resolution;

        /// <summary>
        /// Gets a resolution from the resolution list.
        /// </summary>
        /// <param name="resolution">The resolution index.</param>
        /// <returns>Returns a Vector2Int representing a resolution.</returns>
        public static Vector2 GetResolution(int resolution) => CustomEnumHelper.GetValue<ResolutionType>(resolution).Resolution;

        #endregion

        #region Unity

        /// <summary>
        /// If the user is interacting with an InputField.
        /// </summary>
        public static bool IsUsingInputField { get; set; }

        /// <summary>
        /// Tries to find a <see cref="GameObject"/>.
        /// </summary>
        /// <param name="find"><see cref="GameObject"/> to find. To search through a chain, do "object 1/object 2/object 3"</param>
        /// <param name="result">Output <see cref="GameObject"/>.</param>
        /// <returns>Returns true if a <see cref="GameObject"/> was found, otherwise returns false.</returns>
        public static bool TryFind(string find, out GameObject result)
        {
            result = GameObject.Find(find);
            return result;
        }

        /// <summary>
        /// Destroys a Unity Object from anywhere.
        /// </summary>
        /// <param name="obj">Unity Object to destroy.</param>
        /// <param name="instant">If object should destroy instantly.</param>
        /// <param name="t">The delay to destroy the object at if instant is off.</param>
        public static void Destroy(UnityEngine.Object obj) => UnityEngine.Object.Destroy(obj);

        /// <summary>
        /// Destroys a Unity Object.
        /// </summary>
        /// <param name="obj">Unity Object to destroy.</param>
        /// <param name="instant">If object should destroy instantly.</param>
        public static void Destroy(UnityEngine.Object obj, bool instant)
        {
            if (instant)
                UnityEngine.Object.DestroyImmediate(obj);
            else
                UnityEngine.Object.Destroy(obj);
        }

        /// <summary>
        /// Destroys a Unity Object from anywhere, includes instant and delay time.
        /// </summary>
        /// <param name="obj">Unity Object to destroy.</param>
        /// <param name="t">The delay to destroy the object at if instant is off.</param>
        public static void Destroy(UnityEngine.Object obj, float t) => UnityEngine.Object.Destroy(obj, t);

        /// <summary>
        /// Destroys multiple Unity Objects.
        /// </summary>
        /// <param name="objects">Unity Objects to destroy.</param>
        public static void Destroy(params UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                Destroy(objects[i]);
        }

        /// <summary>
        /// Destroys multiple Unity Objects.
        /// </summary>
        /// <param name="t">The delay to destroy the object at if instant is off.</param>
        /// <param name="objects">Unity Objects to destroy.</param>
        public static void Destroy(float t, params UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                Destroy(objects[i], t);
        }

        /// <summary>
        /// Destroys multiple Unity Objects.
        /// </summary>
        /// <param name="instant">If object should destroy instantly.</param>
        /// <param name="objects">Unity Objects to destroy.</param>
        public static void Destroy(bool instant, params UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                Destroy(objects[i], instant);
        }

        /// <summary>
        /// Deletes a <see cref="GameObject"/> by removing it from its parent and destroying it.
        /// </summary>
        /// <param name="gameObject">GameObject to destroy.</param>
        public static void Delete(GameObject gameObject)
        {
            if (!gameObject)
                return;

            gameObject.transform.SetParent(null);
            UnityEngine.Object.Destroy(gameObject);
        }

        /// <summary>
        /// Deletes a <see cref="Transform"/> by removing it from its parent and destroying it.
        /// </summary>
        /// <param name="transform">Transform to destroy.</param>
        public static void Delete(Transform transform)
        {
            if (transform)
                Delete(transform.gameObject);
        }

        /// <summary>
        /// Deletes a <see cref="Component"/> by removing it from its parent and destroying it.
        /// </summary>
        /// <param name="component">Component to destroy.</param>
        public static void Delete(Component component)
        {
            if (component)
                Delete(component.gameObject);
        }

        /// <summary>
        /// Deletes several <see cref="GameObject"/>s by removing it from its parent and destroying it.
        /// </summary>
        /// <param name="objects">GameObjects to destroy.</param>
        public static void Delete(params GameObject[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                Delete(objects[i]);
        }

        /// <summary>
        /// Removes all children from the transform and destroys them. This is done due to Unity's Destroy method not working in some cases.
        /// </summary>
        /// <param name="transform">Transform to delete the children of.</param>
        public static void DestroyChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Removes all children from the transform and destroys them. This is done due to Unity's Destroy method not working in some cases.
        /// </summary>
        /// <param name="transform">Transform to delete the children of.</param>
        /// <param name="predicate">If a match is found, delete the child.</param>
        public static void DestroyChildren(Transform transform, Predicate<GameObject> predicate)
        {
            var listToDestroy = new List<GameObject>();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (predicate(child.gameObject))
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
            }
            foreach (var child in listToDestroy)
                Destroy(child);
        }

        /// <summary>
        /// Removes all children from the transform and destroys them. This is done due to Unity's Destroy method not working in some cases.
        /// </summary>
        /// <param name="transform">Transform to delete the children of.</param>
        /// <param name="startIndex">Start sibling index to delete from.</param>
        /// <param name="endIndex">End sibling index to delete to.</param>
        public static void DestroyChildren(Transform transform, int startIndex, int endIndex)
        {
            for (int i = endIndex; i >= startIndex; i--)
            {
                var child = transform.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Sets the game object active / inactive.
        /// </summary>
        /// <param name="gameObject">Game object to set the active state of.</param>
        /// <param name="active">Active state to set.</param>
        public static void SetGameObjectActive(GameObject gameObject, bool active)
        {
            if (gameObject)
                gameObject.SetActive(active);
        }

        /// <summary>
        /// Removes unnecessary animator from selectables, due to it getting in the way of editor themes.
        /// </summary>
        /// <param name="selectable">Selectable element to remove an animator from.</param>
        public static void RemoveAnimator(Selectable selectable)
        {
            if (!selectable)
                return;

            Destroy(selectable.GetComponent<Animator>(), true);
            selectable.transition = Selectable.Transition.ColorTint;
        }

        public static IEnumerator FixUIText()
        {
            var texts = Resources.FindObjectsOfTypeAll<Text>();
            var textArray = new string[texts.Length];

            for (int i = 0; i < texts.Length; i++)
            {
                textArray[i] = texts[i].text;
                texts[i].text = "";
            }

            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].text = textArray[i];
            }

            yield break;
        }

        /// <summary>
        /// Converts a string array to a Dropdown OptionData list.
        /// </summary>
        /// <param name="str">String array to convert.</param>
        /// <returns>Returns a list of <see cref="Dropdown.OptionData"/> based on the string array.</returns>
        public static List<Dropdown.OptionData> StringToOptionData(params string[] str) => str.Select(x => new Dropdown.OptionData(x)).ToList();

        public static KeyValuePair<List<Dropdown.OptionData>, List<bool>> ToDropdownData<T>() where T : struct
        {
            var options = new List<Dropdown.OptionData>();
            var disabledOptions = new List<bool>();
            var type = typeof(T);
            if (!type.IsEnum)
                return new KeyValuePair<List<Dropdown.OptionData>, List<bool>>(options, disabledOptions);

            var keyCodes = Enum.GetValues(type);

            for (int i = 0; i < keyCodes.Length; i++)
            {
                var str = Enum.GetName(type, i) ?? "Invalid Value";

                options.Add(new Dropdown.OptionData(str));
                disabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(type, i)));
            }

            return new KeyValuePair<List<Dropdown.OptionData>, List<bool>>(options, disabledOptions);
        }

        public static List<Dropdown.OptionData> ToOptionData<T>() where T : struct
        {
            var options = new List<Dropdown.OptionData>();
            var type = typeof(T);
            if (!type.IsEnum)
                return new List<Dropdown.OptionData>();

            var keyCodes = Enum.GetValues(type);

            for (int i = 0; i < keyCodes.Length; i++)
            {
                var str = Enum.GetName(type, i) ?? "Invalid Value";

                options.Add(new Dropdown.OptionData(str));
            }

            return options;
        }

        public static void CreateCollider(PolygonCollider2D polygonCollider, Mesh mesh)
        {
            // Stop if no polygon collider nor mesh filter exists
            if (!polygonCollider || !mesh)
                return;

            // Get triangles and vertices from mesh
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

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

        /// <summary>
        /// Gets the size of a box collider based on a texts' size.
        /// </summary>
        /// <param name="collider">Box Collider to set the size to.</param>
        /// <param name="text">TMP Text to get the size from.</param>
        public static void GetColliderSize(BoxCollider2D collider, TMPro.TextMeshPro text) => collider.size = !text ? Vector2.one : text.GetRenderedValues();

        /// <summary>
        /// Gets the size of a box collider based on a texts' size.
        /// </summary>
        /// <param name="collider">Box Collider to set the size to.</param>
        /// <param name="spriteRenderer">Sprite Renderer to get the size from.</param>
        public static void GetColliderSize(BoxCollider2D collider, SpriteRenderer spriteRenderer) => collider.size = !spriteRenderer || !spriteRenderer.sprite || !spriteRenderer.sprite.texture ? Vector2.one : new Vector2(spriteRenderer.sprite.texture.width / 100f, spriteRenderer.sprite.texture.height / 100f);

        #endregion

        #region Logging

        /// <summary>
        /// For logging with a className.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message) => Debug.Log($"{LegacyPlugin.className}{message}");

        /// <summary>
        /// For logging with a className. Message is logged as a warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message) => Debug.LogWarning($"{LegacyPlugin.className}{message}");

        /// <summary>
        /// For logging with a className. Message is logged as an error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message) => Debug.LogError($"{LegacyPlugin.className}{message}");

        /// <summary>
        /// For logging an exception with a className. Message is logged as an error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void LogException(Exception exception) => LogError($"Exception: {exception}");

        /// <summary>
        /// Logs the initialization of an object with a provided class name.
        /// </summary>
        /// <param name="className">Class name to log.</param>
        public static void LogInit(string className) => Debug.Log($"{className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

        public static void LogSeparator() => Debug.Log("---------------------------------------------------------------------");

        public static void LogArray<T>(T[] array)
        {
            if (array == null)
                return;

            var sb = new System.Text.StringBuilder(LegacyPlugin.className);
            foreach (var item in array)
                sb.AppendLine(item.ToString());
            Debug.Log(sb.ToString());
        }
        
        public static void LogList<T>(List<T> list)
        {
            if (list == null)
                return;

            var sb = new System.Text.StringBuilder(LegacyPlugin.className);
            foreach (var item in list)
                sb.AppendLine(item.ToString());
            Debug.Log(sb.ToString());
        }

        public static void LogDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                return;

            var sb = new System.Text.StringBuilder(LegacyPlugin.className);
            foreach (var item in dictionary)
                sb.AppendLine(item.ToString());
            Debug.Log(sb.ToString());
        }

        #region Stopwatch

        public static System.Diagnostics.Stopwatch StartNewStopwatch() => System.Diagnostics.Stopwatch.StartNew();
        public static void StopAndLogStopwatch(System.Diagnostics.Stopwatch sw, string message = "")
        {
            sw.Stop();
            Log($"{(string.IsNullOrEmpty(message) ? message : message + "\n")}Time taken: {sw.Elapsed}");
        }

        public static void LogStopwatch(System.Diagnostics.Stopwatch sw) => Log($"Time: {sw.Elapsed}");

        #endregion

        #endregion

        #region Controls

        /// <summary>
        /// Gets the current pressed key.
        /// </summary>
        /// <returns>Returns the current pressed down key. If there is none, returns <see cref="KeyCode.None"/>.</returns>
        public static KeyCode GetKeyCodeDown()
        {
            var keyCodes = Enum.GetValues(typeof(KeyCode));
            for (int i = 0; i < keyCodes.Length; i++)
            {
                var name = Enum.GetName(typeof(KeyCode), i);
                if (!string.IsNullOrEmpty(name) && name.ToLower() != "none" && Input.GetKeyDown((KeyCode)i))
                    return (KeyCode)i;
            }

            return KeyCode.None;
        }

        /// <summary>
        /// Assigns both Keyboard and Controller to actions.
        /// </summary>
        /// <returns>MyGameActions with both Keyboard and Controller inputs.</returns>
        public static MyGameActions CreateWithBothBindings()
        {
            var myGameActions = new MyGameActions();

            // Controller
            myGameActions.Up.AddDefaultBinding(InputControlType.DPadUp);
            myGameActions.Up.AddDefaultBinding(InputControlType.LeftStickUp);
            myGameActions.Down.AddDefaultBinding(InputControlType.DPadDown);
            myGameActions.Down.AddDefaultBinding(InputControlType.LeftStickDown);
            myGameActions.Left.AddDefaultBinding(InputControlType.DPadLeft);
            myGameActions.Left.AddDefaultBinding(InputControlType.LeftStickLeft);
            myGameActions.Right.AddDefaultBinding(InputControlType.DPadRight);
            myGameActions.Right.AddDefaultBinding(InputControlType.LeftStickRight);
            myGameActions.Boost.AddDefaultBinding(InputControlType.RightTrigger);
            myGameActions.Boost.AddDefaultBinding(InputControlType.RightBumper);
            myGameActions.Boost.AddDefaultBinding(InputControlType.Action1);
            myGameActions.Boost.AddDefaultBinding(InputControlType.Action3);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action1);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action2);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action3);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action4);
            myGameActions.Pause.AddDefaultBinding(InputControlType.Command);
            myGameActions.Escape.AddDefaultBinding(InputControlType.Action2);
            myGameActions.Escape.AddDefaultBinding(InputControlType.Action4);

            // Keyboard
            myGameActions.Up.AddDefaultBinding(new Key[] { Key.UpArrow });
            myGameActions.Up.AddDefaultBinding(new Key[] { Key.W });
            myGameActions.Down.AddDefaultBinding(new Key[] { Key.DownArrow });
            myGameActions.Down.AddDefaultBinding(new Key[] { Key.S });
            myGameActions.Left.AddDefaultBinding(new Key[] { Key.LeftArrow });
            myGameActions.Left.AddDefaultBinding(new Key[] { Key.A });
            myGameActions.Right.AddDefaultBinding(new Key[] { Key.RightArrow });
            myGameActions.Right.AddDefaultBinding(new Key[] { Key.D });
            myGameActions.Boost.AddDefaultBinding(new Key[] { Key.Space });
            myGameActions.Boost.AddDefaultBinding(new Key[] { Key.Return });
            myGameActions.Boost.AddDefaultBinding(new Key[] { Key.Z });
            myGameActions.Boost.AddDefaultBinding(new Key[] { Key.X });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.Space });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.A });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.S });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.D });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.W });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.LeftArrow });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.RightArrow });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.DownArrow });
            myGameActions.Join.AddDefaultBinding(new Key[] { Key.UpArrow });
            myGameActions.Pause.AddDefaultBinding(new Key[] { Key.Escape });
            myGameActions.Escape.AddDefaultBinding(new Key[] { Key.Escape });
            return myGameActions;
        }

        #endregion

        #region Screenshot

        public static void TakeScreenshot()
        {
            string directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;
            RTFile.CreateDirectory(directory);

            var file = RTFile.CombinePaths(directory, DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + FileFormat.PNG.Dot());
            ScreenCapture.CaptureScreenshot(file, 1);
            Log($"Took Screenshot! - {file}");

            CoroutineHelper.StartCoroutine(ScreenshotNotification());
        }

        static IEnumerator ScreenshotNotification()
        {
            yield return CoroutineHelper.Seconds(0.1f);

            // In-Game Screenshot notification

            var scr = ScreenCapture.CaptureScreenshotAsTexture();

            SoundManager.inst.PlaySound(DefaultSounds.glitch);

            var uiCanvas = UIManager.GenerateUICanvas("Screenshot Canvas", null, true);

            var imageObj = Creator.NewUIObject("image", uiCanvas.GameObject.transform);

            imageObj.transform.AsRT().anchoredPosition = new Vector2(850f, -480f);
            imageObj.transform.AsRT().sizeDelta = new Vector2(scr.width / 10f, scr.height / 10f);

            var im = imageObj.AddComponent<Image>();
            im.sprite = Sprite.Create(scr, new Rect(0f, 0f, scr.width, scr.height), Vector2.zero);

            var textObj = Creator.NewUIObject("text", imageObj.transform);

            textObj.transform.AsRT().anchoredPosition = new Vector2(0f, 20f);
            textObj.transform.AsRT().sizeDelta = new Vector2(200f, 100f);

            var text = textObj.AddComponent<Text>();
            text.font = FontManager.inst.DefaultFont;
            text.text = "Took Screenshot!";

            var animation = new RTAnimation("Screenshot Notification");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Color>(new List<IKeyframe<Color>>
                {
                    new ColorKeyframe(0f, Color.white, Ease.Linear),
                    new ColorKeyframe(1.5f, new Color(1f, 1f, 1f, 0f), Ease.SineIn),
                }, color =>
                {
                    if (im)
                        im.color = color;
                    if (text)
                        text.color = color;
                }),
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, new Vector2(850f, -480f), Ease.Linear),
                    new Vector2Keyframe(1.5f, new Vector2(850f, -600f), Ease.BackIn)
                }, vector => imageObj.transform.AsRT().anchoredPosition = vector),
            };

            animation.onComplete = () =>
            {
                scr = null;

                if (uiCanvas.GameObject)
                    Destroy(uiCanvas.GameObject);
                uiCanvas = null;

                AnimationManager.inst?.Remove(animation.id);
            };

            AnimationManager.inst?.Play(animation);

            yield break;
        }

        #endregion

        #region Misc

        public static int CombineHashCodes<T1, T2>(T1 t1, T2 t2)
        {
            int hash = 17;
            hash = hash * 23 + t1.GetHashCode();
            hash = hash * 23 + t2.GetHashCode();
            return hash;
        }
        
        public static int CombineHashCodes<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            int hash = 17;
            hash = hash * 23 + t1.GetHashCode();
            hash = hash * 23 + t2.GetHashCode();
            hash = hash * 23 + t3.GetHashCode();
            return hash;
        }

        public static int CombineHashCodes(params object[] array)
        {
            int hash = 17;
            for (int i = 0; i < array.Length; i++)
                hash = hash * 23 + array[i].GetHashCode();
            return hash;
        }

        /// <summary>
        /// Compares a singular object against an array of objects.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="obj">Object to compare.</param>
        /// <param name="array">Array of objects to compare to.</param>
        /// <returns>Returns true if any of the objects in <paramref name="array"/> match <paramref name="obj"/>, otherwise returns false.</returns>
        public static bool Equals<T>(T obj, params T[] array)
        {
            for (int i = 0; i < array.Length; i++)
                if (obj.Equals(array[i]))
                    return true;

            return false;
        }

        public static void For<T>(Action<T> action, params T[] array)
        {
            for (int i = 0; i < array.Length; i++)
                action?.Invoke(array[i]);
        }

        /// <summary>
        /// Gets an array containing all the values of an enum.
        /// </summary>
        /// <typeparam name="T">The enum.</typeparam>
        /// <returns>Returns an array representing the enum.</returns>
        public static T[] GetValues<T>() where T : struct
        {
            var values = Enum.GetValues(typeof(T));
            var result = new T[values.Length];
            int num = 0;
            foreach (T value in values)
            {
                result[num] = value;
                num++;
            }
            return result;
        }

        /// <summary>
        /// Compares given values and invokes a method if they are not the same.
        /// </summary>
        /// <param name="prev">The previous value.</param>
        /// <param name="current">The current value.</param>
        /// <param name="action">The method to invoke if the parameters are not the same.</param>
        public static void UpdateValue<T>(T prev, T current, Action<T> action)
        {
            if (!prev.Equals(current))
                action?.Invoke(current);
        }

        /// <summary>
        /// Cleans up memory.
        /// </summary>
        public static void Cleanup()
        {
            Log($"{nameof(Cleanup)} unused memory.");
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        // CoreHelper.Notify("abcdefghijklmnopqrstuvwxyz", Color.white);
        static UICanvas currentNotification;
        static RTAnimation currentNotificationAnim;

        /// <summary>
        /// Notifies the user of a song or something else.
        /// </summary>
        /// <param name="notifText">Text to display.</param>
        /// <param name="color">Color to set.</param>
        /// <param name="fontSize">Font size.</param>
        public static void Notify(string notifText, Color color, int fontSize = 30)
        {
            if (currentNotification != null)
            {
                if (currentNotification.GameObject)
                    Destroy(currentNotification.GameObject);

                if (currentNotificationAnim)
                    AnimationManager.inst.Remove(currentNotificationAnim.id);
            }

            var uiCanvas = UIManager.GenerateUICanvas("Screenshot Canvas", null, true);
            currentNotification = uiCanvas;

            var textObj = Creator.NewUIObject("text", uiCanvas.GameObject.transform);

            RectValues.BottomLeftAnchored.SizeDelta(1000f, 100f).AssignToRectTransform(textObj.transform.AsRT());

            var text = textObj.AddComponent<Text>();
            text.font = FontManager.inst.DefaultFont;
            text.text = notifText;
            text.color = color;
            text.fontSize = fontSize;

            var animation = new RTAnimation("Notification");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(1f, 1f, Ease.SineOut),
                    new FloatKeyframe(2f, 1f, Ease.Linear),
                    new FloatKeyframe(3.5f, 0f, Ease.SineIn),
                }, x =>
                {
                    if (text)
                        text.color = RTColors.FadeColor(text.color, x);
                }),
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, new Vector2(10f, -60f), Ease.Linear),
                    new Vector2Keyframe(1f, new Vector2(10f, -40f), Ease.BackOut),
                    new Vector2Keyframe(2f, new Vector2(10f, -40f), Ease.Linear),
                    new Vector2Keyframe(3.5f, new Vector2(10f, -60f), Ease.BackIn)
                }, vector => textObj.transform.AsRT().anchoredPosition = vector),
            };

            animation.onComplete = () =>
            {
                if (uiCanvas.GameObject)
                    Destroy(uiCanvas.GameObject);
                uiCanvas = null;

                AnimationManager.inst?.Remove(animation.id);
                currentNotification = null;
            };

            AnimationManager.inst?.Play(animation);
            currentNotificationAnim = animation;
        }

        public static void SetConfigPreset(UserPreferenceType preset)
        {
            switch (preset)
            {
                // Beginner
                case UserPreferenceType.Beginner: {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Simple;
                        EditorConfig.Instance.EditorTheme.Value = EditorThemeType.Legacy;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = true;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = true;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Split;
                        EditorConfig.Instance.WaveformBGColor.Reset();
                        EditorConfig.Instance.WaveformBottomColor.Reset();
                        EditorConfig.Instance.WaveformTopColor.Reset();
                        EditorConfig.Instance.TimelineGridEnabled.Value = false;
                        EditorConfig.Instance.EventLabelsRenderLeft.Value = true;
                        EditorConfig.Instance.TimelineCursorColor.Reset();
                        EditorConfig.Instance.KeyframeCursorColor.Reset();
                        EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value = true;
                        EditorConfig.Instance.EventKeyframesRenderBinColor.Value = true;
                        EditorConfig.Instance.LevelLoadsLastTime.Value = false;
                        EditorConfig.Instance.LevelPausesOnStart.Value = false;
                        EditorConfig.Instance.DragUI.Value = true;
                        EditorConfig.Instance.ImportPrefabsDirectly.Value = true;
                        EditorConfig.Instance.RenderDepthRange.Value = new Vector2Int(0, 30);
                        EditorConfig.Instance.PlayEditorAnimations.Value = true;
                        EditorConfig.Instance.PreviewGridEnabled.Value = false;

                        CoreConfig.Instance.IncreasedClipPlanes.Value = true;
                        CoreConfig.Instance.EnableVideoBackground.Value = true;
                        CoreConfig.Instance.ShowBackgroundObjects.Value = true;

                        EventsConfig.Instance.ShakeEventMode.Value = ShakeType.Catalyst;

                        PlayerConfig.Instance.QueueBoost.Value = true;
                        PlayerConfig.Instance.PlaySoundB.Value = true;
                        PlayerConfig.Instance.PlaySoundR.Value = true;

                        break;
                    }

                // Legacy
                case UserPreferenceType.Legacy: {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Normal;
                        EditorConfig.Instance.EditorTheme.Value = EditorThemeType.Legacy;
                        EditorConfig.Instance.RoundedUI.Value = false;
                        EditorConfig.Instance.DraggingPlaysSound.Value = false;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = true;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Split;
                        EditorConfig.Instance.WaveformBGColor.Reset();
                        EditorConfig.Instance.WaveformBottomColor.Reset();
                        EditorConfig.Instance.WaveformTopColor.Reset();
                        EditorConfig.Instance.TimelineGridEnabled.Value = false;
                        EditorConfig.Instance.EventLabelsRenderLeft.Value = false;
                        EditorConfig.Instance.TimelineCursorColor.Reset();
                        EditorConfig.Instance.KeyframeCursorColor.Reset();
                        EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value = false;
                        EditorConfig.Instance.EventKeyframesRenderBinColor.Value = true;
                        EditorConfig.Instance.LevelLoadsLastTime.Value = false;
                        EditorConfig.Instance.LevelPausesOnStart.Value = false;
                        EditorConfig.Instance.DragUI.Value = false;
                        EditorConfig.Instance.ImportPrefabsDirectly.Value = true;
                        EditorConfig.Instance.RenderDepthRange.Value = new Vector2Int(0, 30);
                        EditorConfig.Instance.PlayEditorAnimations.Value = false;
                        EditorConfig.Instance.PreviewGridEnabled.Value = false;

                        CoreConfig.Instance.IncreasedClipPlanes.Value = false;
                        CoreConfig.Instance.EnableVideoBackground.Value = true;
                        CoreConfig.Instance.ShowBackgroundObjects.Value = true;

                        EventsConfig.Instance.ShakeEventMode.Value = ShakeType.Original;

                        PlayerConfig.Instance.QueueBoost.Value = false;
                        PlayerConfig.Instance.PlaySoundB.Value = false;
                        PlayerConfig.Instance.PlaySoundR.Value = false;

                        break;
                    }

                // Alpha
                case UserPreferenceType.Alpha: {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Normal;
                        EditorConfig.Instance.EditorTheme.Value = EditorThemeType.Modern;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = false;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = false;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Bottom;
                        EditorConfig.Instance.WaveformBGColor.Reset();
                        EditorConfig.Instance.WaveformBottomColor.Reset();
                        EditorConfig.Instance.WaveformTopColor.Value = new Color(0.5f, 0.5f, 0.5f, 1f);
                        EditorConfig.Instance.TimelineGridEnabled.Value = true;
                        EditorConfig.Instance.EventLabelsRenderLeft.Value = true;
                        EditorConfig.Instance.TimelineCursorColor.Value = LSColors.HexToColor("03AEF0");
                        EditorConfig.Instance.KeyframeCursorColor.Value = LSColors.HexToColor("03AEF0");
                        EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value = false;
                        EditorConfig.Instance.EventKeyframesRenderBinColor.Value = false;
                        EditorConfig.Instance.LevelLoadsLastTime.Value = false;
                        EditorConfig.Instance.LevelPausesOnStart.Value = false;
                        EditorConfig.Instance.DragUI.Value = false;
                        EditorConfig.Instance.ImportPrefabsDirectly.Value = true;
                        EditorConfig.Instance.RenderDepthRange.Value = new Vector2Int(0, 40); // todo: verify that the range is 40
                        EditorConfig.Instance.PlayEditorAnimations.Value = false;
                        EditorConfig.Instance.PreviewGridEnabled.Value = true;

                        CoreConfig.Instance.IncreasedClipPlanes.Value = false; // todo: make sure alpha levels work without this on
                        CoreConfig.Instance.EnableVideoBackground.Value = false;
                        CoreConfig.Instance.ShowBackgroundObjects.Value = false;

                        EventsConfig.Instance.ShakeEventMode.Value = ShakeType.Original;

                        PlayerConfig.Instance.QueueBoost.Value = true;
                        PlayerConfig.Instance.PlaySoundB.Value = false;
                        PlayerConfig.Instance.PlaySoundR.Value = false;

                        break;
                    }

                // Modded
                case UserPreferenceType.None: {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Advanced;
                        EditorConfig.Instance.EditorTheme.Value = EditorThemeType.Dark;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = true;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = false;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.SplitDetailed;
                        EditorConfig.Instance.WaveformBGColor.Reset();
                        EditorConfig.Instance.WaveformBottomColor.Value = LSColors.HexToColor("2FCBD6");
                        EditorConfig.Instance.WaveformTopColor.Value = LSColors.HexToColor("F6AC1A");
                        EditorConfig.Instance.TimelineGridEnabled.Value = true;
                        EditorConfig.Instance.EventLabelsRenderLeft.Value = false;
                        EditorConfig.Instance.TimelineCursorColor.Value = LSColors.HexToColor("F6AC1A");
                        EditorConfig.Instance.KeyframeCursorColor.Value = LSColors.HexToColor("F6AC1A");
                        EditorConfig.Instance.ObjectKeyframesRenderBinColor.Value = true;
                        EditorConfig.Instance.EventKeyframesRenderBinColor.Value = true;
                        EditorConfig.Instance.LevelLoadsLastTime.Value = true;
                        EditorConfig.Instance.LevelPausesOnStart.Value = true;
                        EditorConfig.Instance.DragUI.Value = true;
                        EditorConfig.Instance.ImportPrefabsDirectly.Value = false;
                        EditorConfig.Instance.RenderDepthRange.Reset();
                        EditorConfig.Instance.PlayEditorAnimations.Value = true;
                        EditorConfig.Instance.PreviewGridEnabled.Value = false;

                        CoreConfig.Instance.IncreasedClipPlanes.Value = true;
                        CoreConfig.Instance.EnableVideoBackground.Value = true;
                        CoreConfig.Instance.ShowBackgroundObjects.Value = true;

                        EventsConfig.Instance.ShakeEventMode.Value = ShakeType.Catalyst;

                        PlayerConfig.Instance.QueueBoost.Value = true;
                        PlayerConfig.Instance.PlaySoundB.Value = true;
                        PlayerConfig.Instance.PlaySoundR.Value = true;

                        break;
                    }
            }
        }

        /// <summary>
        /// Sets mostly unlimited render depth range.
        /// </summary>
        public static void SetCameraRenderDistance()
        {
            if (!InGame)
                return;

            var camera = Camera.main;
            camera.farClipPlane = CoreConfig.Instance.IncreasedClipPlanes.Value ? 100000 : 22f;
            camera.nearClipPlane = CoreConfig.Instance.IncreasedClipPlanes.Value ? -100000 : -9.9f;
        }

        /// <summary>
        /// Sets anti aliasing.
        /// </summary>
        public static void SetAntiAliasing()
        {
            if (RTGameManager.inst && RTGameManager.inst.postProcessLayer)
            {
                RTGameManager.inst.postProcessLayer.antialiasingMode
                    = CoreConfig.Instance.AntiAliasing.Value ? PostProcessLayer.Antialiasing.FastApproximateAntialiasing : PostProcessLayer.Antialiasing.None;
            }
        }

        public static string[] discordSubIcons = new string[]
        {
            "arcade",
            "editor",
            "play",
            "menu",
        };

        public static string[] discordIcons = new string[]
        {
            PA_LOGO_WHITE,
            PA_LOGO_BLACK,
        };

        public const string PA_LOGO_WHITE = "pa_logo_white";
        public const string PA_LOGO_BLACK = "pa_logo_black";
        public static string discordLevel = string.Empty;
        public static string discordDetails = string.Empty;
        public static string discordIcon = string.Empty;
        public static string discordArt = string.Empty;
        public static void UpdateDiscordStatus(string level, string details, string icon, string art = PA_LOGO_WHITE)
        {
            DiscordController.inst.OnStateChange(CoreConfig.Instance.DiscordShowLevel.Value ? level : string.Empty);
            DiscordController.inst.OnArtChange(art);
            DiscordController.inst.OnIconChange(icon);
            DiscordController.inst.OnDetailsChange(details);

            discordLevel = level;
            discordDetails = details;
            discordIcon = icon;
            discordArt = art;

            DiscordRpc.UpdatePresence(DiscordController.inst.presence);
        }

        public static void ListObjectLayers()
        {
            var allLayers = new List<int>();

            var beatmapObjects = GameData.Current.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];
                // we add 1 to the objects' layer since people using the editor only see the non-zero version of the layer system.
                if (!allLayers.Contains(beatmapObject.editorData.Layer + 1))
                    allLayers.Add(beatmapObject.editorData.Layer + 1);
            }
            
            var prefabObjects = GameData.Current.prefabObjects;
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];
                if (!allLayers.Contains(prefabObject.editorData.Layer + 1))
                    allLayers.Add(prefabObject.editorData.Layer + 1);
            }

            allLayers.Sort();

            EditorManager.inst.DisplayNotification($"Objects on Layers:<br>{RTString.ListToString(allLayers)}", 3f, EditorManager.NotificationType.Info);
        }

        public static string GetShape(int _shape, int _shapeOption)
        {
            if (ObjectManager.inst != null && ObjectManager.inst.objectPrefabs.Count > 0)
            {
                int s = Mathf.Clamp(_shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                int so = Mathf.Clamp(_shapeOption, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);
                return ObjectManager.inst.objectPrefabs[s].options[so].name;
            }
            return "no shape";
        }

        #endregion
    }
}
