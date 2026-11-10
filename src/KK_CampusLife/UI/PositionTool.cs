using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Hand-placed spots, kept in BepInEx\config\joojkk.campuslife.spots.txt (one per line, editable by hand):
    ///   store &lt;map&gt; x y z        where the school store icon stands
    ///   warp &lt;map&gt; x y z yaw     where the menu's "Go there" buttons put you on that map
    ///   act:&lt;activity&gt; &lt;map&gt; x y z yaw [animation]   where an activity is done (ActivitySpots); animation = game action number
    /// </summary>
    internal static class Spots
    {
        internal struct Spot { public int Map; public Vector3 Pos; public float Yaw; public int Anim; }

        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, "joojkk.campuslife.spots.txt");
        private static Dictionary<string, Spot> _spots;
        private static Dictionary<string, Spot> _defaults;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static string Id(string kind, int map) { return kind == "store" ? "store" : kind + ":" + map; }

        private static Dictionary<string, Spot> All
        {
            get
            {
                if (_spots != null) return _spots;
                _spots = new Dictionary<string, Spot>();
                try
                {
                    if (!File.Exists(FilePath)) return _spots;
                    Parse(File.ReadAllLines(FilePath), _spots);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogWarning("Could not read " + FilePath + ": " + e.Message); }
                return _spots;
            }
        }

        /// over them.</summary>
        private static Dictionary<string, Spot> Defaults
        {
            get
            {
                if (_defaults != null) return _defaults;
                _defaults = new Dictionary<string, Spot>();
                try
                {
                    using (var st = typeof(Spots).Assembly.GetManifestResourceStream("CampusLife.spots_default.txt"))
                    {
                        if (st == null) return _defaults;
                        using (var rd = new StreamReader(st))
                            Parse(rd.ReadToEnd().Split('\n'), _defaults);
                    }
                }
                catch (Exception e) { CampusLifePlugin.Log.LogWarning("Could not read the mod's spots: " + e.Message); }
                return _defaults;
            }
        }

        private static void Parse(IEnumerable<string> lines, Dictionary<string, Spot> into)
        {
            foreach (var raw in lines)
            {
                var line = raw.Split('#')[0].Trim();
                var f = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 5) continue;
                int map;
                float x, y, z, yaw = 0f;
                int anim = -1;
                if (!int.TryParse(f[1], NumberStyles.Integer, Inv, out map) || !Num(f[2], out x) || !Num(f[3], out y) || !Num(f[4], out z)) continue;
                if (f.Length > 5) Num(f[5], out yaw);
                if (f.Length > 6 && !int.TryParse(f[6], NumberStyles.Integer, Inv, out anim)) anim = -1;
                into[Id(f[0].ToLowerInvariant(), map)] = new Spot { Map = map, Pos = new Vector3(x, y, z), Yaw = yaw, Anim = anim };
            }
        }

        private static bool Num(string s, out float v) { return float.TryParse(s, NumberStyles.Float, Inv, out v); }

        public static bool TryGet(string kind, int map, out Spot spot)
        {
            var id = Id(kind, map);
            if (All.TryGetValue(id, out spot) && (kind == "store" || spot.Map == map)) return true;
            return Defaults.TryGetValue(id, out spot) && (kind == "store" || spot.Map == map);
        }

        /// <summary>Is this spot in his own file (not the mod's default)?</summary>
        public static bool IsUsers(string kind, int map) { return All.ContainsKey(Id(kind, map)); }

        /// <summary>Is there a mod default for this spot?</summary>
        public static bool HasDefault(string kind, int map) { return Defaults.ContainsKey(Id(kind, map)); }

        public static bool TryGetStore(out Spot spot) { return All.TryGetValue("store", out spot) || Defaults.TryGetValue("store", out spot); }

        public static void Set(string kind, int map, Vector3 pos, float yaw, int anim = -1)
        {
            All[Id(kind, map)] = new Spot { Map = map, Pos = pos, Yaw = yaw, Anim = anim };
            Save();
        }

        public static bool Remove(string kind, int map)
        {
            if (!All.Remove(Id(kind, map))) return false;
            Save();
            return true;
        }

        private static void Save()
        {
            var lines = new List<string>
            {
                "# Campus Life spots (Joojkk). One per line: <kind> <map> <x> <y> <z> [yaw]",
                "#   store: where the university store icon stands.   warp: where \"Go there\" puts you on that map.",
                "#   act:<activity>: where that activity is done on that map. Optional 7th number: the game's action animation (0 read, 2 loosen up, 3 squats, 4 stretch, 5 shadow boxing, 6 'research' magazine).",
                "# Easiest way to change them: turn on \"11. Position tools > Show position readout\" and use its keys in game.",
            };
            foreach (var kv in All.OrderBy(k => k.Key))
            {
                var s = kv.Value;
                var kind = kv.Key == "store" ? "store" : kv.Key.Substring(0, kv.Key.LastIndexOf(':'));
                lines.Add(string.Format(Inv, "{0} {1} {2:0.00} {3:0.00} {4:0.00} {5:0}{6}   # {7}", kind, s.Map, s.Pos.x, s.Pos.y, s.Pos.z, s.Yaw,
                    s.Anim >= 0 ? " " + s.Anim.ToString(Inv) : "", Places.MapLabel(s.Map)));
            }
            try { File.WriteAllLines(FilePath, lines.ToArray()); }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Could not write " + FilePath + ": " + e.Message); }
        }
    }

    /// <summary>
    /// Coordinate readout for placing things: shows the map and the player's position and facing, and with its keys
    /// saves the current spot as the warp point of this map or moves the school store here.
    /// </summary>
    internal static class PositionTool
    {
        private static GUIStyle _style;
        private static string _last = "";

        private static bool Active { get { return Cfg.PositionReadout.Value && G.Act != null && G.Act.Player != null; } }

        public static void Update()
        {
            if (!Active || !G.IsFreeRoaming) return;
            var p = G.Act.Player;
            var map = G.PlayerMapNo;
            if (Cfg.SaveWarpSpotKey.Value.IsDown())
            {
                Spots.Set("warp", map, p.position, p.rotation.eulerAngles.y);
                Copy();
                Toasts.Show("\"Go there\" for " + Places.MapLabel(map) + " now brings you to this spot.");
            }
            else if (Cfg.MoveStoreKey.Value.IsDown())
            {
                Spots.Set("store", map, p.position + p.rotation * Vector3.forward * 0.6f, 0f);
                CafeteriaStore.Replace();
                Copy();
                Toasts.Show("Campus store moved here (" + Places.MapLabel(map) + ").");
            }
        }

        private static string Text()
        {
            var p = G.Act.Player;
            var pos = p.position;
            var map = G.PlayerMapNo;
            return string.Format(CultureInfo.InvariantCulture, "map {0} {1}   x {2:0.00}  y {3:0.00}  z {4:0.00}   facing {5:0}°", map, Places.MapLabel(map), pos.x, pos.y, pos.z, p.rotation.eulerAngles.y);
        }

        private static void Copy()
        {
            _last = Text();
            GUIUtility.systemCopyBuffer = _last;
            CampusLifePlugin.Log.LogInfo("Position: " + _last);
        }

        public static void OnGUI()
        {
            if (!Active) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = false };
                _style.normal.textColor = Color.white;
            }
            var help = Cfg.SaveWarpSpotKey.Value + ": warp spot for this map    " + Cfg.MoveStoreKey.Value + ": move the store here";
            GUI.Box(new Rect(10f, 10f, 640f, 50f), Text() + "\n" + help, _style);
        }
    }
}
