using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    public class NewLevelPopup : EditorPopup
    {
        public NewLevelPopup() : base(NEW_FILE_POPUP) { }

        public InputField SongPath { get; set; }

        public RectTransform DifficultyContent { get; set; }

        public Text FormatLabel { get; set; }

        public override void Open()
        {
            base.Open();

            // progresses the Create New Level tutorial if it's at the start.
            Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 0);
        }

        public void RenderFormat() => FormatLabel.text = $"{EditorLevelManager.inst.newLevelSettings.songArtist} - {EditorLevelManager.inst.newLevelSettings.songTitle}";
    }
}
