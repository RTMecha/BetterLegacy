using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Displays certain icons depending on what is active.
    /// </summary>
    public class EditorIndicators : MonoBehaviour
    {
        #region Values

        /// <summary>
        /// The <see cref="EditorIndicators"/> global instance.
        /// </summary>
        public static EditorIndicators inst;

        /// <summary>
        /// List of indicators.
        /// </summary>
        public List<Indicator> indicators = new List<Indicator>();

        /// <summary>
        /// Indicators parent.
        /// </summary>
        public RectTransform indicatorParent;

        #endregion

        #region Functions

        void Awake()
        {
            var parent = new LayoutGroupElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildAlignment(TextAnchor.UpperRight).ChildControlWidth(false).ChildForceExpandWidth(false));
            parent.Init(EditorElement.InitSettings.Default.Name("Indicators").Parent(RTEditor.inst.titleBar.parent).SiblingIndex(4).Rect(RectValues.Default
                .AnchoredPosition(194f, 0f)
                .AnchorMax(0.5f, 1f)
                .AnchorMin(0.5f, 1f)
                .Pivot(1f, 1f)
                .SizeDelta(400f, 32f)));
            indicatorParent = parent.GameObject.transform.AsRT();
            AddIndicator(new Indicator(SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/camera.png")), "Freecam Indicator", () => RTEditor.inst.Freecam));
            AddIndicator(new Indicator(SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/flag_start.png")), "Marker Looping Indicator", () => RTMarkerEditor.inst.markerLooping));
            AddIndicator(new Indicator(SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/no_pointer.png")), "Disabled Preview Selection Indicator", () => !EditorConfig.Instance.SelectObjectsInPreview.Value));
        }

        void Update()
        {
            for (int i = 0; i < indicators.Count; i++)
                indicators[i].Tick();
        }

        /// <summary>
        /// Adds an indicator to the indicators list.
        /// </summary>
        /// <param name="indicator">Indicator to add.</param>
        public void AddIndicator(Indicator indicator)
        {
            if (!indicator || !indicator.icon)
                return;

            indicator.gameObject = Creator.NewUIObject("indicator", indicatorParent);
            indicator.image = indicator.gameObject.AddComponent<Image>();
            indicator.image.sprite = indicator.icon;
            EditorThemeManager.ApplyGraphic(indicator.image, ThemeGroup.Light_Text);
            RectValues.Default.SizeDelta(32f, 32f).AssignToRectTransform(indicator.image.rectTransform);
            if (!string.IsNullOrEmpty(indicator.tooltipGroup))
                TooltipHelper.AssignTooltip(indicator.gameObject, indicator.tooltipGroup);
            indicator.gameObject.SetActive(false);
            indicators.Add(indicator);
        }

        #endregion

        #region Sub Classes

        public class Indicator : Exists
        {
            #region Constructors

            public Indicator() { }

            public Indicator(Sprite icon, Func<bool> predicate)
            {
                this.icon = icon;
                this.predicate = predicate;
            }

            public Indicator(Sprite icon, string tooltipGroup, Func<bool> predicate)
            {
                this.icon = icon;
                this.tooltipGroup = tooltipGroup;
                this.predicate = predicate;
            }

            #endregion

            #region Values

            /// <summary>
            /// Game Object reference.
            /// </summary>
            public GameObject gameObject;

            /// <summary>
            /// Image reference.
            /// </summary>
            public Image image;

            /// <summary>
            /// Function for if <see cref="gameObject"/> should be active.
            /// </summary>
            public Func<bool> predicate;

            /// <summary>
            /// Icon to display.
            /// </summary>
            public Sprite icon;

            /// <summary>
            /// Tooltip group for the element.
            /// </summary>
            public string tooltipGroup;

            bool prevActive;

            #endregion

            #region Functions

            /// <summary>
            /// Ticks the editor indicator.
            /// </summary>
            public virtual void Tick()
            {
                if (!gameObject)
                    return;

                var active = predicate?.Invoke() ?? false;
                if (prevActive == active)
                    return;
                prevActive = active;
                gameObject.SetActive(active);
            }

            #endregion
        }

        #endregion
    }
}
