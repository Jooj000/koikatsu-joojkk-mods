using System;
using System.Collections.Generic;
using ActionGame.Chara;
using ActionGame.Point;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// interagir"). With a door and a girl both in reach, the game's door took the action key (its trigger runs before the
    /// scene's talk check, and the talk check gives way to any door). While a regular girl you can talk to is the one in
    /// your sights, the touch box of the action-key doors next to you is switched off; it comes back as soon as she is not.
    /// Walk-through doors (moveType 1) are never touched.
    /// </summary>
    internal static class TalkFirst
    {
        private static readonly List<Gate> Gates = new List<Gate>();
        private static readonly HashSet<BoxCollider> Off = new HashSet<BoxCollider>();
        private static int _map = -99;
        private static float _scan;
        public static string Last = "";

        public static void Update()
        {
            try
            {
                var act = G.Act;
                var p = act != null ? act.Player : null;
                if (p == null) { RestoreAll(); return; }
                var map = G.PlayerMapNo;
                if (map != _map || Time.realtimeSinceStartup > _scan)
                {
                    if (map != _map) RestoreAll();
                    _map = map;
                    _scan = Time.realtimeSinceStartup + 3f;
                    Gates.Clear();
                    foreach (var g in UnityEngine.Object.FindObjectsOfType<Gate>())
                        if (g != null && g.moveType == 0 && g.playerHitBox != null) Gates.Add(g);
                }
                var n = p.actionTarget as NPC;
                var talkable = n != null && n.heroine != null && n.heroine.fixCharaID == 0 && n.isAction && !p.isActionNow && !G.TalkSceneOpen;
                foreach (var g in Gates)
                {
                    if (g == null) continue;
                    var box = g.playerHitBox;
                    var near = talkable && (g.transform.position - p.position).sqrMagnitude < 5f * 5f;
                    if (near && box.enabled)
                    {
                        box.enabled = false;
                        Off.Add(box);
                        Last = "door to map " + g.mapNo + " waits: " + GirlInfo.FirstName(n.heroine) + " comes first";
                        CampusLifePlugin.Log.LogDebug("TalkFirst: " + Last);
                    }
                    else if (!near && Off.Contains(box))
                    {
                        box.enabled = true;
                        Off.Remove(box);
                    }
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("TalkFirst: " + e.Message); RestoreAll(); }
        }

        private static void RestoreAll()
        {
            foreach (var b in Off) try { if (b != null) b.enabled = true; } catch { }
            Off.Clear();
            Gates.Clear();
        }
    }
}
