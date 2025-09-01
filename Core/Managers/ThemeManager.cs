using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages themes.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="ThemeManager"/> global instance reference.
        /// </summary>
        public static ThemeManager inst;

        /// <summary>
        /// Initializes <see cref="ThemeManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(ThemeManager), SystemManager.inst.transform).AddComponent<ThemeManager>();

        void Awake()
        {
            inst = this;

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"default_themes{FileFormat.LST.Dot()}")));
            for (int i = 0; i < jn["themes"].Count; i++)
            {
                var beatmapTheme = BeatmapTheme.Parse(jn["themes"][i]);
                beatmapTheme.isDefault = true;
                defaultThemes.Add(beatmapTheme);
            }

            Current = defaultThemes[0].Copy(false);

            UpdateAllThemes();
        }

        #endregion

        #region Themes

        public BeatmapTheme Current { get; set; }

        public int ThemeCount => defaultThemes.Count + GameData.Current.beatmapThemes.Count;

        public List<BeatmapTheme> AllThemes { get; set; }

        public List<BeatmapTheme> defaultThemes = new List<BeatmapTheme>();
        public List<BeatmapTheme> DefaultThemes
        {
            get => defaultThemes;
            set
            {
                defaultThemes = value;
                UpdateAllThemes();
            }
        }

        public Color bgColorToLerp;
        public Color timelineColorToLerp;

        /// <summary>
        /// Gets a theme based on the ID.
        /// </summary>
        /// <param name="id">Finds the theme with the matching ID.</param>
        /// <returns>Returns the current theme.</returns>
        public BeatmapTheme GetTheme(string id) => AllThemes.TryFind(x => x.id == id, out BeatmapTheme beatmapTheme) ? beatmapTheme : DefaultThemes[0];

        /// <summary>
        /// Gets a theme based on the ID.
        /// </summary>
        /// <param name="id">Finds the theme with the matching ID.</param>
        /// <returns>Returns the current theme.</returns>
        public BeatmapTheme GetTheme(int id) => AllThemes.TryFind(x => Parser.TryParse(x.id, 0) == id, out BeatmapTheme beatmapTheme) ? beatmapTheme : DefaultThemes[0];

        public void UpdateAllThemes() => AllThemes = !GameData.Current || GameData.Current.beatmapThemes.IsEmpty() ? defaultThemes : defaultThemes.Union(GameData.Current.beatmapThemes).ToList();

        public void UpdateThemes()
        {
            if (RTGameManager.inst && EventManager.inst)
            {
                EventManager.inst.camPer.backgroundColor = bgColorToLerp;
                if (RTGameManager.inst.checkpointImages.Count > 0)
                    foreach (var image in RTGameManager.inst.checkpointImages)
                        image.color = timelineColorToLerp;

                RTGameManager.inst.timelinePlayer.color = timelineColorToLerp;
                RTGameManager.inst.timelineLeftCap.color = timelineColorToLerp;
                RTGameManager.inst.timelineRightCap.color = timelineColorToLerp;
                RTGameManager.inst.timelineLine.color = timelineColorToLerp;
            }

            if (!CoreHelper.InEditor && AudioManager.inst.CurrentAudioSource.time < 15f)
            {
                bool introActive = GameData.Current && GameData.Current.data && GameData.Current.data.level && !GameData.Current.data.level.hideIntro;

                GameManager.inst.introTitle.gameObject.SetActive(introActive);
                GameManager.inst.introArtist.gameObject.SetActive(introActive);
                if (introActive && GameManager.inst.introTitle.color != timelineColorToLerp)
                    GameManager.inst.introTitle.color = timelineColorToLerp;
                if (introActive && GameManager.inst.introArtist.color != timelineColorToLerp)
                    GameManager.inst.introArtist.color = timelineColorToLerp;
            }

            if (GameManager.inst.guiImages.Length > 0)
                foreach (var image in GameManager.inst.guiImages)
                    image.color = timelineColorToLerp;
        }

        #endregion
    }
}
