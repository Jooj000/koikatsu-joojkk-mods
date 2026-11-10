using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The radar becomes a map. The game's radar (bl_MiniMap) has a top-down orthographic camera that follows the player
    /// but draws nothing (no map picture in this game), only the icons on top.
    /// Rendering that camera's view 10 times a second looked choppy. Now the whole floor is photographed once from straight above (cut just over the head and just under
    /// the feet, so only the floor he is on shows) into an image per map and floor, and every frame that image is only
    /// moved and turned under the icons with the radar camera's own mapping (WorldToViewportPoint, the same one the icons
    /// use). Girls more than a floor's height away get a dimmed icon and a tiny arrow.
    /// </summary>
    internal static class RadarMap
    {
        private const float Above = 2.0f, Below = 0.6f, OtherFloor = 2.2f, CamHeight = 60f;
        /// <summary>Floors are cached by the player's height in 1 m steps; at most this many pictures are kept.</summary>
        private const int MaxCached = 4;
        private const float TexelsPerMeter = 6f;
        private const int MaxTexture = 2048;
        /// <summary>The radar shows 10% less ground than the game's (asked).</summary>
        private const float Zoom = 0.9f;

        private sealed class Floor
        {
            public string Key;
            public Texture Tex;
            public Rect World;     // x = min X, y = min Z, in meters
            public float Y;
            public float Used;
        }

        private static Camera _cam;
        private static RectTransform _frame;
        private static RawImage _img;
        private static bool _logged, _failed;
        private static Sprite _arrow;
        private static global::bl_MiniMap _map;
        private static string _boundsMap;
        private static Rect _bounds;
        private static float _boundsAt;
        private static Floor _shown;
        private static readonly List<Floor> Cache = new List<Floor>();
        private static readonly AccessTools.FieldRef<NPC, ActionGame.bl_MiniMapItem> NpcIcon = AccessTools.FieldRefAccess<NPC, ActionGame.bl_MiniMapItem>("miniMapIcon");
        private static readonly Dictionary<NPC, Image> Arrows = new Dictionary<NPC, Image>();

        private static readonly AccessTools.FieldRef<ActionGame.bl_MiniMapItem, RectTransform> ItemRoot = AccessTools.FieldRefAccess<ActionGame.bl_MiniMapItem, RectTransform>("RectRoot");
        private static readonly AccessTools.FieldRef<ActionGame.bl_MiniMapItem, RectTransform> ItemGraphic = AccessTools.FieldRefAccess<ActionGame.bl_MiniMapItem, RectTransform>("graphicRectTransform");

        public static void Install(Harmony harmony)
        {
            try { harmony.Patch(AccessTools.Method(typeof(ActionGame.bl_MiniMapItem), "Update"), postfix: new HarmonyMethod(typeof(RadarMap), nameof(OnTheCircle))); }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Radar icons: " + e.Message); }
        }

        /// <summary>
        /// An icon out of range (a girl with a request, an event) is kept inside the radar's square by the game, so in the
        /// round radar it slid along the hidden corners and seemed to turn around another point (mid). It now sits on the
        /// circle's edge, in her direction from the center.
        /// </summary>
        internal static int Clamped;
        internal static string LastClamp = "-";

        private static void OnTheCircle(ActionGame.bl_MiniMapItem __instance)
        {
            try
            {
                if (!Campus.On || !__instance.OffScreen) return;
                var root = ItemRoot(__instance);
                var g = ItemGraphic(__instance);
                if (root == null || g == null) return;
                var r = Mathf.Min(root.sizeDelta.x, root.sizeDelta.y) * 0.5f - g.sizeDelta.x * 0.5f;
                var v = g.anchoredPosition;
                if (r > 0f && v.sqrMagnitude > r * r) { g.anchoredPosition = v.normalized * r; Clamped++; LastClamp = __instance.name + " r " + r.ToString("0") + " from " + v; }
            }
            catch { }
        }

        public static void Update()
        {
            if (_failed) return;
            try
            {
                var src = global::bl_MiniMap.MiniMapCamera;
                var root = global::bl_MiniMap.MapUIRoot;
                var on = Cfg.RadarMap.Value && G.Act != null && G.Act.Player != null && src != null && root != null && root.gameObject.activeInHierarchy;
                if (!on)
                {
                    if (_frame != null && _frame.gameObject.activeSelf) _frame.gameObject.SetActive(false);
                    Floors(null, false);
                    if (G.Act == null && Cache.Count > 0) Forget();
                    return;
                }
                Ensure(src, root);
                ZoomIn(src);
                var floor = FloorFor();
                if (floor == null)
                {
                    if (_frame.gameObject.activeSelf) _frame.gameObject.SetActive(false);
                }
                else
                {
                    if (!_frame.gameObject.activeSelf) _frame.gameObject.SetActive(true);
                    if (_shown != floor) { _shown = floor; _img.texture = floor.Tex; }
                    Place(src, root, floor);
                }
                Floors(root, true);
            }
            catch (Exception e)
            {
                // A radar that cannot be drawn must never cost the game anything: switch off for this session.
                _failed = true;
                CampusLifePlugin.Log.LogWarning("Radar map off for this session: " + e);
            }
        }

        private static void Ensure(Camera src, RectTransform root)
        {
            if (_cam == null)
            {
                var go = new GameObject("cl_radar_cam");
                Object.DontDestroyOnLoad(go);
                _cam = go.AddComponent<Camera>();
                _cam.enabled = false; // only used to take the floor pictures
                _cam.orthographic = true;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _cam.useOcclusionCulling = false;
                _cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // straight down, +Z up in the picture
                _cam.cullingMask = WorldMask(src);
                LogSetup(src, root);
            }
            // Inside the radar's round mask (its teal disc is the mask's own image and covered anything placed before
            // it), first among its children: over the disc, under the rings and the icons, cut to the circle.
            var holder = root.Cast<Transform>().FirstOrDefault(c => c.GetComponent<Mask>() != null) ?? root;
            if (_frame == null || _frame.parent != holder)
            {
                if (_frame != null) Object.Destroy(_frame.gameObject);
                _frame = GameUi.Rect(holder, "cl_radar_frame");
                _frame.SetAsFirstSibling();
                _frame.anchorMin = _frame.anchorMax = new Vector2(0.5f, 0.5f);
                _frame.pivot = new Vector2(0.5f, 0.5f);
                var go = new GameObject("cl_radar_floor", typeof(RectTransform));
                go.transform.SetParent(_frame, false);
                _img = go.AddComponent<RawImage>();
                _img.raycastTarget = false;
                _img.color = new Color(1f, 1f, 1f, 0.9f);
                var rt = _img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                _shown = null;
            }
            // The frame lies exactly over the icons' rectangle (they are placed by the radar camera's viewport times the
            // root's size); the floor picture moves inside it.
            _frame.sizeDelta = root.rect.size;
            _frame.position = root.TransformPoint(root.rect.center);
            _frame.rotation = root.rotation;
            _frame.localScale = new Vector3(root.lossyScale.x / Mathf.Max(0.0001f, holder.lossyScale.x), root.lossyScale.y / Mathf.Max(0.0001f, holder.lossyScale.y), 1f);
        }

        /// <summary>
        /// The game eases its camera toward DefaultHeight, which it reads from PlayerPrefs (the keypad +/- keys save it).
        /// We keep it at 90% of the saved height while the radar is small, without saving anything; full screen is the game's.
        /// The floor picture follows the camera's own mapping, so it zooms with the icons.
        /// </summary>
        private static void ZoomIn(Camera src)
        {
            if (_map == null || _map.MMCamera != src) _map = src.GetComponentInParent<global::bl_MiniMap>() ?? Object.FindObjectOfType<global::bl_MiniMap>();
            if (_map == null || global::bl_MiniMap.isFullScreen) return;
            var saved = PlayerPrefs.GetFloat("MinimapCameraHeight", 30f);
            if (Mathf.Abs(_map.DefaultHeight - saved) < 0.01f) _map.DefaultHeight = saved * Zoom;
        }

        /// <summary>Everything that is the school itself: not the UI, not the radar's own layer, not the people.</summary>
        private static int WorldMask(Camera src)
        {
            var mask = 0;
            for (var i = 0; i < 32; i++)
            {
                var n = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(n) || (src.cullingMask & (1 << i)) != 0) continue;
                var l = n.ToLowerInvariant();
                if (l.Contains("ui") || l.Contains("chara") || l.Contains("player") || l.Contains("npc") || l.Contains("minimap") || l.Contains("raycast") || l.Contains("effect")) continue;
                mask |= 1 << i;
            }
            return mask;
        }

        /// <summary>
        /// The picture of the floor the player is on. every picture ever taken is saved as a PNG in
        /// BepInEx/cache/joojkk_radar (one per map and floor, the same at any time of day), and from then on it is only
        /// loaded. A new picture is taken only where there is none within 1.5 m of height, and only once the player has
        /// stood on that height for a second (never on the steps of a staircase).
        /// </summary>
        private static Floor FloorFor()
        {
            if (G.PlayerMapNo < 0) return null;   // between maps (loading): nothing to photograph
            var mapNo = G.PlayerMapNo.ToString();
            if (mapNo != _boundsMap)
            {
                Forget();
                _boundsMap = mapNo;
                _bounds = new Rect();
                _boundsAt = -10f;
            }
            var now = Time.realtimeSinceStartup;
            var pp = G.Act.Player.position;
            var py = pp.y;
            if (Mathf.Abs(py - _stableY) > 0.25f) { _stableY = py; _stableSince = now; }
            var prefix = mapNo + "@";
            if (_shown != null && Mathf.Abs(py - _shown.Y) < 2.5f && Covers(_shown.World, pp) && Cache.Contains(_shown))
            {
                _shown.Used = now;
                return _shown;
            }
            var f = Cache.Where(c => Mathf.Abs(c.Y - py) < 1.5f && Covers(c.World, pp)).OrderBy(c => Mathf.Abs(c.Y - py)).FirstOrDefault();
            if (f == null)
            {
                // A picture that does not reach the player was taken before the map had loaded: taken again.
                var saved = Disk().Where(d => d.Map == mapNo && Mathf.Abs(d.Y - py) < 1.5f && Covers(d.World, pp)).OrderBy(d => Mathf.Abs(d.Y - py)).FirstOrDefault();
                if (saved != null) f = Load(saved);
            }
            if (f == null)
            {
                // Nothing saved here yet: one picture, taken standing still, then never again.
                if (now - _stableSince < 1f || now - _lastBake < 1.5f) return _shown != null && Cache.Contains(_shown) ? _shown : null;
                if (_bounds.width < 1f || _bounds.height < 1f)
                {
                    // Measured while the map was still loading: measure again every second until there is ground.
                    if (now - _boundsAt < 1f) return null;
                    _boundsAt = now;
                    var map = G.Act.Map;
                    if (map == null || map.isMapLoading) return null;
                    _bounds = Disk().Where(d => d.Map == mapNo && Covers(d.World, pp)).Select(d => d.World).FirstOrDefault();
                    if (_bounds.width < 1f || _bounds.height < 1f) _bounds = MapBounds();
                    if (_bounds.width < 1f || _bounds.height < 1f || !Covers(_bounds, pp)) { _bounds = new Rect(); return null; }
                }
                _lastBake = now;
                var y = Mathf.Round(py);
                f = Bake(prefix + y, y);
                Save(mapNo, f);
            }
            Cache.Add(f);
            while (Cache.Count > MaxCached)
            {
                var old = Cache.Where(c => c != f).OrderBy(c => c.Used).First();
                Release(old);
                Cache.Remove(old);
            }
            f.Used = now;
            return f;
        }

        private static float _stableY = -999f, _stableSince;

        private static bool Covers(Rect world, Vector3 p) { return world.Contains(new Vector2(p.x, p.z)); }

        private sealed class Saved
        {
            public string Map, File;
            public float Y;
            public Rect World;
        }

        private static List<Saved> _disk;
        private static string Dir { get { return System.IO.Path.Combine(System.IO.Path.Combine(BepInEx.Paths.CachePath, "joojkk_radar"), "v1"); } }

        /// <summary>index.txt: map|y|xMin|zMin|width|depth|file, one line per saved floor.</summary>
        private static List<Saved> Disk()
        {
            if (_disk != null) return _disk;
            _disk = new List<Saved>();
            try
            {
                var idx = System.IO.Path.Combine(Dir, "index.txt");
                if (!System.IO.File.Exists(idx)) return _disk;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                foreach (var line in System.IO.File.ReadAllLines(idx))
                {
                    var p = line.Split('|');
                    if (p.Length < 7 || !System.IO.File.Exists(System.IO.Path.Combine(Dir, p[6]))) continue;
                    _disk.Add(new Saved
                    {
                        Map = p[0], Y = float.Parse(p[1], inv), File = p[6],
                        World = new Rect(float.Parse(p[2], inv), float.Parse(p[3], inv), float.Parse(p[4], inv), float.Parse(p[5], inv)),
                    });
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Radar index: " + e.Message); }
            return _disk;
        }

        private static Floor Load(Saved d)
        {
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false) { name = "cl_radar_floor_" + d.Map + "@" + d.Y, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                if (!tex.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Dir, d.File)), true)) { Object.Destroy(tex); return null; }
                return new Floor { Key = d.Map + "@" + d.Y, Tex = tex, World = d.World, Y = d.Y };
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Radar load " + d.File + ": " + e.Message); return null; }
        }

        private static void Save(string map, Floor f)
        {
            try
            {
                var tex = f.Tex as Texture2D;
                if (tex == null) return;   // the outline could not be read back: nothing to keep
                System.IO.Directory.CreateDirectory(Dir);
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var file = "map" + map + "_y" + f.Y.ToString("0", inv) + ".png";
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Dir, file), tex.EncodeToPNG());
                // The same file again (an old picture that did not reach the player): its line is replaced.
                Disk().RemoveAll(d => d.File == file);
                Disk().Add(new Saved { Map = map, Y = f.Y, World = f.World, File = file });
                System.IO.File.WriteAllLines(System.IO.Path.Combine(Dir, "index.txt"), Disk().Select(d => string.Join("|", new[]
                {
                    d.Map, d.Y.ToString("0.##", inv), d.World.xMin.ToString("0.###", inv), d.World.yMin.ToString("0.###", inv),
                    d.World.width.ToString("0.###", inv), d.World.height.ToString("0.###", inv), d.File,
                })).ToArray());
                CampusLifePlugin.Log.LogInfo("Radar map: saved " + file);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Radar save: " + e.Message); }
        }

        /// <summary>The map's ground rectangle (X by Z) from the school's own renderers; huge ones (sky, far scenery) are left out.</summary>
        private static Rect MapBounds()
        {
            var mask = _cam.cullingMask;
            var have = false;
            var b = new Bounds();
            foreach (var r in Object.FindObjectsOfType<Renderer>())
            {
                if (r == null || !r.enabled || (mask & (1 << r.gameObject.layer)) == 0) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var rb = r.bounds;
                if (rb.size.x > 500f || rb.size.z > 500f) continue;
                if (!have) { b = rb; have = true; }
                else b.Encapsulate(rb);
            }
            if (!have) return new Rect();
            const float pad = 2f;
            return new Rect(b.min.x - pad, b.min.z - pad, b.size.x + 2 * pad, b.size.z + 2 * pad);
        }

        private static Floor Bake(string key, float y)
        {
            var w = _bounds.width;
            var h = _bounds.height;
            var scale = Mathf.Min(TexelsPerMeter, MaxTexture / Mathf.Max(w, h));
            var tw = Mathf.Clamp(Mathf.CeilToInt(w * scale), 16, MaxTexture);
            var th = Mathf.Clamp(Mathf.CeilToInt(h * scale), 16, MaxTexture);
            var rt = new RenderTexture(tw, th, 16) { name = "cl_radar_floor_" + key, wrapMode = TextureWrapMode.Clamp };
            rt.Create();
            var t = _cam.transform;
            t.position = new Vector3(_bounds.center.x, y + CamHeight, _bounds.center.y);
            _cam.orthographicSize = h * 0.5f;
            _cam.aspect = w / h;
            // The slice between just under the feet and just over the head.
            _cam.nearClipPlane = CamHeight - Above;
            _cam.farClipPlane = CamHeight + Below;
            _cam.targetTexture = rt;
            Texture tex = rt;
            try
            {
                _cam.Render();
                var outlined = Outlined(rt);
                if (outlined != null) tex = outlined;
            }
            finally { _cam.targetTexture = null; }
            if (tex != rt) { rt.Release(); Object.Destroy(rt); }
            CampusLifePlugin.Log.LogDebug("Radar map: floor " + key + " " + tw + "x" + th + " over " + w.ToString("0") + "x" + h.ToString("0") + " m" + (tex != rt ? " (walls outlined)" : ""));
            return new Floor { Key = key, Tex = tex, World = _bounds, Y = y };
        }

        /// <summary>
        /// Seen from above, the cut slice shows walls as thin bands that blend into the floor (P3): where the picture
        /// changes sharply (brightness or void against floor) a dark two-pixel line is drawn, which turns rooms and walls
        /// into a plan. Null when the picture cannot be read back.
        /// </summary>
        private static Texture2D Outlined(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            try
            {
                var w = rt.width; var h = rt.height;
                var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { name = rt.name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                var px = tex.GetPixels32();
                var edge = new bool[px.Length];
                Func<Color32, float> v = c => (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) / 255f * (c.a / 255f) + (c.a / 255f) * 0.6f;
                for (var y = 0; y < h - 1; y++)
                    for (var x = 0; x < w - 1; x++)
                    {
                        var i = y * w + x;
                        var d = Mathf.Abs(v(px[i]) - v(px[i + 1])) + Mathf.Abs(v(px[i]) - v(px[i + w]));
                        if (d > 0.22f) edge[i] = true;
                    }
                var dark = new Color32(14, 22, 30, 255);
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                    {
                        var i = y * w + x;
                        if (edge[i] || x > 0 && edge[i - 1] || y > 0 && edge[i - w]) px[i] = dark;
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return tex;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Radar outline: " + e.Message); return null; }
            finally { RenderTexture.active = prev; }
        }

        /// <summary>Lays the floor picture under the icons: its corners go where the radar camera puts those world points.</summary>
        private static void Place(Camera src, RectTransform root, Floor f)
        {
            var size = root.rect.size;
            var o = Ui(src, new Vector3(f.World.xMin, f.Y, f.World.yMin), size);
            var ex = Ui(src, new Vector3(f.World.xMax, f.Y, f.World.yMin), size) - o;   // the picture's bottom edge
            var ez = Ui(src, new Vector3(f.World.xMin, f.Y, f.World.yMax), size) - o;   // its left edge
            var r = _img.rectTransform;
            r.sizeDelta = new Vector2(ex.magnitude, ez.magnitude);
            r.anchoredPosition = o + (ex + ez) * 0.5f;
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(ex.y, ex.x) * Mathf.Rad2Deg);
        }

        private static Vector2 Ui(Camera src, Vector3 world, Vector2 size)
        {
            var vp = src.WorldToViewportPoint(world);
            return new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
        }

        private static void Release(Floor f)
        {
            if (f == null || f.Tex == null) return;
            if (_shown == f)
            {
                _shown = null;
                if (_img != null) _img.texture = null;
            }
            var rt = f.Tex as RenderTexture;
            if (rt != null) rt.Release();
            Object.Destroy(f.Tex);
        }

        /// <summary>A new map, or the scene left: the old pictures go.</summary>
        private static void Forget()
        {
            foreach (var f in Cache) Release(f);
            Cache.Clear();
            _boundsMap = null;
            _shown = null;
        }

        /// <summary>Girls on another floor: a dimmed icon and a tiny arrow, up or down.</summary>
        private static float _lastBake = -10f;

        private static void Floors(RectTransform root, bool on)
        {
            var act = G.Act;
            if (!on || act == null || act.npcList == null || act.Player == null)
            {
                foreach (var kv in Arrows) if (kv.Value != null && kv.Value.gameObject.activeSelf) kv.Value.gameObject.SetActive(false);
                return;
            }
            var py = act.Player.position.y;
            foreach (var npc in act.npcList)
            {
                if (npc == null) continue;
                ActionGame.bl_MiniMapItem item;
                try { item = NpcIcon(npc); } catch { continue; }
                if (item == null || item.iconItem == null || item.iconItem.graphic == null) continue;
                var g = item.iconItem.graphic;
                // The icon follows the NPC's active switch, and a switch the game missed leaves a girl who is right here
                // invisible on the radar.
                if (npc.isActive && act.Map != null && npc.mapNo == act.Map.no && !item.iconItem.visible) item.SetVisible(true);
                var dy = npc.position.y - py;
                var other = npc.isActiveAndEnabled && Mathf.Abs(dy) > OtherFloor;
                var a = other ? 0.35f : item.IconColor.a;
                if (Mathf.Abs(g.color.a - a) > 0.01f) g.color = new Color(g.color.r, g.color.g, g.color.b, a);
                Image arrow;
                Arrows.TryGetValue(npc, out arrow);
                if (!other)
                {
                    if (arrow != null && arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
                    continue;
                }
                var parent = g.rectTransform.parent;
                if (arrow == null || arrow.transform.parent != parent)
                {
                    if (arrow != null) Object.Destroy(arrow.gameObject);
                    arrow = GameUi.Image(parent, "cl_floor_arrow", null, new Color(1f, 1f, 1f, 0.95f));
                    arrow.sprite = Arrow();
                    arrow.raycastTarget = false;
                    var rt = arrow.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(11f, 11f);
                    Arrows[npc] = arrow;
                }
                if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);
                // Beside the icon on the screen, up or down on the screen, whichever way the radar turns (the offset in the
                // icon's own space turned with it: "se movendo tortos ao girar o radar").
                arrow.transform.position = g.rectTransform.position + new Vector3(9f, 9f, 0f) * g.rectTransform.lossyScale.x;
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, dy > 0f ? 0f : 180f);
                arrow.transform.SetAsLastSibling();
            }
            foreach (var dead in Arrows.Keys.Where(n => n == null || !act.npcList.Contains(n)).ToList())
            {
                if (Arrows[dead] != null) Object.Destroy(Arrows[dead].gameObject);
                Arrows.Remove(dead);
            }
        }

        /// <summary>A white chevron with a dark line, pointing up (it was a flat triangle).</summary>
        private static Sprite Arrow()
        {
            if (_arrow != null) return _arrow;
            _arrow = MapSprite.Make(IconArt.Chevron(48), 48, 100f, "cl_floor_arrow");
            return _arrow;
        }

        private static void LogSetup(Camera src, RectTransform root)
        {
            if (_logged) return;
            _logged = true;
            CampusLifePlugin.Log.LogInfo("Radar map: " + Describe());
        }

        public static string Describe()
        {
            var src = global::bl_MiniMap.MiniMapCamera;
            var root = global::bl_MiniMap.MapUIRoot;
            if (src == null || root == null) return "no radar";
            var layers = string.Join(",", Enumerable.Range(0, 32).Where(i => !string.IsNullOrEmpty(LayerMask.LayerToName(i))).Select(i => i + ":" + LayerMask.LayerToName(i)).ToArray());
            var kids = string.Join(", ", root.Cast<Transform>().Select(c => c.name + "[" + string.Join("/", c.GetComponents<Component>().Select(x => x.GetType().Name).ToArray()) + "]").Take(12).ToArray());
            var floors = string.Join(",", Cache.Select(c => c.Key + ":" + c.Tex.width + "x" + c.Tex.height).ToArray());
            return "camera at " + src.transform.position + " rot " + src.transform.eulerAngles + " ortho " + src.orthographic + " size " + src.orthographicSize.ToString("0.0") +
                   " aspect " + src.aspect.ToString("0.00") + " mask " + src.cullingMask + " target " + (src.targetTexture != null ? src.targetTexture.width + "x" + src.targetTexture.height : "screen " + src.rect) +
                   " | ours mask " + (_cam != null ? _cam.cullingMask.ToString() : "-") + " | map " + _bounds + " floors " + floors + " | root " + root.rect.size + " | children " + kids + " | layers " + layers + " | on the circle " + Clamped + " (last " + LastClamp + ")";
        }
    }
}
