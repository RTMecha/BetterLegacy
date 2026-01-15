using System;
using System.IO;

using UnityEngine;

namespace BetterLegacy.Core.Data.Network
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class NetworkReader : IDisposable
    {
        #region Constructors

        public NetworkReader(Stream stream) => reader = new BinaryReader(stream);

        public NetworkReader(ArraySegment<byte> data) : this(new MemoryStream(data.Array)) { }

        #endregion

        #region Values

        public readonly BinaryReader reader;

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
        public Color ReadColor() => new Color(ReadSingle(), ReadSingle(), ReadSingle());

        public void Dispose()
        {
            reader.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
