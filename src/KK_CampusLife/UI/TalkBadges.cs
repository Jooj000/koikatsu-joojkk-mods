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
    /// </summary>
    internal static class TalkBadges
    {
        private const float Size = 30f, Gap = 6f, Refresh = 0.5f;
        private static readonly AccessTools.FieldRef<TalkScene, Image> LewdRef = AccessTools.FieldRefAccess<TalkScene, Image>("imageLewdness");

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
                    if (!Build(ts)) return;
                    _next = 0f;
                }
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

        /// <summary>The row of icons, just under the H bar's lower left corner, in the bar's own canvas.</summary>
        private static bool Build(TalkScene ts)
        {
            var bar = LewdRef(ts);
            if (bar == null) return false;
            var parent = bar.transform.parent as RectTransform;
            if (parent == null) return false;
            _row = GameUi.Rect(parent, "cl_talk_badges");
            _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 0.5f);
            _row.pivot = new Vector2(0f, 1f);
            _row.sizeDelta = new Vector2(2 * Size + Gap, Size);
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

        private static Mood _shownMood = Mood.None;
        private static List<string> _shownLikes = new List<string>();

        /// <summary>Only what the player found out: the mood when he can read it, the likes she told or showed him.</summary>
        private static void Fill(SaveData.Heroine h)
        {
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            _shownMood = Cfg.MoodsEnabled.Value && gs.Mood != Mood.None && Moods.Known(gs) ? gs.Mood : Mood.None;
            if (_shownMood != Mood.None) _mood.sprite = ConvArt.Badge((int)_shownMood);
            if (_mood.gameObject.activeSelf != (_shownMood != Mood.None)) _mood.gameObject.SetActive(_shownMood != Mood.None);
            _shownLikes = GirlInfo.LikeIds.Where((l, i) => (gs.KnownLikes & (1 << i)) != 0 && GirlInfo.Likes(h, l)).ToList();
            // The star moves into the first place when the mood is not known.
            _likes.rectTransform.Place(_shownMood != Mood.None ? Size + Gap : 0f, 0f, Size, Size);
            if (_likes.gameObject.activeSelf != (_shownLikes.Count > 0)) _likes.gameObject.SetActive(_shownLikes.Count > 0);
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
            _tip.Place(slot * (Size + Gap), Size + 6f, width, pad * 2 + lines.Count * lineH);
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
            ShowTip(slot);
            return _tip == null ? "no note (" + (slot == 0 ? "mood not known" : "no likes known") + ")" : "note shown";
        }
    }
}
