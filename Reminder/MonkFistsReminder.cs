using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class MonkFistsReminder : GeneralReminder {
        [JsonIgnore]
        public override string Name => "Monk Fists Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply a monk stance.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "Apply Fists";
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return (Service.ClientState.LocalPlayer.ClassJob.Id == 2 || Service.ClientState.LocalPlayer.ClassJob.Id == 20) &&
                   Service.ClientState.LocalPlayer.Level >= 15 &&
                   Service.ClientState.LocalPlayer.StatusList.All(s => s.StatusId != 103 && s.StatusId != 104 && s.StatusId != 105);
        }

        public override uint GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                return Service.Data.Excel.GetSheet<Action>().GetRow(Service.ClientState.LocalPlayer.Level >= 40 ? 63U : 60U).Icon;
            } catch {
                return 0;
            }
        }

    }
}
