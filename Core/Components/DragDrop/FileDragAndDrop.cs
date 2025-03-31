using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;

// from https://github.com/Bunny83/UnityWindowsFileDrag-Drop

public class FileDragAndDrop : MonoBehaviour
{
    void Start ()
    {
        // must be installed on the main thread to get the right thread id.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }

    void OnDestroy()
    {
        UnityDragAndDropHook.UninstallHook();
        UnityDragAndDropHook.OnDroppedFiles -= OnFiles;
    }

    void OnFiles(List<string> aFiles, POINT aPos) => onFilesDropped?.Invoke(aFiles.Select(x => new DropInfo(x, new Vector2Int(aPos.x, aPos.y))).ToList());

    public System.Action<List<DropInfo>> onFilesDropped;

    public class DropInfo
    {
        public DropInfo(string filePath, Vector2Int pos)
        {
            this.filePath = filePath;
            this.pos = pos;
        }

        public string filePath;

        public Vector2Int pos;

        public override string ToString() => $"{filePath} at {pos}";
    }
}
