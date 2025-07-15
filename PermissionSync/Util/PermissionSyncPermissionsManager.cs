using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PermissionSync.Database;
using Rocket.API;
using Rocket.API.Serialisation;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace PermissionSync.Util
{
    public sealed class PermissionSyncPermissionsManager : MonoBehaviour, IRocketPermissionsProvider
    {
        private DBConnectionManager _dbConnectionManager = new DBConnectionManager();

        public RocketPermissionsProviderResult AddGroup(RocketPermissionsGroup group)
        {
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                // 检查是否已存在
                connection.Open();
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                checkCmd.Parameters.AddWithValue("@groupId", group.Id);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                connection.Close();
                if (exists)
                {
                    return RocketPermissionsProviderResult.DuplicateEntry;
                }
                _dbConnectionManager.ExecuteQuery(false,
                    $"INSERT INTO `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` (`GroupID`, `GroupName`, `GroupColor`, `GroupPriority`, `GroupPrefix` , `GroupSuffix`) VALUES ('{group.Id}', '{group.DisplayName}', '{group.Color}', '{group.Priority}', '{group.Prefix}', '{group.Suffix}')");
                // 插入权限子表
                if (group.Permissions != null)
                {
                    foreach (var perm in group.Permissions)
                    {
                        _dbConnectionManager.ExecuteQuery(false,
                            $"INSERT INTO `{PermissionSync.Instance.Configuration.Instance.PermissionSubTableName}` (`GroupID`, `PermissionName`, `PermissionCooldown`) VALUES ('{group.Id}', '{perm.Name}', '{perm.Cooldown}')");
                    }
                }
                return RocketPermissionsProviderResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return RocketPermissionsProviderResult.UnspecifiedError;
            }
            finally
            {
                connection.Close();
            }
        }

        public RocketPermissionsProviderResult AddPlayerToGroup(string groupId, IRocketPlayer player)
        {
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                // 检查组是否存在
                connection.Open();
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                checkCmd.Parameters.AddWithValue("@groupId", groupId);
                var groupExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (!groupExists)
                {
                    return RocketPermissionsProviderResult.GroupNotFound;
                }
                // 检查玩家是否已在该组
                var checkPlayerCmd = connection.CreateCommand();
                checkPlayerCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `SteamID` = @steamid AND `PermissionGroup` = @groupId";
                checkPlayerCmd.Parameters.AddWithValue("@steamid", player.Id);
                checkPlayerCmd.Parameters.AddWithValue("@groupId", groupId);
                var alreadyInGroup = Convert.ToInt32(checkPlayerCmd.ExecuteScalar()) > 0;
                if (alreadyInGroup)
                {
                    return RocketPermissionsProviderResult.DuplicateEntry;
                }
                var expireDate = DateTime.MaxValue;
                var operatorId = "PermissionSyncPermissionManager";
                _dbConnectionManager.ExecuteQuery(false,
                    $"INSERT INTO `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` (`SteamID`, `PermissionGroup`, `ExpireDate`, `Operator`) VALUES ('{player.Id}', '{groupId}', '{expireDate:yyyy-MM-dd HH:mm:ss}', '{operatorId}')");
                return RocketPermissionsProviderResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return RocketPermissionsProviderResult.UnspecifiedError;
            }
            finally
            {
                connection.Close();
            }
        }

        public RocketPermissionsProviderResult DeleteGroup(string groupId)
        {
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                // 检查组是否存在
                connection.Open();
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                checkCmd.Parameters.AddWithValue("@groupId", groupId);
                var groupExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                connection.Close();
                if (!groupExists)
                {
                    return RocketPermissionsProviderResult.GroupNotFound;
                }
                _dbConnectionManager.ExecuteQuery(false,
                    $"DELETE FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = '{groupId}'");
                _dbConnectionManager.ExecuteQuery(false,
                    $"DELETE FROM `{PermissionSync.Instance.Configuration.Instance.PermissionSubTableName}` WHERE `GroupID` = '{groupId}'");
                _dbConnectionManager.ExecuteQuery(false,
                    $"DELETE FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `PermissionGroup` = '{groupId}'");
                return RocketPermissionsProviderResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return RocketPermissionsProviderResult.UnspecifiedError;
            }
            finally
            {
                connection.Close();
            }
        }

        public RocketPermissionsGroup GetGroup(string groupId)
        {
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                command.Parameters.AddWithValue("@groupId", groupId);
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var group = new RocketPermissionsGroup
                    {
                        Id = reader["GroupID"].ToString(),
                        DisplayName = reader["GroupName"].ToString(),
                        Color = reader["GroupColor"].ToString(),
                        Priority = Convert.ToInt16(reader["GroupPriority"]),
                        Prefix = reader["GroupPrefix"].ToString(),
                        Permissions = GetPermissionsBelongGroup(groupId)
                    };
                    return group;
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
            return null;
        }

        public List<RocketPermissionsGroup> GetGroups(IRocketPlayer player, bool includeParentGroups)
        {
            var groups = new List<RocketPermissionsGroup>();
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT `PermissionGroup` FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `SteamID` = @steamid";
                command.Parameters.AddWithValue("@steamid", player.Id);
                var reader = command.ExecuteReader();
                var groupIds = new List<string>();
                while (reader.Read())
                {
                    groupIds.Add(reader["PermissionGroup"].ToString());
                }
                reader.Close();
                foreach (var groupId in groupIds)
                {
                    var group = GetGroup(groupId);
                    if (group != null)
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

        public List<Permission> GetPermissions(IRocketPlayer player)
        {
            var permissions = new List<Permission>();
            var groups = GetGroups(player, false);
            foreach (var group in groups)
            {
                permissions.AddRange(group.Permissions);
            }
            return permissions;
        }

        public List<Permission> GetPermissions(IRocketPlayer player, List<string> requestedPermissions)
        {
            var allPerms = GetPermissions(player);
            return allPerms.Where(p => requestedPermissions.Contains(p.Name)).ToList();
        }

        public bool HasPermission(IRocketPlayer player, List<string> requestedPermissions)
        {
            var perms = GetPermissions(player, requestedPermissions);
            return perms.Count == requestedPermissions.Count;
        }

        public void Reload()
        {
            // 可根据需要实现缓存刷新等逻辑
        }

        public RocketPermissionsProviderResult RemovePlayerFromGroup(string groupId, IRocketPlayer player)
        {
            try
            {
                // 检查权限组是否存在
                var connection = _dbConnectionManager.CreateConnection();
                connection.Open();
                var checkGroupCmd = connection.CreateCommand();
                checkGroupCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                checkGroupCmd.Parameters.AddWithValue("@groupId", groupId);
                var groupExists = Convert.ToInt32(checkGroupCmd.ExecuteScalar()) > 0;
                if (!groupExists)
                {
                    connection.Close();
                    return RocketPermissionsProviderResult.GroupNotFound;
                }
                // 检查玩家是否存在于该组
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `SteamID` = @steamid AND `PermissionGroup` = @groupId";
                checkCmd.Parameters.AddWithValue("@steamid", player.Id);
                checkCmd.Parameters.AddWithValue("@groupId", groupId);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                connection.Close();
                if (!exists)
                {
                    return RocketPermissionsProviderResult.PlayerNotFound;
                }
                _dbConnectionManager.ExecuteQuery(false,
                    $"DELETE FROM `{PermissionSync.Instance.Configuration.Instance.PermissionPlayerTableName}` WHERE `SteamID` = '{player.Id}' AND `PermissionGroup` = '{groupId}'");
                return RocketPermissionsProviderResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return RocketPermissionsProviderResult.UnspecifiedError;
            }
        }

        public RocketPermissionsProviderResult SaveGroup(RocketPermissionsGroup group)
        {
            try
            {
                // 检查组是否存在
                var connection = _dbConnectionManager.CreateConnection();
                connection.Open();
                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` WHERE `GroupID` = @groupId";
                checkCmd.Parameters.AddWithValue("@groupId", group.Id);
                var groupExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                connection.Close();
                if (!groupExists)
                {
                    return RocketPermissionsProviderResult.GroupNotFound;
                }
                _dbConnectionManager.ExecuteQuery(false,
                    $"UPDATE `{PermissionSync.Instance.Configuration.Instance.PermissionGroupTableName}` SET `GroupName` = '{group.DisplayName}', `GroupColor` = '{group.Color}', `GroupPriority` = '{group.Priority}', `GroupPrefix` = '{group.Prefix}' WHERE `GroupID` = '{group.Id}'");
                // 更新权限子表
                _dbConnectionManager.ExecuteQuery(false,
                    $"DELETE FROM `{PermissionSync.Instance.Configuration.Instance.PermissionSubTableName}` WHERE `GroupID` = '{group.Id}'");
                if (group.Permissions != null)
                {
                    foreach (var perm in group.Permissions)
                    {
                        _dbConnectionManager.ExecuteQuery(false,
                            $"INSERT INTO `{PermissionSync.Instance.Configuration.Instance.PermissionSubTableName}` (`GroupID`, `PermissionName`, `PermissionCooldown`) VALUES ('{group.Id}', '{perm.Name}', '{perm.Cooldown}')");
                    }
                }
                return RocketPermissionsProviderResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return RocketPermissionsProviderResult.UnspecifiedError;
            }
        }

        private List<Permission> GetPermissionsBelongGroup(string groupId)
        {
            var list = new List<Permission>();
            var connection = _dbConnectionManager.CreateConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM `{PermissionSync.Instance.Configuration.Instance.PermissionSubTableName}` WHERE `GroupID` = @groupid";
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
    }
}
