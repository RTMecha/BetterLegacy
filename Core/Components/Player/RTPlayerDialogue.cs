using System.Collections.Generic;

using UnityEngine;

using TMPro;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

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

            var gameObject = Creator.NewGameObject("Dialogue", runtimePlayer.gameObject.transform);
            runtimePlayer.dialogue = gameObject.AddComponent<RTPlayerDialogue>();
            runtimePlayer.dialogue.runtimePlayer = runtimePlayer;
        }

        void Awake()
        {
            // the object this component is on is treated as the parent.

            canvas = UIManager.GenerateUICanvas("Dialogue Canvas", gameObject.transform);

            for (int i = 0; i < styles.Length; i++)
            {
                var style = styles[i];
                var box = Creator.NewUIObject($"Dialogue Box [{style}]", canvas.GameObject.transform);
                var text = UIManager.GenerateTextMeshPro("Text", box.transform);

                switch (style)
                {
                    case PlayerDialogueStyle.Legacy: {
                            // setup Legacy style...
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

        public List<BoxStyle> boxStyles = new List<BoxStyle>();

        /// <summary>
        /// The current dialogue style. Can be set per player model.
        /// </summary>
        public PlayerDialogueStyle currentStyle;

        public RectValues boxRect = new RectValues(new Vector2(0f, 0f), RectValues.CenterPivot, RectValues.CenterPivot, RectValues.CenterPivot, new Vector2(100f, 100f));
        public RectValues textRect = new RectValues(new Vector2(0f, 0f), RectValues.CenterPivot, RectValues.CenterPivot, RectValues.CenterPivot, new Vector2(100f, 100f));

        UICanvas canvas;
        RTPlayer runtimePlayer;

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
        public class BoxStyle
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
