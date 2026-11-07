using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// "Give a gift" opens a list of the gifts you carry using the conversation menu's own buttons
    /// (same sprite, font and place as "Invite to lunch", "Let's study"...), plus Back.
    /// </summary>
    internal static class GiftMenu
    {
        private const int PerPage = 9;
        private static readonly List<GameObject> Hidden = new List<GameObject>();
        private static readonly List<GameObject> Created = new List<GameObject>();
        private static Transform _list;
        private static GameObject _template;
        private static SaveData.Heroine _target;
        private static int _page;

        public static bool IsOpen { get { return _list != null; } }

        public static void Open(Button template, SaveData.Heroine h)
        {
            Close();
            if (template == null || template.transform.parent == null || h == null) return;
            _list = template.transform.parent;
            _template = template.gameObject;
            _target = h;
            _page = 0;
            foreach (Transform c in _list)
                if (c.gameObject.activeSelf) { Hidden.Add(c.gameObject); c.gameObject.SetActive(false); }
            Fill();
        }

        public static void Close()
        {
            foreach (var go in Created) if (go != null) Object.Destroy(go);
            Created.Clear();
            foreach (var go in Hidden) if (go != null) go.SetActive(true);
            Hidden.Clear();
            _list = null;
            _template = null;
        }

        private static void Fill()
        {
            foreach (var go in Created) if (go != null) Object.Destroy(go);
            Created.Clear();
            var s = Campus.S;
            var gs = s.Girl(GirlInfo.Key(_target));
            var gifts = Items.All.Where(i => i.Type == ItemType.Gift && s.Count(i.Id) > 0).ToList();
            var pages = Math.Max(1, (gifts.Count + PerPage - 1) / PerPage);
            _page = Mathf.Clamp(_page, 0, pages - 1);

            if (gs.LastGiftDay == s.Day) Add("Already gave one today", null, false);
            if (gifts.Count == 0) Add("No gifts (store: cafeteria)", null, false);
            foreach (var item in gifts.Skip(_page * PerPage).Take(PerPage))
            {
                var it = item;
                // Gifts she is known to like carry the like's icon.
                var liked = item.Likes.FirstOrDefault(l => Conversation.KnownLike(_target, l));
                var label = item.Name + " x" + s.Count(item.Id);
                var fits = Board.ActiveFor(GirlInfo.Key(_target)).Any(r => r.Kind == RequestKind.Gift) && Board.WantsGift(_target, item);
                if (fits) label = "<color=#FF8CCC>\u2605</color> " + label;
                var go = Add(label, liked, () =>
                {
                    var h = _target;
                    Close();
                    TalkButtons.GiveGift(h, it);
                }, true);
                if (fits && go != null) Border(go);
            }
            if (pages > 1) Add("More... (" + (_page + 1) + "/" + pages + ")", () => { _page = (_page + 1) % pages; Fill(); }, true);
            Add("Back", Close, true);
        }

        private static void Add(string label, Action onClick, bool interactable) { Add(label, null, onClick, interactable); }

        /// <summary>
        /// frame and strip, gold for Actions, turn pink; no outline is added around it.
        /// </summary>
        internal static void Border(GameObject go)
        {
            try
            {
                var img = go.GetComponent<Image>();
                if (img == null) return;
                img.sprite = ConvArt.FitCard();
                img.type = Image.Type.Sliced;
                if (!go.name.EndsWith("_fits")) go.name += "_fits";
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Gift border: " + e.Message); }
        }

        private static GameObject Add(string label, string like, Action onClick, bool interactable)
        {
            var go = Object.Instantiate(_template, _list, false);
            go.name = "cl_gift_option";
            go.SetActive(true);
            var b = go.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.interactable = interactable && onClick != null;
            if (onClick != null)
                b.onClick.AddListener(() =>
                {
                    try { Sfx.Select(); onClick(); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                });
            var t = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null)
            {
                t.richText = true;
                t.enableAutoSizing = true;
                t.fontSizeMin = 14f;
                t.fontSizeMax = t.fontSize;
                t.text = label;
            }
            if (like != null) { go.name = "cl_gift_option_liked"; Conversation.LikeMark(go.transform as RectTransform, like, t); }
            Created.Add(go);
            return go;
        }
    }
}
