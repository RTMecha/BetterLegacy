using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using SteamworksFacepunch;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages multiplayer editor presence: remote playheads, selection colors, and player visibility.
    /// </summary>
    public class EditorMultiplayer
    {
        public class EditorPeer
        {
            public ulong id;
            public float playheadTime;
            public int layer;
            public EditorTimeline.LayerType layerType;
            public Color color;
            public HashSet<string> selectedIDs = new HashSet<string>();
            public GameObject ghost;
        }

        public static Dictionary<ulong, EditorPeer> Peers { get; private set; } = new Dictionary<ulong, EditorPeer>();
        public static Dictionary<string, List<ulong>> ObjectSelectors { get; private set; } = new Dictionary<string, List<ulong>>();

        public static bool selectionDirty;

        static GameObject ghostPoolParent;
        static List<GameObject> ghostPool = new List<GameObject>();
        static int nextGhostPoolIndex = 0;

        public static void ApplyPlayhead(ulong id, float time, int layer, EditorTimeline.LayerType layerType, Color color)
        {
            if (!Peers.TryGetValue(id, out var peer))
            {
                peer = new EditorPeer { id = id };
                Peers[id] = peer;
            }

            peer.playheadTime = time;
            peer.layer = layer;
            peer.layerType = layerType;
            peer.color = color;
        }

        public static void ApplySelection(ulong id, HashSet<string> newSet)
        {
            if (!Peers.TryGetValue(id, out var peer))
                return;

            var oldSet = peer.selectedIDs;
            var oldList = new List<string>(oldSet);

            peer.selectedIDs = newSet;

            var union = new HashSet<string>(oldSet);
            union.UnionWith(newSet);

            foreach (var objID in union)
            {
                if (!oldSet.Contains(objID) && newSet.Contains(objID))
                {
                    if (!ObjectSelectors.TryGetValue(objID, out var selectors))
                    {
                        selectors = new List<ulong>();
                        ObjectSelectors[objID] = selectors;
                    }
                    selectors.Add(id);
                }
                else if (oldSet.Contains(objID) && !newSet.Contains(objID))
                {
                    if (ObjectSelectors.TryGetValue(objID, out var selectors))
                    {
                        selectors.Remove(id);
                        if (selectors.Count == 0)
                            ObjectSelectors.Remove(objID);
                    }
                }

                if (EditorTimeline.inst)
                {
                    var timelineObj = EditorTimeline.inst.timelineObjects.Find(x => x.ID == objID);
                    if (timelineObj)
                        timelineObj.RenderVisibleState(false);
                }
            }
        }

        public static void RemovePeer(ulong id)
        {
            if (!Peers.TryGetValue(id, out var peer))
                return;

            var selectedList = peer.selectedIDs.ToList();
            foreach (var objID in selectedList)
            {
                if (ObjectSelectors.TryGetValue(objID, out var selectors))
                {
                    selectors.Remove(id);
                    if (selectors.Count == 0)
                        ObjectSelectors.Remove(objID);
                    else if (EditorTimeline.inst)
                    {
                        var timelineObj = EditorTimeline.inst.timelineObjects.Find(x => x.ID == objID);
                        if (timelineObj)
                            timelineObj.RenderVisibleState(false);
                    }
                }
            }

            if (peer.ghost)
                peer.ghost.SetActive(false);

            Peers.Remove(id);
        }

        public static bool TryGetRemoteSelectionColor(string objID, out Color color)
        {
            color = Color.white;
            if (ObjectSelectors.TryGetValue(objID, out var selectors) && selectors.Count > 0)
            {
                var peerID = selectors[0];
                if (Peers.TryGetValue(peerID, out var peer))
                {
                    color = peer.color;
                    return true;
                }
            }
            return false;
        }

        static bool ShouldShowPeerPlayhead(EditorPeer p)
        {
            return true;
            // Layer condition ready for future use:
            // return p.layer == EditorTimeline.inst.Layer && p.layerType == EditorTimeline.inst.layerType;
        }

        public static void RenderPlayheads()
        {
            if (!EditorTimeline.inst || !EditorTimeline.inst.timelineObjectsParent)
                return;

            EnsureGhostPool();

            nextGhostPoolIndex = 0;

            foreach (var peer in Peers.Values)
            {
                if (!ShouldShowPeerPlayhead(peer) || EditorConfig.Instance.HideOtherUsers.Value)
                {
                    if (peer.ghost)
                        peer.ghost.SetActive(false);
                    continue;
                }

                var ghost = GetOrCreateGhost();
                peer.ghost = ghost;
                ghost.SetActive(true);

                var rect = ghost.GetComponent<RectTransform>();
                var image = ghost.GetComponent<Image>();

                float xPos = peer.playheadTime * EditorManager.inst.Zoom;
                rect.anchoredPosition = new Vector2(xPos, 0f);
                image.color = peer.color;
            }

            while (nextGhostPoolIndex < ghostPool.Count)
            {
                var pooled = ghostPool[nextGhostPoolIndex++];
                if (pooled)
                    pooled.SetActive(false);
            }
        }

        static void EnsureGhostPool()
        {
            if (!ghostPoolParent)
            {
                if (!EditorTimeline.inst || !EditorTimeline.inst.timelineObjectsParent)
                    return;

                ghostPool.Clear();
                nextGhostPoolIndex = 0;
                foreach (var peer in Peers.Values)
                    peer.ghost = null;
                ghostPoolParent = Creator.NewUIObject("Ghost Playheads", EditorTimeline.inst.timelineObjectsParent);
                RectValues.FullAnchored.AssignToRectTransform(ghostPoolParent.transform.AsRT());
                ghostPoolParent.transform.SetAsLastSibling();
            }
        }

        static GameObject GetOrCreateGhost()
        {
            while (nextGhostPoolIndex < ghostPool.Count)
            {
                if (ghostPool[nextGhostPoolIndex])
                    return ghostPool[nextGhostPoolIndex++];
                ghostPool.RemoveAt(nextGhostPoolIndex);
            }

            var ghost = new GameObject("Ghost Playhead");
            ghost.transform.SetParent(ghostPoolParent.transform, false);

            var rect = ghost.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(2f, 0f);

            var image = ghost.AddComponent<Image>();
            image.color = Color.white;

            ghostPool.Add(ghost);
            return ghostPool[nextGhostPoolIndex++];
        }

        public static void UpdatePlayerModelVisibility()
        {
            if (!PlayerManager.inst)
                return;

            bool hide = EditorConfig.Instance.HideOtherUsers.Value;
            foreach (var player in PlayerManager.inst.players)
            {
                if (!player.IsLocalPlayer && player.RuntimePlayer && player.RuntimePlayer.gameObject)
                    player.RuntimePlayer.gameObject.SetActive(!hide);
            }
        }

        public static void BroadcastLocalPlayhead()
        {
            if (!ProjectArrhythmia.State.IsInLobby || !ProjectArrhythmia.State.InEditor)
                return;

            if (!AudioManager.inst || !EditorTimeline.inst)
                return;

            float time = AudioManager.inst.CurrentAudioSource.time;
            int layer = EditorTimeline.inst.Layer;
            var layerType = EditorTimeline.inst.layerType;
            string colorHex = RTColors.ColorToHexOptional(EditorConfig.Instance.TimelineCursorColor.Value);

            NetworkFunction.SetPlayheadPresence(RTSteamManager.inst.steamUser.steamID, time, layer, (byte)layerType, colorHex);
        }

        public static void BroadcastLocalSelection()
        {
            if (!ProjectArrhythmia.State.IsInLobby || !ProjectArrhythmia.State.InEditor)
                return;

            if (!EditorTimeline.inst)
                return;

            var ids = EditorTimeline.inst.SelectedObjects.Select(x => x.ID).ToList();
            string joinedIDs = string.Join("\n", ids);

            NetworkFunction.SetSelectionPresence(RTSteamManager.inst.steamUser.steamID, joinedIDs);
        }
        public static void BroadcastNewObject(BeatmapObject beatmapObject)
        {
            if (beatmapObject == null || !ProjectArrhythmia.State.IsInLobby)
                return;

            if (ProjectArrhythmia.State.IsHosting)
                NetworkFunction.CreateBeatmapObject(beatmapObject);
            else
                NetworkFunction.SubmitBeatmapObject(beatmapObject);
        }

        public static void Clear()
        {
            Peers.Clear();
            ObjectSelectors.Clear();
            selectionDirty = false;

            if (ghostPoolParent)
            {
                UnityEngine.Object.Destroy(ghostPoolParent);
                ghostPoolParent = null;
                ghostPool.Clear();
                nextGhostPoolIndex = 0;
            }

            UpdatePlayerModelVisibility();
        }
    }
}
