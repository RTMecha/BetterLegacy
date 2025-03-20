using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    public abstract class PAObject<T> : Exists
    {
        public PAObject() { }

        public abstract T Parse(JSONNode jn, ArrhythmiaType type = ArrhythmiaType.LS);
        public abstract JSONNode ToJSON(ArrhythmiaType type = ArrhythmiaType.LS);
    }
}
