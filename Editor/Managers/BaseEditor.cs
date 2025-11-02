using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    public abstract class BaseEditor<T, TSettings, TEditor> : BaseManager<T, TSettings>
        where T : BaseEditor<T, TSettings, TEditor>
        where TSettings : ManagerSettings, new()
        where TEditor : MonoBehaviour
    {
        public abstract TEditor BaseInstance { get; set; }

        public override void OnManagerDestroyed() => BaseInstance = null;
    }
}
