using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.API.Serialisation;
using UnityEngine;

namespace PermissionSync.Util
{
    public class PermissionSyncPermissionsManager : MonoBehaviour, IRocketPermissionsProvider
    {
        public RocketPermissionsProviderResult AddGroup(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }

        public RocketPermissionsProviderResult AddPlayerToGroup(string groupId, IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public RocketPermissionsProviderResult DeleteGroup(string groupId)
        {
            throw new NotImplementedException();
        }

        public RocketPermissionsGroup GetGroup(string groupId)
        {
            throw new NotImplementedException();
        }

        public List<RocketPermissionsGroup> GetGroups(IRocketPlayer player, bool includeParentGroups)
        {
            throw new NotImplementedException();
        }

        public List<Permission> GetPermissions(IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public List<Permission> GetPermissions(IRocketPlayer player, List<string> requestedPermissions)
        {
            throw new NotImplementedException();
        }

        public bool HasPermission(IRocketPlayer player, List<string> requestedPermissions)
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
            throw new NotImplementedException();
        }

        public RocketPermissionsProviderResult RemovePlayerFromGroup(string groupId, IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public RocketPermissionsProviderResult SaveGroup(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }
    }
}
