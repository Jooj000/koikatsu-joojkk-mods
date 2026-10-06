using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A scene shape with roles (actor, target, witness, you), a hook from the seed, a complication from the strongest
    /// fact in the data, choices along the usual axes (back one, back the other, mediate, stay out) labelled by the
    /// situation, and an echo (a memory, a change in the pair, gossip) that shapes what the girls want next.
    /// </summary>
    internal abstract class Mold
    {
        public abstract string Id { get; }
        public virtual bool Big { get { return false; } }           // counts against the weekly drama budget
        public virtual bool Solo { get { return false; } }          // one girl only
        public virtual bool IgnoresCooldown { get { return false; } }
        public abstract IEnumerable<Situation> Detect(List<SaveData.Heroine> girls);
        public abstract void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result);
        /// <summary>How it goes without you; returns the news line (or empty).</summary>
        public abstract string Offscreen(Situation s);

        protected static CampusState S { get { return Campus.S; } }
        protected static string N(SaveData.Heroine h) { return GirlInfo.FirstName(h); }
        protected static string K(SaveData.Heroine h) { return GirlInfo.Key(h); }
        protected static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }
        protected static PairState P(Situation s) { return Pairs.Of(s.A, s.B); }

        protected static IEnumerable<SaveData.Heroine[]> PairsOf(List<SaveData.Heroine> girls)
        {
            for (var i = 0; i < girls.Count; i++)
                for (var j = i + 1; j < girls.Count; j++)
                    yield return new[] { girls[i], girls[j] };
        }

        protected static void Ask(SceneBuilder b, SaveData.Heroine a, SaveData.Heroine bb, string lineA, string lineB)
        {
            b.Say(a, lineA);
            if (bb != null) b.Say(bb, lineB);
        }
    }

    internal static class Molds
    {
        public static readonly List<Mold> All = new List<Mold>
        {
            new Approach(), new Friction(), new Confession(), new Caught(), new Gossip(),
            new Comfort(), new Sides(), new Negotiation(), new Reunion(), new Discovery(),
        };
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Aproximação: two girls who are close and spend their breaks together; something is growing.</summary>
    internal sealed class Approach : Mold
    {
        public override string Id { get { return "approach"; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var bond = Social.Bond(x[0], x[1]);
                if (bond < 40) continue;
                var p = Pairs.Of(x[0], x[1]);
                if (p.Stage == PairStage.Couple || p.Stage == PairStage.Strained || p.Stage == PairStage.Broken) continue;
                var w = 0.5f + p.Together * 0.4f + (Pairs.GirlsOk(x[0]) && Pairs.GirlsOk(x[1]) ? 0.6f : 0f);
                yield return new Situation { A = x[0], B = x[1], Why = p.Together >= 2 ? "always together" : "close friends", Weight = w };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var romance = Pairs.GirlsOk(a) && Pairs.GirlsOk(o);
            intro.Say(a, T(a, "Ah, there you are! We were just talking about you... || Oh, you're here! We were talking about you... || Ah, hello! Your name just came up. || There you are! Perfect timing...", "Hey hey! Perfect timing! || Perfect timing! Come here! || Hey! Just who we needed! || Yo! Get over here!", "You. We need a judge. || You. Judge for us. || We need an opinion. You. || Good. A neutral party.", "Oh, look who's here. Just in time~ || Right on time~ || Speak of the devil~ || Oh, perfect. Look who it is."));
            intro.Say(o, T(o, "Only good things, I promise! || Good things, promise! || Nothing bad, really! || Only nice things!", "Don't listen to her, it was nothing bad! || Nothing bad! Swear! || Ignore her! All good! || Totally positive stuff!", "Mostly good things. || Mostly good. || Largely positive. || Mostly.", "Mostly. || Mostly. || Sort of. || Some of it."));
            b.Narr(N(a) + " and " + N(o) + " stand close, shoulders touching, finishing each other's sentences.");
            b.Say(a, T(a, "We've been spending every break together lately. " + N(o) + " is teaching me her secret study method." + " || " + N(o) + " and I spend every break together now... she's teaching me to study." + " || " + N(o) + " has been so kind to me lately. We're always together. || I've been with " + N(o) + " a lot. She's wonderful.", "Me and " + N(o) + " are basically joined at the hip now! We even have a handshake! || Me and " + N(o) + "? Inseparable! Secret handshake and all!" + " || " + N(o) + "'s my partner in crime now! || We're always together! Me and " + N(o) + "!",
                "We've been studying together. It's... efficient." + " || " + N(o) + " and I study together. Productive." + " || " + N(o) + " and I. Efficient pairing. || We work well together. " + N(o) + " and I.", "We've been inseparable lately. People are starting to talk~" + " || " + N(o) + " and I are inseparable. Rumors are flying~ || People talk about me and " + N(o) + ". Let them." + " || " + N(o) + " and I are close. Very."));
            b.Say(o, romance ? T(o, "...I like being with her. A lot. || ...I like being with her. So much. || ...She makes me happy. || ...I really like her company.", "She's the best! Seriously! || She's amazing! || The best! Really! || She rocks!", "...She's tolerable. Very tolerable. || ...Very tolerable. || ...Pleasant company. || ...I like her. Somewhat.", "She's cute when she concentrates. Don't tell her. || She's cute focused. Shh. || Adorable when serious. Secret. || She's cute. Don't tell.") : T(o, "She's a good friend. || She's a good friend. || She's kind to me. || A dear friend.", "Best friend material! || Best friend material! || Bestie! || Top friend!", "She's reliable. || Reliable. || Dependable. || Solid.", "She's fun to tease. || Fun to tease. || Easy target. || Teasing her is fun."));
            b.Face(a, Feel.Shy);
            b.Choice("Say they look like a couple", "ap_tease", "Invite them both to lunch", "ap_lunch", "Say you're glad they found each other", "ap_glad", "Leave them to it with a smile", "ap_leave");
            b.Branch("ap_tease");
            b.Face(a, Feel.Surprised).Say(a, T(a, "A c-couple?! W-we're not... are we? || A couple?! We're not... are we? || C-couple?! Um... || W-what? Us?!", "W-WHAT?! No! ...Wait, are we? || WHAT?! No! ...Are we?! || Couple?! Nah! ...Wait! || Huh?! Us?! ...Maybe?!", "...That's an unfounded conclusion. || ...Unfounded. || ...Baseless. || ...Speculation.", "Hah! ...Hah. ...Wait. || Hah! ...Wait. || Ha! ...Hm. || Funny. ...Hm."));
            b.Face(o, Feel.Shy).Say(o, romance ? T(o, "...Would that be so bad? || ...Would that be bad? || ...Maybe I wouldn't mind... || ...Is that so wrong?", "...I mean... I wouldn't hate it? || ...Wouldn't hate it! || ...I mean... maybe! || ...Not bad, actually!", "...Not impossible. || ...Not impossible. || ...Possible. || ...Conceivable.", "...Don't say it so loud. || ...Not so loud. || ...Shh. || ...Keep it down.") : T(o, "Ahaha, no way! || Ahaha, no way! || No, no! Friends! || Hehe, of course not!", "We're just friends, silly! || Just friends, silly! || Nope! Friends! || Haha! No!", "No. || No. || Incorrect. || Friends.", "In your dreams. || In your dreams. || Dream on. || As if."));
            b.End();
            b.Branch("ap_lunch");
            b.Face(a, Feel.Smile).Say(a, T(a, "Lunch together? I'd love that! || Lunch together? Yes! || I'd love lunch together! || Lunch? Oh, yes!", "Yes! Lunch party! || Lunch party! || Yes! Food! || Let's eat!", "...Acceptable. || ...Acceptable. || ...Fine. || ...Agreed.", "Oh? Treating us both? Generous. || Treating us? Generous. || Paying for both? Nice. || Oh? Lunch on you?"));
            b.End();
            b.Branch("ap_glad");
            b.Face(o, Feel.Smile).Say(o, T(o, "Thank you... me too. || Thank you... me too. || Me too... || Same...", "Hehe, me too! || Me too! || Hehe, same! || Me too!!", "...Me too. || ...Same. || ...Me too. || ...Agreed.", "...Yeah. Me too. || ...Me too. || ...Yeah. || ...Same here."));
            b.End();
            b.Branch("ap_leave");
            b.Narr("You leave them to their laughter. Behind you, " + N(a) + " whispers something and " + N(o) + " turns red.");
            b.End();
            result = c =>
            {
                Arcs.AddBond(K(a), K(o), 4);
                switch (c)
                {
                    case "ap_tease": Pairs.OnTeasedTogether(a, o); Pairs.Nudge(K(a), K(o), romance ? 4 : 0, 2); break;
                    case "ap_lunch": G.AddFavor(a, 3, 0); G.AddFavor(o, 3, 0); Memories.Add(a, "lunch", "you invited " + N(o) + " and me to lunch", 1); break;
                    case "ap_glad": Pairs.Nudge(K(a), K(o), 2, 4); G.AddFavor(a, 2, 0); G.AddFavor(o, 2, 0); break;
                    case "ap_leave": Pairs.Nudge(K(a), K(o), romance ? 5 : 0, 3); break;
                }
            };
        }

        public override string Offscreen(Situation s)
        {
            Arcs.AddBond(K(s.A), K(s.B), 3);
            Pairs.Nudge(K(s.A), K(s.B), 3, 2);
            return N(s.A) + " and " + N(s.B) + " spent the whole break together again.";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Atrito: jealousy, a broken promise or a rough patch between two girls.</summary>
    internal sealed class Friction : Mold
    {
        public override string Id { get { return "friction"; } }
        public override bool Big { get { return true; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var p = S.FindPair(K(x[0]), K(x[1]));
                if (p != null && p.Stage == PairStage.Couple && p.Jealousy >= 45) yield return new Situation { A = x[0], B = x[1], Why = "jealousy", Weight = 1.2f + p.Jealousy / 50f };
                else if (p != null && p.Stage == PairStage.Strained) yield return new Situation { A = x[0], B = x[1], Why = "a rough patch", Weight = 1.4f };
                else if (p != null && p.Arc == PairArc.Rivals) yield return new Situation { A = x[0], B = x[1], Why = "rivalry", Weight = 1f };
                else if (Social.Bond(x[0], x[1]) <= -25) yield return new Situation { A = x[0], B = x[1], Why = "bad blood", Weight = 0.7f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B; var why = s.Why;
            var couple = why == "jealousy" || why == "a rough patch";
            intro.Say(a, T(a, "Um... can you help us with something? || Could you help us? || Um... can we ask you something? || Help us, please?", "You! Settle this for us! || Settle this! || You! Judge! || Help! Decide!", "We need a third opinion. || A third opinion. || We need a judge. || Arbitrate.", "Perfect, a witness. Get over here. || A witness. Come here. || You. Testify. || Perfect. Get over here."));
            intro.Say(o, T(o, "It's not a big deal... || It's nothing big... || It's minor... || Not a big deal...", "It IS a big deal! || It IS big! || Super big deal! || Huge!", "It's a minor matter. || Minor. || Trivial. || Small.", "Don't believe a word she says. || Don't trust her. || She lies. || Ignore her version."));
            b.Narr("The air between " + N(a) + " and " + N(o) + " is tight enough to snap.");
            if (couple)
            {
                b.Face(a, Feel.Pout).Say(a, T(a, "She was laughing with another girl all lunch. She forgot we were supposed to eat together. || She laughed with another girl all lunch... she forgot our plans. || She forgot we were eating together... || She left me alone at lunch...", "She ditched me for lunch! For someone else! Again! || She ditched me! Again! || Lunch! Abandoned! Again! || She bailed! For someone else!",
                    "She broke a plan we had. Twice now. || She broke plans. Twice. || Plans canceled. Again. || Twice now. Broken promises.", "Apparently I'm only her girlfriend when it's convenient. || Girlfriend when convenient, apparently. || Only her girlfriend when it suits her. || I'm part-time, apparently."));
                b.Face(o, Feel.Worried).Say(o, T(o, "I didn't forget! I just... lost track of time. I'm sorry... || I didn't forget! I lost track... sorry... || I'm sorry... time just slipped... || I didn't mean to... sorry...", "That's not fair! I said sorry! I was helping a friend! || I said sorry! I was helping! || Not fair! I apologized! || I was busy helping! Sorry!", "I said I was sorry. She's not listening. || I apologized. She ignores it. || I said sorry. Unheard. || Apology given. Rejected.", "And she's being dramatic. As usual. || She's being dramatic. As always. || Drama queen. || Overreacting, as usual."));
            }
            else
            {
                b.Face(a, Feel.Pout).Say(a, T(a, "She took the spot I'd reserved for my club. Without asking. || She took my club's spot. Without asking. || She took our space... || She didn't ask before taking it...", "She said my drawing was childish! In front of everyone! || She called my drawing childish! Publicly! || In front of everyone! Mean! || She mocked me!",
                    "She keeps undermining me in class. || She undermines me. || She contradicts me. Constantly. || She belittles me in class.", "She thinks she's better than me. She isn't. || She thinks she's better. She's not. || She acts superior. Laughable. || Miss Perfect. Hardly."));
                b.Face(o, Feel.Serious).Say(o, T(o, "That isn't what happened... || That's not what happened... || It wasn't like that... || That's not true...", "Oh come ON! I was joking! || It was a JOKE! || Come ON! Joking! || Lighten up!", "Her version is selective. || Selective version. || Partial truth. || Incomplete.", "I'm just honest. She can't take it. || I'm honest. She can't handle it. || Truth hurts. || I'm just frank."));
            }
            var opts = new List<string> { "Take " + N(a) + "'s side", "fr_a", "Take " + N(o) + "'s side", "fr_b" };
            opts.Add(G.Int >= 40 ? "Get them to say what really hurt [INT]" : "Try to calm them down"); opts.Add("fr_mid");
            opts.Add("Say it's between the two of them"); opts.Add("fr_out");
            b.Choice(opts.ToArray());
            b.Branch("fr_a");
            b.Face(a, Feel.Smile).Say(a, T(a, "...Thank you. || ...Thank you. || ...Thanks. || ...I'm grateful.", "See?! Thank you! || See?! Thanks! || Ha! Thanks! || Yes! Thank you!", "Obviously. || Obviously. || Naturally. || Of course.", "Told you. || Told you. || Duh. || See?"));
            b.Face(o, Feel.Sad).Say(o, T(o, "...I see. Fine. || ...Fine. || ...I see. || ...Okay.", "Ugh! Whatever! || Whatever! || Ugh! || Fine!", "...Noted. || ...Noted. || ...Hm. || ...Remembered.", "Wow. Okay. || Wow. || Okay. || Noted."));
            b.End();
            b.Branch("fr_b");
            b.Face(o, Feel.Smile).Say(o, T(o, "...Thank you. || ...Thank you. || ...Thanks. || ...Thank you so much.", "Ha! See?! || Ha! See?! || Yes! || Told you!", "Thank you. || Thank you. || Thanks. || Good.", "Finally, someone sensible. || Someone sensible. || Finally. || A reasonable person."));
            b.Face(a, Feel.Sad).Say(a, T(a, "...Oh. || ...Oh. || ...I see. || ...Hm.", "Seriously?! || Seriously?! || What?! || No way!", "...Hmph. || ...Hmph. || ...Tch. || ...Fine.", "Traitor. || Traitor. || Turncoat. || Wow."));
            b.End();
            b.Branch("fr_mid");
            var good = G.Int >= 40 || Random.value < 0.5f;
            if (good)
            {
                b.Narr("You get each of them to say one thing, slowly. It turns out the fight was never about lunch or a seat.");
                b.Say(a, T(a, "...I just felt left behind. || ...I felt left behind. || ...I felt forgotten. || ...I was lonely.", "...I just wanted you to pick me. That's all. || ...I wanted you to pick me. || ...I just wanted to be first. || ...Choose me, that's all.", "...I felt unimportant. || ...I felt unimportant. || ...Overlooked. || ...Secondary.", "...Fine. I was jealous. Happy? || ...Fine. Jealous. Happy? || ...Okay. I was jealous. || ...Jealous. There."));
                b.Say(o, T(o, "...I didn't know. I'm sorry. || ...I didn't know. Sorry. || ...I'm sorry. I didn't realize. || ...Oh... I'm so sorry.", "...Oh. I'm sorry. For real. || ...Oh. Sorry. For real. || ...I'm sorry! Really! || ...Oh no. Sorry!", "...Understood. I'm sorry. || ...Understood. Sorry. || ...I see. Apologies. || ...Sorry.", "...Idiot. You could've just said so. || ...Idiot. Just say so. || ...Dummy. Tell me next time. || ...You could've said."));
            }
            else
            {
                b.Narr("You try, but every word you say is taken by one of them as proof she's right.");
                b.Say(o, T(o, "...Let's just drop it. || ...Let's drop it. || ...Never mind. || ...Forget it.", "Forget it! || Forget it! || Whatever! || Drop it!", "This is pointless. || Pointless. || Futile. || Enough.", "Nice try, peacemaker. || Nice try. || Cute attempt. || No."));
            }
            b.End();
            b.Branch("fr_out");
            b.Say(a, T(a, "...You're right. Sorry for pulling you in. || ...You're right. Sorry. || ...Sorry for involving you. || ...You're right.", "Ugh, fine. Sorry. || Fine. Sorry. || Ugh. Okay. Sorry. || Sorry!", "...Fair. || ...Fair. || ...Reasonable. || ...True.", "Coward. ...Fair, though. || Coward. Fair. || Neutral? Coward. Fine. || Fair. Cowardly, but fair."));
            b.End();
            result = c =>
            {
                var p = P(s);
                switch (c)
                {
                    case "fr_a": G.AddFavor(a, 4, 0); G.AddFavor(o, -3, 0); Social.AddTrust(a, 4); Social.AddTrust(o, -4); p.Trust = PairState.Clamp(p.Trust - 6); Social.Add(Social.Kinds.Defended, a, new[] { o }); break;
                    case "fr_b": G.AddFavor(o, 4, 0); G.AddFavor(a, -3, 0); Social.AddTrust(o, 4); Social.AddTrust(a, -4); p.Trust = PairState.Clamp(p.Trust - 6); Social.Add(Social.Kinds.Defended, o, new[] { a }); break;
                    case "fr_mid":
                        if (good) { p.Trust = PairState.Clamp(p.Trust + 15); p.Jealousy = Mathf.Max(0, p.Jealousy - 25); Arcs.AddBond(K(a), K(o), 8); Social.AddTrust(a, 3); Social.AddTrust(o, 3); }
                        else { p.Trust = PairState.Clamp(p.Trust - 3); }
                        break;
                    case "fr_out": p.Trust = PairState.Clamp(p.Trust + 2); break;
                }
                Pairs.Advance(p);
                Memories.Add(a, "friction", "you were there when " + N(o) + " and I fought", c == "fr_a" ? 1 : c == "fr_b" ? -1 : 0, true, K(o));
                Memories.Add(o, "friction", "you were there when " + N(a) + " and I fought", c == "fr_b" ? 1 : c == "fr_a" ? -1 : 0, true, K(a));
            };
        }

        public override string Offscreen(Situation s)
        {
            var p = P(s);
            p.Trust = PairState.Clamp(p.Trust - 8);
            p.Jealousy = PairState.Clamp(p.Jealousy + 5);
            Arcs.AddBond(K(s.A), K(s.B), -4);
            Pairs.Advance(p, true);
            return N(s.A) + " and " + N(s.B) + " had an argument in the hallway (" + s.Why + ").";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Confissão: one girl's feelings for another are strong enough to say out loud.</summary>
    internal sealed class Confession : Mold
    {
        public override string Id { get { return "confession"; } }
        public override bool Big { get { return true; } }
        public override bool IgnoresCooldown { get { return true; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            if (!Cfg.StudentRomance.Value) yield break;
            foreach (var x in PairsOf(girls))
            {
                var p = S.FindPair(K(x[0]), K(x[1]));
                if (p == null || p.Stage != PairStage.Talking && p.Stage != PairStage.Crush) continue;
                if (Pairs.PairOf(x[0]) != null || Pairs.PairOf(x[1]) != null) continue;
                var seed = Director.HasSeed("confession", K(x[0]), K(x[1]));
                var a = p.RomanceOf(K(x[0])) >= p.RomanceOf(K(x[1])) ? x[0] : x[1];
                var o = a == x[0] ? x[1] : x[0];
                if (p.RomanceOf(K(a)) < 55 && !seed) continue;
                yield return new Situation { A = a, B = o, Why = seed ? "she promised herself" : "feelings", Weight = seed ? 5f : 1.5f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var p = P(s);
            var yes = p.RomanceOf(K(o)) >= 40;
            intro.Say(a, T(a, "C-can you stay with me for a moment? I need courage... || Stay with me? I need courage... || Please stay... I'm scared. || Could you stay a moment?", "Stay! Please! I need backup! || Stay! Backup needed! || Don't leave! Help! || Stay! Moral support!", "Don't go anywhere. I need a witness. || Stay. Witness. || Don't leave. || Remain here.", "Stay. If I chicken out, push me. || Stay. Push me if I chicken out. || If I run, stop me. || Stay. Keep me brave."));
            intro.Say(o, T(o, "What's going on...? || What's happening...? || What's going on? || Hm...?", "Huh? What's with the serious faces? || Serious faces?! || Huh? What's up?! || Why so serious?!", "What is this about? || What is this? || Explain. || What's this about?", "Ooh, mysterious. || Mysterious. || Ooh, drama. || Intriguing."));
            b.Narr(N(a) + " takes a long breath, glances at you, and turns to " + N(o) + ".");
            b.Face(a, Feel.Love).Say(a, T(a, N(o) + "... I like you. Not as a friend. I've liked you for a while." + " || " + N(o) + "... I like you. More than a friend." + " || " + N(o) + ", I've liked you for so long... || I love you, " + N(o) + ". Not as friends.", N(o) + "! I like you! Like, really LIKE you! There, I said it!" + " || " + N(o) + "! I LIKE you! Really!" + " || " + N(o) + "! Crush! You! Done! || I like you, " + N(o) + "! A lot!",
                N(o) + ". I have feelings for you. Romantic ones." + " || " + N(o) + ". Romantic feelings. For you." + " || " + N(o) + ". I love you." + " || " + N(o) + ". Feelings. Romantic.", "So, " + N(o) + "... I like you. Seriously. Don't laugh." + " || " + N(o) + "... I like you. Don't laugh. || So, " + N(o) + ". I like you. Seriously." + " || " + N(o) + ", I'm into you. Really."));
            b.Choice("Give her an encouraging nod", "cf_nod", "Step back and give them privacy", "cf_back", "Break the tension with a joke", "cf_joke");
            b.Branch("cf_nod"); b.Label("cf_answer");
            if (yes)
            {
                b.Face(o, Feel.Love).Say(o, T(o, "...Me too. I was too scared to say it first. || ...Me too. I was too scared. || ...I feel the same... || ...Me too. Always.", "...REALLY?! Me too! Oh my gosh! || REALLY?! Me too! || No way! Same! || Yes! Me too!", "...The feeling is mutual. || ...Mutual. || ...Same. || ...Reciprocated.", "...Took you long enough. Me too. || ...Took long enough. Me too. || ...Finally. Same. || ...About time. Me too."));
                b.Narr("Their hands find each other. You've never seen either of them smile like that.");
            }
            else
            {
                b.Face(o, Feel.Worried).Say(o, T(o, "I... I'm sorry. I care about you, but not like that. Can we still be friends? || I care about you... but not that way. Friends? || I'm so sorry... only as a friend. || Please... stay my friend?", "Oh... I'm so sorry. I love you as a friend, but... || I'm so sorry! Friend love only! || Oh no... friends? || Sorry! Just friends!", "...I don't feel the same. I'm sorry. || ...Not the same. Sorry. || ...I don't reciprocate. || ...No. Sorry.", "...Hey. I'm flattered. But no. Sorry. || Flattered. But no. Sorry. || Sweet. No. Sorry. || Nice. But no."));
                b.Face(a, Feel.Sad).Say(a, T(a, "...I see. Thank you for being honest. || ...Thank you for honesty. || ...I understand. Thank you. || ...Okay. Thanks.", "...Ahaha. Yeah. Okay. Okay... || ...Ahaha. Okay... || ...Yeah. Okay... || ...Okay. Okay.", "...Understood. || ...Understood. || ...I see. || ...Accepted.", "...Yeah. Figured. It's fine. || ...Figured. Fine. || ...Expected. It's fine. || ...Yeah. Okay."));
            }
            b.End();
            b.Branch("cf_back");
            b.Narr("You step away. From the corner of the hall you hear " + N(o) + "'s voice, then a silence that " + (yes ? "ends in laughter." : "goes on a little too long."));
            b.End();
            b.Branch("cf_joke");
            b.Face(a, Feel.Pout).Say(a, T(a, "D-don't make fun of me right now...! || Don't tease me now...! || Please, not now...! || S-stop it...!", "Dude! Not now! || Not now! || Dude! Seriously! || Not the time!", "...Unhelpful. || ...Unhelpful. || ...Not helpful. || ...Stop.", "...I'm going to kill you later. || ...I'll kill you later. || ...You're dead later. || ...Revenge pending."));
            b.Face(o, Feel.Smile).Say(o, T(o, "...Hehe. Okay. I'm listening, " + N(a) + ". || ...Okay. Listening, " + N(a) + ". || ...Hehe. Go on, " + N(a) + ". || ...I'm listening.", "Pfft! Okay, okay. Go on. || Okay! Go on! || Pfft! Continue! || Alright! Go!", "...Continue. || ...Continue. || ...Go on. || ...Proceed.", "Heh. Go on, then. || Go on, then. || Heh. Continue. || Proceed."));
            b.Goto("cf_answer");
            result = c =>
            {
                Director.UseSeed("confession", K(a), K(o));
                p = P(s);
                if (yes) { Pairs.BecomeCouple(p, "confessed with you there"); if (c == "cf_nod") { G.AddFavor(a, 5, 0); Memories.Add(a, "confession", "you stood by me when I confessed to " + N(o), 1, true, K(o)); } }
                else { p.AddRomance(K(a), -25); p.Stage = PairStage.Crush; p.StageDay = S.Day; Pairs.Advance(p); if (c == "cf_nod") { Social.AddTrust(a, 4); Board.Add(Board.Make(RequestKind.Talk, a, "Cheer up " + N(a), N(a) + " was turned down. Talk to her.", 5)); } }
            };
        }

        public override string Offscreen(Situation s)
        {
            var p = P(s);
            Director.UseSeed("confession", K(s.A), K(s.B));
            if (p.RomanceOf(K(s.B)) >= 40) { Pairs.BecomeCouple(p, "a confession behind the gym"); return ""; }
            p.AddRomance(K(s.A), -25);
            p.Stage = PairStage.Crush;
            return N(s.A) + " confessed to " + N(s.B) + ". It didn't go the way she hoped.";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Flagra: you walk in on two girls being close (a secret couple, or one who already has a girlfriend).</summary>
    internal sealed class Caught : Mold
    {
        public override string Id { get { return "caught"; } }
        public override bool Big { get { return true; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var p = S.FindPair(K(x[0]), K(x[1]));
                if (p == null || p.Intimacy < 25) continue;
                var cheating = Pairs.PairOf(x[0]) != null && Pairs.PairOf(x[0]) != p || Pairs.PairOf(x[1]) != null && Pairs.PairOf(x[1]) != p;
                if (p.Stage == PairStage.Couple && p.Exposure < 50 || cheating)
                    yield return new Situation { A = x[0], B = x[1], Why = cheating ? "cheating" : "secret couple", Weight = cheating ? 1.5f : 1f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var cheating = s.Why == "cheating";
            var third = cheating ? (Pairs.PartnerOf(a) ?? Pairs.PartnerOf(o)) : null;
            intro.Narr("You turn the corner and almost walk right into " + N(a) + " and " + N(o) + ", much closer than friends usually stand.");
            intro.Say(a, T(a, "Ah?! W-wait, it's not...! || W-wait! It's not...! || Ah! It's not what...! || Eek! Wait!", "WAAH! Where did you come from?! || WAAH! Where'd you come from?! || Eek! Hi?! || Whoa! Surprise!", "...This is awkward. || ...Awkward. || ...Uncomfortable. || ...Hm.", "...Well. This is a thing that's happening. || ...Well. This is happening. || ...Oops. || ...Hello there."));
            b.Face(a, Feel.Shy).Say(a, T(a, "Please... can you keep this between us? || Please keep this secret? || Between us... please? || Could you not tell anyone?", "Okay, okay, you saw nothing, right? RIGHT? || You saw NOTHING! Right?! || Nothing happened! Okay?! || Shh! Secret!", "I'd appreciate your discretion. || Discretion, please. || Silence appreciated. || Keep it private.", "So. You're going to keep quiet. Aren't you? || You'll keep quiet. Right? || Silence. Understood? || Not a word. Got it?"));
            b.Face(o, Feel.Shy).Say(o, cheating ? T(o, "If " + N(third) + " finds out... || If " + N(third) + " finds out..." + " || " + N(third) + " can't know... || Please, not " + N(third) + "...", "If " + N(third) + " hears about this, I'm dead!" + " || " + N(third) + " can't hear this! || If " + N(third) + " knows, I'm dead! || Keep it from " + N(third) + "!", N(third) + " can't know." + " || " + N(third) + " must not know. || Not " + N(third) + "." + " || " + N(third) + " stays uninformed.", "Not a word to " + N(third) + ". || Not a word to " + N(third) + "." + " || " + N(third) + " hears nothing. || Keep " + N(third) + " out of it.")
                : T(o, "We're not ready for everyone to know yet... || We're not ready to tell everyone... || We want to wait... || It's too soon to tell people...", "We wanted to tell people ourselves! || We'll tell people ourselves! || Our announcement! Later! || We wanna tell them!", "We prefer privacy. || Privacy, please. || We prefer discretion. || Private matter.", "It's our business. For now. || Our business. For now. || Private. For now. || None of their business."));
            var opts = new List<string> { "Promise to keep their secret", "cg_keep", "Tease them a little", "cg_tease" };
            if (cheating) { opts.Add("Say " + N(third) + " deserves the truth"); opts.Add("cg_truth"); }
            else { opts.Add("Tell them they make a cute couple"); opts.Add("cg_cute"); }
            opts.Add("Pretend you saw nothing and walk on"); opts.Add("cg_walk");
            b.Choice(opts.ToArray());
            b.Branch("cg_keep"); b.Face(a, Feel.Smile).Say(a, T(a, "Thank you... really. || Thank you... really. || Thank you so much. || I'm grateful...", "You're the best! I owe you one! || You're the best! || Owe you one! || Thanks a ton!", "...I owe you. || ...I owe you. || ...Indebted. || ...Thanks.", "...Good. You're alright. || ...You're alright. || ...Good. || ...Okay, you pass.")); b.End();
            b.Branch("cg_tease"); b.Face(o, Feel.Pout).Say(o, T(o, "S-stop it...! || S-stop...! || Don't...! || Please stop...", "Hey! Don't make it weird! || Don't make it weird! || Hey! Stop! || Not weird!", "...Childish. || ...Childish. || ...Juvenile. || ...Stop.", "...Laugh it up. || ...Laugh it up. || ...Enjoy it. || ...Funny, huh.")); b.End();
            b.Branch("cg_truth"); b.Face(a, Feel.Sad).Say(a, T(a, "...You're right. I can't keep doing this. || ...You're right. I can't keep this up. || ...I know. I need to stop. || ...You're right...", "...Ugh. Yeah. I know. I KNOW. || ...Ugh. I KNOW. || ...Yeah. I know! || ...Fine! I know!", "...I know. || ...I know. || ...Aware. || ...Yes.", "...Yeah. I know. || ...Yeah. I know. || ...I'm aware. || ...Yeah.")); b.End();
            b.Branch("cg_cute"); b.Face(a, Feel.Love).Say(a, T(a, "Y-you think so? ...Thank you. || You think so? Thank you. || Really? ...Thanks. || Oh... thank you.", "Hehe, right?! We are! || Right?! We are! || Hehe! Yes! || We are, right?!", "...Thank you. || ...Thanks. || ...Thank you. || ...Appreciated.", "Obviously. || Obviously. || Of course. || Naturally.")); b.End();
            b.Branch("cg_walk"); b.Narr("You keep walking. Behind you, two sighs of relief."); b.End();
            result = c =>
            {
                var p = P(s);
                switch (c)
                {
                    // Keeping the secret also warms them to you; teasing them stirs them up (user, 2026-10-11).
                    case "cg_keep": Social.AddTrust(a, 6); Social.AddTrust(o, 6); G.AddFavor(a, 4, 0); G.AddFavor(o, 4, 0); Memories.Add(a, "secret", "you kept " + N(o) + "'s and my secret", 1, true, K(o)); break;
                    case "cg_tease": p.Exposure = PairState.Clamp(p.Exposure + 20); Social.AddTrust(a, -2); G.AddFavor(a, 0, 5); G.AddFavor(o, 0, 5); break;
                    case "cg_cute": p.Exposure = PairState.Clamp(p.Exposure + 30); p.Trust = PairState.Clamp(p.Trust + 5); G.AddFavor(a, 2, 0); G.AddFavor(o, 2, 0); break;
                    case "cg_truth":
                        if (third != null)
                        {
                            var tp = Pairs.PairOf(third);
                            if (tp != null) Pairs.Break(tp, "the truth came out");
                            Rumors.Start("caught", K(a), K(o), K(third), 55);
                        }
                        Social.AddTrust(a, -3);
                        break;
                    case "cg_walk": break;
                }
                if (cheating && c != "cg_truth" && c != "cg_keep" && Random.value < 0.4f) Rumors.Start("caught", K(a), K(o), "", 40);
            };
        }

        public override string Offscreen(Situation s)
        {
            var p = P(s);
            p.Exposure = PairState.Clamp(p.Exposure + 25);
            if (s.Why == "cheating") { Rumors.Start("caught", K(s.A), K(s.B), "", 50); return "Someone saw " + N(s.A) + " and " + N(s.B) + " together behind the gym..."; }
            return p.Exposure >= 50 ? "Everyone knows about " + N(s.A) + " and " + N(s.B) + " now." : "";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Fofoca: a rumor with heat reaches two friends, and they want your take.</summary>
    internal sealed class Gossip : Mold
    {
        public override string Id { get { return "gossip"; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            var sc = Rumors.Hottest();
            if (sc == null) yield break;
            foreach (var x in PairsOf(girls))
            {
                if (K(x[0]) == sc.Subject || K(x[1]) == sc.Subject) continue;
                if (Social.Bond(x[0], x[1]) < 30) continue;
                yield return new Situation { A = x[0], B = x[1], Why = "rumor " + sc.Id, Weight = 0.6f + sc.Heat / 60f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var sc = Rumors.ById(s.Why);
            var version = sc == null ? "something about a girl in class" : Rumors.VersionFor(sc, a);
            var subj = sc == null ? null : Social.Get(sc.Subject);
            intro.Say(a, T(a, "Psst! Did you hear...? || Psst! Have you heard...? || Um, did you hear? || Psst... come here...", "Oh my gosh, you HAVE to hear this! || You HAVE to hear this! || OMG! News! || Listen listen!", "You'll want to hear this. || Worth hearing. || Listen. || You'll want this.", "Ooh, perfect, fresh ears. || Fresh ears. Perfect. || Ooh, a new audience. || Come here. Gossip."));
            b.Face(a, Feel.Surprised).Say(a, T(a, "They say " + version + "... || People say " + version + "... || I heard " + version + "... || Someone said " + version + "...", "So apparently " + version + "! Can you believe it?! || Apparently " + version + "! Crazy!" + " || " + version + "! Can you believe it?! || Get this: " + version + "!", "Word is that " + version + ". || Reportedly, " + version + "." + " || " + version + ", they say. || The word: " + version + ".", "Rumor has it " + version + ". Juicy, right? || Rumor: " + version + ". Juicy." + " || " + version + ". Delicious. || They say " + version + ". Spicy."));
            b.Face(o, Feel.Normal).Say(o, T(o, "I don't know if it's true, though... || Maybe it's not true... || I'm not sure it's real... || It might be wrong...", "No way! Is it true?! || Is it true?! || Real?! || No way!", "Unverified. || Unconfirmed. || Unverified. || Uncertain.", "Totally true. Probably. || True. Probably. || Definitely real. Maybe. || Totally. Possibly."));
            var opts = new List<string> { "Ask where they heard it", "gs_where" };
            if (subj != null) { opts.Add("Stand up for " + N(subj)); opts.Add("gs_defend"); }
            opts.Add("Join in"); opts.Add("gs_join");
            opts.Add("Change the subject"); opts.Add("gs_change");
            b.Choice(opts.ToArray());
            b.Branch("gs_where"); b.Say(a, T(a, "Um... from " + (sc != null && !string.IsNullOrEmpty(sc.Teller) ? GirlInfo.FirstName(sc.Teller) : "someone") + ", I think? || From " + ((sc != null && !string.IsNullOrEmpty(sc.Teller) ? GirlInfo.FirstName(sc.Teller) : "someone")) + ", I think... || I heard it from " + ((sc != null && !string.IsNullOrEmpty(sc.Teller) ? GirlInfo.FirstName(sc.Teller) : "someone")) + "... || Um... " + ((sc != null && !string.IsNullOrEmpty(sc.Teller) ? GirlInfo.FirstName(sc.Teller) : "someone")) + " said it.", "Uh... everyone's saying it! || Everyone! || The whole school! || Everybody's saying it!", "...Second-hand. || ...Second-hand. || ...Hearsay. || ...Indirect.", "Does it matter? ...Fine, I don't know. || Who cares? ...Don't know. || Does it matter? || No idea. Who cares.")); b.End();
            b.Branch("gs_defend"); b.Face(o, Feel.Shy).Say(o, T(o, "...You're right. We shouldn't spread it. || ...Right. We shouldn't spread it. || ...You're right. Let's stop. || ...Okay. No more gossip.", "...Okay, okay. You're right. || ...Okay! You're right! || ...Fine! Fair! || ...Yeah, okay!", "...Fair. || ...Fair. || ...True. || ...Agreed.", "...Boring. But fair. || ...Boring. Fair. || ...Dull. Okay. || ...Spoilsport. Fine.")); b.End();
            b.Branch("gs_join"); b.Face(a, Feel.Laugh).Say(a, T(a, "I know, right?! || I know, right?! || Right?! || Hehe, yes!", "RIGHT?! Ahaha! || RIGHT?! || Ahaha! Yes! || Totally!", "Hm. You agree. || You agree. || Hm. Yes. || Agreed.", "Ha! I knew you'd get it. || Knew you'd get it. || Ha! Yes. || See?")); b.End();
            b.Branch("gs_change"); b.Say(o, T(o, "...Yeah, let's talk about something else. || ...Let's talk about something else. || ...Okay, new topic. || ...Never mind.", "Aw, fine. Boring! || Boring! Fine! || Aw! Okay! || Lame! Fine!", "...Fine. || ...Fine. || ...Okay. || ...Next.", "Spoilsport. || Spoilsport. || Party pooper. || Killjoy.")); b.End();
            result = c =>
            {
                if (sc == null) return;
                switch (c)
                {
                    case "gs_where": Rumors.Trace(sc); break;
                    case "gs_defend": Rumors.Cool(sc, 20); if (subj != null) { Social.AddTrust(subj, 4); Social.Add(Social.Kinds.Defended, subj, new[] { a, o }); Pairs.OnHelped(subj); } break;
                    case "gs_join": Rumors.Heat(sc, 15); if (subj != null) G.AddFavor(subj, -3, 0); break;
                    case "gs_change": Rumors.Cool(sc, 5); break;
                }
            };
        }

        public override string Offscreen(Situation s)
        {
            var sc = Rumors.ById(s.Why);
            if (sc != null) Rumors.Heat(sc, 8);
            return "";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Consolo: someone is hurt (a breakup, a rumor, a bad day) and her friend brings her to you.</summary>
    internal sealed class Comfort : Mold
    {
        public override string Id { get { return "comfort"; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var h in girls)
            {
                var gs = S.Girl(K(h));
                string why = null;
                var broke = S.Pairs.Values.FirstOrDefault(p => (p.A == K(h) || p.B == K(h)) && p.Stage == PairStage.Broken && S.Day - p.StageDay <= 3);
                if (broke != null) why = "breakup";
                else if (Rumors.HeatAbout(K(h)) >= 40) why = "rumor";
                else if (gs.Mood == Mood.Upset || gs.Mood == Mood.Lonely) why = "bad day";
                if (why == null || Pairs.IsScorned(h)) continue;
                var friend = girls.Where(f => f != h && Social.Bond(h, f) >= 35).OrderByDescending(f => Social.Bond(h, f)).FirstOrDefault();
                if (friend == null) continue;
                yield return new Situation { A = friend, B = h, Why = why, Weight = why == "breakup" ? 1.8f : 1f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var f = s.A; var h = s.B;
            intro.Say(f, T(f, "Um... " + N(h) + " isn't doing so well. Can you talk to her a little?" + " || " + N(h) + " is feeling down... could you talk to her? || Um, " + N(h) + " needs someone... please? || Could you cheer " + N(h) + " up?", "Hey! " + N(h) + " needs cheering up. Help me out!" + " || " + N(h) + " needs cheering! Help! || Help me cheer up " + N(h) + "!" + " || " + N(h) + "'s sad! Mission time!", N(h) + " needs company. Yours will do." + " || " + N(h) + " needs company. || Talk to " + N(h) + "." + " || " + N(h) + " could use you.", "Make yourself useful. " + N(h) + "'s down. || Be useful. " + N(h) + "'s down." + " || " + N(h) + " needs you. Go. || Fix " + N(h) + "'s mood."));
            b.Narr(N(h) + " keeps her eyes on the floor. " + N(f) + " stays close, a hand on her back.");
            var line = s.Why == "breakup" ? T(h, "It's over between us... I thought it would last. || It's over... I thought it would last. || We broke up... I'm so sad. || It ended... I didn't want it to.", "We broke up. It's dumb. I'm fine. I'm totally fine. || Broke up! I'm FINE! Totally! || Single now! Fine! Really! || Dumped! I'm okay! Kinda!", "We broke up. That's all. || Broke up. That's all. || It ended. || Over. Moving on.", "Got dumped. Or I dumped her. I don't even know anymore. || Dumped. Or dumper. Whatever. || It's over. Who cares. || Single. Again.")
                : s.Why == "rumor" ? T(h, "Everyone's whispering about me... I can hear them. || Everyone's whispering... || I can hear them talking about me... || The rumors hurt...", "Everyone keeps staring at me! I hate it! || Everyone's staring! Hate it! || Stop looking at me! || Whispers everywhere!", "The rumors are... tiresome. || Tiresome rumors. || The gossip is exhausting. || Rumors. Tedious.", "Apparently I'm the school's favorite topic. Lucky me. || School's favorite topic. Me. || Celebrity status. Yay. || Everyone loves talking about me.")
                : T(h, "It's just been one of those days... || Just one of those days... || A hard day... || Today was tough...", "Today was the WORST. Everything went wrong! || WORST day! || Everything went wrong! || Today stinks!", "...Bad day. || ...Bad day. || ...Rough. || ...Off day.", "Today sucked. That's all. || Today sucked. || Awful day. || Bad. Leave it.");
            b.Face(h, Feel.Sad).Say(h, line);
            b.Choice("Sit with her and listen", "co_listen", "Make her laugh", "co_laugh", "Give her some honest advice", "co_advice", "Let " + N(f) + " handle it", "co_friend");
            b.Branch("co_listen");
            b.Narr("You sit on either side of her and say nothing for a while. Slowly her shoulders come down.");
            b.Face(h, Feel.Smile).Say(h, T(h, "...Thank you. Both of you. || ...Thank you both. || ...You two are so kind. || ...Thanks, both of you.", "...Okay. That helped. Weirdly. || ...That helped! Weirdly! || ...Okay! Better! || ...Thanks, guys!", "...Thanks. || ...Thanks. || ...Appreciated. || ...Thank you.", "...You two are sappy. ...Thanks. || ...Sappy. Thanks. || ...Gross. Thanks. || ...So cheesy. Thanks."));
            b.End();
            b.Branch("co_laugh");
            var ok = GirlInfo.ToneOf(h) == Tone.Energetic || GirlInfo.ToneOf(h) == Tone.Teasing || Random.value < 0.5f;
            if (ok) b.Face(h, Feel.Laugh).Say(h, T(h, "Pfft... ahaha! Stop it, that's so stupid! || Ahaha! Stop, so stupid! || Pfft! That's silly! || Hehe! Stop it!", "AHAHA! What even was that?! || AHAHA! What was that?! || Hahaha! Dumb! || LOL! Again!", "...Heh. Idiot. || ...Heh. Idiot. || ...Pfft. || ...Stupid. Funny.", "Hah! Okay, okay, you win. || Okay, you win. || Hah! Fine. || Alright, funny."));
            else b.Face(h, Feel.Pout).Say(h, T(h, "...I'm not in the mood. || ...Not in the mood. || ...Not now. || ...Please, no.", "...Not now, okay? || ...Not now! || ...Nope. || ...Later.", "...No. || ...No. || ...Stop. || ...Not now.", "...Read the room. || ...Read the room. || ...Bad timing. || ...Not funny."));
            b.End();
            b.Branch("co_advice");
            b.Say(h, T(h, "...Mm. You might be right. || ...You might be right. || ...Maybe... || ...That makes sense.", "...Yeah. Maybe. Thanks. || ...Maybe. Thanks. || ...Yeah. Thanks! || ...True!", "...Logical. || ...Logical. || ...Reasonable. || ...Sound.", "...Ugh. Annoyingly wise. || ...Annoyingly wise. || ...Smart. Ugh. || ...Fine. Wise."));
            b.End();
            b.Branch("co_friend");
            b.Say(f, T(f, "...Okay. I've got her. || ...I've got her. || ...I'll take care of her. || ...Leave it to me.", "Leave it to me! || On it! || Leave it to me! || Got it!", "Understood. || Understood. || Handled. || On it.", "Fine, fine. I've got this. || I've got this. || Fine. Mine now. || I'll handle it."));
            b.End();
            result = c =>
            {
                var gs = S.Girl(K(h));
                gs.NeedSocial = Mathf.Min(100, gs.NeedSocial + 30);
                switch (c)
                {
                    case "co_listen": G.AddFavor(h, 4, 0); Social.AddTrust(h, 6); Arcs.AddBond(K(f), K(h), 4); Memories.Add(h, "comfort", "you sat with me when I was down", 1); if (gs.Mood == Mood.Upset || gs.Mood == Mood.Lonely) gs.Mood = Mood.None; break;
                    case "co_laugh": if (ok) { G.AddFavor(h, 3, 0); gs.Mood = Mood.Cheerful; } break;
                    case "co_advice": Social.AddTrust(h, 3); break;
                    case "co_friend": Arcs.AddBond(K(f), K(h), 6); Pairs.Nudge(K(f), K(h), 3, 4); break;
                }
                G.AddFavor(f, 2, 0);
            };
        }

        public override string Offscreen(Situation s)
        {
            Arcs.AddBond(K(s.A), K(s.B), 5);
            Pairs.Nudge(K(s.A), K(s.B), 2, 4);
            return N(s.A) + " stayed with " + N(s.B) + " all lunch after her " + s.Why + ".";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Lados: a public quarrel where the class watches and you must pick a side (or find a third way).</summary>
    internal sealed class Sides : Mold
    {
        public override string Id { get { return "sides"; } }
        public override bool Big { get { return true; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var bond = Social.Bond(x[0], x[1]);
                var both = x[0].favor >= 40 && x[1].favor >= 40;
                if (bond <= -10 && both)
                {
                    var w = girls.FirstOrDefault(g => g != x[0] && g != x[1]);
                    yield return new Situation { A = x[0], B = x[1], W = w, Why = "who's right", Weight = 0.8f };
                }
                if (x[0].isGirlfriend && x[1].isGirlfriend && !Harem.Agreed(x[0], x[1]))
                    yield return new Situation { A = x[0], B = x[1], Why = "your girlfriends", Weight = 1.6f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var gfs = s.Why == "your girlfriends";
            intro.Say(a, T(a, "There you are. We need you to decide something. || There you are. Help us decide. || We need you to choose something. || Oh, good. Decide for us?", "YOU. Come here. Now. || YOU! Here! Now! || Come here! || Get over here!", "We need a decision. || A decision is needed. || Decide. || Choose.", "Ah, the man of the hour. || The man of the hour. || Ah, our judge. || Perfect. Decide."));
            intro.Say(o, T(o, "In front of everyone, apparently... || In front of everyone... || Publicly, apparently... || Everyone's watching...", "Yeah! Decide! || Decide! || Choose! || Pick!", "Publicly. || Publicly. || Openly. || Now.", "Choose wisely~ || Choose wisely~ || Careful~ || No pressure~"));
            b.Narr("A few students slow down to watch. " + (s.W != null ? N(s.W) + " pretends not to listen, very badly." : "This is going to be all over school by tomorrow."));
            if (gfs)
            {
                b.Face(a, Feel.Pout).Say(a, T(a, "You promised to walk me home today. || You promised to walk me home. || You said you'd walk with me today... || Our walk home... you promised.", "You said you'd walk ME home today! || You said you'd walk ME! || Me! You promised me! || My walk! Mine!", "You're walking me home. As agreed. || You're walking me. Agreed. || As promised. Me. || Our agreement. Me.", "You're walking me home. Obviously. || Walking me home. Obviously. || Me. Duh. || You're mine this evening."));
                b.Face(o, Feel.Pout).Say(o, T(o, "But you promised me too... || But you promised me too... || You said the same to me... || But... me too...", "Nuh-uh! He promised ME! || Nuh-uh! ME! || No! He promised me! || Mine first!", "He agreed to walk me home first. || He agreed with me first. || I was first. || My agreement predates yours.", "Funny. He told me the same thing. || He told me the same thing. || Funny. Same promise. || Oh? Me too."));
            }
            else
            {
                b.Face(a, Feel.Serious).Say(a, T(a, "The class trip should go to the aquarium. It's obvious. || The aquarium for the trip. Obviously. || The aquarium... it'd be lovely. || I think the aquarium is best.", "The class trip HAS to be the amusement park! || Amusement park! Has to be! || Theme park! Rides! || Roller coasters!", "The trip should be educational. The museum. || Museum. Educational. || The museum. Logical. || Educational. Museum.", "The trip should be fun. Unlike her ideas. || Fun. Unlike her ideas. || Somewhere fun. Not hers. || Fun place. Hers is boring."));
                b.Face(o, Feel.Serious).Say(o, T(o, "No, it should be the hot spring town... || No, the hot springs... || The hot spring town... || I'd prefer the onsen...", "No way! The beach! || The beach! || No! Beach! || Ocean!", "Incorrect. || Incorrect. || Wrong. || No.", "Wrong, as usual. || Wrong. As usual. || Typical. Wrong. || Nope. Wrong again."));
            }
            var opts = new List<string> { "Side with " + N(a), "sd_a", "Side with " + N(o), "sd_b" };
            if (G.Int >= 45 || G.Sens >= 45) { opts.Add(gfs ? "Walk them both home [SEN]" : "Suggest a plan with both [INT]"); opts.Add("sd_both"); }
            opts.Add("Refuse to choose"); opts.Add("sd_none");
            b.Choice(opts.ToArray());
            b.Branch("sd_a"); b.Face(a, Feel.Smile).Say(a, T(a, "...Thank you! || ...Thank you! || ...Yay! || ...I'm glad!", "YES! Ha! || YES! || Ha! Win! || Woo!", "As expected. || As expected. || Naturally. || Correct.", "Good boy. || Good boy. || Smart choice. || Obviously.")); b.Face(o, Feel.Sad).Say(o, T(o, "...Okay. || ...Okay. || ...I see. || ...Fine.", "Hmph! || Hmph! || Boo! || Ugh!", "...I see. || ...I see. || ...Noted. || ...Hm.", "...Remember this. || ...Remember this. || ...Noted. || ...You'll regret it.")); b.End();
            b.Branch("sd_b"); b.Face(o, Feel.Smile).Say(o, T(o, "...Thank you! || ...Thank you! || ...Yay! || ...I'm glad!", "YES! Ha! || YES! || Ha! Win! || Woo!", "As expected. || As expected. || Naturally. || Correct.", "Good boy. || Good boy. || Smart choice. || Obviously.")); b.Face(a, Feel.Sad).Say(a, T(a, "...Okay. || ...Okay. || ...I see. || ...Fine.", "Hmph! || Hmph! || Boo! || Ugh!", "...I see. || ...I see. || ...Noted. || ...Hm.", "...Remember this. || ...Remember this. || ...Noted. || ...You'll regret it.")); b.End();
            b.Branch("sd_both");
            b.Narr(gfs ? "You offer each of them an arm. After a second of shock, both of them take one." : "You lay out a plan that takes the best of both. The watching students actually nod.");
            b.Say(a, T(a, "...That's... actually nice. || ...That's nice. || ...Oh, that's sweet. || ...That works.", "...Huh. Okay. That works! || ...Huh! Works! || ...Okay! Cool! || ...Nice!", "...Acceptable. || ...Acceptable. || ...Fine. || ...Reasonable.", "...Smooth. Too smooth. || ...Too smooth. || ...Slick. || ...Smooth operator."));
            b.Say(o, T(o, "...Mm. Fine. || ...Fine. || ...Okay. || ...Alright.", "...Okay, fine! Yeah! || ...Fine! Yeah! || ...Okay! || ...Sure!", "...Agreed. || ...Agreed. || ...Fine. || ...Okay.", "...Hmph. Fine. || ...Hmph. Fine. || ...Fine. || ...Okay, then."));
            b.End();
            b.Branch("sd_none"); b.Say(a, T(a, "...You're no help. || ...No help. || ...You're no use. || ...Hmph.", "Ugh! Useless! || Useless! || Ugh! || Come on!", "...Indecisive. || ...Indecisive. || ...Weak. || ...Hm.", "Coward~ || Coward~ || Chicken~ || Spineless~")); b.End();
            result = c =>
            {
                var wit = s.W != null ? new[] { s.W } : new SaveData.Heroine[0];
                switch (c)
                {
                    case "sd_a": G.AddFavor(a, 5, 0); G.AddFavor(o, -4, 0); Social.Add(Social.Kinds.Defended, a, wit); if (gfs) Harem.OnAttention(a); break;
                    case "sd_b": G.AddFavor(o, 5, 0); G.AddFavor(a, -4, 0); Social.Add(Social.Kinds.Defended, o, wit); if (gfs) Harem.OnAttention(o); break;
                    case "sd_both": G.AddFavor(a, 3, 0); G.AddFavor(o, 3, 0); Arcs.AddBond(K(a), K(o), 6); if (gfs) Harem.Hint(a, o); break;
                    case "sd_none": G.AddFavor(a, -1, 0); G.AddFavor(o, -1, 0); break;
                }
                Rumors.Start("sides", K(a), K(o), s.W != null ? K(s.W) : "", 30);
            };
        }

        public override string Offscreen(Situation s)
        {
            Arcs.AddBond(K(s.A), K(s.B), -3);
            return N(s.A) + " and " + N(s.B) + " argued in front of the class (" + s.Why + ").";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Negociação: the last part of the harem talk, the three of you together (Harem has the first two).</summary>
    internal sealed class Negotiation : Mold
    {
        public override string Id { get { return "negotiation"; } }
        public override bool Big { get { return true; } }
        public override bool IgnoresCooldown { get { return true; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            if (!Cfg.StealAndHarem.Value) yield break;
            foreach (var x in PairsOf(girls))
                if (Harem.ReadyToMeet(x[0], x[1])) yield return new Situation { A = x[0], B = x[1], Why = "harem", Weight = 6f };
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            Harem.WriteMeeting(s.A, s.B, intro, b, out result);
        }

        public override string Offscreen(Situation s) { return ""; } // only with you
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Reencontro: an ex-couple, or two friends who drifted apart, meet again with you there.</summary>
    internal sealed class Reunion : Mold
    {
        public override string Id { get { return "reunion"; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var p = S.FindPair(K(x[0]), K(x[1]));
                if (p == null) continue;
                if (p.Stage == PairStage.Broken && S.Day - p.StageDay >= 4) yield return new Situation { A = x[0], B = x[1], Why = "exes", Weight = 1.2f };
                else if (p.Arc == PairArc.Drifting || p.Arc == PairArc.Reconcile) yield return new Situation { A = x[0], B = x[1], Why = "drifted apart", Weight = 1f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var a = s.A; var o = s.B;
            var exes = s.Why == "exes";
            intro.Narr(N(a) + " and " + N(o) + " stop dead when they see each other, then both look at you as if you could save them.");
            b.Say(a, T(a, "...Hi, " + N(o) + ". It's been a while. || ...Hi, " + N(o) + ". It's been long. || ..." + N(o) + "... hello. || ...It's been a while, " + N(o) + ".", "...Oh. Hey. ...Hi. || ...Oh. Hey. || ...Hi! Um. || ...Hey, " + N(o) + ".", "...Hello. || ...Hello. || ..." + N(o) + ". || ...Hi.", "...Well. Look who it is. || ...Look who it is. || ...Well, well. || ...Oh. You."));
            b.Say(o, T(o, "...Yeah. It has. || ...Yes. It has. || ...Long time. || ...It has.", "...H-hey! || ...H-hey! || ...Hi! || ...Yeah!", "...Hi. || ...Hi. || ...Yes. || ...Hello.", "...Hey yourself. || ...Hey yourself. || ...Hi. || ...Yeah, hi."));
            b.Narr(exes ? "Neither of them knows where to look. There's a lot they never said." : "They used to be inseparable. Now they stand an arm's length apart.");
            b.Choice("Help them start talking", "re_help", "Remind them of a good memory", "re_memory", "Leave them alone", "re_leave");
            b.Branch("re_help");
            b.Say(a, T(a, "...I've missed you, you know. || ...I missed you. || ...I've missed you so much. || ...I missed this.", "...Honestly? I missed you. A lot. || ...I missed you! A lot! || ...Honestly? Missed you! || ...So much!", "...I missed this. || ...I missed this. || ...I missed you. || ...Missed.", "...Fine. I missed you. Don't make it weird. || ...Missed you. Don't make it weird. || ...Fine. Missed you. || ...Okay. I missed you."));
            b.Say(o, T(o, "...Me too. || ...Me too. || ...Same. || ...I did too.", "...ME TOO, you dummy! || ...ME TOO, dummy! || ...Me too! || ...Same!!", "...Likewise. || ...Likewise. || ...Same. || ...Me too.", "...Yeah. Me too. || ...Yeah. Me too. || ...Same. || ...Me too, idiot."));
            b.End();
            b.Branch("re_memory");
            b.Narr("You bring up the time they " + (exes ? "skipped class to see the cherry blossoms" : "got locked in the art room together") + ". Both of them try not to smile, and fail.");
            b.End();
            b.Branch("re_leave");
            b.Narr("You slip away. When you glance back, they're still standing there, but they're talking.");
            b.End();
            result = c =>
            {
                var p = P(s);
                var d = c == "re_help" ? 15 : c == "re_memory" ? 10 : 5;
                p.Trust = PairState.Clamp(p.Trust + d);
                Arcs.AddBond(K(a), K(o), d / 2);
                if (exes) { p.AddRomance(K(a), d / 3); p.AddRomance(K(o), d / 3); Pairs.Advance(p, true); }
                if (p.Arc == PairArc.Drifting || p.Arc == PairArc.Reconcile) BondsInMotion.Resolve(p, true);
                if (c != "re_leave") { G.AddFavor(a, 2, 0); G.AddFavor(o, 2, 0); }
            };
        }

        public override string Offscreen(Situation s)
        {
            var p = P(s);
            p.Trust = PairState.Clamp(p.Trust + 5);
            return N(s.A) + " and " + N(s.B) + " were seen talking for the first time in a while.";
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Descoberta: a secret comes out (her crush, a love letter, a hobby she hid).</summary>
    internal sealed class Discovery : Mold
    {
        public override string Id { get { return "discovery"; } }

        public override IEnumerable<Situation> Detect(List<SaveData.Heroine> girls)
        {
            foreach (var x in PairsOf(girls))
            {
                var p = S.FindPair(K(x[0]), K(x[1]));
                if (p == null || p.Stage != PairStage.Crush) continue;
                var secret = p.RomanceOf(K(x[0])) >= p.RomanceOf(K(x[1])) ? x[0] : x[1];
                var other = secret == x[0] ? x[1] : x[0];
                if (p.RomanceOf(K(secret)) < 35) continue;
                yield return new Situation { A = other, B = secret, Why = "a hidden crush", Weight = 1.1f };
            }
        }

        public override void Write(Situation s, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var finder = s.A; var secret = s.B;
            intro.Say(finder, T(finder, "Um... I found something. I don't know what to do... || I found something... what should I do? || Um... I found this... help? || I don't know what to do...", "Okay, I found something and I'm FREAKING OUT. || Found something! FREAKING OUT! || Help! I found a thing! || Emergency! Look!", "I need advice. Quietly. || Advice. Quietly. || I need counsel. || Discreetly. Help.", "Okay, this is big. Come here. || This is big. Come here. || Big news. Here. || Look at this."));
            b.Narr(N(finder) + " holds a folded note. Your name isn't on it. Hers is, in " + N(secret) + "'s neat handwriting, with a small heart.");
            b.Face(finder, Feel.Surprised).Say(finder, T(finder, "Does " + N(secret) + "... like me? Like THAT?" + " || " + N(secret) + "... likes me? Like that? || Is this from " + N(secret) + "...? For me?" + " || " + N(secret) + " wrote this... to me?", "Is this... a love letter? For ME?! From " + N(secret) + "?! || A love letter?! From " + N(secret) + "?! || For ME?! " + N(secret) + "?!" + " || " + N(secret) + " likes me?!", "This appears to be a confession. Addressed to me. || A confession. To me. || Addressed to me. From " + N(secret) + "." + " || " + N(secret) + ". A confession.", "So... " + N(secret) + " has a crush on me. Apparently." + " || " + N(secret) + " has a crush on me. || So " + N(secret) + " likes me." + " || " + N(secret) + "? Interesting."));
            b.Face(secret, Feel.Shy).Say(secret, T(secret, "W-wait! Where did you find that?! || Where did you find that?! || Wait! Give it back! || No! Where...?!", "NO NO NO give that back! || NO! Give it back! || NOOO! Mine! || Return it!", "...That wasn't meant to be found. || ...Not meant to be found. || ...That was private. || ...Hidden for a reason.", "...Oh, come on. || ...Oh, come on. || ...Seriously? || ...Great."));
            b.Choice("Tell " + N(finder) + " to answer her honestly", "dc_honest", "Help " + N(secret) + " take it back", "dc_cover", "Say it's sweet", "dc_sweet");
            b.Branch("dc_honest");
            var yes = Pairs.GirlsOk(finder) && Pairs.Of(finder, secret).RomanceOf(K(finder)) >= 25;
            b.Say(finder, yes ? T(finder, "...I don't hate it. Can we... take it slow? || ...I don't hate it. Slowly? || ...Maybe... slowly? || ...Let's go slow?", "...Okay, honestly? I'm kinda happy?! || ...Honestly? Happy! || ...I'm kinda thrilled! || ...Okay! Yes!", "...I'm not opposed. || ...Not opposed. || ...Open to it. || ...Possible.", "...Heh. Okay. Maybe. || ...Okay. Maybe. || ...Heh. Sure. || ...Maybe.") : T(finder, "...I'm sorry. I don't feel that way. But I'm glad you told me... kind of. || ...Sorry. Not that way. Glad you told me. || ...I can't return it... sorry. || ...I'm sorry... friends?", "...I'm really sorry. You're my friend, though! Always! || ...Sorry! Friends always! || ...I'm sorry! Still pals! || ...Friends forever, okay?!", "...I don't share those feelings. I'm sorry. || ...I don't share it. Sorry. || ...Not mutual. Sorry. || ...No. Apologies.", "...Sorry. Not like that. Still friends? || ...Sorry. Friends? || ...Not like that. Friends. || ...No. Still friends."));
            b.End();
            b.Branch("dc_cover");
            b.Narr("You snatch the note and declare it's yours, a practice letter for a story. Nobody believes you. Everyone pretends to.");
            b.Face(secret, Feel.Love).Say(secret, T(secret, "...Thank you. || ...Thank you. || ...Thanks. || ...I'm grateful.", "...You're a lifesaver. || ...Lifesaver! || ...Thanks! || ...You rock!", "...I owe you. || ...I owe you. || ...Thanks. || ...Indebted.", "...You idiot. ...Thanks. || ...Idiot. Thanks. || ...Dummy. Thanks. || ...Thanks."));
            b.End();
            b.Branch("dc_sweet");
            b.Face(secret, Feel.Shy).Say(secret, T(secret, "...D-don't say that... || ...Don't say that... || ...S-stop... || ...Please, no...", "Stoooop! || Stoooop! || Stop it! || No!", "...Please stop. || ...Stop. || ...Enough. || ...Please stop.", "...I'll get you for this. || ...I'll get you. || ...Payback later. || ...You'll pay."));
            b.End();
            result = c =>
            {
                var p = Pairs.Of(finder, secret);
                switch (c)
                {
                    case "dc_honest": if (yes) { p.AddRomance(K(finder), 20); p.Stage = PairStage.Talking; p.StageDay = S.Day; } else { p.AddRomance(K(secret), -15); Arcs.AddBond(K(finder), K(secret), 2); } break;
                    case "dc_cover": Social.AddTrust(secret, 8); G.AddFavor(secret, 4, 0); Memories.Add(secret, "secret", "you covered for me with that letter", 1); break;
                    case "dc_sweet": p.AddRomance(K(finder), 6); p.Exposure = PairState.Clamp(p.Exposure + 20); break;
                }
                Pairs.Advance(p);
            };
        }

        public override string Offscreen(Situation s)
        {
            var p = P(s);
            p.Exposure = PairState.Clamp(p.Exposure + 20);
            Rumors.Start("crush", K(s.B), K(s.A), "", 35);
            return "";
        }
    }
}
