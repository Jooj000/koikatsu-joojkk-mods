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
    /// talk scene's copy of the first girl 0.35 m to the left, the second girl (her map body, shown by Duo) 0.35 m to the
    /// right, both facing the camera and turned a little toward each other. The second girl gets the talk poses through
    /// her own Motion (the game only animates the first one). The camera is the game's own, moved after it each frame:
    /// to the middle of the two, pulled back a little, with a wider view; while the game does not update it (its ADV),
    /// the last pose it gave is kept, so nothing piles up. A panel with the second girl's numbers (the game's panel shows
    /// the first one's) and a marker over the head of whoever you are talking to.
    /// </summary>
    internal static class TrioStage
    {
        private const float Half = 0.35f, Turn = 8f, Pull = 0.55f, Wider = 6f;

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
        private static TextMeshProUGUI _panel, _marker, _marker2;
        private static Image _panelBack;
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
                Place(_a.transform, _mid - right * Half, right);
                Place(_nb.transform, _mid + right * Half, -right);
                if (_nb.transform != _b.transform) _b.transform.SetPositionAndRotation(_nb.transform.position, _nb.transform.rotation);
                _headA = Bone(_a, "cf_j_head");
                _headB = Bone(_b, "cf_j_head");
                _bCtrl = _b.animBody != null ? _b.animBody.runtimeAnimatorController : null;
                _now.fieldOfView += Wider;
                _fovAdded = Wider;
                _hasBase = false;
                _offset = Vector3.zero;
                _bGone = false;
                On = true;
                PoseB("通常立ち");
                BuildUi(font, fontMat);
                Target(0);
                CampusLifePlugin.Log.LogInfo("Trio stage: " + GirlInfo.FirstName(ts.targetHeroine) + " left, " + GirlInfo.FirstName(nb.heroine) + " right, " + (_mid - _now.transform.position).magnitude.ToString("0.00") + " m from the camera");
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
                // A slight lean toward whoever you talk to (10 cm), never a cut.
                var lean = _bGone ? 0f : _target == 0 ? -0.1f : _target == 1 ? 0.1f : 0f;
                // The option list takes the right quarter of the screen: the pair sits left of it (center at 37% of
                var depth = Mathf.Max(0.5f, Vector3.Dot(center - _basePos, fwd));
                var halfW = depth * Mathf.Tan(_now.fieldOfView * 0.5f * Mathf.Deg2Rad) * _now.aspect;
                var aside = _bGone ? 0f : 0.26f * halfW;
                var want = shift + right * (lean + aside) - fwd * (_bGone ? 0f : Pull);
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
                if (_panel != null) _panel.transform.parent.gameObject.SetActive(false);
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
            _panel = _marker = _marker2 = null;
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

            // The panel: top right, the same place on her side as the game's panel on the first girl's.
            var box = new GameObject("panel", typeof(RectTransform));
            var brt = (RectTransform)box.transform;
            brt.SetParent(_ui.transform, false);
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-28f, -28f);
            brt.sizeDelta = new Vector2(330f, 128f);
            _panelBack = box.AddComponent<Image>();
            _panelBack.color = new Color(0.08f, 0.07f, 0.12f, 0.72f);
            _panelBack.raycastTarget = false;
            _panel = Text(box.transform, font, mat, 24f, TextAlignmentOptions.TopLeft);
            var prt = _panel.rectTransform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(16f, 10f);
            prt.offsetMax = new Vector2(-14f, -10f);

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
            RefreshPanel();
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

        /// <summary>Her name, heart and H, mood and who she is to the other girl (written by us: the game's panel scripts
        /// read the first girl).</summary>
        public static void RefreshPanel()
        {
            if (_panel == null || _nb == null || _nb.heroine == null) return;
            try
            {
                var h = _nb.heroine;
                var a = _ts != null ? _ts.targetHeroine : null;
                var gs = Campus.S.Girl(GirlInfo.Key(h));
                var mood = gs.MoodDay == Campus.S.Day ? gs.Mood : Mood.None;
                var rel = h.isGirlfriend ? "girlfriend" : h.relation >= 2 ? "close" : h.relation == 1 ? "friend" : h.relation == 0 ? "acquaintance" : "stranger";
                var bond = a != null ? Social.Bond(a, h) : 0;
                var them = bond >= 60 ? "best friends" : bond >= 25 ? "friends" : bond <= -30 ? "rivals" : "classmates";
                _panel.text = "<b><size=30>" + GirlInfo.FirstName(h) + "</size></b>  <color=#bbbbbb>" + rel + "</color>\n" +
                              "<color=#ff7fa8>♥ " + h.favor + "</color>   <color=#ff9bd8>H " + h.lewdness + "</color>   <color=#9fd0ff>" + (mood == Mood.None ? "fine" : Lines.MoodName(mood).ToLowerInvariant()) + "</color>\n" +
                              "<size=20><color=#d8c8a8>" + (a != null ? "with " + GirlInfo.FirstName(a) + ": " + them : "") + "</color></size>";
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio panel: " + e.Message); }
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
                   ", camera offset " + _offset.magnitude.ToString("0.00") + " m, fov " + (_now != null ? _now.fieldOfView.ToString("0") : "?") + ", target " + _target;
        }
    }
}
