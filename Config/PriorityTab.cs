using System;
using System.Numerics;
using ImGuiNET;
using RemindMe.Config;

namespace RemindMe 
{
    public partial class RemindMeConfig
    {
        public void DrawPriorityTab() 
        {
            ImGui.BeginChild("###priorityScroll", ImGui.GetWindowSize() - (ImGui.GetStyle().WindowPadding * 2) - new Vector2(0, ImGui.GetCursorPosY()));

            foreach (var display in MonitorDisplays.Values) {
                if (ImGui.CollapsingHeader($"{(display.Enabled ? "":"[Disabled] ")}{display.Name}###configDisplay{display.Guid}")) {
                    DrawDisplayPriority(display);
                }
            }
            ImGui.EndChild();
        }

        private void DrawDisplayPriority(MonitorDisplay display)
        {
            ImGui.Checkbox("Enable priority sort", ref display.SortByPriority);

            if (!display.SortByPriority) return;

            ImGui.BeginListBox("", new Vector2(ImGui.GetWindowWidth(), 118f));
            foreach (var cooldown in display.Cooldowns)
            {
                var action = plugin.ActionManager.GetAction(cooldown.ActionId);
                if (action == null) continue;

                var icon = plugin.IconManager.GetActionIcon(action);
                if (icon != null) {
                    ImGui.Image(icon.ImGuiHandle, new Vector2(25));
                } else {
                    ImGui.Dummy(new Vector2(24));
                }

                ImGui.SameLine();
                ImGui.Text(action.Name);

                ImGui.SameLine(ImGui.GetWindowWidth() - 80);
                if (ImGui.ArrowButton("Up", ImGuiDir.Up))
                {
                    // Move action up            
                }

                ImGui.SameLine(ImGui.GetWindowWidth() - 50);
                if (ImGui.ArrowButton("Down", ImGuiDir.Down))
                {
                    // Move action down
                }
            }
            ImGui.EndListBox();
        }
    }
}