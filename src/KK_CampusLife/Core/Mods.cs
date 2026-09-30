using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// se o mesmo está presente, caso não esteja, a opção sai"). Anything that names one of them (an achievement, an
    /// option, a line) asks <see cref="Has"/> first; without the mod it does not show. The values are read by reflection,
    /// never linked at build time, so the mod loads the same without them (a game without the HF Patch). The full list
    /// of required and optional mods is in DEPENDENCIES.md at the repo root.
    /// </summary>
    internal static class Mods
    {
        public const string LewdCrestX = "LewdCrestX";      // KK_LewdCrestX, by ManlyMarco
        public const string PantyFairy = "PantyFairy";      // KK_PantyFairy, by ManlyMarco
        public const string Pregnancy = "KK_Pregnancy";     // KK_Pregnancy, by ManlyMarco

        /// <summary>The plugin with this GUID is loaded.</summary>
        public static bool Has(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return true;
            try { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid); }
            catch { return false; }
        }

        private static object Get(object obj, Type t, string name)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | (obj == null ? BindingFlags.Static : BindingFlags.Instance);
            var p = t.GetProperty(name, flags);
            if (p != null) return p.GetValue(obj, null);
            var f = t.GetField(name, flags);
            return f != null ? f.GetValue(obj) : null;
        }

        // ---------------- KK_LewdCrestX ----------------

        private static MethodInfo _crestOf;

        /// <summary>Her crest's name ("" for none, or without the mod).</summary>
        public static string Crest(SaveData.Heroine h)
        {
            if (h == null || !Has(LewdCrestX)) return "";
            try
            {
                if (_crestOf == null)
                {
                    var t = AccessTools.TypeByName("KK_LewdCrestX.Extensions");
                    _crestOf = t == null ? null : t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "GetCurrentCrest" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(SaveData.Heroine));
                    if (_crestOf == null) return "";
                }
                var v = _crestOf.Invoke(null, new object[] { h });
                var s = v != null ? v.ToString() : "";
                return s == "None" ? "" : s;
            }
            catch { return ""; }
        }

        // ---------------- KK_PantyFairy ----------------

        /// <summary>How far her story went: 0 not met, 1 met (first event or later), 2 finished. -1 without the mod.</summary>
        public static int FairyStage()
        {
            if (!Has(PantyFairy)) return -1;
            try
            {
                var t = AccessTools.TypeByName("KK_PantyFairy.CustomEvents");
                var p = t != null ? Get(null, t, "Progress") : null;
                if (p == null) return 0;
                var name = p.ToString();
                if (name == "Complete") return 2;
                return name == "Unknown" || name == "E1_Initial" ? 0 : 1;
            }
            catch { return 0; }
        }

        /// <summary>Pairs taken in all (her save data).</summary>
        public static int FairyHaul()
        {
            if (!Has(PantyFairy)) return 0;
            try
            {
                var t = AccessTools.TypeByName("KK_PantyFairy.CustomEvents");
                var data = t != null ? Get(null, t, "SaveData") : null;
                var v = data != null ? Get(data, data.GetType(), "PantiesStolenTotal") : null;
                return v != null ? Convert.ToInt32(v) : 0;
            }
            catch { return 0; }
        }

        // ---------------- KK_Pregnancy ----------------

        private static MethodInfo _pregOf;

        /// <summary>She is pregnant now or has been (0 without the mod).</summary>
        public static bool EverPregnant(SaveData.Heroine h)
        {
            if (h == null || !Has(Pregnancy)) return false;
            try
            {
                if (_pregOf == null)
                {
                    var t = AccessTools.TypeByName("KK_Pregnancy.PregnancyDataUtils");
                    _pregOf = t == null ? null : t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "GetPregnancyData" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(SaveData.Heroine));
                    if (_pregOf == null) return false;
                }
                var d = _pregOf.Invoke(null, new object[] { h });
                if (d == null) return false;
                var now = Get(d, d.GetType(), "IsPregnant");
                var count = Get(d, d.GetType(), "PregnancyCount");
                return now is bool && (bool)now || count != null && Convert.ToInt32(count) > 0;
            }
            catch { return false; }
        }

        public static string Describe()
        {
            return "mods: LewdCrestX " + Has(LewdCrestX) + ", PantyFairy " + Has(PantyFairy) + " (stage " + FairyStage() + ", haul " + FairyHaul() + "), Pregnancy " + Has(Pregnancy);
        }
    }
}
