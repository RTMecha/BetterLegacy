using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BetterLegacy.Companion.Data
{
    public class DialogueGroup
    {
        public DialogueGroup()
        {

        }

        public DialogueGroup(string name, ExampleDialogue[] dialogues)
        {
            this.name = name;
            this.dialogues = dialogues;
        }

        public string name;
        public ExampleDialogue[] dialogues;
    }

    public class ExampleDialogue
    {
        public ExampleDialogue(string text, Func<bool> dialogueFunction, Action action = null)
        {
            this.text = text;
            this.dialogueFunction = dialogueFunction;
            this.action = action;
        }

        public bool CanSay => dialogueFunction != null && dialogueFunction.Invoke() && canSay;

        public void Action() => action?.Invoke();

        public string text;
        public Func<bool> dialogueFunction;
        public bool canSay = true;

        Action action;
    }

    public static class Test
    {
        static void Demo()
        {
            // a dialogue can be registered like this:
            dialogues.Add(new Dialogue("Some Dialogue", (companion, parameters) => "Hello!"));
            // "Some Dialogue" is the dialogue's key. This is how the dialogue is found when using SayDialogue.

            // you can then make Example say the dialogue:
            SayDialogue("Some Dialogue");

            // you can also create parameters for the dialogue:
            SayDialogue("Some Dialogue", new DialogueParameters()
            {
                // text interpolation will take 4 seconds
                textLength = 4f,

                // prevents other dialogues from disrupting this dialogue
                allowOverride = false,

                // "Done!" outputs to log when chat bubble animation is done.
                onComplete = () => CompanionManager.Log("Done!"),
            });

            // let's try out custom dialogue parameters
            dialogues.Add(new Dialogue("Custom Dialogue", (companion, parameters) =>
            {
                // return a custom message and check the toggle.
                if (parameters is DemoDialogueParameters demoParameters)
                    return string.Format(demoParameters.message, demoParameters.toggle ? " On!" : " Off!");

                // handle dialogue if parameters is not the custom demo dialogue parameters.
                return "Is not demo dialogue parameters.";
            }));

            // now let's say the dialogue and pass our custom dialogue parameters
            SayDialogue("Custom Dialogue", new DemoDialogueParameters()
            {
                toggle = true,
                message = "Object is {0}",
            });
        }

        public class DemoDialogueParameters : DialogueParameters
        {
            public DemoDialogueParameters() { }

            public bool toggle;
            public string message;
        }

        public static List<Dialogue> dialogues = new List<Dialogue>();

        public static void RegisterDialogues()
        {
            dialogues.Add(new Dialogue("Singular Test", (companion, parameters) => "test"));
            dialogues.Add(new Dialogue("Random Test", (companion, parameters) =>
            {
                var dialogues = new string[]
                {
                    "Test 1",
                    "Test 2",
                    "Test 3",
                    "Test 4",
                };
                return dialogues[UnityEngine.Random.Range(0, dialogues.Length)];
            }));
            dialogues.Add(new Dialogue("Check Dialogue", (companion, parameters) =>
            {
                return EditorManager.inst && EditorManager.inst.hasLoadedLevel ? "Has loaded level!" : "Load a level pls";
            }));
            dialogues.Add(new Dialogue("Parameters Dialogue", 3, (companion, parameters) =>
            {
                return parameters.dialogueOption switch
                {
                    0 => "Option 1",
                    1 => "Option 2",
                    2 => "Option 3",
                    _ => "Null",
                };
            }));
        }

        public static void SayDialogue(string key, DialogueParameters parameters = null)
        {
            var dialogue = dialogues.Find(x => x.key == key);
            if (!dialogue)
                return;

            Say(dialogue, parameters);
        }

        public static void Say(Dialogue dialogue, DialogueParameters parameters = null)
        {
            if (!parameters)
                parameters = new DialogueParameters(UnityEngine.Random.Range(0, dialogue.dialogueCount));

            var text = dialogue?.get?.Invoke(Example.Current, parameters);

            CompanionManager.Log($"text: {text} parameters: {parameters}");
        }
    }

    /// <summary>
    /// Dialogue parameters passed to Example's chat bubble.
    /// </summary>
    public class DialogueParameters : Exists
    {
        public DialogueParameters() { }

        public DialogueParameters(int dialogueOption)
        {
            this.dialogueOption = dialogueOption;
        }

        public DialogueParameters(float textLength, float stayTime, float time, int dialogueOption, Action onComplete = null)
        {
            this.textLength = textLength;
            this.stayTime = stayTime;
            this.time = time;
            this.dialogueOption = dialogueOption;
            this.onComplete = onComplete;
        }

        /// <summary>
        /// Length of the text.
        /// </summary>
        public float textLength = 1.5f;

        /// <summary>
        /// Time the chat bubble should stay for.
        /// </summary>
        public float stayTime = 4f;

        /// <summary>
        /// Speed of the chat bubble animation.
        /// </summary>
        public float time = 0.7f;

        /// <summary>
        /// For dialogues with multiple options. If set to -1, dialogue should default to random values.
        /// </summary>
        public int dialogueOption = -1;

        /// <summary>
        /// Action to run when the dialogue is finished.
        /// </summary>
        public Action onComplete;

        /// <summary>
        /// If the dialogue can be overridden by other dialogues.
        /// </summary>
        public bool allowOverride = true;

        public override string ToString() => $"Dialogue Parameters: [textLength = {textLength}, stayTime = {stayTime}, time = {time}]";
    }

    /// <summary>
    /// Represents a line of text that Example says.
    /// </summary>
    public class Dialogue : Exists
    {
        public Dialogue() { }

        public Dialogue(GetDialogue get)
        {
            this.get = get;
        }

        public Dialogue(string key, GetDialogue get)
        {
            this.key = key;
            this.get = get;
        }
        
        public Dialogue(string key, int dialogueCount, GetDialogue get)
        {
            this.key = key;
            this.dialogueCount = dialogueCount;
            this.get = get;
        }

        /// <summary>
        /// Registered key of the dialogue.
        /// </summary>
        public string key;

        /// <summary>
        /// Gets the dialogue.
        /// </summary>
        public GetDialogue get;

        /// <summary>
        /// How many dialogues the dialogue stores.
        /// </summary>
        public int dialogueCount = 1;

        /// <summary>
        /// Parses a dialogue.
        /// </summary>
        /// <param name="key">Key of the dialogue.</param>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed dialogue.</returns>
        public static Dialogue Parse(string key, JSONNode jn)
        {
            var dialogue = new Dialogue();
            dialogue.key = key;

            if (jn.IsString)
            {
                string text = jn;
                dialogue.get = (companion, parameters) => text;
            }
            else if (jn["text"].IsString)
            {
                string text = jn["text"];
                dialogue.get = (companion, parameters) => text;
            }
            else
            {
                var dialogues = new string[jn["text"].Count];
                string defaultText = jn["default_text"];
                for (int i = 0; i < dialogues.Length; i++)
                    dialogues[i] = jn["text"][i];
                dialogue.get = (companion, parameters) => dialogues.TryGetAt(parameters.dialogueOption, out string text) ? text : defaultText;
                dialogue.dialogueCount = dialogues.Length;
            }

            return dialogue;
        }

        /// <summary>
        /// Delegate for generating a new dialogue.
        /// </summary>
        /// <param name="companion">Passed companion.</param>
        /// <param name="parameters">Passed parameters.</param>
        /// <returns>Returns a new dialogue.</returns>
        public delegate string GetDialogue(Example companion, DialogueParameters parameters);

        public static implicit operator Dialogue(string text) => new Dialogue((companion, parameters) => text);
    }
}
