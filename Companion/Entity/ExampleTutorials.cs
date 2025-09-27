using System;
using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Companion.Data;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Companion.Entity
{
    public class ExampleTutorials : ExampleModule<ExampleTutorials>
    {
        #region Default Instance

        public ExampleTutorials() { }

        public override void InitDefault()
        {
            RegisterFunctions();
        }

        #endregion

        #region Core

        public override void Build()
        {

        }

        public override void Tick()
        {

        }

        public override void Clear()
        {
            base.Clear();

            onTutorialEnd = null;
            CurrentTutorial = null;
        }

        #endregion

        #region Tutorials

        /// <summary>
        /// If Example is in tutorial mode.
        /// </summary>
        public bool inTutorial;

        /// <summary>
        /// The current progress of <see cref="CurrentTutorial"/>.
        /// </summary>
        public int tutorialActIndex;

        public Action onTutorialEnd;

        /// <summary>
        /// The currently active tutorial.
        /// </summary>
        public ExampleTutorial CurrentTutorial { get; set; } = ExampleTutorial.NONE;

        /// <summary>
        /// Advanced the current tutorial.
        /// </summary>
        /// <param name="key">Key of the tutorial to match.</param>
        /// <param name="actIndex">Current action index to check.</param>
        public bool AdvanceTutorial(ExampleTutorial tutorialType, int actIndex)
        {
            if (IsCurrentTutorial(tutorialType, actIndex))
                return AdvanceTutorial();
            return false;
        }

        /// <summary>
        /// Advanced the current tutorial.
        /// </summary>
        /// <returns>Returns true if the tutorial advances, otherwise returns false if the tutorial has ended or there is no current tutorial.</returns>
        public bool AdvanceTutorial()
        {
            if (!inTutorial || CurrentTutorial == default || CurrentTutorial == ExampleTutorial.NONE)
                return false;

            tutorialActIndex++;
            CoreHelper.Log($"Progress tutorial: {CurrentTutorial} to {tutorialActIndex}");

            if (tutorialActIndex < 0 || tutorialActIndex >= CurrentTutorial.ActionCount)
            {
                CurrentTutorial = ExampleTutorial.NONE;
                inTutorial = false;
                onTutorialEnd?.Invoke();
                onTutorialEnd = null;
                return false;
            }

            CurrentTutorial[tutorialActIndex]?.Invoke(reference);
            return true;
        }

        /// <summary>
        /// Checks if a tutorial is the current one.
        /// </summary>
        /// <param name="key">Key of the tutorial to match.</param>
        /// <param name="actIndex">Current action index to check.</param>
        /// <returns>Returns true if the current tutorial matches the key, otherwise returns false.</returns>
        public bool IsCurrentTutorial(ExampleTutorial tutorialType, int actIndex) => CurrentTutorial == tutorialType && tutorialActIndex == actIndex;

        /// <summary>
        /// Starts a tutorial.
        /// </summary>
        /// <param name="key">Key of the tutorial to start</param>
        public void StartTutorial(ExampleTutorial tutorialType)
        {
            inTutorial = true;
            CurrentTutorial = tutorialType;
            tutorialActIndex = 0;
            CurrentTutorial[0]?.Invoke(reference);
        }

        /// <summary>
        /// Cancels the current tutorial.
        /// </summary>
        public void CancelTutorial()
        {
            if (!inTutorial || CurrentTutorial == default || CurrentTutorial == ExampleTutorial.NONE)
                return;

            CurrentTutorial = ExampleTutorial.NONE;
            onTutorialEnd?.Invoke();
            inTutorial = false;
        }

        #endregion

        #region JSON Functions

        public override void RegisterFunctions()
        {
            functions = new Functions();
            functions.LoadCustomJSONFunctions("companion/tutorials/functions.json");
        }

        public override Dictionary<string, JSONNode> GetVariables() => new Dictionary<string, JSONNode>();

        public class Functions : JSONFunctionParser<ExampleTutorials>
        {
            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, ExampleTutorials thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, ExampleTutorials thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, ExampleTutorials thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }

        #endregion
    }
}
