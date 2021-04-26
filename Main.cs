using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Num = System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Interface;
using SharpDX;

namespace PixelPerfectPlus
{
    public class PixelPerfectPlus : IDalamudPlugin
    {
        public string Name => "Pixel Perfect Plus";
        private DalamudPluginInterface _pluginInterface;
        //private Config _configuration;
        private Config _configuration;
        //private List<Fields> _configs;
        private bool _enabled = true;
        private bool _targetEnabled = true;
        private bool _config;
        private bool _combat = true;
        private bool _showSelfcircle;
        private bool _instance;
        private bool _IsColManaged;
        //private bool _DisplayPtMembers;
        private bool _drawNearbyTargets = false;
        private bool _showKofi = true;
        private bool _DisplayMobs;
        private bool _DisplayPlayers;
        //private int _currentConfig;

        private bool _showFinetuning;

        private Num.Vector4 Grey = new Num.Vector4(1f, 1f, 1f, 0.8f);
        public Num.Vector4 LightBlue = new Num.Vector4(0.403f, 1f, 0.886f, 1f);
        public Num.Vector4 LightOrange = new Num.Vector4(1f, 0.823f, 0.403f, 1f);
        public Num.Vector4 Red = new Num.Vector4(1f, 0f, 0f, 1f);
        public Num.Vector4 Green = new Num.Vector4(0f, 1f, 0f, 1f);
        private Num.Vector4 _col = new Num.Vector4(1f, 1f, 1f, 1f);
        private Num.Vector4 _col2;
        private Num.Vector4 _targetCol = new Num.Vector4(1f, 1f, 1f, 1f);
        private Num.Vector4 _colTargetRing;
        
        private bool _ring;
        private bool _showTargetRing;
        private Num.Vector4 _colRing = new Num.Vector4(0.4f, 0.4f, 0.4f, 0.5f);
        
        private float _radius = 10f;
        private float _selfHitboxSize = 2f;
        private float _targetHitboxSize = 2f;
        private int _segments = 100;
        private float _thickness = 10f;

        private int _fineTuningSource;
        private bool _CurrentFromPlayerCenter = false;
        private float _currentDistance = 0;
        private bool _currentColorEnabled = false;
        private Num.Vector4 _currentDotColor = new Num.Vector4(0f, 0f, 0f, 1f);
        
        private List<float> _Distances = new List<float>();
        private List<Num.Vector4> _DotColors = new List<Num.Vector4>();
        private List<bool> _ColorEnabled = new List<bool>();
        private List<bool> _FromPlayerCenter;
        //private List<bool> _PtMembersToDisplay;

        private readonly List<ObjectKind> _IgnoreKinds = new List<ObjectKind>()
        {
            ObjectKind.Aetheryte, ObjectKind.Area, ObjectKind.Companion, ObjectKind.Cutscene,
            ObjectKind.Housing, ObjectKind.None, ObjectKind.Retainer, ObjectKind.Treasure, ObjectKind.CardStand,
            ObjectKind.EventNpc, ObjectKind.EventObj, ObjectKind.GatheringPoint, ObjectKind.MountType
        };

        private float[] aDistances;
        private Num.Vector4[] aDotColors;
        private bool[] aColorEnabled;
        private bool[] aFromPlayerCenter;
        //private bool[] aPtMembersToDisplay;
        
        private bool _showRingConfig = false;

        private string _currentLabel =
            "Manage colour based on player to target distance";
        
        private string _defaultDesc =
            "";

        private string _descIcon = FontAwesomeIcon.None.ToIconString();
        private string _currentDesc;
        
        private bool _combatOtherPlayers;
        private bool _instanceOtherPlayers;
        private bool _combatMobs;
        private bool _instanceMobs;


        public void Initialize(DalamudPluginInterface pI)
        {
            _pluginInterface = pI;
            //_pluginConfig = _pluginInterface.GetPluginConfig() as Config ?? new Config();
            //_configs = _pluginConfig.Configs;
            //_currentConfig = _pluginConfig.CurrentConfigIndex;
            LoadConfig();
            _currentDesc = _defaultDesc;
            
            
            _pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            _pluginInterface.CommandManager.AddHandler("/ppp", new CommandInfo(Command)
            {
                HelpMessage = "Pixel Perfect Plus config."
            });
        }

        private void LoadConfig( bool forceReset=false)
        {
            /*if (forceReset)
            {
                _pluginConfig.Configs = new List<Fields>() {_configuration};
                _currentConfig = 0;
            }*/
            _configuration =  _pluginInterface.GetPluginConfig() as Config ?? new Config();
            if (forceReset) _configuration = new Config();
            _Distances = _configuration.Distances ?? new List<float>();
            _DotColors = _configuration.DotColors ?? new List<Num.Vector4>();
            _ColorEnabled = _configuration.ColorEnabled ?? new List<bool>();
            _FromPlayerCenter = _configuration.FromPlayerCenter ?? new List<bool>();
            //_PtMembersToDisplay = _configuration.PtMembersToDisplay ?? new List<bool>(new bool[7]);
            
            aDistances = _Distances.ToArray();
            aDotColors = _DotColors.ToArray();
            aColorEnabled = _ColorEnabled.ToArray();
            aFromPlayerCenter = _FromPlayerCenter.ToArray();
            //aPtMembersToDisplay = _PtMembersToDisplay.ToArray();
            
            _IsColManaged = _configuration.IsColManaged;
            _showRingConfig = _configuration.ShowRingConfig;
            _drawNearbyTargets = _configuration.DrawNearbyTargets;
            //_DisplayPtMembers = _configuration.DisplayPtMembers;
            _DisplayMobs = _configuration.DisplayMobs;
            _DisplayPlayers = _configuration.DisplayPlayers;
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
        
        public void Dispose()
        {
            _pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            _pluginInterface.CommandManager.RemoveHandler("/ppp");
        }
        
        private int FindRangeForDistance(float? distance)
        {
            float smallest = 999;
            int smallestIdx = -1;
            if (distance == null) return smallestIdx;
            for (int i = 0; i < _Distances.Count; i++)
            {
                if (_Distances[i] != -999 && _ColorEnabled[i])
                {
                    if (_Distances[i] < smallest && _Distances[i] > distance)
                    {
                        smallest = _Distances[i];
                        smallestIdx = i;
                    } 
                }
            }
            return smallestIdx;
        }

        public float? GetDistanceFrom(out bool Reachable, Actor actor, bool fromPlayerCenter)
        {
            var dist = GetDistanceFromTarget(out bool withinReach,fromPlayerCenter,  actor);
            Reachable = withinReach;
            return dist;
        }
        public unsafe float? GetDistanceFromTarget(out bool Reachable, bool fromPlayerCenter=true,  Actor manualtarget=null)
        {

            Reachable = true;
            var t = _pluginInterface.ClientState.Targets.CurrentTarget;
            if (manualtarget != null) t = manualtarget;
            if (t == null) return null;            
            
            var distActorToActor = Vector2.Distance(new Vector2(t.Position.X, t.Position.Y),
                new Vector2(_pluginInterface.ClientState.LocalPlayer.Position.X,
                    _pluginInterface.ClientState.LocalPlayer.Position.Y));
            var elevationDiff = t.Position.Z - _pluginInterface.ClientState.LocalPlayer.Position.Z;
            Reachable = elevationDiff > -5;
            var Radius = t.HitboxRadius;
            if (Radius < 0) // this is just in case dalamud says something weird but it shouldn't
            {
                Radius = *(float*)(_pluginInterface.ClientState.Targets.CurrentTarget.Address + 0xC0);
            }
            
            if (fromPlayerCenter) return (distActorToActor - Radius + (Radius < 2 && Radius >= 1 ? 1 : 0));
            float pRadius = *(float*)(_pluginInterface.ClientState.LocalPlayer.Address + 0xC0);
            return (distActorToActor - pRadius - Radius);
        }
        private void HoverDragFloat()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Set the hitbox size to whichever size you wish!\nBe aware that setting it to be any bigger or smaller than 2 will probably not give you an accurate representation of the actual hitbox but it might make it easier to see.");
            ImGui.EndTooltip();
        }
        private void HoverCheckBoxHitboxes()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Toggle the outer ring on or off for the selected hitbox, making it generally easier to perceive and contrast more with the terrain or the enemy's model.\nThe outer ring's radius will by default match that of the hitbox.");
            ImGui.EndTooltip();
        }

        private void HoverHitboxEnable(bool ranges=false)
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
        
        private void DrawWindow()
        {
            try
            {
                #region Config
                if (_config)
                {
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
                    ImGui.Checkbox("##NearbyActors", ref _drawNearbyTargets);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Only players matching at least the largest registered range will be displayed.\nFor example, if have a range set for 30 yalms, all actors within 30 yalms will \nhave their hitboxes drawn with the set color.");
                        ImGui.EndTooltip();
                    }
                    ImGui.SameLine();   
                    
                    if (_drawNearbyTargets)
                    {
                        if (ImGui.TreeNodeEx("##nearbyTargets", ImGuiTreeNodeFlags.None, "Nearby Targets"))
                        {
                            ImGui.SetCursorPosX(cursorX+xpadding*2);
                            ImGui.Checkbox("##displayNearbyMobs", ref _DisplayMobs); ImGui.SameLine();
                            if (_DisplayMobs)
                            {
                                ImGui.Checkbox("##combatOnlyMobs", ref _combatMobs); ImGui.SameLine();
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Only display during combat");
                                    ImGui.EndTooltip();
                                }
                                ImGui.Checkbox("##instanceOnlyMobs", ref _instanceMobs); ImGui.SameLine();
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Only display while instanced");
                                    ImGui.EndTooltip();
                                }
                            }
                            ImGui.Text("Nearby Mobs");
                            
                            /* commenting this out for now until dalamud's partylist class kinda just.. uhhh starts working again hehe
                            ImGui.SetCursorPosX(cursorX+xpadding*2);
                            ImGui.Checkbox("##displayPartyMembers", ref _DisplayPtMembers); ImGui.SameLine(); 
                            ImGui.Text("Party Members  "); 
                            if (_DisplayPtMembers)
                            {
                                ImGui.SameLine(); var cx = ImGui.GetCursorPosX();
                                for (var i = 0; i < aPtMembersToDisplay.Length; i++)
                                {
                                    if (i != 4)
                                        ImGui.SameLine();
                                    else
                                    {
                                        ImGui.NewLine();
                                        ImGui.SetCursorPosX(cx);
                                    }
                                    
                                    if (ImGui.Checkbox("##ptMemberToggle" + i, ref aPtMembersToDisplay[i]))
                                    {
                                        _PtMembersToDisplay[i] = aPtMembersToDisplay[i];
                                    } ImGui.SameLine();
                                    ImGui.TextColored(_PtMembersToDisplay[i] ? Green : Red, "#"+(i+2)); ImGui.SameLine();
                                }
                            }*/
                            
                            ImGui.SetCursorPosX(cursorX+xpadding*2);
                            ImGui.Checkbox("##displayPlayers", ref _DisplayPlayers); ImGui.SameLine();
                            var curX = ImGui.GetCursorPosX();
                            if (_DisplayPlayers)
                            {
                                ImGui.Checkbox("##combatOnlyOtherPlayers", ref _combatOtherPlayers); ImGui.SameLine();
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Only display during combat");
                                    ImGui.EndTooltip();
                                }
                                ImGui.Checkbox("##instanceOnlyOtherPlayers", ref _instanceOtherPlayers); ImGui.SameLine();
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Only display while instanced");
                                    ImGui.EndTooltip();
                                }
                            }
                            ImGui.Text("Other Players");
                            
                            if ((_DisplayPlayers || _DisplayMobs) && !_DotColors.Any())
                            {
                                ImGui.SetCursorPosX(curX);
                                ImGui.PushStyleColor(ImGuiCol.Text, Red);
                                ImGui.Text("\nDon't forget to set up a custom range or neither \nNearby Mobs or Other Players will be drawn on your screen.");
                                ImGui.PopStyleColor();
                            } 
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.Text("Nearby Targets");
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
                    ImGui.Checkbox("##colManager", ref _IsColManaged);
                    ImGui.SameLine();
                    
                    // color ranges manager
                    if (_IsColManaged)
                    {
                        if (ImGui.TreeNode("##colManagerTree", _currentLabel))
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
                            var dist = (float) Math.Round(GetDistanceFromTarget(out bool Reachable, _CurrentFromPlayerCenter) ?? -1f, 2);
                            var btnlabel = "No target";
                            ImGui.SameLine();
                            if (dist != -1f)
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
                                if (!_Distances.Contains(_currentDistance) || (_Distances.Contains(_currentDistance)&&_FromPlayerCenter[_Distances.IndexOf(_currentDistance)]!=_CurrentFromPlayerCenter)
                                ) // make sure distance isn't already registered
                                {
                                    //populate
                                    _Distances.Add(_currentDistance);
                                    _DotColors.Add(_currentDotColor);
                                    _ColorEnabled.Add(_currentColorEnabled);
                                    _FromPlayerCenter.Add(_CurrentFromPlayerCenter);

                                    aDistances = _Distances.ToArray();
                                    aDotColors = _DotColors.ToArray();
                                    aColorEnabled = _ColorEnabled.ToArray();
                                    aFromPlayerCenter = _FromPlayerCenter.ToArray();

                                    //reset
                                    _currentDistance = 0f;
                                    _currentDotColor = new Num.Vector4(0f, 0f, 0f, 1f);
                                    _currentColorEnabled = false;
                                    _currentDesc = _defaultDesc;
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
                            ImGui.PushStyleColor(ImGuiCol.Text, _CurrentFromPlayerCenter ? LightBlue : LightOrange);
                            ImGui.Text("<="); 
                            if (ImGui.IsItemClicked())
                            {
                                var duplicate = _Distances.Count(d => d == _currentDistance);
                                if (duplicate != 0)
                                {
                                    if (_FromPlayerCenter[_Distances.IndexOf(_currentDistance)] == _CurrentFromPlayerCenter)
                                        _CurrentFromPlayerCenter = !_CurrentFromPlayerCenter;
                                }
                                else
                                {
                                    _CurrentFromPlayerCenter = !_CurrentFromPlayerCenter;
                                }
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                                ImGui.BeginTooltip();
                                ImGui.Text("For this range distance will be\ncalculated from your Hitbox's "+(_CurrentFromPlayerCenter?"Center":"Edge")+"\nClick to quickly toggle distance calculation mode.");
                                ImGui.EndTooltip();
                            }
                            ImGui.SameLine();
                            ImGui.PopStyleColor();


                            ImGui.SameLine(); ImGui.SetCursorPosX((int)(cx+offset*8.2));
                            ImGui.SetNextItemWidth(90);
                            if (ImGui.InputFloat($" yalms##currentYalms", ref _currentDistance, 1.0f, 1.0f, "%.3g"))
                            {
                                if (_currentDistance > 55) _currentDistance = 55;
                                if (_currentDistance < -1) _currentDistance = -1;
                            };
                            if (_showFinetuning && _fineTuningSource == -1)
                            {
                                ShowFineTuning(ref _CurrentFromPlayerCenter, winx);
                            }
                            ImGui.SetCursorPosX(cx);
                            
                            
                            
                            ImGui.NewLine();
                            
                            for (var i = 0; i < _Distances.Count; i++)
                            {
                                
                                ImGui.PushFont(UiBuilder.IconFont);
                                ImGui.Button(FontAwesomeIcon.Times.ToIconString());
                                ImGui.PopFont();
                                if (ImGui.IsItemClicked() && i>=0)
                                {
                                    _Distances.RemoveAt(i);
                                    _DotColors.RemoveAt(i);
                                    _ColorEnabled.RemoveAt(i);
                                    _FromPlayerCenter.RemoveAt(i);
                                    aDistances = _Distances.ToArray();
                                    aDotColors = _DotColors.ToArray();
                                    aColorEnabled = _ColorEnabled.ToArray();
                                    aFromPlayerCenter = _FromPlayerCenter.ToArray();
                                    continue;
                                }

                                
                                    
                                ImGui.SameLine(); // enabled
                                ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset);
                                if (ImGui.Checkbox($"##{i}Enabled", ref aColorEnabled[i]))
                                {
                                    _ColorEnabled[i] = aColorEnabled[i];
                                }

                                ImGui.SameLine(); // color
                                ImGui.SameLine(); ImGui.SetCursorPosX(cx+offset*2);
                                if (ImGui.ColorEdit4($"Color to be displayed when you stand\ncloser than "+aDistances[i]+$" yalms to your target##{i}Color{_FromPlayerCenter[i]}", ref aDotColors[i],
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                                {
                                    _DotColors[i] = aDotColors[i];
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
                                ImGui.PushStyleColor(ImGuiCol.Text, _FromPlayerCenter[i] ? LightBlue : LightOrange);
                                ImGui.Text("<="); 
                                if (ImGui.IsItemClicked())
                                {
                                    /*var duplicate = _Distances.Count(d => d == _Distances[i]);
                                    if (duplicate == 0) */_FromPlayerCenter[i] = !_FromPlayerCenter[i];
                                }
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                                    ImGui.BeginTooltip();
                                    ImGui.Text("For this range distance will be\ncalculated from your Hitbox's "+(_FromPlayerCenter[i]?"Center":"Edge")+"\nClick to quickly toggle distance calculation mode.");
                                    ImGui.EndTooltip();
                                }
                                ImGui.SameLine();
                                
                                
                                ImGui.PopStyleColor();
                                
                                ImGui.SameLine(); // distance
                                ImGui.SetCursorPosX((int)(cx+offset*8.2));
                                ImGui.SetNextItemWidth(90);
                                if (ImGui.InputFloat($" yalms##{i}Distance", ref aDistances[i], 1.0f, 1.0f, "%.3g"))
                                {
                                    if (aDistances[i] > 55) aDistances[i] = 55;
                                    if (aDistances[i] < -1) aDistances[i] = -1;
                                    _Distances[i] = aDistances[i];
                                }
                                if (_showFinetuning && _fineTuningSource == i)
                                {
                                    ShowFineTuning(ref aFromPlayerCenter[i], winx);
                                    _FromPlayerCenter[i] = aFromPlayerCenter[i];
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
                        ImGui.Text(_currentLabel);
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
                    if (ImGui.Button(!_pluginInterface.ClientState.KeyState[0x10] ? FontAwesomeIcon.Sync.ToIconString() : FontAwesomeIcon.Trash.ToIconString())) 
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
                        var kofi = "Buy "+
                                   (_pluginInterface.ClientState.KeyState[0x10] ? "Child?": "Haplo")+
                                   " a Hot Chocolate";
                        
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
                        ImGui.SetCursorPosX(winx-(ImGui.CalcTextSize(kofi).X+xpadding/2));
                        if (ImGui.Button(kofi + (_pluginInterface.ClientState.KeyState[0x10] ?"????":"")))
                        {
                            System.Diagnostics.Process.Start(_pluginInterface.ClientState.KeyState[0x10] ? "https://trollface.dk/" : "https://ko-fi.com/haplo");
                        }
                        ImGui.PopStyleColor(3);
                    }
                    
                    /*ImGui.SameLine();
                    ImGui.SetNextItemWidth(100f);
                    if (ImGui.BeginCombo("##configLoaderTest", "Config #"+_currentConfig, ImGuiComboFlags.HeightSmall))
                    {
                        for (int n = 0; n < _pluginConfig.Configs.Count; n++)
                        {
                            bool isSelected = (_currentConfig==n);
                            if (ImGui.Selectable("Config #"+n, isSelected)) _currentConfig = n;
                            if (isSelected) ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }*/
                    
                    /*if (ImGui.Combo("##configLoader", ref _currentConfig, _pluginConfig.Configs.Select(c=>(c?.Name??"no name")).ToArray(),
                        _configs.Count))
                    {
                        _configuration = _pluginConfig.Configs[_currentConfig];
                        LoadConfig();
                    };*/
                    /*ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString());
                    ImGui.PopFont();
                    if (ImGui.IsItemClicked())
                    {
                        _pluginConfig.Configs.Add(new Fields());
                        _currentConfig = (_pluginConfig.Configs.Count - 1 >= 0 ? _pluginConfig.Configs.Count - 1 : 0);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Add a new configuration to the list");
                        ImGui.EndTooltip();
                    }
                    
                    
                    ImGui.Text("Config Loader ("+_pluginConfig.Configs.Count+")");
                    ImGui.Text("_pluginConfig.Configs[0]: " + _configuration.Name);*/
                    #endregion
                    
                    cursorY = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(cursorY+ypadding);
                    ImGui.End();
                }
                #endregion
                
                #region Checks to Draw 
                if (_pluginInterface.ClientState.LocalPlayer == null) return;
                if (_combat)
                {
                    if (!_pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat])
                    {
                        return;
                    }
                }

                if (_instance)
                {
                    if (!_pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty])
                    {
                        return;
                    }
                }
                #endregion
                
                #region Drawing Hitboxes / Ring
                
                
                
                #region Self Hitbox
                if (_enabled)
                {
                    
                    var actor = _pluginInterface.ClientState.LocalPlayer;
                    if (!_pluginInterface.Framework.Gui.WorldToScreen(new SharpDX.Vector3(actor.Position.X, actor.Position.Z, actor.Position.Y), out var pos)) { return; }
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Num.Vector2(pos.X - 10 - ImGuiHelpers.MainViewport.Pos.X, pos.Y - 10 - ImGuiHelpers.MainViewport.Pos.Y));
                    ImGui.Begin(
                        "Pixel Perfect Plus Self Hitbox##sHitbox",
                        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);

                    var appliedColor = ImGui.GetColorU32(_col);
                    if (_IsColManaged)
                    {
                        /*
                         * for each range in ranges registered:
                         *      take the range distance parameters (from player center, to target center)
                         *      calculate distance of player to target with parameters
                         *      if the distance is <= to the range's
                         *      and there isn't another range that ALSO is > to the player distance
                         *          then we say the dot's color is the one we found
                         */

                        var smallest = 999f;
                        var smallestidx = -1;
                        for (int i = 0; i < _Distances.Count; i++)
                        {
                            if (!_ColorEnabled[i]) continue;
                            var distance = GetDistanceFromTarget(out bool Reachable, _FromPlayerCenter[i]);
                            if ((distance ?? -1f) != -1f && Reachable)
                            {
                                if (distance <= _Distances[i] && distance < smallest)
                                {
                                    smallest = _Distances[i];
                                    smallestidx = i;
                                }
                            }
                        }
                        if (smallestidx > -1) appliedColor = ImGui.GetColorU32(_DotColors[smallestidx]);
                    }
                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Num.Vector2(pos.X, pos.Y),
                        _selfHitboxSize,
                        appliedColor,
                        100);
                    if (_showSelfcircle)
                    {
                        ImGui.GetWindowDrawList().AddCircle(
                            new Num.Vector2(pos.X, pos.Y),
                            _selfHitboxSize+0.2f,
                            ImGui.GetColorU32(_col2),
                            100);
                    }
                    ImGui.End();
                }
                #endregion
                
                #region Ring
                if (_ring)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Num.Vector2(0, 0));
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Num.Vector2(0, 0));
                    ImGui.Begin("Ring##Ring",
                        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
                    ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
                    DrawRingWorld(_pluginInterface.ClientState.LocalPlayer, _radius, _segments, _thickness,
                        ImGui.GetColorU32(_colRing));
                    ImGui.End();
                    ImGui.PopStyleVar();
                }
                #endregion
                
                #region Target Hitbox
                
                var DisplayPlayers = true; 
                var DisplayMobs = true;
                
                if (_combatMobs)
                    DisplayMobs =
                        _pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat]; 
                if (_instanceMobs)
                    DisplayMobs = 
                        _pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty];
                if (_combatOtherPlayers)
                    DisplayPlayers =
                        _pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat];
                if (_instanceOtherPlayers)
                    DisplayPlayers = 
                        _pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty];
                    
                var targetDrawn = false;
                if (_targetEnabled && _drawNearbyTargets && (DisplayMobs || DisplayPlayers))
                {
                    //var maxrenderdistance = _Distances.Max();
                    // todo: implement a custom Max() to account for custom distance calc
                    // probably not worth doing though
                    
                    for (var i = 0; i < _pluginInterface.ClientState.Actors.Length; i++)
                    {
                        var actor = _pluginInterface.ClientState.Actors[i];
                        if (actor == null) continue;
                        if (actor.ActorId == _pluginInterface.ClientState.LocalPlayer.ActorId) continue;
                        if ((!_DisplayMobs || !DisplayMobs) && actor.ObjectKind == ObjectKind.BattleNpc) continue;

                        /*var pMatched = false; // whether or not the current actor matches a party member
                        var pDisplayMember = false;
                        for (int j = 0; j < _pluginInterface.ClientState.PartyList.Count; j++)
                        {
                            pMatched = _pluginInterface.ClientState.PartyList[j].Actor.ActorId == actor.ActorId;
                            if (_PtMembersToDisplay[j]) pDisplayMember = true;
                        }
                        
                        if (!_DisplayPtMembers && pMatched) continue;*/
                        
                        if (_IgnoreKinds.Contains(actor.ObjectKind)) continue;
                        if ((!_DisplayPlayers || !DisplayPlayers )&& actor.ObjectKind == ObjectKind.Player/* && !pMatched*/) continue;
                        //if (pMatched && !_DisplayPtMembers) continue;
                        if (!_pluginInterface.Framework.Gui.WorldToScreen(actor.Position, out var pos)) continue;
                        // taken from the Dalamud repo hehe
                        // // So, while WorldToScreen will return false if the point is off of game client screen, to
                        // // to avoid performance issues, we have to manually determine if creating a window would
                        // // produce a new viewport, and skip rendering it if so

                        var screenPos = ImGui.GetMainViewport().Pos;
                        var screenSize = ImGui.GetMainViewport().Size;

                        if (pos.X > screenPos.X + screenSize.X ||
                            pos.Y > screenPos.Y + screenSize.Y)
                            continue;
                            
                        var smallest = 999f;
                        var smallestidx = -1;
                        for (int j = 0; j < _Distances.Count; j++)
                        {
                            
                            var distance = GetDistanceFrom(out bool Reachable, actor, _FromPlayerCenter[j]);
                            if ((distance ?? -1f) != -1f && Reachable)
                            {
                                if (distance <= _Distances[j] && distance < smallest)
                                {
                                    smallest = _Distances[j];
                                    smallestidx = j;
                                }
                            }
                            
                        }
                        
                        if (smallestidx == -1)
                            continue;

                        targetDrawn = actor.ActorId == _pluginInterface.ClientState.Targets?.CurrentTarget?.ActorId;

                        ImGui.SetNextWindowPos(new Num.Vector2(pos.X - 10 - ImGuiHelpers.MainViewport.Pos.X, 
                            pos.Y - 10 - ImGuiHelpers.MainViewport.Pos.Y));
                            
                        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Num.Vector2(pos.X - 10 - ImGuiHelpers.MainViewport.Pos.X, pos.Y - 10 - ImGuiHelpers.MainViewport.Pos.Y));
                            
                        ImGui.Begin(
                            "Pixel Perfect Plus Nearby Targets##" + actor.ActorId,
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                            ImGuiWindowFlags.AlwaysAutoResize |
                            ImGuiWindowFlags.NoInputs | 
                            ImGuiWindowFlags.NoBackground);
                            
                        var appliedColor = ImGui.GetColorU32(_targetCol);
                            
                        appliedColor = ImGui.GetColorU32(_DotColors[smallestidx]);
                            
                        ImGui.GetWindowDrawList().AddCircleFilled(
                            new Num.Vector2(pos.X, pos.Y),
                            _targetHitboxSize,
                            appliedColor,
                            100);
                        if (_showTargetRing)
                        {
                            ImGui.GetWindowDrawList().AddCircle(
                                new Num.Vector2(pos.X, pos.Y),
                                _targetHitboxSize+0.2f,
                                ImGui.GetColorU32(_colTargetRing),
                                100);
                        }
                        ImGui.End();
                    }
                }
                
                // draw only target
                if (_targetEnabled && _pluginInterface.ClientState.Targets.CurrentTarget != null && !targetDrawn)
                {
                    var target = _pluginInterface.ClientState.Targets.CurrentTarget;
                    
                    if (!_pluginInterface.Framework.Gui.WorldToScreen(new SharpDX.Vector3(target.Position.X, target.Position.Z, target.Position.Y), out var pos)) { return; }
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Num.Vector2(pos.X - 10 - ImGuiHelpers.MainViewport.Pos.X, pos.Y - 10 - ImGuiHelpers.MainViewport.Pos.Y));
                    ImGui.Begin(
                        "Pixel Perfect Plus Target Hitbox##tHitbox",
                        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);

                    var appliedColor = ImGui.GetColorU32(_targetCol);
                    
                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Num.Vector2(pos.X, pos.Y),
                        _targetHitboxSize,
                        appliedColor,
                        100);
                    if (_showTargetRing)
                    {
                        ImGui.GetWindowDrawList().AddCircle(
                            new Num.Vector2(pos.X, pos.Y),
                            _targetHitboxSize+0.2f,
                            ImGui.GetColorU32(_colTargetRing),
                            100);
                    }
                    ImGui.End();
                }
                #endregion
                
                #endregion
            }
            catch (Exception e)
            {
                PluginLog.Log(e.Message);
            }
        }

        private void ShowFineTuning(ref bool currentFromPlayerCenter, float winX)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Grey);
            var displayedStr = "Calculate distance from" +
                               (currentFromPlayerCenter ? "Player's Center" : "Player's Hitbox End")+
                               "to" +
                               "Target's Hitbox End";
            ImGui.SetCursorPosX((winX-ImGui.CalcTextSize(displayedStr).X)/2);
            ImGui.Text("Calculate distance from"); ImGui.SameLine();
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Text, currentFromPlayerCenter ? LightBlue : LightOrange);
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
            ImGui.PushStyleColor(ImGuiCol.Text, Grey);
            ImGui.Text("to"); ImGui.SameLine();
            ImGui.Text(currentFromPlayerCenter ? "Target's Center" : "Target's Hitbox End");
            ImGui.PopStyleColor();
            ImGui.NewLine();
        }
        
        private void ConfigWindow(object sender, EventArgs args)
        {
            LoadConfig();
            _config = true;
        }

        private void Command(string command, string arguments)
        {
            LoadConfig();
            _config = !_config;
        }

        private void SaveConfig()
        {
            _configuration.Combat = _combat; // limit display to when in combat
            _configuration.Instance = _instance; // limit display to instances
            _configuration.ShowRingConfig = _showRingConfig; // whether to show the configuration for the additional player ring or not
            _configuration.IsColManaged = _IsColManaged; // whether or not the plugin manages the selfhitbox color
            _configuration.Distances = _Distances; // distances component of the custom ranges
            _configuration.DotColors = _DotColors; // color component of the custom ranges
            _configuration.ColorEnabled = _ColorEnabled; // individual toggles for custom ranges
            _configuration.FromPlayerCenter = _FromPlayerCenter; // whether to calculate distance from the player center for a given range
            _configuration.DrawNearbyTargets = _drawNearbyTargets;
            //_configuration.PtMembersToDisplay = _PtMembersToDisplay;
            //_configuration.DisplayPtMembers = _DisplayPtMembers;
            _configuration.DisplayMobs = _DisplayMobs;
            _configuration.DisplayPlayers = _DisplayPlayers;
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
            //_configs[0] = _configuration;

            /*if (_pluginConfig.Configs?[_currentConfig] != null)
            {
                PluginLog.Log("updated " + _currentConfig);
                _pluginConfig.Configs[_currentConfig] = _configuration;
            }
            else
            {
                _pluginConfig?.Configs?.Add(_configuration);
            }*/
            

            _pluginInterface.SavePluginConfig(_configuration);
        }

        private void DrawRingWorld(Dalamud.Game.ClientState.Actors.Types.Actor actor, float radius, int numSegments, float thicc, uint colour)
        {
            var seg = numSegments / 2;
            for (var i = 0; i <= numSegments; i++)
            {
                _pluginInterface.Framework.Gui.WorldToScreen(new SharpDX.Vector3(actor.Position.X + (radius * (float)Math.Sin((Math.PI / seg) * i)), actor.Position.Z, actor.Position.Y + (radius * (float)Math.Cos((Math.PI / seg) * i))), out SharpDX.Vector2 pos);
                ImGui.GetWindowDrawList().PathLineTo(new Num.Vector2(pos.X, pos.Y));
            }
            ImGui.GetWindowDrawList().PathStroke(colour, ImDrawFlags.Closed, thicc);
        }
    }

    /*public class Fields
    {
        public string Name { get; set; } = "Default";
        public int Version { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Combat { get; set; } = false;
        public bool Circle { get; set; }
        public bool Instance { get; set; }
        public Num.Vector4 Col { get; set; } = new Num.Vector4(1f, 1f, 1f, 0.5882f);
        public Num.Vector4 Col2 { get; set; } = new Num.Vector4(0f, 0f, 0f, 1f);
        public Num.Vector4 ColRing { get; set; } = new Num.Vector4(0.4f, 0.4f, 0.4f, 0.5f);
        public int Segments { get; set; } = 100;
        public float Thickness { get; set; } = 10f;
        public bool Ring { get; set; }
        public float Radius { get; set; } = 2f;
        public bool IsColManaged { get; set; }
        public List<float> Distances { get; set; }
        public List<Num.Vector4> DotColors { get; set; }
        public List<bool> ColorEnabled { get; set; }
        public float SelfHitboxSize { get; set; } = 2f;
        public bool TargetEnabled { get; set; } = true;
        public float TargetHitboxSize { get; set; } = 2f;
        public Num.Vector4 TargetHitboxCol { get; set; } = new Num.Vector4(1f, 0f, 0f, 0.5882f);
        public bool TargetHitboxRing { get; set; }
        public bool ShowRingConfig { get; set; }
        public List<bool> FromPlayerCenter { get; set; }
        public List<bool> PtMembersToDisplay { get; set; }
        public Num.Vector4 ColTargetRing { get; set; } = new Num.Vector4(0f, 0f, 0f, 1f);
        public bool DrawNearbyTargets { get; set; }
        public bool DisplayPtMembers { get; set; }
        public bool DisplayMobs { get; set; }
        public bool DisplayPlayers { get; set; }
    }*/
    public class Config : IPluginConfiguration
    {
        //public string Name { get; set; } = "Default";
        
        public int Version { get; set; } = 0;

        //public List<Fields> Configs = new List<Fields>(new Fields[1]);
        public bool Enabled { get; set; } = true;
        public bool Combat { get; set; } = false;
        public bool Circle { get; set; }
        public bool Instance { get; set; }
        public Num.Vector4 Col { get; set; } = new Num.Vector4(1f, 1f, 1f, 0.5882f);
        public Num.Vector4 Col2 { get; set; } = new Num.Vector4(0f, 0f, 0f, 1f);
        public Num.Vector4 ColRing { get; set; } = new Num.Vector4(0.4f, 0.4f, 0.4f, 0.5f);
        public int Segments { get; set; } = 100;
        public float Thickness { get; set; } = 10f;
        public bool Ring { get; set; }
        public float Radius { get; set; } = 2f;
        public bool IsColManaged { get; set; }
        public List<float> Distances { get; set; }
        public List<Num.Vector4> DotColors { get; set; }
        public List<bool> ColorEnabled { get; set; }
        public float SelfHitboxSize { get; set; } = 2f;
        public bool TargetEnabled { get; set; } = true;
        public float TargetHitboxSize { get; set; } = 2f;
        public Num.Vector4 TargetHitboxCol { get; set; } = new Num.Vector4(1f, 0f, 0f, 0.5882f);
        public bool TargetHitboxRing { get; set; } 
        public bool ShowRingConfig { get; set; }
        public List<bool> FromPlayerCenter { get; set; }
        //public List<bool> PtMembersToDisplay { get; set; }
        public Num.Vector4 ColTargetRing { get; set; } = new Num.Vector4(0f, 0f, 0f, 1f);
        public bool DrawNearbyTargets { get; set; }
        //public bool DisplayPtMembers { get; set; }
        public bool DisplayMobs { get; set; }
        public bool DisplayPlayers { get; set; }

        public bool CombatOtherPlayers { get; set; }
        public bool InstanceOtherPlayers { get; set; }
        public bool CombatMobs { get; set; }
        public bool InstanceMobs { get; set; }
        public bool ShowKofi { get; set; }

        //public int CurrentConfigIndex { get; set; } = 0;
    }

}
