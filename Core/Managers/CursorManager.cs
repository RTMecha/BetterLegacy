using System;
using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manager class for handling the cursor.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="CursorManager"/> global instance reference.
        /// </summary>
        public static CursorManager inst;

        /// <summary>
        /// Initializes <see cref="CursorManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(CursorManager), SystemManager.inst.transform).AddComponent<CursorManager>();

        void Awake() => inst = this;

        void Update()
        {
            time = Time.time - timeOffset;

            if (CoreHelper.IsEditing && CoreConfig.Instance.EditorCursorAlwaysVisible.Value) // cursor should always be visible in the editor
            {
                Cursor.visible = true;
                return;
            }

            if (CoreHelper.InEditorPreview && !ConfigManager.inst.Active && !InterfaceManager.inst.CurrentInterface && !CoreConfig.Instance.GameCursorCanShow.Value) // cursor should never be visible in the game / editor preview
            {
                Cursor.visible = false;
                return;
            }

            if (!Enabled)
                return;

            var cursorMoved = cursorPosition != CursorPosition;
            if (!initCursorPosition || cursorMoved)
            {
                if (initCursorPosition)
                    ShowCursor();

                initCursorPosition = true;
                cursorPosition = CursorPosition;

                try
                {
                    if (cursorMoved)
                        onCursorMoved?.Invoke(CursorPosition);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            if (cursorEnabled && onScreenTime < time)
                HideCursor();
        }

        #endregion

        #region Properties

        /// <summary>
        /// If the <see cref="CursorManager"/> state is active.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Position of the cursor.
        /// </summary>
        public static Vector2 CursorPosition => new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        #endregion

        #region Fields

        /// <summary>
        /// Changed cursor position.
        /// </summary>
        public Vector2 cursorPosition = Vector2.zero;

        /// <summary>
        /// Function fired when cursor moves.
        /// </summary>
        public Action<Vector2> onCursorMoved;

        /// <summary>
        /// How long the cursor should be visible for.
        /// </summary>
        public static float onScreenTime = 1f;

        #region Internal

        bool cursorEnabled;
        bool initCursorPosition;
        float time;
        float timeOffset;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Shows the cursor temporarily.
        /// </summary>
        public void ShowCursor()
        {
            LSHelpers.ShowCursor();
            timeOffset = Time.time;
            cursorEnabled = true;
        }

        /// <summary>
        /// Shows the cursor with a set on screen time length.
        /// </summary>
        /// <param name="onScreenTime">On screen time the cursor is visible for.</param>
        public void ShowCursor(float time)
        {
            onScreenTime = time;
            ShowCursor();
        }

        /// <summary>
        /// Hides the cursor temporarily.
        /// </summary>
        public void HideCursor()
        {
            LSHelpers.HideCursor();
            cursorEnabled = false;
        }

        /// <summary>
        /// Disables the automatic cursor updater and shows the cursor.
        /// </summary>
        public void PermaShowCursor()
        {
            Enabled = false;
            ShowCursor();
        }

        /// <summary>
        /// Disables the automatic cursor updater and hides the cursor.
        /// </summary>
        public void PermaHideCursor()
        {
            Enabled = false;
            HideCursor();
        }

        /// <summary>
        /// Sets the cursors' position.
        /// </summary>
        /// <param name="pos">Position to set.</param>
        public void SetCursorPosition(Vector2 pos) => System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)pos.x, (int)pos.y);

        #endregion
    }
}
