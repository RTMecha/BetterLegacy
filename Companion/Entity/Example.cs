using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// An instance of Example.
    /// </summary>
    public class Example : Exists
    {
        public Example(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleDiscussion discussion, ExampleInteractions interactions)
        {
            internalID = LSText.randomNumString(8);

            this.brain = brain;
            this.model = model;
            this.chatBubble = chatBubble;
            this.options = options;
            this.discussion = discussion;
            this.interactions = interactions;

            this.brain.SetReference(this);
            this.model.SetReference(this);
            this.chatBubble.SetReference(this);
            this.options.SetReference(this);
            this.discussion.SetReference(this);
            this.interactions.SetReference(this);
        }

        string internalID;

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
        public ExampleDiscussion discussion;
        /// <summary>
        /// How Example interacts with the rest of the mod.
        /// </summary>
        public ExampleInteractions interactions;

        #endregion

        #region Values

        public float time;
        float timeOffset;

        #region Dragging

        public bool canDrag = true;
        public bool canDragLeftHand = true;
        public bool canDragRightHand = true;

        public bool Dragging => dragging || draggingLeftHand || draggingRightHand;

        public bool dragging;
        public bool draggingLeftHand;
        public bool draggingRightHand;

        public float dragDelay = 0.3f;

        public float DragDelay { get; set; }

        public Vector2 startDragPos;
        public Vector2 startMousePos;
        public Vector2 dragPos;

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

        public Vector2 pupilsOffset;

        #endregion

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

            Init(ExampleBrain.Default, ExampleModel.Default, ExampleChatBubble.Default, ExampleOptions.Default, ExampleDiscussion.Default, ExampleInteractions.Default);
        }

        /// <summary>
        /// Initializes Example with a set brain and model.
        /// </summary>
        /// <param name="brain">Brain to set.</param>
        /// <param name="model">Model to set.</param>
        public static void Init(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleDiscussion discussion, ExampleInteractions interactions)
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

            timeOffset = Time.time;
            brain.Build();
            model.Build();
            chatBubble.Build();
            options.Build();
            discussion.Build();
            interactions.Build();

            Enter();
        }

        /// <summary>
        /// Runs Example per-tick.
        /// </summary>
        public virtual void Tick()
        {
            time = Time.time - timeOffset;
            DragDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(dragDelay, 0.001f, 1f), Time.deltaTime * 60f);

            brain?.Tick();
            model?.Tick();
            chatBubble?.Tick();
            options?.Tick();
            discussion?.Tick();
            interactions?.Tick();

            tickCount++;
        }

        /// <summary>
        /// Tick count since start.
        /// </summary>
        public ulong tickCount;

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
            chatBubble?.Say("Okay, I'll get out of your way.");

            model.GetAttribute("FACE_CAN_LOOK").Value = 0.0;

            var animation = new RTAnimation("Cya");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                // Base
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, model.position.x, Ease.Linear),
                    new FloatKeyframe(0.5f, model.position.x, Ease.Linear),
                    new FloatKeyframe(1.5f, model.position.x + -400f, Ease.SineOut),
                }, x => { if (model) model.position.x = x; }, interpolateOnComplete: true),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, model.position.y, Ease.Linear),
                    new FloatKeyframe(0.5f, model.position.y - 10f, Ease.Linear),
                    new FloatKeyframe(0.8f, model.position.y + 80f, Ease.SineOut),
                    new FloatKeyframe(1.5f, model.position.y + -1200f, Ease.CircIn),
                }, x => { if (model) model.position.y = x; }, interpolateOnComplete: true),
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, model.scale, Ease.Linear),
                    new Vector2Keyframe(0.5f, new Vector2(0.99f, 1.01f), Ease.Linear),
                    new Vector2Keyframe(0.7f, new Vector2(1.15f, 0.95f), Ease.SineOut),
                    new Vector2Keyframe(0.9f, new Vector2(1f, 1f), Ease.SineInOut),
                    new Vector2Keyframe(1.5f, new Vector2(0.7f, 1.3f), Ease.SineIn),
                }, vector2 => { if (model) model.scale = vector2; }, interpolateOnComplete: true),

                // Face
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, model.facePosition, Ease.Linear),
                    new Vector2Keyframe(0.7f, new Vector2(0f, 2f), Ease.SineInOut),
                    new Vector2Keyframe(1.3f, new Vector2(0f, -3f), Ease.SineInOut),
                }, vector2 => { if (model) model.facePosition = vector2; }, interpolateOnComplete: true),
            };
            animation.onComplete = () =>
            {
                CompanionManager.inst.animationController.animations.Clear();
                Kill();
            };
            CompanionManager.inst.animationController.Play(animation);
        }

        /// <summary>
        /// Kills Example.
        /// </summary>
        public void Kill()
        {
            brain?.Clear();
            brain = null;
            model?.Clear();
            model = null;
            chatBubble?.Clear();
            chatBubble = null;
            options?.Clear();
            options = null;
            discussion?.Clear();
            discussion = null;
            interactions?.Clear();
            interactions = null;

            if (Current && Current.internalID == internalID)
                Current = null;
        }
    }
}
