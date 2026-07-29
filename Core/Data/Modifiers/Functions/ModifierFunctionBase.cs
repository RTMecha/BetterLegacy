using System.Collections.Generic;

using UnityEngine;

using ILMath;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public abstract class ModifierFunctionBase : Exists
    {
        #region Values

        /// <summary>
        /// Name of the modifier function.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Category of the modifier.
        /// </summary>
        public abstract ModifierCategoryType Category { get; }

        /// <summary>
        /// What the modifier function is compatible with.
        /// </summary>
        public virtual ModifierCompatibility Compatibility => ModifierCompatibility.AllCompatible;

        /// <summary>
        /// The default modifier.
        /// </summary>
        public Modifier Modifier { get; internal set; }

        /// <summary>
        /// If the modifier function is a special function that overrides the default action function.
        /// </summary>
        public virtual bool SpecialFunction => false;

        /// <summary>
        /// If the running state should be overridden.
        /// </summary>
        public virtual bool OverrideRunningState => false;

        /// <summary>
        /// If the modifier is a group type.
        /// </summary>
        public bool IsGroup { get; internal set; }

        /// <summary>
        /// Icon of the modifier function to display in the editor.
        /// </summary>
        public virtual Sprite Icon { get; }

        /// <summary>
        /// If the modifier is only for the editor.
        /// </summary>
        public virtual bool IsEditorModifier => false;

        /// <summary>
        /// If the modifier should display in the editor.
        /// </summary>
        public virtual bool DisplayInEditor => true;

        #endregion

        #region Functions

        /// <summary>
        /// Validates the modifier has the correct values.
        /// </summary>
        /// <param name="modifier">Modifier to validate.</param>
        /// <param name="modifyable">Modifyable object reference.</param>
        public virtual void ValidateModifier(Modifier modifier, IModifyable modifyable) { }

        /// <summary>
        /// Inactive function.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">Modifier loop reference.</param>
        public virtual void Inactive(Modifier modifier, ModifierLoop modifierLoop) { }

        /// <summary>
        /// Renders the modifier in the editor.
        /// </summary>
        /// <param name="modifier">Modifier to render.</param>
        /// <param name="modifierCard">Editor display.</param>
        /// <param name="reference">Object reference.</param>
        /// <param name="modifyable">Modifyable reference.</param>
        public abstract void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable);

        /// <summary>
        /// Function occurs when the cache is removed.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        public virtual void OnRemoveCache(Modifier modifier) { }

        /// <summary>
        /// Creates an instance of the modifier.
        /// </summary>
        /// <returns>Returns a copy of the modifier.</returns>
        public Modifier Create() => Modifier?.Copy();

        /// <summary>
        /// Handles the skip function in the modifier loop.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="modifierLoop">Modifier loop.</param>
        /// <param name="modifiers">Modifier list.</param>
        public virtual void HandleSkip(Modifier modifier, ModifierLoop modifierLoop, List<Modifier> modifiers)
        {
            modifierLoop.state.previousType = modifier.type;
            modifierLoop.state.index++;
        }

        /// <summary>
        /// Formats the input based on the modifier variables.
        /// </summary>
        /// <param name="input">Input to format.</param>
        /// <param name="variables">Modifier variables.</param>
        /// <returns>Returns the formatted input.</returns>
        public static string FormatStringVariables(string input, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            foreach (var variable in variables)
                input = input.Replace("{" + variable.Key + "}", variable.Value);
            return input;
        }

        public static bool TryGetBeatmapObject(Modifier modifier, ModifierLoop modifierLoop, bool isGroup, int index, out BeatmapObject beatmapObject)
        {
            beatmapObject = GetBeatmapObject(modifier, modifierLoop, isGroup, index);
            return beatmapObject;
        }

        public static BeatmapObject GetBeatmapObject(Modifier modifier, ModifierLoop modifierLoop, bool isGroup, int index)
        {
            if (!isGroup)
                return modifierLoop.reference as BeatmapObject;
            if (modifierLoop.reference is IPrefabable prefabable && GameData.Current.TryFindObjectWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables), out BeatmapObject beatmapObject))
                return beatmapObject;
            return null;
        }

        public static bool TryGetModifierReference(Modifier modifier, ModifierLoop modifierLoop, bool isGroup, int index, out IModifierReference reference)
        {
            reference = GetModifierReference(modifier, modifierLoop, isGroup, index);
            return reference != null;
        }

        public static IModifierReference GetModifierReference(Modifier modifier, ModifierLoop modifierLoop, bool isGroup, int index)
        {
            if (!isGroup)
                return modifierLoop.reference;
            if (modifierLoop.reference is IPrefabable prefabable && GameData.Current.TryFindModifierReferenceWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables), out IModifierReference reference))
                return reference;
            return null;
        }

        public override string ToString() => Name ?? "Invalid Modifier";

        #endregion

        #region Sub Classes

        public class MathCache
        {
            public string input;
            public Evaluator evaluator;
        }
        
        public class GenericGroupCache<TList, TObject>
        {
            public GenericGroupCache() { }

            public GenericGroupCache(string tag, List<TList> group) => UpdateCache(tag, group);
            public GenericGroupCache(string tag, TObject obj) => UpdateCache(tag, obj);

            public string tag;
            public List<TList> group;
            public TObject obj;

            public void UpdateCache(string tag, List<TList> group)
            {
                this.tag = tag;
                this.group = group;
            }

            public void UpdateCache(string tag, TObject obj)
            {
                this.tag = tag;
                this.obj = obj;
            }

            public virtual void UpdateCache(Modifier modifier, IPrefabable prefabable, string tag)
            {
                this.tag = tag;
            }
        }

        public class GenericGroupCache<T> : GenericGroupCache<T, T>
        {
            public GenericGroupCache() { }

            public GenericGroupCache(string tag, List<T> group) => UpdateCache(tag, group);
            public GenericGroupCache(string tag, T obj) => UpdateCache(tag, obj);
        }

        public class ParentableGroupCache : GenericGroupCache<IParentable, BeatmapObject>
        {
            public ParentableGroupCache() { }

            public string otherGroup;
            bool multi;

            public static ParentableGroupCache GetSingle(Modifier modifier, IPrefabable prefabable, string group)
            {
                var cache = new ParentableGroupCache();
                cache.tag = group;
                cache.UpdateCache(modifier, prefabable, group);
                return cache;
            }

            public static ParentableGroupCache GetGroup(Modifier modifier, IPrefabable prefabable, string group, string otherGroup)
            {
                var cache = new ParentableGroupCache();
                cache.tag = group;
                cache.otherGroup = otherGroup;
                cache.multi = true;
                cache.UpdateCache(modifier, prefabable, group);
                return cache;
            }

            public override void UpdateCache(Modifier modifier, IPrefabable prefabable, string tag)
            {
                this.tag = tag;
                if (!multi)
                {
                    if (!string.IsNullOrEmpty(tag) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                        obj = target;
                }
                else
                {
                    if (!string.IsNullOrEmpty(tag) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                        obj = target;
                    if (!obj && prefabable is BeatmapObject parent)
                        obj = parent;
                    group = GameData.Current.FindParentablesWithTag(modifier, prefabable, otherGroup);
                }
            }
        }

        public class GroupBeatmapObjectCache : GenericGroupCache<BeatmapObject>
        {
            public GroupBeatmapObjectCache(string tag) => this.tag = tag;

            public static GroupBeatmapObjectCache Get(Modifier modifier, IPrefabable prefabable, string tag)
            {
                var cache = new GroupBeatmapObjectCache(tag);
                cache.UpdateCache(modifier, prefabable, tag);
                return cache;
            }

            public override void UpdateCache(Modifier modifier, IPrefabable prefabable, string tag)
            {
                if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                    obj = target;
            }
        }

        #endregion
    }

    public abstract class ModifierTriggerBase : ModifierFunctionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.QuestionSprite;

        #endregion

        #region Functions

        public abstract bool Run(Modifier modifier, ModifierLoop modifierLoop);

        public Modifier CreateModifier(string name, params string[] values) => new Modifier(Modifier.Type.Trigger, name, true, values)
        {
            function = this,
            trigger = this,
            compatibility = Compatibility,
        };

        public Modifier CreateModifier(string name, int version, params string[] values) => new Modifier(Modifier.Type.Trigger, name, true, values)
        {
            function = this,
            trigger = this,
            compatibility = Compatibility,
            version = version,
        };

        public void SetupModifier(params string[] values) => Modifier = CreateModifier(Name, values);
        
        public void SetupModifier(int version, params string[] values) => Modifier = CreateModifier(Name, version, values);

        #endregion
    }

    public abstract class ModifierActionBase : ModifierFunctionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.ExclaimSprite;

        #endregion

        #region Functions

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

    public abstract class ModifierVariableBase : ModifierActionBase
    {
        #region Values

        public override Sprite Icon => EditorSprites.DownArrow;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = GetValue(modifier, modifierLoop);
            if (value != null)
                modifierLoop.variables[GetKey(modifier, modifierLoop)] = value;
        }

        public virtual string GetKey(Modifier modifier, ModifierLoop modifierLoop) => FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);

        public abstract string GetValue(Modifier modifier, ModifierLoop modifierLoop);

        #endregion
    }
}
