using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>The shape a conversation takes. The shape changes what you can do, not only the words.</summary>
    internal enum Form { None, SceneTalk, Aside, OneLiner, Favor, Walk, Quiet, Triangle, Callback }

    /// <summary>One small situation she is in (a mood, a place, an item you carry), with concrete things you can answer.</summary>
    internal sealed class Bit
    {
        public string Id = "";
        public string Narr = "";            // what you see her do
        public string Line = "";            // what she says
        public readonly List<Reply> Replies = new List<Reply>();
        public Reply Add(string label, string her, int favor, int trust = 0, string memory = null)
        {
            var r = new Reply { Label = label, Her = her, Favor = favor, Trust = trust, Memory = memory, Tag = "r" + Replies.Count };
            Replies.Add(r);
            return r;
        }
    }

    internal sealed class Reply
    {
        public string Label = "", Tag = "", Her = "", Memory;
        public int Favor, Trust, Lewd;
        public string UseItem;              // an item it spends (a snack you share)
        public Action<SaveData.Heroine> Then;  // anything else (a request, a walk, a talk somewhere quieter)
    }

    /// <summary>
    /// Conversation by scene, on the map: a girl walking past may start a short exchange by herself. An Aside (she says
    /// something in passing; talk to her within a few seconds to react, or let it pass), a One-liner (she stops, one line,
    /// two or three concrete answers), a Favor (she asks you to do something, which becomes a task) or a Walk-and-talk
    /// (she is heading somewhere and asks you to come along: she follows you, and each stop on the way is a short beat).
    /// The form is drawn from her state, and the forms used with her lately count less, so the same girl does not stop you
    /// the same way twice. In the talk screen the forms are Scene Talk, Quiet, Triangle and Callback (<see cref="SceneTalk"/>).
    /// </summary>
    internal static class Forms
    {
        private static CampusState S { get { return Campus.S; } }

        // ================= bookkeeping =================

        public static void Used(SaveData.Heroine h, Form f)
        {
            if (h == null || f == Form.None) return;
            var gs = S.Girl(GirlInfo.Key(h));
            gs.Forms.Add(f.ToString());
            if (gs.Forms.Count > 5) gs.Forms.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Form: " + f + " with " + GirlInfo.FirstName(h) + " (recent " + string.Join(",", gs.Forms.ToArray()) + ")");
        }

        /// <summary>Recent forms with her weigh less: the last one almost never repeats.</summary>
        public static float Fresh(GirlState gs, Form f)
        {
            var name = f.ToString();
            if (gs.Forms.Count > 0 && gs.Forms[gs.Forms.Count - 1] == name) return 0.08f;
            var n = gs.Forms.Count(x => x == name);
            return n == 0 ? 1f : n == 1 ? 0.55f : 0.3f;
        }

        /// <summary>Talk time (the blue bar) a form uses, in points.</summary>
        public static float Cost(Form f)
        {
            switch (f)
            {
                case Form.Aside: return 0f;
                case Form.OneLiner: case Form.Favor: case Form.Quiet: return 0.5f;
                default: return 1f;
            }
        }

        /// <summary>Uses her talk time for a form (whole points go off the game's blue bar).</summary>
        public static void Spend(SaveData.Heroine h, Form f)
        {
            var c = Cost(f);
            if (c > 0f) Conversation.Spend(h, c);
        }

        private static bool Recent(GirlState gs, string id) { return gs.LinesUsed.Contains(id); }

        private static void Remember(GirlState gs, string id)
        {
            gs.LinesUsed.Add(id);
            if (gs.LinesUsed.Count > 14) gs.LinesUsed.RemoveAt(0);
        }

        // ================= asides =================

        internal sealed class AsideNote { public string Girl = ""; public Bit Bit; public float Until; }

        /// <summary>The last thing a girl said in passing: talk to her before it runs out to react (<see cref="SceneTalk"/>).</summary>
        public static AsideNote Aside;

        public static Bit TakeAside(SaveData.Heroine h)
        {
            var a = Aside;
            if (a == null || h == null || a.Girl != GirlInfo.Key(h) || Time.realtimeSinceStartup > a.Until) return null;
            Aside = null;
            return a.Bit;
        }

        // ================= the map: who stops you =================

        private static float _nextCheck, _lastForm = -999f;
        public static string LastMapForm = "";

        /// <summary>About once a second from Calendar.Tick.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.ConversationForms.Value || !Cfg.MapForms.Value || !G.InRoamingPeriod) return;
            if (Duo.Busy || WalkTalk.Active || !Scenes.CanPlayOnMap || G.TalkSceneOpen) return;
            var now = Time.realtimeSinceStartup;
            if (now < _nextCheck) return;
            _nextCheck = now + 4f;
            if (now - _lastForm < Cfg.MapFormGap.Value) return;
            var near = Nearby(3.5f);
            if (near.Count == 0) return;
            if (Random.value > 0.35f) return;
            var n = near[Random.Range(0, near.Count)];
            var f = ChooseMap(n.heroine);
            if (f == Form.None) return;
            var kind = f == Form.Walk ? BeatKind.Personal : BeatKind.Ambient;
            if (!Agenda.May("map " + f, kind, n.heroine)) return;
            if (Play(n, f)) Agenda.Took("map " + f, kind, n.heroine);
        }

        /// <summary>Students close to the player who are walking about (not sitting, not busy with someone).</summary>
        internal static List<NPC> Nearby(float meters)
        {
            var act = G.Act;
            if (act == null || act.Player == null || act.Map == null) return new List<NPC>();
            var p = act.Player.transform.position;
            var stamp = S.Day * 16 + (int)G.CurrentMask;
            return act.npcList.Where(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && GirlInfo.IsRegular(n.heroine)
                                          && (n.transform.position - p).sqrMagnitude < meters * meters && n.AI != null && n.AI.isRunning && !n.isArrival
                                          && S.Girl(GirlInfo.Key(n.heroine)).MapFormStamp != stamp && !Pairs.IsScorned(n.heroine)
                                          && (act.Player.chaser == null || act.Player.chaser != n)).ToList();
        }

        /// <summary>Which form she stops you with (None = she just walks past).</summary>
        public static Form ChooseMap(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var w = new Dictionary<Form, float>();
            w[Form.None] = 1.2f;
            w[Form.Aside] = 2f * Fresh(gs, Form.Aside);
            if (h.talkTime >= 1)
            {
                if (h.favor >= 15 || h.relation >= 1) w[Form.OneLiner] = 2f * Fresh(gs, Form.OneLiner);
                if (Social.Trust(h) >= 25 && !S.Requests.Any(r => r.Source == "favor" && r.Girl == gs.Key && (r.State == RequestState.Active)))
                    w[Form.Favor] = 1.4f * Fresh(gs, Form.Favor);
                if (Cfg.WalkAndTalk.Value && (h.relation >= 1 || h.favor >= 40) && WalkTalk.RouteFor(h).Count >= 2)
                    w[Form.Walk] = (MoodOf(h) == Mood.Lonely || MoodOf(h) == Mood.Bored ? 2f : 1.1f) * Fresh(gs, Form.Walk);
            }
            var roll = Random.value * w.Values.Sum();
            foreach (var kv in w) { roll -= kv.Value; if (roll <= 0f) return kv.Key; }
            return Form.None;
        }

        internal static Mood MoodOf(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.MoodDay == S.Day ? gs.Mood : Mood.None;
        }

        /// <summary>Plays a map form with this girl now.</summary>
        public static bool Play(NPC n, Form f)
        {
            if (n == null || n.heroine == null) return false;
            var h = n.heroine;
            var gs = S.Girl(GirlInfo.Key(h));
            try
            {
                bool ok;
                switch (f)
                {
                    case Form.Aside: ok = PlayAside(n); break;
                    case Form.OneLiner: ok = PlayOneLiner(n); break;
                    case Form.Favor: ok = PlayFavor(n); break;
                    case Form.Walk: ok = PlayWalkAsk(n); break;
                    default: ok = false; break;
                }
                if (!ok) return false;
                gs.MapFormStamp = S.Day * 16 + (int)G.CurrentMask;
                _lastForm = Time.realtimeSinceStartup;
                LastMapForm = f + " " + GirlInfo.FirstName(h);
                Used(h, f);
                return true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); return false; }
        }

        // ---------------- stopping and letting her go ----------------

        /// <summary>Girls stopped by a map scene: kept stopped every frame until <see cref="Let"/>.</summary>
        private static readonly List<NPC> _held = new List<NPC>();
        private static readonly Dictionary<NPC, Vector3> _heldAt = new Dictionary<NPC, Vector3>();

        /// <summary>
        /// She stops and faces you (her AI paused, her path kept for later). Stopped the way the game does it
        /// (<c>move.isStop</c> + AI pause, NPC.cs): setting only <c>agent.isStopped</c> was undone every frame by the mover,
        /// so she slid along her path in the standing pose and even changed rooms (Pitou, 2026-10-11).
        /// </summary>
        internal static void Hold(NPC n)
        {
            try
            {
                if (n == null) return;
                if (!_held.Contains(n)) _held.Add(n);
                _heldAt[n] = n.transform.position;
                // The director stops her, facing you.
                Director.Halt(n, "a scene with you", true);
                var cam = Camera.main;
                if (cam != null && n.chaCtrl != null)
                {
                    n.chaCtrl.ChangeLookEyesTarget(0, cam.transform, 0.5f, 0f, 1f, 2f);
                    n.chaCtrl.ChangeLookNeckTarget(0, cam.transform, 0.5f, 0f, 1f, 0.8f);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Forms hold: " + e.Message); }
        }

        internal static void Let(NPC n)
        {
            try
            {
                if (n == null) return;
                _held.Remove(n);
                Vector3 at;
                if (_heldAt.TryGetValue(n, out at))
                {
                    _heldAt.Remove(n);
                    var moved = (n.transform.position - at).magnitude;
                    CampusLifePlugin.Log.LogInfo("Forms: " + GirlInfo.FirstName(n.heroine) + " held, moved " + moved.ToString("0.00") + " m meanwhile" + (moved > 0.3f ? " (SLID)" : ""));
                }
                Director.Release(n);
                if (n.chaCtrl != null) { n.chaCtrl.ChangeLookEyesPtn(0); n.chaCtrl.ChangeLookNeckPtn(3); }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Forms let: " + e.Message); }
        }

        /// <summary>Every frame: a held girl the director let go of by mistake (a map change) is stopped again; the director keeps her feet (Director.KeepHalted).</summary>
        internal static void KeepHeld()
        {
            if (_held.Count == 0) return;
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                var n = _held[i];
                try
                {
                    if (n == null || n.move == null) { _held.RemoveAt(i); continue; }
                    if (!Director.Halted(n.heroine)) Director.Halt(n, "a scene with you", true);
                }
                catch (Exception e) { _held.RemoveAt(i); CampusLifePlugin.Log.LogDebug("Forms keep held: " + e.Message); }
            }
        }

        // ================= situations: what she is going through right now =================

        private static string T(SaveData.Heroine h, string gentle, string energetic, string cool, string teasing) { return Lines.T(GirlInfo.ToneOf(h), gentle, energetic, cool, teasing); }

        private static string SnackYouCarry(SaveData.Heroine h)
        {
            foreach (var id in new[] { "daifuku", "chocolate", "cookies", "spicy_chips", "bento", "canned_coffee", "sports_drink" })
            {
                if (S.Count(id) <= 0) continue;
                if (id == "spicy_chips" && !GirlInfo.Likes(h, "spicy")) continue;
                return id;
            }
            return null;
        }

        /// <summary>A situation for her: her mood first, then the place, then something about you two. Recent ones are skipped.</summary>
        public static Bit MakeBit(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var all = Bits(h).ToList();
            var fresh = all.Where(b => !Recent(gs, b.Id)).ToList();
            var pick = (fresh.Count > 0 ? fresh : all).FirstOrDefault();
            if (pick != null) Remember(gs, pick.Id);
            return pick;
        }

        private static IEnumerable<Bit> Bits(SaveData.Heroine h)
        {
            var name = GirlInfo.FirstName(h);
            var mood = MoodOf(h);
            var place = Places.CurrentId;
            var snack = SnackYouCarry(h);
            var club = GirlInfo.InNoClub(h) ? "" : GirlInfo.ClubName(h);
            var friend = Social.BestFriend(h);
            var last = Memories.Last(h);
            var list = new List<Bit>();

            if (mood == Mood.Hungry)
            {
                var b = new Bit { Id = "hungry", Narr = name + "'s stomach growls loudly as she passes. She freezes.",
                    Line = T(h, "Ah... p-please pretend you didn't hear that... || Ah! Y-you didn't hear my stomach, right...? || Eek... please forget that sound... || Um... that wasn't me... okay, it was...", "Ugh, I'm STARVING! Is lunch ever gonna come?! || My stomach's yelling at me! Lunch, where are you?! || Food! I need FOOD! || I could eat a whole bento box! Two!", "...You heard nothing. || ...Ignore that. || ...That didn't happen. || ...Forget it.", "Not a word. I mean it. || You heard that? Keep it secret. || One word and you're dead. Cutely. || Pretend you're deaf.") };
                if (snack != null)
                {
                    var r = b.Add("Split your " + Episodes.ItemName(snack) + " with her",
                        T(h, "F-for me? Thank you... it's really good. || For me...? Thank you, it's delicious. || Ah... you shared? That's so kind... || Thank you... this is really nice.", "Whoa, you're a lifesaver! Mmm! || Food! You're amazing! Mmm! || Yesss! Snack hero! || Wow! Thanks! So good!", "...Thanks. I owe you one. || ...Thank you. I'll repay it. || ...Helpful. Thanks. || ...Good timing. Thanks.", "Feeding me now? You're spoiling me~ || Feeding me? How sweet~ || Taming me with snacks? Smart~ || You know the way to my heart~"), 5, 2, "you shared your " + Episodes.ItemName(snack) + " with me");
                    r.UseItem = snack;
                }
                b.Add("Tell her the cafeteria line is short right now",
                    T(h, "Really? Then I'll hurry. Thank you! || Oh! Thank you, I'll go now! || Really? I'll hurry, thanks! || Ah, thank you for telling me!", "Seriously?! I'm running! Bye! || No way?! Gotta run! Thanks! || Dashing! Bye! || Thanks! I'm off!", "Useful. Thanks. || Good to know. Thanks. || Helpful. || Noted. Thanks.", "Oh? Good intel. || Useful tip. || Oho, insider info. || Nice tip."), 2, 1);
                b.Add("Pretend you didn't notice", T(h, "...Thank you for that. || ...That was kind of you. || ...Thanks for that. || ...You're very sweet.", "Hehe, smooth! || Ooh, nice one! || Hehe, slick! || Smooth move!", "...Appreciated. || ...Thanks. || ...Noted. || ...Kind.", "Gentleman, huh. || So gallant. || Such a gentleman~ || Chivalry. Cute."), 1);
                list.Add(b);
            }
            if (mood == Mood.Sleepy)
            {
                var b = new Bit { Id = "sleepy", Narr = name + " shuffles past, eyes half closed, and almost walks into you.",
                    Line = T(h, "Ah, sorry... I barely slept last night... || Sorry... I'm still half asleep... || Ah... I stayed up too late... || Mm... forgive me, I'm sleepy...", "Whoa! Sorry! My brain's still in bed! || Whoa! My brain's still loading! || Sorry! Not awake yet! || Oops! Running on zero sleep!", "...Mm. Morning. Still. || ...Mm. Too early. || ...Not awake yet. || ...Morning. Barely.", "You again. Catch me if I fall asleep, okay? || If I doze off, catch me. || Don't let me fall asleep, okay? || You'll carry me if I pass out, right?") };
                if (S.Count("canned_coffee") > 0)
                {
                    var r = b.Add("Hand her your canned coffee", T(h, "Coffee... you're an angel. || Coffee... thank you so much. || Ah... you're a lifesaver. || Warm coffee... thank you...", "CAFFEINE! I love you! ...the coffee! I love the coffee! || COFFEE! You're the best! ...The coffee is! || Caffeine! Yes! I love it! || Fuel! Thank you!", "...You read my mind. || ...Exactly what I needed. || ...Perfect. || ...You knew.", "Bribing me awake? It's working. || Bribery. Effective. || Waking me with gifts? Clever. || Coffee? You do know me."), 5, 2, "you gave me your coffee when I was half asleep");
                    r.UseItem = "canned_coffee";
                }
                b.Add("Ask if she stayed up gaming again", T(h, "N-not gaming! Reading... until three... || N-no games! Just a book... late... || Reading... I couldn't stop... || A novel... until the sun came up...", "Maaaybe. One more level turned into twelve! || One more round turned into twenty! || Gaming marathon! Oops! || Couldn't stop! So good!", "...Studying. Mostly. || ...Studying. Mainly. || ...Work. Mostly. || ...Reading. Long.", "Wouldn't you like to know what kept me up~ || Secrets keep me up at night~ || Wouldn't you love to know~ || Something fun. Very fun."), 2, 1);
                b.Add("Steady her by the shoulder", T(h, "Th-thanks... I'm awake now. Very awake. || Th-thank you... I'm wide awake now. || Ah... okay, awake! || Thank you... that woke me up.", "Whoa, thanks! Okay, I'm up! || Okay! Awake! Thanks! || Whoa! I'm up now! || Thanks! Ready!", "...Thanks. || ...Thanks. || ...Better. || ...Okay.", "Hands on my shoulders already? || Touchy this morning, are we? || Your hands woke me up. || Already touching me?"), 2, 2);
                list.Add(b);
            }
            if (mood == Mood.Stressed)
            {
                var b = new Bit { Id = "stressed", Narr = name + " walks by muttering to herself, clutching a notebook.",
                    Line = T(h, "Can't fail, can't fail, can't fail... || I can't fail, I can't fail... || Please let me pass, please... || Focus... I have to focus...", "Okay okay okay, formulas, formulas, FORMULAS... || Formulas formulas formulas! || Okay brain, WORK! || Memorize! Now!", "...If I miss this, I'm finished. || ...Failing isn't an option. || ...I have to pass this. || ...Concentrate.", "Don't talk to me. I'm memorizing. ...Okay, you can talk. || Shh. Memorizing. ...Fine, talk. || Quiet, I'm cramming. ...Okay, what? || Busy. ...Unless it's you.") };
                b.Add("Offer to study together later", T(h, "You'd do that? Then... after class? || You'd help? After class, then? || Really...? That would be wonderful. || Thank you... could we study later?", "YES. Please. Save me! || Yes! Save me, please! || A tutor! Yes! || You'd help?! YES!", "...That would help. Thanks. || ...That helps. Thanks. || ...Accepted. || ...Useful. Thanks.", "A study date? Fine. Don't distract me. || Study date? Don't distract me. || Fine. But no funny business. || A tutoring session? Okay~"), 2, 2).Then = Want(RequestKind.Study, "Study with ", " asked you to study with her later. Invite her to study.", PeriodMask.Any);
                b.Add("Give her a thumbs up", T(h, "...Thanks. I'll do my best. || ...Thanks. I'll try my best. || ...Okay. I'll do it. || ...Thank you, I feel braver.", "Yeah! Thumbs up! I got this! || Yeah! I got this! || Thumbs up! Let's go! || You bet!", "...Mm. || ...Mm. || ...Okay. || ...Right.", "Cute. ...Thanks. || Cute. ...Thanks. || Sweet. ...Thanks. || Aw. ...Okay, thanks."), 1, 1);
                b.Add("Tell her she always pulls through", T(h, "You think so...? That helps a little. || You think so...? I feel better. || Really? That helps... || Thank you... that's reassuring.", "You're right! I always do! Kinda! || Right! I usually do! Kinda! || Yeah! You're right! || True! I got this!", "...You've been paying attention. || ...You noticed. || ...Observant. || ...True.", "Flattery. ...Keep going. || Flattery works. Continue. || More, please. || Keep the compliments coming."), 3, 2);
                list.Add(b);
            }
            if (mood == Mood.Bored)
            {
                var b = new Bit { Id = "bored", Narr = name + " drags her feet down the hall, sighing dramatically.",
                    Line = T(h, "Nothing ever happens around here... || It's so quiet today... || Nothing's happening... || I'm a little bored...", "BORED! I'm so bored I could explode! || BORED! So, so bored! || Ugh, nothing to do! || Boredom is killing me!", "...This day is endless. || ...Endless day. || ...Tedious. || ...Slow day.", "Entertain me. That's an order. || Amuse me. || Entertain me. Now. || Be interesting.") };
                b.Add("Suggest racing her to the stairs", T(h, "Eh?! W-wait, that's not fair! || W-wait, that's not fair! || Eh?! Hold on! || No fair...!", "You're ON! Go go go! || You're ON! || Game on! Go! || Race you!", "...Childish. ...Fine. Go. || ...Childish. ...Go. || ...Fine. Begin. || ...Silly. Okay.", "Loser buys drinks. Ready? || Loser pays. Go! || Winner picks the prize. Ready? || Losing costs you. Go."), 3, 1, "we raced down the hall like kids");
                b.Add("Tell her the vending machine ate your coins", T(h, "Oh no... want me to tap it? It works sometimes. || Should I tap it? Sometimes it works. || Oh no... want me to try? || Aw... let me help.", "Pfft! The beast strikes again! || Ha! The machine wins again! || Pfft! Got eaten! || The beast strikes!", "...Kick it on the left side. Trust me. || ...Hit the left side. || ...Tap the side. Works. || ...Left panel. Kick it.", "Ha! Rookie mistake. || Amateur. || Ha! Classic. || Beginner's mistake."), 2);
                b.Add("Shrug: same here", T(h, "Then we're bored together... || Bored together, then... || At least we're bored together... || Then let's be bored together...", "Bored buddies! High five! || Bored buddies! || Bored team! Yay! || Boredom club!", "...Solidarity. || ...Solidarity. || ...Together, then. || ...Shared misery.", "Wow, so helpful. || Wow. Helpful. || Very helpful. Not. || Thanks for nothing."), 1);
                list.Add(b);
            }
            if (mood == Mood.Lonely)
            {
                var b = new Bit { Id = "lonely", Narr = name + " stands by the window alone, glancing up when you come near.",
                    Line = T(h, "Oh... hi. Everyone's busy today, I guess... || Oh... hello. Everyone's busy... || Hi... I'm a bit alone today... || Oh, hi... it's quiet today...", "Hey! Finally, a face I know! || Hey! A familiar face! || Hi! Finally, someone! || Yay! You!", "...Oh. It's you. || ...It's you. || ...Hello. || ...Oh. Hi.", "Look who decided to show up. || Look who showed up. || There you are. || Finally.") };
                b.Add("Say you were hoping to run into her", T(h, "R-really...? That makes me happy. || R-really? I'm happy... || That makes me glad... || Oh... thank you...", "Aww! Really?! Okay, best day! || Aww! Best day ever! || Really?! Yay! || You mean it?!", "...Hm. Good answer. || ...Good answer. || ...Acceptable. || ...Hm. Nice.", "Smooth. I'll pretend I believe you. || Smooth. I'll believe it. || Nice line. || Sweet talker."), 4, 3, "you said you were hoping to run into me");
                b.Add("Ask her to walk with you a bit", T(h, "Okay... I'd like that. || Okay... I'd like that. || Sure... let's go. || I'd love to.", "Yes! Let's go! || Yes! Go go! || Let's go! || Yay!", "...Sure. || ...Sure. || ...Fine. || ...Okay.", "Fine. Lead the way. || Lead on. || After you. || Fine. Go."), 2, 1).Then = AskWalk;
                b.Add("Wave and keep going", T(h, "...Oh. Bye. || ...Oh. Okay. Bye. || ...Bye. || ...I see. Bye.", "Hey, wait! ...Oh. || Wait! ...Oh. Okay. || Aw. Bye. || Huh? ...Oh.", "...Right. || ...Right. || ...Fine. || ...Bye.", "Tch. Busy man. || Busy man. Tch. || Running away? || Tch. Fine."), -1);
                list.Add(b);
            }
            if (mood == Mood.Flirty)
            {
                var b = new Bit { Id = "flirty", Narr = name + " slows down as you pass, with a little smile.",
                    Line = T(h, "Ah... you look nice today... || Ah... you look nice... || Um... hi. You look good today... || Oh... hello... nice outfit...", "Hey you~ Notice anything different? || Hey you~ See anything new? || Notice something?! || Hiii~ Look at me!", "...You're staring. Not that I mind. || ...You're staring. It's fine. || ...Looking? Okay. || ...Eyes up. Or don't.", "Oh, it's you~ Miss me? || Missed me? || Hello there~ || There's my favorite~") };
                b.Add("Tell her she looks great today", T(h, "Eh?! Th-thank you...! || Th-thank you...! || Eh?! You noticed...! || Ah... thank you...!", "I KNEW you'd notice! || You noticed! Yay! || I KNEW it! || Yes! Thank you!", "...Obviously. But thanks. || ...Of course. Thanks. || ...Naturally. Thanks. || ...Hm. Thanks.", "Took you long enough~ || Finally noticed~ || Took you ages~ || About time~"), 4, 1).Lewd = 3;
                b.Add("Wink back", T(h, "!!! ...You're so bold... || ...So bold... || !!! Wh-what... || ...You're too forward...", "Ooh, smooth operator! || Ooh, smooth! || Whoa, slick! || Ha! Bold!", "...Hm. || ...Hm. || ...Interesting. || ...Bold.", "Oh? Two can play that game. || Oh? My turn. || Playing that game? || I can do that too."), 2).Lewd = 5;
                b.Add("Ask what she's so happy about", T(h, "N-nothing... just a good day. || Nothing... just a nice day. || J-just happy today... || Nothing special...", "Secret! Hehe! || A secret! || It's a secret! Hehe! || Not telling!", "...Nothing you need to know. Yet. || ...Not yet. || ...Later. || ...You'll see.", "Maybe it's you. Maybe. || Maybe you're the reason. || Could be you. || Who knows~ You?"), 2, 1);
                list.Add(b);
            }
            if (mood == Mood.Upset)
            {
                var b = new Bit { Id = "upset", Narr = name + " turns her face away as you come near. Her eyes are red.",
                    Line = T(h, "...Please don't look at me right now. || ...Please don't look right now. || ...Not now, please. || ...I'm sorry, don't look.", "I'm FINE. Totally fine. Go away. ...Sorry. || I'm fine! Go! ...Sorry. || Totally fine! ...Not really. || Leave! ...Sorry.", "...Not now. || ...Not now. || ...Leave it. || ...Later.", "Don't. Just don't. || Don't. || Not today. || Don't even.") };
                b.Add("Quietly hand her a tissue", T(h, "...Thank you. || ...Thanks. || ...Thank you so much. || ...Thanks...", "...Thanks. Sorry. || ...Thanks. Sorry. || ...Thank you. || ...Thanks. Really.", "...Mm. || ...Mm. || ...Thanks. || ...Okay.", "...Thanks. || ...Thanks. || ...Fine. Thanks. || ...Okay. Thanks."), 3, 3, "you handed me a tissue without a word");
                b.Add("Ask if she wants to talk later", T(h, "...Maybe. Later. || ...Later, maybe. || ...Maybe later. || ...Another time.", "...Yeah. Later. Okay? || ...Later. Okay? || ...Yeah. Later. || ...Maybe later.", "...Perhaps. || ...Possibly. || ...Later. || ...Maybe.", "...We'll see. || ...Maybe. || ...Perhaps. || ...We'll see."), 1, 2).Then = x => S.Girl(GirlInfo.Key(x)).Pending = "Quiet:upset";
                b.Add("Give her space", T(h, "... || ... || ...Mm. || ...", "... || ... || ...Hm. || ...", "... || ... || ...Mm. || ...", "... || ... || ...Hm. || ..."), 0, 1);
                list.Add(b);
            }
            if (mood == Mood.Cheerful)
            {
                var b = new Bit { Id = "cheerful", Narr = name + " is humming as she walks, practically bouncing.",
                    Line = T(h, "Oh! Good morning... I mean, hello! Hehe. || Oh! Good morning... er, hello! || Ah, hi! Today's nice, isn't it? || Hello! Hehe.", "Hiii! Isn't today great?! || Hiii! Today is awesome! || Hey! Great day, right?! || Hello hello! Best day!", "...Hello. It's a decent day. || ...Hello. Decent day. || ...Hi. Not bad today. || ...Hello. Pleasant.", "Well, aren't you lucky to see me in a good mood. || Lucky you, I'm in a good mood. || You caught me happy. Lucky. || Good mood today. Enjoy it.") };
                b.Add("Ask what's got her in such a good mood", T(h, "My cat learned a new trick! ...Is that silly? || My cat did something cute! ...Silly? || I found a pretty flower! || The baker gave me extra bread!", "I got the last melon bread! Victory! || Last melon bread! Mine! || I won the raffle! || Top score! Victory!", "...Someone finally returned my book. || ...My book came back. || ...Good grade. That's all. || ...A small win.", "Wouldn't you like to know~ || Secret~ || Not telling~ || Guess~"), 2, 1);
                b.Add("Hum along", T(h, "Ehehe, you know this song? || You know this song? Hehe. || Ah, you like this song too? || Hehe, sing with me?", "Duet! Duet! || Duet! Now! || Sing with me! || Karaoke time!", "...Off-key. But fine. || ...Off-key. Fine. || ...Not bad. || ...Mediocre. Continue.", "Oh no. Never sing again. ...Kidding. || Never sing again. ...Kidding. || Awful. ...Do it again. || Terrible. I love it."), 3, 0, "you hummed along with me in the hallway");
                b.Add("Return the good mood with a grin", T(h, "Hehe. || Hehe. || Ehehe. || Fufu.", "Yeah! || Yeah! || Woo! || Yay!", "...Mm. || ...Mm. || ...Heh. || ...", "Heh. || Heh. || Hm~ || Fufu."), 1);
                list.Add(b);
            }

            // The place.
            switch (place)
            {
                case "library":
                    list.Add(PlaceBit(h, "lib", name + " hugs a stack of books against her chest.", T(h, "Shh... I found the book I was looking for! || Shh... I found my book! || Shh... found it! || Quiet... look, I found it!", "Library voice! ...I found it! || Library voice! ...Found it! || Whisper! ...Got it! || Shh! ...Yes!", "...Quiet. I'm in the middle of a chapter. || ...Quiet. Reading. || ...Shh. Chapter. || ...Busy reading.", "Shh. Or I'll shush you. || Shh. Or else. || Hush~ || Shh. Behave."),
                        "Ask what she's reading", T(h, "A mystery... I can't tell you the ending! || A mystery... no spoilers! || It's a romance... don't laugh. || A fairy tale... it's sweet.", "It's got DRAGONS! || DRAGONS! And swords! || Space battles! || It has a giant robot!", "...Poetry. Don't laugh. || ...Poetry. No laughing. || ...Philosophy. || ...History.", "Something too mature for you. || Too mature for you. || Something spicy. || Not for kids."), 2,
                        "Offer to carry the stack", T(h, "Ah, thank you... they're heavy. || Thank you... they're so heavy. || Ah... thank you for carrying them. || You're kind... thank you.", "My hero! || My hero! || Strong! Thanks! || Yay! Helper!", "...Fine. Careful. || ...Careful. Thanks. || ...Fine. Don't drop them. || ...Thanks. Gently.", "Muscles. Noted. || Nice arms. || Strong, huh. || Muscles. Noted again."), 3, "you carried my books in the library"));
                    break;
                case "gym":
                case "field":
                case "pool":
                    list.Add(PlaceBit(h, "sport", name + " wipes sweat from her forehead, catching her breath.", T(h, "Hah... hah... practice is tough today... || Hah... practice is hard... || Phew... so tired... || Hah... hah... I'm trying...", "Phew! One more lap and I'm a legend! || One more lap! Legend! || Phew! Almost there! || Go go go!", "...New personal record. Almost. || ...Almost a record. || ...Close to my best. || ...Improving.", "Like what you see? I'm in top form. || Enjoying the view? || Like this? || Top form. Watch."),
                        "Cheer her on", T(h, "Thank you... that helps! || Thank you... that helps! || Thanks... I'll keep going! || That's encouraging...!", "YEAH! Watch me! || YEAH! Watch! || Woo! More! || Thanks! Faster!", "...Hm. Thanks. || ...Thanks. || ...Hm. Good. || ...Noted.", "Louder~ || Louder, please~ || More cheering~ || Again~"), 3,
                        "Offer her your water", T(h, "Thank you... it's cold! || Thank you... it's so cold! || Ah... refreshing! || Thank you... I needed that!", "Gulp gulp! Ahh! || Gulp! Ahh! || Water! Yes! || Ahhh! Revived!", "...Appreciated. || ...Thanks. || ...Helpful. || ...Good.", "Indirect kiss, you know. || Indirect kiss~ || You drank from this, right? || Sharing a bottle? Bold."), 3, "you gave me water after practice"));
                    break;
                case "rooftop":
                    list.Add(PlaceBit(h, "roof", name + " leans on the railing, her hair moving in the wind.", T(h, "The wind feels nice up here... || The breeze is nice... || It's peaceful up here... || I love the wind here...", "You can see the whole town! Look! || Look! The whole town! || What a view! || So high up!", "...It's quiet. That's why I come here. || ...Quiet. That's why. || ...Peaceful. || ...I like it here.", "Hiding up here too, huh? || Hiding too? || Skipping, huh? || You too?"),
                        "Stand beside her and enjoy the view", T(h, "...This is nice. || ...This is lovely. || ...So nice. || ...I like this.", "See? Best spot ever! || Best spot! || See? Perfect! || Awesome, right?!", "...You can stay. || ...Stay. || ...You may stay. || ...Fine. Stay.", "Fine, you can share my spot. || Sharing my spot. Lucky. || My spot. You're allowed. || Fine. Share."), 3,
                        "Point out the clouds", T(h, "That one looks like a cat! || That one's a bunny! || That one looks like a cake! || Look, a heart!", "That one's a dragon! || A dragon! || A spaceship! || A giant whale!", "...It's just a cloud. || ...A cloud. || ...Just vapor. || ...Water. In air.", "That one looks like you. Sort of dumb. || That one looks like you. Goofy. || That one's dumb. Like you. || That's you. Sort of."), 2, "we watched the clouds on the rooftop"));
                    break;
                case "cafeteria":
                    list.Add(PlaceBit(h, "cafe", name + " stares at the menu board like it's an exam.", T(h, "Curry or udon... I can't decide... || Curry or udon... hmm... || I can't choose... || Everything looks good...", "Everything! I want EVERYTHING! || ALL of it! || Everything! || I want it all!", "...The set lunch. Probably. || ...Set lunch. || ...The usual. || ...Set meal.", "Choose for me. If it's wrong, you pay. || Pick for me. Wrong pick, you pay. || You choose. Choose well. || Decide. Or pay."),
                        "Recommend the curry", T(h, "Curry it is! Thank you! || Curry, then! Thanks! || Curry! Okay! || Curry sounds good!", "Curry! Good call! || Curry! Nice! || Curry! Yes! || Good pick!", "...Acceptable. || ...Fine. || ...Acceptable. || ...Okay.", "Hm. If it's bad, I'm blaming you. || Bad? Your fault. || If it's bad, you're paying. || We'll see."), 2,
                        "Say you'll buy her a drink", T(h, "Eh? You don't have to...! || Eh? You don't have to! || Ah... really? Thank you. || Oh... you're kind...", "Free drink! You're the best! || Free drink! Best! || Yay! Thanks! || Awesome!", "...You don't have to. ...Thanks. || ...Unnecessary. ...Thanks. || ...You didn't have to. || ...Thanks.", "Buying me things? Interesting. || Spoiling me? || Gifts? Interesting. || Buying my affection?"), 3, "you bought me a drink in the cafeteria"));
                    break;
                case "courtyard":
                case "park":
                    list.Add(PlaceBit(h, "yard", name + " is crouching by the flower beds.", T(h, "Look... the flowers are blooming. || Look, the flowers opened today... || The buds finally bloomed... || Aren't the flowers pretty today?", "A cat was here! It ran off! || There was a kitty! It ran away! || A cat! Right here! Gone now! || Did you see the cat?!", "...Someone stepped on these. Barbarians. || ...Someone trampled these. || ...Careless people. || ...These need water.", "Don't tell anyone I like flowers. || Don't spread it around, but I like flowers. || My secret: flowers. Tell no one. || You saw nothing. I'm not cute."),
                        "Crouch down beside her", T(h, "They're pretty, right? || So pretty, right? || Aren't they lovely? || They make me happy...", "Shh! Maybe the cat comes back! || Quiet! The cat might return! || Shh! Kitty watch! || Wait for the cat!", "...You may look. || ...You can look. || ...Look, then. || ...Fine. Look.", "Secret garden club. Two members. || Garden club: you and me. || Two-member club. Exclusive. || Secret club. You're in."), 3,
                        "Ask what kind they are", T(h, "Cosmos, I think... || Cosmos, maybe... || I think they're cosmos... || Um... cosmos?", "No idea! Pretty ones! || Dunno! Pretty ones! || No clue! Colorful! || The nice ones!", "...Marigolds. || ...Marigolds. Obviously. || ...Marigolds. || ...Those are marigolds.", "The pretty kind, obviously. || The pretty ones. Duh. || Pretty ones. Like me. || The nice kind."), 2, "we looked at the flowers in the courtyard"));
                    break;
                case "clubroom":
                    if (club != "")
                        list.Add(PlaceBit(h, "club", name + " is carrying a box of club stuff.", T(h, "The " + club + " is getting busy... || The " + club + " has so much to do... || It's busy at the " + club + "... || The " + club + " needs help today...", "Big day for the " + club + "! || Huge day for the " + club + "! || The " + club + " is buzzing! || Busy " + club + " day!", "...The " + club + " never runs out of chores. || ...The " + club + "'s chores never end. || ...Endless " + club + " work. || ...The " + club + ". Always busy.", "The " + club + " needs a strong pair of arms. Hint. || The " + club + " could use some muscle. Hint. || The " + club + " needs a helper. You. || Strong arms wanted at the " + club + "."),
                            "Help her carry the box", T(h, "Thank you so much! || Thank you! || Ah, thanks so much! || You're a big help!", "Teamwork! || Team power! || Go team! || We did it!", "...Thanks. || ...Appreciated. || ...Thanks. || ...Good work.", "Good boy~ || Good helper~ || Obedient~ || Well done~"), 3,
                            "Ask how the club is doing", T(h, "We're doing well, I think! || It's going well, I think! || We're doing okay! || Pretty good!", "We're unstoppable! || Unstoppable! || We're on fire! || Crushing it!", "...Fine. || ...Adequate. || ...Fine. || ...Okay.", "Thriving. Thanks to me. || Great. Because of me. || Thriving. Obviously. || Perfect. My doing."), 2, "you helped me carry the club box"));
                    break;
                case "classroom":
                    list.Add(PlaceBit(h, "class", name + " is stretching beside her desk.", T(h, "Today's lesson was so long... || That class was so long... || I'm tired from that lesson... || So much to remember...", "My brain is FULL! || Brain overflow! || Too much info! || My head's spinning!", "...That lecture could have been an email. || ...Could've been a handout. || ...Dull lecture. || ...Waste of an hour.", "Did you sleep through it too? || Slept through it too? || You dozed off, right? || Napping buddies?"),
                        "Ask if she got today's assignments", T(h, "Mm. Want to compare later? || Compare notes later? || Want to study together? || Shall we check answers?", "Assignments?! There was ASSIGNMENTS?! || Wait, ASSIGNMENTS?! || There's assignments?! || Since when?!", "...Page 34. Don't forget. || ...Page 34. || ...Due tomorrow. Page 34. || ...Remember page 34.", "Copying is cheating~ ...Maybe. || Copying is bad~ ...Maybe. || Cheating? Never~ ...Okay maybe. || Let me see yours~"), 2,
                        "Joke about the professor's tie", T(h, "Hehe... it had ducks on it. || Hehe... ducks... || The ducks were cute... || Hehe, those ducks!", "THE DUCKS! I saw them! || DUCKS! I saw! || The duck tie! || Ducks everywhere!", "...It was a crime. || ...A tragedy. || ...Unforgivable. || ...Awful.", "Fashion victim. || Fashion disaster. || What a look. || Style crime."), 2, "we laughed about the professor's duck tie"));
                    break;
            }

            // Something about the two of you.
            if (last != null && S.Day - last.Day >= 1)
                list.Add(PlaceBit(h, "mem" + last.Id, name + " stops when she sees you, as if remembering something.", T(h, "Ah... about " + Memories.When(last) + "... || Um... about " + Memories.When(last) + "..." + " || " + Memories.When(last) + "... I remember it. || Ah, " + Memories.When(last) + " was nice...", "Hey! " + Memories.When(last) + " was fun!" + " || " + Memories.When(last) + " was so fun! || Remember " + Memories.When(last) + "?! || Hey! " + Memories.When(last) + "! Again?", "..." + Memories.When(last) + ". I remember. || ..." + Memories.When(last) + ". Yes. || ...About " + Memories.When(last) + ". || ...I recall " + Memories.When(last) + ".", "Hey, about " + Memories.When(last) + "~ || So, " + Memories.When(last) + "~ || Remember " + Memories.When(last) + "?~ || About " + Memories.When(last) + "... hm~"),
                    "Smile: you remember too", T(h, "Hehe... me too. || Hehe... same. || Me too... || I'm glad...", "Right?! || Right?! || Yes! || Totally!", "...Good. || ...Good. || ...Same. || ...Fine.", "Of course you do. || Obviously. || Naturally. || Of course."), 3,
                    "Say you'd like to do it again", T(h, "...I'd like that. || ...I'd love that. || ...Okay. || ...Yes.", "Anytime! || Anytime! || Sure thing! || Whenever!", "...Maybe. || ...Perhaps. || ...Maybe. || ...Possibly.", "Maybe. If you're lucky. || If you're lucky. || We'll see. || Maybe~"), 2, null));
            if (friend != null)
                list.Add(PlaceBit(h, "friend" + GirlInfo.Key(friend), name + " is looking around, phone in hand.", T(h, "Have you seen " + GirlInfo.FirstName(friend) + "? I can't find her... || Have you seen " + GirlInfo.FirstName(friend) + "? I'm looking for her... || Um, where's " + GirlInfo.FirstName(friend) + "...? || I can't find " + GirlInfo.FirstName(friend) + "...", "Where's " + GirlInfo.FirstName(friend) + "?! We had plans!" + " || " + GirlInfo.FirstName(friend) + "! Seen her?! || Where'd " + GirlInfo.FirstName(friend) + " go?! || Help! Lost " + GirlInfo.FirstName(friend) + "!", "...Have you seen " + GirlInfo.FirstName(friend) + "? || ..." + GirlInfo.FirstName(friend) + ". Seen her? || ...Where's " + GirlInfo.FirstName(friend) + "? || ...Looking for " + GirlInfo.FirstName(friend) + ".", "Seen " + GirlInfo.FirstName(friend) + "? She owes me a juice. || Where's " + GirlInfo.FirstName(friend) + "? She owes me." + " || " + GirlInfo.FirstName(friend) + " is hiding from me. || Find " + GirlInfo.FirstName(friend) + " for me."),
                    "Say you'll tell her if you see her", T(h, "Thank you! || Thanks! || Thank you so much! || Ah, thank you!", "You're the best! || Awesome! || Thanks! || Yay!", "...Thanks. || ...Thanks. || ...Good. || ...Noted.", "Good. Report back. || Good. Keep me posted. || Nice. Report in. || Useful."), 2,
                    "Tease: so you two are inseparable", T(h, "Hehe... we kind of are. || Hehe... we are. || Yes... best friends. || Hehe, kind of.", "Best friends forever! || BFFs! || Best friends! || Forever!", "...Don't make it weird. || ...Not weird, please. || ...Friends. || ...Don't.", "Jealous? || Jealous? || Envious? || Hm? Jealous?"), 2, null));

            // Always something.
            list.Add(PlaceBit(h, "hello", name + " gives you a small wave as she comes by.", T(h, "Oh, hello... || Oh, hi... || Hello... || Ah, hi...", "Heya! || Hey! || Yo! || Hiya!", "...Hey. || ...Hi. || ...Hm. || ...Hello.", "Yo. || Hey. || Sup. || Hi."),
                "Wave back with a smile", T(h, "Hehe. || Hehe. || Fufu. || Ehehe.", "Hehe! Bye! || Bye! || See ya! || Later!", "...Mm. || ...Mm. || ...Bye. || ...", "Cute. || Adorable. || Sweet. || Cute~"), 1,
                "Ask where she's off to", T(h, "Just... around. Hehe. || Just around... || Wherever... hehe. || Nowhere special.", "Adventure! Probably the vending machine! || Adventure! Vending machine! || Exploring! Snacks! || Quest for snacks!", "...Somewhere quiet. || ...Quiet places. || ...Somewhere calm. || ...Away.", "Somewhere you're not. Kidding. || Away from you. Kidding. || Secret. || Places."), 2, null));
            return list;
        }

        private static Bit PlaceBit(SaveData.Heroine h, string id, string narr, string line, string l1, string h1, int f1, string l2, string h2, int f2, string memory)
        {
            var b = new Bit { Id = id, Narr = narr, Line = line };
            b.Add(l1, h1, f1, 1);
            b.Add(l2, h2, f2, 1, memory);
            return b;
        }

        private static Action<SaveData.Heroine> Want(RequestKind kind, string title, string text, PeriodMask when)
        {
            return h =>
            {
                var name = GirlInfo.FirstName(h);
                var r = Board.Make(kind, h, title + name, name + text, 8, when);
                r.DeadlineDay = S.Day + 1;
                r.LearnedFrom = "her";
                Board.Add(r);
                Toasts.Show("New request: " + r.Title + ".");
            };
        }

        private static void AskWalk(SaveData.Heroine h)
        {
            var n = G.Act != null ? G.Act.npcList.FirstOrDefault(x => x != null && x.heroine == h) : null;
            if (n != null) WalkTalk.Start(n, WalkTalk.RouteFor(h));
        }

        /// <summary>Applies one reply (favor, trust, H, item, memory, follow-up).</summary>
        public static void Apply(SaveData.Heroine h, Reply r, Form f)
        {
            if (h == null || r == null) return;
            if (!string.IsNullOrEmpty(r.UseItem) && S.Count(r.UseItem) > 0) S.AddItem(r.UseItem, -1);
            if (r.Favor != 0 || r.Lewd != 0) Campus.Apply(new Rewards { Favor = r.Favor, Lewd = r.Lewd }, h);
            if (r.Trust != 0) Social.AddTrust(h, r.Trust);
            var gs = S.Girl(GirlInfo.Key(h));
            gs.Attention = Mathf.Clamp(gs.Attention + 5, 0, 100);
            Social.RecordTalk(h);
            if (!string.IsNullOrEmpty(r.Memory)) Memories.Add(h, f.ToString().ToLowerInvariant(), r.Memory, r.Favor >= 0 ? 1 : -1);
            if (r.Then != null) { try { r.Then(h); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } }
            if (r.Favor > 0) Toasts.Show(GirlInfo.FirstName(h) + (r.Favor >= 4 ? " lights up." : " smiles.") + " (+favor)");
            else if (r.Favor < 0) Toasts.Show(GirlInfo.FirstName(h) + " looks a little let down.");
        }

        // ================= Aside =================

        private static bool PlayAside(NPC n)
        {
            var h = n.heroine;
            var bit = MakeBit(h);
            if (bit == null) return false;
            Aside = new AsideNote { Girl = GirlInfo.Key(h), Bit = bit, Until = Time.realtimeSinceStartup + 12f };
            Toasts.Show(GirlInfo.FirstName(h) + " (passing by): \"" + bit.Line + "\"  - talk to her now to react.");
            return true;
        }

        // ================= One-liner =================

        private static bool PlayOneLiner(NPC n)
        {
            var h = n.heroine;
            var bit = MakeBit(h);
            if (bit == null) return false;
            Hold(n);
            var b = new SceneBuilder(null, false);
            b.Narr(bit.Narr);
            b.Say(h, bit.Line);
            var args = new List<string>();
            foreach (var r in bit.Replies.Take(3)) { args.Add(r.Label); args.Add(r.Tag); }
            b.Choice(args.ToArray());
            foreach (var r in bit.Replies.Take(3))
            {
                b.Branch(r.Tag);
                b.Say(h, r.Her);
                b.End();
            }
            if (!Scenes.PlayOnMap(b, c =>
            {
                Let(n);
                var r = bit.Replies.FirstOrDefault(x => x.Tag == c);
                Apply(h, r, Form.OneLiner);
                Spend(h, Form.OneLiner);
            }))
            {
                Let(n);
                return false;
            }
            return true;
        }

        // ================= Favor =================

        private static bool PlayFavor(NPC n)
        {
            var h = n.heroine;
            var name = GirlInfo.FirstName(h);
            var friend = Social.FriendsOf(h, 35).FirstOrDefault(f => f != h && GirlInfo.IsSpawned(f));
            var roll = Random.value;
            string ask, kind;
            if (friend != null && roll < 0.45f)
            {
                kind = "message";
                var fn = GirlInfo.FirstName(friend);
                var what = Lines.Pick("I can't make it to lunch with her today", "I found her lost eraser", "the club meeting moved to tomorrow", "I'm sorry about yesterday");
                ask = T(h, "Um... could you tell " + fn + " that " + what + "? I can't find her... || Could you tell " + fn + " that " + what + "? I can't find her... || Um... please tell " + fn + " that " + what + "? || If you see " + fn + ", could you say that " + what + "?", "Hey! Tell " + fn + " that " + what + ", okay? I gotta run! || Tell " + fn + " that " + what + "! Gotta go! || Message for " + fn + ": " + what + "! Thanks! || Quick! Tell " + fn + " that " + what + "!",
                    "Tell " + fn + " that " + what + ". If you see her. || If you see " + fn + ", tell her " + what + "." + " || " + fn + " should know that " + what + ". Tell her. || Pass it on to " + fn + ": " + what + ".", "Be my messenger. Tell " + fn + " that " + what + ". || Messenger duty. Tell " + fn + " that " + what + ". || You'll tell " + fn + " that " + what + ". Thanks. || Be useful: tell " + fn + " that " + what + ".");
                var r = Board.Make(RequestKind.Talk, h, "Message for " + fn, name + " asked you to tell " + fn + " that " + what + ". Talk to " + fn + ", then " + name + " will thank you.", 7);
                r.Source = "favor"; r.Other = GirlInfo.Key(friend); r.Text = name + " asked you to tell " + fn + " that " + what + "."; r.Item = what;
                r.DeadlineDay = S.Day + 1; r.LearnedFrom = "her"; r.Button = "Pass on " + name + "'s message";
                return AskFavor(n, ask, r);
            }
            if (roll < 0.75f)
            {
                kind = "fetch";
                var like = GirlInfo.LikesOf(h).FirstOrDefault(l => l == "sweets" || l == "coffee" || l == "food" || l == "spicy") ?? "sweets";
                ask = T(h, "If you go by the store... could you get me something " + GirlInfo.LikeLabel(like) + "? I'll pay you back... someday. || Could you buy something " + GirlInfo.LikeLabel(like) + " at the store? I'll pay you back... || Um... something " + GirlInfo.LikeLabel(like) + " from the store? Please? || If it's no trouble... something " + GirlInfo.LikeLabel(like) + "?", "Snack run! Pleeease? Something " + GirlInfo.LikeLabel(like) + "! || Snacks! Something " + GirlInfo.LikeLabel(like) + "! Please! || Store run! " + GirlInfo.LikeLabel(like) + " stuff! || Get me something " + GirlInfo.LikeLabel(like) + "! Pretty please!",
                    "If you pass the store, something " + GirlInfo.LikeLabel(like) + ". I'd be grateful. || Something " + GirlInfo.LikeLabel(like) + " from the store. If convenient. || I'd appreciate something " + GirlInfo.LikeLabel(like) + ". || Store. Something " + GirlInfo.LikeLabel(like) + ". Thanks.", "Fetch me something " + GirlInfo.LikeLabel(like) + ". Consider it an honor. || Bring me something " + GirlInfo.LikeLabel(like) + ". Lucky you. || Something " + GirlInfo.LikeLabel(like) + ", servant. || Go. Something " + GirlInfo.LikeLabel(like) + ". Hurry~");
                var r = Board.Make(RequestKind.Gift, h, "Snack for " + name, name + " asked you to bring her something (" + GirlInfo.LikeLabel(like) + "). The store is in the cafeteria.", 6);
                r.Item = like; r.Source = "favor"; r.DeadlineDay = S.Day + 1; r.LearnedFrom = "her";
                return AskFavor(n, ask, r);
            }
            kind = "check";
            var e = Board.Errands[Random.Range(0, Board.Errands.Length)];
            ask = T(h, "Um, could you do me a favor? " + e[1] + " || " + "Could you help me? " + (e[1]) + " || " + "Um, a small favor? " + (e[1]) + " || " + "Please...? " + (e[1]), "Favor! Favor! " + e[1] + " || " + "Help! " + (e[1]) + " || " + "Pleeease! " + (e[1]) + " || " + "Quick favor! " + (e[1]), "I need a hand. " + e[1] + " || " + "A request. " + (e[1]) + " || " + "Help needed. " + (e[1]) + " || " + "Assist me. " + (e[1]), "You're free, right? " + e[1] + " || " + "You're not busy. " + (e[1]) + " || " + "Perfect timing. " + (e[1]) + " || " + "Do me a favor. " + (e[1]));
            var rq = Board.Make(RequestKind.Errand, h, "Favor for " + name, name + " asked a favor: " + e[1] + " Go to the " + Places.Name(e[0]) + ", then tell her.", 7);
            rq.Place = e[0]; rq.Source = "favor"; rq.DeadlineDay = S.Day + 1; rq.LearnedFrom = "her";
            CampusLifePlugin.Log.LogDebug("favor kind " + kind);
            return AskFavor(n, ask, rq);
        }

        private static bool AskFavor(NPC n, string ask, Request r)
        {
            var h = n.heroine;
            Hold(n);
            var gs = S.Girl(GirlInfo.Key(h));
            var heavy = Board.HiddenFor(gs.Key).Any() && Social.Trust(h) >= 30;
            var b = new SceneBuilder(null, false);
            b.Narr(GirlInfo.FirstName(h) + " hurries over to you.");
            b.Say(h, ask);
            var opts = new List<string> { "Sure, leave it to me.", "yes", "Sorry, not today.", "no" };
            if (heavy) { opts.Add("Is something else bothering you?"); opts.Add("more"); }
            b.Choice(opts.ToArray());
            b.Branch("yes"); b.Say(h, T(h, "Thank you! Really! || Thank you so much! || Ah, thank you! || You're wonderful!", "You're the best! || You rock! || Best ever! || Thanks a ton!", "...Thanks. I mean it. || ...Thank you. Truly. || ...Appreciated. || ...I owe you.", "Knew I could count on you. || Knew you would. || Reliable. || Good boy.")); b.End();
            b.Branch("no"); b.Say(h, T(h, "Oh... okay. It's fine. || Oh... it's okay. || That's fine... || Okay...", "Aww. Okay! || Aw! Okay! || Darn! Fine! || Okay!", "...Fine. || ...Fine. || ...Understood. || ...Okay.", "Hmph. Your loss. || Your loss. || Hmph. || Pity.")); b.End();
            if (heavy)
            {
                b.Branch("more");
                b.Say(h, T(h, "...You noticed? Can we... talk somewhere a little quieter? || ...You can tell? Somewhere quieter? || ...Could we talk somewhere else? || ...You noticed... not here, please.", "...Ugh, you're sharp. Okay. Not here, though. || ...Sharp. Okay. Not here. || ...Ugh. Fine. Somewhere else. || ...You caught that. Elsewhere.", "...Perceptive. Not here. || ...Observant. Elsewhere. || ...Not here. || ...Somewhere private.", "...Fine. But not in the hallway. || ...Fine. Not in public. || ...Okay. Somewhere quieter. || ...Not here. Come."));
                b.End();
            }
            if (!Scenes.PlayOnMap(b, c =>
            {
                Let(n);
                if (c == "yes")
                {
                    Board.Add(r);
                    Toasts.Show("New favor: " + r.Title + ".");
                    Social.AddTrust(h, 1);
                    Spend(h, Form.Favor);
                }
                else if (c == "more")
                {
                    gs.Pending = "SceneTalk:reveal";
                    Board.Add(r);
                    Escalate(n);
                }
                else if (c == "no") Social.AddTrust(h, -1);
            }))
            {
                Let(n);
                return false;
            }
            return true;
        }

        /// <summary>The map form gets serious: she wants to talk alone, and the talk screen opens with her.</summary>
        public static void Escalate(NPC n)
        {
            try
            {
                if (n == null || G.Act == null || !G.IsFreeRoaming) return;
                CampusLifePlugin.Log.LogInfo("Form escalates: talk screen with " + GirlInfo.FirstName(n.heroine));
                G.Act.SceneEvent(n);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        // ================= Walk-and-talk (the ask) =================

        private static bool PlayWalkAsk(NPC n)
        {
            var h = n.heroine;
            var route = WalkTalk.RouteFor(h);
            if (route.Count < 2) return false;
            Hold(n);
            var dest = Places.Name(route[route.Count - 1]).ToLowerInvariant();
            var b = new SceneBuilder(null, false);
            b.Narr(GirlInfo.FirstName(h) + " falls into step beside you.");
            b.Say(h, T(h, "I'm going to the " + dest + "... would you walk with me? Only if you're free. || I'm heading to the " + dest + "... walk with me? If you're free. || Would you come with me to the " + dest + "? || Um... walk me to the " + dest + "?", "Heading to the " + dest + "! Come with! It's more fun with two! || Off to the " + dest + "! Come along!" + " || " + dest + " time! Walk with me! || Let's go to the " + dest + " together!",
                "I'm on my way to the " + dest + ". Walk with me. If you want. || Going to the " + dest + ". Join me." + " || " + dest + ". Walk with me. || Heading to the " + dest + ". Come.", "Escort me to the " + dest + ". You look like you have nothing better to do. || Walk me to the " + dest + ". You're free. || Escort duty. The " + dest + "." + " || " + dest + ". You're coming."));
            b.Choice("Sure, let's go together.", "go", "Maybe next time.", "no");
            b.Branch("go"); b.Say(h, T(h, "Hehe... okay. This way. || Okay... this way. || Hehe, let's go. || Yay... come on.", "Yay! Let's go! || Let's go! || Woo! Go! || Yay! Move!", "...Good. Keep up. || ...Keep up. || ...This way. || ...Good.", "Good boy. Follow me... or I'll follow you. || Follow me. || Good boy. This way. || Come along.")); b.End();
            b.Branch("no"); b.Say(h, T(h, "Oh... okay. Another time. || Okay... next time. || Another day, then. || Oh... okay.", "Aww. Next time for sure! || Next time! || Aw! Later then! || Okay! Another day!", "...Fine. || ...Fine. || ...Okay. || ...Later.", "Your loss. || Pity. || Your loss. || Hmph.")); b.End();
            if (!Scenes.PlayOnMap(b, c =>
            {
                Let(n);
                if (c == "go") WalkTalk.Start(n, route);
            }))
            {
                Let(n);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A walk with her: she follows you (the game's own "come with me" follow), and you walk her to two or three places on
    /// the way to where she was going. At each stop a short beat plays; you can stop halfway (she goes on alone). The
    /// whole walk is walking: the "Go there" teleport ends it.
    /// </summary>
    internal static class WalkTalk
    {
        private static CampusState S { get { return Campus.S; } }
        private static NPC _npc;
        private static SaveData.Heroine _her;
        private static readonly List<string> Stops = new List<string>();
        private static int _i, _period, _good;
        private static float _started, _lost = -1f, _arrivedAt = -1f;
        public static string LastResult = "";

        public static bool Active { get { return _her != null; } }

        /// <summary>
        /// e o texto ir fluindo enquanto caminha, passando sozinho, sem fazer parar"): one line at a time under the walk bar
        /// (WalkHint), each for a few seconds, then the next; nobody stops. "" when nothing is said.
        /// </summary>
        public static string Flow { get { return Active && Time.realtimeSinceStartup < _flowUntil ? _flow : ""; } }
        private static string _flow = "";
        private static float _flowUntil, _nextChat;
        private static int _chat;
        private static float _baseSpeed = -1f;

        private static void SayAsWeWalk(string who, string line, float seconds = 5f)
        {
            _flow = (string.IsNullOrEmpty(who) ? "" : who + ": ") + line;
            _flowUntil = Time.realtimeSinceStartup + seconds;
            _nextChat = _flowUntil + 4f;
        }

        /// <summary>A little faster than the game's follow, so she keeps up with you (the follow walks at her own pace).</summary>
        internal static void KeepPace()
        {
            try
            {
                var a = _npc != null ? _npc.agent : null;
                if (a == null || !a.enabled) return;
                if (_baseSpeed < 0f) _baseSpeed = a.speed;
                var want = Mathf.Max(_baseSpeed * 1.3f, 2.6f);
                if (a.speed < want) a.speed = want;
            }
            catch { }
        }

        private static void RestorePace(NPC n)
        {
            try { if (n != null && n.agent != null && _baseSpeed > 0f) n.agent.speed = _baseSpeed; } catch { }
            _baseSpeed = -1f;
        }

        /// <summary>Small talk on the way, one line every ~9 s, in her tone; your answers now and then.</summary>
        private static void Chatter()
        {
            if (Time.realtimeSinceStartup < _nextChat) return;
            var h = _her;
            var name = GirlInfo.FirstName(h);
            var here = Places.Name(Places.CurrentId == "" ? "" : Places.CurrentId).ToLowerInvariant();
            var next = Places.Name(NextStop).ToLowerInvariant();
            var lines = new[]
            {
                T("It's nice, walking like this. || I like walking with you... || This is nice. Just walking. || Hehe... it's quiet with you.",
                  "Come on, keep up! || Walking is more fun with two! || Faster, faster! || Hehe! This is fun!",
                  "...You walk fast. Good. || ...Don't dawdle. || ...Quiet walk. I like it. || ...This is fine.",
                  "Stay close. People might think we're on a date~ || Are you matching my pace on purpose? || Walk beside me, not behind. || Hm. You're decent at this."),
                "You: " + Lines.Pick("So, what's at the " + next + "?", "Do you go this way every day?", "Tell me if I'm walking too fast.", "I like this route."),
                T("The " + next + " isn't far now. || We're almost at the " + next + ". || Just a little more to the " + next + ". || The " + next + " is close.",
                  "Next up, the " + next + "! || " + next + ", here we come! || To the " + next + "! || " + next + " next!",
                  "...The " + next + " is next. || ...Next: the " + next + ". || ...Almost at the " + next + ". || ...The " + next + ".",
                  "The " + next + " next. Try to keep up~ || Next, the " + next + ". Don't get lost. || " + next + ". Lead the way. || The " + next + ". You know the way?"),
                T("Do you think people are looking at us...? || Um... thanks for coming with me. || I always walk alone, so this is new. || You're easy to be with.",
                  "Hey hey, guess what happened in class! || I could walk all day! || Race you! ...Kidding! || Isn't campus pretty today?",
                  "...Thank you for the company. || ...You're not bad company. || ...Hm. Nice weather. || ...I don't mind this.",
                  "Admit it, you like walking with me. || Everyone's jealous, I bet. || You'd follow me anywhere, huh~ || Carry my bag next time."),
            };
            var line = lines[_chat++ % lines.Length];
            if (line.StartsWith("You: ", StringComparison.Ordinal)) SayAsWeWalk("You", line.Substring(5), 4f);
            else SayAsWeWalk(name, line);
        }

        public static SaveData.Heroine Her { get { return _her; } }
        public static string NextStop { get { return Active && _i < Stops.Count ? Stops[_i] : ""; } }
        public static int StopIndex { get { return _i; } }
        public static int StopCount { get { return Stops.Count; } }

        /// <summary>Two or three places on the way to where she was going, from her mood, likes and club (never where you are).</summary>
        public static List<string> RouteFor(SaveData.Heroine h)
        {
            var here = Places.CurrentId;
            var pool = new List<string>();
            var mood = Forms.MoodOf(h);
            if (GirlInfo.InSportsClub(h) || GirlInfo.Likes(h, "exercise")) { pool.Add("gym"); pool.Add("field"); }
            if (GirlInfo.Likes(h, "study") || mood == Mood.Stressed) pool.Add("library");
            if (mood == Mood.Hungry || GirlInfo.Likes(h, "food") || GirlInfo.Likes(h, "sweets")) pool.Add("cafeteria");
            if (mood == Mood.Bored || mood == Mood.Lonely || mood == Mood.Flirty) pool.Add("rooftop");
            if (!GirlInfo.InNoClub(h)) pool.Add("clubroom");
            pool.AddRange(new[] { "courtyard", "rooftop", "library", "cafeteria", "pool" });
            var route = new List<string>();
            foreach (var p in pool)
            {
                if (route.Count >= 3) break;
                if (p == here || route.Contains(p) || Places.MapFor(p) < 0) continue;
                route.Add(p);
            }
            if (route.Count > 2 && Random.value < 0.5f) route.RemoveAt(1);
            return route;
        }

        public static bool Start(NPC n, List<string> route)
        {
            try
            {
                if (n == null || route == null || route.Count == 0 || G.Act == null || G.Act.Player == null) return false;
                if (Active) Finish(false);
                _npc = n;
                _her = n.heroine;
                Stops.Clear();
                Stops.AddRange(route);
                _i = 0;
                _good = 0;
                _started = Time.realtimeSinceStartup;
                _lost = -1f;
                _arrivedAt = -1f;
                _period = S.Day * 16 + (int)G.CurrentMask;
                _chat = 0;
                _baseSpeed = -1f;
                _nextChat = Time.realtimeSinceStartup + 4f;
                G.Act.Player.ChaserSet(n);
                Toasts.Show("Walking with " + GirlInfo.FirstName(_her) + ". Stops: " + string.Join(" > ", Stops.Select(Places.Name).ToArray()) + ". She follows you.");
                CampusLifePlugin.Log.LogInfo("Walk-and-talk: " + GirlInfo.FirstName(_her) + " route " + string.Join(",", Stops.ToArray()));
                return true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); _her = null; _npc = null; return false; }
        }

        /// <summary>About once a second from Calendar.Tick.</summary>
        public static void Tick()
        {
            if (!Active) return;
            try
            {
                var act = G.Act;
                if (act == null || act.Player == null) { Finish(false); return; }
                var period = S.Day * 16 + (int)G.CurrentMask;
                if (period != _period || !G.InRoamingPeriod) { Finish(false, "The bell rang. " + GirlInfo.FirstName(_her) + " hurries off."); return; }
                if (Scenes.Running || G.TalkSceneOpen || !G.IsFreeRoaming) return;
                // Lost her (she stopped following): a few seconds of grace, then she goes on alone.
                if (act.Player.chaser != _npc)
                {
                    if (_lost < 0f) _lost = Time.realtimeSinceStartup;
                    else if (Time.realtimeSinceStartup - _lost > 15f) { Finish(false, GirlInfo.FirstName(_her) + " waves and goes on by herself."); return; }
                    if (_npc != null && _npc.mapNo == G.PlayerMapNo && (_npc.transform.position - act.Player.transform.position).sqrMagnitude < 25f) act.Player.ChaserSet(_npc);
                    return;
                }
                _lost = -1f;
                KeepPace();
                Chatter();
                if (Time.realtimeSinceStartup - _started > 420f) { Finish(false, GirlInfo.FirstName(_her) + " laughs: \"We got a little lost, huh?\" and goes on by herself."); return; }
                if (_i >= Stops.Count) { Finish(true); return; }
                var map = Places.MapFor(Stops[_i]);
                if (G.PlayerMapNo != map) { _arrivedAt = -1f; return; }
                if (_arrivedAt < 0f) { _arrivedAt = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - _arrivedAt < 2.5f) return; // the map settles, she comes through the door
                if (_npc.mapNo != G.PlayerMapNo || (_npc.transform.position - act.Player.transform.position).sqrMagnitude > 12f * 12f) return;
                // A stop on the way is said as you walk; the last one is the scene with her answer.
                if (_i < Stops.Count - 1) { FlowStop(); return; }
                if (!Scenes.CanPlayOnMap) return;
                Beat();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); Finish(false); }
        }

        private static string T(string gentle, string energetic, string cool, string teasing) { return Lines.T(GirlInfo.ToneOf(_her), gentle, energetic, cool, teasing); }

        /// <summary>A stop on the way: what she says about it scrolls by while you keep walking.</summary>
        private static void FlowStop()
        {
            var place = Stops[_i];
            var line = Conversation.EverydayStop(_her, place);
            SayAsWeWalk(GirlInfo.FirstName(_her), line, 6f);
            _good++;
            CampusLifePlugin.Log.LogInfo("Walk-and-talk: stop at " + place + " (" + (_i + 1) + "/" + Stops.Count + ") said on the way: " + line);
            _i++;
            _arrivedAt = -1f;
        }

        private static void Beat()
        {
            var h = _her;
            var place = Stops[_i];
            var last = _i == Stops.Count - 1;
            var name = GirlInfo.FirstName(h);
            var b = new SceneBuilder(null, false);
            b.Narr(last ? "You reach the " + Places.Name(place).ToLowerInvariant() + " together." : name + " stops for a moment at the " + Places.Name(place).ToLowerInvariant() + ".");
            b.Say(h, Conversation.EverydayStop(h, place));
            if (!last)
            {
                b.Choice(Reply1(place), "good", "Keep walking with her.", "plain", "Say you have to go.", "leave");
                b.Branch("good"); b.Say(h, T("Hehe... you think so too? || You think so too? Hehe. || Ah, same! || Really? Me too.", "Right?! You get it! || You get it! || Exactly! || Yes!", "...Exactly. || ...Precisely. || ...Correct. || ...Yes.", "Oh? We agree on something. || We agree? Wow. || Oh, agreement. || Finally, agreement.")); b.End();
                b.Branch("plain"); b.Say(h, T("Mm. Let's keep going. || Let's keep walking. || This way... || Let's go on.", "Onward! || Onward! || Let's go! || Keep moving!", "...This way. || ...Here. || ...Continue. || ...This way.", "Keep up. || Keep pace. || Come on. || Don't fall behind.")); b.End();
                b.Branch("leave"); b.Say(h, T("Oh... okay. Thanks for walking with me this far. || Okay... thanks for walking with me. || Oh... thank you for coming this far. || Alright... thank you.", "Aww, already? Okay, thanks! || Already? Okay! Thanks! || Aw! Thanks anyway! || Okay! Thanks!", "...Fine. Thanks for the company. || ...Fine. Thanks. || ...Thank you for the company. || ...Okay. Thanks.", "Bailing on me? Hmph. ...Thanks, though. || Leaving? ...Thanks anyway. || Abandoning me? ...Thanks. || Hmph. Thanks, I guess.")); b.End();
            }
            else
            {
                var pending = !string.IsNullOrEmpty(S.Girl(GirlInfo.Key(h)).Pending) || Board.HiddenFor(GirlInfo.Key(h)).Any();
                b.Say(h, T("This is where I was going... thank you for coming all the way. || Here we are... thank you for coming. || This is it... thank you. || We made it... thank you so much.", "Made it! That was way more fun than walking alone! || We're here! So fun! || Arrived! Best walk! || Made it! Again tomorrow?", "...We're here. That was... nice. || ...Here. That was pleasant. || ...Arrived. Nice walk. || ...We're here. Thanks.", "Here we are. You were tolerable company. || Here. You were decent company. || Arrived. Not bad. || Here we are. Passable."));
                var opts = new List<string> { "Say you'd walk with her again anytime.", "again", "Tell her it was fun.", "fun" };
                if (h.isGirlfriend || h.favor >= 70) { opts.Add("Take her hand for a moment."); opts.Add("hand"); }
                if (pending) { opts.Add("Ask what's really on her mind."); opts.Add("talk"); }
                b.Choice(opts.ToArray());
                b.Branch("again"); b.Say(h, T("...Then I'll ask again. Promise. || ...Then I'll ask you again. || ...I'll invite you again. || ...Promise I'll ask again.", "Deal! Every day! || Deal! || Every day! || Promise!", "...I'll remember you said that. || ...Remembered. || ...Noted. || ...I'll hold that.", "Careful. I'll hold you to that. || I'll hold you to it. || Careful what you promise. || Don't forget.")); b.End();
                b.Branch("fun"); b.Say(h, T("It was. Hehe. || It was. || Hehe, yes. || It really was.", "The best! || Best! || So fun! || Totally!", "...It was. || ...Yes. || ...It was. || ...Indeed.", "It was. Don't let it go to your head. || It was. Don't get cocky. || Yes. Don't gloat. || It was. Calm down.")); b.End();
                b.Branch("hand"); b.Say(h, T("Ah... your hand is warm. || Your hand is so warm... || Ah... warm... || Hehe... warm hand.", "Hehe! Holding hands! || Handholding! || Hehe! Hands! || Yay! Holding hands!", "...Only for a moment. || ...Briefly. || ...Just a moment. || ...For now.", "Bold. ...I'll allow it. || Bold. ...Allowed. || Fine. ...Allowed. || Oh? ...I'll permit it.")); b.End();
                b.Branch("talk"); b.Say(h, T("...You noticed. Can we talk for a bit? Just us. || ...You can tell? Talk with me? Just us? || ...You noticed... can we talk? || ...Could we talk, just the two of us?", "...Ugh, you can tell, huh. Okay. Let's talk. || ...You can tell. Okay. Talk. || ...Ugh. Fine. Let's talk. || ...Busted. Let's talk.", "...Perceptive. Fine. Let's talk. || ...Observant. Let's talk. || ...Fine. Talk. || ...You noticed. Talk, then.", "...Fine. You earned it. Let's talk. || ...You earned it. Talk. || ...Fine. Let's talk. || ...Okay. Talk.")); b.End();
            }
            var at = _i;
            if (!Scenes.PlayOnMap(b, c =>
            {
                if (c == "good" || c == "again" || c == "fun" || c == "hand" || c == "talk") _good++;
                if (c == "leave") { Finish(false, null, true); return; }
                if (c == "hand") Campus.Apply(new Rewards { Favor = 3, Lewd = 3 }, h);
                if (at == Stops.Count - 1)
                {
                    if (c == "talk")
                    {
                        var gs = S.Girl(GirlInfo.Key(h));
                        if (string.IsNullOrEmpty(gs.Pending)) gs.Pending = "SceneTalk:reveal";
                        var npc = _npc;
                        Finish(true);
                        Forms.Escalate(npc);
                        return;
                    }
                    Finish(true);
                }
                else _i++;
                _arrivedAt = -1f;
            })) return;
            CampusLifePlugin.Log.LogInfo("Walk-and-talk: beat at " + place + " (" + (at + 1) + "/" + Stops.Count + ")");
        }

        private static string Reply1(string place)
        {
            switch (place)
            {
                case "gym": return "Say the gym smells like hard work.";
                case "field": return "Promise her a race another day.";
                case "library": return "Whisper that you like it here too.";
                case "cafeteria": return "Offer to buy her a melon bread later.";
                case "rooftop": return "Stand beside her and enjoy the wind.";
                case "clubroom": return "Ask what her club is working on.";
                case "pool": return "Say you'd like to see her swim.";
                case "courtyard": return "Look for the cat with her.";
                default: return "Say it's a nice place.";
            }
        }

        /// <summary>The walk is over (completed or not); she goes back to her own business.</summary>
        public static void Finish(bool completed, string note = null, bool leftHalfway = false)
        {
            var h = _her;
            var n = _npc;
            RestorePace(n);
            _flow = "";
            _her = null;
            _npc = null;
            try { if (G.Act != null && G.Act.Player != null && G.Act.Player.chaser != null && G.Act.Player.chaser == n) G.Act.Player.ChaserEnd(); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Walk end: " + e.Message); }
            if (h == null) return;
            var stops = _i + (completed ? 0 : 0);
            var dest = Stops.Count > 0 ? Places.Name(Stops[Math.Min(Stops.Count - 1, Math.Max(0, completed ? Stops.Count - 1 : _i))]).ToLowerInvariant() : "";
            if (completed || stops > 0 || leftHalfway)
            {
                var favor = 3 + 2 * _good + (completed ? 3 : 0);
                Campus.Apply(new Rewards { Favor = favor }, h);
                Social.AddTrust(h, completed ? 3 : 1);
                var gs = S.Girl(GirlInfo.Key(h));
                gs.Attention = Mathf.Clamp(gs.Attention + 12, 0, 100);
                Social.RecordTalk(h);
                Forms.Spend(h, Form.Walk);
                Memories.Add(h, "walk", completed ? "you walked me all the way to the " + dest : "you walked with me part of the way to the " + dest, 1);
                if (completed) Pairs.NoteTogether(h, null);
            }
            LastResult = (completed ? "completed" : leftHalfway ? "left halfway" : "abandoned") + " with " + GirlInfo.FirstName(h) + " at stop " + (completed ? Stops.Count : _i) + "/" + Stops.Count;
            CampusLifePlugin.Log.LogInfo("Walk-and-talk: " + LastResult);
            if (!string.IsNullOrEmpty(note)) Toasts.Show(note);
            else if (completed) Toasts.Show("Walk with " + GirlInfo.FirstName(h) + ": done. (+favor, +trust)");
        }

        /// <summary>The menu's "Go there" teleport ends the walk (it is a walk).</summary>
        public static void OnTeleport()
        {
            if (Active) Finish(false, GirlInfo.FirstName(_her) + " lost you in the crowd and goes on by herself.");
        }
    }
}
