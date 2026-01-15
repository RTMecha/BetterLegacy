using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages themes.
    /// </summary>
    public class ThemeManager : BaseManager<ThemeManager, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// The current interpolated theme.
        /// </summary>
        public BeatmapTheme Current { get; set; }

        /// <summary>
        /// Total amount of themes.
        /// </summary>
        public int ThemeCount => defaultThemes.Count + GameData.Current.beatmapThemes.Count;

        /// <summary>
        /// All themes.
        /// </summary>
        public List<BeatmapTheme> AllThemes { get; set; }

        /// <summary>
        /// List of default themes.
        /// </summary>
        public List<BeatmapTheme> defaultThemes = new List<BeatmapTheme>();

        /// <summary>
        /// List of default themes.
        /// </summary>
        public List<BeatmapTheme> DefaultThemes
        {
            get => defaultThemes;
            set
            {
                defaultThemes = value;
                UpdateAllThemes();
            }
        }

        /// <summary>
        /// Lerped color for the background.
        /// </summary>
        public Color bgColorToLerp;

        /// <summary>
        /// Lerped color for the timeline GUI.
        /// </summary>
        public Color timelineColorToLerp;

        #endregion

        #region Functions

        public override void OnInit()
        {
            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"builtin/default_themes{FileFormat.LST.Dot()}")));
            for (int i = 0; i < jn["themes"].Count; i++)
            {
                var beatmapTheme = BeatmapTheme.Parse(jn["themes"][i]);
                beatmapTheme.isDefault = true;
                defaultThemes.Add(beatmapTheme);
            }

            Current = defaultThemes[0].Copy(false);

            UpdateAllThemes();
        }

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

        /// <summary>
        /// Updates <see cref="AllThemes"/> to include both default themes and custom themes.
        /// </summary>
        public void UpdateAllThemes() => AllThemes = !GameData.Current || GameData.Current.beatmapThemes.IsEmpty() ? defaultThemes : defaultThemes.Union(GameData.Current.beatmapThemes).ToList();

        /// <summary>
        /// Updates misc elements to use the current theme colors.
        /// </summary>
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

            if (!ProjectArrhythmia.State.InEditor && AudioManager.inst.CurrentAudioSource.time < 15f)
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
