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
        public string DatabaseTableName;
        public int DatabasePort;
        public void LoadDefaults()
        {
            TableVer = 2;
            DatabaseAddress = "127.0.0.1";
            DatabaseUsername = "root";
            DatabasePassword = "password";
            DatabaseName = "unturned";
            DatabaseTableName = "permissionsync";
            DatabasePort = 3306;
        }
    }
}
