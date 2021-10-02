using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace RemindMe {
    public partial class RemindMeConfig {

        private int selectValue = 80;

        public void DrawDebugTab() {
            try {
                ImGui.Text($"Current ClassJobID: {Service.ClientState.LocalPlayer.ClassJob.Id}");
                ImGui.Text($"Current Level: {Service.ClientState.LocalPlayer.Level}");
                ImGui.Text($"In PvP: {plugin.InPvP}");
                ImGui.Text($"Not In Combat for: {plugin.OutOfCombatTimer.Elapsed.TotalSeconds} seconds.");

                if (Service.Targets.Target is BattleChara battleChara) {
                    ImGui.Text("\nEffects on Target: ");
                    foreach (var se in battleChara.StatusList) {
                        if (se.StatusId <= 0) continue;
                        var status = Service.Data?.Excel.GetSheet<Status>()?.GetRow(se.StatusId);
                        if (status == null) continue;
                        ImGui.Text($"\t{status.Name}: {status.RowId}  [{se.Param}, {se.RemainingTime}]");
                    }
                }


                ImGui.Text("\nEffects on Self: ");
                foreach (var se in Service.ClientState.LocalPlayer.StatusList) {
                    if (se.StatusId <= 0) continue;
                    var status = Service.Data?.Excel.GetSheet<Status>()?.GetRow(se.StatusId);
                    if (status == null) continue;
                    ImGui.Text($"\t{status.Name}: {status.RowId}  [{se.Param}, {se.RemainingTime}]");
                }

                var lastAction = Service.Data?.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?.GetRow(plugin.ActionManager.LastActionId);
                ImGui.Text(lastAction != null ? $"\nLast Action: [{lastAction.RowId}] {lastAction.Name}" : $"\nLast Action: [{plugin.ActionManager.LastActionId}] Unknown");

                if (lastAction != null) {
                    var ptr = plugin.ActionManager.GetCooldownPointer(lastAction.CooldownGroup).ToInt64().ToString("X");
                    ImGui.InputText("Cooldown Ptr", ref ptr, 16, ImGuiInputTextFlags.ReadOnly);
                }

                if (lastAction != null) {
                    var maxCharges0 = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.GetMaxCharges(lastAction.RowId, 0);
                    var maxCharges80 = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.GetMaxCharges(lastAction.RowId, 80);
                    ImGui.Text($"Last Action Max Charges\n\tData Sheet: {lastAction.MaxCharges}\n\tCurrent Level: {maxCharges0}\n\tLevel 80: {maxCharges80}");
                }

                // Bars

                var sw = new Stopwatch();
                sw.Start();

                ImGui.SliderFloat("Debug Bars Fill Percent", ref debugFraction, 0, 1);


                var completeColor = new Vector4(1f, 0f, 0f, 0.25f);
                var incompleteColor = new Vector4(0f, 0f, 1f, 0.25f);

                var usedFraction = (float)Math.Min(1, Math.Max(0, debugFraction));

                ImGui.Text($"{usedFraction:F2}");
                sw.Start();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromBottom, incompleteColor, completeColor);
                ImGui.SameLine();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromTop, incompleteColor, completeColor);

                ImGui.SameLine();
                ImGui.BeginGroup();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromLeft, incompleteColor, completeColor);
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromRight, incompleteColor, completeColor);
                usedFraction = 1 - usedFraction;

                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromBottom, incompleteColor, completeColor);
                ImGui.SameLine();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(40, 200), usedFraction, RemindMe.FillDirection.FromTop, incompleteColor, completeColor);

                ImGui.SameLine();
                ImGui.BeginGroup();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromLeft, incompleteColor, completeColor);
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(200, 40), usedFraction, RemindMe.FillDirection.FromRight, incompleteColor, completeColor);

                ImGui.EndGroup();
                ImGui.EndGroup();

                sw.Stop();
                ImGui.Text($"Time to draw bars: {sw.ElapsedTicks}");

            } catch {
                // ignored
            }

        }
    }
}
