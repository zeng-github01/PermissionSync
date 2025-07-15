using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace PermissionSync.Data
{
    public class PermissionData
    {
        public ulong SteamID { get; internal set; }

        public string OperatorID { get; internal set; }
        public string PermissionID { get; internal set; }
        public DateTime ExpireDate { get; internal set; }


        public PermissionData(ulong steamID, string permissionID, DateTime expireDate, string operatorID)
        {
            SteamID = steamID;
            PermissionID = permissionID;
            ExpireDate = expireDate;
            OperatorID = operatorID;
        }
    }
}
