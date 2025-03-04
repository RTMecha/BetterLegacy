using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
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

            if (Current == this)
                Current = null;
        }
    }
}
