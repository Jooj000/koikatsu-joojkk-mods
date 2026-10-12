using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// One thing to talk about, with its own lines: what the player says, how she answers when she loves it, finds it
    /// fine or doesn't care for it (per tone: gentle, energetic, cool, teasing), and two or three ways to go on, which can
    /// teach you something about her (a <see cref="Conversation.Fact"/>) that later subjects use. Replaces the old turn
    /// where any option fell back on a generic topic answer ("talk about the flowers" -> "my hobbies?", 2026-10-11).
    /// </summary>
    internal sealed class TalkSubject
    {
        public string Id = "";
        public Topic Topic;                       // for her taste in topics (mood, personality, what you talked about)
        public string Label = "";                 // the option on the list
        public string[] Opener;                   // what you say (one is picked)
        public string Like;                       // a profile like that makes her love it
        public string NeedsFact, UsesFact;        // offered only if you know this fact / gets better if you know it
        public string[] Loves, Fine, Dislikes;    // her answer by reaction, 4 tones each
        public TalkReply[] Replies;                   // ways to go on (when she didn't dislike it)
        public int Tier = -1;                     // heart: 0 anyone, 1 friend, 2 close, 3 girlfriend (talk subjects: -1)
        public bool Lewd;                         // heart: the flirt list (else sweet talk)
        public int LewdGain;                      // lust when it lands
        public bool Teacher;                      // a teacher's lines (teachers and the free teachers)
        public Func<SaveData.Heroine, bool> When; // extra condition
    }

    internal sealed class TalkReply
    {
        public string Label = "", Tag = "";
        public string[] Her;                      // 4 tones
        public int Delta;                         // -1..1 added to how the turn went
        public int Lewd, Trust;
        public string Learn;                      // fact she tells you
    }

    internal static partial class Conversation
    {
        // ================= facts you learn about her =================

        private static readonly Dictionary<string, string[]> FactValues = new Dictionary<string, string[]>
        {
            { "food", new[] { "curry", "omurice", "melon bread", "ramen", "strawberry daifuku", "takoyaki", "karaage", "cheese anything" } },
            { "book", new[] { "mystery novels", "shoujo manga", "fantasy series", "poetry", "horror stories", "science magazines" } },
            { "season", new[] { "spring", "summer", "autumn", "winter" } },
            { "animal", new[] { "cats", "dogs", "rabbits", "penguins", "hamsters", "otters" } },
            { "dream", new[] { "a patissier", "a professor", "a nurse", "a singer", "a vet", "a novelist", "a pro athlete", "a designer" } },
            { "music", new[] { "idol songs", "rock", "classical piano", "city pop", "game soundtracks", "old enka" } },
            { "place", new[] { "the riverbank at sunset", "the old bookstore by the station", "the arcade", "the shrine on the hill", "the beach", "the rooftop garden downtown" } },
            { "secret", new[] { "still sleeps with a plushie", "can't swim", "is scared of thunder", "practices idol dances in her room", "cried at a dog food commercial", "talks to her plants" } },
        };

        private static int Hash(string s) { var x = 17; foreach (var c in s) x = unchecked(x * 31 + c); return x & 0x7fffffff; }

        /// <summary>Her answer about this (always the same for the same girl).</summary>
        internal static string Fact(SaveData.Heroine h, string id)
        {
            string[] values;
            if (h == null || !FactValues.TryGetValue(id, out values)) return "";
            if (id == "food")
            {
                if (GirlInfo.Likes(h, "sweets")) values = new[] { "strawberry daifuku", "parfaits", "melon bread", "crepes" };
                else if (GirlInfo.Likes(h, "spicy")) values = new[] { "mapo tofu", "extra-hot curry", "kimchi ramen" };
            }
            if (id == "animal" && GirlInfo.Likes(h, "animals")) values = new[] { "cats", "dogs", "rabbits" };
            return values[Hash(GirlInfo.Key(h) + "|" + id) % values.Length];
        }

        internal static bool Knows(SaveData.Heroine h, string id) { return h != null && id != null && S.Girl(GirlInfo.Key(h)).Facts.Contains(id); }

        private static void Learn(SaveData.Heroine h, string id)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Facts.Contains(id)) return;
            gs.Facts.Add(id);
            Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " " + FactPhrase(h, id) + ".");
        }

        private static string FactPhrase(SaveData.Heroine h, string id)
        {
            var v = Fact(h, id);
            switch (id)
            {
                case "food": return "loves " + v;
                case "book": return "reads " + v;
                case "season": return "likes " + v + " best";
                case "animal": return "adores " + v;
                case "dream": return "wants to be " + v;
                case "music": return "listens to " + v;
                case "place": return "loves " + v;
                case "secret": return v;
                default: return v;
            }
        }

        /// <summary>{name}, {club}, {fact:x} in a line.</summary>
        private static string Fill(string text, SaveData.Heroine h)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            text = text.Replace("{name}", GirlInfo.FirstName(h)).Replace("{club}", GirlInfo.ClubName(h));
            foreach (var id in FactValues.Keys) text = text.Replace("{fact:" + id + "}", Fact(h, id));
            return text;
        }

        private static string[] L(string gentle, string energetic, string cool, string teasing) { return new[] { gentle, energetic, cool, teasing }; }
        private static string Pick(string[] l, SaveData.Heroine h) { return Fill(l[(int)GirlInfo.ToneOf(h)], h); }
        private static TalkReply R(string label, string tag, string[] her, int delta = 0, string learn = null, int lewd = 0, int trust = 0)
        {
            return new TalkReply { Label = label, Tag = tag, Her = her, Delta = delta, Learn = learn, Lewd = lewd, Trust = trust };
        }

        // ================= the catalog =================

        internal static readonly List<TalkSubject> Catalog = BuildCatalog();

        private static TalkSubject Find(string id) { return Catalog.FirstOrDefault(x => x.Id == id); }

        private static List<TalkSubject> BuildCatalog()
        {
            var c = new List<TalkSubject>();

            // ---- her mood ----
            c.Add(new TalkSubject { Id = "m_hungry", Topic = Topic.Food, Label = "Ask what she's craving for lunch", Like = "food",
                Opener = new[] { "You look like you could eat a horse. What are you craving?" },
                Loves = L("Ehehe... is it that obvious? I keep thinking about {fact:food}...", "STARVING. I'd fight someone for {fact:food} right now!", "...{fact:food}. I've been thinking about it since second period.", "Food? Mm. {fact:food}. Or you're buying, and then anything."),
                Fine = L("Hmm, anything warm would be nice...", "Anything! Food is food!", "...Something filling.", "Whatever you're paying for."),
                Dislikes = L("I-I'm fine, really...", "Who said I was hungry?! ...Okay, a little.", "I'm fine.", "Are you calling me a glutton?"),
                Replies = new[]
                {
                    R("Offer to share your lunch with her", "share", L("Eh? Really? ...Then just a bite. Thank you.", "YES! You're a lifesaver!", "...One piece. Thanks.", "Sharing your lunch with me? Bold move."), 1),
                    R("Ask what her favorite food is, then", "fav", L("Hmm... {fact:food}, I think. Since I was little.", "{fact:food}! Forever and always!", "...{fact:food}. Don't laugh.", "{fact:food}. Remember that, it might come in handy."), 0, "food"),
                    R("Joke that she'll faint before lunch", "joke", L("D-don't say that, it might happen...", "Then you'll have to carry me! Hehe!", "...Not funny.", "Then catch me when I fall."), -1),
                } });
            c.Add(new TalkSubject { Id = "m_sleepy", Topic = Topic.AboutHer, Label = "Ask if she stayed up late again",
                Opener = new[] { "Late night again? You're half asleep." },
                Loves = L("Ehehe... I couldn't put my book down. I'm a little sleepy...", "I was up gaming 'til three! Worth it! ...Maybe.", "...I was reading. Lost track of time.", "Up late thinking about things. Want to guess what?"),
                Fine = L("Just a little... I'll be okay.", "Nah, I'm fine! Mostly!", "...Somewhat.", "A bit. Don't worry about it."),
                Dislikes = L("Do I look that bad...?", "Rude! I look fresh! ...Ish.", "...I'm fine.", "Wow. Thanks."),
                Replies = new[]
                {
                    R("Ask what kept her up", "what", L("I was reading {fact:book}... one more chapter turned into ten.", "{fact:music} on repeat and a game I couldn't quit!", "...{fact:book}. It got good at the end.", "{fact:book}. Spicy ones. Don't ask."), 0, "book"),
                    R("Tell her to nap at lunch, you'll wake her", "nap", L("You'd wake me...? Then I'll trust you.", "Deal! Wake me with melon bread!", "...Fine. Don't draw on my face.", "Watching me sleep? ...Fine. Gently."), 1),
                    R("Say she still looks cute sleepy", "cute", L("E-eh?! D-don't say that...", "Hehe, smooth talker!", "...Go away.", "Sleepy and cute. Noted."), 0, null, 1),
                } });
            c.Add(new TalkSubject { Id = "m_bored", Topic = Topic.Weekend, Label = "Suggest something fun for after class",
                Opener = new[] { "This day's dragging, huh? Let's think of something fun to do after class." },
                Loves = L("Something fun... ehehe, I'd like that a lot.", "YES. Anything! Save me from this boredom!", "...I'm listening.", "Now you're talking. Entertain me."),
                Fine = L("Maybe... if I'm not too tired.", "Sure, what'd you have in mind?", "...Depends.", "Impress me first."),
                Dislikes = L("I have to go straight home today...", "Ehh, I've got stuff today.", "Not today.", "Pass."),
                Replies = new[]
                {
                    R("Suggest the arcade by the station", "arcade", L("The arcade? I'm bad at games... but okay!", "Arcade! I'll crush you at the crane game!", "...I'm good at rhythm games. Prepare.", "Arcade date? I accept."), 1),
                    R("Ask where she likes to hang out", "where", L("Um... {fact:place}. It's quiet there.", "{fact:place}! It's the best spot!", "...{fact:place}.", "{fact:place}. Take me there sometime."), 0, "place"),
                    R("Suggest studying together instead", "study", L("Studying...? Well, it's not bad, with you.", "Booooring! ...Fine, with snacks.", "...That's actually useful.", "Study date. How romantic."), -1),
                } });
            c.Add(new TalkSubject { Id = "m_lonely", Topic = Topic.AboutHer, Label = "Say you were hoping to see her",
                Opener = new[] { "I was kind of hoping I'd run into you today." },
                Loves = L("R-really? ...I was hoping too. A little.", "Me too! I looked for you at lunch!", "...I'm glad you did.", "Oh? You were looking for me? Cute."),
                Fine = L("Oh... that's nice.", "Haha, well here I am!", "...I see.", "Hm. Good timing, then."),
                Dislikes = L("Um... why?", "Huh? What for?", "...Why.", "Need something?"),
                Replies = new[]
                {
                    R("Ask if she wants to walk together later", "walk", L("Yes... I'd like that.", "Yes! Wait for me!", "...Okay.", "If you insist."), 1),
                    R("Ask what she's been up to lately", "lately", L("Not much... I've been reading {fact:book} a lot.", "Listening to {fact:music} nonstop! You should try it!", "...Reading. {fact:book}.", "Thinking about you. Kidding. ...Maybe."), 0, "book"),
                } });

            // ---- the place ----
            c.Add(new TalkSubject { Id = "p_library", Topic = Topic.Hobbies, Label = "Ask what she's reading", Like = "study", UsesFact = "book",
                Opener = new[] { "What are you reading? It looked good." },
                Loves = L("Oh, this? It's {fact:book}... I could talk about it all day.", "{fact:book}! The last chapter had me SCREAMING!", "...{fact:book}. It's well written.", "{fact:book}. Want me to read you the good part?"),
                Fine = L("Just something for class...", "Eh, something boring for class.", "...Assignments reading.", "Nothing exciting."),
                Dislikes = L("It's... a little private.", "Nothing! Don't look!", "...None of your business.", "Curious, aren't you?"),
                Replies = new[]
                {
                    R("Ask her to recommend you one", "rec", L("Really? Then... I'll lend you my favorite. Take care of it.", "I'll bring you a whole stack tomorrow!", "...Start with this one. Don't skip the prologue.", "I'll pick one for you. You'll love it or I'll mock you."), 1, "book"),
                    R("Admit you only read manga", "manga", L("Manga is reading too! I like it as well.", "Same! Manga gang!", "...Manga counts. Some.", "Picture books for grown-ups. Cute."), 0),
                    R("Say the library is too quiet for you", "quiet", L("Ah... I like the quiet, though.", "Right?! I keep getting shushed!", "...Then don't come.", "Then let's be noisy somewhere else."), -1),
                } });
            c.Add(new TalkSubject { Id = "p_sport", Topic = Topic.Sports, Label = "Ask how training is going", Like = "exercise",
                Opener = new[] { "How's training going? You looked serious out there." },
                Loves = L("It's going well! I beat my best time today...", "AMAZING! New personal record! Wanna see?!", "...I improved. A little.", "I'm unstoppable. You were watching, huh?"),
                Fine = L("It's okay... my legs hurt, though.", "Meh! Could be better!", "...Average.", "It's fine. Sweaty, but fine."),
                Dislikes = L("I'm not really a sports person...", "Ugh, don't remind me!", "...I'd rather not.", "Training? Me? Please."),
                Replies = new[]
                {
                    R("Offer to train with her tomorrow", "train", L("With me? I'm slow, but... okay!", "Yes! I'll go easy on you! Not really!", "...Keep up.", "Training partner? Prepare to suffer."), 1),
                    R("Bring her a sports drink next time", "drink", L("Thank you... that's really sweet.", "You're the best manager ever!", "...Appreciated.", "Spoiling me already?"), 1),
                    R("Joke that watching is your sport", "watch", L("Ehh? You were watching...?", "Hey! Get out here and run!", "...Lazy.", "Enjoying the view, were you?"), -1, null, 1),
                } });
            c.Add(new TalkSubject { Id = "p_cafe", Topic = Topic.Food, Label = "Ask what's good on the menu today", Like = "food", UsesFact = "food",
                Opener = new[] { "What's good today? I trust your taste." },
                Loves = L("Today? The {fact:food} is really good here, honestly.", "{fact:food}! Run, before it's gone!", "...{fact:food}. Trust me.", "{fact:food}. You're buying me one too, right?"),
                Fine = L("Um... the daily set is okay.", "Anything! It's all food!", "...The set meal.", "Whatever's cheapest."),
                Dislikes = L("I brought my own, sorry...", "No idea! I just grab bread!", "...Don't know.", "Ask the lunch lady."),
                Replies = new[]
                {
                    R("Buy her one too", "buy", L("Eh? You don't have to... thank you.", "Yes! Best lunch ever!", "...Thanks. I owe you.", "A gentleman. I'll allow it."), 1, "food"),
                    R("Ask if she can cook it herself", "cook", L("A little... maybe I'll make you some one day.", "Me? Cook? I burn water!", "...I can. Don't spread it.", "Fishing for a homemade lunch?"), 0),
                } });
            c.Add(new TalkSubject { Id = "p_roof", Topic = Topic.AboutHer, Label = "Say the view is better with company",
                Opener = new[] { "The view up here's nice. Better with company, though." },
                Loves = L("...Yeah. I think so too.", "Right?! And the wind's amazing today!", "...Agreed.", "Smooth. ...But true."),
                Fine = L("It's nice up here...", "Yeah! Super windy though!", "...It's alright.", "Mm-hm."),
                Dislikes = L("I came up here to be alone, actually...", "Eh? I was about to go down.", "...I prefer it quiet.", "Was that a line?"),
                Replies = new[]
                {
                    R("Ask what her favorite season is up here", "season", L("{fact:season}... the sky is prettiest then.", "{fact:season}! Obviously!", "...{fact:season}.", "{fact:season}. Remember that."), 0, "season"),
                    R("Stand quietly beside her for a while", "quiet", L("...This is nice.", "...Hehe. Okay, this is nice.", "...Mm.", "...Don't get used to this."), 1),
                } });
            c.Add(new TalkSubject { Id = "p_class", Topic = Topic.Classes, Label = "Ask if she followed today's lesson", Like = "study",
                Opener = new[] { "Did you follow today's lesson? I got lost halfway." },
                Loves = L("Mm, I think so. Want me to go over it with you?", "Totally! Want the cheat sheet version?", "...Of course. Which part?", "Lost? Poor thing. I'll explain, for a price."),
                Fine = L("Kind of... the last part was hard.", "Half of it! The other half was a nap.", "...Mostly.", "I followed the clock. Does that count?"),
                Dislikes = L("Please don't talk about class...", "Ugh, campus talk? Pass!", "...Not now.", "Boring. Next."),
                Replies = new[]
                {
                    R("Ask her to explain it after class", "explain", L("Okay! I'll bring my notes.", "Study session! With snacks!", "...Thirty minutes. Bring a pen.", "Private lesson. I'll be strict."), 1),
                    R("Ask what she wants to be after class", "dream", L("I want to be {fact:dream}... it's embarrassing to say.", "{fact:dream}! Big dreams!", "...{fact:dream}.", "{fact:dream}. Laugh and you die."), 0, "dream"),
                } });
            c.Add(new TalkSubject { Id = "p_yard", Topic = Topic.Hobbies, Label = "Point out the flowers by the bench",
                Opener = new[] { "Look, the flowers by the bench are blooming." },
                Loves = L("Oh, they're so pretty! I check on them every morning...", "Whoa, pretty! Let's take a picture!", "...They're beautiful. Someone takes good care of them.", "Bringing me to see flowers? How romantic."),
                Fine = L("Oh... they are, aren't they.", "Huh! Didn't notice!", "...They're flowers.", "Mm. Pretty enough."),
                Dislikes = L("Achoo... I'm a little allergic, sorry...", "Bugs live in there! No thanks!", "...Pollen. Pass.", "Flowers? Really?"),
                Replies = new[]
                {
                    R("Ask if she'd like some for her desk", "pick", L("You'd pick some for me? ...Thank you.", "Yes! My desk needs color!", "...Just one. Don't hurt the plant.", "Giving me flowers already?"), 1),
                    R("Ask if she has a secret she's never told anyone", "secret", L("A secret...? Well... I {fact:secret}. Don't tell!", "Okay, but shh! I {fact:secret}!", "...I {fact:secret}. Forget it.", "A secret for a secret. ...I {fact:secret}. Your turn later."), 0, "secret", 0, 2),
                } });
            c.Add(new TalkSubject { Id = "p_clubroom", Topic = Topic.Club, Label = "Ask what the club is working on",
                When = h => !GirlInfo.InNoClub(h),
                Opener = new[] { "What's the club working on these days?" },
                Loves = L("Oh! We're preparing something for the festival... want to hear?", "Big project! Super secret! Okay I'll tell you!", "...A project for the festival. It's going well.", "Top secret. ...Fine, I'll tell you."),
                Fine = L("The usual things...", "Eh, same old!", "...Routine stuff.", "Nothing exciting."),
                Dislikes = L("Um, it's a bit messy right now...", "Don't ask! It's a disaster!", "...I'd rather not say.", "Spy much?"),
                Replies = new[]
                {
                    R("Offer to help the club out", "help", L("Really? Everyone would be so happy!", "New member!! Kidding! But yes, help!", "...We could use hands.", "Free labor? Accepted."), 1),
                    R("Ask why she joined", "why", L("Because I love it... and the people are kind.", "Because it's FUN!", "...It suits me.", "Because I'm good at it. Obviously."), 0),
                } });
            c.Add(new TalkSubject { Id = "club_practice", Topic = Topic.Club, Label = "Ask how {club} practice went",
                When = h => !GirlInfo.InNoClub(h),
                Opener = new[] { "How did {club} practice go?" },
                Loves = L("It went well! The seniors praised me today...", "AWESOME! I nailed it today!", "...Well. Better than last week.", "Brilliantly, of course."),
                Fine = L("Normal, I think...", "Okay-ish!", "...Normal.", "Fine."),
                Dislikes = L("Not great... I messed up.", "Ugh, don't ask!", "...Badly.", "Next question."),
                Replies = new[]
                {
                    R("Say you'd like to watch her practice", "watch", L("Eh... that's embarrassing... but okay.", "Come cheer for me!", "...Don't distract me.", "Come watch. Bring snacks."), 1),
                    R("Tell her everyone has bad days", "bad", L("...Thank you. That helps.", "You're right! Tomorrow I'll win!", "...True.", "Comforting me? Sweet."), 1),
                } });

            // ---- what she likes (you found out) ----
            c.Add(new TalkSubject { Id = "like_animals", Topic = Topic.Hobbies, Like = "animals", Label = "Tell her about the stray cat by the gate",
                Opener = new[] { "There's a stray cat by the gate. It let me pet it today." },
                Loves = L("Really?! The calico one? I always leave it treats...", "WHAT. Take me there right now!", "...The calico? It never lets me near.", "It let YOU pet it? Unfair."),
                Fine = L("Oh, a cat? That's nice.", "Cute!", "...Okay.", "Lucky you."),
                Dislikes = L("I'm a little scared of cats...", "Cats scratch!", "...Allergic.", "Pass."),
                Replies = new[]
                {
                    R("Ask if she has a pet at home", "pet", L("No... but I'd love {fact:animal} someday.", "Not yet! But {fact:animal}! Someday!", "...I want {fact:animal}.", "{fact:animal}. Buy me one."), 0, "animal"),
                    R("Invite her to feed it with you after class", "feed", L("Yes! I'll bring treats!", "Date with a cat! Yes!", "...Okay. Quietly.", "A date with a cat as chaperone? Fine."), 1),
                } });
            c.Add(new TalkSubject { Id = "like_sweets", Topic = Topic.Food, Like = "sweets", Label = "Ask if she's tried the new parfait place",
                Opener = new[] { "Have you tried the new parfait place by the station?" },
                Loves = L("Not yet! I've been dying to go...", "NO. Is it good?! Tell me everything!", "...I've walked past it four times.", "Are you inviting me? Sounds like an invitation."),
                Fine = L("I heard about it...", "Ooh, maybe!", "...Not yet.", "Haven't. Should I?"),
                Dislikes = L("I'm watching my weight...", "Too sweet for me!", "...Pass.", "Trying to fatten me up?"),
                Replies = new[]
                {
                    R("Invite her there this weekend", "go", L("Really?! Then... it's a promise.", "YES! It's a date! A parfait date!", "...Saturday. Don't be late.", "A date. Finally. You're paying."), 1),
                    R("Ask her favorite sweet", "fav", L("{fact:food}... always.", "{fact:food}! The king of sweets!", "...{fact:food}.", "{fact:food}. Write it down."), 0, "food"),
                } });
            c.Add(new TalkSubject { Id = "like_food", Topic = Topic.Food, Like = "cooking", Label = "Ask for her best bento recipe",
                Opener = new[] { "Your bento always looks amazing. What's your secret?" },
                Loves = L("Ehehe... I get up early and make it myself. The secret is love! ...Kidding!", "I make it myself! The secret is MORE FRIED STUFF!", "...Practice. And a good knife.", "My secret? I'll tell you if you taste-test."),
                Fine = L("It's nothing special...", "Thanks! It's mom's!", "...It's simple.", "It's just food."),
                Dislikes = L("It's from the convenience store, actually...", "Hah! Store bought!", "...I didn't make it.", "Don't look at my lunch."),
                Replies = new[]
                {
                    R("Ask her to make you one someday", "make", L("F-for you? ...I'll think about it.", "Sure! One mega bento coming up!", "...If you clean the box after.", "A homemade lunch? We'd be practically married."), 1, null, 1),
                    R("Ask what she'd cook for a special day", "special", L("Hmm... {fact:food}, I think.", "{fact:food}! A mountain of it!", "...{fact:food}.", "{fact:food}. For the right person."), 0, "food"),
                } });
            c.Add(new TalkSubject { Id = "like_exercise", Topic = Topic.Sports, Like = "exercise", Label = "Challenge her to a run after class",
                Opener = new[] { "Race you around the field after class?" },
                Loves = L("A race? Okay! I won't lose...", "YOU'RE ON! Loser buys drinks!", "...You'll regret this.", "Prepare to eat my dust."),
                Fine = L("Maybe a short one...", "Sure, why not!", "...Fine.", "If I'm bored."),
                Dislikes = L("I'm not good at running...", "Today? My legs are dead!", "...No.", "Sweat? No thanks."),
                Replies = new[]
                {
                    R("Bet the loser buys drinks", "bet", L("Then I'll try my best!", "DEAL! Get your wallet ready!", "...Deal.", "Deal. Hope you're rich."), 1),
                    R("Say you'll let her win", "let", L("Don't you dare go easy on me!", "Boo! Race me for real!", "...Insulting.", "Let me win? Cute. You'd lose anyway."), -1),
                } });
            c.Add(new TalkSubject { Id = "like_study", Topic = Topic.Classes, Like = "study", Label = "Ask her to explain the assignments",
                Opener = new[] { "You're good at this stuff. Can you explain today's assignments?" },
                Loves = L("Of course! Look, it's easier if you start here...", "Leave it to me! Professor mode ON!", "...Sit. Watch carefully.", "Tutoring fee: one juice. Okay, look."),
                Fine = L("I can try...", "Sure, I think I get it!", "...Fine. Quickly.", "Hm. Maybe."),
                Dislikes = L("I didn't get it either...", "No idea! Copy someone else!", "...Do your own assignments.", "Lazy, are we?"),
                Replies = new[]
                {
                    R("Ask what she wants to study later", "future", L("I want to be {fact:dream}... so I'm studying hard.", "{fact:dream}! That's the plan!", "...{fact:dream}.", "{fact:dream}. Ambitious, right?"), 0, "dream"),
                    R("Thank her with a juice", "juice", L("Ehehe... thank you.", "Payment accepted!", "...Fair.", "Good. You learn fast."), 1),
                } });
            c.Add(new TalkSubject { Id = "like_fashion", Topic = Topic.Hobbies, Like = "fashion", Label = "Ask where she got her hair clip",
                Opener = new[] { "Where did you get that hair clip? It suits you." },
                Loves = L("You noticed? I found it at a little shop downtown...", "You noticed!! It's new! Isn't it cute?!", "...A shop downtown. Thank you.", "Noticing details. I like that."),
                Fine = L("Oh, this? It's old...", "Ah, this? Random shop!", "...Somewhere.", "Don't remember."),
                Dislikes = L("Is it weird...?", "Huh? It's just a clip.", "...Why.", "Odd thing to notice."),
                Replies = new[]
                {
                    R("Ask her to take you shopping sometime", "shop", L("Really? I'd love to pick clothes for you...", "Shopping trip! Makeover time!", "...You need it.", "I'll dress you up. You'll have no say."), 1),
                    R("Say anything looks good on her", "any", L("Th-that's too much...", "Hehe, smooth!", "...Flatterer.", "I know. But say it again."), 0, null, 1),
                } });
            c.Add(new TalkSubject { Id = "like_coffee", Topic = Topic.Food, Like = "coffee", Label = "Ask her which canned coffee is best",
                Opener = new[] { "Settle this: which canned coffee from the machine is the best?" },
                Loves = L("The black one, of course! The others are too sweet...", "The black one! Sugar is for quitters!", "...Black. No question.", "Black. Like my heart."),
                Fine = L("The café au lait, maybe?", "The one with the cat on it!", "...They're all the same.", "Whichever you're buying."),
                Dislikes = L("I can't drink coffee...", "Bitter! Gross!", "...Tea.", "Coffee? Pass."),
                Replies = new[]
                {
                    R("Buy two and drink it with her", "buy", L("Thank you... it tastes better together.", "Coffee buddies!", "...Thanks.", "Coffee date. Cheap, but cute."), 1),
                    R("Admit you put five sugars in yours", "sugar", L("Five?! That's... cute, actually.", "FIVE?! Monster!", "...Barbaric.", "Five? You're a child."), 0),
                } });
            c.Add(new TalkSubject { Id = "like_spicy", Topic = Topic.Food, Like = "spicy", Label = "Dare her to try the extra-hot curry",
                Opener = new[] { "The cafeteria has an extra-hot curry now. I bet you can't finish it." },
                Loves = L("Hehe... I can, you know. Want to see?", "CHALLENGE ACCEPTED!", "...Watch me.", "Bet? I'll take your money."),
                Fine = L("Maybe a little...", "Ooh, maybe!", "...Possibly.", "Hm. Tempting."),
                Dislikes = L("I can't handle spicy...", "My mouth would explode!", "...No.", "I'm not stupid."),
                Replies = new[]
                {
                    R("Loser does the winner's assignments", "bet", L("Then... I'll do my best!", "You're ON!", "...Deal.", "Deal. Start on my assignments now."), 1),
                    R("Say you'll bring milk just in case", "milk", L("Thank you... you're thoughtful.", "Hah! I won't need it!", "...Smart.", "Planning my rescue? Cute."), 0),
                } });

            // ---- asking about her (teaches facts) ----
            c.Add(new TalkSubject { Id = "ask_free", Topic = Topic.Hobbies, Label = "Ask what she does on her days off",
                Opener = new[] { "What do you do on your days off?" },
                Loves = L("Mostly I read... {fact:book}, lately.", "Music! {fact:music} all day! And snacks!", "...I read. {fact:book}.", "Days off? Secret. ...Okay, I listen to {fact:music}."),
                Fine = L("Nothing much... I stay home.", "Sleep! Lots of sleep!", "...Nothing special.", "Things."),
                Dislikes = L("Um... why do you ask?", "That's private!", "...Why.", "Stalker?"),
                Replies = new[]
                {
                    R("Ask what she's into these days", "into", L("{fact:book}... and {fact:music}, when I study.", "{fact:music}! You HAVE to listen!", "...{fact:book}.", "{fact:music}. Judge me and die."), 0, "music"),
                    R("Say you'd like to join her someday", "join", L("Eh? ...Then, someday.", "Yes! Come over! Kidding! ...Sort of!", "...Maybe.", "Inviting yourself? Bold."), 1),
                } });
            c.Add(new TalkSubject { Id = "ask_place", Topic = Topic.Weekend, Label = "Ask where her favorite place in town is",
                Opener = new[] { "Where's your favorite place in town?" },
                Loves = L("{fact:place}... I go there when I want to think.", "{fact:place}! It's the BEST!", "...{fact:place}.", "{fact:place}. Not that I'd take just anyone."),
                Fine = L("Hmm... home, I guess.", "Everywhere! The whole town!", "...Home.", "Wherever I'm not bored."),
                Dislikes = L("I don't go out much...", "Eh? Dunno!", "...Nowhere.", "Why, planning something?"),
                Replies = new[]
                {
                    R("Ask her to show you someday", "show", L("Then... someday. Just us.", "Totally! Let's go!", "...Someday.", "Maybe. If you behave."), 1, "place"),
                    R("Tell her about yours", "mine", L("Ehehe... I'd like to see it too.", "Ooh, take me there!", "...I'll remember.", "Hm. Interesting."), 0, "place"),
                } });
            c.Add(new TalkSubject { Id = "ask_animal", Topic = Topic.Hobbies, Label = "Ask if she's a cat person or a dog person",
                Opener = new[] { "Important question: cat person or dog person?" },
                Loves = L("Ehehe... {fact:animal}. Definitely.", "{fact:animal}!! No contest!", "...{fact:animal}.", "{fact:animal}. And you'd better agree."),
                Fine = L("Both are cute...", "Both! All of them!", "...Neither.", "Depends on the day."),
                Dislikes = L("I'm a little scared of animals...", "Neither! They bite!", "...Neither.", "People person. Sometimes."),
                Replies = new[]
                {
                    R("Agree with her", "agree", L("Hehe, I knew it.", "High five!", "...Good.", "Correct answer."), 1, "animal"),
                    R("Pick the other one, to tease her", "tease", L("Eh?! We'll have to talk about this...", "WHAT?! Traitor!", "...Wrong.", "We can't be friends anymore. ...Kidding."), 0, "animal"),
                } });

            // ---- what you learned (later conversations) ----
            c.Add(new TalkSubject { Id = "fact_food", Topic = Topic.Food, NeedsFact = "food", UsesFact = "food", Label = "Tell her you found a place with great {fact:food}",
                Opener = new[] { "I found a place near the station that makes really good {fact:food}." },
                Loves = L("Really?! You remembered I love {fact:food}...", "WHERE?! Take me!!", "...You remembered. ...Where is it?", "You remembered? Careful, I might fall for you."),
                Fine = L("Oh, that sounds nice...", "Ooh, nice!", "...Noted.", "Hm. Good."),
                Dislikes = L("I'm on a diet right now...", "Not hungry today!", "...Not now.", "Later."),
                Replies = new[]
                {
                    R("Promise to take her this weekend", "take", L("It's a promise, then...", "Yes! It's a date!", "...Saturday.", "A date over {fact:food}. Perfect."), 1),
                    R("Say you'll bring her some tomorrow", "bring", L("Thank you... I'll look forward to it.", "You're the best!", "...Thanks.", "Spoiling me. Keep it up."), 1),
                } });
            c.Add(new TalkSubject { Id = "fact_book", Topic = Topic.Hobbies, NeedsFact = "book", UsesFact = "book", Label = "Say the bookstore got new {fact:book}",
                Opener = new[] { "The bookstore by the station got a new shelf of {fact:book}. I thought of you." },
                Loves = L("You thought of me...? Ehehe... let's go see together!", "NO WAY. After class! Let's go!", "...You remembered. Thank you.", "Thinking of me in a bookstore? Cute."),
                Fine = L("Oh, I'll check it out...", "Huh, cool!", "...I'll look.", "Mm. Thanks."),
                Dislikes = L("I'm saving money right now...", "Broke! Sadly!", "...Not now.", "Later."),
                Replies = new[]
                {
                    R("Offer to buy her the first one", "buy", L("I-I couldn't! ...But thank you...", "Really?! You're an angel!", "...I'll pay you back.", "A gift? I'll treasure it."), 1),
                    R("Ask her to tell you her favorite one", "fav", L("Then I'll lend it to you! You'll love it!", "Okay, so there's this one where-", "...I'll lend it. Read it properly.", "I'll tell you. Over tea."), 1),
                } });
            c.Add(new TalkSubject { Id = "fact_animal", Topic = Topic.Hobbies, NeedsFact = "animal", UsesFact = "animal", Label = "Show her a video of {fact:animal}",
                Opener = new[] { "Look at this video. I saved it because I know you love {fact:animal}." },
                Loves = L("Ahh, so cute...! Send it to me, please!", "KYAAA! Play it again! Again!", "...Again.", "You saved it for me? ...Play it again."),
                Fine = L("Hehe, cute.", "Aww!", "...Cute.", "Mm."),
                Dislikes = L("Not now, the professor's looking...", "Later! Phone police!", "...Later.", "Not now."),
                Replies = new[]
                {
                    R("Promise to find a {fact:animal} café together", "cafe", L("A date with {fact:animal}...? I'd love that.", "A {fact:animal} café?! YES!", "...Yes.", "Combining my two favorite things. Clever."), 1),
                    R("Send her the video", "send", L("Thank you! I'll watch it before bed.", "Got it! Thanks!", "...Thanks.", "Thanks. I'll think of you watching it."), 0),
                } });
            c.Add(new TalkSubject { Id = "fact_music", Topic = Topic.Weekend, NeedsFact = "music", UsesFact = "music", Label = "Share a {fact:music} playlist with her",
                Opener = new[] { "I made a playlist of {fact:music}. Want half my earphones?" },
                Loves = L("Yes... hehe, our heads are so close like this...", "Gimme! This song is the BEST!", "...Good taste.", "Sharing earphones? That's intimate."),
                Fine = L("Oh, thanks...", "Cool!", "...Okay.", "Sure."),
                Dislikes = L("I don't like earphones much...", "Later!", "...No.", "Not now."),
                Replies = new[]
                {
                    R("Stay like that until the bell", "stay", L("...Okay. Until the bell.", "Yeah! Until the bell!", "...Mm.", "...Fine. Until the bell."), 1, null, 1),
                    R("Ask her to add her favorite song", "add", L("Then this one... it's special to me.", "This one! Trust me!", "...This one.", "This one. It's about you. Kidding."), 1),
                } });
            c.Add(new TalkSubject { Id = "fact_dream", Topic = Topic.AboutHer, NeedsFact = "dream", UsesFact = "dream", Label = "Ask how her plan to be {fact:dream} is going",
                Opener = new[] { "So, future {fact:dream}. How's it going?" },
                Loves = L("You remembered...? I'm working hard. Thank you for asking.", "You remembered!! It's going great!", "...You remembered. ...It's going well.", "Remembering my dreams? You're getting serious."),
                Fine = L("Slowly...", "One step at a time!", "...Slowly.", "It's going."),
                Dislikes = L("Please don't... it's embarrassing.", "Shh! Not so loud!", "...Drop it.", "Don't tease me about it."),
                Replies = new[]
                {
                    R("Tell her you believe in her", "believe", L("...Thank you. That means a lot.", "You're the best! I'll do it!", "...I'll remember that.", "...Don't make me emotional."), 1, null, 0, 3),
                    R("Ask what she'd do if it doesn't work out", "fail", L("I... don't want to think about that.", "It WILL work out!", "...I have a backup.", "Way to kill the mood."), -1),
                } });

            // ---- heart: sweet talk, by how close you are ----
            c.Add(new TalkSubject { Id = "r_time", Topic = Topic.Love, Tier = 0, Label = "Say you like spending time with her",
                Opener = new[] { "I like spending time with you. Just so you know." },
                Loves = L("E-eh? ...Me too. I like it too.", "Hehe! Same! You're fun!", "...I don't mind your company.", "Oh? Is that a confession? ...I'm teasing."),
                Fine = L("Oh... thank you.", "Haha, thanks!", "...Okay.", "Hm."), Dislikes = L("Um... that's sudden...", "Whoa, where'd that come from?", "...Why.", "Weird thing to say."),
                Replies = new[]
                {
                    R("Ask if she'd hang out after class", "after", L("...Yes. I'd like that.", "Yes! Where to?", "...Fine.", "Pushing your luck. ...Yes."), 1),
                    R("Say it with a straight face, then grin", "grin", L("Mou... you're teasing me!", "Hey! Hehe!", "...Idiot.", "Two can play that game."), 0),
                } });
            c.Add(new TalkSubject { Id = "r_type", Topic = Topic.Love, Tier = 0, Label = "Ask what her type is",
                Opener = new[] { "Hey, what's your type? Just curious." },
                Loves = L("M-my type...? Someone kind... and who listens. Like... um, nevermind!", "Someone fun! Who makes me laugh! Like- ANYWAY!", "...Someone dependable. Why?", "My type? ...Someone who asks questions like that."),
                Fine = L("I don't really know...", "Dunno! Someone cool!", "...Haven't thought about it.", "Wouldn't you like to know."),
                Dislikes = L("That's private...", "Nope! Not telling!", "...None of your business.", "Too early for that question."),
                Replies = new[]
                {
                    R("Ask if you're close", "close", L("...Maybe. A little. Don't make me say it!", "Hehe... maybe!", "...Not far.", "Closer than you think."), 1, null, 1),
                    R("Say yours is someone like her", "mine", L("E-eh?! You can't just say that!", "Wha-! Hehe... smooth!", "...Hm.", "Bold. I like bold."), 1, null, 1),
                } });
            c.Add(new TalkSubject { Id = "r_mind", Topic = Topic.Love, Tier = 1, Label = "Tell her she's been on your mind",
                Opener = new[] { "You've been on my mind a lot lately." },
                Loves = L("...You too. I mean, you've been on mine too.", "Me too! Is that weird?!", "...Same.", "Took you long enough to say it."),
                Fine = L("Oh... I see...", "Haha, really?", "...Hm.", "Is that so."),
                Dislikes = L("I-I don't know what to say...", "Whoa, intense!", "...That's a lot.", "Slow down."),
                Replies = new[]
                {
                    R("Ask if you can walk her home", "home", L("...Yes. Please.", "Yes! Escort me!", "...Okay.", "Walking me home? Cliché. ...Yes."), 1),
                    R("Look at her a moment too long", "look", L("...W-what? Is something on my face?", "Wh-what? Why are you staring?!", "...Stop.", "...Like what you see?"), 0, null, 2),
                } });
            c.Add(new TalkSubject { Id = "r_firstlove", Topic = Topic.Love, Tier = 1, Label = "Ask about her first love",
                Opener = new[] { "Who was your first love?" },
                Loves = L("Um... a boy in elementary school. He gave me his umbrella once...", "A boy who could whistle REALLY loud! I was eight!", "...A character in a book. Don't laugh.", "Hm. Maybe it hasn't happened yet. Or maybe it's happening."),
                Fine = L("I don't remember...", "No idea! Never had one!", "...Pass.", "Secret."),
                Dislikes = L("That's embarrassing...", "No way I'm telling you!", "...No.", "Nice try."),
                Replies = new[]
                {
                    R("Say you hope it's not over", "hope", L("...Maybe it's not.", "Hehe... maybe not!", "...We'll see.", "Fishing, are we?"), 1, null, 1),
                    R("Tell her about yours", "yours", L("Ehehe... that's cute.", "Pfft! Cute!", "...Huh.", "Hm. Lucky girl."), 0),
                } });
            c.Add(new TalkSubject { Id = "r_future", Topic = Topic.Love, Tier = 2, Label = "Ask if she thinks about the future, with you",
                Opener = new[] { "Do you ever think about the future? Like, with me in it?" },
                Loves = L("...All the time. Is that bad?", "Yes! A lot! Is that weird?!", "...Sometimes. More than I'd admit.", "Every night. Happy?"),
                Fine = L("Sometimes...", "Maybe!", "...Occasionally.", "Hm. Sometimes."),
                Dislikes = L("That's... too soon to think about.", "Whoa, slow down!", "...Too early.", "Getting ahead of yourself."),
                Replies = new[]
                {
                    R("Tell her you'd like that future", "yes", L("...Me too.", "Then let's make it happen!", "...Then I'll hold you to it.", "Then don't let me go."), 1, null, 2),
                    R("Squeeze her hand", "hand", L("...Your hand is warm.", "Hehe... warm!", "...Mm.", "...Don't let go."), 1, null, 2),
                } });
            c.Add(new TalkSubject { Id = "r_love", Topic = Topic.Love, Tier = 3, Label = "Tell her you love her, out of nowhere",
                Opener = new[] { "I love you. That's all. I just wanted to say it." },
                Loves = L("...I love you too. So much.", "I love you too! Hehe, so much!", "...I know. ...Me too.", "Say it again. Slower."),
                Fine = L("Ehehe... thank you.", "Hehe! Me too!", "...Mm.", "I know."),
                Dislikes = L("Here...? People are looking...", "Shh! People can hear!", "...Not here.", "Not in public, idiot."),
                Replies = new[]
                {
                    R("Steal a quick kiss", "kiss", L("Ah...! ...Do it again.", "Hehe! Sneaky!", "...Again.", "That was too short."), 1, null, 4),
                    R("Plan a date this Sunday", "date", L("Yes! I'll think of somewhere nice!", "Sunday date! Yes!", "...Sunday. It's a plan.", "Sunday. Wear something nice."), 1),
                } });

            // ---- heart: flirting, by how lewd she is ----
            c.Add(new TalkSubject { Id = "x_uniform", Topic = Topic.Love, Tier = 0, Lewd = true, LewdGain = 3, Label = "Tease her that her uniform looks good on her",
                Opener = new[] { "Is it just me, or does the uniform look better on you than anyone?" },
                Loves = L("E-eh?! D-don't look so closely...", "Hehe! I do wear it well, huh?", "...Pervert. ...Thank you.", "Noticed, did you? I adjusted the skirt."),
                Fine = L("It's just a uniform...", "Haha, okay!", "...It's a uniform.", "Mm-hm."),
                Dislikes = L("That's... a little creepy...", "Eww! Where are you looking?!", "...Gross.", "Eyes up here."),
                Replies = new[]
                {
                    R("Say the ribbon's crooked and fix it", "ribbon", L("Ah... th-thank you... your hands...", "Hey! ...Thanks.", "...Thanks. Too close.", "...You did that on purpose."), 1, null, 3),
                    R("Back off and laugh it off", "laugh", L("Mou... you're teasing me again.", "Hehe, jerk!", "...Idiot.", "Coward. ...Cute coward."), 0),
                } });
            c.Add(new TalkSubject { Id = "x_night", Topic = Topic.Love, Tier = 1, Lewd = true, LewdGain = 5, Label = "Ask what she thinks about before falling asleep",
                Opener = new[] { "What do you think about before you fall asleep?" },
                Loves = L("...Wh-why would you ask that? ...Things. Embarrassing things.", "Hehe... secret things! ...Okay, some are about you.", "...Things I won't say out loud.", "You. Obviously. In great detail."),
                Fine = L("Assignments, mostly...", "Food!", "...Tomorrow.", "Sleep."),
                Dislikes = L("That's too personal...", "Nope! Not telling!", "...No.", "Creepy question."),
                Replies = new[]
                {
                    R("Ask for details", "details", L("N-no way! ...Maybe later.", "Hehe! You'll have to earn it!", "...Not here.", "Details? Come closer and I'll whisper."), 1, null, 4),
                    R("Say you think about her too", "too", L("...Really? ...Good.", "Hehe! Good!", "...Hm. Good.", "I know. I can tell."), 1, null, 2),
                } });
            c.Add(new TalkSubject { Id = "x_whisper", Topic = Topic.Love, Tier = 2, Lewd = true, LewdGain = 7, Label = "Whisper what you'd like to do after class",
                Opener = new[] { "(You lean in and whisper what you'd like to do with her after class.)" },
                Loves = L("...! Y-you can't say that here... ...After class, then.", "WHA-! ...Hehe... after class. Promise?", "...Lower your voice. ...Yes.", "Only after class? I was thinking sooner."),
                Fine = L("Eh? I didn't catch that...", "Huh? Say again?", "...What?", "Mumbling?"),
                Dislikes = L("D-don't say things like that!", "Hey! Too much!", "...Disgusting.", "Not here, idiot."),
                Replies = new[]
                {
                    R("Brush her hand under the desk", "brush", L("...Ah... d-don't stop...", "Hehe... sneaky...", "...Keep going.", "...Under the desk. Naughty."), 1, null, 5),
                    R("Pull back and act innocent", "innocent", L("M-mou...! Don't leave me like this...", "Hey! You can't just stop!", "...Coward.", "Teasing me? You'll pay for that."), 0, null, 3),
                } });
            c.Add(new TalkSubject { Id = "x_lasttime", Topic = Topic.Love, Tier = 2, Lewd = true, LewdGain = 8, Label = "Remind her of last time, quietly",
                When = h => h.hCount > 0,
                Opener = new[] { "I keep thinking about last time. You were amazing." },
                Loves = L("...Me too. I can't stop thinking about it...", "Hehe... me too. A lot. Like, a LOT.", "...Don't. I'll blush in class.", "Just amazing? I'll do better next time."),
                Fine = L("...Shh.", "Hehe, shh!", "...Not here.", "Hm."),
                Dislikes = L("Please don't talk about that here...", "Shhh! Someone will hear!", "...Not here.", "Discretion, please."),
                Replies = new[]
                {
                    R("Ask when there'll be a next time", "next", L("...Soon. Maybe today...", "Hehe... today?", "...Soon.", "Whenever you find us a quiet room."), 1, null, 5),
                    R("Tell her what you liked most", "liked", L("...! Stop, I'm going to melt...", "Waaah, stop! ...Keep going.", "...Enough. ...No, go on.", "Tell me more. Slowly."), 1, null, 6),
                } });
            c.Add(new TalkSubject { Id = "x_body", Topic = Topic.Love, Tier = 3, Lewd = true, LewdGain = 9, Label = "Tell her exactly what drives you crazy about her",
                Opener = new[] { "(You tell her, in a low voice, exactly what about her drives you crazy.)" },
                Loves = L("...Y-you really think that...? ...Then look as much as you want...", "Hehe... you like that, huh? I'll remember...", "...Hm. I'll keep that in mind. Tonight.", "Only that? I have a list for you too."),
                Fine = L("Mou... you're embarrassing...", "Hehe! Stop!", "...Idiot.", "Mm."),
                Dislikes = L("Not here... please.", "Too much, too much!", "...Not here.", "Save it for later."),
                Replies = new[]
                {
                    R("Suggest somewhere private", "private", L("...Okay. Lead the way.", "Hehe... let's go!", "...Quickly.", "Finally."), 1, null, 6),
                    R("Leave her wanting more", "more", L("M-mean...!", "Noooo, come back!", "...Unfair.", "You'll pay for that tonight."), 0, null, 5),
                } });

            // ---- a teacher's lines (teachers and teachers who are now free) ----
            c.Add(new TalkSubject { Id = "t_lesson", Topic = Topic.Classes, Teacher = true, Label = "Ask her a question about the lesson",
                Opener = new[] { "Sensei, could you explain that last part again?" },
                Loves = L("Of course. Look, it's simpler than it looks...", "Yes! A student who asks! Okay, so-", "...Good question. Sit.", "Finally, someone listening. Come here."),
                Fine = L("Mm, read the textbook first, then ask me.", "After class, okay?", "...Page forty.", "Did you even try?"),
                Dislikes = L("We covered that three times...", "Again?! Okay, okay...", "...Pay attention next time.", "Were you sleeping?"),
                Replies = new[]
                {
                    R("Thank her and promise to study", "thanks", L("Fufu, I'll check, you know.", "I'll quiz you tomorrow!", "...I'll hold you to it.", "Promises, promises."), 1),
                    R("Ask why she became a professor", "why", L("I had a professor who believed in me... I wanted to be that for someone.", "Because campus is the best! So I never left!", "...I like seeing people grow.", "Summer vacations. ...And a professor who changed my life."), 1),
                } });
            c.Add(new TalkSubject { Id = "t_tired", Topic = Topic.AboutHer, Teacher = true, Label = "Tell her she looks tired today",
                Opener = new[] { "You look tired today, sensei. Are you okay?" },
                Loves = L("Ah... you noticed? I was grading until late... thank you for asking.", "Ugh, papers! Mountains of papers! Thanks for asking!", "...Grading. Thank you.", "Worried about your professor? How sweet."),
                Fine = L("I'm alright.", "I'm fine! Totally!", "...Fine.", "Professors don't get tired."),
                Dislikes = L("Is it that obvious...?", "Rude!", "...Thanks.", "Saying I look old?"),
                Replies = new[]
                {
                    R("Offer to help carry the papers", "carry", L("Then... just to the faculty room. Thank you.", "My hero!", "...Accepted.", "A gentleman. I'll allow it."), 1),
                    R("Bring her a canned coffee", "coffee", L("For me? ...It's warm. Thank you.", "Caffeine! Bless you!", "...Exactly what I needed.", "Bribing your professor? ...Thanks."), 1),
                } });
            c.Add(new TalkSubject { Id = "t_weekend", Topic = Topic.Weekend, Teacher = true, Label = "Ask what professors do on weekends",
                Opener = new[] { "What do professors even do on weekends?" },
                Loves = L("Fufu... I bake, mostly. And sleep a lot.", "Karaoke! Don't tell the dean!", "...I go to the museum. Alone. It's nice.", "Things students aren't supposed to know about."),
                Fine = L("Prepare lessons...", "Grading! Fun!", "...Work.", "Rest."),
                Dislikes = L("That's a little private...", "Secret!", "...Private.", "Why, want to follow me?"),
                Replies = new[]
                {
                    R("Say you'd like to see that side of her", "side", L("E-eh? That's... not something a student should say...", "Hehe... maybe someday!", "...Out of the question. ...Maybe.", "Careful. I might let you."), 1, null, 2),
                    R("Tell her she deserves the rest", "rest", L("Thank you... you're kind.", "Right?! I do!", "...I do.", "I know."), 1),
                } });
            c.Add(new TalkSubject { Id = "t_close", Topic = Topic.Love, Teacher = true, Tier = 1, Label = "Tell her you like talking to her",
                Opener = new[] { "I like talking with you, sensei. More than with anyone." },
                Loves = L("...You shouldn't say that to your professor. ...But I'm happy.", "Hehe... me too. Don't tell anyone!", "...I feel the same. Keep it quiet.", "Dangerous words. ...I like them."),
                Fine = L("Fufu, thank you.", "Aww, thanks!", "...Hm.", "Is that so."),
                Dislikes = L("Let's keep things proper, okay?", "Whoa, whoa! Professor, remember?", "...Inappropriate.", "Careful, kid."),
                Replies = new[]
                {
                    R("Ask to see her after class", "after", L("...In the faculty room. Only to talk.", "Okay! Secret meeting!", "...Briefly.", "After class. Don't be late."), 1, null, 2),
                    R("Apologize for being forward", "sorry", L("No, it's alright... really.", "No no, it's fine!", "...It's fine.", "Don't apologize. I liked it."), 0),
                } });
            c.Add(new TalkSubject { Id = "t_flirt", Topic = Topic.Love, Teacher = true, Tier = 2, Lewd = true, LewdGain = 6, Label = "Lean on her desk after class",
                Opener = new[] { "(After class, you lean on her desk, a little too close.)" },
                Loves = L("...Someone might come in... ...lock the door.", "H-hey... the door's open... ...close it?", "...The door. Now.", "Bold. The door, please."),
                Fine = L("Do you need something?", "Yes? What's up?", "...What.", "Hm?"),
                Dislikes = L("Please step back.", "Back up, back up!", "...Step back.", "Down, boy."),
                Replies = new[]
                {
                    R("Close the door", "door", L("...Only for a little while.", "Hehe... okay...", "...Quickly.", "Good boy."), 1, null, 6),
                    R("Step back with a smile", "back", L("M-mou... you're teasing me.", "Hey! Don't tease your professor!", "...Brat.", "Teasing me? You'll regret it."), 0, null, 3),
                } });
            return c;
        }

        // ================= choosing and playing =================

        /// <summary>Whether this subject can be offered to her now.</summary>
        private static bool Offerable(TalkSubject x, SaveData.Heroine h)
        {
            if (x.NeedsFact != null && !Knows(h, x.NeedsFact)) return false;
            if (x.When != null && !x.When(h)) return false;
            // A freed teacher has every normal subject and keeps her own; one still on her event has only hers.
            if (x.Teacher ? !Teachers.IsTeacherVoice(h) : h.isTeacher) return false;
            return true;
        }

        /// <summary>Her tier for the heart: 0 acquaintance, 1 friend, 2 close, 3 girlfriend.</summary>
        private static int HeartTier(SaveData.Heroine h)
        {
            if (h.isGirlfriend) return 3;
            if (h.relation >= 2 || h.favor >= 80) return 2;
            if (h.relation >= 1 || h.favor >= 50) return 1;
            return 0;
        }

        private static int LewdTier(SaveData.Heroine h)
        {
            if (h.isGirlfriend && h.lewdness >= 40) return 3;
            if (h.lewdness >= 60 || h.isGirlfriend) return 2;
            if (h.lewdness >= 30) return 1;
            return 0;
        }

        /// <summary>Heart subjects for her, the closest tiers first, fresh ones before the ones used lately.</summary>
        private static List<TalkSubject> HeartSubjects(SaveData.Heroine h, bool lewd, int count)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var tier = lewd ? LewdTier(h) : HeartTier(h);
            return Catalog.Where(x => x.Tier >= 0 && x.Lewd == lewd && x.Tier <= tier && Offerable(x, h))
                .OrderByDescending(x => (gs.LinesUsed.Contains("s:" + x.Id) ? 0f : 10f) + x.Tier * 2f + Random.value * 3f)
                .Take(count).ToList();
        }

        /// <summary>Talk subjects for her (not heart ones), for "Chat about...".</summary>
        private static List<TalkSubject> TalkSubjects(SaveData.Heroine h, int count)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return Catalog.Where(x => x.Tier < 0 && Offerable(x, h) && !x.Id.StartsWith("m_", StringComparison.Ordinal) && !x.Id.StartsWith("p_", StringComparison.Ordinal))
                .OrderByDescending(x => (gs.LinesUsed.Contains("s:" + x.Id) ? 0f : 5f) + (x.NeedsFact != null ? 3f : 0f) + (x.Like != null && GirlInfo.Likes(h, x.Like) ? 2f : 0f) + Random.value * 3f)
                .Take(count).ToList();
        }

        private static string SubjectLabel(TalkSubject x, SaveData.Heroine h, Session s)
        {
            var label = Fill(x.Label, h);
            if (x.Tier >= 0) label = "<color=#ff8ad8>" + label + "</color>" + (x.Lewd ? Odds(TeaseChance(s)) : FlirtOdds(s));
            if (CanRead(h) && SubjectScore(x, h, s) >= 3) label = "<color=#FFD27A>★</color> " + label;
            return label;
        }

        private static int SubjectScore(TalkSubject x, SaveData.Heroine h, Session s)
        {
            var sc = Score(h, x.Topic, s);
            if (x.Like != null && GirlInfo.Likes(h, x.Like)) sc += 2;
            if (x.UsesFact != null && Knows(h, x.UsesFact)) sc += 1;
            if (x.NeedsFact != null) sc += 2; // you remembered
            if (x.Tier >= 0)
            {
                // Heart: her closeness and mood decide, like the old flirting odds.
                var chance = x.Lewd ? TeaseChance(s) : FlirtChance(s);
                sc = Random.value < chance ? 3 : Random.value < 0.5f ? 1 : -1;
            }
            return sc;
        }

        /// <summary>One turn: you say it, she answers as she feels about it, and you choose how to go on.</summary>
        private static void RunSubject(TalkSubject x)
        {
            var s = Sess;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            gs.LinesUsed.Add("s:" + x.Id);
            if (gs.LinesUsed.Count > 16) gs.LinesUsed.RemoveAt(0);
            if (!s.Used.Contains(x.Topic)) s.Used.Add(x.Topic);
            var sc = SubjectScore(x, h, s);
            var reaction = sc >= 3 ? 2 : sc >= 1 ? 1 : sc >= 0 ? 0 : -1;
            var b = new SceneBuilder(h, true);
            b.Me(Fill(x.Opener[Random.Range(0, x.Opener.Length)], h));
            b.Face(h, reaction >= 2 ? (x.Tier >= 0 ? Feel.Love : Feel.Smile) : reaction >= 0 ? Feel.Normal : Feel.Pout);
            b.Her_(Pick(reaction >= 2 ? x.Loves : reaction >= 0 ? x.Fine : x.Dislikes, h));
            var replies = reaction >= 0 && x.Replies != null ? x.Replies.Take(3).ToArray() : new TalkReply[0];
            if (replies.Length > 0)
            {
                var args = new List<string>();
                foreach (var r in replies) { args.Add(Fill(r.Label, h)); args.Add(r.Tag); }
                b.Choice(args.ToArray());
                foreach (var r in replies) { b.Branch(r.Tag); b.Her_(Pick(r.Her, h)); b.End(); }
            }
            var lewdLanded = x.LewdGain > 0 && reaction >= 1 ? x.LewdGain : 0;
            PlayTurn(b, c =>
            {
                var r = replies.FirstOrDefault(y => y.Tag == c);
                var delta = Mathf.Clamp(reaction + (r != null ? r.Delta : 0), -1, 2);
                var turn = new Turn
                {
                    Delta = delta,
                    Lewd = lewdLanded + (r != null && reaction >= 1 ? r.Lewd : 0),
                    Toast = delta >= 2 ? (x.Tier >= 0 ? "♥ " + GirlInfo.FirstName(h) + " is blushing." : "♪♪ She loved talking about that.") : delta == 1 ? "♪ She liked that." : null,
                };
                turn.Done = () =>
                {
                    if (r != null && r.Learn != null) Learn(h, r.Learn);
                    if (r != null && r.Trust != 0) Social.AddTrust(h, r.Trust);
                    if (x.Tier >= 0 && delta >= 1)
                    {
                        var witnesses = Witness.Who(h, Witness.Flirt);
                        if (witnesses.Count > 0) Social.Add(Social.Kinds.Flirted, h, witnesses, Witness.Flirt);
                        if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 1);
                    }
                };
                CampusLifePlugin.Log.LogInfo("Talk: " + GirlInfo.FirstName(h) + " / " + x.Id + " -> " + (reaction >= 2 ? "loves it" : reaction >= 0 ? "fine" : "dislikes it") + (r != null ? ", you: " + r.Tag : "") + " (delta " + delta + ", H +" + turn.Lewd + ")");
                LastSubject = x.Id + ":" + (reaction >= 2 ? "loves" : reaction >= 0 ? "fine" : "dislikes") + (r != null ? ":" + r.Tag : "");
                return turn;
            });
        }

        internal static string PreviewHeart(SaveData.Heroine h)
        {
            return "tier " + HeartTier(h) + "/" + LewdTier(h) + " sweet: " + string.Join(" | ", HeartSubjects(h, false, 3).Select(x => x.Id).ToArray())
                   + " ; flirt: " + string.Join(" | ", HeartSubjects(h, true, 3).Select(x => x.Id).ToArray())
                   + " ; chat: " + string.Join(" | ", TalkSubjects(h, 3).Select(x => x.Id).ToArray());
        }

        /// <summary>The last subject and how it went.</summary>
        public static string LastSubject = "";

        internal static bool RunSubjectNow(string id)
        {
            var x = Find(id);
            if (x == null || Sess == null) return false;
            RunSubject(x);
            return true;
        }

        private sealed class Turn
        {
            public int Delta, Lewd;
            public string Toast;
            public Action Done, Next;
        }
    }
}
