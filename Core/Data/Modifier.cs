﻿using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    public class Modifier<T> : ModifierBase
    {
        public Modifier() { }

        public T reference;

        public Action<Modifier<T>> Action { get; set; }
        public Patchers.PrefixMethod<Modifier<T>> Trigger { get; set; }

        public Action<Modifier<T>> Inactive { get; set; }

        #region Methods

        public void VerifyModifier(List<Modifier<T>> modifiers)
        {
            if (commands.Count < 1)
                return;

            if (modifiers.TryFind(x => x.commands[0] == commands[0] && x.type == type, out Modifier<T> defaultModifier))
            {
                int num = commands.Count;
                while (commands.Count < defaultModifier.commands.Count)
                {
                    commands.Add(defaultModifier.commands[num]);
                    num++;
                }
            }
        }

        public bool IsValid(List<Modifier<T>> modifiers) => commands.Count > 0 && modifiers.Has(x => x.commands[0] == commands[0]);

        public static Modifier<T> DeepCopy(Modifier<T> orig, T reference = default) => new Modifier<T>
        {
            type = orig.type,
            commands = orig.commands.Clone(),
            value = orig.value,
            reference = reference ?? orig.reference,
            not = orig.not,
            constant = orig.constant,
            prefabInstanceOnly = orig.prefabInstanceOnly,
        };

        public static Modifier<T> Parse(JSONNode jn, T reference = default)
        {
            var modifier = new Modifier<T>();
            modifier.type = (Type)jn["type"].AsInt;
            modifier.not = jn["not"].AsBool;

            modifier.commands.Clear();
            for (int i = 0; i < jn["commands"].Count; i++)
                modifier.commands.Add(((string)jn["commands"][i]).Replace("{{colon}}", ":"));

            modifier.constant = jn["const"].AsBool;
            modifier.value = string.IsNullOrEmpty(jn["value"]) ? "" : jn["value"];

            modifier.prefabInstanceOnly = jn["po"].AsBool;

            modifier.reference = reference;

            return modifier;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["type"] = (int)type;

            if (not)
                jn["not"] = not.ToString();

            for (int i = 0; i < commands.Count; i++)
                jn["commands"][i] = commands[i];

            jn["value"] = value;

            jn["const"] = constant.ToString();

            if (prefabInstanceOnly)
                jn["po"] = prefabInstanceOnly.ToString();

            return jn;
        }

        public override string ToString() => Name;

        #endregion
    }
}
