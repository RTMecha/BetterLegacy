using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    // todo: replace original editor dialog with this
    public class EditorDialog : Exists
    {
        #region Properties

        /// <summary>
        /// The editor dialog that is currently being viewed.
        /// </summary>
        public static EditorDialog CurrentDialog { get; set; }

        /// <summary>
        /// Name of the editor dialog.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Game object of the editor dialog.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// Background image of the editor dialogs' title.
        /// </summary>
        public Image TitlePanel { get; set; }

        /// <summary>
        /// Title of the editor dialog.
        /// </summary>
        public Text Title { get; set; }

        #endregion

        public bool init;

        #region Methods

        public virtual void Init()
        {

        }

        /// <summary>
        /// Opens the editor dialog and sets it as the current dialog.
        /// </summary>
        public virtual void Open()
        {
            CurrentDialog?.Close();
            CurrentDialog = this;
            SetActive(true);
        }

        /// <summary>
        /// Closes the editor dialog.
        /// </summary>
        public virtual void Close()
        {
            CurrentDialog = null;
            SetActive(false);
        }

        /// <summary>
        /// Sets the active state of the editor dialog.
        /// </summary>
        /// <param name="active">The active state to set.</param>
        public virtual void SetActive(bool active)
        {
            //if (GameObject)
            //    GameObject.SetActive(active);
            EditorManager.inst.SetDialogStatus(Name, active);
        }

        #endregion
    }
}
