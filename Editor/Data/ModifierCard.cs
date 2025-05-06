using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Editor.Data
{
    public class ModifierCard<T> : Exists
    {
        public ModifierCard(GameObject gameObject, Modifier<T> modifier, int index)
        {
            GameObject = gameObject;
            Modifier = modifier;
            this.index = index;
        }

        public GameObject GameObject { get; set; }

        public Modifier<T> Modifier { get; set; }

        public int index;
    }
}
