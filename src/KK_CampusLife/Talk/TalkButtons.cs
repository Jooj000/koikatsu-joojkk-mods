using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>Extra buttons inside conversations: Give a gift, Chat, How are you today?, Campus request.</summary>
    internal static class TalkButtons
    {
        private static readonly List<IDisposable> Registered = new List<IDisposable>();
        private static readonly HashSet<Button> Ours = new HashSet<Button>();

        /// <summary>A button made by this class (the conversation does not offer it again as another mod's button).</summary>
        public static bool IsOurs(Button b) { return b != null && Ours.Contains(b); }

        public static void Register()
        {
            // Temporary story button: its text changes to the current step ("Ask about the tournament", "Comfort her"...).
            Registered.Add(GameAPI.AddTalkButton("Talk about it", btn => Setup(btn, OnRequest, () => CurrentRequest() != null, () =>
            {
                var r = CurrentRequest();
                return r == null ? null : Board.ButtonLabel(r);
            }), TalkSceneActionKind.Event));
            // With the new conversation these live inside it (Talk > Give a gift / Ask her > How's your day?).
            Registered.Add(GameAPI.AddTalkButton("Give a gift", btn => Setup(btn, () => GiftMenu.Open(btn, Target), () => Cfg.GiftsEnabled.Value && !Conversation.Active), TalkSceneActionKind.Event));
            Registered.Add(GameAPI.AddTalkButton("How are you today?", btn => Setup(btn, OnAsk, () => Cfg.AskAboutDayButton.Value && !Conversation.Active), TalkSceneActionKind.Talk));
        }

        private static SaveData.Heroine Target
        {
            get
            {
                var ts = G.TalkScene;
                return ts != null ? ts.targetHeroine : null;
            }
        }

        private static void Setup(Button btn, Action onClick, Func<bool> visible, Func<string> label = null)
        {
            Ours.RemoveWhere(x => x == null);
            Ours.Add(btn);
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            var txt = tmp == null ? btn.GetComponentInChildren<Text>(true) : null;
            btn.onClick.AddListener(() =>
            {
                if (Scenes.Running) return;
                try { onClick(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });
            // The parent stays active while the menu is open, so it can drive our visibility.
            var parent = btn.transform.parent;
            if (parent == null) return;
            parent.UpdateAsObservable().Subscribe(_ =>
            {
                if (btn == null) return;
                bool show;
                try { show = Campus.On && Target != null && !GiftMenu.IsOpen && !Conversation.Active && !Duo.Busy && visible(); }
                catch { show = false; }
                if (btn.gameObject.activeSelf != show) btn.gameObject.SetActive(show);
                if (!show || label == null) return;
                string l;
                try { l = label(); } catch { l = null; }
                if (string.IsNullOrEmpty(l)) return;
                if (tmp != null && tmp.text != l) tmp.text = l;
                else if (txt != null && txt.text != l) txt.text = l;
            }).AddTo(btn);
        }

        // ---------------- story step button ----------------

        private static Request CurrentRequest()
        {
            var h = Target;
            return h == null ? null : Board.CompletableInTalk(h);
        }

        private static void OnRequest()
        {
            var h = Target;
            var r = CurrentRequest();
            if (h == null || r == null) return;
            Board.PlayInTalk(r, h);
        }

        // ---------------- How are you today? ----------------

        private static void OnAsk()
        {
            var h = Target;
            if (h == null) return;
            var s = Campus.S;
            var gs = s.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            var b = new SceneBuilder(h, true);
            b.Me(Lines.AskHowAreYou(close));
            var already = gs.MoodKnownDay == s.Day;
            if (already)
                b.Her_(Lines.AlreadyAsked(tone, close));
            else
                b.Her_(Lines.MoodLine(tone, gs.Mood, close));

            // A like she hasn't told you yet: more likely with good INT (you ask the right questions).
            string newLike = null;
            if (!already)
            {
                var unknown = GirlInfo.LikesOf(h).Where(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) == 0).ToList();
                if (unknown.Count > 0 && UnityEngine.Random.value < 0.35f + G.Int / 200f)
                {
                    newLike = unknown[UnityEngine.Random.Range(0, unknown.Count)];
                    b.Her_(Lines.LikeHint(tone, GirlInfo.LikeLabel(newLike), close));
                }
            }

            Scenes.PlayInTalk(b, _ =>
            {
                if (already) return;
                gs.MoodKnownDay = s.Day;
                G.AddFavor(h, 1, 0);
                gs.Attention = Mathf.Clamp(gs.Attention + 3, 0, 100);
                var tip = Lines.MoodTip(gs.Mood);
                Toasts.Show(GirlInfo.FirstName(h) + ": " + Lines.MoodName(gs.Mood) + (tip == "" ? "" : ". " + tip));
                if (newLike != null)
                {
                    gs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, newLike);
                    Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + GirlInfo.LikeLabel(newLike) + ".");
                }
            });
        }

        // ---------------- Give a gift ----------------

        public static void GiveGift(SaveData.Heroine h, ItemDef item)
        {
            var s = Campus.S;
            var gs = s.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);

            if (gs.LastGiftDay == s.Day && !Board.WantsGift(h, item))
            {
                b.Her_(Lines.GiftAlreadyToday(tone));
                Scenes.PlayInTalk(b, _ => Conversation.AfterGift(), false);
                return;
            }
            {
                var art = item.Name ?? "";
                art = art.Length > 0 && "aeiouAEIOU".IndexOf(art[0]) >= 0 ? "an " + art.ToLowerInvariant() : "a " + art.ToLowerInvariant();
                Lore.Record("gave_gift", Lore.Player, GirlInfo.Key(h), note: art);
            }

            b.Me(Lines.PlayerGivesGift(item.Name));
            var liked = item.Likes.Any(l => GirlInfo.Likes(h, l));
            var accepts = true;
            if (item.Intimate)
            {
                accepts = h.isGirlfriend || h.lewdness >= 70 || (G.Sens >= 60 && h.lewdness >= 50);
                b.Her_(accepts ? Lines.GiftIntimateYes(tone) : Lines.GiftIntimateNo(tone));
            }
            else b.Her_(liked ? Lines.GiftLiked(tone, item.Name) : Lines.GiftNeutral(tone, item.Name));

            // No talk time: a gift is always possible, even when she has nothing left to say.
            Scenes.PlayInTalk(b, _ =>
            {
                Conversation.AfterGift();
                if (!accepts)
                {
                    G.AddFavor(h, -5, 0);
                    Toasts.Show(GirlInfo.FirstName(h) + " refused the gift. (-5 favor)");
                    return;
                }
                s.AddItem(item.Id, -1);
                gs.LastGiftDay = s.Day;
                gs.Gifts++;
                Pairs.OnGift(h, liked);
                s.Stat("gifts");

                // A gift she likes is the strongest thing you can do in a day: far more than a chat.
                var favor = liked ? Mathf.RoundToInt(item.LikedFavor * 1.5f) : item.BaseFavor;
                var devotion = liked ? 6 + item.Price / 250 : 1;
                // Mood matches add a bonus and cheer her up.
                var moodHit = (gs.Mood == Mood.Hungry && (item.Likes.Contains("food") || item.Likes.Contains("sweets")))
                              || (gs.Mood == Mood.Sleepy && item.Likes.Contains("coffee"))
                              || (gs.Mood == Mood.Stressed && item.Likes.Contains("sweets"))
                              || (gs.Mood == Mood.Upset && liked)
                              || (gs.Mood == Mood.Flirty && item.Lewd > 0);
                if (moodHit) favor += Mathf.RoundToInt(4 * Cfg.MoodBonusMultiplier.Value);
                if (gs.Mood == Mood.Upset && !liked) favor = Mathf.Max(0, favor - 3);

                if (moodHit) devotion += 3;
                var applied = Campus.Apply(new Rewards { Favor = favor, Lewd = item.Lewd, Devotion = devotion }, h);
                if (moodHit) gs.Mood = Mood.Cheerful;
                if (liked)
                    foreach (var l in item.Likes.Where(l => GirlInfo.Likes(h, l)))
                        gs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, l);
                gs.Attention = Mathf.Clamp(gs.Attention + 10, 0, 100);
                var msg = "Gave " + item.Name + " to " + GirlInfo.FirstName(h) + (liked ? " (she loved it)" : "") + ": " + applied.Describe();
                s.Log(msg);
                Toasts.Show(msg);
                Board.OnGift(h, item);
            }, false);
        }
    }
}
