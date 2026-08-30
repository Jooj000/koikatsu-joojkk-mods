using System;
using HarmonyLib;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Makes the game's invitations agree with what the social director says about her today: a hungry girl who likes
    /// you goes to lunch, a sleepy one doesn't want to exercise, a lonely one wants to walk home with you. The game
    /// rolls each invitation with Info.RandomBranch([chance, 100 - chance]) inside Info.GetEventADV; while an
    /// invitation is being decided we shift that chance (never skipping the game's code).
    /// </summary>
    internal static class InviteOdds
    {
        // Event command numbers (TalkScene command - 10).
        private const int Confess = 1, H = 3, Lunch = 4, Club = 5, GoHome = 6, Date = 7, Study = 8, Exercise = 9, Follow = 11;

        private static int _command = -1;
        private static SaveData.Heroine _her;

        public static void Install(Harmony harmony)
        {
            var info = AccessTools.TypeByName("ActionGame.Communication.Info");
            var getEvent = info != null ? AccessTools.Method(info, "GetEventADV") : null;
            var roll = info != null ? AccessTools.Method(info, "RandomBranch") : null;
            if (getEvent == null || roll == null) { CampusLifePlugin.Log.LogWarning("Invitation odds: game methods not found, left as vanilla."); return; }
            harmony.Patch(getEvent, new HarmonyMethod(AccessTools.Method(typeof(InviteOdds), nameof(Before))), new HarmonyMethod(AccessTools.Method(typeof(InviteOdds), nameof(After))));
            harmony.Patch(roll, new HarmonyMethod(AccessTools.Method(typeof(InviteOdds), nameof(Roll))));
        }

        private static void Before(object __instance, int _command)
        {
            try
            {
                InviteOdds._command = _command;
                _her = Traverse.Create(__instance).Field("heroine").GetValue<SaveData.Heroine>();
            }
            catch { InviteOdds._command = -1; }
        }

        private static void After()
        {
            _command = -1;
            _her = null;
        }

        private static void Roll(int[] _values)
        {
            try
            {
                if (_command < 0 || _her == null || _values == null || _values.Length != 2 || _values[0] + _values[1] != 100) return;
                if (!Campus.On || !Cfg.Enabled.Value) return;
                int bias;
                var sure = Bias(_her, _command, out bias);
                if (bias == 0 && !sure) return;
                var before = _values[0];
                _values[0] = sure ? 100 : Mathf.Clamp(_values[0] + bias, 0, 100);
                _values[1] = 100 - _values[0];
                CampusLifePlugin.Log.LogDebug("Invitation " + _command + " to " + GirlInfo.FirstName(_her) + ": " + before + "% -> " + _values[0] + "% (her mood and trust)");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>How much today's mood and her trust change the chance; true when it is a sure yes.</summary>
        public static bool Bias(SaveData.Heroine h, int command, out int bias)
        {
            bias = 0;
            var s = Campus.S;
            var gs = s.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == s.Day ? gs.Mood : Mood.None;
            var romantic = command == Confess || command == Date || command == H;
            switch (mood)
            {
                case Mood.Cheerful: bias += romantic ? 10 : 15; break;
                case Mood.Hungry:
                    if (command == Lunch)
                    {
                        if (h.favor >= 40 || h.relation >= 1 || h.isGirlfriend) return true;
                        bias += 50;
                    }
                    break;
                case Mood.Bored:
                    if (command == Exercise || command == Club) bias += 30;
                    if (command == Follow) bias += 20;
                    if (command == Study) bias -= 15;
                    break;
                case Mood.Stressed:
                    if (command == Study) bias += 35;
                    if (command == Exercise) bias -= 10;
                    break;
                case Mood.Lonely:
                    if (command == GoHome) bias += 40;
                    if (command == Follow) bias += 30;
                    if (command == Lunch || command == Date) bias += 20;
                    break;
                case Mood.Flirty:
                    if (command == H) bias += 25;
                    if (command == Date) bias += 20;
                    if (command == Confess) bias += 15;
                    break;
                case Mood.Sleepy:
                    if (command == Exercise) bias -= 40;
                    if (command == Study) bias -= 15;
                    if (command == GoHome) bias += 20;
                    break;
                case Mood.Upset:
                    bias -= 20;
                    break;
            }
            var trust = Social.Trust(h);
            bias += Mathf.Clamp((trust - 50) / 5, -10, 10);
            if (Social.Grudge(h) != null) bias -= 15;
            return false;
        }
    }
}
