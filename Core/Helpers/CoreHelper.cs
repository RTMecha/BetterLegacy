﻿using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
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
using CielaSpike;

namespace BetterLegacy.Core.Helpers
{
    public static class CoreHelper
    {
        #region Properties

        public static float ScreenScale => Screen.width / 1920f;
        public static float ScreenScaleInverse => 1f / ScreenScale;

        public static bool InEditor => EditorManager.inst;
        public static bool InGame => GameManager.inst;
        public static bool InMenu => ArcadeManager.inst.ic;

		public static bool Paused => GameManager.inst && GameManager.inst.gameState == GameManager.State.Paused;
		public static bool Playing => GameManager.inst && GameManager.inst.gameState == GameManager.State.Playing;

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

        public static float Pitch => EditorManager.inst != null ? 1f : new List<float>
            { 0.1f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 3f, }[Mathf.Clamp(DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2), 0, 7)];

        public static Data.BeatmapTheme CurrentBeatmapTheme => EditorManager.inst && EventEditor.inst.showTheme ? (Data.BeatmapTheme)EventEditor.inst.previewTheme : (Data.BeatmapTheme)GameManager.inst?.LiveTheme;

		public static bool AprilFools => System.DateTime.Now.ToString("M") == "1 April" || System.DateTime.Now.ToString("M") == "April 1";

		public static Vector2Int CurrentResolution => GetResolution((int)CoreConfig.Instance.Resolution.Value);

		public static Vector2Int GetResolution(int resolution) => new Vector2Int((int)DataManager.inst.resolutions[resolution].x, (int)DataManager.inst.resolutions[resolution].y);

		public static DataManager.Difficulty GetDifficulty(int difficulty)
			=> difficulty >= 0 && difficulty < DataManager.inst.difficulties.Count ?
			DataManager.inst.difficulties[difficulty] : new DataManager.Difficulty("Unknown Difficulty", LSColors.HexToColor("424242"));

		#endregion

		#region Unity

		public static void Destroy(UnityEngine.Object obj, bool instant = false, float t = 0f)
        {
			if (instant)
            {
				UnityEngine.Object.DestroyImmediate(obj);
				return;
            }

			UnityEngine.Object.Destroy(obj, t);
        }
		public static Coroutine StartCoroutine(IEnumerator routine) => LegacyPlugin.inst.StartCoroutine(routine);
		public static Coroutine StartCoroutineAsync(IEnumerator routine) => LegacyPlugin.inst.StartCoroutineAsync(routine);

        #endregion

        /// <summary>
        /// Compares given values and invokes a method if they are not the same.
        /// </summary>
        /// <param name="prev">The previous value.</param>
        /// <param name="current">The current value.</param>
        /// <param name="action">The method to invoke if the parameters are not the same.</param>
        /// <returns>Returns true if previous value is not equal to current value, otherwise returns false.</returns>
        public static bool UpdateValue(bool prev, bool current, Action<bool> action)
        {
			bool value = prev != current;
			if (value)
				action?.Invoke(current);
			return value;
		}

		/// <summary>
		/// Compares given values and invokes a method if they are not the same.
		/// </summary>
		/// <param name="prev">The previous value.</param>
		/// <param name="current">The current value.</param>
		/// <param name="action">The method to invoke if the parameters are not the same.</param>
		/// <returns>Returns true if previous value is not equal to current value, otherwise returns false.</returns>
		public static bool UpdateValue(float prev, float current, Action<float> action)
        {
			bool value = prev != current;
			if (value)
				action?.Invoke(current);
			return value;
		}

		/// <summary>
		/// Compares given values and invokes a method if they are not the same.
		/// </summary>
		/// <param name="prev">The previous value.</param>
		/// <param name="current">The current value.</param>
		/// <param name="action">The method to invoke if the parameters are not the same.</param>
		/// <returns>Returns true if previous value is not equal to current value, otherwise returns false.</returns>
		public static bool UpdateValue(int prev, int current, Action<int> action)
        {
			bool value = prev != current;
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

		public static void LogInit(string className) => Debug.Log($"{className}" +
				$"---------------------------------------------------------------------\n" +
				$"---------------------------- INITIALIZED ----------------------------\n" +
				$"---------------------------------------------------------------------\n");

        #endregion

        #region Color

        public static string ColorToHex(Color32 color) => color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");

        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            double num;
            double saturation;
            double value;
            LSColors.ColorToHSV(color, out num, out saturation, out value);
            return LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        public static Color InvertColorHue(Color color)
        {
            double num;
            double saturation;
            double value;
            LSColors.ColorToHSV(color, out num, out saturation, out value);
            return LSColors.ColorFromHSV(num - 180.0, saturation, value);
        }

        public static Color InvertColorValue(Color color)
        {
            double num;
            double sat;
            double val;
            LSColors.ColorToHSV(color, out num, out sat, out val);

            if (val < 0.5)
            {
                val = -val + 1;
            }
            else
            {
                val = -(val - 1);
            }

            return LSColors.ColorFromHSV(num, sat, val);
        }

        #endregion

        #region Strings

        public static float TimeCodeToFloat(string str)
        {
            if (RegexMatch(str, new Regex(@"([0-9]+):([0-9]+):([0-9.]+)"), out Match match1))
            {
                var hours = float.Parse(match1.Groups[1].ToString()) * 3600f;
                var minutes = float.Parse(match1.Groups[2].ToString()) * 60f;
                var seconds = float.Parse(match1.Groups[3].ToString());

                return hours + minutes + seconds;
            }
            else if (RegexMatch(str, new Regex(@"([0-9]+):([0-9.]+)"), out Match match2))
            {
                var minutes = float.Parse(match2.Groups[1].ToString()) * 60f;
                var seconds = float.Parse(match2.Groups[2].ToString());

                return minutes + seconds;
            }

            return 0f;
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

		#region Misc

		public static IEnumerator Empty()
		{
			yield break;
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
			text.font = FontManager.inst.Inconsolata;
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
			titleText.font = FontManager.inst.Inconsolata;
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
			text.font = FontManager.inst.Inconsolata;
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
