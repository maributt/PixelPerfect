using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using SharpDX;
using Num = System.Numerics;

namespace PixelPerfectPlus
{
    public class PluginUI
    {
        private DalamudPluginInterface _pluginInterface;
        private Config _configuration;

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

        private int FindRangeForDistance(float? distance)
        {
            float smallest = 999;
            var smallestIdx = -1;
            if (distance == null) return smallestIdx;
            for (var i = 0; i < _distances.Count; i++)
            {
                if (_distances[i] != -999 && _colorEnabled[i])
                {
                    if (_distances[i] < smallest && _distances[i] > distance)
                    {
                        smallest = _distances[i];
                        smallestIdx = i;
                    }
                }
            }
            return smallestIdx;
        }

        private float? GetDistanceFrom(out bool reachable, Actor actor, bool fromPlayerCenter)
        {
            var dist = GetDistanceFromTarget(out var withinReach,fromPlayerCenter,  actor);
            reachable = withinReach;
            return dist;
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
        
        public PluginUI(DalamudPluginInterface pI)
        {
            _pluginInterface = pI;
            LoadConfig();
        }

        public void Draw()
        {
            
        }
    }
}