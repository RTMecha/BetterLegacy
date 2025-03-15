using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Modifier<T> : ModifierBase
    {
        public Modifier()
        {
            var type = typeof(T);
            if (type == typeof(BeatmapObject))
                referenceType = ModifierReferenceType.BeatmapObject;
            else if (type == typeof(BackgroundObject))
                referenceType = ModifierReferenceType.BackgroundObject;
            else if (type == typeof(CustomPlayer))
                referenceType = ModifierReferenceType.CustomPlayer;
            else if (type == typeof(GameData))
                referenceType = ModifierReferenceType.GameData;
        }

        public Modifier(string name) : this()
        {
            commands = new List<string> { name };
            ModifiersHelper.AssignModifierActions(this);
        }

        public T reference;

        public ModifierReferenceType referenceType;

        public Action<Modifier<T>> Action { get; set; }

        public Predicate<Modifier<T>> Trigger { get; set; }

        public Action<Modifier<T>> Inactive { get; set; }

        #region Methods

        public void VerifyModifier(List<Modifier<T>> modifiers)
        {
            if (commands.IsEmpty())
                return;

            if (modifiers.TryFind(x => x.Name == Name && x.type == type, out Modifier<T> defaultModifier))
            {
                int num = commands.Count;
                while (commands.Count < defaultModifier.commands.Count)
                {
                    commands.Add(defaultModifier.commands[num]);
                    num++;
                }
            }
        }

        public bool IsValid(List<Modifier<T>> modifiers) => commands.Count > 0 && modifiers.Has(x => x.Name == Name);

        public static Modifier<T> DeepCopy(Modifier<T> orig, T reference = default) => new Modifier<T>
        {
            type = orig.type,
            commands = orig.commands.Clone(),
            value = orig.value,
            reference = reference ?? orig.reference,
            not = orig.not,
            elseIf = orig.elseIf,
            constant = orig.constant,
            prefabInstanceOnly = orig.prefabInstanceOnly,
            collapse = orig.collapse,

            Action = orig.Action,
            Trigger = orig.Trigger,
            Inactive = orig.Inactive,
        };

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

            modifier.constant = jn["const"].AsBool;
            modifier.prefabInstanceOnly = jn["po"].AsBool;

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

                return modifier;
            }

            for (int i = 0; i < jn["commands"].Count; i++)
                modifier.commands.Add(((string)jn["commands"][i]).Replace("{{colon}}", ":"));

            modifier.value = string.IsNullOrEmpty(jn["value"]) ? "" : jn["value"];

            modifier.collapse = jn["collapse"].AsBool;

            ModifiersHelper.AssignModifierActions(modifier);

            return modifier;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["type"] = (int)type;

            if (not)
                jn["not"] = not;

            if (elseIf)
                jn["else"] = elseIf;

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
        BeatmapObject,
        BackgroundObject,
        CustomPlayer,
        GameData,
    }
}
