using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTPrefabModifiers : RTModifiers
    {
        public RTPrefabModifiers(List<Modifier> modifiers, PrefabObject prefabObject, bool orderMatters, float startTime, float killTime, RTLevelBase parentRuntime)
        {
            this.modifiers = modifiers;
            reference = prefabObject;
            this.orderMatters = orderMatters;
            UpdateCache();

            ParentRuntime = parentRuntime;
            StartTime = startTime;
            KillTime = killTime;

            modifiers.ForLoop(modifier => ModifiersHelper.AssignModifierActions(modifier, reference.ReferenceType));

            runtimePrefabObject = prefabObject.runtimeObject;
        }

        public RTPrefabObject runtimePrefabObject;

        public override void Interpolate(float time)
        {
            if (orderMatters)
                ModifiersHelper.RunModifiersLoop(modifiers, reference, variables);
            else
                ModifiersHelper.RunModifiersAll(triggers, actions, modifiers, reference, variables);

            if (!runtimePrefabObject)
                return;

            for (int i = 0; i < runtimePrefabObject.modifiers.Count; i++)
                if (runtimePrefabObject.modifiers[i] is RTModifiers runtimeModifiers)
                    runtimeModifiers.variables.InsertRange(variables);

            for (int i = 0; i < runtimePrefabObject.bgModifiers.Count; i++)
                if (runtimePrefabObject.bgModifiers[i] is RTModifiers runtimeModifiers)
                    runtimeModifiers.variables.InsertRange(variables);

            for (int i = 0; i < runtimePrefabObject.prefabModifiers.Count; i++)
                if (runtimePrefabObject.prefabModifiers[i] is RTPrefabModifiers runtimeModifiers)
                    runtimeModifiers.variables.InsertRange(variables);
        }
    }
}
