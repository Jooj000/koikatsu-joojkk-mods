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
    /// ícones"). Her mood once you know it, and one star for the likes you found out; hovering shows a small note.
    /// descoberto. mantenha as funções que você atribuiu juntas dessa." The row now sits under the talk's time bar, and
    /// after the mood and the star each like you found out has its own small badge (its name on hover); a like learned
    /// during the talk pops into the row. The like badges on the options, the gift menu and the shop stay.
    /// </summary>
    internal static class TalkBadges
    {
        private const float Size = 28f, Small = 22f, Gap = 5f, Refresh = 0.5f, Pop = 0.45f;
        private static readonly AccessTools.FieldRef<TalkScene, Image> LewdRef = AccessTools.FieldRefAccess<TalkScene, Image>("imageLewdness");
        private static readonly AccessTools.FieldRef<TalkScene, Image> TimeRef = AccessTools.FieldRefAccess<TalkScene, Image>("imageTime");
        private static readonly Dictionary<string, Image> LikeIcons = new Dictionary<string, Image>();
        private static readonly Dictionary<string, float> Popping = new Dictionary<string, float>();

        private static TalkScene _scene;
        private static RectTransform _row, _tip;
        private static Image _mood, _likes;
        private static float _next;
        private static bool _failed;

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
                    _scene = null;
                    return;
                }
                if (ts != _scene || _row == null)
                {
                    if (_row != null) Object.Destroy(_row.gameObject);
                    _row = null;
                    _scene = ts;
                    LikeIcons.Clear();
                    Popping.Clear();
                    _known = null;
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

        /// <summary>The row of icons, just under the time bar's lower left corner (the H bar's if there is none), in the bar's own canvas.</summary>
        private static bool Build(TalkScene ts)
        {
            Image bar = null;
            try { bar = TimeRef(ts); } catch { }
            if (bar == null) bar = LewdRef(ts);
            if (bar == null) return false;
            var parent = bar.transform.parent as RectTransform;
            if (parent == null) return false;
            _row = GameUi.Rect(parent, "cl_talk_badges");
            _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 0.5f);
            _row.pivot = new Vector2(0f, 1f);
            _row.sizeDelta = new Vector2(2 * Size + Gap + GirlInfo.LikeIds.Length * (Small + Gap), Size);
            var corners = new Vector3[4];
            bar.rectTransform.GetWorldCorners(corners);
            _row.position = corners[0];
            _row.anchoredPosition += new Vector2(0f, -Gap);
            _row.SetAsLastSibling();
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
            Hook(trig, EventTriggerType.PointerEnter, () => ShowTip(slot));
            Hook(trig, EventTriggerType.PointerExit, HideTip);
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

        /// <summary>A like's own small badge, made the first time it is shown.</summary>
        private static Image LikeIcon(string like)
        {
            Image img;
            if (LikeIcons.TryGetValue(like, out img) && img != null) return img;
            img = GameUi.Image(_row, "like_" + like, null, Color.white);
            img.sprite = ConvArt.Badge(IconArt.FirstLike + Array.IndexOf(GirlInfo.LikeIds, like));
            img.raycastTarget = true;
            img.preserveAspect = true;
            var trig = img.gameObject.AddComponent<EventTrigger>();
            var l = like;
            Hook(trig, EventTriggerType.PointerEnter, () => ShowLikeTip(l));
            Hook(trig, EventTriggerType.PointerExit, HideTip);
            LikeIcons[like] = img;
            return img;
        }

        /// <summary>A like learned while the row is up grows in from nothing, a little past its size and back.</summary>
        private static void Animate()
        {
            if (Popping.Count == 0) return;
            var now = Time.realtimeSinceStartup;
            foreach (var kv in Popping.ToList())
            {
                Image img;
                if (!LikeIcons.TryGetValue(kv.Key, out img) || img == null) { Popping.Remove(kv.Key); continue; }
                var t = Mathf.Clamp01((now - kv.Value) / Pop);
                var k = t < 0.6f ? Mathf.Lerp(0.2f, 1.3f, t / 0.6f) : Mathf.Lerp(1.3f, 1f, (t - 0.6f) / 0.4f);
                img.rectTransform.localScale = new Vector3(k, k, 1f);
                if (t >= 1f) { img.rectTransform.localScale = Vector3.one; Popping.Remove(kv.Key); }
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
            _shownLikes = GirlInfo.LikeIds.Where((l, i) => (gs.KnownLikes & (1 << i)) != 0 && GirlInfo.Likes(h, l)).ToList();
            // The star moves into the first place when the mood is not known; the likes' own badges follow it.
            var x = _shownMood != Mood.None ? Size + Gap : 0f;
            _likes.rectTransform.Place(x, 0f, Size, Size);
            if (_likes.gameObject.activeSelf != (_shownLikes.Count > 0)) _likes.gameObject.SetActive(_shownLikes.Count > 0);
            if (_shownLikes.Count > 0) x += Size + Gap;
            var first = _known == null;
            if (first) _known = new HashSet<string>();
            foreach (var like in GirlInfo.LikeIds)
            {
                Image img;
                if (!_shownLikes.Contains(like))
                {
                    if (LikeIcons.TryGetValue(like, out img) && img != null && img.gameObject.activeSelf) img.gameObject.SetActive(false);
                    continue;
                }
                img = LikeIcon(like);
                img.rectTransform.Place(x, (Size - Small) / 2f, Small, Small);
                // centered pivot, so the pop grows from the middle
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                img.rectTransform.anchoredPosition += new Vector2(Small / 2f, -Small / 2f);
                if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
                if (_known.Add(like) && !first)
                {
                    Popping[like] = Time.realtimeSinceStartup;
                    img.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
                    CampusLifePlugin.Log.LogInfo("Talk badges: " + like + " learned, its badge joins the row under the time bar");
                }
                x += Small + Gap;
            }
        }

        internal static string Shown()
        {
            if (_row == null) return "no row";
            var parts = new List<string>();
            if (_shownMood != Mood.None) parts.Add("mood " + _shownMood);
            if (_shownLikes.Count > 0) parts.Add("star");
            parts.AddRange(_shownLikes);
            return parts.Count == 0 ? "empty" : string.Join(", ", parts.ToArray());
        }

        private static void ShowLikeTip(string like)
        {
            HideTip();
            Image img;
            if (_row == null || !LikeIcons.TryGetValue(like, out img) || img == null) return;
            var text = GirlInfo.LikeLabel(like);
            var width = 24f + text.Length * 10.5f;
            var back = GameUi.Image(_row, "cl_badge_tip", null, GameUi.NoteBack);
            _tip = back.rectTransform;
            var left = img.rectTransform.anchoredPosition.x - img.rectTransform.pivot.x * Small;
            _tip.Place(left, Size + 6f, width, 30f);
            var t = GameUi.Text(_tip, "text", text, 19f, GameUi.White, TextAlignmentOptions.Midline, false);
            t.rectTransform.Place(4f, 0f, width - 8f, 30f);
        }

        // ---- hover note ----

        private static void ShowTip(int slot)
        {
            HideTip();
            if (_row == null) return;
            var lines = new List<KeyValuePair<Sprite, string>>();
            if (slot == 0)
            {
                if (_shownMood == Mood.None) return;
                lines.Add(new KeyValuePair<Sprite, string>(null, "Mood: " + Lines.MoodName(_shownMood)));
            }
            else
            {
                if (_shownLikes.Count == 0) return;
                lines.Add(new KeyValuePair<Sprite, string>(null, "Likes"));
                foreach (var l in _shownLikes)
                    lines.Add(new KeyValuePair<Sprite, string>(ConvArt.Badge(IconArt.FirstLike + Array.IndexOf(GirlInfo.LikeIds, l)), GirlInfo.LikeLabel(l)));
            }
            const float lineH = 26f, pad = 8f, icon = 22f;
            var width = 40f + lines.Max(x => x.Value.Length) * 10.5f + (lines.Any(x => x.Key != null) ? icon + 6f : 0f);
            var back = GameUi.Image(_row, "cl_badge_tip", null, GameUi.NoteBack);
            _tip = back.rectTransform;
            _tip.Place(slot == 0 || _shownMood == Mood.None ? 0f : Size + Gap, Size + 6f, width, pad * 2 + lines.Count * lineH);
            var y = pad;
            foreach (var kv in lines)
            {
                var x = pad + 2f;
                if (kv.Key != null)
                {
                    var im = GameUi.Image(_tip, "icon", null, Color.white);
                    im.sprite = kv.Key;
                    im.preserveAspect = true;
                    im.rectTransform.Place(x, y + (lineH - icon) / 2f, icon, icon);
                    x += icon + 6f;
                }
                var t = GameUi.Text(_tip, "text", kv.Value, 19f, kv.Key == null && lines.Count > 1 ? GameUi.Gold : GameUi.White, TextAlignmentOptions.MidlineLeft, false);
                t.rectTransform.Place(x, y, width - x - pad, lineH);
                y += lineH;
            }
        }

        private static void HideTip()
        {
            if (_tip != null) Object.Destroy(_tip.gameObject);
            _tip = null;
        }

        internal static string Hover(int slot)
        {
            if (slot >= 2)
            {
                // 2, 3... = the like badges, in the row's order
                var i = slot - 2;
                if (i >= _shownLikes.Count) return "no like badge " + i + " (row: " + Shown() + ")";
                ShowLikeTip(_shownLikes[i]);
                return _tip == null ? "no note" : "note shown: " + GirlInfo.LikeLabel(_shownLikes[i]) + " (row: " + Shown() + ")";
            }
            ShowTip(slot);
            return _tip == null ? "no note (" + (slot == 0 ? "mood not known" : "no likes known") + ")" : "note shown";
        }
    }
}
