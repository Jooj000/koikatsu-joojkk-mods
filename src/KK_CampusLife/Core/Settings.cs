using BepInEx.Configuration;
using KKAPI.Utilities;
using UnityEngine;

namespace Joojkk.CampusLife
{
    public enum GirlVoiceMode { GameVoice, Off }
    public enum MovementMode { Off, Observe, On }

    /// <summary>All user settings. Shown in the F1 Configuration Manager, in section order.</summary>
    internal static class Cfg
    {
        // 1. General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> PlannerKey;
        public static ConfigEntry<bool> ShowToasts;
        public static ConfigEntry<bool> LoadingScreen;
        public static ConfigEntry<bool> RadarMap;
        public static ConfigEntry<float> ToastSeconds;
        public static ConfigEntry<bool> DebugMode;

        // 2. Time & energy
        public static ConfigEntry<int> TalkTimeCost;
        public static ConfigEntry<float> PeriodLength;
        public static ConfigEntry<bool> PositionReadout;
        public static ConfigEntry<KeyboardShortcut> SaveWarpSpotKey;
        public static ConfigEntry<KeyboardShortcut> MoveStoreKey;

        // 12. School life director
        public static ConfigEntry<bool> ConversationForms;
        public static ConfigEntry<bool> MapForms;
        public static ConfigEntry<int> MapFormGap;
        public static ConfigEntry<bool> WalkAndTalk;
        public static ConfigEntry<bool> DirectorEnabled;
        public static ConfigEntry<MovementMode> IntentMovement;
        public static ConfigEntry<bool> FullControl;
        public static ConfigEntry<bool> StudentRomance;
        public static ConfigEntry<bool> StudentH;
        public static ConfigEntry<int> DramaPerWeek;
        public static ConfigEntry<bool> StealAndHarem;
        public static ConfigEntry<bool> SundayStories;
        public static ConfigEntry<bool> RelationshipSeasons;
        public static ConfigEntry<bool> RumorChains;
        public static ConfigEntry<bool> ClubProjects;
        public static ConfigEntry<bool> CouncilRace;
        public static ConfigEntry<bool> Anthology;
        public static ConfigEntry<bool> StrengthReducesTalkCost;
        public static ConfigEntry<bool> ActivitiesEnabled;
        public static ConfigEntry<bool> ActivitySpots;
        public static ConfigEntry<bool> EnergyEnabled;
        public static ConfigEntry<int> BaseEnergy;

        // 3. Club board
        public static ConfigEntry<bool> BoardEnabled;
        public static ConfigEntry<int> MinRequests;
        public static ConfigEntry<int> MaxRequests;
        public static ConfigEntry<int> CastPerExtraRequest;
        public static ConfigEntry<bool> AppealAddsRequests;
        public static ConfigEntry<bool> LewdRequests;
        public static ConfigEntry<bool> FailedRequestPenalty;
        public static ConfigEntry<float> RewardMultiplier;

        // 4. Girls
        public static ConfigEntry<bool> MoodsEnabled;
        public static ConfigEntry<int> MoodHintMinIntelligence;
        public static ConfigEntry<float> MoodBonusMultiplier;
        public static ConfigEntry<bool> GiftsEnabled;
        public static ConfigEntry<bool> AskAboutDayButton;
        public static ConfigEntry<bool> ChatEnabled;
        public static ConfigEntry<bool> TalkFatigue;
        public static ConfigEntry<bool> NewConversation;
        public static ConfigEntry<bool> SingleConversation;
        public static ConfigEntry<float> TurnTalkTime;
        public static ConfigEntry<bool> DiscoverRequests;
        public static ConfigEntry<bool> DuoScenes;
        public static ConfigEntry<bool> DuoInTalk;
        public static ConfigEntry<bool> TrioTalk;
        public static ConfigEntry<bool> EpisodesEnabled;
        public static ConfigEntry<int> EpisodeEveryDays;
        public static ConfigEntry<bool> DevotionEnabled;

        // 5. Large classes
        public static ConfigEntry<bool> SpotlightEnabled;
        public static ConfigEntry<int> SpotlightMinCast;
        public static ConfigEntry<int> SpotlightSize;
        public static ConfigEntry<bool> OffscreenLife;
        public static ConfigEntry<int> OffscreenDrift;

        // 6. Stories
        public static ConfigEntry<bool> ArcsEnabled;
        public static ConfigEntry<int> MaxActiveArcs;
        public static ConfigEntry<float> ArcChancePerDay;
        public static ConfigEntry<bool> DramaEnabled;
        public static ConfigEntry<bool> StoryGirlsWalk;
        public static ConfigEntry<bool> StoryDirector;
        public static ConfigEntry<bool> StoryInPerson;
        public static ConfigEntry<bool> StoryGraduates;
        public static ConfigEntry<bool> TeachersWalk;
        public static ConfigEntry<bool> TeacherTalks;

        // 7. School calendar
        public static ConfigEntry<bool> CalendarEnabled;
        public static ConfigEntry<bool> AutoPlaySchoolEvents;
        public static ConfigEntry<bool> HappeningsEnabled;
        public static ConfigEntry<float> HappeningChance;
        public static ConfigEntry<bool> AutoPlayHappenings;

        // 8. Economy
        public static ConfigEntry<int> StartingMoney;
        public static ConfigEntry<int> WeeklyAllowance;
        public static ConfigEntry<float> PayMultiplier;
        public static ConfigEntry<float> PriceMultiplier;

        // 9. Weekly report
        public static ConfigEntry<bool> WeeklyReport;

        // 10. Voice
        public static ConfigEntry<bool> VoiceEnabled;
        public static ConfigEntry<GirlVoiceMode> GirlVoice;
        public static ConfigEntry<bool> ShortGameClips;

        private static int _order = 10000;

        private static ConfigEntry<T> B<T>(ConfigFile c, string section, string key, T def, string desc, AcceptableValueBase range = null, bool advanced = false)
        {
            var attrs = new ConfigurationManagerAttributes { Order = _order--, IsAdvanced = advanced ? (bool?)true : null };
            return c.Bind(section, key, def, new ConfigDescription(desc, range, attrs));
        }

        public static void Init(ConfigFile c)
        {
            const string g = "1. General";
            Enabled = B(c, g, "Enable Campus Life", true, "Master switch. When off, the school plays like vanilla (your mod data stays in the save).");
            PlannerKey = B(c, g, "Menu hotkey", new KeyboardShortcut(KeyCode.F7), "Opens the Campus Life menu inside the clipboard (Character Status). The same menu is in the clipboard (middle mouse button / F4) as the Campus Life card. Time is paused while it is open, like the clipboard itself. Do not use F8: in the game F8 ends the current period.");
            ShowToasts = B(c, g, "Show notifications", true, "Pop-up notes like 'Request completed' or 'New rumor on the board'.");
            LoadingScreen = B(c, g, "Loading screen", true, "Replaces the game's white loading fade with a gray screen: a heart, where you are going (with a faint picture of the place) and a tip or a little joke. Story flashes are left alone.");
            RadarMap = B(c, g, "Floor plan on the radar", true, "Draws the floor you are on under the radar icons (seen from above, cut just over your head). Girls on another floor are dimmed, with a small arrow up or down.");
            ToastSeconds = B(c, g, "Notification duration (s)", 5f, "", new AcceptableValueRange<float>(1f, 20f));
            DebugMode = B(c, g, "Debug tools", false, "Verbose logging for bug reports.", null, true);

            const string t = "2. Time and energy";
            TalkTimeCost = B(c, t, "Conversation time cost (s)", 0, "Seconds of the period clock spent by each finished conversation (a period lasts 500 s). 0 = the clock stays paused while you talk, like vanilla.", new AcceptableValueRange<int>(0, 200));
            PeriodLength = B(c, t, "Period length multiplier", 1.5f, "How much longer each free period lasts while you walk around (1 = vanilla 500 s, 1.5 = 750 s). Conversations, menus and events never use up time.", new AcceptableValueRange<float>(1f, 4f));
            StrengthReducesTalkCost = B(c, t, "Strength reduces time cost", true, "Up to -40% conversation time cost at Strength 100 (stamina).");
            ActivitiesEnabled = B(c, t, "Enable school activities", true, "Study, work out, part-time jobs, club work, etc. depending on where you are.");
            ActivitySpots = B(c, t, "Activity spots on the map", true, "Each activity is done at a real spot of its place (next to the game's study desks, training corners...), with its own action icon and the game's animation or black-screen narration. Off: activities happen instantly from the menu. Spots can be moved with the position tools (section 11).");
            ActivitySpots.SettingChanged += (o, ea) => Joojkk.CampusLife.ActivitySpots.Reset();
            ActivitiesEnabled.SettingChanged += (o, ea) => Joojkk.CampusLife.ActivitySpots.Reset();
            EnergyEnabled = B(c, t, "Enable daily energy", true, "Activities cost energy. Energy refills every morning; Strength raises the maximum.");
            BaseEnergy = B(c, t, "Base energy per day", 4, "Energy at Strength 0. +1 for every 25 Strength.", new AcceptableValueRange<int>(1, 12));

            const string b = "3. Club board";
            BoardEnabled = B(c, b, "Enable club board", true, "Daily requests, rumors and opportunities that tell you who needs you today.");
            MinRequests = B(c, b, "Minimum requests per day", 2, "", new AcceptableValueRange<int>(0, 10));
            MaxRequests = B(c, b, "Maximum requests per day", 6, "", new AcceptableValueRange<int>(1, 15));
            CastPerExtraRequest = B(c, b, "Students per extra request", 8, "One extra daily request for every N students in the class.", new AcceptableValueRange<int>(2, 30));
            AppealAddsRequests = B(c, b, "Sensuality attracts requests", true, "+1 request at Sensuality 50 and +2 at 90: girls come to you.");
            LewdRequests = B(c, b, "Allow lewd requests", true, "Girlfriends and very lewd girls can post intimate requests.");
            FailedRequestPenalty = B(c, b, "Missed requests have consequences", true, "Ignoring a request lowers her favor a little and makes her lonely the next day.");
            RewardMultiplier = B(c, b, "Reward multiplier", 1f, "", new AcceptableValueRange<float>(0.25f, 4f));

            const string gi = "4. Girls";
            MoodsEnabled = B(c, gi, "Enable daily moods", true, "Each girl wakes up with a mood (hungry, bored, stressed, lonely, flirty, sleepy, cheerful). Matching it gives bonus favor.");
            MoodHintMinIntelligence = B(c, gi, "Intelligence to read moods", 30, "Below this Intelligence you only learn a girl's mood by asking her ('How are you today?').", new AcceptableValueRange<int>(0, 100));
            MoodBonusMultiplier = B(c, gi, "Mood bonus multiplier", 1f, "", new AcceptableValueRange<float>(0f, 4f));
            GiftsEnabled = B(c, gi, "Enable gifts", true, "Adds 'Give a gift' to conversations. Gifts matching her profile (sweets, coffee, sports...) work best.");
            AskAboutDayButton = B(c, gi, "Enable How are you today button", true, "Conversation option that reveals her mood and a hint about what she likes.");
            ChatEnabled = B(c, gi, "Enable Chat button", true, "A short conversation of three topics you pick. What she enjoys depends on her mood today, her personality, her club and what you already talked about (not on her profile likes). Ask How are you today? first to see which topics she will like. Once per girl per day.");
            NewConversation = B(c, gi, "New conversation system", true, "The talk screen's buttons open Campus Life's conversation: the speech bubble for talking (topics, let her talk, ask about friends, jokes, compliments, the game's small talk and listen), the heart for romance and H, the question mark for invitations, gifts and \"come with me\". Game options only show when the game allows them. Off = the game's buttons as they were.");
            SingleConversation = B(c, gi, "Single conversation list", true, "One list for the whole talk, under Talk, Actions, Do together, Romance and Spicy: every action straight on it when it can happen now (no sub-pages). Off = the three pages of the speech bubble, heart and question mark.");
            TurnTalkTime = B(c, gi, "Talk time per turn", 0.5f, "How much of the game's talk time (the blue bar, 4 points a day) each Campus Life conversation turn uses. The game's own topics and invitations use 1 point, as in vanilla.", new AcceptableValueRange<float>(0f, 1f));
            DiscoverRequests = B(c, gi, "Requests must be discovered", true, "Girls don't post their problems on the board: you learn about them by letting her talk, asking her friends about her, or noticing (Intelligence). Requests pay in favor, trust, reputation and items; only jobs pay money.");
            DuoScenes = B(c, gi, "Two-girl scenes", true, "Sometimes two friends come over to talk to you together: friendship jealousy, introductions, a quarrel where you take a side, gossip told as a pair.");
            DuoInTalk = B(c, gi, "Two-girl scenes in the talk screen", true, "The two girls walk over and call you on the map, then the scene plays in the talk screen with both of them, and they say goodbye on the map. Off: everything plays on the map.");
            TrioTalk = B(c, gi, "Talk with both girls", true, "After a two-girl scene the talk goes on with both of them side by side: pick who you talk to (one, the other, or both), the other girl reacts to what you do, they talk to each other, and sometimes they disagree and look to you to settle it.");
            EpisodesEnabled = B(c, gi, "Spotlight episodes", true, "The girls in the spotlight (or the ones you see most, in a small class) invite you to an outing played like the story mode: she stands in front of a painted background, with choices built from how close you are, your reputation, what you did for her, what she remembers and what you carry. Friendship stories (food, mysteries, festivals, heart-to-hearts); alone with her, the last choice can turn romantic: her hand, a kiss, a confession, or somewhere private (H).");
            EpisodeEveryDays = B(c, gi, "Days between spotlight episodes", 1, "At most one episode every N school days.", new AcceptableValueRange<int>(1, 7));
            TalkFatigue = B(c, gi, "Repeated questions count less", true, "The game's own questions give the same favor every time. From the 3rd conversation of the day with the same girl, only half (then a quarter) of that favor counts.");
            DevotionEnabled = B(c, gi, "Enable devotion", true, "Long-term bond that keeps growing after favor reaches 100. Every level unlocks a perk (more pay, energy, daily presents, stat bonuses, double rewards).");

            const string s = "5. Large classes";
            SpotlightEnabled = B(c, s, "Enable weekly spotlight", true, "With big classes, a few girls are 'in the spotlight' each week and get the stories. Nobody stays out for more than 3 weeks.");
            SpotlightMinCast = B(c, s, "Spotlight from class size", 16, "Spotlight only kicks in with at least this many students.", new AcceptableValueRange<int>(2, 99));
            SpotlightSize = B(c, s, "Girls in the spotlight", 5, "", new AcceptableValueRange<int>(1, 15));
            OffscreenLife = B(c, s, "Off-screen life", true, "Girls you don't meet still change a little each day (moods, small favor drift), so the class feels alive.");
            OffscreenDrift = B(c, s, "Off-screen favor drift", 2, "Maximum favor change per day for girls you didn't talk to.", new AcceptableValueRange<int>(0, 10));

            const string a = "6. Stories";
            ArcsEnabled = B(c, a, "Enable story arcs", true, "Short multi-day stories generated from each girl's profile (make-up exam, club tournament, secret hobby, rivalry...).");
            MaxActiveArcs = B(c, a, "Max active stories", 0, "0 = automatic (1 for tiny classes, up to 3 for big ones).", new AcceptableValueRange<int>(0, 8));
            ArcChancePerDay = B(c, a, "Chance of a new story per day", 0.4f, "", new AcceptableValueRange<float>(0f, 1f));
            DramaEnabled = B(c, a, "Enable drama", true, "Rivalry and jealousy stories where two girls want you on the same day and you must choose.");
            StoryGirlsWalk = B(c, a, "Story heroines attend school daily", true, "The game's story heroines walk around the school every day like the other students. They have no everyday conversations until their story is over (see the next setting).");
            StoryDirector = B(c, a, "Daily story heroine", true, "Each school day one story heroine is chosen; her next story event opens when you interact with her that day, whatever the game's weekly schedule says. Off = vanilla schedule.");
            StoryInPerson = B(c, a, "Story heroine waits in person", true, "Today's story heroine stands at her event spot, even for events that normally only show a star on the map.");
            TeachersWalk = B(c, a, "Teachers walk around the school", true, "The teachers walk around the school like the students and the story heroines, instead of only standing at their event spot.");
            TeacherTalks = B(c, a, "Teacher conversations with choices", true, "A teacher's event is a conversation with choices that moves her favor and H, instead of the game's string of short talks. Before you are friends she only talks at her event spot; from friend on, anywhere; when the friendship is complete she joins school life as a free character.");
            StoryGraduates = B(c, a, "Story heroines join a class when their story ends", true, "When a story heroine's story is complete she takes a free seat in a class as a regular student (like the PantyFairy fairy) and all everyday conversations work with her. Her story self stays in the save for the game's own scenes.");

            const string ca = "7. School calendar";
            CalendarEnabled = B(c, ca, "Enable school events", true, "Exams, sports festival, culture festival, field trips and seasonal days.");
            AutoPlaySchoolEvents = B(c, ca, "Start school events automatically", true, "When off, events wait for you to press 'Attend' in the planner.");
            HappeningsEnabled = B(c, ca, "Enable everyday happenings", true, "Small random events during the day: lost items, pop quizzes, love letters, notes asking for help, helping teachers...");
            HappeningChance = B(c, ca, "Happening chance per period", 0.35f, "", new AcceptableValueRange<float>(0f, 1f));
            AutoPlayHappenings = B(c, ca, "Play happenings automatically", true, "When off, happenings wait in the planner until you open them.");

            const string e = "8. Economy";
            StartingMoney = B(c, e, "Starting money", 3000, "Yen you get when Campus Life starts on a save.", new AcceptableValueRange<int>(0, 100000));
            WeeklyAllowance = B(c, e, "Weekly allowance", 2000, "Yen received every Monday.", new AcceptableValueRange<int>(0, 50000));
            PayMultiplier = B(c, e, "Pay multiplier", 1f, "", new AcceptableValueRange<float>(0f, 5f));
            PriceMultiplier = B(c, e, "Price multiplier", 1f, "", new AcceptableValueRange<float>(0.1f, 5f));

            WeeklyReport = B(c, "9. Weekly report", "Show weekly report", true, "Summary on Saturday night: what you did, who felt left out, ongoing stories and upcoming events.");

            const string v = "10. Voice";
            VoiceEnabled = B(c, v, "Enable voice", true, "Voice for Campus Life dialogue. The game's own scenes are not affected.");
            GirlVoice = B(c, v, "Girl voice", GirlVoiceMode.GameVoice, "GameVoice: each girl reacts with a fitting clip of her own Japanese voice (thanks, laugh, surprise, anger...) while the English text is shown. Off: silent.");
            ShortGameClips = B(c, v, "Prefer short game clips", true, "GameVoice mode: prefer short reactions (a laugh, a gasp, a short thank-you) over long sentences that don't match the English text.");

            const string pt = "11. Position tools";
            PositionReadout = B(c, pt, "Show position readout", false, "Shows the map number and your position and facing at the top left while walking around, to place things. The keys below only work while it is shown; each also copies the position to the clipboard and the log.");
            SaveWarpSpotKey = B(c, pt, "Set warp spot key", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl), "Makes the current spot the place where the menu's \"Go there\" buttons put you on this map. Saved in BepInEx/config/joojkk.campuslife.spots.txt.");
            MoveStoreKey = B(c, pt, "Move store key", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl, KeyCode.LeftShift), "Moves the school store icon to just in front of you (any map). Saved in the same spots file.");

            const string d = "12. School life director";
            ConversationForms = B(c, d, "Conversation by scene", true, "Talks open with a scene when she has something on her mind (a worry, a friend, something you did together), and the first page offers three concrete things to say instead of topic lists. The old pages stay under \"Something else...\".");
            MapForms = B(c, d, "Girls start small moments on the map", true, "Now and then a girl near you says a quick line, asks a small favor, sits quietly with you or asks you to walk her somewhere.");
            MapFormGap = B(c, d, "Seconds between map moments", 60, "", new AcceptableValueRange<int>(20, 600));
            WalkAndTalk = B(c, d, "Walk and talk", true, "A girl may ask you to walk her to a few places; she follows you and talks at each stop. Warping cancels the walk.");
            DirectorEnabled = B(c, d, "Enable the school life director", true, "Students have needs and intentions, become friends, fall for each other, fight, make up, and stories spread as rumors.");
            IntentMovement = B(c, d, "Students move by intention", MovementMode.On, "On: where a student walks follows what she wants (food, rest, a friend, her girlfriend, you). Observe: only logged. Off: the game decides as usual.");
            StudentRomance = B(c, d, "Romance between students", true, "Girls who like girls (the card's setting, or about a third of the others) can get crushes, become couples, break up and make up.");
            FullControl = B(c, d, "The director decides every action", true, "Every action of every girl is the director's choice (her needs, intention, mood, club); the game only carries it out. Off: the game draws actions from its own desire table and the director only leans them.");
            StudentH = B(c, d, "Student couples get intimate", true, "Couples between students may slip away together (the game's own scene between two girls), more often the closer they are.");
            DramaPerWeek = B(c, d, "Big drama per week", 2, "How many big scenes (confessions, breakups, scandals) the director allows each week.", new AcceptableValueRange<int>(0, 6));
            StealAndHarem = B(c, d, "Love triangles with you", true, "A girl in a couple who falls for you is torn; you may win her over (her ex is hurt for a while and can forgive you) or, with girlfriends who get along, agree on sharing.");
            SundayStories = B(c, d, "Sunday stories", true, "On Sunday dates and Sunday mornings a short story plays from your week together.");
            RelationshipSeasons = B(c, d, "Relationship seasons", true, "Each girlfriend goes through chapters (the first weeks, settling in, a test, a promise) with little traditions you build together.");
            RumorChains = B(c, d, "Rumor chains", true, "What happens at school spreads from girl to girl and changes on the way; you can confirm, deny or ignore it.");
            ClubProjects = B(c, d, "Club projects", true, "Each week the club takes on a project (a stand, a contest, a show); members' choices shape the club's identity.");
            CouncilRace = B(c, d, "Student council race", true, "Once every 9-week loop two girls run for president; you can support one. The winner's platform changes school life for a while.");
            Anthology = B(c, d, "Class anthology", true, "The big moments are written down; at the end of each loop a ceremony shows the best ones and every girl gets an epilogue.");
        }
    }
}
