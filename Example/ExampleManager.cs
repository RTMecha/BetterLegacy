using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        public GameObject blocker;

        #region Sprites

        public string ExamplePartsPath = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/";
        public string SpeakPath => $"{ExamplePartsPath}example speak.ogg";
        public string TailPath => $"{ExamplePartsPath}example tail.png";
        public string EarBottomPath => $"{ExamplePartsPath}example ear bottom.png";
        public string HeadPath => $"{ExamplePartsPath}example head.png";
        public string EyesPath => $"{ExamplePartsPath}example eyes.png";
        public string PupilsPath => $"{ExamplePartsPath}example pupils.png";
        public string BlinkPath => $"{ExamplePartsPath}example blink.png";
        public string SnoutPath => $"{ExamplePartsPath}example snout.png";
        public string MouthPath => $"{ExamplePartsPath}example mouth.png";
        public string LipsPath => $"{ExamplePartsPath}example lips.png";
        public string NosePath => $"{ExamplePartsPath}example nose.png";
        public string BrowsPath => $"{ExamplePartsPath}example brow.png";
        public string EarTopPath => $"{ExamplePartsPath}example ear top.png";

        public string HandsPath => $"{ExamplePartsPath}example hand.png";

        #endregion

        #region Movement

        public Vector2 TotalPosition => parentX == null || parentY == null ? Vector2.zero : new Vector2(parentX.localPosition.x, parentY.localPosition.y);

        public float floatingLevel;

        #endregion

        #region Parents

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

        public string lastDialogue;

        public AudioClip speakSound;

        public Text dialogueText;
        public Image dialogueImage;
        public Transform dialogueBase;

        public Dictionary<string, DialogueGroup> dialogueDictionary = new Dictionary<string, DialogueGroup>();

        float repeat = 60f;
        bool said = false;
        public bool canSayThing = false;

        public void Sleep()
        {
            var a = allowBlinking;
            allowBlinking = false;
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
                    }, x => { eyes.localScale = new Vector3(1f, x, 1f); }),
                };

            animation.onComplete = () => { allowBlinking = a; };

            PlayOnce(animation, false);
        }

        public void SayDialogue(string dialogueName)
        {
            if (!dialogueDictionary.ContainsKey(dialogueName))
                return;

            var dialogues = dialogueDictionary[dialogueName].dialogues;
            var random = UnityEngine.Random.Range(0, dialogues.Length);

            while (!dialogues[random].CanSay)
                random = UnityEngine.Random.Range(0, dialogues.Length);

            Say(dialogues[random].text);

            talking = false;
        }

        void LoadDialogue()
        {
            dialogueDictionary.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/dialogue.json"));

            for (int i = 0; i < jn["dialogue_groups"].Count; i++)
            {
                var dialogueGroup = jn["dialogue_groups"][i];

                var dialogues = new ExampleDialogue[dialogueGroup["dialogue"].Count];
                for (int j = 0; j < dialogueGroup["dialogue"].Count; j++)
                {
                    var dialogue = dialogueGroup["dialogue"][j];
                    int dialogueFunction = 0;
                    if (dialogue["func_index"] != null)
                        dialogueFunction = dialogue["func_index"].AsInt;

                    DialogueFunction dialogueFunc =
                        dialogue["func"] != null ?
                        () =>
                        {
                            try
                            {
                                var evaluation = RTCode.EvaluateWithReturn(dialogue["func"]);
                                return evaluation is bool boolean && boolean;
                            }
                            catch
                            {
                                return false;
                            }
                        } : dialogueFunctions[dialogueFunction];

                    dialogues[j] = new ExampleDialogue(dialogue["text"], dialogueFunc, () =>
                    {
                        if (dialogue["action"] != null)
                            RTCode.Evaluate(dialogue["action"]);
                    });
                }

                if (!dialogueDictionary.ContainsKey(dialogueGroup["name"]))
                    dialogueDictionary.Add(dialogueGroup["name"], new DialogueGroup(dialogueGroup["name"], dialogues));
            }
        }

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
            CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "diggy" ||
            CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "diggydog" ||
            CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "diggydog176";

        public static bool UserIsCubeCube() =>
            CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "cubecube";

        public static bool UserIsTori() => CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "karasutori";
        public static bool UserIsPlayer() => CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "player";

        public static bool TimeLongerThan10Hours() => Time.time > 36000f;

        public static bool SayAnyways() => true;

        public static bool ObjectsAliveCountHigherThan900() => DataManager.inst.gameData != null && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.TimeWithinLifespan()).Count > 900;

        public static bool LevelCountIsZero() => EditorManager.inst && EditorManager.inst.loadedLevels.Count <= 0;

        public static bool UserIsMoNsTeR() => CoreConfig.Instance.DisplayName.Value.Replace(" ", "").ToLower() == "monster";

        public void RepeatDialogues()
        {
            if (talking)
                return;

            float t = time % repeat;
            if (t > repeat - 1f)
            {
                if (said)
                    return;

                said = true;

                SayDialogue("OccasionalDialogue");
                repeat = UnityEngine.Random.Range(60f, 6000f);
            }
            else
                said = false;
        }

        public DialogueFunction[] dialogueFunctions = new DialogueFunction[]
        {
            SayAnyways, // 0
            HasLoadedLevel, // 1
            CanSay, // 2
            ApplicationNotFocused, // 3
            HasNotLoadedLevel, // 4
            LeftHandBeingDragged, // 5
            RightHandBeingDragged, // 6
            BeingDragged, // 7
            UserIsSleepyz, // 8
            UserIsMecha, // 9
            UserIsDiggy, // 10
            UserIsCubeCube, // 11
            UserIsTori, // 12
            UserIsPlayer, // 13
            TimeLongerThan10Hours, // 14
            ObjectsAliveCountHigherThan900, // 15
            LevelCountIsZero, // 16
            UserIsMoNsTeR, // 17
        };

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

        public float dragDelay = 0.3f;

        #endregion

        #region Memories

        public JSONNode memory = JSON.Parse("{}");

        public void LoadMemory()
        {
            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/example_memory.json"))
            {
                memory = JSON.Parse(RTFile.ReadFromFile("profile/example_memory.json"));
            }
        }

        #endregion

        #region Talk

        void LoadCommands()
        {
            commands.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/commands.json"));

            for (int i = 0; i < jn["commands"].Count; i++)
                commands.Add(ExampleCommand.Parse(jn["commands"][i]));
        }

        public List<ExampleCommand> commands = new List<ExampleCommand>();

        public GameObject autocomplete;
        public InputField chatter;
        public RectTransform chatterBase;
        public RectTransform autocompleteContent;

        public bool chatting = false;

        float timeSinceLastInteractedOffset = 0f;
        float timeSinceLastInteracted = 0f;

        public void HandleChatting()
        {
            if (chatter == null)
                return;
            SetLastInteracted();
            for (int i = 0; i < commands.Count; i++)
                commands[i].CheckResponse(chatter.text);
        }

        public bool RegexMatch(Regex regex, string text, out Match match)
        {
            match = regex.Match(text);
            return match.Success;
        }

        public GameObject commandAutocompletePrefab;

        #endregion

        #region Options

        public Transform optionsLayout;
        public Transform optionsBase;
        public static bool optionsActive;

        #endregion

        #region Delegates

        public static Action<EditorManager> onEditorAwake;
        public static Action<bool> onEditorToggle;
        public static Action<string> onSceneLoad;
        public static Action<GameManager> onGameAwake;

        public static Action onInit = () => Creator.NewGameObject("ExampleManager", SystemManager.inst.transform).AddComponent<ExampleManager>();

        public Action update;
        public Action fixedUpdate;
        public Action lateUpdate;
        public static Action onSpawnComplete;

        #endregion

        float time = 0f;

        float timeOffset;

        public bool talking;

        public bool dying;

        /// <summary>
        /// Spawns Example.
        /// </summary>
        public static void Init()
        {
            if (!inst && ExampleConfig.Instance.ExampleSpawns.Value)
            {
                onInit?.Invoke();
                return;
            }

            if (inst)
                inst.Say("I'm already here!");
        }

        void Awake()
        {
            timeOffset = Time.time;
            timeSinceLastInteractedOffset = Time.time;

            if (inst == null)
                inst = this;
            else if (inst != this)
            {
                Kill();
                return;
            }

            try
            {
                LoadMemory();
                LoadTutorials();
                LoadDialogue();
                LoadCommands();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not load Example data.\nException: {ex}");
            }

            StartCoroutine(SpawnExample());
        }

        void Update()
        {
            time = Time.time - timeOffset;
            timeSinceLastInteracted = Time.time - timeSinceLastInteractedOffset;

            if (Input.GetKeyDown(ExampleConfig.Instance.ExampleVisiblityToggle.Value) && !CoreHelper.IsUsingInputField)
                ExampleConfig.Instance.ExampleVisible.Value = !ExampleConfig.Instance.ExampleVisible.Value;

            if (ProjectPlannerManager.inst && animations.Where(x => x.name.Contains("DIALOGUE: ") && x.playing).Count() < 1)
                foreach (var schedule in ProjectPlannerManager.inst.planners.Where(x => x.PlannerType == ProjectPlannerManager.PlannerItem.Type.Schedule).Select(x => x as ProjectPlannerManager.ScheduleItem))
                {
                    if (!schedule.hasBeenChecked && schedule.IsActive)
                    {
                        schedule.hasBeenChecked = true;
                        Say($"Reminding you about your schedule \"{schedule.Description}\" at {schedule.DateTime}");
                        ProjectPlannerManager.inst.SaveSchedules();
                    }
                }

            update?.Invoke();

            if (baseCanvas && canvas)
            {
                if (EditorManager.inst && EditorManager.inst.isEditing)
                    baseCanvas.SetActive(ExampleConfig.Instance.EnabledInEditor.Value);
                else if (GameManager.inst)
                    baseCanvas.SetActive(ExampleConfig.Instance.EnabledInGame.Value);
                else if (ArcadeManager.inst.ic)
                    baseCanvas.SetActive(ExampleConfig.Instance.EnabledInMenus.Value);

                canvas.scaleFactor = CoreHelper.ScreenScale;
            }

            if (autocomplete && chatter)
                autocomplete.SetActive(!string.IsNullOrEmpty(chatter.text));

            if (animations == null || !Visible || dying)
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

                var active = t > blinkRate - 0.3f && t < blinkRate && canBlink;
                blinkCanChange = !active;
                blink?.gameObject.SetActive(active);
            }
            else
            {
                blink?.gameObject?.SetActive(true);
            }

            RepeatDialogues();

            floatingLevel = time * 0.5f % 2f;

            if (floatingParent != null)
                floatingParent.localPosition = new Vector3(0f, (Ease.SineInOut(floatingLevel) - 0.5f) * 2f, 0f);
        }

        void FixedUpdate() => fixedUpdate?.Invoke();

        void LateUpdate()
        {
            lateUpdate?.Invoke();

            // Example breaks if application isn't focused.
            if (!Application.isFocused || spawning || !Visible)
                return;

            if (lookAt)
            {
                float t = time % pupilsLookRate;

                // Here we add a tiny amount of movement to the pupils to make Example feel a lot more alive.
                if (t > pupilsLookRate - 0.3f && t < pupilsLookRate && pupilsCanChange)
                    pupilsOffset = new Vector2(UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f));

                pupilsCanChange = !(t > pupilsLookRate - 0.3f && t < pupilsLookRate);

                pupils.AsRT().anchoredPosition = RTMath.Lerp(Vector2.zero, MousePosition - new Vector2(pupils.position.x, pupils.position.y), lookMultiplier) + pupilsOffset;

                // If face should also track mouse position.
                if (faceCanLook)
                {
                    var lerp = RTMath.Lerp(Vector2.zero, MousePosition - new Vector2(faceX.position.x, faceY.position.y), faceLookMultiplier);
                    faceX.localPosition = new Vector3(lerp.x, 0f, 0f);
                    faceY.localPosition = new Vector3(0f, lerp.y, 0f);
                    mouthBase.localPosition = new Vector3(lerp.x, (lerp.y * 0.5f) + -30f, 0f);
                    nose.localPosition = new Vector3(lerp.x, (lerp.y * 0.5f) + -20f, 0f);
                }
            }

            // Set ear and tail rotation, forcing them to rotate to the other side of the face.
            if (faceX != null && tail != null && ears != null)
            {
                float faceXPos = faceX.localPosition.x * 5f;

                tail.localRotation = Quaternion.Euler(0f, 0f, -faceXPos);
                tail.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -faceXPos);
                ears.localRotation = Quaternion.Euler(0f, 0f, faceX.localPosition.x * 0.8f);
            }

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;

            float p = Time.deltaTime * 60f;
            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(dragDelay, 0.001f, 1f), p);

            float x = startMousePos.x - vector.x;
            float y = startMousePos.y - vector.y;
            var target = new Vector3(startDragPos.x + -x, startDragPos.y + -y);

            if (dragging)
            {
                parentRotscale.localRotation = Quaternion.Euler(0f, 0f, (target.x - dragPos.x) * po);

                dragPos += (target - dragPos) * po;

                parentX.localPosition = new Vector3(Mathf.Clamp(dragPos.x, -970f, 970f), 0f);
                parentY.localPosition = new Vector3(0f, Mathf.Clamp(dragPos.y, -560f, 560f));

                faceX.localPosition = new Vector3(-((target.x - dragPos.x) * po), 0f);
                faceY.localPosition = new Vector3(0f, -((target.y - dragPos.y) * po));
            }

            if (draggingLeftHand)
                handLeft.localPosition += (target - handLeft.localPosition) * po;

            if (draggingRightHand)
                handRight.localPosition += (target - handRight.localPosition) * po;

            float addToDialogueY = 200f;
            if (TotalPosition.y > 355f)
                addToDialogueY = -200f;

            // Set dialogue box position instead of parent because only position needs to be tracked instead of scale, rotation, etc.
            if (dialogueBase != null)
                dialogueBase.localPosition = new Vector3(Mathf.Clamp(TotalPosition.x, -820f, 820f), TotalPosition.y + addToDialogueY, 0f);

            float addToOptionsX = -222f;
            if (TotalPosition.x < -640f)
                addToOptionsX = 222f;

            // Same situation as the dialogue box.
            if (optionsBase != null && optionsActive)
                optionsBase.localPosition = new Vector3(TotalPosition.x + addToOptionsX, TotalPosition.y);

            if (canvasGroup)
                canvasGroup.alpha = ExampleConfig.Instance.ExampleVisible.Value ? ExampleConfig.Instance.ExampleVisibility.Value : 1f;

            if (dragging || draggingLeftHand || draggingRightHand)
                SetLastInteracted();

            if (mouthLower != null)
            {
                var m = mouthLower.localScale;
                m.y = Mathf.Clamp(m.y, 0f, 1f);
                mouthLower.localScale = m;
            }
        }

        public void SetLastInteracted() => timeSinceLastInteractedOffset = Time.time;

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
                    }, x =>
                    {
                        if (parentRotscale != null)
                            parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.9f, 60f, Ease.BackIn),
                        new FloatKeyframe(1.3f, 80f, Ease.SineOut),
                        new FloatKeyframe(1.5f, 70f, Ease.SineInOut),
                        new FloatKeyframe(1.8f, 80f, Ease.SineInOut),
                    }, x =>
                    {
                        if (handLeft != null)
                            handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, new Vector2(1f, 0.3f), Ease.Linear),
                        new Vector2Keyframe(0.5f, new Vector2(1f, 0.4f), Ease.CircIn),
                        new Vector2Keyframe(1f, new Vector2(1f, 0.6f), Ease.BackOut),
                    }, x =>
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
                    }, x =>
                    {
                        if (parentY != null)
                            parentY.localScale = new Vector3(x.x, x.y, 1f);
                    }),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, new Vector3(200f, 0f), Ease.Linear),
                        new Vector3Keyframe(2f, new Vector3(750f, 0f, 0f), Ease.SineOut),
                    }, x =>
                    {
                        if (parentX != null)
                            parentX.localPosition = x;
                    }),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, new Vector3(0f, -700f, 0f), Ease.Linear),
                        new Vector3Keyframe(1f, new Vector3(0f, -290f, 0f), Ease.SineOut),
                        new Vector3Keyframe(2f, new Vector3(0f, -410f, 0f), Ease.SineInOut),
                    }, x =>
                    {
                        if (parentY != null)
                            parentY.localPosition = x;
                    }),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, new Vector3(0f, 1, 0f), Ease.Linear),
                        new Vector3Keyframe(0.4f, new Vector3(0f, -1f, 0f), Ease.SineOut),
                        new Vector3Keyframe(0.8f, new Vector3(0f, 0f, 0f), Ease.SineOut),
                    }, x =>
                    {
                        if (pupils != null)
                            pupils.localPosition = x;
                    })
                };

                waveAnimation.onComplete += () =>
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
                    }, x => { browLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.2f, -15f, Ease.SineOut),
                    }, x => { browRight.localRotation = Quaternion.Euler(0f, 0f, x); }),
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
                    }, x => { faceX.localPosition = new Vector3(x, 0f, 0f); }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(1.5f, -3f, Ease.CircIn),
                    }, x => { faceY.localPosition = new Vector3(0f, x, 0f); }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(1.5f, 180f, Ease.CircIn),
                    }, x => { handLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(1.5f, 30f, Ease.CircIn),
                    }, x => { handRight.localRotation = Quaternion.Euler(0f, 0f, x); }),
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
                        new Vector2Keyframe(0.5f, new Vector2(1.1f, 0.9f), Ease.SineOut),
                        new Vector2Keyframe(1.1f, Vector2.one, Ease.SineInOut),
                        new Vector2Keyframe(4f, new Vector2(0.7f, 1.3f), Ease.SineIn),
                    }, x => { parentY.localScale = new Vector3(x.x, x.y, 1f); }),
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
                    }, x => { parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x); }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.6f, 0f, Ease.SineInOut),
                    }, x => { handLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
                        new Vector3Keyframe(0.6f, Vector3.zero, Ease.SineInOut)
                    }, x => { parentX.localPosition = x; }),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
                        new Vector3Keyframe(0.6f, Vector3.zero, Ease.SineInOut)
                    }, x => { parentY.localPosition = x; }),
                };

                animations.Add(animation);
            }

            yield break;
        }

        IEnumerator SpawnExample()
        {
            spawning = true;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/ExampleHooks.cs"))
                yield return StartCoroutine(RTCode.IEvaluate(RTFile.ReadFromFile("settings/ExampleHooks.cs")));

            var p = SpeakPath;
            StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{SpeakPath}", RTFile.GetAudioType(SpeakPath), audioClip => { speakSound = audioClip; }));

            #region Canvas

            var uiCanvas = UIManager.GenerateUICanvas("Example Canvas", null, true);

            baseCanvas = uiCanvas.GameObject;
            canvas = uiCanvas.Canvas;
            canvasGroup = uiCanvas.CanvasGroup;

            blocker = Creator.NewUIObject("Interaction Blocker", baseCanvas.transform);
            blocker.transform.AsRT().anchoredPosition = Vector2.zero;
            blocker.transform.AsRT().sizeDelta = new Vector2(10000f, 10000f);
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(1f, 1f, 1f, 0f);
            blocker.SetActive(false);

            #endregion

            #region Floating Parent

            var l_floatingParent = Creator.NewUIObject("Example", baseCanvas.transform);
            l_floatingParent.transform.AsRT().anchoredPosition = Vector2.zero;
            floatingParent = l_floatingParent.transform;

            #endregion

            #region X Parent

            var xparent = Creator.NewUIObject("Example X", floatingParent);
            xparent.transform.AsRT().anchoredPosition = Vector2.zero;
            parentX = xparent.transform;

            #endregion

            #region Y Parent

            var yparent = Creator.NewUIObject("Example Y", parentX);
            yparent.transform.AsRT().anchoredPosition = new Vector2(0f, -1600f);
            parentY = yparent.transform;

            #endregion

            #region Rotscale Parent

            var rotscaleparent = Creator.NewUIObject("Example Rotscale", parentY);
            rotscaleparent.transform.AsRT().anchoredPosition = Vector3.zero;
            parentRotscale = rotscaleparent.transform;

            #endregion

            #region Head

            var l_head = Creator.NewUIObject("Example Head", parentRotscale);
            l_head.transform.AsRT().anchoredPosition = Vector2.zero;
            head = l_head.transform;
            {
                var im = Creator.NewUIObject("image", l_head.transform);
                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HeadPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        optionsActive = !optionsActive;
                        optionsBase.gameObject.SetActive(optionsActive);
                    }
                };
                clickable.onDown = pointerEventData =>
                {
                    if (pointerEventData.button != PointerEventData.InputButton.Left)
                        return;

                    StopAnimations(x => x.name == "End Drag Example" || x.name == "Drag Example" || x.name.ToLower().Contains("movement"));

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
                        }, x => { mouthLower.localScale = new Vector3(1f, x, 1f); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0.5f, Ease.SineOut),
                        }, x => { lips.localScale = new Vector3(1f, x, 1f); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 2f, Ease.SineOut),
                        }, x => { lips.localPosition = new Vector3(0f, x, 0f); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 10f, Ease.SineOut),
                        }, x => { head.localPosition = new Vector3(head.localPosition.x, x, head.localPosition.z); }),
						// Hands
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.05f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                        }, x => { handLeft.GetChild(0).localPosition = new Vector3(0f, x, 0f); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.05f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                        }, x => { handRight.GetChild(0).localPosition = new Vector3(0f, x, 0f); }),
						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browLeft.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.3f, tbrowLeft, Ease.SineOut),
                        }, x => { browLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browRight.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.3f, tbrowRight, Ease.SineOut),
                        }, x => { browRight.localRotation = Quaternion.Euler(0f, 0f, x); }),

                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
                            new Vector2Keyframe(0.3f, new Vector2(1.05f, 0.95f), Ease.SineOut),
                        }, x => { parentY.localScale = new Vector3(x.x, x.y, 1f); }),
                    };

                    PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
                };
                clickable.onUp = pointerEventData =>
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
                        }, x =>
                        {
                            parentY.localScale = new Vector3(x.x, x.y, 1f);
                        }),
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, new Vector2(parentRotscale.localScale.x, parentRotscale.localScale.y), Ease.Linear),
                            new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.ElasticOut),
                        }, x =>
                        {
                            parentRotscale.localScale = new Vector3(x.x, x.y, 1f);
                        }),

                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, parentRotscale.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(1f, t, Ease.BackOut),
                        }, x =>
                        {
                            parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, mouthLower.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0.5f, Ease.SineIn),
                        }, x =>
                        {
                            mouthLower.localScale = new Vector3(1f, x, 1f);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 1f, Ease.SineIn),
                        }, x =>
                        {
                            lips.localScale = new Vector3(1f, x, 1f);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0f, Ease.SineIn),
                        }, x =>
                        {
                            lips.localPosition = new Vector3(0f, x, 0f);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.BounceOut),
                        }, x =>
                        {
                            head.localPosition = new Vector3(head.localPosition.x, x, head.localPosition.z);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.1f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                        }, x =>
                        {
                            handLeft.GetChild(0).localPosition = new Vector3(0f, x, 0f);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.1f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                        }, x =>
                        {
                            handRight.GetChild(0).localPosition = new Vector3(0f, x, 0f);
                        }),

						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browLeft.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.5f, tbrowLeft, Ease.SineOut),
                        }, x =>
                        {
                            browLeft.localRotation = Quaternion.Euler(0f, 0f, x);
                        }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browRight.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.5f, tbrowRight, Ease.SineOut),
                        }, x => { browRight.localRotation = Quaternion.Euler(0f, 0f, x); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, x => { faceX.localPosition = new Vector3(x, 0f, 0f); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, x => { faceY.localPosition = new Vector3(0f, x, 0f); }),
                    };

                    faceX.localPosition = Vector3.zero;
                    faceY.localPosition = Vector3.zero;

                    PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
                    dragging = false;
                    faceCanLook = true;
                };
            }

            var l_tail = Creator.NewUIObject("Example Tail", head);
            l_tail.transform.AsRT().anchoredPosition = Vector2.zero;
            tail = l_tail.transform;
            tail.SetSiblingIndex(0);
            {
                var im = Creator.NewUIObject("image", l_tail.transform); 
                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, -58f);
                im.transform.AsRT().sizeDelta = new Vector2(28f, 42f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{TailPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    talking = true;
                    Say("Please don't touch me there.", new List<IKeyframe<float>> { new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear) }, new List<IKeyframe<float>> { new FloatKeyframe(0f, parentY.localPosition.y + 200f, Ease.Linear) }, onComplete: () => { talking = false; });
                    Play("Angry", false);
                };
            }

            var l_ears = Creator.NewUIObject("Example Ears", head);
            l_ears.transform.AsRT().anchoredPosition = Vector2.zero;
            ears = l_ears.transform;
            ears.SetSiblingIndex(0);

            #endregion

            #region Ears

            var l_earbottomleft = Creator.NewUIObject("Example Ear Bottom Left", ears);
            l_earbottomleft.transform.AsRT().anchoredPosition = new Vector2(25f, 35f);
            l_earbottomleft.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -30f));
            earBottomLeft = l_earbottomleft.transform;
            {
                var im = Creator.NewUIObject("image", l_earbottomleft.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.2f);
                im.transform.AsRT().sizeDelta = new Vector2(44f, 52f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_earbottomright = Creator.NewUIObject("Example Ear Bottom Right", ears);
            l_earbottomright.transform.AsRT().anchoredPosition = new Vector2(-25f, 35f);
            l_earbottomright.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 30f));
            earBottomRight = l_earbottomright.transform;
            {
                var im = Creator.NewUIObject("image", l_earbottomright.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.2f);
                im.transform.AsRT().sizeDelta = new Vector2(44f, 52f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarBottomPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => {  Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            #endregion

            #region Face

            var l_faceX = Creator.NewUIObject("Example Face X", head);
            l_faceX.transform.AsRT().anchoredPosition = Vector3.zero;
            faceX = l_faceX.transform;

            var l_faceY = Creator.NewUIObject("Example Face Y", faceX);
            l_faceY.transform.AsRT().anchoredPosition = Vector3.zero;
            faceY = l_faceY.transform;

            #endregion

            #region Eyes

            var l_eyes = Creator.NewUIObject("Example Eyes", faceY);
            l_eyes.transform.AsRT().anchoredPosition = Vector2.zero;
            eyes = l_eyes.transform;
            {
                var im = Creator.NewUIObject("image", l_eyes.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().sizeDelta = new Vector2(74f, 34f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EyesPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_pupils = Creator.NewUIObject("Example Pupils", eyes);
            l_pupils.transform.AsRT().anchoredPosition = Vector2.zero;
            pupils = l_pupils.transform;
            {
                var im = Creator.NewUIObject("image", l_pupils.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().sizeDelta = new Vector2(47f, 22f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{PupilsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_blink = Creator.NewUIObject("Example Blink", eyes);
            l_blink.transform.AsRT().anchoredPosition = Vector2.zero;
            blink = l_blink.transform;
            {
                var im = Creator.NewUIObject("image", l_blink.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().sizeDelta = new Vector2(74f, 34f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BlinkPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            #endregion

            #region Snout

            var l_snout = Creator.NewUIObject("Example Snout", faceY);
            l_snout.transform.AsRT().anchoredPosition = Vector2.zero;
            snout = l_snout.transform;
            {
                var im = Creator.NewUIObject("image", l_snout.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, -31f);
                im.transform.AsRT().sizeDelta = new Vector2(60f, 31f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{SnoutPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_mouthBase = Creator.NewUIObject("Example Mouth Base", snout);
            l_mouthBase.transform.AsRT().anchoredPosition = new Vector2(0f, -30f);
            mouthBase = l_mouthBase.transform;

            var l_mouthUpper = Creator.NewUIObject("Example Mouth Upper", mouthBase);
            l_mouthUpper.transform.localScale = new Vector3(1f, 0.15f, 1f);
            l_mouthUpper.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
            l_mouthUpper.transform.AsRT().anchoredPosition = Vector3.zero;
            mouthUpper = l_mouthUpper.transform;
            {
                var im = Creator.NewUIObject("image", l_mouthUpper.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 1f);
                im.transform.AsRT().sizeDelta = new Vector2(32f, 16f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_mouthLower = Creator.NewUIObject("Example Mouth Lower", mouthBase);
            l_mouthLower.transform.localScale = new Vector3(1f, 0.5f);
            l_mouthLower.transform.AsRT().anchoredPosition = Vector3.zero;
            mouthLower = l_mouthLower.transform;
            {
                var im = Creator.NewUIObject("image", l_mouthLower.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 1f);
                im.transform.AsRT().sizeDelta = new Vector2(32f, 16f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{MouthPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_lips = Creator.NewUIObject("Example Lips", mouthBase);
            l_lips.transform.AsRT().anchoredPosition = Vector3.zero;
            lips = l_lips.transform;
            {
                var im = Creator.NewUIObject("image", l_lips.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 3f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 1f);
                im.transform.AsRT().sizeDelta = new Vector2(32f, 8f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{LipsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_nose = Creator.NewUIObject("Example Nose", snout);
            l_nose.transform.AsRT().anchoredPosition = new Vector2(0f, -20f);
            nose = l_nose.transform;
            {
                var im = Creator.NewUIObject("image", l_nose.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                im.transform.AsRT().sizeDelta = new Vector2(22f, 8f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{NosePath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            #endregion

            #region Brows

            var l_browBase = Creator.NewUIObject("Example Brow Base", faceY);
            l_browBase.transform.AsRT().anchoredPosition = new Vector2(0f, 30f);
            browBase = l_browBase.transform;

            var l_browLeft = Creator.NewUIObject("Example Brow Left", browBase);
            l_browLeft.transform.AsRT().anchoredPosition = new Vector2(22f, 0f);
            browLeft = l_browLeft.transform;
            {
                var im = Creator.NewUIObject("image", l_browLeft.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(18f, 0f);
                im.transform.AsRT().pivot = new Vector2(1.7f, 0.5f);
                im.transform.AsRT().sizeDelta = new Vector2(20f, 6f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            var l_browRight = Creator.NewUIObject("Example Brow Right", browBase);
            l_browRight.transform.AsRT().anchoredPosition = new Vector2(-22f, 0f);
            browRight = l_browRight.transform;
            {
                var im = Creator.NewUIObject("image", l_browRight.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(-18f, 0f);
                im.transform.AsRT().pivot = new Vector2(-0.7f, 0.5f);
                im.transform.AsRT().sizeDelta = new Vector2(20f, 6f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{BrowsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));
            }

            #endregion

            #region Top Ears

            var l_earTopLeft = Creator.NewUIObject("Example Ear Top Left", earBottomLeft);
            l_earTopLeft.transform.AsRT().anchoredPosition = Vector2.zero;
            l_earTopLeft.transform.localRotation = Quaternion.identity;
            earTopLeft = l_earTopLeft.transform;
            {
                var im = Creator.NewUIObject("image", l_earTopLeft.transform);

                im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 45f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.275f);
                im.transform.AsRT().sizeDelta = new Vector2(44f, 80f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    var animation = new RTAnimation("Ear Left Flick");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 330f, Ease.Linear),
                            new FloatKeyframe(0.1f, 300f, Ease.SineOut),
                            new FloatKeyframe(0.7f, 330f, Ease.SineInOut),
                        }, x => { earBottomLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 270f, Ease.Linear),
                            new FloatKeyframe(0.05f, 230f, Ease.Linear),
                            new FloatKeyframe(0.3f, 300f, Ease.SineOut),
                            new FloatKeyframe(0.9f, 270f, Ease.SineInOut),
                        }, x => { earTopLeft.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, x); }),
                    };

                    PlayOnce(animation, false);
                };
            }

            var l_earTopRight = Creator.NewUIObject("Example Ear Top Right", earBottomRight);
            l_earTopRight.transform.AsRT().anchoredPosition = Vector2.zero;
            l_earTopRight.transform.localRotation = Quaternion.identity;
            earTopRight = l_earTopRight.transform;
            {
                var im = Creator.NewUIObject("image", l_earTopRight.transform);

                im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 45f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.275f);
                im.transform.AsRT().sizeDelta = new Vector2(44f, 80f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{EarTopPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    var animation = new RTAnimation("Ear Right Flick");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 30f, Ease.Linear),
                            new FloatKeyframe(0.1f, 60f, Ease.SineOut),
                            new FloatKeyframe(0.7f, 30f, Ease.SineInOut),
                        }, x => { earBottomRight.localRotation = Quaternion.Euler(0f, 0f, x); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 90f, Ease.Linear),
                            new FloatKeyframe(0.05f, 130f, Ease.Linear),
                            new FloatKeyframe(0.3f, 60f, Ease.SineOut),
                            new FloatKeyframe(0.9f, 90f, Ease.SineInOut),
                        }, x => { earTopRight.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, x); }),
                    };

                    PlayOnce(animation, false);
                };
            }

            #endregion

            #region Hands

            var l_handsBase = Creator.NewUIObject("Example Hands Base", parentRotscale);
            l_handsBase.transform.AsRT().anchoredPosition = Vector2.zero;
            handsBase = l_handsBase.transform;

            var l_handLeft = Creator.NewUIObject("Example Hand Left", handsBase);
            l_handLeft.transform.AsRT().anchoredPosition = new Vector2(40f, 0f);
            handLeft = l_handLeft.transform;
            {
                var im = Creator.NewUIObject("image", l_handLeft.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, -80f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                im.transform.AsRT().sizeDelta = new Vector2(42f, 42f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData =>
                {
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    startDragPos = new Vector2(handLeft.localPosition.x, handLeft.localPosition.y);
                    draggingLeftHand = true;

                    if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
                    else AudioManager.inst.PlaySound("Click");
                };
                clickable.onUp = pointerEventData =>
                {
                    draggingLeftHand = false;

                    foreach (var levelItem in EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper))
                    {
                        if (EditorManager.RectTransformToScreenSpace(image.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                        {
                            Debug.LogFormat("{0}Picked level: {1}", className, levelItem.folder);
                        }
                    }
                };
            }

            var l_handRight = Creator.NewUIObject("Example Hand Right", handsBase);
            l_handRight.transform.AsRT().anchoredPosition = new Vector2(-40f, 0f);
            handRight = l_handRight.transform;
            {
                var im = Creator.NewUIObject("image", l_handRight.transform);
                im.transform.localRotation = Quaternion.identity;

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, -80f);
                im.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                im.transform.AsRT().sizeDelta = new Vector2(42f, 42f);

                StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{HandsPath}", texture2D =>
                {
                    image.sprite = SpriteManager.CreateSprite(texture2D);
                }, onError => { Debug.LogErrorFormat("{0}File does not exist.", className); }));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData =>
                {
                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                    startDragPos = new Vector2(handRight.localPosition.x, handRight.localPosition.y);
                    draggingRightHand = true;

                    if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
                    else AudioManager.inst.PlaySound("Click");
                };
                clickable.onUp = pointerEventData =>
                {
                    draggingRightHand = false;

                    foreach (var levelItem in EditorManager.inst.loadedLevels.Select(x => x as EditorWrapper))
                    {
                        if (EditorManager.RectTransformToScreenSpace(image.rectTransform).Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                        {
                            Debug.LogFormat("{0}Picked level: {1}", className, levelItem.folder);
                        }
                    }
                };
            }

            #endregion

            yield return StartCoroutine(SpawnOptions());
            yield return StartCoroutine(SpawnChatter());
            yield return StartCoroutine(SetupAnimations());
            yield return StartCoroutine(SpawnDialogue());

            onSpawnComplete?.Invoke();
            spawning = false;

            yield break;
        }

        IEnumerator SpawnOptions()
        {
            while (!FontManager.inst || !FontManager.inst.loadedFiles)
                yield return null;

            var optionsBase = Creator.NewUIObject("Options Base", baseCanvas.transform);

            this.optionsBase = optionsBase.transform;
            optionsBase.SetActive(optionsActive);

            var options = Creator.NewUIObject("Options", optionsBase.transform);

            var optionsImage = options.AddComponent<Image>();
            optionsImage.rectTransform.anchoredPosition = Vector2.zero;
            optionsImage.rectTransform.sizeDelta = new Vector2(200f, 250f);

            EditorThemeManager.ApplyGraphic(optionsImage, ThemeGroup.Background_2, true);

            var optionsLayout = Creator.NewUIObject("Layout", options.transform);
            this.optionsLayout = optionsLayout.transform;
            var optionsVLG = optionsLayout.AddComponent<VerticalLayoutGroup>();
            optionsVLG.childControlHeight = false;
            optionsVLG.childForceExpandHeight = false;
            optionsVLG.spacing = 4f;
            UIManager.SetRectTransform(optionsLayout.transform.AsRT(), Vector2.zero, new Vector2(0.95f, 0.95f), new Vector2(0.05f, 0.05f), new Vector2(0.5f, 0.5f), Vector2.zero);

            try
            {
                SetupOptionButton("Chat", () =>
                {
                    if (!chatterBase.gameObject.activeSelf)
                        Say("What do you want to talk about?");

                    chatterBase.gameObject.SetActive(!chatterBase.gameObject.activeSelf);
                });
                SetupOptionButton("Cya later", () =>
                {
                    faceCanLook = false;
                    Say("Alright, I'll get out of your way.", new List<IKeyframe<float>> { new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear) }, new List<IKeyframe<float>> { new FloatKeyframe(0f, parentY.localPosition.y + 200f, Ease.Linear) }, onComplete: Kill);

                    Play("Get Out", false);
                    Move(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(1.5f, parentX.localPosition.x + -400f, Ease.SineInOut)
                    }, new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(1f, parentY.localPosition.y + 80f, Ease.SineOut),
                        new FloatKeyframe(1.5f, parentY.localPosition.y + -1200f, Ease.CircIn)
                    });
                });
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error in generating buttons {ex}");
            }

            yield break;
        }

        public void SetupOptionButton(string name, UnityAction action, int index = -1)
        {
            var buttonObject = Creator.NewUIObject(name, optionsLayout);
            if (index >= 0 && index < optionsLayout.childCount)
                buttonObject.transform.SetSiblingIndex(index);

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.rectTransform.sizeDelta = new Vector2(180f, 32f);

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            button.image = buttonImage;

            var textObject = Creator.NewUIObject("Text", buttonObject.transform);
            var text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 16;
            text.font = FontManager.inst.DefaultFont;
            text.text = name;
            UIManager.SetRectTransform(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            EditorThemeManager.ApplySelectable(button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Function_2_Text);
        }

        IEnumerator SetupCommandsAutocomplete()
        {
            LSHelpers.DeleteChildren(autocompleteContent);

            foreach (var command in commands.Where(x => x.autocomplete))
            {
                var autocomplete = commandAutocompletePrefab.Duplicate(autocompleteContent, "Autocomplete");
                autocomplete.SetActive(true);

                var autocompleteButton = autocomplete.GetComponent<Button>();
                autocompleteButton.onClick.ClearAll();
                autocompleteButton.onClick.AddListener(() =>
                {
                    chatter.text = command.name;
                    command.response?.Invoke(command.name);
                    SearchCommandAutocomplete(command.name);
                });

                EditorThemeManager.ApplySelectable(autocompleteButton, ThemeGroup.List_Button_1);

                var autocompleteName = autocomplete.transform.Find("Name").GetComponent<Text>();
                autocompleteName.text = command.name.ToUpper();
                autocompleteName.fontSize = 28;
                autocompleteName.fontStyle = FontStyle.Bold;
                EditorThemeManager.ApplyLightText(autocompleteName);
                var autocompleteDesc = autocomplete.transform.Find("Desc").GetComponent<Text>();
                autocompleteDesc.text = command.desc;
                EditorThemeManager.ApplyLightText(autocompleteDesc);
            }

            yield break;
        }

        public void SearchCommandAutocomplete(string searchTerm)
        {
            CoreHelper.Log($"Typing: {searchTerm}");
            int num = 0;
            for (int i = 0; i < commands.Count; i++)
            {
                if (!commands[i].autocomplete)
                    continue;
                
                try
                {
                    autocompleteContent.GetChild(num).gameObject.SetActive(CoreHelper.SearchString(searchTerm, commands[i].name));
                }
                catch
                {

                }
                num++;
            }
        }

        IEnumerator SpawnChatter()
        {
            chatterBase = Creator.NewUIObject("Discussion Base", baseCanvas.transform).transform.AsRT();
            chatterBase.transform.AsRT().anchoredPosition = Vector2.zero;
            chatterBase.transform.AsRT().sizeDelta = new Vector2(800f, 96f);
            chatterBase.transform.localScale = Vector2.one;

            var chatterImage = chatterBase.gameObject.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(chatterImage, ThemeGroup.Background_1, true, roundedSide: SpriteManager.RoundedSide.Top);

            var draggable = chatterBase.gameObject.AddComponent<SelectGUI>();
            draggable.OverrideDrag = true;
            draggable.target = chatterBase;
            draggable.ogPos = chatterBase.anchoredPosition;

            var title = UIManager.GenerateUIText("Title", chatterBase.transform);
            var titleText = (Text)title["Text"];
            titleText.text = "Example Commands";
            titleText.rectTransform.anchoredPosition = new Vector2(8f, -16f);
            titleText.rectTransform.sizeDelta = new Vector2(800f, 100f);

            var uiField = UIManager.GenerateUIInputField("Discussion", chatterBase);

            var chatterField = ((GameObject)uiField["GameObject"]).transform.AsRT();
            chatter = (InputField)uiField["InputField"];

            chatterField.AsRT().anchoredPosition = new Vector2(0f, -32);
            chatterField.AsRT().sizeDelta = new Vector2(800f, 64f);

            chatter.textComponent.alignment = TextAnchor.MiddleLeft;
            chatter.textComponent.fontSize = 40;

            chatter.onValueChanged.AddListener(SearchCommandAutocomplete);

            chatter.onEndEdit.AddListener(_val => { HandleChatting(); });

            EditorThemeManager.ApplyInputField(chatter);

            autocomplete = Creator.NewUIObject("Autocomplete", chatterBase);
            UIManager.SetRectTransform(autocomplete.transform.AsRT(), new Vector2(-16f, -64f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(768f, 300f));

            EditorThemeManager.ApplyGraphic(autocomplete.AddComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteManager.RoundedSide.Bottom);

            var scrollrect = autocomplete.AddComponent<ScrollRect>();
            scrollrect.decelerationRate = 0.135f;
            scrollrect.elasticity = 0.1f;
            scrollrect.horizontal = false;
            scrollrect.movementType = ScrollRect.MovementType.Elastic;
            scrollrect.scrollSensitivity = 20f;

            var scrollbar = Creator.NewUIObject("Scrollbar", autocomplete.transform);
            var scrollbarImage = scrollbar.AddComponent<Image>();
            UIManager.SetRectTransform(scrollbar.transform.AsRT(), Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f));

            var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = Creator.NewUIObject("Sliding Area", scrollbar.transform);
            UIManager.SetRectTransform(slidingArea.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

            var handle = Creator.NewUIObject("Handle", slidingArea.transform);
            UIManager.SetRectTransform(handle.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
            var handleImage = handle.AddComponent<Image>();
            scrollbarComponent.handleRect = handle.transform.AsRT();
            scrollbarComponent.targetGraphic = handleImage;

            scrollrect.verticalScrollbar = scrollbarComponent;

            EditorThemeManager.ApplyScrollbar(scrollbarComponent);

            var mask = Creator.NewUIObject("Mask", autocomplete.transform);
            UIManager.SetRectTransform(mask.transform.AsRT(), new Vector2(0f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            var maskImage = mask.AddComponent<Image>();
            var maskComponent = mask.AddComponent<Mask>();
            maskComponent.showMaskGraphic = false;

            var content = Creator.NewUIObject("Content", mask.transform);
            UIManager.SetRectTransform(content.transform.AsRT(), new Vector2(0f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var gridLayoutGroup = content.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.cellSize = new Vector2(768f, 86f);
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 1;
            gridLayoutGroup.spacing = new Vector2(0f, 8f);
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Vertical;
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;

            autocompleteContent = content.transform.AsRT();
            scrollrect.content = autocompleteContent;

            commandAutocompletePrefab = Creator.NewUIObject("Autocomplete Prefab", chatterBase);
            commandAutocompletePrefab.SetActive(false);
            var commandAutocompletePrefabImage = commandAutocompletePrefab.AddComponent<Image>();

            commandAutocompletePrefab.AddComponent<Button>().image = commandAutocompletePrefabImage;

            var commandAutocompletePrefabName = UIManager.GenerateUIText("Name", commandAutocompletePrefab.transform);
            UIManager.SetRectTransform((RectTransform)commandAutocompletePrefabName["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            var commandAutocompletePrefabDesc = UIManager.GenerateUIText("Desc", commandAutocompletePrefab.transform);
            UIManager.SetRectTransform((RectTransform)commandAutocompletePrefabDesc["RectTransform"], new Vector2(0f, -16f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f));

            StartCoroutine(SetupCommandsAutocomplete());

            chatterBase.gameObject.SetActive(false);

            yield break;
        }

        IEnumerator SpawnDialogue()
        {
            var dialogueBase = Creator.NewUIObject("Dialogue Base", baseCanvas.transform);
            dialogueBase.transform.AsRT().anchoredPosition = Vector2.zero;

            this.dialogueBase = dialogueBase.transform;

            var dialogueImage = Creator.NewUIObject("Image", dialogueBase.transform);
            dialogueImage.transform.AsRT().anchoredPosition = Vector2.zero;
            dialogueImage.transform.AsRT().sizeDelta = new Vector2(300f, 150f);

            this.dialogueImage = dialogueImage.AddComponent<Image>();

            var dialogueText = Creator.NewUIObject("Text", dialogueImage.transform);
            dialogueText.transform.AsRT().anchoredPosition = Vector2.zero;
            dialogueText.transform.AsRT().sizeDelta = new Vector2(280f, 140f);

            var text = dialogueText.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = 22;
            text.color = new Color(0.06f, 0.06f, 0.06f, 1f);
            this.dialogueText = text;

            talking = true;
            Play("Wave", onComplete: () =>
            {
                ResetPositions(1.6f, onComplete: () => { SayDialogue("SpawnText"); });
            });

            Say("Hello, I am Example and this is a test!");

            Debug.Log($"{className}Spawned!");

            yield break;
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
                animations.FindAll(x => x.playing).ForEach(anim => { anim.Stop(); });

            var animation = animations.Find(x => x.name == anim);

            animation.ResetTime();

            if (onComplete != null)
                animation.onComplete = onComplete;

            animation.Play();
        }

        //ExampleManager.inst.Say("Hello, I am Example and this is a test!", new Vector2(0f, 200f))
        public void Say(string dialogue, List<IKeyframe<float>> xPos = null, List<IKeyframe<float>> yPos = null, float textLength = 1.5f, float stayTime = 4f, float time = 0.7f, bool stopOthers = true, Action onComplete = null)
        {
            dialogue = dialogue.Replace("{{username}}", CoreConfig.Instance.DisplayName.Value);

            var regex = new Regex(@"{{Config_(.*?)_(.*?)}}");
            var match = regex.Match(dialogue);
            if (match.Success)
            {
                var str = match.Groups[1].ToString();
                BaseConfig baseConfig = str == "CoreConfig" ? CoreConfig.Instance : EditorConfig.Instance;

                var setting = baseConfig.Settings.Find(x => x.Key == match.Groups[2].ToString());

                if (setting != null)
                {
                    dialogue = dialogue.Replace("{{Config_" + match.Groups[1].ToString() + "_" + match.Groups[2].ToString() + "}}", setting.BoxedValue.ToString());
                }
            }

            if (stopOthers)
                animations.FindAll(x => x.name.Contains("DIALOGUE: ")).ForEach(anim =>
                 {
                     anim.Stop();
                     animations.Remove(anim);
                 });

            lastDialogue = dialogue;

            if (!ExampleConfig.Instance.ExampleSpeaks.Value)
            {
                onComplete?.Invoke();
                dialogueBase.transform.localScale = new Vector3(0f, 0f, 1f);
                return;
            }

            CoreHelper.Log($"Example says: {dialogue}");
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
                xPos.ForEach(d => { listX.Add(d); });
            else listX.Add(new FloatKeyframe(0f, 0f, Ease.Linear));
            if (yPos != null)
                yPos.ForEach(d => { listY.Add(d); });
            else listY.Add(new FloatKeyframe(0f, 0f, Ease.Linear));

            int prevLetterNum = 0;

            float posX = 0f;

            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 90f, Ease.Linear),
                    new FloatKeyframe(1.5f * time, 0f, Ease.ElasticOut),
                }, x => { dialogueBase.localRotation = Quaternion.Euler(0f, 0f, x); }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1f, Ease.Linear),
                    new FloatKeyframe(textLength * time, dialogue.Length, Ease.SineOut),
                }, x =>
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
                new AnimationHandler<float>(list, x =>
                {
                    if (mouthLower != null)
                        mouthLower.localScale = new Vector3(1f, Mathf.Clamp(0f, x, 1.5f), 1f);
				}),
                new AnimationHandler<float>(listX, x => { posX = x; }),
                new AnimationHandler<float>(listY, x => { dialogueBase.localPosition = new Vector3(posX, x, 0f); }),

                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, Vector2.zero, Ease.Linear),
                    new Vector2Keyframe(0.2f * time, new Vector2(1.1f, 1.1f), Ease.SineOut),
                    new Vector2Keyframe(0.8f * time, Vector2.one, Ease.SineInOut),
                    new Vector2Keyframe(stayTime * time, Vector2.one, Ease.Linear),
                    new Vector2Keyframe((stayTime + 0.3f) * time, Vector2.zero, Ease.BackIn),
                    new Vector2Keyframe((stayTime + 0.6f) * time, Vector2.zero, Ease.Linear),
                }, x => { dialogueBase.transform.localScale = new Vector3(x.x, x.y, 1f); }),
            };

            if (DebugsOn)
                for (int i = 0; i < animation.animationHandlers.Count; i++)
                {
                    Debug.LogFormat("{0}RTAnimation {1} Length: {2}", className, i, animation.animationHandlers[i].Length);
                }

            animation.onComplete = () =>
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
                animations.FindAll(x => x.playing && x.name == "MOVEMENT").ForEach(anim =>
                {
                    anim.Stop();
                    animations.Remove(anim);
                });

            var animation = new RTAnimation("MOVEMENT");

            //var listX = new List<IKeyframe<float>>();
            //listX.Add(new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear));

            //var listY = new List<IKeyframe<float>>();
            //listY.Add(new FloatKeyframe(0f, parentY.localPosition.y, Ease.Linear));

            //x.ForEach(d => { listX.Add(d); });
            //y.ForEach(d => { listY.Add(d); });

            x.Insert(0, new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear));
            y.Insert(0, new FloatKeyframe(0f, parentY.localPosition.y, Ease.Linear));

            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(x, x => { parentX.localPosition = new Vector3(x, 0f, 0f); }),
                new AnimationHandler<float>(y, x => { parentY.localPosition = new Vector3(0f, x, 0f); }),
            };

            animation.onComplete = () =>
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
                animations.FindAll(x => x.playing && x.name == "FACE MOVEMENT").ForEach(anim =>
                {
                    anim.Stop();
                });

            var animation = new RTAnimation("FACE MOVEMENT");

            //var listX = new List<IKeyframe<float>>();
            //listX.Add(new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear));

            //var listY = new List<IKeyframe<float>>();
            //listY.Add(new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear));

            //x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
            //y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

            x.Insert(0, new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear));
            y.Insert(0, new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear));

            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(x, x => { faceX.localPosition = new Vector3(x, 0f, 0f); }),
                new AnimationHandler<float>(y, x => { faceY.localPosition = new Vector3(0f, x, 0f); }),
            };

            animation.onComplete = () =>
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
                animations.FindAll(x => x.playing && x.name == "PUPILS MOVEMENT").ForEach(anim =>
                {
                    anim.Stop();
                });

            var animation = new RTAnimation("PUPILS MOVEMENT");

            //var listX = new List<IKeyframe<float>>();
            //listX.Add(new FloatKeyframe(0f, pupils.localPosition.x, Ease.Linear));

            //var listY = new List<IKeyframe<float>>();
            //listY.Add(new FloatKeyframe(0f, pupils.localPosition.y, Ease.Linear));

            //x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
            //y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

            x.Insert(0, new FloatKeyframe(0f, pupils.localPosition.x, Ease.Linear));
            y.Insert(0, new FloatKeyframe(0f, pupils.localPosition.y, Ease.Linear));

            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(x, x => { pupils.localPosition = new Vector3(x, pupils.localPosition.y, pupils.localPosition.z); }),
                new AnimationHandler<float>(y, x => { pupils.localPosition = new Vector3(pupils.localPosition.x, x, pupils.localPosition.z); }),
            };

            animation.onComplete = () =>
            {
                lookAt = true;
                animations.Remove(animation);
                onComplete?.Invoke();
            };

            lookAt = false;
            animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        public void ResetPositions(float speed, bool stopOthers = true, Action onComplete = null)
        {
            if (stopOthers)
                animations.FindAll(x => x.playing && !x.name.Contains("DIALOGUE: ")).ForEach(anim => { anim.Stop(); });

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
                }, x => { parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x); }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                    new FloatKeyframe(speed, thl, Ease.SineInOut),
                }, x => { handLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),

                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, parentX.localPosition, Ease.Linear),
                    new Vector3Keyframe(speed, new Vector3(700f, 0f), Ease.SineInOut)
                }, x => { parentX.localPosition = x; }),
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, parentY.localPosition, Ease.Linear),
                    new Vector3Keyframe(speed, new Vector3(0f, -380f), Ease.SineInOut)
                }, x => { parentY.localPosition = x; }),
            };

            animation.onComplete = () =>
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
                animations.FindAll(predicate).ForEach(anim => { anim.Stop(); });

            animation.onComplete += () =>
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
                animations.FindAll(predicate).ForEach(anim =>
                {
                    anim.Stop();
                    animations.Remove(anim);
                });
            else
                animations.FindAll(match).ForEach(anim =>
                {
                    anim.Stop();
                    animations.Remove(anim);
                });
        }

        public void Kill()
        {
            dying = true;

            StopAnimations();
            animations = null;

            if (parentX)
                Destroy(parentX.gameObject);
            if (dialogueBase)
                Destroy(dialogueBase.gameObject);

            Destroy(baseCanvas);
            Destroy(gameObject);
        }

        public void PlaySound(AudioClip clip, float pitch = 1f, float volume = 1f, bool loop = false)
        {
            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.pitch = pitch;
            audioSource.volume = Mathf.Clamp(volume, 0f, 2f) * AudioManager.inst.sfxVol;
            audioSource.Play();

            inst.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length));
        }

        public void BrowsRaise()
        {
            float tbrowLeft = -15f;
            if (browLeft.localRotation.eulerAngles.z > 180f)
                tbrowLeft = 345f;

            float tbrowRight = 15f;
            if (browRight.localRotation.eulerAngles.z > 180f)
                tbrowRight = 345f;

            var animation = new RTAnimation("Brows");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
				// Brows
				new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, browLeft.localRotation.eulerAngles.z, Ease.Linear),
                    new FloatKeyframe(0.3f, tbrowLeft, Ease.SineOut),
                }, x => { browLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, browRight.localRotation.eulerAngles.z, Ease.Linear),
                    new FloatKeyframe(0.3f, tbrowRight, Ease.SineOut),
                }, x => { browRight.localRotation = Quaternion.Euler(0f, 0f, x); }),
            };

            PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: ") && !x.name.ToLower().Contains("movement"));
        }

        #endregion

        #region Animations

        public List<string> defaultAnimations = new List<string>();

        public List<RTAnimation> animations = new List<RTAnimation>();

        #endregion
    }
}
