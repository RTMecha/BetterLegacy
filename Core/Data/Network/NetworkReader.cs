using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Network
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class NetworkReader : IDisposable
    {
        #region Constructors

        public NetworkReader(Stream stream) => reader = new BinaryReader(stream);

        public NetworkReader(byte[] array) : this(new MemoryStream(array)) { }

        public NetworkReader(ArraySegment<byte> data) : this(data.Array) { }

        #endregion

        #region Values

        readonly BinaryReader reader;

        public long Count => reader.BaseStream.Length;

        #endregion

        #region Functions

        public bool ReadBoolean() => reader.ReadBoolean();
        public byte ReadByte() => reader.ReadByte();
        public byte[] ReadBytes(int count) => reader.ReadBytes(count);
        public char ReadChar() => reader.ReadChar();
        public char[] ReadChars(int count) => reader.ReadChars(count);
        public decimal ReadDecimal() => reader.ReadDecimal();
        public double ReadDouble() => reader.ReadDouble();
        public short ReadInt16() => reader.ReadInt16();
        public int ReadInt32() => reader.ReadInt32();
        public long ReadInt64() => reader.ReadInt64();
        public sbyte ReadSByte() => reader.ReadSByte();
        public float ReadSingle() => reader.ReadSingle();
        public string ReadString() => reader.ReadString();
        public ushort ReadUInt16() => reader.ReadUInt16();
        public uint ReadUInt32() => reader.ReadUInt32();
        public ulong ReadUInt64() => reader.ReadUInt64();
        public Vector2 ReadVector2() => new Vector2(ReadSingle(), ReadSingle());
        public Vector3 ReadVector3() => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        public Vector4 ReadVector4() => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        public Vector2Int ReadVector2Int() => new Vector2Int(ReadInt32(), ReadInt32());
        public Vector3Int ReadVector3Int() => new Vector3Int(ReadInt32(), ReadInt32(), ReadInt32());
        public Color ReadColor() => new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        public bool[] ReadBooleanArray()
        {
            var count = ReadInt32();
            var array = new bool[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadBoolean();
            return array;
        }
        public string[] ReadStringArray()
        {
            var count = ReadInt32();
            var array = new string[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadString();
            return array;
        }
        public short[] ReadInt16Array()
        {
            var count = ReadInt32();
            var array = new short[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadInt16();
            return array;
        }
        public int[] ReadInt32Array()
        {
            var count = ReadInt32();
            var array = new int[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadInt32();
            return array;
        }
        public long[] ReadInt64Array()
        {
            var count = ReadInt32();
            var array = new long[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadInt64();
            return array;
        }
        public ushort[] ReadUInt16Array()
        {
            var count = ReadInt32();
            var array = new ushort[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadUInt16();
            return array;
        }
        public uint[] ReadUInt32Array()
        {
            var count = ReadInt32();
            var array = new uint[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadUInt32();
            return array;
        }
        public ulong[] ReadUInt64Array()
        {
            var count = ReadInt32();
            var array = new ulong[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadUInt64();
            return array;
        }
        public float[] ReadSingleArray()
        {
            var count = ReadInt32();
            var array = new float[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadSingle();
            return array;
        }
        public double[] ReadDoubleArray()
        {
            var count = ReadInt32();
            var array = new double[count];
            for (int i = 0; i < count; i++)
                array[i] = ReadDouble();
            return array;
        }
        public List<T> ReadList<T>(Func<T> read)
        {
            var count = ReadInt32();
            var list = new List<T>();
            for (int i = 0; i < count; i++)
                list.Add(read.Invoke());
            return list;
        }
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<TKey> readKey, Func<TValue> readValue)
        {
            var count = ReadInt32();
            var dictionary = new Dictionary<TKey, TValue>();
            for (int i = 0; i < count; i++)
            {
                var key = readKey.Invoke();
                var value = readValue.Invoke();
                dictionary[key] = value;
            }
            return dictionary;
        }
        public Sprite ReadSprite()
        {
            var hasIcon = ReadBoolean();
            if (hasIcon)
            {
                var byteCount = ReadInt32();
                var data = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                    data[i] = ReadByte();
                return SpriteHelper.LoadSprite(data);
            }
            return null;
        }
        public Texture2D ReadTexture2D()
        {
            var exists = ReadBoolean();
            if (exists)
            {
                var byteCount = ReadInt32();
                var data = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                    data[i] = ReadByte();
                return SpriteHelper.LoadTexture(data);
            }
            return null;
        }

        public void Dispose()
        {
            reader.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
