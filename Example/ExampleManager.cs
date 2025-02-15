﻿using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Planners;
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

        public static string ExamplePartsPath = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Example Parts/";
        public static string SpeakPath => $"{ExamplePartsPath}example speak.ogg";
        public static string TailPath => $"{ExamplePartsPath}example tail.png";
        public static string EarBottomPath => $"{ExamplePartsPath}example ear bottom.png";
        public static string HeadPath => $"{ExamplePartsPath}example head.png";
        public static string EyesPath => $"{ExamplePartsPath}example eyes.png";
        public static string PupilsPath => $"{ExamplePartsPath}example pupils.png";
        public static string BlinkPath => $"{ExamplePartsPath}example blink.png";
        public static string SnoutPath => $"{ExamplePartsPath}example snout.png";
        public static string MouthPath => $"{ExamplePartsPath}example mouth.png";
        public static string LipsPath => $"{ExamplePartsPath}example lips.png";
        public static string NosePath => $"{ExamplePartsPath}example nose.png";
        public static string BrowsPath => $"{ExamplePartsPath}example brow.png";
        public static string EarTopPath => $"{ExamplePartsPath}example ear top.png";

        public string HandsPath => $"{ExamplePartsPath}example hand.png";

        #endregion

        #region Movement

        public Vector2 TotalPosition => parentX == null || parentY == null ? Vector2.zero : new Vector2(parentX.localPosition.x, parentY.localPosition.y);

        public float floatingLevel;

        public bool MusicPlaying => CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel && AudioManager.inst.CurrentAudioSource.isPlaying;
        public bool dancing;
        public bool canDance = true;

        public float handsBaseRotation = 0f;
        public float browLeftRotation = 0f;
        public float browRightRotation = 0f;

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
            if (!dialogueDictionary.TryGetValue(dialogueName, out DialogueGroup dialogueGroup))
                return;

            var dialogues = dialogueGroup.dialogues;
            var random = UnityEngine.Random.Range(0, dialogues.Length);

            while (!dialogues[random].CanSay)
            {
                if (dancing)
                    break;
                random = UnityEngine.Random.Range(0, dialogues.Length);
            }

            if (dancing)
                return;

            Say(dialogues[random].text);
            dialogues[random].Action();
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

                    Action action = dialogue["action"] != null ? RTCode.ConvertToAction(dialogue["action"]) : null;

                    dialogues[j] = new ExampleDialogue(dialogue["text"], dialogueFunc, action);
                }

                if (!dialogueDictionary.ContainsKey(dialogueGroup["name"]))
                    dialogueDictionary.Add(dialogueGroup["name"], new DialogueGroup(dialogueGroup["name"], dialogues));
            }
        }

        public static bool CanSay() => inst.canSayThing;

        public static bool ApplicationNotFocused() => !Application.isFocused;

        public static bool HasNotLoadedLevel() => EditorManager.inst && !EditorManager.inst.hasLoadedLevel && RTEditor.inst.LevelPanels.Count > 0;

        public static bool HasLoadedLevel() => EditorManager.inst && EditorManager.inst.hasLoadedLevel;

        public static bool LeftHandBeingDragged() => inst.draggingLeftHand;

        public static bool RightHandBeingDragged() => inst.draggingRightHand;

        public static bool BeingDragged() => inst.dragging;

        public static bool UserIsSleepyz() => CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyz" || CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyzgamer";

        public static bool UserIsMecha() => CoreConfig.Instance.DisplayName.Value == "RTMecha";

        public static bool UserIsDiggy() =>
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggy" ||
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggydog" ||
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggydog176";

        public static bool UserIsCubeCube() =>
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "cubecube";

        public static bool UserIsTori() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "karasutori";
        public static bool UserIsPlayer() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "player";

        public static bool TimeLongerThan10Hours() => Time.time > 36000f;

        public static bool SayAnyways() => true;

        public static bool ObjectsAliveCountHigherThan900() => GameData.IsValid && GameData.Current.beatmapObjects.FindAll(x => x.Alive).Count > 900;

        public static bool LevelCountIsZero() => CoreHelper.InEditor && RTEditor.inst.LevelPanels.Count <= 0;

        public static bool UserIsMoNsTeR() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "monster";

        public void RepeatDialogues()
        {
            if (talking)
                return;

            float t = time % repeat;
            if (t <= repeat - 0.1f || said)
            {
                said = false;
                return;
            }

            said = true;

            SayDialogue("OccasionalDialogue");
            repeat = UnityEngine.Random.Range(120f, 600f);
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

        public bool faceCanLook = true;
        public float faceLookMultiplier = 0.006f;

        Vector2 pupilsOffset;
        float pupilsLookRate = 3f;
        bool pupilsCanChange = true;

        public bool allowBlinking = true;

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

        public bool canDrag = true;
        public bool canDragLeftHand = true;
        public bool canDragRightHand = true;

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

            AchievementManager.inst.UnlockAchievement("example_chat");
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

        public float time = 0f;

        float timeOffset;

        public bool talking;

        public bool dying;

        public bool pokingEyes;

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
                animationController = gameObject.AddComponent<AnimationController>();
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

            if (ProjectPlanner.inst && animationController.animations.Where(x => x.name.Contains("DIALOGUE: ") && x.playing).Count() < 1)
                foreach (var schedule in ProjectPlanner.inst.schedules)
                {
                    if (!schedule.hasBeenChecked && schedule.IsActive)
                    {
                        schedule.hasBeenChecked = true;
                        Say($"Reminding you about your schedule \"{schedule.Description}\" at {schedule.DateTime}");
                        ProjectPlanner.inst.SaveSchedules();
                    }
                }

            update?.Invoke();

            if (baseCanvas && canvas)
            {
                UpdateActive();
                canvas.scaleFactor = CoreHelper.ScreenScale;
            }

            if (autocomplete && chatter)
                autocomplete.SetActive(!string.IsNullOrEmpty(chatter.text));

            if (!Visible || dying)
                return;

            if (!spawning && allowBlinking && !dragging && !draggingLeftHand && !draggingRightHand && !pokingEyes && !dancing)
            {
                float t = time % blinkRate;

                if (t > blinkRate - 0.3f && t < blinkRate && blinkCanChange)
                    canBlink = UnityEngine.Random.Range(0, 100) > blinkChance;

                var active = t > blinkRate - 0.3f && t < blinkRate && canBlink;
                blinkCanChange = !active;
                blink?.gameObject.SetActive(active);
            }
            else
                blink?.gameObject?.SetActive(true);

            if (!dancing)
                RepeatDialogues();

            floatingLevel = time * 0.5f % 2f;

            if (floatingParent != null)
                floatingParent.localPosition = new Vector3(0f, (Ease.SineInOut(floatingLevel) - 0.5f) * 2f, 0f);
        }

        void FixedUpdate() => fixedUpdate?.Invoke();

        void LateUpdate()
        {
            lateUpdate?.Invoke();

            if (spawning)
                return;

            if (canDance && !dragging && !draggingLeftHand && !draggingRightHand && !talking && MusicPlaying && !dancing && RandomHelper.PercentChanceSingle(0.2f))
                StartDancing();
            if (!MusicPlaying && dancing)
                StopDancing();

            if (handsBase)
                handsBase.localRotation = Quaternion.Euler(new Vector3(0f, 0f, handsBaseRotation));

            if (browLeft)
                browLeft.localRotation = Quaternion.Euler(new Vector3(0f, 0f, browLeftRotation));

            if (browRight)
                browRight.localRotation = Quaternion.Euler(new Vector3(0f, 0f, browRightRotation));

            // Example breaks if application isn't focused.
            if (!Application.isFocused || !Visible)
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
                }
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

                faceX.localPosition = new Vector3(Mathf.Clamp(-((target.x - dragPos.x) * po), -14f, 14f), 0f);
                faceY.localPosition = new Vector3(0f, Mathf.Clamp(-((target.y - dragPos.y) * po), -14f, 14f));
            }

            if (draggingLeftHand)
                handLeft.localPosition += (target - handLeft.localPosition) * po;

            if (draggingRightHand)
                handRight.localPosition += (target - handRight.localPosition) * po;

            // Set mouth position & ear and tail rotation, forcing them to rotate to the other side of the face.
            if (faceX && faceY && tail && ears && mouthBase && nose)
            {
                float faceXPos = faceX.localPosition.x * 5f;

                tail.localRotation = Quaternion.Euler(0f, 0f, -faceXPos);
                tail.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -faceXPos);
                ears.localRotation = Quaternion.Euler(0f, 0f, faceX.localPosition.x * 0.8f);

                mouthBase.localPosition = new Vector3(faceX.localPosition.x, (faceY.localPosition.y * 0.5f) + -30f, 0f);
                nose.localPosition = new Vector3(faceX.localPosition.x, (faceY.localPosition.y * 0.5f) + -20f, 0f);
            }

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

            if (mouthLower != null)
            {
                var m = mouthLower.localScale;
                m.y = Mathf.Clamp(m.y, 0f, 1f);
                mouthLower.localScale = m;
            }
        }

        public bool Active { get; set; }

        public void SetActive(bool active)
        {
            Active = active;
            UpdateActive();
        }

        public void UpdateActive()
        {
            if (!baseCanvas)
                return;

            if (EditorManager.inst && EditorManager.inst.isEditing)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInEditor.Value);
            else if (GameManager.inst)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInGame.Value);
            else if (Menus.MenuManager.inst && Menus.MenuManager.inst.ic || Menus.InterfaceManager.inst && Menus.InterfaceManager.inst.CurrentInterface || LoadLevelsManager.inst)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInMenus.Value);
            else baseCanvas.SetActive(Active);
        }

        public void SetLastInteracted()
        {
            timeSinceLastInteractedOffset = Time.time;
            StopDancing();
        }

        #region Spawning

        RTAnimation danceAnimLoop;

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

                animationController.animations.Add(waveAnimation);
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
                    }, x => { browLeftRotation = x; }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.2f, -15f, Ease.SineOut),
                    }, x => { browRightRotation = x; }),
                };

                animationController.animations.Add(animation);
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

                animationController.animations.Add(animation);
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

                animationController.animations.Add(animation);
            }

            // Dancing
            {
                var animation = new RTAnimation("Dance Loop")
                {
                    animationHandlers = new List<AnimationHandlerBase>()
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f,   new Vector3(1.05f, 0.95f, 1f), Ease.Linear),
                            new Vector3Keyframe(0.4f, new Vector3(0.95f, 1.05f, 1f), Ease.SineOut),
                            new Vector3Keyframe(0.8f, new Vector3(1.05f, 0.95f, 1f), Ease.SineOut),
                            new Vector3Keyframe(1.2f, new Vector3(0.95f, 1.05f, 1f), Ease.SineOut),
                            new Vector3Keyframe(1.6f, new Vector3(1.05f, 0.95f, 1f), Ease.SineOut),
                        }, x => { parentRotscale.localScale = x; }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f,   0f, Ease.Linear),
                            new FloatKeyframe(0.4f, -2f, Ease.SineOut),
                            new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                            new FloatKeyframe(1.2f, 2f, Ease.SineOut),
                            new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                        }, head.SetLocalPositionX),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f,   0f, Ease.Linear),
                            new FloatKeyframe(0.4f, 1f, Ease.SineOut),
                            new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                            new FloatKeyframe(1.2f, 1f, Ease.SineOut),
                            new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                        }, head.SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, -3f, Ease.Linear),
                            new FloatKeyframe(0.8f, 3f, Ease.SineOut),
                            new FloatKeyframe(1.6f, -3f, Ease.SineOut),
                        }, faceX.SetLocalPositionX),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(0.4f, 6f, Ease.SineOut),
                            new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                            new FloatKeyframe(1.2f, 6f, Ease.SineOut),
                            new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                        }, faceY.SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f,   0f, Ease.Linear),
                            new FloatKeyframe(0.4f, 15f, Ease.SineOut),
                            new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                            new FloatKeyframe(1.2f, -15f, Ease.SineOut),
                            new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                        }, x => { handsBaseRotation = x; }),
                    },
                    loop = true,
                };
                animationController.animations.Add(animation);
                danceAnimLoop = animation;
            }

            yield break;
        }

        void LogFileDoesNotExist(string onError) => Debug.LogError($"{className}File does not exist.\nError: {onError}");

        IEnumerator SpawnExample()
        {
            spawning = true;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/ExampleHooks.cs"))
                yield return StartCoroutine(RTCode.IEvaluate(RTFile.ReadFromFile(RTFile.ApplicationDirectory + "settings/ExampleHooks.cs")));

            #region Canvas

            var uiCanvas = UIManager.GenerateUICanvas("Example Canvas", null, true);

            baseCanvas = uiCanvas.GameObject;
            canvas = uiCanvas.Canvas;
            canvasGroup = uiCanvas.CanvasGroup;

            blocker = Creator.NewUIObject("Interaction Blocker", baseCanvas.transform);
            blocker.transform.AsRT().anchoredPosition = Vector2.zero;
            blocker.transform.AsRT().sizeDelta = new Vector2(10000f, 10000f);
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.3f);
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{HeadPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button != PointerEventData.InputButton.Right)
                        return;

                    optionsActive = !optionsActive;
                    optionsBase.gameObject.SetActive(optionsActive);
                };
                clickable.onDown = pointerEventData =>
                {
                    if (pointerEventData.button != PointerEventData.InputButton.Left || !canDrag)
                        return;

                    StopAnimations(x => x.name == "End Drag Example" || x.name == "Drag Example" || x.name.ToLower().Contains("movement"));
                    SetLastInteracted();

                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;
                    startDragPos = new Vector2(TotalPosition.x, TotalPosition.y);
                    dragPos = new Vector3(TotalPosition.x, TotalPosition.y);
                    dragging = true;

                    faceCanLook = false;

                    //if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.6f, 0.7f));
                    //else AudioManager.inst.PlaySound("Click");

                    if (SoundManager.inst)
                        SoundManager.inst.PlaySound(gameObject, DefaultSounds.example_speak, UnityEngine.Random.Range(0.6f, 0.7f), UnityEngine.Random.Range(1.1f, 1.3f));

                    var animation = new RTAnimation("Drag Example");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, mouthLower.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0.7f, Ease.SineOut),
                        }, mouthLower.SetLocalScaleY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0.5f, Ease.SineOut),
                        }, lips.SetLocalScaleY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 2f, Ease.SineOut),
                        }, lips.SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 10f, Ease.SineOut),
                        }, head.SetLocalPositionY),
						// Hands
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.05f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                        }, handLeft.GetChild(0).SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.05f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                        }, handRight.GetChild(0).SetLocalPositionY),
						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browLeftRotation, Ease.Linear),
                            new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                        }, x => { browLeftRotation = x; }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browRightRotation, Ease.Linear),
                            new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                        }, x => { browRightRotation = x; }),

                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
                            new Vector2Keyframe(0.3f, new Vector2(1.05f, 0.95f), Ease.SineOut),
                        }, x => { parentY.localScale = new Vector3(x.x, x.y, 1f); }),

                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handsBaseRotation, Ease.Linear),
                            new FloatKeyframe(0.3f, 0f, Ease.SineOut),
                            new FloatKeyframe(0.31f, 0f, Ease.Linear),
                        }, x => { handsBaseRotation = x; }),
                    };

                    PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
                };
                clickable.onUp = pointerEventData =>
                {
                    if (!canDrag)
                        return;

                    var animation = new RTAnimation("End Drag Example");

                    float t = 0f;
                    if (parentRotscale.localRotation.eulerAngles.z > 180f)
                        t = 360f;

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, new Vector2(parentY.localScale.x, parentY.localScale.y), Ease.Linear),
                            new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.ElasticOut),
                        }, x => { parentY.localScale = new Vector3(x.x, x.y, 1f); }),
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, new Vector2(parentRotscale.localScale.x, parentRotscale.localScale.y), Ease.Linear),
                            new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.ElasticOut),
                        }, x => { parentRotscale.localScale = new Vector3(x.x, x.y, 1f); }),

                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, parentRotscale.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(1f, t, Ease.BackOut),
                        }, x => { parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x); }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, mouthLower.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0.5f, Ease.SineIn),
                        }, mouthLower.SetLocalScaleY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 1f, Ease.SineIn),
                        }, lips.SetLocalScaleY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.2f, 0f, Ease.SineIn),
                        }, lips.SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, head.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.BounceOut),
                        }, head.SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.1f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                        }, handLeft.GetChild(0).SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.1f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                        }, handRight.GetChild(0).SetLocalPositionY),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.1f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                        }, handLeft.SetLocalRotationEulerZ),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, handRight.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.1f, handRight.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                        }, handRight.SetLocalRotationEulerZ),

						// Brows
						new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browLeftRotation, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, x => { browLeftRotation = x; }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, browRightRotation, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, x => { browRightRotation = x; }),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, faceX.SetLocalPositionX),
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear),
                            new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                        }, faceY.SetLocalPositionY),
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{TailPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    talking = true;
                    Say("Please don't touch me there.", new List<IKeyframe<float>> { new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear) }, new List<IKeyframe<float>> { new FloatKeyframe(0f, parentY.localPosition.y + 200f, Ease.Linear) });
                    Play("Angry", false);
                    AchievementManager.inst.UnlockAchievement("example_touch");
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{EarBottomPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{EarBottomPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{EyesPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData => { pokingEyes = true; };
                clickable.onUp = pointerEventData => { pokingEyes = false; };
            }

            var l_pupils = Creator.NewUIObject("Example Pupils", eyes);
            l_pupils.transform.AsRT().anchoredPosition = Vector2.zero;
            pupils = l_pupils.transform;
            {
                var im = Creator.NewUIObject("image", l_pupils.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().sizeDelta = new Vector2(47f, 22f);

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{PupilsPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData => { pokingEyes = true; };
                clickable.onUp = pointerEventData => { pokingEyes = false; };
            }

            var l_blink = Creator.NewUIObject("Example Blink", eyes);
            l_blink.transform.AsRT().anchoredPosition = Vector2.zero;
            blink = l_blink.transform;
            {
                var im = Creator.NewUIObject("image", l_blink.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = Vector2.zero;
                im.transform.AsRT().sizeDelta = new Vector2(74f, 34f);

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{BlinkPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{SnoutPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{MouthPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{MouthPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{LipsPath}", image.AssignTexture, LogFileDoesNotExist));
            }

            var l_nose = Creator.NewUIObject("Example Nose", snout);
            l_nose.transform.AsRT().anchoredPosition = new Vector2(0f, -20f);
            nose = l_nose.transform;
            {
                var im = Creator.NewUIObject("image", l_nose.transform);

                var image = im.AddComponent<Image>();

                im.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                im.transform.AsRT().sizeDelta = new Vector2(22f, 8f);

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{NosePath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{BrowsPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{BrowsPath}", image.AssignTexture, LogFileDoesNotExist));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{EarTopPath}", image.AssignTexture, LogFileDoesNotExist));

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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{EarTopPath}", image.AssignTexture, LogFileDoesNotExist));

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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{HandsPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData =>
                {
                    if (!canDragLeftHand)
                        return;

                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;
                    startDragPos = new Vector2(handLeft.localPosition.x, handLeft.localPosition.y);
                    draggingLeftHand = true;

                    //if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
                    //else AudioManager.inst.PlaySound("Click");

                    if (SoundManager.inst)
                        SoundManager.inst.PlaySound(gameObject, DefaultSounds.example_speak, UnityEngine.Random.Range(0.08f, 0.12f), UnityEngine.Random.Range(1.1f, 1.3f));

                };
                clickable.onUp = pointerEventData =>
                {
                    if (!canDragLeftHand)
                        return;

                    draggingLeftHand = false;

                    try
                    {
                        SelectObject(image);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    }

                    SetLastInteracted();

                    var animation = new RTAnimation("Example Hand Reset")
                    {
                        animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                            {
                                new Vector2Keyframe(0f, handLeft.AsRT().anchoredPosition, Ease.Linear),
                                new Vector2Keyframe(0.3f, new Vector2(40f, 0f), Ease.SineOut),
                                new Vector2Keyframe(0.32f, new Vector2(40f, 0f), Ease.Linear),
                            }, x => { handLeft.AsRT().anchoredPosition = x; }),
                        },
                    };

                    PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
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

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{HandsPath}", image.AssignTexture, LogFileDoesNotExist));

                var clickable = im.AddComponent<ExampleClickable>();
                clickable.onDown = pointerEventData =>
                {
                    if (!canDragRightHand)
                        return;

                    startMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f) * CoreHelper.ScreenScaleInverse;
                    startDragPos = new Vector2(handRight.localPosition.x, handRight.localPosition.y);
                    draggingRightHand = true;

                    //if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(01.1f, 1.3f), UnityEngine.Random.Range(0.08f, 0.12f));
                    //else AudioManager.inst.PlaySound("Click");

                    if (SoundManager.inst)
                        SoundManager.inst.PlaySound(gameObject, DefaultSounds.example_speak, UnityEngine.Random.Range(0.08f, 0.12f), UnityEngine.Random.Range(1.1f, 1.3f));
                };
                clickable.onUp = pointerEventData =>
                {
                    if (!canDragRightHand)
                        return;

                    draggingRightHand = false;

                    try
                    {
                        SelectObject(image);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    }

                    SetLastInteracted();

                    var animation = new RTAnimation("Example Hand Reset")
                    {
                        animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                            {
                                new Vector2Keyframe(0f, handRight.AsRT().anchoredPosition, Ease.Linear),
                                new Vector2Keyframe(0.3f, new Vector2(-40f, 0f), Ease.SineOut),
                                new Vector2Keyframe(0.32f, new Vector2(-40f, 0f), Ease.Linear),
                            }, x => { handRight.AsRT().anchoredPosition = x; }),
                        },
                    };

                    PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: "));
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
                SetupOptionButton("Tutorials", () => { Say("No tutorials yet!"); });
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
                    command.CheckResponse(command.name);

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
                    autocompleteContent.GetChild(num).gameObject.SetActive(RTString.SearchString(searchTerm, commands[i].name));
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
            EditorThemeManager.ApplyGraphic(chatterImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

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
            chatter.image = chatter.GetComponent<Image>();

            chatterField.AsRT().anchoredPosition = new Vector2(0f, -32);
            chatterField.AsRT().sizeDelta = new Vector2(800f, 64f);

            chatter.textComponent.alignment = TextAnchor.MiddleLeft;
            chatter.textComponent.fontSize = 40;

            chatter.onValueChanged.AddListener(SearchCommandAutocomplete);

            chatter.onEndEdit.AddListener(_val => { HandleChatting(); });

            EditorThemeManager.ApplyInputField(chatter);

            autocomplete = Creator.NewUIObject("Autocomplete", chatterBase);
            UIManager.SetRectTransform(autocomplete.transform.AsRT(), new Vector2(-16f, -64f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(768f, 300f));

            EditorThemeManager.ApplyGraphic(autocomplete.AddComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

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
            Play("Wave", onComplete: () =>  { ResetPositions(1.6f, onComplete: () => { SayDialogue("SpawnText"); }, resetPos: true); });

            Say("Hello, I am Example and this is a test!");

            Debug.Log($"{className}Spawned!");

            yield break;
        }

        #endregion

        #region Play Animations

        public void Play(string anim, bool stopOthers = true, Action onComplete = null)
        {
            if (animationController.animations.Find(x => x.name == anim) == null)
                return;

            if (DebugsOn)
                Debug.LogFormat("{0}Playing Example Animation: {1}", className, anim);

            if (stopOthers)
                animationController.animations.FindAll(x => x.playing).ForEach(anim => { anim.Pause(); });

            var animation = animationController.animations.Find(x => x.name == anim);

            animation.ResetTime();

            if (onComplete != null)
                animation.onComplete = onComplete;

            animation.Play();
        }

        //ExampleManager.inst.Say("Hello, I am Example and this is a test!", new Vector2(0f, 200f))
        public void Say(string dialogue, List<IKeyframe<float>> xPos = null, List<IKeyframe<float>> yPos = null, float textLength = 1.5f, float stayTime = 4f, float time = 0.7f, bool stopOthers = true, Action onComplete = null)
        {
            talking = true;

            dialogue = dialogue.Replace("{{Username}}", CoreConfig.Instance.DisplayName.Value);

            foreach (var obj in Regex.Matches(dialogue, @"{{Config_(.*?)_(.*?)}}"))
            {
                var match = (Match)obj;
                var configName = match.Groups[1].ToString();
                var config = LegacyPlugin.configs.Find(x => x.Name == configName);
                var setting = config.Settings.Find(x => x.Key == match.Groups[2].ToString());

                dialogue = dialogue.Replace(match.Groups[0].ToString(), setting.BoxedValue.ToString());
            }

            if (stopOthers)
                animationController.animations.FindAll(x => x.name.Contains("DIALOGUE: ")).ForEach(anim =>
                 {
                     anim.Pause();
                     animationController.animations.Remove(anim);
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
                        //if (speakSound != null) PlaySound(speakSound, UnityEngine.Random.Range(0.97f, 1.03f), UnityEngine.Random.Range(0.2f, 0.3f));
                        //else AudioManager.inst.PlaySound("Click");

                        if (SoundManager.inst)
                            SoundManager.inst.PlaySound(gameObject, DefaultSounds.example_speak, UnityEngine.Random.Range(0.2f, 0.3f), UnityEngine.Random.Range(0.97f, 1.03f));
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
                animationController.animations.Remove(animation);
                onComplete?.Invoke();

                animation = null;

                talking = false;

                if (dialogueBase)
                    dialogueBase.transform.localScale = new Vector3(0f, 0f, 1f);

                if (DebugsOn)
                    Debug.Log($"{className}Say onComplete");
            };

            animationController.animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        public void Move(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
            if (stopOthers)
                animationController.animations.FindAll(x => x.playing && x.name == "MOVEMENT").ForEach(anim =>
                {
                    anim.Pause();
                    animationController.animations.Remove(anim);
                });

            var animation = new RTAnimation("MOVEMENT");

            x.Insert(0, new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear));
            y.Insert(0, new FloatKeyframe(0f, parentY.localPosition.y, Ease.Linear));

            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(x, x => parentX.localPosition = new Vector3(x, 0f)),
                new AnimationHandler<float>(y, x => parentY.localPosition = new Vector3(0f, x)),
            };

            animation.onComplete = () =>
            {
                animationController.animations.Remove(animation);
                parentX.localPosition = new Vector3(((FloatKeyframe)x[x.Count - 1]).Value, 0f);
                parentY.localPosition = new Vector3(0f, ((FloatKeyframe)y[y.Count - 1]).Value);
                onComplete?.Invoke();
            };

            animationController.Play(animation);
        }

        public void FaceLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
            if (stopOthers)
                animationController.animations.FindAll(x => x.playing && x.name == "FACE MOVEMENT").ForEach(anim =>
                {
                    anim.Pause();
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
                animationController.animations.Remove(animation);
                onComplete?.Invoke();
            };

            animationController.animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        public void PupilsLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
            if (stopOthers)
                animationController.animations.FindAll(x => x.playing && x.name == "PUPILS MOVEMENT").ForEach(anim =>
                {
                    anim.Pause();
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
                animationController.animations.Remove(animation);
                onComplete?.Invoke();
            };

            lookAt = false;
            animationController.animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        void SelectObject(Image image)
        {
            var rect = EditorManager.RectTransformToScreenSpace(image.rectTransform);
            if (CoreHelper.InEditor && rect.Overlaps(EditorManager.RectTransformToScreenSpace(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("mask").AsRT())))
                foreach (var levelItem in RTEditor.inst.LevelPanels)
                {
                    if (levelItem.GameObject.activeInHierarchy && rect.Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                    {
                        Debug.LogFormat("{0}Picked level: {1}", className, levelItem.FolderPath);
                        Say($"What's \"{levelItem.Name}\"?");
                        break; // only select one level
                    }
                }
        }

        public void StartDancing()
        {
            Debug.Log($"{className}Example has started dancing!");
            dancing = true;
            if (danceAnimLoop != null)
                danceAnimLoop.speed = UnityEngine.Random.Range(0.5f, 2f);

            browLeftRotation = -15f;
            browRightRotation = 15f;
            Play("Dance Loop");
            faceCanLook = false;
        }

        public void StopDancing()
        {
            if (!dancing)
                return;

            Debug.Log($"{className}Example has stopped dancing!");
            dancing = false;

            ResetPositions(0.3f, onComplete: () => { faceCanLook = true; });
        }

        public void ResetPositions(float speed, bool stopOthers = true, Action onComplete = null, bool resetPos = false)
        {
            if (stopOthers)
                animationController.animations.FindAll(x => x.playing && !x.name.Contains("DIALOGUE: ")).ForEach(anim => { anim.Pause(); });

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
                    new FloatKeyframe(speed + 0.01f, trs, Ease.Linear),
                }, x => { parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x); }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                    new FloatKeyframe(speed, thl, Ease.SineInOut),
                    new FloatKeyframe(speed + 0.01f, thl, Ease.Linear),
                }, x => { handLeft.localRotation = Quaternion.Euler(0f, 0f, x); }),

                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, parentRotscale.localScale, Ease.Linear),
                    new Vector3Keyframe(speed, Vector3.one, Ease.SineInOut),
                    new Vector3Keyframe(speed + 0.01f, Vector3.one, Ease.Linear),
                }, x => { parentRotscale.localScale = x; }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, handsBaseRotation, Ease.Linear),
                    new FloatKeyframe(speed, 0f, Ease.SineInOut),
                    new FloatKeyframe(speed + 0.01f, 0f, Ease.Linear),
                }, x => { handsBaseRotation = x; }),
            };
            if (resetPos)
            {
                animation.animationHandlers.Add(new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, parentX.localPosition, Ease.Linear),
                    new Vector3Keyframe(speed, new Vector3(700f, 0f), Ease.SineInOut),
                    new Vector3Keyframe(speed + 0.01f, new Vector3(700f, 0f), Ease.Linear),
                }, x => { parentX.localPosition = x; }));
                animation.animationHandlers.Add(new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, parentY.localPosition, Ease.Linear),
                    new Vector3Keyframe(speed, new Vector3(0f, -380f), Ease.SineInOut),
                    new Vector3Keyframe(speed + 0.01f, new Vector3(0f, -380f), Ease.Linear),
                }, x => { parentY.localPosition = x; }));
            }

            animation.onComplete = () =>
            {
                animationController.animations.Remove(animation);
                onComplete?.Invoke();
            };

            animationController.animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        public void PlayOnce(RTAnimation animation, bool stopOthers = true, Predicate<RTAnimation> predicate = null, Action onComplete = null)
        {
            if (stopOthers && predicate != null)
                animationController.animations.FindAll(predicate).ForEach(anim => { anim.Pause(); });

            animation.onComplete += () =>
            {
                animationController.animations.RemoveAll(x => x.id == animation.id);
                onComplete?.Invoke();
            };

            animationController.animations.Add(animation);

            animation.ResetTime();

            animation.Play();
        }

        public void StopAnimations(Predicate<RTAnimation> predicate = null)
        {
            Predicate<RTAnimation> match = x => x.playing;
            if (predicate != null)
                animationController.animations.FindAll(predicate).ForEach(anim =>
                {
                    anim.Pause();
                    animationController.animations.Remove(anim);
                });
            else
                animationController.animations.FindAll(match).ForEach(anim =>
                {
                    anim.Pause();
                    animationController.animations.Remove(anim);
                });
        }

        public void Kill()
        {
            dying = true;

            StopAnimations();
            animationController.animations = null;
            animationController = null;

            if (parentX)
                Destroy(parentX.gameObject);
            if (dialogueBase)
                Destroy(dialogueBase.gameObject);

            Destroy(baseCanvas);
            Destroy(gameObject);
        }

        public void BrowsRaise()
        {
            var animation = new RTAnimation("Brows");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
				// Brows
				new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, browLeftRotation, Ease.Linear),
                    new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                }, x => { browLeftRotation = x; }),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, browRightRotation, Ease.Linear),
                    new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                }, x => { browRightRotation = x; }),
            };

            PlayOnce(animation, true, x => x.playing && !x.name.Contains("DIALOGUE: ") && !x.name.ToLower().Contains("movement"));
        }

        #endregion

        #region Animations

        public List<string> defaultAnimations = new List<string>();

        public AnimationController animationController;

        #endregion
    }
}
