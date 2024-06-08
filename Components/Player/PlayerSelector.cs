using BetterLegacy.Editor.Managers;
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
            EditorManager.inst.ShowDialog("Player Editor New");
            StartCoroutine(PlayerEditor.inst.RefreshEditor());
        }
    }
}
