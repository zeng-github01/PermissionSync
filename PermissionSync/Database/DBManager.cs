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
using Rocket.API.Serialisation;

namespace PermissionSync.Database
{
    public class DBManager
    {
        private DBConnectionManager DBConnection = new DBConnectionManager();
        public DBManager()
        {
            CheckSchama();
        }

        internal void CheckSchama()
        {
            try
            {
                // 1. 创建表（初始版本）
                DBConnection.ExecuteQuery(true,
                    $"CREATE TABLE IF NOT EXISTS `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` (`SteamID` BIGINT NOT NULL, `PermissionGroup` varchar(32) NOT NULL, `ExpireDate` datetime(6) NOT NULL DEFAULT '{DateTime.MaxValue}', `Operator` VARCHAR(32) NOT NULL,UNIQUE KEY unique_permission (`SteamID`,`PermissionGroup`));");
                DBConnection.ExecuteQuery(true,
                    $"CREATE TABLE IF NOT EXISTS `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName)}` (`GroupID` varchar(32) NOT NULL, `GroupName` varchar(32) NOT NULL, `GroupColor` varchar(32) NOT NULL, `GroupPriority` int NOT NULL DEFAULT 0, `GroupPrefix` NOT NULL varchar(32) DEFAULT '', `GroupSuffix` NOT NULL varchar(32) DEFAULT '', `ParentGroup` varchar(32) NOT NULL DEFAULT '', UNIQUE KEY unique_group (`GroupID`));");
                DBConnection.ExecuteQuery(true,
                    $"CREATE TABLE IF NOT EXISTS `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionSubTableName)}` (`GroupID` varchar(32) NOT NULL, `PermissionName` varchar(32) NOT NULL, `PermissionCooldown` int NOT NULL DEFAULT 0, UNIQUE KEY unique_permission_sub (`GroupID`,`PermissionName`));");

                // 2. 迁移逻辑
                var config = global::PermissionSync.PermissionSync.Instance.Configuration.Instance;
                if (config.TableVer < 2)
                {
                    // v1 -> v2: 添加唯一索引
                    DBConnection.ExecuteQuery(true,
                        $"ALTER TABLE `{(config.PermissionPlayerTableName)}` ADD CONSTRAINT unique_permission UNIQUE KEY(`SteamID`,`PermissionGroup`)");
                    config.TableVer = 2;
                }
                // 保存 TableVer 到配置文件
                global::PermissionSync.PermissionSync.Instance.Configuration.Save();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public List<Permission> GetPermissionsBelongGroup(string groupId)
        {
            var list = new List<Permission>();
            var connection = DBConnection.CreateConnection();
            connection.Open();
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"Select * from `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionSubTableName)}` where `GroupID` = @groupid";
                command.Parameters.AddWithValue("@groupid", groupId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Permission permission = new Permission
                    {
                        Name = reader["PermissionName"].ToString(),
                        Cooldown = Convert.ToUInt32(reader["PermissionCooldown"]),
                    };
                    list.Add(permission);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }

            return list;
        }

        // 获取指定权限组的所有成员SteamID
        public List<string> GetGroupMembers(string groupId)
        {
            var members = new List<string>();
            var connection = DBConnection.CreateConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT SteamID FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `PermissionGroup` = @groupId";
                command.Parameters.AddWithValue("@groupId", groupId);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    members.Add(reader["SteamID"].ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }
            return members;
        }

        public List<RocketPermissionsGroup> GetPermissionGroup()
        {
            List<RocketPermissionsGroup> groups = new List<RocketPermissionsGroup>();
            var connection = DBConnection.CreateConnection();
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"Select * from `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName)}`";
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    RocketPermissionsGroup group = new RocketPermissionsGroup
                    {
                        Id = reader["GroupID"].ToString(),
                        DisplayName = reader["GroupName"].ToString(),
                        Color = reader["GroupColor"].ToString(),
                        Priority = Convert.ToInt16(reader["GroupPriority"]),
                        Permissions = GetPermissionsBelongGroup(reader["GroupID"].ToString()),
                        Prefix = reader["GroupPrefix"] != DBNull.Value ? reader["GroupPrefix"].ToString() : string.Empty,
                        Suffix = reader["GroupSuffix"] != DBNull.Value ? reader["GroupSuffix"].ToString() : string.Empty,
                        Members = GetGroupMembers(reader["GroupID"].ToString()),
                        ParentGroup = reader["ParentGroup"] != DBNull.Value ? reader["ParentGroup"].ToString() : string.Empty
                    };
                    groups.Add(group);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }
            return groups;
        }

        public List<PermissionData> GetPlayerPermissionData(CSteamID steamID,EDBQueryType type)
        {
            List<PermissionData> permissionDatas = new List<PermissionData>();

            var connection = DBConnection.CreateConnection();
            try
            {
                var command = connection.CreateCommand();
                switch(type)
                {
                    case EDBQueryType.BySteamID:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.CommandText = $"Select * from `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` where `SteamID` = @steamid";
                        break;
                    case EDBQueryType.BySteamIDAndTime:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.CommandText = $"SELECT * FROM `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` WHERE `SteamID` = @steamid AND `ExpireDate` < now()";
                        break;
                }
                connection.Open();
                var reader = command.ExecuteReader();
                while(reader.Read())
                {
                    PermissionData permissionData = new PermissionData(Convert.ToUInt64(reader["SteamID"]), reader["PermissionGroup"].ToString(), DateTime.Parse(reader["ExpireDate"].ToString()), reader["Operator"].ToString());
                    permissionDatas.Add(permissionData); 
                }

            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }
            return permissionDatas;
        }

        [Obsolete("This method is deprecated, please use the overload with DateTime parameter instead.")]
        public bool AddPermission(string oeratorID,UnturnedPlayer player,string PermissionGroupId,string expireTime = "2099-12-31")
        {
             bool AddGroup;
           if(DateTime.TryParse(expireTime,out DateTime dateTime))
            {
                PermissionData data = new PermissionData(player.CSteamID.m_SteamID, PermissionGroupId, dateTime, oeratorID);
                SaveDataToDB(data);
                R.Permissions.AddPlayerToGroup(PermissionGroupId, player);
                AddGroup = true;
            }
           else
            {
                AddGroup = false;
            }
            return AddGroup;
        }

        public void AddPermission(string oeratorID, UnturnedPlayer player, string PermissionGroupId, DateTime dateTime)
        {
            PermissionData data = new PermissionData(player.CSteamID.m_SteamID, PermissionGroupId, dateTime, oeratorID);
            SaveDataToDB(data);
            R.Permissions.AddPlayerToGroup(PermissionGroupId, player);
        }

        public void AddPermission(string oeratorID, CSteamID steamID, string PermissionGroupId, DateTime dateTime)
        {
            PermissionData data = new PermissionData(steamID.m_SteamID, PermissionGroupId, dateTime, oeratorID);
            SaveDataToDB(data);
            if (UnturnedPlayer.FromCSteamID(steamID) is UnturnedPlayer player)
                R.Permissions.AddPlayerToGroup(PermissionGroupId, player);
        }

        public void RemovePermission(UnturnedPlayer player, string PermiisonGroupId)
        {
            RemoveDataFromDB(player.CSteamID.m_SteamID, PermiisonGroupId);
            R.Permissions.RemovePlayerFromGroup(PermiisonGroupId, player);
        }

        public void RemovePermission(CSteamID steamID, string PermiisonGroupId)
        {
            RemoveDataFromDB(steamID.m_SteamID, PermiisonGroupId);
            if (UnturnedPlayer.FromCSteamID(steamID) is UnturnedPlayer player)
                R.Permissions.RemovePlayerFromGroup(PermiisonGroupId, player);
        }

        public void UpdatePermission(UnturnedPlayer player, string PermissionGroupId,DateTime dateTime,string operatorID)
        {
            UpdateDataInDB(new PermissionData(player.CSteamID.m_SteamID, PermissionGroupId, dateTime, operatorID));
        }

        internal void SaveDataToDB(PermissionData permissionData)
        {
            DBConnection.ExecuteQuery(true,
                $"INSERT INTO `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` (SteamID,PermissionGroup,ExpireDate,Operator) values('{permissionData.SteamID}','{permissionData.PermissionID}','{permissionData.ExpireDate}','{permissionData.OperatorID}') ON DUPLICATE KEY UPDATE `SteamID` = VALUES(`SteamID`),`PermissionGroup` = VALUES(`PermissionGroup`), `ExpireDate` = VALUES(`ExpireDate`), `Operator` = VALUES(`Operator`)");
        }

        internal void RemoveDataFromDB(ulong steamID,string groupid)
        {
            DBConnection.ExecuteQuery(true,
                $"Delete from `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` Where `SteamID` = '{steamID}' and `PermissionGroup` = '{groupid}'");
        }

        internal void UpdateDataInDB(PermissionData permissionData)
        {
            DBConnection.ExecuteQuery(true,
                $"Update `{(global::PermissionSync.PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName)}` SET `ExpireDate` = '{permissionData.ExpireDate}',`Operator` = '{permissionData.OperatorID}' Where `SteamID` = '{permissionData.SteamID}' AND `PermissionGroup` = '{permissionData.PermissionID}'");
        }
    }
}
