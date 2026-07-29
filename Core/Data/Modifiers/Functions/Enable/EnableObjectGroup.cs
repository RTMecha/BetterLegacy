using System.Collections.Generic;

using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EnableObjectGroup : ModifierActionBase
    {
        #region Constructors

        public EnableObjectGroup()
        {
            SetupModifier("True", "0");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "enableObjectGroup";

        public override ModifierCategoryType Category => ModifierCategoryType.Enable;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var enabled = modifier.GetBool(0, true, modifierLoop.variables);
            var state = modifier.GetInt(1, 0, modifierLoop.variables);

            var cache = modifier.GetResultOrDefault(() =>
            {
                var cache = new Cache();
                var prefabable = modifierLoop.reference.AsPrefabable();
                if (prefabable == null)
                    return cache;

                var groups = new List<List<IPrefabable>>();
                int count = 0;
                for (int i = 2; i < modifier.values.Count; i++)
                {
                    var tag = modifier.values[i];
                    if (string.IsNullOrEmpty(tag))
                        continue;

                    var list = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, tag);
                    groups.Add(list);
                    cache.allObjects.AddRange(list);

                    count++;
                }
                cache.Init(groups.ToArray(), enabled);
                return cache;
            });
            cache?.SetGroupActive(enabled, state);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0, true);

            var options = new List<string>() { "All" };
            for (int i = 2; i < modifier.values.Count; i++)
                options.Add(modifier.values[i]);

            modifierCard.DropdownGenerator(modifier, reference, "Value", 1, options);

            int a = 0;
            for (int i = 2; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Group {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", i, _val =>
                {
                    var value = modifierCard.DialogScrollbarValue;
                    modifierCard.RenderModifier(reference);
                    CoroutineHelper.PerformAtNextFrame(() => modifierCard.DialogScrollbarValue = value);
                }).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));

                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Group", () =>
            {
                modifier.values.Add($"Object Group");
            });
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public Cache() { }

            int currentState = -1;

            /// <summary>
            /// List of object groups.
            /// </summary>
            public List<IPrefabable>[] objects;
            /// <summary>
            /// List of all objects in the cache.
            /// </summary>
            public List<IPrefabable> allObjects = new List<IPrefabable>();

            readonly HashSet<IPrefabable> activeObjects = new HashSet<IPrefabable>();

            /// <summary>
            /// Initializes the cache.
            /// </summary>
            /// <param name="objects">List of object groups.</param>
            /// <param name="enabled">Enabled / disabled state.</param>
            public void Init(List<IPrefabable>[] objects, bool enabled)
            {
                this.objects = objects;
                foreach (var obj in allObjects)
                    ModifiersHelper.SetObjectActive(obj, !enabled);
            }

            /// <summary>
            /// Recalculates the currently active objects.
            /// </summary>
            /// <param name="enabled">Enabled / disabled state.</param>
            /// <param name="state">Currently active group.</param>
            public void RecalculateActiveObjects(bool enabled, int state)
            {
                foreach (var obj in activeObjects)
                    ModifiersHelper.SetObjectActive(obj, !enabled);

                activeObjects.Clear();
                if (state == 0)
                {
                    foreach (var obj in allObjects)
                        activeObjects.Add(obj);
                    return;
                }

                var current = objects.GetAt(state - 1);
                if (current == default)
                    return;
                foreach (var obj in current)
                    activeObjects.Add(obj);
            }

            /// <summary>
            /// Sets a group active.
            /// </summary>
            /// <param name="enabled">Enabled / disabled state.</param>
            /// <param name="state">Currently active group.</param>
            public void SetGroupActive(bool enabled, int state)
            {
                if (currentState == state)
                    return;

                RecalculateActiveObjects(enabled, state);

                foreach (var obj in activeObjects)
                    ModifiersHelper.SetObjectActive(obj, enabled);

                currentState = state;
            }

            /// <summary>
            /// Gets the active state for an object group. If <paramref name="state"/> is 0, then all should have their active state the same as <paramref name="enabled"/>. Otherwise if the state equals the modifier group, set only that object group to <paramref name="enabled"/>.
            /// </summary>
            /// <param name="enabled">If the active group should be enabled / disabled.</param>
            /// <param name="state">The currently active group.</param>
            /// <param name="groupIndex">The group index.</param>
            /// <returns>Returns true if the group is active, otherwise returns false.</returns>
            public bool GetState(bool enabled, int state, int groupIndex)
            {
                // if state is 0, then all should be active / inactive. otherwise if state equals the modifier group, set only that object group active / inactive.
                var innerEnabled = state == 0 || state == groupIndex - 1;
                if (!enabled)
                    innerEnabled = !innerEnabled;

                return innerEnabled;
            }
        }

        #endregion
    }
}
