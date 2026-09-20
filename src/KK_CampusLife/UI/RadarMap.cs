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
    /// (no map picture in this game), only the icons on top. A camera of ours rides on it and draws the school itself,
    /// cut just above the player's head and just below the feet, so only the floor he is on shows; the picture goes under
    /// the icons, stretched over the radar exactly like the icons' own viewport mapping. Girls more than a floor's height
    /// away get a dimmed icon and a tiny arrow.
    /// </summary>
    internal static class RadarMap
    {
        private const float Above = 2.0f, Below = 0.6f, OtherFloor = 2.2f, RenderEvery = 0.1f;
        /// <summary>The radar shows 10% less ground than the game's (asked 2026-10-26: "dê um zoom de 10% no radar").</summary>
        private const float Zoom = 0.9f;

        private static Camera _src, _cam;
        private static RenderTexture _rt;
        private static RawImage _img;
        private static float _nextRender;
        private static bool _logged, _failed;
        private static Sprite _arrow;
        private static global::bl_MiniMap _map;
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
                    if (_img != null && _img.gameObject.activeSelf) _img.gameObject.SetActive(false);
                    Floors(null, false);
                    return;
                }
                Ensure(src, root);
                ZoomIn(src);
                if (!_img.gameObject.activeSelf) _img.gameObject.SetActive(true);
                if (Time.realtimeSinceStartup >= _nextRender)
                {
                    _nextRender = Time.realtimeSinceStartup + RenderEvery;
                    Render(src);
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
            if (_rt == null)
            {
                _rt = new RenderTexture(256, 256, 16) { name = "cl_radar_floor" };
                _rt.Create();
            }
            if (_cam == null || _src != src)
            {
                if (_cam != null) Object.Destroy(_cam.gameObject);
                _src = src;
                var go = new GameObject("cl_radar_cam");
                go.transform.SetParent(src.transform, false);
                _cam = go.AddComponent<Camera>();
                _cam.enabled = false; // drawn by hand a few times a second
                _cam.orthographic = true;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _cam.targetTexture = _rt;
                _cam.cullingMask = WorldMask(src);
                _cam.useOcclusionCulling = false;
                LogSetup(src, root);
            }
            // Inside the radar's round mask (its teal disc is the mask's own image and covered anything placed before it,
            var holder = root.Cast<Transform>().FirstOrDefault(c => c.GetComponent<Mask>() != null) ?? root;
            if (_img == null || _img.transform.parent != holder)
            {
                if (_img != null) Object.Destroy(_img.gameObject);
                var go = new GameObject("cl_radar_floor", typeof(RectTransform));
                go.transform.SetParent(holder, false);
                go.transform.SetAsFirstSibling();
                _img = go.AddComponent<RawImage>();
                _img.texture = _rt;
                _img.raycastTarget = false;
                _img.color = new Color(1f, 1f, 1f, 0.9f);
                var rt = _img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
            // Exactly over the icons' rectangle (they are placed by the radar camera's viewport times the root's size).
            var r = _img.rectTransform;
            r.sizeDelta = root.rect.size;
            r.position = root.TransformPoint(root.rect.center);
            r.rotation = root.rotation;
            r.localScale = new Vector3(root.lossyScale.x / Mathf.Max(0.0001f, holder.lossyScale.x), root.lossyScale.y / Mathf.Max(0.0001f, holder.lossyScale.y), 1f);
        }

        /// <summary>
        /// The game eases its camera toward DefaultHeight, which it reads from PlayerPrefs (the keypad +/- keys save it).
        /// We keep it at 90% of the saved height while the radar is small, without saving anything; full screen is the game's.
        /// Our camera copies the size, so the floor plan and the icons stay together.
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

        private static void Render(Camera src)
        {
            var player = G.Act.Player.position;
            var t = _cam.transform;
            _cam.orthographicSize = src.orthographicSize;
            _cam.aspect = src.aspect;
            var fwd = t.forward;
            // The slice between just under the feet and just over the head, along the camera's view.
            var near = Vector3.Dot(player + Vector3.up * Above - t.position, fwd);
            var far = Vector3.Dot(player - Vector3.up * Below - t.position, fwd);
            if (far <= 0.1f) return; // the camera is not above the player yet (map loading)
            _cam.nearClipPlane = Mathf.Max(0.01f, near);
            _cam.farClipPlane = Mathf.Max(_cam.nearClipPlane + 0.5f, far);
            _cam.Render();
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
            return "camera at " + src.transform.position + " rot " + src.transform.eulerAngles + " ortho " + src.orthographic + " size " + src.orthographicSize.ToString("0.0") +
                   " aspect " + src.aspect.ToString("0.00") + " mask " + src.cullingMask + " target " + (src.targetTexture != null ? src.targetTexture.width + "x" + src.targetTexture.height : "screen " + src.rect) +
                   " | ours mask " + (_cam != null ? _cam.cullingMask.ToString() : "-") + " | root " + root.rect.size + " | children " + kids + " | layers " + layers;
        }
    }
}
