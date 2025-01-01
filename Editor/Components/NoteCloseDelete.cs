using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Sets the close / delete buttons active / inactive depending on the state of Project Planner.
    /// </summary>
    public class NoteCloseDelete : MonoBehaviour
    {
        public GameObject delete;
        public GameObject close;

        /// <summary>
        /// Assigns Delete and Close buttons.
        /// </summary>
        /// <param name="delete">Delete to assign.</param>
        /// <param name="close">Close to assign.</param>
        public void Init(GameObject delete, GameObject close)
        {
            this.delete = delete;
            this.close = close;
        }

        void Update()
        {
            // Delete should only be active when you're looking at the Note tab of the Project Planner.
            delete?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Intro && ProjectPlannerManager.inst.CurrentTab == 5);
            // Close should appear every BUT the Note tab.
            close?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Main || ProjectPlannerManager.inst.CurrentTab != 5);
        }
    }
}
