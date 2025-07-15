using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.Core.Plugins;
using PermissionSync.Database;
using Rocket.Core.Logging;
using Rocket.Unturned;
using Rocket.API.Collections;
using PermissionSync.Enum;
using Rocket.Core;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Rocket.API.Extensions;
using PermissionSync.Util;

namespace PermissionSync
{
    public class PermissionSync :RocketPlugin<Configuration>
    {
        public static PermissionSync Instance { get; private set; }
        public DBManager Database { get; private set; }

        protected override void Load()
        {
            Instance = this;
            Database = new DBManager();
            SyncGroup();
            //R.Permissions = gameObject.TryAddComponent<PermissionSyncPermissionsManager>();
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            Logger.Log($"{Name} has been loaded");
            
        }

        private void Events_OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            SyncPermission(player);
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            Logger.Log($"{Name} has been unloaded");
        }

        private void SyncPermission(UnturnedPlayer player)
        {
            var servergroupids = GetPlayerPermissionGroupId(player);
            // Get Player's PermissionGroup
            var dbgroups = Database.GetPlayerPermissionData(player.CSteamID, EDBQueryType.ByStamID);
            foreach (var dbgroup in dbgroups)
            {
                if (servergroupids.Contains(dbgroup.PermissionID))
                {
                    if (dbgroup.ExpireDate < DateTime.Now)
                    {
                        R.Permissions.RemovePlayerFromGroup(dbgroup.PermissionID, player);
                        UnturnedChat.Say(player, global::PermissionSync.PermissionSync.Instance.Translate("permission_expired", dbgroup.PermissionID));
                    }
                }
                else
                {
                    if (dbgroup.ExpireDate >= DateTime.Now)
                    {
                        R.Permissions.AddPlayerToGroup(dbgroup.PermissionID, player);
                        UnturnedChat.Say(player, global::PermissionSync.PermissionSync.Instance.Translate("sync_permission", dbgroup.PermissionID));
                    }
                }
            }

        }

        private void SyncGroup()
        {
            var servergroup = Database.GetRocketPermissionsGroup();
            foreach (var group in servergroup)
            {
                if (R.Permissions.GetGroup(group.Id) == default)
                {
                    R.Permissions.AddGroup(group);
                }
                else
                {
                    var members = R.Permissions.GetGroup(group.Id).Members;
                    group.Members = members;
                    R.Permissions.SaveGroup(group);
                }
            }
        }

        private List<string> GetPlayerPermissionGroupId(UnturnedPlayer player)
        {
            var groupids = new List<string>();
            var playergroups = R.Permissions.GetGroups(player, true);
            foreach (var group in playergroups)
            {
                groupids.Add(group.Id);
            }
            return groupids;
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            {"player_not_found","Player not found!" },
            {"permission_expired","your permission group has been expired:{0}." },
            {"sync_permission","Your permission group has been synced." },
            {"add_permission","Permission group:{0} has been added to {1}" },
            {"remove_permission","Successfully removed permission group:{0} from {1}" },
            {"wrong_usage","wrong usage.Usage:{0}." },
            {"illegal_datetime","Illegal expire date.please insert like '2022-01-31'."}
        };
    }
}
