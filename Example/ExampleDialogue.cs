using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Example
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
        public ExampleDialogue(string text, DialogueFunction dialogueFunction, Action action = null)
        {
            this.text = text;
            this.dialogueFunction = dialogueFunction;
            this.action = action;
        }

        public bool CanSay => dialogueFunction != null && dialogueFunction.Invoke() && canSay;

        public void Action() => action?.Invoke();

        public string text;
        public DialogueFunction dialogueFunction;
        public bool canSay = true;

        Action action;
    }
}
