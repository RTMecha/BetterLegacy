using System;
using System.IO;
using System.IO.Compression;

using HarmonyLib;

namespace BetterLegacy.Patchers
{
    /// <summary>
    /// Fixes an issue with ZipArchives.
    /// </summary>
    [HarmonyPatch(typeof(ZipArchiveEntry))]
    public class ZipArchiveEntryPatch
    {
        [HarmonyPatch(nameof(ZipArchiveEntry.OpenInWriteMode))]
        [HarmonyPrefix]
        static bool OpenInWriteModePrefix(ref Stream __result, ZipArchiveEntry __instance)
        {
            __result = OpenInWriteMode(__instance);
            return false;
        }

        static Stream OpenInWriteMode(ZipArchiveEntry __instance)
        {
            if (__instance._everOpenedForWrite)
            {
                throw new IOException("Ever opened for write");
            }
            __instance._everOpenedForWrite = true;
            var dataCompressor = GetDataCompressor(__instance, __instance._archive.ArchiveStream, true, delegate (object o, EventArgs e)
            {
                __instance._archive.ReleaseArchiveStream(__instance);
                __instance._outstandingWriteStream = null;
            });
            __instance._outstandingWriteStream = new ZipArchiveEntry.DirectToArchiveWriterStream(dataCompressor, __instance);
            return new WrappedStream(__instance._outstandingWriteStream, delegate (object o, EventArgs e)
            {
                __instance._outstandingWriteStream.Close();
            });
        }

        static CheckSumAndSizeWriteStream GetDataCompressor(ZipArchiveEntry __instance, Stream backingStream, bool leaveBackingStreamOpen, EventHandler onClose)
        {
            var stream = new DeflateStream(backingStream, CompressionMode.Compress, leaveBackingStreamOpen);

            return new CheckSumAndSizeWriteStream(stream, backingStream, leaveBackingStreamOpen && !true, delegate (long initialPosition, long currentPosition, uint checkSum)
            {
                __instance._crc32 = checkSum;
                __instance._uncompressedSize = currentPosition;
                __instance._compressedSize = backingStream.Position - initialPosition;
                onClose?.Invoke(__instance, EventArgs.Empty);
            });
        }

    }
}
