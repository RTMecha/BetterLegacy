using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    // TODO: rework / remove
    public class UIManager : BaseManager<UIManager, ManagerSettings>
    {
        public static GameObject textMeshPro;

        public override void OnInit()
        {
            var findButton = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
                              where x.name == "Text Element"
                              select x).ToList();

            textMeshPro = findButton[0].transform.GetChild(1).gameObject;
        }

        /// <summary>
        /// Sets a RectTransforms' main values.
        /// </summary>
        /// <param name="rectTransform">RectTransform to apply to.</param>
        public static void SetRectTransform(RectTransform rectTransform, Vector2 anchoredPos, Vector2 anchorMax, Vector2 anchorMin, Vector2 pivot, Vector2 sizeDelta)
        {
            rectTransform.anchoredPosition = anchoredPos;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchorMin = anchorMin;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
        }

        public static Image GenerateImage(string name, Transform parent)
        {
            var gameObject = Creator.NewUIObject(name, parent);
            gameObject.layer = 5;
            return gameObject.AddComponent<Image>();
        }

        public static Text GenerateText(string name, Transform parent)
        {
            var gameObject = Creator.NewUIObject(name, parent);
            gameObject.layer = 5;

            var text = gameObject.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = 20;

            return text;
        }

        public static TextMeshProUGUI GenerateTextMeshPro(string name, Transform parent)
        {
            var gameObject = textMeshPro.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;

            return gameObject.GetComponent<TextMeshProUGUI>();
        }

        public static InputField GenerateInputField(string name, Transform parent)
        {
            var gameObject = Creator.NewUIObject(name, parent);
            var image = gameObject.AddComponent<Image>();

            var text = Creator.NewUIObject("text", gameObject.transform);
            var inputText = text.AddComponent<Text>();
            inputText.font = Font.GetDefault();
            inputText.fontSize = 20;

            var placeholder = Creator.NewUIObject("placeholder", gameObject.transform);
            var placeholderText = placeholder.AddComponent<Text>();
            placeholderText.font = Font.GetDefault();
            placeholderText.fontSize = 20;

            SetRectTransform(inputText.rectTransform, new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));
            SetRectTransform(placeholderText.rectTransform, new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

            var inputField = gameObject.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.image = image;
            return inputField;
        }

        public static Prefabs.DropdownStorage GenerateDropdown(string name, Transform parent)
        {
            var dropdownBase = Creator.NewUIObject(name, parent);
            var dropdownStorage = dropdownBase.AddComponent<Prefabs.DropdownStorage>();
            dropdownStorage.hideOptions = dropdownBase.AddComponent<HideDropdownOptions>();

            var dropdownImage = dropdownBase.AddComponent<Image>();

            var dropdown = dropdownBase.AddComponent<Dropdown>();
            dropdownStorage.dropdown = dropdown;

            var labelText = GenerateText("Label", dropdownBase.transform);
            labelText.font = Font.GetDefault();
            labelText.fontSize = 20;
            labelText.color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);
            labelText.alignment = TextAnchor.MiddleLeft;

            var arrowImage = GenerateImage("Arrow", dropdownBase.transform);
            arrowImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/left.png"));
            arrowImage.color = new Color(0.2157f, 0.2157f, 0.2196f, 1f);
            arrowImage.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            dropdownStorage.arrow = arrowImage;

            SetRectTransform(labelText.rectTransform, new Vector2(-15.3f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-46.6f, 0f));
            SetRectTransform(arrowImage.rectTransform, new Vector2(-2f, -0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(32f, 32f));

            #region Template

            var template = GenerateImage("Template", dropdownBase.transform);
            SetRectTransform(template.rectTransform, new Vector2(0f, 2f), Vector2.right, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 192f));
            var scrollRect = template.gameObject.AddComponent<ScrollRect>();

            var viewport = GenerateImage("Viewport", template.transform);
            SetRectTransform(viewport.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.up, Vector2.zero);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollbar = GenerateImage("Scrollbar", template.transform);
            SetRectTransform(scrollbar.rectTransform, Vector2.zero, Vector2.one, Vector2.right, Vector2.one, new Vector2(20f, 0f));
            var ssbar = scrollbar.gameObject.AddComponent<Scrollbar>();

            var slidingArea = Creator.NewUIObject("Sliding Area", scrollbar.transform);
            slidingArea.layer = 5;
            SetRectTransform(slidingArea.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

            var handle = GenerateImage("Handle", slidingArea.transform);
            SetRectTransform(handle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
            handle.color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            var content = Creator.NewUIObject("Content", viewport.transform);
            content.layer = 5;
            SetRectTransform(content.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));

            dropdownStorage.templateGrid = content.AddComponent<GridLayoutGroup>();
            dropdownStorage.templateGrid.cellSize = new Vector2(1000f, 32f);
            dropdownStorage.templateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            dropdownStorage.templateGrid.constraintCount = 1;

            dropdownStorage.templateFitter = dropdownStorage.templateGrid.gameObject.AddComponent<ContentSizeFitter>();
            dropdownStorage.templateFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            scrollRect.scrollSensitivity = 15f;
            scrollRect.content = content.transform.AsRT();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = ssbar;
            scrollRect.viewport = viewport.rectTransform;
            ssbar.handleRect = handle.rectTransform;
            ssbar.direction = Scrollbar.Direction.BottomToTop;
            ssbar.numberOfSteps = 0;

            var item = Creator.NewUIObject("Item", content.transform);
            item.layer = 5;
            SetRectTransform(item.transform.AsRT(), Vector2.zero, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
            var itemToggle = item.AddComponent<Toggle>();

            var itemBackground = GenerateImage("Item Background", item.transform);
            SetRectTransform(itemBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            itemBackground.color = new Color(0.9608f, 0.9608f, 0.9608f, 1f);
            itemBackground.transform.localScale = Vector3.one;

            var itemCheckmark = GenerateImage("Item Checkmark", item.transform);
            SetRectTransform(itemCheckmark.rectTransform, new Vector2(8f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));
            itemCheckmark.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/diamond.png"));
            itemCheckmark.color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            itemCheckmark.transform.localScale = Vector3.one;

            var itemLabel = GenerateText("Item Label", item.transform);
            SetRectTransform(itemLabel.rectTransform, new Vector2(15f, 0.5f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-50f, -3f));
            itemLabel.rectTransform.localScale = Vector3.one;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.font = Font.GetDefault();
            itemLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemLabel.verticalOverflow = VerticalWrapMode.Truncate;
            itemLabel.text = "Option A";
            itemLabel.color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);

            itemToggle.image = itemBackground;
            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = itemCheckmark;

            dropdown.captionText = labelText;
            dropdown.itemText = itemLabel;
            dropdown.alphaFadeSpeed = 0.15f;
            dropdown.template = template.rectTransform;
            dropdown.image = dropdownImage;
            template.gameObject.SetActive(false);

            #endregion

            return dropdownStorage;
        }

        public static ColorBlock SetColorBlock(ColorBlock cb, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, float fade = 0.2f)
        {
            cb.normalColor = normal;
            cb.highlightedColor = highlighted;
            cb.pressedColor = pressed;
            cb.selectedColor = selected;
            cb.disabledColor = disabled;
            cb.fadeDuration = fade;
            return cb;
        }

        public static UICanvas GenerateUICanvas(string name, Transform parent, bool dontDestroy = false, int sortingOrder = 10000)
        {
            var canvas = new UICanvas();
            canvas.Init(name, parent, dontDestroy, sortingOrder);
            return canvas;
        }
    }
}
