using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Used for indexing an object.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="obj"/>.</typeparam>
    public struct Indexer<T>
    {
        public Indexer(int index, T obj)
        {
            this.index = index;
            this.obj = obj;
        }

        public Indexer(T obj) : this(0, obj) { }

        /// <summary>
        /// Index of <see cref="obj"/>.
        /// </summary>
        public int index;

        /// <summary>
        /// Object reference.
        /// </summary>
        public T obj;

        /// <summary>
        /// Gets the index of the <see cref="obj"/> in a collection.
        /// </summary>
        /// <param name="collection">Collection to get the index from.</param>
        public void GetIndex(List<T> collection) => index = collection.IndexOf(obj);

        /// <summary>
        /// Gets the index of the <see cref="obj"/> in a collection.
        /// </summary>
        /// <param name="collection">Collection to get the index from.</param>
        public void GetIndex(T[] collection) => index = Array.IndexOf(collection, obj);
    }
}
