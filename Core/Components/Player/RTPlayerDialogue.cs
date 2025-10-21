using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Runtime Player dialogue box component.
    /// </summary>
    public class RTPlayerDialogue : MonoBehaviour
    {
        /* Info
        The player dialogue works the same as in modern, except with more usage.
        Might be able to get it to support asset packs? (extra styles)
        The style can be set per model or from the level. (VG levels use Modern style)

        TODO:
        - Text interpolation
        - Animation styles?
        - Actually implement this feature
         */

        #region Init

        /// <summary>
        /// Initializes the dialogue box onto a runtime player.
        /// </summary>
        /// <param name="runtimePlayer">Runtime player to set the box to.</param>
        public static void Init(RTPlayer runtimePlayer)
        {
            if (runtimePlayer.dialogue)
                CoreHelper.Delete(runtimePlayer.dialogue);

            var canvas = UIManager.GenerateUICanvas("Dialogue Canvas", runtimePlayer.gameObject.transform);
            canvas.SetWorldSpace(RTLevel.UI_LAYER, RTLevel.Cameras.FG);
            canvas.GameObject.transform.localScale = Vector3.one;
            runtimePlayer.dialogue = canvas.GameObject.AddComponent<RTPlayerDialogue>();
            runtimePlayer.dialogue.runtimePlayer = runtimePlayer;
            runtimePlayer.dialogue.canvas = canvas;
            runtimePlayer.dialogue.InternalInit();
        }

        void InternalInit()
        {
            baseObject = Creator.NewUIObject("Base", canvas.GameObject.transform);

            for (int i = 0; i < styles.Length; i++)
            {
                var style = styles[i];
                var box = Creator.NewUIObject($"Dialogue Box [{style}]", baseObject.transform);
                box.transform.AsRT().anchoredPosition = Vector2.zero;
                box.transform.AsRT().anchorMax = new Vector2(0.5f, 0f);
                box.transform.AsRT().anchorMin = new Vector2(0.5f, 0f);
                box.transform.AsRT().pivot = new Vector2(0.5f, 0f);
                box.transform.AsRT().sizeDelta = Vector2.zero;
                var boxImage = box.AddComponent<Image>();
                var verticalLayoutGroup = box.AddComponent<VerticalLayoutGroup>();
                verticalLayoutGroup.childControlHeight = true;
                verticalLayoutGroup.childControlWidth = true;
                verticalLayoutGroup.childForceExpandHeight = false;
                verticalLayoutGroup.childForceExpandWidth = false;
                verticalLayoutGroup.childScaleHeight = false;
                verticalLayoutGroup.childScaleWidth = false;

                var contentSizeFitter = box.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var text = UIManager.GenerateTextMeshPro("Text", box.transform);
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.anchorMax = new Vector2(0f, 1f);
                text.rectTransform.anchorMin = new Vector2(0f, 1f);
                text.rectTransform.pivot = RectValues.CenterPivot;
                text.rectTransform.sizeDelta = Vector2.zero;

                text.autoSizeTextContainer = true;
                text.fontSize = 1.2f;
                //text.fontSize = 20f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;

                switch (style)
                {
                    case PlayerDialogueStyle.Legacy: {
                            boxImage.color = new Color(0f, 0f, 0f, 1f);

                            var border = Creator.NewUIObject("Border", boxImage.transform, 0);
                            var layoutElement = border.AddComponent<LayoutElement>();
                            layoutElement.ignoreLayout = true;
                            RectValues.FullAnchored.AssignToRectTransform(border.transform.AsRT());
                            var borderImage = border.AddComponent<Image>();
                            borderImage.sprite = SpriteHelper.BorderSprite;
                            borderImage.type = Image.Type.Tiled;
                            borderImage.color = Color.white;
                            break;
                        }
                    case PlayerDialogueStyle.Modern: {
                            boxImage.color = new Color(0.0902f, 0.0902f, 0.098f, 0.8f);

                            var border = Creator.NewUIObject("Border", boxImage.transform, 0);
                            var layoutElement = border.AddComponent<LayoutElement>();
                            layoutElement.ignoreLayout = true;
                            RectValues.FullAnchored.AssignToRectTransform(border.transform.AsRT());
                            var borderImage = border.AddComponent<Image>();
                            borderImage.sprite = SpriteHelper.BorderSprite;
                            borderImage.type = Image.Type.Tiled;
                            borderImage.color = Color.white;
                            break;
                        }
                }

                boxStyles.Add(new BoxStyle
                {
                    gameObject = box,
                    textUI = text,
                });

                box.SetActive(false);
            }

            canvas.GameObject.SetActive(false);
        }

        void Update()
        {
            if (runtimePlayer && runtimePlayer.rb && baseObject)
                baseObject.transform.localPosition = runtimePlayer.rb.position + positionOffset;
        }

        #endregion

        #region Values

        /// <summary>
        /// The current text.
        /// </summary>
        public string Text
        {
            get => CurrentBox.textUI.text;
            set => CurrentBox.textUI.text = value;
        }

        /// <summary>
        /// The current dialogue box.
        /// </summary>
        public BoxStyle CurrentBox
        {
            get => boxStyles[(int)currentStyle];
            set => boxStyles[(int)currentStyle] = value;
        }

        /// <summary>
        /// List of dialogue box styles.
        /// </summary>
        public List<BoxStyle> boxStyles = new List<BoxStyle>();

        /// <summary>
        /// The current dialogue style. Can be set per player model.
        /// </summary>
        public PlayerDialogueStyle currentStyle;

        public Vector2 positionOffset = new Vector2(0f, 2f);

        public RectValues boxRect = new RectValues(new Vector2(0f, 0f), RectValues.CenterPivot, RectValues.CenterPivot, RectValues.CenterPivot, new Vector2(100f, 100f));
        public RectValues textRect = new RectValues(new Vector2(0f, 0f), RectValues.CenterPivot, RectValues.CenterPivot, RectValues.CenterPivot, new Vector2(100f, 100f));

        UICanvas canvas;
        RTPlayer runtimePlayer;
        GameObject baseObject;

        static readonly PlayerDialogueStyle[] styles = new PlayerDialogueStyle[]
        {
            PlayerDialogueStyle.Legacy,
            PlayerDialogueStyle.Modern,
            PlayerDialogueStyle.Comic,
        };

        #endregion

        #region Methods

        /// <summary>
        /// Sets the current dialogue box style.
        /// </summary>
        /// <param name="style">Style to set.</param>
        public void SetStyle(PlayerDialogueStyle style)
        {
            if (currentStyle == style)
                return;

            CurrentBox.SetActive(false);
            currentStyle = style;
            CurrentBox.SetActive(true);
        }

        /// <summary>
        /// Shows the dialogue box.
        /// </summary>
        public void Show()
        {
            boxRect.AssignToRectTransform(CurrentBox.gameObject.transform.AsRT());
            textRect.AssignToRectTransform(CurrentBox.textUI.rectTransform);
            canvas.GameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the dialogue box.
        /// </summary>
        public void Hide() => canvas.GameObject.SetActive(false);

        #endregion

        /// <summary>
        /// Represents a dialogue box style.
        /// </summary>
        public class BoxStyle : Exists
        {
            /// <summary>
            /// Game object reference.
            /// </summary>
            public GameObject gameObject;
            /// <summary>
            /// Text Mesh Pro (noob) name reference.
            /// </summary>
            public TextMeshProUGUI nameUI;
            /// <summary>
            /// Text Mesh Pro (noob) text reference.
            /// </summary>
            public TextMeshProUGUI textUI;

            /// <summary>
            /// Sets the box style active.
            /// </summary>
            /// <param name="active">Active state to set.</param>
            public void SetActive(bool active) => gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Style of the players' dialogue box.
    /// </summary>
    public enum PlayerDialogueStyle
    {
        /// <summary>
        /// BetterLegacy style.
        /// </summary>
        Legacy,
        /// <summary>
        /// Modern (VG / alpha) style.
        /// </summary>
        Modern,
        /// <summary>
        /// When the feature was first introduced style.
        /// </summary>
        Comic,
        /// <summary>
        /// Example companion style.
        /// </summary>
        Example,
    }
}
