using System.Runtime.CompilerServices;

using LSFunctions;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Base object used for PA Objects.
    /// </summary>
    /// <typeparam name="T">Type of the PA Object.</typeparam>
    public abstract class PAObject<T> : Exists where T : PAObject<T>, new()
    {
        public PAObject() => id = LSText.randomString(16);

        /// <summary>
        /// Identification of the object.
        /// </summary>
        public string id;

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
    }
}
