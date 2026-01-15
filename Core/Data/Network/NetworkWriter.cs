using System;
using System.IO;
using System.Text;

using UnityEngine;

namespace BetterLegacy.Core.Data.Network
{
    // class based on https://github.com/Aiden-ytarame/AttributeNetworkWrapper
    public class NetworkWriter : IDisposable
    {
        #region Constructors

        public NetworkWriter() => memoryStream.Position = 0;

        #endregion

        #region Values

        static MemoryStream memoryStream = new MemoryStream(1024);
        public readonly BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);

        #endregion

        #region Functions

        public ArraySegment<byte> GetData() => new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);

        public void Write(string value) => writer.Write(value);
        public void Write(float value) => writer.Write(value);
        public void Write(ulong value) => writer.Write(value);
        public void Write(long value) => writer.Write(value);
        public void Write(uint value) => writer.Write(value);
        public void Write(int value) => writer.Write(value);
        public void Write(ushort value) => writer.Write(value);
        public void Write(short value) => writer.Write(value);
        public void Write(double value) => writer.Write(value);
        public void Write(char[] chars, int index, int count) => writer.Write(chars, index, count);
        public void Write(char[] chars) => writer.Write(chars);
        public void Write(char ch) => writer.Write(ch);
        public void Write(byte[] buffer, int index, int count) => writer.Write(buffer, index, count);
        public void Write(byte[] buffer) => writer.Write(buffer);
        public void Write(sbyte value) => writer.Write(value);
        public void Write(byte value) => writer.Write(value);
        public void Write(decimal value) => writer.Write(value);
        public void Write(bool value) => writer.Write(value);
        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }
        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
        public void Write(Vector4 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }
        public void Write(Vector2Int value)
        {
            Write(value.x);
            Write(value.y);
        }
        public void Write(Vector3Int value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
        public void Write(Color value)
        {
            Write(value.r);
            Write(value.g);
            Write(value.b);
            Write(value.a);
        }

        public void Dispose()
        {
            writer.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
