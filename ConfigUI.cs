using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using SharpDX;
using Num = System.Numerics;

namespace PixelPerfectPlus
{
    public class ConfigUI
    {
        private Config _configuration;
        private DalamudPluginInterface _pluginInterface;
        public bool IsVisible { get; set; }
        #region local vars
        private bool _enabled = true;
        private bool _targetEnabled = true;
        private bool _config;
        private bool _combat = true;
        private bool _showSelfcircle;
        private bool _instance;
        private bool _isColManaged;
        //private bool _DisplayPtMembers;
        private bool _drawNearbyTargets = false;
        private bool _showKofi = false;
        private bool _displayMobs;
        private bool _displayPlayers;
        private bool _showInteractiveSelector;
        private ObjectKind _interactiveSelectorObjKind;

        private const ImGuiWindowFlags DefaultPpFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                                                  ImGuiWindowFlags.AlwaysAutoResize |
                                                  ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground |
                                                  ImGuiWindowFlags.NoFocusOnAppearing;

        private bool _showFinetuning;

        private readonly Num.Vector4 _grey = new Num.Vector4(1f, 1f, 1f, 0.8f);
        private readonly Num.Vector4 _lightBlue = new Num.Vector4(0.403f, 1f, 0.886f, 1f);
        private readonly Num.Vector4 _lightOrange = new Num.Vector4(1f, 0.823f, 0.403f, 1f);
        
        private Num.Vector4 _col = new Num.Vector4(1f, 1f, 1f, 1f);
        private Num.Vector4 _col2;
        private Num.Vector4 _targetCol = new Num.Vector4(1f, 1f, 1f, 1f);
        private Num.Vector4 _colTargetRing;

        private Dictionary<ObjectKind, bool> _drawObjectKinds = new Dictionary<ObjectKind, bool>();
        private Dictionary<ObjectKind, List<string>> _objectKindFilters = new Dictionary<ObjectKind, List<string>>();
        private Dictionary<ObjectKind, List<int>> _objectKindIdFilter = new Dictionary<ObjectKind, List<int>>();
        private Dictionary<ObjectKind, bool[]> _objectKindFiltering = new Dictionary<ObjectKind, bool[]>();  

        private int _currentObjectKind = 0;
        private string[] _objectKinds = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        private string[] _currentFilters = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        private int[] _currentIdFilters = new int[Enum.GetValues(typeof (ObjectKind)).Length-1];
        
        private float _longestStrImGuiLen = 0;
        
        private bool _ring;
        private bool _showTargetRing;
        private Num.Vector4 _colRing = new Num.Vector4(0.4f, 0.4f, 0.4f, 0.5f);
        
        private float _radius = 10f;
        private float _selfHitboxSize = 2f;
        private float _targetHitboxSize = 2f;
        private int _segments = 100;
        private float _thickness = 10f;

        private int _fineTuningSource;
        private bool _currentFromPlayerCenter;
        private float _currentDistance;
        private bool _currentColorEnabled;
        private Num.Vector4 _currentDotColor = new Num.Vector4(0f, 0f, 0f, 1f);
        
        private List<float> _distances = new List<float>();
        private List<Num.Vector4> _dotColors = new List<Num.Vector4>();
        private List<bool> _colorEnabled = new List<bool>();
        private List<bool> _fromPlayerCenter;

        private float[] _aDistances;
        private Num.Vector4[] _aDotColors;
        private bool[] _aColorEnabled;
        private bool[] _aFromPlayerCenter;

        private bool _showRingConfig = false;

        private const string CurrentLabel = "Manage colour based on player to target distance";
        private const string DefaultDesc = "";

        private string _descIcon = FontAwesomeIcon.None.ToIconString();
        private string _currentDesc;
        
        private bool _combatOtherPlayers;
        private bool _instanceOtherPlayers;
        private bool _combatMobs;
        private bool _instanceMobs;
        #endregion
        
        private static bool CanObjKindBeFilteredById(ObjectKind obj)
        {
            return obj == ObjectKind.EventObj || obj == ObjectKind.BattleNpc;
        }
        
        private unsafe float? GetDistanceFromTarget(out bool reachable, bool fromPlayerCenter=true,  Actor manualtarget=null)
        {

            reachable = true;
            var t = _pluginInterface.ClientState.Targets.CurrentTarget;
            if (manualtarget != null) t = manualtarget;
            if (t == null) return null;            
            
            var distActorToActor = Vector2.Distance(new Vector2(t.Position.X, t.Position.Y),
                new Vector2(_pluginInterface.ClientState.LocalPlayer.Position.X,
                    _pluginInterface.ClientState.LocalPlayer.Position.Y));
            var elevationDiff = t.Position.Z - _pluginInterface.ClientState.LocalPlayer.Position.Z;
            reachable = elevationDiff > -5;
            var radius = t.HitboxRadius;
            if (radius < 0) // this is just in case dalamud says something weird but it shouldn't
            {
                radius = *(float*)(_pluginInterface.ClientState.Targets.CurrentTarget.Address + 0xC0);
            }
            
            if (fromPlayerCenter) return (distActorToActor - radius + (radius < 2 && radius >= 1 ? 1 : 0));
            var pRadius = *(float*)(_pluginInterface.ClientState.LocalPlayer.Address + 0xC0);
            return (distActorToActor - pRadius - radius);
        }
        
        private static void HoverDragFloat()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Set the hitbox size to whichever size you wish!\nBe aware that setting it to be any bigger or smaller than 2 will probably not give you an accurate representation of the actual hitbox but it might make it easier to see.");
            ImGui.EndTooltip();
        }
        private static void HoverCheckBoxHitboxes()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Toggle the outer ring on or off for the selected hitbox, making it generally easier to perceive and contrast more with the terrain or the enemy's model.\nThe outer ring's radius will by default match that of the hitbox.");
            ImGui.EndTooltip();
        }

        private static void HoverHitboxEnable(bool ranges=false)
        {
            ImGui.BeginTooltip();
            ImGui.Text("Whether the selected hitbox should be shown or not");
            if (ranges)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1f, 0f, 0f, 1f));
                ImGui.Text("Note that in order for the selected color to show,\nyou need to enable your self hitbox above.");
                ImGui.PopStyleColor();
            }
            ImGui.EndTooltip();
        }
        
        private void ShowFineTuning(ref bool currentFromPlayerCenter, float winX)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, _grey);
            var displayedStr = "Calculate distance from" +
                               (currentFromPlayerCenter ? "Player's Center" : "Player's Hitbox End")+
                               "to" +
                               "Target's Hitbox End";
            ImGui.SetCursorPosX((winX-ImGui.CalcTextSize(displayedStr).X)/2);
            ImGui.Text("Calculate distance from"); ImGui.SameLine();
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Text, currentFromPlayerCenter ? _lightBlue : _lightOrange);
            ImGui.Text(currentFromPlayerCenter ? "Player's Center" : "Player's Hitbox Edge");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.BeginTooltip();
                ImGui.Text(
                    currentFromPlayerCenter 
                        ? "Get an accurate distance reading for spells, weaponskills\n" +
                          "and abilities centered around yourself (e.g. Blizzard 2, Medica, etc.)\n"
                        :  "Get an accurate distance reading for spells, weaponskills\n" +
                           "and abilities that fire from your hitbox's end (e.g. Tomahawk, Cure, etc.)");
                ImGui.EndTooltip();
            }
            ImGui.PopStyleColor();
            if (ImGui.IsItemClicked())
            {
                currentFromPlayerCenter = !currentFromPlayerCenter;
            }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, _grey);
            ImGui.Text("to"); ImGui.SameLine();
            ImGui.Text(currentFromPlayerCenter ? "Target's Center" : "Target's Hitbox End");
            ImGui.PopStyleColor();
            ImGui.NewLine();
        }
        
        private void LoadConfig(bool forceReset=false)
        {
            if (forceReset) _configuration = new Config();
            _drawObjectKinds = _configuration.drawObjectKinds;
            _objectKindFilters = _configuration.ObjectKindFilters;
            _objectKindIdFilter = _configuration.ObjectKindIdFilter;
            _objectKindFiltering = _configuration.ObjectKindFiltering;
            _currentObjectKind = _configuration._currentObjectKind;
            _objectKinds = _configuration.ObjectKinds;
            _currentFilters = _configuration.currentFilters;
            _currentIdFilters = _configuration.currentIdFilters;
            _longestStrImGuiLen = _configuration.longestStrImGuiLen;
            
            _showKofi = _configuration.ShowKofi;
            _distances = _configuration.Distances ?? new List<float>();
            _dotColors = _configuration.DotColors ?? new List<Num.Vector4>();
            _colorEnabled = _configuration.ColorEnabled ?? new List<bool>();
            _fromPlayerCenter = _configuration.FromPlayerCenter ?? new List<bool>();

            _aDistances = _distances.ToArray();
            _aDotColors = _dotColors.ToArray();
            _aColorEnabled = _colorEnabled.ToArray();
            _aFromPlayerCenter = _fromPlayerCenter.ToArray();

            _isColManaged = _configuration.IsColManaged;
            _showRingConfig = _configuration.ShowRingConfig;
            _drawNearbyTargets = _configuration.DrawNearbyTargets;
            _displayMobs = _configuration.DisplayMobs;
            _displayPlayers = _configuration.DisplayPlayers;
            _combatOtherPlayers = _configuration.CombatOtherPlayers;
            _instanceOtherPlayers = _configuration.InstanceOtherPlayers;
            _combatMobs = _configuration.CombatMobs;
            _instanceMobs = _configuration.InstanceMobs;
            
            _ring = _configuration.Ring;
            _thickness = _configuration.Thickness;
            _colRing = _configuration.ColRing;
            _segments = _configuration.Segments;
            _radius = _configuration.Radius;
            
            _combat = _configuration.Combat;
            _instance = _configuration.Instance;
            
            // outer ring
            _col2 = _configuration.Col2;

            // self hitbox fields
            _selfHitboxSize = _configuration.SelfHitboxSize;
            _col = _configuration.Col;
            _enabled = _configuration.Enabled;
            _showSelfcircle = _configuration.Circle;
            
            // target hitbox fields
            _targetHitboxSize = _configuration.TargetHitboxSize;
            _targetCol = _configuration.TargetHitboxCol;
            _targetEnabled = _configuration.TargetEnabled;
            _showTargetRing = _configuration.TargetHitboxRing;
            _colTargetRing = _configuration.ColTargetRing;
        }
        
        private void SaveConfig()
        {
            _configuration.Combat = _combat; // limit display to when in combat
            _configuration.Instance = _instance; // limit display to instances
            _configuration.ShowRingConfig = _showRingConfig; // whether to show the configuration for the additional player ring or not
            _configuration.IsColManaged = _isColManaged; // whether or not the plugin manages the selfhitbox color
            _configuration.Distances = _distances; // distances component of the custom ranges
            _configuration.DotColors = _dotColors; // color component of the custom ranges
            _configuration.ColorEnabled = _colorEnabled; // individual toggles for custom ranges
            _configuration.FromPlayerCenter = _fromPlayerCenter; // whether to calculate distance from the player center for a given range
            _configuration.DrawNearbyTargets = _drawNearbyTargets;
            _configuration.ShowKofi = _showKofi;
            
            _configuration.drawObjectKinds = _drawObjectKinds;
            _configuration.ObjectKindFilters = _objectKindFilters;
            _configuration.ObjectKindIdFilter = _objectKindIdFilter; 
            _configuration.ObjectKindFiltering = _objectKindFiltering; 
            _configuration._currentObjectKind = _currentObjectKind;
            _configuration.ObjectKinds = _objectKinds;
            _configuration.currentFilters = _currentFilters;
            _configuration.currentIdFilters = _currentIdFilters;
            _configuration.longestStrImGuiLen = _longestStrImGuiLen;
            
            _configuration.DisplayMobs = _displayMobs;
            _configuration.DisplayPlayers = _displayPlayers;
            _configuration.CombatOtherPlayers = _combatOtherPlayers;
            _configuration.InstanceOtherPlayers = _instanceOtherPlayers;
            _configuration.CombatMobs = _combatMobs;
            _configuration.InstanceMobs = _instanceMobs;
            
            _configuration.Enabled = _enabled; // if selfhitbox is enabled
            _configuration.SelfHitboxSize = _selfHitboxSize; // size of selfhitbox
            _configuration.Col = _col; // color of selfhitbox
            _configuration.Circle = _showSelfcircle; // if selfhitbox's outer ring is showing
            _configuration.Col2 = _col2; // color of selfhitbox's outer ring
            _configuration.ColTargetRing = _colTargetRing; // color of target's outer ring
            
            _configuration.TargetEnabled = _targetEnabled; // if targethitbox is enabled
            _configuration.TargetHitboxSize = _targetHitboxSize; // targethitbox size
            _configuration.TargetHitboxCol = _targetCol; // targethitbox color
            _configuration.TargetHitboxRing = _showTargetRing; // targethitbox ring showing bool

            _configuration.ColRing = _colRing; // color of the large ring (not hitbox ring)
            _configuration.Thickness = _thickness; // thickness of large ring
            _configuration.Segments = _segments; // segments of large ring
            _configuration.Ring = _ring; // display the ring
            _configuration.Radius = _radius; // radius of ring

            _pluginInterface.SavePluginConfig(_configuration);
        }

        public ConfigUI(DalamudPluginInterface pI, Config config)
        {
            this._pluginInterface = pI;
            this._configuration = config;
        }
        public void Draw()
        {
            if (!IsVisible) return;
            
            #region Interactive Selector
            if (_showInteractiveSelector)
            {
                foreach (var actor in _pluginInterface.ClientState.Actors)
                {
                    if (actor.ActorId == _pluginInterface.ClientState.LocalPlayer.ActorId || actor.ObjectKind != _interactiveSelectorObjKind || string.IsNullOrEmpty(actor.Name)) continue;
                    if (!_objectKindFiltering[actor.ObjectKind][0]) continue;
                    
                    if (!_pluginInterface.Framework.Gui.WorldToScreen(
                        new SharpDX.Vector3(actor.Position.X, actor.Position.Z+2, actor.Position.Y), out var pos))
                        continue;
                    ImGui.SetNextWindowPos(new Num.Vector2(pos.X-(int)(ImGui.CalcTextSize(actor.Name).X)+20, pos.Y));
                    ImGui.Begin($"##selector{actor.Address}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                                                              ImGuiWindowFlags.AlwaysAutoResize |
                                                              ImGuiWindowFlags.NoFocusOnAppearing);
                    ImGui.Text(actor.Name); 
                    ImGui.SameLine();
                    var id = 0;
                    var IdInWhitelist = false;
                    var NameInWhitelist = _objectKindFilters[actor.ObjectKind].Contains(actor.Name);
                    var canBeFiltered = CanObjKindBeFilteredById(actor.ObjectKind);
                    var label = _objectKinds[(int) actor.ObjectKind-1]; 
                    if (canBeFiltered)
                    {
                        switch (actor)
                        {
                            case EventObj eObj:
                                id = eObj.DataId;
                                break;
                            case BattleNpc bNpc:
                                id = bNpc.DataId;
                                break;
                        }

                        IdInWhitelist = _objectKindIdFilter[actor.ObjectKind].Contains(id);
                    }

                    var setId = canBeFiltered &&
                                _pluginInterface.ClientState.KeyState[0x10];

                    ImGui.PushFont(UiBuilder.IconFont);
                    var icon = setId
                        ? FontAwesomeIcon.SortNumericUp.ToIconString()
                        : FontAwesomeIcon.SortAlphaUp.ToIconString();

                    if ( NameInWhitelist && !setId || IdInWhitelist && _pluginInterface.ClientState.KeyState[0x10])
                        icon = FontAwesomeIcon.Times.ToIconString();
                    
                    if (ImGui.Button($"{icon}##addToWhitelist{actor.Address}"))
                    {
                        if (setId)
                        {
                            if (IdInWhitelist) {
                                _objectKindIdFilter[actor.ObjectKind].Remove(id);
                                break;
                            }
                            _objectKindIdFilter[actor.ObjectKind].Add(id);
                            break;
                        }
                        if (NameInWhitelist)
                        {
                            _objectKindFilters[actor.ObjectKind].Remove(actor.Name);
                            break;
                        }
                        _objectKindFilters[actor.ObjectKind].Add(actor.Name);
                        break;
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        if (setId)
                            ImGui.Text(IdInWhitelist
                                ? "Click to remove ID from whitelist."
                                : "Click to add ID to whitelist.");
                        else
                            ImGui.Text(NameInWhitelist
                                ? "Click to remove Name from whitelist."
                                : "Click to add Name to whitelist.");
                        if (!_pluginInterface.ClientState.KeyState[0x10] && canBeFiltered) ImGui.TextDisabled("Hold Shift to modify ID whitelist instead.");
                        ImGui.EndTooltip();
                    }
                    ImGui.End();
                }
            }
            #endregion 
            
            var winx = 400;
            var winy = 700;
            var winSize = new Num.Vector2(winx, winy);
            ImGui.SetNextWindowSize(winSize, ImGuiCond.FirstUseEver);
            var wtitle = "Pixel Perfect Plus Config";
            ImGui.Begin(wtitle, ref _config,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.AlwaysUseWindowPadding);
            var cursorXoffset = 275;
            var cursorX = ImGui.GetCursorPosX();
            var xpadding = 25f;
            var cursorY = ImGui.GetCursorPosY();
            var ypadding = 20f;
            ImGui.SetCursorPosX(winx+xpadding);
            ImGui.SetCursorPosY(ypadding);
            ImGui.SetCursorPosX(cursorX+xpadding);
            ImGui.Text("Hitboxes");

            // toggles and colors
            
            //self hitbox
            ImGui.SetCursorPosX(cursorX+xpadding);
            ImGui.Checkbox("##SelfHitboxEnabled", ref _enabled);
            if (ImGui.IsItemHovered()) HoverHitboxEnable();
            if (_enabled)
            {
                ImGui.SameLine();
                ImGui.ColorEdit4("##SelfHitboxCol", ref _col, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
                ImGui.SameLine();
                ImGui.Checkbox("##SelfOuterRingShow", ref _showSelfcircle);
                if (ImGui.IsItemHovered()) HoverCheckBoxHitboxes();
                if (_showSelfcircle)
                {
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##SelfOuterRingColor", ref _col2, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(25f);
                if (ImGui.DragFloat("##SelfCircleSize", ref _selfHitboxSize, 0.1f, 0.5f, 6.5f, "%.4g"))
                {
                    if (_selfHitboxSize > 6.5f) _selfHitboxSize = 6.5f;
                    if (_selfHitboxSize < 0.5f) _selfHitboxSize = 0.5f;
                };
                if (ImGui.IsItemHovered()) HoverDragFloat();
            }
            ImGui.SameLine(); ImGui.Text("Self");
            
            // target hitbox
            ImGui.SetCursorPosX(cursorX+xpadding);
            ImGui.Checkbox("##TargetHitboxEnabled", ref _targetEnabled);
            if (ImGui.IsItemHovered()) HoverHitboxEnable();
            if (_targetEnabled)
            {
                ImGui.SameLine();
                ImGui.ColorEdit4("##TargetHitboxCol", ref _targetCol, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
                ImGui.SameLine();
                ImGui.Checkbox("##TargetOuterRingShow", ref _showTargetRing);
                if (ImGui.IsItemHovered()) HoverCheckBoxHitboxes();
                if (_showTargetRing)
                {
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##TargetOuterRingColor", ref _colTargetRing, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
                }
                ImGui.SameLine();

                ImGui.SetNextItemWidth(25f);
                if (ImGui.DragFloat("##TargetCircleSize", ref _targetHitboxSize, 0.1f, 0.5f, 6.5f, "%.4g"))
                {
                    if (_targetHitboxSize > 6.5f) _targetHitboxSize = 6.5f;
                    if (_targetHitboxSize < 0.5f) _targetHitboxSize = 0.5f;
                };
                if (ImGui.IsItemHovered()) HoverDragFloat();
            }
            ImGui.SameLine(); ImGui.Text("Target");
            
            ImGui.SetCursorPosX(cursorX+xpadding);
            ImGui.Checkbox("##otherTargets", ref _drawNearbyTargets);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Only players matching at least the largest registered range will be displayed.\nFor example, if have a range set for 30 yalms, all actors within 30 yalms will \nhave their hitboxes drawn with the set color.");
                ImGui.EndTooltip();
            }
            ImGui.SameLine();   
            
            // other targets
            if (_drawNearbyTargets)
            {
                if (ImGui.TreeNodeEx("##nearbyTargets", ImGuiTreeNodeFlags.None, "Other Targets"))
                {
                    ImGui.Indent(60);
                    ImGui.Text("Draw hitbox of "); ImGui.SameLine();
                    ImGui.SetNextItemWidth(_longestStrImGuiLen+40);
                    ImGui.Combo("##addObjectKind", ref _currentObjectKind, _objectKinds, _objectKinds.Length);
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    var alreadyRegistered = _drawObjectKinds.ContainsKey((ObjectKind) _currentObjectKind + 1);
                    if (ImGui.Button( alreadyRegistered ? 
                        FontAwesomeIcon.Times.ToIconString() : FontAwesomeIcon.Plus.ToIconString()
                        ) && !alreadyRegistered)
                    {
                        _drawObjectKinds.Add((ObjectKind)_currentObjectKind+1, true);
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered() && !alreadyRegistered)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"...Add {_objectKinds[(int) _currentObjectKind]} to the list of drawn hitboxes.");
                        ImGui.EndTooltip();
                    }
                    ImGui.Indent();
                    
                    foreach (var objKind in _drawObjectKinds)
                    {
                        var label = _objectKinds[(int) objKind.Key-1]; 
                        if (ImGui.Button($"  -  ##remove{objKind.Key.ToString()}"))
                        {
                            _drawObjectKinds.Remove(objKind.Key);
                            if (objKind.Key == _interactiveSelectorObjKind) _showInteractiveSelector = false;
                            break;
                        };
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Remove {label} from the list of drawn hitboxes.");
                            ImGui.EndTooltip();
                        }
                        ImGui.SameLine();
                        
                        ImGui.Checkbox($"##toggleFiltering{label}", ref _objectKindFiltering[objKind.Key][0]);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Enable whitelisting for {label} hitboxes.");
                            ImGui.EndTooltip();
                        }
                        ImGui.SameLine();
                        
                        var cx = ImGui.GetCursorPosX();
                        if (_objectKindFiltering[objKind.Key][0])
                        {
                            if (ImGui.TreeNode($"{label}##{objKind.Key.ToString()}"))
                            {
                                ImGui.Indent();
                                if (CanObjKindBeFilteredById(objKind.Key))
                                    ImGui.Checkbox($"Filter by DataID instead of Name##filterById{label}",
                                        ref _objectKindFiltering[objKind.Key][1]);
                                if (_objectKindFiltering[objKind.Key][1])
                                {
                                    ImGui.Text($"Only show {label}s matching the IDs:");
                                    ImGui.SetNextItemWidth(150f);
                                    ImGui.InputInt($"##filter{label}", ref _currentIdFilters[_currentObjectKind]);
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()) && !_objectKindIdFilter[objKind.Key].Contains(_currentIdFilters[_currentObjectKind]))
                                    {
                                        _objectKindIdFilter[objKind.Key].Add(_currentIdFilters[_currentObjectKind]);
                                        _currentIdFilters[_currentObjectKind] = 0;
                                    }
                                    ImGui.PopFont();
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text("Add ID to whitelist.");
                                        ImGui.EndTooltip();
                                    }
                                    foreach (var filter in _objectKindIdFilter[objKind.Key])
                                    {
                                        if (ImGui.Button($"x##{filter}"))
                                        {
                                            _objectKindIdFilter[objKind.Key].Remove(filter);
                                            break;
                                        }; ImGui.SameLine();
                                        ImGui.Text("ID "+filter);
                                    }
                                }
                                else
                                {
                                    ImGui.Text($"Only show {label}s matching the names:");
                                    ImGui.SetNextItemWidth(150f);
                                    var old = _currentFilters[_currentObjectKind];
                                    if (ImGui.InputText($"##filter{objKind.GetHashCode()}",
                                        ref _currentFilters[_currentObjectKind], 31))
                                    {
                                        if (_currentFilters[_currentObjectKind].Split(' ').Length > 2)
                                            _currentFilters[_currentObjectKind] = old;
                                    };
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()) && !_objectKindFilters[objKind.Key].Contains(_currentFilters[_currentObjectKind]))
                                    {
                                        _objectKindFilters[objKind.Key].Add(_currentFilters[_currentObjectKind]);
                                        _currentFilters[_currentObjectKind] = "";
                                    }
                                    ImGui.PopFont();
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text("Add name to whitelist.");
                                        ImGui.EndTooltip();
                                    }
                                    var hideSelector = objKind.Key == _interactiveSelectorObjKind &&
                                                       _showInteractiveSelector;
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    if (ImGui.Button((!hideSelector 
                                        ? FontAwesomeIcon.Eye.ToIconString() 
                                        : FontAwesomeIcon.EyeSlash.ToIconString())
                                                     +"##showSelector"))
                                    {
                                        _interactiveSelectorObjKind = objKind.Key;
                                        _showInteractiveSelector = !hideSelector;
                                    }
                                    ImGui.PopFont();
                                    
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text( !hideSelector 
                                            ? $"Show interactive selector for {label}s."
                                            : "Hide interactive selector.");
                                        ImGui.EndTooltip();
                                    }
                                    ImGui.Indent();
                                    foreach (var filter in _objectKindFilters[objKind.Key])
                                    {
                                        if (ImGui.Button($"  x  ##{filter}"))
                                        {
                                            _objectKindFilters[objKind.Key].Remove(filter);
                                            break;
                                        }; ImGui.SameLine();
                                        ImGui.Text(filter);
                                    }
                                    ImGui.Unindent();
                                }
                                ImGui.Unindent();
                                ImGui.NewLine();
                                ImGui.TreePop();
                            }
                        }
                        else ImGui.Text("          "+label);
                    }

                    ImGui.Unindent();
                    ImGui.Unindent(60);
                    ImGui.TreePop();
                }
            }
            else
            {
                var cx = ImGui.GetCursorPosX();
                ImGui.SetCursorPosX(cx+25);
                ImGui.Text("Other Targets");
                ImGui.SetCursorPosX(cx);
            }
            
            
            ImGui.NewLine();
            var bottomHitboxFields = ImGui.GetCursorPosY();
            ImGui.SetCursorPosX(cursorX+xpadding);

            ImGui.SetCursorPosY(ypadding);
            ImGui.SetCursorPosX(cursorXoffset);
            ImGui.Text("Only display hitboxes");
            ImGui.SetCursorPosX(cursorXoffset);
            ImGui.Checkbox("... in Combat##combat", ref _combat);
            ImGui.SetCursorPosX(cursorXoffset);
            ImGui.Checkbox("... in Instance##instance", ref _instance);
            ImGui.SetCursorPosY(bottomHitboxFields);

            ImGui.NewLine();
            ImGui.SetCursorPosX(cursorX+xpadding); 
            ImGui.Checkbox("##colManager", ref _isColManaged);
            ImGui.SameLine();
            
            // color ranges manager
            if (_isColManaged)
            {
                if (ImGui.TreeNode("##colManagerTree", CurrentLabel))
                {
                    var cy = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(cy+20);
                    ImGui.Text("Distance from target");
                    ImGui.SameLine();
                    ImGui.Text("(?)");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(300f);
                        ImGui.TextWrapped("The distance calculation does not take into\naccount the elevation difference between\nyou and your target, although if you are\n5 or more yalms above your target your\nself-centered moves will not hit your target.");
                        ImGui.EndTooltip();
                    } ImGui.SameLine();
                    var dist = (float) Math.Round(GetDistanceFromTarget(out bool Reachable, _currentFromPlayerCenter) ?? -999f, 2);
                    var btnlabel = "No target";
                    ImGui.SameLine();
                    if (dist != -999f)
                    {
                        btnlabel = "" + dist;
                        ImGui.Button(btnlabel);
                        if (Reachable)
                        {
                            if (ImGui.IsItemClicked())
                            {
                                _currentDistance = (float)Math.Round(dist, 1);
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                                ImGui.BeginTooltip();
                                ImGui.Text("Click to set current entry's distance for\nactivation to current distance to target.");
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.Text("Elevation difference with target   "+
                                   (_pluginInterface.ClientState.Targets.CurrentTarget.Position.Z - _pluginInterface.ClientState.LocalPlayer.Position.Z)
                                   + (Reachable?"":"\n>> Target not within height reach of self-centered moves."));
                    }
                    else
                    {
                        ImGui.Text(btnlabel);
                    }
                    
                    

                    ImGui.SetCursorPosY(cy+100);
                    
                    var cx = ImGui.GetCursorPosX();
                    var offset = 30;
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString());
                    ImGui.PopFont();
                    if (ImGui.IsItemClicked()) // add new color
                    {
                        if (!_distances.Contains(_currentDistance) || (_distances.Contains(_currentDistance)&&_fromPlayerCenter[_distances.IndexOf(_currentDistance)]!=_currentFromPlayerCenter)
                        ) // make sure distance isn't already registered
                        {
                            //populate
                            _distances.Add(_currentDistance);
                            _dotColors.Add(_currentDotColor);
                            _colorEnabled.Add(_currentColorEnabled);
                            _fromPlayerCenter.Add(_currentFromPlayerCenter);

                            _aDistances = _distances.ToArray();
                            _aDotColors = _dotColors.ToArray();
                            _aColorEnabled = _colorEnabled.ToArray();
                            _aFromPlayerCenter = _fromPlayerCenter.ToArray();

                            //reset
                            _currentDistance = 0f;
                            _currentDotColor = new Num.Vector4(0f, 0f, 0f, 1f);
                            _currentColorEnabled = false;
                            _currentDesc = DefaultDesc;
                            _descIcon = FontAwesomeIcon.None.ToIconString();

                        }
                        else
                        {
                            _currentDesc =
                                $"Error: You already have registered a color range for the value: {_currentDistance} yalms";
                            _descIcon = FontAwesomeIcon.ExclamationTriangle.ToIconString();
                        }
                    }
                    
                    ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset);
                    ImGui.Checkbox("##currentColorEnabled", ref _currentColorEnabled);
                    
                    ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset*2);
                    ImGui.ColorEdit4($"Color to be displayed when you stand\ncloser than "+_currentDistance+" yalms to your target##currentDotColor", ref _currentDotColor,
                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview);
                    
                    ImGui.SameLine(); ImGui.SetCursorPosX((int)(cx+offset*3));
                    ImGui.Text("Show when distance is"); ImGui.SameLine();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.BeginTooltip();
                        ImGui.Text("Click to finetune how distance is calculated for the given range.");
                        ImGui.EndTooltip();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        if (_fineTuningSource == -1)
                        {
                            _showFinetuning = !_showFinetuning;
                        }
                        _fineTuningSource = -1;
                    }
                    ImGui.PushStyleColor(ImGuiCol.Text, _currentFromPlayerCenter ? _lightBlue : _lightOrange);
                    ImGui.Text("<="); 
                    if (ImGui.IsItemClicked())
                    {
                        if (_distances.Any(d=>d==_currentDistance))
                        {
                            if (_fromPlayerCenter[_distances.IndexOf(_currentDistance)] == _currentFromPlayerCenter)
                                _currentFromPlayerCenter = !_currentFromPlayerCenter;
                        }
                        else
                        {
                            _currentFromPlayerCenter = !_currentFromPlayerCenter;
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.BeginTooltip();
                        ImGui.Text("For this range distance will be\ncalculated from your Hitbox's "+(_currentFromPlayerCenter?"Center":"Edge")+"\nClick to quickly toggle distance calculation mode.");
                        ImGui.EndTooltip();
                    }
                    ImGui.SameLine();
                    ImGui.PopStyleColor();


                    ImGui.SameLine(); ImGui.SetCursorPosX((int)(cx+offset*8.2));
                    ImGui.SetNextItemWidth(90);
                    if (ImGui.InputFloat($" yalms##currentYalms", ref _currentDistance, 1.0f, 1.0f, "%.3g"))
                    {
                        if (!_pluginInterface.ClientState.KeyState[0x10])
                        {
                            if (_currentDistance > 55) _currentDistance = 55;
                            if (_currentDistance < -1) _currentDistance = -1;
                        }
                        
                    };
                    if (_showFinetuning && _fineTuningSource == -1)
                    {
                        ShowFineTuning(ref _currentFromPlayerCenter, winx);
                    }
                    ImGui.SetCursorPosX(cx);
                    
                    
                    
                    ImGui.NewLine();
                    
                    for (var i = 0; i < _distances.Count; i++)
                    {
                        
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Button(FontAwesomeIcon.Times.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemClicked() && i>=0)
                        {
                            _distances.RemoveAt(i);
                            _dotColors.RemoveAt(i);
                            _colorEnabled.RemoveAt(i);
                            _fromPlayerCenter.RemoveAt(i);
                            _aDistances = _distances.ToArray();
                            _aDotColors = _dotColors.ToArray();
                            _aColorEnabled = _colorEnabled.ToArray();
                            _aFromPlayerCenter = _fromPlayerCenter.ToArray();
                            continue;
                        }

                        
                            
                        ImGui.SameLine(); // enabled
                        ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset);
                        if (ImGui.Checkbox($"##{i}Enabled", ref _aColorEnabled[i]))
                        {
                            _colorEnabled[i] = _aColorEnabled[i];
                        }

                        ImGui.SameLine(); // color
                        ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset*2);
                        if (ImGui.ColorEdit4($"Color to be displayed when you stand\ncloser than "+_aDistances[i]+$" yalms to your target##{i}Color{_fromPlayerCenter[i]}", ref _aDotColors[i],
                            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                        {
                            _dotColors[i] = _aDotColors[i];
                        }

                        ImGui.SameLine(); ImGui.SetCursorPosX((int)(cx+offset*3));
                        
                        ImGui.Text("Show when distance is"); ImGui.SameLine();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                            ImGui.BeginTooltip();
                            ImGui.Text("Click to finetune how distance is calculated for the given range.");
                            ImGui.EndTooltip();
                        }
                        if (ImGui.IsItemClicked())
                        {
                            if (_fineTuningSource == i)
                            {
                                _showFinetuning = !_showFinetuning;
                            }
                            _fineTuningSource = i;
                        }
                        ImGui.PushStyleColor(ImGuiCol.Text, _fromPlayerCenter[i] ? _lightBlue : _lightOrange);
                        ImGui.Text("<="); 
                        if (ImGui.IsItemClicked())
                        {
                            /*var duplicate = _Distances.Count(d => d == _Distances[i]);
                            if (duplicate == 0) */_fromPlayerCenter[i] = !_fromPlayerCenter[i];
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                            ImGui.BeginTooltip();
                            ImGui.Text("For this range distance will be\ncalculated from your Hitbox's "+(_fromPlayerCenter[i]?"Center":"Edge")+"\nClick to quickly toggle distance calculation mode.");
                            ImGui.EndTooltip();
                        }
                        ImGui.SameLine();
                        
                        
                        ImGui.PopStyleColor();
                        
                        ImGui.SameLine(); // distance
                        ImGui.SetCursorPosX((int)(cx+offset*8.2));
                        ImGui.SetNextItemWidth(90);
                        if (ImGui.InputFloat($" yalms##{i}Distance", ref _aDistances[i], 1.0f, 1.0f, "%.3g"))
                        {
                            if (!_pluginInterface.ClientState.KeyState[0x10])
                            {
                                if (_aDistances[i] > 55) _aDistances[i] = 55;
                                if (_aDistances[i] < -1) _aDistances[i] = -1;
                            }
                            _distances[i] = _aDistances[i];
                            
                        }
                        if (_showFinetuning && _fineTuningSource == i)
                        {
                            ShowFineTuning(ref _aFromPlayerCenter[i], winx);
                            _fromPlayerCenter[i] = _aFromPlayerCenter[i];
                        }
                    }

                    ImGui.TreePop();
                }

                if (_currentDesc != "")
                {
                    ImGui.NewLine();
                    ImGui.SetCursorPosX(cursorX+(int)(xpadding*2.3));
                    ImGui.PushStyleColor(ImGuiCol.Text, 0x90FFFFFF);
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Text(_descIcon);
                    ImGui.PopFont(); ImGui.SameLine();
                    ImGui.TextWrapped(_currentDesc);
                    ImGui.PopStyleColor();
                    ImGui.PopTextWrapPos();
                    ImGui.SetCursorPosX(cursorX+xpadding);
                }
                ImGui.NewLine();
            }
            else
            {
                ImGui.Text(CurrentLabel);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(400f);
                    ImGui.TextWrapped(
                        "Allows you to change the color of your hitbox\nbased on the distance between you and your target.\nYou can create \"ranges\" which all will automatically \ntake priority over one another.\n\nIf you are using a color on your Self Hitbox along with this, \nthe color specified in the Self Hitbox section will show up\n if no ranges are matched.");
                    ImGui.EndTooltip();
                }
            }
            
            
            //additional ring config
            if (_showRingConfig)
            {
                ImGui.Separator();
                ImGui.NewLine();
                ImGui.SetCursorPosX(cursorX+xpadding);
                ImGui.Checkbox("Ring", ref _ring);
                ImGui.SetCursorPosX(cursorX+xpadding);
                ImGui.DragFloat("Yalms", ref _radius);
                ImGui.SetCursorPosX(cursorX+xpadding);
                ImGui.DragFloat("Thickness", ref _thickness);
                ImGui.SetCursorPosX(cursorX+xpadding);
                ImGui.DragInt("Smoothness", ref _segments);
                ImGui.SetCursorPosX(cursorX+xpadding);
                ImGui.ColorEdit4("Ring Colour", ref _colRing, ImGuiColorEditFlags.NoInputs);
                ImGui.NewLine();
            }

            #region bottom row
            ImGui.NewLine();
            ImGui.SetCursorPosX(cursorX+xpadding);
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Save.ToIconString()))
            {
                SaveConfig();
            }

            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(400f);
                ImGui.TextWrapped("Save your changes.");
                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Times.ToIconString()))
            {
                _config = false;
            }

            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(400f);
                ImGui.TextWrapped("Close out of the config menu.");
                ImGui.EndTooltip();
            } ImGui.SameLine();
            
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(!(_pluginInterface.ClientState.KeyState[0x10]&&_pluginInterface.ClientState.KeyState[0x11]) ? FontAwesomeIcon.Sync.ToIconString() : FontAwesomeIcon.Trash.ToIconString())) 
                LoadConfig(_pluginInterface.ClientState.KeyState[0x10]);
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(400f);
                ImGui.TextWrapped(!_pluginInterface.ClientState.KeyState[0x10] ? "Reload saved config.":"Reset config.");
                ImGui.EndTooltip();
            } ImGui.SameLine();

            ImGui.Checkbox("##enableRingConfig", ref _showRingConfig);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(400f);
                ImGui.TextWrapped("Show the configuration menu for the additional player ring.");
                ImGui.EndTooltip();
            } ImGui.SameLine();

            if (ImGui.Checkbox("##showKofi", ref _showKofi))
            {
                _configuration.ShowKofi = _showKofi;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(400f);
                ImGui.TextWrapped("Show Ko-Fi donation button.");
                ImGui.EndTooltip();
            }

            if (_showKofi)
            {
                var kofi = "Buy Haplo a Hot Chocolate";
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
                ImGui.SetCursorPosX(winx-(ImGui.CalcTextSize(kofi).X+xpadding/2));
                if (ImGui.Button(kofi))
                    System.Diagnostics.Process.Start("https://ko-fi.com/haplo");
                ImGui.PopStyleColor(3);
            }
            #endregion
            
            cursorY = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(cursorY+ypadding);
            ImGui.End();
        }
    }
}