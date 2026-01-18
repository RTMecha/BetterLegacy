using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Elements;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ProgressInterface : InterfaceBase
    {
        public static ProgressInterface Current { get; set; }

        public ProgressInterface(string currentMessage) : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Progress";

            if (!ProjectArrhythmia.State.InGame)
                elements.Add(new MenuEvent
                {
                    id = "09",
                    name = "Effects",
                    func = MenuEffectsManager.inst.SetDefaultEffects,
                    length = 0f,
                    wait = false,
                });

            if (ProjectArrhythmia.State.InGame)
                elements.Add(new MenuImage
                {
                    id = "35255236785",
                    name = "Background",
                    siblingIndex = 0,
                    rect = RectValues.FullAnchored,
                    color = 0,
                    val = -999f,
                    opacity = 0.7f,
                    length = 0f,
                    wait = false,
                });

            elements.Add(new MenuText
            {
                id = "0",
                name = "message",
                text = $"<align=center>{currentMessage}",
                rect = RectValues.Default,
                hideBG = true,
                color = 0,
                opacity = 1f,
                val = ProjectArrhythmia.State.InGame ? 40f : 0f,
                textColor = ProjectArrhythmia.State.InGame ? 0 : 6,
                textVal = ProjectArrhythmia.State.InGame ? 40f : 0f,
            });

            elements.Add(new MenuImage
            {
                id = "1",
                name = "progress base",
                rect = RectValues.Default.AnchoredPosition(0f, -100f).SizeDelta(900f, 64f),
                color = ProjectArrhythmia.State.InGame ? 0 : 6,
                opacity = 0.1f,
                val = ProjectArrhythmia.State.InGame ? 40f : 0f,
                length = 0f,
                wait = false,
            });
            progressBar = new MenuImage
            {
                id = "2",
                name = "progress",
                parent = "1",
                rect = RectValues.Default.AnchorMax(0f, 0.5f).AnchorMin(0f, 0.5f).Pivot(0f, 0.5f).SizeDelta(0f, 64f),
                color = ProjectArrhythmia.State.InGame ? 0 : 6,
                opacity = 1f,
                val = ProjectArrhythmia.State.InGame ? 40f : 0f,
                length = 0f,
                wait = false,
            };
            elements.Add(progressBar);

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar && progressBar.gameObject)
                progressBar.gameObject.transform.AsRT().sizeDelta = new UnityEngine.Vector2(900f * progress, 64f);
        }

        public MenuImage progressBar;

        public static void Init(string currentMessage) => Current = new ProgressInterface(currentMessage);

        public override void UpdateTheme()
        {
            if (ProjectArrhythmia.State.InGame)
                Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }
    }
}
