using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class Labels : Exists
    {
        public Labels(InitSettings initSettings, params Label[] labels)
        {
            this.labels = labels.ToList();
            Init(initSettings);
        }

        public Label this[int index]
        {
            get => labels[index];
            set => labels[index] = value;
        }

        public int Count => labels.Count;

        public List<Label> labels = new List<Label>();

        public GameObject GameObject { get; set; }

        public virtual void Init(InitSettings initSettings)
        {
            if (!GameObject)
                GameObject = EditorPrefabHolder.Instance.Labels.Duplicate(initSettings.parent, initSettings.name, initSettings.siblingIndex);
            else
                CoreHelper.DestroyChildren(GameObject.transform, 1, GameObject.transform.childCount - 1);

            var first = GameObject.transform.GetChild(0);

            for (int i = 0; i < Count; i++)
            {
                var label = labels[i];
                if (i >= GameObject.transform.childCount)
                    first.gameObject.Duplicate(GameObject.transform, first.name);

                var child = GameObject.transform.GetChild(i);
                var labelText = child.GetComponent<Text>();
                label.Apply(labelText);

                if (initSettings.applyThemes)
                    EditorThemeManager.AddLightText(labelText);
            }
        }

        public struct InitSettings
        {
            public static InitSettings Default => new InitSettings()
            {
                parent = null,
                name = string.Empty,
                siblingIndex = -1,
                applyThemes = true,
            };

            public InitSettings Parent(Transform parent)
            {
                this.parent = parent;
                return this;
            }

            public InitSettings Name(string name)
            {
                this.name = name;
                return this;
            }

            public InitSettings SiblingIndex(int siblingIndex)
            {
                this.siblingIndex = siblingIndex;
                return this;
            }

            public InitSettings ApplyThemes(bool applyThemes)
            {
                this.applyThemes = applyThemes;
                return this;
            }

            public Transform parent;
            public string name;
            public int siblingIndex;
            public bool applyThemes;
        }
    }
}
