using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The base of BetterLegacy's editors.
    /// </summary>
    /// <typeparam name="T">Type of the manager.</typeparam>
    /// <typeparam name="TSettings">Initialize settings of the manager.</typeparam>
    /// <typeparam name="TEditor">Type of the vanilla editor manager this editor manager wraps.</typeparam>
    public abstract class BaseEditor<T, TSettings, TEditor> : BaseManager<T, TSettings>
        where T : BaseEditor<T, TSettings, TEditor>
        where TSettings : ManagerSettings, new()
        where TEditor : MonoBehaviour
    {
        /// <summary>
        /// Gets and sets the instance of the vanilla editor manager.<br></br>
        /// Type is <typeparamref name="TEditor"/>.
        /// </summary>
        public abstract TEditor BaseInstance { get; set; }

        public override void OnManagerDestroyed() => BaseInstance = null; // set as null so null checks can work properly
    }
}
