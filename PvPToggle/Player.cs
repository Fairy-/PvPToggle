using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace PvPToggle
{
    public class Player
    {
        public enum largeGem
        {
            Amethyst = 1522,
            Topaz = 1523,
            Sapphire = 1524,
            Emerald = 1525,
            Ruby = 1526,
            Diamond = 1527,
            Amber = 3643
        }

        [Flags]
        public enum PlayerPvPType
        {
            None = 0,
            ForceOn = 1,
            ForceGem = 2,
            ForceBloodmoon = 4
        }

        public PlayerPvPType PvPType;
        public int Index;
        public int DBTeam = 0;
        public bool isForcedTeam = false;
        public Dictionary<int, int> previousGemsCarried = new Dictionary<int, int>();
        public Dictionary<int, int> gemsCarried = new Dictionary<int, int>();

        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string PlayerName { get { return Main.player[Index].name; } }

        public bool hasGems()
        {
           return gemsCarried.Count != 0;
        }

        public Player(int index)
        {
            Index = index;
        }
    }
}
