using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using KKAPI.MainGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The game's story heroines only show up when their weekly event schedule allows, one per period.
    /// Campus Life lets them attend school every day as normal students (walking, chatting, requests), and a
    /// "director" picks one of them each school day: interacting with her that day opens her next story event.
    /// </summary>
    internal static class StoryCast
    {
        private static CampusState S { get { return Campus.S; } }

        private static readonly MethodInfo CheckOne = AccessTools.Method(typeof(FixEventScheduler), "Check",
            new[] { typeof(ActionScene), typeof(SaveData.Heroine), typeof(FixEventSchedule.Param), typeof(FixEventScheduler.Result).MakeByRefType() });

        public static IEnumerable<SaveData.Heroine> StoryGirls
        {
            get { return G.Heroines.Where(h => h != null && h.fixCharaID <= -5); }
        }

        // ---------------- the daily pick ----------------

        public static SaveData.Heroine Pick
        {
            get { return S.DirectorDay == S.Day ? GirlInfo.Find(S.DirectorPick) : null; }
        }

        private static FixEventSchedule.Param NextEvent(SaveData.Heroine h)
        {
            var act = G.Act;
            if (act == null || act.Cycle == null || act.Cycle.fixEventSchedule == null) return null;
            List<FixEventSchedule.Param> list;
            if (!act.Cycle.fixEventSchedule.TryGetValue(h.fixCharaID, out list) || list == null) return null;
            foreach (var p in list)
            {
                int asset;
                if (!int.TryParse(p.Asset, out asset)) continue;
                if (h.talkEvent.Contains(asset)) continue;
                return p;
            }
            return null;
        }

        public static bool HasPendingEvent(SaveData.Heroine h)
        {
            return h != null && !h.isTaked && NextEvent(h) != null;
        }

        /// <summary>Called once per school day (and on load if today has no pick yet).</summary>
        public static void ChooseToday()
        {
            if (!Cfg.StoryDirector.Value || S.DirectorDay == S.Day || !G.IsSchoolDay) return;
            var w = G.Weekday;
            if (w.HasValue && w.Value == Cycle.Week.Saturday) return; // the game has no story events on Saturdays
            // An event with no spot of its own is for a lesson: placed in a free period it has no waiting point and the load hangs.
            var candidates = StoryGirls.Where(HasPendingEvent).Where(h => !string.IsNullOrEmpty(NextEvent(h).LayerName)).ToList();
            S.DirectorDay = S.Day;
            if (candidates.Count == 0) { S.DirectorPick = ""; return; }
            // Whoever waited longest goes first; ties are random.
            var pick = candidates.OrderBy(h =>
            {
                int last;
                return S.Buffs.TryGetValue("dir:" + GirlInfo.Key(h), out last) ? last : -1;
            }).ThenBy(_ => Random.value).First();
            S.DirectorPick = GirlInfo.Key(pick);
            S.DirectorEvents = pick.talkEvent.Count;
            S.Buffs["dir:" + S.DirectorPick] = S.Day;
            CampusLifePlugin.Log.LogInfo("Today's story heroine: " + GirlInfo.FullName(pick));
        }

        private static bool DoneToday(SaveData.Heroine h) { return h.talkEvent.Count > S.DirectorEvents; }

        /// <summary>Today's pick while her event is still waiting (null otherwise).</summary>
        public static SaveData.Heroine Waiting
        {
            get
            {
                if (!Cfg.StoryDirector.Value) return null;
                var p = Pick;
                return p != null && !p.isTaked && !DoneToday(p) && NextEvent(p) != null ? p : null;
            }
        }

        private static SaveData.Heroine _periodGirl;
        private static int _periodAsset = -1;

        /// <summary>
        /// The story heroine the game (or the director) placed this period whose event has not played yet, or null. The
        /// game's own schedule can put a girl other than today's pick there (she was missing from the board).
        /// </summary>
        public static SaveData.Heroine ThisPeriod
        {
            get
            {
                var act = G.Act;
                var h = act != null && act.fixChara != null ? act.fixChara.heroine : null;
                if (h == null || h != _periodGirl || h.isTeacher || h.isTaked || !StoryGirls.Contains(h)) return null;
                return _periodAsset >= 0 && !h.talkEvent.Contains(_periodAsset) ? h : null;
            }
        }

        /// <summary>Story heroines to show on the board and the student cards: today's pick and this period's one.</summary>
        public static List<SaveData.Heroine> Shown
        {
            get
            {
                var list = new List<SaveData.Heroine>();
                var w = Waiting;
                if (w != null) list.Add(w);
                var p = ThisPeriod;
                if (p != null && !list.Contains(p)) list.Add(p);
                return list;
            }
        }

        /// <summary>Map where the game put this heroine for her event this period, or -1.</summary>
        public static int MapOf(SaveData.Heroine h)
        {
            var act = G.Act;
            if (act == null || h == null || act.fixChara == null || act.fixChara.heroine != h) return -1;
            try { return act.fixChara.mapNo; } catch { return -1; }
        }

        /// <summary>Map where the game put her for the event this period, or -1.</summary>
        public static int EventMap
        {
            get
            {
                var act = G.Act;
                var p = Waiting;
                if (act == null || p == null || act.fixChara == null || act.fixChara.heroine != p) return -1;
                try { return act.fixChara.mapNo; } catch { return -1; }
            }
        }

        /// <summary>Postfix of ActionScene.FindFixEventHeroine: today's pick gets her next event regardless of the weekly schedule.</summary>
        public static void AfterFindFix(ActionScene act, ref SaveData.Heroine result, ref FixEventScheduler.Result info)
        {
            Direct(act, ref result, ref info);
            _periodGirl = result;
            _periodAsset = -1;
            try { if (result != null && info != null && info.param != null) int.TryParse(info.param.Asset, out _periodAsset); }
            catch { _periodAsset = -1; }
            // Any story heroine the game (or the director) placed today stands there in person, not only today's pick.
            var r = result;
            if (Campus.On && r != null && !r.isTeacher && StoryGirls.Contains(r)) InPerson(ref info);
        }

        private static void Direct(ActionScene act, ref SaveData.Heroine result, ref FixEventScheduler.Result info)
        {
            if (!Campus.On || !Cfg.StoryDirector.Value || act == null || act.Cycle == null) return;
            if (act.Cycle.nowWeek == Cycle.Week.Saturday || act.Cycle.nowWeek == Cycle.Week.Holiday) return;
            ChooseToday();
            var pick = Pick;
            if (pick == null || pick.isTaked || DoneToday(pick)) return;
            if (result == pick) return;
            // Only in the periods you walk around (breaks, lunch, after school): she waits for you there.
            if (G.CurrentMask == PeriodMask.None) return;
            var next = NextEvent(pick);
            if (next == null || CheckOne == null) return;
            var relaxed = Copy(next);
            relaxed.AfterDay = 0;
            relaxed.Week = new string[0];
            relaxed.Cycle = new[] { act.Cycle.nowType.GetName() };
            var args = new object[] { act, pick, relaxed, null };
            bool ok;
            try { ok = (bool)CheckOne.Invoke(null, args); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Story event check failed: " + e.Message); return; }
            if (!ok || args[3] == null || ((FixEventScheduler.Result)args[3]).wp == null) return;
            result = pick;
            info = (FixEventScheduler.Result)args[3];
        }

        private static FixEventSchedule.Param Copy(FixEventSchedule.Param p)
        {
            return new FixEventSchedule.Param
            {
                Asset = p.Asset, Bundle = p.Bundle, isVisible = p.isVisible, Map = p.Map, LayerName = p.LayerName,
                Coordinate = p.Coordinate, AfterDay = p.AfterDay, Week = p.Week, Cycle = p.Cycle,
            };
        }

        /// <summary>
        /// Some events only put a star on the map (the girl is "hidden" until the scene starts). Story heroines stand
        /// there in person instead, so the player can see who is waiting. The schedule's own Param is never changed.
        /// </summary>
        private static void InPerson(ref FixEventScheduler.Result info)
        {
            if (!Cfg.StoryInPerson.Value || info == null || info.param == null || info.param.isVisible) return;
            var p = Copy(info.param);
            p.isVisible = true;
            info = info.wp != null ? new FixEventScheduler.Result(info.wp, info.layerIndex, p) : new FixEventScheduler.Result(info.mapNo, p);
        }

        /// <summary>
        /// The period's Fix and a walking student must never share one heroine: SaveData.Heroine points at a single body,
        /// and when the Fix goes away the student copy is left without one (the clipboard then fails to open).
        /// Called after the game's loading, when the period's Fix is known.
        /// </summary>
        private static void DropDuplicate(ActionScene act)
        {
            var fix = act.fixChara;
            var h = fix != null ? fix.heroine : null;
            if (h == null) return;
            var copies = act.npcList.Where(n => n != null && n.heroine == h).ToList();
            if (copies.Count == 0) return;
            foreach (var npc in copies)
            {
                act.npcList.Remove(npc);
                UnityEngine.Object.Destroy(npc.gameObject);
            }
            h.SetRoot(fix.gameObject);
            CampusLifePlugin.Log.LogDebug(GirlInfo.FullName(h) + " left the students: she is this period's story heroine.");
        }

        /// <summary>
        /// Before the clipboard opens: every listed student must point at her own body (the game reads heroine.charaBase
        /// for each card and the whole screen fails if one is missing). Repairs leftovers from older versions too.
        /// </summary>
        public static void RepairRoots(ActionScene act)
        {
            if (act == null) return;
            var fix = act.fixChara;
            if (fix != null && fix.heroine != null && fix.heroine.charaBase == null) fix.heroine.SetRoot(fix.gameObject);
            if (act.npcList == null) return;
            act.npcList.RemoveAll(n => n == null);
            foreach (var npc in act.npcList)
                if (npc.heroine != null && npc.heroine.charaBase == null) npc.heroine.SetRoot(npc.gameObject);
        }

        // ---------------- story finished: she joins a class ----------------

        private static string GradKey(SaveData.Heroine h) { return "grad:" + GirlInfo.Key(h); }

        public static bool Graduated(SaveData.Heroine h) { return h != null && S.Buffs.ContainsKey(GradKey(h)); }

        /// <summary>Her final scene played, or every event of her schedule was seen.</summary>
        internal static bool StoryOver(SaveData.Heroine h)
        {
            if (h.isTaked) return true;
            var act = G.Act;
            List<FixEventSchedule.Param> list;
            if (act == null || act.Cycle == null || act.Cycle.fixEventSchedule == null) return false;
            if (!act.Cycle.fixEventSchedule.TryGetValue(h.fixCharaID, out list) || list == null || list.Count == 0) return false;
            return NextEvent(h) == null;
        }

        // Closest everyday personality for each story heroine (everyday conversations exist only for those).
        private static readonly Dictionary<int, string[]> Likeness = new Dictionary<int, string[]>
        {
            { -5, new[] { "ミーハー", "ギャル", "気さく" } },        // Mai Himekawa, idol
            { -8, new[] { "後輩", "純真", "素直" } },               // Konomi Hiiragi, underclassman
            { -9, new[] { "不良", "姉御", "ボーイッシュ", "勝ち気" } }, // Sakura Yuuki, tough girl
            { -10, new[] { "正統派", "大和撫子", "誠実" } },          // Ai Minase, classic heroine
        };

        private static int EverydayPersonality(SaveData.Heroine h)
        {
            var names = Singleton<Manager.Game>.Instance.HeroinePersonalitys;
            // 0-29 are the base game's personalities: all of them have the full set of conversations.
            var usable = names.Where(kv => kv.Key >= 0 && kv.Key < 30 && !string.IsNullOrEmpty(kv.Value)).OrderBy(kv => kv.Key).ToList();
            if (usable.Count == 0) return 0;
            string[] wanted;
            if (Likeness.TryGetValue(h.fixCharaID, out wanted))
                foreach (var w in wanted)
                    foreach (var kv in usable)
                        if (kv.Value.Contains(w)) return kv.Key;
            return usable[Math.Abs(h.fixCharaID) % usable.Count].Key;
        }

        public static SaveData.Heroine FreeSeatFor(SaveData save) { return FreeSeat(save); }
        public static int EverydayPersonalityFor(SaveData.Heroine h) { return EverydayPersonality(h); }

        private static SaveData.Heroine FreeSeat(SaveData save)
        {
            var cls = save.player.schoolClass;
            foreach (var c in new[] { cls, cls + 1, cls - 1, cls + 2, cls - 2 })
            {
                if (c < 0) continue;
                var seat = save.GetFreeClassSlots(c).FirstOrDefault();
                if (seat != null) return seat;
            }
            return null;
        }

        private static int _noSeatDay = -1;

        /// <summary>
        /// Like KK_PantyFairy's fairy: once a story heroine's story is over she takes a free seat in a class as a regular
        /// student (a copy of her card with an everyday personality, so all normal conversations work), keeping what she
        /// built up with you. The story heroine herself stays in the save untouched for the game's own scenes.
        /// Runs before the students of a period are loaded.
        /// </summary>
        private static void GraduateFinished()
        {
            if (!Cfg.StoryGraduates.Value || !Singleton<Manager.Game>.IsInstance()) return;
            var save = Singleton<Manager.Game>.Instance.saveData;
            if (save == null) return;
            foreach (var h in StoryGirls.Where(x => !Graduated(x) && StoryOver(x)).ToList())
            {
                var seat = FreeSeat(save);
                if (seat == null)
                {
                    if (_noSeatDay != S.Day)
                    {
                        _noSeatDay = S.Day;
                        Toasts.Show(GirlInfo.FirstName(h) + "'s story is over and she wants to join a class, but every seat is taken. Free a seat in the class roster.");
                    }
                    return;
                }
                var card = new ChaFileControl();
                ChaFile.CopyChaFile(card, h.charFile);
                card.pngData = h.charFile.pngData;
                card.facePngData = h.charFile.facePngData; // left out by CopyChaFile
                card.parameter.personality = EverydayPersonality(h);
                seat.SetCharFile(card);
                seat.charFileInitialized = true;
                // Talk events 0-2 make her a friend (relation is derived from them), like the PantyFairy fairy; favor carries over.
                seat.talkEvent.Add(0);
                seat.talkEvent.Add(1);
                seat.talkEvent.Add(2);
                seat.favor = Mathf.Clamp(Math.Max(h.favor, 40), 0, 100);
                save.heroineList.Add(seat);

                var from = S.Girl(GirlInfo.Key(h));
                var to = S.Girl(GirlInfo.Key(seat));
                to.Devotion = from.Devotion;
                to.Talks = from.Talks;
                to.Gifts = from.Gifts;
                to.KnownLikes = from.KnownLikes;
                S.Buffs[GradKey(h)] = int.MaxValue;
                Toasts.Show(GirlInfo.FirstName(h) + "'s story is complete: she joined class " + Cycle.GetClassRoomName(seat.schoolClass)
                            + " as a regular student. You can talk to her like anyone else now.");
            }
        }

        // ---------------- everyone else walks around ----------------

        private static float _nextWave;

        /// <summary>Story heroines walking as students have no everyday conversations in the game: a click on one is dropped.</summary>
        public static void GuardInteraction(ref Base chara)
        {
            // Teachers: Campus Life's conversation with choices instead of the game's event (Fix or walking).
            if (Teachers.Click(chara)) { chara = null; return; }
            var npc = chara as NPC;
            if (npc == null || npc.heroine == null || npc.heroine.fixCharaID >= 0) return;
            chara = null;
            if (Time.realtimeSinceStartup < _nextWave) return;
            _nextWave = Time.realtimeSinceStartup + 4f;
            var msg = GirlInfo.FirstName(npc.heroine) + " smiles and waves, but she is busy right now.";
            var pick = Waiting;
            if (pick != null)
            {
                var map = EventMap;
                msg += " Today's story: " + GirlInfo.FirstName(pick) + (map >= 0 ? " at the " + Places.MapLabel(map) : "") + ".";
            }
            else msg += " Her story continues on the day she is chosen.";
            Toasts.Show(msg);
        }

        private static readonly FieldInfo DicTarget = AccessTools.Field(typeof(ActionControl), "dicTarget");
        private static readonly FieldInfo DicBase = AccessTools.Field(typeof(ActionControl), "dicBaseInfo");
        private static readonly FieldInfo DicPersonality = AccessTools.Field(typeof(ActionControl), "dicPersonalityInfo");
        private static readonly Type DesireInfo = AccessTools.Inner(typeof(ActionControl), "DesireInfo");

        /// <summary>
        /// The AI picks what a student does next from her "desires", which the game only keeps for regular students
        /// (fixCharaID 0). Story heroines walking around as students get the same entry, built the same way.
        /// </summary>
        private static bool HasTimetable(ActionScene act, SaveData.Heroine h)
        {
            ActionGame.FixCharaInfo.Param info;
            return act.fixCharaInfoDic != null && act.fixCharaInfoDic.TryGetValue(h.fixCharaID, out info) && info != null
                   && act.Cycle.classScheduleDic != null && act.Cycle.classScheduleDic.ContainsKey(info.ClassIndex);
        }

        private static bool GiveDesires(ActionScene act, SaveData.Heroine h)
        {
            var ctrl = act.actCtrl;
            var targets = DicTarget != null && ctrl != null ? DicTarget.GetValue(ctrl) as IDictionary : null;
            if (targets == null || DesireInfo == null) return false;
            if (targets.Contains(h)) return true;
            var personalities = DicPersonality.GetValue(ctrl) as IDictionary;
            if (personalities == null || !personalities.Contains(h.personality) || personalities[h.personality] == null) return false;
            var club = 0;
            try
            {
                var info = Manager.Game.GetClubInfo(h, true);
                if (info != null) club = info.ID;
            }
            catch { }
            targets[h] = Activator.CreateInstance(DesireInfo, DicBase.GetValue(ctrl), personalities[h.personality], club);
            return true;
        }

        /// <summary>
        /// The game fills the desires when the Action scene starts, so a seat taken after that (graduate, freed teacher) has
        /// none, and the Refresh of a loaded save drops those of the story heroines already walking. KK_SkinEffects reads
        /// them without checking: the throw kills NPC.Start, "initialized" never comes and NPCLoadAll waits forever
        /// ("Going to: Classroom 2-1"). It needs a result pending, so it only happened on some days.
        /// </summary>
        private static void NoOneWithoutDesires(ActionScene act)
        {
            var ctrl = act != null ? act.actCtrl : null;
            var targets = DicTarget != null && ctrl != null ? DicTarget.GetValue(ctrl) as IDictionary : null;
            if (targets == null) return;
            var who = Singleton<Manager.Game>.Instance.HeroineList.Where(h => h != null && h.fixCharaID == 0)
                .Concat(act.npcList.Where(n => n != null && n.heroine != null).Select(n => n.heroine)).Distinct()
                .Where(h => !targets.Contains(h)).ToList();
            foreach (var h in who)
            {
                var ok = GiveDesires(act, h);
                CampusLifePlugin.Log.LogInfo("Desires for " + GirlInfo.FullName(h) + (ok ? ": entry added before loading" : ": could not be added"));
            }
        }

        /// <summary>Wraps NPCLoadAll: after the game's own loading, the other story heroines join as normal students.</summary>
        public static IEnumerator Wrap(ActionScene act, IEnumerator original)
        {
            if (Campus.On)
            {
                try { GraduateFinished(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                try { Teachers.FreeFinished(); Teachers.MendPortraits(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                try { NoOneWithoutDesires(act); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            }
            while (true)
            {
                bool more;
                try { more = original.MoveNext(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); yield break; }
                if (!more) break;
                yield return original.Current;
            }
            if (!Campus.On || act == null) yield break;
            try { DropDuplicate(act); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            if (!Cfg.StoryGirlsWalk.Value) yield break;
            var fixH = act.fixChara != null ? act.fixChara.heroine : null;
            // Only girls whose real class (kept by the game in fixCharaInfoDic, see ClassLoan) has a timetable.
            var add = StoryGirls.Where(h => h != fixH && !h.isTaked && !Graduated(h) && !act.npcList.Any(n => n != null && n.heroine == h) && HasTimetable(act, h) && Scenery.Today(h)).ToList();
            // Teachers walk around too (ClassLoan lends them a timetable when the game has none for them).
            add.AddRange(Teachers.All.Where(h => h != fixH && Teachers.Walks(h) && !act.npcList.Any(n => n != null && n.heroine == h) && Scenery.Today(h)));
            if (add.Count == 0) yield break;
            var created = new List<NPC>();
            foreach (var h in add)
            {
                try
                {
                    // Desires first: the AI and other plugins look them up as soon as the NPC starts.
                    if (!GiveDesires(act, h)) continue;
                    var npc = NPC.Create(act.transform, h);
                    act.npcList.Add(npc);
                    npc.AI.Reset(true);
                    npc.LoadStart(false);
                    created.Add(npc);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogWarning("Story heroine " + GirlInfo.FullName(h) + " could not join: " + e.Message); }
            }
            // Still behind the loading screen; right after this the game calls SetAllCharaActive, which gives every
            // student (these too, see ClassLoan) her first action of the period.
            var t0 = Time.realtimeSinceStartup;
            while (created.Any(n => n != null && !n.initialized) && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            CampusLifePlugin.Log.LogInfo("Story heroines and professors walking around: " + created.Count + " (" + string.Join(", ", created.Where(n => n != null).Select(n => GirlInfo.FirstName(n.heroine)).ToArray()) + ")");
        }
    }
}
