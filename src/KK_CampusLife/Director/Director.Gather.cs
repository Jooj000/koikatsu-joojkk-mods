using System;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;
using UnityEngine.AI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// What happens between two girls happens where you can see it. When the director's situation has both girls on your
    /// map, one walks over to the other and they stand there talking (or arguing) for a while, facing each other, both
    /// stopped by the director. Talk to either one and the talk is with both, in the situation's mood (Duo.PullIn). Leave
    /// them be and after a while they come to you as before (Play); leave the map and they go on with their day, the
    /// situation still planned.
    /// </summary>
    internal static partial class Director
    {
        private static Situation _gathered;
        private static NPC _ga, _gb;
        private static float _gatherAt;
        private const float GatherWait = 45f;

        /// <summary>The girl she stands with in a gathered situation (null when none).</summary>
        internal static NPC GatherPartner(SaveData.Heroine h)
        {
            if (_gathered == null || h == null) return null;
            if (_ga != null && _ga.heroine == h) return _gb;
            if (_gb != null && _gb.heroine == h) return _ga;
            return null;
        }

        /// <summary>The mood of the talk with both, from the situation they were in.</summary>
        internal static string GatherMoment(SaveData.Heroine h)
        {
            if (GatherPartner(h) == null) return null;
            switch (_gathered.Mold.Id)
            {
                case "friction": case "negotiation": return "quarrel";
                case "sides": return "rivals";
                case "caught": return "caught";
                case "gossip": case "discovery": return "gossip";
                case "comfort": return "thanks";
                case "reunion": return "bestfriend";
                default: return "hangout";
            }
        }

        private static bool Quarrel(Situation s) { var m = s.Mold.Id; return m == "friction" || m == "negotiation" || m == "sides"; }

        /// <summary>From Tick, before the scene walks over to you: true when the two now meet on the map instead.</summary>
        private static bool TryGather(Situation s)
        {
            if (s == null || s.B == null || _gathered != null || G.Act == null || G.Act.Player == null) return false;
            var na = NpcOf(s.A);
            var nb = NpcOf(s.B);
            var map = G.PlayerMapNo;
            if (na == null || nb == null || na.mapNo != map || nb.mapNo != map || !na.gameObject.activeInHierarchy || !nb.gameObject.activeInHierarchy) return false;
            if (Halted(s.A) || Halted(s.B) || na.isLesH || nb.isLesH || na.isOnanism || nb.isOnanism) return false;
            if ((na.transform.position - nb.transform.position).sqrMagnitude > 30f * 30f) return false;
            // The one who has a reason to go (the actor) walks to the other; she stops 0.9 m short, on the navmesh.
            var dir = nb.transform.position - na.transform.position;
            dir.y = 0f;
            var spot = na.transform.position + (dir.sqrMagnitude > 0.01f ? dir.normalized : na.transform.forward) * 0.9f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spot, out hit, 1f, NavMesh.AllAreas)) spot = hit.position;
            var verb = Quarrel(s) ? "arguing with " : "talking with ";
            HaltFacing(nb, na, verb + GirlInfo.FirstName(s.A));
            Lead(na, spot, nb, verb + GirlInfo.FirstName(s.B));
            _gathered = s;
            _ga = na;
            _gb = nb;
            _gatherAt = Time.realtimeSinceStartup;
            CampusLifePlugin.Log.LogInfo("Director: " + s + " happens on the map: " + GirlInfo.FirstName(s.A) + " goes to " + GirlInfo.FirstName(s.B) + " (" + (na.transform.position - nb.transform.position).magnitude.ToString("0.0") + " m), " + (G.Act.Player.position - nb.transform.position).magnitude.ToString("0.0") + " m from you");
            return true;
        }

        /// <summary>Every Tick: the gathered two wait for you; you talked to one, walked away, or they come to you.</summary>
        private static void TickGather()
        {
            var s = _gathered;
            if (s == null) return;
            if (Duo.Busy) return; // the talk with both (PullIn) or a duo scene owns them now
            var map = G.PlayerMapNo;
            var gone = _ga == null || _gb == null || _ga.mapNo != map || _gb.mapNo != map || !G.Act.isActiveAndEnabled;
            if (gone)
            {
                CampusLifePlugin.Log.LogInfo("Director: " + s + " on the map is over (you left or a girl did); still planned");
                EndGather(false);
                return;
            }
            if (G.TalkSceneOpen || Scenes.Running) { _gatherAt = Time.realtimeSinceStartup; return; } // a talk with someone else
            if (Time.realtimeSinceStartup - _gatherAt < GatherWait || Walking(_ga)) return;
            CampusLifePlugin.Log.LogInfo("Director: " + s + ": you left them be; they come to you");
            EndGather(false);
            if (StillFits(s) && Agenda.May("situation " + s.Mold.Id, BeatKind.Social, s.A, s.B) && Play(s)) Agenda.Took("situation " + s.Mold.Id, BeatKind.Social, s.A, s.B);
        }

        /// <summary>The talk with both took it (PullIn): the situation is spent, as when it plays.</summary>
        internal static void GatherTalked(SaveData.Heroine h)
        {
            var s = _gathered;
            if (s == null || GatherPartner(h) == null) return;
            CampusLifePlugin.Log.LogInfo("Director: " + s + " went into the talk with both");
            Spent(s);
            if (_pending == s) _pending = null;
            foreach (var g in new[] { s.A, s.B }.Where(x => x != null)) { S.Girl(GirlInfo.Key(g)).DuoDay = S.Day; Changed(g, "played " + s.Mold.Id + " (with you, on the map)"); }
            LastPlayed = s.ToString();
            Agenda.Took("situation " + s.Mold.Id, BeatKind.Social, s.A, s.B);
            EndGather(true);
        }

        private static void EndGather(bool keepHeld)
        {
            if (!keepHeld) { Release(_ga); Release(_gb); }
            _gathered = null;
            _ga = _gb = null;
        }
    }
}
