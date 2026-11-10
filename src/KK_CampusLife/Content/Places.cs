using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>Recognises school locations from the (Japanese) map name.</summary>
    internal static class Places
    {
        public sealed class Place
        {
            public string Id;
            public string Name;
            public string[] Keywords;
        }

        public static readonly List<Place> All = new List<Place>
        {
            new Place { Id = "library",   Name = "Library",      Keywords = new[] { "図書", "Library" } },
            new Place { Id = "gym",       Name = "Gym",          Keywords = new[] { "体育館", "Gym" } },
            new Place { Id = "pool",      Name = "Pool",         Keywords = new[] { "プール", "Pool" } },
            new Place { Id = "rooftop",   Name = "Rooftop",      Keywords = new[] { "屋上", "Roof" } },
            new Place { Id = "infirmary", Name = "Infirmary",    Keywords = new[] { "保健室", "Infirmary" } },
            new Place { Id = "cafeteria", Name = "Cafeteria",    Keywords = new[] { "食堂", "学食", "購買", "Cafeteria" } },
            new Place { Id = "clubroom",  Name = "Club Room",    Keywords = new[] { "部室", "Club" } },
            new Place { Id = "staffroom", Name = "Faculty Room",   Keywords = new[] { "職員室", "Staff", "Faculty" } },
            new Place { Id = "classroom", Name = "Lecture Hall", Keywords = new[] { "教室", "Classroom", "Lecture" } },
            new Place { Id = "courtyard", Name = "Courtyard",    Keywords = new[] { "中庭", "Courtyard" } },
            new Place { Id = "field",     Name = "Campus Field", Keywords = new[] { "グラウンド", "校庭", "野原", "運動場", "Field", "Ground" } },
            // The game has no park map: "park" is the backyard behind the buildings (裏庭), where the stray cats are.
            new Place { Id = "park",      Name = "Backyard",     Keywords = new[] { "裏庭", "公園", "Park", "Backyard" } },
        };

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        public static string IdForMapName(string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return "";
            string id;
            if (Cache.TryGetValue(mapName, out id)) return id;
            var p = All.FirstOrDefault(x => x.Keywords.Any(mapName.Contains));
            id = p != null ? p.Id : "";
            Cache[mapName] = id;
            return id;
        }

        public static string IdForMap(int mapNo) { return IdForMapName(G.MapName(mapNo)); }

        private static readonly Dictionary<string, int> MapCache = new Dictionary<string, int>();

        /// <summary>Map to warp to for a place (the player's own classroom for "classroom"), or -1.</summary>
        public static int MapFor(string placeId)
        {
            var act = G.Act;
            if (act == null || act.Map == null || act.Map.infoDic == null || string.IsNullOrEmpty(placeId)) return -1;
            if (placeId == "classroom")
            {
                try { return act.Cycle.GetClassRoomMapNo(G.Player.schoolClass); } catch { }
            }
            int no;
            if (MapCache.TryGetValue(placeId, out no)) return no;
            no = -1;
            foreach (var kv in act.Map.infoDic.OrderBy(k => k.Key))
                if (kv.Value != null && IdForMapName(kv.Value.MapName) == placeId) { no = kv.Key; break; }
            if (no >= 0) MapCache[placeId] = no;
            return no;
        }

        public static string CurrentId { get { return IdForMap(G.PlayerMapNo); } }

        public static string Name(string id)
        {
            var p = All.FirstOrDefault(x => x.Id == id);
            return p != null ? p.Name : (string.IsNullOrEmpty(id) ? "somewhere" : id);
        }

        /// <summary>English name of any map number, for showing where a girl is.</summary>
        public static string MapLabel(int mapNo)
        {
            if (mapNo < 0) return "unknown";
            var id = IdForMap(mapNo);
            if (!string.IsNullOrEmpty(id)) return Name(id);
            var raw = G.MapName(mapNo);
            string tl;
            try
            {
                if (KKAPI.Utilities.TranslationHelper.TryTranslate(raw, out tl) && !string.IsNullOrEmpty(tl) && tl.IndexOf("{{", System.StringComparison.Ordinal) < 0) return tl;
            }
            catch { }
            return string.IsNullOrEmpty(raw) ? "map " + mapNo : raw;
        }
    }
}
