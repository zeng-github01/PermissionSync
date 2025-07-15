using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using Steamworks;
using SDG.Unturned;

namespace PermissionSync.Command
{
    public class PermissionSyncCommand :IRocketCommand
    {
        public string Name => "PermissionSync";

        public List<string> Aliases => new List<string>() { "ps", "permssionsy" };

        public List<string> Permissions => new List<string>() { "permissionsync.sync" };

        public string Help => "Sync permission group between multiple servers";

        public string Syntax => "<add | remove > <player> <PermissionGroupId> [ExpireDate] ";

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public void Execute(IRocketPlayer caller, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("wrong_usage", Syntax), UnityEngine.Color.red);
                return;
            }

            var action = args[0];
            var player = TryGetPlayerSteamID(args[1]);

            if (player == default || !player.IsValid())
            {
                UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("player_not_found"), UnityEngine.Color.red);
                return;
            }

            var permission = args[2];
            DateTime expireDate = DateTime.MaxValue;

            if (args.Length == 4)
            {
                if (!DateTime.TryParse(args[3], out expireDate))
                {
                    UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("illegal_datetime"), UnityEngine.Color.red);
                    return;
                }
            }

            if (action == "add")
            {
                PermissionSync.Instance.Database.AddPermission(caller.Id, player, permission, expireDate);
                UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("add_permission", permission, player.m_SteamID));
            }
            else if (action == "remove")
            {
                PermissionSync.Instance.Database.RemovePermission(player, permission);
                UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("remove_permission", permission, player.m_SteamID));
            }
            else
            {
                UnturnedChat.Say(caller, global::PermissionSync.PermissionSync.Instance.Translate("wrong_usage", Syntax), UnityEngine.Color.red);
            }
        }

        private CSteamID TryGetPlayerSteamID(string input)
        {
            return ulong.TryParse(input, out ulong id)
                ? new CSteamID(id)
                : UnturnedPlayer.FromName(input).CSteamID;
        }
    }
}
