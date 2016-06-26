using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace PvPToggle
{
    public class Player
    {
        public string PvPType = "";
        public int Index;
        public int DBTeam = 0;
        public bool isForcedTeam = false;

        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string PlayerName { get { return Main.player[Index].name; } }


        public Player(int index)
        {
            Index = index;
        }
    }
}
