using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RollGiveawayPlugin
{
    public sealed partial class Plugin : IDalamudPlugin
    {
        public string Name => "Roll Giveaway Plugin";

        private const string commandName = "/rgiveaway";

        private DalamudPluginInterface pluginInterface { get; init; }
        private CommandManager commandManager { get; init; }
        private ChatGui chatGui { get; init; }
        private Configuration configuration { get; init; }

        public class Entry : IComparable<Entry>
		{
            public string player { get; init; }
            public int roll { get; set; }
            public bool eliminated { get; set; }

			public Entry(string player, int roll)
			{
				this.player = player;
				this.roll = roll;
			}

			public int CompareTo(Entry? other)
			{
                return -this.roll.CompareTo(other?.roll);
			}
		}

        public class Game
		{
            public enum State
            {
                NewGame = 0,
                WaitingForEntries = 1,
                WaitingForRolls = 2,
                RollsComplete = 3,
            }

            public enum Mode
			{
                HighestWins = 0,
                LowestWins = 1,
                HighestPercent = 2,
                LowestPercent = 3,
                HighestCount = 4,
                LowestCount = 5,
                MinMax = 6,
			}

            public State state = State.NewGame;
            public List<Entry> entries = new List<Entry>();
		}

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ChatGui chatGui)
        {
            this.pluginInterface = pluginInterface;
            this.commandManager = commandManager;
            this.chatGui = chatGui;

            this.chatGui.Enable();

            this.configuration = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(this.pluginInterface);

            this.commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the window to use the plugin."
            });

            this.pluginInterface.UiBuilder.Draw += DrawUI;

            this.chatGui.ChatMessage += ProcessMessages;
        }

		public void Dispose()
        {
            this.commandManager.RemoveHandler(commandName);

            this.chatGui.ChatMessage -= ProcessMessages;

            this.pluginInterface.UiBuilder.Draw -= DrawUI;
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.visible = true;
        }

        private void ProcessMessages(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!visible)
			{
                return;
			}

            if (configuration.currentGame.state != Game.State.WaitingForRolls && configuration.currentGame.state != Game.State.WaitingForEntries)
			{
                return;
			}

            if (((int)type & 0x7F) == 74)
			{
                var msgSplit = message.ToString().Split(" ");

                if (msgSplit.Length == 9)
				{
                    return;
				}

                var senderName = string.Join(" ", msgSplit[1..3]);

                //PluginLog.LogWarning($"{senderName}");

                var regex = new Regex(@"([A-Z])");
                var nameSplit = regex.Split(senderName);

                //PluginLog.LogWarning($"{msgSplit.Length}");

                string player;
                player = senderName;

                if (nameSplit.Length == 7)
				{
                    player = $"{nameSplit[1]}{nameSplit[2]}{nameSplit[3]}{nameSplit[4]} - {nameSplit[5]}{nameSplit[6]}";
                }

                //PluginLog.LogWarning($"{player}");

                //PluginLog.LogWarning($"{Int32.Parse(msgSplit[^1][..^1])}");

                foreach (var entry in configuration.currentGame.entries)
                {
                    if (entry.player == player)
					{
                        if (entry.roll >= 0)
						{
                            return;
                        }
						else
						{
                            entry.roll = Int32.Parse(msgSplit[^1][..^1]);
                            PluginLog.LogWarning($"{entry.roll}");
                        }
                    }
                }
                if (configuration.currentGame.state == Game.State.WaitingForEntries)
				{
                    configuration.currentGame.entries.Add(new Entry(player, Int32.Parse(msgSplit[^1][..^1]) ));
				}
            }

            //PluginLog.LogWarning($"{type}, {sender}, {message}");


        }

        private void DrawUI()
        {
            this.DrawMainWindow();
        }

        private void EndRound()
		{
            configuration.currentGame.entries.Sort();

            switch ((configuration.mode))
			{
                case Game.Mode.HighestWins:
                    var targetRoll = configuration.currentGame.entries[0].roll;
                    for (int i = 1; i < configuration.currentGame.entries.Count; i++)
					{
                        if (configuration.currentGame.entries[i].roll < targetRoll)
						{
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    break;
                case Game.Mode.MinMax:
                    var allElim = true;
                    for (int i = 0; i < configuration.currentGame.entries.Count; i++)
                    {
                        if (configuration.currentGame.entries[i].roll > configuration.max || configuration.currentGame.entries[i].roll < configuration.min)
						{
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                        else
						{
                            allElim = false;
						}
                    }
                    if (allElim)
                    {
                        for (int i = 0; i < configuration.currentGame.entries.Count; i++)
                        {
                            configuration.currentGame.entries[i].eliminated = false;
                        }
                    }
                    break;
                case Game.Mode.HighestPercent:
                    var toKeep = Math.Ceiling(configuration.currentGame.entries.Count * (configuration.percentage / 100f));
                    if (toKeep == configuration.currentGame.entries.Count)
					{
                        toKeep--;
					}
                    targetRoll = configuration.currentGame.entries[(int)toKeep - 1].roll;
                    for (int i = (int)toKeep; i < configuration.currentGame.entries.Count; i++)
                    {
                        if (configuration.currentGame.entries[i].roll < targetRoll)
                        {
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    break;
                case Game.Mode.HighestCount:
                    targetRoll = configuration.currentGame.entries[(int)configuration.count - 1].roll;
                    for (int i = configuration.count; i < configuration.currentGame.entries.Count; i++)
                    {
                        if (configuration.currentGame.entries[i].roll < targetRoll)
                        {
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    configuration.count--;
                    break;
                case Game.Mode.LowestWins:
                    targetRoll = configuration.currentGame.entries[configuration.currentGame.entries.Count - 1].roll;
                    for (int i = configuration.currentGame.entries.Count - 2; i >= 0; i--)
                    {
                        if (configuration.currentGame.entries[i].roll > targetRoll)
                        {
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    break;
                case Game.Mode.LowestPercent:
                    toKeep = Math.Ceiling(configuration.currentGame.entries.Count * (configuration.percentage / 100f));
                    if (toKeep == configuration.currentGame.entries.Count)
                    {
                        toKeep--;
                    }
                    targetRoll = configuration.currentGame.entries[configuration.currentGame.entries.Count - (int)toKeep].roll;
                    for (int i = configuration.currentGame.entries.Count - (int)toKeep; i >= 0; i--)
                    {
                        if (configuration.currentGame.entries[i].roll > targetRoll)
                        {
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    break;
                case Game.Mode.LowestCount:
                    targetRoll = configuration.currentGame.entries[configuration.currentGame.entries.Count - configuration.count].roll;
                    for (int i = configuration.currentGame.entries.Count - configuration.count; i >= 0; i--)
                    {
                        if (configuration.currentGame.entries[i].roll > targetRoll)
                        {
                            configuration.currentGame.entries[i].eliminated = true;
                        }
                    }
                    configuration.count--;
                    break;
            }
        }

        private void RemoveEliminatedPlayers()
		{
            for (int i = configuration.currentGame.entries.Count - 1; i >= 0; i--)
            {
                if (configuration.currentGame.entries[i].eliminated)
                {
                    configuration.currentGame.entries.RemoveAt(i);
                }
                else
				{
                    configuration.currentGame.entries[i].roll = -1;

                }
            }

        }

        private string ListRemainingPlayers()
		{
            var list = new List<string>();

            foreach (var entry in configuration.currentGame.entries)
			{
                if (!entry.eliminated)
				{
                    var split = entry.player.Split(" - ");
                    list.Add(split[0]);
				}
			}

            return String.Join(", ", list);
		}
    }
}
