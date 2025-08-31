using System;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    public class PageContentPopup : ContentPopup, IPageUI
    {
        public PageContentPopup(string name) : base(name) { }

        public PageContentPopup(string name, string title, Vector2? defaultPosition = null, Vector2? size = null, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...") : base(name)
        {
            this.title = title;
            this.defaultPosition = defaultPosition;
            this.size = size;
            this.refreshSearch = refreshSearch;
            this.close = close;
            this.placeholderText = placeholderText;
        }

        public InputFieldStorage PageField { get; set; }

        public int Page { get; set; }

        public int MaxPageCount => getMaxPageCount?.Invoke() ?? int.MaxValue;

        public Func<int> getMaxPageCount;

        public override void Init()
        {
            base.Init();
            InitPageField();
        }

        /// <summary>
        /// Initializes the page field.
        /// </summary>
        public void InitPageField()
        {
            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(TopPanel, "page");
            page.GetComponent<HorizontalLayoutGroup>().spacing = 4f;
            //page.transform.AsRT().anchoredPosition = new Vector2(230f, 16f);
            //page.transform.AsRT().sizeDelta = new Vector2(144f, 32f);
            RectValues.RightAnchored.AnchoredPosition(-32f, 0f).SizeDelta(144f, 0f).AssignToRectTransform(page.transform.AsRT());
            PageField = page.GetComponent<InputFieldStorage>();
            PageField.inputField.SetTextWithoutNotify("0");
            PageField.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    Page = Mathf.Clamp(p, 0, MaxPageCount);
                    SearchField?.onValueChanged?.Invoke(SearchField.text);
                }
            });

            PageField.leftGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.inputField.text, out int p))
                    PageField.inputField.text = "0";
            });

            PageField.leftButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.inputField.text, out int p))
                    PageField.inputField.text = Mathf.Clamp(p - 1, 0, MaxPageCount).ToString();
            });

            PageField.rightButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.inputField.text, out int p))
                    PageField.inputField.text = Mathf.Clamp(p + 1, 0, MaxPageCount).ToString();
            });

            PageField.rightGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.inputField.text, out int p))
                    PageField.inputField.text = MaxPageCount.ToString();
            });

            CoreHelper.Delete(PageField.middleButton);

            EditorThemeManager.AddInputField(PageField);
        }
    }
}
