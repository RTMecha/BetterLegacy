using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using CielaSpike;
using InControl;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

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
        public static bool InMenu => MenuManager.inst.ic || InterfaceManager.inst.CurrentInterface;

        /// <summary>
        /// If the player is in the Classic Arrhythmia story mode.
        /// </summary>
        public static bool InStory { get; set; }

        /// <summary>
        /// The currently open level.
        /// </summary>
        public static Level CurrentLevel => InEditor ? RTEditor.inst.CurrentLevel : LevelManager.CurrentLevel;

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
        /// The current pitch setting.
        /// </summary>
        public static float Pitch
        {
            get
            {
                var gameSpeeds = PlayerManager.GameSpeeds;
                return InEditor || InStory ? 1f : gameSpeeds[Mathf.Clamp(PlayerManager.ArcadeGameSpeed, 0, gameSpeeds.Length - 1)];
            }
        }

        /// <summary>
        /// Gets the current interpolated theme or if the user is in the theme editor, the preview theme.
        /// </summary>
        public static BeatmapTheme CurrentBeatmapTheme => InEditor && EventEditor.inst.showTheme ? (BeatmapTheme)EventEditor.inst.previewTheme : (BeatmapTheme)GameManager.inst?.LiveTheme;

        /// <summary>
        /// jokes on you, I FIXED THE BUG
        /// </summary>
        public static bool AprilFools => DateTime.Now.ToString("M") == "1 April" || DateTime.Now.ToString("M") == "April 1";
        
        /// <summary>
        /// For the Project Arrhythmia (release) Anniversary.
        /// </summary>
        public static bool PAAnniversary => DateTime.Now.ToString("M") == "15 June" || DateTime.Now.ToString("M") == "June 15";

        /// <summary>
        /// Gets the current resolution as a Vector2Int based on Core Config's resolution value.
        /// </summary>
        public static Vector2Int CurrentResolution => GetResolution((int)CoreConfig.Instance.Resolution.Value);

        /// <summary>
        /// Gets a resolution from the resolution list.
        /// </summary>
        /// <param name="resolution">The resolution index.</param>
        /// <returns>Returns a Vector2Int representing a resolution.</returns>
        public static Vector2Int GetResolution(int resolution) => new Vector2Int((int)DataManager.inst.resolutions[resolution].x, (int)DataManager.inst.resolutions[resolution].y);

        /// <summary>
        /// Gets a difficulty from the difficulty list.
        /// </summary>
        /// <param name="difficulty">The difficulty index.</param>
        /// <returns>Returns a known difficulty if the index is in the range of the difficulty list. If it isn't, it'll return an unknown difficulty.</returns>
        public static DataManager.Difficulty GetDifficulty(int difficulty)
            => difficulty >= 0 && difficulty < DataManager.inst.difficulties.Count ?
            DataManager.inst.difficulties[difficulty] : new DataManager.Difficulty("Unknown Difficulty", LSColors.HexToColor("424242"));

        #endregion

        #region Unity

        /// <summary>
        /// If the user is interacting with an InputField.
        /// </summary>
        public static bool IsUsingInputField { get; set; }

        /// <summary>
        /// Loads an AssetBundle from the Assets folder.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundle(string file)
            => AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}{file}");

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
        /// Destroys a Unity Object from anywhere, includes instant and delay time.
        /// </summary>
        /// <param name="obj">Unity Object to destroy.</param>
        /// <param name="instant">If object should destroy instantly.</param>
        /// <param name="t">The delay to destroy the object at if instant is off.</param>
        public static void Destroy(UnityEngine.Object obj, bool instant = false, float t = 0f)
        {
            if (instant)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }

            UnityEngine.Object.Destroy(obj, t);
        }

        /// <summary>
        /// Destroys multiple Unity Objects from anywhere, includes instant and delay time.
        /// </summary>
        /// <param name="instant">If object should destroy instantly.</param>
        /// <param name="t">The delay to destroy the object at if instant is off.</param>
        /// <param name="objects">Unity Objects to destroy.</param>
        public static void Destroy(bool instant, float t, params UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                Destroy(objects[i], instant, t);
        }

        public static void Delete(GameObject gameObject)
        {
            if (!gameObject)
                return;

            gameObject.transform.SetParent(null);
            UnityEngine.Object.Destroy(gameObject);
        }

        /// <summary>
        /// Deletes all children from a transform.
        /// </summary>
        /// <param name="transform">Transform to delete the children of.</param>
        public static void DeleteChildren(Transform transform)
        {
            var listToDelete = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
                listToDelete.Add(transform.GetChild(i).gameObject);
            for (int i = 0; i < listToDelete.Count; i++)
                Destroy(listToDelete[i], true);
            listToDelete.Clear();
            listToDelete = null;
        }

        /// <summary>
        /// Removes all children from the transform and destroys them. This is done due to Unity's Destroy method not working in some cases.
        /// </summary>
        /// <param name="transform">Transform to delete the children of.</param>
        public static void DestroyChildren(Transform transform)
        {
            var listToDestroy = new List<GameObject>();
            while (transform.childCount > 0)
            {
                var child = transform.GetChild(0);
                child.SetParent(null);
                listToDestroy.Add(child.gameObject);
            }
            foreach (var child in listToDestroy)
                Destroy(child);
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

        #region Coroutines

        /// <summary>
        /// Starts a coroutine from anywhere.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated Coroutine.</returns>
        public static Coroutine StartCoroutine(IEnumerator routine) => LegacyPlugin.inst.StartCoroutine(routine);

        /// <summary>
        /// Starts a coroutine from anywhere asynchronously.
        /// </summary>
        /// <param name="routine">Routine to start.</param>
        /// <returns>Returns a generated Coroutine.</returns>
        public static Coroutine StartCoroutineAsync(IEnumerator routine) => LegacyPlugin.inst.StartCoroutineAsync(routine);

        public static void PerformActionAfterSeconds(float t, Action action) => StartCoroutine(IPerformActionAfterSeconds(t, action));
        
        public static IEnumerator IPerformActionAfterSeconds(float t, Action action)
        {
            yield return new WaitForSeconds(t);
            action?.Invoke();
        }

        public static void WaitUntil(Func<bool> func, Action action) => StartCoroutine(IWaitUntil(func, action));

        public static IEnumerator IWaitUntil(Func<bool> func, Action action)
        {
            yield return new WaitUntil(func);
            action?.Invoke();
        }

        public static IEnumerator DoAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        public static void ReturnToUnity(Action action) => StartCoroutine(IReturnToUnity(action));

        public static IEnumerator IReturnToUnity(Action action)
        {
            yield return Ninja.JumpToUnity;
            action?.Invoke();
        }

        public static void LogOnMainThread(string message) => ReturnToUnity(() => { Log(message); });

        public static string DefaultYieldInstructionDescription => "Some options will run faster but freeze the game, while others run slower but allow you to see them update in real time.";

        /// <summary>
        /// Gets a specific <see cref="YieldInstruction"/> from a <see cref="YieldType"/>.
        /// </summary>
        /// <param name="yieldType">YieldType to get an instruction from.</param>
        /// <param name="delay">Delay reference for <see cref="WaitForSeconds"/>.</param>
        /// <returns>Returns a <see cref="YieldInstruction"/>.</returns>
        public static YieldInstruction GetYieldInstruction(YieldType yieldType, ref float delay)
        {
            switch (yieldType)
            {
                case YieldType.Delay: delay += 0.0001f; return new WaitForSeconds(delay);
                case YieldType.EndOfFrame: return new WaitForEndOfFrame();
                case YieldType.FixedUpdate: return new WaitForFixedUpdate();
            }
            return null;
        }

        #endregion

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

        #region Color

        /// <summary>
        /// Hex ToString format.
        /// </summary>
        public const string X2 = "X2";

        /// <summary>
        /// Converts all color channels (including alpha) to a hex value.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <returns>Returns a hex code from the color provided.</returns>
        public static string ColorToHex(Color32 color) => color.r.ToString(X2) + color.g.ToString(X2) + color.b.ToString(X2) + color.a.ToString(X2);

        /// <summary>
        /// Converts R, G and B color channels to a hex value. If the alpha channel is not full, then add that to the hex value.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <returns>Returns a hex code from the color provided.</returns>
        public static string ColorToHexOptional(Color32 color)
        {
            var result = color.r.ToString(X2) + color.g.ToString(X2) + color.b.ToString(X2);
            var a = color.a.ToString(X2);
            if (a != "FF")
                result += a;
            return result;
        }

        /// <summary>
        /// Changes the hue, saturation and value of a color.
        /// </summary>
        /// <param name="color">Color to change.</param>
        /// <param name="hue">Hue offset.</param>
        /// <param name="sat">Saturation offset.</param>
        /// <param name="val">Value offset.</param>
        /// <returns>Returns a changed color based on the hue / sat / val offset values.</returns>
        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            LSColors.ColorToHSV(color, out double num, out double saturation, out double value);
            return LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        /// <summary>
        /// Inverts a color.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColor(Color color) => InvertColorHue(InvertColorValue(color));

        /// <summary>
        /// Inverts a colors' hue.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColorHue(Color color)
        {
            LSColors.ColorToHSV(color, out double hue, out double saturation, out double value);
            return LSColors.ColorFromHSV(hue - 180.0, saturation, value);
        }

        /// <summary>
        /// Inverts a colors' value.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColorValue(Color color)
        {
            LSColors.ColorToHSV(color, out double hue, out double sat, out double val);
            return LSColors.ColorFromHSV(hue, sat, val < 0.5 ? -val + 1 : -(val - 1));
        }

        /// <summary>
        /// Gets a custom player object color.
        /// </summary>
        /// <param name="playerIndex">Index reference.</param>
        /// <param name="col"></param>
        /// <param name="alpha"></param>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Color GetPlayerColor(int playerIndex, int col, float alpha, string hex)
            => LSColors.fadeColor(col >= 0 && col < 4 ? CurrentBeatmapTheme.playerColors[col] : col == 4 ? CurrentBeatmapTheme.guiColor : col > 4 && col < 23 ? CurrentBeatmapTheme.objectColors[col - 5] :
                col == 23 ? CurrentBeatmapTheme.playerColors[playerIndex % 4] : col == 24 ? LSColors.HexToColor(hex) : col == 25 ? CurrentBeatmapTheme.guiAccentColor : LSColors.pink500, alpha);

        /// <summary>
        /// Creates and fills a color list.
        /// </summary>
        /// <param name="count">Amount to fill.</param>
        /// <returns></returns>
        public static List<Color> NewColorList(int count)
        {
            var list = new List<Color>();
            while (list.Count < count)
                list.Add(LSColors.pink500);
            return list;
        }

        public static Color MixColors(List<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Count;
        }

        public static Color MixColors(params Color[] colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Length;
        }

        public static bool ColorMatch(Color a, Color b, float range, bool alpha = false)
            => alpha ? a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range && a.a < b.a + range && a.a > b.a - range :
                a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range;

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

            StartCoroutine(ScreenshotNotification());
        }

        static IEnumerator ScreenshotNotification()
        {
            yield return new WaitForSeconds(0.1f);

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

        public static void SetConfigPreset(UserPreferenceType preset)
        {
            switch (preset)
            {
                // Beginner
                case UserPreferenceType.Beginner:
                    {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Simple;
                        EditorConfig.Instance.EditorTheme.Value = EditorTheme.Legacy;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = true;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = true;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Legacy;
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
                case UserPreferenceType.Legacy:
                    {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Normal;
                        EditorConfig.Instance.EditorTheme.Value = EditorTheme.Legacy;
                        EditorConfig.Instance.RoundedUI.Value = false;
                        EditorConfig.Instance.DraggingPlaysSound.Value = false;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = true;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Legacy;
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
                case UserPreferenceType.Alpha:
                    {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Normal;
                        EditorConfig.Instance.EditorTheme.Value = EditorTheme.Modern;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = false;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = false;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.Modern;
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
                case UserPreferenceType.None:
                    {
                        EditorConfig.Instance.EditorComplexity.Value = Complexity.Advanced;
                        EditorConfig.Instance.EditorTheme.Value = EditorTheme.Dark;
                        EditorConfig.Instance.RoundedUI.Value = true;
                        EditorConfig.Instance.DraggingPlaysSound.Value = true;
                        EditorConfig.Instance.PrefabExampleTemplate.Value = false;
                        EditorConfig.Instance.WaveformMode.Value = WaveformType.LegacyFast;
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

        public static string discordLevel = "";
        public static string discordDetails = "";
        public static string discordIcon = "";
        public static string discordArt = "";
        public static void UpdateDiscordStatus(string level, string details, string icon, string art = "pa_logo_white")
        {
            DiscordController.inst.OnStateChange(CoreConfig.Instance.DiscordShowLevel.Value ? level : "");
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
                if (!allLayers.Contains(beatmapObject.editorData.layer + 1))
                    allLayers.Add(beatmapObject.editorData.layer + 1);
            }
            
            var prefabObjects = GameData.Current.prefabObjects;
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];
                if (!allLayers.Contains(prefabObject.editorData.layer + 1))
                    allLayers.Add(prefabObject.editorData.layer + 1);
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
