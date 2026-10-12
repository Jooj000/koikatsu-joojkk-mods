using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Professors and the nurse walking around are staff, not students: the director keeps them
    /// at their own desk in the faculty room, the nurse in the infirmary, walking the corridors, at the cafeteria for lunch;
    /// never at a student's desk, a club or the showers. The game has a waiting point for each of them on many maps (layers
    /// </summary>
    internal static class StaffLife
    {
        private const int Staffroom = 17, Infirmary = 20, Library = 21, Courtyard = 33, Cafeteria = 38;
        private static readonly int[] Corridors = { 2, 3, 4 };
        private static readonly int[] LectureHalls = { 8, 9 };
        private static readonly int[] Social = { 5, 7, 8 };

        public static int Placed, Failed;
        private static bool _social;

        public static readonly Dictionary<string, string> Last = new Dictionary<string, string>();

        private sealed class Spot
        {
            public int Map;
            public string Layer;
            public float Weight;
            public Spot(int map, string layer, float weight) { Map = map; Layer = layer; Weight = weight; }
        }

        /// <summary>Her waiting layer, by the teacher she is (the freed copy by her original).</summary>
        private static string Desk(SaveData.Heroine h)
        {
            var name = h.charFile.parameter.fullname;
            var origin = h.isTeacher ? h : G.Heroines.FirstOrDefault(x => x != null && x.isTeacher && x.charFile.parameter.fullname == name);
            switch (origin != null ? origin.fixCharaID : -4)
            {
                case -1: return "FixHealth";
                case -2: return "FixMath";
                case -3: return "FixPhysical";
                default: return "FixHomeRoom";
            }
        }

        private static List<Spot> Spots(SaveData.Heroine h)
        {
            var desk = Desk(h);
            var lunch = G.CurrentMask == PeriodMask.Lunch;
            var l = new List<Spot>();
            if (desk == "FixHealth")
            {
                l.Add(new Spot(Infirmary, desk, lunch ? 30f : 60f));
                l.Add(new Spot(Infirmary, "Wait", 10f));
                l.Add(new Spot(Staffroom, desk, 10f));
            }
            else
            {
                l.Add(new Spot(Staffroom, desk, lunch ? 30f : 45f));
                l.Add(new Spot(Staffroom, "Reading", 8f));
                foreach (var m in LectureHalls) l.Add(new Spot(m, desk, 5f));
                l.Add(new Spot(Library, desk, 6f));
                l.Add(new Spot(Library, "Reading", 3f));
                l.Add(new Spot(Courtyard, desk, 3f));
            }
            foreach (var m in Corridors) l.Add(new Spot(m, "Wait", 8f));
            if (lunch)
            {
                l.Add(new Spot(Cafeteria, desk, 20f));
                l.Add(new Spot(Cafeteria, "Dinner", 15f));
            }
            return l;
        }

        /// <summary>From Brain.Decide: true when she got her staff's action. Every now and then she talks to someone instead.</summary>
        public static bool Decide(ActionControl ac, SaveData.Heroine h, int[] not, ref ActionControl.ResultInfo r, bool first)
        {
            if (_social || !Cfg.TeachersWalk.Value || !Teachers.IsTeacherVoice(h)) return false;
            try
            {
                if (!first && Random.value < 0.2f)
                {
                    _social = true;
                    try
                    {
                        var skip = Enumerable.Range(0, 31).Where(a => !Social.Contains(a)).Concat(not ?? new int[0]).ToArray();
                        if (Brain.Decide(ac, h, skip, ref r)) return true;
                    }
                    finally { _social = false; }
                }
                var npc = G.Act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
                if (npc == null) return false;
                var spots = Spots(h);
                while (spots.Count > 0)
                {
                    var roll = Random.value * spots.Sum(s => s.Weight);
                    var pick = spots[spots.Count - 1];
                    foreach (var s in spots) { roll -= s.Weight; if (roll <= 0f) { pick = s; break; } }
                    spots.Remove(pick);
                    if (Put(ac, h, npc, pick, ref r)) return true;
                }
                Failed++;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("StaffLife: " + e.Message); Failed++; }
            return false;
        }

        private static bool Put(ActionControl ac, SaveData.Heroine h, NPC npc, Spot spot, ref ActionControl.ResultInfo r)
        {
            List<WaitPoint> points;
            if (!G.Act.PosSet.waitPointDic.TryGetValue(spot.Map, out points) || points == null) return false;
            if (Weather.Shelters(spot.Map, h)) return false;
            var mine = npc.AI != null && npc.AI.result != null ? npc.AI.result.point : null;
            var free = points.Where(w => w != null && (w == mine || !w.isReserved) && w.parameterList.Any(p => p.layer == spot.Layer) && Brain.SpotTaken(w, h) == null).ToList();
            if (free.Count == 0) return false;
            var wp = free[Random.Range(0, free.Count)];
            // A way of filling the rest of the result (a game action with her table); the point is then ours.
            var res = ac.SetAction(h, 17, spot.Map);
            if (res == null) return false;
            var targets = Traverse.Create(ac).Field("dicTarget").GetValue() as System.Collections.IDictionary;
            var di = targets != null && targets.Contains(h) ? targets[h] : null;
            if (di == null) return false;
            Traverse.Create(di).Method("SetWaitPoint", new[] { typeof(WaitPoint), typeof(NPC) }).GetValue(wp, npc);
            res.point = wp;
            res.pointIndex = wp.parameterList.FindIndex(p => p.layer == spot.Layer);
            res.layerName = spot.Layer;
            res.notAction = new int[0]; // a teacher's desk motions are in no personality's "does not do" list; her motion must exist
            res.move = ActionGame.Chara.Mover.AgentSpeeder.Mode.Walk;
            r = res;
            Placed++;
            Last[GirlInfo.Key(h)] = spot.Layer + " at " + Places.MapLabel(spot.Map);
            return true;
        }

        /// <summary>The Fix motions of a desk: a teacher stays a good while, she does not get up every few seconds.</summary>
        public static void DeskTime(int id, ref float time)
        {
            if (id >= 200 && id < 220) time = Random.Range(60f, 120f);
        }
    }
}
