using System.Runtime.CompilerServices;

using LSFunctions;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Base object used for PA Objects.
    /// </summary>
    /// <typeparam name="T">Type of the PA Object.</typeparam>
    public abstract class PAObject<T> : Exists
    {
        public PAObject() { }

        /// <summary>
        /// Identification of the object.
        /// </summary>
        public string id = LSText.randomString(16);

        /// <summary>
        /// Copies data from another <typeparamref name="T"/>.
        /// </summary>
        /// <param name="orig">Original object to copy data from.</param>
        public abstract void CopyData(T orig, bool newID = true);

        /// <summary>
        /// Creates a copy of the <typeparamref name="T"/>.
        /// </summary>
        /// <param name="newID">If the ID of the <typeparamref name="T"/> should be copied.</param>
        /// <returns>Returns a copy of the object.</returns>
        public abstract T Copy(bool newID = true);

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
        public virtual JSONNode ToJSONVG() => JSON.Parse("{}");

        /// <summary>
        /// Converts the current <typeparamref name="T"/> to the LS format.
        /// </summary>
        /// <returns>Returns a JSONNode.</returns>
        public virtual JSONNode ToJSON() => JSON.Parse("{}");
    }
}
