using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data.Planners
{
    /// <summary>
    /// Base Planner class.
    /// </summary>
    public abstract class PlannerBase : Exists, ISelectable
    {
        public PlannerBase() { }

        #region Values

        /// <summary>
        /// Type of the planner item.
        /// </summary>
        public abstract Type PlannerType { get; }

        /// <summary>
        /// Identification of the planner item.
        /// </summary>
        public string ID { get; set; } = LSText.randomNumString(10);

        /// <summary>
        /// Unity Game Object of the planner item.
        /// </summary>
        public GameObject GameObject { get; set; }

        bool selected;
        /// <summary>
        /// If the planner is selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                if (SelectedUI)
                    SelectedUI.SetActive(selected);
            }
        }

        /// <summary>
        /// Selected UI display.
        /// </summary>
        public GameObject SelectedUI { get; set; }

        /// <summary>
        /// Type of a planner item.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Folder that contains planners.
            /// </summary>
            Folder,
            /// <summary>
            /// Document planner.
            /// </summary>
            Document,
            /// <summary>
            /// TODO planner.
            /// </summary>
            TODO,
            /// <summary>
            /// Character planner.
            /// </summary>
            Character,
            /// <summary>
            /// Timeline planner.
            /// </summary>
            Timeline,
            /// <summary>
            /// Schedule planner.
            /// </summary>
            Schedule,
            /// <summary>
            /// Note planner
            /// </summary>
            Note,
            /// <summary>
            /// OST planner.
            /// </summary>
            OST,
        }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the planner item.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Renders the planner item.
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Initializes the selected UI.
        /// </summary>
        public void InitSelectedUI()
        {
            if (!GameObject)
                return;

            SelectedUI = Creator.NewUIObject("selected", GameObject.transform);
            SelectedUI.SetActive(false);
            var selectedImage = SelectedUI.AddComponent<Image>();
            selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);
        }

        /// <summary>
        /// Reads planner item data from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public abstract void ReadJSON(JSONNode jn);
        
        /// <summary>
        /// Converts the planner item to JSON.
        /// </summary>
        /// <returns>Returns a <see cref="JSONNode"/> representing the item.</returns>
        public abstract JSONNode ToJSON();

        /// <summary>
        /// Checks if the planner is the same as another, thus cannot be added to the planner list.
        /// </summary>
        /// <param name="other">Other planner item to check.</param>
        /// <returns>Returns <see langword="true"/> if the current planner item is the same as <paramref name="other"/>, otherwise returns <see langword="false"/>.</returns>
        public abstract bool SamePlanner(PlannerBase other);

        #endregion
    }

    /// <summary>
    /// Base Planner class.
    /// </summary>
    public abstract class PlannerBase<T> : PlannerBase where T : PlannerBase<T>, new()
    {
        public PlannerBase() { }

        #region Functions

        /// <summary>
        /// Parses a <typeparamref name="T"/> from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <typeparamref name="T"/>.</returns>
        public static T Parse(JSONNode jn)
        {
            var obj = new T();
            obj.ReadJSON(jn);
            return obj;
        }

        /// <summary>
        /// Creates a copy of the current <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Returns a copy of the current <typeparamref name="T"/> with the same data values.</returns>
        public abstract T CreateCopy();

        #endregion
    }
}
