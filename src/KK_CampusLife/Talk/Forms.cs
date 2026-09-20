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
                n.Pause(true);
                if (n.AI != null && n.AI.isRunning && !n.AI.isPaused) n.AI.Pause();
                if (n.move != null) n.move.isStop = true;
                if (n.move != null) n.move.animParamSpeed = 0f;
                var p = G.Act.Player.transform.position;
                var look = p - n.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) n.transform.rotation = Quaternion.LookRotation(look);
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
                if (n.move != null) n.move.isStop = false;
                if (n.chaCtrl != null) { n.chaCtrl.ChangeLookEyesPtn(0); n.chaCtrl.ChangeLookNeckPtn(3); }
                n.Pause(false);
                if (n.AI != null && (!n.AI.isRunning || n.AI.isPaused)) n.AI.Start();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Forms let: " + e.Message); }
        }

        /// <summary>Every frame: the game's own "player came near / walked away" check restarts her AI, so held girls are re-stopped.</summary>
        internal static void KeepHeld()
        {
            if (_held.Count == 0) return;
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                var n = _held[i];
                try
                {
                    if (n == null || n.move == null) { _held.RemoveAt(i); continue; }
                    if (!n.move.isStop) n.move.isStop = true;
                    if (n.AI != null && n.AI.isRunning && !n.AI.isPaused) n.AI.Pause();
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
                    Line = T(h, "Ah... p-please pretend you didn't hear that...", "Ugh, I'm STARVING! Is lunch ever gonna come?!", "...You heard nothing.", "Not a word. I mean it.") };
                if (snack != null)
                {
                    var r = b.Add("Split your " + Episodes.ItemName(snack) + " with her",
                        T(h, "F-for me? Thank you... it's really good.", "Whoa, you're a lifesaver! Mmm!", "...Thanks. I owe you one.", "Feeding me now? You're spoiling me~"), 5, 2, "you shared your " + Episodes.ItemName(snack) + " with me");
                    r.UseItem = snack;
                }
                b.Add("Tell her the cafeteria line is short right now",
                    T(h, "Really? Then I'll hurry. Thank you!", "Seriously?! I'm running! Bye!", "Useful. Thanks.", "Oh? Good intel."), 2, 1);
                b.Add("Pretend you didn't notice", T(h, "...Thank you for that.", "Hehe, smooth!", "...Appreciated.", "Gentleman, huh."), 1);
                list.Add(b);
            }
            if (mood == Mood.Sleepy)
            {
                var b = new Bit { Id = "sleepy", Narr = name + " shuffles past, eyes half closed, and almost walks into you.",
                    Line = T(h, "Ah, sorry... I barely slept last night...", "Whoa! Sorry! My brain's still in bed!", "...Mm. Morning. Still.", "You again. Catch me if I fall asleep, okay?") };
                if (S.Count("canned_coffee") > 0)
                {
                    var r = b.Add("Hand her your canned coffee", T(h, "Coffee... you're an angel.", "CAFFEINE! I love you! ...the coffee! I love the coffee!", "...You read my mind.", "Bribing me awake? It's working."), 5, 2, "you gave me your coffee when I was half asleep");
                    r.UseItem = "canned_coffee";
                }
                b.Add("Ask if she stayed up gaming again", T(h, "N-not gaming! Reading... until three...", "Maaaybe. One more level turned into twelve!", "...Studying. Mostly.", "Wouldn't you like to know what kept me up~"), 2, 1);
                b.Add("Steady her by the shoulder", T(h, "Th-thanks... I'm awake now. Very awake.", "Whoa, thanks! Okay, I'm up!", "...Thanks.", "Hands on my shoulders already?"), 2, 2);
                list.Add(b);
            }
            if (mood == Mood.Stressed)
            {
                var b = new Bit { Id = "stressed", Narr = name + " walks by muttering to herself, clutching a notebook.",
                    Line = T(h, "Can't fail, can't fail, can't fail...", "Okay okay okay, formulas, formulas, FORMULAS...", "...If I miss this, I'm finished.", "Don't talk to me. I'm memorizing. ...Okay, you can talk.") };
                b.Add("Offer to study together later", T(h, "You'd do that? Then... after class?", "YES. Please. Save me!", "...That would help. Thanks.", "A study date? Fine. Don't distract me."), 2, 2).Then = Want(RequestKind.Study, "Study with ", " asked you to study with her later. Invite her to study.", PeriodMask.Any);
                b.Add("Give her a thumbs up", T(h, "...Thanks. I'll do my best.", "Yeah! Thumbs up! I got this!", "...Mm.", "Cute. ...Thanks."), 1, 1);
                b.Add("Tell her she always pulls through", T(h, "You think so...? That helps a little.", "You're right! I always do! Kinda!", "...You've been paying attention.", "Flattery. ...Keep going."), 3, 2);
                list.Add(b);
            }
            if (mood == Mood.Bored)
            {
                var b = new Bit { Id = "bored", Narr = name + " drags her feet down the hall, sighing dramatically.",
                    Line = T(h, "Nothing ever happens around here...", "BORED! I'm so bored I could explode!", "...This day is endless.", "Entertain me. That's an order.") };
                b.Add("Suggest racing her to the stairs", T(h, "Eh?! W-wait, that's not fair!", "You're ON! Go go go!", "...Childish. ...Fine. Go.", "Loser buys drinks. Ready?"), 3, 1, "we raced down the hall like kids");
                b.Add("Tell her the vending machine ate your coins", T(h, "Oh no... want me to tap it? It works sometimes.", "Pfft! The beast strikes again!", "...Kick it on the left side. Trust me.", "Ha! Rookie mistake."), 2);
                b.Add("Shrug: same here", T(h, "Then we're bored together...", "Bored buddies! High five!", "...Solidarity.", "Wow, so helpful."), 1);
                list.Add(b);
            }
            if (mood == Mood.Lonely)
            {
                var b = new Bit { Id = "lonely", Narr = name + " stands by the window alone, glancing up when you come near.",
                    Line = T(h, "Oh... hi. Everyone's busy today, I guess...", "Hey! Finally, a face I know!", "...Oh. It's you.", "Look who decided to show up.") };
                b.Add("Say you were hoping to run into her", T(h, "R-really...? That makes me happy.", "Aww! Really?! Okay, best day!", "...Hm. Good answer.", "Smooth. I'll pretend I believe you."), 4, 3, "you said you were hoping to run into me");
                b.Add("Ask her to walk with you a bit", T(h, "Okay... I'd like that.", "Yes! Let's go!", "...Sure.", "Fine. Lead the way."), 2, 1).Then = AskWalk;
                b.Add("Wave and keep going", T(h, "...Oh. Bye.", "Hey, wait! ...Oh.", "...Right.", "Tch. Busy man."), -1);
                list.Add(b);
            }
            if (mood == Mood.Flirty)
            {
                var b = new Bit { Id = "flirty", Narr = name + " slows down as you pass, with a little smile.",
                    Line = T(h, "Ah... you look nice today...", "Hey you~ Notice anything different?", "...You're staring. Not that I mind.", "Oh, it's you~ Miss me?") };
                b.Add("Tell her she looks great today", T(h, "Eh?! Th-thank you...!", "I KNEW you'd notice!", "...Obviously. But thanks.", "Took you long enough~"), 4, 1).Lewd = 3;
                b.Add("Wink back", T(h, "!!! ...You're so bold...", "Ooh, smooth operator!", "...Hm.", "Oh? Two can play that game."), 2).Lewd = 5;
                b.Add("Ask what she's so happy about", T(h, "N-nothing... just a good day.", "Secret! Hehe!", "...Nothing you need to know. Yet.", "Maybe it's you. Maybe."), 2, 1);
                list.Add(b);
            }
            if (mood == Mood.Upset)
            {
                var b = new Bit { Id = "upset", Narr = name + " turns her face away as you come near. Her eyes are red.",
                    Line = T(h, "...Please don't look at me right now.", "I'm FINE. Totally fine. Go away. ...Sorry.", "...Not now.", "Don't. Just don't.") };
                b.Add("Quietly hand her a tissue", T(h, "...Thank you.", "...Thanks. Sorry.", "...Mm.", "...Thanks."), 3, 3, "you handed me a tissue without a word");
                b.Add("Ask if she wants to talk later", T(h, "...Maybe. Later.", "...Yeah. Later. Okay?", "...Perhaps.", "...We'll see."), 1, 2).Then = x => S.Girl(GirlInfo.Key(x)).Pending = "Quiet:upset";
                b.Add("Give her space", T(h, "...", "...", "...", "..."), 0, 1);
                list.Add(b);
            }
            if (mood == Mood.Cheerful)
            {
                var b = new Bit { Id = "cheerful", Narr = name + " is humming as she walks, practically bouncing.",
                    Line = T(h, "Oh! Good morning... I mean, hello! Hehe.", "Hiii! Isn't today great?!", "...Hello. It's a decent day.", "Well, aren't you lucky to see me in a good mood.") };
                b.Add("Ask what's got her in such a good mood", T(h, "My cat learned a new trick! ...Is that silly?", "I got the last melon bread! Victory!", "...Someone finally returned my book.", "Wouldn't you like to know~"), 2, 1);
                b.Add("Hum along", T(h, "Ehehe, you know this song?", "Duet! Duet!", "...Off-key. But fine.", "Oh no. Never sing again. ...Kidding."), 3, 0, "you hummed along with me in the hallway");
                b.Add("Return the good mood with a grin", T(h, "Hehe.", "Yeah!", "...Mm.", "Heh."), 1);
                list.Add(b);
            }

            // The place.
            switch (place)
            {
                case "library":
                    list.Add(PlaceBit(h, "lib", name + " hugs a stack of books against her chest.", T(h, "Shh... I found the book I was looking for!", "Library voice! ...I found it!", "...Quiet. I'm in the middle of a chapter.", "Shh. Or I'll shush you."),
                        "Ask what she's reading", T(h, "A mystery... I can't tell you the ending!", "It's got DRAGONS!", "...Poetry. Don't laugh.", "Something too mature for you."), 2,
                        "Offer to carry the stack", T(h, "Ah, thank you... they're heavy.", "My hero!", "...Fine. Careful.", "Muscles. Noted."), 3, "you carried my books in the library"));
                    break;
                case "gym":
                case "field":
                case "pool":
                    list.Add(PlaceBit(h, "sport", name + " wipes sweat from her forehead, catching her breath.", T(h, "Hah... hah... practice is tough today...", "Phew! One more lap and I'm a legend!", "...New personal record. Almost.", "Like what you see? I'm in top form."),
                        "Cheer her on", T(h, "Thank you... that helps!", "YEAH! Watch me!", "...Hm. Thanks.", "Louder~"), 3,
                        "Offer her your water", T(h, "Thank you... it's cold!", "Gulp gulp! Ahh!", "...Appreciated.", "Indirect kiss, you know."), 3, "you gave me water after practice"));
                    break;
                case "rooftop":
                    list.Add(PlaceBit(h, "roof", name + " leans on the railing, her hair moving in the wind.", T(h, "The wind feels nice up here...", "You can see the whole town! Look!", "...It's quiet. That's why I come here.", "Hiding up here too, huh?"),
                        "Stand beside her and enjoy the view", T(h, "...This is nice.", "See? Best spot ever!", "...You can stay.", "Fine, you can share my spot."), 3,
                        "Point out the clouds", T(h, "That one looks like a cat!", "That one's a dragon!", "...It's just a cloud.", "That one looks like you. Sort of dumb."), 2, "we watched the clouds on the rooftop"));
                    break;
                case "cafeteria":
                    list.Add(PlaceBit(h, "cafe", name + " stares at the menu board like it's an exam.", T(h, "Curry or udon... I can't decide...", "Everything! I want EVERYTHING!", "...The set lunch. Probably.", "Choose for me. If it's wrong, you pay."),
                        "Recommend the curry", T(h, "Curry it is! Thank you!", "Curry! Good call!", "...Acceptable.", "Hm. If it's bad, I'm blaming you."), 2,
                        "Say you'll buy her a drink", T(h, "Eh? You don't have to...!", "Free drink! You're the best!", "...You don't have to. ...Thanks.", "Buying me things? Interesting."), 3, "you bought me a drink in the cafeteria"));
                    break;
                case "courtyard":
                case "park":
                    list.Add(PlaceBit(h, "yard", name + " is crouching by the flower beds.", T(h, "Look... the flowers are blooming.", "A cat was here! It ran off!", "...Someone stepped on these. Barbarians.", "Don't tell anyone I like flowers."),
                        "Crouch down beside her", T(h, "They're pretty, right?", "Shh! Maybe the cat comes back!", "...You may look.", "Secret garden club. Two members."), 3,
                        "Ask what kind they are", T(h, "Cosmos, I think...", "No idea! Pretty ones!", "...Marigolds.", "The pretty kind, obviously."), 2, "we looked at the flowers in the courtyard"));
                    break;
                case "clubroom":
                    if (club != "")
                        list.Add(PlaceBit(h, "club", name + " is carrying a box of club stuff.", T(h, "The " + club + " is getting busy...", "Big day for the " + club + "!", "...The " + club + " never runs out of chores.", "The " + club + " needs a strong pair of arms. Hint."),
                            "Help her carry the box", T(h, "Thank you so much!", "Teamwork!", "...Thanks.", "Good boy~"), 3,
                            "Ask how the club is doing", T(h, "We're doing well, I think!", "We're unstoppable!", "...Fine.", "Thriving. Thanks to me."), 2, "you helped me carry the club box"));
                    break;
                case "classroom":
                    list.Add(PlaceBit(h, "class", name + " is stretching beside her desk.", T(h, "Today's lesson was so long...", "My brain is FULL!", "...That lecture could have been an email.", "Did you sleep through it too?"),
                        "Ask if she got today's homework", T(h, "Mm. Want to compare later?", "Homework?! There was HOMEWORK?!", "...Page 34. Don't forget.", "Copying is cheating~ ...Maybe."), 2,
                        "Joke about the teacher's tie", T(h, "Hehe... it had ducks on it.", "THE DUCKS! I saw them!", "...It was a crime.", "Fashion victim."), 2, "we laughed about the teacher's duck tie"));
                    break;
            }

            // Something about the two of you.
            if (last != null && S.Day - last.Day >= 1)
                list.Add(PlaceBit(h, "mem" + last.Id, name + " stops when she sees you, as if remembering something.", T(h, "Ah... about " + Memories.When(last) + "...", "Hey! " + Memories.When(last) + " was fun!", "..." + Memories.When(last) + ". I remember.", "Hey, about " + Memories.When(last) + "~"),
                    "Smile: you remember too", T(h, "Hehe... me too.", "Right?!", "...Good.", "Of course you do."), 3,
                    "Say you'd like to do it again", T(h, "...I'd like that.", "Anytime!", "...Maybe.", "Maybe. If you're lucky."), 2, null));
            if (friend != null)
                list.Add(PlaceBit(h, "friend" + GirlInfo.Key(friend), name + " is looking around, phone in hand.", T(h, "Have you seen " + GirlInfo.FirstName(friend) + "? I can't find her...", "Where's " + GirlInfo.FirstName(friend) + "?! We had plans!", "...Have you seen " + GirlInfo.FirstName(friend) + "?", "Seen " + GirlInfo.FirstName(friend) + "? She owes me a juice."),
                    "Say you'll tell her if you see her", T(h, "Thank you!", "You're the best!", "...Thanks.", "Good. Report back."), 2,
                    "Tease: so you two are inseparable", T(h, "Hehe... we kind of are.", "Best friends forever!", "...Don't make it weird.", "Jealous?"), 2, null));

            // Always something.
            list.Add(PlaceBit(h, "hello", name + " gives you a small wave as she comes by.", T(h, "Oh, hello...", "Heya!", "...Hey.", "Yo."),
                "Wave back with a smile", T(h, "Hehe.", "Hehe! Bye!", "...Mm.", "Cute."), 1,
                "Ask where she's off to", T(h, "Just... around. Hehe.", "Adventure! Probably the vending machine!", "...Somewhere quiet.", "Somewhere you're not. Kidding."), 2, null));
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
                ask = T(h, "Um... could you tell " + fn + " that " + what + "? I can't find her...", "Hey! Tell " + fn + " that " + what + ", okay? I gotta run!",
                    "Tell " + fn + " that " + what + ". If you see her.", "Be my messenger. Tell " + fn + " that " + what + ".");
                var r = Board.Make(RequestKind.Talk, h, "Message for " + fn, name + " asked you to tell " + fn + " that " + what + ". Talk to " + fn + ", then " + name + " will thank you.", 7);
                r.Source = "favor"; r.Other = GirlInfo.Key(friend); r.Text = name + " asked you to tell " + fn + " that " + what + "."; r.Item = what;
                r.DeadlineDay = S.Day + 1; r.LearnedFrom = "her"; r.Button = "Pass on " + name + "'s message";
                return AskFavor(n, ask, r);
            }
            if (roll < 0.75f)
            {
                kind = "fetch";
                var like = GirlInfo.LikesOf(h).FirstOrDefault(l => l == "sweets" || l == "coffee" || l == "food" || l == "spicy") ?? "sweets";
                ask = T(h, "If you go by the store... could you get me something " + GirlInfo.LikeLabel(like) + "? I'll pay you back... someday.", "Snack run! Pleeease? Something " + GirlInfo.LikeLabel(like) + "!",
                    "If you pass the store, something " + GirlInfo.LikeLabel(like) + ". I'd be grateful.", "Fetch me something " + GirlInfo.LikeLabel(like) + ". Consider it an honor.");
                var r = Board.Make(RequestKind.Gift, h, "Snack for " + name, name + " asked you to bring her something (" + GirlInfo.LikeLabel(like) + "). The store is in the cafeteria.", 6);
                r.Item = like; r.Source = "favor"; r.DeadlineDay = S.Day + 1; r.LearnedFrom = "her";
                return AskFavor(n, ask, r);
            }
            kind = "check";
            var e = Board.Errands[Random.Range(0, Board.Errands.Length)];
            ask = T(h, "Um, could you do me a favor? " + e[1], "Favor! Favor! " + e[1], "I need a hand. " + e[1], "You're free, right? " + e[1]);
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
            b.Branch("yes"); b.Say(h, T(h, "Thank you! Really!", "You're the best!", "...Thanks. I mean it.", "Knew I could count on you.")); b.End();
            b.Branch("no"); b.Say(h, T(h, "Oh... okay. It's fine.", "Aww. Okay!", "...Fine.", "Hmph. Your loss.")); b.End();
            if (heavy)
            {
                b.Branch("more");
                b.Say(h, T(h, "...You noticed? Can we... talk somewhere a little quieter?", "...Ugh, you're sharp. Okay. Not here, though.", "...Perceptive. Not here.", "...Fine. But not in the hallway."));
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
            b.Say(h, T(h, "I'm going to the " + dest + "... would you walk with me? Only if you're free.", "Heading to the " + dest + "! Come with! It's more fun with two!",
                "I'm on my way to the " + dest + ". Walk with me. If you want.", "Escort me to the " + dest + ". You look like you have nothing better to do."));
            b.Choice("Sure, let's go together.", "go", "Maybe next time.", "no");
            b.Branch("go"); b.Say(h, T(h, "Hehe... okay. This way.", "Yay! Let's go!", "...Good. Keep up.", "Good boy. Follow me... or I'll follow you.")); b.End();
            b.Branch("no"); b.Say(h, T(h, "Oh... okay. Another time.", "Aww. Next time for sure!", "...Fine.", "Your loss.")); b.End();
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
                if (Time.realtimeSinceStartup - _started > 420f) { Finish(false, GirlInfo.FirstName(_her) + " laughs: \"We got a little lost, huh?\" and goes on by herself."); return; }
                if (_i >= Stops.Count) { Finish(true); return; }
                var map = Places.MapFor(Stops[_i]);
                if (G.PlayerMapNo != map) { _arrivedAt = -1f; return; }
                if (_arrivedAt < 0f) { _arrivedAt = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - _arrivedAt < 2.5f) return; // the map settles, she comes through the door
                if (_npc.mapNo != G.PlayerMapNo || (_npc.transform.position - act.Player.transform.position).sqrMagnitude > 12f * 12f) return;
                if (!Scenes.CanPlayOnMap) return;
                Beat();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); Finish(false); }
        }

        private static string T(string gentle, string energetic, string cool, string teasing) { return Lines.T(GirlInfo.ToneOf(_her), gentle, energetic, cool, teasing); }

        private static void Beat()
        {
            var h = _her;
            var place = Stops[_i];
            var last = _i == Stops.Count - 1;
            var name = GirlInfo.FirstName(h);
            var b = new SceneBuilder(null, false);
            b.Narr(last ? "You reach the " + Places.Name(place).ToLowerInvariant() + " together." : name + " stops for a moment at the " + Places.Name(place).ToLowerInvariant() + ".");
            b.Say(h, StopLine(place));
            if (!last)
            {
                b.Choice(Reply1(place), "good", "Keep walking with her.", "plain", "Say you have to go.", "leave");
                b.Branch("good"); b.Say(h, T("Hehe... you think so too?", "Right?! You get it!", "...Exactly.", "Oh? We agree on something.")); b.End();
                b.Branch("plain"); b.Say(h, T("Mm. Let's keep going.", "Onward!", "...This way.", "Keep up.")); b.End();
                b.Branch("leave"); b.Say(h, T("Oh... okay. Thanks for walking with me this far.", "Aww, already? Okay, thanks!", "...Fine. Thanks for the company.", "Bailing on me? Hmph. ...Thanks, though.")); b.End();
            }
            else
            {
                var pending = !string.IsNullOrEmpty(S.Girl(GirlInfo.Key(h)).Pending) || Board.HiddenFor(GirlInfo.Key(h)).Any();
                b.Say(h, T("This is where I was going... thank you for coming all the way.", "Made it! That was way more fun than walking alone!", "...We're here. That was... nice.", "Here we are. You were tolerable company."));
                var opts = new List<string> { "Say you'd walk with her again anytime.", "again", "Tell her it was fun.", "fun" };
                if (h.isGirlfriend || h.favor >= 70) { opts.Add("Take her hand for a moment."); opts.Add("hand"); }
                if (pending) { opts.Add("Ask what's really on her mind."); opts.Add("talk"); }
                b.Choice(opts.ToArray());
                b.Branch("again"); b.Say(h, T("...Then I'll ask again. Promise.", "Deal! Every day!", "...I'll remember you said that.", "Careful. I'll hold you to that.")); b.End();
                b.Branch("fun"); b.Say(h, T("It was. Hehe.", "The best!", "...It was.", "It was. Don't let it go to your head.")); b.End();
                b.Branch("hand"); b.Say(h, T("Ah... your hand is warm.", "Hehe! Holding hands!", "...Only for a moment.", "Bold. ...I'll allow it.")); b.End();
                b.Branch("talk"); b.Say(h, T("...You noticed. Can we talk for a bit? Just us.", "...Ugh, you can tell, huh. Okay. Let's talk.", "...Perceptive. Fine. Let's talk.", "...Fine. You earned it. Let's talk.")); b.End();
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

        private static string StopLine(string place)
        {
            switch (place)
            {
                case "gym": return T("I like the smell of the gym... is that weird?", "Gym! My second home! I could do a hundred laps right now!", "...The gym. Where I go to think. Sort of.", "This is where I train. Impressed yet?");
                case "field": return T("The field is so wide... I feel tiny here.", "Wanna race? No? Next time!", "...Good wind today.", "Run a lap. For me. I'll watch.");
                case "library": return T("It's so peaceful in here... I could stay all day.", "Shhh! Library voice! ...This is hard for me.", "...This is the only quiet place in the school.", "Don't make noise. Or do. I like watching people get shushed.");
                case "cafeteria": return T("It smells so good... I'm getting hungry.", "Melon bread! They still have melon bread!", "...The set lunch is underrated.", "If you buy me something, I'll remember it forever. Maybe.");
                case "rooftop": return T("The wind up here feels so nice...", "Woo! You can see EVERYTHING from here!", "...I come here when I need air.", "Nobody comes up here. Just us. Interesting, isn't it?");
                case "clubroom": return T("This is where my club meets... it's a bit messy.", "Club HQ! Don't touch anything! Okay, touch a little!", "...My club's room. It's... a work in progress.", "Our club room. Members only. You can be an honorary one.");
                case "pool": return T("The water looks so clear today...", "I wanna jump in so bad!", "...Smells like chlorine. I like it.", "Swimsuit season. Imagining things?");
                case "courtyard": return T("The flowers here are so pretty...", "A cat was here yesterday! Let's look!", "...It's calm here.", "This bench is mine. You can sit. Maybe.");
                default: return T("It's nice here...", "Ooh, this place is great!", "...Hm. Here.", "Here. Satisfied?");
            }
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
