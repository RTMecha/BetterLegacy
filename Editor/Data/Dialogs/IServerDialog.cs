using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Components;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public interface IServerDialog
    {
        public RectTransform ServerBase { get; set; }
        public RectTransform ServerContent { get; set; }

        public Toggle RequireVersion { get; set; }

        public Dropdown VersionComparison { get; set; }

        public Dropdown ServerVisibilityDropdown { get; set; }

        public RectTransform CollaboratorsScrollView { get; set; }

        public RectTransform CollaboratorsContent { get; set; }

        public GameObject ChangelogLabel { get; set; }
        public GameObject Changelog { get; set; }
        public InputField ChangelogField { get; set; }

        public Text ServerIDText { get; set; }
        public ContextClickable ServerIDContextMenu { get; set; }
        public Text UserIDText { get; set; }
        public ContextClickable UserIDContextMenu { get; set; }

        public Button UploadButton { get; set; }
        public ContextClickable UploadContextMenu { get; set; }
        public Text UploadButtonText { get; set; }
        public Button PullButton { get; set; }
        public ContextClickable PullContextMenu { get; set; }
        public Button DeleteButton { get; set; }
        public ContextClickable DeleteContextMenu { get; set; }

        public void Open();

        public void ShowChangelog(bool show);
    }
}
