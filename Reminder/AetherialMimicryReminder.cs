using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class AetherialMimicryReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Aetherial Mimicry Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply mimicry.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Aetherial Mimicry";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display)
        {
            return Service.ClientState.LocalPlayer is not null && Service.ClientState.LocalPlayer.ClassJob.Id == 36 && Service.ClientState.LocalPlayer.StatusList.All(s => s.StatusId != 2124 && s.StatusId != 2125 && s.StatusId != 2126);
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return Service.Data.Excel.GetSheet<Action>().GetRow(18322).Icon;
            } catch {
                return 0;
            }
        }

    }
}
