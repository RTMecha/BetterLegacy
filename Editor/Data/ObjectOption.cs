using System;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class ObjectOption
    {
        public ObjectOption(string name, string hint, Action<TimelineObject> action)
        {
            this.name = name;
            this.hint = hint;
            this.action = action;
        }

        public string name;
        public string hint;
        public Action<TimelineObject> action;

        public void Create()
        {
            try
            {
                ObjectEditor.inst.CreateNewObject(action);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }
    }
}
