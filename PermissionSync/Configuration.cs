using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;

namespace PermissionSync
{
    public class Configuration :IRocketPluginConfiguration
    {
        public int TableVer;
        public string DatabaseAddress;
        public string DatabaseUsername;
        public string DatabasePassword;
        public string DatabaseName;
        public string PermissionPlayerTableName;
        public string PermissionGroupTableName;
        public string PermissionSubTableName;
        public int DatabasePort;
        public bool SyncPermissionGroups;
        public bool UseRocketPermissionSystem;
        public void LoadDefaults()
        {
            TableVer = 2;
            DatabaseAddress = "127.0.0.1";
            DatabaseUsername = "root";
            DatabasePassword = "password";
            DatabaseName = "unturned";
            PermissionPlayerTableName = "permissionsync_player";
            PermissionGroupTableName = "permissionsync_groups";
            PermissionSubTableName = "permissionsync_permission";
            DatabasePort = 3306;
            SyncPermissionGroups = true;
            UseRocketPermissionSystem = true;
        }
    }
}
