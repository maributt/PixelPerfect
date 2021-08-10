using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Actors;
using ImGuiNET;

namespace PixelPerfectPlus
{
    public class Config: IPluginConfiguration
    {
        public int Version { get; set; } = 2;

        public PixelPerfectPlus.Hitbox SelfHitbox = new PixelPerfectPlus.Hitbox(
            new Vector4(1f, 1f, 1f, 0.75f),
            new Vector4(0f, 0f, 0f, 0.75f), 
            true, 
            2f, 
            true,
            new PixelPerfectPlus.OuterRing(),
            true
        );

        public PixelPerfectPlus.Hitbox TargetHitbox = new PixelPerfectPlus.Hitbox(
            new Vector4(1f, 0f, 0f, 0.75f),
            new Vector4(0f, 0f, 0f, 0.75f),
            false,
            2f,
            true,
            new PixelPerfectPlus.OuterRing(),
            false
        );
        public bool Enabled { get; set; } = true;
        public bool Combat { get; set; } = false;
        public bool Circle { get; set; }
        public bool Instance { get; set; }
        public Vector4 Col { get; set; } = new Vector4(1f, 1f, 1f, 0.5882f);
        public Vector4 Col2 { get; set; } = new Vector4(0f, 0f, 0f, 1f);
        public Vector4 ColRing { get; set; } = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
        public int Segments { get; set; } = 100;
        public float Thickness { get; set; } = 10f;
        public bool Ring { get; set; }
        public float Radius { get; set; } = 2f;
        public bool IsColManaged { get; set; }
        public List<float> Distances { get; set; }
        public List<Vector4> DotColors { get; set; }
        public List<bool> ColorEnabled { get; set; }
        public float SelfHitboxSize { get; set; } = 2f;
        public bool TargetEnabled { get; set; } = true;
        public float TargetHitboxSize { get; set; } = 2f;
        public Vector4 TargetHitboxCol { get; set; } = new Vector4(1f, 0f, 0f, 0.5882f);
        public bool TargetHitboxRing { get; set; } 
        public bool ShowRingConfig { get; set; }
        public List<bool> FromPlayerCenter { get; set; }
        public Vector4 ColTargetRing { get; set; } = new Vector4(0f, 0f, 0f, 1f);
        public bool DrawNearbyTargets { get; set; }
        public bool DisplayMobs { get; set; }
        public bool DisplayPlayers { get; set; }
        public bool CombatOtherPlayers { get; set; }
        public bool InstanceOtherPlayers { get; set; }
        public bool CombatMobs { get; set; }
        public bool InstanceMobs { get; set; }
        public bool ShowKofi { get; set; }
        public Dictionary<string, Vector4> NameFilterColor { get; set; } = new Dictionary<string, Vector4>();
        public Dictionary<string, Vector4> IdFilterColor { get; set; } = new Dictionary<string, Vector4>();
        public Dictionary<string, bool> IdFilterColorEnabled { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> NameFilterColorEnabled { get; set; } = new Dictionary<string, bool>();
        

        public Dictionary<ObjectKind, bool> drawObjectKinds = new Dictionary<ObjectKind, bool>();
        public Dictionary<ObjectKind, List<string>> ObjectKindFilters = new Dictionary<ObjectKind, List<string>>();
        public Dictionary<ObjectKind, List<int>> ObjectKindIdFilter = new Dictionary<ObjectKind, List<int>>();
        public Dictionary<ObjectKind, bool[]> ObjectKindFiltering = new Dictionary<ObjectKind, bool[]>();  
        public int _currentObjectKind = 0;
        public string[] ObjectKinds = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        public string[] currentFilters = new string[Enum.GetValues(typeof (ObjectKind)).Length-1];
        public int[] currentIdFilters = new int[Enum.GetValues(typeof (ObjectKind)).Length-1];
        
        public float longestStrImGuiLen = 0;

        public Config()
        {
            for (var i = 1; i < Enum.GetValues(typeof (ObjectKind)).Length; i++)
            {
                ObjectKinds[i-1] = Enum.GetName(typeof(ObjectKind), i);
                longestStrImGuiLen = ImGui.CalcTextSize(ObjectKinds[i - 1]).X > longestStrImGuiLen
                    ? ImGui.CalcTextSize(ObjectKinds[i - 1]).X : longestStrImGuiLen;
                ObjectKindFilters.Add((ObjectKind)i, new List<string>());
                ObjectKindIdFilter.Add((ObjectKind) i, new List<int>());
                ObjectKindFiltering.Add((ObjectKind)i, new bool[2] {false, false});
                currentFilters[i-1] = "";
                currentIdFilters[i-1] = 0;
            }
        }
    }
}