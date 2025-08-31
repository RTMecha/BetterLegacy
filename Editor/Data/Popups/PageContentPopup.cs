using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core;
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

        public int MaxPageCount => getMaxPageCount?.Invoke() ?? 1;

        public Func<int> getMaxPageCount;

        public override void Init()
        {
            base.Init();

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(TopPanel, "page");
            page.transform.AsRT().anchoredPosition = new Vector2(240f, 16f);
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
