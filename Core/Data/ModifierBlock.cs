using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data
{
    public class ModifierBlock<T> : PAObject<ModifierBlock<T>>, IModifyable<T>
    {
        public ModifierBlock() { }

        public ModifierReferenceType ReferenceType => throw new NotImplementedException();

        public List<string> Tags { get; set; }
        public List<Modifier<T>> Modifiers { get; set; }
        public bool IgnoreLifespan { get; set; }
        public bool OrderModifiers { get; set; }
        public int IntVariable { get; set; }

        public bool ModifiersActive => true;

        public void SetReference(T reference)
        {
            for (int i = 0; i < Modifiers.Count; i++)
                Modifiers[i].reference = reference;
        }

        public override void CopyData(ModifierBlock<T> orig, bool newID = true)
        {
            Tags = orig.Tags.Clone();
            Modifiers = orig.Modifiers.Select(x => x.Copy(x.reference)).ToList();
            IgnoreLifespan = orig.IgnoreLifespan;
            OrderModifiers = orig.OrderModifiers;
            IntVariable = orig.IntVariable;
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
