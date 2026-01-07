using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a default keybind function.
    /// </summary>
    public class KeybindFunction
    {
        #region Constructors

        public KeybindFunction()
        {
            settings = new List<Keybind.Setting>();
        }

        public KeybindFunction(string name, Action<Keybind> action)
        {
            this.name = name;
            this.action = action;
            settings = new List<Keybind.Setting>();
        }
        
        public KeybindFunction(string name, Action<Keybind> action, List<Keybind.Setting> settings)
        {
            this.name = name;
            this.action = action;
            this.settings = settings ?? new List<Keybind.Setting>();
        }

        public KeybindFunction(string name, Action<Keybind> action, params Keybind.Setting[] settings) : this(name, action, settings.ToList()) { }

        #endregion

        #region Values

        /// <summary>
        /// Name of the keybind function.
        /// </summary>
        public string name;

        /// <summary>
        /// Action to run when the keybind is active.
        /// </summary>
        public Action<Keybind> action;

        /// <summary>
        /// The default settings for the keybind.
        /// </summary>
        public List<Keybind.Setting> settings;

        #endregion
    }
}
