using System;
using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data.Network
{
    /// <summary>
    /// Indicates an object can be sent between client and server.
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// Reads packet data.
        /// </summary>
        /// <param name="reader">The current network reader.</param>
        public void ReadPacket(NetworkReader reader);

        /// <summary>
        /// Writes object values to packet data.
        /// </summary>
        /// <param name="writer">The current network writer.</param>
        public void WritePacket(NetworkWriter writer);
    }

    // this class tests IPacket
    class PacketTest : IPacket
    {
        public PacketTest() { }

        public int number;

        public Vector2 position;

        public List<int> numbers = new List<int>();

        public List<SubClass> subClasses = new List<SubClass>();

        public void ReadPacket(NetworkReader reader)
        {
            // reads a basic number
            number = reader.ReadInt32();
            // reads a vector2 value
            position = reader.ReadVector2();
            // reads a simple list
            var count = reader.ReadInt32();
            numbers.Clear();
            for (int i = 0; i < count; i++)
                numbers.Add(reader.ReadInt32());
            // reads an advanced list
            Packet.ReadPacketList(subClasses, reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            // writes a basic number
            writer.Write(number);
            // writes a vector2 value
            writer.Write(position);
            // writes a simple list
            writer.Write(numbers.Count);
            for (int i = 0; i < numbers.Count; i++)
                writer.Write(numbers[i]);
            // writes an advanced list
            Packet.WritePacketList(subClasses, writer);
        }

        // tests sub data in packets
        public class SubClass : IPacket
        {
            public int number;
            public int offset;

            public void ReadPacket(NetworkReader reader)
            {
                number = reader.ReadInt32();
                offset = reader.ReadInt32();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(number);
                writer.Write(offset);
            }
        }
    }

    /// <summary>
    /// Helper class for managing packets.
    /// </summary>
    public static class Packet
    {
        /// <summary>
        /// Creates an object from packet data.
        /// </summary>
        /// <typeparam name="T">Type of the object to create. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="data">Packet data to read.</param>
        /// <returns>Returns a new <typeparamref name="T"/>.</returns>
        public static T CreateFromPacket<T>(ArraySegment<byte> data) where T : IPacket, new()
        {
            var obj = new T();
            obj.ReadPacket(data);
            return obj;
        }

        /// <summary>
        /// Creates an object from packet data.
        /// </summary>
        /// <typeparam name="T">Type of the object to create. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="reader">The current network reader.</param>
        /// <returns>Returns a new <typeparamref name="T"/>.</returns>
        public static T CreateFromPacket<T>(NetworkReader reader) where T : IPacket, new()
        {
            var obj = new T();
            obj.ReadPacket(reader);
            return obj;
        }

        /// <summary>
        /// Creates an audio clip from packet data.
        /// </summary>
        /// <param name="data">Packet data to read.</param>
        /// <returns>Returns a new <see cref="AudioClip"/>.</returns>
        public static AudioClip AudioClipFromPacket(ArraySegment<byte> data)
        {
            using var reader = new NetworkReader(data);
            var audioClip = AudioClip.Create(reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), false);
            audioClip.ReadPacket(reader, true);
            return audioClip;
        }

        /// <summary>
        /// Creates an audio clip from packet data.
        /// </summary>
        /// <param name="reader">The current network reader.</param>
        /// <returns>Returns a new <see cref="AudioClip"/>.</returns>
        public static AudioClip AudioClipFromPacket(NetworkReader reader)
        {
            var audioClip = AudioClip.Create(reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), false);
            audioClip.ReadPacket(reader, true);
            return audioClip;
        }

        /// <summary>
        /// Reads the packet data.
        /// </summary>
        /// <param name="data">Packet data to read.</param>
        public static void ReadPacket(this IPacket obj, ArraySegment<byte> data)
        {
            using var reader = new NetworkReader(data);
            obj.ReadPacket(reader);
        }

        /// <summary>
        /// Reads packet data.
        /// </summary>
        /// <param name="reader">The current network reader.</param>
        public static void ReadPacket(this AudioClip clip, NetworkReader reader, bool read = false)
        {
            if (!read)
            {
                reader.ReadString();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
            }

            clip.SetData(reader.ReadSingleArray(), 0);
        }

        /// <summary>
        /// Writes object values to packet data.
        /// </summary>
        /// <param name="writer">The current network writer.</param>
        public static void WritePacket(this AudioClip clip, NetworkWriter writer)
        {
            writer.Write(clip.name);
            writer.Write(clip.samples);
            writer.Write(clip.channels);
            writer.Write(clip.frequency);
            float[] data = new float[clip.frequency * clip.channels];
            clip.GetData(data, 0);
            writer.Write(data);
        }

        /// <summary>
        /// Converts to packet data.
        /// </summary>
        /// <returns>Returns packet data.</returns>
        public static ArraySegment<byte> ToPacket(this IPacket obj)
        {
            using var writer = new NetworkWriter();
            obj.WritePacket(writer);
            return writer.GetData();
        }

        /// <summary>
        /// Converts to packet data.
        /// </summary>
        /// <returns>Returns packet data.</returns>
        public static ArraySegment<byte> ToPacket(this AudioClip clip)
        {
            using var writer = new NetworkWriter();
            clip.WritePacket(writer);
            return writer.GetData();
        }

        /// <summary>
        /// Reads list data from packet data.
        /// </summary>
        /// <typeparam name="T">Type of the object in the list to read. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="list">List to read to.</param>
        /// <param name="reader">The current network reader.</param>
        public static void ReadPacketList<T>(List<T> list, NetworkReader reader) where T : IPacket, new()
        {
            var count = reader.ReadInt32();
            list.Clear();
            for (int i = 0; i < count; i++)
                list.Add(CreateFromPacket<T>(reader));
        }

        /// <summary>
        /// Reads dictionary data from packet data.
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
        /// <param name="dictionary">Dictionary to read to.</param>
        /// <param name="reader">The current network reader.</param>
        /// <param name="getKey">Get key function.</param>
        public static void ReadPacketDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, NetworkReader reader, Func<TKey> getKey) where TValue : IPacket, new()
        {
            var count = reader.ReadInt32();
            dictionary.Clear();
            for (int i = 0; i < count; i++)
            {
                var key = getKey.Invoke();
                var value = CreateFromPacket<TValue>(reader);
                dictionary[key] = value;
            }
        }

        /// <summary>
        /// Writes a list of <see cref="IPacket"/> objects.
        /// </summary>
        /// <typeparam name="T">Type of the object in the list to write. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="list">List to write from.</param>
        /// <param name="writer">The current network writer.</param>
        public static void WritePacketList<T>(List<T> list, NetworkWriter writer) where T : IPacket
        {
            writer.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
                list[i].WritePacket(writer);
        }

        /// <summary>
        /// Writes a dictionary of <see cref="IPacket"/> objects.
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
        /// <param name="dictionary">Dictionary to write from.</param>
        /// <param name="writer">The current network writer.</param>
        /// <param name="writeKey">Write key function.</param>
        /// <param name="writeValue">Write value function.</param>
        public static void WritePacketDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, NetworkWriter writer, Action<TKey> writeKey, Action<TValue> writeValue)
        {
            writer.Write(dictionary.Count);
            foreach (var keyValuePair in dictionary)
            {
                writeKey.Invoke(keyValuePair.Key);
                writeValue.Invoke(keyValuePair.Value);
            }
        }
    }

    /// <summary>
    /// Represents a list of objects that can read from / write to a packet..
    /// </summary>
    /// <typeparam name="T">Type of the items in the list.</typeparam>
    public class PacketList<T> : IPacket where T : IPacket, new()
    {
        public PacketList(List<T> list) => this.list = list;

        public List<T> list;

        public int Count => list.Count;

        public T this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public void ReadPacket(NetworkReader reader) => Packet.ReadPacketList(list, reader);

        public void WritePacket(NetworkWriter writer) => Packet.WritePacketList(list, writer);
    }
}
