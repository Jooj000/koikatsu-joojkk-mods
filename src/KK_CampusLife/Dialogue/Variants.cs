using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Simple answers vary (2026-10-27: "coloque umas 4 respostas diferentes pra cada resposta simples"). A line that is
    /// only a short stock answer ("Maybe.", "Sure.", "I guess so.", "We'll see.", "Thanks.", "Hm.") is swapped for one of
    /// at least five lines of the same intent in her tone, never the one she said last. Longer lines stay as written.
    /// </summary>
    internal static class Variants
    {
        private sealed class Pool
        {
            public string Id;
            public string[][] ByTone;   // Gentle, Energetic, Cool, Teasing
        }

        private static Dictionary<string, Pool> _byKey;
        private static readonly Dictionary<string, string> Last = new Dictionary<string, string>();

        private static Pool P(string id, string keys, string[] gentle, string[] energetic, string[] cool, string[] teasing)
        {
            var p = new Pool { Id = id, ByTone = new[] { gentle, energetic, cool, teasing } };
            foreach (var k in keys.Split('|')) _byKey[Norm(k)] = p;
            return p;
        }

        private static void Ensure()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, Pool>();
            P("maybe", "maybe|we'll see|maybe i'll think about it|i'll think about it|if i have time|perhaps|depends|hm maybe|maybe later|later maybe|maybe we'll see|possibly",
                new[] { "Maybe... I'd have to see.", "I'll think about it, okay?", "Um... maybe. Can I tell you later?", "If I have time, I'd like that.", "Let me think about it a little.", "Maybe... it does sound nice." },
                new[] { "Hmm, maybe! Ask me again later!", "Could be fun! Lemme check my schedule.", "Ooh, maybe! No promises!", "We'll see! Depends on my mood!", "Maybe! If nothing comes up!" },
                new[] { "Perhaps.", "We'll see.", "I'll consider it.", "Depends on my schedule.", "Not a no.", "Possibly." },
                new[] { "Maybe~ Convince me.", "I'll think about it. If you're lucky.", "Hmm, depends on what's in it for me.", "We'll see~ Don't get your hopes up.", "Maybe. Keep asking nicely." });
            P("yes", "sure|fine|yes|i'd like that|sounds good|alright|of course|sure thing|okay sure|yeah sure",
                new[] { "Okay... I'd like that.", "Sure, if it's with you.", "Mm. Yes.", "That sounds nice.", "Of course." },
                new[] { "Sure thing!", "Yeah, totally!", "Okay, deal!", "You bet!", "Heck yes!" },
                new[] { "Fine.", "Sure.", "Alright.", "Very well.", "No objection." },
                new[] { "Sure~ You owe me.", "Fine, fine. Just for you.", "Okay. Don't make me regret it.", "Why not~", "Alright, you win." });
            P("meh", "i guess so|i guess|if you say so|i suppose|yeah i suppose|mm i guess so|hm could be|could be|huh okay|okay then|sure i guess|okay|yeah|true|interesting i guess",
                new[] { "I guess so...", "Mm, maybe you're right.", "That could be true.", "I suppose so.", "Hm... yeah, maybe." },
                new[] { "Huh, yeah, could be!", "I guess! Sure!", "Eh, fair enough!", "Yeah, I can see that!", "Hmm, okay, okay!" },
                new[] { "If you say so.", "Could be.", "I suppose.", "Fair enough.", "Hm. Possibly." },
                new[] { "If you say so~", "Sure, let's pretend that's true.", "Hm, I'll allow it.", "Could be. Could also not be.", "Whatever you say~" });
            P("no", "no|i don't think so|um i don't think so|sorry i'm busy|not now|no thanks|pass|not today",
                new[] { "Sorry... not this time.", "I don't think I can, sorry.", "Um... maybe another day?", "I'd rather not... sorry.", "Not right now, okay?" },
                new[] { "Nah, can't today! Sorry!", "Ehh, pass! Next time!", "No can do!", "Not today, sorry!", "Ugh, I wish, but no!" },
                new[] { "No.", "Not interested.", "I'll pass.", "Not today.", "No, thank you." },
                new[] { "Nope~", "Not a chance. Try again later.", "Hmm... no.", "Cute try. No.", "In your dreams~" });
            P("thanks", "thanks|thank you|thanks really|yeah thanks|thank you really|thanks though",
                new[] { "Thank you... really.", "Thanks. That means a lot.", "Thank you so much.", "I'm grateful, really.", "Thanks... you're kind." },
                new[] { "Thanks a bunch!", "You're the best, thanks!", "Thank youuu!", "Thanks! Seriously!", "Aw, thanks!" },
                new[] { "Thanks.", "Thank you.", "I appreciate it.", "Noted. Thanks.", "Much obliged." },
                new[] { "Thanks~ I'll remember this.", "Aww, how sweet. Thanks.", "Thanks. Don't let it go to your head.", "Why, thank you~", "Hmph. ...Thanks." });
            P("welcome", "you're welcome|anytime|no problem|no need to thank me|hehe you're welcome|don't mention it",
                new[] { "You're welcome.", "Anytime, really.", "I was happy to.", "It was nothing.", "Of course, anytime." },
                new[] { "No prob!", "Anytime!", "Don't mention it!", "Happy to help!", "Hehe, sure thing!" },
                new[] { "Don't mention it.", "It's fine.", "No need.", "Hm. Anytime.", "It was nothing." },
                new[] { "You owe me~", "Anytime. For you.", "I'll collect later~", "Don't mention it. Really, don't.", "Hehe, you're welcome." });
            P("hm", "hm|hmm|huh|oh|i see|hm i see|interesting|oh really|mm",
                new[] { "Oh... I see.", "Mm, okay.", "Ah, I see.", "Oh, really?", "Hm... okay." },
                new[] { "Huh! Okay!", "Oh! Got it!", "Ooh, okay!", "Ah, gotcha!", "Hm! Interesting!" },
                new[] { "Hm.", "I see.", "Noted.", "Huh.", "Right." },
                new[] { "Hmm~", "Oh? Is that so.", "I see, I see.", "Huh. Okay then.", "Interesting~" });
            P("cold", "right|wow okay|oh okay|hmph|seriously|okay then?|was that supposed to help|that didn't help|wow read the room",
                new[] { "...Oh. Okay.", "That's... not what I meant.", "Um... right.", "...I see.", "Oh... alright." },
                new[] { "Wow. Okay then.", "Huh?! Seriously?", "Ehh... not cool.", "Uh, what?", "Hey, come on." },
                new[] { "...Right.", "Hmph.", "Noted. Unfortunately.", "...Okay.", "Was that necessary?" },
                new[] { "Wow. Smooth.", "Read the room much?", "Hmph. Whatever.", "Cute. Not.", "Seriously?" });
        }

        /// <summary>"...Maybe. I'll think about it!" -> "maybe i'll think about it".</summary>
        private static string Norm(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in (s ?? "").ToLowerInvariant())
            {
                if (c == '.' || c == ',' || c == '!' || c == '?' || c == '~' || c == '*' || c == '"') { sb.Append(' '); continue; }
                sb.Append(c == '’' ? '\'' : c);
            }
            var t = sb.ToString().Trim();
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t;
        }

        /// <summary>The same answer in other words when the line is a short stock answer; otherwise the line itself.</summary>
        public static string Swap(SaveData.Heroine h, string text)
        {
            if (h == null || string.IsNullOrEmpty(text) || text.Length > 40 || text.IndexOf('<') >= 0) return text;
            try
            {
                Ensure();
                Pool p;
                if (!_byKey.TryGetValue(Norm(text), out p)) return text;
                var l = p.ByTone[(int)GirlInfo.ToneOf(h)];
                var lastKey = GirlInfo.Key(h) + "|" + p.Id;
                string last;
                Last.TryGetValue(lastKey, out last);
                var pick = l[Random.Range(0, l.Length)];
                for (var i = 0; i < 4 && pick == last; i++) pick = l[Random.Range(0, l.Length)];
                Last[lastKey] = pick;
                return pick;
            }
            catch { return text; }
        }

        public static string IntentOf(string text)
        {
            Ensure();
            Pool p;
            return text != null && text.Length <= 40 && _byKey.TryGetValue(Norm(text), out p) ? p.Id : "";
        }
    }
}
