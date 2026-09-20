using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionGame;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Story heroines have no class of their own in the save (schoolClass -1); the game keeps it in
    /// ActionScene.fixCharaInfoDic. The student AI reads the timetable of heroine.schoolClass, so while one of its methods
    /// runs, the story heroines walking around as students borrow their real class, and get -1 back right after.
    /// Nothing is ever saved with the borrowed value.
    /// </summary>
    internal static class ClassLoan
    {
        private static int _depth;
        private static readonly List<KeyValuePair<SaveData.Heroine, int>> Lent = new List<KeyValuePair<SaveData.Heroine, int>>();

        public static void Install(Harmony harmony)
        {
            var targets = new List<MethodBase>
            {
                AccessTools.Method(typeof(ActionControl), "NextTime", new[] { typeof(int[]) }),
                AccessTools.Method(typeof(ActionControl), "FirstAction", new[] { typeof(SaveData.Heroine) }),
                AccessTools.Method(typeof(ActionControl), "AddDesire", new[] { typeof(SaveData.Heroine), typeof(int[]) }),
                AccessTools.Method(typeof(ActionControl), "NextAction", new[] { typeof(SaveData.Heroine), typeof(int[]) }),
                AccessTools.Method(typeof(ActionControl), "EscapeAction"),
                AccessTools.Method(typeof(ActionGame.Chara.AI), "CoordinateSetting"),
            };
            var prefix = new HarmonyMethod(AccessTools.Method(typeof(ClassLoan), nameof(Prefix)));
            var postfix = new HarmonyMethod(AccessTools.Method(typeof(ClassLoan), nameof(Postfix)));
            foreach (var m in targets)
            {
                if (m == null) continue;
                try { harmony.Patch(m, prefix, postfix); }
                catch (Exception e) { CampusLifePlugin.Log.LogWarning("Class loan hook on " + m.Name + ": " + e.Message); }
            }
        }

        private static void Prefix()
        {
            try
            {
                if (_depth++ > 0) return;
                var act = G.Act;
                if (act == null || act.npcList == null || act.fixCharaInfoDic == null) return;
                foreach (var npc in act.npcList)
                {
                    var h = npc != null ? npc.heroine : null;
                    if (h == null || h.fixCharaID > -5 && !h.isTeacher || h.schoolClass >= 0) continue;
                    FixCharaInfo.Param info;
                    int cls;
                    if (act.fixCharaInfoDic.TryGetValue(h.fixCharaID, out info) && info != null) cls = info.ClassIndex;
                    else if (h.isTeacher) cls = Math.Max(0, Singleton<Manager.Game>.Instance.Player.schoolClass); // a teacher walks with your class's timetable
                    else continue;
                    Lent.Add(new KeyValuePair<SaveData.Heroine, int>(h, h.schoolClass));
                    h.schoolClass = cls;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Class loan: " + e.Message); }
        }

        /// <summary>Called every frame: if a hooked method threw (postfix skipped), give the classes back anyway.</summary>
        public static void Sweep()
        {
            if (_depth == 0 && Lent.Count == 0) return;
            _depth = 1;
            Postfix();
        }

        private static void Postfix()
        {
            try
            {
                if (--_depth > 0) return;
                _depth = 0;
                foreach (var kv in Lent) kv.Key.schoolClass = kv.Value;
                Lent.Clear();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Class loan: " + e.Message); }
        }
    }
}
