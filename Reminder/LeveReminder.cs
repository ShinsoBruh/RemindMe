// 88 05 ?? ?? ?? ?? 0F B7 41 06

using System;
using Dalamud.Plugin;
using ImGuiNET;
using Newtonsoft.Json;
using RemindMe.Config;

namespace RemindMe.Reminder {
    public unsafe class LeveReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Leve Allowance Reminder";

        public override string Description => "Reminds you to use some leve allowances.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return $"You have {LeveCount} Leve Allowances";
        }

        private byte* leveCountPtr = null;
        private bool error;

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return LeveCount >= plugin.PluginConfig.LeveReminderThreshold;
        }

        private byte LeveCount {
            get {
                if (error) return 0;
                if (leveCountPtr != null) return *leveCountPtr;
                var ptr = Service.SigScanner.GetStaticAddressFromSig("88 05 ?? ?? ?? ?? 0F B7 41 06");
                if (ptr == IntPtr.Zero) {
                    error = true;
                    return 0;
                }
                leveCountPtr = (byte*) ptr;
                return *leveCountPtr;
            }
        }

        public static void ConfigEditor(RemindMe plugin) {
            ImGui.SetNextItemWidth(-120 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.SliderInt("Allowance Threshold##levelReminder", ref plugin.PluginConfig.LeveReminderThreshold, 0, 100)) {
                plugin.PluginConfig.Save();
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Hides the reminder when at above the set number of leve allowances.");
            }
        }

        public override uint GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return 71241;
        }
    }
}
