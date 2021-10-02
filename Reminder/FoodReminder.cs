using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using ImGuiNET;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {

    internal class FoodReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Food Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to eat some food.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Eat Food";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            if (Service.ClientState.LocalPlayer == null) return false;
            if (Service.ClientState.LocalPlayer.StatusList.Any(s => s.StatusId == 48 && s.RemainingTime > plugin.PluginConfig.FoodReminderMinimum)) return false;
            return true;
        }

        public static void ConfigEditor(RemindMe plugin) {
            ImGui.SetNextItemWidth(-120 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.SliderInt("Minimum Time##foodReminder", ref plugin.PluginConfig.FoodReminderMinimum, 1, 1800)) {
                plugin.PluginConfig.Save();
            }
        }

        public override uint GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return 76851;
        }

    }
}
