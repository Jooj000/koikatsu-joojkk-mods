using BepInEx.Configuration;
using KKAPI.Utilities;
using UnityEngine;

namespace Joojkk.CampusLife
{
    public enum TtsEngine { Piper, WindowsSapi }
    public enum SpeakerMode { ByGirl, ByPersonality, Fixed }
    public enum GirlVoiceMode { GameVoice, Tts, Off }

    /// <summary>All user settings. Shown in the F1 Configuration Manager, in section order.</summary>
    internal static class Cfg
    {
        // 1. General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> PlannerKey;
        public static ConfigEntry<bool> ShowToasts;
        public static ConfigEntry<bool> LoadingScreen;
        public static ConfigEntry<float> ToastSeconds;
        public static ConfigEntry<bool> DebugMode;

        // 2. Time & energy
        public static ConfigEntry<int> TalkTimeCost;
        public static ConfigEntry<float> PeriodLength;
        public static ConfigEntry<bool> PositionReadout;
        public static ConfigEntry<KeyboardShortcut> SaveWarpSpotKey;
        public static ConfigEntry<KeyboardShortcut> MoveStoreKey;
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
        public static ConfigEntry<float> TurnTalkTime;
        public static ConfigEntry<bool> DiscoverRequests;
        public static ConfigEntry<bool> DuoScenes;
        public static ConfigEntry<bool> DuoInTalk;
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
        public static ConfigEntry<bool> TtsFallback;
        public static ConfigEntry<TtsEngine> Engine;
        public static ConfigEntry<string> PiperExe;
        public static ConfigEntry<string> PiperModel;
        public static ConfigEntry<SpeakerMode> PiperSpeakerMode;
        public static ConfigEntry<int> PiperSpeakerCount;
        public static ConfigEntry<int> PiperFixedSpeaker;
        public static ConfigEntry<int> PiperPlayerSpeaker;
        public static ConfigEntry<int> PiperNarratorSpeaker;
        public static ConfigEntry<float> LengthScale;
        public static ConfigEntry<float> NoiseScale;
        public static ConfigEntry<float> Volume;
        public static ConfigEntry<bool> PitchByTone;
        public static ConfigEntry<float> PitchVariation;
        public static ConfigEntry<bool> SpeakPlayer;
        public static ConfigEntry<bool> SpeakNarrator;
        public static ConfigEntry<string> SapiFemaleVoice;
        public static ConfigEntry<string> SapiMaleVoice;
        public static ConfigEntry<int> SapiRate;
        public static ConfigEntry<bool> TtsCache;
        public static ConfigEntry<int> TtsCacheMaxMb;

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
            TurnTalkTime = B(c, gi, "Talk time per turn", 0.5f, "How much of the game's talk time (the blue bar, 4 points a day) each Campus Life conversation turn uses. The game's own topics and invitations use 1 point, as in vanilla.", new AcceptableValueRange<float>(0f, 1f));
            DiscoverRequests = B(c, gi, "Requests must be discovered", true, "Girls don't post their problems on the board: you learn about them by letting her talk, asking her friends about her, or noticing (Intelligence). Requests pay in favor, trust, reputation and items; only jobs pay money.");
            DuoScenes = B(c, gi, "Two-girl scenes", true, "Sometimes two friends come over to talk to you together: friendship jealousy, introductions, a quarrel where you take a side, gossip told as a pair.");
            DuoInTalk = B(c, gi, "Two-girl scenes in the talk screen", true, "The two girls walk over and call you on the map, then the scene plays in the talk screen with both of them, and they say goodbye on the map. Off: everything plays on the map.");
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

            const string v = "10. Voice (TTS)";
            VoiceEnabled = B(c, v, "Enable voice", true, "Voice for Campus Life dialogue. The game's own scenes are not affected.");
            GirlVoice = B(c, v, "Girl voice", GirlVoiceMode.GameVoice, "GameVoice: each girl reacts with a fitting clip of her own Japanese voice (thanks, laugh, surprise, anger...) while the English text is shown. Tts: an English text-to-speech voice reads the whole line. Off: silent.");
            ShortGameClips = B(c, v, "Prefer short game clips", true, "GameVoice mode: prefer short reactions (a laugh, a gasp, a short thank-you) over long sentences that don't match the English text.");
            TtsFallback = B(c, v, "TTS when no game voice", true, "GameVoice mode: use the TTS voice for girls whose personality has no voice catalog (some modded personalities).");
            Engine = B(c, v, "Engine", TtsEngine.Piper, "TTS engine. Piper: open-source neural voices (installed in the Joojkk/piper plugin folder). Windows SAPI: built-in voices, robotic.");
            PiperExe = B(c, v, "Piper executable", "", "Full path to piper.exe. Empty = BepInEx\\plugins\\Joojkk\\piper\\piper.exe.");
            PiperModel = B(c, v, "Piper voice model", "", "Full path to the .onnx voice. Empty = first .onnx in BepInEx\\plugins\\Joojkk\\piper\\voices. Multi-speaker models (e.g. en_US-libritts_r-medium) give each girl her own voice.");
            PiperSpeakerMode = B(c, v, "Speaker selection", SpeakerMode.ByGirl, "ByGirl: every girl keeps her own speaker. ByPersonality: same personality = same voice. Fixed: one voice for all.");
            PiperSpeakerCount = B(c, v, "Speakers in model", 0, "0 = read from the model's .json. Used to spread girls across speakers.", new AcceptableValueRange<int>(0, 2000), true);
            PiperFixedSpeaker = B(c, v, "Fixed speaker id", 0, "Speaker used in Fixed mode.", new AcceptableValueRange<int>(0, 2000), true);
            PiperPlayerSpeaker = B(c, v, "Player speaker id", 261, "Speaker for the protagonist's lines (when enabled).", new AcceptableValueRange<int>(0, 2000), true);
            PiperNarratorSpeaker = B(c, v, "Narrator speaker id", 535, "Speaker for narration (when enabled).", new AcceptableValueRange<int>(0, 2000), true);
            LengthScale = B(c, v, "Speech length (slower > 1)", 1f, "Piper length scale. 0.8 = faster, 1.2 = slower.", new AcceptableValueRange<float>(0.5f, 2f));
            NoiseScale = B(c, v, "Expressiveness", 0.667f, "Piper noise scale. Higher = more varied intonation.", new AcceptableValueRange<float>(0f, 1.5f), true);
            Volume = B(c, v, "Volume", 0.9f, "", new AcceptableValueRange<float>(0f, 1f));
            PitchByTone = B(c, v, "Pitch by personality", true, "Slightly raises or lowers pitch by personality type (cheerful higher, cool lower).");
            PitchVariation = B(c, v, "Pitch variation", 0.06f, "How far pitch can move from normal.", new AcceptableValueRange<float>(0f, 0.25f));
            SpeakPlayer = B(c, v, "Speak protagonist lines", false, "");
            SpeakNarrator = B(c, v, "Speak narration", false, "");
            SapiFemaleVoice = B(c, v, "SAPI female voice", "", "Name of an installed Windows voice for girls (e.g. 'Microsoft Zira Desktop'). Empty = first female voice.", null, true);
            SapiMaleVoice = B(c, v, "SAPI male voice", "", "Windows voice for the protagonist and narrator. Empty = first male voice.", null, true);
            SapiRate = B(c, v, "SAPI rate", 0, "-10 slow .. 10 fast.", new AcceptableValueRange<int>(-10, 10), true);
            TtsCache = B(c, v, "Cache generated audio", true, "Keeps generated lines so they play instantly next time.");
            TtsCacheMaxMb = B(c, v, "Cache size limit (MB)", 300, "", new AcceptableValueRange<int>(20, 5000), true);

            const string pt = "11. Position tools";
            PositionReadout = B(c, pt, "Show position readout", false, "Shows the map number and your position and facing at the top left while walking around, to place things. The keys below only work while it is shown; each also copies the position to the clipboard and the log.");
            SaveWarpSpotKey = B(c, pt, "Set warp spot key", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl), "Makes the current spot the place where the menu's \"Go there\" buttons put you on this map. Saved in BepInEx/config/joojkk.campuslife.spots.txt.");
            MoveStoreKey = B(c, pt, "Move store key", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl, KeyCode.LeftShift), "Moves the school store icon to just in front of you (any map). Saved in the same spots file.");
        }
    }
}
