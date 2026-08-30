using System;
using System.Collections.Generic;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Character pieces lying loose in the middle of the map (seen as a pile of clothes at the centre of the gym). The
    /// game creates every accessory at the scene root and attaches it to her only at the end of the loading coroutine
    /// (<c>ChaControl.ChangeAccessoryAsync</c>: <c>LoadCharaFbxDataAsync</c>, then <c>ChangeAccessoryParent</c>). When that
    /// coroutine is cut short (the character is reloaded again or released in between, or a step throws), the piece stays
    /// at the world origin forever, owned by nobody. Every 2 seconds, pieces at the scene root that no character points
    /// at are noted; still there on the next pass, they are destroyed. A piece being attached right now is at the root for
    /// a frame only, so it is never touched.
    /// </summary>
    internal static class Orphans
    {
        public static int Removed;
        private static HashSet<int> _seen = new HashSet<int>();
        private static readonly string[] Prefixes = { "ca_slot", "ct_", "ch_hair", "p_cf_head", "p_cm_head" };

        public static void Sweep()
        {
            if (!SchoolOptimizerPlugin.Enabled.Value || !SchoolOptimizerPlugin.OrphanCleanup.Value || !Singleton<Character>.IsInstance() ||
                KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.MainGame)
            {
                _seen.Clear();
                return;
            }
            var next = new HashSet<int>();
            HashSet<GameObject> owned = null;
            for (var s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go == null || !Piece(go.name) || go.GetComponent<ListInfoComponent>() == null) continue;
                    if (owned == null) owned = Owned();
                    if (owned.Contains(go)) continue;
                    var id = go.GetInstanceID();
                    if (!_seen.Contains(id)) { next.Add(id); continue; }
                    SchoolOptimizerPlugin.Log.LogInfo("Removed a piece left behind at " + go.transform.position.ToString("F1") + ": " + go.name + " (" + Describe(go) + ")");
                    UnityEngine.Object.Destroy(go);
                    Removed++;
                }
            }
            _seen = next;
        }

        private static bool Piece(string name)
        {
            foreach (var p in Prefixes)
                if (name.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Every piece some character (loaded or not, active or not) still points at.</summary>
        private static HashSet<GameObject> Owned()
        {
            var set = new HashSet<GameObject>();
            foreach (var cha in Singleton<Character>.Instance.dictEntryChara.Values)
            {
                if (cha == null) continue;
                Add(set, cha.objAccessory);
                Add(set, cha.objClothes);
                Add(set, cha.objParts);
                Add(set, cha.objHair);
                if (cha.objHead != null) set.Add(cha.objHead);
            }
            return set;
        }

        private static void Add(HashSet<GameObject> set, GameObject[] objs)
        {
            if (objs == null) return;
            foreach (var o in objs)
                if (o != null) set.Add(o);
        }

        private static string Describe(GameObject go)
        {
            try
            {
                var info = go.GetComponent<ListInfoComponent>().data;
                return info == null ? "?" : info.Name + ", category " + info.Category + ", id " + info.Id;
            }
            catch { return "?"; }
        }
    }
}
