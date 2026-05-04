using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    public class SelectBackgroundObject : MonoBehaviour
    {
        #region Values

        bool hovered;
        public BackgroundObject backgroundObject;

        #endregion

        #region Functions

        void OnDisable()
        {
            hovered = false; // set hovered off so when the object re-enables it won't think it's still hovered.
        }

        void OnMouseDown()
        {
            if (!ProjectArrhythmia.State.IsEditing || CoreHelper.IsUsingInputField || EventSystem.current.IsPointerOverGameObject() || RTMarkerEditor.inst && RTMarkerEditor.inst.Dialog.IsCurrent && RTMarkerEditor.inst.Settings.tool != AnnotationTool.None)
                return;

            // don't drag object if Example is being dragged.
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.Dragging)
                return;

            if (SelectObject.clicked)
                return;

            EditorTimeline.inst.SelectObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
        }

        void OnMouseEnter()
        {
            hovered = true;
        }

        void OnMouseExit()
        {
            hovered = false;
        }

        void Update()
        {
            if (!ProjectArrhythmia.State.IsEditing)
            {
                hovered = false;
                return;
            }

            if (!backgroundObject)
                return;

            if (!EventSystem.current.IsPointerOverGameObject())
                Highlight(SelectObject.HighlightObjects && hovered);
        }

        void Highlight(bool highlight)
        {
            if (!highlight || !backgroundObject || !backgroundObject.runtimeObject)
                return;
            var runtimeObject = backgroundObject.runtimeObject;
            var mainColor = runtimeObject.mainColor;
            var fadeColor = runtimeObject.fadeColor;
            mainColor += SelectObject.Highlight(mainColor);
            fadeColor += SelectObject.Highlight(fadeColor);
            runtimeObject.SetColor(mainColor, fadeColor);
        }

        #endregion
    }
}
