using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a line of text that Example says.
    /// </summary>
    public class ExampleDialogue : Exists
    {
        public ExampleDialogue() { }

        public ExampleDialogue(GetDialogue get)
        {
            this.get = get;
        }

        public ExampleDialogue(GetDialogue get, Func<bool> canSay) : this(get)
        {
            this.canSay = canSay;
        }

        public ExampleDialogue(int dialogueCount, GetDialogue get) : this(get)
        {
            this.dialogueCount = dialogueCount;
        }

        public ExampleDialogue(int dialogueCount, GetDialogue get, Func<bool> canSay) : this(dialogueCount, get)
        {
            this.canSay = canSay;
        }

        public ExampleDialogue(string key, GetDialogue get)
        {
            this.key = key;
            this.get = get;
        }

        public ExampleDialogue(string key, GetDialogue get, Func<bool> canSay) : this(key, get)
        {
            this.canSay = canSay;
        }
        
        public ExampleDialogue(string key, int dialogueCount, GetDialogue get)
        {
            this.key = key;
            this.dialogueCount = dialogueCount;
            this.get = get;
        }

        public ExampleDialogue(string key, int dialogueCount, GetDialogue get, Func<bool> canSay) : this(key, dialogueCount, get)
        {
            this.canSay = canSay;
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
        /// If Example can say this line of dialogue.
        /// </summary>
        public Func<bool> canSay;

        /// <summary>
        /// How many dialogues the dialogue stores.
        /// </summary>
        public int dialogueCount = 1;

        /// <summary>
        /// Checks if Example can say this line of dialogue.
        /// </summary>
        /// <returns>Returns true if Example can say this line of dialogue.</returns>
        public bool CanSay() => canSay?.Invoke() ?? true;

        /// <summary>
        /// Parses a dialogue.
        /// </summary>
        /// <param name="key">Key of the dialogue.</param>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed dialogue.</returns>
        public static ExampleDialogue Parse(string key, JSONNode jn)
        {
            var dialogue = new ExampleDialogue();
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

        public static implicit operator ExampleDialogue(string text) => new ExampleDialogue((companion, parameters) => text);
    }
}
