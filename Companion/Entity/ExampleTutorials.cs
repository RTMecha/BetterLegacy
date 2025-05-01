using System;
using System.Collections.Generic;

using BetterLegacy.Companion.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Companion.Entity
{
    public class ExampleTutorials : ExampleModule
    {
        #region Default Instance

        public ExampleTutorials() { }

        public static Func<ExampleTutorials> getDefault = () =>
        {
            var tutorials = new ExampleTutorials();
            tutorials.InitDefault();

            return tutorials;
        };

        public override void InitDefault()
        {
            RegisterTutorials();
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
            tutorials.Clear();
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
        public ExampleTutorial CurrentTutorial { get; set; }

        /// <summary>
        /// List of the tutorials.
        /// </summary>
        public List<ExampleTutorial> tutorials = new List<ExampleTutorial>();

        /// <summary>
        /// Registers tutorials.
        /// </summary>
        public virtual void RegisterTutorials()
        {
            // this tutorial is just a test.
            tutorials.Add(new ExampleTutorial(Tutorials.CREATE_LEVEL, "Creating a new level", new Action[]
            {
                // start
                // open new level popup
                () => { reference?.chatBubble?.Say("First, go to the New Level Creator popup."); },
                // step 1
                // search for a song to use
                () => { reference?.chatBubble?.Say("Next, search for a song to use."); },
                // step 2
                // name the level
                () => { reference?.chatBubble?.Say("Now name the level."); },
                // step 3
                // name the song
                () => { reference?.chatBubble?.Say("Then name the song."); },
                // step 4
                // create level
                () => { reference?.chatBubble?.Say("And finally, click Create."); },
                // step 5
                // new level has loaded
                () => { reference?.chatBubble?.Say("You just made a level."); },
            }));
        }

        /// <summary>
        /// Advanced the current tutorial.
        /// </summary>
        /// <param name="key">Key of the tutorial to match.</param>
        /// <param name="actIndex">Current action index to check.</param>
        public bool AdvanceTutorial(string key, int actIndex)
        {
            if (IsCurrentTutorial(key, actIndex))
                return AdvanceTutorial();
            return false;
        }

        /// <summary>
        /// Advanced the current tutorial.
        /// </summary>
        /// <returns>Returns true if the tutorial advances, otherwise returns false if the tutorial has ended or there is no current tutorial.</returns>
        public bool AdvanceTutorial()
        {
            if (!inTutorial || !CurrentTutorial)
                return false;

            tutorialActIndex++;
            CoreHelper.Log($"Progress tutorial: {CurrentTutorial} to {tutorialActIndex}");

            if (tutorialActIndex < 0 || tutorialActIndex >= CurrentTutorial.actions.Length)
            {
                CurrentTutorial = null;
                inTutorial = false;
                onTutorialEnd?.Invoke();
                onTutorialEnd = null;
                return false;
            }

            CurrentTutorial[tutorialActIndex]?.Invoke();
            return true;
        }

        /// <summary>
        /// Checks if a tutorial is the current one.
        /// </summary>
        /// <param name="key">Key of the tutorial to match.</param>
        /// <param name="actIndex">Current action index to check.</param>
        /// <returns>Returns true if the current tutorial matches the key, otherwise returns false.</returns>
        public bool IsCurrentTutorial(string key, int actIndex) => CurrentTutorial && CurrentTutorial.key == key && tutorialActIndex == actIndex;

        /// <summary>
        /// Gets a tutorial from the registered list.
        /// </summary>
        /// <param name="key">Key of the tutorial to find.</param>
        /// <returns>Returns a found tutorial.</returns>
        public ExampleTutorial GetTutorial(string key) => tutorials.Find(x => x.key == key);

        /// <summary>
        /// Starts a tutorial.
        /// </summary>
        /// <param name="key">Key of the tutorial to start</param>
        public void StartTutorial(string key)
        {
            var tutorial = GetTutorial(key);
            if (!tutorial)
                return;

            inTutorial = true;
            CurrentTutorial = tutorial;
            tutorialActIndex = 0;
            CurrentTutorial[0]?.Invoke();
        }

        /// <summary>
        /// Cancels the current tutorial.
        /// </summary>
        public void CancelTutorial()
        {
            if (!inTutorial || !CurrentTutorial)
                return;

            CurrentTutorial = null;
            onTutorialEnd?.Invoke();
            inTutorial = false;
        }

        /// <summary>
        /// Library of default tutorials.
        /// </summary>
        public static class Tutorials
        {
            /// <summary>
            /// Tutorial on creating a new level.
            /// </summary>
            public const string CREATE_LEVEL = "CREATE_LEVEL";
        }

        #endregion
    }
}
