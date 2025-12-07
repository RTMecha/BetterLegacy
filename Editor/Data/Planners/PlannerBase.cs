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
    public abstract class PlannerBase : Exists
    {
        public PlannerBase() { }

        public string ID { get; set; } = LSText.randomNumString(10);

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

        public GameObject SelectedUI { get; set; }

        public abstract Type PlannerType { get; }

        public enum Type
        {
            Document,
            TODO,
            Character,
            Timeline,
            Schedule,
            Note,
            OST
        }

        /// <summary>
        /// Initializes the planner item.
        /// </summary>
        public abstract void Init();

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

        public abstract void ReadJSON(JSONNode jn);

        public abstract JSONNode ToJSON();

        public abstract bool SamePlanner(PlannerBase other);
    }

    public abstract class PlannerBase<T> : PlannerBase where T : PlannerBase<T>, new()
    {
        public PlannerBase() { }

        public static T Parse(JSONNode jn)
        {
            var obj = new T();
            obj.ReadJSON(jn);
            return obj;
        }

        public abstract T CreateCopy();
    }
}
