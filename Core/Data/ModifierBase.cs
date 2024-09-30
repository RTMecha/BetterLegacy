using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    public class ModifierBase
    {
        public ModifierBase() { }

        public bool verified = false;

        public bool constant = true;

        public bool prefabInstanceOnly = false;

        public enum Type { Trigger, Action }

        public Type type = Type.Action;
        public string value;
        public bool running = false;
        public bool active = false;
        public List<string> commands = new List<string> { "" };
        public string Name => commands != null && commands.Count > 0 ? commands[0] : "Null";

        public bool not = false;

        public object Result { get; set; }

        public bool hasChanged;

        public override string ToString() => Name;
    }
}
