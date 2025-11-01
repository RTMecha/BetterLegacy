using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Managers.Settings
{
    /// <summary>
    /// Represents the settings a manager is initialized with.
    /// </summary>
    public class ManagerSettings : Exists
    {
        public ManagerSettings() { }

        /// <summary>
        /// Parent the manager is added to. By default the parent is <see cref="SystemManager.inst"/>.
        /// </summary>
        public virtual Transform Parent => SystemManager.inst.transform;

        /// <summary>
        /// if the manager is a component rather than an object of its own.
        /// </summary>
        public virtual bool IsComponent => false;

        /// <summary>
        /// Debug name of the manager.
        /// </summary>
        public virtual string ClassName => null;

        public virtual Color Color => RTColors.errorColor;
    }
}
