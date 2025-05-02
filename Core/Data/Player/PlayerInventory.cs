using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents a players' inventory.
    /// </summary>
    public class PlayerInventory : Exists
    {
        public PlayerInventory() { }

        /// <summary>
        /// Currently selected item index.
        /// </summary>
        public int currentItemIndex;

        /// <summary>
        /// Currently selected item.
        /// </summary>
        public PlayerItem CurrentItem => GetItem(currentItemIndex);

        /// <summary>
        /// Items the player has in their inventory.
        /// </summary>
        public List<PlayerItem> items = new List<PlayerItem>();

        /// <summary>
        /// Gets an item at an index.
        /// </summary>
        /// <param name="index">Index of the item.</param>
        /// <returns>Returns the item at the index.</returns>
        public PlayerItem GetItem(int index)
        {
            var item = items.GetAt(index);
            item.inventory = this;
            return item;
        }

        /// <summary>
        /// Removes an item from the inventory.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>Returns true if the item was successfully removed, otherwise returns false.</returns>
        public bool RemoveItem(PlayerItem item) => items.Remove(item);
    }
}
