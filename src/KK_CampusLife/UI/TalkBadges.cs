using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// redondos abaixo da barra de H na conversa, e somente se descobrir. faça um ícone diferente para cada mood, e passando
    /// o mouse por cima aparece o nome. nos likes, só faça um ícone e ao passar o mouse aparecem os gostos com seus
    /// ícones"). Her mood once you know it, and one star for the likes you found out.
    /// sólida. faça com que tenha apenas um ícone principal, em que é passado hover ou clicado e abre uma bandeja com os
    /// ícones e seus nomes." So the row is the mood and one main likes icon, bigger and clear of the bar; hovering the star
    /// opens a tray with each like found out (its own colored icon and its name), and a click keeps the tray open until the
    /// next click. A like learned during the talk makes the star pulse and opens the tray for a moment with the new one
    /// popping in. The like badges on the options, the gift menu and the shop stay.
    /// </summary>
    internal static class TalkBadges
    {
        private const float Size = 36f, Gap = 8f, Below = 14f, Refresh = 0.5f, Pop = 0.45f, Flash = 3f;
        private const float RowH = 40f, TrayIcon = 32f, TrayPad = 10f;
        private static readonly AccessTools.FieldRef<TalkScene, Image> LewdRef = AccessTools.FieldRefAccess<TalkScene, Image>("imageLewdness");
        private static readonly AccessTools.FieldRef<TalkScene, Image> TimeRef = AccessTools.FieldRefAccess<TalkScene, Image>("imageTime");

        private static TalkScene _scene;
        private static RectTransform _row, _tip, _tray;
        private static Image _mood, _likes;
        private static float _next, _pulseAt = -10f, _flashUntil;
        private static bool _failed, _pinned, _hover;
        private static string _newLike;

        public static void Update()
        {
            if (_failed) return;
            try
            {
                TalkScene ts;
                try { ts = G.TalkScene; } catch { ts = null; }
                var h = ts != null ? ts.targetHeroine : null;
                if (ts == null || h == null || !Campus.On || !GirlInfo.IsRegular(h))
                {
                    if (_row != null) Object.Destroy(_row.gameObject);
                    _row = null;
                    _tray = _tip = null;
                    _scene = null;
                    return;
                }
                if (ts != _scene || _row == null)
                {
                    if (_row != null) Object.Destroy(_row.gameObject);
                    _row = null;
                    _tray = _tip = null;
                    _scene = ts;
                    _known = null;
                    _pinned = _hover = false;
                    _flashUntil = 0f;
                    if (!Build(ts)) return;
                    _next = 0f;
                }
                Animate();
                if (Time.realtimeSinceStartup < _next) return;
                _next = Time.realtimeSinceStartup + Refresh;
                Fill(h);
            }
            catch (Exception e)
            {
                _failed = true;
                CampusLifePlugin.Log.LogWarning("Talk badges off for this session: " + e);
            }
        }

        /// <summary>
        /// The row, under the time bar (the H bar's if there is none), in the bar's own canvas. The bar's frame can reach
        /// below the fill image, so the row goes under the lowest of the bar and its frame (its parent, when that is a
        /// small box around it), with a clear gap.
        /// </summary>
        private static bool Build(TalkScene ts)
        {
            Image bar = null;
            try { bar = TimeRef(ts); } catch { }
            if (bar == null) bar = LewdRef(ts);
            if (bar == null) return false;
            var parent = bar.transform.parent as RectTransform;
            if (parent == null) return false;
            var corners = new Vector3[4];
            bar.rectTransform.GetWorldCorners(corners);
            var left = corners[0];
            var bottom = corners[0].y;
            var pc = new Vector3[4];
            parent.GetWorldCorners(pc);
            var barH = corners[1].y - corners[0].y;
            if (pc[1].y - pc[0].y < barH * 4f) bottom = Mathf.Min(bottom, pc[0].y);
            foreach (var g in parent.GetComponentsInChildren<Graphic>(false))
            {
                if (g == null || g.rectTransform == bar.rectTransform) continue;
                var gc = new Vector3[4];
                g.rectTransform.GetWorldCorners(gc);
                // a frame or back of the bar: overlaps it sideways and is not much taller
                if (gc[2].x < corners[0].x || gc[0].x > corners[2].x || gc[1].y - gc[0].y > barH * 4f) continue;
                if (gc[1].y < corners[0].y - barH || gc[0].y > corners[1].y) continue;
                bottom = Mathf.Min(bottom, gc[0].y);
            }
            _row = GameUi.Rect(parent, "cl_talk_badges");
            _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 0.5f);
            _row.pivot = new Vector2(0f, 1f);
            _row.sizeDelta = new Vector2(2 * Size + Gap, Size);
            _row.position = new Vector3(left.x, bottom, left.z);
            _row.anchoredPosition += new Vector2(0f, -Below);
            _row.SetAsLastSibling();
            CampusLifePlugin.Log.LogDebug("Talk badges: bar bottom " + corners[0].y + ", frame bottom " + bottom + ", row at " + _row.anchoredPosition);
            _mood = Icon("mood", 0);
            _likes = Icon("likes", 1);
            _likes.sprite = ConvArt.Badge(IconArt.LikesStar);
            return true;
        }

        private static Image Icon(string name, int slot)
        {
            var img = GameUi.Image(_row, name, null, Color.white);
            img.raycastTarget = true;
            img.preserveAspect = true;
            img.rectTransform.Place(slot * (Size + Gap), 0f, Size, Size);
            var trig = img.gameObject.AddComponent<EventTrigger>();
            if (slot == 0)
            {
                Hook(trig, EventTriggerType.PointerEnter, ShowMoodTip);
                Hook(trig, EventTriggerType.PointerExit, HideTip);
            }
            else
            {
                Hook(trig, EventTriggerType.PointerEnter, () => { _hover = true; ShowTray(); });
                Hook(trig, EventTriggerType.PointerExit, () => { _hover = false; if (!_pinned && Time.realtimeSinceStartup > _flashUntil) HideTray(); });
                Hook(trig, EventTriggerType.PointerClick, () =>
                {
                    _pinned = !_pinned;
                    if (_pinned) ShowTray();
                    else if (!_hover) HideTray();
                });
            }
            img.gameObject.SetActive(false);
            return img;
        }

        private static void Hook(EventTrigger t, EventTriggerType type, Action a)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(_ =>
            {
                try { a(); }
                catch (Exception ex) { CampusLifePlugin.Log.LogDebug("Talk badge: " + ex.Message); }
            });
            t.triggers.Add(e);
        }

        /// <summary>The star pulses after a like is learned; the new like's row in the tray pops in; the flash tray closes by itself.</summary>
        private static void Animate()
        {
            var now = Time.realtimeSinceStartup;
            if (_likes != null)
            {
                var t = (now - _pulseAt) / 1.2f;
                var k = t >= 0f && t < 1f ? 1f + 0.22f * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) : 1f;
                _likes.rectTransform.localScale = new Vector3(k, k, 1f);
            }
            if (_tray != null && _newLike != null)
            {
                var row = _tray.Find("like_" + _newLike);
                if (row != null)
                {
                    var t = Mathf.Clamp01((now - _pulseAt) / Pop);
                    var k = t < 0.6f ? Mathf.Lerp(0.2f, 1.15f, t / 0.6f) : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f);
                    row.localScale = new Vector3(k, k, 1f);
                }
            }
            if (_tray != null && _flashUntil > 0f && now > _flashUntil)
            {
                _flashUntil = 0f;
                _newLike = null;
                if (!_pinned && !_hover) HideTray();
            }
        }

        private static Mood _shownMood = Mood.None;
        private static List<string> _shownLikes = new List<string>();
        private static HashSet<string> _known;

        /// <summary>Only what the player found out: the mood when he can read it, the likes she told or showed him.</summary>
        private static void Fill(SaveData.Heroine h)
        {
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            _shownMood = Cfg.MoodsEnabled.Value && gs.Mood != Mood.None && Moods.Known(gs) ? gs.Mood : Mood.None;
            if (_shownMood != Mood.None) _mood.sprite = ConvArt.Badge((int)_shownMood);
            if (_mood.gameObject.activeSelf != (_shownMood != Mood.None)) _mood.gameObject.SetActive(_shownMood != Mood.None);
            var likes = GirlInfo.LikeIds.Where((l, i) => (gs.KnownLikes & (1 << i)) != 0 && GirlInfo.Likes(h, l)).ToList();
            // The star moves into the first place when the mood is not known.
            _likes.rectTransform.Place(_shownMood != Mood.None ? Size + Gap : 0f, 0f, Size, Size);
            if (_likes.gameObject.activeSelf != (likes.Count > 0)) _likes.gameObject.SetActive(likes.Count > 0);
            var first = _known == null;
            if (first) _known = new HashSet<string>();
            string learned = null;
            foreach (var like in likes)
                if (_known.Add(like) && !first) learned = like;
            var changed = !likes.SequenceEqual(_shownLikes);
            _shownLikes = likes;
            if (learned != null)
            {
                _newLike = learned;
                _pulseAt = Time.realtimeSinceStartup;
                _flashUntil = Time.realtimeSinceStartup + Flash;
                ShowTray();
                CampusLifePlugin.Log.LogInfo("Talk badges: " + learned + " learned, the likes star pulses and its tray shows it");
            }
            else if (changed && _tray != null) ShowTray();
        }

        internal static string Shown()
        {
            if (_row == null) return "no row";
            var parts = new List<string>();
            if (_shownMood != Mood.None) parts.Add("mood " + _shownMood);
            if (_shownLikes.Count > 0) parts.Add("star (" + _shownLikes.Count + " likes)");
            var s = parts.Count == 0 ? "empty" : string.Join(", ", parts.ToArray());
            if (_tray != null) s += "; tray open" + (_pinned ? " (pinned)" : "") + ": " + string.Join(", ", _shownLikes.ToArray());
            return s;
        }

        // ---- the tray ----

        private static void ShowTray()
        {
            HideTip();
            HideTray();
            if (_row == null || _shownLikes.Count == 0) return;
            var labels = _shownLikes.Select(GirlInfo.LikeLabel).ToList();
            var width = TrayPad * 2 + TrayIcon + 10f + Mathf.Max(70f, labels.Max(l => l.Length) * 11.5f);
            var back = GameUi.Image(_row, "cl_likes_tray", null, GameUi.NoteBack);
            _tray = back.rectTransform;
            var head = 30f;
            _tray.Place(_likes.rectTransform.anchoredPosition.x, Size + 8f, width, TrayPad * 2 + head + _shownLikes.Count * RowH);
            var title = GameUi.Text(_tray, "title", "Likes (" + _shownLikes.Count + ")", 19f, GameUi.Gold, TextAlignmentOptions.MidlineLeft, false);
            title.rectTransform.Place(TrayPad + 2f, TrayPad - 2f, width - TrayPad * 2, head);
            var y = TrayPad + head;
            for (var i = 0; i < _shownLikes.Count; i++)
            {
                var like = _shownLikes[i];
                var row = GameUi.Rect(_tray, "like_" + like);
                row.Place(TrayPad, y, width - TrayPad * 2, RowH);
                row.pivot = new Vector2(0f, 0.5f);
                row.anchoredPosition += new Vector2(0f, -RowH / 2f);
                var icon = GameUi.Image(row, "icon", null, Color.white);
                icon.sprite = ConvArt.Badge(IconArt.FirstLike + Array.IndexOf(GirlInfo.LikeIds, like));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.Place(0f, (RowH - TrayIcon) / 2f, TrayIcon, TrayIcon);
                var t = GameUi.Text(row, "name", labels[i], 21f, GameUi.White, TextAlignmentOptions.MidlineLeft, false);
                t.rectTransform.Place(TrayIcon + 10f, 0f, width - TrayPad * 2 - TrayIcon - 10f, RowH);
                if (like == _newLike) row.localScale = new Vector3(0.2f, 0.2f, 1f);
                y += RowH;
            }
            back.raycastTarget = false;
        }

        private static void HideTray()
        {
            if (_tray != null) Object.Destroy(_tray.gameObject);
            _tray = null;
        }

        // ---- the mood's hover note ----

        private static void ShowMoodTip()
        {
            HideTip();
            if (_row == null || _shownMood == Mood.None) return;
            var text = "Mood: " + Lines.MoodName(_shownMood);
            var width = 28f + text.Length * 11f;
            var back = GameUi.Image(_row, "cl_badge_tip", null, GameUi.NoteBack);
            _tip = back.rectTransform;
            _tip.Place(0f, Size + 8f, width, 34f);
            back.raycastTarget = false;
            var t = GameUi.Text(_tip, "text", text, 20f, GameUi.White, TextAlignmentOptions.Midline, false);
            t.rectTransform.Place(4f, 0f, width - 8f, 34f);
        }

        private static void HideTip()
        {
            if (_tip != null) Object.Destroy(_tip.gameObject);
            _tip = null;
        }

        /// <summary>
        /// </summary>
        internal static string Hover(int slot)
        {
            if (_row == null) return "no row";
            if (slot == 0)
            {
                HideTray();
                ShowMoodTip();
                return _tip == null ? "no note (mood not known)" : "mood note shown";
            }
            if (slot >= 2)
            {
                _pinned = !_pinned;
                if (_pinned) ShowTray(); else HideTray();
                return "star clicked: " + Shown();
            }
            ShowTray();
            return _tray == null ? "no tray (no likes known)" : Shown();
        }
    }
}
