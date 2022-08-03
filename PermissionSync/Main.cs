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
            {"","" }
        };
    }
}
