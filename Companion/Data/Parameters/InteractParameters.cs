using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data.Parameters
{
    /// <summary>
    /// Interact parameters passed to Example.
    /// </summary>
    public class InteractParameters : Exists
    {
        public InteractParameters() { }
    }

    /// <summary>
    /// Chat interaction parameters passed to Example.
    /// </summary>
    public class ChatInteractParameters : InteractParameters
    {
        public ChatInteractParameters() { }

        public ChatInteractParameters(string dialogue, string message)
        {
            this.dialogue = dialogue;
            this.message = message;
        }

        public string dialogue;
        public string message;
    }
}
