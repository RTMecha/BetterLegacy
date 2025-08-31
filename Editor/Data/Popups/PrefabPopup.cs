namespace BetterLegacy.Editor.Data.Popups
{
    /// <summary>
    /// Represents the set of prefab popups.
    /// </summary>
    public class PrefabPopup : EditorPopup
    {
        public PrefabPopup(string name) : base(name) { }

        /// <summary>
        /// The popup for internal prefabs loaded from the game data.
        /// </summary>
        public ContentPopup InternalPrefabs { get; set; }

        /// <summary>
        /// The popup for external prefabs loaded from the users' set prefabs folder.
        /// </summary>
        public ContentPopup ExternalPrefabs { get; set; }

        /// <summary>
        /// Gets the internal / external popup.
        /// </summary>
        /// <param name="prefabDialog">Type of prefab popup to get.</param>
        /// <returns>Returns a prefab popup.</returns>
        public ContentPopup GetPopup(ObjectSource source) => source switch
        {
            ObjectSource.External => ExternalPrefabs,
            ObjectSource.Internal => InternalPrefabs,
            _ => null,
        };
    }
}
