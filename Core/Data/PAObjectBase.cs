using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Base PA object with ID.
    /// </summary>
    public class PAObjectBase : Exists
    {
        public PAObjectBase() => id = LSText.randomString(16);

        /// <summary>
        /// Identification of the object.
        /// </summary>
        public string id;

        /// <summary>
        /// Creates a 16 length string ID.
        /// </summary>
        /// <returns>Returns a string ID of 16 length.</returns>
        public static string GetStringID() => LSText.randomString(16);

        /// <summary>
        /// Creates a 16 length number ID.
        /// </summary>
        /// <returns>Returns a number ID of 16 length.</returns>
        public static string GetNumberID() => LSText.randomNumString(16);

        /// <summary>
        /// Creates a 8 length number ID.
        /// </summary>
        /// <returns>Returns a number ID of 8 length.</returns>
        public static string GetShortNumberID() => LSText.randomNumString(8);

        /// <summary>
        /// Parses and applies object values from VG to formatted JSON.
        /// </summary>
        /// <param name="jn">VG JSON.</param>
        public virtual void ReadJSONVG(JSONNode jn, Version version = default) { }

        /// <summary>
        /// Parses and applies object values from LS to formatted JSON.
        /// </summary>
        /// <param name="jn">LS JSON.</param>
        public virtual void ReadJSON(JSONNode jn) { }

        /// <summary>
        /// Converts the current <typeparamref name="T"/> to the VG format.
        /// </summary>
        /// <returns>Returns a JSONNode.</returns>
        public virtual JSONNode ToJSONVG() => Parser.NewJSONObject();

        /// <summary>
        /// Converts the current <typeparamref name="T"/> to the LS format.
        /// </summary>
        /// <returns>Returns a JSONNode.</returns>
        public virtual JSONNode ToJSON() => Parser.NewJSONObject();

        /// <summary>
        /// Gets the type of object the <paramref name="obj"/> is.
        /// </summary>
        /// <param name="obj">Object reference.</param>
        /// <returns>Returns the <see cref="PAObjectType"/> the object reference is.</returns>
        public static PAObjectType GetObjectType(object obj)
        {
            if (obj is GameData)
                return PAObjectType.GameData;
            if (obj is BeatmapObject)
                return PAObjectType.BeatmapObject;
            if (obj is BackgroundObject)
                return PAObjectType.BackgroundObject;
            if (obj is BackgroundObject)
                return PAObjectType.BackgroundObject;
            if (obj is PrefabObject)
                return PAObjectType.PrefabObject;
            if (obj is Prefab)
                return PAObjectType.Prefab;
            if (obj is BeatmapTheme)
                return PAObjectType.BeatmapTheme;
            if (obj is Modifier)
                return PAObjectType.Modifier;
            if (obj is PlayerModel)
                return PAObjectType.PlayerModel;
            if (obj is PlayerItem)
                return PAObjectType.PlayerItem;

            return PAObjectType.Null;
        }
    }
}
