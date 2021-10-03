using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Newtonsoft.Json;
using RemindMe.JsonConverters;

namespace RemindMe.Config {

    public class MonitorDisplay {

        [JsonIgnore] private List<DisplayTimer> cachedTimerList;
        [JsonIgnore] private readonly Stopwatch cacheTimerListStopwatch = new Stopwatch();
        [JsonIgnore]
        public List<DisplayTimer> TimerList {
            get {
                if (cachedTimerList == null) return null;
                if (!cacheTimerListStopwatch.IsRunning) return null;
                if (cacheTimerListStopwatch.ElapsedMilliseconds > UpdateInterval) return null;
                return cachedTimerList;
            }
            set {
                cachedTimerList = value;
                cacheTimerListStopwatch.Restart();
            }
        }

        [JsonIgnore]
        public TimeSpan CacheAge => cacheTimerListStopwatch.Elapsed;
        [JsonIgnore] public Vector2? LastPosition = null;
        [JsonIgnore] public Vector2? LastSize = null;
        private static readonly string[] _displayTypes = new string[] {
            "Horizontal",
            "Vertical",
            "Icons",
        };

        public bool DirectionRtL = false;
        public bool DirectionBtT = false;
        public bool IconVerticalStack = false;

        public int UpdateInterval = 50;

        public bool Enabled = true;
        public Guid Guid;
        public string Name = "New Display";

        public bool Locked = false;
        public bool AllowClicking = false;
        public bool HideUnlockedWarning = false;

        public bool OnlyShowReady = false;
        public bool OnlyShowCooldown = false;

        public int RowSize = 32;
        public float TextScale = 1;
        public int BarSpacing = 5;

        public bool ShowActionIcon = true;
        public float ActionIconScale = 0.9f;
        public bool ReverseSideIcon = false;

        public bool OnlyInCombat = true;
        public bool OnlyNotInCombat = false;
        public bool KeepVisibleOutsideCombat = false;
        public int KeepVisibleOutsideCombatSeconds = 15;

        public bool ShowSkillName = true;
        public bool ShowStatusEffectTarget = true;
        public bool SkillNameRight = false;
        public bool ShowCountdown = false;
        public bool ShowCountdownReady = false;
        public bool ReverseCountdownSide = false;
        public bool StatusOnlyShowTargetName = false;
        public bool NoMissingStatus = false;

        public bool OnlyInDungeon = false;
        public bool OnlyNotInDungeon = false;

        public bool PulseReady = false;
        public float PulseSpeed = 1.0f;
        public float PulseIntensity = 1.0f;

        public bool FillToComplete = false;
        public bool ReverseFill = false;
        public RemindMe.FillDirection IconDisplayFillDirection = RemindMe.FillDirection.FromBottom;

        public bool LimitDisplayTime = false;
        public int LimitDisplayTimeSeconds = 10;

        public bool LimitDisplayReadyTime;
        public int LimitDisplayReadyTimeSeconds = 15;

        public List<CooldownMonitor> Cooldowns = new List<CooldownMonitor>();

        public List<StatusMonitor> StatusMonitors = new List<StatusMonitor>();

        public List<GeneralReminder> GeneralReminders = new List<GeneralReminder>();

        public Vector4 AbilityReadyColor = new Vector4(0.70f, 0.25f, 0.25f, 0.75f);
        public Vector4 AbilityCooldownColor = new Vector4(0.75f, 0.125f, 0.665f, 0.75f);
        public Vector4 StatusEffectColor = new Vector4(1f, 0.5f, 0.1f, 0.75f);
        public Vector4 TextColor = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 BarBackgroundColor = new Vector4(0.3f, 0.3f, 0.3f, 0.5f);

        public Vector4 UnlockedBorderColour = new Vector4(1, 0, 0, 1);
        public Vector4 UnlockedBackgroundColor = new Vector4(0, 0, 0, 1);

        public int DisplayType = 0;

        public class PositionOption {
            public bool InvertSide = false;
            public bool UsePercentage = false;
            public float Percentage = 0;
            public int Pixels = 0;
        }

        public bool UseFixedPosition = false;
        public bool UseFixedSize = false;
        public PositionOption HorizontalPosition = new PositionOption();
        public PositionOption VerticalPosition = new PositionOption();
        public Vector2 FixedSize = new Vector2(200, 200);

        [JsonIgnore] private bool tryDelete;
        [JsonIgnore] private bool tryCopy;
        [JsonIgnore] internal bool IsClickableHovered;
        
        public MonitorDisplay MakeCopy(bool withReminders) {
            // Horrible but easy copy
            var json = JsonConvert.SerializeObject(this);
            var copy = JsonConvert.DeserializeObject<MonitorDisplay>(json) ?? new MonitorDisplay();
            copy.Guid = Guid.NewGuid();
            copy.Name += " (Copy)";

            if (!withReminders) {
                copy.Cooldowns.Clear();
                copy.GeneralReminders.Clear();
                copy.StatusMonitors.Clear();
            }
            
            return copy;
        }

        public void DrawConfigEditor(RemindMeConfig mainConfig, RemindMe plugin, ref Guid? deletedMonitor, ref MonitorDisplay copiedDisplay) {
            ImGui.Indent(10);
            if (ImGui.Checkbox($"Enabled##{this.Guid}", ref this.Enabled)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputText($"###displayName{this.Guid}", ref this.Name, 32)) mainConfig.Save();
            if (ImGui.Checkbox($"Lock Display##{this.Guid}", ref this.Locked)) mainConfig.Save();
            if (!Locked) {
                ImGui.SameLine();
                if (ImGui.Checkbox($"Hide Unlocked Warning##{this.Guid}", ref this.HideUnlockedWarning)) mainConfig.Save();
                ImGui.TextDisabled("An unlocked display will always show the background and border\n");
                ImGui.TextDisabled("and will not respect some options such as 'Hide outside combat'.");
            }
            if (ImGui.Checkbox($"Clickable##{this.Guid}", ref this.AllowClicking)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.TextDisabled("A clickable display will allow selecting targets from status effects\nbut may get in the way of other activity.");
            ImGui.Separator();
            if (ImGui.Combo($"Display Type##{Guid}", ref DisplayType, _displayTypes, _displayTypes.Length)) mainConfig.Save();
            

            if ((DisplayType == 1 || DisplayType == 2) && ImGui.Checkbox($"Right to Left##{Guid}", ref DirectionRtL)) mainConfig.Save();
            if ((DisplayType == 0 || DisplayType == 2) && ImGui.Checkbox($"Bottom to Top##{Guid}", ref DirectionBtT)) mainConfig.Save();
            if (DisplayType == 2 && ImGui.Checkbox($"Vertical Stack##{Guid}", ref IconVerticalStack)) mainConfig.Save();

            ImGui.Separator();
            ImGui.Separator();

            ImGui.Text("Colours");
            ImGui.Separator();

            if (ImGui.ColorEdit4($"Ability Ready##{Guid}", ref AbilityReadyColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Ability Cooldown##{Guid}", ref AbilityCooldownColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Status Effect##{Guid}", ref StatusEffectColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Bar Background##{Guid}", ref BarBackgroundColor)) mainConfig.Save();
            if (ImGui.ColorEdit4($"Text##{Guid}", ref TextColor)) mainConfig.Save();

            if (!Locked && HideUnlockedWarning) {
                if (ImGui.ColorEdit4($"Border Colour (Unlocked)##{Guid}", ref UnlockedBorderColour)) mainConfig.Save();
                if (ImGui.ColorEdit4($"Background Colour (Unlocked)##{Guid}", ref UnlockedBackgroundColor, ImGuiColorEditFlags.AlphaBar)) mainConfig.Save();
            }

            ImGui.Separator();
            ImGui.Separator();

            ImGui.Text("Size and Position");
            ImGui.Separator();



            if (ImGui.Checkbox($"Use Fixed Size##{Guid}", ref UseFixedSize)) mainConfig.Save();

            if (UseFixedSize) {
                if (ImGui.DragFloat2($"##fixedSize##{Guid}", ref FixedSize)) mainConfig.Save();
            } else {
                ImGui.DragFloat2($"##fixedSize##{Guid}", ref FixedSize);
                if (LastSize != null) {
                    FixedSize = LastSize.Value;
                }
            }


            if (ImGui.Checkbox($"Use Fixed Position##{Guid}", ref UseFixedPosition)) mainConfig.Save();

            bool PositionEditor(bool vertical, string name, string normal, string inverted) {
                var positionOption = vertical ? VerticalPosition : HorizontalPosition;
                var r = false;

                ImGui.Text($"{name}:");
                ImGui.Indent();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

                ImGui.SetNextItemWidth(60 * ImGui.GetIO().FontGlobalScale);
                if (positionOption.UsePercentage) {
                    if (!UseFixedPosition) {
                        ImGui.Text($"{positionOption.Percentage}");
                    } else {
                        r |= ImGui.DragFloat($"##pos_percentage_{name}_{Guid}", ref positionOption.Percentage, 0.05f);
                    }
                } else {
                    if (!UseFixedPosition) {
                        ImGui.Text($"{positionOption.Pixels}");
                    } else {
                        r |= ImGui.DragInt($"##pos_pixel_{name}_{Guid}", ref positionOption.Pixels);
                    }
                }
                ImGui.SameLine();
                if (!UseFixedPosition) {
                     ImGui.TextUnformatted(positionOption.UsePercentage ? "%" : "px");
                } else {
                    var percentageOption = positionOption.UsePercentage ? 1 : 0;
                    ImGui.SetNextItemWidth(50 * ImGui.GetIO().FontGlobalScale);
                    if (ImGui.Combo($"##pos_usePercentage_{name}_{Guid}", ref percentageOption, "px\0%")) {
                        r = true;
                        positionOption.UsePercentage = percentageOption == 1;
                    }
                }

                ImGui.SameLine();
                ImGui.Text(" from the ");
                ImGui.SameLine();
                if (!UseFixedPosition) {
                    ImGui.Text(positionOption.InvertSide ? inverted : normal);
                } else {
                    var invertOption = positionOption.InvertSide ? 1 : 0;
                    ImGui.SetNextItemWidth(80 * ImGui.GetIO().FontGlobalScale);
                    if (ImGui.Combo($"##pos_invert_{name}_{Guid}", ref invertOption, $"{normal}\0{inverted}")) {
                        r = true;
                        positionOption.InvertSide = invertOption == 1;
                    }
                }

                ImGui.SameLine();
                ImGui.Text(" side.");

                if (LastPosition != null && LastSize != null) {
                    var cPosPx = vertical ? LastPosition.Value.Y : LastPosition.Value.X;
                    var cSize = vertical ? LastSize.Value.Y : LastSize.Value.X;
                    var cWinSize = vertical ? ImGui.GetIO().DisplaySize.Y : ImGui.GetIO().DisplaySize.X;
                    if (positionOption.InvertSide) {
                        if (!UseFixedPosition || positionOption.UsePercentage) positionOption.Pixels = (int)(cWinSize - (cPosPx + cSize));
                        if (!UseFixedPosition || !positionOption.UsePercentage) positionOption.Percentage = (cWinSize - (cPosPx + cSize)) / cWinSize * 100;
                    } else {
                        if (!UseFixedPosition || positionOption.UsePercentage) positionOption.Pixels = (int)cPosPx;
                        if (!UseFixedPosition || !positionOption.UsePercentage) positionOption.Percentage = cPosPx / cWinSize * 100f;
                    }
                }

                ImGui.PopStyleVar(2);
                ImGui.Unindent();
                return r;
            }

            if (PositionEditor(false, "Horizontal", "Left", "Right")) mainConfig.Save();
            if (PositionEditor(true, "Vertical", "Top", "Bottom")) mainConfig.Save();

            ImGui.Separator();
            ImGui.Separator();

            ImGui.Text("Display Options");
            ImGui.Separator();
            if (ImGui.Checkbox($"Hide outside of combat##{this.Guid}", ref this.OnlyInCombat)) {
                this.OnlyNotInCombat = false;
                mainConfig.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox($"Hide when in combat##{this.Guid}", ref this.OnlyNotInCombat)) {
                this.OnlyInCombat = false;
                mainConfig.Save();
            }
            
            if (OnlyInCombat) {
                ImGui.Indent(20);
                if (ImGui.Checkbox($"Keep visible for###keepVisibleOutsideCombat{this.Guid}", ref this.KeepVisibleOutsideCombat)) mainConfig.Save();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputInt($"seconds after exiting combat.###keepVisibleOutsideCombatSeconds{this.Guid}", ref KeepVisibleOutsideCombatSeconds)) mainConfig.Save();
                if (KeepVisibleOutsideCombatSeconds < 0) {
                    KeepVisibleOutsideCombatSeconds = 0;
                    mainConfig.Save();
                }
                ImGui.Indent(-20);
            }
            
            if (ImGui.Checkbox($"Only show inside dungeons##{this.Guid}", ref this.OnlyInDungeon)) {
                this.OnlyNotInDungeon = false;
                mainConfig.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox($"Only show outside dungeons##{this.Guid}", ref this.OnlyNotInDungeon)) {
                this.OnlyInDungeon = false;
                mainConfig.Save();
            }

            if (ImGui.Checkbox($"Don't show complete cooldowns##{this.Guid}", ref this.OnlyShowCooldown)) {
                OnlyShowReady = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Only show complete cooldowns##{this.Guid}", ref this.OnlyShowReady)) {
                OnlyShowCooldown = false;
                mainConfig.Save();
            }
            if (ImGui.Checkbox($"Fill bar to complete##{this.Guid}", ref this.FillToComplete)) mainConfig.Save();
            if (DisplayType < 2 && ImGui.Checkbox($"Reverse fill direction##{this.Guid}", ref this.ReverseFill)) mainConfig.Save();
            if (DisplayType == 2) { 
                ImGui.BeginGroup();
                plugin.DrawBar(ImGui.GetCursorScreenPos(), new Vector2(22, 22), 0.45f, IconDisplayFillDirection, new Vector4(0.3f, 0.3f, 0.3f, 1), new Vector4(0.8f, 0.8f, 0.8f, 1), 3); 
                ImGui.SameLine();
                ImGui.Text("Fill Direction");
                ImGui.EndGroup();
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
                   IconDisplayFillDirection = (RemindMe.FillDirection) ((((int) IconDisplayFillDirection) + 1) % Enum.GetValues(typeof(RemindMe.FillDirection)).Length);
                }
            }
            

            if (ImGui.Checkbox($"Show Ability Icon##{this.Guid}", ref this.ShowActionIcon)) mainConfig.Save();
            if (this.ShowActionIcon) {
                switch (DisplayType) {
                    case 0: {
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(75);
                        var v = ReverseSideIcon ? 1 : 0;
                        var text = ReverseSideIcon ? "Right" : "Left";
                        ImGui.SliderInt($"###actionIconReverse##{Guid}", ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseSideIcon = !ReverseSideIcon;
                        break;
                    }
                    case 1: {
                        ImGui.SameLine();
                        var v = ReverseSideIcon ? 1 : 0;
                        var text = ReverseSideIcon ? "Top" : "Bottom";
                        ImGui.VSliderInt($"###actionIconReverse##{Guid}", new Vector2(60, 25), ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseSideIcon = !ReverseSideIcon;
                        break;
                    }
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###actionIconScale{this.Guid}", ref this.ActionIconScale, 0.1f, 1f, "Scale")) mainConfig.Save();

            }

            if ((DisplayType == 0 || DisplayType == 1) && ImGui.Checkbox($"Show Skill Name##{this.Guid}", ref this.ShowSkillName)) mainConfig.Save();

            if (DisplayType == 0 && this.ShowSkillName) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(75);
                var v = SkillNameRight ? 1 : 0;
                var text = SkillNameRight ? "Right" : "Left";
                ImGui.SliderInt("###skillNameAlign", ref v, 0, 1, text);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) SkillNameRight = !SkillNameRight;
            }

            if ((DisplayType == 0 || DisplayType == 1) && ShowSkillName && ImGui.Checkbox($"Show Status Effect Target Name##{this.Guid}", ref this.ShowStatusEffectTarget)) mainConfig.Save();
            
            if ((DisplayType == 0 || DisplayType == 1) && ShowSkillName && ShowStatusEffectTarget && ImGui.Checkbox($"Only show target name on status effects##{this.Guid}", ref this.StatusOnlyShowTargetName)) mainConfig.Save();
            
            if (ImGui.Checkbox($"Don't display missing permanent statuses##{this.Guid}", ref this.NoMissingStatus)) mainConfig.Save();
            if (ImGui.Checkbox($"Show Countdown##{this.Guid}", ref this.ShowCountdown)) mainConfig.Save();
            if (ShowCountdown) {

                switch (DisplayType) {
                    case 0: {
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(75);
                        var v = ReverseCountdownSide ? 0 : 1;
                        var text = ReverseCountdownSide ? "Left" : "Right";
                        ImGui.SliderInt($"###actionCountdownReverse##{Guid}", ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseCountdownSide = !ReverseCountdownSide;
                        break;
                    }
                    case 1: {
                        ImGui.SameLine();
                        var v = ReverseCountdownSide ? 0 : 1;
                        var text = ReverseCountdownSide ? "Bottom" : "Top";
                        ImGui.VSliderInt($"###countdownReverse##{Guid}", new Vector2(60, 25), ref v, 0, 1, text);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) ReverseCountdownSide = !ReverseCountdownSide;
                        break;
                    }
                }



                ImGui.Indent(20);
                if (ImGui.Checkbox($"Show Countup while ready##{this.Guid}", ref this.ShowCountdownReady)) mainConfig.Save();
                ImGui.Indent(-20);

            }
            if (ImGui.Checkbox($"Pulse when ready##{this.Guid}", ref this.PulseReady)) mainConfig.Save();

            if (this.PulseReady) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###pulseSpeed{this.Guid}", ref this.PulseSpeed, 0.5f, 2f, "Speed")) mainConfig.Save();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat($"###pulseIntensity{this.Guid}", ref this.PulseIntensity, 0.1f, 2f, "Intensity")) mainConfig.Save();
            }

            ImGui.Separator();
            if (ImGui.Checkbox($"###limitDisplay{this.Guid}", ref this.LimitDisplayTime)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.Text("Only show when below");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt($"seconds##limitSeconds{this.Guid}", ref LimitDisplayTimeSeconds)) mainConfig.Save();

            if (ImGui.Checkbox($"###limitDisplayReady{this.Guid}", ref this.LimitDisplayReadyTime)) mainConfig.Save();
            ImGui.SameLine();
            ImGui.Text("Don't show ready abilities after");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt($"seconds##limitReadySeconds{this.Guid}", ref LimitDisplayReadyTimeSeconds)) mainConfig.Save();

            ImGui.Separator();
            if (ImGui.InputInt($"Bar Height##{this.Guid}", ref this.RowSize, 1, 5)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }
            if (ImGui.InputInt($"Bar Spacing##{this.Guid}", ref this.BarSpacing, 1, 2)) {
                if (this.BarSpacing < 0) this.BarSpacing = 0;
                mainConfig.Save();
            }
            if (ImGui.InputFloat($"Text Scale##{this.Guid}", ref this.TextScale, 0.01f, 0.1f)) {
                if (this.RowSize < 8) this.RowSize = 8;
                mainConfig.Save();
            }
            
            if (ImGui.InputInt($"Update Interval##{this.Guid}", ref this.UpdateInterval, 1, 50)) {
                if (this.UpdateInterval < 1) this.UpdateInterval = 1;
                mainConfig.Save();
            }
            
            ImGui.Separator();

            if (tryCopy) {
                ImGui.Text("Copy with actions and statuses?");
                ImGui.SameLine();
                
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88888800);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99999900);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAAAAAA00);
                if (ImGui.Button($"Yes##copyWithActionsAndStatus{Guid}")) {
                    tryCopy = false;
                    copiedDisplay = MakeCopy(true);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Copy the display and all the configured reminders.");
                ImGui.SameLine();
                if (ImGui.Button($"No##copyWithActionsAndStatus{Guid}")) {
                    tryCopy = false;
                    copiedDisplay = MakeCopy(false);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Only copy the base display settings.");

                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                if (ImGui.Button($"Don't Copy##{Guid}")) tryCopy = false;
                
            } else if (tryDelete) {

                ImGui.Text("Delete this display?");
                ImGui.SameLine();
                if (ImGui.Button($"Don't Delete##{Guid}")) tryDelete = false;
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button($"Delete this display##{Guid}confirm")) deletedMonitor = Guid;
                ImGui.PopStyleColor(3);

            } else {
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99000099);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAA0000AA);
                if (ImGui.Button($"Delete this display##{Guid}")) {
                    tryDelete = true;
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(15 * ImGui.GetIO().FontGlobalScale, 1));
                ImGui.SameLine();
                
                ImGui.PushStyleColor(ImGuiCol.Button, 0x88888800);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x99999900);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xAAAAAA00);
                if (ImGui.Button($"Copy this display##{Guid}")) {
                    tryCopy = true;
                }
                ImGui.PopStyleColor(3);
            }

            

            ImGui.Separator();
            ImGui.Indent(-10);
        }

    }
}
