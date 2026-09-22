using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Rumor chains: what happens at school (a breakup, two girls seen together, a crush, a public quarrel) becomes a
    /// rumor that travels from girl to girl along friendships, and changes on the way: some hear what happened, some a
    /// twisted version, friends of the girl it hurt hear her side. It has heat that cools by itself. The director follows
    /// one chain at a time: a girl tells you (you hear it), someone who was there tells you what really happened (a
    /// witness), you meet a twisted version and can correct it, and you settle it with the girl it is about. Confirming,
    /// denying or ignoring it all leave a mark.
    /// </summary>
    internal static class Rumors
    {
        private static CampusState S { get { return Campus.S; } }

        public static IEnumerable<Scandal> Open { get { return S.Scandals.Where(s => !s.Closed); } }
        public static Scandal Main { get { return Open.FirstOrDefault(s => s.Main); } }

        // ================= starting and finding =================

        public static Scandal Start(string kind, string subject, string other, string teller, int heat)
        {
            if (!Cfg.RumorChains.Value) return null;
            var same = Open.FirstOrDefault(s => s.Kind == kind && s.Subject == subject && s.Other == other);
            if (same != null) { same.Heat = Mathf.Min(100, Mathf.Max(same.Heat, heat) + 10); return same; }
            var sc = new Scandal { Id = S.NextScandalId++, Day = S.Day, Kind = kind, Subject = subject ?? "", Other = other ?? "", Teller = teller ?? "", Heat = Mathf.Clamp(heat, 10, 100) };
            // Who knows from the start: the teller (her side if it is about her) and the girls it is about.
            if (!string.IsNullOrEmpty(sc.Teller)) sc.Versions[sc.Teller] = sc.Teller == sc.Subject && (kind == "stole" || kind == "cheated") ? 2 : 0;
            if (!string.IsNullOrEmpty(sc.Other) && !sc.Versions.ContainsKey(sc.Other)) sc.Versions[sc.Other] = 0;
            S.Scandals.Add(sc);
            try { Lore.FromRumor(kind, sc.Subject, sc.Other, sc.Teller); } catch (Exception e) { CampusLifePlugin.Log.LogDebug("Lore rumor: " + e.Message); }
            if (Main == null && sc.Heat >= 30) sc.Main = true;
            if (S.Scandals.Count > 20) S.Scandals.RemoveAll(x => x.Closed && S.Day - x.Day > 14);
            CampusLifePlugin.Log.LogInfo("Rumor #" + sc.Id + " started: " + What(sc, 0) + " (heat " + sc.Heat + (sc.Main ? ", main" : "") + ")");
            return sc;
        }

        public static int HeatAbout(string key)
        {
            var l = Open.Where(s => s.Subject == key).Select(s => s.Heat).ToList();
            return l.Count == 0 ? 0 : l.Max();
        }

        public static Scandal Hottest() { return Open.Where(s => s.Heat >= 30).OrderByDescending(s => s.Heat).FirstOrDefault(); }

        /// <summary>"rumor 12" or "12".</summary>
        public static Scandal ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int n;
            var t = id.StartsWith("rumor ", StringComparison.Ordinal) ? id.Substring(6) : id;
            return int.TryParse(t, out n) ? S.Scandals.FirstOrDefault(s => s.Id == n) : null;
        }

        private static string N(string key) { return string.IsNullOrEmpty(key) ? "someone" : GirlInfo.FirstName(key); }

        /// <summary>The rumor told one way: 0 what happened, 1 twisted, 2 the side of the girl it hurt.</summary>
        public static string What(Scandal s, int version)
        {
            var a = N(s.Subject); var o = N(s.Other); var t = N(s.Teller);
            switch (s.Kind)
            {
                case "stole": return version == 0 ? o + " left " + a + " for you" : version == 1 ? "you were two-timing " + a + " and " + o + " for weeks" : "you stole " + o + " right out from under " + a;
                case "cheated": return version == 0 ? a + " got close to you while dating " + t : version == 1 ? a + " has a secret boyfriend in another school" : a + " is playing with " + t + "'s feelings";
                case "caught": return version == 0 ? a + " and " + o + " were seen together behind the gym" : version == 1 ? a + " and " + o + " are secretly engaged" : o + " is trying to steal " + a;
                case "crush": return version == 0 ? a + " has a crush on " + o : version == 1 ? a + " wrote " + o + " a ten-page love poem" : a + " won't leave " + o + " alone";
                case "sides": return version == 0 ? a + " and " + o + " fought over you in the hallway" : version == 1 ? a + " slapped " + o + " in front of everyone" : o + " started the fight with " + a;
                case "seen_with": return version == 0 ? "you and " + a + " were seen together after school" : version == 1 ? "you and " + a + " are dating in secret" : "you're just using " + a;
                case "council": return version == 0 ? a + " is running for student council" : version == 1 ? a + " is bribing people with sweets for votes" : a + " only wants the council room for herself";
                default: return version == 0 ? "something happened with " + a : "something scandalous happened with " + a;
            }
        }

        /// <summary>The version this girl heard (what happened if she was not told).</summary>
        public static string VersionFor(Scandal s, SaveData.Heroine h)
        {
            int v;
            return What(s, h != null && s.Versions.TryGetValue(GirlInfo.Key(h), out v) ? v : 0);
        }

        public static void Heat(Scandal s, int d) { s.Heat = Mathf.Clamp(s.Heat + d, 0, 100); }
        public static void Cool(Scandal s, int d) { s.Heat = Mathf.Clamp(s.Heat - d, 0, 100); if (s.Heat <= 5) Close(s, "faded"); }

        /// <summary>You asked who started it: the teller is known.</summary>
        public static void Trace(Scandal s) { s.Step = Mathf.Max(s.Step, 2); }

        public static void Close(Scandal s, string outcome)
        {
            if (s.Closed) return;
            s.Closed = true;
            s.Main = false;
            s.Outcome = outcome;
            CampusLifePlugin.Log.LogInfo("Rumor #" + s.Id + " closed: " + outcome);
            if (outcome == "cleared") Anthology.Add("rumor", "You cleared up the rumor that " + What(s, 1), 2, s.Subject);
            var next = Hottest();
            if (next != null && Main == null) next.Main = true;
        }

        // ================= spreading =================

        /// <summary>Each period: girls who know tell a friend; versions drift; heat cools a little.</summary>
        public static void PeriodTick()
        {
            if (!Cfg.RumorChains.Value) return;
            foreach (var s in Open.ToList())
            {
                foreach (var kv in s.Versions.ToList())
                {
                    if (Random.value > s.Heat / 120f) continue;
                    var teller = Social.Get(kv.Key);
                    if (teller == null) continue;
                    var friend = Social.FriendsOf(teller, 30).FirstOrDefault(f => !s.Versions.ContainsKey(GirlInfo.Key(f)));
                    if (friend == null) continue;
                    var v = kv.Value;
                    // Her friends hear her side; anyone may hear it twisted.
                    var subj = Social.Get(s.Subject);
                    if (subj != null && Social.Bond(friend, subj) >= 40 && (s.Kind == "stole" || s.Kind == "sides")) v = 2;
                    else if (v == 0 && Random.value < 0.25f) v = 1;
                    s.Versions[GirlInfo.Key(friend)] = v;
                    // A rumor about you costs a little with girls who hear the bad versions.
                    if (v > 0 && (s.Kind == "stole" || s.Kind == "seen_with")) G.AddFavor(friend, -1, 0);
                }
                s.Heat = Mathf.Max(0, s.Heat - 3);
            }
        }

        public static void Daily()
        {
            foreach (var s in Open.ToList())
            {
                s.Heat = Mathf.Max(0, s.Heat - 8);
                if (s.Heat <= 5) Close(s, "faded");
            }
            if (Main == null) { var h = Hottest(); if (h != null) h.Main = true; }
        }

        // ================= the chain, in conversations =================

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        public static IEnumerable<Subject> Subjects(SaveData.Heroine h)
        {
            if (!Cfg.RumorChains.Value) yield break;
            var s = Main;
            if (s == null) yield break;
            var k = GirlInfo.Key(h);
            int v;
            var knows = s.Versions.TryGetValue(k, out v);
            if (k == s.Subject && s.Step >= 1) yield return AboutHer(h, s);
            else if (s.Step == 0 && knows && k != s.Subject) yield return Heard(h, s, v);
            else if (s.Step == 1 && (k == s.Other || k == s.Teller) && k != s.Subject) yield return Witness(h, s);
            else if (s.Step >= 1 && knows && v > 0 && k != s.Subject) yield return Twisted(h, s, v);
        }

        /// <summary>Step 1: a girl tells you what she heard.</summary>
        private static Subject Heard(SaveData.Heroine h, Scandal s, int v)
        {
            var text = What(s, v);
            var aboutYou = s.Kind == "stole" || s.Kind == "seen_with" || s.Kind == "sides";
            return new Subject
            {
                Id = "rumor_heard", Weight = 1.2f + s.Heat / 50f,
                Write = b =>
                {
                    b.Face(h, Feel.Surprised).Her_(T(h, "Um... is it true? People are saying " + text + "...", "Okay, spill! Everyone's saying " + text + "!", "A question. There's a rumor that " + text + ".", "So, I heard something juicy. " + text + ". True?"));
                    b.Choice(aboutYou ? "Confirm it" : "Say it might be true", "rh_yes", "Deny it", "rh_no", "Ask who told her", "rh_who", "Shrug it off", "rh_ignore");
                    b.Branch("rh_yes"); b.Face(h, Feel.Surprised).Her_(T(h, "R-really...?", "WHOA. Okay!", "...I see.", "Ha! I knew it.")); b.End();
                    b.Branch("rh_no"); b.Face(h, Feel.Normal).Her_(T(h, "Oh... okay. I believe you.", "Huh. Okay! Good to know!", "...Noted.", "Hm. If you say so.")); b.End();
                    b.Branch("rh_who"); b.Face(h, Feel.Normal).Her_(T(h, "I heard it from " + N(s.Teller) + "... I think.", "Uh, " + N(s.Teller) + " said something, I think?", N(s.Teller) + ", originally.", "A little bird named " + N(s.Teller) + ".")); b.End();
                    b.Branch("rh_ignore"); b.Face(h, Feel.Pout).Her_(T(h, "Mm... okay. Sorry for asking.", "Aw, no fun!", "...Evasive.", "Mysterious. I like it.")); b.End();
                },
                Result = c =>
                {
                    s.Step = Mathf.Max(s.Step, 1);
                    switch (c)
                    {
                        case "rh_yes": Heat(s, 10); s.Versions[GirlInfo.Key(h)] = 0; if (v == 0) Social.AddTrust(h, 2); break;
                        case "rh_no": if (v == 0 && s.Kind != "council") { Social.AddTrust(h, -3); } else Cool(s, 10); break;
                        case "rh_who": Trace(s); break;
                        case "rh_ignore": Cool(s, 4); break;
                    }
                    Memories.Add(h, "rumor", "I asked you about the rumor that " + What(s, v), 0, false);
                },
                Interest = 1,
            };
        }

        /// <summary>Step 2: someone who was there tells you what really happened.</summary>
        private static Subject Witness(SaveData.Heroine h, Scandal s)
        {
            return new Subject
            {
                Id = "rumor_witness", Weight = 2f,
                Write = b =>
                {
                    b.Narr(GirlInfo.FirstName(h) + " lowers her voice.");
                    b.Face(h, Feel.Serious).Her_(T(h, "About that rumor... I was there. What really happened is: " + What(s, 0) + ". That's all.", "Okay, real talk, I was THERE. It's just: " + What(s, 0) + ". People are exaggerating!",
                        "I was present. The facts: " + What(s, 0) + ". Nothing more.", "Want the real story? " + What(s, 0) + ". Boring, right?"));
                    b.Choice("Thank her for the truth", "rw_thanks", "Ask her to help set it straight", "rw_help");
                    b.Branch("rw_thanks"); b.Face(h, Feel.Smile).Her_(T(h, "Of course.", "No prob!", "...You're welcome.", "You owe me.")); b.End();
                    b.Branch("rw_help"); b.Face(h, Feel.Normal).Her_(T(h, "I'll try... I'll tell anyone who asks.", "You got it! I'll spread the TRUTH!", "I'll correct people. Efficiently.", "Fine. I'll be the voice of reason. For once.")); b.End();
                },
                Result = c =>
                {
                    s.Step = Mathf.Max(s.Step, 2);
                    if (c == "rw_help") { foreach (var k in s.Versions.Keys.ToList()) if (Random.value < 0.4f) s.Versions[k] = 0; Cool(s, 12); }
                    Social.AddTrust(h, 3);
                },
                Interest = 1,
            };
        }

        /// <summary>Step 3: a girl repeats a twisted version; you can correct it.</summary>
        private static Subject Twisted(SaveData.Heroine h, Scandal s, int v)
        {
            return new Subject
            {
                Id = "rumor_twisted", Weight = 1.2f,
                Write = b =>
                {
                    b.Face(h, Feel.Surprised).Her_(T(h, "Did you hear? " + What(s, v) + "! Isn't that awful?", "Oh my gosh, " + What(s, v) + "! Crazy, right?!", "Apparently " + What(s, v) + ". Disappointing.", "Heard the latest? " + What(s, v) + ". Wild."));
                    var opts = new List<string>();
                    if (s.Step >= 2) { opts.Add("Tell her what really happened"); opts.Add("tw_fix"); }
                    opts.Add("Laugh it off"); opts.Add("tw_laugh");
                    opts.Add("Let it go"); opts.Add("tw_go");
                    b.Choice(opts.ToArray());
                    b.Branch("tw_fix"); b.Face(h, Feel.Normal).Her_(T(h, "Oh... that's not as bad. I'm glad I asked you.", "Wait, that's it? Huh! Way less exciting!", "...I was misinformed. Thank you.", "Boo. The truth is boring.")); b.End();
                    b.Branch("tw_laugh"); b.Face(h, Feel.Laugh).Her_(T(h, "Ehehe...", "Ahaha! Right?!", "...Hm.", "Heh.")); b.End();
                    b.Branch("tw_go"); b.Face(h, Feel.Normal).Her_(T(h, "...Mm.", "Okay, okay.", "...Fine.", "Suit yourself.")); b.End();
                },
                Result = c =>
                {
                    s.Step = Mathf.Max(s.Step, 3);
                    if (c == "tw_fix") { s.Versions[GirlInfo.Key(h)] = 0; Cool(s, 10); Social.AddTrust(h, 2); }
                    else if (c == "tw_laugh") Heat(s, 5);
                },
                Interest = 0,
            };
        }

        /// <summary>Step 4: the girl it is about. Settling it closes the chain.</summary>
        private static Subject AboutHer(SaveData.Heroine h, Scandal s)
        {
            return new Subject
            {
                Id = "rumor_settle", Weight = 1.5f + s.Heat / 40f,
                Write = b =>
                {
                    b.Narr(GirlInfo.FirstName(h) + " looks like she hasn't slept well.");
                    b.Face(h, Feel.Sad).Her_(T(h, "You've heard what they're saying about me, haven't you...?", "Everybody's whispering about me! You heard it too, right?!", "The rumor. You've heard it.", "So. You've heard the rumor too. Everyone has."));
                    var opts = new List<string> { "Say you believe her", "rs_believe", "Ask for her side of it", "rs_side" };
                    if (s.Step >= 2) { opts.Add("Promise to set it straight"); opts.Add("rs_fix"); }
                    opts.Add("Tell her to ignore it"); opts.Add("rs_ignore");
                    b.Choice(opts.ToArray());
                    b.Branch("rs_believe"); b.Face(h, Feel.Love).Her_(T(h, "...Thank you. That means more than you know.", "...You do? ...Thank you. Seriously.", "...Thank you.", "...Heh. You're too nice. ...Thanks.")); b.End();
                    b.Branch("rs_side"); b.Face(h, Feel.Normal).Her_(T(h, "...It's simple, really. " + What(s, 0) + ". That's all it was.", "It's just " + What(s, 0) + "! That's IT!", "The truth: " + What(s, 0) + ".", "The truth? " + What(s, 0) + ". Disappointed?")); b.End();
                    b.Branch("rs_fix"); b.Face(h, Feel.Smile).Her_(T(h, "You'd do that...? Thank you...", "You'd do that for me?! You're the best!", "...I'd appreciate it.", "...My hero. Kinda.")); b.End();
                    b.Branch("rs_ignore"); b.Face(h, Feel.Sad).Her_(T(h, "...Easy for you to say.", "Ugh, I'm TRYING!", "...If only.", "...Thanks for nothing.")); b.End();
                },
                Result = c =>
                {
                    switch (c)
                    {
                        case "rs_believe": Social.AddTrust(h, 6); G.AddFavor(h, 3, 0); Cool(s, 10); Pairs.OnHelped(h); break;
                        case "rs_side": Social.AddTrust(h, 4); s.Step = Mathf.Max(s.Step, 2); break;
                        case "rs_fix": Social.AddTrust(h, 8); G.AddFavor(h, 4, 0); Social.Add(Social.Kinds.Defended, h); Pairs.OnHelped(h); Close(s, "cleared"); break;
                        case "rs_ignore": Social.AddTrust(h, -2); break;
                    }
                    Memories.Add(h, "rumor", c == "rs_fix" ? "you set the record straight for me" : "you talked to me about that rumor", c == "rs_ignore" ? -1 : 1);
                },
                Interest = 1,
            };
        }
    }
}
