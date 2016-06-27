using System;
using System.Data;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using TShockAPI;
using Terraria;

namespace PvPToggle
{
    class PvPManager
    {
        public IDbConnection database;

        public PvPManager(IDbConnection db)
        {
            database = db;

            var table = new SqlTable("tsPvP",
                new SqlColumn("Account", MySqlDbType.Int32) { Primary = true },
                new SqlColumn("Team", MySqlDbType.Int32));

            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            creator.EnsureTableStructure(table);
        }

        public PvPPlayerData GetPlayerTeam(int accountid)
        {
            PvPPlayerData pvpplayer = new PvPPlayerData();
            if (Main.ServerSideCharacter)
            {
                try
                {
                    using (var reader = database.QueryReader("SELECT * FROM tsPvP WHERE Account=@0", accountid))
                    {
                        if (reader.Read())
                        {
                            pvpplayer.exists = true;
                            pvpplayer.teamid = reader.Get<int>("Team");
                            return pvpplayer;
                        }
                    }

                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
                return pvpplayer;
            }
            pvpplayer.exists = true;
            pvpplayer.teamid = 0;
            return pvpplayer;
        }

        public bool InsertPlayerTeam(int accountid, int teamid)
        {
            if (Main.ServerSideCharacter)
            {
                PvPPlayerData player = GetPlayerTeam(accountid);
                if (!player.exists)
                {
                    try
                    {
                        database.Query(
                            "INSERT INTO tsPvP (Account, Team) VALUES (@0, @1);",
                            accountid, teamid);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        TShock.Log.Error(ex.ToString());
                    }
                }
                else
                {
                    try
                    {
                        database.Query(
                            "UPDATE tsPvP SET Team = @0 WHERE Account = @1;",
                            teamid, accountid);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        TShock.Log.Error(ex.ToString());
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
