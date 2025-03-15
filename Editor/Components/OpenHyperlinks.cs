using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

// component from https://gitlab.com/jonnohopkins/tmp-hyperlinks/tree/master and based on changes from dev+

namespace BetterLegacy.Editor.Components
{
    // somewhat based upon the TextMesh Pro example script: TMP_TextSelector_B
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class OpenHyperlinks : MonoBehaviour, IPointerClickHandler
    {
        public bool doesColorChangeOnHover = true;
        public Color hoverColor = new Color(60f / 255f, 120f / 255f, 1f);

        TextMeshProUGUI textMeshPro;
        Canvas canvas;
        Camera camera;

        public bool IsLinkHighlighted => currentLink != -1;
        public bool highlighted;
        public int linkIndex = -1;

        public int currentLink = -1;
        List<Color32[]> originalVertexColors = new List<Color32[]>();

        void Awake()
        {
            textMeshPro = GetComponent<TextMeshProUGUI>();
            canvas = GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        void LateUpdate()
        {
            // is the cursor in the correct region (above the text area) and furthermore, in the link region?
            highlighted = TMP_TextUtilities.IsIntersectingRectTransform(textMeshPro.rectTransform, Input.mousePosition, camera);
            linkIndex = highlighted ? TMP_TextUtilities.FindIntersectingLink(textMeshPro, Input.mousePosition, camera) : -1;

            // Clear previous link selection if one existed.
            if (currentLink != -1 && linkIndex != currentLink)
            {
                SetLinkToColor(currentLink, (linkIdx, vertIdx) => originalVertexColors[linkIdx][vertIdx]);
                originalVertexColors.Clear();
                currentLink = -1;
            }

            // Handle new link selection.
            if (linkIndex != -1 && linkIndex != currentLink)
            {
                currentLink = linkIndex;
                if (doesColorChangeOnHover)
                    originalVertexColors = SetLinkToColor(linkIndex, (_linkIdx, _vertIdx) => hoverColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // was a link clicked?
            if (linkIndex == -1)
                return;

            var linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];

            // open the link id as a url, which is the metadata we added in the text field
            InvokeLinkAction(linkInfo.GetLinkID());
        }

        List<Color32[]> SetLinkToColor(int linkIndex, Func<int, int, Color32> colorForLinkAndVert)
        {
            TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];

            var oldVertColors = new List<Color32[]>(); // store the old character colors

            for (int i = 0; i < linkInfo.linkTextLength; i++)
            { // for each character in the link string
                int characterIndex = linkInfo.linkTextfirstCharacterIndex + i; // the character index into the entire text
                var charInfo = textMeshPro.textInfo.characterInfo[characterIndex];
                int meshIndex = charInfo.materialReferenceIndex; // Get the index of the material / sub text object used by this character.
                int vertexIndex = charInfo.vertexIndex; // Get the index of the first vertex of this character.

                Color32[] vertexColors = textMeshPro.textInfo.meshInfo[meshIndex].colors32; // the colors for this character
                oldVertColors.Add(vertexColors.ToArray());

                if (charInfo.isVisible)
                {
                    vertexColors[vertexIndex + 0] = colorForLinkAndVert(i, vertexIndex + 0);
                    vertexColors[vertexIndex + 1] = colorForLinkAndVert(i, vertexIndex + 1);
                    vertexColors[vertexIndex + 2] = colorForLinkAndVert(i, vertexIndex + 2);
                    vertexColors[vertexIndex + 3] = colorForLinkAndVert(i, vertexIndex + 3);
                }
            }

            // Update Geometry
            textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

            return oldVertColors;
        }

        public void RegisterLink(string link, Action function)
        {
            if (linkActions.TryFindIndex(x => x.link == link, out int index))
                linkActions[index].func = function;
            else
                linkActions.Add(new LinkAction(link, function));
        }

        public void InvokeLinkAction(string link)
        {
            CoreHelper.Log($"Invoke link action: {link}");
            if (linkActions.TryFind(x => x.link == link, out LinkAction linkAction))
                linkAction.func?.Invoke();
            else
                Application.OpenURL(link);
        }

        List<LinkAction> linkActions = new List<LinkAction>();

        public class LinkAction
        {
            public LinkAction(string link, Action func)
            {
                this.link = link;
                this.func = func;
            }

            public string link;

            public Action func;

            public override string ToString() => link;
        }
    }
}
