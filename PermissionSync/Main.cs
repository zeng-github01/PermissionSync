﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.Core.Plugins;
using PermissionSync.Database;
using Rocket.Core.Logging;
using Rocket.Unturned;
using Rocket.API.Collections;

namespace PermissionSync
{
    public class Main :RocketPlugin<Configuration>
    {
        public static Main Instance;
        public DBManager databese;

        protected override void Load()
        {
            Instance = this;
            databese = new DBManager();
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            Logger.Log($"{Name} has been loaded");
            
        }

        private void Events_OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            databese.PermissionSync(player);
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            Logger.Log($"{Name} has been unloaded");
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
