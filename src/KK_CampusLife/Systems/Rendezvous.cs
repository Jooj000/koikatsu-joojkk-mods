using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// "Meet me at..." requests: when the player reaches the place in the right period, the girl actually comes.
    /// Uses the game's own summon (the phone call): she walks in through the nearest door and up to the player,
    /// and the request completes when they talk there.
    /// </summary>
    internal static class Rendezvous
    {
        private static string _place;
        private static float _arrivedAt;
        private static int _period = -1;
        private static readonly HashSet<int> Called = new HashSet<int>();

        /// <summary>Called about once a second from Calendar.Tick.</summary>
        public static void Tick()
        {
            if (!Campus.On || !G.InRoamingPeriod) return;
            var period = Campus.S.Day * 16 + (int)G.CurrentMask;
            if (period != _period) { _period = period; Called.Clear(); }
            var here = Places.CurrentId;
            if (here != _place) { _place = here; _arrivedAt = Time.realtimeSinceStartup; return; }
            // A few seconds after arriving, so the map and the students are settled.
            if (string.IsNullOrEmpty(here) || Time.realtimeSinceStartup - _arrivedAt < 3f || !G.IsFreeRoaming) return;
            var act = G.Act;
            foreach (var r in Board.Active.Where(x => x.Kind == RequestKind.MeetAt && x.Place == here && Board.InWindow(x)).ToList())
            {
                if (Called.Contains(r.Id)) continue;
                Called.Add(r.Id);
                var h = GirlInfo.Find(r.Girl);
                if (h == null) continue;
                if (act.fixChara != null && act.fixChara.heroine == h) continue; // she is today's event girl somewhere else
                var npc = act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
                if (npc != null && npc.mapNo == act.Map.no) continue; // already here
                try
                {
                    NPC.Summon(h, act, null);
                    Toasts.Show(GirlInfo.FirstName(h) + " is on her way.");
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Rendezvous: " + e.Message); }
                return; // one at a time
            }
        }
    }
}
