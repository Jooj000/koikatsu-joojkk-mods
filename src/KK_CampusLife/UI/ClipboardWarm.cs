using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// prancheta porque sempre trava na primeira vez de abrir"): the places' thumbnails come out of the map asset bundles,
    /// the font and the sprites are looked up among every loaded object, and our icons are drawn pixel by pixel, all on
    /// that first frame. Now that work is done beforehand while you walk around, one small piece per frame (a thumbnail,
    /// a few icons), so the first open only lays out the cards. Each piece is timed; the log says what took longest.
    /// </summary>
    internal static class ClipboardWarm
    {
        private static readonly Queue<KeyValuePair<string, Action>> Jobs = new Queue<KeyValuePair<string, Action>>();
        private static bool _queued, _done;
        private static float _startAt;
        private static double _total, _worst;
        private static string _worstName = "";

        public static bool Done { get { return _done; } }

        public static void Tick()
        {
            try
            {
                if (_done) return;
                if (!Campus.On || G.Act == null || !G.IsFreeRoaming || StatusUi.Scene != null || TransitionScreen.Visible) { _startAt = 0f; return; }
                if (!_queued)
                {
                    // A few seconds after you can walk: the map's own loading is over by then.
                    if (_startAt <= 0f) { _startAt = Time.realtimeSinceStartup + 4f; return; }
                    if (Time.realtimeSinceStartup < _startAt) return;
                    Queue();
                    _queued = true;
                }
                var sw = Stopwatch.StartNew();
                // At most ~4 ms of work per frame.
                while (Jobs.Count > 0 && sw.Elapsed.TotalMilliseconds < 4.0)
                {
                    var j = Jobs.Dequeue();
                    var one = Stopwatch.StartNew();
                    try { j.Value(); }
                    catch (Exception e) { CampusLifePlugin.Log.LogDebug("Clipboard warm " + j.Key + ": " + e.Message); }
                    var ms = one.Elapsed.TotalMilliseconds;
                    _total += ms;
                    if (ms > _worst) { _worst = ms; _worstName = j.Key; }
                }
                if (Jobs.Count == 0)
                {
                    _done = true;
                    CampusLifePlugin.Log.LogInfo("Clipboard warm: ready (" + _total.ToString("0") + " ms of work spread over the walk; longest " + _worstName + " " + _worst.ToString("0") + " ms)");
                }
            }
            catch (Exception e) { _done = true; CampusLifePlugin.Log.LogDebug("Clipboard warm: " + e.Message); }
        }

        private static void Add(string name, Action a) { Jobs.Enqueue(new KeyValuePair<string, Action>(name, a)); }

        private static void Queue()
        {
            Add("font", () => { var f = GameUi.Font; });
            // (the clipboard's sprites are not loaded before it opens: looking for them now would only cost a search)
            foreach (var p in Places.All)
            {
                var map = Places.MapFor(p.Id);
                if (map >= 0) Add("thumbnail " + p.Id, () => PlaceThumbs.Get(map));
            }
            Add("store icon", () => StoreIcon.Get(true));
            foreach (var a in Activities.All)
            {
                var act = a;
                Add("icon " + a.Id, () => ActivityIcon.Get(ActivitySpots.KindOf(act), act.Id, true));
            }
            for (var i = 0; i < GirlInfo.LikeIds.Length; i++)
            {
                var k = i;
                Add("like badge " + i, () => ConvArt.Badge(IconArt.FirstLike + k));
            }
            Add("request badges", () => { foreach (var r in Board.Active.Take(12)) Markers.Badge(r); Markers.StoryBadge(); });
            Add("heart", TransitionScreen.WarmHeart);
        }

        /// <summary>A new game loaded: the next walk warms again what a scene change dropped.</summary>
        public static void Reset()
        {
            Jobs.Clear();
            _queued = _done = false;
            _startAt = 0f;
            _total = _worst = 0;
            _worstName = "";
        }
    }
}
