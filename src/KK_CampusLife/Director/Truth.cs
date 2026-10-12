using System;
using System.Linq;
using ActionGame.Chara;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// What is actually true in the game right now, for anything that tells the player about a girl: whether she came to
    /// school today and what she is doing this moment. The game draws who comes each day (ActionScene.NPCLoadAll takes a
    /// shuffled MaxCharaNum of the heroines); a girl with no NPC is not at school. User, 2026-10-26: a request for Tatsumaki
    /// on a day she was absent, "hanging out with Eris" with Eris absent, "getting something to eat" while she listened to
    /// music in the gym storage.
    /// </summary>
    internal static class Truth
    {
        public static NPC NpcOf(SaveData.Heroine h)
        {
            try { return h == null || G.Act == null || G.Act.npcList == null ? null : G.Act.npcList.FirstOrDefault(n => n != null && n.heroine == h); }
            catch { return null; }
        }

        /// <summary>The scene knows who came today (a school period with girls loaded).</summary>
        public static bool Known
        {
            get
            {
                try { return G.Act != null && G.Act.npcList != null && G.Act.npcList.Count > 0 && G.IsSchoolDay && G.InRoamingPeriod; }
                catch { return false; }
            }
        }

        /// <summary>She came to school today. True when the scene cannot tell (outside the school periods).</summary>
        public static bool AtSchool(SaveData.Heroine h)
        {
            if (h == null) return false;
            if (!GirlInfo.IsRegular(h) || !Known) return true; // story heroines and teachers have their own rules
            var n = NpcOf(h);
            return n != null && n.mapNo >= 0;
        }

        public static bool AtSchool(string key) { var h = Social.Get(key); return h == null || AtSchool(h); }

        /// <summary>
        /// do (on the way it is "going to the cafeteria to eat"), talking is who she talks to, and no "(place)" since the
        /// card shows the place above.
        /// </summary>
        public static string Doing(SaveData.Heroine h)
        {
            try
            {
                if (h == null || !Known) return "";
                var n = NpcOf(h);
                if (n == null || n.mapNo < 0) return "not on campus today";
                var ts = G.TalkScene;
                if (ts != null && ts.targetHeroine == h) return "talking with you";
                if (n.isLesH) return "slipped away with someone";
                if (n.isOnanism) return "somewhere private";
                var r = n.AI != null ? n.AI.result : null;
                var with = r != null && r.heroine != null && r.heroine != h && AtSchool(r.heroine) ? GirlInfo.FirstName(r.heroine) : "";
                var no = r != null ? r.actionNo : -1;
                if (n.isTalking)
                {
                    var p = TalkingWith(n);
                    return p != null ? "talking with " + GirlInfo.FirstName(p) : "chatting";
                }
                var moving = !n.isArrival && n.agent != null && n.agent.enabled && n.agent.velocity.sqrMagnitude > 0.04f;
                if (moving)
                {
                    var dest = r != null && r.point != null ? r.point.MapNo : n.mapNo;
                    var to = ActionTo(no, with);
                    var place = Places.MapLabel(dest);
                    if (dest != n.mapNo && place != "unknown") return "going to the " + place + (to != "" ? " " + to : "");
                    return to != "" ? "on her way " + to : "walking around";
                }
                var what = Action(no, with);
                if (no == 7 && with == "") return "waiting to chat";   // the game's chat with nobody next to her yet
                if (!string.IsNullOrEmpty(what)) return what;
                var where = Places.MapLabel(n.mapNo);
                return where == "unknown" ? "" : "hanging around";
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Truth.Doing: " + e.Message); return ""; }
        }

        /// <summary>The girl next to her in the talking motion (a chat pair, a group, a duo scene).</summary>
        private static SaveData.Heroine TalkingWith(NPC n)
        {
            var pos = n.transform.position;
            var other = G.Act.npcList.Where(x => x != null && x != n && x.heroine != null && x.mapNo == n.mapNo && x.isTalking && (x.transform.position - pos).sqrMagnitude < 6.25f)
                .OrderBy(x => (x.transform.position - pos).sqrMagnitude).FirstOrDefault();
            return other != null ? other.heroine : null;
        }

        /// <summary>What she is on her way to do ("to eat"), "" when the action says nothing about a trip.</summary>
        public static string ActionTo(int no, string with)
        {
            switch (no)
            {
                case 0: return "to change";
                case 1: return "to use the restroom";
                case 2: return "to shower";
                case 3: return "to eat";
                case 6: return "for club";
                case 7: return string.IsNullOrEmpty(with) ? "to chat" : "to chat with " + with;
                case 8: return "to talk to you";
                case 9: return "to read";
                case 13: return "to listen to music";
                case 16: return "for a drink";
                case 18: return "to exercise";
                case 21: return "to study";
                case 22: return "to nap";
                case 23: case 24: return "to find you";
                case 26: case 27: return string.IsNullOrEmpty(with) ? "to slip away" : "to slip away with " + with;
                case 28: case 29: return "to wait";
                default: return "";
            }
        }

        /// <summary>The game's action numbers (koikatsu-referencia-ia-das-alunas) as a short phrase.</summary>
        public static string Action(int no, string with)
        {
            switch (no)
            {
                case 0: return "changing clothes";
                case 1: return "in the restroom";
                case 2: return "taking a shower";
                case 3: return "eating";
                case 4: return "keeping to herself";
                case 5: return "restless";
                case 6: return "club activities";
                case 7: return string.IsNullOrEmpty(with) ? "chatting" : "talking with " + with;
                case 8: return "coming to talk to you";
                case 9: return "reading";
                case 10: return "on her phone";
                case 11: return "playing a phone game";
                case 12: return "taking selfies";
                case 13: return "listening to music";
                case 14: return "dancing";
                case 15: return "fixing her hair";
                case 16: return "having a drink";
                case 17: return "thinking it over";
                case 18: return "exercising";
                case 19: return "doing her own thing";
                case 20: return "running off";
                case 21: return "studying";
                case 22: return "napping";
                case 23: case 24: return "looking for you";
                case 25: return "feeling shy";
                case 26: case 27: return string.IsNullOrEmpty(with) ? "slipped away" : "slipped away with " + with;
                case 28: case 29: return "waiting";
                case 30: return "hesitating";
                default: return "";
            }
        }
    }
}
