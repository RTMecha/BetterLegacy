using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Menus.UI.Interfaces
{
    /// <summary>
    /// Custom list of interfaces that contain branches of interfaces.
    /// </summary>
    public class CustomMenuList : Exists
    {
        public CustomMenuList() { }

        public CustomMenuList(string name) => this.name = name;

        public CustomMenuList(List<MenuBase> interfaces) => this.interfaces = interfaces;

        #region Values

        /// <summary>
        /// Identification of the interface list.
        /// </summary>
        public string id;

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
        public List<MenuBase> interfaces = new List<MenuBase>();

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
        public List<MenuBase> interfaceChain = new List<MenuBase>();

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
        public void SetCurrentInterface(MenuBase menu, bool addToChain = true)
        {
            InterfaceManager.inst.CloseMenus();
            InterfaceManager.inst.CurrentInterface = menu;
            if (addToChain)
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
            // prevent exiting if the interface chain only contains 1 interface
            if (interfaceChain.Count <= 1)
                return;

            InterfaceManager.inst.CloseMenus();
            interfaceChain.RemoveAt(interfaceChain.Count - 1);
            if (!HasChain)
                return;

            SetCurrentInterface(interfaceChain.Last(), false);
        }

        /// <summary>
        /// Clears the open interface chain.
        /// </summary>
        public void ClearChain()
        {
            for (int i = 0; i < interfaceChain.Count; i++)
            {
                try
                {
                    interfaceChain[i].Clear();
                }
                catch
                {

                }
            }
            interfaceChain.Clear();
        }

        /// <summary>
        /// Closes and clears all interfaces.
        /// </summary>
        public void CloseMenus()
        {
            onClear?.Invoke(this);
            onClear = null;
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
                InterfaceManager.inst.CloseMenus();
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
        public static CustomMenuList Parse(JSONNode jn, bool open = true, string openInterfaceID = null, List<string> branchChain = null, Dictionary<string, JSONNode> customVariables = null)
        {
            var customMenuList = new CustomMenuList(jn["name"]);

            var variables = InterfaceManager.inst.ParseVarFunction(jn["variables"]);
            if (jn["variables"] != null)
            {
                if (variables.IsObject)
                {
                    foreach (var keyValuePair in variables.Linq)
                    {
                        var value = InterfaceManager.inst.ParseVarFunction(keyValuePair.Value, customVariables: customVariables);
                        if (value == null)
                            continue;

                        if (customVariables == null)
                            customVariables = new Dictionary<string, JSONNode>();

                        customVariables[keyValuePair.Key] = value;
                    }
                }

                if (variables.IsArray)
                {
                    for (int i = 0; i < variables.Count; i++)
                    {
                        var variable = variables[i];
                        var name = variable["name"];
                        if (name == null || !name.IsString)
                            continue;

                        if (customVariables == null)
                            customVariables = new Dictionary<string, JSONNode>();

                        var value = InterfaceManager.inst.ParseVarFunction(variable["value"], customVariables: customVariables);
                        if (value == null)
                            continue;

                        if (customVariables == null)
                            customVariables = new Dictionary<string, JSONNode>();

                        customVariables[name] = value;
                    }
                }
            }

            var branches = InterfaceManager.inst.ParseVarFunction(jn["branches"], customVariables: customVariables);
            if (branches != null)
                customMenuList.LoadInterfaces(branches, customVariables);

            var defaultBranch = InterfaceManager.inst.ParseVarFunction(jn["default_branch"], customVariables: customVariables);
            if (defaultBranch != null && defaultBranch.IsString)
            {
                customMenuList.defaultInterfaceID = defaultBranch;
                if (open)
                {
                    if (branchChain != null)
                    {
                        foreach (var id in branchChain)
                        {
                            if (customMenuList.TryFind(id, out MenuBase menu))
                                customMenuList.interfaceChain.Add(menu);
                        }
                    }

                    customMenuList.SetCurrentInterface(!string.IsNullOrEmpty(openInterfaceID) ? openInterfaceID : customMenuList.defaultInterfaceID);
                    InterfaceManager.inst.PlayMusic();
                }
            }

            return customMenuList;
        }

        /// <summary>
        /// Loads a range of interfaces.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void LoadInterfaces(JSONNode jn, Dictionary<string, JSONNode> customVariables = null) => interfaces.AddRange(ParseInterfaces(jn, customVariables));

        /// <summary>
        /// Parses a collection of interfaces from JSON. JSON can be an object or an array. If the JSON is an object, the key of each interface will replace the ID of the interface.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed collection of <see cref="MenuBase"/>.</returns>
        public static IEnumerable<MenuBase> ParseInterfaces(JSONNode jn, Dictionary<string, JSONNode> customVariables = null)
        {
            if (jn == null)
                yield break;

            if (jn.IsObject)
            {
                foreach (var keyValuePair in jn.Linq)
                {
                    var key = keyValuePair.Key;
                    var jnChild = InterfaceManager.inst.ParseVarFunction(keyValuePair.Value, customVariables: customVariables);
                    if (jnChild == null)
                        continue;

                    if (jnChild.IsArray)
                    {
                        var interfaces = ParseInterfaces(jnChild, customVariables);
                        foreach (var innerMenu in interfaces)
                            yield return innerMenu;

                        continue;
                    }

                    var menu = ParseInterface(jnChild, customVariables);
                    menu.id = key;
                    yield return menu;
                }
            }

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    var jnChild = InterfaceManager.inst.ParseVarFunction(jn[i], customVariables: customVariables);
                    if (jnChild == null)
                        continue;

                    if (jnChild.IsArray)
                    {
                        var interfaces = ParseInterfaces(jnChild, customVariables);
                        foreach (var menu in interfaces)
                            yield return menu;

                        continue;
                    }

                    yield return ParseInterface(jnChild, customVariables);
                }
            }
        }

        /// <summary>
        /// Parses an interface from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="MenuBase"/>.</returns>
        public static MenuBase ParseInterface(JSONNode jn, Dictionary<string, JSONNode> customVariables = null)
        {
            var file = InterfaceManager.inst.ParseVarFunction(jn["file"], customVariables: customVariables);
            if (file == null)
                return CustomMenu.Parse(jn, customVariables);

            var jnPath = InterfaceManager.inst.ParseVarFunction(jn["path"], customVariables: customVariables);
            if (jnPath != null)
                InterfaceManager.inst.MainDirectory = InterfaceManager.inst.ParseText(jnPath, customVariables);

            if (!InterfaceManager.inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                InterfaceManager.inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, InterfaceManager.inst.MainDirectory);

            var path = RTFile.CombinePaths(InterfaceManager.inst.MainDirectory, file + FileFormat.LSI.Dot());

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"Interface {jn["file"]} does not exist!");

                return null;
            }

            var menu = CustomMenu.Parse(JSON.Parse(RTFile.ReadFromFile(path)), customVariables);
            menu.filePath = path;

            return menu;
        }

        public override string ToString() => string.IsNullOrEmpty(id) ? name : $"{id} - {name}";

        #endregion
    }
}
