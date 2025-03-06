﻿using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// An instance of Example.
    /// </summary>
    public class Example : Exists
    {
        public Example(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleCommands commands, ExampleInteractions interactions)
        {
            internalID = LSText.randomNumString(8);

            this.brain = brain;
            this.model = model;
            this.chatBubble = chatBubble;
            this.options = options;
            this.commands = commands;
            this.interactions = interactions;

            this.brain.SetReference(this);
            this.model.SetReference(this);
            this.chatBubble.SetReference(this);
            this.options.SetReference(this);
            this.commands.SetReference(this);
            this.interactions.SetReference(this);
        }

        // TODO:
        /*
            - Rework dialogue system (maybe pass a "DialogueParameters" class? this'll act as the context for what should be said)
            - Rework commands system (include a random "idea" command link)
            - Add more poses and expressions
            - Add more parts
            - Expand on interaction module (more editor interactions)
            - Add more things for Example to remember
            - More random occurances that can be enabled / disabled
            - Ensure Example has a lot of settings that can control his behavior if people want him to be a specific way.
            - Figure out custom models / companions. Probably have it all in one package?
            - Random enter & leave sequences
            - Implement tutorials module. This utilizes the model's screen blocker and has its own UI, similar to what was concepted.
         */

        #region Modules

        /// <summary>
        /// Example's brain.
        /// </summary>
        public ExampleBrain brain;
        /// <summary>
        /// Example's body.
        /// </summary>
        public ExampleModel model;
        /// <summary>
        /// Example's speech.
        /// </summary>
        public ExampleChatBubble chatBubble;
        /// <summary>
        /// Example's options.
        /// </summary>
        public ExampleOptions options;
        /// <summary>
        /// Example's chat window.
        /// </summary>
        public ExampleCommands commands;
        /// <summary>
        /// How Example interacts with the rest of the mod.
        /// </summary>
        public ExampleInteractions interactions;

        #endregion

        #region Values

        /// <summary>
        /// The folder containing all of Example's assets and files.
        /// </summary>
        public const string EXAMPLE_FOLDER = "Example Parts";
        //public const string EXAMPLE_FOLDER = "Example Companion";

        string internalID;

        /// <summary>
        /// Example's timer.
        /// </summary>
        public RTTimer timer;

        #region Dragging

        /// <summary>
        /// If Example can be dragged around.
        /// </summary>
        public bool canDrag = true;

        /// <summary>
        /// If Example's left hand can be dragged around.
        /// </summary>
        public bool canDragLeftHand = true;

        /// <summary>
        /// If Example's right hand can be dragged around.
        /// </summary>
        public bool canDragRightHand = true;

        /// <summary>
        /// If Example is being dragged around at all.
        /// </summary>
        public bool Dragging => dragging || draggingLeftHand || draggingRightHand;

        /// <summary>
        /// If Example is being dragged around.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// If Example's left hand is being dragged around.
        /// </summary>
        public bool draggingLeftHand;

        /// <summary>
        /// If Example's right hand is being dragged around.
        /// </summary>
        public bool draggingRightHand;

        /// <summary>
        /// Base drag delay.
        /// </summary>
        public float dragDelay = 0.7f;

        /// <summary>
        /// Calculated drag delay.
        /// </summary>
        public float DragDelay { get; set; }

        /// <summary>
        /// Start drag position reference.
        /// </summary>
        public Vector2 startDragPos;

        /// <summary>
        /// Start mouse position reference.
        /// </summary>
        public Vector2 startMousePos;

        /// <summary>
        /// Where Example is being dragged to.
        /// </summary>
        public Vector2 dragPos;

        /// <summary>
        /// Calculated drag offset target.
        /// </summary>
        public Vector2 DragTarget
        {
            get
            {
                var vector = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;

                float x = startMousePos.x - vector.x;
                float y = startMousePos.y - vector.y;

                return new Vector2(startDragPos.x + -x, startDragPos.y + -y);
            }
        }

        #endregion

        #endregion

        #region Init / Core

        /// <summary>
        /// The current Example.
        /// </summary>
        public static Example Current { get; set; }

        /// <summary>
        /// Custom initialization function.
        /// </summary>
        public static Action onInit;

        /// <summary>
        /// Initializes Example.
        /// </summary>
        public static void Init()
        {
            if (onInit != null)
            {
                onInit();
                return;
            }

            Init(ExampleBrain.Default, ExampleModel.Default, ExampleChatBubble.Default, ExampleOptions.Default, ExampleCommands.Default, ExampleInteractions.Default);
        }

        /// <summary>
        /// Initializes Example with a set brain and model.
        /// </summary>
        /// <param name="brain">Brain to set.</param>
        /// <param name="model">Model to set.</param>
        public static void Init(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleCommands discussion, ExampleInteractions interactions)
        {
            Current?.Kill();
            Current = new Example(brain, model, chatBubble, options, discussion, interactions);
            Current.Build();
        }

        /// <summary>
        /// Builds Example.
        /// </summary>
        public virtual void Build()
        {
            try
            {
                if (RTFile.TryReadFromFile(RTFile.ApplicationDirectory + "settings/ExampleHooks.cs", out string exampleHooksFile))
                    RTCode.Evaluate(exampleHooksFile);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Custom code parsing failed. Exception: {ex}");
            }

            timer.UpdateTimeOffset();
            brain.Build();
            model.Build();
            chatBubble.Build();
            options.Build();
            commands.Build();
            interactions.Build();

            Enter();
        }

        /// <summary>
        /// Runs Example per-tick.
        /// </summary>
        public virtual void Tick()
        {
            timer.Update();
            DragDelay = 1f - Mathf.Pow(dragDelay, Time.deltaTime * 60f);

            brain?.Tick();
            model?.Tick();
            chatBubble?.Tick();
            options?.Tick();
            commands?.Tick();
            interactions?.Tick();

            tickCount++;
        }

        /// <summary>
        /// Tick count since start.
        /// </summary>
        public ulong tickCount;

        /// <summary>
        /// Kills Example.
        /// </summary>
        public virtual void Kill()
        {
            if (Current && Current.internalID == internalID)
                Current = null;

            brain?.Clear();
            brain = null;
            model?.Clear();
            model = null;
            chatBubble?.Clear();
            chatBubble = null;
            options?.Clear();
            options = null;
            commands?.Clear();
            commands = null;
            interactions?.Clear();
            interactions = null;
        }

        #endregion

        #region Enter / Exit

        public bool leaving;

        // TODO: random entrance and exit animations?
        /// <summary>
        /// Enters the scene.
        /// </summary>
        public virtual void Enter()
        {
            if (CoreHelper.InEditor)
                chatBubble?.SayDialogue("SpawnText");
            else
                chatBubble?.SayDialogue("Greeting");

            var arm = model.GetPart("HAND_LEFT");

            var animation = new RTAnimation("Hiii");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.5f, 800f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 750f, Ease.SineInOut),
                }, x => model.position.x = x, interpolateOnComplete: true),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -1200f, Ease.Linear),
                    new FloatKeyframe(0.3f, -380f, Ease.SineOut),
                    new FloatKeyframe(0.6f, -410f, Ease.SineInOut),
                }, x => model.position.y = x, interpolateOnComplete: true),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.5f, 150f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 130f, Ease.SineInOut),
                    new FloatKeyframe(1.2f, 150f, Ease.SineInOut),
                    new FloatKeyframe(1.7f, 130f, Ease.SineInOut),
                    new FloatKeyframe(2f, 0f, Ease.SineInOut),
                }, x => arm.rotation = x, interpolateOnComplete: true),
            };
            animation.onComplete = () =>
            {
                CompanionManager.inst.animationController.Remove(animation.id);
            };
            CompanionManager.inst.animationController.Play(animation);
        }

        /// <summary>
        /// Leaves the scene.
        /// </summary>
        public virtual void Exit()
        {
            leaving = true;

            chatBubble?.Say("Okay, I'll get out of your way.");

            model.GetAttribute("FACE_CAN_LOOK").Value = 0.0;

            model.SetPose(ExampleModel.Poses.LEAVE, onCompleteAnim: animation =>
            {
                CompanionManager.inst.animationController.animations.Clear();
                Kill();
            });
        }

        #endregion
    }
}
