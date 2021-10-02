using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe.Reminder {
    internal class TankStanceReminder : GeneralReminder {

        private readonly uint[] tankStatusEffectIDs = { 79, 91, 743, 1833 };

        private readonly Dictionary<uint, uint> TankStanceActions = new Dictionary<uint, uint>() {
            { 1, 28 },
            { 3, 48 },
            { 19, 28 },
            { 21, 48 },
            { 32, 3629 },
            { 37, 16142 }
        };

        [JsonIgnore]
        public override string Name => "Tank Stance Reminder";

        [JsonIgnore]
        public override string Description => "Reminds you to apply tank stance if there are no other tanks in the area with it.";

        public override string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                var action = Service.Data.Excel.GetSheet<Action>().GetRow(TankStanceActions[Service.ClientState.LocalPlayer.ClassJob.Id]);
                return $"Tank Stance: {action.Name}";
            } catch {
                return "Tank Stance";
            }
        }

        public override bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                if (Service.ClientState.LocalPlayer.ClassJob.GameData.Role != 1) return false;
                // Check have stance
                if (Service.ClientState.LocalPlayer.StatusList.Any(s => tankStatusEffectIDs.Contains(s.StatusId))) return false;
                // Check other tanks have stance


                foreach (var a in Service.Objects) {
                    if (!(a is PlayerCharacter pc)) continue;
                    if (pc.ClassJob.GameData.Role != 1 || pc.ObjectId == Service.ClientState.LocalPlayer.ObjectId) continue;
                    if (pc.StatusList.Any(s => tankStatusEffectIDs.Contains(s.StatusId))) return false;
                }
                return true;
            } catch {
                return false;
            }

        }

        public override uint GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            try {
                var action = Service.Data.Excel.GetSheet<Action>().GetRow(TankStanceActions[Service.ClientState.LocalPlayer.ClassJob.Id]);
                return action.Icon;
            } catch {
                return 0;
            }
        }

    }
}
