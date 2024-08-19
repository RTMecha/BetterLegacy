using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using CielaSpike;
using InControl;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class CoreHelper
    {
        #region Properties

        /// <summary>
        /// The current scene PA is in.
        /// </summary>
        public static SceneType CurrentSceneType { get; set; }

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
        /// For checking if the user is in the editor preview or in game.
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
        public static bool InMenu => MenuManager.inst.ic;

        /// <summary>
        /// If the player is in the Classic Arrhythmia story mode.
        /// </summary>
        public static bool InStory { get; set; }

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
        public static float Pitch => InEditor ? 1f : new List<float>
            { 0.1f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 3f, }[Mathf.Clamp(DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2), 0, 7)];

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

        #endregion

        /// <summary>
        /// Compares given values and invokes a method if they are not the same.
        /// </summary>
        /// <param name="prev">The previous value.</param>
        /// <param name="current">The current value.</param>
        /// <param name="action">The method to invoke if the parameters are not the same.</param>
        /// <returns>Returns true if previous value is not equal to current value, otherwise returns false.</returns>
        public static bool UpdateValue<T>(T prev, T current, Action<T> action)
        {
            bool value = !prev.Equals(current);
            if (value)
                action?.Invoke(current);
            return value;
        }

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

        #endregion

        #region Color

        /// <summary>
        /// Converts all color channels (including alpha) to a hex number.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <returns>Returns a hex code from the color provided.</returns>
        public static string ColorToHex(Color32 color) => color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");

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

        #endregion

        #region Strings

        public static string[] GetLines(string str) => str.Split(new string[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        public static string InterpolateString(string str, float t) => str.Substring(0, Mathf.Clamp((int)RTMath.Lerp(0, str.Length, t), 0, str.Length));

        public static KeyValuePair<string, string> ReplaceMatching(KeyValuePair<string, string> keyValuePair, string sequenceText, string pattern)
        {
            var text = keyValuePair.Key;
            var replace = keyValuePair.Value;
            var matches1 = Regex.Matches(text, pattern);
            for (int i = 0; i < matches1.Count; i++)
            {
                var m = matches1[i];
                if (!sequenceText.Contains(m.Groups[0].ToString()))
                    text = text.Replace(m.Groups[0].ToString(), "");
                replace = replace.Replace(m.Groups[0].ToString(), "");
            }
            return new KeyValuePair<string, string>(text, replace);
        }

        public static bool RegexMatch(string str, Regex regex, out Match match)
        {
            if (regex != null && regex.Match(str).Success)
            {
                match = regex.Match(str);
                return true;
            }

            match = null;
            return false;
        }

        public static void RegexMatch(string str, Regex regex, Action<Match> matchAction)
        {
            if (RegexMatch(str, regex, out Match match))
                matchAction?.Invoke(match);
        }

        public static string Flip(string str)
        {
            string s;
            s = str.Replace("Left", "LSLeft87344874")
                .Replace("Right", "LSRight87344874")
                .Replace("left", "LSleft87344874")
                .Replace("right", "LSright87344874")
                .Replace("LEFT", "LSLEFT87344874")
                .Replace("RIGHT", "LSRIGHT87344874");

            return s.Replace("LSLeft87344874", "Right")
                .Replace("LSRight87344874", "Left")
                .Replace("LSleft87344874", "right")
                .Replace("LSright87344874", "left")
                .Replace("LSLEFT87344874", "RIGHT")
                .Replace("LSRIGHT87344874", "LEFT");
        }

        public static bool ColorMatch(Color a, Color b, float range, bool alpha = false)
            => alpha ? a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range && a.a < b.a + range && a.a > b.a - range :
                a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range;

        public static bool SearchString(string searchTerm, string a) => string.IsNullOrEmpty(searchTerm) || a.ToLower().Contains(searchTerm.ToLower());

        #endregion

        #region Links

        public static string GetURL(int type, int site, string link)
        {
            if (type != 0)
                return UserLinks[site].linkFormat;

            if (InstanceLinks[site].linkFormat.Contains("{1}"))
            {
                var split = link.Split(',');
                return string.Format(InstanceLinks[site].linkFormat, split[0], split[1]);
            }
            else
                return string.Format(InstanceLinks[site].linkFormat, link);
        }

        public static List<DataManager.LinkType> InstanceLinks => new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com/{1}"),
            new DataManager.LinkType("YouTube", "https://youtube.com/watch?v={0}"),
            new DataManager.LinkType("Newgrounds", "https://newgrounds.com/audio/listen/{0}"),
        };

        public static List<DataManager.LinkType> UserLinks => new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
            new DataManager.LinkType("YouTube", "https://youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
        };

        #endregion

        #region Controls

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

        public static string DefaultYieldInstructionDescription => "Some options will run faster but freeze the game, while others run slower but allow you to see them update in real time.";

        public static YieldInstruction GetYieldInstruction(YieldType yieldType, ref float delay)
        {
            switch (yieldType)
            {
                case YieldType.Delay: delay += 0.0001f; return new WaitForSeconds(delay);
                case YieldType.Null: return null;
                case YieldType.EndOfFrame: return new WaitForEndOfFrame();
                case YieldType.FixedUpdate: return new WaitForFixedUpdate();
            }
            return null;
        }

        #region Misc

        public static System.Diagnostics.Stopwatch StartNewStopwatch() => System.Diagnostics.Stopwatch.StartNew();
        public static void StopAndLogStopwatch(System.Diagnostics.Stopwatch sw, string message = "")
        {
            sw.Stop();
            Log($"{(string.IsNullOrEmpty(message) ? message : message + "\n")}Time taken: {sw.Elapsed}");
        }

        public static IEnumerator Empty()
        {
            yield break;
        }

        public static IEnumerator PerformActionAfterSeconds(float t, Action action)
        {
            yield return new WaitForSeconds(t);
            action?.Invoke();
        }

        public static string currentPopupID;
        public static GameObject currentPopup;
        public static void Popup(string dialogue, Color bar, string title, float time = 2f, bool destroyPrevious = true)
        {
            if (destroyPrevious && currentPopup)
            {
                if (AnimationManager.inst.animations.Has(x => x.id == currentPopupID))
                    AnimationManager.inst.RemoveID(currentPopupID);
                Destroy(currentPopup);
            }

            var inter = new GameObject("Canvas");
            currentPopup = inter;
            inter.transform.localScale = Vector3.one * ScreenScale;
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = ScreenScale;
            canvas.sortingOrder = 1000;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            inter.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("image");
            imageObj.transform.SetParent(inter.transform);
            imageObj.transform.localScale = Vector3.zero;

            var imageRT = imageObj.AddComponent<RectTransform>();
            imageRT.anchoredPosition = new Vector2(0f, 0f);
            imageRT.sizeDelta = new Vector2(610f, 250f);

            var im = imageObj.AddComponent<Image>();
            im.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var textObj = new GameObject("text");
            textObj.transform.SetParent(imageObj.transform);
            textObj.transform.localScale = Vector3.one;

            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(0f, 0f);
            textRT.sizeDelta = new Vector2(590f, 250f);

            var text = textObj.AddComponent<Text>();
            text.font = FontManager.inst.DefaultFont;
            text.text = dialogue;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;

            var top = new GameObject("top");
            top.transform.SetParent(imageRT);
            top.transform.localScale = Vector3.one;

            var topRT = top.AddComponent<RectTransform>();
            topRT.anchoredPosition = new Vector2(0f, 110f);
            topRT.sizeDelta = new Vector2(610f, 32f);

            var topImage = top.AddComponent<Image>();
            topImage.color = bar;

            var titleTextObj = new GameObject("text");
            titleTextObj.transform.SetParent(topRT);
            titleTextObj.transform.localScale = Vector3.one;

            var titleTextRT = titleTextObj.AddComponent<RectTransform>();
            titleTextRT.anchoredPosition = Vector2.zero;
            titleTextRT.sizeDelta = new Vector2(590f, 32f);

            var titleText = titleTextObj.AddComponent<Text>();
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = FontManager.inst.DefaultFont;
            titleText.fontSize = 20;
            titleText.text = title;
            titleText.color = InvertColorHue(InvertColorValue(bar));

            var animation = new RTAnimation("Popup Notification");
            currentPopupID = animation.id;
            animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.2f, 1f, Ease.BackOut),
                        new FloatKeyframe(time + 0.2f, 1f, Ease.Linear),
                        new FloatKeyframe(time + 0.7f, 0f, Ease.BackIn),
                        new FloatKeyframe(time + 0.8f, 0f, Ease.Linear),
                    }, delegate (float x)
                    {
                        imageObj.transform.localScale = new Vector3(x, x, x);
                    }),
                };
            animation.onComplete = delegate ()
            {
                Destroy(inter);

                AnimationManager.inst.RemoveID(animation.id);
            };

            AnimationManager.inst?.Play(animation);
        }

        public static void TakeScreenshot()
        {
            string directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var file = directory + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".png";
            ScreenCapture.CaptureScreenshot(file, 1);
            Log($"Took Screenshot! - {file}");

            StartCoroutine(ScreenshotNotification());
        }

        static IEnumerator ScreenshotNotification()
        {
            yield return new WaitForSeconds(0.1f);

            // In-Game Screenshot notification

            var scr = ScreenCapture.CaptureScreenshotAsTexture();

            AudioManager.inst.PlaySound("glitch");

            var inter = new GameObject("Canvas");
            inter.transform.localScale = Vector3.one * ScreenScale;
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = ScreenScale;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            inter.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("image");
            imageObj.transform.SetParent(inter.transform);
            imageObj.transform.localScale = Vector3.one;


            var imageRT = imageObj.AddComponent<RectTransform>();
            imageRT.anchoredPosition = new Vector2(850f, -480f);
            imageRT.sizeDelta = new Vector2(scr.width / 10f, scr.height / 10f);

            var im = imageObj.AddComponent<Image>();
            im.sprite = Sprite.Create(scr, new Rect(0f, 0f, scr.width, scr.height), Vector2.zero);

            var textObj = new GameObject("text");
            textObj.transform.SetParent(imageObj.transform);
            textObj.transform.localScale = Vector3.one;

            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(0f, 20f);
            textRT.sizeDelta = new Vector2(200f, 100f);

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
                }, delegate (Color x)
                {
                    if (im)
                        im.color = x;
                    if (text)
                        text.color = x;
                }),
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, new Vector2(850f, -480f), Ease.Linear),
                    new Vector2Keyframe(1.5f, new Vector2(850f, -600f), Ease.BackIn)
                }, delegate (Vector2 x)
                {
                    imageRT.anchoredPosition = x;
                }, delegate ()
                {
                    scr = null;

                    Destroy(inter);

                    AnimationManager.inst?.RemoveID(animation.id);
                }),
            };

            animation.onComplete = delegate ()
            {
                if (inter)
                    Destroy(inter);
            };

            AnimationManager.inst?.Play(animation);

            yield break;
        }

        /// <summary>
        /// Sets mostly unlimited render depth range.
        /// </summary>
        public static void SetCameraRenderDistance()
        {
            if (GameManager.inst == null)
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
            if (GameStorageManager.inst && GameStorageManager.inst.postProcessLayer)
            {
                GameStorageManager.inst.postProcessLayer.antialiasingMode
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

            allLayers.AddRange(DataManager.inst.gameData.beatmapObjects.Where(x => !allLayers.Contains(x.editorData.layer)).Select(x => x.editorData.layer));
            allLayers.AddRange(DataManager.inst.gameData.prefabObjects.Where(x => !allLayers.Contains(x.editorData.layer)).Select(x => x.editorData.layer));

            allLayers = (from x in allLayers
                         orderby x ascending
                         select x).ToList();

            string lister = "";

            for (int i = 0; i < allLayers.Count; i++)
            {
                int num = allLayers[i] + 1;
                if (!lister.Contains(num.ToString()))
                {
                    lister += num.ToString();
                    if (i != allLayers.Count - 1)
                        lister += ", ";
                }
            }

            EditorManager.inst.DisplayNotification($"Objects on Layers:<br>[ {lister} ]", 2f, EditorManager.NotificationType.Info);
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
