using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Base object used for PA Objects.
    /// </summary>
    /// <typeparam name="T">Type of the PA Object.</typeparam>
    public abstract class PAObject<T> : PAObjectBase where T : PAObject<T>, new()
    {
        public PAObject() : base() { }

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
        public T Copy(bool newID = true)
        {
            var obj = new T();
            obj.CopyData(this as T, newID);
            return obj;
        }

        /// <summary>
        /// Parses a <typeparamref name="T"/> from VG formatted JSON.
        /// </summary>
        /// <param name="jn">VG JSON.</param>
        /// <returns>Returns a parsed <typeparamref name="T"/>.</returns>
        public static T ParseVG(JSONNode jn, Version version = default)
        {
            var obj = new T();
            obj.ReadJSONVG(jn, version);
            return obj;
        }

        /// <summary>
        /// Parses a <typeparamref name="T"/> from LS formatted JSON.
        /// </summary>
        /// <param name="jn">LS JSON.</param>
        /// <returns>Returns a parsed <typeparamref name="T"/>.</returns>
        public static T Parse(JSONNode jn)
        {
            var obj = new T();
            obj.ReadJSON(jn);
            return obj;
        }
    }
}
