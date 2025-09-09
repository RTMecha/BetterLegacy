using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    /// <summary>
    /// Represents a double set of content popups.
    /// </summary>
    public class DoubleContentPopup : EditorPopup
    {
        public DoubleContentPopup(string name) : base(name) { }

        public DoubleContentPopup(string name, string internalTitle, string externalTitle) : base(name)
        {
            this.internalTitle = internalTitle;
            this.externalTitle = externalTitle;
        }

        /// <summary>
        /// The popup for internal items.
        /// </summary>
        public ContentPopup Internal { get; set; }

        /// <summary>
        /// The popup for external items loaded from a folder.
        /// </summary>
        public ContentPopup External { get; set; }

        public string internalTitle;
        public string externalTitle;

        /// <summary>
        /// Gets the internal / external popup.
        /// </summary>
        /// <param name="prefabDialog">Type of prefab popup to get.</param>
        /// <returns>Returns a prefab popup.</returns>
        public ContentPopup GetPopup(ObjectSource source) => source switch
        {
            ObjectSource.Internal => Internal,
            ObjectSource.External => External,
            _ => null,
        };

        public override void Init()
        {
            var gameObject = EditorPrefabHolder.Instance.DoubleContentPopup.Duplicate(RTEditor.inst.popups, Name);
            Assign(gameObject);
            if (!string.IsNullOrEmpty(internalTitle))
                Internal.title = internalTitle;
            if (!string.IsNullOrEmpty(externalTitle))
                External.title = externalTitle;
            Render();
        }

        public override void Assign(UnityEngine.GameObject popup)
        {
            GameObject = popup;
            Internal = new ContentPopup("internal");
            Internal.Assign(popup.transform.Find("internal").gameObject);
            var internalSelectGUI = Internal.GameObject.GetOrAddComponent<SelectGUI>();
            internalSelectGUI.ogPos = Internal.GameObject.transform.position;
            internalSelectGUI.target = Internal.GameObject.transform;

            External = new ContentPopup("external");
            External.Assign(popup.transform.Find("external").gameObject);
            var externalSelectGUI = External.GameObject.GetOrAddComponent<SelectGUI>();
            externalSelectGUI.ogPos = External.GameObject.transform.position;
            externalSelectGUI.target = External.GameObject.transform;
        }

        public override void Render()
        {
            Internal?.Render();
            External?.Render();
        }
    }
}
