using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's brain. This controls his behavior.
    /// </summary>
    public class ExampleBrain : ExampleModule
    {
        public ExampleBrain() { }

        #region Default Instance

        /// <summary>
        /// The default brain.
        /// </summary>
        public static ExampleBrain Default
        {
            get
            {
                var brain = new ExampleBrain();
                brain.InitDefault();

                return brain;
            }
        }

        public override void InitDefault()
        {
            AddAttribute("HAPPINESS", 0, -100, 100);
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
                if (!Cursor.visible && InputDataManager.inst.players.Count > 0)
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
        /// If Example is currently dancing.
        /// </summary>
        public bool dancing;

        /// <summary>
        /// If Example is talking.
        /// </summary>
        public bool talking;

        float timeSinceLastInteractedOffset = 0f;

        /// <summary>
        /// Time since the user last interacted with Example. The more this increases the more likely he is to be bored.
        /// </summary>
        public float timeSinceLastInteracted = 0f;

        #endregion

        #region Neuron Activation 0_0

        public Action<string> onInteract;

        /// <summary>
        /// The default interaction context.
        /// </summary>
        public void Interact() => Interact(string.Empty);

        /// <summary>
        /// Engages Example's brain, making him react.
        /// </summary>
        /// <param name="context">Context of the interaction.</param>
        public virtual void Interact(string context)
        {
            timeSinceLastInteractedOffset = Time.time;
            onInteract?.Invoke(context);

            switch (context)
            {
                case ExampleInteractions.PET: {
                        SetAttribute("HAPPINESS", 1.0, MathOperation.Addition);

                        break;
                    }
                case ExampleInteractions.CHAT: {
                        SetAttribute("HAPPINESS", 1.0, MathOperation.Addition);

                        break;
                    }
                case ExampleInteractions.HOLD_HAND: {
                        SoundManager.inst.PlaySound(reference.model.baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.08f, 0.12f), UnityEngine.Random.Range(1.1f, 1.3f));
                        reference?.model?.SetPose(ExampleModel.Poses.WORRY);
                        break;
                    }
                case ExampleInteractions.TOUCHIE: {
                        SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);

                        reference?.chatBubble?.Say("Please don't touch me there.");
                        reference?.model?.SetPose(ExampleModel.Poses.ANGRY);

                        AchievementManager.inst.UnlockAchievement("example_touch");

                        break;
                    }
                case ExampleInteractions.INTERRUPT: {
                        SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);

                        reference?.chatBubble?.Say("Hey!");
                        reference?.model?.SetPose(ExampleModel.Poses.ANGRY);

                        break;
                    }
            }
        }

        /// <summary>
        /// Example notices something.
        /// </summary>
        /// <param name="context">Context of what was noticed.</param>
        public virtual void Notice(string context)
        {
            switch (context)
            {
                case NOTICE_MORE_BINS: {
                        if (GetAttribute("SEEN_MORE_BINS").Value == 1.0)
                            break;

                        SetAttribute("SEEN_MORE_BINS", 1.0, MathOperation.Set);
                        reference.chatBubble?.Say("Ooh, you found a way to change the bin count! That's awesome!");

                        break;
                    }
            }
        }

        public const string NOTICE_MORE_BINS = "More Bins";

        #endregion

        #region Thoughts

        float dialogueRepeatRate;

        public void RepeatDialogues()
        {
            if (reference.brain.talking)
                return;

            float t = reference.timer.time % dialogueRepeatRate;
            if (t <= dialogueRepeatRate - 0.1f || reference.chatBubble.saidDialogue)
            {
                reference.chatBubble.saidDialogue = false;
                return;
            }

            reference.chatBubble.saidDialogue = true;

            reference.chatBubble.SayDialogue("OccasionalDialogue");
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
        }

        #endregion

        #region Core

        public override void Build()
        {
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
            LoadMemory();
        }

        public override void Tick()
        {
            timeSinceLastInteracted = Time.time - timeSinceLastInteractedOffset;

            if (reference.leaving) // stop Example from doing anything while he is leaving.
                return;

            // does Example want to dance to the music?
            if (reference.brain.canDance && !reference.Dragging && !reference.brain.talking && ExampleConfig.Instance.CanDance.Value &&
                CompanionManager.MusicPlaying && !reference.brain.dancing && RandomHelper.PercentChanceSingle(0.02f * ((timeSinceLastInteracted + (float)GetAttribute("HAPPINESS").Value) / 100f)))
                                                                                                                    // increases desire to dance the longer you leave him alone
                StartDancing();

            // should he stop dancing?
            else if ((!CompanionManager.MusicPlaying || !ExampleConfig.Instance.CanDance.Value) && reference.brain.dancing)
                StopDancing();

            if (!dancing)
                RepeatDialogues();
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Example starts dancing.
        /// </summary>
        public void StartDancing()
        {
            dancing = true;
            reference?.model?.startDancing?.Invoke();
        }

        /// <summary>
        /// Example stops dancing.
        /// </summary>
        public void StopDancing()
        {
            dancing = false;
            reference?.model?.stopDancing?.Invoke();
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
