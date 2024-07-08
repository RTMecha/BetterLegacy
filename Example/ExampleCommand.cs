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

        public Action response;

        public void CheckResponse(string input)
        {
            if (phrases.Any(x => x == input.ToLower()))
                response?.Invoke();
        }

        public static ExampleCommand Parse(JSONNode jn)
        {
            var command = new ExampleCommand();

            command.phrases = new string[jn["phrases"].Count];
            for (int i = 0; i < jn["phrases"].Count; i++)
                command.phrases[i] = jn["phrases"][i];

            command.response = delegate ()
            {
                if (jn["response"] != null)
                    RTCode.Evaluate(jn["response"]);
            };

            return command;
        }
    }
}
