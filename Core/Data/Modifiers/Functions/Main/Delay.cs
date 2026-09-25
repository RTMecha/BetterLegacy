using System.Collections.Generic;
using UnityEngine;
using BetterLegacy.Editor.Data.Elements;
namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Delay : ModifierActionBase
    {
        public Delay(bool isEnd)
        {
            this.isEnd = isEnd;
            Name = isEnd ? "delayEnd" : "delay";
            if (isEnd)
            {
                SetupModifier();
                Modifier.collapse = true;
            }
            else
                SetupModifier("0.5");
        }
        public override string Name { get; }
        public override ModifierCategoryType Category => ModifierCategoryType.Main;
        readonly bool isEnd;
        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isEnd)
                return;
            var modifiers = modifierLoop.reference is IModifyable modifyable ? modifyable.Modifiers : null;
            if (modifiers == null)
                return;
            var cache = modifier.GetResultOrDefault(() => new DelayCache(modifierLoop.reference));
            var endIndex = GetEndIndex(modifierLoop.state.index, modifiers);
            var delay = Mathf.Max(0f, modifier.GetFloat(0, 0.5f, modifierLoop.variables));
            cache.queue.Add(new Pending
            {
                fireTime = Time.time + delay,
                block = modifiers.GetIndexRange(modifierLoop.state.index + 1, endIndex),
                variables = new Dictionary<string, string>(modifierLoop.variables),
            });
            Drain(cache);
            modifierLoop.state.index = endIndex;
        }
        public override void HandleSkip(Modifier modifier, ModifierLoop modifierLoop, List<Modifier> modifiers)
        {
            if (!isEnd && modifier.TryGetResult(out DelayCache cache))
                Drain(cache);
            modifierLoop.state.previousType = modifier.type;
            modifierLoop.state.index = isEnd ? modifierLoop.state.index + 1 : GetEndIndex(modifierLoop.state.index, modifiers);
        }
        void Drain(DelayCache cache)
        {
            var now = Time.time;
            while (cache.queue.Count > 0 && cache.queue[0].fireTime <= now)
            {
                var pending = cache.queue[0];
                cache.queue.RemoveAt(0);
                if (pending.block.IsEmpty())
                    continue;
                cache.innerLoop.variables = pending.variables;
                cache.innerLoop.Run(pending.block);
            }
        }
        int GetEndIndex(int startIndex, List<Modifier> modifiers)
        {
            var depth = 0;
            for (int i = startIndex + 1; i < modifiers.Count; i++)
            {
                var name = modifiers[i].Name;
                if (name == "delay")
                    depth++;
                else if (name == "delayEnd")
                {
                    if (depth == 0)
                        return i;
                    depth--;
                }
            }
            return modifiers.Count;
        }
        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.TryGetResult(out DelayCache cache))
                cache.queue.Clear();
        }
        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isEnd)
                return;
            modifierCard.SingleGenerator(modifier, reference, "Delay", 0, 0.5f);
        }
        class DelayCache
        {
            public DelayCache(IModifierReference reference) => innerLoop = new ModifierLoop(reference, null);
            public readonly List<Pending> queue = new List<Pending>();
            public readonly ModifierLoop innerLoop;
        }
        struct Pending
        {
            public float fireTime;
            public List<Modifier> block;
            public Dictionary<string, string> variables;
        }
    }
}