using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Companion.Data;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// An instance of Example.
    /// </summary>
    public class Example : Exists
    {
        public Example() => internalID = LSText.randomNumString(8);

        public Example(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleCommands commands, ExampleInteractions interactions, List<ExampleModule> modules = null) : this()
        {
            this.brain = brain;
            this.model = model;
            this.chatBubble = chatBubble;
            this.options = options;
            this.commands = commands;
            this.interactions = interactions;
            this.modules = modules;

            this.brain.SetReference(this);
            this.model.SetReference(this);
            this.chatBubble.SetReference(this);
            this.options.SetReference(this);
            this.commands.SetReference(this);
            this.interactions.SetReference(this);
            this.modules?.ForLoop(module => module?.SetReference(this));
        }

        // TODO:
        /*
            - Add more poses and expressions
            - Add more parts
            - Expand on interaction module (more editor interactions)
            - Add more things for Example to remember
            - More random occurances that can be enabled / disabled (sleeping due to boredom, etc)
            - Ensure Example has a lot of settings that can control his behavior if people want him to be a specific way. I don't want him to come off as annoying.
            - Figure out custom models / companions. Probably have it all in one package?
            - Random enter & leave sequences
            - Implement tutorials module. This utilizes the model's screen blocker and has its own UI, similar to what was concepted by MoNsTeR.
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

        /// <summary>
        /// List of custom modules.
        /// </summary>
        public List<ExampleModule> modules;

        /// <summary>
        /// Gets a custom module.
        /// </summary>
        /// <typeparam name="T">Type of the module that implements <see cref="ExampleModule"/>.</typeparam>
        /// <param name="key">Key of the module to find.</param>
        /// <returns>Returns a found module.</returns>
        public T GetModule<T>(string key) where T : ExampleModule => modules?.Find(x => x.key == key) as T;

        /// <summary>
        /// Registers a module to Example.
        /// </summary>
        /// <param name="key">Key of the module to register.</param>
        /// <param name="module">Module to register.</param>
        public void RegisterModule(string key, ExampleModule module)
        {
            module.key = key;
            module.SetReference(this);
            modules.Add(module);
        }

        #endregion

        #region Values

        /// <summary>
        /// The folder containing all of Example's assets and files.
        /// </summary>
        public const string EXAMPLE_FOLDER = "Example Companion";

        /// <summary>
        /// Gets a file in Example's folder.
        /// </summary>
        /// <param name="file">File to get.</param>
        /// <returns>Returns the full path to the file.</returns>
        public static string GetFile(string file) => RTFile.GetAsset(RTFile.CombinePaths(EXAMPLE_FOLDER, file));

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
        public static Action<Example> onInit;

        /// <summary>
        /// Gets the default Example.
        /// </summary>
        public static Func<Example> getDefault = () => new Example(ExampleBrain.getDefault(), ExampleModel.getDefault(), ExampleChatBubble.getDefault(), ExampleOptions.getDefault(), ExampleCommands.getDefault(), ExampleInteractions.getDefault());

        /// <summary>
        /// Initializes Example.
        /// </summary>
        public static void Init()
        {
            Current?.Kill();
            Current = getDefault();
            Current.Build();
            onInit?.Invoke(Current);
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

            LogStartup("Beginning timer...");
            var sw = CoreHelper.StartNewStopwatch();
            timer.Reset();
            LogStartup("Building the brain...");
            brain.Build();
            LogStartup("Building the model...");
            model.Build();
            LogStartup("Building the chat bubble...");
            chatBubble.Build();
            LogStartup("Building the options...");
            options.Build();
            LogStartup("Building the commands...");
            commands.Build();
            LogStartup("Building the interactions...");
            interactions.Build();
            if (modules != null)
            {
                LogStartup("Building the custom modules...");
                modules.ForLoop(module => module?.Build());
            }

            LogStartup($"Done! Took {sw.Elapsed} to build. Now Example should enter the scene");
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

            modules?.ForLoop(module => module?.Tick());

            tickCount++;
        }

        ulong tickCount;
        /// <summary>
        /// Tick count since start.
        /// </summary>
        public ulong TickCount => tickCount;

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

            modules?.ForLoop(module => module?.Clear());
            modules?.Clear();
            modules = null;
        }

        void LogStartup(string message)
        {
            if (ExampleConfig.Instance.LogStartup.Value)
                CompanionManager.Log($"STARTUP -> {message}");
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
                chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.SPAWN);
            else
                chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.GREETING);
            model?.SetPose(ExampleModel.Poses.ENTER);
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
