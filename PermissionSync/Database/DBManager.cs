using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PermissionSync.Database;
using Rocket.Unturned.Player;
using MySql.Data.MySqlClient;
using Rocket.Core;
using PermissionSync.Data;
using Steamworks;
using PermissionSync.Enum;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;

namespace PermissionSync.Database
{
    public class DBManager
    {
        private DBConnectionManager DBConnection = new DBConnectionManager();
        public DBManager()
        {
            CheckSchama();
        }

        public void CheckSchama()
        {
            DBConnection.ExecuteQuery(true,
                $"CREATE TABLE IF NOT EXISTS `{Main.Instance.Configuration.Instance.DatabaseTableName}` (`SteamID` BIGINT NOT NULL, `PermissionGroup` varchar(32) NOT NULL, `ExpireDate` datetime(6) NOT NULL DEFAULT '{DateTime.MaxValue}', `Operator` BIGINT NOT NULL)");
        }

        internal void PermissionSync(UnturnedPlayer player) 
        {
            var servergroupids = GetPlayerPermissionGroupId(player);
            var dbgroups = GetDBPermissionGroup(player.CSteamID, EDBQueryType.ByStamID);
            foreach (var dbgroup in dbgroups )
            {
               if( servergroupids.Contains(dbgroup.PermissionID))
                {
                    if(dbgroup.ExpireDate < DateTime.Now)
                    {
                        R.Permissions.RemovePlayerFromGroup(dbgroup.PermissionID, player);
                        UnturnedChat.Say(player, Main.Instance.Translate("permission_expired", dbgroup.PermissionID));
                    }
                }
               else
                {
                    if(dbgroup.ExpireDate> DateTime.Now)
                    {
                        R.Permissions.AddPlayerToGroup(dbgroup.PermissionID, player);
                        UnturnedChat.Say(player, Main.Instance.Translate("sync_permission",dbgroup.PermissionID));
                    }
                }
            }
            
        }

        public List<PermissionData> GetDBPermissionGroup(CSteamID steamID,EDBQueryType type)
        {
            List<PermissionData> permissionDatas = new List<PermissionData>();

            var connection = DBConnection.CreateConnection();
            try
            {
                var command = connection.CreateCommand();
                switch(type)
                {
                    case EDBQueryType.ByStamID:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.CommandText = $"Select * from `{Main.Instance.Configuration.Instance.DatabaseTableName}` where `SteamID` = @steamid";
                        break;
                    case EDBQueryType.BySteamIDAndTime:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.CommandText = $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseTableName}` WHERE `SteamID` = @steamid AND `ExpireDate` < now()";
                        break;
                }
                connection.Open();
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    PermissionData permissionData = new PermissionData(new CSteamID((ulong)reader["SteamID"]), reader["PermissionGroup"].ToString(), DateTime.Parse(reader["ExpireDate"].ToString()), reader["Operator"].ToString());
                    permissionDatas.Add(permissionData);
                }

            }
            catch
            {

            }
            return permissionDatas;
        }

        private List<string> GetPlayerPermissionGroupId(UnturnedPlayer player)
        {
            var groupids = new List<string>();
            var playergroups = R.Permissions.GetGroups(player, false);
            foreach (var group in playergroups)
            {
                groupids.Add(group.Id);
            }
            return groupids;
        }
        public void AddPermission(string oeratorID,UnturnedPlayer player,string PermissionGroupId,string expireTime = "2099-12-31")
        {
            PermissionData data = new PermissionData(player.CSteamID, PermissionGroupId, DateTime.Parse(expireTime),oeratorID);
            SaveDataToDB(data);
            R.Permissions.AddPlayerToGroup(PermissionGroupId, player);
        }

        public void RemovePermission(UnturnedPlayer player, string PermiisonGroupId)
        {
            RemoveDataFromDB(player, PermiisonGroupId);
            R.Permissions.RemovePlayerFromGroup(PermiisonGroupId, player);
        }

        public void UpdatePermission(UnturnedPlayer player, string PermissionGroupId,DateTime dateTime,string operatorID)
        {
            UpdateDataInDB(new PermissionData(player.CSteamID, PermissionGroupId, dateTime, operatorID));
        }

        internal void SaveDataToDB(PermissionData permissionData)
        {
            DBConnection.ExecuteQuery(true,
                $"INSERT INTO `{Main.Instance.Configuration.Instance.DatabaseTableName}` (SteamID,PermissionID,ExpireDate,Operator) values('{permissionData.SteamID}','{permissionData.PermissionID}','{permissionData.ExpireDate}','{permissionData.OperatorID}') ");
        }

        internal void RemoveDataFromDB(UnturnedPlayer player,string groupid)
        {
            DBConnection.ExecuteQuery(true,
                $"Delect from `{Main.Instance.Configuration.Instance.DatabaseTableName}` Where `SteamID` = '{player.CSteamID}' and `PermissionGroup` = '{groupid}'");
        }

        internal void UpdateDataInDB(PermissionData permissionData)
        {
            DBConnection.ExecuteQuery(true,
                $"Update `{Main.Instance.Configuration.Instance.DatabaseTableName}` SET `ExpireDate` = '{permissionData.ExpireDate}',`Operator` = '{permissionData.OperatorID}' Where `SteamID` = '{permissionData.SteamID}' AND `PermissionID` = '{permissionData.PermissionID}'");
        }
    }
}
