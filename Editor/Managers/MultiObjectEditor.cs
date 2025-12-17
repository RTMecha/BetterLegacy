using System;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class MultiObjectEditor : BaseManager<MultiObjectEditor, EditorManagerSettings>
    {
        #region Values

        public MultiObjectEditorDialog Dialog { get; set; }

        /// <summary>
        /// String to format from.
        /// </summary>
        public const string DEFAULT_TEXT = "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nBG Count: {5}/{6}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new MultiObjectEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public override void OnTick()
        {
            if (!Dialog || !Dialog.Text || !Dialog.Text.isActiveAndEnabled || !GameData.Current)
                return;

            Dialog.Text.text = string.Format(DEFAULT_TEXT,
                EditorTimeline.inst.SelectedBeatmapObjects.Count,
                EditorTimeline.inst.SelectedPrefabObjects.Count,
                EditorTimeline.inst.SelectedObjects.Count,
                GameData.Current.beatmapObjects.Count,
                GameData.Current.prefabObjects.Count,
                EditorTimeline.inst.SelectedBackgroundObjects.Count,
                GameData.Current.backgroundObjects.Count);
        }

        #endregion
    }
}
