using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace RollGiveawayPlugin
{

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public Plugin.Game.Mode mode = Plugin.Game.Mode.HighestWins;
        public int min = 200;
        public int max = 800;
        public int percentage = 50;
        public int count = 5;

        public Plugin.Game currentGame = new Plugin.Game();



        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
