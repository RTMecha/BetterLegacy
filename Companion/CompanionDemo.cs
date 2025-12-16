using System.Collections.Generic;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;

namespace BetterLegacy.Companion
{
    /// <summary>
    /// Demonstrates Example's capabilities.
    /// </summary>
    public static class CompanionDemo
    {
        static void DemoDialogue()
        {
            // a dialogue can be registered like this:
            dialogues.Add(new ExampleDialogue("Some Dialogue", (companion, parameters) => "Hello!"));
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
            dialogues.Add(new ExampleDialogue("Custom Dialogue", (companion, parameters) =>
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

        public static List<ExampleDialogue> dialogues = new List<ExampleDialogue>();

        public static void RegisterDialogues()
        {
            dialogues.Add(new ExampleDialogue("Singular Test", (companion, parameters) => "test"));
            dialogues.Add(new ExampleDialogue("Random Test", (companion, parameters) =>
            {
                var dialogues = new string[]
                {
                    "Test 1",
                    "Test 2",
                    "Test 3",
                    "Test 4",
                };
                return dialogues[UnityRandom.Range(0, dialogues.Length)];
            }));
            dialogues.Add(new ExampleDialogue("Check Dialogue", (companion, parameters) =>
            {
                return EditorManager.inst && EditorManager.inst.hasLoadedLevel ? "Has loaded level!" : "Load a level pls";
            }));
            dialogues.Add(new ExampleDialogue("Parameters Dialogue", 3, (companion, parameters) =>
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

        public static void Say(ExampleDialogue dialogue, DialogueParameters parameters = null)
        {
            if (!parameters)
                parameters = new DialogueParameters(UnityRandom.Range(0, dialogue.dialogueCount));

            var text = dialogue?.get?.Invoke(Example.Current, parameters);
            Example.Current?.chatBubble?.Say(text, parameters);
        }
    }
}
