using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's interactions with the game and how the user can interact with him.
    /// </summary>
    public class ExampleInteractions : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default interactions.
        /// </summary>
        public static ExampleInteractions Default
        {
            get
            {
                var interactions = new ExampleInteractions();
                interactions.InitDefault();
                return interactions;
            }
        }

        public override void InitDefault()
        {
            RegisterChecks();
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
                                case ExampleChatBubble.Dialogues.LOVE: {
                                        reference?.brain?.SetAttribute("HAPPINESS", 5.0, MathOperation.Addition);

                                        break;
                                    }
                                case ExampleChatBubble.Dialogues.HATE: {
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

        #region Core

        public override void Build()
        {

        }

        public override void Tick()
        {
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
            attributes.Clear();
        }

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
            checks.Add(new ExampleCheck(Checks.OBJECTS_ALIVE_COUNT_HIGH, () => Updater.levelProcessor && Updater.levelProcessor.engine && Updater.levelProcessor.engine.objectSpawner != null && Updater.levelProcessor.engine.objectSpawner.activateList.Count > 900));
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
