using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data
{
    public class ModifierBlock<T> : PAObject<ModifierBlock<T>>, IModifyable
    {
        public ModifierBlock() { }

        public ModifierReferenceType ReferenceType => ModifierReferenceType.Null;

        public List<string> Tags { get; set; }
        public List<Modifier> Modifiers { get; set; }
        public bool IgnoreLifespan { get; set; }
        public bool OrderModifiers { get; set; }
        public int IntVariable { get; set; }

        public bool ModifiersActive => true;

        public override void CopyData(ModifierBlock<T> orig, bool newID = true)
        {
            this.CopyModifyableData(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            this.ReadModifiersJSON(jn, null);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            this.WriteModifiersJSON(jn);

            return jn;
        }
    }
}
