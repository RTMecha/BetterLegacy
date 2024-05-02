using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;
using LSFunctions;

using UnityAnimation = UnityEngine.Animation;
using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;

namespace BetterLegacy.Example
{
	public delegate bool DialogueFunction();

    public class ExampleManager : MonoBehaviour
	{
		public static ExampleManager inst;
		public static string className = "[<color=#3F59FC>ExampleManager</color>]\n";
		bool spawning = false;

		public bool Visible => baseCanvas && baseCanvas.activeSelf;

		public static bool DebugsOn => false;

		#region Sprites

		public string speakURL = "https://cdn.discordapp.com/attachments/811214540141363201/1151208881125593108/example_speak.ogg";
		public string tailURL = "https://media.discordapp.net/attachments/811214540141363201/1151188714769354954/example_tail.png";
		public string earBottomURL = "https://media.discordapp.net/attachments/811214540141363201/1151188681588211802/example_ear_bottom.png";
		public string headURL = "https://media.discordapp.net/attachments/811214540141363201/1151188682540323016/example_head.png";
		public string eyesURL = "https://media.discordapp.net/attachments/811214540141363201/1151188682078961734/example_eyes.png";
		public string pupilsURL = "https://media.discordapp.net/attachments/811214540141363201/1151188714131832893/example_pupils.png";
		public string blinkURL = "https://media.discordapp.net/attachments/811214540141363201/1151188681080705094/example_blink.png";
		public string snoutURL = "https://media.discordapp.net/attachments/811214540141363201/1151188714412843098/example_snout.png";
		public string mouthURL = "https://media.discordapp.net/attachments/811214540141363201/1151188683056218173/example_mouth.png";
		public string lipsURL = "https://media.discordapp.net/attachments/811214540141363201/1151188682812965004/example_lips.png";
		public string noseURL = "https://media.discordapp.net/attachments/811214540141363201/1151188683295297536/example_nose.png";
		public string browsURL = "https://media.discordapp.net/attachments/811214540141363201/1151188681315598456/example_brow.png";
		public string earTopURL = "https://media.discordapp.net/attachments/811214540141363201/1151188681835679754/example_ear_top.png";
		public string handsURL = "https://media.discordapp.net/attachments/811214540141363201/1151188682334797844/example_hand.png";

		public string SpeakPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example speak.ogg";
		public string TailPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example tail.png";
		public string EarBottomPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear bottom.png";
		public string HeadPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example head.png";
		public string EyesPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example eyes.png";
		public string PupilsPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example pupils.png";
		public string BlinkPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example blink.png";
		public string SnoutPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example snout.png";
		public string MouthPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example mouth.png";
		public string LipsPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example lips.png";
		public string NosePath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example nose.png";
		public string BrowsPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example brow.png";
		public string EarTopPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear top.png";

		public string HandsPath => RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example hand.png";

		#endregion

		#region Movement

		public Vector2 TotalPosition
        {
			get
            {
				if (parentX == null || parentY == null) return Vector2.zero;
				return new Vector2(parentX.localPosition.x, parentY.localPosition.y);
            }
        }

		public float floatingLevel;

        #endregion

        #region Parents

        public Transform EditorParent
		{
			get
			{
				return EditorManager.inst.GUIMain.transform;
			}
		}

		public Canvas canvas;
		public GameObject baseCanvas;
		public CanvasGroup canvasGroup;

		public Transform floatingParent;

		public Transform parentX;
		public Transform parentY;
		public Transform parentRotscale;

		public Transform head;

		public Transform faceX;
		public Transform faceY;

		public Transform ears;
		public Transform earBottomLeft;
		public Transform earBottomRight;
		
		public Transform earTopLeft;
		public Transform earTopRight;

		public Transform eyes;
		public Transform pupils;
		public Transform blink;

		public Transform snout;
		public Transform nose;
		public Transform mouthBase;
		public Transform mouthUpper;
		public Transform mouthLower;
		public Transform lips;

		public Transform browBase;
		public Transform browLeft;
		public Transform browRight;

		public Transform handsBase;
		public Transform handLeft;
		public Transform handRight;

		public Transform tail;

		#endregion

		#region Dialogue

		bool previewSay;
		bool previewSayCanChange = true;

		public string lastDialogue;

		public AudioClip speakSound;

        public Text dialogueText;
		public Image dialogueImage;
		public Transform dialogueBase;

		public string[] dialogues = new string[]
		{
			"Hey!",
			"You should go touch some grass.",
			"How are you doing there?"
		};

		public Dictionary<string, Dialogue> dialogueDictionary = new Dictionary<string, Dialogue>
		{
			{ "LoadedLevel", new Dialogue(new string[] { "Level has loaded! Have fun building.", "Have fun!", "I hope you enjoy the building process.", "Whatcha building today?" }, SayAnyways, delegate () { }) },
			{ "OnPreview", new Dialogue(new string[] { "This looks amazing so far! :3", "I think it could use a little more something...", "Huh?", "This just needs more work... I think..." }, HasLoadedLevel, delegate ()
			{

			}) },
		};

		public List<Dialogue> occasionalDialogues = new List<Dialogue>
		{
			new Dialogue("Seems like you have no levels...", LevelCount),
			new Dialogue("Maybe you should make something.", LevelCount),
			new Dialogue("Uh oh... I hope your computer isn't crashing...", ObjectsAlive),
			new Dialogue("Hmmm...", SayAnyways),
			new Dialogue("What's up?", SayAnyways),
			new Dialogue("How are you doing so far?", SayAnyways),
			new Dialogue("Hey! You should probably have a break. Just saying.", TimeLongerThan10Hours),
			new Dialogue("Go touch some grass.", TimeLongerThan10Hours),

			new Dialogue("*Caw caw*", UserIsTori),
			new Dialogue("CrowBirb moment", UserIsTori),
			new Dialogue("A four dimensional tesseract.", UserIsCubeCube),
			new Dialogue("penis monkey", UserIsDiggy),
			new Dialogue("So... where's all the cookies?", UserIsMecha),
			new Dialogue("Hey, you might want to set a username.", UserIsPlayer),
			new Dialogue("Zzzzzzz...", UserIsSleepyz, delegate ()
			{
				var a = inst.allowBlinking;
				inst.allowBlinking = false;
				var animation = new RTAnimation("Sleepy");
				animation.animationHandlers = new List<AnimationHandlerBase>
				{
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 1f, Ease.Linear),
						new FloatKeyframe(0.5f, 0.3f, Ease.SineInOut),
						new FloatKeyframe(0.55f, 0f, Ease.SineIn),
						new FloatKeyframe(0.6f, 0.3f, Ease.SineOut),
						new FloatKeyframe(0.65f, 0f, Ease.SineIn),
						new FloatKeyframe(0.7f, 0.3f, Ease.SineOut),
						new FloatKeyframe(1f, 1f, Ease.SineInOut),
						new FloatKeyframe(1.1f, 1f, Ease.Linear),
					}, delegate (float x)
					{
						inst.eyes.localScale = new Vector3(1f, x, 1f);
					}),
				};

				animation.onComplete = delegate ()
				{
					inst.allowBlinking = a;
				};

				inst.PlayOnce(animation, false);
			}),

			new Dialogue("THIS IS SO MUCH FUN! :D", BeingDragged),
			new Dialogue("Oh that's craazy.", BeingDragged),
			new Dialogue("Oh...", LeftHandBeingDragged),
			new Dialogue("Maybe you should open a level?", HasNotLoadedLevel),
			new Dialogue("I wonder what these levels are like...", HasNotLoadedLevel),
			new Dialogue("Hello...?", ApplicationNotFocused),
			new Dialogue("Are you there?", ApplicationNotFocused),
			new Dialogue("Uhhh...", ApplicationNotFocused),
			new Dialogue("Where'd you go?", ApplicationNotFocused),
		};

		float repeat = 60f;
		bool said = false;
		public bool canSayThing = false;

		public static bool CanSay() => inst.canSayThing;

		public static bool ApplicationNotFocused() => !Application.isFocused;

		public static bool HasNotLoadedLevel() => EditorManager.inst && !EditorManager.inst.hasLoadedLevel && EditorManager.inst.loadedLevels.Count > 0;

		public static bool HasLoadedLevel() => EditorManager.inst && EditorManager.inst.hasLoadedLevel;

		public static bool LeftHandBeingDragged() => inst.draggingLeftHand;

		public static bool RightHandBeingDragged() => inst.draggingRightHand;

		public static bool BeingDragged() => inst.dragging;

		public static bool UserIsSleepyz() => CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyz" || CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyzgamer";

		public static bool UserIsMecha() => CoreConfig.Instance.DisplayName.Value == "RTMecha";

		public static bool UserIsDiggy() =>
			CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "diggydog" ||
			CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "diggydog176";

		public static bool UserIsCubeCube() =>
			CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "cubecube";

		public static bool UserIsTori() => CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "karasutori";
		public static bool UserIsPlayer() => CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "player";

		public static bool TimeLongerThan10Hours() => Time.time > 36000f;

		public static bool SayAnyways() => true;

		public static bool ObjectsAlive() => DataManager.inst.gameData != null && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.TimeWithinLifespan()).Count > 900;

		public static bool LevelCount() =>  EditorManager.inst && EditorManager.inst.loadedLevels.Count <= 0;

		public void RepeatDialogues()
		{
			if (!talking)
			{
				float t = time % repeat;
				if (t > repeat - 1f)
				{
					if (!said)
					{
						said = true;
						int random = UnityEngine.Random.Range(0, occasionalDialogues.Count - 1);

						if (occasionalDialogues[random].CanSay)
                        {
							Say(occasionalDialogues[random].text);
							occasionalDialogues[random].Action();
                        }
					}

				}
				else
					said = false;
			}
		}

		int currentDialogueIndex = 0;

		public int GetCurrentDialogueLength() => CurrentDialogueLength;

		public int CurrentDialogueLength
		{
			get
			{
				return CurrentDialogue.Length;
			}
		}

		public int CurrentDialogueIndex
		{
			get
			{
				return Mathf.Clamp(currentDialogueIndex, 0, dialogues.Length - 1);
			}
			set
            {
				currentDialogueIndex = Mathf.Clamp(value, 0, dialogues.Length - 1);
            }
		}

		public string CurrentDialogue
		{
			get
			{
				return dialogues[CurrentDialogueIndex];
			}
		}

		public class Dialogue
		{
			public Dialogue(string text, DialogueFunction dialogueFunction, Action action = null)
			{
				this.text = text;
				this.dialogueFunction = dialogueFunction;
				this.action = action;
			}

			public Dialogue(string[] dialogues, DialogueFunction dialogueFunction, Action action)
            {
				this.dialogues = dialogues;
				this.dialogueFunction = dialogueFunction;
				this.action = action;
            }

			public bool CanSay => dialogueFunction != null && dialogueFunction.Invoke() && canSay;

			public void Action() => action?.Invoke();

			public string[] dialogues;
			public string text;
			public DialogueFunction dialogueFunction;
			public bool canSay = true;

			Action action;
		}

		#endregion

		#region Tracking

		bool faceCanLook = true;
		float faceLookMultiplier = 0.006f;

		Vector2 pupilsOffset;
		float pupilsLookRate = 3f;
		bool pupilsCanChange = true;

		bool allowBlinking = true;

		float blinkRate = 5f;

		bool blinkCanChange = true;
		bool canBlink = true;
		int blinkChance = 45;

		public bool lookAt = true;

		float lookMultiplier = 0.004f;

		public Vector2 MousePosition
		{
			get
			{
				if (!Cursor.visible && GameObject.Find("Players/Player 1/Player"))
				{
					var p = GameObject.Find("Players/Player 1/Player").transform.position;

					p = Camera.main.WorldToScreenPoint(p);

					return p /* * RTHelpers.screenScaleInverse*/;
				}

				return Input.mousePosition /* * RTHelpers.screenScaleInverse*/;
			}
		}

		public Vector3[] pointsOfInterest = new Vector3[]
		{
			new Vector3(0f, 0f)
		};

        #endregion

        #region Tutorials

		public enum GuideType
        {
			Beginner,
			Familiar,
			Companion
        }

		public GuideType guideType = GuideType.Beginner;

		public Dictionary<string, bool> tutorials = new Dictionary<string, bool>
		{
			{ "Events Basics", false },
			{ "Events Modded", false },
		};

		public void LoadTutorials()
        {

        }

		public void SaveTutorials()
        {

        }

		#endregion

		#region Dragging

		public bool draggingLeftHand = false;
		public bool draggingRightHand = false;

		public bool dragging = false;
		public Vector2 startDragPos;
		public Vector3 startMousePos;
		public Vector3 dragPos;

		public Vector3 lastPos;

		public float dragDelay = 0.2f;

		#endregion

		#region Memories

		public JSONNode memory = JSON.Parse("{}");

		public void LoadMemory()
        {
			if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile") && RTFile.FileExists(RTFile.ApplicationDirectory + "profile/example.txt"))
            {
                memory = JSON.Parse(FileManager.inst.LoadJSONFile("profile/example.txt"));
            }
        }

		#endregion

		#region Talk

		public InputField chatter;
		public Transform chatterBase;

		public bool chatting = false;

		float timeSinceLastInteractedOffset = 0f;
		float timeSinceLastInteracted = 0f;

		public Action chatAction = delegate ()
		{
			if (inst.chatter == null || inst.chatting)
				return;

			inst.chatting = true;

			string text = inst.chatter.text;
			string toLower = text.ToLower();
			var words = text.Split(' ').ToList();
			if (words[0].ToLower() == "hello" || words[0].ToLower() == "hi" || words[0].ToLower() == "hey")
			{
				string[] randomGreeting = new string[]
				{
					$"Hey, {CoreConfig.Instance.DisplayName.Value}! How are you doing?",
					$"Hello {CoreConfig.Instance.DisplayName.Value}!",
					$"What's up, {CoreConfig.Instance.DisplayName.Value}?",
					$"{CoreConfig.Instance.DisplayName.Value}! What are you up to today?",
				};

				inst.Say(randomGreeting[UnityEngine.Random.Range(0, randomGreeting.Length)], onComplete: delegate () { inst.chatting = false; });
			}
			else if (EditorManager.inst && toLower.Contains("flip") && toLower.Contains("object"))
			{
				var timelineObjects = ObjectEditor.inst.SelectedObjects;
				bool hasFlipped = false;
				foreach (var timelineObject in timelineObjects)
                {
                    if (timelineObject.IsBeatmapObject)
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.name = CoreHelper.Flip(beatmapObject.name);

                        for (int i = 0; i < 3; i++)
                        {
                            foreach (var kf in beatmapObject.events[i])
                            {
                                kf.eventValues[0] = -kf.eventValues[0];
                            }
                        }

						ObjectEditor.inst.RenderTimelineObject(timelineObject);

						Updater.UpdateProcessor(beatmapObject, "Keyframes");
						hasFlipped = true;
					}
				}

				if (timelineObjects.Count == 1 && timelineObjects[0].IsBeatmapObject)
					CoreHelper.StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObjects[0].GetData<BeatmapObject>()));

				if (hasFlipped)
				{
					var animation = new RTAnimation("Flip!");
					animation.animationHandlers = new List<AnimationHandlerBase>
					{
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, 0f, Ease.Linear),
							new FloatKeyframe(0.2f, 360f, Ease.SineOut),
							new FloatKeyframe(0.22f, 0f, Ease.Instant),
							new FloatKeyframe(0.24f, 0f, Ease.Linear)
						}, delegate (float x)
						{
							inst.parentY.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
					};

					animation.onComplete = delegate ()
					{
						inst.animations.Remove(animation);

						animation = null;
					};

					inst.animations.Add(animation);

					animation.ResetTime();

					animation.Play();

					inst.Say("Object flipped!", onComplete: delegate () { inst.chatting = false; });
				}
				else
					inst.Say("I couldn't flip that... sorry...", onComplete: delegate () { inst.chatting = false; });
			}
			else if (toLower.Contains("give") && toLower.Contains("random") && toLower.Contains("number"))
			{
				var randomString = LSText.randomNumString(UnityEngine.Random.Range(1, 16));

				LSText.CopyToClipboard(randomString);

				inst.Say("Okay, here's a random number: " + randomString + ". It has been copied to your clipboard!", onComplete: delegate () { inst.chatting = false; });
			}
			else if (toLower.Contains("run") && toLower.Contains("code") && inst.RegexMatch(new Regex(@"from: \((.*?)\)"), text, out Match coder))
			{
				Debug.LogFormat("{0}Path: {1}", className, coder.Groups[1].ToString());
				if (RTFile.FileExists(coder.Groups[1].ToString()))
				{
					RTCode.Evaluate(RTFile.ReadFromFile(coder.Groups[1].ToString()));
					inst.Say("Running code from: " + coder.Groups[1].ToString() + "!", onComplete: delegate () { inst.chatting = false; });
				}
				else
					inst.Say("I couldn't run that code... sorry.", onComplete: delegate () { inst.chatting = false; });
			}
			else inst.chatting = false;
		};

		public void HandleChatting() => chatAction();

		public bool RegexMatch(Regex regex, string text, out Match match)
        {
			match = regex.Match(text);
			return match.Success;
        }

		#endregion

		#region Delegates

		public static Action<EditorManager> onEditorAwake = delegate (EditorManager x) { };
		public static Action<bool> onEditorToggle = delegate (bool x) { };
		public static Action<string> onSceneLoad = delegate (string x) { };
		public static Action<GameManager> onGameAwake = delegate (GameManager x) { };

		public static Action onInit = delegate ()
		{
			var gameObject = new GameObject("ExampleManager");
			gameObject.transform.SetParent(SystemManager.inst.transform);
			gameObject.AddComponent<ExampleManager>();
		};

		public Action update;
		public Action fixedUpdate;
		public Action lateUpdate;
		public Action onSpawnComplete = delegate () { };

		public Action headSpawned = delegate () { };

		#endregion

		float time = 0f;

		float timeOffset;

		public bool talking = false;

		public static bool addedOnLevelLoad = false;

		public bool downloadMode = false;

		/// <summary>
		/// Spawns Example.
		/// </summary>
		public static void Init()
		{
			if (inst || !ExampleConfig.Instance.ExampleSpawns.Value)
				return;

			onInit?.Invoke();
		}

		void Awake()
		{
			timeOffset = Time.time;
			timeSinceLastInteractedOffset = Time.time;

			if (inst == null)
				inst = this;
			else if (inst != this)
				Kill();

			try
			{
				LoadMemory();
				LoadTutorials();
			}
			catch
            {

            }

			StartCoroutine(SpawnExample());
			
			onSceneLoad = delegate (string x)
			{
				if (inst && baseCanvas)
					SetParent(baseCanvas.transform);
			};
        }

		void Update()
		{
			time = Time.time - timeOffset;
			timeSinceLastInteracted = Time.time - timeSinceLastInteractedOffset;

			if (Input.GetKeyDown(ExampleConfig.Instance.ExampleVisiblityToggle.Value) && !LSHelpers.IsUsingInputField())
				ExampleConfig.Instance.ExampleVisible.Value = !ExampleConfig.Instance.ExampleVisible.Value;

			update?.Invoke();

			if (animations == null || !Visible)
				return;

			for (int i = 0; i < animations.Count; i++)
			{
				if (animations[i].playing)
					animations[i].Update();
			}

			if (!spawning && allowBlinking && !dragging)
			{
				float t = time % blinkRate;

				if (t > blinkRate - 0.3f && t < blinkRate && blinkCanChange)
					canBlink = UnityEngine.Random.Range(0, 100) > blinkChance;


				if (t > blinkRate - 0.3f && t < blinkRate && canBlink)
				{
					blinkCanChange = false;
					blink?.gameObject?.SetActive(true);
				}
				else
				{
					blinkCanChange = true;
					blink?.gameObject?.SetActive(false);
				}
			}
			else
			{
				blink?.gameObject?.SetActive(true);
			}

			RepeatDialogues();

			floatingLevel = time * 0.5f % 2f;

			if (floatingParent != null)
				floatingParent.localPosition = new Vector3(0f, (Ease.SineInOut(floatingLevel) - 0.5f) * 2f, 0f);

			if (chatterBase != null)
				chatterBase.localPosition = new Vector3(TotalPosition.x, TotalPosition.y - 110f, 0f);

			if (TotalPosition.x < 130f && TotalPosition.y > -80f && EditorManager.inst && !EditorManager.inst.isEditing)
			{
				if (previewSayCanChange)
				{
					previewSay = UnityEngine.Random.Range(0, 100) > 45;
					previewSayCanChange = false;
				}

				if (dialogueDictionary["OnPreview"].CanSay && previewSay && !talking)
				{
					dialogueDictionary["OnPreview"].canSay = false;
					var dialogues = dialogueDictionary["OnPreview"].dialogues;
					Say(dialogues[UnityEngine.Random.Range(0, dialogues.Length - 1)]);
				}
			}
			else { dialogueDictionary["OnPreview"].canSay = true; previewSayCanChange = true; }
		}

		void FixedUpdate()
        {
            fixedUpdate?.Invoke();

            if (baseCanvas && canvas)
            {
				if (EditorManager.inst && EditorManager.inst.isEditing)
					baseCanvas.SetActive(ExampleConfig.Instance.EnabledInEditor.Value);
				else if (GameManager.inst)
					baseCanvas.SetActive(ExampleConfig.Instance.EnabledInArcade.Value);
				else if (ArcadeManager.inst.ic)
					baseCanvas.SetActive(ExampleConfig.Instance.EnabledInMenus.Value);

				canvas.scaleFactor = CoreHelper.ScreenScale;
			}

			if (!addedOnLevelLoad && ModCompatibility.sharedFunctions.ContainsKey("EditorOnLoadLevel"))
			{
				addedOnLevelLoad = true;
				Action action = (Action)ModCompatibility.sharedFunctions["EditorOnLoadLevel"];
				action += delegate ()
				{
					var dialogues = dialogueDictionary["LoadedLevel"].dialogues;
					Say(dialogues[dialogues.Length - 1]);
				};
				ModCompatibility.sharedFunctions["EditorOnLoadLevel"] = action;
			}
		}

		void LateUpdate()
		{
			lateUpdate?.Invoke();

			if (!Application.isFocused || spawning || !Visible)
				return;

			if (pupils != null && lookAt)
			{
				float t = time % pupilsLookRate;

				if (t > pupilsLookRate - 0.3f && t < pupilsLookRate && pupilsCanChange)
					pupilsOffset = new Vector2(UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f));

				if (t > pupilsLookRate - 0.3f && t < pupilsLookRate)
					pupilsCanChange = false;
				else
					pupilsCanChange = true;

				((RectTransform)pupils).anchoredPosition = RTMath.Lerp(Vector2.zero, MousePosition - new Vector2(pupils.position.x, pupils.position.y), lookMultiplier) + pupilsOffset;

				if (faceCanLook)
				{
					var lerp = RTMath.Lerp(Vector2.zero, MousePosition - new Vector2(faceX.position.x, faceY.position.y), faceLookMultiplier);
					faceX.localPosition = new Vector3(lerp.x, 0f, 0f);
					faceY.localPosition = new Vector3(0f, lerp.y, 0f);
					mouthBase.localPosition = new Vector3(lerp.x, (lerp.y * 0.5f) + -30f, 0f);
					nose.localPosition = new Vector3(lerp.x, (lerp.y * 0.5f) + -20f, 0f);
				}
			}

			if (faceX != null && tail != null && ears != null)
			{
				float x = faceX.localPosition.x * 5f;

				tail.localRotation = Quaternion.Euler(0f, 0f, -x);
				tail.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -x);
				ears.localRotation = Quaternion.Euler(0f, 0f, faceX.localPosition.x * 0.8f);
			}

			if (dragging)
			{
				Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;

				float p = Time.deltaTime * 60f;
				float po = 1f - Mathf.Pow(1f - Mathf.Clamp(dragDelay, 0.001f, 1f), p);

				float x = startMousePos.x - vector.x;
				float y = startMousePos.y - vector.y;

				var target = new Vector3(startDragPos.x + -x, startDragPos.y + -y);

				parentRotscale.localRotation = Quaternion.Euler(0f, 0f, (target.x - dragPos.x) * po);

				dragPos += (target - dragPos) * po;

				parentX.localPosition = new Vector3(dragPos.x, 0f);
				parentY.localPosition = new Vector3(0f, dragPos.y);

				faceX.localPosition = new Vector3(-((target.x - dragPos.x) * po), 0f);
				faceY.localPosition = new Vector3(0f, -((target.y - dragPos.y) * po));

				if (Input.GetKeyDown(KeyCode.G))
				{
					chatterBase.gameObject.SetActive(!chatterBase.gameObject.activeSelf);
				}
			}

			if (draggingLeftHand)
			{
				Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);

				float p = Time.deltaTime * 60f;
				float po = 1f - Mathf.Pow(1f - Mathf.Clamp(dragDelay, 0.001f, 1f), p);

				float x = startMousePos.x - vector.x;
				float y = startMousePos.y - vector.y;

				var target = new Vector3(startDragPos.x + -x, startDragPos.y + -y);

				handLeft.localPosition += (target - handLeft.localPosition) * po;
			}

			if (draggingRightHand)
			{
				Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);

				float p = Time.deltaTime * 60f;
				float po = 1f - Mathf.Pow(1f - Mathf.Clamp(dragDelay, 0.001f, 1f), p);

				float x = startMousePos.x - vector.x;
				float y = startMousePos.y - vector.y;

				var target = new Vector3(startDragPos.x + -x, startDragPos.y + -y);

				handRight.localPosition += (target - handRight.localPosition) * po;
			}

			float add = 200f;
			if (TotalPosition.y > 355f)
				add = -200f;

			if (dialogueBase != null)
				dialogueBase.localPosition = new Vector3(Mathf.Clamp(TotalPosition.x, -820f, 820f), TotalPosition.y + add, 0f);

			if (canvasGroup != null && ExampleConfig.Instance.ExampleVisible.Value) canvasGroup.alpha = ExampleConfig.Instance.ExampleVisibility.Value;
			else if (canvasGroup != null) canvasGroup.alpha = 1f;

			if (dragging || draggingLeftHand || draggingRightHand)
				timeSinceLastInteractedOffset = Time.time;

			if (mouthLower != null)
			{
				var m = mouthLower.localScale;
				m.y = Mathf.Clamp(m.y, 0f, 1f);
				mouthLower.localScale = m;
			}

			lastPos = new Vector3(parentX.localPosition.x, parentY.localPosition.y, 0f);
		}

		#region Spawning

		IEnumerator SetupAnimations()
		{
			//Wave
			{
				var waveAnimation = new RTAnimation("Wave");

				waveAnimation.animationHandlers = new List<AnimationHandlerBase>
				{
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1f, -15f, Ease.BackIn),
						new FloatKeyframe(1.7f, -20f, Ease.SineOut),
					}, delegate (float x)
					{
						if (parentRotscale != null)
						{
							parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
						}
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.9f, 60f, Ease.BackIn),
						new FloatKeyframe(1.3f, 80f, Ease.SineOut),
						new FloatKeyframe(1.5f, 70f, Ease.SineInOut),
						new FloatKeyframe(1.8f, 80f, Ease.SineInOut),
					}, delegate (float x)
					{
						if (handLeft != null)
							handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
					{
						new Vector2Keyframe(0f, new Vector2(1f, 0.3f), Ease.Linear),
						new Vector2Keyframe(0.5f, new Vector2(1f, 0.4f), Ease.CircIn),
						new Vector2Keyframe(1f, new Vector2(1f, 0.6f), Ease.BackOut),
					}, delegate (Vector2 x)
					{
						if (mouthLower != null)
							mouthLower.localScale = new Vector3(x.x, x.y, 1f);
					}),
					new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
					{
						new Vector2Keyframe(0f, new Vector2(1f, 1f), Ease.Linear),
						new Vector2Keyframe(0.5f, new Vector2(0.8f, 1.2f), Ease.SineInOut),
						new Vector2Keyframe(0.8f, new Vector2(1.05f, 0.95f), Ease.SineInOut),
						new Vector2Keyframe(1.1f, new Vector2(1f, 1f), Ease.SineInOut),
					}, delegate (Vector2 x)
					{
						if (parentY != null)
							parentY.localScale = new Vector3(x.x, x.y, 1f);
					}),
					new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, new Vector3(200f, 0f), Ease.Linear),
						new Vector3Keyframe(2f, new Vector3(750f, 0f, 0f), Ease.SineOut),
					}, delegate (Vector3 x)
					{
						if (parentX != null)
							parentX.localPosition = x;
					}),
					new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, new Vector3(0f, -700f, 0f), Ease.Linear),
						new Vector3Keyframe(1f, new Vector3(0f, -290f, 0f), Ease.SineOut),
						new Vector3Keyframe(2f, new Vector3(0f, -410f, 0f), Ease.SineInOut),
					}, delegate (Vector3 x)
					{
						if (parentY != null)
						parentY.localPosition = x;
					}),
					new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, new Vector3(0f, 1, 0f), Ease.Linear),
						new Vector3Keyframe(0.4f, new Vector3(0f, -1f, 0f), Ease.SineOut),
						new Vector3Keyframe(0.8f, new Vector3(0f, 0f, 0f), Ease.SineOut),
					}, delegate (Vector3 x)
					{
						if (pupils != null)
							pupils.localPosition = x;
					})
				};

				waveAnimation.onComplete += delegate ()
				{
					lookAt = true;
				};

				animations.Add(waveAnimation);
			}

            //Anger
            {
				var animation = new RTAnimation("Angry");
				animation.animationHandlers = new List<AnimationHandlerBase>
				{
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.2f, 15f, Ease.SineOut),
					}, delegate (float x)
					{
						browLeft.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.2f, -15f, Ease.SineOut),
					}, delegate (float x)
					{
						browRight.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
				};

				animations.Add(animation);
            }

            //Get Out
            {
				var animation = new RTAnimation("Get Out");

				animation.animationHandlers = new List<AnimationHandlerBase>
				{
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.3f, -3f, Ease.SineOut),
					}, delegate (float x)
					{
						faceX.localPosition = new Vector3(x, 0f, 0f);
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1.5f, -3f, Ease.CircIn),
					}, delegate (float x)
					{
						faceY.localPosition = new Vector3(0f, x, 0f);
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1.5f, 180f, Ease.CircIn),
					}, delegate (float x)
					{
						handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1.5f, 30f, Ease.CircIn),
					}, delegate (float x)
					{
						handRight.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
					{
						new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
						new Vector2Keyframe(0.5f, new Vector2(1.1f, 0.9f), Ease.SineOut),
						new Vector2Keyframe(1.1f, Vector2.one, Ease.SineInOut),
						new Vector2Keyframe(4f, new Vector2(0.7f, 1.3f), Ease.SineIn),
					}, delegate (Vector2 x)
					{
						parentY.localScale = new Vector3(x.x, x.y, 1f);
					}),
				};

				animations.Add(animation);
            }

			//Reset
			{
				var animation = new RTAnimation("Reset");

				animation.animationHandlers = new List<AnimationHandlerBase>
				{
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.6f, 0f, Ease.SineInOut),
					}, delegate (float x)
					{
						parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(0.6f, 0f, Ease.SineInOut),
					}, delegate (float x)
					{
						handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
						new Vector3Keyframe(0.6f, Vector3.zero, Ease.SineInOut)
					}, delegate (Vector3 x)
					{
						parentX.localPosition = x;
					}),
					new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
						new Vector3Keyframe(0.6f, Vector3.zero, Ease.SineInOut)
					}, delegate (Vector3 x)
					{
						parentY.localPosition = x;
					}),
				};

				animations.Add(animation);
			}

			yield break;
		}

		public static IEnumerator headEnumerator;

		IEnumerator SpawnExample()
		{
			spawning = true;

			if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/ExampleHooks.cs"))
            {
				yield return StartCoroutine(RTCode.IEvaluate(RTFile.ReadFromFile("settings/ExampleHooks.cs")));
            }

			if (downloadMode)
			{
				yield return AlephNetworkManager.DownloadAudioClip(speakURL, RTFile.GetAudioType(speakURL), delegate (AudioClip audioClip)
				{
					speakSound = audioClip;
				}, delegate (string onError)
				{
					var p = SpeakPath;
					StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{p}", RTFile.GetAudioType(p), delegate (AudioClip audioClip)
					{
						speakSound = audioClip;
					}));
				});
			}
			else
			{
				var p = SpeakPath;
				StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{p}", RTFile.GetAudioType(p), delegate (AudioClip audioClip)
				{
					speakSound = audioClip;
				}));
			}


            #region Canvas

            var inter = new GameObject("Canvas");
			baseCanvas = inter;
			DontDestroyOnLoad(inter);
			inter.transform.localScale = Vector3.one * CoreHelper.ScreenScale;
			var interfaceRT = inter.AddComponent<RectTransform>();
			interfaceRT.anchoredPosition = new Vector2(960f, 540f);
			interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
			interfaceRT.pivot = new Vector2(0.5f, 0.5f);
			interfaceRT.anchorMin = Vector2.zero;
			interfaceRT.anchorMax = Vector2.zero;

			canvas = inter.AddComponent<Canvas>();
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.scaleFactor = CoreHelper.ScreenScale;
			canvas.sortingOrder = 10000;

			canvasGroup = inter.AddComponent<CanvasGroup>();
			
			var canvasScaler = inter.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

			inter.AddComponent<GraphicRaycaster>();

			Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

            #endregion

            #region Floating Parent

            var l_floatingParent = new GameObject("Example");
			l_floatingParent.transform.SetParent(inter.transform);
			l_floatingParent.transform.localScale = Vector3.one;

			var l_floatingParentRT = l_floatingParent.AddComponent<RectTransform>();
			l_floatingParentRT.anchoredPosition = Vector2.zero;
			floatingParent = l_floatingParent.transform;

            #endregion

            #region X Parent

            var xparent = new GameObject("Example X");
			xparent.transform.SetParent(l_floatingParent.transform);
			xparent.transform.localScale = Vector3.one;

			var xRT = xparent.AddComponent<RectTransform>();
			xRT.anchoredPosition = Vector2.zero;
			parentX = xparent.transform;

            #endregion

            #region Y Parent

            var yparent = new GameObject("Example Y");
			yparent.transform.SetParent(xparent.transform);
			yparent.transform.localScale = Vector3.one;

			var yRT = yparent.AddComponent<RectTransform>();
			yRT.anchoredPosition = new Vector2(0f, -1600f);
			parentY = yparent.transform;

            #endregion

            #region Rotscale Parent

            var rotscaleparent = new GameObject("Example Rotscale");
			rotscaleparent.transform.SetParent(yparent.transform);
			rotscaleparent.transform.localScale = Vector3.one;

			var rotscaleRT = rotscaleparent.AddComponent<RectTransform>();
			rotscaleRT.anchoredPosition = Vector3.zero;
			parentRotscale = rotscaleparent.transform;

            #endregion

            #region Tail

            var l_tail = new GameObject("Example Tail");
			l_tail.transform.SetParent(rotscaleparent.transform);
			l_tail.transform.localScale = Vector3.one;

			var l_tailRT = l_tail.AddComponent<RectTransform>();
			l_tailRT.anchoredPosition = Vector2.zero;
			tail = l_tail.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_tail.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -58f);
				rt.sizeDelta = new Vector2(28f, 42f);

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(tailURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);

						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{TailPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{TailPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onClick = delegate (PointerEventData x)
				{
					talking = true;
					Say("Please don't touch me there.", new List<IKeyframe<float>> { new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear) }, new List<IKeyframe<float>> { new FloatKeyframe(0f, parentY.localPosition.y + 200f, Ease.Linear) }, onComplete: delegate () { talking = false; });
					Play("Angry", false);
				};
			}

            #endregion

            #region Head

            var l_ears = new GameObject("Example Ears");

			var l_head = new GameObject("Example Head");
			l_head.transform.SetParent(rotscaleparent.transform);
			l_head.transform.localScale = Vector3.one;

			l_tail.transform.SetParent(l_head.transform);
			l_tail.transform.localScale = Vector3.one;
			l_tailRT.anchoredPosition = Vector2.zero;

			l_ears.transform.SetParent(l_head.transform);
			l_ears.transform.localScale = Vector3.one;

			var l_earsRT = l_ears.AddComponent<RectTransform>();
			l_earsRT.anchoredPosition = Vector2.zero;
			ears = l_ears.transform;

			var l_headRT = l_head.AddComponent<RectTransform>();
			l_headRT.anchoredPosition = Vector2.zero;
			head = l_head.transform;

			if (headEnumerator != null)
			{
				yield return StartCoroutine(headEnumerator);
			}

			headSpawned();

            #endregion

            #region Ears

            var l_earbottomleft = new GameObject("Example Ear Bottom Left");
			l_earbottomleft.transform.SetParent(l_ears.transform);
			l_earbottomleft.transform.localScale = Vector3.one;

			var l_earbottomleftRT = l_earbottomleft.AddComponent<RectTransform>();
			l_earbottomleftRT.anchoredPosition = new Vector2(25f, 35f);
			l_earbottomleftRT.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -30f));
			earBottomLeft = l_earbottomleft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earbottomleft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.pivot = new Vector2(0.5f, 0.2f);
				rt.sizeDelta = new Vector2(44f, 52f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earBottomURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}, delegate (string str)
				//	{

				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earBottomURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_earbottomright = new GameObject("Example Ear Bottom Right");
			l_earbottomright.transform.SetParent(l_ears.transform);
			l_earbottomright.transform.localScale = Vector3.one;

			var l_earbottomrightRT = l_earbottomright.AddComponent<RectTransform>();
			l_earbottomrightRT.anchoredPosition = new Vector2(-25f, 35f);
			l_earbottomrightRT.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 30f));
			earBottomRight = l_earbottomright.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earbottomright.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.pivot = new Vector2(0.5f, 0.2f);
				rt.sizeDelta = new Vector2(44f, 52f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earBottomURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}, delegate (string str)
				//	{

				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earBottomURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

            #endregion

            //Head
            {
                var im = new GameObject("image");
				im.transform.SetParent(l_head.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;

				//StartCoroutine(SpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example head.png", new Vector2Int(540, 540), callback: delegate (Sprite spr)
				//{
				//	image.sprite = spr;
				//}, onError: delegate (string onError)
				//{

				//}));

				//yield return StartCoroutine(SpriteManager.DownloadSprite("https://media.discordapp.net/attachments/811214540141363201/1151188682540323016/example_head.png", new Vector2Int(540, 540), callback: delegate (Sprite x)
				//{
				//	image.sprite = x;
				//}));

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(headURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HeadPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(headURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HeadPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HeadPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onClick = delegate (PointerEventData x)
				{
					if (x.button == PointerEventData.InputButton.Right)
					{
						faceCanLook = false;
						Say("Alright, I'll get out of your way.", new List<IKeyframe<float>> { new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear) }, new List<IKeyframe<float>> { new FloatKeyframe(0f, parentY.localPosition.y + 200f, Ease.Linear) }, onComplete: delegate ()
						{
							Kill();
						});

						Play("Get Out", false);
						Move(new List<IKeyframe<float>> { new FloatKeyframe(1.5f, parentX.localPosition.x + -400f, Ease.SineInOut) }, new List<IKeyframe<float>> { new FloatKeyframe(1f, parentY.localPosition.y + 80f, Ease.SineOut), new FloatKeyframe(1.5f, parentY.localPosition.y + -1200f, Ease.CircIn) }, false, delegate ()
						{
							//Kill();
						});
					}
				};
				clickable.onDown = delegate (PointerEventData x)
				{
					if (x.button != PointerEventData.InputButton.Left)
						return;

					StopAnimations(x => x.name == "End Drag Example" || x.name == "Drag Example");

					startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;
					startDragPos = new Vector2(TotalPosition.x, TotalPosition.y);
					dragPos = new Vector3(TotalPosition.x, TotalPosition.y);
					dragging = true;

					faceCanLook = false;

					if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.6f, 0.7f));
					else AudioManager.inst.PlaySound("Click");

					float tbrowLeft = -15f;
					if (browLeft.localRotation.eulerAngles.z > 180f)
						tbrowLeft = 345f;

					float tbrowRight = 15f;
					if (browRight.localRotation.eulerAngles.z > 180f)
						tbrowRight = 345f;

					var animation = new RTAnimation("Drag Example");
					animation.animationHandlers = new List<AnimationHandlerBase>
					{
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, mouthLower.localScale.y, Ease.Linear),
							new FloatKeyframe(0.2f, 0.7f, Ease.SineOut),
						}, delegate (float x)
						{
							mouthLower.localScale = new Vector3(1f, x, 1f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
							new FloatKeyframe(0.2f, 0.5f, Ease.SineOut),
						}, delegate (float x)
						{
							lips.localScale = new Vector3(1f, x, 1f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
							new FloatKeyframe(0.2f, 2f, Ease.SineOut),
						}, delegate (float x)
						{
							lips.localPosition = new Vector3(0f, x, 0f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
							new FloatKeyframe(0.2f, 10f, Ease.SineOut),
						}, delegate (float x)
						{
							head.localPosition = new Vector3(head.localPosition.x, x, head.localPosition.z);
						}),
						// Hands
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.05f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.3f, -30f, Ease.SineOut),
						}, delegate (float x)
						{
							handLeft.GetChild(0).localPosition = new Vector3(0f, x, 0f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.05f, handRight.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.3f, -30f, Ease.SineOut),
						}, delegate (float x)
						{
							handRight.GetChild(0).localPosition = new Vector3(0f, x, 0f);
						}),
						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, browLeft.localRotation.eulerAngles.z, Ease.Linear),
							new FloatKeyframe(0.3f, tbrowLeft, Ease.SineOut),
						}, delegate (float x)
						{
							browLeft.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, browRight.localRotation.eulerAngles.z, Ease.Linear),
							new FloatKeyframe(0.3f, tbrowRight, Ease.SineOut),
						}, delegate (float x)
						{
							browRight.localRotation = Quaternion.Euler(0f, 0f, x);
						}),

						new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
						{
							new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
							new Vector2Keyframe(0.3f, new Vector2(1.05f, 0.95f), Ease.SineOut),
						}, delegate (Vector2 x)
						{
							parentY.localScale = new Vector3(x.x, x.y, 1f);
						}),
					};

					PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
				};
				clickable.onUp = delegate (PointerEventData x)
				{
					var animation = new RTAnimation("End Drag Example");

					float t = 0f;
					if (parentRotscale.localRotation.eulerAngles.z > 180f)
						t = 360f;

					float tbrowLeft = 0f;
					if (browLeft.localRotation.eulerAngles.z > 180f)
						tbrowLeft = 360f;

					float tbrowRight = 0f;
					if (browRight.localRotation.eulerAngles.z > 180f)
						tbrowRight = 360f;

					animation.animationHandlers = new List<AnimationHandlerBase>
					{
						new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
						{
							new Vector2Keyframe(0f, new Vector2(parentY.localScale.x, parentY.localScale.y), Ease.Linear),
							new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.ElasticOut),
						}, delegate (Vector2 x)
						{
							parentY.localScale = new Vector3(x.x, x.y, 1f);
						}),
						new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
						{
							new Vector2Keyframe(0f, new Vector2(parentRotscale.localScale.x, parentRotscale.localScale.y), Ease.Linear),
							new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.ElasticOut),
						}, delegate (Vector2 x)
						{
							parentRotscale.localScale = new Vector3(x.x, x.y, 1f);
						}),

						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, parentRotscale.localRotation.eulerAngles.z, Ease.Linear),
							new FloatKeyframe(1f, t, Ease.BackOut),
						}, delegate (float x)
						{
							parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, mouthLower.localScale.y, Ease.Linear),
							new FloatKeyframe(0.2f, 0.5f, Ease.SineIn),
						}, delegate (float x)
						{
							mouthLower.localScale = new Vector3(1f, x, 1f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
							new FloatKeyframe(0.2f, 1f, Ease.SineIn),
						}, delegate (float x)
						{
							lips.localScale = new Vector3(1f, x, 1f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
							new FloatKeyframe(0.2f, 0f, Ease.SineIn),
						}, delegate (float x)
						{
							lips.localPosition = new Vector3(0f, x, 0f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
							new FloatKeyframe(0.5f, 0f, Ease.BounceOut),
						}, delegate (float x)
						{
							head.localPosition = new Vector3(head.localPosition.x, x, head.localPosition.z);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.1f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
						}, delegate (float x)
						{
							handLeft.GetChild(0).localPosition = new Vector3(0f, x, 0f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.1f, handRight.GetChild(0).localPosition.y, Ease.Linear),
							new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
						}, delegate (float x)
						{
							handRight.GetChild(0).localPosition = new Vector3(0f, x, 0f);
						}),

						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, browLeft.localRotation.eulerAngles.z, Ease.Linear),
							new FloatKeyframe(0.5f, tbrowLeft, Ease.SineOut),
						}, delegate (float x)
						{
							browLeft.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, browRight.localRotation.eulerAngles.z, Ease.Linear),
							new FloatKeyframe(0.5f, tbrowRight, Ease.SineOut),
						}, delegate (float x)
						{
							browRight.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear),
							new FloatKeyframe(0.5f, 0f, Ease.SineOut),
						}, delegate (float x)
						{
							faceX.localPosition = new Vector3(x, 0f, 0f);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear),
							new FloatKeyframe(0.5f, 0f, Ease.SineOut),
						}, delegate (float x)
						{
							faceY.localPosition = new Vector3(0f, x, 0f);
						}),
					};

					faceX.localPosition = Vector3.zero;
					faceY.localPosition = Vector3.zero;

					PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
					dragging = false;
					faceCanLook = true;
				};
			}

            #region Face

            var l_faceX = new GameObject("Example Face X");
			l_faceX.transform.SetParent(l_head.transform);
			l_faceX.transform.localScale = Vector3.one;

			var l_faceXRT = l_faceX.AddComponent<RectTransform>();
			l_faceXRT.anchoredPosition = Vector3.zero;
			faceX = l_faceX.transform;

			var l_faceY = new GameObject("Example Face Y");
			l_faceY.transform.SetParent(l_faceX.transform);
			l_faceY.transform.localScale = Vector3.one;

			var l_faceYRT = l_faceY.AddComponent<RectTransform>();
			l_faceYRT.anchoredPosition = Vector3.zero;
			faceY = l_faceY.transform;

            #endregion

            #region Eyes

            var l_eyes = new GameObject("Example Eyes");
			l_eyes.transform.SetParent(l_faceY.transform);
			l_eyes.transform.localScale = Vector3.one;

			var l_eyesRT = l_eyes.AddComponent<RectTransform>();
			l_eyesRT.anchoredPosition = Vector2.zero;
			eyes = l_eyes.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_eyes.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(74f, 34f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(eyesURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EyesPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(eyesURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EyesPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EyesPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_pupils = new GameObject("Example Pupils");
			l_pupils.transform.SetParent(l_eyes.transform);
			l_pupils.transform.localScale = Vector3.one;

			var l_pupilsRT = l_pupils.AddComponent<RectTransform>();
			l_pupilsRT.anchoredPosition = Vector2.zero;
			pupils = l_pupils.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_pupils.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(47f, 22f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(pupilsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{PupilsPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(pupilsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{PupilsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{PupilsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_blink = new GameObject("Example Blink");
			l_blink.transform.SetParent(l_eyes.transform);
			l_blink.transform.localScale = Vector3.one;

			var l_blinkRT = l_blink.AddComponent<RectTransform>();
			l_blinkRT.anchoredPosition = Vector2.zero;
			blink = l_blink.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_blink.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(74f, 34f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(blinkURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BlinkPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(blinkURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BlinkPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BlinkPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

            #endregion

            #region Snout

            var l_snout = new GameObject("Example Snout");
			l_snout.transform.SetParent(l_faceY.transform);
			l_snout.transform.localScale = Vector3.one;

			var l_snoutRT = l_snout.AddComponent<RectTransform>();
			l_snoutRT.anchoredPosition = Vector2.zero;
			snout = l_snout.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_snout.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -31f);
				rt.sizeDelta = new Vector2(60f, 31f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(snoutURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{SnoutPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(snoutURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{SnoutPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{SnoutPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_mouthBase = new GameObject("Example Mouth Base");
			l_mouthBase.transform.SetParent(l_snout.transform);
			l_mouthBase.transform.localScale = Vector3.one;

			var l_mouthBaseRT = l_mouthBase.AddComponent<RectTransform>();
			l_mouthBaseRT.anchoredPosition = new Vector2(0f, -30f);
			mouthBase = l_mouthBase.transform;

			var l_mouthUpper = new GameObject("Example Mouth Upper");
			l_mouthUpper.transform.SetParent(l_mouthBase.transform);
			l_mouthUpper.transform.localScale = new Vector3(1f, 0.15f, 1f);
			l_mouthUpper.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));

			var l_mouthUpperRT = l_mouthUpper.AddComponent<RectTransform>();
			l_mouthUpperRT.anchoredPosition = Vector3.zero;
			mouthUpper = l_mouthUpper.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_mouthUpper.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 16f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(mouthURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(mouthURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_mouthLower = new GameObject("Example Mouth Lower");
			l_mouthLower.transform.SetParent(l_mouthBase.transform);
			l_mouthLower.transform.localScale = new Vector3(1f, 0.5f);

			var l_mouthLowerRT = l_mouthLower.AddComponent<RectTransform>();
			l_mouthLowerRT.anchoredPosition = Vector3.zero;
			mouthLower = l_mouthLower.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_mouthLower.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 16f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(mouthURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(mouthURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_lips = new GameObject("Example Lips");
			l_lips.transform.SetParent(l_mouthBase.transform);
			l_lips.transform.localScale = Vector3.one;

			var l_lipsRT = l_lips.AddComponent<RectTransform>();
			l_lipsRT.anchoredPosition = Vector3.zero;
			lips = l_lips.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_lips.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 3f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 8f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(lipsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{LipsPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(lipsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{LipsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{LipsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_nose = new GameObject("Example Nose");
			l_nose.transform.SetParent(l_snout.transform);
			l_nose.transform.localScale = Vector3.one;

			var l_noseRT = l_nose.AddComponent<RectTransform>();
			l_noseRT.anchoredPosition = new Vector2(0f, -20f);
			nose = l_nose.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_nose.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.sizeDelta = new Vector2(22f, 8f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(noseURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{NosePath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(noseURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{NosePath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{NosePath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

            #endregion

            #region Brows

            var l_browBase = new GameObject("Example Brow Base");
			l_browBase.transform.SetParent(l_faceY.transform);
			l_browBase.transform.localScale = Vector3.one;

			var l_browBaseRT = l_browBase.AddComponent<RectTransform>();
			l_browBaseRT.anchoredPosition = new Vector2(0f, 30f);
			browBase = l_browBase.transform;

			var l_browLeft = new GameObject("Example Brow Left");
			l_browLeft.transform.SetParent(l_browBase.transform);
			l_browLeft.transform.localScale = Vector3.one;

			var l_browLeftRT = l_browLeft.AddComponent<RectTransform>();
			l_browLeftRT.anchoredPosition = new Vector2(22f, 0f);
			browLeft = l_browLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_browLeft.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(18f, 0f);
				rt.pivot = new Vector2(1.7f, 0.5f);
				rt.sizeDelta = new Vector2(20f, 6f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(browsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(browsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

			var l_browRight = new GameObject("Example Brow Right");
			l_browRight.transform.SetParent(l_browBase.transform);
			l_browRight.transform.localScale = Vector3.one;

			var l_browRightRT = l_browRight.AddComponent<RectTransform>();
			l_browRightRT.anchoredPosition = new Vector2(-22f, 0f);
			browRight = l_browRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_browRight.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(-18f, 0f);
				rt.pivot = new Vector2(-0.7f, 0.5f);
				rt.sizeDelta = new Vector2(20f, 6f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(browsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(browsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}
			}

            #endregion

            #region Top Ears

            var l_earTopLeft = new GameObject("Example Ear Top Left");
			l_earTopLeft.transform.SetParent(l_earbottomleft.transform);
			l_earTopLeft.transform.localScale = Vector3.one;

			var l_earTopLeftRT = l_earTopLeft.AddComponent<RectTransform>();
			l_earTopLeftRT.anchoredPosition = new Vector2(0f, 0f);
			l_earTopLeftRT.localRotation = Quaternion.identity;
			earTopLeft = l_earTopLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earTopLeft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 45f);
				rt.pivot = new Vector2(0.5f, 0.275f);
				rt.sizeDelta = new Vector2(44f, 80f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earTopURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earTopURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onClick = delegate (PointerEventData x)
				{
					var animation = new RTAnimation("Ear Left Flick");
					animation.animationHandlers = new List<AnimationHandlerBase>
					{
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, 330f, Ease.Linear),
							new FloatKeyframe(0.1f, 300f, Ease.SineOut),
							new FloatKeyframe(0.7f, 330f, Ease.SineInOut),
						}, delegate (float x)
						{
							earBottomLeft.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, 270f, Ease.Linear),
							new FloatKeyframe(0.05f, 230f, Ease.Linear),
							new FloatKeyframe(0.3f, 300f, Ease.SineOut),
							new FloatKeyframe(0.9f, 270f, Ease.SineInOut),
						}, delegate (float x)
						{
							earTopLeft.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, x);
						}),
					};

					PlayOnce(animation, false);
				};
			}

			var l_earTopRight = new GameObject("Example Ear Top Right");
			l_earTopRight.transform.SetParent(l_earbottomright.transform);
			l_earTopRight.transform.localScale = Vector3.one;

			var l_earTopRightRT = l_earTopRight.AddComponent<RectTransform>();
			l_earTopRightRT.anchoredPosition = new Vector2(0f, 0f);
			l_earTopRightRT.localRotation = Quaternion.identity;
			earTopRight = l_earTopRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earTopRight.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 45f);
				rt.pivot = new Vector2(0.5f, 0.275f);
				rt.sizeDelta = new Vector2(44f, 80f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earTopURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}, delegate (string onError)
				//{
				//	StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
				//	{
				//		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//	}));
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(earTopURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onClick = delegate (PointerEventData x)
				{
					var animation = new RTAnimation("Ear Right Flick");
					animation.animationHandlers = new List<AnimationHandlerBase>
					{
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, 30f, Ease.Linear),
							new FloatKeyframe(0.1f, 60f, Ease.SineOut),
							new FloatKeyframe(0.7f, 30f, Ease.SineInOut),
						}, delegate (float x)
						{
							earBottomRight.localRotation = Quaternion.Euler(0f, 0f, x);
						}),
						new AnimationHandler<float>(new List<IKeyframe<float>>
						{
							new FloatKeyframe(0f, 90f, Ease.Linear),
							new FloatKeyframe(0.05f, 130f, Ease.Linear),
							new FloatKeyframe(0.3f, 60f, Ease.SineOut),
							new FloatKeyframe(0.9f, 90f, Ease.SineInOut),
						}, delegate (float x)
						{
							earTopRight.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, x);
						}),
					};

					PlayOnce(animation, false);
				};
			}

            #endregion

            #region Hands

            var l_handsBase = new GameObject("Example Hands Base");
			l_handsBase.transform.SetParent(rotscaleparent.transform);
			l_handsBase.transform.localScale = Vector3.one;

			var l_handsBaseRT = l_handsBase.AddComponent<RectTransform>();
			l_handsBaseRT.anchoredPosition = Vector2.zero;
			handsBase = l_handsBase.transform;

			var l_handLeft = new GameObject("Example Hand Left");
			l_handLeft.transform.SetParent(l_handsBase.transform);
			l_handLeft.transform.localScale = Vector3.one;

			var l_handLeftRT = l_handLeft.AddComponent<RectTransform>();
			l_handLeftRT.anchoredPosition = new Vector2(40f, 0f);
			handLeft = l_handLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_handLeft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -80f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.sizeDelta = new Vector2(42f, 42f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(handsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(handsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onDown = delegate (PointerEventData x)
				{

					startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
					startDragPos = new Vector2(handLeft.localPosition.x, handLeft.localPosition.y);
					draggingLeftHand = true;

					if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
					else AudioManager.inst.PlaySound("Click");
				};
				clickable.onUp = delegate (PointerEventData x)
				{
					draggingLeftHand = false;

					foreach (var levelItem in (List<LevelFolder<EditorManager.MetadataWrapper>>)ModCompatibility.sharedFunctions["EditorLevelFolders"])
					{
						if (EditorManager.RectTransformToScreenSpace(image.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.icon.rectTransform)))
						{
							Debug.LogFormat("{0}Picked level: {1}", className, levelItem.level.folder);
						}
					}
				};
			}

			var l_handRight = new GameObject("Example Hand Right");
			l_handRight.transform.SetParent(l_handsBase.transform);
			l_handRight.transform.localScale = Vector3.one;

			var l_handRightRT = l_handRight.AddComponent<RectTransform>();
			l_handRightRT.anchoredPosition = new Vector2(-40f, 0f);
			handRight = l_handRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_handRight.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -80f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.sizeDelta = new Vector2(42f, 42f);

				//yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(handsURL, delegate (Texture2D texture2D)
				//{
				//	image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				//}));

				if (downloadMode)
				{
					yield return StartCoroutine(AlephNetworkManager.DownloadImageTexture(handsURL, delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}Had http error {1} so trying to get offline file.", className, onError);
						StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", delegate (Texture2D texture2D)
						{
							image.sprite = SpriteManager.CreateSprite(texture2D);
						}, delegate (string onError)
						{
							Debug.LogErrorFormat("{0}File does not exist.", className);
						}));
					}));
				}
				else
				{
					StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", delegate (Texture2D texture2D)
					{
						image.sprite = SpriteManager.CreateSprite(texture2D);
					}, delegate (string onError)
					{
						Debug.LogErrorFormat("{0}File does not exist.", className);
					}));
				}

				var clickable = im.AddComponent<ExampleClickable>();
				clickable.onDown = delegate (PointerEventData x)
				{
					startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
					startDragPos = new Vector2(handRight.localPosition.x, handRight.localPosition.y);
					draggingRightHand = true;

					if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
					else AudioManager.inst.PlaySound("Click");
				};
				clickable.onUp = delegate (PointerEventData x)
				{
					draggingRightHand = false;

					foreach (var levelItem in (List<LevelFolder<EditorManager.MetadataWrapper>>)ModCompatibility.sharedFunctions["EditorLevelFolders"])
					{
						if (EditorManager.RectTransformToScreenSpace(image.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.icon.rectTransform)))
						{
							Debug.LogFormat("{0}Picked level: {1}", className, levelItem.level.folder);
						}
					}
				};
			}

            #endregion

            yield return StartCoroutine(SpawnChatter());
			yield return StartCoroutine(SetupAnimations());
			yield return StartCoroutine(SpawnDialogue());

			spawning = false;

			yield break;
		}

		IEnumerator SpawnChatter()
        {
			var uiField = UIManager.GenerateUIInputField("Discussion", baseCanvas.transform);

			chatterBase = ((GameObject)uiField["GameObject"]).transform;
			chatter = (InputField)uiField["InputField"];

			chatter.textComponent.color = new Color(0.1188679f, 0.1188679f, 0.1188679f, 1f);

			((RectTransform)chatterBase).sizeDelta = new Vector2(200f, 32f);

			chatter.onValueChanged.AddListener(delegate (string _val)
			{
				Debug.LogFormat("{0}Chatter: {1}", className, _val);
			});

			chatter.onEndEdit.AddListener(delegate (string _val)
			{
				HandleChatting();
			});

			chatterBase.gameObject.SetActive(false);

			yield break;
        }

		IEnumerator SpawnDialogue()
        {
			var dialogueBase = new GameObject("Dialogue Base");
			dialogueBase.transform.SetParent(baseCanvas.transform);
			dialogueBase.transform.localScale = Vector3.one;

			var dialogueBaseRT = dialogueBase.AddComponent<RectTransform>();
			dialogueBaseRT.anchoredPosition = Vector2.zero;

			this.dialogueBase = dialogueBase.transform;

			var dialogueImage = new GameObject("Image");
			dialogueImage.transform.SetParent(dialogueBase.transform);
			dialogueImage.transform.localScale = Vector3.one;

			var dialogueImageRT = dialogueImage.AddComponent<RectTransform>();
			dialogueImageRT.anchoredPosition = Vector2.zero;
			dialogueImageRT.sizeDelta = new Vector2(300f, 150f);

			dialogueImage.AddComponent<CanvasRenderer>();

			var image = dialogueImage.AddComponent<Image>();
			this.dialogueImage = image;

			var dialogueText = new GameObject("Text");
			dialogueText.transform.SetParent(dialogueImage.transform);
			dialogueText.transform.localScale = Vector3.one;

			var dialogueTextRT = dialogueText.AddComponent<RectTransform>();
			dialogueTextRT.anchoredPosition = Vector2.zero;
			dialogueTextRT.sizeDelta = new Vector2(280f, 140f);

			dialogueText.AddComponent<CanvasRenderer>();

			var text = dialogueText.AddComponent<Text>();
			text.font = Font.GetDefault();
			text.fontSize = 22;
			text.color = new Color(0.06f, 0.06f, 0.06f, 1f);
			this.dialogueText = text;

			//this.dialogueBase.localPosition = new Vector3(-800f, -55f, 0f);

			talking = true;
            Play("Wave", onComplete: delegate ()
            {
                ResetPositions(1.6f, onComplete: delegate ()
                {
                    Say("What would you like me to do?", onComplete: delegate () { talking = false; });
                });
            });

            Say("Hello, I am Example and this is a test!");

			onSpawnComplete();

			Debug.Log($"{className}Spawned!");

			yield break;
        }

		public void SetParent(Transform tf)
		{
			var x = floatingParent.localPosition;
			var y = floatingParent.localScale;
			var z = floatingParent.localRotation;
            floatingParent.SetParent(tf);

			floatingParent.localPosition = x;
			floatingParent.localScale = y;
			floatingParent.localRotation = z;
        }

        #endregion

        #region Play Animations

        public void Play(string anim, bool stopOthers = true, Action onComplete = null)
        {
			if (animations.Find(x => x.name == anim) == null)
				return;

			if (DebugsOn)
				Debug.LogFormat("{0}Playing Example Animation: {1}", className, anim);

			if (stopOthers)
				animations.FindAll(x => x.playing).ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
				});

			var animation = animations.Find(x => x.name == anim);

			animation.ResetTime();

			if (onComplete != null)
				animation.onComplete = onComplete;

			animation.Play();
        }

		//ExampleManager.inst.Say("Hello, I am Example and this is a test!", new Vector2(0f, 200f))
		public void Say(string dialogue, List<IKeyframe<float>> xPos = null, List<IKeyframe<float>> yPos = null, float textLength = 1.5f, float stayTime = 4f, float time = 0.7f, bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.name.Contains("DIALOGUE: ")).ForEach(delegate (RTAnimation anim)
				 {
					 anim.Stop();
					 animations.Remove(anim);
				 });

			lastDialogue = dialogue;

			var animation = new RTAnimation("DIALOGUE: " + dialogue);

			var ogMouth = mouthLower.localScale.y;

			var list = new List<IKeyframe<float>>();
			list.Add(new FloatKeyframe(0f, 0.5f, Ease.Linear));

			float t = 0.1f;

			var r = textLength * time * 10;
			for (int i = 0; i < (int)r / 2; i++)
			{
				list.Add(new FloatKeyframe(t * time, ogMouth * 1.85f, Ease.SineOut));
				t += 0.1f;
				list.Add(new FloatKeyframe(t * time, 0.5f, Ease.SineIn));
				t += 0.1f;
			}

			list.Add(new FloatKeyframe((t + 0.05f) * time, 0.5f, Ease.Linear));

			var listX = new List<IKeyframe<float>>();
			var listY = new List<IKeyframe<float>>();

			if (xPos != null)
				xPos.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			else listX.Add(new FloatKeyframe(0f, 0f, Ease.Linear));
			if (yPos != null)
				yPos.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });
			else listY.Add(new FloatKeyframe(0f, 0f, Ease.Linear));

			int prevLetterNum = 0;

			float posX = 0f;

			animation.animationHandlers = new List<AnimationHandlerBase>
			{
				new AnimationHandler<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, 90f, Ease.Linear),
					new FloatKeyframe(1.5f * time, 0f, Ease.ElasticOut),
				}, delegate (float x)
				{
					dialogueBase.localRotation = Quaternion.Euler(0f, 0f, x);
				}),
				new AnimationHandler<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, 1f, Ease.Linear),
					new FloatKeyframe(textLength * time, dialogue.Length, Ease.SineOut),
				}, delegate (float x)
				{
					if (prevLetterNum != (int)x)
					{
						prevLetterNum = (int)x;
						if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(0.97f, 1.03f), UnityEngine.Random.Range(0.2f, 0.3f));
						else AudioManager.inst.PlaySound("Click");
					}

					try
					{
						dialogueText.text = dialogue.Substring(0, (int)x + 1);
					}
					catch
					{
						dialogueText.text = dialogue.Substring(0, (int)x);
					}
				}),
				new AnimationHandler<float>(list, delegate (float x)
				{
					if (mouthLower != null)
						mouthLower.localScale = new Vector3(1f, Mathf.Clamp(0f, x, 1.5f), 1f);
					//Debug.LogFormat("{0}Mouth: {1}", className, x);
				}),
				new AnimationHandler<float>(listX, delegate (float x)
				{
					posX = x;
				}),
				new AnimationHandler<float>(listY, delegate (float x)
				{
					dialogueBase.localPosition = new Vector3(posX, x, 0f);
				}),

				new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
				{
					new Vector2Keyframe(0f, Vector2.zero, Ease.Linear),
					new Vector2Keyframe(0.2f * time, new Vector2(1.1f, 1.1f), Ease.SineOut),
					new Vector2Keyframe(0.8f * time, Vector2.one, Ease.SineInOut),
					new Vector2Keyframe(stayTime * time, Vector2.one, Ease.Linear),
					new Vector2Keyframe((stayTime + 0.3f) * time, Vector2.zero, Ease.BackIn),
					new Vector2Keyframe((stayTime + 0.6f) * time, Vector2.zero, Ease.Linear),
				}, delegate (Vector2 x)
				{
					dialogueBase.transform.localScale = new Vector3(x.x, x.y, 1f);
				}),
			};

			if (DebugsOn)
				for (int i = 0; i < animation.animationHandlers.Count; i++)
				{
					Debug.LogFormat("{0}RTAnimation {1} Length: {2}", className, i, animation.animationHandlers[i].Length);
				}

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				onComplete?.Invoke();

				animation = null;

				if (DebugsOn)
					Debug.Log($"{className}Say onComplete");
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void Move(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "MOVEMENT").ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
					animations.Remove(anim);
				});

			var animation = new RTAnimation("MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, parentY.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.animationHandlers = new List<AnimationHandlerBase>
			{
				new AnimationHandler<float>(listX, delegate (float x) { parentX.localPosition = new Vector3(x, 0f, 0f); }),
				new AnimationHandler<float>(listY, delegate (float x) { parentY.localPosition = new Vector3(0f, x, 0f); }),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				onComplete?.Invoke();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void FaceLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "FACE MOVEMENT").ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
				});

			var animation = new RTAnimation("FACE MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.animationHandlers = new List<AnimationHandlerBase>
			{
				new AnimationHandler<float>(x, delegate (float x) { faceX.localPosition = new Vector3(x, 0f, 0f); }),
				new AnimationHandler<float>(y, delegate (float x) { faceY.localPosition = new Vector3(0f, x, 0f); }),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				onComplete?.Invoke();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}
		
		public void PupilsLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "PUPILS MOVEMENT").ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
				});

			var animation = new RTAnimation("PUPILS MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, pupils.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, pupils.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.animationHandlers = new List<AnimationHandlerBase>
			{
				new AnimationHandler<float>(x, delegate (float x) { pupils.localPosition = new Vector3(x, pupils.localPosition.y, pupils.localPosition.z); }),
				new AnimationHandler<float>(y, delegate (float x) { pupils.localPosition = new Vector3(pupils.localPosition.x, x, pupils.localPosition.z); }),
			};

			animation.onComplete = delegate ()
			{
				lookAt = true;
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			lookAt = false;
			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void ResetPositions(float speed, bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.playing && !x.name.Contains("DIALOGUE: ")).ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
				});

			var animation = new RTAnimation("RESET");

			float trs = 0f;
			if (parentRotscale.localRotation.eulerAngles.z > 180f)
				trs = 360f;

			float thl = 0f;
			if (handLeft.localRotation.eulerAngles.z > 180f)
				thl = 360f;

			animation.animationHandlers = new List<AnimationHandlerBase>
			{
				new AnimationHandler<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, parentRotscale.localRotation.eulerAngles.z, Ease.Linear),
					new FloatKeyframe(speed, trs, Ease.SineInOut),
				}, delegate (float x)
				{
					parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
				}),
				new AnimationHandler<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
					new FloatKeyframe(speed, thl, Ease.SineInOut),
				}, delegate (float x)
				{
					handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
				}),

				new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
				{
					new Vector3Keyframe(0f, parentX.localPosition, Ease.Linear),
					new Vector3Keyframe(speed, new Vector3(700f, 0f), Ease.SineInOut)
				}, delegate (Vector3 x)
				{
					parentX.localPosition = x;
				}),
				new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
				{
					new Vector3Keyframe(0f, parentY.localPosition, Ease.Linear),
					new Vector3Keyframe(speed, new Vector3(0f, -380f), Ease.SineInOut)
				}, delegate (Vector3 x)
				{
					parentY.localPosition = x;
				}),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				onComplete?.Invoke();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void PlayOnce(RTAnimation animation, bool stopOthers = true, Predicate<RTAnimation> predicate = null, Action onComplete = null)
		{
			if (stopOthers && predicate != null)
				animations.FindAll(predicate).ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
				});

			animation.onComplete += delegate ()
			{
				animations.Remove(animation);
				onComplete?.Invoke();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void StopAnimations(Predicate<RTAnimation> predicate = null)
		{
			Predicate<RTAnimation> match = x => x.playing;
			if (predicate != null)
				animations.FindAll(predicate).ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
					animations.Remove(anim);
				});
			else
				animations.FindAll(match).ForEach(delegate (RTAnimation anim)
				{
					anim.Stop();
					animations.Remove(anim);
				});
		}

		public void Kill()
		{
			StopAnimations();
			animations = null;
			Destroy(parentX.gameObject);
			Destroy(dialogueBase.gameObject);
			Destroy(baseCanvas);
			Destroy(gameObject);
		}

		public void PlaySound(AudioClip clip, float pitch = 1f, float volume = 1f, bool loop = false)
		{
			AudioSource audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
			audioSource.clip = clip;
			audioSource.playOnAwake = true;
			audioSource.loop = loop;
			audioSource.pitch = pitch;
			audioSource.volume = Mathf.Clamp(volume, 0f, 2f) * AudioManager.inst.sfxVol;
			audioSource.Play();

			inst.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length));
		}

        #endregion

        #region Animations

        public List<string> defaultAnimations = new List<string>();

		public List<RTAnimation> animations = new List<RTAnimation>();

        #endregion
    }
}
