using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using Illusion.Game;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// usam o banheiro dos meninos ao invés de vestiário, chuveiro e banheiro feminino. ele também perde reputação se
    /// entrar nas áreas femininas, e não liga se o jogador o ver no masculino. meninas no masculino são pegas por eles
    /// também. o 'likes girls' de todos contam para todos").
    /// <list type="bullet">
    /// <item>The class roster lists male cards too: with BrowserFolders the folder tree starts at <c>chara</c>, so
    /// <c>male</c> sits next to <c>female</c>; without it, the male folder's cards are added to the list.</item>
    /// <item>A male card in a class seat gets a male body: the game creates every student as a girl, so the sex asked
    /// for is switched to the card's own (both bodies share the bone names, so the girls' motions play on him).</item>
    /// <item>He never goes to the girls' restroom, changing room or shower: any spot the game or the director gives him
    /// there is moved to the boys' restroom (changing clothes there too); with no spot there, he stays where he is.</item>
    /// <item>On a girls' map he loses reputation, more with girls there to see him; a girl in the boys' restroom with a
    /// boy there is caught (her secret, as when you find her) and he does not mind being seen there.</item>
    /// </list>
    /// Romance between students reads everyone's "likes girls" the same way (no rule of its own for boys).
    /// </summary>
    internal static class Boys
    {
        public static bool IsBoyCard(ChaFile f) { return f != null && f.parameter != null && f.parameter.sex == 0; }

        public static bool Is(SaveData.Heroine h) { return h != null && IsBoyCard(h.charFile); }

        public static int BodiesSwitched, SpotsMoved, StayedPut, MaleCardsListed;

        public static void Install(Harmony harmony)
        {
            var create = AccessTools.Method(typeof(Manager.Character), "CreateChara", new[] { typeof(byte), typeof(int), typeof(GameObject), typeof(int), typeof(ChaFileControl), typeof(bool) });
            if (create != null) harmony.Patch(create, prefix: new HarmonyMethod(typeof(Boys), nameof(CreatePrefix)));
            else CampusLifePlugin.Log.LogWarning("Character.CreateChara not found: boys in the class get a girl's body.");

            var getAction = AccessTools.Method(typeof(ActionControl), "GetAction");
            if (getAction != null) harmony.Patch(getAction, postfix: new HarmonyMethod(typeof(Boys), nameof(SpotPostfix)));
            var escape = AccessTools.Method(typeof(ActionControl), "EscapeAction");
            if (escape != null) harmony.Patch(escape, postfix: new HarmonyMethod(typeof(Boys), nameof(EscapePostfix)));

            var start = AccessTools.Method(typeof(ClassRoomCharaFile), "Start");
            if (start != null) harmony.Patch(start, prefix: new HarmonyMethod(typeof(Boys), nameof(RosterStartPrefix)) { priority = Priority.Low });
            var init = AccessTools.Method(typeof(ClassRoomCharaFile), "InitializeList");
            if (init != null) harmony.Patch(init, postfix: new HarmonyMethod(typeof(Boys), nameof(RosterListPostfix)));
        }

        // ================= his body =================

        private static void CreatePrefix(ref byte _sex, ChaFileControl _chaFile)
        {
            try
            {
                if (_sex != 1 || !IsBoyCard(_chaFile) || !Singleton<Game>.IsInstance()) return;
                // The maker and the free H list build their own characters; only story-mode students are switched.
                var scene = Singleton<Scene>.IsInstance() ? Singleton<Scene>.Instance.LoadSceneName ?? "" : "";
                if (scene == "CustomScene" || scene == "FreeH") return;
                _sex = 0;
                BodiesSwitched++;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys body: " + e.Message); }
        }

        // ================= the roster =================

        /// <summary>BrowserFolders' tree for the class list starts at chara/female; it now starts at chara.</summary>
        private static void RosterStartPrefix()
        {
            try
            {
                var t = AccessTools.TypeByName("BrowserFolders.Hooks.KK.ClassroomFolders");
                var tree = t != null ? AccessTools.Field(t, "_folderTreeView").GetValue(null) : null;
                if (tree == null) return;
                var tr = Traverse.Create(tree);
                var female = tr.Property("DefaultPath").GetValue<string>();
                if (string.IsNullOrEmpty(female)) return;
                var root = Path.GetDirectoryName(female.TrimEnd('/', '\\'));
                if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "male"))) return;
                tr.Property("DefaultPath").SetValue(root);
                tr.Property("CurrentFolder").SetValue(female);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys roster folders: " + e.Message); }
        }

        /// <summary>The game's list skips male cards: they are added after it.</summary>
        private static void RosterListPostfix(ClassRoomCharaFile __instance)
        {
            try
            {
                var tr = Traverse.Create(__instance);
                var listCtrl = tr.Field("listCtrl").GetValue<ClassRoomFileListCtrl>();
                var dic = tr.Field("chaFileDic").GetValue<Dictionary<int, ChaFileControl>>();
                if (listCtrl == null || dic == null) return;
                var folders = new List<string>();
                var bf = AccessTools.TypeByName("BrowserFolders.Hooks.KK.ClassroomFolders");
                var rel = bf != null ? AccessTools.Field(bf, "_currentRelativeFolder").GetValue(null) as string : null;
                if (rel != null) folders.Add(Path.Combine(UserData.Path, rel.TrimStart('/', '\\')));
                else folders.Add(Path.Combine(UserData.Path, "chara/male/"));
                var next = dic.Count == 0 ? 0 : dic.Keys.Max() + 1;
                var added = 0;
                var events = ExtensibleSaveFormat.ExtendedSave.LoadEventsEnabled;
                ExtensibleSaveFormat.ExtendedSave.LoadEventsEnabled = false;
                try
                {
                    foreach (var dir in folders)
                    {
                        if (!Directory.Exists(dir)) continue;
                        foreach (var path in Directory.GetFiles(dir, "*.png"))
                        {
                            var f = new ChaFileControl();
                            if (!f.LoadCharaFile(path, 255, false, true) || !IsBoyCard(f)) continue;
                            VoiceInfo.Param vp;
                            var personality = Singleton<Manager.Voice>.Instance.voiceInfoDic.TryGetValue(f.parameter.personality, out vp) ? vp.Personality : "不明";
                            ClubInfo.Param cp;
                            var club = Game.ClubInfos.TryGetValue(f.parameter.clubActivities, out cp) ? cp.Name : "不明";
                            listCtrl.AddList(next, f.parameter.fullname, club, personality, path, Path.GetFileNameWithoutExtension(path), File.GetLastWriteTime(path), false, false);
                            dic.Add(next, f);
                            next++;
                            added++;
                        }
                    }
                }
                finally { ExtensibleSaveFormat.ExtendedSave.LoadEventsEnabled = events; }
                if (added == 0) return;
                MaleCardsListed += added;
                listCtrl.Create(fic => { });
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys roster list: " + e.Message); }
        }

        // ================= where he goes =================

        /// <summary>Girls' restroom, girls' changing room or the shower.</summary>
        public static bool GirlsArea(int mapNo)
        {
            var name = G.MapName(mapNo) ?? "";
            var kind = Social.PlaceKind(name);
            return kind == "girls" || kind == "lockers" || name.Contains("シャワー") || name.Contains("Shower");
        }

        public static bool BoysRoom(int mapNo) { return Social.PlaceKind(G.MapName(mapNo) ?? "") == "boys"; }

        private static void SpotPostfix(ActionControl __instance, SaveData.Heroine _heroine, object _di, ref ActionControl.ResultInfo __result)
        {
            try { Redirect(_heroine, _di, ref __result); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys spot: " + e.Message); }
        }

        private static void EscapePostfix(ActionControl __instance, SaveData.Heroine _heroine, ref ActionControl.ResultInfo __result)
        {
            try
            {
                if (!Is(_heroine) || __result == null) return;
                var targets = Traverse.Create(__instance).Field("dicTarget").GetValue() as System.Collections.IDictionary;
                Redirect(_heroine, targets != null && targets.Contains(_heroine) ? targets[_heroine] : null, ref __result);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys escape: " + e.Message); }
        }

        /// <summary>A spot in the girls' areas becomes one in the boys' restroom; with none free, he stays put.</summary>
        private static void Redirect(SaveData.Heroine h, object di, ref ActionControl.ResultInfo r)
        {
            if (!Is(h) || r == null || r.point == null || !GirlsArea(r.point.MapNo)) return;
            var act = G.Act;
            if (act == null || act.PosSet == null) return;
            // Only a spot made for what he came to do: the game's one boys' restroom spot has no Wait or Toilet layer, and
            // falling back to its first layer played the wrong action there.
            var layer = r.layerName;
            var free = act.PosSet.waitPointDic.Where(kv => BoysRoom(kv.Key)).SelectMany(kv => kv.Value)
                .Where(p => p != null && !p.isReserved && p.parameterList != null && p.parameterList.Any(q => q.layer == layer)).ToList();
            var npc = act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
            if (free.Count == 0)
            {
                // No spot for him there: he waits where he is instead (the girls' spot is given back).
                if (di != null) Traverse.Create(di).Method("SetWaitPoint", new[] { typeof(WaitPoint), typeof(NPC) }).GetValue(null, null);
                r.point = null;
                r.pointIndex = -1;
                StayedPut++;
                return;
            }
            var wp = free[Random.Range(0, free.Count)];
            if (di != null) Traverse.Create(di).Method("SetWaitPoint", new[] { typeof(WaitPoint), typeof(NPC) }).GetValue(wp, npc);
            var idx = Math.Max(0, wp.parameterList.FindIndex(p => p.layer == layer));
            r.point = wp;
            r.pointIndex = idx;
            r.layerName = wp.parameterList[idx].layer;
            SpotsMoved++;
        }

        // ================= being seen =================

        private static readonly HashSet<string> Noted = new HashSet<string>();
        private static int _period = -1;

        /// <summary>About once a second (Calendar): a boy on a girls' map, a girl in the boys' restroom with a boy there.</summary>
        public static void Tick()
        {
            try
            {
                var act = G.Act;
                if (act == null || !Campus.On || !G.InRoamingPeriod || act.npcList == null) return;
                var period = Campus.S.Day * 16 + (int)G.CurrentMask;
                if (period != _period) { _period = period; Noted.Clear(); }
                var npcs = act.npcList.Where(n => n != null && n.heroine != null && n.mapNo >= 0 && n.isActiveAndEnabled).ToList();
                foreach (var b in npcs.Where(n => Is(n.heroine) && GirlsArea(n.mapNo)))
                {
                    var key = "b" + GirlInfo.Key(b.heroine);
                    if (!Noted.Add(key)) continue;
                    var seen = npcs.Where(n => n != b && n.mapNo == b.mapNo && !Is(n.heroine)).Select(n => n.heroine).ToList();
                    Standing.Add(b.heroine, -(4 + Math.Min(3, seen.Count) * 3), "in the girls' area (map " + b.mapNo + ")");
                    if (seen.Count > 0) Social.Add(Social.Kinds.SeenGirlsRoom, b.heroine, seen);
                    if (b.mapNo == G.PlayerMapNo)
                        Toasts.Show(GirlInfo.FirstName(b.heroine) + " is in the girls' " + (Social.PlaceKind(G.MapName(b.mapNo) ?? "") == "girls" ? "restroom" : "area") + "...? " + (seen.Count > 0 ? "The girls saw him." : "He'd better leave quickly."));
                }
                foreach (var g in npcs.Where(n => !Is(n.heroine) && GirlInfo.IsRegular(n.heroine) && BoysRoom(n.mapNo)))
                {
                    var boys = npcs.Where(n => n.mapNo == g.mapNo && Is(n.heroine)).Select(n => n.heroine).ToList();
                    if (boys.Count == 0 || !Noted.Add("g" + GirlInfo.Key(g.heroine))) continue;
                    Social.Add(Social.Kinds.HerBoysRoom, g.heroine, boys);
                    Standing.Add(g.heroine, -3, "caught in the boys' restroom by " + GirlInfo.FirstName(boys[0]));
                    if (g.mapNo == G.PlayerMapNo) Toasts.Show(GirlInfo.FirstName(boys[0]) + " caught " + GirlInfo.FirstName(g.heroine) + " in the boys' restroom. She looks mortified.");
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Boys tick: " + e.Message); }
        }
    }
}
