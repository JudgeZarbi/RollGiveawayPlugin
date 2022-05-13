using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RollGiveawayPlugin
{
    public sealed partial class Plugin
    {
        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        private int saveTimer = 0;

        private Vector4 red = new Vector4(1, 0, 0, 1);
        private Vector4 green = new Vector4(0, 1, 0, 1);

        public void DrawMainWindow()
        {
            if (!visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Roll Giveaway Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (ImGui.BeginTable("Entries", 2, ImGuiTableFlags.ScrollY|ImGuiTableFlags.SizingStretchProp, outer_size: new Vector2(ImGui.GetContentRegionAvail().X * 0.3f, 0)))
				{
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"Player Name");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"Roll");
                    for (int row = 0; row < configuration.currentGame.entries.Count; row++)
					{
                        if (row == 0)
						{
                            ImGui.TableSetColumnIndex(0);
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.2f);
                            ImGui.TableSetColumnIndex(1);
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.05f);
                        }
                        var entry = configuration.currentGame.entries[row];
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextColored(entry.eliminated ? red : green, $"{entry.player}");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextColored(entry.eliminated ? red : green, $"{entry.roll}");
                    }
                    ImGui.EndTable();
                }

                ImGui.SameLine();

                ImGui.BeginGroup();

                ImGui.BeginGroup();
                ImGui.Text("Mode:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.3f);
                if (ImGui.BeginCombo("", configuration.mode.ToString()))
				{
                    var modes = (Plugin.Game.Mode[])Enum.GetValues(typeof(Plugin.Game.Mode));
                    foreach (var mode in modes)
                    {
                        var is_selected = (configuration.mode == (mode));
                        if (ImGui.Selectable(mode.ToString(), is_selected))
						{
                            configuration.mode = mode;
						}

                        if (is_selected)
						{
                            ImGui.SetItemDefaultFocus();
						}
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button("Start new game!"))
                {
                    configuration.currentGame.entries.Clear();
                    configuration.currentGame.state = Game.State.NewGame;
                }

                switch (configuration.currentGame.state)
                {
                    case Game.State.NewGame:
                        if (ImGui.Button("Begin rolls!"))
                        {
                            configuration.currentGame.state = Game.State.WaitingForEntries;
                        }
                        break;
                    case Game.State.WaitingForEntries:
                    case Game.State.WaitingForRolls:
                        if (ImGui.Button("End round!"))
                        {
                            EndRound();
                            configuration.currentGame.state = Game.State.RollsComplete;
                        }
                        break;
                    case Game.State.RollsComplete:
                        if (ImGui.Button("Copy next round names"))
                        {
                            ImGui.SetClipboardText(ListRemainingPlayers());
                        }
                        if (ImGui.Button("Begin next round!"))
                        {
                            RemoveEliminatedPlayers();
                            configuration.currentGame.state = Game.State.WaitingForRolls;
                        }
                        break;
                }

                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();

                switch (configuration.mode)
				{
                    case Game.Mode.MinMax:
                        ImGui.Text("Settings:");
                        ImGui.Text("Min:");
                        ImGui.InputInt("Min", ref configuration.min);
                        ImGui.Text("Max:");
                        ImGui.InputInt("Max", ref configuration.max);
                        break;
                    case Game.Mode.LowestPercent:
                    case Game.Mode.HighestPercent:
                        ImGui.Text("Settings:");
                        ImGui.Text("Percentage per round to keep:");
                        ImGui.InputInt("Percentage per round", ref configuration.percentage);
                        break;
                    case Game.Mode.LowestCount:
                    case Game.Mode.HighestCount:
                        ImGui.Text("Settings:");
                        ImGui.Text("Number per round to keep:");
                        ImGui.InputInt("Number to keep", ref configuration.count);
                        break;
                }
                ImGui.EndGroup();


                ImGui.EndGroup();
            }
            ImGui.End();


            if (saveTimer > 100)
            {
                configuration.Save();
                saveTimer = 0;
            }

            saveTimer++;
        }
    }
}
