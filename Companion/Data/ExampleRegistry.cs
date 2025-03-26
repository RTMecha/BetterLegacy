using System;
using System.Collections.Generic;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data
{
    public static class TestRegistry
    {
        static ExampleRegistry registry = new ExampleRegistry();

        public static void Test()
        {
            registry.RegisterItem("TEST", new ExampleRegistryGetter<string>(() => "This is a test."));

            registry.GetItem<ExampleRegistryGetter<string>>("TEST").get();

            registry.RegisterItem("TEST_PARAMS", new ExampleRegistryGetter<string, int>(num => $"This is a test: {num}."));

            var result = registry.GetItem<ExampleRegistryGetter<string, int>>("TEST_PARAMS").get(0);
        }
    }

    /// <summary>
    /// Represents a registry of items.
    /// </summary>
    /// <typeparam name="T">Type of the item contained in the registry.</typeparam>
    /// <typeparam name="TGet">Type to get from the item.</typeparam>
    public class ExampleRegistry : Exists
    {
        public ExampleRegistry() { }

        List<ExampleRegistryItem> items = new List<ExampleRegistryItem>();

        public ExampleRegistryItem this[string key]
        {
            get => GetItem(key);
            set => OverrideItem(key, value);
        }

        /// <summary>
        /// Gets the number of items in the registry.
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Gets an item from the registry.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <returns>Returns a found item.</returns>
        public ExampleRegistryItem GetItem(string key) => items.Find(x => x.key == key);

        /// <summary>
        /// Gets an item from the registry.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <param name="defaultItem">Default item to return if none is found.</param>
        /// <returns>Returns a found item.</returns>
        public ExampleRegistryItem GetItem(string key, ExampleRegistryItem defaultItem) => items.Find(x => x.key == key) ?? defaultItem;

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <param name="index">Index of the item.</param>
        /// <returns>Returns a found item.</returns>
        public ExampleRegistryItem GetItem(int index) => items[index];

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <param name="index">Index of the item.</param>
        /// <param name="defaultItem">Default item to return if none is found.</param>
        /// <returns>Returns a found item.</returns>
        public ExampleRegistryItem GetItem(int index, ExampleRegistryItem defaultItem) => items.TryGetAt(index, out ExampleRegistryItem item) ? item : defaultItem;

        /// <summary>
        /// Gets an item from the registry.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="key">Key to find.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(string key) where T : ExampleRegistryItem => items.Find(x => x.key == key) as T;

        /// <summary>
        /// Gets an item from the registry.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="key">Key to find.</param>
        /// <param name="defaultItem">Default item to return if none is found.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(string key, T defaultItem) where T : ExampleRegistryItem => items.Find(x => x.key == key) is T result ? result : defaultItem;

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="index">Index of the item.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(int index) where T : ExampleRegistryItem => items[index] as T;

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="index">Index of the item.</param>
        /// <param name="defaultItem">Default item to return if none is found.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(int index, T defaultItem) where T : ExampleRegistryItem => items.TryGetAt(index, out ExampleRegistryItem item) && item is T result ? result : defaultItem;

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="predicate">Predicate to match.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(Predicate<ExampleRegistryItem> predicate) where T : ExampleRegistryItem => items.Find(predicate) as T;

        /// <summary>
        /// Gets an item from the registry at an index.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="predicate">Predicate to match.</param>
        /// <param name="defaultItem">Default item to return if none is found.</param>
        /// <returns>Returns a found item.</returns>
        public T GetItem<T>(Predicate<ExampleRegistryItem> predicate, T defaultItem) where T : ExampleRegistryItem => items.Find(predicate) is T result ? result : defaultItem;

        /// <summary>
        /// Registers an item to the registry.
        /// </summary>
        /// <param name="registryItem">Item to register.</param>
        public void RegisterItem(ExampleRegistryItem registryItem) => items.Add(registryItem);

        /// <summary>
        /// Registers an item to the registry.
        /// </summary>
        /// <param name="key">Key of the item.</param>
        /// <param name="registryItem">Item to register.</param>
        public void RegisterItem(string key, ExampleRegistryItem registryItem)
        {
            registryItem.key = key;
            items.Add(registryItem);
        }

        /// <summary>
        /// Overrides an item in the registry.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <param name="registryItem">Item to override with. If no item is found, then add the item.</param>
        public void OverrideItem(string key, ExampleRegistryItem registryItem)
        {
            if (items.TryFindIndex(x => x.key == key, out int index))
                items[index] = registryItem;
            else
                items.Add(registryItem);
        }

        /// <summary>
        /// Performs a for loop to the list. Action passes the item and its index.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public void ForLoop(Action<ExampleRegistryItem, int> action) => items.ForLoop(action);

        /// <summary>
        /// Performs a for loop to the list. Action passes the item.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/></typeparam>
        /// <param name="action">Action to perform for each element.</param>
        public void ForLoop(Action<ExampleRegistryItem> action) => items.ForLoop(action);

        /// <summary>
        /// Removes an item from the registry.
        /// </summary>
        /// <param name="key">Key to find.</param>
        public void RemoveItem(string key) => items.Remove(x => x.key == key);

        /// <summary>
        /// Removes an item from the registry at an index.
        /// </summary>
        /// <param name="index">Index of the item.</param>
        public void RemoveItem(int index) => items.RemoveAt(index);

        /// <summary>
        /// Clears the registry.
        /// </summary>
        public void Clear() => items.Clear();
    }

    /// <summary>
    /// Represents an item in a registry.
    /// </summary>
    public class ExampleRegistryItem : Exists
    {
        public ExampleRegistryItem() { }

        public ExampleRegistryItem(string key) => this.key = key;

        /// <summary>
        /// Key of the item.
        /// </summary>
        public string key;
    }

    /// <summary>
    /// Represents an item in a registry.
    /// </summary>
    /// <typeparam name="T">Type of the object in the item.</typeparam>
    public class ExampleRegistryItem<T> : ExampleRegistryItem
    {
        public ExampleRegistryItem() { }

        public ExampleRegistryItem(string key) : base(key) { }

        public ExampleRegistryItem(string key, T obj) : base(key) => this.obj = obj;

        public ExampleRegistryItem(T obj) => this.obj = obj;

        public T obj;
    }

    /// <summary>
    /// Represents an item in a registry.
    /// </summary>
    /// <typeparam name="TGet">Type to get from the item.</typeparam>
    public class ExampleRegistryGetter<TGet> : ExampleRegistryItem
    {
        public ExampleRegistryGetter() : base() { }

        public ExampleRegistryGetter(string key) : base(key) { }

        public ExampleRegistryGetter(string key, Func<TGet> get) : this(key) => this.get = get;

        public ExampleRegistryGetter(Func<TGet> get) => this.get = get;

        /// <summary>
        /// Item to get.
        /// </summary>
        public Func<TGet> get;
    }

    /// <summary>
    /// Represents an item in a registry.
    /// </summary>
    /// <typeparam name="TGet">Type to get from the item.</typeparam>
    /// <typeparam name="T">First parameter.</typeparam>
    public class ExampleRegistryGetter<TGet, T> : ExampleRegistryItem
    {
        public ExampleRegistryGetter() : base() { }

        public ExampleRegistryGetter(string key) : base(key) { }

        public ExampleRegistryGetter(string key, Func<T, TGet> get) : this(key) => this.get = get;

        public ExampleRegistryGetter(Func<T, TGet> get) => this.get = get;

        /// <summary>
        /// Item to get.
        /// </summary>
        public Func<T, TGet> get;
    }

    /// <summary>
    /// Represents an item in a registry.
    /// </summary>
    /// <typeparam name="TGet">Type to get from the item.</typeparam>
    /// <typeparam name="T1">First parameter.</typeparam>
    /// <typeparam name="T2">Second parameter.</typeparam>
    public class ExampleRegistryGetter<TGet, T1, T2> : ExampleRegistryItem
    {
        public ExampleRegistryGetter() : base() { }

        public ExampleRegistryGetter(string key) : base(key) { }

        public ExampleRegistryGetter(string key, Func<T1, T2, TGet> get) : this(key) => this.get = get;

        public ExampleRegistryGetter(Func<T1, T2, TGet> get) => this.get = get;

        /// <summary>
        /// Item to get.
        /// </summary>
        public Func<T1, T2, TGet> get;
    }
}
