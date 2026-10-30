using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Sunday Stories: right after the game's own Sunday date, a three-part episode plays (The Meeting: she arrives with a
    /// plan built on something she likes; Unexpected Trouble: rain, a closed shop, a crowd; The Part We Remember: how you
    /// handled it). Some endings start a tradition; on Monday she brings it up herself. The episode is picked from her
    /// mood, trust, likes, traditions and last Sunday (the same one never twice in a row), with backgrounds for each part.
    /// </summary>
    internal static class Sunday
    {
        private static CampusState S { get { return Campus.S; } }
        public static string LastPlayed = "";

        public static void Install(Harmony harmony)
        {
            foreach (var name in new[] { "WakeUpHolidayEvent", "WakeUpHolidayMapSelectEvent" })
            {
                var m = AccessTools.Method(typeof(Cycle), name);
                if (m == null) { CampusLifePlugin.Log.LogWarning("Cycle." + name + " not found: no Sunday stories after that date."); continue; }
                harmony.Patch(m, postfix: new HarmonyMethod(AccessTools.Method(typeof(Sunday), nameof(DatePostfix))));
            }
        }

        /// <summary>
        /// The story plays right after the date, still on Sunday, inside the game's date coroutine (the date takes the whole
        /// day: the first free moment after it is Monday, user 2026-10-12). It is opened the way the game opens its own
        /// date ADVs there (<see cref="Scenes.PlayInline"/>); the first try used KKAPI's StartAdvEvent, whose list waits for
        /// the scene fade, and the game sat on the loading screen for good (2026-10-11). If it cannot play there, it waits
        /// for the first free moment instead.
        /// </summary>
        private static SaveData.Heroine _pending;
        private static int _pendingDay = -1;
        private static float _nextCheck;

        private static void DatePostfix(SaveData.Heroine dateChara, ref IEnumerator __result)
        {
            try
            {
                if (!Campus.On || !Cfg.SundayStories.Value || dateChara == null) return;
                __result = AfterDate(__result, dateChara);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Sunday: " + e); }
        }

        private static IEnumerator AfterDate(IEnumerator date, SaveData.Heroine h)
        {
            yield return date;
            IEnumerator story = null;
            try
            {
                if (!Scene.isReturnTitle && !Scene.isGameEnd && Campus.On)
                {
                    Action<string> done;
                    var b = Story(h, out done);
                    if (b != null) story = Scenes.PlayInline(b, done);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Sunday story: " + e); }
            if (story == null)
            {
                _pending = h;
                _pendingDay = S.Day;
                CampusLifePlugin.Log.LogInfo("Sunday: the story with " + GirlInfo.FirstName(h) + " plays at the first free moment.");
                yield break;
            }
            CampusLifePlugin.Log.LogInfo("Sunday: the story with " + GirlInfo.FirstName(h) + " plays right after the date.");
            yield return story;
            CampusLifePlugin.Log.LogInfo("Sunday: the story with " + GirlInfo.FirstName(h) + " is over.");
        }

        /// <summary>From the plugin's Update: the pending story, once the map is free (for two days at most).</summary>
        public static void Tick()
        {
            if (_pending == null || Time.realtimeSinceStartup < _nextCheck) return;
            _nextCheck = Time.realtimeSinceStartup + 1f;
            try
            {
                if (!Campus.On || S.Day - _pendingDay > 2) { _pending = null; return; }
                if (!Scenes.CanPlayOnMap || Duo.Busy) return;
                var h = _pending;
                _pending = null;
                if (!PlayNow(h)) CampusLifePlugin.Log.LogInfo("Sunday: the story with " + GirlInfo.FirstName(h) + " could not play.");
            }
            catch (Exception e) { _pending = null; CampusLifePlugin.Log.LogError("Sunday tick: " + e); }
        }

        public static string PendingName { get { return _pending != null ? GirlInfo.FirstName(_pending) : null; } }

        public static bool PlayNow(SaveData.Heroine h)
        {
            Action<string> done;
            var b = Story(h, out done);
            return b != null && Scenes.PlayOnMap(b, done);
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        // ================= the episode =================

        private sealed class Plan { public string Like, Idea, Place, Bg, Back; }

        /// <summary>Her plan for the day, from what she likes.</summary>
        private static Plan PlanFor(SaveData.Heroine h)
        {
            var likes = GirlInfo.LikesOf(h);
            var l = new List<Plan>
            {
                new Plan { Like = "sweets", Idea = "a new crepe stand by the station", Place = "the crepe stand", Bg = "@sweets|tanabata_street", Back = "@street|festival_stalls_night" },
                new Plan { Like = "food", Idea = "an all-you-can-eat place downtown", Place = "the restaurant", Bg = "@street|tanabata_street", Back = "@cafe|festival_stalls_night" },
                new Plan { Like = "study", Idea = "the big bookstore two stations away", Place = "the bookstore", Bg = "@station|train", Back = "@street|kyoto_temple" },
                new Plan { Like = "animals", Idea = "the cat café that just opened", Place = "the cat café", Bg = "@cafe|tanabata_street", Back = "@parkway|snow_park_night" },
                new Plan { Like = "fashion", Idea = "window shopping in the mall", Place = "the mall", Bg = "@station|train", Back = "@shop|tanabata_street" },
                new Plan { Like = "exercise", Idea = "a hike up to the shrine on the hill", Place = "the shrine", Bg = "kyoto_temple", Back = "@park|inn_room_day" },
                new Plan { Like = "coffee", Idea = "a quiet café she's been wanting to try", Place = "the café", Bg = "@cafe|tanabata_street", Back = "@street|inn_room_day" },
                new Plan { Like = "cooking", Idea = "the morning market for ingredients", Place = "the market", Bg = "@shop|festival_stalls_night", Back = "@street|inn_room_day" },
            };
            var mine = l.Where(p => likes.Contains(p.Like)).ToList();
            if (mine.Count == 0) mine = l.Where(p => p.Like == "sweets" || p.Like == "animals").ToList();
            return mine[Random.Range(0, mine.Count)];
        }

        /// <summary>Which episode: a tradition revisited, a quiet one when she's tired, or the plan that went wrong.</summary>
        private static string Pick(SaveData.Heroine h, GirlState gs)
        {
            var l = new List<KeyValuePair<string, float>>();
            l.Add(new KeyValuePair<string, float>("plan", 2f));
            if (gs.Traditions.Count > 0) l.Add(new KeyValuePair<string, float>("tradition", 1.5f));
            if (gs.Mood == Mood.Sleepy || gs.Mood == Mood.Stressed || gs.NeedRest < 40) l.Add(new KeyValuePair<string, float>("quiet", 2f));
            var friend = Social.BestFriend(h);
            if (friend != null && Social.Trust(h) >= 40) l.Add(new KeyValuePair<string, float>("friend", 0.8f));
            l = l.Where(x => x.Key != gs.LastSunday || l.Count == 1).ToList();
            var roll = Random.value * l.Sum(x => x.Value);
            foreach (var x in l) { roll -= x.Value; if (roll <= 0f) return x.Key; }
            return l[0].Key;
        }

        /// <summary>The whole story and what it leaves behind.</summary>
        public static SceneBuilder Story(SaveData.Heroine h, out Action<string> done)
        {
            done = null;
            if (h == null) return null;
            var gs = S.Girl(GirlInfo.Key(h));
            var kind = Pick(h, gs);
            var plan = PlanFor(h);
            var name = GirlInfo.FirstName(h);
            var b = new SceneBuilder(h, false);
            var friend = kind == "friend" ? Social.BestFriend(h) : null;
            var trad = kind == "tradition" ? Seasons.Tradition(h) : null;

            // Part 1: The Meeting
            b.Bg(kind == "quiet" ? "inn_room_day" : plan.Bg);
            b.Narr(kind == "quiet" ? "Sunday Story: \"A Day for Doing Nothing\"" : kind == "tradition" ? "Sunday Story: \"Our Usual\"" : kind == "friend" ? "Sunday Story: \"Party of Three\"" : "Sunday Story: \"The Plan That Went Wrong\"");
            b.Narr("Part 1: The Meeting.");
            if (kind == "quiet")
            {
                b.Narr(name + " shows up with dark circles under her eyes and a sheepish smile.");
                b.Say(h, T(h, "I'm sorry... I'm so tired this week. Could we just... not go anywhere? Is that boring? || Sorry... I'm really tired. Could we just stay in today? || I'm worn out this week... is it okay if we rest? || Um, could today be a lazy day? I'm so sleepy...", "Ugh, I'm dead! Can we do nothing today? Like, professionally nothing? || I'm wiped out! Lazy day? Please?! || Ugh, no energy! Let's just chill all day! || Zero battery! Nap day! Pleeease!",
                    "I'm exhausted. I propose a day of doing absolutely nothing. || I'm tired. Let's rest today. || A day without plans. I insist. || I propose rest. Nothing else.", "Hot take: let's do nothing today. Together. It counts as a date. || Proposal: we do nothing. Together. Very romantic. || Lazy date. Still counts. || Let's be useless together today~"));
            }
            else if (kind == "tradition")
            {
                b.Narr(name + " is already waiting, and she doesn't need to say where you're going.");
                b.Say(h, T(h, "Our usual, right? " + char.ToUpper(trad[0]) + trad.Substring(1) + "... I look forward to it all week. || The usual? " + char.ToUpper(trad[0]) + trad.Substring(1) + "... I always look forward to it." + " || " + char.ToUpper(trad[0]) + trad.Substring(1) + ", like always? Hehe... || Our usual... " + char.ToUpper(trad[0]) + trad.Substring(1) + ". I waited all week.", "Usual?! Usual! " + char.ToUpper(trad[0]) + trad.Substring(1) + "! || Usual! " + char.ToUpper(trad[0]) + trad.Substring(1) + "! Let's go!" + " || " + char.ToUpper(trad[0]) + trad.Substring(1) + "! Like always! Yay! || The usual?! YES! " + char.ToUpper(trad[0]) + trad.Substring(1) + "!",
                    "The usual. " + char.ToUpper(trad[0]) + trad.Substring(1) + "." + " || " + char.ToUpper(trad[0]) + trad.Substring(1) + ". As always. || The usual. " + char.ToUpper(trad[0]) + trad.Substring(1) + ". Good. || Routine. " + char.ToUpper(trad[0]) + trad.Substring(1) + ".", "Don't tell me you forgot. " + char.ToUpper(trad[0]) + trad.Substring(1) + ". Our thing." + " || " + char.ToUpper(trad[0]) + trad.Substring(1) + ". You'd better not have forgotten. || Our thing. " + char.ToUpper(trad[0]) + trad.Substring(1) + ". Obviously." + " || " + char.ToUpper(trad[0]) + trad.Substring(1) + ", of course. Our little ritual."));
            }
            else
            {
                if (friend != null) b.Narr(name + " arrives... and she isn't alone. " + GirlInfo.FirstName(friend) + " waves from behind her.");
                b.Say(h, T(h, "I made a plan! I want to take you to " + plan.Idea + ". I've been wanting to go all week... || I planned something... I'd like to take you to " + plan.Idea + ". || I thought about it all week... can we go to " + plan.Idea + "? || Um, I have a plan! " + plan.Idea + "... if you'd like.", "I have a PLAN! " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "! It's gonna be amazing! || Plan time! " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "! Gonna be epic! || Guess what! " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "! Let's go! || I've got it all planned! " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "!",
                    "I've planned the day. " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + ". It's efficient. || The plan: " + plan.Idea + ". || I've decided. " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "." + " || " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + ". I've arranged everything.", "Today, I'm taking you to " + plan.Idea + ". You're welcome. || I'm taking you to " + plan.Idea + ". No objections. || Today: " + plan.Idea + ". Thank me later." + " || " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + ". You're lucky I planned it."));
                if (friend != null) b.Say(friend, T(friend, "S-sorry for tagging along! She said it'd be okay... || S-sorry to intrude! She said I could come... || Um, hello! I hope I'm not in the way... || Sorry for tagging along...", "Hi hi! Don't mind me! Third wheel reporting! || Hi! Extra friend here! Don't mind me! || Surprise! I'm coming too! || Hey hey! Third wheel, at your service!", "I'm only here for the first half. || I'm only staying a while. || Don't mind me. I'll leave early. || Only for a bit.", "Relax, I'll leave you two alone. Eventually. || Don't worry, I'll vanish. Eventually~ || I'll leave you lovebirds soon. Maybe. || Pretend I'm not here. If you can."));
            }

            // Part 2: Unexpected Trouble
            b.Bg(kind == "quiet" ? "inn_room_day" : plan.Back);
            b.Narr("Part 2: Unexpected Trouble.");
            var trouble = Random.Range(0, 3);
            if (kind == "quiet") b.Narr("The plan was nothing. Then the power goes out, and nothing becomes candles, a deck of cards and a thunderstorm outside.");
            else if (trouble == 0) b.Narr("Halfway there, the sky opens up. Rain, the heavy kind. You end up under the same small awning, shoulder to shoulder.");
            else if (trouble == 1) b.Narr("When you get to " + (kind == "tradition" ? "your usual spot" : plan.Place) + ", there's a sign on the door: CLOSED TODAY.");
            else b.Narr("It seems the whole town had the same idea. The line goes around the block twice.");
            b.Say(h, T(h, "Ah... this isn't how I imagined it... || Oh... this isn't how I pictured today... || Ah... it's all going wrong... || Um... I didn't expect this...", "Nooo! Seriously?! Today of all days! || No way! Not today! || Seriously?! Of all days! || Argh! This is the worst!", "...Unfortunate. || ...Inconvenient. || ...Not ideal. || ...Hm. Unlucky.", "...Well, this is a disaster. || ...Well. That's ruined. || ...Fantastic. Just fantastic. || ...Of course it went wrong."));

            // Part 3: The Part We Remember
            b.Narr("Part 3: The Part We Remember.");
            var opts = new List<string> { "Stick to her original plan", "su_plan", "Improvise something together", "su_improv", "Make her laugh about it", "su_laugh", "Ask what she really wanted today", "su_ask" };
            b.Choice(opts.ToArray());
            var moodOk = gs.Mood == Mood.Cheerful || gs.Mood == Mood.Flirty || gs.Mood == Mood.None;
            var trustOk = Social.Trust(h) >= 45;
            var laughOk = G.Sens >= 40 || GirlInfo.ToneOf(h) == Tone.Energetic || GirlInfo.ToneOf(h) == Tone.Teasing;
            var askOk = G.Int >= 40 || trustOk;
            b.Branch("su_plan");
            if (moodOk) { b.Narr("You wait it out together. It takes forever. She doesn't seem to mind at all."); b.Say(h, T(h, "...We made it. Thank you for not giving up on my plan. || ...We did it. Thank you for staying with my plan. || ...It worked out. Thank you for believing in it. || ...We made it after all. I'm so happy.", "WE DID IT! Totally worth it! || WE MADE IT! Totally worth it! || Yes! Told you it'd be great! || Woo! We did it!", "...Persistence. I respect that. || ...We persisted. Good. || ...It worked. Thanks. || ...Worth it.", "...Okay, you're stubborn. I like that. || ...Stubborn, aren't you? I like it. || ...You didn't give up. Cute. || ...Persistent. That's attractive.")); }
            else { b.Narr("You push on, but she's clearly not enjoying it anymore."); b.Say(h, T(h, "...It's fine. Really. || ...It's okay. Really. || ...Don't worry about it. || ...I'm fine. It's fine.", "...Yeah. Great. Super fun. || ...Yeah. Super. Fun. || ...Great. Amazing. Totally. || ...Whatever. Fun, I guess.", "...Hm. || ...Hm. || ...I see. || ...Right.", "...You really didn't read the room, huh. || ...You missed the hint, huh. || ...Not your best idea. || ...Reading the mood isn't your thing.")); }
            b.End();
            b.Branch("su_improv");
            b.Narr(trustOk ? "You grab her hand and take a random turn. A tiny shop, a park bench, a vending machine with a drink neither of you has heard of. It becomes the best part of the day." : "You try something else. It's... fine. A little awkward, but fine.");
            b.Say(h, trustOk ? T(h, "...This was better than my plan. Don't tell anyone I said that. || ...This was better than my plan. Don't tell. || ...Your idea was lovely. Thank you. || ...I like this better. Shh.", "This is AMAZING! Why didn't we do this from the start?! || This is AWESOME! Why didn't we start with this?! || Okay, your plan rocks! || Best idea ever! You win!", "...Unexpectedly pleasant. || ...Better than expected. || ...A good alternative. || ...I approve.", "...Okay, fine, you win this one. || ...Fine, you win. This time. || ...Okay, point to you. || ...Don't get smug. It was good.") : T(h, "...It was nice. I think. || ...It was nice, I think. || ...It was okay. || ...Mm, it was fine.", "...Huh. Okay! || ...Huh. Okay, sure! || ...That was... a thing! || ...Alright! I guess!", "...Acceptable. || ...Adequate. || ...Fine. || ...Passable.", "...Well, that happened. || ...Well, that was something. || ...Interesting choice. || ...Okay. Sure."));
            b.End();
            b.Branch("su_laugh");
            if (laughOk) { b.Narr("You do a ridiculous impression of the weather forecaster. She laughs so hard she has to sit down."); b.Say(h, T(h, "Hahaha! Stop, stop, I can't breathe...! || Hahaha! Stop, please! My stomach hurts! || Ahaha! I can't stop laughing! || Hehehe! You're so silly!", "AHAHA! Do it again! Do it again! || BAHAHA! Again! Again! || AHAHA! You're hilarious! || Hahaha! I'm dying!", "...Pfft. ...Idiot. || ...Pfft. ...Stop. || ...Heh. Idiot. || ...That was funny. Don't repeat it.", "Hah! Okay, that was actually funny. || Hah! Okay, you got me. || Ahaha! Fine, that was good. || Pfft! You're ridiculous.")); }
            else { b.Narr("Your joke lands somewhere far away from her."); b.Say(h, T(h, "...Ehehe...? || ...Ehehe? || ...Haha... um? || ...Was that funny?", "...Uh, haha? || ...Ha... ha? || ...Uh, good one? || ...Heh?", "... || ...No. || ...Hm. || ......", "...Was that a joke? || ...Was that supposed to be funny? || ...Oof. || ...Tough crowd, huh? Me, I mean.")); }
            b.End();
            b.Branch("su_ask");
            b.Say(h, askOk ? T(h, "...What I wanted? Honestly... just to spend the day with you. The place didn't matter. || ...What I wanted? Just a day with you. Anywhere. || ...Honestly, I only wanted to be with you. || ...The place didn't matter. Only you.", "...Okay, real answer? I just wanted to hang out with you! The rest was an excuse! || ...Real talk? I just wanted to hang out with you! || ...Okay, confession! The plan was an excuse to see you! || ...It was always about you! The rest was decoration!",
                "...To be with you. The plan was a pretext. || ...You. The plan didn't matter. || ...Time with you. That was the point. || ...To be near you. Nothing more.", "...You really want to know? Just you. The plan was cover. || ...Want the truth? You. The rest was a cover story. || ...Just you. Don't let it go to your head. || ...You. Obviously. Keep up.") : T(h, "...I don't know. Just... something fun, I guess. || ...I'm not sure. Something nice, I suppose. || ...Um... just fun, maybe? || ...I don't really know.", "Uh... fun? Fun stuff? || Uh... fun? Fun things? || ...Stuff! Good stuff! || ...I dunno! Something!", "...That's a complicated question. || ...Hard to say. || ...I'd rather not say. || ...Complicated.", "...Why do you ask? || ...Why so curious? || ...That's a secret. || ...Wouldn't you like to know."));
            b.End();

            var trustOkC = trustOk; var moodOkC = moodOk; var laughOkC = laughOk; var askOkC = askOk;
            var kindC = kind; var planC = plan; var tradC = trad; var friendC = friend;
            done = c =>
            {
                var good = c == "su_plan" && moodOkC || c == "su_improv" && trustOkC || c == "su_laugh" && laughOkC || c == "su_ask" && askOkC;
                Campus.Apply(new Rewards { Favor = good ? 6 : 2, Devotion = good ? 6 : 2 }, h);
                Social.AddTrust(h, good ? 5 : 1);
                if (friendC != null) Arcs.AddBond(GirlInfo.Key(h), GirlInfo.Key(friendC), 2);
                var gs2 = S.Girl(GirlInfo.Key(h));
                gs2.Sundays++;
                gs2.LastSunday = kindC;
                if (kindC == "quiet") gs2.NeedRest = 100;
                var memory = kindC == "quiet" ? "we spent a rainy Sunday doing nothing at all" : kindC == "tradition" ? "we kept up " + tradC + " even when everything went wrong"
                    : c == "su_improv" ? "we got lost on purpose on Sunday instead of going to " + planC.Place : c == "su_laugh" ? "you made me laugh so hard on Sunday" : "we tried to go to " + planC.Place + " on Sunday";
                Memories.Add(h, "sunday", memory, good ? 1 : 0, true);
                // A good Sunday may become a tradition.
                if (good && kindC == "plan" && gs2.Sundays >= 2) Seasons.AddTradition(h, c == "su_improv" ? "getting lost on purpose on Sundays" : "Sundays at " + planC.Place);
                if (good) Anthology.Add("sunday", "A Sunday with " + GirlInfo.FirstName(h) + ": " + memory, 1, GirlInfo.Key(h));
                LastPlayed = kindC + " with " + GirlInfo.FirstName(h) + " (" + c + (good ? ", good" : "") + ")";
                CampusLifePlugin.Log.LogInfo("Sunday story: " + LastPlayed);
            };
            return b;
        }
    }
}
