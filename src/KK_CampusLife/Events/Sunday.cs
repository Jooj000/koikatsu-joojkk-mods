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
                b.Say(h, T(h, "I'm sorry... I'm so tired this week. Could we just... not go anywhere? Is that boring?", "Ugh, I'm dead! Can we do nothing today? Like, professionally nothing?",
                    "I'm exhausted. I propose a day of doing absolutely nothing.", "Hot take: let's do nothing today. Together. It counts as a date."));
            }
            else if (kind == "tradition")
            {
                b.Narr(name + " is already waiting, and she doesn't need to say where you're going.");
                b.Say(h, T(h, "Our usual, right? " + char.ToUpper(trad[0]) + trad.Substring(1) + "... I look forward to it all week.", "Usual?! Usual! " + char.ToUpper(trad[0]) + trad.Substring(1) + "!",
                    "The usual. " + char.ToUpper(trad[0]) + trad.Substring(1) + ".", "Don't tell me you forgot. " + char.ToUpper(trad[0]) + trad.Substring(1) + ". Our thing."));
            }
            else
            {
                if (friend != null) b.Narr(name + " arrives... and she isn't alone. " + GirlInfo.FirstName(friend) + " waves from behind her.");
                b.Say(h, T(h, "I made a plan! I want to take you to " + plan.Idea + ". I've been wanting to go all week...", "I have a PLAN! " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + "! It's gonna be amazing!",
                    "I've planned the day. " + char.ToUpper(plan.Idea[0]) + plan.Idea.Substring(1) + ". It's efficient.", "Today, I'm taking you to " + plan.Idea + ". You're welcome."));
                if (friend != null) b.Say(friend, T(friend, "S-sorry for tagging along! She said it'd be okay...", "Hi hi! Don't mind me! Third wheel reporting!", "I'm only here for the first half.", "Relax, I'll leave you two alone. Eventually."));
            }

            // Part 2: Unexpected Trouble
            b.Bg(kind == "quiet" ? "inn_room_day" : plan.Back);
            b.Narr("Part 2: Unexpected Trouble.");
            var trouble = Random.Range(0, 3);
            if (kind == "quiet") b.Narr("The plan was nothing. Then the power goes out, and nothing becomes candles, a deck of cards and a thunderstorm outside.");
            else if (trouble == 0) b.Narr("Halfway there, the sky opens up. Rain, the heavy kind. You end up under the same small awning, shoulder to shoulder.");
            else if (trouble == 1) b.Narr("When you get to " + (kind == "tradition" ? "your usual spot" : plan.Place) + ", there's a sign on the door: CLOSED TODAY.");
            else b.Narr("It seems the whole town had the same idea. The line goes around the block twice.");
            b.Say(h, T(h, "Ah... this isn't how I imagined it...", "Nooo! Seriously?! Today of all days!", "...Unfortunate.", "...Well, this is a disaster."));

            // Part 3: The Part We Remember
            b.Narr("Part 3: The Part We Remember.");
            var opts = new List<string> { "Stick to her original plan", "su_plan", "Improvise something together", "su_improv", "Make her laugh about it", "su_laugh", "Ask what she really wanted today", "su_ask" };
            b.Choice(opts.ToArray());
            var moodOk = gs.Mood == Mood.Cheerful || gs.Mood == Mood.Flirty || gs.Mood == Mood.None;
            var trustOk = Social.Trust(h) >= 45;
            var laughOk = G.Sens >= 40 || GirlInfo.ToneOf(h) == Tone.Energetic || GirlInfo.ToneOf(h) == Tone.Teasing;
            var askOk = G.Int >= 40 || trustOk;
            b.Branch("su_plan");
            if (moodOk) { b.Narr("You wait it out together. It takes forever. She doesn't seem to mind at all."); b.Say(h, T(h, "...We made it. Thank you for not giving up on my plan.", "WE DID IT! Totally worth it!", "...Persistence. I respect that.", "...Okay, you're stubborn. I like that.")); }
            else { b.Narr("You push on, but she's clearly not enjoying it anymore."); b.Say(h, T(h, "...It's fine. Really.", "...Yeah. Great. Super fun.", "...Hm.", "...You really didn't read the room, huh.")); }
            b.End();
            b.Branch("su_improv");
            b.Narr(trustOk ? "You grab her hand and take a random turn. A tiny shop, a park bench, a vending machine with a drink neither of you has heard of. It becomes the best part of the day." : "You try something else. It's... fine. A little awkward, but fine.");
            b.Say(h, trustOk ? T(h, "...This was better than my plan. Don't tell anyone I said that.", "This is AMAZING! Why didn't we do this from the start?!", "...Unexpectedly pleasant.", "...Okay, fine, you win this one.") : T(h, "...It was nice. I think.", "...Huh. Okay!", "...Acceptable.", "...Well, that happened."));
            b.End();
            b.Branch("su_laugh");
            if (laughOk) { b.Narr("You do a ridiculous impression of the weather forecaster. She laughs so hard she has to sit down."); b.Say(h, T(h, "Hahaha! Stop, stop, I can't breathe...!", "AHAHA! Do it again! Do it again!", "...Pfft. ...Idiot.", "Hah! Okay, that was actually funny.")); }
            else { b.Narr("Your joke lands somewhere far away from her."); b.Say(h, T(h, "...Ehehe...?", "...Uh, haha?", "...", "...Was that a joke?")); }
            b.End();
            b.Branch("su_ask");
            b.Say(h, askOk ? T(h, "...What I wanted? Honestly... just to spend the day with you. The place didn't matter.", "...Okay, real answer? I just wanted to hang out with you! The rest was an excuse!",
                "...To be with you. The plan was a pretext.", "...You really want to know? Just you. The plan was cover.") : T(h, "...I don't know. Just... something fun, I guess.", "Uh... fun? Fun stuff?", "...That's a complicated question.", "...Why do you ask?"));
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
                if (good && kindC == "plan" && Random.value < 0.5f) Seasons.AddTradition(h, c == "su_improv" ? "getting lost on purpose on Sundays" : "Sundays at " + planC.Place);
                if (good) Anthology.Add("sunday", "A Sunday with " + GirlInfo.FirstName(h) + ": " + memory, 1, GirlInfo.Key(h));
                LastPlayed = kindC + " with " + GirlInfo.FirstName(h) + " (" + c + (good ? ", good" : "") + ")";
                CampusLifePlugin.Log.LogInfo("Sunday story: " + LastPlayed);
            };
            return b;
        }
    }
}
