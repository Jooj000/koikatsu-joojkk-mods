using System.Collections.Generic;
using ActionGame.Chara;
using KKAPI.MainGame;
using Manager;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Hair, skirt and accessory physics (DynamicBone) is the most expensive thing students do. Each bone simulates at
    /// 60 Hz and catches up with up to 3 steps per frame, so the lower the FPS the more it costs (a busy gym at 20 FPS
    /// pays 3 steps for every bone of every student). Near the camera nothing changes; farther away the physics takes
    /// exactly one step per frame (update rate 0), and past a set distance it stops (the game's own "distant disable").
    /// A lower fixed rate (the 20 Hz of 1.3/1.4) made hair jump while girls walked: on frames without a step the game
    /// only carries the hair along rigidly (SkipUpdateParticles), and the next step catches up at once. Students out of
    /// view stop too, judged by the camera's view with a margin, so the physics is back (and its reset done) before she
    /// comes into view. Only in free roaming: talks, H and events are untouched.
    /// Works together with IllusionFixes' "Throttle dynamic bone updates" (that one stops bones of unseen students).
    /// </summary>
    internal static class DynamicBoneLod
    {
        private sealed class Entry
        {
            public DynamicBone[] Bones;
            public float[] Rate;
            public bool[] Distant;
            public float[] Distance;
            public Transform[] Reference;

            public float Scanned;
            public int Tier = -1;
        }

        private static readonly Dictionary<ChaControl, Entry> Chars = new Dictionary<ChaControl, Entry>();
        private static bool _active;
        public static int Near, Mid, Far;

        public static void Apply()
        {
            var on = SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.BoneLod.Value && Roaming();
            if (!on)
            {
                if (_active) RestoreAll();
                return;
            }
            _active = true;
            var cam = Camera.main;
            if (cam == null) return;
            var act = Singleton<Game>.Instance.actScene;
            var camPos = cam.transform.position;
            _planes = GeometryUtility.CalculateFrustumPlanes(cam);
            float near = SchoolOptimizerPlugin.BoneFullDistance.Value, far = SchoolOptimizerPlugin.BoneOffDistance.Value;
            Near = Mid = Far = 0;
            foreach (var npc in act.npcList)
            {
                if (npc == null) continue;
                var cha = npc.chaCtrl;
                if (cha == null || !cha.loadEnd) continue;
                var d = Vector3.Distance(camPos, cha.transform.position);
                var tier = d < near ? 0 : d < far ? 1 : 2;
                Entry e;
                if (!Chars.TryGetValue(cha, out e) || Time.realtimeSinceStartup - e.Scanned > 15f || Stale(e))
                {
                    if (e != null) Restore(e);
                    e = Scan(cha);
                    Chars[cha] = e;
                }
                // Out of view (with a margin, so it is back before she shows up) and past full physics: off like the far ones.
                if (tier > 0 && SchoolOptimizerPlugin.BoneOffScreen.Value && !InView(cha)) tier = 3;
                if (tier == 0) Near++; else if (tier == 1) Mid++; else Far++;
                if (e.Tier == tier) continue;
                e.Tier = tier;
                for (var i = 0; i < e.Bones.Length; i++)
                {
                    var b = e.Bones[i];
                    if (b == null) continue;
                    // Mid range: one step every frame. Smooth (no frames without a step) and, below 60 FPS, cheaper.
                    b.m_UpdateRate = tier == 0 ? e.Rate[i] : 0f;
                    // Every bone checks its own distance each frame; the reference avoids a Camera.main lookup per bone.
                    b.m_DistantDisable = true;
                    b.m_DistanceToObject = tier == 3 ? 0f : far; // 0: the bone's own distance check always switches it off
                    b.m_ReferenceObject = cam.transform;
                }
            }
            if (Chars.Count > 80) Prune();
        }

        private static bool Roaming()
        {
            if (!Singleton<Game>.IsInstance()) return false;
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || !act.isActiveAndEnabled || act.npcList == null) return false;
            if (GameAPI.InsideHScene || act.isEventNow) return false;
            var scene = Singleton<Scene>.Instance;
            return !scene.IsNowLoading && !scene.IsNowLoadingFade;
        }

        private static Plane[] _planes = new Plane[0];
        private const float ViewMargin = 3f;

        /// <summary>Her body, grown by a margin, is inside the camera's view.</summary>
        private static bool InView(ChaControl cha)
        {
            var c = cha.objBodyBone != null ? cha.objBodyBone.transform.position : cha.transform.position;
            return GeometryUtility.TestPlanesAABB(_planes, new Bounds(c + Vector3.up * 0.8f, new Vector3(1f, 2f, 1f) + Vector3.one * (2f * ViewMargin)));
        }

        private static Entry Scan(ChaControl cha)
        {
            var bones = cha.GetComponentsInChildren<DynamicBone>(true);
            var e = new Entry
            {

                Bones = bones, Rate = new float[bones.Length], Distant = new bool[bones.Length], Distance = new float[bones.Length],
                Reference = new Transform[bones.Length], Scanned = Time.realtimeSinceStartup
            };
            for (var i = 0; i < bones.Length; i++)
            {
                e.Rate[i] = bones[i].m_UpdateRate;
                e.Distant[i] = bones[i].m_DistantDisable;
                e.Distance[i] = bones[i].m_DistanceToObject;
                e.Reference[i] = bones[i].m_ReferenceObject;
            }
            return e;
        }

        /// <summary>Outfit changes replace accessories and their bones.</summary>
        private static bool Stale(Entry e)
        {
            for (var i = 0; i < e.Bones.Length; i++) if (e.Bones[i] == null) return true;
            return false;
        }

        private static readonly System.Reflection.FieldInfo AsleepField = HarmonyLib.AccessTools.Field(typeof(DynamicBone), "m_DistantDisabled");
        public static int WokenWithReset;

        private static void Restore(Entry e)
        {
            for (var i = 0; i < e.Bones.Length; i++)
            {
                var b = e.Bones[i];
                if (b == null) continue;
                // A bone we put to sleep (far or out of view) keeps the particles of the moment it slept. DynamicBone only
                // resets them when its own distance check wakes it; turning m_DistantDisable off skips that check, so the
                // first step pulled the hair toward where she was before (another map, or before the talk moved her: the
                // second girl of a duo scene came out of the talk bald). Wake it with a reset.
                var asleep = false;
                try { asleep = b.m_DistantDisable && AsleepField != null && (bool)AsleepField.GetValue(b); } catch { }
                b.m_UpdateRate = e.Rate[i];
                b.m_DistantDisable = e.Distant[i];
                b.m_DistanceToObject = e.Distance[i];
                b.m_ReferenceObject = e.Reference[i];
                if (asleep && !b.m_DistantDisable)
                {
                    try
                    {
                        AsleepField.SetValue(b, false);
                        b.ResetParticlesPosition();
                        WokenWithReset++;
                    }
                    catch (System.Exception ex) { SchoolOptimizerPlugin.Log.LogDebug("Bone wake: " + ex.Message); }
                }
            }
            e.Tier = -1;
        }

        public static void RestoreAll()
        {
            _active = false;
            foreach (var kv in Chars)
            {
                try { if (kv.Key != null) Restore(kv.Value); }
                catch { }
            }
            Chars.Clear();
        }

        private static void Prune()
        {
            var dead = new List<ChaControl>();
            foreach (var kv in Chars) if (kv.Key == null) dead.Add(kv.Key);
            foreach (var k in dead) Chars.Remove(k);
        }
    }
}
