using System.Collections.Generic;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Modifiers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTModifiers : Exists, IRTObject
    {
        public RTModifiers() { }

        public RTModifiers(List<Modifier> modifiers, IModifierReference reference, float startTime, float killTime, RTLevelBase parentRuntime)
        {
            this.modifiers = modifiers;
            this.reference = reference;

            ParentRuntime = parentRuntime;
            StartTime = startTime;
            KillTime = killTime;

            loop = new ModifierLoop
            {
                variables = variables,
                reference = reference,
            };
        }

        public IModifierReference reference;

        public List<Modifier> modifiers;

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }
        public float KillTime { get; set; }
        public bool Active { get; set; }

        public bool active;

        public ModifierLoop loop;


        public Dictionary<string, string> variables = new Dictionary<string, string>();

        public void Clear() => modifiers.ForLoop(modifier =>
        {
            modifier.active = false;
            modifier.runCount = 0;
            modifier.RunInactive(modifier, reference);
            modifier.OnRemoveCache();
            modifier.Result = default;
        });

        public void SetActive(bool active)
        {
            Active = active;
            this.active = active;
            if (active)
                return;

            modifiers.ForLoop(modifier => modifier.Reset(loop));
        }

        public virtual void Interpolate(float time) => loop.Run(modifiers);

        public override string ToString() => reference?.ToString() ?? string.Empty;
    }
}
