using BetterLegacy.Editor.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Editor.Data
{
    public class Keybind
    {
        public Keybind(string id, List<Key> keys, int actionType, Dictionary<string, string> settings = null)
        {
            this.id = id;
            this.keys = keys;
            ActionType = actionType;
            this.settings = settings ?? new Dictionary<string, string>();
        }

        public static Keybind Parse(JSONNode jn)
        {
            string id = jn["id"];

            int actionType = jn["action"].AsInt;

            var keys = new List<Key>();
            for (int i = 0; i < jn["keys"].Count; i++)
                keys.Add(Key.Parse(jn["keys"][i]));

            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < jn["settings"].Count; i++)
            {
                if (!dictionary.ContainsKey(jn["settings"][i]))
                    dictionary.Add(jn["settings"][i]["type"], jn["settings"][i]["value"]);
            }

            var setting = KeybindEditor.inst.Settings[actionType];
            if (setting != null)
            {
                foreach (var keyValuePair in setting)
                {
                    if (!dictionary.ContainsKey(keyValuePair.Key))
                        dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            return new Keybind(id, keys, actionType, dictionary);
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["name"] = Name;

            jn["action"] = ActionType.ToString();
            for (int i = 0; i < keys.Count; i++)
            {
                jn["keys"][i] = keys[i].ToJSON();
            }

            for (int i = 0; i < settings.Count; i++)
            {
                var element = settings.ElementAt(i);
                jn["settings"][i]["type"] = element.Key;
                jn["settings"][i]["value"] = element.Value;
            }

            return jn;
        }

        public void Activate()
        {
            if (KeybindEditor.inst.KeyCodeHandler(this))
                Action?.Invoke(this);
        }

        public string id;
        public Dictionary<string, string> settings = new Dictionary<string, string>();
        public KeyCode KeyCode { get; set; }
        public KeyCode KeyCodeDown { get; set; }
        public int ActionType { get; set; }
        public Action<Keybind> Action
        {
            get
            {
                if (ActionType < 0 || ActionType > KeybindEditor.KeybinderMethods.Count - 1)
                    return keybind => { Debug.LogError($"{KeybindEditor.className}No action assigned to key!"); };

                return KeybindEditor.KeybinderMethods[ActionType];
            }
        }

        public string Name => ActionType >= 0 && ActionType < KeybindEditor.KeybinderMethods.Count ? KeybindEditor.KeybinderMethods[ActionType].Method.Name : "Invalid method";

        public bool watchingKeybind;

        public string DefaultCode => $"var keybind = EditorManagement.Functions.Editors.KeybindManager.inst.keybinds.Find(x => x.id == \"{id}\");{Environment.NewLine}";

        public List<Key> keys = new List<Key>();

        public override string ToString() => Name;

        public class Key
        {
            public Key(Type type, KeyCode keyCode)
            {
                InteractType = type;
                KeyCode = keyCode;
            }

            public enum Type
            {
                Down,
                Pressed,
                Up,
                NotPressed
            }

            public Type InteractType { get; set; }
            public KeyCode KeyCode { get; set; }

            public static Key Parse(JSONNode jn) => new Key((Type)jn["type"].AsInt, (KeyCode)jn["key"].AsInt);

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["type"] = ((int)InteractType).ToString();
                jn["key"] = ((int)KeyCode).ToString();

                return jn;
            }
        }
    }
}
