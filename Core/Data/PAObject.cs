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

        public string id = LSText.randomString(16);

        public virtual T ReadJSONVG(JSONNode jn) => default;

        public virtual T ReadJSON(JSONNode jn) => default;

        public virtual JSONNode ToJSONVG() => JSON.Parse("{}");

        public virtual JSONNode ToJSON() => JSON.Parse("{}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PAObject<T> a, PAObject<T> b) => a && a.Equals(b);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PAObject<T> a, PAObject<T> b) => !a || !a.Equals(b);

        public override bool Equals(object obj) => obj is PAObject<T> paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();
    }
}
