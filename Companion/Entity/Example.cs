using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

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

        public Example(ExampleBrain brain, ExampleModel model, ExampleChatBubble chatBubble, ExampleOptions options, ExampleCommands commands, ExampleTutorials tutorials, List<ExampleModule> modules = null) : this()
        {
            this.brain = brain;
            this.model = model;
            this.chatBubble = chatBubble;
            this.options = options;
            this.commands = commands;
            this.tutorials = tutorials;
            this.modules = modules;

            this.brain.SetReference(this);
            this.model.SetReference(this);
            this.chatBubble.SetReference(this);
            this.options.SetReference(this);
            this.commands.SetReference(this);
            this.tutorials.SetReference(this);
            this.modules?.ForLoop(module => module?.SetReference(this));
        }

        // TODO:
        /*
            - Add more poses and expressions
            - Expand on interaction module (more editor interactions)
            - Add more things for Example to remember
            - More random occurances that can be enabled / disabled (sleeping due to boredom, etc)
            - Ensure Example has a lot of settings that can control his behavior if people want him to be a specific way. I don't want him to come off as annoying.
            - Figure out custom models / companions. Probably have it all in one package?
            - Multiple companions?
            - Implement tutorials module. This won't be another "documentation". Instead, it'll be a proper hands-on tutorial that guides you along a specific process.
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

        public ExampleTutorials tutorials;

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
            if (!module)
            {
                CoreHelper.LogError($"Module: [key: {key}, module: {module}] was null, so cannot be registered.");
                return;
            }

            module.key = key;
            module.SetReference(this);

            if (modules == null)
                modules = new List<ExampleModule>();

            modules.Add(module);
        }

        #endregion

        #region Values

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
        public static Func<Example> getDefault = () => new Example(ExampleBrain.getDefault(), ExampleModel.getDefault(), ExampleChatBubble.getDefault(), ExampleOptions.getDefault(), ExampleCommands.getDefault(), ExampleTutorials.getDefault(), getDefaultModules?.Select(x => x?.Invoke())?.ToList() ?? null);

        public static List<Func<ExampleModule>> getDefaultModules;

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
            LogStartup("Building the tutorials...");
            tutorials.Build();
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
            tutorials?.Tick();

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
            CompanionManager.inst.animationController.animations.Clear();

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
            tutorials?.Clear();
            tutorials = null;

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

        #region JSON Functions

        public virtual void RegisterFunctions()
        {
            functions = new Functions();
        }

        public virtual Dictionary<string, JSONNode> GetVariables()
        {
            var dictionary = new Dictionary<string, JSONNode>();

            return dictionary;
        }

        public JSONFunctionParser<Example> functions;

        public class Functions : JSONFunctionParser<Example>
        {
            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, Example thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, Example thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, Example thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }

        #endregion

        #region Enter / Exit

        public bool leaving;

        /// <summary>
        /// Enters the scene.
        /// </summary>
        public virtual void Enter()
        {
            if (ProjectArrhythmia.State.InEditor)
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

            model?.SetAttribute("FACE_CAN_LOOK", 0.0, MathOperation.Set);

            model?.SetPose(ExampleModel.Poses.LEAVE, onCompleteAnim: animation => Kill());

            if (!model)
                Kill();
        }

        #endregion
    }
}
