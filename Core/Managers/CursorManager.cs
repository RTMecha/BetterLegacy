using LSFunctions;

using UnityEngine;

namespace BetterLegacy.Core.Managers
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager inst;

        public float OnScreenTime { get; set; } = 1f;
        float onScreenTime;

        public bool Enabled { get; set; } = true;

        public bool canShowCursor;
        public bool canEnableCursor = true;
        public Vector2 cursorPosition = Vector2.zero;
        bool initCursorPosition;

        float time;
        float timeOffset;

        public static Vector2 CursorPosition => new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        public static void Init() => new GameObject("CursorManager", typeof(CursorManager));

        void Awake()
        {
            inst = this;
        }

        void Update()
        {
            time = Time.time - timeOffset;

            if (!Enabled)
                return;

            if (!initCursorPosition || cursorPosition != CursorPosition)
            {
                if (initCursorPosition)
                    ShowCursor(OnScreenTime);

                initCursorPosition = true;
                cursorPosition = CursorPosition;
            }

            if (canShowCursor && onScreenTime < time)
            {
                LSHelpers.HideCursor();
                canShowCursor = false;
            }
        }

        public void ShowCursor(float onScreenTime)
        {
            LSHelpers.ShowCursor();
            timeOffset = Time.time;
            this.onScreenTime = onScreenTime;
            canShowCursor = true;
        }
    }
}
