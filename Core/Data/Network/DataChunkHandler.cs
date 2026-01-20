using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Network
{
    public class DataChunkHandler : Exists, IDisposable
    {
        public DataChunkHandler() => writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);

        public string id;

        public long Position { get => memoryStream.Position; set => memoryStream.Position = value; }
        MemoryStream memoryStream = new MemoryStream(1024);
        readonly BinaryWriter writer;

        /// <summary>
        /// Gets the byte data of the current writer.
        /// </summary>
        /// <returns>Returns a byte array.</returns>
        public ArraySegment<byte> GetData() => new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);

        /// <summary>
        /// Writes chunk data from packet data.
        /// </summary>
        /// <param name="reader">The current network reader.</param>
        /// <returns>Returns <see langword="true"/> if the chunk data has reached the end, otherwise returns <see langword="false"/>.</returns>
        public bool WriteChunkData(NetworkReader reader, long dataCount)
        {
            if (Position >= dataCount)
                return true;

            byte[] data = reader.ReadBytes(8192);
            while (!data.IsEmpty())
            {
                writer.Write(data);
                data = reader.ReadBytes(8192);
            }
            return false;
        }

        public void Dispose()
        {
            writer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
