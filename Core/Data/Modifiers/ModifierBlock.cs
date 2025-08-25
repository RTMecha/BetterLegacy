using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers
{
    public class ModifierBlock : PAObject<ModifierBlock>, IModifyable
    {
        public ModifierBlock() { }

        public ModifierBlock(string name) => Name = name;
        public ModifierBlock(string name, ModifierReferenceType referenceType) : this(name) => ReferenceType = referenceType;
        public ModifierBlock(ModifierReferenceType referenceType) => ReferenceType = referenceType;

        public string Name { get; set; }

        public ModifierReferenceType ReferenceType { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
        public List<Modifier> Modifiers { get; set; } = new List<Modifier>();
        public bool IgnoreLifespan { get; set; }
        public bool OrderModifiers { get; set; } = true;
        public int IntVariable { get; set; }

        public bool ModifiersActive => true;

        /// <summary>
        /// Runs the modifiers loop.
        /// </summary>
        /// <param name="reference">Modifier object reference.</param>
        public ModifierLoopResult Run(IModifierReference reference, Dictionary<string, string> variables = null)
        {
            if (IsEmpty())
                return default;

            if (variables == null)
                variables = new Dictionary<string, string>();

            return OrderModifiers ? ModifiersHelper.RunModifiersLoop(Modifiers, reference, variables) : ModifiersHelper.RunModifiersAll(Modifiers, reference, variables);
        }

        /// <summary>
        /// Checks if the modifiers list contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <returns>Returns true if the list doesn't contain elements.</returns>
        public bool IsEmpty() => Modifiers.IsEmpty();

        public override void CopyData(ModifierBlock orig, bool newID = true)
        {
            Name = orig.Name;
            this.CopyModifyableData(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            this.ReadModifiersJSON(jn);
            if (!Modifiers.IsEmpty())
                this.UpdateFunctions();
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            this.WriteModifiersJSON(jn);

            return jn;
        }
    }
}
