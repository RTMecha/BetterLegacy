using System.Collections.Generic;

using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Modifiers.Functions;
using BetterLegacy.Core.Data.Modifiers.Updaters;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    public class ModifiersManager : BaseManager<ModifiersManager, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// List of modifier functions.
        /// </summary>
        public List<ModifierFunctionBase> functions = new List<ModifierFunctionBase>();

        /// <summary>
        /// List of modifier categories.
        /// </summary>
        public Dictionary<ModifierCategoryType, List<ModifierFunctionBase>> categories = new Dictionary<ModifierCategoryType, List<ModifierFunctionBase>>();

        /// <summary>
        /// List of modifier triggers.
        /// </summary>
        public List<ModifierTriggerBase> triggers = new List<ModifierTriggerBase>();

        /// <summary>
        /// List of modifier actions.
        /// </summary>
        public List<ModifierActionBase> actions = new List<ModifierActionBase>();

        /// <summary>
        /// List of modifier updaters.
        /// </summary>
        public List<ModifierUpdaterBase> updaters = new List<ModifierUpdaterBase>
        {
            new DisableObjectUpdater(),
            new FollowMousePositionUpdater(),
            new HideMouseUpdater(),
            new LoadJSONUpdater(),
            new MusicTimeCompareUpdater(),
            new ObjectActiveOtherUpdater(),
            new ObjectVariableUpdater(),
            new PlayerDisableBoostUpdater(),
            new PlaySoundOnlineUpdater(),
            new RealTimeUpdater(1),
            new RealTimeUpdater(2),
            new RealTimeUpdater(3),
            new RealTimeUpdater(4),
            new RealTimeUpdater(5),
            new RealTimeUpdater(6),
            new RealTimeUpdater(7),
            new RealTimeUpdater(8),
            new ReinitLevelUpdater(),
            new SaveJSONUpdater(),
            new SaveLevelDataUpdater(),
            new SetActiveUpdater(),
            new SetBGActiveUpdater(),
            new SetGameModeUpdater(),
            new SetGlobalPlayerSpeedUpdater(),
            new SetPlayerVelocityUpdater(),
            new TextUpdater(),
        };

        #endregion

        #region Functions

        public override void OnInit()
        {
            var type = typeof(ModifierFunctions);
            var fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].GetValue(null) is ModifierFunctionBase function)
                    functions.Add(function);
            }

            for (int i = 0; i < functions.Count; i++)
            {
                var function = functions[i];
                if (function is ModifierTriggerBase trigger)
                    triggers.Add(trigger);
                if (function is ModifierActionBase action)
                    actions.Add(action);
                if (!categories.TryGetValue(function.Category, out List<ModifierFunctionBase> list))
                {
                    list = new List<ModifierFunctionBase>();
                    categories[function.Category] = list;
                }
                list.Add(function);
            }
        }

        /// <summary>
        /// Verifies a modifier is up to date.
        /// </summary>
        /// <param name="modifier">Modifier to verify.</param>
        /// <param name="modifyable">Modifyable reference.</param>
        /// <returns>Returns <see langword="true"/> if the modifier is valid, otherwise returns <see langword="false"/>.</returns>
        public bool VerifyModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.verified)
                return !string.IsNullOrEmpty(modifier.name);

            for (int i = 0; i < updaters.Count; i++)
            {
                var updater = updaters[i];
                if (updater.RequiresUpdate(modifier))
                    updater.UpdateModifier(modifier, modifyable);
            }

            modifier.verified = true;
            if (!functions.TryFind(x => x.Modifier && x.Name == modifier.Name && x.Modifier.type == modifier.type, out ModifierFunctionBase function))
                return !string.IsNullOrEmpty(modifier.Name);
            if (function.Compatibility.StoryOnly ? !ProjectArrhythmia.State.InEditor && !ProjectArrhythmia.State.InStory : !function.Compatibility.CompareType(modifyable.ReferenceType))
                return !string.IsNullOrEmpty(modifier.Name);

            modifier.function = function;
            if (function is ModifierTriggerBase trigger)
                modifier.trigger = trigger;
            if (function is ModifierActionBase action)
                modifier.action = action;
            modifier.compatibility = function.Compatibility;

            modifier.function.ValidateModifier(modifier, modifyable);

            int num = modifier.values.Count;
            while (modifier.values.Count < function.Modifier.values.Count)
            {
                modifier.values.Add(function.Modifier.values[num]);
                num++;
            }

            return !string.IsNullOrEmpty(modifier.name);
        }

        #endregion
    }
}
