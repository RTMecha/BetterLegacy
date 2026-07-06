using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;

namespace BetterLegacy.Core.Data.Network
{
    public class RTSync : IPacket
    {
        #region Constructors

        public RTSync() { }

        #endregion

        #region Functions

        public void ReadPacket(NetworkReader reader)
        {
            var beatmapObjects = new List<SyncBeatmapObject>();
            Packet.ReadPacketList(beatmapObjects, reader);
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];
                if (GameData.Current.beatmapObjects.TryFind(x => x.id == beatmapObject.id, out BeatmapObject orig))
                    beatmapObject.WriteData(orig);
            }

            var backgroundObjects = new List<SyncBackgroundObject>();
            Packet.ReadPacketList(backgroundObjects, reader);
            for (int i = 0; i < backgroundObjects.Count; i++)
            {
                var backgroundObject = backgroundObjects[i];
                if (GameData.Current.backgroundObjects.TryFind(x => x.id == backgroundObject.id, out BackgroundObject orig))
                    backgroundObject.WriteData(orig);
            }

            var prefabObjects = new List<SyncPrefabObject>();
            Packet.ReadPacketList(prefabObjects, reader);
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];
                if (GameData.Current.prefabObjects.TryFind(x => x.id == prefabObject.id, out PrefabObject orig))
                    prefabObject.WriteData(orig);
            }
        }

        public void WritePacket(NetworkWriter writer)
        {
            Packet.WritePacketList(new List<SyncBeatmapObject>(GameData.Current.beatmapObjects.Select(SyncBeatmapObject.ToSyncData<SyncBeatmapObject>)), writer);
            Packet.WritePacketList(new List<SyncBackgroundObject>(GameData.Current.backgroundObjects.Select(SyncBackgroundObject.ToSyncData<SyncBackgroundObject>)), writer);
            Packet.WritePacketList(new List<SyncPrefabObject>(GameData.Current.prefabObjects.Select(SyncPrefabObject.ToSyncData<SyncPrefabObject>)), writer);
        }

        #endregion

        #region Sub Classes

        public abstract class SyncObject<T> : IPacket
        {
            public SyncObject() { }

            #region Values

            public string id;

            #endregion

            #region Functions

            public abstract void ReadPacket(NetworkReader reader);

            public abstract void WritePacket(NetworkWriter writer);

            public abstract void ReadData(T obj);

            public abstract void WriteData(T obj);

            public static TSync ToSyncData<TSync>(T obj) where TSync : SyncObject<T>, new()
            {
                var sync = new TSync();
                sync.ReadData(obj);
                return sync;
            }

            #endregion
        }

        public class SyncModifier : SyncObject<Modifier>
        {
            #region Values

            public int runCount;

            #endregion

            #region Functions

            public override void ReadPacket(NetworkReader reader)
            {
                runCount = reader.ReadInt32();
            }

            public override void WritePacket(NetworkWriter writer)
            {
                writer.Write(runCount);
            }

            public override void ReadData(Modifier obj)
            {
                runCount = obj.runCount;
            }

            public override void WriteData(Modifier obj)
            {
                obj.runCount = runCount;
            }

            #endregion
        }
        
        public class SyncBeatmapObject : SyncObject<BeatmapObject>
        {
            public SyncBeatmapObject() { }

            #region Values

            public List<SyncModifier> modifiers;

            #endregion

            #region Functions

            public override void ReadPacket(NetworkReader reader)
            {
                id = reader.ReadString();
                modifiers = new List<SyncModifier>();
                Packet.ReadPacketList(modifiers, reader);
            }

            public override void WritePacket(NetworkWriter writer)
            {
                writer.Write(id);
                if (modifiers == null)
                    modifiers = new List<SyncModifier>();
                Packet.WritePacketList(modifiers, writer);
            }

            public override void ReadData(BeatmapObject obj)
            {
                id = obj.id;
                modifiers = new List<SyncModifier>(obj.modifiers.Select(SyncModifier.ToSyncData<SyncModifier>));
            }

            public override void WriteData(BeatmapObject obj)
            {
                for (int i = 0; i < modifiers.Count; i++)
                    modifiers[i].WriteData(obj.modifiers[i]);
            }

            #endregion
        }

        public class SyncBackgroundObject : SyncObject<BackgroundObject>
        {
            public SyncBackgroundObject() { }

            #region Values

            public List<SyncModifier> modifiers;

            #endregion

            #region Functions

            public override void ReadPacket(NetworkReader reader)
            {
                id = reader.ReadString();
                modifiers = new List<SyncModifier>();
                Packet.ReadPacketList(modifiers, reader);
            }

            public override void WritePacket(NetworkWriter writer)
            {
                writer.Write(id);
                if (modifiers == null)
                    modifiers = new List<SyncModifier>();
                Packet.WritePacketList(modifiers, writer);
            }

            public override void ReadData(BackgroundObject obj)
            {
                id = obj.id;
                modifiers = new List<SyncModifier>(obj.modifiers.Select(SyncModifier.ToSyncData<SyncModifier>));
            }

            public override void WriteData(BackgroundObject obj)
            {
                for (int i = 0; i < modifiers.Count; i++)
                    modifiers[i].WriteData(obj.modifiers[i]);
            }

            #endregion
        }

        public class SyncPrefabObject : SyncObject<PrefabObject>
        {
            public SyncPrefabObject() { }

            #region Values

            public List<SyncModifier> modifiers;

            #endregion

            #region Functions

            public override void ReadPacket(NetworkReader reader)
            {
                id = reader.ReadString();
                modifiers = new List<SyncModifier>();
                Packet.ReadPacketList(modifiers, reader);
            }

            public override void WritePacket(NetworkWriter writer)
            {
                writer.Write(id);
                if (modifiers == null)
                    modifiers = new List<SyncModifier>();
                Packet.WritePacketList(modifiers, writer);
            }

            public override void ReadData(PrefabObject obj)
            {
                id = obj.id;
                modifiers = new List<SyncModifier>(obj.modifiers.Select(SyncModifier.ToSyncData<SyncModifier>));
            }

            public override void WriteData(PrefabObject obj)
            {
                for (int i = 0; i < modifiers.Count; i++)
                    modifiers[i].WriteData(obj.modifiers[i]);
            }

            #endregion
        }

        #endregion
    }
}
