using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages viewing screenshots in the editor.
    /// </summary>
    public class ScreenshotsView : BaseManager<ScreenshotsView, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// The screenshot view dialog.
        /// </summary>
        public ScreenshotsViewDialog Dialog { get; set; }

        /// <summary>
        /// Screenshots to display per page.
        /// </summary>
        public int screenshotsPerPage = 10;

        /// <summary>
        /// Amount of screenshots loaded.
        /// </summary>
        public int screenshotCount;

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new ScreenshotsViewDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        /// <summary>
        /// Refreshes the screenshots in the screenshots folder.
        /// </summary>
        public void Refresh()
        {
            var directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;

            Dialog.ClearContent();
            var files = Directory.GetFiles(directory, FileFormat.PNG.ToPattern(), SearchOption.TopDirectoryOnly);
            screenshotCount = files.Length;

            if (screenshotCount > screenshotsPerPage)
                TriggerHelper.AddEventTriggers(Dialog.PageField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.PageField.inputField, max: Dialog.MaxPageCount));
            else
                TriggerHelper.AddEventTriggers(Dialog.PageField.inputField.gameObject);

            var screenshotsOnPage = 0;
            for (int i = 0; i < files.Length; i++)
            {
                if (!Dialog.InPage(i, screenshotsPerPage))
                    continue;

                var index = i;
                var file = files[index];

                var gameObject = Creator.NewUIObject("screenshot", Dialog.Content);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.AsRT().sizeDelta = new Vector2(720f, 405f);

                var image = gameObject.AddComponent<Image>();
                image.enabled = false;

                var button = gameObject.AddComponent<Button>();
                button.onClick.NewListener(() => RTFile.OpenInFileBrowser.OpenFile(file));
                button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), new Color(0.7f, 0.7f, 0.7f), Color.white, Color.red);

                EditorContextMenu.AddContextMenu(gameObject,
                    new ButtonElement("Open File", () => RTFile.OpenInFileBrowser.OpenFile(file)),
                    new ButtonElement("Open Folder", () => RTFile.OpenInFileBrowser.Open(directory)),
                    new SpacerElement(),
                    new ButtonElement("Duplicate", () =>
                    {
                        var destination = RTFile.CombinePaths(directory, DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT) + FileFormat.PNG.Dot());
                        RTFile.CopyFile(file, destination);
                        EditorManager.inst.DisplayNotification("Made a copy of the screenshot!", 2f, EditorManager.NotificationType.Success);
                        Refresh();
                    }),
                    new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this screenshot? This is permanent!", () =>
                    {
                        RTFile.DeleteFile(file);
                        EditorManager.inst.DisplayNotification("Deleted the screenshot!", 2f, EditorManager.NotificationType.Success);
                        if (screenshotsOnPage == 1)
                            Dialog.SetPage(Dialog.Page - 1);
                        else
                            Refresh();
                    })));

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{files[i]}", texture2D =>
                {
                    if (!image)
                        return;

                    image.enabled = true;
                    image.sprite = SpriteHelper.CreateSprite(texture2D);
                }));
                screenshotsOnPage++;
            }
        }

        #endregion
    }
}
