using System;
using System.Collections.Generic;
using System.Linq;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using SimpleJSON;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class CustomMenuList
    {
        public CustomMenuList() { }

        public CustomMenuList(string name) => this.name = name;

        public CustomMenuList(List<MenuBase> interfaces) => this.interfaces = interfaces;

        #region Values

        /// <summary>
        /// Name of the interface list.
        /// </summary>
        public string name;

        /// <summary>
        /// Action to run when the interface list is being cleared.
        /// </summary>
        public Action<CustomMenuList> onClear;

        /// <summary>
        /// All loaded interfaces.
        /// </summary>
        public List<MenuBase> interfaces;

        /// <summary>
        /// The default interface to load.
        /// </summary>
        public string defaultInterfaceID;

        /// <summary>
        /// The amount of interfaces loaded.
        /// </summary>
        public int Count => interfaces.Count;

        public MenuBase this[int index]
        {
            get => interfaces[index];
            set => interfaces[index] = value;
        }

        public MenuBase this[string id]
        {
            get => interfaces.Find(x => x.id == id);
            set
            {
                if (interfaces.TryFindIndex(x => x.id == id, out int index))
                    interfaces[index] = value;
                else
                    Add(value);
            }
        }

        /// <summary>
        /// Checks if the currently open interface is in the interface list.
        /// </summary>
        public bool CurrentInterfaceInList => Contains(InterfaceManager.inst.CurrentInterface);

        /// <summary>
        /// Interfaces that have been opened.
        /// </summary>
        public List<MenuBase> interfaceChain;

        /// <summary>
        /// Checks if there are any interfaces that are opened.
        /// </summary>
        public bool HasChain => interfaceChain.Count > 0;

        #endregion

        #region Methods

        /// <summary>
        /// Opens the default interface.
        /// </summary>
        public void OpenDefaultInterface() => SetCurrentInterface(defaultInterfaceID);

        /// <summary>
        /// Closes all interfaces and opens an interface.
        /// </summary>
        /// <param name="menu">Interface to open.</param>
        public void SetCurrentInterface(MenuBase menu)
        {
            CloseMenus();
            InterfaceManager.inst.CurrentInterface = menu;
            interfaceChain.Add(menu);
            menu.StartGeneration();
        }

        /// <summary>
        /// Closes all interfaces and opens an interface.
        /// </summary>
        /// <param name="id">Interface ID to find. If no interface is found, do nothing.</param>
        public void SetCurrentInterface(string id)
        {
            if (TryFind(id, out MenuBase menu))
                SetCurrentInterface(menu);
        }

        /// <summary>
        /// Exits the current interface and returns to the previous.
        /// </summary>
        public void ExitInterface()
        {
            if (HasChain)
            {
                CloseMenus();
                interfaceChain.RemoveAt(interfaceChain.Count - 1);
                if (!HasChain)
                    return;

                SetCurrentInterface(interfaceChain.Last());
            }
        }

        /// <summary>
        /// Closes and clears all interfaces.
        /// </summary>
        public void CloseMenus()
        {
            onClear?.Invoke(this);
            onClear = null;

            InterfaceManager.inst.CloseMenus();
        }

        /// <summary>
        /// Clears interface data and stops interface generation.
        /// </summary>
        /// <param name="clearThemes">If interface themes should be cleared.</param>
        /// <param name="stopGenerating">If the current interface should stop generating.</param>
        public void Clear(bool clearThemes = true, bool stopGenerating = true)
        {
            for (int i = 0; i < interfaces.Count; i++)
            {
                try
                {
                    interfaces[i].Clear();
                }
                catch
                {

                }
            }
            interfaces.Clear();

            InterfaceManager.inst.Clear(clearThemes, stopGenerating);
        }

        /// <summary>
        /// Removes an interface from the interface list and optionally closes it.
        /// </summary>
        /// <param name="id">ID to match and remove.</param>
        /// <param name="close">If the interface should close if it is the currently opened interface.</param>
        public void Remove(string id, bool close = true)
        {
            if (TryFind(id, out MenuBase menu))
                Remove(menu, close);
        }

        /// <summary>
        /// Removes an interface from the interface list and optionally closes it.
        /// </summary>
        /// <param name="menu">Interface to remove.</param>
        /// <param name="close">If the interface should close if it is the currently opened interface.</param>
        public void Remove(MenuBase menu, bool close = true)
        {
            interfaces.Remove(menu);
            if (close && menu.id == InterfaceManager.inst.CurrentInterface.id)
                CloseMenus();
        }

        /// <summary>
        /// Adds an interface to the interface list and optionally opens it.
        /// </summary>
        /// <param name="menu">Interface to add.</param>
        /// <param name="open">If the interface should be opened.</param>
        public void Add(MenuBase menu, bool open = false)
        {
            interfaces.Add(menu);
            if (open)
                SetCurrentInterface(menu);
        }

        /// <summary>
        /// Tries to find an interface with a matching ID.
        /// </summary>
        /// <param name="id">ID to match.</param>
        /// <param name="menu">Output interface.</param>
        /// <returns>Returns true if an interface was found, otherwise returns false.</returns>
        public bool TryFind(string id, out MenuBase menu)
        {
            menu = this[id];
            return menu;
        }

        /// <summary>
        /// Checks if the interface list contains an interface with a matching ID.
        /// </summary>
        /// <param name="id">ID to match.</param>
        /// <returns>Returns true if an interface was found, otherwise returns false.</returns>
        public bool Contains(string id) => this[id];

        /// <summary>
        /// Checks if the interface list contains an interface.
        /// </summary>
        /// <param name="menu">Interface to match.</param>
        /// <returns>Returns true if an interface was found, otherwise returns false.</returns>
        public bool Contains(MenuBase menu) => interfaces.Contains(menu);

        /// <summary>
        /// Parses a Custom Menu List.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="open">If the default interface should open.</param>
        /// <returns>Returns a parsed custom menu list.</returns>
        public static CustomMenuList Parse(JSONNode jn, bool open = true)
        {
            var customMenuList = new CustomMenuList(jn["name"]);
            customMenuList.LoadInterfaces(jn["branches"]);

            if (!string.IsNullOrEmpty(jn["default_branch"]))
            {
                customMenuList.defaultInterfaceID = jn["default_branch"];
                if (open)
                    customMenuList.SetCurrentInterface(customMenuList.defaultInterfaceID);
            }

            return customMenuList;
        }

        /// <summary>
        /// Loads a range of interfaces.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void LoadInterfaces(JSONNode jn) => interfaces.AddRange(ParseInterfaces(jn));

        /// <summary>
        /// Parses a range of interfaces.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns an IEnumerable of <see cref="MenuBase"/>.</returns>
        public IEnumerable<MenuBase> ParseInterfaces(JSONNode jn)
        {
            if (!jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
            {
                var jnChild = jn[i];

                if (jnChild["file"] != null)
                {
                    if (jnChild["path"] != null)
                        InterfaceManager.inst.MainDirectory = InterfaceManager.inst.ParseText(jnChild["path"]);

                    if (!InterfaceManager.inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                        InterfaceManager.inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, InterfaceManager.inst.MainDirectory);

                    var path = RTFile.CombinePaths(InterfaceManager.inst.MainDirectory, $"{jnChild["file"].Value}{FileFormat.LSI.Dot()}");

                    if (!RTFile.FileExists(path))
                    {
                        CoreHelper.LogError($"Interface {jnChild["file"]} does not exist!");

                        continue;
                    }

                    var interfaceJN = JSON.Parse(RTFile.ReadFromFile(path));

                    var menu = CustomMenu.Parse(interfaceJN);
                    menu.filePath = path;

                    Add(menu);

                    continue;
                }

                yield return CustomMenu.Parse(jnChild);
            }
        }

        /// <summary>
        /// Parses a global interface bool function.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public bool ParseIfFunctionSingle(JSONNode jn)
        {
            var parameters = jn["params"];
            string name = jn["name"];

            switch (name)
            {
                case "LIST_ContainsInterface":
                    {
                        Contains(parameters.Get(0, "id"));
                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// Parses a global interface function.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void ParseFunctionSingle(JSONNode jn)
        {
            var parameters = jn["params"];
            string name = jn["name"];

            switch (name)
            {
                case "LIST_OpenDefaultInterface":
                    {
                        OpenDefaultInterface();
                        break;
                    }
                case "LIST_ExitInterface":
                    {
                        ExitInterface();
                        break;
                    }
                case "LIST_SetCurrentInterface":
                    {
                        var id = parameters.Get(0, "id");
                        if (id == null)
                            break;

                        SetCurrentInterface(id);
                        break;
                    }
                case "LIST_AddInterface":
                    {
                        LoadInterfaces(parameters.Get(0, "interfaces"));
                        SetCurrentInterface(parameters.Get(1, "open_id"));
                        break;
                    }
                case "LIST_RemoveInterface":
                    {
                        Remove(parameters.Get(0, "id"));
                        break;
                    }
                case "LIST_ClearInterfaces":
                    {
                        Clear();
                        break;
                    }
                case "LIST_CloseInterfaces":
                    {
                        CloseMenus();
                        break;
                    }
            }
        }

        #endregion

        public static implicit operator bool(CustomMenuList menuList) => menuList != null;
    }
}
