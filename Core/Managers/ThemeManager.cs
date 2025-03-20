using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

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
                defaultThemes.Add(BeatmapTheme.Parse(jn["themes"][i]));

            Current = BeatmapTheme.DeepCopy(defaultThemes[0]);

            UpdateAllThemes();
        }

        #endregion

        #region Themes

        public BeatmapTheme Current { get; set; }

        public List<string> themeIDs = new List<string>();

        public int ThemeCount => defaultThemes.Count + customThemes.Count;

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

        public List<BeatmapTheme> customThemes = new List<BeatmapTheme>();
        public List<BeatmapTheme> CustomThemes
        {
            get => customThemes;
            set
            {
                customThemes = value;
                UpdateAllThemes();
            }
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

        public void UpdateAllThemes() => AllThemes = customThemes.IsEmpty() ? defaultThemes : defaultThemes.Union(customThemes).ToList();

        public void AddTheme(BeatmapTheme beatmapTheme, Action<BeatmapTheme> onLoaded = null, Action<BeatmapTheme> onDuplicateFound = null)
        {
            CustomThemes.Add(beatmapTheme);

            if (!themeIDs.Contains(beatmapTheme.id))
            {
                onLoaded?.Invoke(beatmapTheme);

                themeIDs.Add(beatmapTheme.id);
            }
            else
            {
                onDuplicateFound?.Invoke(beatmapTheme);

                var list = CustomThemes.Where(x => x.id == beatmapTheme.id).ToList();
                var str = "";
                for (int j = 0; j < list.Count; j++)
                {
                    str += list[j].name;
                    if (j != list.Count - 1)
                        str += ", ";
                }

                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification($"Unable to Load theme [{beatmapTheme.name}] due to conflicting themes: {str}", 2f, EditorManager.NotificationType.Error);
            }
        }

        public void Clear()
        {
            customThemes.Clear();
            themeIDs.Clear();
        }

        #endregion
    }
}
