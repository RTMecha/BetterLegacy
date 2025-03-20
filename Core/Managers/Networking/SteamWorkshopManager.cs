using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using SteamworksFacepunch;
using SteamworksFacepunch.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using SteamworksFacepunch.Ugc;

namespace BetterLegacy.Core.Managers.Networking
{
    /// <summary>
    /// <see cref="SteamManager"/>, <see cref="SteamWorkshop"/> and <see cref="SteamWrapper"/> wrapper.
    /// </summary>
    public class SteamWorkshopManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="SteamWorkshopManager"/> global instance reference.
        /// </summary>
        public static SteamWorkshopManager inst;

        /// <summary>
        /// Manager class name.
        /// </summary>
        public static string className = "[<color=#e81e62>Steam</color>] \n";

        /// <summary>
        /// Initializes <see cref="SteamWorkshopManager"/>.
        /// </summary>
        /// <param name="steamManager">Wrap.</param>
        public static void Init(SteamManager steamManager) => steamManager.gameObject.AddComponent<SteamWorkshopManager>();

        /// <summary>
        /// If Steam Client was initialized.
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Error message to show if Steam was not initialized.
        /// </summary>
        public const string NOT_INIT_MESSAGE = "Steam was not initialized. Open Steam if it isn't already, otherwise if it is then please replace the steam_api64.dll in Project Arrhythmia_Data/Plugins with the newer version provided.";

        #region Internal

        void Awake()
        {
            if (!inst)
                inst = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            try
            {
                SteamClient.Init(ProjectArrhythmia.STEAM_APP_ID);
                steamUser = new SteamUser(SteamClient.SteamId, SteamClient.SteamId.Value, SteamClient.Name);
                Debug.Log($"{className}Init Steam User: {SteamClient.Name}");
                Initialized = true;
                try
                {
                    var displayName = CoreConfig.Instance.DisplayName;
                    if (displayName.Value == displayName.Default)
                        displayName.Value = steamUser.name;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{className}Had an error setting the default config: {ex}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{className}Steam Workshop Init failed.\nPlease replace the steam_api64.dll in Project Arrhythmia_Data/Plugins with the newer version!\n{ex}");
                Initialized = false;
            }
        }

        void Update()
        {
            if (Initialized)
                SteamClient.RunCallbacks();
        }

        void OnApplicationQuit()
        {
            if (Initialized)
                SteamClient.Shutdown();
        }

        #endregion

        #endregion

        #region Levels

        /// <summary>
        /// Subscribed Steam Workshop levels.
        /// </summary>
        public List<Level> Levels { get; set; } = new List<Level>();

        /// <summary>
        /// True if the subscribed Steam levels have finished loading.
        /// </summary>
        public bool hasLoaded;

        /// <summary>
        /// True if <see cref="GetSubscribedItems(Action{Level, int})"/> is running.
        /// </summary>
        public bool loading;

        /// <summary>
        /// If <see cref="DownloadLevel(Item)"/> is running.
        /// </summary>
        public bool downloading;

        /// <summary>
        /// Array containing all subscribed Steam Workshop items.
        /// </summary>
        public PublishedFileId[] subscribedFiles;

        /// <summary>
        /// Total count of Steam Workshop items.
        /// </summary>
        public uint LevelCount { get; set; }

        /// <summary>
        /// Loads all subscribed Steam levels.
        /// </summary>
        /// <param name="onLoad">Function to run when an item is loaded.</param>
        public IEnumerator GetSubscribedItems(Action<Level, int> onLoad = null)
        {
            hasLoaded = false;
            loading = true;
            Levels.Clear();

            if (!Initialized)
            {
                Debug.LogError($"{className}{NOT_INIT_MESSAGE}");
                yield break;
            }

            var loadYieldMode = ArcadeConfig.Instance.LoadYieldMode.Value;

            uint numSubscribedItems = SteamUGC.Internal.GetNumSubscribedItems();
            subscribedFiles = new PublishedFileId[numSubscribedItems];
            LevelCount = numSubscribedItems;
            uint subscribedItems = SteamUGC.Internal.GetSubscribedItems(subscribedFiles, numSubscribedItems);
            float delay = 0f;
            for (int i = 0; i < subscribedFiles.Length; i++)
            {
                var publishedFileID = subscribedFiles[i];

                if (LoadLevelsManager.inst && LoadLevelsManager.inst.cancelled)
                {
                    SceneHelper.LoadInputSelect();
                    ArcadeHelper.currentlyLoading = false;
                    Levels.Clear();
                    loading = false;
                    hasLoaded = false;
                    yield break;
                }

                if (loadYieldMode != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(loadYieldMode, ref delay);

                string folder;
                var installInfo = TryGetItemPath(publishedFileID, out folder);

                Item item = default;
                bool exists = false;
                yield return GetItem(publishedFileID, result =>
                {
                    item = result;
                    exists = true;
                });

                if (!installInfo)
                {
                    if (exists)
                    {
                        if (item.Result == Result.AccessDenied)
                        {
                            CoreHelper.Log($"Item {publishedFileID} could not be accessed..");
                            if (LoadLevelsManager.inst)
                                LoadLevelsManager.inst.UpdateInfo(LegacyPlugin.AtanPlaceholder, $"Item {publishedFileID} could not be accessed.", i);
                            continue;
                        }

                        CoreHelper.Log($"Attempting to download item: {publishedFileID} Result: {item.Result}.");
                        if (LoadLevelsManager.inst)
                            LoadLevelsManager.inst.UpdateInfo(LegacyPlugin.AtanPlaceholder, $"Downloading item: {publishedFileID}", i);
                        yield return item.DownloadAsync();
                        installInfo = TryGetItemPath(publishedFileID, out folder);

                        // for cases where the level no longer exists.
                        if (!installInfo)
                        {
                            CoreHelper.Log($"Item {publishedFileID} no longer exists. {item.Result}");
                            if (LoadLevelsManager.inst)
                                LoadLevelsManager.inst.UpdateInfo(LegacyPlugin.AtanPlaceholder, $"Item {publishedFileID} no longer exists.", i);

                            continue;
                        }
                    }
                    else
                    {
                        CoreHelper.Log($"Item {publishedFileID} no longer exists.");
                        if (LoadLevelsManager.inst)
                            LoadLevelsManager.inst.UpdateInfo(LegacyPlugin.AtanPlaceholder, $"Item {publishedFileID} no longer exists.", i);

                        continue;
                    }
                }

                if (!Level.TryVerify(folder, true, out Level level))
                    continue;

                level.id = publishedFileID.Value.ToString();
                level.isSteamLevel = true;
                level.steamItem = item;
                level.steamLevelInit = exists;

                LevelManager.AssignPlayerData(level);

                onLoad?.Invoke(level, i);

                Levels.Add(level);
            }

            loading = false;
            hasLoaded = true;
        }

        /// <summary>
        /// Toggles the Subscribe state of a Steam item.
        /// </summary>
        /// <param name="item">Item to subscribe / unsubscribe from.</param>
        /// <param name="onSubscribedLevel">Action to run when item is subscribed to.</param>
        public IEnumerator ToggleSubscribedState(Item item, Action<Level> onSubscribedLevel = null)
        {
            if (!Initialized)
            {
                Debug.LogError($"{className}{NOT_INIT_MESSAGE}");
                yield break;
            }

            CoreHelper.LogSeparator();
            CoreHelper.Log($"Beginning {(!item.IsSubscribed ? "subscribing" : "unsubscribing")} of {item.Id}\nTitle: {item.Title}");

            ProgressMenu.Init($"Updating Steam item: {item.Id} - {item.Title}<br>Please wait...");

            var subscribed = item.IsSubscribed;
            downloading = true;
            if (!subscribed)
            {
                CoreHelper.Log($"Subscribing...");
                yield return item.Subscribe();
                yield return item.DownloadAsync(progress =>
                {
                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }
                });

                while (!item.IsInstalled || item.IsDownloadPending || item.IsDownloading)
                {
                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }

                    yield return null;
                }
                ProgressMenu.Current.UpdateProgress(1f);

                subscribed = true;
            }
            else
            {
                CoreHelper.Log($"Unsubscribing...");
                yield return item.Unsubscribe();
                yield return item.DownloadAsync();

                subscribed = false;
            }
            downloading = false;

            yield return new WaitForSeconds(0.1f);
            CoreHelper.Log($"{item.Id} Status: {(subscribed ? "Subscribed" : "Unsubscribed")}");

            while (InterfaceManager.inst.CurrentInterface && InterfaceManager.inst.CurrentInterface.generating)
                yield return null;

            int levelIndex = -1;
            if (!subscribed && Levels.TryFindIndex(x => x.metadata != null && x.id == item.Id.ToString(), out levelIndex))
            {
                CoreHelper.Log($"Unsubscribed > Remove level {Levels[levelIndex]}.");
                Levels.RemoveAt(levelIndex);
            }

            if (subscribed && item.IsInstalled && Level.TryVerify(item.Directory, true, out Level level))
            {
                CoreHelper.Log($"Subscribed > Add level {level.path}.");
                level.id = item.Id.Value.ToString();
                Levels.Add(level);

                if (onSubscribedLevel != null)
                {
                    onSubscribedLevel(level);
                    item = default;
                    yield break;
                }

                PlayLevelMenu.Init(level);
                item = default;
                yield break;
            }
            else if (subscribed)
                CoreHelper.LogError($"Item doesn't exist.");

            CoreHelper.Log($"Finished updating Steam item.");
            item = default;
            ArcadeMenu.Init();
        }

        /// <summary>
        /// Sets the Subscribe state of a Steam item.
        /// </summary>
        /// <param name="item">Item to subscribe / unsubscribe from.</param>
        /// <param name="subscribed">If the item should be subscribed to.</param>
        /// <param name="onSubscribedLevel">Action to run when item is subscribed to.</param>
        public IEnumerator SetSubscribedState(Item item, bool subscribed, Action<Level> onSubscribedLevel = null)
        {
            if (!Initialized)
            {
                Debug.LogError($"{className}{NOT_INIT_MESSAGE}");
                yield break;
            }

            CoreHelper.LogSeparator();
            CoreHelper.Log($"Beginning {(!item.IsSubscribed ? "subscribing" : "unsubscribing")} of {item.Id}\nTitle: {item.Title}");

            ProgressMenu.Init($"Updating Steam item: {item.Id} - {item.Title}<br>Please wait...");

            var isSubscribed = item.IsSubscribed;
            downloading = true;
            if (!isSubscribed && isSubscribed != subscribed)
            {
                CoreHelper.Log($"Subscribing...");
                yield return item.Subscribe();
                yield return item.DownloadAsync(progress =>
                {
                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }
                });

                while (!item.IsInstalled || item.IsDownloadPending || item.IsDownloading)
                {
                    try
                    {
                        ProgressMenu.Current.UpdateProgress(item.DownloadAmount);
                    }
                    catch
                    {

                    }

                    yield return null;
                }
                ProgressMenu.Current.UpdateProgress(1f);

                subscribed = true;
            }

            if (isSubscribed && isSubscribed != subscribed)
            {
                CoreHelper.Log($"Unsubscribing...");
                yield return item.Unsubscribe();
                yield return item.DownloadAsync();

                subscribed = false;
            }
            downloading = false;

            yield return new WaitForSeconds(0.1f);
            CoreHelper.Log($"{item.Id} Status: {(subscribed ? "Subscribed" : "Unsubscribed")}");

            while (InterfaceManager.inst.CurrentInterface && InterfaceManager.inst.CurrentInterface.generating)
                yield return null;

            int levelIndex = -1;
            if (!isSubscribed && Levels.TryFindIndex(x => x.metadata != null && x.id == item.Id.ToString(), out levelIndex))
            {
                CoreHelper.Log($"Unsubscribed > Remove level {Levels[levelIndex]}.");
                Levels.RemoveAt(levelIndex);
            }

            if (isSubscribed && item.IsInstalled && Level.TryVerify(item.Directory, true, out Level level))
            {
                CoreHelper.Log($"Subscribed > Add level {level.path}.");
                level.id = item.Id.Value.ToString();
                Levels.Add(level);

                if (onSubscribedLevel != null)
                {
                    onSubscribedLevel(level);
                    item = default;
                    yield break;
                }

                PlayLevelMenu.Init(level);
                item = default;
                yield break;
            }
            else if (isSubscribed)
                CoreHelper.LogError($"Item doesn't exist.");

            CoreHelper.Log($"Finished updating Steam item.");
            item = default;
            ArcadeMenu.Init();
        }

        public bool TryGetItemPath(PublishedFileId publishedFileID, out string folder)
        {
            ulong punSizeOnDisk = 0;
            uint punTimeStamp = 0;
            return SteamUGC.Internal.GetItemInstallInfo(publishedFileID, ref punSizeOnDisk, out folder, ref punTimeStamp);
        }

        /// <summary>
        /// Searches the PA Steam Workshop for levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        /// <param name="page">Page to view.</param>
        /// <param name="onAddItem">Function to run when an item is loaded.</param>
        public async Task SearchAsync(string search, int page = 1, Action<Item, int> onAddItem = null)
        {
            if (!Initialized)
            {
                Debug.LogError($"{className}{NOT_INIT_MESSAGE}");
                return;
            }

            if (search == null)
                search = "";

            page = Mathf.Clamp(page, 1, int.MaxValue);

            var query = Query.Items
                .Sort(ArcadeConfig.Instance.SteamWorkshopOrderby.Value)
                .WhereSearchText(search);

            if (ArcadeConfig.Instance.SteamWorkshopFriendsOnly.Value)
                query = query.CreatedByFriends();

            if (ArcadeConfig.Instance.SteamWorkshopFollowingOnly.Value)
                query = query.CreatedByFollowedUsers();

            if (ArcadeConfig.Instance.SteamWorkshopFavoritedOnly.Value)
                query = query.WhereUserFavorited(steamUser.steamID);

            ResultPage? resultPage = await query.GetPageAsync(page);

            if (resultPage == null || !resultPage.HasValue || resultPage.Value.ResultCount <= 0)
            {
                Debug.LogError($"{className}Page has no content.");
                return;
            }

            for (int i = 0; i < resultPage.Value.ResultCount; i++)
            {
                var item = resultPage.Value.Entries.ElementAt(i);
                onAddItem?.Invoke(item, i);
            }
        }

        /// <summary>
        /// Gets a published file ID.
        /// </summary>
        /// <param name="id">ID to convert to <see cref="PublishedFileId"/>.</param>
        /// <returns>Returns a Workshop ID.</returns>
        public static PublishedFileId GetWorkshopID(string id) => ulong.TryParse(id, out ulong result) ? new PublishedFileId() { Value = result } : default;

        /// <summary>
        /// Gets a Steam Workshop item.
        /// </summary>
        /// <param name="publishedFileID">Workshop ID.</param>
        /// <param name="result">Output item.</param>
        /// <param name="onFail">Runs on fail.</param>
        public static async Task GetItem(PublishedFileId publishedFileID, Action<Item> result, Action onFail = null)
        {
            var item = await Item.GetAsync(publishedFileID);
            if (item is Item steamItem)
                result?.Invoke(steamItem);
            else
                onFail?.Invoke();
        }

        /// <summary>
        /// Opens an item's Steam Workshop page.
        /// </summary>
        /// <param name="id">ID to open.</param>
        public void OpenWorkshop(PublishedFileId publishedFileID) => Application.OpenURL($"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedFileID}");

        #endregion

        #region User

        /// <summary>
        /// Local Steam User reference.
        /// </summary>
        public SteamUser steamUser;

        /// <summary>
        /// Steam User wrapper.
        /// </summary>
        public class SteamUser
        {
            public SteamUser() { }

            public SteamUser(SteamId steamID, ulong id, string name)
            {
                this.steamID = steamID;
                this.id = id;
                this.name = name;
            }

            public SteamId steamID;
            public ulong id;
            public string name = "No Steam User";

            /// <summary>
            /// Unlocks an achievement.
            /// </summary>
            /// <param name="achievement">Achievement to unlock.</param>
            public void SetAchievement(string achievement)
            {
                if (!inst.Initialized)
                    return;

                SteamUserStats.Internal.SetAchievement(achievement);
                bool flag = false;
                SteamUserStats.Internal.GetAchievement(achievement, ref flag);
                Debug.Log($"{className} Set Achievement : [{achievement}] -> [{flag}]");
                SteamUserStats.StoreStats();
            }

            /// <summary>
            /// Gets an achievements' status.
            /// </summary>
            /// <param name="achievement">Achievement to get.</param>
            /// <returns>Returns true if the achievement is unlocked, otherwise returns false.</returns>
            public bool GetAchievement(string achievement)
            {
                if (!inst.Initialized)
                    return false;

                bool flag = false;
                SteamUserStats.Internal.GetAchievement(achievement, ref flag);
                return flag;
            }

            /// <summary>
            /// Clears an achievement.
            /// </summary>
            /// <param name="achievement">Achievement to clear.</param>
            public void ClearAchievement(string achievement)
            {
                if (!inst.Initialized)
                    return;

                SteamUserStats.Internal.ClearAchievement(achievement);
                Debug.Log($"{className} Cleared Achievement : [{achievement}]");
            }
        }

        #endregion
    }
}
