using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.MainGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Talk fatigue for the game's own questions (now under "Game topics"): they give the same favor every time, so
    /// from the 3rd conversation of the day with the same girl only part of it counts. The topic chat that lived here
    /// became Campus Life's conversation (<see cref="Conversation"/>).
    /// </summary>
    internal static class Chat
    {
        private static CampusState S { get { return Campus.S; } }

        public static bool IsOpen { get { return Conversation.IsOpen; } }

        public static void Close() { Conversation.Close(); }

        // ---------------- talk fatigue (the game's own questions) ----------------

        private static SaveData.Heroine _talkHer;
        private static int _startFavor;
        private static int _ownFavor;

        /// <summary>Every frame: remembers her favor when a conversation starts.</summary>
        public static void Track()
        {
            var ts = G.TalkScene;
            var h = ts != null ? ts.targetHeroine : null;
            if (h == null || h == _talkHer) return;
            _talkHer = h;
            _startFavor = h.favor;
            _ownFavor = 0;
        }

        /// <summary>Favor Campus Life itself gave during the conversation (gifts, chat) is not the game's.</summary>
        public static void NoteFavor(SaveData.Heroine h, int applied)
        {
            if (h != null && h == _talkHer) _ownFavor += applied;
        }

        /// <summary>
        /// The game's questions give the same favor every time, so repeating them all day was the best strategy.
        /// From the 3rd plain conversation of the day with the same girl, only half (then a quarter) of it counts.
        /// </summary>
        public static void OnTalkEnd(SaveData.Heroine h, GirlState gs, bool plainTalk)
        {
            if (gs.TalkDay != S.Day) { gs.TalkDay = S.Day; gs.TalksToday = 0; }
            gs.TalksToday++;
            var tracked = h == _talkHer;
            var gained = tracked ? h.favor - _startFavor - _ownFavor : 0;
            _talkHer = null;
            if (!tracked || !plainTalk || !Cfg.TalkFatigue.Value || gained <= 0 || gs.TalksToday <= 2) return;
            var keep = gs.TalksToday == 3 ? 0.5f : 0.25f;
            var cut = Mathf.RoundToInt(gained * (1f - keep));
            if (cut <= 0) return;
            h.favor = Mathf.Clamp(h.favor - cut, 0, 100);
            if (gs.TalksToday == 3)
                Toasts.Show(GirlInfo.FirstName(h) + " has answered plenty of questions today: more of the same counts less. Try a Chat, a gift, or come back tomorrow.");
        }
    }
}
