using System;
using System.Collections.Generic;
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
        readonly BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);

        #endregion

        #region Functions

        /// <summary>
        /// Gets the byte data of the current writer.
        /// </summary>
        /// <returns>Returns a byte array.</returns>
        public ArraySegment<byte> GetData() => new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);

        public void Write(string value) => writer.Write(value ?? string.Empty);
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
        public void Write(bool[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(string[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(short[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(int[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(long[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(ushort[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(uint[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(ulong[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(float[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(double[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write<T>(List<T> list, Action<T> write)
        {
            Write(list.Count);
            for (int i = 0; i < list.Count; i++)
                write.Invoke(list[i]);
        }
        public void Write<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Action<TKey> writeKey, Action<TValue> writeValue)
        {
            Write(dictionary.Count);
            foreach (var keyValuePair in dictionary)
            {
                writeKey.Invoke(keyValuePair.Key);
                writeValue.Invoke(keyValuePair.Value);
            }
        }
        public void Write(Sprite sprite)
        {
            bool hasIcon = sprite && sprite.texture;
            writer.Write(hasIcon);
            if (hasIcon)
            {
                var data = sprite.texture.EncodeToPNG();
                writer.Write(data.Length);
                for (int i = 0; i < data.Length; i++)
                    writer.Write(data[i]);
            }
        }
        public void Write(Texture2D texture2D)
        {
            bool hasIcon = texture2D;
            writer.Write(hasIcon);
            if (hasIcon)
            {
                var data = texture2D.EncodeToPNG();
                writer.Write(data.Length);
                for (int i = 0; i < data.Length; i++)
                    writer.Write(data[i]);
            }
        }

        public void Dispose()
        {
            writer.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
