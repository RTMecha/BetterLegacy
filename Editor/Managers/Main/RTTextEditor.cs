using System;

using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Expanded text editing.
    /// </summary>
    public class RTTextEditor : BaseManager<RTTextEditor, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// Popup UI of the Text Editor.
        /// </summary>
        public TextEditorPopup Popup { get; set; }

        /// <summary>
        /// If <see cref="currentInputField"/> should update when Text Editor is changed.
        /// </summary>
        public bool autoUpdate = true;

        /// <summary>
        /// Input Field to update when Text Editor is changed.
        /// </summary>
        public InputField currentInputField;

        /// <summary>
        /// Text the Text Editor currently has.
        /// </summary>
        public string Text
        {
            get => Popup.EditorField.text;
            set => Popup.EditorField.text = value;
        }

        #endregion

        #region Functions

        public override void OnInit()
        {
            Popup = new TextEditorPopup();
            Popup.Init();
            RTEditor.inst.editorPopups.Add(Popup);
        }

        /// <summary>
        /// Applies the Text Editor's text to the currently selected Input Field.
        /// </summary>
        public void UpdateText()
        {
            if (!currentInputField)
                return;

            currentInputField.text = Text;
        }

        /// <summary>
        /// Sets the currently selected Input Field to edit.
        /// </summary>
        /// <param name="inputField">Input Field to set as current.</param>
        public void SetInputField(InputField inputField)
        {
            if (!inputField)
                return;

            Popup.Open();

            currentInputField = inputField;
            Popup.EditorField.SetTextWithoutNotify(currentInputField.text);
            Popup.EditorField.onValueChanged.NewListener(SetText);

            Popup.UpdateButton.Text = "Update";
            Popup.UpdateButton.OnClick.NewListener(UpdateText);

            Popup.AutoUpdateLabel.gameObject.SetActive(true);
            Popup.AutoUpdateToggle.gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets the Text Editor.
        /// </summary>
        /// <param name="val">Value to set.</param>
        /// <param name="onValueChanged">Function to run when Text Editor is changed.</param>
        /// <param name="updateLabel">Label to display on the Update button.</param>
        /// <param name="update">Function to run when the Update button is clicked.</param>
        public void SetEditor(string val, Action<string> onValueChanged, string updateLabel, Action update)
        {
            Popup.Open();

            currentInputField = null;
            Popup.EditorField.SetTextWithoutNotify(val);
            Popup.EditorField.onValueChanged.NewListener(onValueChanged);

            Popup.UpdateButton.Text = updateLabel;
            Popup.UpdateButton.OnClick.NewListener(update);

            Popup.AutoUpdateLabel.gameObject.SetActive(false);
            Popup.AutoUpdateToggle.gameObject.SetActive(false);
        }

        void SetText(string _val)
        {
            if (!currentInputField || !autoUpdate)
                return;
            currentInputField.text = _val;
        }

        #endregion
    }
}
