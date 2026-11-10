using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>What kind of beat a proposal is.</summary>
    internal enum BeatKind
    {
        Fixed,        // a school event, Sunday, the story heroine, a meeting the player set up: always
        Consequence,  // something the player did comes back (caught, rude, thanks, a rumor about him)
        Personal,     // a girl comes to the player for herself (episode, walk, a friend bringing a friend)
        Social,       // the girls' own lives (a situation from a mold, a quarrel, gossip)
        Ambient       // small things (a happening, a line in passing)
    }

    /// <summary>
    /// The director's agenda: every scene that could reach the player (duos, episodes, situations, map moments,
    /// happenings) is a proposal, and plays only when the agenda has room. Before, each source had its own roll and its
    /// own clock, and they could follow one another within seconds. Rules per period: Personal and Social together at
    /// most 2, 75 s apart; Ambient at most 2, 40 s apart; Consequence 30 s apart; Fixed always; nothing in the first 15
    /// s; a girl who comes to the player does so once a period (consequences aside).
    /// </summary>
    internal static class Agenda
    {
        private static int _stamp = -1;
        private static float _periodAt;
        private static int _story, _ambient;
        private static float _lastStory = -999f, _lastAmbient = -999f, _lastAny = -999f;
        private static readonly HashSet<SaveData.Heroine> _came = new HashSet<SaveData.Heroine>();
        private static readonly HashSet<string> _denied = new HashSet<string>();
        public static readonly List<string> Played = new List<string>();

        public const int StoryCap = 2, AmbientCap = 2;
        public const float StoryGap = 75f, AmbientGap = 40f, ConsequenceGap = 30f, Settle = 15f;

        private static int Stamp { get { return Campus.S.Day * 16 + (int)G.CurrentMask; } }

        private static void Roll()
        {
            var st = Stamp;
            if (st == _stamp) return;
            _stamp = st;
            _periodAt = Time.realtimeSinceStartup;
            _story = _ambient = 0;
            _came.Clear();
            _denied.Clear();
        }

        /// <summary>Called at each new period (Director.OnPeriod), so the settle time starts with the period.</summary>
        public static void NewPeriod() { _stamp = -1; Roll(); }

        /// <summary>May this proposal play now? Logs each refusal once per source and reason in the period.</summary>
        public static bool May(string source, BeatKind kind, params SaveData.Heroine[] cast)
        {
            try
            {
                Roll();
                var now = Time.realtimeSinceStartup;
                string why = null;
                if (kind != BeatKind.Fixed && now - _periodAt < Settle) why = "period just started";
                else if (kind == BeatKind.Consequence && now - _lastAny < ConsequenceGap) why = "another scene " + (int)(now - _lastAny) + " s ago";
                else if (kind == BeatKind.Personal || kind == BeatKind.Social)
                {
                    if (_story >= StoryCap) why = "period has its " + StoryCap + " scenes";
                    else if (now - _lastStory < StoryGap) why = "last scene " + (int)(now - _lastStory) + " s ago";
                    else if (now - _lastAny < ConsequenceGap) why = "another scene " + (int)(now - _lastAny) + " s ago";
                }
                else if (kind == BeatKind.Ambient)
                {
                    if (_ambient >= AmbientCap) why = "period has its " + AmbientCap + " small moments";
                    else if (now - _lastAmbient < AmbientGap || now - _lastAny < ConsequenceGap) why = "too soon after the last";
                }
                if (why == null && kind != BeatKind.Fixed && kind != BeatKind.Consequence && cast != null)
                {
                    var again = cast.FirstOrDefault(h => h != null && _came.Contains(h));
                    if (again != null) why = GirlInfo.FirstName(again) + " already came this period";
                }
                if (why == null) return true;
                var key = source.Split(' ')[0] + "|" + why.Split(' ')[0];
                if (_denied.Add(key)) CampusLifePlugin.Log.LogInfo("Agenda: ✕ " + source + " [" + kind + "]: " + why);
                return false;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Agenda: " + e.Message); return true; }
        }

        /// <summary>A proposal really started: it takes its place in the period.</summary>
        public static void Took(string source, BeatKind kind, params SaveData.Heroine[] cast)
        {
            try
            {
                Roll();
                var now = Time.realtimeSinceStartup;
                _lastAny = now;
                if (kind == BeatKind.Personal || kind == BeatKind.Social) { _story++; _lastStory = now; }
                else if (kind == BeatKind.Ambient) { _ambient++; _lastAmbient = now; }
                if (cast != null && kind != BeatKind.Consequence) foreach (var h in cast) if (h != null) _came.Add(h);
                var names = cast == null ? "" : string.Join(", ", cast.Where(h => h != null).Select(GirlInfo.FirstName).ToArray());
                var line = "Agenda: ▶ " + source + " [" + kind + "]" + (names != "" ? " " + names : "") + " | period: stories " + _story + "/" + StoryCap +
                           ", small " + _ambient + "/" + AmbientCap + ", " + (int)(now - _periodAt) + " s in";
                CampusLifePlugin.Log.LogInfo(line);
                Played.Add(Campus.S.Day + "/" + G.CurrentMask + " " + source + " [" + kind + "]" + (names != "" ? " " + names : ""));
                if (Played.Count > 60) Played.RemoveAt(0);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Agenda: " + e.Message); }
        }
    }
}
