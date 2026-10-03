using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionGame.Chara;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The stage of the conversation with two girls: the
    /// talk scene's copy of the first girl to the left, the second girl (her map body, shown by Duo) to the right, the
    /// same distance from the middle, both facing the camera and turned a little toward each other. The second girl gets
    /// the talk poses through her own Motion (the game only animates the first one). The camera is the game's own, moved
    /// screen and framed to mid-thigh like a normal talk, no pulling back); while the game does not update it (its ADV),
    /// the last pose it gave is kept, so nothing piles up. The second girl's panel is a copy of the game's own (face,
    /// heart, H, state) mirrored to the other side, with her numbers written by us; the game's touch, look and camera
    /// icons move to the top center while it lasts. A marker over the head of whoever you are talking to.
    /// </summary>
    internal static class TrioStage
    {
        private const float Turn = 8f;
        private static float _half = 0.32f;   // each one's distance from the middle (from the width the camera sees)

        public static bool On { get; private set; }
        private static TalkScene _ts;
        private static NPC _nb;
        private static ChaControl _a, _b;
        private static Vector3 _mid, _aHome;
        private static Quaternion _aHomeRot;
        private static RuntimeAnimatorController _bCtrl;
        private static readonly Illusion.Game.Elements.EasyLoader.Motion BMotion = new Illusion.Game.Elements.EasyLoader.Motion();
        private static Camera _now, _map;
        private static float _fovAdded;
        private static Vector3 _basePos, _offset;
        private static Quaternion _baseRot;
        private static bool _hasBase, _bGone;
        private static Transform _headA, _headB;
        private static int _target;   // 0 A, 1 B, 2 both
        private static GameObject _ui;
        private static TextMeshProUGUI _marker, _marker2;
        // The second girl's copy of the game's panel, and the game's icons we moved (put back at the end).
        private static readonly List<GameObject> _bPanel = new List<GameObject>();
        private static RawImage _bFace;
        private static Image _bState, _bLewd, _bTime;
        private static Image[] _bFavor;
        private static Sprite[] _sprState, _sprLewd;
        private static Texture2D _bFaceTex;
        private static readonly List<KeyValuePair<RectTransform, Vector3>> _moved = new List<KeyValuePair<RectTransform, Vector3>>();
        private static readonly FieldInfo UpdateCam = AccessTools.Field(typeof(TalkScene), "isUpdateCamera");

        public static bool Begin(TalkScene ts, NPC nb, TMP_FontAsset font, Material fontMat)
        {
            End();
            try
            {
                if (ts == null || nb == null || nb.chaCtrl == null || ts.targetHeroine == null || ts.targetHeroine.chaCtrl == null) return false;
                var tr = Traverse.Create(ts);
                _now = tr.Field("nowCamera").GetValue<Camera>();
                _map = tr.Field("cameraMap").GetValue<Camera>();
                if (_now == null) return false;
                _ts = ts;
                _nb = nb;
                _a = ts.targetHeroine.chaCtrl;
                _b = nb.chaCtrl;
                _aHome = _a.transform.position;
                _aHomeRot = _a.transform.rotation;
                _mid = _aHome;
                var right = _now.transform.right;
                right.y = 0f;
                right.Normalize();
                // Apart enough not to overlap, close enough to both fit the game's own framing.
                var fwd0 = _now.transform.forward;
                var depth0 = Mathf.Max(0.5f, Vector3.Dot(_mid - _now.transform.position, fwd0));
                var halfW0 = depth0 * Mathf.Tan(_now.fieldOfView * 0.5f * Mathf.Deg2Rad) * _now.aspect;
                _half = Mathf.Clamp(0.3f * halfW0, 0.24f, 0.36f);
                Place(_a.transform, _mid - right * _half, right);
                Place(_nb.transform, _mid + right * _half, -right);
                if (_nb.transform != _b.transform) _b.transform.SetPositionAndRotation(_nb.transform.position, _nb.transform.rotation);
                _headA = Bone(_a, "cf_j_head");
                _headB = Bone(_b, "cf_j_head");
                _bCtrl = _b.animBody != null ? _b.animBody.runtimeAnimatorController : null;
                _fovAdded = 0f;
                _hasBase = false;
                _offset = Vector3.zero;
                _bGone = false;
                On = true;
                PoseB("通常立ち");
                BuildUi(font, fontMat);
                Conversation.AddBSpots(_b);
                MoveIcons();
                MirrorPanel();
                RefreshPanel();
                Target(0);
                CampusLifePlugin.Log.LogInfo("Trio stage: " + GirlInfo.FirstName(ts.targetHeroine) + " left, " + GirlInfo.FirstName(nb.heroine) + " right, " + _half.ToString("0.00") + " m from the middle, " +
                    depth0.ToString("0.00") + " m from the camera (it sees " + (2f * halfW0).ToString("0.00") + " m), panel " + _bPanel.Count + " piece(s), icons moved " + _moved.Count);
                return true;
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError("Trio stage: " + e);
                End();
                return false;
            }
        }

        private static void Place(Transform t, Vector3 pos, Vector3 toward)
        {
            var look = _now.transform.position - pos;
            look.y = 0f;
            if (look.sqrMagnitude < 0.01f) look = -_now.transform.forward;
            look = Vector3.RotateTowards(look.normalized, toward, Turn * Mathf.Deg2Rad, 0f);
            t.SetPositionAndRotation(new Vector3(pos.x, _aHome.y, pos.z), Quaternion.LookRotation(look));
        }

        private static Transform Bone(ChaControl c, string name)
        {
            try { return c.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name); }
            catch { return null; }
        }

        /// <summary>Every frame after the game's Update: the camera between the two (plugin LateUpdate).</summary>
        public static void LateTick(bool uiVisible)
        {
            if (!On) return;
            try
            {
                if (_ts == null || G.TalkScene != _ts || _now == null) { End(); return; }
                var gameMoves = UpdateCam == null || (bool)UpdateCam.GetValue(_ts);
                if (gameMoves || !_hasBase)
                {
                    // The game set it this frame (or never moved it since we last did): the camera's own pose.
                    if (gameMoves || _offset == Vector3.zero) { _basePos = _now.transform.position - (gameMoves ? Vector3.zero : _offset); _baseRot = _now.transform.rotation; }
                    _hasBase = true;
                }
                var fwd = _baseRot * Vector3.forward;
                var right = _baseRot * Vector3.right;
                var center = _bGone ? _a.transform.position : _mid;
                var shift = center - (_a != null ? _a.transform.position : center);
                shift.y = 0f;
                // the middle, the other to the left, cut below the knee).
                var want = shift;
                _offset = Vector3.Lerp(_offset, want, Mathf.Clamp01(Time.deltaTime * 4f));
                _now.transform.SetPositionAndRotation(_basePos + _offset, _baseRot);
                if (_map != null) _map.transform.SetPositionAndRotation(_basePos + _offset, _baseRot);
                Marker(uiVisible);
                if (_ui != null && _ui.activeSelf != uiVisible) _ui.SetActive(uiVisible);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio camera: " + e.Message); }
        }

        // ---------------- the second girl ----------------

        /// <summary>One of the talk poses on the second girl (the game's AnimePlay does it for the first one).</summary>
        public static void PoseB(string pose)
        {
            if (!On || _bGone || _b == null || _b.animBody == null || string.IsNullOrEmpty(pose)) return;
            try
            {
                var h = _nb.heroine;
                var p = Singleton<Manager.Communication>.Instance.GetPoseInfo(h.FixCharaIDOrPersonality, pose);
                if (p == null || p.Length < 3) return;
                if (BMotion.Setting(_b.animBody, p[0], p[1], p[2], false))
                {
                    BMotion.Play(_b.animBody);
                    _b.animBody.Update(0f);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio pose " + pose + ": " + e.Message); }
        }

        /// <summary>Who you talk to: the other one looks at her; with both, both look at you.</summary>
        public static void Target(int who)
        {
            _target = who;
            if (!On) return;
            try
            {
                var cam = _now != null ? _now.transform : null;
                if (who == 2 || _bGone)
                {
                    Look(_a, cam, 0.5f);
                    Look(_b, cam, 0.5f);
                }
                else if (who == 0)
                {
                    Look(_a, cam, 0.5f);
                    Look(_b, _headA ?? cam, 0.7f);
                }
                else
                {
                    Look(_b, cam, 0.5f);
                    Look(_a, _headB ?? cam, 0.7f);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio looks: " + e.Message); }
        }

        private static void Look(ChaControl c, Transform t, float rate)
        {
            if (c == null || t == null || c == _b && _bGone) return;
            c.ChangeLookEyesTarget(0, t, rate, 0f, 1f, 2f);
            c.ChangeLookNeckTarget(0, t, rate, 0f, 1f, 0.8f);
        }

        /// <summary>She walks off in the middle (angry, left out): the first girl back to the middle, alone.</summary>
        public static void BLeaves()
        {
            if (!On || _bGone) return;
            _bGone = true;
            try
            {
                RestoreB();
                if (_nb != null) _nb.SetActive(false);
                if (_a != null) _a.transform.SetPositionAndRotation(_aHome, _aHomeRot);
                Target(0);
                foreach (var g in _bPanel) if (g != null) g.SetActive(false);
                PutIconsBack();
                Conversation.ClearBSpots();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio B leaves: " + e.Message); }
        }

        private static void RestoreB()
        {
            try
            {
                if (_b != null && _b.animBody != null && _bCtrl != null && _b.animBody.runtimeAnimatorController != _bCtrl) _b.animBody.runtimeAnimatorController = _bCtrl;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio B animator: " + e.Message); }
        }

        /// <summary>Everything back as the game had it (the talk may go on with the first girl alone).</summary>
        public static void End()
        {
            if (On)
            {
                try
                {
                    RestoreB();
                    if (_now != null && _fovAdded != 0f) _now.fieldOfView -= _fovAdded;
                    if (_now != null && _hasBase && G.TalkScene == _ts) _now.transform.SetPositionAndRotation(_basePos, _baseRot);
                    if (_map != null && _hasBase && G.TalkScene == _ts) _map.transform.SetPositionAndRotation(_basePos, _baseRot);
                    if (!_bGone && _a != null && G.TalkScene == _ts) _a.transform.SetPositionAndRotation(_aHome, _aHomeRot);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio stage end: " + e.Message); }
                try
                {
                    PutIconsBack();
                    Conversation.ClearBSpots();
                    foreach (var g in _bPanel) if (g != null) Object.Destroy(g);
                    if (_bFaceTex != null) Object.Destroy(_bFaceTex);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio stage end: " + e.Message); }
                CampusLifePlugin.Log.LogInfo("Trio stage: over");
            }
            On = false;
            _fovAdded = 0f;
            _ts = null;
            _nb = null;
            _a = _b = null;
            _headA = _headB = null;
            _bCtrl = null;
            if (_ui != null) Object.Destroy(_ui);
            _ui = null;
            _marker = _marker2 = null;
            _bPanel.Clear();
            _bFace = null;
            _bState = _bLewd = _bTime = null;
            _bFavor = null;
            _bFaceTex = null;
        }

        // ---------------- the second girl's panel and the marker ----------------

        private static void BuildUi(TMP_FontAsset font, Material mat)
        {
            _ui = new GameObject("cl_trio_ui");
            Object.DontDestroyOnLoad(_ui);
            var canvas = _ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = _ui.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            _marker = Text(_ui.transform, font, mat, 46f, TextAlignmentOptions.Center);
            _marker2 = Text(_ui.transform, font, mat, 46f, TextAlignmentOptions.Center);
            foreach (var m in new[] { _marker, _marker2 })
            {
                m.text = "▼";
                m.color = new Color(1f, 0.82f, 0.35f, 1f);
                m.outlineWidth = 0.25f;
                m.outlineColor = new Color32(40, 20, 0, 255);
                m.rectTransform.sizeDelta = new Vector2(60f, 60f);
            }
        }

        private static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, Material mat, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            if (mat != null) t.fontSharedMaterial = mat;
            t.fontSize = size;
            t.richText = true;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        // ---------------- the game's panel, mirrored for her ----------------

        /// <summary>A copy of the game's panel (canvasFemale: face, heart, H, state) on the other side of the screen. The
        /// copy sits in the same canvas, so it shows and hides with the game's; its numbers are hers (written by us, the
        /// game's scripts read the first girl).</summary>
        private static void MirrorPanel()
        {
            try
            {
                var tr = Traverse.Create(_ts);
                var face = tr.Field("rawFace").GetValue<RawImage>();
                var state = tr.Field("imageState").GetValue<Image>();
                var favor = tr.Field("imageFavor").GetValue<Image[]>();
                var lewd = tr.Field("imageLewdness").GetValue<Image>();
                var time = tr.Field("imageTime").GetValue<Image>();
                var canvasF = tr.Field("canvasFemale").GetValue<Canvas>();
                _sprState = tr.Field("spriteState").GetValue<Sprite[]>();
                _sprLewd = tr.Field("spriteLewdness").GetValue<Sprite[]>();
                if (face == null || canvasF == null) return;
                var root = canvasF.transform;
                var parts = new List<Transform> { face.transform };
                if (state != null) parts.Add(state.transform);
                if (lewd != null) parts.Add(lewd.transform);
                if (favor != null) parts.AddRange(favor.Where(f => f != null).Select(f => f.transform));
                // One panel node holding them all, or, if they hang straight off the canvas, each one's piece.
                var lca = Common(parts);
                var pieces = lca != null && lca != root && lca.IsChildOf(root)
                    ? new List<Transform> { lca }
                    : parts.Select(x => TopUnder(x, root)).Where(x => x != null).Distinct().ToList();
                // Copies in the original's drawing order.
                pieces = pieces.OrderBy(x => x.GetSiblingIndex()).ToList();
                foreach (var piece in pieces)
                {
                    var copy = Object.Instantiate(piece.gameObject, piece.parent, true);
                    copy.name = "cl_trio_bpanel";
                    foreach (var g in copy.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
                    foreach (var sel in copy.GetComponentsInChildren<Selectable>(true)) sel.interactable = false;
                    MoveOnScreen((RectTransform)copy.transform, (RectTransform)piece, true, 0f);
                    // The gauges (a node with no image of its own) are turned over too, so the arc wraps her face on
                    // the arc ended up far from her face). The face and the state icon keep their drawing.
                    // The face's mask is turned over too (its round corner follows the arc), the picture inside is not.
                    var isFace = face.transform.IsChildOf(piece);
                    if (piece.GetComponent<Graphic>() == null || isFace) Flip((RectTransform)copy.transform);
                    _bPanel.Add(copy);
                    if (_bFace == null) _bFace = Twin<RawImage>(piece, copy.transform, face.transform);
                    if (_bState == null && state != null) _bState = Twin<Image>(piece, copy.transform, state.transform);
                    if (_bLewd == null && lewd != null) _bLewd = Twin<Image>(piece, copy.transform, lewd.transform);
                    if (_bFavor == null && favor != null && favor.Length > 0 && Twin<Image>(piece, copy.transform, favor[0].transform) != null)
                        _bFavor = favor.Select(f => f != null ? Twin<Image>(piece, copy.transform, f.transform) : null).ToArray();
                    // Her own talk time.
                    if (_bTime == null && time != null) _bTime = Twin<Image>(piece, copy.transform, time.transform);
                    // Our own badges on the first girl's panel (her mood, her likes) are hers, not this girl's.
                    foreach (var tr2 in copy.GetComponentsInChildren<Transform>(true))
                        if (tr2 != copy.transform && tr2.name.StartsWith("cl_")) Object.Destroy(tr2.gameObject);
                }
                if (_bFace != null)
                {
                    _bFaceTex = new Texture2D(240, 320);
                    _bFaceTex.LoadImage(_nb.heroine.charFile.facePngData);
                    _bFace.texture = _bFaceTex;
                    Flip((RectTransform)_bFace.transform);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio panel copy: " + e.Message); }
        }

        /// <summary>Her heart, H and state on the copy (the game's own rules: the favor bar of her stage, the H bar's
        /// full sprite, the state by relation and anger).</summary>
        public static void RefreshPanel()
        {
            if (_nb == null || _nb.heroine == null || _bPanel.Count == 0) return;
            try
            {
                var h = _nb.heroine;
                var rel = Mathf.Max(0, h.relation);
                if (_bState != null && _sprState != null && _sprState.Length > 0)
                    _bState.sprite = _sprState[Mathf.Clamp(rel * 2 + (h.isAnger ? 1 : 0), 0, _sprState.Length - 1)];
                var fill = Mathf.InverseLerp(0f, 100f, h.favor);
                if (_bFavor != null && _bFavor.Length >= 3)
                {
                    var stage = Mathf.Clamp(rel, 0, 2);
                    _bFavor[0].fillAmount = stage == 1 ? 1f : 0f;
                    _bFavor[1].fillAmount = stage == 2 ? 1f : 0f;
                    _bFavor[2].fillAmount = 0f;
                    _bFavor[stage].fillAmount = fill;
                }
                else if (_bFavor != null && _bFavor.Length > 0 && _bFavor[0] != null) _bFavor[0].fillAmount = fill;
                if (_bTime != null)
                    _bTime.fillAmount = Mathf.InverseLerp(0f, Mathf.Max(1, h.talkTimeMax), h.talkTime);
                if (_bLewd != null)
                {
                    var l = Mathf.InverseLerp(0f, 100f, h.lewdness);
                    _bLewd.fillAmount = l;
                    if (_sprLewd != null && _sprLewd.Length > 1) _bLewd.sprite = _sprLewd[l >= 1f ? 1 : 0];
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio panel: " + e.Message); }
        }

        /// <summary>The game's touch, look and camera icons (the touch buttons and whatever button sits by them) to the
        /// top center, where the second panel does not cover them.</summary>
        private static void MoveIcons()
        {
            try
            {
                var touch = Traverse.Create(_ts).Field("buttonTouch").GetValue<Button[]>();
                if (touch == null || touch.Length == 0) return;
                var rects = touch.Where(b => b != null).Select(b => (RectTransform)b.transform).ToList();
                if (rects.Count == 0) return;
                var box = ScreenBox(rects[0]);
                foreach (var r in rects.Skip(1)) box = Join(box, ScreenBox(r));
                var near = new Rect(box.xMin - 140f, box.yMin - 140f, box.width + 280f, box.height + 280f);
                var canvas = rects[0].GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                foreach (var b in Object.FindObjectsOfType<Button>())
                {
                    if (b == null || !b.gameObject.activeInHierarchy || b.name.StartsWith("cl_", StringComparison.Ordinal)) continue;
                    // Only the talk's own icons: the map's buttons (ClothChange, hidden under the talk) sit near them and
                    // were pulled into the row as an empty gap.
                    var bc = b.GetComponentInParent<Canvas>();
                    if (root != null && (bc == null || bc.rootCanvas != root)) continue;
                    if (b.GetComponentsInParent<CanvasGroup>().Any(g => g.alpha < 0.05f)) continue;
                    var r = (RectTransform)b.transform;
                    if (rects.Contains(r) || rects.Any(x => r.IsChildOf(x) || x.IsChildOf(r))) continue;
                    var sb = ScreenBox(r);
                    if (sb.width > 260f || sb.height > 260f || !near.Contains(sb.center)) continue;
                    rects.Add(r);
                    box = Join(box, sb);
                }
                // One row: the game stands them as a corner (two over one); at the top center they go
                // side by side, left to right as they were, level with the highest one.
                rects = rects.OrderBy(r => ScreenBox(r).center.x).ThenByDescending(r => ScreenBox(r).center.y).ToList();
                const float gap = 14f;
                var total = rects.Sum(r => ScreenBox(r).width) + gap * (rects.Count - 1);
                var rowY = box.yMax - rects.Max(r => ScreenBox(r).height) * 0.5f;
                var rowX = Screen.width * 0.5f - total * 0.5f;
                var said = new List<string>();
                foreach (var r in rects)
                {
                    var sb = ScreenBox(r);
                    _moved.Add(new KeyValuePair<RectTransform, Vector3>(r, r.localPosition));
                    MoveTo(r, new Vector2(rowX + sb.width * 0.5f, rowY));
                    rowX += sb.width + gap;
                    var now = ScreenBox(r);
                    said.Add(r.name + " " + now.center.x.ToString("0") + "," + now.center.y.ToString("0") + " " + now.width.ToString("0") + "x" + now.height.ToString("0"));
                }
                CampusLifePlugin.Log.LogInfo("Trio icons in a row at the top center (screen " + Screen.width + "x" + Screen.height + "): " + string.Join("; ", said.ToArray()));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio icons: " + e.Message); }
        }

        private static void PutIconsBack()
        {
            foreach (var m in _moved)
                try { if (m.Key != null) m.Key.localPosition = m.Value; }
                catch { }
            _moved.Clear();
        }

        // ---------------- screen helpers (the game's canvases may be overlay or camera) ----------------

        private static Camera CamOf(Transform t)
        {
            var c = t.GetComponentInParent<Canvas>();
            c = c != null ? c.rootCanvas : null;
            return c == null || c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
        }

        private static Rect ScreenBox(RectTransform r)
        {
            var cam = CamOf(r);
            var w = new Vector3[4];
            r.GetWorldCorners(w);
            var a = RectTransformUtility.WorldToScreenPoint(cam, w[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, w[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Rect Join(Rect a, Rect b) { return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)); }

        /// <summary>Puts <paramref name="r"/> where <paramref name="from"/> is on screen, mirrored across the middle
        /// (mirror) or moved sideways by dx pixels; the content is not flipped.</summary>
        /// <summary>Turned over left to right in place (the same box on screen).</summary>
        private static void Flip(RectTransform r)
        {
            var before = r.TransformPoint(r.rect.center);
            var s = r.localScale;
            r.localScale = new Vector3(-s.x, s.y, s.z);
            r.position += before - r.TransformPoint(r.rect.center);
        }

        /// <summary>Moves <paramref name="r"/> so its middle is at screen point <paramref name="want"/>.</summary>
        private static void MoveTo(RectTransform r, Vector2 want)
        {
            var cam = CamOf(r);
            var c = ScreenBox(r).center;
            var parent = r.parent as RectTransform;
            Vector3 w0, w1;
            if (parent == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, c, cam, out w0) || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, want, cam, out w1)) return;
            r.position += w1 - w0;
        }

        private static void MoveOnScreen(RectTransform r, RectTransform from, bool mirror, float dx)
        {
            var cam = CamOf(from);
            var c = ScreenBox(from).center;
            var want = mirror ? new Vector2(Screen.width - c.x, c.y) : new Vector2(c.x + dx, c.y);
            var parent = r.parent as RectTransform;
            Vector3 w0, w1;
            if (parent == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, c, cam, out w0) || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, want, cam, out w1)) return;
            r.position = from.position + (w1 - w0);
        }

        private static Transform Common(List<Transform> ts)
        {
            if (ts.Count == 0) return null;
            var t = ts[0].parent;
            while (t != null && !ts.All(x => x.IsChildOf(t))) t = t.parent;
            return t;
        }

        private static Transform TopUnder(Transform t, Transform root)
        {
            while (t != null && t.parent != root) t = t.parent;
            return t;
        }

        /// <summary>The copy's twin of a node of the original (same path of child indices).</summary>
        private static T Twin<T>(Transform orig, Transform copy, Transform node) where T : Component
        {
            if (node != orig && !node.IsChildOf(orig)) return null;
            var path = new List<int>();
            for (var t = node; t != orig; t = t.parent) path.Add(t.GetSiblingIndex());
            var c = copy;
            for (var i = path.Count - 1; i >= 0; i--)
            {
                if (path[i] >= c.childCount) return null;
                c = c.GetChild(path[i]);
            }
            return c.GetComponent<T>();
        }

        private static void Marker(bool visible)
        {
            if (_marker == null) return;
            var showA = visible && (_target == 0 || _target == 2 && !_bGone);
            var showB = visible && !_bGone && (_target == 1 || _target == 2);
            Over(_marker, _target == 1 ? _headB : _headA, _target == 1 ? showB : showA);
            Over(_marker2, _headB, _target == 2 && showB);
        }

        private static void Over(TextMeshProUGUI m, Transform head, bool show)
        {
            if (m == null) return;
            if (!show || head == null || _now == null) { if (m.gameObject.activeSelf) m.gameObject.SetActive(false); return; }
            var p = _now.WorldToScreenPoint(head.position + Vector3.up * 0.3f);
            if (p.z <= 0f) { m.gameObject.SetActive(false); return; }
            if (!m.gameObject.activeSelf) m.gameObject.SetActive(true);
            // A little bob, so it reads as a marker and not part of the scene.
            m.rectTransform.position = new Vector3(p.x, p.y + Mathf.Sin(Time.realtimeSinceStartup * 3f) * 4f, 0f);
        }

        public static string Describe()
        {
            if (!On) return "stage off";
            Func<Vector3, string> v = x => x.x.ToString("0.00") + "," + x.z.ToString("0.00");
            return "A at " + (_a != null ? v(_a.transform.position) : "?") + ", B at " + (_b != null && !_bGone ? v(_b.transform.position) : "gone") +
                   ", apart " + (2f * _half).ToString("0.00") + " m, camera offset " + _offset.magnitude.ToString("0.00") + " m, fov " + (_now != null ? _now.fieldOfView.ToString("0") : "?") + ", target " + _target +
                   ", B panel " + _bPanel.Count(g => g != null && g.activeInHierarchy) + " piece(s)" + (_bFace != null ? " with face" : "") + ", icons moved " + _moved.Count;
        }
    }
}
