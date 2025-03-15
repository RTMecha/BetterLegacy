using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data
{
    public abstract class PAObject<T> : Exists
    {
        public PAObject() { }

        public abstract T Parse(JSONNode jn, ArrhythmiaType type = ArrhythmiaType.LS);
        public abstract JSONNode ToJSON(ArrhythmiaType type = ArrhythmiaType.LS);
    }
}
