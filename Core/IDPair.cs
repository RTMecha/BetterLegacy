using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core
{
    public class IDPair
    {
        public IDPair(string oldID, string newID)
        {
            this.oldID = oldID;
            this.newID = newID;
        }

        public string oldID;
        public string newID;
    }
}
