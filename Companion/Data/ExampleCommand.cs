using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SimpleJSON;

using BetterLegacy.Core;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a way to communicate with Example.
    /// </summary>
    public class ExampleCommand
    {
        public ExampleCommand() { }

        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response)
        {
            this.name = name;
            this.desc = desc;
            this.autocomplete = autocomplete;
            this.response = response;
        }
        
        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response, List<Phrase> phrases) : this(name, desc, autocomplete, response)
        {
            this.phrases = phrases;
        }
        
        public ExampleCommand(string name, string desc, bool autocomplete, Action<string> response, bool requirePhrase, List<Phrase> phrases) : this(name, desc, autocomplete, response, phrases)
        {
            this.requirePhrase = requirePhrase;
        }

        /// <summary>
        /// Name of the command to display.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the command to display.
        /// </summary>
        public string desc;

        /// <summary>
        /// Function to respond to the input with.
        /// </summary>
        public Action<string> response;

        /// <summary>
        /// If the command should show up in autocomplete.
        /// </summary>
        public bool autocomplete = true;

        /// <summary>
        /// If phrases are required.
        /// </summary>
        public bool requirePhrase;

        /// <summary>
        /// List of things to say to prompt the response.
        /// </summary>
        public List<Phrase> phrases;

        /// <summary>
        /// Checks the input for a response.
        /// </summary>
        /// <param name="input">Input text.</param>
        public void CheckResponse(string input)
        {
            if (!requirePhrase && input == name)
                response?.Invoke(input);
            else if (phrases != null && phrases.TryFind(x => x.CheckPhrase(input), out Phrase phrase))
            {
                if (phrase.isRegex)
                    response?.Invoke(input.Replace(phrase.match.Groups[0].ToString(), phrase.match.Groups[1].ToString()));
                else
                    response?.Invoke(phrase.text);
            }
        }

        /// <summary>
        /// Parses a command.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed command.</returns>
        public static ExampleCommand Parse(JSONNode jn)
        {
            var command = new ExampleCommand();

            if (jn["phrases"] != null)
            {
                command.phrases = new List<Phrase>();
                for (int i = 0; i < jn["phrases"].Count; i++)
                {
                    var phrase = new Phrase(jn["phrases"][i]["text"]);
                    phrase.isRegex = jn["phrases"][i]["regex"].AsBool;
                    command.phrases.Add(phrase);
                }
            }

            command.response = _val =>
            {
                if (jn["response"] != null)
                    RTCode.Evaluate($"var input = \"{_val}\";" + jn["response"]);
            };

            command.name = jn["name"];
            command.desc = jn["desc"];

            if (jn["autocomplete"] != null)
                command.autocomplete = jn["autocomplete"].AsBool;

            command.requirePhrase = jn["require_phrase"].AsBool;

            return command;
        }

        public override string ToString() => name;

        /// <summary>
        /// Represents a phrase prompt.
        /// </summary>
        public class Phrase
        {
            public Phrase(string text) => this.text = text;

            public Phrase(string text, bool isRegex) : this(text) => this.isRegex = isRegex;

            /// <summary>
            /// Text of the phrase.
            /// </summary>
            public string text;

            /// <summary>
            /// Does the phrase contain regex?
            /// </summary>
            public bool isRegex;

            /// <summary>
            /// Match of the phrase.
            /// </summary>
            public Match match;

            /// <summary>
            /// Checks if the phrase matches the input.
            /// </summary>
            /// <param name="input">Input text to check.</param>
            /// <returns>Returns true if the match was successful, otherwise returns false.</returns>
            public bool CheckPhrase(string input)
            {
                if (!isRegex)
                    return text.ToLower() == input.ToLower();

                var regex = new Regex(text);
                match = regex.Match(input);
                return match.Success;
            }
        }
    }
}
