using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a tutorial where Example teaches the user about the many things in BetterLegacy.
    /// </summary>
    public class ExampleTutorial : Exists
    {
        public ExampleTutorial() { }

        public ExampleTutorial(string key, string name, Action[] actions)
        {
            this.key = key;
            this.name = name;
            this.actions = actions;
        }

        /// <summary>
        /// Key of the tutorial.
        /// </summary>
        public string key;

        /// <summary>
        /// Displayable name of the tutorial.
        /// </summary>
        public string name;

        /// <summary>
        /// List of actions to progress through.
        /// </summary>
        public Action[] actions;

        public Action this[int index]
        {
            get => actions[index];
            set => actions[index] = value;
        }

        public override string ToString() => key;
    }
}
