using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Components.Editor
{
    public class NoteCloseDelete : MonoBehaviour
    {
        public GameObject delete;
        public GameObject close;

        public void Init(GameObject delete, GameObject close)
        {
            this.delete = delete;
            this.close = close;
        }

        void Update()
        {
            delete?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Intro && ProjectPlannerManager.inst.CurrentTab == 5);
            close?.SetActive(EditorManager.inst.editorState == EditorManager.EditorState.Main || ProjectPlannerManager.inst.CurrentTab != 5);
        }
    }
}
