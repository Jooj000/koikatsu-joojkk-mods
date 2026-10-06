using System;
using System.Collections;
using KKAPI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A girl asleep on an infirmary bed: the game only says she is sleeping and ends the talk. Now,
    /// once that is over, you can tease her awake; half asleep she gives in far more easily than awake, and if she does,
    /// the game's H starts where she lies (as a girl can wake you with her hand when you nap there).
    /// </summary>
    internal static class SleepTease
    {
        private static TalkScene _seen;
        private static SaveData.Heroine _pending;

        /// <summary>Every frame, from Conversation.Watch.</summary>
        public static void Watch(TalkScene ts)
        {
            try
            {
                if (ts != null)
                {
                    if (ts == _seen) return;
                    _seen = ts;
                    var h = ts.targetHeroine;
                    if (!Campus.On || !Cfg.Enabled.Value || h == null || !GirlInfo.IsRegular(h) || ts.necessaryInfo == null || ts.necessaryInfo.state != 2) return;
                    if (Places.CurrentId != "infirmary") return;
                    _pending = h;
                    return;
                }
                _seen = null;
                if (_pending == null) return;
                var her = _pending;
                _pending = null;
                CampusLifePlugin.Instance.StartCoroutine(Offer(her).PreventFromCrashing());
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Sleep tease: " + e.Message); }
        }

        /// <summary>How likely she gives in, half asleep (much easier than the usual ask).</summary>
        public static float Chance(SaveData.Heroine h)
        {
            var q = GirlInfo.Quirks(h);
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == Campus.S.Day ? gs.Mood : Mood.None;
            var c = 0.3f + h.favor / 250f + h.lewdness / 180f;
            if (h.isGirlfriend) c += 0.4f;
            else if (h.relation >= 1) c += 0.12f;
            else if (h.relation < 0) c -= 0.2f;
            if (q.bitch) c += 0.2f;
            if (q.choroi) c += 0.1f;
            if (mood == Mood.Flirty) c += 0.15f;
            if (mood == Mood.Upset) c -= 0.15f;
            if (h.isAnger || Pairs.IsScorned(h)) c -= 0.5f;
            return Mathf.Clamp(c, 0.05f, 0.97f);
        }

        private static IEnumerator Offer(SaveData.Heroine h)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f && !Scenes.CanPlayOnMap) yield return null;
            if (!Scenes.CanPlayOnMap || Truth.NpcOf(h) == null) yield break;
            var name = GirlInfo.FirstName(h);
            var t = GirlInfo.ToneOf(h);
            var chance = Chance(h);
            var b = new SceneBuilder(null, false);
            b.Narr(name + " is fast asleep on the infirmary bed, breathing softly. The curtain is half drawn.");
            b.Choice("Tease her awake.", "tease", "Let her sleep.", "leave");
            b.Branch("leave");
            b.Narr("You pull the curtain closed and leave her be.");
            b.End();
            var yes = Random.value < chance;
            b.Branch("tease");
            b.Narr("You sit on the edge of the bed and brush the hair from her face, then let your hand wander.");
            if (yes)
            {
                b.Face(h, Feel.Love).Say(h, Lines.T(t, "Mmh... hm? ...Oh. It's you... Don't stop... || Nn... you're here... It's okay. Stay a little longer... || Mm... was I dreaming? ...No, it's really you. Don't go... || Hm...? Oh... your hands are warm. ...Keep going.", "Nnn... w-wha...? Hehe... sneaky. Come here, then. || Huh...? Hey, no fair, I was asleep! ...Fine, don't stop now! || Mmh... what time is— oh! Ehehe, okay, you caught me! Come closer! || W-wake up call? Ha... sneaky, sneaky. Well? Don't just stand there!", "...You woke me up. ...Take responsibility. || ...So this is how you wake people. ...Fine. Continue. || ...I was asleep. You knew that. ...Don't stop halfway. || ...Hm. Bold. ...I didn't say you could stop.", "Mm~ ...Is this how you wake up a girl? Then do it properly. || Mm~ ...took you long enough to try. Go on, then. || Ara... a sleeping girl and you couldn't resist? Cute. Keep going. || Nnh~ ...If you're going to wake me, at least make it worth it."));
                b.Narr("She pulls you down by your collar, still half asleep.");
            }
            else
            {
                b.Face(h, Feel.Surprised).Say(h, Lines.T(t, "Hya?! W-what are you doing?! || Eek?! Wh-what... why are you touching me while I sleep?! || Ah?! N-no, don't! I was sleeping...! || Hyah?! Th-that's not okay! Not while I'm asleep!", "WHOA! Hey! I was sleeping! || Hey-hey-HEY! What's the big idea?! || Whoa, whoa! Not cool! I was napping! || Yikes! Okay, NO. Hands where I can see them!", "...Hands. Off. Now. || ...Don't. Touch. Me. While I sleep. || ...Step back. Right now. || ...I was asleep. That was not an invitation.", "Ah-ah. Nice try. I'm awake now, you know. || Oho? Caught red-handed. Not today, sweetie. || Mm-mm. Sneaking up on me? You'll have to try harder. || Ah, ah, ah~ I'm awake. And the answer is no."));
                b.Narr("She sits up and pulls the blanket over herself.");
            }
            b.End();
            Scenes.PlayOnMap(b, c =>
            {
                if (c != "tease") return;
                CampusLifePlugin.Log.LogInfo("Sleep tease: " + name + " " + (yes ? "gives in" : "says no") + " (chance " + (int)(chance * 100) + "%)");
                if (yes)
                {
                    G.AddFavor(h, 2, 3);
                    Teachers.StartH(h);
                }
                else
                {
                    G.AddFavor(h, h.relation >= 1 ? -1 : -4, 1);
                    Social.Add(Social.Kinds.Flirted, h, Witness.Who(h, Witness.Flirt), Witness.Flirt);
                }
            });
        }
    }
}
