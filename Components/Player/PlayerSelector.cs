using UnityEngine;

namespace BetterLegacy.Components.Player
{
    public class PlayerSelector : MonoBehaviour
    {
        public int id;

        void Awake()
        {
            if (EditorManager.inst == null)
                Destroy(this);
        }

        void OnMouseDown()
        {
            var playerEditor = GameObject.Find("PlayerEditorManager").GetComponentByName("CreativePlayersEditor");

            playerEditor.GetType().GetMethod("OpenDialog").Invoke(playerEditor, new object[] { });
        }
    }
}
