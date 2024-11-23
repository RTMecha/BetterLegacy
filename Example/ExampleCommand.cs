using BetterLegacy.Core;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BetterLegacy.Example
{
    public class ExampleCommand
    {
        public List<Phrase> phrases;

        public bool requirePhrase;
        public Action<string> response;

        public bool autocomplete = true;

        public string name;

        public string desc;

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

        public class Phrase
        {
            public Phrase(string text) => this.text = text;
            public string text;
            public bool isRegex;
            public Match match;

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
