using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using CielaSpike;
using InControl;
using LSFunctions;
using SimpleJSON;
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
        public static float Pitch => InEditor || InStory ? 1f : new List<float>
            { 0.1f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 3f, }[Mathf.Clamp(PlayerManager.ArcadeGameSpeed, 0, 7)];

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

        public static Color MixColors(List<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Count;
        }

        public static bool ColorMatch(Color a, Color b, float range, bool alpha = false)
            => alpha ? a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range && a.a < b.a + range && a.a > b.a - range :
                a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range;

        #endregion

        #region Links

        public const string MOD_DISCORD_LINK = "https://discord.gg/nB27X2JZcY";

        public enum LinkType
        {
            Song,
            Artist,
            Creator
        }

        public static string GetURL(LinkType type, int site, string link)
        {
            if (string.IsNullOrEmpty(link))
                return link;

            var userLinks = ArtistLinks;
            var instanceLinks = SongLinks;
            var creatorLinks = CreatorLinks;

            switch (type)
            {
                case LinkType.Song:
                    {
                        if (site < 0 || site >= instanceLinks.Count)
                            return null;


                        if (site < 0 || site >= instanceLinks.Count)
                            return null;

                        if (instanceLinks[site].linkFormat.Contains("{1}"))
                        {
                            var split = link.Split(',');
                            return string.Format(instanceLinks[site].linkFormat, split[0], split[1]);
                        }
                        else
                            return string.Format(instanceLinks[site].linkFormat, link);
                    }
                case LinkType.Artist:
                    {
                        if (site < 0 || site >= userLinks.Count)
                            return null;

                        return string.Format(userLinks[site].linkFormat, link);
                    }
                case LinkType.Creator:
                    {
                        if (site < 0 || site >= creatorLinks.Count)
                            return null;

                        return string.Format(creatorLinks[site].linkFormat, link);
                    }
            }

            return null;
        }

        public static List<DataManager.LinkType> SongLinks => new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com/{1}"),
            new DataManager.LinkType("YouTube", "https://youtube.com/watch?v={0}"),
            new DataManager.LinkType("Newgrounds", "https://newgrounds.com/audio/listen/{0}"),
        };

        public static List<DataManager.LinkType> ArtistLinks => new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
            new DataManager.LinkType("YouTube", "https://youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
        };

        public static List<DataManager.LinkType> CreatorLinks => new List<DataManager.LinkType>
        {
            new DataManager.LinkType("YouTube", "https://youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
            new DataManager.LinkType("Discord", "https://discord.gg/{0}"),
            new DataManager.LinkType("Patreon", "https://patreon.com/{0}"),
            new DataManager.LinkType("Twitter", "https://twitter.com/{0}"),
        };

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

        #region GameData

        public static void SetParent(TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo, bool recalculate = true, bool renderParent = true) => TrySetParent(currentSelection, beatmapObjectToParentTo, recalculate, renderParent);

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="currentSelection"></param>
        /// <param name="beatmapObjectToParentTo"></param>
        /// <returns></returns>
        public static bool TrySetParent(TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo, bool recalculate = true, bool renderParent = true)
        {
            var dictionary = new Dictionary<string, bool>();
            var beatmapObjects = GameData.Current.beatmapObjects;

            foreach (var obj in beatmapObjects)
            {
                bool canParent = true;
                if (!string.IsNullOrEmpty(obj.parent))
                {
                    string parentID = currentSelection.ID;
                    while (!string.IsNullOrEmpty(parentID))
                    {
                        if (parentID == obj.parent)
                        {
                            canParent = false;
                            break;
                        }

                        int index = beatmapObjects.FindIndex(x => x.parent == parentID);
                        parentID = index != -1 ? beatmapObjects[index].id : null;
                    }
                }

                dictionary[obj.id] = canParent;
            }

            dictionary[currentSelection.ID] = false;

            var shouldParent = dictionary.TryGetValue(beatmapObjectToParentTo.id, out bool value) && value;

            if (shouldParent)
            {
                currentSelection.GetData<BeatmapObject>().parent = beatmapObjectToParentTo.id;
                var bm = currentSelection.GetData<BeatmapObject>();
                Updater.UpdateObject(bm, recalculate: recalculate);

                if (renderParent)
                    ObjectEditor.inst.RenderParent(bm);
            }

            return shouldParent;
        }

        /// <summary>
        /// Gets closest event keyframe to current time.
        /// </summary>
        /// <param name="_type">Event Keyframe Type</param>
        /// <returns>Event Keyframe Index</returns>
        public static int ClosestEventKeyframe(int _type)
        {
            var allEvents = GameData.Current.eventObjects.allEvents;
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (allEvents[_type].TryFindIndex(x => x.eventTime > time, out int nextKF))
            {
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

        public static bool TryFindObjectWithTag(Modifier<BeatmapObject> modifier, string tag, out BeatmapObject result)
        {
            result = FindObjectWithTag(modifier, tag);
            return result != null;
        }

        public static BeatmapObject FindObjectWithTag(string tag) => GameData.Current.beatmapObjects.Find(x => x.tags.Contains(tag));

        public static BeatmapObject FindObjectWithTag(Modifier<BeatmapObject> modifier, string tag)
        {
            var gameData = GameData.Current;

            if (modifier.reference.fromPrefab && modifier.prefabInstanceOnly && gameData.prefabObjects.TryFind(x => x.ID == modifier.reference.prefabInstanceID, out PrefabObject prefabObject))
            {
                var bm = gameData.beatmapObjects.Find(x => x.tags.Contains(tag) && x.fromPrefab && x.prefabID == prefabObject.prefabID && x.prefabInstanceID == prefabObject.ID);

                if (bm)
                    return bm;
            }

            return gameData.beatmapObjects.Find(x => x.tags.Contains(tag));
        }

        public static BeatmapObject FindObjectWithTag(List<BeatmapObject> beatmapObjects, BeatmapObject beatmapObject, string tag) => beatmapObjects.Find(x => x.tags.Contains(tag));

        public static BeatmapObject FindObjectWithTag(List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects, BeatmapObject beatmapObject, string tag)
        {
            if (beatmapObject.fromPrefab && prefabObjects.TryFind(x => x.ID == beatmapObject.prefabInstanceID, out PrefabObject prefabObject))
            {
                var bm = beatmapObjects.Find(x => x.tags.Contains(tag) && x.fromPrefab && x.prefabID == prefabObject.prefabID && x.prefabInstanceID == prefabObject.ID);

                if (bm)
                    return bm;
            }

            return beatmapObjects.Find(x => x.tags.Contains(tag) && x.prefabID == beatmapObject.prefabID && x.prefabInstanceID == beatmapObject.prefabInstanceID);
        }

        public static List<BeatmapObject> FindObjectsWithTag(string tag) => GameData.Current.beatmapObjects.FindAll(x => x.tags.Contains(tag));

        public static List<BeatmapObject> FindObjectsWithTag(BeatmapObject beatmapObject, string tag)
        {
            var gameData = GameData.Current;
            var beatmapObjects = gameData.beatmapObjects;

            if (beatmapObject.fromPrefab && gameData.prefabObjects.TryFind(x => x.ID == beatmapObject.prefabInstanceID, out PrefabObject prefabObject))
                return beatmapObjects.FindAll(x => x.tags.Contains(tag) && x.fromPrefab && x.prefabID == prefabObject.prefabID && x.prefabInstanceID == prefabObject.ID);

            return beatmapObjects.FindAll(x => x.tags.Contains(tag) && x.prefabID == beatmapObject.prefabID && x.prefabInstanceID == beatmapObject.prefabInstanceID);
        }

        public static List<BeatmapObject> FindObjectsWithTag(List<BeatmapObject> beatmapObjects, string tag) => beatmapObjects.FindAll(x => x.tags.Contains(tag));

        public static List<BeatmapObject> FindObjectsWithTag(List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects, BeatmapObject beatmapObject, string tag)
        {
            if (beatmapObject.fromPrefab && prefabObjects.TryFind(x => x.ID == beatmapObject.prefabInstanceID, out PrefabObject prefabObject))
                return beatmapObjects.FindAll(x => x.tags.Contains(tag) && x.fromPrefab && x.prefabID == prefabObject.prefabID && x.prefabInstanceID == prefabObject.ID);

            return beatmapObjects.FindAll(x => x.tags.Contains(tag) && x.prefabID == beatmapObject.prefabID && x.prefabInstanceID == beatmapObject.prefabInstanceID);
        }

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to get the parent chain of.</param>
        /// <returns>List of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public static List<BeatmapObject> GetParentChain(BeatmapObject beatmapObject)
        {
            var list = new List<BeatmapObject>();
            if (beatmapObject == null)
                return list;

            var beatmapObjects = GameData.Current.beatmapObjects;
            string parent = beatmapObject.parent;
            int index = beatmapObjects.FindIndex(x => x.id == parent);

            list.Add(beatmapObject);
            while (index >= 0)
            {
                list.Add(beatmapObjects[index]);
                parent = beatmapObjects[index].parent;
                index = beatmapObjects.FindIndex(x => x.id == parent);
            }
            return list;
        }

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to get the parent chain of.</param>
        /// <returns>List of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public static IEnumerable<DataManager.GameData.BeatmapObject> IGetParentChain(DataManager.GameData.BeatmapObject beatmapObject)
        {
            if (beatmapObject == null)
                yield break;

            var beatmapObjects = GameData.Current.beatmapObjects;
            string parent = beatmapObject.parent;
            int index = beatmapObjects.FindIndex(x => x.id == parent);

            yield return beatmapObject;
            while (index >= 0)
            {
                yield return beatmapObjects[index];
                parent = beatmapObjects[index].parent;
                index = beatmapObjects.FindIndex(x => x.id == parent);
            }
        }

        #endregion

        #region Screenshot

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

                    AnimationManager.inst?.Remove(animation.id);
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

        #endregion

        #region Misc

        /// <summary>
        /// Compares a singular object agianst an array of objects.
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

        public static void LoadResourceLevel(int type)
        {
            switch (type)
            {
                case 0: // save
                    {
                        LoadResourceLevel(0, "demo/level", "demo/level");
                        break;
                    }
                case 1: // ahead of the curve
                    {
                        LoadResourceLevel(1, "demo_new/level", "demo_new/level");
                        break;
                    }
                case 2: // new
                    {
                        LoadResourceLevel(1, "new/level", "new/level");
                        break;
                    }
                case 3: // node
                    {
                        LoadResourceLevel(0, "node/level", "node/level");
                        break;
                    }
                case 4: // video test
                    {
                        LoadResourceLevel(1, "video_test/video_test", "video_test/video_test_music", "video_test/video_test_bg");
                        break;
                    }
            }
        }

        // CoreHelper.LoadResourceLevel(0, "demo/level", "demo/level")
        public static void LoadResourceLevel(int time, string jsonPath, string audioPath, string videoClipPath = null)
        {
            var json = Resources.Load<TextAsset>($"beatmaps/{jsonPath}");
            var audio = Resources.Load<AudioClip>($"beatmaps/{audioPath}");
            var jnPlayers = JSON.Parse("{}");
            for (int i = 0; i < 4; i++)
                jnPlayers["indexes"][i] = PlayerModel.BETA_ID;

            GameData gameData = time switch
            {
                0 => ParseSave(JSON.Parse(json.text)),
                _ => GameData.Parse(JSON.Parse(LevelManager.UpdateBeatmap(json.text, "1.0.0"))),
            };

            if (jsonPath == "demo_new/level")
            {
                gameData.beatmapThemes["003051"] = new BeatmapTheme()
                {
                    id = "003051",
                    name = "PA Ahead of the Curve",
                    guiColor = new Color(0.1294f, 0.1216f, 0.1294f, 1f),
                    guiAccentColor = new Color(0.1294f, 0.1216f, 0.1294f, 1f),
                    backgroundColor = new Color(0.9686f, 0.9529f, 0.9686f, 1f),
                    backgroundColors = new List<Color>()
                    {
                        new Color(0.9686f, 0.9529f, 0.9686f, 1f),
                        new Color(0.1882f, 0.1882f, 0.1882f, 1f),
                        new Color(0.9176f, 0.9176f, 0.9176f, 1f),
                        new Color(0.7176f, 0.7176f, 0.7176f, 1f),
                        new Color(0.3059f, 0.3059f, 0.3059f, 1f),
                        new Color(0.298f, 0.298f, 0.298f, 1f),
                        new Color(0.7608f, 0.0941f, 0.3569f, 1f),
                        new Color(0.6784f, 0.0784f, 0.3412f, 1f),
                        new Color(0.5333f, 0.0549f, 0.3098f, 1f),
                    },
                    objectColors = new List<Color>()
                    {
                        Color.white,
                        new Color(0.0627f, 0.8941f, 0.3373f, 1f),
                        new Color(0.1804f, 0.3569f, 0.9686f, 1f),
                    }.Fill(15, new Color(0.1804f, 0.3569f, 0.9686f, 1f)),
                    playerColors = new List<Color>()
                    {
                        new Color(0.7569f, 0f, 0.0078f, 1f),
                        new Color(0.0471f, 0.0863f, 0.9686f, 1f),
                        new Color(0.1294f, 0.9882f, 0.0118f, 1f),
                        new Color(0.9922f, 0.5922f, 0.0039f, 1f),
                    },
                    effectColors = new List<Color>().Fill(18, Color.white),
                };
                gameData.eventObjects.allEvents[4][0].eventValues[0] = 3051;
            }

            var storyLevel = new Story.StoryLevel
            {
                json = gameData.ToJSON(true).ToString(),
                jsonPlayers = jnPlayers.ToString(),
                metadata = new MetaData
                {
                    uploaderName = "Vitamin Games",
                    creator = new LevelCreator
                    {
                        steam_name = "Pidge",
                    },
                    beatmap = new LevelBeatmap
                    {
                        name = jsonPath switch
                        {
                            "demo/level" => "Beluga Bugatti",
                            "demo_new/level" => "Ahead of the Curve",
                            "new/level" => "new",
                            "node/level" => "Node",
                            "video_test/video_test" => "miku",
                            _ => ""
                        }
                    },
                    song = new LevelSong
                    {
                        tags = new string[] { },
                        title = jsonPath switch
                        {
                            "demo/level" => "Save",
                            "demo_new/level" => "Ahead of the Curve",
                            "new/level" => "Staring Down the Barrels",
                            "node/level" => "Node",
                            "video_test/video_test" => "miku",
                            _ => ""
                        }
                    },
                    artist = new LevelArtist
                    {
                        Name = jsonPath switch
                        {
                            "demo/level" => "meganeko",
                            "demo_new/level" => "Creo",
                            "new/level" => "Creo",
                            "node/level" => "meganeko",
                            "video_test/video_test" => "miku",
                            _ => ""
                        }
                    },
                },
                music = audio,
                
            };
            storyLevel.metadata.beatmap.name = jsonPath;
            if (!string.IsNullOrEmpty(videoClipPath))
                storyLevel.videoClip = Resources.Load<UnityEngine.Video.VideoClip>($"beatmaps/{videoClipPath}");

            LevelManager.OnLevelEnd = () =>
            {
                LevelManager.OnLevelEnd = null;
                SceneHelper.LoadScene(SceneName.Main_Menu);
            };
            StartCoroutine(LevelManager.Play(storyLevel));
        }

        public static GameData ParseSave(JSONNode jn)
        {
            var gameData = new GameData();

            gameData.beatmapData = new LevelBeatmapData();
            gameData.beatmapData.checkpoints = new List<DataManager.GameData.BeatmapData.Checkpoint>();
            gameData.beatmapData.editorData = new LevelEditorData();
            gameData.beatmapData.levelData = new LevelData();

            gameData.beatmapData.markers = gameData.beatmapData.markers.OrderBy(x => x.time).ToList();

            Log($"Parsing checkpoints...");
            for (int i = 0; i < jn["levelData"]["checkpoints"].Count; i++)
            {
                var jnCheckpoint = jn["levelData"]["checkpoints"][i];
                gameData.beatmapData.checkpoints.Add(new DataManager.GameData.BeatmapData.Checkpoint(
                    jnCheckpoint["active"].AsBool,
                    jnCheckpoint["name"],
                    jnCheckpoint["time"].AsFloat,
                    new Vector2(
                        jnCheckpoint["pos"]["x"].AsFloat,
                        jnCheckpoint["pos"]["y"].AsFloat)));
            }

            Log($"Update...");
            gameData.beatmapData.checkpoints = gameData.beatmapData.checkpoints.OrderBy(x => x.time).ToList();

            Log($"Set...");
            foreach (var theme in DataManager.inst.BeatmapThemes)
                gameData.beatmapThemes.Add(theme.id, theme);

            Log($"Clear...");
            DataManager.inst.CustomBeatmapThemes.Clear();
            DataManager.inst.BeatmapThemeIndexToID.Clear();
            DataManager.inst.BeatmapThemeIDToIndex.Clear();
            if (jn["themes"] != null)
                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    var beatmapTheme = BeatmapTheme.Parse(jn["themes"][i]);

                    DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                    if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                    {
                        var list = DataManager.inst.CustomBeatmapThemes.Where(x => x.id == beatmapTheme.id).ToList();
                        var str = "";
                        for (int j = 0; j < list.Count; j++)
                        {
                            str += list[j].name;
                            if (i != list.Count - 1)
                                str += ", ";
                        }

                        if (CoreHelper.InEditor)
                            EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.name}] due to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
                    }
                    else
                    {
                        DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count - 1, int.Parse(beatmapTheme.id));
                        DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count - 1);
                    }

                    if (!string.IsNullOrEmpty(jn["themes"][i]["id"]) && !gameData.beatmapThemes.ContainsKey(jn["themes"][i]["id"]))
                        gameData.beatmapThemes.Add(jn["themes"][i]["id"], beatmapTheme);
                }

            Log($"Parsing beatmap objects...");
            for (int i = 0; i < jn["beatmapObjects"].Count; i++)
            {
                var jnObject = jn["beatmapObjects"][i];
                var beatmapObject = new BeatmapObject();
                if (!string.IsNullOrEmpty(jnObject["id"]))
                    beatmapObject.id = jnObject["id"];
                if (!string.IsNullOrEmpty(jnObject["parent"]))
                    beatmapObject.parent = jnObject["parent"];
                beatmapObject.depth = jnObject["layer"].AsInt;
                beatmapObject.objectType = jnObject["helper"].AsBool ? BeatmapObject.ObjectType.Helper : BeatmapObject.ObjectType.Normal;
                beatmapObject.StartTime = jnObject["startTime"].AsFloat;
                beatmapObject.name = jnObject["name"];
                beatmapObject.origin = Parser.TryParse(jnObject["origin"], Vector2.zero);
                beatmapObject.editorData = new ObjectEditorData(jnObject["editorData"]["bin"].AsInt, jnObject["editorData"]["layer"].AsInt, false, false);

                var events = new List<List<DataManager.GameData.EventKeyframe>>();
                events.Add(new List<DataManager.GameData.EventKeyframe>());
                events.Add(new List<DataManager.GameData.EventKeyframe>());
                events.Add(new List<DataManager.GameData.EventKeyframe>());
                events.Add(new List<DataManager.GameData.EventKeyframe>());

                float eventTime = 0f;
                var lastPos = Vector2.zero;
                var lastSca = Vector2.one;
                var lastCol = 0;

                for (int j = 0; j < jnObject["events"].Count; j++)
                {
                    var jnEvent = jnObject["events"][j];
                    eventTime += jnEvent["eventTime"].AsFloat;

                    bool hasPos = false;
                    bool hasSca = false;
                    bool hasRot = false;
                    bool hasCol = false;

                    for (int k = 0; k < jnEvent["eventParts"].Count; k++)
                    {
                        switch (jnEvent["eventParts"][k]["kind"].AsInt)
                        {
                            case 0:
                                {
                                    if (hasPos)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastPos = new Vector2(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1]);
                                    events[0].Add(eventKeyframe);
                                    hasPos = true;
                                    break;
                                }
                            case 1:
                                {
                                    if (hasSca)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastSca = new Vector2(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1]);
                                    events[1].Add(eventKeyframe);
                                    hasSca = true;
                                    break;
                                }
                            case 2:
                                {
                                    if (hasRot)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0) { relative = true, };
                                    events[2].Add(eventKeyframe);
                                    hasRot = true;
                                    break;
                                }
                            case 3:
                                {
                                    if (hasCol)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, }, new float[] { });
                                    lastCol = (int)eventKeyframe.eventValues[0];
                                    events[3].Add(eventKeyframe);
                                    hasCol = true;
                                    break;
                                }
                        }
                    }

                    if (!hasPos)
                        events[0].Add(new EventKeyframe(eventTime, new float[] { lastPos.x, lastPos.y }, new float[] { }));
                    if (!hasSca)
                        events[1].Add(new EventKeyframe(eventTime, new float[] { lastSca.x, lastSca.y }, new float[] { }));
                    if (!hasRot)
                        events[2].Add(new EventKeyframe(eventTime, new float[] { 0f }, new float[] { }) { relative = true });
                    if (!hasCol)
                        events[3].Add(new EventKeyframe(eventTime, new float[] { lastCol }, new float[] { }));

                    if (i == 0)
                    {
                        Log($"obj\nhasPos: {hasPos}\nlastPos: {lastPos}");
                    }
                }

                beatmapObject.events = events;

                gameData.beatmapObjects.Add(beatmapObject);
            }

            AssetManager.SpriteAssets.Clear();

            Log($"Parsing background objects...");
            for (int i = 0; i < jn["backgroundObjects"].Count; i++)
            {
                var jnObject = jn["backgroundObjects"][i];
                gameData.backgroundObjects.Add(new BackgroundObject()
                {
                    name = jnObject["name"],
                    kind = jnObject["kind"].AsInt,
                    pos = Parser.TryParse(jnObject["pos"], Vector2.zero),
                    scale = Parser.TryParse(jnObject["size"], Vector2.zero),
                    rot = jnObject["rot"].AsFloat,
                    color = jnObject["color"].AsInt,
                    layer = jnObject["layer"].AsInt,
                    drawFade = jnObject["fade"].AsBool,
                    reactive = jnObject["reactiveSettings"]["active"].AsBool,
                });
            }

            var allEvents = new List<List<DataManager.GameData.EventKeyframe>>();
            allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // move
            allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // zoom
            allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // rotate
            allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // shake
            allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // theme

            Log($"Parsing event objects...");
            var eventObjects = jn["eventObjects"].Children.OrderBy(x => x["startTime"].AsFloat).ToList();
            var lastMove = Vector2.zero;
            var lastZoom = 0f;
            var lastRotate = 0f;
            //var lastShake = 0f;
            for (int i = 0; i < eventObjects.Count; i++)
            {
                var jnObject = eventObjects[i];
                var startTime = jnObject["startTime"].AsFloat;
                var eventTime = jnObject["eventTime"].AsFloat;

                bool hasMove = false;
                bool hasZoom = false;
                bool hasRotate = false;
                //bool hasShake = false;

                for (int j = 0; j < jnObject["events"].Count; j++)
                {
                    var jnEvent = jnObject["events"][j];

                    switch (jnEvent["kind"].AsInt)
                    {
                        case 0:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastMove.x, lastMove.y, }, new float[] { });
                                allEvents[0].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, jnEvent["value1"].AsFloat, }, new float[] { });
                                lastMove = new Vector2(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1]);
                                allEvents[0].Add(eventKeyframe);
                                hasMove = true;
                                break;
                            }
                        case 1:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastZoom, }, new float[] { });
                                allEvents[1].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, }, new float[] { });
                                lastZoom = eventKeyframe.eventValues[0];
                                allEvents[1].Add(eventKeyframe);
                                hasZoom = true;
                                break;
                            }
                        case 2:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastRotate, }, new float[] { });
                                allEvents[2].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, }, new float[] { });
                                lastRotate = eventKeyframe.eventValues[0];
                                allEvents[2].Add(eventKeyframe);
                                hasRotate = true;
                                break;
                            }
                        //case 3:
                        //    {
                        //        var eventKeyframe = new EventKeyframe(startTime, new float[] { lastShake, }, new float[] { });
                        //        allEvents[3].Add(eventKeyframe);

                        //        eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat * 0.1f, }, new float[] { });
                        //        lastShake = eventKeyframe.eventValues[0];
                        //        allEvents[3].Add(eventKeyframe);
                        //        hasShake = true;
                        //        break;
                        //    }
                    }

                    if (!hasMove)
                    {
                        allEvents[0].Add(new EventKeyframe(startTime, new float[] { lastMove.x, lastMove.y }, new float[] { }));
                        allEvents[0].Add(new EventKeyframe(startTime + eventTime, new float[] { lastMove.x, lastMove.y }, new float[] { }));
                    }
                    if (!hasZoom)
                    {
                        allEvents[1].Add(new EventKeyframe(startTime, new float[] { lastZoom }, new float[] { }));
                        allEvents[1].Add(new EventKeyframe(startTime + eventTime, new float[] { lastZoom }, new float[] { }));
                    }
                    if (!hasRotate)
                    {
                        allEvents[2].Add(new EventKeyframe(startTime, new float[] { lastRotate }, new float[] { }));
                        allEvents[2].Add(new EventKeyframe(startTime + eventTime, new float[] { lastRotate }, new float[] { }));
                    }
                    //if (!hasShake)
                    //{
                    //    allEvents[3].Add(new EventKeyframe(startTime, new float[] { lastShake }, new float[] { }));
                    //    allEvents[3].Add(new EventKeyframe(startTime + eventTime, new float[] { lastShake }, new float[] { }));
                    //}
                }
            }

            allEvents[4].Add(new EventKeyframe(0f, new float[] { 1f }, new float[] { }));

            gameData.eventObjects = new DataManager.GameData.EventObjects();
            gameData.eventObjects.allEvents = allEvents;

            GameData.ClampEventListValues(gameData.eventObjects.allEvents, GameData.EventCount);

            return gameData;
        }

        #endregion
    }
}
