using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTPrefabModifiers : RTModifiers
    {
        public RTPrefabModifiers(List<Modifier> modifiers, PrefabObject prefabObject, bool orderMatters, float startTime, float killTime)
        {
            this.modifiers = modifiers;
            this.reference = prefabObject;
            this.orderMatters = orderMatters;

            StartTime = startTime;
            KillTime = killTime;

            modifiers.ForLoop(modifier => ModifiersHelper.AssignModifierActions(modifier, reference));

            runtimePrefabObject = prefabObject.runtimeObject;
        }

        public RTPrefabObject runtimePrefabObject;

        public override void Interpolate(float time)
        {
            if (orderMatters)
                ModifiersHelper.RunModifiersLoop(modifiers, reference, variables);
            else
                ModifiersHelper.RunModifiersAll(modifiers, reference, variables);

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
