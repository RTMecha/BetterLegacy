using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's brain. This controls his behavior.
    /// </summary>
    public class ExampleBrain : ExampleModule
    {
        public ExampleBrain() { }

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
            AddAttribute("HAPPINESS", 0, 0, 0);
        }

        #region Main Values

        public bool canDance = true;
        public bool dancing;
        public bool talking;

        float timeSinceLastInteractedOffset = 0f;
        public float timeSinceLastInteracted = 0f;

        #endregion

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
                case ExampleInteractions.HOLD_HAND: {
                        SoundManager.inst.PlaySound(reference.model.baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.08f, 0.12f), UnityEngine.Random.Range(1.1f, 1.3f));
                        break;
                    }
                case ExampleInteractions.TOUCHIE: {
                        SetAttribute("HAPPINESS", 1.0, MathOperation.Subtract);

                        reference.chatBubble.Say("Please don't touch me there.");

                        var browLeft = reference.model.GetPart("BROW_LEFT");
                        var browRight = reference.model.GetPart("BROW_RIGHT");

                        var animation = new RTAnimation("Angry");
                        animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, browLeft.rotation, Ease.Linear),
                                new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                            }, x => browLeft.rotation = x, interpolateOnComplete: true),
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, browRight.rotation, Ease.Linear),
                                new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                            }, x => browRight.rotation = x, interpolateOnComplete: true),
                        };
                        animation.onComplete = () =>
                        {
                            CompanionManager.inst.animationController.Remove(animation.id);
                        };
                        CompanionManager.inst.animationController.Play(animation);

                        AchievementManager.inst.UnlockAchievement("example_touch");

                        break;
                    }
            }
        }

        float dialogueRepeatRate;

        public void RepeatDialogues()
        {
            if (reference.brain.talking)
                return;

            float t = reference.time % dialogueRepeatRate;
            if (t <= dialogueRepeatRate - 0.1f || reference.chatBubble.saidDialogue)
            {
                reference.chatBubble.saidDialogue = false;
                return;
            }

            reference.chatBubble.saidDialogue = true;

            reference.chatBubble.SayDialogue("OccasionalDialogue");
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
        }

        public override void Build()
        {
            dialogueRepeatRate = UnityEngine.Random.Range(120f, 600f);
            LoadMemory();
        }

        public override void Tick()
        {
            timeSinceLastInteracted = Time.time - timeSinceLastInteractedOffset;

            // does Example want to dance to the music?
            if (reference.brain.canDance && !reference.Dragging && !reference.brain.talking &&
                CompanionManager.MusicPlaying && !reference.brain.dancing && RandomHelper.PercentChanceSingle(0.02f * (timeSinceLastInteracted / 100f)))
                                                                                                                    // increases desire to dance the longer you leave him alone
            {
                dancing = true;
                reference?.model?.startDancing?.Invoke();
            }

            // should he stop dancing?
            else if (!CompanionManager.MusicPlaying && reference.brain.dancing)
            {
                dancing = false;
                reference?.model?.startDancing?.Invoke();
            }

            if (!dancing)
                RepeatDialogues();
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        public void Read(JSONNode jn)
        {
            for (int i = 0; i < jn["attributes"].Count; i++)
            {
                var jnAttribute = jn["attributes"][i];
                var value = jnAttribute["value"].AsDouble;

                // add the attribute in case it wasn't initially there
                var attribute = AddAttribute(jnAttribute["id"], value, jnAttribute["min"].AsDouble, jnAttribute["max"].AsDouble);
                // read the value
                attribute.Value = value;
            }
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            for (int i = 0; i < attributes.Count; i++)
                jn["attributes"][i] = attributes[i].ToJSON();

            return jn;
        }

        public string Path => RTFile.ApplicationDirectory + "profile/example_memory.json";
        public void SetAttribute(string id, double value, MathOperation operation)
        {
            double num = GetAttribute(id).Value;
            RTMath.Operation(ref num, value, operation);
            GetAttribute(id).Value = num;
            SaveMemory();
        }

        public void SaveMemory() => RTFile.WriteToFile(Path, ToJSON().ToString());
        public void LoadMemory()
        {
            if (RTFile.TryReadFromFile(Path, out string file))
                Read(JSON.Parse(file));
        }
    }
}
