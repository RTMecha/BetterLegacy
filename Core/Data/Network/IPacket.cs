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
            number = reader.reader.ReadInt32();
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
        /// Reads the packet data.
        /// </summary>
        /// <param name="data">Packet data to read.</param>
        public static void ReadPacket(this IPacket obj, ArraySegment<byte> data)
        {
            using var reader = new NetworkReader(data);
            obj.ReadPacket(reader);
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
        /// Reads list data from packet data.
        /// </summary>
        /// <typeparam name="T">Type of the object list to create. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="data">Packet data to read.</param>
        public static List<T> CreateListFromPacket<T>(ArraySegment<byte> data) where T : IPacket, new()
        {
            var list = new List<T>();
            using var reader = new NetworkReader(data);
            ReadPacketList(list, reader);
            return list;
        }

        /// <summary>
        /// Reads list data from packet data.
        /// </summary>
        /// <typeparam name="T">Type of the object in the list to read. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="list">List to read.</param>
        /// <param name="reader">The current network reader.</param>
        public static void ReadPacketList<T>(List<T> list, NetworkReader reader) where T : IPacket, new()
        {
            var count = reader.ReadInt32();
            list.Clear();
            for (int i = 0; i < count; i++)
                list.Add(CreateFromPacket<T>(reader));
        }

        /// <summary>
        /// Writes a list of <see cref="IPacket"/> objects.
        /// </summary>
        /// <typeparam name="T">Type of the object in the list to write. Type must be <see cref="IPacket"/>.</typeparam>
        /// <param name="list">List to write.</param>
        /// <param name="writer">The current network writer.</param>
        public static void WritePacketList<T>(List<T> list, NetworkWriter writer) where T : IPacket
        {
            writer.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
                list[i].WritePacket(writer);
        }
    }
}
