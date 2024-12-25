using BetterLegacy.Arcade;
using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using SteamworksFacepunch;
using SteamworksFacepunch.Data;
using SteamworksFacepunch.Ugc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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
                SteamClient.Init(440310U);
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
                Debug.LogError($"{className}Steam is not initialized! Try restarting with Steam open or updating the steam_api64.dll file to the newer version provided in the BetterLegacy latest release.");
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

                if (SteamUGC.Internal.GetItemState(publishedFileID) == 8U)
                {
                    var download = SteamUGC.Internal.DownloadItem(publishedFileID, false);
                    Debug.Log($"{className}Downloaded File: {publishedFileID}\nDownloadItem returns: {download}");
                    if (!download)
                        continue;
                }
                else
                {
                    ulong punSizeOnDisk = 0;
                    string pchFolder;
                    uint punTimeStamp = 0;
                    SteamUGC.Internal.GetItemInstallInfo(publishedFileID, ref punSizeOnDisk, out pchFolder, ref punTimeStamp);

                    if (!Level.TryVerify(pchFolder, true, out Level level))
                        continue;

                    level.id = publishedFileID.Value.ToString();
                    level.isSteamLevel = true;
                    Task.Run(() => GetItem(publishedFileID, level));

                    if (LevelManager.Saves.TryFindIndex(x => x.ID == level.id, out int saveIndex))
                        level.playerData = LevelManager.Saves[saveIndex];

                    onLoad?.Invoke(level, i);

                    Levels.Add(level);
                }
            }

            loading = false;
            hasLoaded = true;
        }

        /// <summary>
        /// Gets the levels' related Steam Item.
        /// </summary>
        /// <param name="publishedFileID">Steam Workshop item ID.</param>
        /// <param name="level">Level to assign the item to.</param>
        async void GetItem(PublishedFileId publishedFileID, Level level)
        {
            var item = await Item.GetAsync(publishedFileID);
            level.steamItem = item.Value;
            level.steamLevelInit = true;
        }

        /// <summary>
        /// Searches the PA Steam Workshop for levels.
        /// </summary>
        /// <param name="search">Search term.</param>
        /// <param name="page">Page to view.</param>
        /// <param name="onAddItem">Function to run when an item is loaded.</param>
        public async Task SearchAsync(string search, int page = 1, Action<Item, int> onAddItem = null)
        {
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
