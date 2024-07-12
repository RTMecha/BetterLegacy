using BetterLegacy.Core;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Example
{
    public class ExampleCommand
    {
        public string[] phrases;

        public Action<string> response;

        public bool autocomplete = true;

        public string name;

        public string desc;

        public void CheckResponse(string input)
        {
            if (input == name || phrases != null && phrases.Any(x => x == input.ToLower()))
                response?.Invoke(input);
        }

        public static ExampleCommand Parse(JSONNode jn)
        {
            var command = new ExampleCommand();

            if (jn["phrases"] != null)
            {
                command.phrases = new string[jn["phrases"].Count];
                for (int i = 0; i < jn["phrases"].Count; i++)
                    command.phrases[i] = jn["phrases"][i];
            }

            command.response = delegate (string _val)
            {
                if (jn["response"] != null)
                    RTCode.Evaluate($"var input = \"{_val}\";" + jn["response"]);
            };

            command.name = jn["name"];
            command.desc = jn["desc"];

            if (jn["autocomplete"] != null)
                command.autocomplete = jn["autocomplete"].AsBool;

            return command;
        }

        public override string ToString() => name;
    }
}
