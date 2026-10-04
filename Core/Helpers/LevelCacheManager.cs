using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace BetterLegacy.Core.Helpers
{
    public static class LevelCacheManager
    {
        const long MAX_LEVEL_CACHE_BYTES = 100L * 1024L * 1024L;
        const long MAX_ICON_CACHE_BYTES = 25L * 1024L * 1024L;
        static readonly HashSet<string> pinnedKeys = new HashSet<string>();
        public static void SetPinnedLevels(IEnumerable<(string id, string hash)> entries)
        {
            pinnedKeys.Clear();
            foreach (var (id, hash) in entries)
                pinnedKeys.Add(Key(id, hash));
        }
        static string Key(string levelId, string hash) => $"{SanitizeID(levelId)}_{hash}";
        static string LevelCacheDirectory => RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/cache/levels");
        static string IconCacheDirectory => RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/cache/icons");
        public static string ComputeHash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
        }
        public static bool TryGetLevel(string levelId, string hash, out byte[] bytes) => TryGet(LevelFile(levelId, hash), out bytes);
        public static void StoreLevel(string levelId, string hash, byte[] bytes) => Store(LevelCacheDirectory, LevelFile(levelId, hash), bytes, MAX_LEVEL_CACHE_BYTES);
        public static bool TryGetIcon(string levelId, string hash, out byte[] bytes) => TryGet(IconFile(levelId, hash), out bytes);
        public static void StoreIcon(string levelId, string hash, byte[] bytes) => Store(IconCacheDirectory, IconFile(levelId, hash), bytes, MAX_ICON_CACHE_BYTES);
        static string LevelFile(string levelId, string hash) => RTFile.CombinePaths(LevelCacheDirectory, $"{Key(levelId, hash)}.zip");
        static string IconFile(string levelId, string hash) => RTFile.CombinePaths(IconCacheDirectory, $"{Key(levelId, hash)}.jpg");
        static string SanitizeID(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "unknown";
            var sb = new StringBuilder(id.Length);
            foreach (var c in id)
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            return sb.ToString();
        }
        static bool TryGet(string file, out byte[] bytes)
        {
            if (!RTFile.FileExists(file))
            {
                bytes = null;
                return false;
            }
            bytes = File.ReadAllBytes(file);
            Touch(file);
            return true;
        }
        static void Store(string directory, string file, byte[] bytes, long maxBytes)
        {
            RTFile.CreateDirectory(directory);
            EvictLeastRecentlyUsed(directory, maxBytes, bytes.LongLength);
            File.WriteAllBytes(file, bytes);
        }
        static void Touch(string file) => File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        static void EvictLeastRecentlyUsed(string directory, long maxBytes, long incomingSize)
        {
            if (!RTFile.DirectoryExists(directory))
                return;
            var files = new DirectoryInfo(directory).GetFiles();
            var total = files.Sum(f => f.Length) + incomingSize;
            if (total <= maxBytes)
                return;
            var ordered = files
                .OrderBy(f => pinnedKeys.Contains(Path.GetFileNameWithoutExtension(f.Name)) ? 1 : 0)
                .ThenBy(f => f.LastWriteTimeUtc);
            foreach (var file in ordered)
            {
                if (total <= maxBytes)
                    break;
                total -= file.Length;
                file.Delete();
            }
        }
    }
}