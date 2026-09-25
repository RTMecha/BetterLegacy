using System;
using BetterLegacy.Core.Managers;
namespace BetterLegacy.Core.Data.Network
{
    public static class NetworkPermissions
    {
        public const int THEME_EVENT_TYPE = 4;
        public static bool IsServerAllowed(NetworkFunction.Group group, int id)
        {
            var settings = SteamLobbyManager.inst ? SteamLobbyManager.inst.LobbySettings : null;
            if (settings == null)
                return true;
            switch (group)
            {
                case NetworkFunction.Group.Editor: return IsEditorAllowed(settings, id);
                default: return true;
            }
        }
        static bool IsEditorAllowed(LobbySettings settings, int id)
        {
            if (!settings.CanEdit)
                return false;
            switch (id)
            {
                case NetworkFunction.SUBMIT_BEATMAP_OBJECT:
                case NetworkFunction.CREATE_BEATMAP_OBJECT:
                case NetworkFunction.EDIT_BEATMAP_OBJECT:
                case NetworkFunction.SUBMIT_DELETE_OBJECT:
                case NetworkFunction.DELETE_OBJECT:
                case NetworkFunction.ADD_TAG:
                case NetworkFunction.REMOVE_TAG:
                case NetworkFunction.CLEAR_TAGS:
                case NetworkFunction.SET_BIN_COUNT:
                case NetworkFunction.CREATE_BACKGROUND_OBJECT:
                case NetworkFunction.EDIT_BACKGROUND_OBJECT:
                case NetworkFunction.CREATE_CHECKPOINT:
                case NetworkFunction.EDIT_CHECKPOINT:
                case NetworkFunction.DELETE_CHECKPOINT:
                case NetworkFunction.CREATE_ANIMATION_GROUP:
                case NetworkFunction.EDIT_ANIMATION_GROUP:
                case NetworkFunction.DELETE_ANIMATION_GROUP:
                    return settings.CanEditObjects;
                case NetworkFunction.CREATE_MARKER:
                case NetworkFunction.EDIT_MARKER:
                case NetworkFunction.DELETE_MARKER:
                    return settings.CanEditMarkers;
                case NetworkFunction.CREATE_EVENT_KEYFRAME:
                case NetworkFunction.EDIT_EVENT_KEYFRAME:
                case NetworkFunction.DELETE_EVENT_KEYFRAME:
                    return settings.CanEditEvents || settings.CanEditThemes;
                case NetworkFunction.SET_META_DATA:
                    return settings.CanEditMetaData;
                case NetworkFunction.CREATE_ACHIEVEMENT:
                case NetworkFunction.EDIT_ACHIEVEMENT:
                case NetworkFunction.DELETE_ACHIEVEMENT:
                    return settings.CanEditAchievements;
                case NetworkFunction.IMPORT_PREFAB:
                    return settings.CanImportPrefabs;
                case NetworkFunction.EXPAND_PREFAB:
                case NetworkFunction.ADD_PREFAB_OBJECT:
                case NetworkFunction.EDIT_PREFAB_OBJECT:
                    return settings.CanExpandPrefabs;
                default:
                    return true;
            }
        }
        public static bool IsServerAllowedPayload(NetworkFunction.Group group, int id, NetworkReader reader)
        {
            if (group != NetworkFunction.Group.Editor)
                return true;
            switch (id)
            {
                case NetworkFunction.CREATE_EVENT_KEYFRAME:
                case NetworkFunction.EDIT_EVENT_KEYFRAME:
                case NetworkFunction.DELETE_EVENT_KEYFRAME: {
                        var settings = SteamLobbyManager.inst ? SteamLobbyManager.inst.LobbySettings : null;
                        if (settings == null)
                            return true;
                        var start = reader.Position;
                        try
                        {
                            reader.ReadUInt64();
                            int type = reader.ReadInt32();
                            return type == THEME_EVENT_TYPE ? settings.CanEditThemes : settings.CanEditEvents;
                        }
                        catch { return true; }
                        finally { reader.Position = start; }
                    }
                default:
                    return true;
            }
        }
        public static bool ClientAllows(Func<LobbySettings, bool> selector)
        {
            if (!ProjectArrhythmia.State.IsClient)
                return true;
            var settings = LobbyInfo.HostLobbySettings;
            if (settings == null)
                return true;
            return settings.CanEdit && selector(settings);
        }
        public static bool ClientAllowsEvent(int type) => ClientAllows(x => type == THEME_EVENT_TYPE ? x.CanEditThemes : x.CanEditEvents);
        static float lastNotifyTime = -10f;
        public static bool IsReadOnly => ProjectArrhythmia.State.IsClient && LobbyInfo.HostLobbySettings != null && !LobbyInfo.HostLobbySettings.CanEdit;
        public static bool BlockEdit(Func<LobbySettings, bool> allowed, string category)
        {
            if (!ProjectArrhythmia.State.IsClient)
                return false;
            var settings = LobbyInfo.HostLobbySettings;
            if (settings == null)
                return false;
            if (settings.CanEdit && allowed(settings))
                return false;
            if (UnityEngine.Time.unscaledTime - lastNotifyTime > 1.5f && EditorManager.inst)
            {
                lastNotifyTime = UnityEngine.Time.unscaledTime;
                var message = !settings.CanEdit ? "Can't make edits in read only." : $"Can't make edits to {category}.";
                EditorManager.inst.DisplayNotification(message, 2f, EditorManager.NotificationType.Warning);
            }
            return true;
        }
        public static bool BlockEditReadOnly() => BlockEdit(x => true, null);
        public static bool BlockEditObjects() => BlockEdit(x => x.CanEditObjects, "objects");
        public static bool BlockEditModifiers() => BlockEdit(x => x.CanUseModifiers, "modifiers");
        public static bool BlockEditEvents(int type) => type == THEME_EVENT_TYPE ? BlockEdit(x => x.CanEditThemes, "themes") : BlockEdit(x => x.CanEditEvents, "events");
        public static bool BlockEditMarkers() => BlockEdit(x => x.CanEditMarkers, "markers");
        public static bool BlockEditPrefabs() => BlockEdit(x => x.CanExpandPrefabs, "prefabs");
        public static bool BlockEditCheckpoints() => BlockEdit(x => x.CanEditObjects, "checkpoints");
        public static bool BlockEditMetaData() => BlockEdit(x => x.CanEditMetaData, "metadata");
        public static bool BlockEditAchievements() => BlockEdit(x => x.CanEditAchievements, "achievements");
    }
}
