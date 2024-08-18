using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleJSON;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class CustomMenuList
    {
        public List<MenuBase> interfaces;

        public void Load(JSONNode jn)
        {
            interfaces.AddRange(LoadMenus(jn));
        }

        public IEnumerable<MenuBase> LoadMenus(JSONNode jn)
        {
            if (!jn.IsArray)
                yield break;

            for (int i = 0; i < jn.Count; i++)
                yield return CustomMenu.Parse(jn[i]);
        }
    }
}
