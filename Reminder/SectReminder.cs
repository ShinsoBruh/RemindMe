using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class SectReminder : GeneralReminder {

        [JsonIgnore]
        public override string Name => "Sect Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply a Sect when playing astrologian.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Apply Sect";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return Service.ClientState.LocalPlayer.ClassJob.Id == 33 &&
                   Service.ClientState.LocalPlayer.Level >= 30 &&
                   Service.ClientState.LocalPlayer.StatusList.All(s => s.StatusId != 839 && s.StatusId != 840);
        }

        public override ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return Service.Data.Excel.GetSheet<Action>().GetRow(16559).Icon;
            } catch {
                return 0;
            }
        }

    }
}
