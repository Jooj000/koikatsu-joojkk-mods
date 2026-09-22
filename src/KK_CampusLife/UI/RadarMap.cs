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
    /// caso a personagem no mapa esteja em outro andar ela fica menos destacada e com uma setinha bem pequena para cima ou
    /// baixo"). The game's radar (bl_MiniMap) has a top-down orthographic camera that follows the player but draws nothing
    /// (no map picture in this game), only the icons on top.
    /// second looked choppy. Now the whole floor is photographed once from straight above (cut just over the head and
    /// just under the feet, so only the floor he is on shows) into an image per map and floor, and every frame that image
    /// is only moved and turned under the icons with the radar camera's own mapping (WorldToViewportPoint, the same one
    /// the icons use). Girls more than a floor's height away get a dimmed icon and a tiny arrow.
    /// </summary>
    internal static class RadarMap
    {
        private const float Above = 2.0f, Below = 0.6f, OtherFloor = 2.2f, CamHeight = 60f;
        /// <summary>Floors are cached by the player's height in 1 m steps; at most this many pictures are kept.</summary>
        private const int MaxCached = 4;
        private const float TexelsPerMeter = 6f;
        private const int MaxTexture = 2048;
        /// <summary>The radar shows 10% less ground than the game's (asked 2026-10-26: "dê um zoom de 10% no radar").</summary>
        private const float Zoom = 0.9f;

        private sealed class Floor
        {
            public string Key;
            public RenderTexture Tex;
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
            // Inside the radar's round mask (its teal disc is the mask's own image and covered anything placed before it,
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

        /// <summary>The picture of the floor the player is on: taken once per map and metre of height, then reused.</summary>
        private static Floor FloorFor()
        {
            // A new map or a new period (the light changes with the time of day): new pictures.
            var mapNo = G.PlayerMapNo + "/" + G.Period;
            if (mapNo != _boundsMap)
            {
                Forget();
                _boundsMap = mapNo;
                _bounds = MapBounds();
                _boundsAt = Time.realtimeSinceStartup;
            }
            // Measured while the map was still loading:
            // measure again every second until there is ground.
            if ((_bounds.width < 1f || _bounds.height < 1f) && Time.realtimeSinceStartup - _boundsAt > 1f)
            {
                _bounds = MapBounds();
                _boundsAt = Time.realtimeSinceStartup;
            }
            if (_bounds.width < 1f || _bounds.height < 1f) return null;
            var y = Mathf.Round(G.Act.Player.position.y);
            var key = mapNo + "@" + y;
            var f = Cache.FirstOrDefault(c => c.Key == key);
            if (f == null)
            {
                f = Bake(key, y);
                Cache.Add(f);
                while (Cache.Count > MaxCached)
                {
                    var old = Cache.Where(c => c != f).OrderBy(c => c.Used).First();
                    Release(old);
                    Cache.Remove(old);
                }
            }
            f.Used = Time.realtimeSinceStartup;
            return f;
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
            var tex = new RenderTexture(tw, th, 16) { name = "cl_radar_floor_" + key, wrapMode = TextureWrapMode.Clamp };
            tex.Create();
            var t = _cam.transform;
            t.position = new Vector3(_bounds.center.x, y + CamHeight, _bounds.center.y);
            _cam.orthographicSize = h * 0.5f;
            _cam.aspect = w / h;
            // The slice between just under the feet and just over the head.
            _cam.nearClipPlane = CamHeight - Above;
            _cam.farClipPlane = CamHeight + Below;
            _cam.targetTexture = tex;
            try { _cam.Render(); }
            finally { _cam.targetTexture = null; }
            CampusLifePlugin.Log.LogDebug("Radar map: floor " + key + " " + tw + "x" + th + " over " + w.ToString("0") + "x" + h.ToString("0") + " m");
            return new Floor { Key = key, Tex = tex, World = _bounds, Y = y };
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
            f.Tex.Release();
            Object.Destroy(f.Tex);
        }

        /// <summary>A new map, or the scene left: the old pictures go.</summary>
        private static void Forget()
        {
            foreach (var f in Cache) Release(f);
            Cache.Clear();
            _boundsMap = null;
        }

        /// <summary>Girls on another floor: a dimmed icon and a tiny arrow, up or down.</summary>
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
                    rt.sizeDelta = new Vector2(9f, 9f);
                    Arrows[npc] = arrow;
                }
                if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);
                arrow.rectTransform.anchoredPosition = g.rectTransform.anchoredPosition + new Vector2(9f, 9f);
                // Up or down on the screen, whichever way the radar turns.
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, dy > 0f ? 0f : 180f);
                arrow.transform.SetAsLastSibling();
            }
            foreach (var dead in Arrows.Keys.Where(n => n == null || !act.npcList.Contains(n)).ToList())
            {
                if (Arrows[dead] != null) Object.Destroy(Arrows[dead].gameObject);
                Arrows.Remove(dead);
            }
        }

        /// <summary>A small white triangle pointing up.</summary>
        private static Sprite Arrow()
        {
            if (_arrow != null) return _arrow;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    // Apex at the top: the half-width grows from 0 at the top row to n/2 at the bottom.
                    var half = (n - 1 - y) * 0.5f;
                    var inside = Mathf.Abs(x - (n - 1) * 0.5f) <= half + 0.5f && y >= 2 && y <= n - 3;
                    px[y * n + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            _arrow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
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
                   " | ours mask " + (_cam != null ? _cam.cullingMask.ToString() : "-") + " | map " + _bounds + " floors " + floors + " | root " + root.rect.size + " | children " + kids + " | layers " + layers;
        }
    }
}
