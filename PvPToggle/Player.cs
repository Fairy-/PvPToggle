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

        public string PvPType = "";
        public int Index;
        public int DBTeam = 0;
        public bool isForcedTeam = false;
        public Dictionary<int, int> gemsCarried = new Dictionary<int, int>();

        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string PlayerName { get { return Main.player[Index].name; } }

        public string GemCheck()
        {
            string droppedstring = "";
            string pickupstring = "";
            string returnstring = "";
            Dictionary<int, int> newGemsCarried = new Dictionary<int, int>();
            Array values = Enum.GetValues(typeof(largeGem));
            foreach (largeGem item in values)
            {
                newGemsCarried[(int)item] = 0;
            }
            IEnumerable<Item> filteredGems = this.TSPlayer.TPlayer.inventory.Where(item => (item.netID >= 1522 && item.netID <= 1527) || item.netID == 3643);
            foreach (Item item in filteredGems)
            {
                newGemsCarried[item.netID]++;
            }

            foreach (largeGem item in values)
            {
                int change = newGemsCarried[(int)item] - gemsCarried[(int)item];
                if (change > 0)
                {
                    pickupstring += change + " " + Enum.GetName(typeof(largeGem), item) + " ";
                }
                else if (change < 0)
                {
                    droppedstring += change * -1 + " " + Enum.GetName(typeof(largeGem), item) + " ";
                }
            }

            if (pickupstring != "")
            {
                returnstring += TSPlayer.Name + " has picked up " + pickupstring.Trim();
            }

            if (droppedstring != "")
            {
                if (returnstring == "")
                {
                    returnstring += TSPlayer.Name + " has dropped " + droppedstring.Trim();
                }
                else
                {
                    returnstring += " and dropped " + droppedstring.Trim();
                }
            }
            gemsCarried = newGemsCarried;
            return returnstring;
        }

        public bool hasGems()
        {
            int i = 0;
            Array values = Enum.GetValues(typeof(largeGem));
            foreach (largeGem item in values)
            {
                i += gemsCarried[(int)item];
            }
            return i > 0;
        }

        public Player(int index)
        {
            Index = index;
            Array values = Enum.GetValues(typeof(largeGem));
            foreach (largeGem item in values)
            {
                gemsCarried[(int)item] = 0;
            }
        }
    }
}
