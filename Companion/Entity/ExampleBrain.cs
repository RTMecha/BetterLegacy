using System;
using System.Collections.Generic;

using UnityEngine;

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
            AddAttribute("HAPPINESS", 0, -1000.0, 1000.0);

            RegisterActions();
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

            switch (context)
            {
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
                case Notices.SCENE_LOADED: {
                        break;
                    }
                case Notices.EXAMPLE_REFERENCE: {
                        reference?.chatBubble?.Say("Hey, it's me!");
                        break;
                    }
                case Notices.GAME_START: {

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
            }
        }

        /// <summary>
        /// Library of things that Example can notice.<br></br>
        /// This is not an automatic process, you have to run <see cref="Notice(string)"/> and include one of the notices as the context parameter.
        /// </summary>
        public static class Notices
        {
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
            /// Triggers when the game file easter egg is discovered.
            /// </summary>
            public const string GAME_FILE_EASTER_EGG = "Game File Easter Egg";

            /// <summary>
            /// Triggers when a scene is loaded.
            /// </summary>
            public const string SCENE_LOADED = "Scene Loaded";

            /// <summary>
            /// Triggers when a reference to Example is found.
            /// </summary>
            public const string EXAMPLE_REFERENCE = "Example Reference";

            /// <summary>
            /// Triggers when <see cref="GameManager"/> is loaded.
            /// </summary>
            public const string GAME_START = "Game Start";

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

            /// <summary>
            /// Triggers when a player was hit.
            /// </summary>
            public const string PLAYER_HIT = "Player Hit";

            /// <summary>
            /// Triggers when a player died.
            /// </summary>
            public const string PLAYER_DEATH = "Player Death";
        }

        #endregion

        #region Thoughts

        float dialogueRepeatRate;

        bool saidDialogue;

        void RepeatDialogues()
        {
            if (reference.brain.talking)
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

            if (Input.GetKeyDown(KeyCode.F8))
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
        }

        public override void Clear()
        {
            base.Clear();
            CurrentAction = null;
            actions.Clear();
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
                () =>
                {
                    return !CompanionManager.MusicPlaying || !ExampleConfig.Instance.CanDance.Value;
                }, // should Example stop dancing?
                true, // can Example be interrupted from dancing?
                () =>
                {
                    reference?.model?.startDancing?.Invoke();
                    SetAttribute("HAPPINESS", 1.0, MathOperation.Addition);
                }, // start dancing
                () =>
                {
                    reference?.model?.stopDancing?.Invoke();
                    SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);
                } // stop dancing
                ));
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

            // todo: implement these

            public const string SLEEPING = "Sleeping";
        }

        #endregion

        #region Memory

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
        public void SetAttribute(string id, double value, MathOperation operation)
        {
            double num = GetAttribute(id).Value;
            RTMath.Operation(ref num, value, operation);
            GetAttribute(id).Value = num;
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
                CompanionManager.LogError($"Example failed to remember something. Exception: {ex}");
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
            {
                CompanionManager.LogError($"Example failed to remember something.");
            }
        }

        #endregion
    }
}
