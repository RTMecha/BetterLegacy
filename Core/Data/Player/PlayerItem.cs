using SimpleJSON;
using System;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents an item that can be used by the player in-game.
    /// </summary>
    public class PlayerItem : PAObject<PlayerItem>
    {
        public PlayerItem() => id = GetNumberID();

        /// <summary>
        /// Name of the item.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// If the item consumes on use.
        /// </summary>
        public bool consumable;

        /// <summary>
        /// How many times the item has been used.
        /// </summary>
        public int uses;

        /// <summary>
        /// The amount of times the item can be used until it is removed from the inventory.
        /// </summary>
        public int maxUses;

        /// <summary>
        /// Function to run on use.
        /// </summary>
        public Action onUse;

        /// <summary>
        /// Name of the function to run on use.
        /// </summary>
        public string onUseFunc = string.Empty;

        /// <summary>
        /// Function to run on item consumed.
        /// </summary>
        public Action onConsume;

        /// <summary>
        /// Name of the function to rune on item consumed.
        /// </summary>
        public string onConsumeFunc = string.Empty;

        /// <summary>
        /// Inventory reference.
        /// </summary>
        public PlayerInventory inventory;

        public override void CopyData(PlayerItem orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            name = orig.name;

            consumable = orig.consumable;
            onConsumeFunc = orig.onConsumeFunc;

            onUseFunc = orig.onUseFunc;
            uses = orig.uses;
            maxUses = orig.maxUses;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetNumberID();
            name = jn["name"] ?? string.Empty;

            consumable = jn["consumable"].AsBool;
            onConsumeFunc = jn["consume_func"] ?? string.Empty;

            onUseFunc = jn["use_func"] ?? string.Empty;
            uses = jn["uses"].AsInt;
            maxUses = jn["max_uses"].AsInt;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetNumberID();
            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            if (consumable)
            {
                jn["consumable"] = consumable;
                if (!string.IsNullOrEmpty(onConsumeFunc))
                    jn["consume_func"] = onConsumeFunc;
            }

            if (!string.IsNullOrEmpty(onUseFunc))
                jn["use_func"] = onUseFunc;
            if (uses > 0)
                jn["uses"] = uses;
            if (maxUses > 0)
                jn["max_uses"] = maxUses;

            return jn;
        }

        /// <summary>
        /// Uses the item and if the item is consumable or it has reached the max use count, removes it from the inventory.
        /// </summary>
        public virtual void Use()
        {
            onUse?.Invoke();

            if (consumable)
                Consume();

            uses++;

            if (maxUses > 0 && uses >= maxUses)
                Consume();
        }

        /// <summary>
        /// Removes the item from the inventory.
        /// </summary>
        public virtual void Consume()
        {
            var consumed = inventory?.RemoveItem(this);
            if (consumed == true)
                onConsume?.Invoke();
        }

        public override string ToString() => string.IsNullOrEmpty(name) ? id : $"{id} - {name}";
    }
}
