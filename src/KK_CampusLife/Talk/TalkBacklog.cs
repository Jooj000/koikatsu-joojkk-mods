using System;
using System.Collections.Generic;
using ADV;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// diálogo individual"): every line of ours plays as its own little ADV scene, and the game clears its log (BackLog)
    /// when a scene starts, so the log only ever showed the last exchange. While one conversation lasts, what the log
    /// received is kept here and put back at the top of each new scene; a new girl (or no talk) starts it empty.
    /// </summary>
    internal static class TalkBacklog
    {
        private const int Max = 200;
        private static readonly List<BackLog.Data> Kept = new List<BackLog.Data>();
        private static SaveData.Heroine _her;
        private static bool _restoring;

        public static int Count { get { return Kept.Count; } }

        public static void Install(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(BackLog), "Add"), postfix: new HarmonyMethod(typeof(TalkBacklog), nameof(OnAdd)));
                harmony.Patch(AccessTools.Method(typeof(MainScenario), "Initialize"), postfix: new HarmonyMethod(typeof(TalkBacklog), nameof(OnInit)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Talk backlog: " + e.Message); }
        }

        /// <summary>The girl of the talk going on now, or null.</summary>
        private static SaveData.Heroine Current()
        {
            var ts = G.TalkScene;
            return ts != null ? ts.targetHeroine : null;
        }

        private static void OnAdd(BackLog.Data data)
        {
            try
            {
                if (_restoring || data == null) return;
                var h = Current();
                if (h == null) { Kept.Clear(); _her = null; return; }
                if (h != _her) { Kept.Clear(); _her = h; }
                Kept.Add(data);
                if (Kept.Count > Max) Kept.RemoveRange(0, Kept.Count - Max);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk backlog add: " + e.Message); }
        }

        private static void OnInit(MainScenario __instance)
        {
            try
            {
                var h = Current();
                if (h == null || h != _her) { Kept.Clear(); _her = h; return; }
                var log = __instance.BackLog;
                if (log == null || Kept.Count == 0) return;
                _restoring = true;
                try { foreach (var d in Kept) log.Add(d); }
                finally { _restoring = false; }
            }
            catch (Exception e) { _restoring = false; CampusLifePlugin.Log.LogDebug("Talk backlog init: " + e.Message); }
        }
    }
}
