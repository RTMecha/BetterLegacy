using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Class for displaying debug information.
    /// </summary>
    public class DebugInfo
    {
        #region Constructors

        public DebugInfo() { }

        public DebugInfo(string name, Func<string> getText)
        {
            this.name = name;
            this.getText = getText;
        }
        
        public DebugInfo(string name, Func<string> getText, Func<bool> shouldDisplay)
        {
            this.name = name;
            this.getText = getText;
            this.shouldDisplay = shouldDisplay;
        }

        #endregion

        #region Values

        /// <summary>
        /// Back image.
        /// </summary>
        public Image image;

        /// <summary>
        /// UI text element.
        /// </summary>
        public TextMeshProUGUI text;

        /// <summary>
        /// Name of the info.
        /// </summary>
        public string name;

        /// <summary>
        /// Info function.
        /// </summary>
        public Func<string> getText;

        /// <summary>
        /// If the element should display.
        /// </summary>
        public Func<bool> shouldDisplay;

        /// <summary>
        /// Global info dragging.
        /// </summary>
        public static DraggableUI InfoSelection { get; set; }

        /// <summary>
        /// If the debug info screen has initialized.
        /// </summary>
        public static bool init;

        /// <summary>
        /// UI canvas reference.
        /// </summary>
        public static UICanvas canvas;

        /// <summary>
        /// Info area reference.
        /// </summary>
        public static GameObject infoArea;

        /// <summary>
        /// Info area layout reference.
        /// </summary>
        public static VerticalLayoutGroup infoAreaLayout;

        public static ContentSizeFitter infoAreaSizeFitter;

        /// <summary>
        /// List of debug information to display.
        /// </summary>
        public static List<DebugInfo> infos = new List<DebugInfo>();

        static GameObject infoPrefab;

        /// <summary>
        /// The current display anchor.
        /// </summary>
        public static DisplayAnchor currentDisplayAnchor;

        /// <summary>
        /// How the debug info should display.
        /// </summary>
        public enum DisplayAnchor
        {
            /// <summary>
            /// The debug info can be dragged around.
            /// </summary>
            Free,
            /// <summary>
            /// The debug info is locked to the top left of the screen.
            /// </summary>
            TopLeft,
            /// <summary>
            /// The debug info is locked to the top right of the screen.
            /// </summary>
            TopRight,
            /// <summary>
            /// The debug info is locked to the bottom left of the screen.
            /// </summary>
            BottomLeft,
            /// <summary>
            /// The debug info is locked to the bottom right of the screen.
            /// </summary>
            BottomRight,
        }

        const float INFO_WIDTH_NULTIPLY = 18f;

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the debug info.
        /// </summary>
        public static void Init()
        {
            if (init)
                return;

            canvas = UIManager.GenerateUICanvas("Debug Info Canvas", null, true, 13000);

            infoArea = Creator.NewUIObject("Info Area", canvas.GameObject.transform);
            //var infoAreaImage = infoArea.AddComponent<Image>();
            InfoSelection = infoArea.AddComponent<DraggableUI>();
            InfoSelection.mode = DraggableUI.DragMode.RequiredDrag;
            InfoSelection.draggingAction = vector2 => CoreConfig.Instance.DebugPosition.Value = vector2;
            InfoSelection.target = infoArea.transform.AsRT();
            infoAreaLayout = infoArea.AddComponent<VerticalLayoutGroup>();
            infoAreaLayout.childControlHeight = false;
            infoAreaLayout.childControlWidth = false;
            infoAreaLayout.childForceExpandHeight = false;
            infoAreaSizeFitter = infoArea.AddComponent<ContentSizeFitter>();
            infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            infoPrefab = Creator.NewUIObject("Info", canvas.GameObject.transform);
            var infoPrefabImage = infoPrefab.AddComponent<Image>();
            infoPrefabImage.color = RTColors.HexToColor("00000040");
            var infoPrefabText = Creator.NewUIObject("Text", infoPrefab.transform).AddComponent<TextMeshProUGUI>();
            RectValues.FullAnchored.AssignToRectTransform(infoPrefabText.rectTransform);
            try
            {
                infoPrefabText.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            infoPrefabText.fontSize = 32;
            infoPrefab.SetActive(false);

            UIManager.SetRectTransform(infoArea.transform.AsRT() , CoreConfig.Instance.DebugPosition.Value, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(800f, 200f));

            infos.Add(new DebugInfo("FPS", () => LegacyPlugin.FPSCounter.Text));
            infos.Add(new DebugInfo("Objects Alive", GetObjectAliveCount, () => GameData.Current && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            infos.Add(new DebugInfo("FG Camera Position", () => RTLevel.Cameras.FG.transform.position.ToString(), () => ProjectArrhythmia.State.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            infos.Add(new DebugInfo("FG Camera Zoom", () => RTLevel.Cameras.FG.orthographicSize.ToString(), () => ProjectArrhythmia.State.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            infos.Add(new DebugInfo("FG Camera Rotation", () => RTLevel.Cameras.FG.transform.eulerAngles.ToString(), () => ProjectArrhythmia.State.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            infos.Add(new DebugInfo("BG Camera Position", () => RTLevel.Cameras.BG.transform.position.ToString(), () => ProjectArrhythmia.State.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            infos.Add(new DebugInfo("BG Camera Rotation", () => RTLevel.Cameras.BG.transform.eulerAngles.ToString(), () => ProjectArrhythmia.State.InGame && !CoreConfig.Instance.DebugShowOnlyFPS.Value));
            RefreshDebugView();
            SetAnchor(CoreConfig.Instance.DebugDisplayAnchor.Value);

            init = true;
        }

        /// <summary>
        /// Destroys and clears the current debug info.
        /// </summary>
        public static void Destroy()
        {
            CoreHelper.Delete(canvas.GameObject);
            canvas = null;
            infoArea = null;
            infoAreaLayout = null;
            infoAreaSizeFitter = null;
            infoPrefab = null;
            infos.Clear();
            currentDisplayAnchor = DisplayAnchor.Free;
            init = false;
        }

        static string GetObjectAliveCount()
        {
            var list = GameData.Current.beatmapObjects.FindAll(x => x && x.objectType != BeatmapObject.ObjectType.Empty);
            return $"{list.Count(x => x.Alive)} / {list.Count}";
        }

        /// <summary>
        /// Refreshes the debug view.
        /// </summary>
        public static void RefreshDebugView()
        {
            CoreHelper.DestroyChildren(infoArea.transform);
            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var gameObject = infoPrefab.Duplicate(infoArea.transform);
                info.image = gameObject.GetComponent<Image>();
                info.text = gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            }
        }

        /// <summary>
        /// Sets the display anchor for the debug info.
        /// </summary>
        /// <param name="displayAnchor">Display anchor to set.</param>
        public static void SetAnchor(DisplayAnchor displayAnchor)
        {
            if (currentDisplayAnchor == displayAnchor)
                return;

            currentDisplayAnchor = displayAnchor;

            switch (currentDisplayAnchor)
            {
                case DisplayAnchor.Free: {
                        infoAreaLayout.childAlignment = TextAnchor.UpperLeft;
                        infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                        RectValues.Default
                            .AnchoredPosition(CoreConfig.Instance.DebugPosition.Value.x, CoreConfig.Instance.DebugPosition.Value.y)
                            .Pivot(0f, 1f)
                            .SizeDelta(800f, 200f).AssignToRectTransform(infoArea.transform.AsRT());
                        break;
                    }
                case DisplayAnchor.TopLeft: {
                        infoAreaLayout.childAlignment = TextAnchor.UpperLeft;
                        infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        RectValues.FullAnchored.AssignToRectTransform(infoArea.transform.AsRT());
                        break;
                    }
                case DisplayAnchor.TopRight: {
                        infoAreaLayout.childAlignment = TextAnchor.UpperRight;
                        infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        RectValues.FullAnchored.AssignToRectTransform(infoArea.transform.AsRT());
                        break;
                    }
                case DisplayAnchor.BottomLeft: {
                        infoAreaLayout.childAlignment = TextAnchor.LowerLeft;
                        infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        RectValues.FullAnchored.AssignToRectTransform(infoArea.transform.AsRT());
                        break;
                    }
                case DisplayAnchor.BottomRight: {
                        infoAreaLayout.childAlignment = TextAnchor.LowerRight;
                        infoAreaSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                        RectValues.FullAnchored.AssignToRectTransform(infoArea.transform.AsRT());
                        break;
                    }
            }

            InfoSelection.enabled = currentDisplayAnchor == DisplayAnchor.Free;
        }

        /// <summary>
        /// Ticks all debug info elements.
        /// </summary>
        public static void TickAll()
        {
            if (!init)
                return;

            var active = CoreConfig.Instance.ShowDebugInfo.Value && ProjectArrhythmia.State.InGame && !Input.GetKey(CoreConfig.Instance.HideDebugKey.Value) && (!ProjectArrhythmia.State.InEditorPreview || CoreConfig.Instance.ShowDebugInfoInGame.Value);
            infoArea.gameObject.SetActive(active);

            if (!active)
                return;

            if (!InfoSelection.dragging && currentDisplayAnchor == DisplayAnchor.Free)
                infoArea.transform.position = CoreConfig.Instance.DebugPosition.Value;

            for (int i = 0; i < infos.Count; i++)
                infos[i].Tick();

            if (canvas != null)
                canvas.Canvas.scaleFactor = CoreHelper.ScreenScale;
        }

        /// <summary>
        /// Ticks the debug info element.
        /// </summary>
        public void Tick()
        {
            var active = shouldDisplay == null || shouldDisplay.Invoke();
            if (image)
                image.gameObject.SetActive(active);
            if (!active)
                return;
            if (text)
                text.text = GetText();
            if (image)
                image.rectTransform.sizeDelta = new Vector2(text.textInfo.characterCount * INFO_WIDTH_NULTIPLY, 32f);
        }

        /// <summary>
        /// Gets the text to display.
        /// </summary>
        /// <returns>Returns the text to display./returns>
        public virtual string GetText() => $"<b>{name}</b>: {getText?.Invoke() ?? null}";

        public override string ToString() => name;

        #endregion
    }
}
