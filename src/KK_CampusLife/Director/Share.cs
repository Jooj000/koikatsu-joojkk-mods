using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// "elas tem que lembrar de tudo, não ficar repetindo as mesmas perguntas"). The first time she learns of you and her
    /// (she saw it, or heard it), she makes up her mind from both personalities, their bond and her tastes:
    /// she refuses (angry; you hide it and apologize until she comes around, or she breaks up and remembers), she lets
    /// you see her separately, she accepts and they become friends, or (likes girls, and lewd by nature or right now) she
    /// would rather have her too: a trio. Once she said it is fine, she never brings it up again. What she saw is "I saw",
    /// what she heard is "they told me".
    /// </summary>
    internal static class Share
    {
        public const int Unknown = 0, Refuses = 1, Separate = 2, Friends = 3, Trio = 4;

        private static CampusState S { get { return Campus.S; } }

        public static int Stance(SaveData.Heroine gf, SaveData.Heroine other)
        {
            if (gf == null || other == null) return Unknown;
            if (Harem.Agreed(gf, other)) return Math.Max(Separate, Raw(gf, other));
            return Raw(gf, other);
        }

        private static int Raw(SaveData.Heroine gf, SaveData.Heroine other)
        {
            int v;
            return S.Girl(GirlInfo.Key(gf)).Share.TryGetValue(GirlInfo.Key(other), out v) ? v : Unknown;
        }

        public static bool Accepts(SaveData.Heroine gf, SaveData.Heroine other) { return Stance(gf, other) >= Separate; }

        public static bool Said(SaveData.Heroine gf, SaveData.Heroine other) { return S.Girl(GirlInfo.Key(gf)).ShareSaid.Contains(GirlInfo.Key(other)); }

        private static void Set(SaveData.Heroine gf, SaveData.Heroine other, int stance, string why)
        {
            var gs = S.Girl(GirlInfo.Key(gf));
            gs.Share[GirlInfo.Key(other)] = stance;
            if (stance != Refuses) gs.ShareCalm.Remove(GirlInfo.Key(other));
            CampusLifePlugin.Log.LogInfo("Share: " + GirlInfo.FirstName(gf) + " about you and " + GirlInfo.FirstName(other) + " -> " + Name(stance) + " (" + why + ")");
        }

        public static string Name(int stance)
        {
            switch (stance)
            {
                case Refuses: return "refuses";
                case Separate: return "shares, separately";
                case Friends: return "shares, friends with her";
                case Trio: return "wants a trio";
                default: return "doesn't know";
            }
        }

        /// <summary>Likes girls, and lewd by nature (quirks, teasing) or right now (flirty, high H).</summary>
        private static bool Open(SaveData.Heroine h)
        {
            if (!Pairs.GirlsOk(h)) return false;
            var q = GirlInfo.Quirks(h);
            var lewd = q.bitch || q.choroi || GirlInfo.ToneOf(h) == Tone.Teasing || h.lewdness >= 60 || S.Girl(GirlInfo.Key(h)).Mood == Mood.Flirty;
            return lewd;
        }

        /// <summary>Her decision, from both personalities, how they get along and her tastes. Some simply never want it.</summary>
        private static int Decide(SaveData.Heroine gf, SaveData.Heroine other, out string why)
        {
            var comp = Harem.Compatibility(gf, other);
            var bond = Social.Bond(gf, other);
            var q = GirlInfo.Quirks(gf);
            var qo = GirlInfo.Quirks(other);
            // Never: serious by nature and not lewd, or the other one is her rival.
            if (bond <= -30) { why = "they are rivals"; return Refuses; }
            if (q.majime && !Open(gf) && comp < 50) { why = "too serious for it (compat " + comp + ")"; return Refuses; }
            if (Open(gf) && (Conversation.RomanceFor(gf, other) >= 20 || other.lewdness >= 40 || comp >= 30) && !qo.majime)
            {
                why = "likes girls and is lewd (compat " + comp + ")";
                return Trio;
            }
            var roll = comp + Random.Range(-12, 13) + (GirlInfo.ToneOf(gf) == Tone.Energetic ? 5 : 0) - (GirlInfo.ToneOf(gf) == Tone.Cool ? 5 : 0);
            if (roll >= 50) { why = "gets along with her (compat " + comp + ", bond " + bond + ")"; return bond >= 25 ? Friends : Separate; }
            if (roll >= 38 && Social.Trust(gf) >= 60) { why = "trusts you (compat " + comp + ")"; return Separate; }
            why = "won't share (compat " + comp + ")";
            return Refuses;
        }

        /// <summary>
        /// She learned about you and the other girl. Decides once and remembers. Returns what she will do: "" nothing (she
        /// already said it was fine), "ok" (tells you once it's fine), "trio" (proposes it once), "confront" (on the
        /// spot, both of you), "wait" (waits for you to come and talk).
        /// </summary>
        public static string Learned(SaveData.Heroine gf, SaveData.Heroine other, bool saw)
        {
            if (gf == null || other == null || gf == other || !gf.isGirlfriend) return "";
            var stance = Stance(gf, other);
            if (stance == Unknown)
            {
                string why;
                stance = Decide(gf, other, out why);
                Set(gf, other, stance, why);
            }
            var gs = S.Girl(GirlInfo.Key(gf));
            var tag = GirlInfo.Key(other) + (saw ? "	saw" : "");
            if (stance >= Separate)
            {
                if (Said(gf, other)) return "";
                gs.Pending = "Share:" + tag;
                return stance == Trio ? "trio" : "ok";
            }
            // She refuses: a hot temper confronts you both right there; the others wait for you to come.
            var hot = GirlInfo.ToneOf(gf) == Tone.Energetic || GirlInfo.ToneOf(gf) == Tone.Teasing || gf.isAnger;
            gs.Pending = (saw && hot ? "ShareConfront:" : "Share:") + tag;
            return saw && hot ? "confront" : "wait";
        }

        public static SaveData.Heroine PendingOther(SaveData.Heroine gf, out bool confront, out bool saw)
        {
            confront = false;
            saw = false;
            if (gf == null) return null;
            var p = S.Girl(GirlInfo.Key(gf)).Pending ?? "";
            string rest;
            if (p.StartsWith("ShareConfront:", StringComparison.Ordinal)) { confront = true; rest = p.Substring(14); }
            else if (p.StartsWith("Share:", StringComparison.Ordinal)) rest = p.Substring(6);
            else return null;
            var parts = rest.Split('	');
            saw = parts.Length > 1 && parts[1] == "saw";
            return Social.Get(parts[0]);
        }

        /// <summary>
        /// Everything she knows about you and that girl is talked about now: no gossip topic, no greeting about it later
        /// in this or the next talks (the stance remembers).
        /// </summary>
        public static void Hush(SaveData.Heroine gf, SaveData.Heroine other)
        {
            if (gf == null || other == null) return;
            var gs = S.Girl(GirlInfo.Key(gf));
            var ok = GirlInfo.Key(other);
            gs.Unmentioned.RemoveAll(id => { var i = S.Incidents.FirstOrDefault(x => x.Id == id); return i != null && i.Girl == ok && i.Kind == Social.Kinds.Flirted; });
            foreach (var kv in Lore.Of(GirlInfo.Key(gf)))
                if (kv.Value.Between(Lore.Player, ok) && (kv.Value.Tags.Contains("romance") || kv.Value.Tags.Contains("h"))) kv.Key.ToPlayer = 2;
        }

        /// <summary>A romance of yours she knows from the lore (heard, saw) that she has no stance on yet: she decides now.</summary>
        public static void FromLore(SaveData.Heroine gf)
        {
            if (gf == null || !gf.isGirlfriend) return;
            var key = GirlInfo.Key(gf);
            foreach (var kv in Lore.Of(key).ToList())
            {
                var f = kv.Value;
                if (!f.Involves(Lore.Player) || f.Involves(key) || !(f.Tags.Contains("romance") || f.Tags.Contains("h")) || S.Day - f.Day > 10) continue;
                var other = Social.Get(f.Subject == Lore.Player ? f.Obj : f.Subject);
                if (other == null || other == gf || Stance(gf, other) != Unknown) continue;
                Learned(gf, other, kv.Key.How == How.Witness);
                return;
            }
        }

        private static void Clear(SaveData.Heroine gf)
        {
            var gs = S.Girl(GirlInfo.Key(gf));
            if (gs.Pending.StartsWith("Share", StringComparison.Ordinal)) gs.Pending = "";
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        /// <summary>
        /// The talk with your girlfriend about her (alone). Writes the beat into b and returns what each choice does, or
        /// null when there is nothing to say (she said her piece already).
        /// </summary>
        public static Action<string> Write(SaveData.Heroine gf, SaveData.Heroine other, bool saw, SceneBuilder b)
        {
            if (gf == null || other == null) return null;
            var on = GirlInfo.FirstName(other);
            var stance = Stance(gf, other);
            var gs = S.Girl(GirlInfo.Key(gf));
            var key = GirlInfo.Key(other);
            if (stance >= Separate && Said(gf, other)) { Clear(gf); return null; }
            if (stance == Separate || stance == Friends)
            {
                b.Face(gf, Feel.Normal).Say(gf, saw
                    ? T(gf, "I saw you with " + on + ". ...It's okay. I've thought about it, and I'm okay with it.", "I saw you and " + on + "! And you know what? It's fine! I mean it!", "I saw you with " + on + ". I've decided I don't mind. I won't bring it up again.", "Saw you with " + on + ". Relax, I'm not mad. Just don't forget me~")
                    : T(gf, "People told me about you and " + on + ". ...It's okay. I'd rather share than lose you.", "So, " + on + ", huh? I heard! It's fine, really. Just keep some time for me!", "I heard about " + on + ". I'm not going to make a scene. It's fine.", "A little bird told me about " + on + ". Don't look so scared. I don't mind~"));
                if (stance == Friends) b.Face(gf, Feel.Smile).Say(gf, T(gf, on + " is nice, actually. Maybe we'll be friends.", "Actually, " + on + " is super fun! We're gonna be friends!", on + " and I talked. She's alright.", on + "'s cute. I'll keep an eye on her. In a friendly way~"));
                gs.ShareSaid.Add(key);
                Clear(gf);
                return c => { Social.AddTrust(gf, 2); if (stance == Friends) Arcs.AddBond(GirlInfo.Key(gf), key, 8); };
            }
            if (stance == Trio)
            {
                b.Face(gf, Feel.Shy).Say(gf, saw
                    ? T(gf, "I saw you with " + on + "... and, um, I don't want to fight about it.", "I saw you and " + on + "! And honestly? I get it. She's cute!", "I saw you with " + on + ". I have a proposal instead of a fight.", "I saw you with " + on + "~ And I'm not even mad. I'm... interested.")
                    : T(gf, "I heard about you and " + on + "... I've been thinking about her too, actually.", "Okay, so, " + on + "! I heard! And... can I say something weird?", "I heard about " + on + ". I've given it thought. Hear me out.", "So " + on + " caught your eye? She caught mine too~"));
                b.Face(gf, Feel.Shy).Say(gf, T(gf, "What if... she was with us? Both of us. Together.", "What if we ask her to join us?! Like, the three of us!", "Instead of sneaking around: the three of us. Openly.", "Why choose? Bring her. The three of us~"));
                b.Choice("Say you'd love that", "sh_trio", "Say she's the only one for you", "sh_only", "Say you need to think", "sh_think");
                b.Branch("sh_trio"); b.Face(gf, Feel.Laugh).Say(gf, T(gf, "Really? Then... I'll talk to her.", "YES! I'll go ask her!", "Good. I'll handle it.", "Leave it to me~")); b.End();
                b.Branch("sh_only"); b.Face(gf, Feel.Smile).Say(gf, T(gf, "...Silly. Okay. But the offer stands.", "Aww! Okay, okay. But think about it!", "...Noted. The offer stays open.", "Sweet. Boring, but sweet~")); b.End();
                b.Branch("sh_think"); b.Face(gf, Feel.Normal).Say(gf, T(gf, "Okay. Take your time.", "Think fast, okay?!", "Fine. Don't take forever.", "Don't think too hard~")); b.End();
                gs.ShareSaid.Add(key);
                Clear(gf);
                return c =>
                {
                    if (c == "sh_trio") ProposeTrio(gf, other);
                    else if (c == "sh_only") { G.AddFavor(gf, 4, 0); Set(gf, other, Separate, "you said she's the only one; the offer stays"); }
                    else Social.AddTrust(gf, 1);
                };
            }
            // She refuses.
            int calm;
            gs.ShareCalm.TryGetValue(key, out calm);
            var again = gs.ShareSaid.Contains(key);
            b.Face(gf, Feel.Pout).Say(gf, again
                ? T(gf, on + " again...? You promised.", "AGAIN with " + on + "?!", on + ". Again. Explain.", "Back to " + on + "? You never learn, do you?")
                : saw
                    ? T(gf, "I saw you with " + on + ". With my own eyes.", "I SAW you with " + on + "! Don't even try!", "I saw you and " + on + ". Explain.", "I saw that, you know. You and " + on + ".")
                    : T(gf, "People are saying things about you and " + on + "...", "Everyone's talking about you and " + on + "! Is it true?!", "I heard about you and " + on + ". Is it true?", "So. " + on + ". I heard everything~ ...I'm not smiling."));
            var excuse = Mathf.Clamp(0.35f + G.Int / 250f + Social.Trust(gf) / 300f - (saw ? 0.3f : 0f) - (again ? 0.15f : 0f), 0.05f, 0.85f);
            var opts = new List<string> { "Make up an excuse" + (G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(excuse * 100) + "%)</color>" : ""), "sh_excuse", "Apologize", "sh_sorry" };
            if (calm >= 50) { opts.Add("Ask her to accept it"); opts.Add("sh_accept"); }
            opts.Add("Promise it won't happen again"); opts.Add("sh_promise");
            b.Choice(opts.ToArray());
            var ok = Random.value < excuse;
            b.Branch("sh_excuse");
            b.Me(saw ? "It's not what it looked like. She tripped, I caught her." : "That's just a rumor. You know how people talk.");
            b.Face(gf, ok ? Feel.Worried : Feel.Pout).Say(gf, ok ? T(gf, "...Okay. I want to believe you.", "...Fine! I believe you! This time!", "...Fine. I'll let it go.", "Hmm. Lucky you. I'll buy it.")
                                                         : T(gf, "Do you think I'm stupid?", "LIAR! I'm not dumb!", "Don't insult me.", "Cute story. No."));
            b.End();
            b.Branch("sh_sorry");
            b.Me("I'm sorry. I didn't want to hurt you.");
            b.Face(gf, Feel.Sad).Say(gf, T(gf, "...Thank you for not lying, at least.", "...Ugh. Okay. At least you said sorry.", "...Apology noted. It still hurts.", "...Hmph. Sorry is a start."));
            b.End();
            var yes = calm + Harem.Compatibility(gf, other) / 2 + Random.Range(-10, 11) >= 70;
            if (calm >= 50)
            {
                b.Branch("sh_accept");
                b.Me("I care about you. And I care about her too. Can you accept that?");
                b.Face(gf, yes ? Feel.Worried : Feel.Pout).Say(gf, yes ? T(gf, "...I've been thinking about it a lot. ...Okay. I can live with it.", "...Ugh. FINE. I can't stay mad at you. Okay!", "...I've considered it. Fine. I accept.", "...You're lucky I like you this much. Fine.")
                                                                      : T(gf, "No. I can't. Not yet.", "No way! Not happening!", "No.", "Ask me again and see what happens."));
                b.End();
            }
            b.Branch("sh_promise");
            b.Me("It won't happen again. I promise.");
            b.Face(gf, Feel.Worried).Say(gf, T(gf, "...Okay. I'm trusting you.", "You promise?! Pinky promise?!", "...I'll remember that promise.", "Promise, huh? I'll be watching~"));
            b.End();
            gs.ShareSaid.Add(key);
            Clear(gf);
            return c => Resolve(gf, other, c, ok, yes);
        }

        private static void Resolve(SaveData.Heroine gf, SaveData.Heroine other, string c, bool excuseOk, bool accepted)
        {
            var gs = S.Girl(GirlInfo.Key(gf));
            var key = GirlInfo.Key(other);
            int calm;
            gs.ShareCalm.TryGetValue(key, out calm);
            switch (c)
            {
                case "sh_excuse":
                    if (excuseOk) { calm += 12; Social.AddTrust(gf, -1); }
                    else { calm -= 10; Social.AddTrust(gf, -8); gf.anger = Mathf.Min(100, gf.anger + 25); }
                    break;
                case "sh_sorry": calm += 18; Social.AddTrust(gf, -2); G.AddFavor(gf, -2, 0); break;
                case "sh_promise": calm += 8; Social.AddTrust(gf, 1); Memories.Add(gf, "promise", "you promised to stop seeing " + GirlInfo.FirstName(other), 0, false, key); break;
                case "sh_accept":
                    if (accepted) { Set(gf, other, Social.Bond(gf, other) >= 25 ? Friends : Separate, "you talked her into it (calm " + calm + ")"); Toasts.Show(GirlInfo.FirstName(gf) + " accepted you and " + GirlInfo.FirstName(other) + "."); return; }
                    calm -= 15; Social.AddTrust(gf, -4);
                    break;
            }
            gs.ShareCalm[key] = Mathf.Clamp(calm, 0, 100);
            // Pushed too far: she breaks up, and remembers why the next time you try.
            if (Social.Trust(gf) < 12 && gf.isGirlfriend)
            {
                gf.isGirlfriend = false;
                gf.isDate = false;
                gf.favor = Math.Min(gf.favor, 55);
                Memories.Add(gf, "breakup", "you and " + GirlInfo.FirstName(other) + " broke us up", -3, true, key);
                Toasts.Show(GirlInfo.FirstName(gf) + " broke up with you over " + GirlInfo.FirstName(other) + ".");
                CampusLifePlugin.Log.LogInfo("Share: " + GirlInfo.FirstName(gf) + " broke up over " + GirlInfo.FirstName(other) + " (trust " + Social.Trust(gf) + ")");
            }
            else CampusLifePlugin.Log.LogInfo("Share: " + GirlInfo.FirstName(gf) + " about " + GirlInfo.FirstName(other) + ": " + c + " -> calm " + gs.ShareCalm[key] + ", trust " + Social.Trust(gf));
        }

        /// <summary>
        /// Your girlfriend asks the other girl. She says yes when she likes girls and gets along with her (or is lewd),
        /// becomes a friend when she likes you both but not that way, or says no.
        /// </summary>
        private static void ProposeTrio(SaveData.Heroine gf, SaveData.Heroine other)
        {
            var comp = Harem.Compatibility(gf, other);
            var yes = Pairs.GirlsOk(other) && (comp + other.favor / 3 + other.lewdness / 4 + Random.Range(-10, 11) >= 50);
            var gk = GirlInfo.Key(gf);
            var ok = GirlInfo.Key(other);
            var p = Pairs.Of(gf, other);
            if (yes)
            {
                foreach (var h in new[] { gf, other }) if (!S.Harem.Contains(GirlInfo.Key(h))) S.Harem.Add(GirlInfo.Key(h));
                if (p != null) { p.AddRomance(gk, 20); p.AddRomance(ok, 20); }
                Arcs.AddBond(gk, ok, 15);
                Set(gf, other, Trio, "she said yes");
                S.Girl(ok).Share[gk] = Trio;
                S.Girl(ok).ShareSaid.Add(gk);
                G.AddFavor(other, 5, 4); G.AddFavor(gf, 3, 4);
                Toasts.Show(GirlInfo.FirstName(other) + " said yes. The three of you are together now.");
                Anthology.Add("trio", GirlInfo.FirstName(gf) + " and " + GirlInfo.FirstName(other) + " are both with you", 4, gk + "," + ok);
            }
            else if (Social.Bond(gf, other) >= 0)
            {
                Arcs.AddBond(gk, ok, 8);
                Set(gf, other, Friends, "she said no to the trio, but they became friends");
                Toasts.Show(GirlInfo.FirstName(other) + " said no to the three of you... but she and " + GirlInfo.FirstName(gf) + " get along now.");
            }
            else
            {
                Set(gf, other, Separate, "she said no to the trio");
                Toasts.Show(GirlInfo.FirstName(other) + " turned the idea down.");
            }
        }

        public static string Describe()
        {
            var lines = new List<string>();
            foreach (var gf in Social.Students.Where(h => h.isGirlfriend || S.Girl(GirlInfo.Key(h)).Share.Count > 0))
            {
                var gs = S.Girl(GirlInfo.Key(gf));
                foreach (var kv in gs.Share)
                {
                    int calm;
                    gs.ShareCalm.TryGetValue(kv.Key, out calm);
                    lines.Add(GirlInfo.FirstName(gf) + " / " + GirlInfo.FirstName(kv.Key) + ": " + Name(kv.Value) + (kv.Value == Refuses ? " (calm " + calm + ")" : "") + (gs.ShareSaid.Contains(kv.Key) ? ", said" : ""));
                }
            }
            return lines.Count == 0 ? "share: none" : "share: " + string.Join("; ", lines.ToArray());
        }

        public static string Force(SaveData.Heroine gf, SaveData.Heroine other, bool saw)
        {
            var what = Learned(gf, other, saw);
            return "share learn " + GirlInfo.FirstName(gf) + " / " + GirlInfo.FirstName(other) + " (" + (saw ? "saw" : "heard") + ") -> " + what + "; " + Name(Stance(gf, other));
        }
    }
}
