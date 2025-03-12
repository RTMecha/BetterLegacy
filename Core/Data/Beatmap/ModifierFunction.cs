using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Beatmap
{
    public abstract class ModifierFunction
    {
        public ModifierFunction(string key) => this.key = key;

        public string key;

    }

    public class ModifierVariableFunction<T> : ModifierFunction
    {
        public ModifierVariableFunction(string key, Func<Modifier<T>, string> get) : base(key) => this.get = get;

        public Func<Modifier<T>, string> get;
    }

    public class ModifierTriggerFunction<T> : ModifierFunction
    {
        public ModifierTriggerFunction(string key, Predicate<Modifier<T>> trigger) : base(key) => this.trigger = trigger;

        public Predicate<Modifier<T>> trigger;
    }

    public class ModifierActionFunction<T> : ModifierFunction
    {
        public ModifierActionFunction(string key, Action<Modifier<T>> action) : base(key) => this.action = action;

        public Action<Modifier<T>> action;
    }
}
