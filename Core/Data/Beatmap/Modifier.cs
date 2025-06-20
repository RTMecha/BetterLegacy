using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Modifier<T> : ModifierBase
    {
        public Modifier() : base() => referenceType = GetReferenceType<T>();

        public Modifier(string name) : this()
        {
            commands = new List<string> { name };
            ModifiersHelper.AssignModifierActions(this);
        }

        public T reference;

        public ModifierReferenceType referenceType;

        public Action<Modifier<T>, Dictionary<string, string>> Action { get; set; }

        public Func<Modifier<T>, Dictionary<string, string>, bool> Trigger { get; set; }

        public Action<Modifier<T>, Dictionary<string, string>> Inactive { get; set; }

        #region Methods

        public override void CopyData(ModifierBase orig, bool newID = true)
        {
            CopyData(orig, newID, reference);
        }

        public void CopyData(ModifierBase orig, bool newID = true, T reference = default)
        {
            base.CopyData(orig, newID);

            var modifier = orig as Modifier<T>;
            if (!modifier)
                return;

            this.reference = reference ?? modifier.reference;
            Action = modifier.Action;
            Trigger = modifier.Trigger;
            Inactive = modifier.Inactive;
        }

        /// <summary>
        /// Creates a copy of the modifier.
        /// </summary>
        /// <param name="reference">Reference that should be copied.</param>
        /// <returns>Returns a copy of the modifier.</returns>
        public Modifier<T> Copy(T reference)
        {
            var obj = new Modifier<T>();
            obj.CopyData(this, false, reference);
            return obj;
        }

        /// <summary>
        /// Creates a copy of the modifier.
        /// </summary>
        /// <param name="newID">If the ID of the modifier should be copied.</param>
        /// <returns>Returns a copy of the modifier.</returns>
        public Modifier<T> Copy(bool newID, T reference = default)
        {
            var obj = new Modifier<T>();
            obj.CopyData(this, newID, reference);
            return obj;
        }

        public static Modifier<T> Parse(JSONNode jn, T reference = default)
        {
            var modifier = new Modifier<T>();
            modifier.reference = reference;

            modifier.type = (Type)jn["type"].AsInt;

            if (modifier.type == Type.Trigger)
            {
                modifier.not = jn["not"].AsBool;
                modifier.elseIf = jn["else"].AsBool;
            }

            modifier.triggerCount = jn["count"].AsInt;
            modifier.constant = jn["const"].AsBool;
            modifier.prefabInstanceOnly = jn["po"].AsBool;
            modifier.groupAlive = jn["ga"].AsBool;

            modifier.collapse = jn["collapse"].AsBool;

            modifier.commands.Clear();
            if (jn["name"] != null)
            {
                modifier.commands.Add(jn["name"]);

                if (jn["values"] != null)
                {
                    modifier.value = jn["values"][0];
                    for (int i = 1; i < jn["values"].Count; i++)
                        modifier.commands.Add(jn["values"][i]);
                }

                ModifiersHelper.AssignModifierActions(modifier);

                return modifier;
            }

            for (int i = 0; i < jn["commands"].Count; i++)
                modifier.commands.Add(((string)jn["commands"][i]).Replace("{{colon}}", ":"));

            modifier.value = string.IsNullOrEmpty(jn["value"]) ? string.Empty : jn["value"];

            ModifiersHelper.AssignModifierActions(modifier);

            return modifier;
        }

        public override JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["type"] = (int)type;

            if (not)
                jn["not"] = not;

            if (elseIf)
                jn["else"] = elseIf;

            if (triggerCount > 0)
                jn["count"] = triggerCount;

            for (int i = 0; i < commands.Count; i++)
                jn["commands"][i] = commands[i];

            jn["value"] = value;

            jn["const"] = constant;

            if (prefabInstanceOnly)
                jn["po"] = prefabInstanceOnly;

            if (collapse)
                jn["collapse"] = collapse;

            return jn;
        }

        public override string ToString() => Name;

        #endregion
    }

    public enum ModifierReferenceType
    {
        Null,
        BeatmapObject,
        BackgroundObject,
        PrefabObject,
        CustomPlayer,
        GameData,
    }
}
