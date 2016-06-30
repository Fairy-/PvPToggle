/*
 * Original plugin by White.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TShockAPI.Hooks;

namespace PvPToggle
{
    [ApiVersion(1, 23)]
    public class PvpToggle : TerrariaPlugin
    {
        public static readonly List<Player> PvPplayer = new List<Player>();
        public static readonly List<Player> GemPlayer = new List<Player>();
        private static readonly List<string> TeamColors = new List<string> { "white", "red", "green", "blue", "yellow", "purple" };
        private static PvPConfig Config { get; set; }

        private static PvPManager pvpdb { get; set; }

        private static bool forcePvP = false;

        private static bool forceTeam = false;

        private static bool forceGem = false;

        private static int countTicks = 0;

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Author
        {
            get { return "Zaicon"; }
        }
        public override string Name
        {
            get { return "PvPToggle"; }
        }

        public override string Description
        {
            get { return "Allows you to set players PvP"; }
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            GeneralHooks.ReloadEvent += onReload;
            GetDataHandlers.PlayerTeam += onPlayerTeamChange;
            GetDataHandlers.PlayerSlot += onInventoryChange;
            GetDataHandlers.TogglePvp += onTogglePvP;
            GetDataHandlers.ItemDrop += onItemDrop;
            pvpdb = new PvPManager(TShock.DB);
            Config = new PvPConfig();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                GeneralHooks.ReloadEvent -= onReload;
                GetDataHandlers.PlayerTeam -= onPlayerTeamChange;
            }
            base.Dispose(disposing);
        }

        public PvpToggle(Main game)
            : base(game)
        {
            Order = 1;
        }

        private static void OnInitialize(EventArgs e)
        {
            Commands.ChatCommands.Add(new Command(PvPSwitch, "pvp"));
            Commands.ChatCommands.Add(new Command("pvp.switch", TogglePvP, "tpvp"));
            Commands.ChatCommands.Add(new Command(TeamSwitch, "team"));
            Commands.ChatCommands.Add(new Command("pvp.team", ToggleTeam, "tteam"));
            Commands.ChatCommands.Add(new Command("pvp.force", ForceToggle, "forcepvp", "fpvp"));
            Commands.ChatCommands.Add(new Command("pvp.moon", BloodToggle, "bloodmoonpvp", "bmpvp"));
            Commands.ChatCommands.Add(new Command("pvp.teamforce", ForceTeams, "forceteam", "fteam"));

            SetUpConfig();

            forcePvP = Config.autoForcePvP;
            forceTeam = Config.autoForceTeams;
        }

        private static void OnGreetPlayer(GreetPlayerEventArgs args)
        {

            Player player = new Player(args.Who);
            if (!pvpdb.GetPlayerTeam(player.TSPlayer.User.ID).exists)
            {
                pvpdb.InsertPlayerTeam(player.TSPlayer.User.ID, 0);
            }

            player.DBTeam = pvpdb.GetPlayerTeam(player.TSPlayer.User.ID).teamid;

            if (forceTeam)
            {
                player.TSPlayer.SendInfoMessage("Force team is enabled, your team has been set to " + TeamColors[player.DBTeam]);
                player.TSPlayer.SetTeam(player.DBTeam);
                player.TSPlayer.SendData(PacketTypes.PlayerTeam, "", player.Index);
                player.isForcedTeam = true;
            }

            if (forcePvP)
            {
                player.TSPlayer.SendInfoMessage("Force PvP is enabled, your PvP status has been set to on");
                player.PvPType = Player.PlayerPvPType.ForceOn;
                Main.player[player.Index].hostile = true;
                player.TSPlayer.SendData(PacketTypes.TogglePvp, "", player.Index);
            }

            lock (PvPplayer)
                PvPplayer.Add(player);

        }

        #region onPlayerTeamChange
        private void onPlayerTeamChange(object sender, GetDataHandlers.PlayerTeamEventArgs args)
        {
            lock (PvPplayer)
            {
                Player player = PvPplayer.Find(p => p.Index == args.PlayerId);
                if (forceTeam)
                {
                    player.TSPlayer.SendErrorMessage("Force team is enabled, you are unable to change your team!");
                    player.TSPlayer.SetTeam(player.DBTeam);
                    args.Handled = true;
                }
            }
        }

        #endregion

        #region OnTogglePvp
        private static void onTogglePvP(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            if (!args.Pvp)
            {
                lock (PvPplayer)
                {
                    var plr = PvPplayer.Find(p => p.Index == args.PlayerId);

                    if (plr.PvPType.HasFlag(Player.PlayerPvPType.ForceOn)) //is forceon?
                    {
                        Main.player[plr.Index].hostile = true;
                        plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        plr.TSPlayer.SendWarningMessage("Your PvP has been forced on, don't try and turn it off!");
                        args.Handled = true;
                    }
                    else if (plr.PvPType.HasFlag(Player.PlayerPvPType.ForceGem)) //is forcegem?
                    {
                        Main.player[plr.Index].hostile = true;
                        plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        plr.TSPlayer.SendWarningMessage("The Large Gem's evil influence stops your PvP from turning off.");
                        args.Handled = true;
                    }
                    else if (plr.PvPType.HasFlag(Player.PlayerPvPType.ForceBloodmoon)) //is bloodmoon?
                    {
                        Main.player[plr.Index].hostile = true;
                        plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        plr.TSPlayer.SendWarningMessage("The Blood Moon's evil influence stops your PvP from turning off.");
                        args.Handled = true;
                    }
                }
            }
        }
        #endregion

        #region OnInventoryChange
        private static void onInventoryChange(object sender, GetDataHandlers.PlayerSlotEventArgs args)
        {

        }
        #endregion

        private static void onItemDrop(object sender, GetDataHandlers.ItemDropEventArgs args)
        {
        }


        #region OnUpdate

        private static void OnUpdate(EventArgs e)
        {
            countTicks++;
            if (countTicks < 30)
            {
                return;

            }
            countTicks = 0;
            lock (PvPplayer)
            {
                foreach (var plr in PvPplayer)
                {
                    if (Config.ForcePvPOnBloodMoon && (Main.dayTime || !Main.bloodMoon) && plr.PvPType.HasFlag(Player.PlayerPvPType.ForceBloodmoon)) {
                        plr.PvPType ^= Player.PlayerPvPType.ForceBloodmoon;
                        plr.TSPlayer.SendInfoMessage("The Blood Moon's influence fades!");
                    }

                    if (Config.EnableGemMechanics)
                    {
                        bool hasGem = plr.hasGems();

                        lock (GemPlayer)
                        {
                            if (hasGem && GemPlayer.IndexOf(plr) == -1)
                            {
                                GemPlayer.Add(plr);
                            }
                            else if (!hasGem && GemPlayer.IndexOf(plr) > -1)
                            {
                                GemPlayer.Remove(plr);
                            }
                        }

                        if (!hasGem && (plr.PvPType.HasFlag(Player.PlayerPvPType.ForceGem))) {
                            plr.PvPType ^= Player.PlayerPvPType.ForceGem;
                            plr.TSPlayer.SendInfoMessage("The Gem's influence fades!");
                        } else if (hasGem && (!plr.PvPType.HasFlag(Player.PlayerPvPType.ForceGem))) {
                            plr.PvPType |= Player.PlayerPvPType.ForceGem;
                            if (!Main.player[plr.Index].hostile)
                            {
                                Main.player[plr.Index].hostile = true;
                                plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                                plr.TSPlayer.SendWarningMessage("The Large Gem in your inventory forced your PvP on!");
                            }
                        }
                    }

                    bool safeZone = GemPlayer.Count == 0 && plr.TSPlayer.CurrentRegion != null && Config.antiPvPRegions.ToList().Contains(plr.TSPlayer.CurrentRegion.Name);

                    if (safeZone)
                    {
                        if (Main.player[plr.Index].hostile)
                        {
                            Main.player[plr.Index].hostile = false;
                            plr.TSPlayer.SendWarningMessage("PvP is disabled in this zone.");
                            plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        }
                    }
                    else if (Config.ForcePvPOnBloodMoon && Main.bloodMoon && !Main.dayTime && (!plr.PvPType.HasFlag(Player.PlayerPvPType.ForceBloodmoon))) {
                        plr.PvPType += 4;
                        Main.player[plr.Index].hostile = true;
                        plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        plr.TSPlayer.SendInfoMessage("The Blood Moon's evil influence forced your PvP on!");
                    } else if (!Main.player[plr.Index].hostile && (!plr.PvPType.Equals(Player.PlayerPvPType.None)))
                    {
                        Main.player[plr.Index].hostile = true;
                        plr.TSPlayer.SendData(PacketTypes.TogglePvp, "", plr.Index);
                        plr.TSPlayer.SendInfoMessage("Your PvP turned back on!");
                    }

                }
            }
        }
        #endregion

        #region onReload
        private static void onReload(ReloadEventArgs args)
        {
            SetUpConfig();
            TShock.Log.Info("Reloaded PvPToggle config.");
        }
        #endregion

        #region Config

        private static void SetUpConfig()
        {
            var configPath = Path.Combine(TShock.SavePath, "PvPtoggle.json");
            (Config = PvPConfig.Read(configPath)).Write(configPath);
        }

        #endregion

        private static void OnLeave(LeaveEventArgs args)
        {
            Player player = new Player(args.Who);

            pvpdb.InsertPlayerTeam(player.TSPlayer.User.ID, player.TSPlayer.Team);

            lock (PvPplayer)
                PvPplayer.RemoveAll(plr => plr.Index == args.Who);

            lock (GemPlayer)
                GemPlayer.RemoveAll(plr => plr.Index == args.Who);
        }

        #region PvPSwitch

        private static void PvPSwitch(CommandArgs args)
        {
            if (TShock.Config.PvPMode == "always" || TShock.Config.PvPMode == "disabled")
            {
                args.Player.SendErrorMessage("Command blocked by server configuration");
                return;
            }

            if (!Main.player[args.Player.Index].hostile)
            {
                Main.player[args.Player.Index].hostile = true;
                NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.Player.Index);
                args.Player.SendSuccessMessage("Your PvP is now enabled.");
            }
            else if (Main.player[args.Player.Index].hostile)
            {
                Main.player[args.Player.Index].hostile = false;
                NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.Player.Index);
                args.Player.SendSuccessMessage("Your PvP is now disabled.");
            }
        }

        #endregion

        #region TogglePvP

        private static void TogglePvP(CommandArgs args)
        {

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid Syntax: /tpvp <player>");
            }

            var plStr = String.Join(" ", args.Parameters);

            var ply = TShock.Utils.FindPlayer(plStr);
            if (ply.Count < 1)
            {
                args.Player.SendErrorMessage($"Unknown Player: {plStr}");
            }
            else if (ply.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, ply.Select(p => p.Name));
            }
            else
            {
                var player = ply[0];

                if (args.Parameters.Count == 1 && ply.Count == 1)
                {
                    if (!Main.player[player.Index].hostile)
                    {
                        Main.player[player.Index].hostile = true;
                        NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player.Index);
                        args.Player.SendSuccessMessage($"You have turned {player.Name}'s PvP on!");
                        if (!args.Silent)
                            player.SendInfoMessage($"{args.Player.Name} has turned your PvP on!");
                        else
                            player.SendInfoMessage("Your PvP has been turned on!");

                    }
                    else if (Main.player[player.Index].hostile)
                    {
                        Main.player[player.Index].hostile = false;
                        NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player.Index);

                        args.Player.SendInfoMessage($"You have turned {player.Name}'s PvP off!");
                        if (!args.Silent)
                            player.SendInfoMessage($"{args.Player.Name} has turned your PvP off!");
                        else
                            player.SendInfoMessage("Your PvP has been turned on!");
                    }
                }
            }

        }

        #endregion

        #region TeamSwitch

        private static void TeamSwitch(CommandArgs args)
        {
            if (args.Player.HasPermission("pvp.teamswitch.bypass"))
            {
                if (args.Parameters.Count != 1)
                {
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /team [team color]");
                    return;
                }

                var team = args.Parameters[0];

                if (TeamColors.Contains(team.ToLower()))
                {

                    args.Player.TPlayer.team = TeamColors.IndexOf(team);
                    NetMessage.SendData((int)PacketTypes.PlayerTeam, -1, -1, "", args.Player.Index);
                    args.Player.SendSuccessMessage($"Joined the {team} team!");

                    lock (PvPplayer)
                    {
                        Player pl = PvPplayer.Find(p => p.Index == args.Player.Index);
                        pl.DBTeam = args.Player.Team;
                        pvpdb.InsertPlayerTeam(pl.TSPlayer.User.ID, pl.DBTeam);
                    }
                }
                else
                    args.Player.SendErrorMessage("Invalid team color!");
            }
            else
            {
                args.Player.SendErrorMessage("You don't have the permission to change your team!");
            }
        }
        #endregion

        #region ToggleTeam

        private static void ToggleTeam(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /tteam [player] [team color]");
                return;
            }
            var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (foundplr.Count == 0)
            {
                args.Player.SendErrorMessage($"Unknown Player: {args.Parameters[0]}");
                return;
            }
            if (foundplr.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, foundplr.Select(p => p.Name));
                return;
            }

            var team = args.Parameters[1];

            if (TeamColors.Contains(team.ToLower()))
            {
                foundplr[0].TPlayer.team = TeamColors.IndexOf(team);
                NetMessage.SendData((int)PacketTypes.PlayerTeam, -1, -1, "", foundplr[0].Index);
                foundplr[0].SendData(PacketTypes.PlayerTeam, "", foundplr[0].Index);
                if (!args.Silent)
                    foundplr[0].SendInfoMessage($"{args.Player.Name} changed you to the {team} team!");
                else
                    foundplr[0].SendInfoMessage($"You are now on the {team} team!");
                args.Player.SendSuccessMessage($"Changed {foundplr[0].Name} to the {team} team!");

                lock (PvPplayer)
                {
                    Player pl = PvPplayer.Find(p => p.Index == foundplr[0].Index);
                    pl.DBTeam = foundplr[0].Team;
                    pvpdb.InsertPlayerTeam(pl.TSPlayer.User.ID, pl.DBTeam);
                }

            }
            else
                args.Player.SendErrorMessage("Invalid team color!");
        }

        #endregion

        #region BloodToggle
        private static void BloodToggle(CommandArgs args)
        {
            Config.ForcePvPOnBloodMoon = !Config.ForcePvPOnBloodMoon;

            args.Player.SendInfoMessage(Config.ForcePvPOnBloodMoon
                ? "Players will now have PvP forced on during bloodmoons"
                : "Players will no longer have PvP forced on during bloodmoons");
        }
        #endregion

        #region LockToggle

        private static void ForceTeams(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendErrorMessage("Invalid syntax. Use /fteam <player name> (or /fteam *).");
                return;
            }

            string plStr = String.Join(" ", args.Parameters);

            var players = TShock.Utils.FindPlayer(plStr);
            if (players.Count == 0 && ((plStr != "*") && (plStr != "all") && (plStr != "*off") && (plStr != "alloff")))
            {
                args.Player.SendErrorMessage("No players matched that name");
                return;
            }
            if (players.Count > 1
                && ((plStr != "*") && (plStr != "all") && (plStr != "*off") && (plStr != "alloff")))
            {
                TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                return;
            }

            if (plStr == "*" || plStr == "all")
            {
                forceTeam = true;
                foreach (var pl in PvPplayer)
                {
                    pl.isForcedTeam = true;
                    pl.DBTeam = pl.TSPlayer.Team;
                    pvpdb.InsertPlayerTeam(pl.TSPlayer.User.ID, pl.TSPlayer.Team);
                }

                if (!args.Silent)
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} has forced everyone's Team");
                return;
            }
            if (plStr == "*off" || plStr == "alloff")
            {
                forceTeam = false;
                foreach (var pl in PvPplayer)
                    pl.isForcedTeam = false;

                if (!args.Silent)
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} has stopped forcing everyone's Team. It can now be changed.");
            }

            else
            {

                var plr = players[0];

                PvPToggle.Player player = Tools.GetPlayerByIndex(players[0].Index);

                if (player.isForcedTeam == false)
                {
                    //Maybe needs work?
                    player.isForcedTeam = true;
                    player.DBTeam = player.TSPlayer.Team;
                    pvpdb.InsertPlayerTeam(player.TSPlayer.User.ID, player.TSPlayer.Team);
                    if (!args.Silent)
                        plr.SendInfoMessage($"{args.Player.Name} has forced your Team!");
                    args.Player.SendSuccessMessage($"You have forced {player.PlayerName}'s Team!");
                }
                else if (player.isForcedTeam == true)
                {
                    player.isForcedTeam = false;
                    if (!args.Silent)
                        plr.SendInfoMessage($"{args.Player.Name} has stopped forcing your Team!");
                    args.Player.SendInfoMessage($"You have stopped forcing {player.PlayerName}'s Team!");

                }
            }
        }

        #endregion

        #region ForceToggle

        private static void ForceToggle(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendErrorMessage("Invalid syntax. Use /fpvp <player name> (or /fpvp *).");
                return;
            }

            string plStr = String.Join(" ", args.Parameters);

            var players = TShock.Utils.FindPlayer(plStr);
            if (players.Count == 0 && ((plStr != "*") && (plStr != "all") && (plStr != "*off") && (plStr != "alloff")))
            {
                args.Player.SendErrorMessage("No players matched that name");
                return;
            }
            if (players.Count > 1
                && ((plStr != "*") && (plStr != "all") && (plStr != "*off") && (plStr != "alloff")))
            {
                TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                return;
            }

            if (plStr == "*" || plStr == "all")
            {
                forcePvP = true;
                foreach (var pl in PvPplayer)
                {
                    Main.player[pl.Index].hostile = true;
                    pl.TSPlayer.SendData(PacketTypes.TogglePvp, "", pl.Index);
                    pl.PvPType |= Player.PlayerPvPType.ForceOn;
                }

                if (!args.Silent)
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} has forced on everyone's PvP");
                return;
            }
            if (plStr == "*off" || plStr == "alloff")
            {
                foreach (var pl in PvPplayer)
                    pl.PvPType ^= Player.PlayerPvPType.ForceOn;

                if (!args.Silent)
                    TSPlayer.All.SendInfoMessage($"{args.Player.Name} has stopped forcing everyone's PvP on. It can now be turned off.");
            }

            else
            {

                var plr = players[0];

                PvPToggle.Player player = Tools.GetPlayerByIndex(players[0].Index);

                if (player.PvPType.Equals(Player.PlayerPvPType.None))
                {
                    player.PvPType |= Player.PlayerPvPType.ForceOn;
                    Main.player[plr.Index].hostile = true;
                    NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.Index, 0f, 0f, 0f);
                    if (!args.Silent)
                        plr.SendInfoMessage($"{args.Player.Name} has forced your PvP on!");
                    args.Player.SendSuccessMessage($"You have forced {player.PlayerName}'s PvP on!");
                }


                else if (player.PvPType.HasFlag(Player.PlayerPvPType.ForceOn))
                {
                    player.PvPType ^= Player.PlayerPvPType.ForceOn;
                    Main.player[plr.Index].hostile = false;
                    NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", plr.Index, 0f, 0f, 0f);
                    if (!args.Silent)
                        plr.SendInfoMessage($"{args.Player.Name} has turned your PvP off!");
                    args.Player.SendInfoMessage($"You have turned {player.PlayerName}'s PvP off!");

                }
            }
        }
    }
    #endregion

    #region Tools
    public class Tools
    {
        public static Player GetPlayerByIndex(int index)
        {
            return PvpToggle.PvPplayer.FirstOrDefault(player => player.Index == index);
        }

    }
    #endregion

    #region Config
    public class PvPConfig
    {
        public bool EnableGemMechanics;
        public bool ForcePvPOnBloodMoon;
        public string[] antiPvPRegions = new string[] { };
        public bool autoForceTeams;
        public bool autoForcePvP;
        public bool announceGemPickup;

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static PvPConfig Read(string path)
        {
            if (!File.Exists(path))
                return new PvPConfig();
            return JsonConvert.DeserializeObject<PvPConfig>(File.ReadAllText(path));
        }
    }
    #endregion
}
