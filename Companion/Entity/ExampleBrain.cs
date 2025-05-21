using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's brain. This controls his behavior.
    /// </summary>
    public class ExampleBrain : ExampleModule
    {
        #region Default Instance

        public ExampleBrain() { }

        /// <summary>
        /// The default brain.
        /// </summary>
        public static Func<ExampleBrain> getDefault = () =>
        {
            var brain = new ExampleBrain();
            brain.InitDefault();

            return brain;
        };

        public override void InitDefault()
        {
            AddAttribute("HAPPINESS", 0.0, -1000.0, 1000.0);

            RegisterActions();
            RegisterChecks();
        }

        #endregion

        #region Main Values

        /// <summary>
        /// The current position Example should be looking at.
        /// </summary>
        public Vector2 LookingAt
        {
            get
            {
                if (!Cursor.visible && !InputDataManager.inst.players.IsEmpty())
                {
                    var player = PlayerManager.Players[0];

                    return !player.Player || !player.Player.rb ? Input.mousePosition : Camera.main.WorldToScreenPoint(player.Player.rb.position);
                }

                return Input.mousePosition;
            }
        }

        /// <summary>
        /// If Example can dance.
        /// </summary>
        public bool canDance = true;

        /// <summary>
        /// If Example is talking.
        /// </summary>
        public bool talking;

        /// <summary>
        /// Time since the user last interacted with Example. The more this increases the more likely he is to be bored.
        /// </summary>
        public RTTimer interactedTimer;

        #endregion

        #region Neuron Activation 0_0

        /// <summary>
        /// Example notices something.
        /// </summary>
        /// <param name="context">Context of what was noticed.</param>
        /// <param name="parameters">Parameters to provide more context.</param>
        public virtual void Notice(string context, NoticeParameters parameters = null)
        {
            if (!ExampleConfig.Instance.CanNotice.Value)
                return;

            if (!reference || !reference.model || !reference.model.Visible) // Example can't notice anything if he's not visible!
                return;

            if (reference.tutorials && reference.tutorials.inTutorial)
            {
                switch (context)
                {
                    #region Editor

                    case Notices.LOADED_LEVEL: {
                            Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 5);

                            break;
                        }

                    #endregion
                }

                return;
            }

            switch (context)
            {
                case Notices.SCENE_LOADED: {

                        break;
                    }
                case Notices.ALREADY_SPAWNED: {
                        reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.ALREADY_HERE);

                        break;
                    }

                #region Editor

                case Notices.MORE_BINS: {
                        if (GetAttribute("SEEN_MORE_BINS").Value == 1.0)
                            break;

                        SetAttribute("SEEN_MORE_BINS", 1.0, MathOperation.Set);
                        reference.chatBubble?.Say("Ooh, you found a way to change the bin count! That's awesome!");

                        break;
                    }
                case Notices.LOADED_LEVEL: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.LoadedLevelNoticeChance.Value))
                            reference?.chatBubble?.SayDialogue(RTEditor.inst.fromNewLevel ? ExampleChatBubble.Dialogues.LOADED_NEW_LEVEL : ExampleChatBubble.Dialogues.LOADED_LEVEL);

                        break;
                    }
                case Notices.NEW_OBJECT: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.NewObjectNoticeChance.Value))
                            reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.CREATE_OBJECT);

                        break;
                    }
                case Notices.WARNING_POPUP: {

                        if (!ExampleConfig.Instance.CanGoToWarningPopup.Value)
                            break;

                        ExceptionHelper.NullReference(RTEditor.inst, "Editor");

                        var warningPopup = RTEditor.inst.WarningPopup.GameObject.transform.GetChild(0);

                        if (Vector2.Distance(reference.model.position, warningPopup.localPosition + new Vector3(140f, 200f)) <= 100f)
                            break;

                        var animation = new RTAnimation("MOVEMENT");
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, Example.Current.model.position.x, Ease.Linear),
                                new FloatKeyframe(0.4f, warningPopup.localPosition.x + 120f, Ease.SineOut),
                                new FloatKeyframe(0.6f, warningPopup.localPosition.x + 140f, Ease.SineInOut),
                            }, x => { if (Example.Current && Example.Current.model) Example.Current.model.position.x = x; }, interpolateOnComplete: true),
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, Example.Current.model.position.y, Ease.Linear),
                                new FloatKeyframe(0.5f, warningPopup.localPosition.y + 200f, Ease.SineInOut),
                            }, x => { if (Example.Current && Example.Current.model) Example.Current.model.position.y = x; }, interpolateOnComplete: true),
                        };
                        animation.onComplete = () =>
                        {
                            CompanionManager.inst.animationController.Remove(animation.id);
                        };
                        CompanionManager.inst.animationController.Play(animation);
                        Example.Current.model.SetPose(ExampleModel.Poses.WORRY);

                        break;
                    }
                case Notices.IMPORT_PREFAB: {
                        if (parameters is PrefabNoticeParameters prefabParameters)
                        {
                            var prefab = prefabParameters.prefab;

                            if (prefab && prefab.name.Contains("Example"))
                                reference?.chatBubble?.Say("Hey, it's me!");
                        }

                        break;
                    }
                case Notices.ADD_PREFAB_OBJECT: {

                        break;
                    }
                case Notices.EDITOR_START: {

                        break;
                    }
                case Notices.EDITOR_PREVIEW_TOGGLE: {

                        break;
                    }
                case Notices.EDITOR_SAVED_LEVEL: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.SavedEditorLevelNoticeChance.Value))
                            reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.EDITOR_SAVED_LEVEL);

                        break;
                    }
                case Notices.EDITOR_AUTOSAVED: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.AutosaveNoticeChance.Value))
                            reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.EDITOR_AUTOSAVED);

                        break;
                    }

                #endregion

                #region Game / Player

                case Notices.GAME_START: {

                        break;
                    }
                case Notices.GAME_FILE_EASTER_EGG: {
                        if (GetAttribute("SEEN_GAME_FILE_EASTER_EGG").Value == 1.0)
                            break;

                        SetAttribute("SEEN_GAME_FILE_EASTER_EGG", 1.0, MathOperation.Set);
                        reference?.chatBubble?.Say("Woah, you found a secret!", new DialogueParameters()
                        {
                            onComplete = () =>
                            {
                                reference?.chatBubble?.Say("The level you're playing right now is a level found in the vanilla game files.", new DialogueParameters(2f, 6f, 0.7f));
                            }
                        });

                        break;
                    }

                case Notices.PLAYER_HIT: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.PlayerHitNoticeChance.Value) && parameters is PlayerNoticeParameters playerParameters && playerParameters.player)
                        {
                            SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);
                            reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.PLAYER_HIT, new PlayerDialogueParameters(playerParameters.player));
                            StopCurrentAction();
                        }

                        break;
                    }
                case Notices.PLAYER_DEATH: {
                        if (RandomHelper.PercentChance(ExampleConfig.Instance.PlayerDeathNoticeChance.Value) && parameters is PlayerNoticeParameters playerParameters && playerParameters.player)
                        {
                            SetAttribute("HAPPINESS", 5.0, MathOperation.Subtract);
                            reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.PLAYER_DEATH, new PlayerDialogueParameters(playerParameters.player));
                        }

                        StopCurrentAction();

                        break;
                    }

                    #endregion
            }
        }

        /// <summary>
        /// Library of things that Example can notice.<br></br>
        /// This is not an automatic process, you have to run <see cref="Notice(string)"/> and include one of the notices as the context parameter.
        /// </summary>
        public static class Notices
        {
            /// <summary>
            /// Triggers when a scene is loaded.
            /// </summary>
            public const string SCENE_LOADED = "Scene Loaded";

            /// <summary>
            /// Triggers if Example is already spawned when being summoned.
            /// </summary>
            public const string ALREADY_SPAWNED = "Already Spawned";

            #region Editor

            /// <summary>
            /// Triggers when the bin count is modified.
            /// </summary>
            public const string MORE_BINS = "More Bins";

            /// <summary>
            /// Triggers when a level is loaded.
            /// </summary>
            public const string LOADED_LEVEL = "Loaded Level";

            /// <summary>
            /// Triggers when a new object is created.
            /// </summary>
            public const string NEW_OBJECT = "New Object";

            /// <summary>
            /// Triggers when the Warning Popup opens.
            /// </summary>
            public const string WARNING_POPUP = "Warning Popup";

            /// <summary>
            /// Triggers when a prefab is imported into the level.
            /// </summary>
            public const string IMPORT_PREFAB = "Import Prefab";

            /// <summary>
            /// Triggers when a prefab is added to the level as a prefab object.
            /// </summary>
            public const string ADD_PREFAB_OBJECT = "Add Prefab Object";

            /// <summary>
            /// Triggers when <see cref="EditorManager"/> is loaded.
            /// </summary>
            public const string EDITOR_START = "Editor Start";

            /// <summary>
            /// Triggers when the editor preview is toggled.
            /// </summary>
            public const string EDITOR_PREVIEW_TOGGLE = "Editor Preview Toggle";

            /// <summary>
            /// Triggers when the current editor level is saved.
            /// </summary>
            public const string EDITOR_SAVED_LEVEL = "Editor Saved Level";

            /// <summary>
            /// Triggers when the editor autosave runs.
            /// </summary>
            public const string EDITOR_AUTOSAVED = "Editor Autosaved";

            #endregion

            #region Game / Player

            /// <summary>
            /// Triggers when the game file easter egg is discovered.
            /// </summary>
            public const string GAME_FILE_EASTER_EGG = "Game File Easter Egg";

            /// <summary>
            /// Triggers when <see cref="GameManager"/> is loaded.
            /// </summary>
            public const string GAME_START = "Game Start";

            /// <summary>
            /// Triggers when a player was hit.
            /// </summary>
            public const string PLAYER_HIT = "Player Hit";

            /// <summary>
            /// Triggers when a player died.
            /// </summary>
            public const string PLAYER_DEATH = "Player Death";

            #endregion
        }

        #endregion

        #region Thoughts

        float dialogueRepeatRate;

        bool saidDialogue;

        void RepeatDialogues()
        {
            if (reference.brain.talking || !reference.model.Visible)
                return;

            float t = reference.timer.time % dialogueRepeatRate;
            if (t <= dialogueRepeatRate - 0.1f || saidDialogue)
            {
                saidDialogue = false;
                return;
            }

            saidDialogue = true;

            reference.chatBubble.SayDialogue(ExampleChatBubble.Dialogues.OCCASIONAL);
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
        }

        #endregion

        #region Core

        public override void Build()
        {
            interactedTimer.Reset();
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
            LoadMemory();
        }

        public override void Tick()
        {
            interactedTimer.Update();

            if (reference.leaving) // stop Example from doing anything while he is leaving.
                return;

            if (ExampleConfig.Instance.RandomResponseKeyCode.Value != KeyCode.None && Input.GetKeyDown(ExampleConfig.Instance.RandomResponseKeyCode.Value))
                reference?.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.RANDOM_RESPONSE);

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                if (!action.setAsCurrent)
                {
                    action.Run();
                    continue;
                }

                if (!CurrentAction && action.Run())
                    CurrentAction = action;
                else if (CurrentAction && action.uniqueID == CurrentAction.uniqueID && action.Interrupt())
                    CurrentAction = null;
            }

            if (!CurrentAction)
                RepeatDialogues();

            if (ProjectPlanner.inst && reference && reference.chatBubble && reference.brain && !reference.brain.talking)
                foreach (var schedule in ProjectPlanner.inst.schedules)
                {
                    if (!schedule.hasBeenChecked && schedule.IsActive)
                    {
                        schedule.hasBeenChecked = true;
                        reference.chatBubble.Say($"Reminding you about your schedule \"{schedule.Description}\" at {schedule.DateTime}");
                        ProjectPlanner.inst.SaveSchedules();
                    }
                }
        }

        public override void Clear()
        {
            base.Clear();
            CurrentAction = null;
            actions.Clear();
            checks.Clear();
            onInteract = null;
        }

        #endregion

        #region Actions

        /// <summary>
        /// The current action that is being run.
        /// </summary>
        public ExampleAction CurrentAction { get; set; }

        /// <summary>
        /// List of actions Example can do.
        /// </summary>
        public List<ExampleAction> actions = new List<ExampleAction>();

        /// <summary>
        /// Registers actions.
        /// </summary>
        public virtual void RegisterActions()
        {
            actions.Add(new ExampleAction(Actions.DANCING,
                () =>
                {
                    return reference.brain.canDance && !reference.Dragging && !reference.brain.talking && ExampleConfig.Instance.CanDance.Value &&
                        CompanionManager.MusicPlaying && RandomHelper.PercentChanceSingle(0.02f * ((interactedTimer.time + (float)GetAttribute("HAPPINESS").Value) / 100f));
                                                                                                    // increases desire to dance the longer you leave him alone and the happier he is
                }, // does Example want to dance?
                () => !CompanionManager.MusicPlaying || !ExampleConfig.Instance.CanDance.Value, // should Example stop dancing?
                true, // can Example be interrupted from dancing?
                () =>
                {
                    reference?.model?.startDancing?.Invoke();
                    SetAttribute("HAPPINESS", 4.0, MathOperation.Addition);
                }, // start dancing
                () =>
                {
                    reference?.model?.stopDancing?.Invoke();
                    SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);
                } // stop dancing
                ));
            actions.Add(new ExampleAction(Actions.IDLE,
                () =>
                {
                    var happinessAttribute = GetAttribute("HAPPINESS");
                    if (happinessAttribute.Value > 0.0)
                    {
                        happinessAttribute.Value -= 1.0;
                        SaveMemory();
                    }
                })
            {
                canDo = () => !reference.Dragging && !reference.brain.talking &&
                    interactedTimer.time > 600f && !CompanionManager.MusicPlaying && RandomHelper.PercentChanceSingle(0.01f) && RandomHelper.PercentChanceSingle(50f),
                interruptCheck = () => false,
                interruptible = false,
                setAsCurrent = false,
            });
            actions.Add(new ExampleAction(Actions.BORED_OUT,
                () => !reference.Dragging && !reference.brain.talking && ExampleConfig.Instance.CanLeave.Value &&
                    interactedTimer.time > 600f && !CurrentAction && GetAttribute("HAPPINESS").Value <= 1.0f && RandomHelper.PercentChanceSingle(0.001f) && RandomHelper.PercentChanceSingle(0.01f),
                () => false,
                false,
                () => reference.Exit()));
        }

        /// <summary>
        /// Overrides an existing action.
        /// </summary>
        /// <param name="key">Key of the action to override.</param>
        /// <param name="action">Action to override.</param>
        public void OverrideAction(string key, ExampleAction action)
        {
            if (actions.TryFindIndex(x => x.key == key, out int index))
                actions[index] = action;
        }

        /// <summary>
        /// Gets an action.
        /// </summary>
        /// <param name="key">Key of the action.</param>
        /// <returns>Returns the found action.</returns>
        public ExampleAction GetAction(string key) => actions.Find(x => x.key == key);

        /// <summary>
        /// Stops the current action.
        /// </summary>
        public void StopCurrentAction()
        {
            CurrentAction?.Stop();
            CurrentAction = null;
        }

        /// <summary>
        /// Library of default actions.
        /// </summary>
        public static class Actions
        {
            /// <summary>
            /// Example is dancing!
            /// </summary>
            public const string DANCING = "Dancing";

            /// <summary>
            /// Example is fully bored and has decided to take his leave.
            /// </summary>
            public const string BORED_OUT = "Bored Out";

            /// <summary>
            /// Example is slowly getting bored.
            /// </summary>
            public const string IDLE = "Idle";

            // todo: implement these

            public const string SLEEPING = "Sleeping";
        }

        #endregion

        #region Interactions

        /// <summary>
        /// Runs when Example is interacted with.
        /// </summary>
        public Action<string> onInteract;

        /// <summary>
        /// The default interaction context.
        /// </summary>
        public void Interact() => Interact(string.Empty);

        /// <summary>
        /// Engages Example's brain, making him react.
        /// </summary>
        /// <param name="context">Context of the interaction.</param>
        public virtual void Interact(string context, InteractParameters parameters = null)
        {
            reference?.brain?.interactedTimer.Reset();
            onInteract?.Invoke(context);

            switch (context)
            {
                case Interactions.PET: {
                        reference?.brain?.SetAttribute("HAPPINESS", 1.0, MathOperation.Addition);

                        break;
                    }
                case Interactions.CHAT: {
                        reference?.brain?.SetAttribute("HAPPINESS", 1.0, MathOperation.Addition);

                        if (parameters is ChatInteractParameters chatParameters)
                        {
                            reference?.chatBubble?.SayDialogue(chatParameters.dialogue);

                            switch (chatParameters.dialogue)
                            {
                                case ExampleChatBubble.Dialogues.LOVE:
                                    {
                                        reference?.brain?.SetAttribute("HAPPINESS", 5.0, MathOperation.Addition);

                                        break;
                                    }
                                case ExampleChatBubble.Dialogues.HATE:
                                    {
                                        reference?.brain?.SetAttribute("HAPPINESS", 5.0, MathOperation.Subtract);

                                        break;
                                    }
                            }
                        }

                        break;
                    }
                case Interactions.HOLD_HAND: {
                        SoundManager.inst.PlaySound(reference.model.baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.08f, 0.12f), UnityEngine.Random.Range(1.1f, 1.3f));
                        reference?.model?.SetPose(ExampleModel.Poses.WORRY);
                        break;
                    }
                case Interactions.TOUCHIE: {
                        reference?.brain?.SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);

                        reference?.chatBubble?.Say("Please don't touch me there.");
                        reference?.model?.SetPose(ExampleModel.Poses.ANGRY);

                        AchievementManager.inst.UnlockAchievement("example_touch");

                        break;
                    }
                case Interactions.INTERRUPT: {
                        reference?.brain?.SetAttribute("HAPPINESS", 2.0, MathOperation.Subtract);

                        reference?.chatBubble?.Say("Hey!");
                        reference?.model?.SetPose(ExampleModel.Poses.ANGRY);

                        break;
                    }
                case Interactions.SELECT_OBJECTS_COMMAND: {
                        reference?.chatBubble?.Say("Selected all objects!");

                        break;
                    }
            }
        }

        /// <summary>
        /// Library of default interactions.
        /// </summary>
        public static class Interactions
        {
            /// <summary>
            /// When Example's head is clicked.
            /// </summary>
            public const string PET = "Pet";

            /// <summary>
            /// When you chat with Example.
            /// </summary>
            public const string CHAT = "Chat";

            /// <summary>
            /// When you hold one of Example's hands.
            /// </summary>
            public const string HOLD_HAND = "Hold Hand";

            /// <summary>
            /// When you touch Example's tail. Why would you do that.
            /// </summary>
            public const string TOUCHIE = "Touchie";

            /// <summary>
            /// When you interupt Example while he's dancing. Bruh.
            /// </summary>
            public const string INTERRUPT = "Interrupt";

            /// <summary>
            /// When you run the select all objects command.
            /// </summary>
            public const string SELECT_OBJECTS_COMMAND = "Select Objects Command";
        }

        // TODO:
        // you can respond to Example's question about what a level is, which will add to his memory.
        /// <summary>
        /// Selects an object based on the image position.
        /// </summary>
        /// <param name="image">Image to check for objects under.</param>
        public void SelectObject(Image image)
        {
            var rect = EditorManager.RectTransformToScreenSpace(image.rectTransform);
            if (CoreHelper.InEditor && rect.Overlaps(EditorManager.RectTransformToScreenSpace(RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("mask").AsRT())))
                foreach (var levelItem in RTEditor.inst.LevelPanels)
                {
                    if (levelItem.GameObject.activeInHierarchy && rect.Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                    {
                        CompanionManager.Log($"Picked level: {levelItem.FolderPath}");
                        reference?.chatBubble?.Say($"What's \"{levelItem.Name}\"?");
                        break; // only select one level
                    }
                }
        }

        #endregion

        #region Memory

        /// <summary>
        /// Example's memory.
        /// </summary>
        public ExampleRegistry memory = new ExampleRegistry();

        /// <summary>
        /// Parses memories from JSON and remembers it.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void Read(JSONNode jn)
        {
            for (int i = 0; i < jn["attributes"].Count; i++)
            {
                var jnAttribute = jn["attributes"][i];
                var value = jnAttribute["val"].AsDouble;
                GetAttribute(jnAttribute["id"], value, jnAttribute["min"].AsDouble, jnAttribute["max"].AsDouble).Value = value;
            }

            memory.Clear();
            for (int i = 0; i < jn["memory"].Count; i++)
            {
                var jnMemory = jn["memory"][i];
                var type = jnMemory["type"].AsInt;
                var key = jnMemory["key"].Value;

                if (string.IsNullOrEmpty(key))
                    continue;

                switch (type)
                {
                    case 0: {
                            memory.RegisterItem(key, new ExampleRegistryItem<string>(obj: jnMemory["obj"]));

                            break;
                        } // string
                    case 1: {
                            memory.RegisterItem(key, new ExampleRegistryItem<JSONNode>(obj: jnMemory["obj"]));

                            break;
                        } // JSON
                }
            }
        }

        /// <summary>
        /// Converts Example's memories to JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing Example's memories.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            for (int i = 0; i < attributes.Count; i++)
                jn["attributes"][i] = attributes[i].ToJSON();

            try
            {
                memory.ForLoop((registryItem, index) =>
                {
                    if (string.IsNullOrEmpty(registryItem.key))
                        return;

                    jn["memory"][index]["key"] = registryItem.key;
                    if (registryItem is ExampleRegistryItem<string> registryItemStr && !string.IsNullOrEmpty(registryItemStr.obj))
                        jn["memory"][index]["obj"] = registryItemStr.obj;
                    if (registryItem is ExampleRegistryItem<JSONNode> registryItemJSON && registryItemJSON.obj != null)
                        jn["memory"][index]["obj"] = registryItemJSON.obj;
                });
            }
            catch (Exception ex)
            {
                LogAmnesia(ex);
            }

            return jn;
        }

        /// <summary>
        /// Path to Example's memory file.
        /// </summary>
        public string Path => RTFile.ApplicationDirectory + "profile/example_memory.json";

        /// <summary>
        /// Updates an attribute and saves the memory.
        /// </summary>
        /// <param name="id">ID of the attribute.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="operation">Operation to use.</param>
        public override void SetAttribute(string id, double value, MathOperation operation)
        {
            base.SetAttribute(id, value, operation);
            SaveMemory();
        }

        /// <summary>
        /// Stores Example's memories.
        /// </summary>
        public void SaveMemory()
        {
            try
            {
                RTFile.WriteToFile(Path, ToJSON().ToString());
            }
            catch (Exception ex)
            {
                LogAmnesia(ex);
            }
        }

        /// <summary>
        /// Remembers Example's memories.
        /// </summary>
        public void LoadMemory()
        {
            if (RTFile.TryReadFromFile(Path, out string file))
            {
                Read(JSON.Parse(file));
                CompanionManager.Log($"Loaded memory");
            }
            else
                LogAmnesia();
        }

        /// <summary>
        /// Logs Example having amnesia.
        /// </summary>
        /// <param name="ex">Exception that caused Example to have amnesia.</param>
        public static void LogAmnesia(Exception ex = null)
            => CompanionManager.LogError($"Example failed to remember something. Seems like he's suffering amnesia." + (ex == null ? string.Empty : $"\nException: {ex}"));

        #endregion

        #region Checks

        /// <summary>
        /// List of checks.
        /// </summary>
        public List<ExampleCheck> checks = new List<ExampleCheck>();

        /// <summary>
        /// Registers checks.
        /// </summary>
        public virtual void RegisterChecks()
        {
            checks.Add(new ExampleCheck(Checks.APPLICATION_FOCUSED, () => Application.isFocused));
            checks.Add(new ExampleCheck(Checks.APRIL_FOOLS, () => Seasons.IsAprilFools));
            checks.Add(new ExampleCheck(Checks.NO_ASSETS, () => !RTFile.DirectoryExists(RTFile.GetAsset("Example Companion"))));
            checks.Add(new ExampleCheck(Checks.HAS_NOT_LOADED_LEVEL, () => CoreHelper.InEditor && !EditorManager.inst.hasLoadedLevel && !RTEditor.inst.LevelPanels.IsEmpty()));
            checks.Add(new ExampleCheck(Checks.HAS_LOADED_LEVEL, () => CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel));
            checks.Add(new ExampleCheck(Checks.BEING_DRAGGED, () => reference && reference.dragging));
            checks.Add(new ExampleCheck(Checks.USER_IS_SLEEPYZ, () => CoreHelper.Equals(CoreConfig.Instance.DisplayName.Value.ToLower(), "sleepyz", "sleepyzgamer")));
            checks.Add(new ExampleCheck(Checks.USER_IS_RTMECHA, () => CoreConfig.Instance.DisplayName.Value == "RTMecha"));
            checks.Add(new ExampleCheck(Checks.USER_IS_DIGGY, () => CoreHelper.Equals(CoreConfig.Instance.DisplayName.Value.ToLower(), "diggy", "diggydog", "diggydog176")));
            checks.Add(new ExampleCheck(Checks.USER_IS_CUBECUBE, () => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "cubecube"));
            checks.Add(new ExampleCheck(Checks.USER_IS_TORI, () => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "karasutori"));
            checks.Add(new ExampleCheck(Checks.USER_IS_MONSTER, () => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "monster"));
            checks.Add(new ExampleCheck(Checks.USER_IS_APPY, () => CoreHelper.Equals(CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower(), "appy", "appysketch", "applebutter")));
            checks.Add(new ExampleCheck(Checks.USER_IS_DEFAULT, () => CoreConfig.Instance.DisplayName.Value == CoreConfig.Instance.DisplayName.Default));
            checks.Add(new ExampleCheck(Checks.TIME_LONGER_THAN_10_HOURS, () => Time.time > 36000f));
            checks.Add(new ExampleCheck(Checks.OBJECTS_ALIVE_COUNT_HIGH, () => RTLevel.Current && RTLevel.Current.objectEngine && RTLevel.Current.objectEngine.spawner && RTLevel.Current.objectEngine.spawner.activateList.Count > 900));
            checks.Add(new ExampleCheck(Checks.NO_EDITOR_LEVELS, () => CoreHelper.InEditor && RTEditor.inst.LevelPanels.IsEmpty()));
            checks.Add(new ExampleCheck(Checks.IS_HAPPY, () => reference && reference.brain && reference.brain.GetAttribute("HAPPINESS").Value > 100.0));
            checks.Add(new ExampleCheck(Checks.IS_SAD, () => reference && reference.brain && reference.brain.GetAttribute("HAPPINESS").Value < 100.0));
        }

        /// <summary>
        /// Overrides an existing check.
        /// </summary>
        /// <param name="key">Key of the check to override.</param>
        /// <param name="check">Check to override.</param>
        public void OverrideCheck(string key, ExampleCheck check)
        {
            if (checks.TryFindIndex(x => x.key == key, out int index))
                checks[index].check = check.check;
        }

        /// <summary>
        /// Gets a check and checks if its active.
        /// </summary>
        /// <param name="key">Key of the check.</param>
        /// <returns>Returns true if the check is found and active, otherwise returns false.</returns>
        public bool Check(string key) => GetCheck(key).Check();

        /// <summary>
        /// Gets a check.
        /// </summary>
        /// <param name="key">Key of the check.</param>
        /// <returns>If a check is found, return the check, otherwise return the default check.</returns>
        public ExampleCheck GetCheck(string key) => checks.Find(x => x.key == key) ?? ExampleCheck.Default;

        /// <summary>
        /// Library of default checks.
        /// </summary>
        public static class Checks
        {
            public const string APPLICATION_FOCUSED = "Application Focused";

            public const string APRIL_FOOLS = "April Fools";

            public const string NO_ASSETS = "No Assets";

            public const string HAS_NOT_LOADED_LEVEL = "Has Not Loaded Level";

            public const string HAS_LOADED_LEVEL = "Has Loaded Level";

            public const string BEING_DRAGGED = "Being Dragged";

            public const string USER_IS_SLEEPYZ = "User Is Sleepyz";

            public const string USER_IS_RTMECHA = "User Is RTMecha";

            public const string USER_IS_DIGGY = "User Is Diggy";

            public const string USER_IS_CUBECUBE = "User Is CubeCube";

            public const string USER_IS_TORI = "User Is Tori";

            public const string USER_IS_MONSTER = "User Is Monster";

            public const string USER_IS_APPY = "User Is Appy";

            public const string USER_IS_DEFAULT = "User Is Default";

            public const string TIME_LONGER_THAN_10_HOURS = "Time Longer Than 10 Hours";

            public const string OBJECTS_ALIVE_COUNT_HIGH = "Objects Alive Count High";

            public const string NO_EDITOR_LEVELS = "No Editor Levels";

            public const string IS_HAPPY = "Is Happy";

            public const string IS_SAD = "Is Sad";
        }

        #endregion
    }
}
