using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Components;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Represents a list item panel in the editor.
    /// </summary>
    /// <typeparam name="T">Type of the panels' reference.</typeparam>
    public abstract class EditorPanel<T> : Exists
    {
        #region Values

        #region UI

        /// <summary>
        /// Unity Game Object of the editor panel.
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// The label of the editor panel.
        /// </summary>
        public Text Label { get; set; }

        /// <summary>
        /// The hover scale component of the editor panel.
        /// </summary>
        public HoverUI HoverFocus { get; set; }

        /// <summary>
        /// The button for the editor panel.
        /// </summary>
        public FolderButtonFunction Button { get; set; }

        #endregion

        #region Data

        /// <summary>
        /// Editor panel item reference.
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// Folder name of the editor panel.
        /// </summary>
        public virtual string Name => !string.IsNullOrEmpty(Path) ? System.IO.Path.GetFileName(RTFile.RemoveEndSlash(Path)) : string.Empty;

        /// <summary>
        /// Direct path to the editor panels' folder.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Size the panel grows to when focused.
        /// </summary>
        public virtual float FocusSize { get; }

        /// <summary>
        /// If the level panel is a folder button instead.
        /// </summary>
        public bool isFolder;

        /// <summary>
        /// Custom tooltip info.
        /// </summary>
        public JSONNode infoJN;

        /// <summary>
        /// Index of the editor panel.
        /// </summary>
        public int index;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Sets the editor panel active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        /// <summary>
        /// Initializes the editor panel as a folder.
        /// </summary>
        public abstract void Init(string directory);

        /// <summary>
        /// Initializes the panel as a reference to the item.
        /// </summary>
        public abstract void Init(T item);

        /// <summary>
        /// Renders the editor panel.
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Renders the editor panel label.
        /// </summary>
        public abstract void RenderLabel();

        /// <summary>
        /// Renders the editor panel label.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public abstract void RenderLabel(string text);

        /// <summary>
        /// Renders the editor panel tooltip.
        /// </summary>
        public virtual void RenderTooltip()
        {
            if (isFolder)
                GetFolderTooltip();
        }

        /// <summary>
        /// Renders the editor panel hover component.
        /// </summary>
        public void RenderHover() => RenderHover(FocusSize);

        /// <summary>
        /// Renders the level panel hover component.
        /// </summary>
        /// <param name="size">Size to grow when hovered.</param>
        public void RenderHover(float size) => HoverFocus.size = size;

        /// <summary>
        /// Gets and renders the custom folder tooltip.
        /// </summary>
        public void GetFolderTooltip()
        {
            try
            {
                if (infoJN == null)
                    GetInfo();

                if (infoJN != null && !string.IsNullOrEmpty(infoJN["desc"]))
                    TooltipHelper.AddHoverTooltip(GameObject, $"Folder - {System.IO.Path.GetFileName(Path)}", infoJN["desc"], clear: true);
                else
                    TooltipHelper.AddHoverTooltip(GameObject, "Folder", System.IO.Path.GetFileName(Path), clear: true);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an exception with trying to add info to the {nameof(EditorPanel<T>)}.\nGameObject: {GameObject}\nFilePath: {Path}\nException: {ex}");
            }
        }

        /// <summary>
        /// Gets the info file.
        /// </summary>
        public void GetInfo()
        {
            if (RTFile.TryReadFromFile(RTFile.CombinePaths(Path, $"folder_info{FileFormat.JSON.Dot()}"), out string file))
                infoJN = JSON.Parse(file);
        }

        #endregion
    }
}
