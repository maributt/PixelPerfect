using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
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
        #region extra

        private const int LatestVersion = 2;
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
        
        public static Random Random = new Random();

        public static float Luminance(Num.Vector4 col)
        {
            return .2126f * col.X + .7152f * col.Y + .0722f * col.Z;
        }

        public static bool ShouldBeBlack(float luminance)
        {
            return luminance > .179f;
        }

        public class Hitbox
        {

            public Num.Vector4 Color { get; set; }
            public Num.Vector4 CircleColor { get; set; }
            
            /// <summary>
            /// Whether or not the Hitbox's color is managed by the defined color ranges
            /// </summary>
            public bool ColorManage { get; set; }
            
            public bool ColorManageRing { get; set; }
            public float Size { get; set; }

            public bool CircleEnabled { get; set; }
            public bool Enabled { get; set; }

            public bool ShowExConfig { get; set; } = false;
            public bool DrawLine { get; set; } = false;
            public bool DrawDistance;

            public bool RestrictToCombat { get; set; } = false;
            public bool RestrictToInstance { get; set; } = false;

            public OuterRing Ring;

            public Hitbox(Num.Vector4 color, Num.Vector4 circleColor, bool colorManage, float size, bool circleEnabled, OuterRing ring, bool enabled)
            {
                Color = color;
                CircleColor = circleColor;
                ColorManage = colorManage;
                Size = size;
                CircleEnabled = circleEnabled;
                Enabled = enabled;
                Ring = ring;
            }

            public Hitbox(bool enabled=false) {
                Color = new Num.Vector4(Random.NextFloat(0, 1), Random.NextFloat(0, 1), Random.NextFloat(0, 1), 1f);
                CircleColor = ShouldBeBlack(Luminance(Color))?new Num.Vector4(0f, 0f, 0f, 1f):new Num.Vector4(1f, 1f, 1f, 1f);
                ColorManage = false;
                Size = 2.0f;
                CircleEnabled = true;
                Enabled = enabled;
                Ring = new OuterRing(Color);
            }
        }

        public class OuterRing
        {

            public Num.Vector4 Color;
            
            public float Thickness;
            public float Radius;
            public int Segments = 50;
            public bool Enabled;
            public bool ColorManage;
            
            public OuterRing(Num.Vector4? col = null)
            {
                Color = col ?? new Num.Vector4(Random.NextFloat(0, 1), Random.NextFloat(0, 1), Random.NextFloat(0, 1), 0.5f);
                Thickness = Random.Next(2, 20);
                Radius = Random.NextFloat(1, 5) + 0f; // actually a diameter 
                Enabled = false;
                ColorManage = ColorManage;
            }
        }
        
        private void DrawHitboxConfigurator(ref Hitbox hb, string label="", bool showLabel = true, bool alwaysShowMenu = false)
        {
            if (!alwaysShowMenu)
            {
                var enabled = hb.Enabled;
                if (ImGui.Checkbox($"##{hb.GetHashCode()}HitboxEnabled", ref enabled))
                    hb.Enabled = enabled;
                if (ImGui.IsItemHovered()) HoverHitboxEnable();
            }
            if (hb.Enabled)
            {
                // Hitbox Color
                ImGui.SameLine();
                var col = hb.Color;
                if (hb.ColorManage)
                {
                    var colmanage = hb.ColorManage;
                    if (ImGui.Checkbox($"##{hb.GetHashCode()}ColorManage", ref colmanage))
                    {
                        hb.ColorManage = colmanage;
                    }

                    if (ImGui.IsItemHovered())
                        HoverColorManage(hb.ColorManage);
                }
                else
                {
                    // select ring color based on the luminance of the hitbox's color
                    if (ImGui.ColorEdit4($"##{hb.GetHashCode()}HitboxCol", ref col,
                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                    {
                        hb.Color = col;
                        hb.Ring.Color = col;
                        if (hb.ColorManageRing)
                            hb.CircleColor = ShouldBeBlack(Luminance(hb.Color))
                                ? new Num.Vector4(0f, 0f, 0f, 1f)
                                : new Num.Vector4(1f, 1f, 1f, 1f);
                    }
                        
                    if (ImGui.IsItemClicked() && _pluginInterface.ClientState.KeyState[0x10])
                    {
                        hb.ColorManage = !hb.ColorManage;
                    }
                    if (ImGui.IsItemHovered())
                        HoverColorManage(hb.ColorManage);
                }
                
                // Hitbox toggle ring
                ImGui.SameLine();
                var showCircle = hb.CircleEnabled;
                if (ImGui.Checkbox($"##{hb.GetHashCode()}OuterRingShow", ref showCircle))
                    hb.CircleEnabled = showCircle;
                if (ImGui.IsItemHovered()) HoverCheckBoxHitboxes();
                if (showCircle)
                {
                    // Hitbox ring color
                    ImGui.SameLine();
                    
                    if (hb.ColorManageRing)
                    {
                        var colmanagering = hb.ColorManageRing;
                        if(ImGui.Checkbox($"##{hb.GetHashCode()}ColorManageRing", ref colmanagering))
                        {
                            hb.ColorManageRing = colmanagering;
                        } 
                        if (ImGui.IsItemHovered())
                            HoverColorManage(hb.ColorManage);
                    }
                    else
                    {
                        var col2 = hb.CircleColor;
                        if (ImGui.ColorEdit4($"##{hb.GetHashCode()}OuterRingColor", ref col2,
                            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                            hb.CircleColor = col2;
                        if (ImGui.IsItemClicked() && _pluginInterface.ClientState.KeyState[0x10])
                        {
                            hb.ColorManageRing = !hb.ColorManageRing;
                        }
                        if (ImGui.IsItemHovered())
                            HoverColorManage(hb.ColorManageRing);
                    }
                } 

                // Hitbox size
                ImGui.SameLine();
                var hitboxSize = hb.Size;
                ImGui.SetNextItemWidth(25f);
                if (ImGui.DragFloat($"##{hb.GetHashCode()}CircleSize", ref hitboxSize, 0.1f, 0.5f, 100f, "%.4g"))
                {
                    //if (_selfHitboxSize > 6.5f) _selfHitboxSize = 6.5f;
                    if (hitboxSize < 0.5f) hitboxSize = 0.5f;
                    hb.Size = hitboxSize;
                }

                ;
                if (ImGui.IsItemHovered()) HoverDragFloat();
                
                ImGui.SameLine();
                if (showLabel) ImGui.Text(label);
                if (ImGui.IsItemClicked())
                {
                    hb.ShowExConfig = !hb.ShowExConfig;
                }
                if (ImGui.IsItemHovered())
                    HoverExtraHitboxConfig(hb.ShowExConfig);
            
                if (hb.ShowExConfig) // extra config (ring, line, etc)
                {
                    // ring
                    if (!showLabel) ImGui.NewLine();
                    var drawRing = hb.Ring.Enabled;
                    if (ImGui.Checkbox($"##{hb.GetHashCode()}DrawRing", ref drawRing))
                        hb.Ring.Enabled = drawRing;
                    if (ImGui.IsItemHovered()) 
                        ImGui.SetTooltip((hb.Ring.Enabled ?"Dis":"En") + "able ring for the selected hitbox.");
                    ImGui.SameLine();
                    if (hb.Ring.Enabled)
                    {
                        if (hb.Ring.ColorManage)
                        {
                            var colmanagering = hb.Ring.ColorManage;
                            if(ImGui.Checkbox($"##{hb.Ring.GetHashCode()}ColorManageRing", ref colmanagering))
                            {
                                hb.Ring.ColorManage = colmanagering;
                            } 
                            if (ImGui.IsItemHovered())
                                HoverColorManage(hb.Ring.ColorManage);
                        }
                        else
                        {
                            var col2 = hb.Ring.Color;
                            if (ImGui.ColorEdit4($"##{hb.Ring.GetHashCode()}OuterRingColor", ref col2,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                                hb.Ring.Color = col2;
                            if (ImGui.IsItemClicked() && _pluginInterface.ClientState.KeyState[0x10])
                            {
                                hb.Ring.ColorManage = !hb.Ring.ColorManage;
                            }
                            if (ImGui.IsItemHovered())
                                HoverColorManage(hb.Ring.ColorManage);
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(ImGui.CalcTextSize(hb.Ring.Radius+"").X+20);
                        ImGui.DragFloat($"##ringDiameter{hb.Ring.GetHashCode()}", ref hb.Ring.Radius, 0.1f, 1f, 100f, "%.1f");
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Diameter");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(ImGui.CalcTextSize(hb.Ring.Thickness+"").X+20);
                        ImGui.DragFloat($"##ringThickness{hb.Ring.GetHashCode()}", ref hb.Ring.Thickness, 0.1f, 1f, 100f, "%.1f");
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Thickness");
                        ImGui.SameLine();
                        var segmentsFloat = hb.Ring.Segments + 0f;
                        ImGui.SetNextItemWidth(ImGui.CalcTextSize(segmentsFloat+"").X+20);
                        if (ImGui.DragFloat($"##ringSegments{hb.Ring.GetHashCode()}", ref segmentsFloat, 1f, 1f, 100f, "%.0f"))
                            hb.Ring.Segments = (int)segmentsFloat;
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Segments");
                        ImGui.SameLine();
                    }
                
                    ImGui.SameLine();
                    var drawLine = hb.DrawLine;
                    if (ImGui.Checkbox($"##{hb.GetHashCode()}DrawLine", ref drawLine))
                        hb.DrawLine = drawLine;
                    if (ImGui.IsItemHovered())
                        HoverCheckBoxLine();

                    ImGui.SameLine();
                    var drawDistance = hb.DrawDistance;
                    if (ImGui.Checkbox($"##{hb.GetHashCode()}DrawDistance", ref drawDistance))
                        hb.DrawDistance = drawDistance;
                    if (ImGui.IsItemHovered())
                        HoverCheckBoxDistance();
                }
            }
            else
            {
                ImGui.SameLine();
                ImGui.Text(label);
            }
            
        }
        
        private void HoverColorManage(bool show)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
                ImGui.PushStyleColor(ImGuiCol.Text, show?_lightOrange:_lightBlue);
                    ImGui.Text((show?"":"Shift+")+"Click"); ImGui.SameLine();
                ImGui.PopStyleColor();
                ImGui.Dummy(new Num.Vector2(-13, 0)); ImGui.SameLine();
                ImGui.Text("to toggle"); ImGui.SameLine();
                ImGui.Dummy(new Num.Vector2(-13, 0)); ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, show?_lightOrange:_lightBlue);
                    ImGui.Text(show?"off":"on"); ImGui.SameLine();
                ImGui.PopStyleColor();
                ImGui.Dummy(new Num.Vector2(-13, 0)); ImGui.SameLine();
                ImGui.Text("color management based on");
                ImGui.Text("the distance from the player to the selected hitbox.");
            ImGui.EndTooltip();
        }

        private void HoverExtraHitboxConfig(bool show)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped((show?"Hide":"Show")+" additional configuration for the selected hitbox");
            ImGui.EndTooltip();
        }
        private void HoverCheckBoxDistance()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Draw the distance to the selected hitbox next to it");
            ImGui.EndTooltip();
        }

        private void HoverCheckBoxLine()
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextWrapped("Draw a line from your hitbox to the selected hitbox");
            ImGui.EndTooltip();
        }

        

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
        private Dictionary<ObjectKind, List<string>> _objectKindStringFilters = new Dictionary<ObjectKind, List<string>>();
        private Dictionary<ObjectKind, List<int>> _objectKindIdFilters = new Dictionary<ObjectKind, List<int>>();
        private Dictionary<ObjectKind, bool[]> _objectKindFiltering = new Dictionary<ObjectKind, bool[]>();
        private Dictionary<string, Num.Vector4> _nameFilterColor = new Dictionary<string, Num.Vector4>();
        private Dictionary<string, Num.Vector4> _idFilterColor = new Dictionary<string, Num.Vector4>();
        private Dictionary<string, bool> _idFilterColorEnabled = new Dictionary<string, bool>();
        private Dictionary<string, bool> _nameFilterColorEnabled = new Dictionary<string, bool>();

        private Dictionary<ObjectKind, FilterList> ObjectFilterLists = new Dictionary<ObjectKind, FilterList>()
        { };

        private class FilterList
        {
            public List<Filter> Filters;
            public ObjectKind objectKind;
            public string objectKindStr;
            public bool WhitelistEnabled;
            public Hitbox GlobalHitbox;
            public bool FilterableById;

            public string CurrentFilter = "";

            public bool Contains(Filter f)
            {
                return Filters.Any(filter => filter.Equals(f));
            }

            public bool Contains(int id)
            {
                return Filters.Any(filter => filter.IntFilter == id);
            }
            
            public bool Contains(string name)
            {
                return Filters.Any(filter => filter.StrFilter == name);
            }

            public bool Remove(string name)
            {
                return Filters.RemoveAll(filter => filter.MatchesFilter(name))>0;
            }
            
            public bool Remove(int id)
            {
                return Filters.RemoveAll(filter => filter.MatchesFilter(id))>0;
            }

            public bool Add(int id)
            {
                if (id != -1)
                    Filters.Add(new Filter(id));
                return id != -1;
            }

            public bool Add(string name)
            {
                var invalid = string.IsNullOrWhiteSpace(name);
                if (!invalid)
                    Filters.Add(new Filter(name));
                return !invalid;
            }

            public bool IsFilerableById()
            {
                return objectKind == ObjectKind.EventObj 
                       || objectKind == ObjectKind.BattleNpc;
            }

            public FilterList(ObjectKind objk)
            {
                Filters = new List<Filter>();
                objectKind = objk;
                objectKindStr = objectKind.ToString();
                GlobalHitbox = new Hitbox();
                FilterableById = IsFilerableById();
                PluginLog.LogDebug($"Created {objectKindStr} FilterList");
            }

            public bool AddFilter(Filter f)
            {
                if (string.IsNullOrWhiteSpace(f.StrFilter) && f.IntFilter == -1) return false;
                //todo: maybe add some verification steps based on the current objectkind for example player names
                if (!Contains(f))
                {
                    PluginLog.Debug($"Added {f} to the FilterList");
                    Filters.Add(f);
                }
                else
                {
                    PluginLog.Debug($"FilterList already contains Filter {f}");
                    return false;
                }
                    
                return true;
            }
        }
        
        private class Filter
        {
            public string StrFilter = null;
            public long IntFilter = -1; // fuk u im using a long AND im calling it an int
            public bool Enabled = false;
            public Hitbox AssociatedHitbox;

            public Filter(string strFilter = null, bool idFilter = false)
            {
                if (idFilter && long.TryParse(strFilter, out var intFilter))
                    IntFilter = intFilter;
                else
                    StrFilter = strFilter;
                AssociatedHitbox = new Hitbox(true);
            }
            
            public Filter(int idFilter)
            {
                IntFilter = idFilter;
                AssociatedHitbox = new Hitbox(true);
            }

            public bool MatchesFilter(int id)
            {
                return id == IntFilter && IntFilter != -1;
            }

            public bool MatchesFilter(string name)
            {
                return name == StrFilter && StrFilter != null;
            }

            public override string ToString()
            {
                return $"Filter {{(strfilter: {StrFilter}), (intFilter: {IntFilter}), (enabled: {Enabled})}}";
            }

            public bool Equals(Filter obj)
            {
                return StrFilter == obj.StrFilter && IntFilter == obj.IntFilter;
            }

            public string ToLabel()
            {
                if (StrFilter != null)
                    return StrFilter;
                else
                    return ""+IntFilter;
            }
        }

        private int _currentObjectKind = 0;
        private string[] _objectKinds = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        private string[] _currentFilters = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        private int[] _currentIdFilters = new int[Enum.GetValues(typeof (ObjectKind)).Length-1];


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


        public void Initialize(DalamudPluginInterface pI)
        {
            _pluginInterface = pI;
            LoadConfig();
            _currentDesc = DefaultDesc;

            _pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            _pluginInterface.CommandManager.AddHandler("/ppp", new CommandInfo(Command)
            {
                HelpMessage = "Pixel Perfect Plus config."
            });
        }

        private void LoadConfig(bool forceReset=false)
        {
            _configuration =  _pluginInterface.GetPluginConfig() as Config ?? new Config();
            if (forceReset || _configuration.Version != LatestVersion) _configuration = new Config();
            _drawObjectKinds = _configuration.drawObjectKinds;
            _objectKindStringFilters = _configuration.ObjectKindFilters;
            _objectKindIdFilters = _configuration.ObjectKindIdFilter;
            _objectKindFiltering = _configuration.ObjectKindFiltering;
            _currentObjectKind = _configuration._currentObjectKind;
            _objectKinds = _configuration.ObjectKinds;
            _currentFilters = _configuration.currentFilters;
            _currentIdFilters = _configuration.currentIdFilters;
            _nameFilterColor = _configuration.NameFilterColor;
            _idFilterColor = _configuration.IdFilterColor;
            _idFilterColorEnabled = _configuration.IdFilterColorEnabled;
            _nameFilterColorEnabled = _configuration.NameFilterColorEnabled;

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

            _col2 = _configuration.Col2;
            
            _selfHitboxSize = _configuration.SelfHitboxSize;
            _col = _configuration.Col;
            _enabled = _configuration.Enabled;
            _showSelfcircle = _configuration.Circle;

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

        /*private float? GetDistanceFrom(out bool reachable, Actor actor, bool fromPlayerCenter)
        {
            var dist = GetDistanceFromTarget(out var withinReach,fromPlayerCenter,  actor);
            reachable = withinReach;
            return dist;
        }*/
        
        private unsafe float? GetDistanceFromTarget(ref bool reachable, bool fromPlayerCenter=true)
        {
            return GetDistanceFrom(_pluginInterface.ClientState.Targets.CurrentTarget, ref reachable, fromPlayerCenter);
        }
        
        private unsafe float? GetDistanceFromTarget(bool fromPlayerCenter=true)
        {
            var reachable = false;
            return GetDistanceFrom(_pluginInterface.ClientState.Targets.CurrentTarget, ref reachable, fromPlayerCenter);
        }

        private unsafe float? GetDistanceFrom(Actor target, bool fromPlayerCenter = true)
        {
            var reachable = false;
            return GetDistanceFrom(target, ref reachable, fromPlayerCenter);

        }
        
        private unsafe float? GetDistanceFrom(Actor target, ref bool reachable, bool fromPlayerCenter=true)
        {
            reachable = true;
            if (target == null) return null;
            var distActorToActor = Vector2.Distance(new Vector2(target.Position.X, target.Position.Y),
                new Vector2(_pluginInterface.ClientState.LocalPlayer.Position.X,
                    _pluginInterface.ClientState.LocalPlayer.Position.Y));
            var elevationDiff = target.Position.Z - _pluginInterface.ClientState.LocalPlayer.Position.Z;
            reachable = elevationDiff > -5;
            if (fromPlayerCenter) return (distActorToActor - target.HitboxRadius + (target.HitboxRadius < 2 && target.HitboxRadius >= 1 ? 1 : 0));
            var pRadius = *(float*)(_pluginInterface.ClientState.LocalPlayer.Address + 0xC0);
            return (distActorToActor - pRadius - target.HitboxRadius);
        }

        private static bool CanObjKindBeFilteredById(ObjectKind obj)
        {
            return obj == ObjectKind.EventObj || obj == ObjectKind.BattleNpc;
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
        
        private bool AddFilterTo<T>(ObjectKind objKind, T filter)
        {
            if (!(filter is string strFilter))
            {
                if (!(filter is int idFilter)) return false;
                
                if (idFilter < 0 || _objectKindIdFilters[objKind].Contains(idFilter)) return false;
                _objectKindIdFilters[objKind].Add(idFilter);
                _idFilterColorEnabled.Add($"{objKind}{idFilter}", false);
                _idFilterColor.Add($"{objKind}{idFilter}", new Num.Vector4 {W = 1f});
                _currentIdFilters[(int) objKind] = 0;
                PluginLog.Debug($"[+] Added \"{filter}\" to {objKind} whitelist");
                return true;
            }
            if (string.IsNullOrEmpty(strFilter) 
                || ( objKind == ObjectKind.Player && (strFilter.Split(' ').Length != 2 || strFilter.Split(' ').Any(string.IsNullOrEmpty)) )
                || _objectKindStringFilters[objKind].Contains(strFilter)) return false;
            _objectKindStringFilters[objKind].Add(strFilter);
            _nameFilterColorEnabled.Add($"{objKind}{strFilter}", false);
            _nameFilterColor.Add($"{objKind}{strFilter}", new Num.Vector4 {W = 1f});
            _currentFilters[(int)objKind] = "";
            PluginLog.Debug($"[+] Added \"{filter}\" to {objKind} whitelist");
            return true;
        }

        private bool RemoveFilterFrom<T>(ObjectKind objKind, T filter)
        {
            if (!(filter is string strFilter))
            {
                if (!(filter is int idFilter)) return false;
                
                if (!_objectKindIdFilters[objKind].Contains(idFilter)) return false;
                _objectKindIdFilters[objKind].Remove(idFilter);
                _idFilterColorEnabled.Remove($"{objKind}{idFilter}");
                _idFilterColor.Remove($"{objKind}{idFilter}");
                _currentIdFilters[(int) objKind] = 0;
                PluginLog.Debug($"[-] Removed \"{filter}\" from {objKind} whitelist");
                return true;
            }
            if (!_objectKindStringFilters[objKind].Contains(strFilter)) return false;
            _objectKindStringFilters[objKind].Remove(strFilter);
            _nameFilterColorEnabled.Remove($"{objKind}{strFilter}");
            _nameFilterColor.Remove($"{objKind}{strFilter}");
            _currentFilters[(int)objKind] = "";
            PluginLog.Debug($"[-] Removed \"{filter}\" from {objKind} whitelist");
            return true;
        }

        private unsafe void DrawConfig()
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
            ImGui.Indent();
            DrawHitboxConfigurator(ref _configuration.SelfHitbox, "Self");
            DrawHitboxConfigurator(ref _configuration.TargetHitbox, "Target");

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
                    ImGui.Indent(10);
                    ImGui.Text("Draw hitbox of ");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(125);
                    ImGui.Combo("##addObjectKind", ref _currentObjectKind, _objectKinds, _objectKinds.Length);
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    //todo: replace with new system
                    if (true){
                        // new system
                        // hitbox filterlists adder
                        var already = ObjectFilterLists.ContainsKey((ObjectKind) _currentObjectKind + 1);
                        if (ImGui.Button(
                            already ? FontAwesomeIcon.Times.ToIconString() : FontAwesomeIcon.Plus.ToIconString()
                        ))
                        {
                            if (!already)
                                ObjectFilterLists.Add(
                                    (ObjectKind) _currentObjectKind + 1,
                                    new FilterList((ObjectKind) _currentObjectKind + 1)
                                    );
                            else ObjectFilterLists.Remove((ObjectKind) _currentObjectKind + 1);
                        }

                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("..." +
                                       (!already ? "Add" : "Remove") +
                                       $" {_objectKinds[_currentObjectKind]} objects " +
                                       (!already ? "to" : "from") +
                                       $" the list of drawn hitboxes.");
                            ImGui.EndTooltip();
                        }

                        // registered filterlists
                        ImGui.Indent();
                        foreach (var kvp in ObjectFilterLists)
                        {
                            var label = kvp.Value.objectKindStr;

                            // Remove FilterList button -
                            if (ImGui.Button($"  -  ##remove{label}"))
                            {
                                ObjectFilterLists.Remove(kvp.Key);
                                // hide the interactive selector if filterlist gets removed and was its target
                                if (kvp.Key == _interactiveSelectorObjKind) _showInteractiveSelector = false;
                                break;
                            }
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip($"Remove {label} from the list of drawn hitboxes.");
                            ImGui.SameLine();

                            // Whitelist Toggle -
                            // checkbox gets created regardless don't even WORRY about it
                            if (ImGui.Checkbox($"##toggleFiltering{label}", ref kvp.Value.WhitelistEnabled)
                                && kvp.Key == _interactiveSelectorObjKind)
                                _showInteractiveSelector =
                                    false; // auto disable interactive selector if whitelisting is disabled for the filterlist
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Toggle whitelisting for {label} hitboxes.");
                            ImGui.SameLine();

                            // Underlying Hitbox toggle -
                            var ghEnabled = kvp.Value.GlobalHitbox.Enabled;
                            if (ImGui.Checkbox($"##toggleGlobalFilterlistHitbox{label}", ref ghEnabled)) 
                                kvp.Value.GlobalHitbox.Enabled = ghEnabled;
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Toggle underlying hitbox for {label} hitboxes.");
                            ImGui.SameLine();
                            // Draw whitelisting interface
                            if (kvp.Value.WhitelistEnabled)
                            {
                                if (ImGui.TreeNode($"{label}##{label}"))
                                {
                                    ImGui.Indent(10);
                                    var filterableById = kvp.Value.IsFilerableById();
                                    ImGui.Text($"Only show {label}s matching the names:");
                                    
                                    // add button
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString());
                                    ImGui.PopFont();
                                    if (ImGui.IsItemClicked()) // Add Button clicked
                                    {
                                        kvp.Value.AddFilter(new Filter(kvp.Value.CurrentFilter, filterableById));
                                    }
                                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add name to whitelist.");
                                    ImGui.SameLine();
                                    
                                    // filter input
                                    ImGui.SetNextItemWidth(150f);
                                    ImGui.InputText($"##filter{kvp.Key.GetHashCode()}{label}",
                                        ref kvp.Value.CurrentFilter, 64);
                                    if (filterableById && ImGui.IsItemHovered())
                                        ImGui.SetTooltip($"You can also filter {label}s by their ID.");
                                    ImGui.SameLine();

                                    
                                    // interactive selector toggle
                                    var hideSelector = kvp.Key == _interactiveSelectorObjKind &&
                                                       _showInteractiveSelector;
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    if (ImGui.Button((!hideSelector
                                                         ? FontAwesomeIcon.Eye.ToIconString()
                                                         : FontAwesomeIcon.EyeSlash.ToIconString())
                                                     + "##showSelector"))
                                    {
                                        _interactiveSelectorObjKind = kvp.Key;
                                        _showInteractiveSelector = !hideSelector;
                                    }
                                    ImGui.PopFont();
                                    if (ImGui.IsItemHovered())
                                        ImGui.SetTooltip(!hideSelector
                                            ? $"Show interactive selector for {label}s."
                                            : "Hide interactive selector.");
                                    foreach (Filter filter in kvp.Value.Filters)
                                    {
                                        var flabel = filter.ToLabel();
                                        if (ImGui.Button($"  -  ##removeFilter{flabel}"))
                                        {
                                            kvp.Value.Filters.Remove(filter);
                                            break;
                                        }
                                        if (ImGui.IsItemHovered())
                                            ImGui.SetTooltip($"Remove {label} from the list of filters.");
                                        ImGui.SameLine();
                                        DrawHitboxConfigurator(ref filter.AssociatedHitbox, flabel);
                                        //ImGui.NewLine();
                                        /*ImGui.Text(flabel);
                                        ImGui.SameLine();
                                        var enabled = filter.AssociatedHitbox.Enabled;
                                        if (ImGui.Checkbox($"##{flabel}CustomHitbox", ref enabled))
                                        {
                                            filter.AssociatedHitbox.Enabled = enabled;
                                        }*/
                                        
                                    }
                                    ImGui.Unindent(10);
                                    ImGui.TreePop();
                                    if (!kvp.Value.GlobalHitbox.Enabled) ImGui.NewLine();
                                }
                            }
                            else
                            {
                                ImGui.Text("          "+label);
                            }
                            
                            // Customize underlying hitbox
                            if (kvp.Value.GlobalHitbox.Enabled)
                            {
                                ImGui.Indent(40);
                                ImGui.Text("Global Hitbox Settings");
                                DrawHitboxConfigurator(ref kvp.Value.GlobalHitbox, label, false, true);
                                ImGui.Unindent(40);
                                ImGui.NewLine();
                                ImGui.NewLine();
                            }
                        }
                    }

                    
                    // old system
                    if (false)
                    {
                        var alreadyRegistered = _drawObjectKinds.ContainsKey((ObjectKind) _currentObjectKind + 1);
                    if (ImGui.Button( alreadyRegistered ? 
                        FontAwesomeIcon.Times.ToIconString() : FontAwesomeIcon.Plus.ToIconString()
                    ))
                    {
                        if (!alreadyRegistered) _drawObjectKinds.Add((ObjectKind) _currentObjectKind + 1, true);
                        else _drawObjectKinds.Remove((ObjectKind) _currentObjectKind + 1);
                    }
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("..." +
                                   (!alreadyRegistered?"Add":"Remove") + 
                                   $" {_objectKinds[(int) _currentObjectKind]} objects " + 
                                   (!alreadyRegistered?"to":"from") + 
                                   $" the list of drawn hitboxes.");
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

                        if (ImGui.Checkbox($"##toggleFiltering{label}", ref _objectKindFiltering[objKind.Key][0]) 
                            && (objKind.Key == _interactiveSelectorObjKind))
                            _showInteractiveSelector = false;
                            
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
                                    
                                // id filtering
                                if (_objectKindFiltering[objKind.Key][1])
                                {
                                    ImGui.Text($"Only show {label}s matching the IDs:");
                                    ImGui.SetNextItemWidth(150f);
                                    ImGui.InputInt($"##filter{label}", ref _currentIdFilters[_currentObjectKind]);
                                        
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString());
                                    ImGui.PopFont();
                                    if (ImGui.IsItemClicked())
                                    {
                                        AddFilterTo(objKind.Key, _currentIdFilters[_currentObjectKind]);
                                    }
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text("Add ID to whitelist.");
                                        ImGui.EndTooltip();
                                    }
                                }
                                // name filtering
                                else
                                {
                                    ImGui.Text($"Only show {label}s matching the names:");
                                    ImGui.SetNextItemWidth(150f);
                                    var old = _currentFilters[(int)objKind.Key];
                                    if (ImGui.InputText($"##filter{objKind.GetHashCode()}{label}",
                                        ref _currentFilters[(int)objKind.Key], 31))
                                    {
                                        var splitName = _currentFilters[(int) objKind.Key].Split(' ');
                                        if (objKind.Key == ObjectKind.Player && splitName.Length > 2)
                                            _currentFilters[(int)objKind.Key] = $"{splitName[0]} {splitName[1]}";
                                    }

                                    ;
                                    ImGui.SameLine();
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString());
                                    ImGui.PopFont();
                                    if (ImGui.IsItemClicked())
                                    {
                                        AddFilterTo(objKind.Key, _currentFilters[(int) objKind.Key]);
                                    }
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text("Add name to whitelist.");
                                        ImGui.EndTooltip();
                                    }
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

                                // id filters list
                                if (_objectKindFiltering[objKind.Key][1])
                                {
                                    foreach (var filter in _objectKindIdFilters[objKind.Key])
                                    {
                                        if (ImGui.Button($"  x  ##{filter}"))
                                        {
                                            RemoveFilterFrom(objKind.Key, filter);
                                            break;
                                        }; 
                                        ImGui.SameLine();
                                        var enabled = _idFilterColorEnabled[label+filter];
                                        if (ImGui.Checkbox($"##customColor{label+filter}", ref enabled))
                                        {
                                            _idFilterColorEnabled[label+filter] = enabled;
                                        }

                                        if (enabled)
                                        {
                                            var col = _idFilterColor[label+filter];
                                            ImGui.SameLine();
                                            if (ImGui.ColorEdit4($"##colorPicker{label+filter}", ref col, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                                                _idFilterColor[label+filter] = col;
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text("ID "+filter);
                                            
                                            
                                    }
                                }
                                // name filters list
                                else
                                {
                                    foreach (var filter in _objectKindStringFilters[objKind.Key])
                                    {
                                        //PluginLog.Log($"[{objKind.Key}] found filter: {filter}");
                                        if (ImGui.Button($"  x  ##{filter}"))
                                        {
                                            RemoveFilterFrom(objKind.Key, filter);
                                            break;
                                        }; 
                                        var enabled = _nameFilterColorEnabled[$"{objKind.Key}{filter}"];
                                        ImGui.SameLine();
                                        if (ImGui.Checkbox($"##customColor{objKind.Key}{filter}", ref enabled))
                                        {
                                            _nameFilterColorEnabled[$"{objKind.Key}{filter}"] = enabled;
                                        }

                                        if (enabled)
                                        {
                                            var col = _nameFilterColor[label + filter];
                                            ImGui.SameLine();
                                            if (ImGui.ColorEdit4($"##colorPicker{label + filter}", ref col, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview))
                                                _nameFilterColor[$"{objKind.Key}{filter}"] = col;
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text(filter);
                                            
                                    }
                                }
                                ImGui.Unindent();
                                ImGui.NewLine();
                                ImGui.TreePop();
                            }
                        }
                        else ImGui.Text("          "+label);
                    }
                    }

                    ImGui.Unindent();
                    ImGui.Unindent(10);
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
                    ImGui.TextDisabled("(?)");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(300f);
                        ImGui.TextWrapped("The distance calculation does not take into\naccount the elevation difference between\nyou and your target, although if you are\n5 or more yalms above your target your\nself-centered moves will not hit your target.");
                        ImGui.EndTooltip();
                    } ImGui.SameLine();
                    var reachable = false;
                    var dist = (float) Math.Round(GetDistanceFromTarget(ref reachable, _currentFromPlayerCenter) ?? -999f, 2);
                    var btnlabel = "No target";
                    ImGui.SameLine();
                    if (dist != -999f)
                    {
                        btnlabel = "" + dist;
                        ImGui.Button(btnlabel);
                        if (reachable)
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
                                   + (reachable?"":"\n>> Target not within height reach of self-centered moves."));
                        ImGui.Text($"Target's hitbox size: {_pluginInterface.ClientState.Targets.CurrentTarget.HitboxRadius}");
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
                        var duplicate = _distances.Count(d => d == _currentDistance);
                        if (duplicate != 0)
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
                        DrawFineTuning(ref _currentFromPlayerCenter, winx);
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
                            DrawFineTuning(ref _aFromPlayerCenter[i], winx);
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
                LoadConfig();
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
                if (!_pluginInterface.ClientState.KeyState[0x10] &&
                    !_pluginInterface.ClientState.KeyState[0x11])
                {
                    ImGui.Text("Reload saved config.");
                    ImGui.TextDisabled("Undo changes since last save.");
                }
                else
                {
                    ImGui.Text("Reset config.");
                }
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

        private unsafe void DrawInteractiveSelector()
        {
            foreach (var actor in _pluginInterface.ClientState.Actors)
            {
                if (actor.ActorId == _pluginInterface.ClientState.LocalPlayer.ActorId || actor.ObjectKind != _interactiveSelectorObjKind || string.IsNullOrEmpty(actor.Name)) continue;
                if (!ObjectFilterLists[actor.ObjectKind].WhitelistEnabled) continue;

                
                // very innacurate window placement but nameHeight as referenced in
                // https://github.com/imchillin/CMTool/blob/9008f0e00efbbf9af8c5d2b06d427b665955f8f6/ConceptMatrix/OffsetSettings.json#L68
                // doesn't seem to be returning anything useful
                if (!_pluginInterface.Framework.Gui.WorldToScreen(
                    new Vector3(actor.Position.X + *(float*) (actor.Address + 0x180),
                        actor.Position.Z +  *(float*) (actor.Address + 0x184), 
                        actor.Position.Y + *(float*) (actor.Address + 0x188)), out var pos))
                    continue;
                ImGui.SetNextWindowPos(new Num.Vector2(pos.X-(int)(ImGui.CalcTextSize(actor.Name).X)+20, pos.Y-40));
                ImGui.Begin($"##selector{actor.Address}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                                                          ImGuiWindowFlags.AlwaysAutoResize |
                                                          ImGuiWindowFlags.NoFocusOnAppearing);
                ImGui.Text(actor.Name); 
                ImGui.SameLine();

                var inWhitelist = ObjectFilterLists[actor.ObjectKind].Contains(actor.Name);
                var icon = FontAwesomeIcon.SortAlphaUp.ToIconString();
                var id = -1;
                if (ObjectFilterLists[actor.ObjectKind].FilterableById && _pluginInterface.ClientState.KeyState[0x10]) // if shiftclicking (adding ID)
                {
                    id = actor is EventObj eobj ? eobj.DataId : actor is BattleNpc bnpc ? bnpc.DataId : -1; 
                    inWhitelist = ObjectFilterLists[actor.ObjectKind].Contains(id);
                    icon = FontAwesomeIcon.SortNumericUp.ToIconString();
                }

                if (inWhitelist)
                    icon = FontAwesomeIcon.Times.ToIconString();

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Button($"{icon}##addToWhitelist{actor.Address}");
                ImGui.PopFont();
                if (ImGui.IsItemClicked()) // button click
                {
                    if (ObjectFilterLists[actor.ObjectKind].FilterableById && _pluginInterface.ClientState.KeyState[0x10]) // modifying id
                    {
                        if (inWhitelist)
                        {
                            if (!ObjectFilterLists[actor.ObjectKind].Remove(id)) 
                                PluginLog.Debug($"[!] Could not remove {id} from {actor.ObjectKind} whitelist");
                            break;
                        }

                        if (!ObjectFilterLists[actor.ObjectKind].Add(id))
                            PluginLog.Debug($"[!] Could not add {id} to {actor.ObjectKind} whitelist");
                        break;
                    }
                    if (inWhitelist)
                    {
                        if (!ObjectFilterLists[actor.ObjectKind].Remove(actor.Name)) 
                            PluginLog.Debug($"[!] Could not remove {actor.Name} from {actor.ObjectKind} whitelist");
                        break;
                    }

                        
                    if (!ObjectFilterLists[actor.ObjectKind].Add(actor.Name))
                    {
                        PluginLog.Log($"[!] Could not add {actor.Name} to {actor.ObjectKind} whitelist");    
                    };
                    break;
                }
                if (ImGui.IsItemHovered()) // button tooltip 
                {
                    ImGui.BeginTooltip();
                    if (ObjectFilterLists[actor.ObjectKind].FilterableById && _pluginInterface.ClientState.KeyState[0x10])
                        ImGui.Text(inWhitelist
                            ? "Click to remove ID from whitelist."
                            : "Click to add ID to whitelist.");
                    else
                        ImGui.Text(inWhitelist
                            ? "Click to remove Name from whitelist."
                            : "Click to add Name to whitelist.");
                    if (ObjectFilterLists[actor.ObjectKind].FilterableById && !_pluginInterface.ClientState.KeyState[0x10])
                        ImGui.TextDisabled("Hold Shift to modify ID whitelist instead.");
                    ImGui.EndTooltip();
                }
                //ImGui.Text("id: "+id);
                ImGui.End();
            }
        }

        private void DrawWorldNearbyTargets()
        {
            foreach (var actor in _pluginInterface.ClientState.Actors)
            {
                if (actor == null 
                    || actor.ActorId == _pluginInterface.ClientState.LocalPlayer.ActorId 
                    || actor.ActorId == _pluginInterface.ClientState.Targets.CurrentTarget?.ActorId
                    || !_drawNearbyTargets ) continue;
                //if (_drawNearbyTargets && !_drawObjectKinds.Keys.Contains(actor.ObjectKind)) continue;
                //if (!_drawObjectKinds[actor.ObjectKind]) continue;
                
                //var doOverrideCol = false;
                //var overrideCol = Num.Vector4.Zero;
                
                // if objkind is in filterlist
                if (ObjectFilterLists.ContainsKey(actor.ObjectKind))
                {
                    bool foundFilterBool = false;
                    if (ObjectFilterLists[actor.ObjectKind].WhitelistEnabled) {
                        
                        if (ObjectFilterLists[actor.ObjectKind].FilterableById)
                        {
                            var id = actor is EventObj eobj ? eobj.DataId : actor is BattleNpc bnpc ? bnpc.DataId:-1;
                            var foundIdFilter = ObjectFilterLists[actor.ObjectKind].Filters
                                .Find(filter => filter.IntFilter == id);
                            if (foundIdFilter != null) foundFilterBool = true; 
                            if (foundFilterBool && foundIdFilter.AssociatedHitbox.Enabled)
                                DrawHitboxWorld(actor, foundIdFilter.AssociatedHitbox);
                        }
                        if (!ObjectFilterLists[actor.ObjectKind].FilterableById || !foundFilterBool) {
                            var foundNameFilter = ObjectFilterLists[actor.ObjectKind].Filters
                                .Find(filter => filter.StrFilter == actor.Name);
                            if (foundNameFilter != null) foundFilterBool = true;
                            if (foundFilterBool && foundNameFilter.AssociatedHitbox.Enabled)
                            {
                                DrawHitboxWorld(actor, foundNameFilter.AssociatedHitbox);
                                // for now in the above function the management of the color only happens if player has a target (oversight)
                                // // ok well i dont remember if its still true because i forget when i wrote that comment oops
                            }
                        }
                    }
                    if (!foundFilterBool) {
                        DrawHitboxWorld(actor, ObjectFilterLists[actor.ObjectKind].GlobalHitbox);
                    }
                    
                }
                /*if (_objectKindFiltering[actor.ObjectKind][0]) // if the objectKind is set to be drawn
                {
                    if (_objectKindFiltering[actor.ObjectKind][1]) // if objectKind is filterable by id
                    {
                        var id = -1;
                        switch (actor) // retrieve special id fields if possible for specific objectKind
                        {
                            case BattleNpc obj:
                                id = obj.DataId;
                                break;
                            case EventObj obj:
                                id = obj.DataId;
                                break;
                        }

                        if (_pluginInterface.ClientState.Targets.CurrentTarget != null
                            && (actor is BattleNpc bnpc ? bnpc.DataId :
                                actor is EventObj eobj ? eobj.DataId : -1) == id)
                            continue;
                        
                        if (!_objectKindIdFilters[actor.ObjectKind].Contains(id)) // if id isn't in drawing/white list
                            continue;
                        if (id!=-1 && _idFilterColorEnabled[$"{actor.ObjectKind}{id}"]) // if id drawing has custom color enabled
                        {
                            doOverrideCol = true;
                            overrideCol = _idFilterColor[$"{actor.ObjectKind}{id}"]; // id's associated color
                        }

                    }
                    else // objectKind will be filtered by name
                    {
                        if (!_objectKindStringFilters[actor.ObjectKind].Contains(actor.Name)) // if name isn't in drawing/white list
                            continue;
                        if (_nameFilterColorEnabled[$"{actor.ObjectKind}{actor.Name}"]) // if name drawing has custom color enabled
                        {
                            doOverrideCol = true;
                            overrideCol = _nameFilterColor[$"{actor.ObjectKind}{actor.Name}"]; // name's associated color
                        }
                    }
                }
                var appliedColor = _targetCol; // TODO: add distance based coloring here
                if (doOverrideCol) appliedColor = overrideCol;
                DrawHitboxWorld(actor, appliedColor, !doOverrideCol, _targetHitboxSize, _showTargetRing, false, true);*/
            }
        }
        
        private void DrawWindow()
        {
            try
            {
                if (_showInteractiveSelector)
                    DrawInteractiveSelector();
                if (_config)
                    DrawConfig();
                
                // instance / combat checks 
                if (_pluginInterface.ClientState.LocalPlayer == null 
                    || (_combat && !_pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat])
                    || (_instance && !_pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty])) return;

                // nearby hitboxes
                if (_drawNearbyTargets) 
                    DrawWorldNearbyTargets();

                // self hitbox & ring
                DrawHitboxWorld(_pluginInterface.ClientState.LocalPlayer, _configuration.SelfHitbox, 
                    _pluginInterface.ClientState.Targets.CurrentTarget ?? _pluginInterface.ClientState.Targets.FocusTarget ?? _pluginInterface.ClientState.Targets.SoftTarget);
                
                // target hitbox & ring
                if (_pluginInterface.ClientState.Targets.CurrentTarget != null)
                    DrawHitboxWorld(_pluginInterface.ClientState.Targets.CurrentTarget, _configuration.TargetHitbox);
            }
            catch (Exception e)
            {
                PluginLog.Log(e.Message);
            }
        }

        private void DrawFineTuning(ref bool currentFromPlayerCenter, float winX)
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
            _configuration.IsColManaged = _isColManaged; // whether or not the plugin manages the selfhitbox color
            _configuration.Distances = _distances; // distances component of the custom ranges
            _configuration.DotColors = _dotColors; // color component of the custom ranges
            _configuration.ColorEnabled = _colorEnabled; // individual toggles for custom ranges
            _configuration.FromPlayerCenter = _fromPlayerCenter; // whether to calculate distance from the player center for a given range
            _configuration.DrawNearbyTargets = _drawNearbyTargets;
            _configuration.ShowKofi = _showKofi;
            
            _configuration.drawObjectKinds = _drawObjectKinds;
            _configuration.ObjectKindFilters = _objectKindStringFilters;
            _configuration.ObjectKindIdFilter = _objectKindIdFilters; 
            _configuration.ObjectKindFiltering = _objectKindFiltering; 
            _configuration._currentObjectKind = _currentObjectKind;
            _configuration.ObjectKinds = _objectKinds;
            _configuration.currentFilters = _currentFilters;
            _configuration.currentIdFilters = _currentIdFilters;
            _configuration.NameFilterColor = _nameFilterColor;
            _configuration.IdFilterColor = _idFilterColor;
            _configuration.IdFilterColorEnabled = _idFilterColorEnabled;
            _configuration.NameFilterColorEnabled = _nameFilterColorEnabled;

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

        private void DrawHitboxWorld(Actor actor, Num.Vector4 colour, bool manageColor, float size, bool edgeEnabled, bool drawLine=false, bool drawDistance=false)
        {
            if (!_pluginInterface.Framework.Gui.WorldToScreen(new SharpDX.Vector3(actor.Position.X, actor.Position.Z, actor.Position.Y), out var pos)) { return; }
            var appliedColor = ImGui.GetColorU32(colour);
            if (manageColor)
                appliedColor = ImGui.GetColorU32(GetColorFromDistance(manageColor, actor));
            /*{
                var smallest = 999f;
                var smallestidx = -1;
                for (int i = 0; i < _distances.Count; i++)
                {
                    if (!_colorEnabled[i]) continue;
                    var distance = GetDistanceFromTarget(out var reachable, _fromPlayerCenter[i]);
                    if (!(distance ?? -1f).Equals(-1f) && reachable)
                    {
                        if (distance <= _distances[i] && distance < smallest)
                        {
                            smallest = _distances[i];
                            smallestidx = i;
                        }
                    }
                }
                if (smallestidx > -1) appliedColor = ImGui.GetColorU32(_dotColors[smallestidx]);
            }*/
            
            ImGui.GetBackgroundDrawList().AddCircleFilled(
                new Num.Vector2(pos.X, pos.Y),
                size,
                appliedColor,
                100);
            if (edgeEnabled)
            {
                ImGui.GetBackgroundDrawList().AddCircle(
                    new Num.Vector2(pos.X, pos.Y),
                    size+0.2f,
                    ImGui.GetColorU32(_col2),
                    100);
            }

            if (drawDistance)
            {
                var dist = GetDistanceFrom( actor, true);
                ImGui.GetBackgroundDrawList().AddText(new Num.Vector2(pos.X, pos.Y+10), ImGui.GetColorU32(_targetCol), $"{dist??0}y" );
            }
        }


        private Num.Vector4 GetColorFromDistance(bool isColManaged, Actor target = null)
        {
            Num.Vector4 appliedColor = new Num.Vector4(-1,-1,-1,-1);
            if (!isColManaged) return appliedColor;
            if (target == null) target = _pluginInterface.ClientState.Targets.CurrentTarget;
            var smallest = 999f;
            var smallestidx = -1;
            var reachable = false;
            for (int i = 0; i < _distances.Count; i++)
            {
                if (!_colorEnabled[i]) continue;
                var distance = GetDistanceFrom(target, ref reachable, _fromPlayerCenter[i]);
                if (!(distance ?? -1f).Equals(-1f) && reachable)
                {
                    if (distance <= _distances[i] && distance < smallest)
                    {
                        smallest = _distances[i];
                        smallestidx = i;
                    }
                }
            }
            if (smallestidx > -1)
            {
                appliedColor = _dotColors[smallestidx];
            }
            return appliedColor;
        }
        private void DrawHitboxWorld(Actor actor, Hitbox hb, Actor targetActor = null)
        {
            if (!hb.Enabled || !_pluginInterface.Framework.Gui.WorldToScreen(actor.Position, out var pos)) { return; }
            if (targetActor == null)
                targetActor = actor;
            
            var vecColor = GetColorFromDistance(hb.ColorManage, targetActor);
            var appliedColor = ImGui.GetColorU32(vecColor.W.Equals(-1.0f) ? hb.Color : vecColor);
            
            if (hb.Ring.Enabled)
            {
                DrawRingWorld(actor, hb.Ring);
            }
            if (hb.DrawLine)
            {
                if (!_pluginInterface.Framework.Gui.WorldToScreen(_pluginInterface.ClientState.LocalPlayer.Position, out var selfPos)) { return; }
                ImGui.GetBackgroundDrawList().AddLine(new Num.Vector2(selfPos.X, selfPos.Y), new Num.Vector2(pos.X, pos.Y), hb.ColorManage ? appliedColor :ImGui.GetColorU32(hb.Color));
            }

            ImGui.GetBackgroundDrawList().AddCircleFilled(
                new Num.Vector2(pos.X, pos.Y),
                hb.Size,
                hb.ColorManage ? appliedColor :ImGui.GetColorU32(hb.Color),
                100);
            if (hb.CircleEnabled)
            {
                ImGui.GetBackgroundDrawList().AddCircle(
                    new Num.Vector2(pos.X, pos.Y),
                    hb.Size+0.2f,
                    ImGui.GetColorU32(hb.CircleColor),
                    100);
            }
            
            if (hb.DrawDistance)
            {
                var dist = GetDistanceFrom(targetActor);
                if (true) // background rectangle for visibility
                {
                    ImGui.GetBackgroundDrawList()
                        .AddRectFilled(
                            new Num.Vector2(pos.X-5, pos.Y+10),
                            new Num.Vector2(pos.X, pos.Y+10) + ImGui.CalcTextSize($"{dist??0}y") + new Num.Vector2(5, 0), 
                            ImGui.GetColorU32(Num.Vector4.UnitW));
                }
                ImGui.GetBackgroundDrawList().AddText(new Num.Vector2(pos.X, pos.Y+10), hb.ColorManage ? appliedColor :ImGui.GetColorU32(hb.Color), $"{dist??0}y" );
            }
        }

        private void DrawRingWorld(Actor actor, OuterRing or, Actor targetactor = null)
        {
            DrawRingWorld(actor, or.Radius, or.Segments, or.Thickness, ImGui.GetColorU32(or.ColorManage?GetColorFromDistance(or.ColorManage, targetactor):or.Color));
        }
        private void DrawRingWorld(Actor actor, float radius, int numSegments, float thicc, uint colour)
        {
            var seg = numSegments / 2;
            for (var i = 0; i <= numSegments; i++)
            {
                _pluginInterface.Framework.Gui.WorldToScreen(new SharpDX.Vector3(actor.Position.X + (radius * (float)Math.Sin((Math.PI / seg) * i)), actor.Position.Z, actor.Position.Y + (radius * (float)Math.Cos((Math.PI / seg) * i))), out SharpDX.Vector2 pos);
                ImGui.GetBackgroundDrawList().PathLineTo(new Num.Vector2(pos.X, pos.Y));
            }
            ImGui.GetBackgroundDrawList().PathStroke(colour, ImDrawFlags.Closed, thicc);
        }

        
    }
}
