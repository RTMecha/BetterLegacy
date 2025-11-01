using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Achievement prefab storage.
    /// </summary>
    public class AchievementStorage : MonoBehaviour
    {
        [SerializeField]
        public Image back;

        [SerializeField]
        public Image iconBase;

        [SerializeField]
        public Image icon;

        [SerializeField]
        public Text title;

        [SerializeField]
        public Text description;

        [SerializeField]
        public Image difficulty;
    }

}
