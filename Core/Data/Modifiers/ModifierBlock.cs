using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers
{
    public class ModifierBlock : PAObject<ModifierBlock>, IPacket, IModifyable
    {
        #region Constructors

        public ModifierBlock() { }

        public ModifierBlock(string name) => Name = name;
        public ModifierBlock(string name, ModifierReferenceType referenceType) : this(name) => ReferenceType = referenceType;
        public ModifierBlock(ModifierReferenceType referenceType) => ReferenceType = referenceType;

        #endregion

        #region Values

        public string Name { get; set; }

        public ModifierReferenceType ReferenceType { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
        public List<Modifier> Modifiers { get; set; } = new List<Modifier>();
        public bool IgnoreLifespan { get; set; }
        public bool OrderModifiers { get; set; } = true;
        public int IntVariable { get; set; }

        public bool ModifiersActive => true;

        #endregion

        #region Functions

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

        public void ReadPacket(NetworkReader reader)
        {
            Name = reader.ReadString();
            ReferenceType = (ModifierReferenceType)reader.ReadByte();
            this.ReadModifiersPacket(reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(Name);
            writer.Write((byte)ReferenceType);
            this.WriteModifiersPacket(writer);
        }

        /// <summary>
        /// Runs the modifiers loop.
        /// </summary>
        /// <param name="reference">Modifier object reference.</param>
        public ModifierLoopResult Run(ModifierLoop loop)
        {
            if (IsEmpty())
                return default;

            loop.ValidateDictionary();
            return OrderModifiers ? ModifiersHelper.RunModifiersLoop(Modifiers, loop) : ModifiersHelper.RunModifiersAll(Modifiers, loop);
        }

        /// <summary>
        /// Checks if the modifiers list contains no elements.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <returns>Returns true if the list doesn't contain elements.</returns>
        public bool IsEmpty() => Modifiers.IsEmpty();

        #endregion
    }
}
