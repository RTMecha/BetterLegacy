using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Network
{
    public class LobbySettings
    {
        public int PlayerCount { get; set; } = 16;

        public bool IsPrivate { get; set; }
    }
}
