using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// resposta, o player solta uma frase no diálogo condizente com a opção, e o contexto"). The option label is his
    /// intent in the imperative ("Ask which event she's in"); this turns it into his line ("Which event are you in?"),
    /// or into a narration when it is an action ("Walk with her to the store" to "You walk with her to the store.").
    /// The label already holds the context (the subject, her name, what she just said), so the line does too. Same
    /// rules as tools\speech.py, which checks every label of the banks; a label that comes out wrong gets its own
    /// line (the bank's "me.(move)" rules, or the Me of a beat answer). The word lists below come from speech.py.
    /// </summary>
    internal static class Speech
    {
        private static readonly HashSet<string> MODAL = Set("can", "could", "may", "might", "must", "should", "will", "would");
        private static readonly HashSet<string> OBJ_NEXT = Set("", "a", "about", "across", "again", "all", "alone", "along", "already", "an", "and", "angry", "anymore", "anything", "anyway", "apart", "around", "as", "at", "away", "back", "be", "behind", "beside", "best", "blush", "both", "busy", "by", "calm", "can", "change", "choose", "come", "company", "cook", "could", "crazy", "cry", "curious", "dance", "decide", "do", "down", "downstairs", "during", "eat", "either", "enough", "even", "every", "everything", "fall", "feel", "finish", "first", "for", "forget", "free", "from", "get", "go", "happy", "have", "hear", "help", "here", "herself", "home", "how", "hungry", "if", "in", "inside", "instead", "into", "is", "it", "jump", "just", "keep", "know", "last", "later", "laugh", "least", "leave", "less", "light", "like", "look", "lose", "luck", "mad", "make", "more", "most", "much", "near", "need", "nervous", "next", "nothing", "now", "nuts", "of", "off", "on", "once", "open", "or", "out", "outside", "over", "panic", "past", "pick", "play", "practice", "properly", "read", "relax", "remember", "rest", "right", "run", "sad", "safe", "say", "see", "sick", "since", "sing", "sleep", "sleepy", "smile", "so", "some", "something", "sometime", "sometimes", "soon", "sound", "space", "start", "stay", "stop", "struggle", "suffer", "sweat", "swim", "take", "talk", "than", "that", "the", "there", "think", "this", "though", "through", "time", "tired", "to", "today", "tomorrow", "tonight", "too", "toward", "towards", "try", "twice", "under", "uneasy", "until", "up", "upset", "upstairs", "wait", "walk", "want", "warm", "was", "well", "what", "when", "where", "which", "who", "why", "will", "win", "with", "within", "without", "worried", "worry", "would", "write", "wrong", "yesterday", "yet", "yourself");
        private static readonly HashSet<string> GIVE = Set("bring", "buy", "call", "cook", "fetch", "give", "hand", "lend", "offer", "owe", "pass", "pour", "promise", "save", "send", "show", "teach", "tell", "wish");
        private static readonly HashSet<string> SPEECH = Set("ah", "all", "any", "anything", "are", "aw", "bet", "bless", "can", "careful", "careful,", "cheers", "come", "congrats", "could", "cute", "deal", "did", "do", "don't", "everyone", "fair", "fine", "good", "great", "guess", "ha", "hah", "happy", "hey", "hm", "hmm", "honestly", "how", "huh", "i", "i'd", "i'll", "i'm", "is", "it", "it's", "just", "keep", "let", "let's", "lucky", "maybe", "me", "mine", "never", "nice", "no", "nobody", "nope", "not", "oh", "ok", "okay", "only", "please", "really", "relax", "right", "same", "seriously", "should", "so", "sorry", "sure", "thank", "thanks", "that", "that's", "this", "too", "trust", "uh", "um", "us", "we", "welcome", "well", "what", "when", "where", "who", "why", "will", "would", "wow", "yeah", "yes", "you");
        private static readonly HashSet<string> ADV = Set("actually", "already", "also", "always", "even", "ever", "ever,", "finally", "honestly", "just", "mostly", "never", "normally", "often", "really", "secretly", "seriously", "sometimes", "still", "usually");
        private static readonly HashSet<string> DETS = Set("a", "all", "an", "any", "both", "each", "every", "her", "his", "its", "most", "my", "no", "our", "some", "that", "the", "their", "these", "this", "those", "your");
        private static readonly HashSet<string> PLURALS = Set("always", "animals", "books", "boys", "cards", "cats", "chores", "classes", "clothes", "clubs", "days", "dogs", "drinks", "events", "exams", "exercises", "eyes", "feet", "flowers", "friends", "games", "girls", "glasses", "grades", "guys", "hands", "holidays", "hours", "legs", "lessons", "lines", "lots", "movies", "news", "notes", "others", "parents", "people", "plans", "professors", "rumors", "shoes", "snacks", "sometimes", "songs", "sports", "students", "sweets", "teachers", "things", "times", "weekends", "words", "years");
        private static readonly HashSet<string> YOU_SUBJ_NEXT = Set("admire", "agree", "also", "always", "are", "aren't", "ask", "ate", "believe", "believed", "bet", "came", "can", "can't", "care", "could", "couldn't", "count", "did", "didn't", "do", "don't", "enjoy", "enjoyed", "feel", "fell", "felt", "figured", "forgot", "found", "get", "go", "got", "guess", "had", "hate", "hated", "have", "haven't", "heard", "hope", "just", "keep", "kept", "knew", "know", "learned", "like", "liked", "look", "lost", "love", "loved", "made", "mean", "meant", "met", "might", "miss", "missed", "must", "need", "needed", "never", "noticed", "owe", "play", "played", "prefer", "promise", "ran", "read", "really", "remember", "run", "said", "saw", "say", "see", "seem", "should", "shouldn't", "slept", "sound", "spent", "started", "still", "stopped", "studied", "study", "suck", "swear", "talk", "think", "thought", "told", "tried", "trust", "understand", "used", "want", "wanted", "went", "were", "weren't", "will", "wish", "won", "won't", "wonder", "work", "worked", "would", "wouldn't");
        private static readonly HashSet<string> PARTICIPLES = Set("become", "been", "begun", "bought", "broken", "brought", "caught", "chosen", "come", "decided", "done", "driven", "drunk", "eaten", "fallen", "felt", "finished", "forgotten", "found", "given", "gone", "got", "gotten", "grown", "had", "heard", "kept", "known", "learned", "left", "lost", "made", "meant", "met", "noticed", "planned", "ridden", "run", "said", "sat", "seen", "shown", "slept", "spent", "started", "stolen", "stood", "sung", "swum", "taken", "taught", "thought", "thrown", "told", "tried", "understood", "wanted", "woken", "won", "worn", "written");
        private static readonly string[] WH = { "what", "why", "how", "where", "when", "who", "which", "whose", "whom" };
        private static readonly string[] NARR = { "nod", "smile", "laugh", "shrug", "listen", "stay quiet", "keep quiet", "hug", "pat", "hold", "take her hand", "wave", "sigh", "wink", "grin", "blush", "look away", "lean in", "play along", "let her", "stay", "sit", "wait", "squeeze", "kiss", "stroke", "touch", "poke", "brush", "walk", "leave", "high-five", "fist-bump", "chuckle", "groan", "pretend", "roll your eyes", "raise an eyebrow", "glance", "watch", "stare", "joke", "tease", "shrug it off", "laugh it off" };
        private static readonly string[] PRON_SUBJ = { "she", "she's", "she'd", "she'll", "they", "they've", "they're", "they'd", "he", "he's", "you", "you're", "you've", "it", "it's", "there's", "we", "we're", "i", "i'm" };
        private static readonly string[] E_STEMS = { "ided", "ated", "iced", "ased", "used", "osed", "oved", "ived", "aved", "iled", "ored", "ared", "ured", "ined", "aked", "iked", "oked", "aped", "iped", "oped", "uded", "aded", "ized", "anced", "inced", "uced", "aced", "ged", "rsed", "lsed", "psed", "aled", "oled", "uled", "ated", "aked", "ated", "ited", "uted", "oted", "aved", "ived" };
        private static readonly string[] NOT_E = { "eated", "oated", "eaded", "oaded", "ained", "eaked", "ooked", "eased", "visited", "edited", "waited", "exited", "limited", "credited", "inhabited", "orbited", "vomited", "audited", "rained", "trained", "joined", "gained", "explained", "complained", "remained", "ruined", "mained", "eaned", "ealed", "ailed", "ooled", "oiled", "uited", "aited", "oited", "ouded", "aired", "eared", "oared", "oured", "ired" };
        private static readonly Dictionary<string, string> IRREG = Map("ate", "eat", "became", "become", "began", "begin", "bought", "buy", "broke", "break", "brought", "bring", "came", "come", "caught", "catch", "chose", "choose", "did", "do", "drank", "drink", "drew", "draw", "fell", "fall", "felt", "feel", "flew", "fly", "forgot", "forget", "found", "find", "gave", "give", "got", "get", "grew", "grow", "had", "have", "heard", "hear", "hid", "hide", "hurt", "hurt", "kept", "keep", "knew", "know", "learnt", "learn", "left", "leave", "let", "let", "lost", "lose", "made", "make", "meant", "mean", "met", "meet", "put", "put", "ran", "run", "rode", "ride", "said", "say", "sang", "sing", "sat", "sit", "saw", "see", "sent", "send", "shook", "shake", "slept", "sleep", "sold", "sell", "spent", "spend", "stole", "steal", "stood", "stand", "swam", "swim", "taught", "teach", "thought", "think", "threw", "throw", "told", "tell", "took", "take", "understood", "understand", "went", "go", "woke", "wake", "won", "win", "wore", "wear", "wrote", "write");
        private static readonly Dictionary<string, string> S3 = Map("does", "do", "goes", "go", "has", "have", "is", "are", "says", "say", "was", "were");

        private static HashSet<string> Set(params string[] a) { return new HashSet<string>(a); }
        private static Dictionary<string, string> Map(params string[] a) { var d = new Dictionary<string, string>(); for (int i = 0; i + 1 < a.Length; i += 2) d[a[i]] = a[i + 1]; return d; }
        private static bool In(string[] a, string x) { return Array.IndexOf(a, x) >= 0; }
        private static bool Starts(string s, params string[] a) { foreach (var x in a) if (s.StartsWith(x, StringComparison.Ordinal)) return true; return false; }
        private static bool Ends(string s, params string[] a) { foreach (var x in a) if (s.EndsWith(x, StringComparison.Ordinal)) return true; return false; }
        private static string FirstWord(string s) { var i = s.IndexOf(' '); return i < 0 ? s : s.Substring(0, i); }
        private static readonly string WhAlt = "what|why|how|where|when|who|which|whose|whom";

        private static string EdBase(string lv)
        {
            if (lv.EndsWith("eed")) return lv == "agreed" || lv == "freed" ? lv.Substring(0, lv.Length - 1) : lv;
            int n = lv.Length;
            if (n > 5 && lv[n - 3] == lv[n - 4] && "bdgmnprt".IndexOf(lv[n - 3]) >= 0 && "aeiou".IndexOf(lv[n - 5]) >= 0 && (n < 6 || "aeiou".IndexOf(lv[n - 6]) < 0)) return lv.Substring(0, n - 3);
            if (Ends(lv, NOT_E)) return lv.Substring(0, n - 2);
            if (lv.EndsWith("ired") || lv.EndsWith("ared") && !lv.EndsWith("eared")) return lv.Substring(0, n - 1);
            if (Ends(lv, E_STEMS)) return lv.Substring(0, n - 1);
            return lv.Substring(0, n - 2);
        }

        private static string Base(string v)
        {
            var lv = v.ToLowerInvariant();
            string r;
            if (IRREG.TryGetValue(lv, out r)) return r;
            if (S3.TryGetValue(lv, out r)) return lv == "is" || lv == "was" ? "be" : r;
            if (lv.EndsWith("ied") || lv.EndsWith("ies")) return lv.Substring(0, lv.Length - 3) + "y";
            if (Regex.IsMatch(lv, "(ch|sh|ss|x|o)es$")) return lv.Substring(0, lv.Length - 2);
            if (lv.EndsWith("ed") && lv.Length > 3) return EdBase(lv);
            if (lv.EndsWith("s") && !lv.EndsWith("ss")) return lv.Substring(0, lv.Length - 1);
            return lv;
        }

        private static readonly HashSet<string> NotEd = Set("red", "bed", "need", "seed", "feed", "speed", "weed", "shed", "bored", "tired", "scared", "excited", "interested", "worried", "stressed", "used", "married", "embarrassed", "relaxed", "talented", "naked", "wicked", "beloved", "sacred");
        private static readonly HashSet<string> Negs = Set("isn't", "wasn't", "doesn't", "didn't", "can't", "won't", "hasn't", "wouldn't", "couldn't", "shouldn't");
        private static readonly HashSet<string> Auxes = Set("is", "are", "was", "were", "has", "have", "had", "do", "does", "did");

        private static bool Verbish(string w, string prev = "")
        {
            var lw = w.ToLowerInvariant().Trim(',');
            if (Auxes.Contains(lw) || MODAL.Contains(lw) || Negs.Contains(lw) || IRREG.ContainsKey(lw)) return true;
            if (Regex.IsMatch(lw, "^[a-z]+ed$") && !NotEd.Contains(lw)) return true;
            if (Regex.IsMatch(lw, "^[a-z]+s$") && !Ends(lw, "ss", "us", "is", "ous") && !DETS.Contains(prev.ToLowerInvariant()) && !PLURALS.Contains(lw)) return true;
            return false;
        }

        private static string Strip(string label)
        {
            var t = Regex.Replace(label ?? "", "<[^>]+>", "");
            t = Regex.Replace(t, @"\s*\[[A-Z]{3}\]", "");
            t = Regex.Replace(t, @"\s*\([^)]*\)\s*$", "");
            t = t.Replace("★", "").Trim().Trim('\'', '"');
            return Regex.Replace(t, @"\s+", " ");
        }

        private static string LastWord(List<string> o) { for (int i = o.Count - 1; i >= 0; i--) if (o[i].Trim().Length > 0) return o[i]; return ""; }

        private static readonly string[] SheHas = { "been", "become", "gone", "done", "seen", "taken", "eaten", "given", "grown", "got", "gotten", "had", "made", "said", "told", "found", "met", "heard", "lost", "won", "spent", "kept", "left", "forgotten", "fallen", "broken", "decided", "finished", "started", "noticed", "learned", "tried", "changed" };
        private static readonly string[] YouSubjPrev = { "", "that", "if", "when", "because", "and", "but", "so", "wish", "think", "hope", "bet", "mean", "until", "unless", "since", "while", "whether", "before", "after" };
        private static readonly string[] WhPrev = { "what", "why", "how", "where", "who", "which" };

        /// <summary>His intent about her and him ("you" is him, "she" is her) to his words to her ("I" is him, "you" is her).</summary>
        private static string Swap(string t, bool obj = false)
        {
            t = Regex.Replace(t, @"\b(just )?the two of you\b", m => m.Groups[1].Value + "the two of us");
            var toks = new List<string>();
            foreach (Match m in Regex.Matches(t, @"[A-Za-z{}@'\-]+|[^A-Za-z{}@'\-\s]+|\s+")) toks.Add(m.Value);
            var o = new List<string>();
            var prev = "";
            for (int i = 0; i < toks.Count; i++)
            {
                var w = toks[i];
                var lw = w.ToLowerInvariant();
                var nxt = "";
                for (int k = i + 1; k < toks.Count; k++) if (toks[k].Trim().Length > 0) { nxt = toks[k].ToLowerInvariant(); break; }
                var r = w;
                var word = Regex.IsMatch(nxt.Length > 0 ? nxt : "-", "^[a-z']+$");
                if (lw == "you")
                {
                    if (obj && prev == "") r = "me";
                    else if (YOU_SUBJ_NEXT.Contains(nxt) || In(WhPrev, prev) && word && !OBJ_NEXT.Contains(nxt) || In(YouSubjPrev, prev) && word && !OBJ_NEXT.Contains(nxt)) r = "I";
                    else r = "me";
                }
                else if (lw == "your") r = "my";
                else if (lw == "yours") r = "mine";
                else if (lw == "yourself") r = "myself";
                else if (lw == "you're") r = "I'm";
                else if (lw == "you'd") r = "I'd";
                else if (lw == "you'll") r = "I'll";
                else if (lw == "you've") r = "I've";
                else if (lw == "she") r = "you";
                else if (lw == "she's") r = In(SheHas, nxt) ? "you've" : "you're";
                else if (lw == "she'd") r = "you'd";
                else if (lw == "she'll") r = "you'll";
                else if (lw == "herself") r = "yourself";
                else if (lw == "her")
                    r = OBJ_NEXT.Contains(nxt) || GIVE.Contains(prev) || nxt.EndsWith("ing") || nxt.EndsWith("ed") && nxt != "bed" && nxt != "red" && nxt != "need" && nxt != "seed" || !Regex.IsMatch(nxt, "^[a-z{]") ? "you" : "your";
                else if (lw == "hers") r = "yours";
                else if (prev == "she" && S3.ContainsKey(lw)) r = S3[lw];
                else if (prev == "she" && (lw == "doesn't" || lw == "isn't" || lw == "wasn't" || lw == "hasn't")) r = lw == "doesn't" ? "don't" : lw == "isn't" ? "aren't" : lw == "wasn't" ? "weren't" : "haven't";
                else if (prev == "she" && Regex.IsMatch(lw, "^[a-z]+s$") && !lw.EndsWith("ss")) r = Base(lw);
                else if (LastWord(o) == "I" && (lw == "are" || lw == "were" || lw == "aren't" || lw == "weren't" || lw == "is"))
                    r = lw == "are" ? "am" : lw == "were" ? "was" : lw == "aren't" ? "am not" : lw == "weren't" ? "wasn't" : "am";
                if (w.Length > 0 && char.IsUpper(w[0]) && r.Length > 0 && char.IsLower(r[0])) r = char.ToUpperInvariant(r[0]) + r.Substring(1);
                o.Add(r);
                if (w.Trim().Length > 0 && Regex.IsMatch(w, "^[A-Za-z{]")) prev = lw;
            }
            return string.Concat(o.ToArray());
        }

        private static string Cap(string s) { s = s.Trim(); return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s; }
        private static string End(string s, string mark) { return s.Trim().TrimEnd('.', '!', '?', ',') + mark; }
        private static string Join(List<string> a, int from, int to) { return from >= to ? "" : string.Join(" ", a.GetRange(from, to - from).ToArray()); }

        private static readonly Dictionary<string, string[]> Contr = new Dictionary<string, string[]>
        {
            { "she's", new[] { "are", "you" } }, { "they've", new[] { "have", "they" } }, { "they're", new[] { "are", "they" } }, { "she'd", new[] { "would", "you" } },
            { "she'll", new[] { "will", "you" } }, { "it's", new[] { "is", "it" } }, { "there's", new[] { "is", "there" } }, { "he's", new[] { "is", "he" } },
            { "you're", new[] { "am", "I" } }, { "you've", new[] { "have", "I" } }, { "they'd", new[] { "would", "they" } }, { "you'd", new[] { "would", "I" } },
            { "that's", new[] { "is", "that" } }, { "we're", new[] { "are", "we" } },
        };
        private static readonly HashSet<string> PlainSubj = Set("she", "you", "they", "we", "he", "that", "this", "it", "everyone", "anyone", "someone", "nobody", "everything", "something", "anything", "there", "people", "everybody");
        private static readonly HashSet<string> Plural = Set("you", "i", "they", "we");

        /// <summary>"she found out" to "did you find out"; "she's in" to "are you in"; "{event} keeps her busy" to "does {event} keep you busy".</summary>
        private static string Question(string clause)
        {
            var toks = clause.Split(' ').Where(x => x.Length > 0).ToList();
            if (toks.Count == 0) return clause;
            var subj = toks[0].ToLowerInvariant();
            string[] c;
            if (Contr.TryGetValue(subj, out c))
            {
                var aux = c[0];
                if (subj.EndsWith("'s") && toks.Count > 1 && PARTICIPLES.Contains(toks[1].ToLowerInvariant())) aux = c[1] == "you" ? "have" : "has";
                return aux + " " + c[1] + (toks.Count > 1 ? " " + Swap(Join(toks, 1, toks.Count), true) : "");
            }
            int vi = -1;
            if (toks.Count > 1 && toks[1].ToLowerInvariant() == "and") { for (int i = 3; i < toks.Count; i++) if (!ADV.Contains(toks[i].ToLowerInvariant())) { vi = i; break; } }
            else if (PlainSubj.Contains(subj)) { for (int i = 1; i < toks.Count; i++) if (!ADV.Contains(toks[i].ToLowerInvariant())) { vi = i; break; } }
            else { for (int i = 1; i < toks.Count; i++) if (!ADV.Contains(toks[i].ToLowerInvariant()) && Verbish(toks[i], toks[i - 1])) { vi = i; break; } }
            int poss = -1;
            for (int i = 0; i < toks.Count; i++)
            {
                var x = toks[i].ToLowerInvariant();
                if (Regex.IsMatch(x, "^[a-z{}]+'s$") && !Contr.ContainsKey(x) && x != "what's" && x != "who's" && x != "where's" && x != "how's") { poss = i; break; }
            }
            if (poss >= 0 && (vi < 0 || poss < vi))
            {
                var part = toks.GetRange(0, poss);
                part.Add(toks[poss].Substring(0, toks[poss].Length - 2));
                return "is " + Swap(string.Join(" ", part.ToArray())) + (poss + 1 < toks.Count ? " " + Swap(Join(toks, poss + 1, toks.Count), true) : "");
            }
            if (vi <= 0 && DETS.Contains(subj) && toks.Count >= 3 && toks[1].ToLowerInvariant().EndsWith("s")) return "do " + Swap(clause);
            if (vi <= 0) return Swap(clause);
            int j = vi - 1;
            var advs = new List<string>();
            while (j >= 1 && ADV.Contains(toks[j].ToLowerInvariant())) { advs.Insert(0, toks[j]); j--; }
            var head = Join(toks, 0, j + 1);
            var s = head.ToLowerInvariant() == "you" ? "I" : head.ToLowerInvariant() == "she" ? "you" : Swap(head);
            if (s.ToLowerInvariant().StartsWith("you and ")) s = "you" + s.Substring(3);
            var v = toks[vi];
            var lv = v.ToLowerInvariant();
            var after = vi + 1 < toks.Count ? Swap(Join(toks, vi + 1, toks.Count), true) : "";
            var mid = advs.Count > 0 ? " " + string.Join(" ", advs.ToArray()) : "";
            var tail = after.Length > 0 ? " " + after : "";
            var nx = vi + 1 < toks.Count ? toks[vi + 1].ToLowerInvariant() : "";
            var pl = Plural.Contains(s.ToLowerInvariant());
            if ((lv == "do" || lv == "does" || lv == "did") && nx != "not" || (lv == "has" || lv == "have" || lv == "had") && !PARTICIPLES.Contains(nx) && nx != "been" && nx != "to" && nx != "got" && nx != "never" && nx != "ever" && nx != "already" && nx != "just" && nx != "always")
            {
                var aux = lv == "did" || lv == "had" ? "did" : lv == "do" || lv == "have" ? "do" : pl ? "do" : "does";
                return aux + " " + s + mid + " " + (lv == "do" || lv == "does" || lv == "did" ? "do" : "have") + tail;
            }
            if (Auxes.Contains(lv) || MODAL.Contains(lv))
            {
                var aux = lv;
                if (s == "you") aux = lv == "is" ? "are" : lv == "was" ? "were" : lv == "has" ? "have" : lv == "does" ? "do" : lv;
                else if (s == "I") aux = lv == "are" || lv == "is" ? "am" : lv == "were" ? "was" : lv == "has" ? "have" : lv == "does" ? "do" : lv;
                return aux + " " + s + mid + tail;
            }
            if (Negs.Contains(lv))
            {
                var aux = s == "you" ? (lv == "isn't" ? "aren't" : lv == "wasn't" ? "weren't" : lv == "doesn't" ? "don't" : lv == "hasn't" ? "haven't" : lv) : lv;
                return aux + " " + s + mid + tail;
            }
            var b = Base(v);
            var past = IRREG.ContainsKey(lv) || lv.EndsWith("ed");
            var third = lv.EndsWith("s") && !past;
            var a2 = past ? "did" : third && !pl ? "does" : "do";
            return a2 + " " + s + mid + " " + b + tail;
        }

        private static readonly Regex VerbRx = new Regex(@"^(say|tell her|tell (?:\{[a-z0-9]+\}|[A-Z][a-z]+)|reply|answer|insist|add|whisper|point out|remind her|confess|claim|swear|agree|admit|promise|apologize|thank her|offer|suggest|invite her|compliment her|ask|explain|reassure her|comfort her|encourage her|cheer her up|give her|defend|disagree|change the subject|let it go|deny|wish her|congratulate her|warn her|beg)\b ?(.*)$", RegexOptions.IgnoreCase);
        private static readonly Regex WhRx = new Regex(@"^((?:" + WhAlt + @")\b(?: (?:kind of|sort of|exactly|else|much|many|long|often|old|far|soon|way|one|ones|part|thing|time|day|class|subject|food|team|event|club|place|song|book|movie|game|flavor|dish|color|animal|season|sport))*)(?: (.*))?$", RegexOptions.IgnoreCase);

        /// <summary>His line for an option label; "*" and a narration when it is an action; null when nothing fits.</summary>
        public static string Line(string label)
        {
            string s;
            try { s = Line0(label); }
            catch (Exception e)
            {
#if !SPEECHTEST
                CampusLifePlugin.Log.LogWarning("Speech: '" + label + "': " + e.Message);
#endif
                return null;
            }
            if (s != null && !s.StartsWith("*"))
            {
                s = Regex.Replace(s, @",? and say (I|my|you)\b", ", and $1");
                s = Regex.Replace(s, @"\bme (am|was|have|can|will|would|do|did)\b", "I $1");
            }
            return s;
        }

        private static string Line0(string label)
        {
            var t = Strip(label);
            if (t.Length == 0) return null;
            var l = t.ToLowerInvariant();
            var m = VerbRx.Match(t);
            if (!m.Success)
            {
                var fw = FirstWord(l).Trim(',', '.', '!', '?');
                if (Starts(l, NARR) || !SPEECH.Contains(fw)) return "*You " + l.Substring(0, 1) + t.Substring(1).TrimEnd('.', '!', '?') + ".";
                return End(Cap(t), Starts(l, WH) || Starts(l, "are ", "is ", "do ", "did ", "can ", "will ", "would ") ? "?" : ".");
            }
            var verb = m.Groups[1].Value.ToLowerInvariant();
            var rest = m.Groups[2].Value.Trim();
            var rl = rest.ToLowerInvariant();
            switch (verb)
            {
                case "say": case "reply": case "answer": case "insist": case "add": case "whisper": case "claim": case "swear": case "point out":
                    rest = Regex.Replace(rest, @"^(?:[Tt]hat|[Ss]o) (?=(?:(?i:she|you|he|they|it|her|the|everyone|nobody|no one|someone|we|i|there)\b|\{|[A-Z]))", "");
                    rl = rest.ToLowerInvariant();
                    if (rl.StartsWith("what ")) rest = "here's " + rest;
                    if (rl.StartsWith("something ")) return "*You " + verb + " " + rest + ".";
                    if (rest.Length == 0) return null;
                    if (rl.StartsWith("you ") && rl.Contains(" together")) rest = "we " + rest.Substring(4);
                    if (verb == "swear") return End("I swear " + Swap(rest), ".");
                    return End(Cap(Swap(rest)), verb == "insist" ? "!" : ".");
                case "tell her":
                    if (rest.Length == 0) return null;
                    if (rl.StartsWith("about ")) return End("Let me tell you " + Swap(rest), ".");
                    if (rl.StartsWith("to ")) return End(Cap(Swap(rest.Substring(3))), ".");
                    if (Starts(rl, "a joke", "a story", "a secret", "something") || In(new[] { "the", "a", "an", "your", "my", "some", "everything", "all", "nothing" }, FirstWord(rl))) return "*You tell her " + rest + ".";
                    if (In(WH, FirstWord(rl))) return End("Let me tell you " + Swap(rest), ".");
                    return End(Cap(Swap(Regex.Replace(rest, "^that ", "", RegexOptions.IgnoreCase))), ".");
                case "remind her": return End("Don't forget, " + Swap(Regex.Replace(rest, "^(that|of|about) ", "", RegexOptions.IgnoreCase)), ".");
                case "confess":
                    rest = Regex.Replace(rest, "^that ", "", RegexOptions.IgnoreCase);
                    return rest.Length > 0 ? End("I have to confess... " + Swap(rest), ".") : "I have something to confess.";
                case "agree":
                    if (rest.Length == 0 || rl == "with her" || rl == "with her completely") return "You're right.";
                    if (rl.StartsWith("that ")) return End("You're right, " + Swap(rest.Substring(5)), ".");
                    if (rl.StartsWith("with ")) return End("I'm with " + Swap(rest.Substring(5)) + " on that", ".");
                    if (rl.StartsWith("to ")) return End("Okay, I'll " + Swap(rest.Substring(3)), ".");
                    return End("Agreed, " + Swap(rest), ".");
                case "admit":
                    if (rest.Length == 0 || rl == "it") return "Okay... I admit it.";
                    return End("Okay. I admit " + Swap(Regex.Replace(rest, "^that ", "", RegexOptions.IgnoreCase)), ".");
                case "promise":
                    if (rl.StartsWith("to ")) return End("I promise I'll " + Swap(rest.Substring(3)), ".");
                    if (rl.StartsWith("her ")) return End("I promise you " + Swap(rest.Substring(4)), ".");
                    rest = Regex.Replace(rest, "^that ", "", RegexOptions.IgnoreCase);
                    return rest.Length > 0 ? End("I promise " + Swap(rest), ".") : "I promise.";
                case "apologize":
                    if (rest.Length == 0) return "I'm sorry.";
                    if (rl.StartsWith("for ")) return End("I'm sorry for " + Swap(rest.Substring(4)), ".");
                    if (rl.StartsWith("to ")) return End("I'll apologize to " + Swap(rest.Substring(3)), ".");
                    return End("I'm sorry, " + Swap(rest), ".");
                case "thank her": return End("Thank you" + (rest.Length > 0 ? " " + Swap(rest) : ""), ".");
                case "offer":
                    if (rl.StartsWith("to ") && rl.Contains(" together")) return End("Want to " + Swap(rest.Substring(3)), "?");
                    if (rl.StartsWith("to ")) return End("Want me to " + Swap(rest.Substring(3)), "?");
                    if (rl.StartsWith("her ")) return End("Here. Want " + Swap(rest.Substring(4)), "?");
                    return "*You offer " + rest + ".";
                case "suggest":
                    if (rl.StartsWith("that ")) { rest = rest.Substring(5); rl = rest.ToLowerInvariant(); }
                    if (Regex.IsMatch(rl, @"^[a-z]+ing\b")) return End("How about " + Swap(rest), "?");
                    if (rl.StartsWith("you ") && rl.Contains(" together")) return End("We should " + Swap(rest.Substring(4)), ".");
                    if (rl.StartsWith("you ")) return End("Maybe I should " + Swap(rest.Substring(4)), ".");
                    if (rl.StartsWith("she ")) return End("Maybe you should " + Swap(rest.Substring(4)), ".");
                    if (Starts(rl, "they ", "the two of you", "you two", "we ")) return End("Maybe " + Swap(rest), ".");
                    return End("How about " + Swap(rest), "?");
                case "invite her":
                    rest = Regex.Replace(rest, "^(to|along to|out to|out for|for) ", "", RegexOptions.IgnoreCase);
                    if (rest.Length == 0) return "Want to come with me?";
                    if (Regex.IsMatch(rest.ToLowerInvariant(), @"^(the|a|an|my|your|lunch|dinner|coffee|tea|karaoke|drinks?)\b"))
                        return End("Want to come to " + Swap(rest) + " with me", "?").Replace("come to a walk", "go for a walk").Replace("come to coffee", "get coffee").Replace("come to tea", "get tea")
                            .Replace("come to drinks", "get drinks").Replace("come to karaoke", "do karaoke").Replace("come to a drink", "get a drink").Replace("come to a coffee", "get a coffee");
                    return End("Want to " + Swap(rest) + " with me", "?");
                case "compliment her":
                    if (rest.Length == 0) return "You look really nice today.";
                    if (rl.StartsWith("on ")) return End("I have to say, " + Swap(rest.Substring(3)) + " is impressive", ".");
                    return End("I really like " + Swap("her " + rest), ".");
                case "explain":
                    if (rl == "" || rl == "yourself") return "Let me explain.";
                    if (In(new[] { "you", "she", "it", "that", "the", "they", "he", "nothing", "there" }, FirstWord(rl))) return End("Let me explain. " + Cap(Swap(Regex.Replace(rest, "^that ", ""))), ".");
                    return End("Let me explain " + Swap(rest), ".");
                case "reassure her": return rl.StartsWith("that ") ? End("Hey. " + Cap(Swap(rest.Substring(5))), ".") : "Hey. It's going to be okay.";
                case "comfort her": return "Hey... it's okay. I'm here.";
                case "encourage her": return End("You've got this" + (rest.Length > 0 ? " " + Swap(rest) : ""), ".");
                case "cheer her up": return "Come on, cheer up.";
                case "give her": return rest.Length > 0 ? End("I'll give you " + Swap(rest), ".") : null;
                case "defend":
                    if (rl.StartsWith("yourself")) return "Hey, that's not fair.";
                    return rest.Length > 0 ? End("Hey, go easy on " + Swap(rest), ".") : null;
                case "disagree": return "I don't know... I don't think so.";
                case "change the subject": return "Anyway...";
                case "let it go": return "Let's just let it go.";
                case "deny": return rest.Length > 0 ? End("No, " + Swap(Regex.Replace(rest, "^(that|it) ", "")), ".") : "No, that's not true.";
                case "wish her": return End("I wish you " + Swap(rest), ".");
                case "congratulate her": return End("Congratulations" + (rl.StartsWith("on ") ? " on " + Swap(rest.Substring(3)) : ""), "!");
                case "warn her": return End("Careful... " + Swap(Regex.Replace(rest, "^(that|about) ", "")), ".");
                case "beg": return rest.Length > 0 ? End("Please, " + Swap(Regex.Replace(rest, "^her ", "")), ".") : "Please?";
                case "ask": return Ask(rest);
            }
            if (verb.StartsWith("tell "))
            {
                var who = t.Split(' ')[1];
                if (rl.StartsWith("about ")) return End("I'll tell " + who + " " + Swap(rest), ".");
                return End("I'll tell " + who + (rest.Length > 0 ? " " + Swap(Regex.Replace(rest, "^that ", "")) : ""), ".");
            }
            return null;
        }

        private static string Ask(string rest)
        {
            if (rest.Length == 0) return null;
            rest = Regex.Replace(rest, "^(gently|quietly|softly|carefully|again|politely|casually|finally|nervously|shyly|bluntly|straight out) ", "", RegexOptions.IgnoreCase);
            var rl = rest.ToLowerInvariant();
            if (rl.StartsWith("her to ")) return End("Could you " + Swap(rest.Substring(7)), "?");
            if (rl.StartsWith("her not to ")) return End("Please don't " + Swap(rest.Substring(11)), ".");
            if (rl.StartsWith("to ")) return End((rl.Contains(" together") ? "Can we " : "Can I ") + Swap(rest.Substring(3)), "?");
            if (rl.StartsWith("more about ")) return End("Tell me more about " + Swap(rest.Substring(11)), ".");
            if (Starts(rl, "a ", "an ", "the ", "something", "anything")) return "*You ask " + rest + ".";
            if (rl.StartsWith("her out")) return "Would you go out with me?";
            if (rl.StartsWith("about ")) return End("Tell me about " + Swap(rest.Substring(6)), ".");
            if (rl.StartsWith("for her ")) return End("What's " + Swap(rest.Substring(4)), "?");
            if (rl.StartsWith("for ")) return End("Can you give me " + Swap(rest.Substring(4)), "?");
            if (Starts(rl, "her the ", "her a ", "her an ", "her something", "her anything", "her one ")) return "*You ask " + rest + ".";
            if (rl.StartsWith("her ")) return End("What's " + Swap(rest), "?");
            if (rl.StartsWith("if ") || rl.StartsWith("whether "))
            {
                var i = rest.IndexOf(' ');
                return End(Cap(Question(i < 0 ? "" : rest.Substring(i + 1))), "?");
            }
            if (Regex.IsMatch(rl, @"^(" + WhAlt + @")'s\b")) return End(Cap(Swap(rest)), "?");
            var mm = WhRx.Match(rest);
            if (!mm.Success) return null;
            var whw = mm.Groups[1].Value.Trim();
            var c = mm.Groups[2].Value.Trim();
            if (c.Length == 0) return End(Cap(whw), "?");
            if (c.ToLowerInvariant().StartsWith("to ")) return End(Cap(whw + " should I " + Swap(c.Substring(3))), "?");
            var ws = c.Split(' ').Where(x => x.Length > 0 && !ADV.Contains(x.ToLowerInvariant())).ToList();
            var first = ws.Count > 0 ? ws[0].ToLowerInvariant() : "";
            // the wh-phrase is the subject: "who let her down", "what finally clicked", "which one worries her most"
            if (first.Length > 0 && !In(PRON_SUBJ, first) && !DETS.Contains(first) && Verbish(ws[0]) && !Regex.IsMatch(ws[0], @"^(\{[a-z0-9]+\}|[A-Z][a-z]+)$"))
                return End(Cap(Swap(whw + " " + c)), "?");
            return End(Cap(whw + " " + Question(c)), "?");
        }
    }
}
