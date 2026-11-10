using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using ActionGame.Point;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The girls' reactions, decided by the director from the relationship instead of the game's fixed rules ("every
    /// action of the girls' AI has to be regulated by the director"):
    /// <list type="bullet">
    /// <item>Caught alone in H (the game's action 4): it counts as a flagra. A girlfriend (or a bold girl close to you)
    /// does not run: she asks if you want to help (no penalty for "not now", she carries on and you can still go).
    /// The others run as in the game, then go to the changing room to dress again, and later stop you about it.</item>
    /// <item>Two girls caught together (26/27): your girlfriend with a girl who is not one of yours keeps going, they
    /// explain, and you join them or leave them gently in your debt (a one-time pass with your girlfriend; the other
    /// girl says yes to H once). Both like you more either way.</item>
    /// <item>Restroom, changing room, shower (the game's "warning" maps): girls who don't mind you seeing them don't
    /// shriek and don't make it a scandal.</item>
    /// <item>Sleeping on the infirmary bed or the rooftop bench: the director picks who comes (or nobody), and the nap
    /// gives Campus Life's rest.</item>
    /// </list>
    /// Postfixes only, everything in try/catch: on any error the game's own reaction stays.
    /// </summary>
    internal static class Reactions
    {
        private static CampusState S { get { return Campus.S; } }

        private sealed class Session
        {
            public bool Calm;                  // the director decided she (they) do not run
            public bool Asked;                 // the ask/explain scene was queued or played
            public string Kind = "";           // "solo", "pair", "cheat"
            public NPC A, B;                   // A: the one asking (the girlfriend in "cheat"); B: the other girl
        }

        /// <summary>Her decision for the action she is doing now (a new action of the game is a new decision).</summary>
        private sealed class Entry
        {
            public object Result;              // the game's action (AI.result)
            public Session Session;
        }

        private sealed class FollowUp
        {
            public SaveData.Heroine H, Other;
            public float At;
            public bool Sent;
            public int Day;
        }

        private static readonly Dictionary<NPC, Entry> _sessions = new Dictionary<NPC, Entry>();
        private static readonly Queue<Session> _asks = new Queue<Session>();
        private static readonly List<FollowUp> _followUps = new List<FollowUp>();
        private static readonly HashSet<SaveData.Heroine> _mustDress = new HashSet<SaveData.Heroine>();
        private static readonly HashSet<SaveData.Heroine> _calmToast = new HashSet<SaveData.Heroine>();
        private static int _toastStamp = -1;
        private static float _nextTick;

        public static readonly List<string> Recent = new List<string>();

        private static void Note(string what)
        {
            Recent.Add(what);
            if (Recent.Count > 30) Recent.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Reactions: " + what);
        }

        public static void Install(Harmony harmony)
        {
            var sight = AccessTools.PropertyGetter(typeof(NPC), "isSightInPlayer");
            if (sight == null) CampusLifePlugin.Log.LogWarning("NPC.isSightInPlayer not found: girls react as in the game.");
            else harmony.Patch(sight, postfix: new HarmonyMethod(AccessTools.Method(typeof(Reactions), nameof(SightPostfix))));
            var escape = AccessTools.Method(typeof(ActionControl), "EscapeAction");
            if (escape == null) CampusLifePlugin.Log.LogWarning("ActionControl.EscapeAction not found: girls run off as in the game.");
            else harmony.Patch(escape, postfix: new HarmonyMethod(AccessTools.Method(typeof(Reactions), nameof(EscapePostfix))));
            var sleep = AccessTools.Method(typeof(ActionPoint), "GetSleepEventChara");
            if (sleep == null) CampusLifePlugin.Log.LogWarning("ActionPoint.GetSleepEventChara not found: the game picks who comes when you sleep.");
            else harmony.Patch(sleep, postfix: new HarmonyMethod(AccessTools.Method(typeof(Reactions), nameof(SleepPostfix))));
        }

        private static bool On { get { return Campus.On && Cfg.DirectorEnabled.Value && G.Act != null; } }

        // ================= who minds =================

        /// <summary>Doesn't mind you seeing her in H (asks you to help instead of running).</summary>
        public static bool CalmInH(SaveData.Heroine h)
        {
            if (h == null || h.isAnger || Pairs.IsScorned(h)) return false;
            if (h.isGirlfriend) return true;
            if (h.lewdness >= 80 && h.HExperience > SaveData.Heroine.HExperienceKind.不慣れ) return true;
            return h.relation >= 2 && h.lewdness >= 60;
        }

        /// <summary>Doesn't mind you seeing her in the restroom, changing room, shower or in a swimsuit.</summary>
        public static bool CalmWhenSeen(SaveData.Heroine h)
        {
            if (h == null || h.isAnger) return false;
            if (Boys.Is(h)) return true; // a boy does not mind
            if (h.isGirlfriend) return true;
            if (h.relation >= 2 && h.lewdness >= 50) return true;
            return h.lewdness >= 85;
        }

        // ================= "she saw you" =================

        private static void SightPostfix(NPC __instance, ref bool __result)
        {
            if (!__result) return;
            try
            {
                if (!On) return;
                var n = __instance;
                var h = n != null ? n.heroine : null;
                if (h == null || n.AI == null || !GirlInfo.IsRegular(h)) return;
                var act = n.AI.actionNo;
                if (act == 4 && n.AI.isArrival) { __result = Solo(n); return; }
                if ((act == 26 || act == 27) && (n.isArrival || n.AI.isArrival)) { __result = Pair(n); return; }
                var map = G.Act.Map;
                if (map != null && map.Info != null && map.Info.isWarning) { __result = Warning(n); return; }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Reactions sight: " + e.Message); }
        }

        private static Session SessionOf(NPC n)
        {
            Entry e;
            return n != null && _sessions.TryGetValue(n, out e) && e.Result == n.AI.result ? e.Session : null;
        }

        private static void Bind(NPC n, Session s) { _sessions[n] = new Entry { Result = n.AI.result, Session = s }; }

        private static bool Solo(NPC n)
        {
            var s = SessionOf(n);
            if (s != null) return !s.Calm;
            var h = n.heroine;
            s = new Session { Kind = "solo", A = n, Calm = CalmInH(h) };
            Bind(n, s);
            Flagra(h, null);
            if (s.Calm)
            {
                s.Asked = true;
                _asks.Enqueue(s);
                Note(GirlInfo.FirstName(h) + " caught alone: stays (" + (h.isGirlfriend ? "girlfriend" : "close and bold") + "), asks you to help");
                return false;
            }
            // The game runs off only if she is new to it or not lewd enough; then she dresses again and talks to you later.
            var runs = h.HExperience <= SaveData.Heroine.HExperienceKind.不慣れ || h.lewdness < 80;
            if (runs) { _mustDress.Add(h); Later(h, null); }
            Note(GirlInfo.FirstName(h) + " caught alone: " + (runs ? "runs to the changing room" : "carries on (game)"));
            return true;
        }

        /// <summary>The other girl of a two-girl scene (the asker has her as target; the asked one is found by receivers).</summary>
        private static NPC PartnerOf(NPC n)
        {
            var t = n.AI.target as NPC;
            if (t != null) return t;
            return n.AI.TargetReceiver(p => p.actionNo == 26 || p.actionNo == 27);
        }

        private static bool Pair(NPC n)
        {
            var s = SessionOf(n);
            if (s != null) return !s.Calm;
            var other = PartnerOf(n);
            if (other == null || other.heroine == null) return true;
            var os = SessionOf(other);
            if (os != null && (os.A == n || os.B == n)) { Bind(n, os); return !os.Calm; } // she already decided for both
            var a = n.heroine;
            var b = other.heroine;
            s = new Session { Kind = "pair", A = n, B = other };
            Flagra(a, b);
            Flagra(b, a);
            if (a.isGirlfriend != b.isGirlfriend)
            {
                s.Kind = "cheat";
                s.A = a.isGirlfriend ? n : other;
                s.B = a.isGirlfriend ? other : n;
                s.Calm = true;
            }
            else s.Calm = CalmInH(a) && CalmInH(b);
            Bind(n, s);
            Bind(other, s);
            if (s.Calm)
            {
                s.Asked = true;
                _asks.Enqueue(s);
                Note(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(b) + " caught together: " + (s.Kind == "cheat" ? "your girlfriend with another girl; they explain" : "they don't mind; they ask you to join"));
                return false;
            }
            _mustDress.Add(a);
            _mustDress.Add(b);
            Later(a, b);
            Note(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(b) + " caught together: both run to the changing room");
            return true;
        }

        private static bool Warning(NPC n)
        {
            var h = n.heroine;
            if (!CalmWhenSeen(h)) return true;
            var stamp = S.Day * 16 + (int)G.CurrentMask;
            if (stamp != _toastStamp) { _toastStamp = stamp; _calmToast.Clear(); }
            if (_calmToast.Add(h))
            {
                Toasts.Show(GirlInfo.FirstName(h) + (h.isGirlfriend ? " just smiles and waves at you." : " notices you... and doesn't seem to mind."));
                Note(GirlInfo.FirstName(h) + " seen in a private place: doesn't mind");
            }
            return false;
        }

        /// <summary>A flagra: counted, remembered, and the director hears about it.</summary>
        private static void Flagra(SaveData.Heroine h, SaveData.Heroine with)
        {
            S.Stat("caught");
            S.Girl(GirlInfo.Key(h)).Caught++;
            Memories.Add(h, "caught", with == null ? "you walked in on me when I was alone" : "you walked in on " + GirlInfo.FirstName(with) + " and me", 1, true, with != null ? GirlInfo.Key(with) : "");
            Director.Changed(h, "caught in H");
        }

        private static void Later(SaveData.Heroine h, SaveData.Heroine other)
        {
            _followUps.RemoveAll(f => f.H == h);
            _followUps.Add(new FollowUp { H = h, Other = other, At = Time.realtimeSinceStartup, Day = S.Day });
        }

        // ================= after running off: dress again =================

        private static void EscapePostfix(ActionControl __instance, SaveData.Heroine _heroine, ref ActionControl.ResultInfo __result)
        {
            try
            {
                if (_heroine != null && On) Lore.OnEscape(_heroine);
                if (_heroine == null || !_mustDress.Remove(_heroine) || !On) return;
                var d = __instance.SetAction(_heroine, 0, -1); // the game's "change clothes", in the changing room
                if (d == null) return;
                d.move = ActionGame.Chara.Mover.AgentSpeeder.Mode.Run;
                d.isPriority = true;
                __result = d;
                Note(GirlInfo.FirstName(_heroine) + " runs to the changing room to dress" + (d.point != null ? " (map " + d.point.MapNo + ")" : ""));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Reactions escape: " + e.Message); }
        }

        // ================= sleeping =================

        private static void SleepPostfix(ref Base __result)
        {
            try
            {
                if (!On) return;
                Rest();
                if (__result is Fix) return; // a teacher's or story heroine's own sleep event stays
                var picked = PickSleeper(__result as NPC);
                if (picked != __result) Note("sleeping: " + (picked != null ? GirlInfo.FirstName(picked.heroine) + " comes" : "nobody comes") + " (game had " + (__result != null && __result.heroine != null ? GirlInfo.FirstName(__result.heroine) : "nobody") + ")");
                __result = picked;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Reactions sleep: " + e.Message); }
        }

        /// <summary>The nap on the game's bed or bench is Campus Life's rest (the separate rest icon is gone).</summary>
        private static void Rest()
        {
            var id = G.PlayerMapNo == 36 ? "nap" : "rest";
            var a = Activities.Get(id);
            var why = a == null ? "no activity " + id : Activities.Blocker(S, a);
            if (why != null) { Note("rest on the game's bed: no gain (" + why + ")"); return; }
            var res = Campus.DoActivity(a);
            if (res != null) Toasts.Show(a.Name + ": " + res.Text.Replace("\n", " "));
            Note("rest on the game's bed: " + id + ", energy now " + S.Energy);
        }

        /// <summary>Who slips in while you sleep: whoever wants it most, or nobody.</summary>
        private static NPC PickSleeper(NPC gamePick)
        {
            var best = new List<KeyValuePair<NPC, float>>();
            foreach (var n in G.Act.npcList)
            {
                if (n == null || n.heroine == null || !GirlInfo.IsRegular(n.heroine)) continue;
                var sc = SleepScore(n.heroine);
                if (n == gamePick) sc += 10f;
                if (sc > 0f) best.Add(new KeyValuePair<NPC, float>(n, sc));
            }
            if (best.Count == 0) return null;
            var top = best.Max(x => x.Value);
            if (Random.value > Mathf.Clamp(top / 110f, 0.15f, 0.9f)) return null;
            var roll = Random.value * best.Sum(x => x.Value);
            foreach (var kv in best) { roll -= kv.Value; if (roll <= 0f) return kv.Key; }
            return best[0].Key;
        }

        private static float SleepScore(SaveData.Heroine h)
        {
            if (h.isAnger || Pairs.IsScorned(h)) return 0f;
            if (Duo.Busy && (Duo.LastA != null && Duo.LastA.heroine == h || Duo.LastB != null && Duo.LastB.heroine == h)) return 0f;
            if (WalkTalk.Active && WalkTalk.Her == h) return 0f;
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.OwesH > 0) return 120f;
            if (!h.isGirlfriend && (h.hCount == 0 || h.lewdness < 50)) return 0f;
            var sc = h.lewdness * 0.6f + (h.isGirlfriend ? 40f : 0f) + (h.relation >= 2 ? 10f : 0f);
            if (gs.Intent == Intents.Player) sc += 20f;
            if (gs.Mood == Mood.Flirty && gs.MoodDay == S.Day) sc += 20f;
            return sc;
        }

        // ================= scenes =================

        /// <summary>From the plugin's Update: plays the asks and the "about earlier" talks when the map is free.</summary>
        public static void Tick()
        {
            if (Time.realtimeSinceStartup < _nextTick) return;
            _nextTick = Time.realtimeSinceStartup + 0.3f;
            try
            {
                if (!On) { _asks.Clear(); return; }
                if (_sessions.Count > 40) foreach (var k in _sessions.Keys.Where(k => k == null).ToList()) _sessions.Remove(k);
                if (!Scenes.CanPlayOnMap || Duo.Busy) return;
                if (_asks.Count > 0)
                {
                    var s = _asks.Dequeue();
                    if (s.A == null || s.A.heroine == null) return;
                    if (s.Kind == "cheat") PlayCheat(s);
                    else PlayAsk(s);
                    return;
                }
                _followUps.RemoveAll(f => f.H == null || f.Day != S.Day);
                foreach (var f in _followUps.ToList())
                {
                    if (Time.realtimeSinceStartup - f.At < 20f) continue;
                    var n = Director.NpcOf(f.H);
                    if (n == null || n.AI == null || n.AI.actionNo == 0 || n.AI.actionNo == 20) continue;
                    if (n.mapNo != G.PlayerMapNo || (n.transform.position - G.Act.Player.position).sqrMagnitude > 7f * 7f)
                    {
                        // Dressed again: the director sends her to find you (the game's "talk to him", 8), once.
                        if (!f.Sent && Time.realtimeSinceStartup - f.At > 25f && !n.isOnanism && !n.isLesbian && n.AI.isRunning && !n.AI.isPaused)
                        {
                            f.Sent = true;
                            var r = G.Act.actCtrl.SetAction(f.H, 8, -1);
                            if (r != null) { HarmonyLib.Traverse.Create(n.AI).Field("actResult").Property("Value").SetValue(r); Note(GirlInfo.FirstName(f.H) + " comes to find you about earlier (from map " + n.mapNo + ")"); }
                        }
                        continue;
                    }
                    _followUps.Remove(f);
                    PlayAboutEarlier(n, f.Other);
                    return;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Reactions tick: " + e); }
        }

        private static string T(SaveData.Heroine h, string gentle, string energetic, string cool, string teasing) { return Lines.T(GirlInfo.ToneOf(h), gentle, energetic, cool, teasing); }

        /// <summary>She (or they) don't stop: "want to help?" / "want to join?". No penalty for "not now".</summary>
        /// <summary>
        /// They carry on while you talk: the game's H action has a timer (actionTime) that ran out during the scene, so
        /// "join" found them already done. The director gives them time.
        /// </summary>
        private static void KeepAtIt(Session s)
        {
            foreach (var n in new[] { s.A, s.B })
            {
                try { if (n != null && n.AI != null && n.AI.result != null) n.AI.result.actionTime += 240f; }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Reactions keep: " + e.Message); }
            }
        }

        private static void PlayAsk(Session s)
        {
            KeepAtIt(s);
            var n = s.A;
            var h = n.heroine;
            var b2 = s.B != null ? s.B.heroine : null;
            var b = new SceneBuilder(null, false);
            if (b2 == null)
            {
                b.Narr(GirlInfo.FirstName(h) + " freezes for a second when she sees you... and doesn't stop.");
                b.Face(h, Feel.Shy).Say(h, h.isGirlfriend
                    ? T(h, "Ah... you saw... W-would you... help me? || Ah... you saw... would you help me...? || Oh... um... could you... help? || You're here... w-would you join me?", "Oh! Hey... perfect timing. Wanna help? || Oh! Hi! Great timing! Wanna help? || Hey! Perfect! Join me! || Ooh, you're here! Help me out?", "...You're here. ...Help me finish. || ...You're here. Help me. || ...Finish what you saw. || ...Make yourself useful.", "Took you long enough. Come here and help. || Finally. Come help. || You're late. Get over here. || About time. Help me out.")
                    : T(h, "Ah... it's you... I-I don't mind... if you want to help... || It's you... if you want to help... I don't mind... || Ah... I-if you'd like to help... || Oh... it's okay... you can help...", "Oops! ...Well, since you're here... wanna help? || Whoops! Well, wanna help? || Oops! Since you're here...! || Caught! ...Wanna join in?", "...You saw. ...Then don't just stand there. || ...You saw. Then help. || ...Don't stand there. || ...Since you've seen, assist.", "Enjoying the view? You could do more than watch. || Just watching? You could participate. || Like the show? Join it. || Staring is rude. Helping isn't."));
                b.Choice("Help her.", "ask_yes", "Not now. (she carries on)", "ask_no");
                b.Branch("ask_yes"); b.Face(h, Feel.Love).Say(h, T(h, "Th-thank you... || Th-thank you... || Ah... thank you... || Mm... thanks...", "Yay~ C'mere! || Yay! Come here! || Woo! C'mon! || Yes! Get over here!", "...Good. || ...Good. || ...Fine. || ...Come.", "Knew you would. || Of course you would. || Knew it. || Predictable. Good.")); b.End();
                b.Branch("ask_no"); b.Face(h, Feel.Smile).Say(h, T(h, "O-okay... the offer stands, then... || O-okay... it's still open... || Alright... the offer stays... || I-it's okay... anytime...", "Your loss~ ...Offer stands! || Your loss! Offer's still on! || Boo! But the offer stays! || Fine! Anytime, though!", "...Suit yourself. Offer stands. || ...Your choice. It stands. || ...Fine. The offer remains. || ...As you wish.", "Fine. You know where to find me. || You know where I am. || Offer's open. Don't wait too long. || Fine. I'll be around.")); b.End();
            }
            else
            {
                b.Narr(GirlInfo.FirstName(h) + " and " + GirlInfo.FirstName(b2) + " notice you... and neither of them pulls away.");
                b.Face(h, Feel.Shy).Say(h, T(h, "Ah... you found us... Do you... want to join? || You found us... would you like to join? || Ah... would you... join us? || Oh... um... there's room for you...", "Oh! Hey! There's room for one more~ || Hey! Room for one more! || Oh! Join us! || Perfect timing! Hop in!", "...You may join. If you want. || ...You may join. || ...Join, if you like. || ...There's room.", "Don't just stare. Get over here. || Stop staring. Come here. || Get over here already. || Watching? Join."));
                b.Face(b2, Feel.Smile).Say(b2, T(b2, "W-we don't mind... || W-we don't mind... || I-it's okay with us... || We'd like that...", "C'mon, it's more fun with three! || More fun with three! || Come on! Three's better! || C'mon, join!", "...Agreed. || ...Agreed. || ...We concur. || ...Yes.", "We were waiting for you, actually. || We were waiting, actually. || We hoped you'd show up. || Took you long enough."));
                b.Choice("Join them.", "ask_yes", "Not now. (they carry on)", "ask_no");
                b.Branch("ask_yes"); b.Face(h, Feel.Love).Say(h, T(h, "Hehe... come here... || Hehe... come here... || Mm... over here... || Hehe... welcome...", "Yay! || Yay! || Woo! || Yes!", "...Good. || ...Good. || ...Fine. || ...Come.", "Thought so. || Thought so. || Knew it. || Obviously.")); b.End();
                b.Branch("ask_no"); b.Say(b2, T(b2, "O-okay... another time... || Okay... maybe another time... || Alright... next time... || It's okay... another time...", "Aww. Next time! || Aww! Next time! || Boo! Later then! || Okay! Next time for sure!", "...Another time. || ...Later, then. || ...Another time. || ...Fine.", "Your loss. Offer stands. || Your loss. Still open. || Next time, then. || Pity. Offer stays.")); b.End();
            }
            if (!Scenes.PlayOnMap(b, c =>
            {
                Note((b2 == null ? GirlInfo.FirstName(h) : GirlInfo.FirstName(h) + " and " + GirlInfo.FirstName(b2)) + ": " + (c == "ask_yes" ? "you joined" : "not now (no penalty, they carry on)"));
                if (c == "ask_yes") Join(n, s.B);
            })) { s.Asked = false; _asks.Enqueue(s); }
        }

        /// <summary>Your girlfriend with a girl who isn't yours: they explain; you join them, or they owe you one.</summary>
        private static void PlayCheat(Session s)
        {
            KeepAtIt(s);
            var gf = s.A.heroine;
            var o = s.B.heroine;
            var gn = GirlInfo.FirstName(gf);
            var on = GirlInfo.FirstName(o);
            var b = new SceneBuilder(null, false);
            b.Narr("You walk in on " + gn + " and " + on + ", tangled up together. They don't stop right away.");
            b.Face(gf, Feel.Shy).Say(gf, T(gf, "W-wait! It's not... okay, it's exactly what it looks like... I'm sorry... || W-wait! ...It's exactly what it looks like. I'm sorry... || I-I can explain... no, I can't. Sorry... || Ah! I'm so sorry...!", "Ah! Um! We were just... practicing? ...Sorry! || Ah! We were... practicing! ...Sorry! || Uh! Surprise?! ...Sorry! || Eep! It's not... okay, it is! Sorry!", "...I can explain. Partially. || ...I can explain. Somewhat. || ...This requires context. || ...Not ideal timing.", "...Busted. Well. Don't make that face. || ...Caught. Don't make that face. || ...Well. Busted. || ...Oops. Hi."));
            b.Face(o, Feel.Worried).Say(o, T(o, "Please don't be angry at her... I started it. || Don't be mad at her... it was me. || Please, it's my fault... not hers. || I started it... don't blame her.", "It's my fault! She's just too cute! || My fault! She's too cute! || Blame me! Totally me! || It was me! She's innocent! Kinda!", "This was my doing. Don't blame her. || My doing. Not hers. || I'm responsible. || Blame me.", "Don't look at her like that. I'm the bad influence here. || I'm the bad influence. Obviously. || Don't glare at her. It was me. || Me. I corrupted her. Proudly."));
            b.Say(gf, T(gf, "I love you, really... it just... happened. Are you mad? || I love you, truly... it just happened. Are you angry? || It doesn't change my feelings for you... are you mad? || I still love you... please don't be angry.", "You're not mad, right? ...Right? || You're not mad, right?! Please?! || Not mad? Right? Right?! || Please don't be mad!", "...It doesn't change how I feel about you. || ...My feelings for you are unchanged. || ...It changes nothing between us. || ...You're still the one.", "You're still my number one. Obviously. || You're still number one. Obviously. || Relax. You're still my favorite. || Still yours. Mostly."));
            b.Choice("Join them.", "cx_join", "Smile: they owe you one.", "cx_owe");
            b.Branch("cx_join");
            b.Face(gf, Feel.Love).Say(gf, T(gf, "Eh...? You'll... join us? ...Okay... || You'll join...? ...Okay... || Eh...? Really...? Okay... || You want to join us...? Okay...", "Wait, really?! Hehe... get over here! || Really?! Get in here! || No way! Yes! Come here! || Seriously?! Awesome!", "...That's one way to settle it. || ...That settles it. || ...An efficient resolution. || ...That works.", "Ha. I knew I liked you. || Ha. I like you more already. || Knew I liked you. || Now that's the spirit."));
            b.Face(o, Feel.Shy).Say(o, T(o, "I-is that okay...? || I-is this okay...? || Are you sure...? || Is it alright...?", "Ooh, yes please! || Ooh, yes! || Yes please! || Yay!", "...Acceptable. || ...Acceptable. || ...Fine. || ...Good.", "Now we're talking. || Now we're talking. || That's more like it. || Finally, fun."));
            b.End();
            b.Branch("cx_owe");
            b.Face(gf, Feel.Smile).Say(gf, T(gf, "...We owe you. Both of us. Thank you for not being angry... || ...We owe you. Thank you for not being mad... || ...Both of us owe you. Thank you. || ...Thank you for forgiving us...", "Deal! We owe you big time! || We owe you big! || Deal! Huge debt! || We'll pay you back!", "...Understood. I'm in your debt. || ...I'm in your debt. || ...Understood. I owe you. || ...A debt. Acknowledged.", "A debt, huh... I'll pay it back. With interest. || A debt? I'll pay it. With interest. || I'll repay you. Generously. || I owe you? I'll make it fun."));
            b.Face(o, Feel.Shy).Say(o, T(o, "Um... if you ever want to... collect... just ask me. || Um... whenever you want to collect... just ask. || If you ever want... just say so. || Ask me anytime... okay?", "Anytime you want to collect, just say so~ || Collect whenever! || Just say the word! || Anytime you want!", "...Name the time. I'll make it up to you. || ...Name the time. || ...Whenever you want. || ...Tell me when.", "Collect whenever you like. I won't say no. || Collect anytime. I won't refuse. || Whenever you like, darling. || Just ask. I'll say yes."));
            b.End();
            if (!Scenes.PlayOnMap(b, c =>
            {
                // Either way: both of them like you more.
                G.AddFavor(gf, 5, 2); G.AddFavor(o, 5, 3);
                Social.AddTrust(gf, 4); Social.AddTrust(o, 4);
                Director.Changed(gf, "caught cheating"); Director.Changed(o, "caught with your girlfriend");
                if (c == "cx_join") { Note(gn + " and " + on + ": you joined them"); Join(s.A, s.B); return; }
                var gs = S.Girl(GirlInfo.Key(gf));
                gs.Pass = Math.Min(2, gs.Pass + 1);
                S.Girl(GirlInfo.Key(o)).OwesH = 1;
                Memories.Add(gf, "debt", "I owe you one for being so kind about " + on, 1, true, GirlInfo.Key(o));
                Toasts.Show(gn + " owes you one: the next slip-up with her is forgiven. " + on + " will say yes to H once.");
                Note(gn + " and " + on + ": in your debt (pass for " + gn + ", " + on + " owes H)");
            })) { s.Asked = false; _asks.Enqueue(s); }
        }

        /// <summary>After running off: she stops you about it (the flagra's choices).</summary>
        private static void PlayAboutEarlier(NPC n, SaveData.Heroine other)
        {
            var h = n.heroine;
            Forms.Hold(n);
            var b = new SceneBuilder(null, false);
            b.Narr(GirlInfo.FirstName(h) + " comes up to you, red to the ears.");
            b.Face(h, Feel.Shy).Say(h, other == null
                ? T(h, "A-about before... you didn't see anything. Right? || A-about earlier... you didn't see anything, right? || Um... earlier... please forget it. || About before... you saw nothing... okay?", "S-so! About earlier! Can we pretend that never happened?! || So! Earlier! Never happened! Right?! || Can we forget earlier?! Please?! || About before! Total amnesia, okay?!", "...About earlier. Forget it. || ...Earlier. Forget it. || ...About before. Erase it. || ...That didn't happen.", "...So. You saw. What now? || ...So. You saw. Now what? || ...You saw. What's your move? || ...Well. You got a show.")
                : T(h, "A-about " + GirlInfo.FirstName(other) + " and me... please don't tell anyone... || About " + GirlInfo.FirstName(other) + " and me... please keep it secret..." + " || " + GirlInfo.FirstName(other) + " and me... don't tell anyone, please... || Please... about " + GirlInfo.FirstName(other) + "... keep quiet?", "About me and " + GirlInfo.FirstName(other) + "! You saw nothing! Right?! || Me and " + GirlInfo.FirstName(other) + "! You saw nothing!" + " || " + GirlInfo.FirstName(other) + " and me?! Not a word! || Zip it about " + GirlInfo.FirstName(other) + " and me!", "...About " + GirlInfo.FirstName(other) + " and me. Keep it to yourself. || ..." + GirlInfo.FirstName(other) + " and me. Silence. || ...Keep " + GirlInfo.FirstName(other) + " out of conversations. || ...Discretion about " + GirlInfo.FirstName(other) + ".", "So. You saw " + GirlInfo.FirstName(other) + " and me. Going to talk? || You saw " + GirlInfo.FirstName(other) + " and me. Gonna blab?" + " || " + GirlInfo.FirstName(other) + " and me. Your lips sealed? || So, " + GirlInfo.FirstName(other) + " and me. Talk and you'll regret it."));
            b.Choice("Promise to keep it secret.", "fl_keep", "Tease her a little.", "fl_tease", "Say it was kind of cute.", "fl_cute", "Pretend nothing happened.", "fl_walk");
            b.Branch("fl_keep"); b.Face(h, Feel.Smile).Say(h, T(h, "Thank you... really... || Thank you... truly... || Thank you so much... || Ah... thank you...", "You're the best! I owe you! || You're the best! || I owe you one! || Thanks a ton!", "...Thank you. || ...Thanks. || ...Appreciated. || ...Thank you.", "...Good. You're alright. || ...Good. You're okay. || ...Smart choice. || ...Alright, you pass.")); b.End();
            b.Branch("fl_tease"); b.Face(h, Feel.Pout).Say(h, T(h, "S-stop it...! Mean... || S-stop...! Mean... || D-don't tease... || That's mean...!", "Hey! Don't make it weird! || Don't make it weird! || Hey! Stop! || Ugh, quit it!", "...Childish. || ...Childish. || ...Immature. || ...Stop.", "...Keep talking and see what happens. || ...Keep going. See what happens. || ...Careful. || ...Push it, I dare you.")); b.End();
            b.Branch("fl_cute"); b.Face(h, h.lewdness >= 40 ? Feel.Love : Feel.Surprised).Say(h, h.lewdness >= 40 ? T(h, "C-cute...? ...You're strange... but thanks... || Cute...? You're strange... thanks... || C-cute? ...Thank you... || Um... cute...? Thanks...", "Cute?! ...Hehe. Okay. || Cute?! Hehe, okay! || Aww! Thanks! || Cute? Heh!", "...Cute. Hm. || ...Cute. Hm. || ...If you say so. || ...Odd compliment.", "Cute, huh? Want an encore? || Cute? Want an encore? || Cute, huh? Next time, watch closer. || Cute? Oh, you liked it.") : T(h, "D-don't say that! || D-don't say that! || Please don't! || N-no!", "That's NOT cute! || NOT cute! || That's not cute! || Stop it!", "...Not funny. || ...Not funny. || ...No. || ...Stop.", "...Nice try. || ...Nice try. || ...Ha. No. || ...Smooth. Not.")); b.End();
            b.Branch("fl_walk"); b.Face(h, Feel.Smile).Say(h, T(h, "...Thank you. || ...Thank you. || ...Thanks. || ...I appreciate it.", "Phew! Okay! Nothing happened! || Phew! Nothing happened! || Great! Never happened! || Okay! Good!", "...Good. || ...Good. || ...Fine. || ...Right.", "...Smart. || ...Smart. || ...Good boy. || ...Wise.")); b.End();
            if (!Scenes.PlayOnMap(b, c =>
            {
                Forms.Let(n);
                // She came over to say it: that was the talk (no game "talk to him" prompt after it).
                try { if (n.AI != null && n.AI.actionNo == 8) n.AI.NextActionNoTarget(); } catch { }
                switch (c)
                {
                    case "fl_keep": Social.AddTrust(h, 6); G.AddFavor(h, 4, 0); Memories.Add(h, "secret", "you kept what you saw to yourself", 1); break;
                    case "fl_tease": Social.AddTrust(h, -2); G.AddFavor(h, 0, 5); break;
                    case "fl_cute": if (h.lewdness >= 40) G.AddFavor(h, 2, 4); else G.AddFavor(h, -2, 2); break;
                    case "fl_walk": Social.AddTrust(h, 2); break;
                }
                Social.RecordTalk(h);
                Note(GirlInfo.FirstName(h) + " about earlier: " + c);
            })) Forms.Let(n);
        }

        /// <summary>Into the game's own H with her (or them): the same thing a click on them does.</summary>
        private static void Join(NPC n, NPC other = null)
        {
            CampusLifePlugin.Instance.StartCoroutine(JoinCo(n, other).PreventFromCrashing());
        }

        /// <summary>
        /// The game's "is in the scene between two girls" (isLesbian) is only true for the partner (27), never for the one
        /// who started it (26): the click that opens the H is on the partner (so the starter always looked as if she had already stopped).
        /// </summary>
        private static IEnumerator JoinCo(NPC n, NPC other)
        {
            if (other != null && !n.isLesbian && (other.isLesbian || n.AI != null && n.AI.actionNo == 26)) n = other;
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 15f && !(G.IsFreeRoaming && !Scenes.Running && G.Act != null && !G.Act.isEventNow)) yield return null;
            yield return null;
            try
            {
                if (n == null || G.Act == null || !(n.isOnanism || n.isLesbian)) { Note("join: she had already stopped"); yield break; }
                G.Act.SceneEvent(n);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Reactions join: " + e); }
        }

        // ================= debts =================

        /// <summary>Her one-time pass absorbs a hurtful thing she hears about you (cheating, a broken promise...).</summary>
        public static bool UsePass(SaveData.Heroine h, Incident inc)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Pass <= 0) return false;
            gs.Pass--;
            Toasts.Show(GirlInfo.FirstName(h) + " lets it go this time: now you're even.");
            Note(GirlInfo.FirstName(h) + " used her pass on '" + inc.Kind + "'");
            return true;
        }

        /// <summary>She owes you H: a sure yes the next time you ask (used up then).</summary>
        public static bool OwesH(SaveData.Heroine h, bool use)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.OwesH <= 0) return false;
            if (use) { gs.OwesH = 0; Note(GirlInfo.FirstName(h) + " paid her debt (H)"); }
            return true;
        }
    }
}
