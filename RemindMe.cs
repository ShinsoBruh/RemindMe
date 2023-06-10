using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using RemindMe.Config;

namespace RemindMe {

    public unsafe partial class RemindMe : IDalamudPlugin {
        public string Name => "Remind Me";
        public RemindMeConfig PluginConfig { get; private set; }
        public bool InPvP { get; private set; } = false;

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow;

        public IconManager IconManager;

        private readonly Stopwatch generalStopwatch = new Stopwatch();

        internal Stopwatch OutOfCombatTimer = new Stopwatch();

        internal Dictionary<uint, List<GameObject>> ActorsWithStatus = new Dictionary<uint, List<GameObject>>();
        private readonly Stopwatch cacheTimer = new Stopwatch();

        private Exception configLoadException;

        private uint* blueSpellBook;
        public uint[] BlueMagicSpellbook { get; } = new uint[24];

        private delegate void* UpdateRetainerListDelegate(void* a);
        private Hook<UpdateRetainerListDelegate> updateRetainerListHook;

        public void Dispose() {
            Service.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
            Service.UiBuilder.Draw -= this.BuildUI;
            Service.Framework.Update -= FrameworkUpdate;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            generalStopwatch.Stop();
            OutOfCombatTimer.Stop();
            cacheTimer.Stop();
            RemoveCommands();
            updateRetainerListHook?.Disable();
            updateRetainerListHook?.Dispose();
        }

        public void LoadConfig(bool clearConfig = false) {
            try {
                if (clearConfig) {
                    this.PluginConfig = new RemindMeConfig();
                } else {
                    this.PluginConfig = (RemindMeConfig)Service.PluginInterface.GetPluginConfig() ?? new RemindMeConfig();
                }
                this.PluginConfig.Init(this);
            } catch (Exception ex) {
                PluginLog.LogError("Failed to load config.");
                PluginLog.LogError(ex.ToString());
                PluginConfig = new RemindMeConfig();
                PluginConfig.Init(this);
                configLoadException = ex;
            }
        }


        public RemindMe(DalamudPluginInterface pluginInterface) {
            pluginInterface.Create<Service>();
            FFXIVClientStructs.Resolver.Initialize(Service.SigScanner.SearchBase);
            generalStopwatch.Start();
            cacheTimer.Start();
#if DEBUG
            drawConfigWindow = true;
#endif
            LoadConfig();

            Service.Framework.Update += FrameworkUpdate;

            IconManager = new IconManager();

            actionManagerStatic = Service.SigScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");
            blueSpellBook = (uint*) (Service.SigScanner.GetStaticAddressFromSig("0F B7 0D ?? ?? ?? ?? 84 C0") + 0x2A);
            PluginLog.Verbose($"Blue Spell Book: {(ulong) blueSpellBook:X}");

            ActionManager = new ActionManager(this, actionManagerStatic);

            Service.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            Service.UiBuilder.Draw += this.BuildUI;

            Service.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(this, Service.ClientState.TerritoryType);

            updateRetainerListHook = new Hook<UpdateRetainerListDelegate>(Service.SigScanner.ScanText("40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 20 84 C0 74 0F 48 8B 03 48 8B CB 48 83 C4 20 5B 48 FF 60 18 E8"), new UpdateRetainerListDelegate(UpdateRetainerListDetour));
            updateRetainerListHook.Enable();

            SetupCommands();
        }

        private void* UpdateRetainerListDetour(void* a) {
            PluginLog.Log($"UpdateRetainerListDetour: {(ulong)a:X}");
            return updateRetainerListHook.Original(a);
        }
        private void TerritoryChanged(object sender, ushort e) {
            InPvP = Service.Data.GetExcelSheet<TerritoryType>()?.GetRow(e)?.IsPvpZone ?? false;
        }

        private void FrameworkUpdate(Framework framework) {
            try {
                if (Service.Condition[ConditionFlag.LoggingOut]) return;
                if (!Service.Condition.Any()) return;
                if (Service.ClientState?.LocalPlayer?.ClassJob == null) return;
                var inCombat = Service.Condition[ConditionFlag.InCombat];
                if (OutOfCombatTimer.IsRunning && inCombat) {
                    generalStopwatch.Restart();
                    ActionManager.ResetTimers();
                    OutOfCombatTimer.Stop();
                    OutOfCombatTimer.Reset();
                } else if (!OutOfCombatTimer.IsRunning && !inCombat) {
                    OutOfCombatTimer.Start();
                }


                if (cacheTimer.ElapsedMilliseconds >= PluginConfig.PollingRate) {
                    cacheTimer.Restart();
                    ActorsWithStatus.Clear();
                    foreach (var a in Service.Objects) {
                        if (a is not BattleChara bChara) continue;
                        // TODO: Deal with this shit
                        if (a is BattleNpc bNpc && bNpc.NameId != 541 && *(ulong*) (a.Address + 0xF0) == 0 || ((*(uint*) (a.Address + 0x104)) & 0x10000) == 0x10000) continue;
                        foreach (var s in bChara.StatusList) {
                            if (s.StatusId == 0) continue;
                            var eid = s.StatusId;
                            if (!ActorsWithStatus.ContainsKey(eid)) ActorsWithStatus.Add(eid, new List<GameObject>());
                            if (ActorsWithStatus[eid].Contains(a)) continue;
                            ActorsWithStatus[eid].Add(a);
                        }
                    }

                    // Blue Magic Spellbook
                    if (BlueMagicSpellbook != null && Service.ClientState.LocalPlayer.ClassJob.Id == 36) {
                        for (var i = 0; i < BlueMagicSpellbook.Length; i++) {
                            BlueMagicSpellbook[i] = blueSpellBook[i];
                        }
                    }
                }

            } catch (Exception ex) {
                PluginLog.Error(ex, "Error in RemindMe.FrameworkUpdate");
            }
        }

        private void OnOpenConfigUi() {
            drawConfigWindow = true;
        }

        public void SetupCommands() {
            Service.Commands.AddHandler("/remindme", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler) {
                HelpMessage = $"Open config window for {this.Name}",
                ShowInHelp = true
            });
        }

        public void OnConfigCommandHandler(string command, string args) {
            drawConfigWindow = !drawConfigWindow;
        }

        public void RemoveCommands() {
            Service.Commands.RemoveHandler("/remindme");
        }

        private void TextShadowed(string text, Vector4 foregroundColor, Vector4 shadowColor, byte shadowWidth = 1) {
            var x = ImGui.GetCursorPosX();
            var y = ImGui.GetCursorPosY();

            for (var i = -shadowWidth; i < shadowWidth; i++) {
                for (var j = -shadowWidth; j < shadowWidth; j++) {
                    ImGui.SetCursorPosX(x + i);
                    ImGui.SetCursorPosY(y + j);
                    ImGui.TextColored(shadowColor, text);
                }
            }
            ImGui.SetCursorPosX(x);
            ImGui.SetCursorPosY(y);
            ImGui.TextColored(foregroundColor, text);
        }


        private delegate bool ActionSpecialCheckDelegate(MonitorDisplay display, CooldownMonitor cooldownMonitor, DalamudPluginInterface pluginInterface);

        private Dictionary<uint, ActionSpecialCheckDelegate> actionSpecialChecks = new Dictionary<uint, ActionSpecialCheckDelegate> {
            { 38, (_, _, _) => Service.ClientState.LocalPlayer?.Level < 70 },
            { 2872, (_, _, _) => Service.ClientState.LocalPlayer?.Level < 76 },
            { 7400, ((display, monitor, pluginInterface) => {
                // Nastrond, Only show if in Life of the Dragon
                if (Service.ClientState.LocalPlayer.ClassJob.Id != 22) return false;
                var jobBar = Service.JobGauges.Get<DRGGauge>();
                return jobBar.BOTDState == BOTDState.LOTD;
            })},
            { 3555, ((display, monitor, pluginInterface) => {
                // Geirskogul, Only show if not in Life of the Dragon
                if (Service.ClientState.LocalPlayer.ClassJob.Id != 22) return false;
                var jobBar = Service.JobGauges.Get<DRGGauge>();
                return jobBar.BOTDState != BOTDState.LOTD;
            })},
        };

        private List<DisplayTimer> GetTimerList(MonitorDisplay display) {
            var timerList = new List<DisplayTimer>();
            if (InPvP) return timerList;
            try {
                if (display.Cooldowns.Count > 0) {

                    foreach (var cd in display.Cooldowns.Where(cd => {
                        if (cd.ClassJob != Service.ClientState.LocalPlayer.ClassJob.Id) return false;
                        var action = ActionManager.GetAction(cd.ActionId, true);
                        if (action == null || !action.ClassJobCategory.Value.HasClass(Service.ClientState.LocalPlayer.ClassJob.Id)) return false;
                        if (action.ClassJobLevel > Service.ClientState.LocalPlayer.Level) return false;
                        if (action.ClassJob.Row == 36 && !BlueMagicSpellbook.Contains(action.RowId)) return false;
                        var cooldown = ActionManager.GetActionCooldown(action);
                        if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                        if (display.OnlyShowCooldown && !cooldown.IsOnCooldown) return false;
                        if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                        if (display.LimitDisplayReadyTime && cooldown.CompleteFor > display.LimitDisplayReadyTimeSeconds) return false;
                        if (actionSpecialChecks.ContainsKey(action.RowId)) {
                            if (!actionSpecialChecks[action.RowId](display, cd, Service.PluginInterface)) return false;
                        }
                        return true;
                    })) {
                        var action = ActionManager.GetAction(cd.ActionId);

                        if (action != null) {
                            var cooldown = ActionManager.GetActionCooldown(action);
                            timerList.Add(new DisplayTimer {
                                TimerMax = cooldown.CooldownMax,
                                TimerCurrent = cooldown.CooldownElapsed + cooldown.CompleteFor,
                                FinishedColor = display.AbilityReadyColor,
                                ProgressColor = display.AbilityCooldownColor,
                                IconId = IconManager.GetActionIconId(action),
                                Name = action.Name
                            });
                        }
                    }
                }
            } catch (Exception ex) {
                PluginLog.LogError("Error parsing cooldowns.");
                PluginLog.Log(ex.ToString());
            }

            try {
                if (display.StatusMonitors.Count > 0) {

                    var localPlayerAsList = new List<GameObject>() { Service.ClientState.LocalPlayer };

                    foreach (var sd in display.StatusMonitors.Where(sm => sm.ClassJob == Service.ClientState.LocalPlayer.ClassJob.Id)) {
                        if (Service.ClientState.LocalPlayer.Level < sd.MinLevel || Service.ClientState.LocalPlayer.Level > sd.MaxLevel) continue;
                        var showMissing = sd.AlwaysAvailable && !display.NoMissingStatus;
                        foreach (var sid in sd.StatusIDs) {
                            var status = Service.Data.Excel.GetSheet<Status>().GetRow(sid);
                            if (status == null) continue;

                            if (!ActorsWithStatus.ContainsKey(status.RowId)) continue;

                            foreach (var a in sd.SelfOnly ? localPlayerAsList : ActorsWithStatus[status.RowId]) {
                                if (a is BattleChara battleChara) {
                                    foreach (var se in battleChara.StatusList) {
                                        if (sd.IsRaid == false && se.SourceID != Service.ClientState.LocalPlayer.ObjectId) continue;
                                        if (sd.LimitedZone > 0 && sd.LimitedZone != Service.ClientState.TerritoryType) continue;
                                        if (display.LimitDisplayTime && se.RemainingTime > display.LimitDisplayTimeSeconds) continue;
                                        if (se.StatusId == (short)status.RowId) {
                                            var t = new DisplayTimer {
                                                TimerMax = sd.MaxDuration,
                                                TimerCurrent = sd.MaxDuration <= 0 ? (1 + generalStopwatch.ElapsedMilliseconds / 1000f) : (sd.MaxDuration - se.RemainingTime),
                                                FinishedColor = display.AbilityReadyColor,
                                                ProgressColor = display.StatusEffectColor,
                                                IconId = (ushort) (status.Icon + (sd.Stacking ? se.StackCount - 1 : 0)),
                                                Name = status.Name,
                                                AllowCountdown = sd.MaxDuration > 0,
                                                StackCount = sd.Stacking ? se.StackCount : -1,
                                            };

                                            if (!sd.SelfOnly) {
                                                t.TargetName = a.Name.TextValue;
                                                t.TargetNameOnly = display.StatusOnlyShowTargetName;
                                                t.ClickAction = sd.ClickHandler;
                                                t.ClickParam = a;
                                            }

                                            showMissing = false;
                                            timerList.Add(t);
                                        }
                                    }
                                }
                            }
                        }

                        if (showMissing) {
                            var status = Service.Data.Excel.GetSheet<Status>().GetRow(sd.Status);
                            if (status != null) {
                                var t = new DisplayTimer {
                                    TimerMax = 0,
                                    TimerCurrent = (1 + generalStopwatch.ElapsedMilliseconds / 1000f),
                                    FinishedColor = display.AbilityReadyColor,
                                    ProgressColor = display.StatusEffectColor,
                                    IconId = status.Icon,
                                    Name = status.Name,
                                    AllowCountdown = false,
                                    StackCount = -1,
                                };
                                timerList.Add(t);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                PluginLog.LogError("Error parsing statuses.");
                PluginLog.Log(ex.ToString());
            }

            timerList.Sort((a, b) => {
                var diff = a.TimerRemaining - b.TimerRemaining;
                if (Math.Abs(diff) < 0.1)
                {
                    if (display.SortByPriority && display.SortPriorities.ContainsKey(a.Name) && display.SortPriorities.ContainsKey(b.Name))
                    {
                        return display.SortPriorities[a.Name] - display.SortPriorities[b.Name];
                    }
                    else
                    {
                        return string.CompareOrdinal(a.Name, b.Name); // Equal
                    }
                }
                if (diff < 0) return -1;
                return 1;
            });

            foreach (var reminder in display.GeneralReminders) {
                if (reminder.ShouldShow(Service.PluginInterface, this, display)) {
                    timerList.Insert(0, new DisplayTimer {
                        TimerMax = 1,
                        TimerCurrent = 1 + generalStopwatch.ElapsedMilliseconds / 1000f,
                        FinishedColor = display.AbilityReadyColor,
                        ProgressColor = display.StatusEffectColor,
                        IconId = reminder.GetIconID(Service.PluginInterface, this, display),
                        Name = reminder.GetText(Service.PluginInterface, this, display),
                        AllowCountdown = false,
                        ClickAction = reminder.HasClickHandle(Service.PluginInterface, this, display) ? reminder.ClickHandler : null,
                        ClickParam = null,
                    });
                }
            }

            return timerList;
        }

        private void DrawDisplays() {
            if (Service.Condition[ConditionFlag.LoggingOut]) return;
            if (!Service.Condition.Any()) return;
            if (Service.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;

            foreach (var display in PluginConfig.MonitorDisplays.Values.Where(d => d.Enabled)) {
                if (display.Locked && (display.OnlyInCombat || display.OnlyNotInCombat)) {
                    var inCombat = Service.Condition[ConditionFlag.InCombat];

                    if (inCombat && display.OnlyNotInCombat) continue;
                    if (!inCombat && !display.KeepVisibleOutsideCombat && display.OnlyInCombat) continue;

                    if (!inCombat && display.KeepVisibleOutsideCombat && display.OnlyInCombat) {
                        if (OutOfCombatTimer.Elapsed.TotalSeconds > display.KeepVisibleOutsideCombatSeconds) {
                            continue;
                        }
                    }
                }

                if (display.Locked && display.OnlyInDungeon && !Service.Condition[ConditionFlag.BoundByDuty]) continue;
                if (display.Locked && display.OnlyNotInDungeon && Service.Condition[ConditionFlag.BoundByDuty]) continue;

                var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;

                if (display.Locked) {
                    flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBackground;
                    if (!display.AllowClicking || !display.IsClickableHovered) {
                        flags |= ImGuiWindowFlags.NoMouseInputs;
                    }
                }

                var timerList = display.TimerList ??= GetTimerList(display);
                
                if (timerList.Count > 0 || !display.Locked) {

                    ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.FirstUseEver);
                    ImGui.SetNextWindowPos(new Vector2(250, 250), ImGuiCond.FirstUseEver);

                    if (display.IsClickableHovered || !display.Locked) {
                        ImGui.PushStyleColor(ImGuiCol.Border, display.UnlockedBorderColour);
                        ImGui.PushStyleColor(ImGuiCol.WindowBg, display.UnlockedBackgroundColor);
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
                    }

                    if (display.UseFixedPosition) {
                        var pX = 0f;
                        var pY = 0f;
                        if (display.LastSize != null) {
                            var hPos = display.HorizontalPosition;
                            var vPos = display.VerticalPosition;
                            var size = display.LastSize.Value;
                            var dSize = ImGui.GetIO().DisplaySize;

                            pX = hPos.InvertSide ? dSize.X - size.X : 0;
                            pY = vPos.InvertSide ? dSize.Y - size.Y : 0;

                            if (hPos.UsePercentage) {
                                if (hPos.InvertSide) {
                                    pX -= dSize.X * (hPos.Percentage / 100);
                                } else {
                                    pX += dSize.X * (hPos.Percentage / 100);
                                }
                            } else {
                                if (hPos.InvertSide) {
                                    pX -= hPos.Pixels;
                                } else {
                                    pX += hPos.Pixels;
                                }
                            }

                            if (vPos.UsePercentage) {
                                if (vPos.InvertSide) {
                                    pY -= dSize.Y * (vPos.Percentage / 100);
                                } else {
                                    pY += dSize.Y * (vPos.Percentage / 100);
                                }
                            } else {
                                if (vPos.InvertSide) {
                                    pY -= vPos.Pixels;
                                } else {
                                    pY += vPos.Pixels;
                                }
                            }
                        }


                        ImGui.SetNextWindowPos(new Vector2(pX, pY));
                    }

                    if (display.UseFixedSize) {
                        flags |= ImGuiWindowFlags.NoResize;
                        ImGui.SetNextWindowSize(display.FixedSize);
                    }


                    var isBegin = ImGui.Begin($"Display##{display.Guid}", flags);
                    if (display.IsClickableHovered || !display.Locked) {
                        ImGui.PopStyleColor(2);
                        ImGui.PopStyleVar();
                    }

                    if (isBegin) {

                        if (!display.Locked && !display.HideUnlockedWarning) {
                            var dl = ImGui.GetWindowDrawList();
                            var winWidth = ImGui.GetWindowSize() * Vector2.UnitX;
                            var cPos = ImGui.GetWindowPos() + (winWidth / 2);
                            var text = "DISPLAY IS UNLOCKED";
                            var textSize = ImGui.CalcTextSize(text);
                            dl.AddText(cPos - (textSize * Vector2.UnitX / 2), 0xAA0000FF, text);

                            cPos += textSize * Vector2.UnitY;

                            text = "LOCK TO ALLOW HIDING";
                            textSize = ImGui.CalcTextSize(text);
                            dl.AddText(cPos - (textSize * Vector2.UnitX / 2), 0xAA0000FF, text);
                        }

                        display.IsClickableHovered = false;

                        switch (display.DisplayType) {
                            case 0: {
                                DrawDisplayHorizontal(display, timerList);
                                break;
                            }
                            case 1: {
                                DrawDisplayVertical(display, timerList);
                                break;
                            }
                            case 2: {
                                DrawDisplayIcons(display, timerList);
                                break;
                            }
                            default: {
                                display.DisplayType = 0;
                                DrawDisplayHorizontal(display, timerList);
                                break;
                            }
                        }
                    }

                    display.LastPosition = ImGui.GetWindowPos();

                    if (display.LastSize != null  && !display.UseFixedSize) {
                        var newSize = ImGui.GetWindowSize();
                        if (display.HorizontalPosition.InvertSide) {
                            // Update Horizontal Position for new size
                            var hDiff = display.LastSize.Value.X - newSize.X;

                            if (hDiff != 0) {
                                if (display.HorizontalPosition.UsePercentage) {
                                    display.HorizontalPosition.Percentage += (hDiff / ImGui.GetIO().DisplaySize.X) * 100;
                                } else {
                                    display.HorizontalPosition.Pixels += (int) hDiff;
                                }
                            }
                        }

                        if (display.VerticalPosition.InvertSide) {
                            // Update Vertical Position for new size
                            var vDiff = display.LastSize.Value.Y - newSize.Y;
                            if (vDiff != 0) {
                                if (display.VerticalPosition.UsePercentage) {
                                    display.VerticalPosition.Percentage += (vDiff / ImGui.GetIO().DisplaySize.Y) * 100;
                                } else {
                                    display.VerticalPosition.Pixels += (int) vDiff;
                                }
                            }

                        }

                        display.LastSize = newSize;
                    } else {
                        display.LastSize = ImGui.GetWindowSize();
                    }

                    ImGui.End();
                }
            }
        }
        
        private void BuildUI() {
            if (Service.Condition[ConditionFlag.LoggingOut]) return;
            if (!Service.Condition.Any()) return;
            if (Service.ClientState.LocalPlayer == null) return;

            if (configLoadException != null || PluginConfig == null) {

                ImGui.PushStyleColor(ImGuiCol.TitleBg, 0x880000AA);
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, 0x880000FF);
                ImGui.Begin($"{Name} - Config Load Error", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.PopStyleColor(2);
                ImGui.Text($"{Name} failed to load the config file.");
                ImGui.Text($"Continuing will result in a loss of any configs you have setup for {Name}.");
                ImGui.Text("Please report this error.");

                if (configLoadException != null) {
                    var str = configLoadException.ToString();
                    ImGui.InputTextMultiline("###exceptionText", ref str, uint.MaxValue, new Vector2(-1, 80), ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                }

                ImGui.Dummy(new Vector2(5));
                if (ImGui.Button("Retry Load")) {
                    PluginConfig = null;
                    configLoadException = null;
                    LoadConfig();
                }
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(15));
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0x880000FF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x880000AA);
                if (ImGui.Button("Clear Config")) {
                    LoadConfig(true);
                    configLoadException = null;
                }
                ImGui.PopStyleColor(3);

                ImGui.End();
            } else {
                drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
                if (!InPvP) DrawDisplays();
            }
        }
    }
}
