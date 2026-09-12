using UnityEngine;

using BetterLegacy.Editor;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public abstract class ModifierActionBase : ModifierFunctionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.ExclaimSprite;

        #endregion

        #region Functions

        /// <summary>
        /// Action function.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">The current modifier loop.</param>
        public abstract void Run(Modifier modifier, ModifierLoop modifierLoop);

        public Modifier CreateModifier(string name, params string[] values) => new Modifier(Modifier.Type.Action, name, true, values)
        {
            function = this,
            action = this,
            compatibility = Compatibility,
        };

        public Modifier CreateModifier(string name, bool constant, params string[] values) => new Modifier(Modifier.Type.Action, name, constant, values)
        {
            function = this,
            action = this,
            compatibility = Compatibility,
        };

        public Modifier CreateModifier(string name, int version, params string[] values) => new Modifier(Modifier.Type.Action, name, true, values)
        {
            function = this,
            action = this,
            compatibility = Compatibility,
            version = version,
        };

        public Modifier CreateModifier(string name, int version, bool constant, params string[] values) => new Modifier(Modifier.Type.Action, name, constant, values)
        {
            function = this,
            action = this,
            compatibility = Compatibility,
            version = version,
        };

        public void SetupModifier(params string[] values) => Modifier = CreateModifier(Name, values);

        public void SetupModifier(bool constant, params string[] values) => Modifier = CreateModifier(Name, constant, values);

        public void SetupModifier(int version, params string[] values) => Modifier = CreateModifier(Name, version, values);

        public void SetupModifier(int version, bool constant, params string[] values) => Modifier = CreateModifier(Name, version, constant, values);

        #endregion
    }
}
