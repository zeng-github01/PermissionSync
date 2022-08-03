using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;

namespace PermissionSync.Command
{
    public class PermissionSync :IRocketCommand
    {
        public string Name => "PermissionSync";

        public List<string> Aliases => new List<string>() { "ps", "permssionsy" };

        public List<string> Permissions => new List<string>() { "permissionsync.sync" };

        public string Help => "Sync permission group between multiple servers";

        public string Syntax => "<add | remove > <player> <PermiisonGroupId> [ExpireDate] ";

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public void Execute(IRocketPlayer caller, string[] args)
        {
            if (args.Length > 2 && args.Length <= 4)
            {
                if (args.Length == 3)
                {
                    if (args[0] == "add")
                    {
                        if (ulong.TryParse(args[1], out ulong result))
                        {
                            var player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(result));
                            Main.Instance.databese.AddPermission((caller.Id),player, args[2]);
                            UnturnedChat.Say(caller, Main.Instance.Translate("add_permission"));
                        }
                        else
                        {
                            var player = UnturnedPlayer.FromName(args[1]);
                            if (player != null)
                            {
                                Main.Instance.databese.AddPermission(caller.Id, player, args[2]);
                                UnturnedChat.Say(caller, Main.Instance.Translate("add_permission"));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, Main.Instance.Translate("player_not_found"), UnityEngine.Color.red);
                            }
                        }
                    }
                    if (args[0] == "remove")
                    {
                        if (ulong.TryParse(args[1], out ulong result))
                        {
                            var player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(result));
                            Main.Instance.databese.RemovePermission(player, args[2]);
                            UnturnedChat.Say(caller, Main.Instance.Translate("remove_permission"));
                        }
                        else
                        {
                            var player = UnturnedPlayer.FromName(args[1]);
                            if (player != null)
                            {
                                Main.Instance.databese.RemovePermission(player, args[2]);
                                UnturnedChat.Say(caller, Main.Instance.Translate("remove_permission"));
                            }
                            else
                            {
                                UnturnedChat.Say(caller, Main.Instance.Translate("player_not_found"), UnityEngine.Color.red);
                            }
                        }
                    }
                }
                if(args.Length == 4)
                {
                    if (args[0] == "add")
                    {
                        if (ulong.TryParse(args[1], out ulong result))
                        {
                            var player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(result));
                            Main.Instance.databese.AddPermission(caller.Id,player, args[2], args[3]);
                            UnturnedChat.Say(caller, Main.Instance.Translate("add_permission"));
                        }
                        else
                        {
                            var player = UnturnedPlayer.FromName(args[1]);
                            if (player != null)
                            {
                                Main.Instance.databese.AddPermission(caller.Id, player, args[2], args[3]);
                            }
                            else
                            {
                                UnturnedChat.Say(caller, Main.Instance.Translate("player_not_found"), UnityEngine.Color.red);
                            }
                        }
                    }
                    if (args[0] == "remove")
                    {
                        if (ulong.TryParse(args[1], out ulong result))
                        {
                            var player = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(result));
                            Main.Instance.databese.RemovePermission(player, args[2]);
                        }
                        else
                        {
                            var player = UnturnedPlayer.FromName(args[1]);
                            if (player != null)
                            {
                                Main.Instance.databese.RemovePermission(player, args[2]);
                            }
                            else
                            {
                                UnturnedChat.Say(caller, Main.Instance.Translate("player_not_found"), UnityEngine.Color.red);
                            }
                        }
                    }
                }
            }
            else
            {
                UnturnedChat.Say(caller, Main.Instance.Translate(""), UnityEngine.Color.red);
            }
        }
    }
}
