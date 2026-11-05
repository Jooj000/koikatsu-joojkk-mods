using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// controlador simples (tela de plugin mesmo), em que eu abro por atalho, e aparecem as atividades do mapa atual, e eu
    /// defino a posição de onde eu estou, ou a um corpo de distância na minha frente"). Alt+F7 opens and closes it. Each
    /// activity of this map has "Here" (where you stand, facing your way), "1 body ahead" (one body length in front of you)
    /// and "Default" (back to the game's point). The spot is kept in BepInEx\config\joojkk.campuslife.spots.txt (the same
    /// file as the Campus Life menu's "Spot here") and the map's icons move at once. Below the list, the loading screen's
    /// photo of this map: he stands where he likes and takes it (MapShots), or deletes it.
    /// </summary>
    internal static class SpotEditor
    {
        private const int WindowId = 0x4A4B5350; // "JKSP"
        private const float BodyLength = 0.75f;
        private static bool _open;
        private static Rect _rect = new Rect(40f, 120f, 560f, 420f);
        private static Vector2 _scroll;
        private static GUIStyle _small;
        private static string _photoMsg;

        public static bool IsOpen { get { return _open; } }

        public static void Update()
        {
            if (!Cfg.SpotEditorKey.Value.IsDown()) return;
            Toggle();
        }

        public static void Toggle()
        {
            _open = !_open && G.Act != null && G.Act.Player != null;
            _photoMsg = null;
            CampusLifePlugin.Log.LogInfo("Spot editor " + (_open ? "open on " + Places.MapLabel(G.PlayerMapNo) : "closed"));
        }

        public static void Close() { _open = false; }

        /// <summary>The activities done on the map the player is on.</summary>
        internal static List<ActivityDef> Here()
        {
            var place = Places.IdForMap(G.PlayerMapNo);
            if (string.IsNullOrEmpty(place)) return new List<ActivityDef>();
            return Activities.All.Where(a => a.Place == place && !a.GameSleep).ToList();
        }

        /// <summary>Puts an activity where the player stands (ahead = one body length in front of him), on the walkable floor.</summary>
        internal static string Place(ActivityDef a, bool ahead)
        {
            var p = G.Act != null ? G.Act.Player : null;
            if (p == null || a == null) return "no player";
            var pos = p.position;
            if (ahead) pos += p.rotation * Vector3.forward * BodyLength;
            NavMeshHit hit;
            try { if (NavMesh.SamplePosition(pos, out hit, 1f, NavMesh.AllAreas)) pos = hit.position; }
            catch { }
            var yaw = p.rotation.eulerAngles.y;
            ActivitySpots.SetSpot(a, G.PlayerMapNo, pos, yaw);
            return a.Id + " at " + Fmt(pos) + " facing " + Mathf.RoundToInt(yaw) + (ahead ? " (1 body ahead)" : " (here)");
        }

        /// <summary>"Alt+F7" rather than BepInEx's "F7 + LeftAlt".</summary>
        private static string KeyName()
        {
            var k = Cfg.SpotEditorKey.Value;
            var mods = k.Modifiers.Select(m => m.ToString().Replace("Left", "").Replace("Right", "").Replace("Control", "Ctrl")).ToArray();
            return (mods.Length > 0 ? string.Join("+", mods) + "+" : "") + k.MainKey;
        }

        private static string Fmt(Vector3 v) { return string.Format(CultureInfo.InvariantCulture, "{0:0.00}, {1:0.00}, {2:0.00}", v.x, v.y, v.z); }

        public static void OnGUI()
        {
            if (!_open) return;
            if (G.Act == null || G.Act.Player == null) { _open = false; return; }
            _rect.height = 10f;   // the window grows to what it lists
            _rect = GUILayout.Window(WindowId, _rect, Draw, "Activity spots: " + Places.MapLabel(G.PlayerMapNo));
        }

        private static void Draw(int id)
        {
            try
            {
                if (_small == null)
                {
                    _small = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
                    _small.normal.textColor = new Color(0.75f, 0.8f, 0.9f);
                }
                var map = G.PlayerMapNo;
                var acts = Here();
                GUILayout.Label("You: " + Fmt(G.Act.Player.position) + "  facing " + Mathf.RoundToInt(G.Act.Player.rotation.eulerAngles.y) + "°", _small);
                if (acts.Count == 0) GUILayout.Label("No activities on this map.");
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Mathf.Min(320f, 20f + acts.Count * 52f)));
                foreach (var a in acts)
                {
                    var an = ActivitySpots.AnchorOf(a, map);
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(a.Name, GUILayout.Width(190f));
                    if (GUILayout.Button("Here", GUILayout.Width(70f))) Place(a, false);
                    if (GUILayout.Button("1 body ahead", GUILayout.Width(110f))) Place(a, true);
                    var mine = Spots.IsUsers("act:" + a.Id, map);
                    GUI.enabled = mine;
                    if (GUILayout.Button("Default", GUILayout.Width(80f))) ActivitySpots.ResetSpot(a);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    GUILayout.Label(an == null ? "no spot on this map yet" : (mine ? "yours: " : an.Custom ? "mod: " : "game point: ") + Fmt(an.Pos) + "  facing " + Mathf.RoundToInt(an.Rot.eulerAngles.y) + "°", _small);
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
                // atividade". The loading screen's picture of this map is his view, taken here.
                var slot = EventBackdrop.TimeSlot();
                var has = MapShots.Has(map, slot);
                var shipped = !has && MapShots.Shipped(map, slot);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Loading screen photo (" + MapShots.SlotNames[slot] + ")", GUILayout.Width(190f));
                if (GUILayout.Button(has ? "Retake from here" : "Take from here", GUILayout.Width(184f))) _photoMsg = MapShots.TakeHere();
                GUI.enabled = has;
                if (GUILayout.Button("Delete", GUILayout.Width(80f))) { MapShots.Delete(map, slot); _photoMsg = "Photo deleted: the game's picture is used again."; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Label(_photoMsg ?? (has ? "This map has your photo for this time of day." : shipped ? "The mod's photo is used here. Take one to replace it." : "No photo yet: the loading screen blurs the game's small picture."), _small);
                GUILayout.EndVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(KeyName() + " opens and closes this window.", _small);
                if (GUILayout.Button("Close", GUILayout.Width(80f))) _open = false;
                GUILayout.EndHorizontal();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Spot editor: " + e.Message); }
            GUI.DragWindow();
        }

        internal static string Describe()
        {
            var map = G.PlayerMapNo;
            return (_open ? "open" : "closed") + " on " + Places.MapLabel(map) + ": " + string.Join(" | ", Here().Select(a =>
            {
                var an = ActivitySpots.AnchorOf(a, map);
                return a.Id + (an == null ? " (none)" : (an.Custom ? " custom " : " game ") + Fmt(an.Pos));
            }).ToArray());
        }
    }
}
