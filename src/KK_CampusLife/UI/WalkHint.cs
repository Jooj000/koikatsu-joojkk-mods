using System;
using KKAPI.MainGame;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Walk and talk: a bar at the top of the screen for the whole walk (who is with you, the next stop and which door
    /// leads there) and a gold arrow over that door. Before, only a toast at the start said where to go (2026-10-11).
    /// </summary>
    internal static class WalkHint
    {
        private static Canvas _canvas;
        private static RectTransform _bar, _arrow;
        private static TMPro.TextMeshProUGUI _text;
        private static string _shown = "";
        private static float _nextRoute;
        private static Vector3? _door;
        private static string _doorTo = "";
        private static int _routeFrom = -1;
        private static string _routeStop = "";

        /// <summary>The text on screen now.</summary>
        public static string ShownText { get { return _canvas != null && _canvas.gameObject.activeSelf ? System.Text.RegularExpressions.Regex.Replace(_shown, "<[^>]+>", "") : ""; } }

        public static void Update()
        {
            try
            {
                var show = WalkTalk.Active && Campus.On && G.IsFreeRoaming && !Scenes.Running && !GameAPI.InsideHScene
                           && !TransitionScreen.Visible && G.Act != null && G.Act.Player != null;
                if (!show)
                {
                    if (_canvas != null && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
                    _shown = "";
                    return;
                }
                if (_canvas == null) Build();
                if (!_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
                WalkTalk.KeepPace();   // every frame: the game's follow sets her speed again on its own
                var stop = WalkTalk.NextStop;
                var target = Places.MapFor(stop);
                var here = G.PlayerMapNo;
                if (here != _routeFrom || stop != _routeStop || Time.realtimeSinceStartup > _nextRoute) FindDoor(here, target, stop);

                var gold = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Gold);
                var name = GirlInfo.FirstName(WalkTalk.Her);
                var text = "Walking with " + name + "  ·  next stop " + (WalkTalk.StopIndex + 1) + "/" + WalkTalk.StopCount + ": <color=" + gold + ">" + Places.Name(stop) + "</color>"
                           + (here == target ? "  ·  you're here" : _doorTo.Length > 0 ? "  ·  door to " + _doorTo : "");
                // What is said as you walk, under the bar, one line at a time.
                var flow = WalkTalk.Flow;
                if (flow != "") text += "\n<size=24><i>" + flow + "</i></size>";
                if (text != _shown)
                {
                    _shown = text;
                    _text.text = text;
                    var size = _text.GetPreferredValues(text);
                    _bar.sizeDelta = new Vector2(size.x + 40f, size.y + 16f);
                }
                PlaceArrow(here == target ? null : _door);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Walk hint: " + e.Message);
                if (_canvas != null) _canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>The first door on the game's own route from this map to the stop (the one the map arrows would show).</summary>
        private static void FindDoor(int here, int target, string stop)
        {
            _routeFrom = here;
            _routeStop = stop;
            _nextRoute = Time.realtimeSinceStartup + 2f;
            _door = null;
            _doorTo = "";
            if (target < 0 || target == here) return;
            var act = G.Act;
            var route = act.Map.SearchRoute(here, target, act.Player.position);
            if (route == null || route.IDs == null || route.IDs.Length == 0) return;
            ActionGame.Point.GateInfo gate;
            if (!act.Map.gateInfoDic.TryGetValue(route.IDs[0], out gate) || gate == null) return;
            _door = gate.pos + Quaternion.Euler(gate.ang) * gate.playerHitPos;
            _doorTo = G.MapName(gate.mapNo);
        }

        private static void PlaceArrow(Vector3? door)
        {
            var cam = Camera.main;
            if (door == null || cam == null) { _arrow.gameObject.SetActive(false); return; }
            var sp = cam.WorldToScreenPoint(door.Value + Vector3.up * 2.2f);
            if (sp.z <= 0.1f) { _arrow.gameObject.SetActive(false); return; } // behind the camera
            Vector2 local;
            var root = (RectTransform)_canvas.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out local)) { _arrow.gameObject.SetActive(false); return; }
            var half = root.rect.size * 0.5f;
            local.x = Mathf.Clamp(local.x, -half.x + 40f, half.x - 40f);
            local.y = Mathf.Clamp(local.y, -half.y + 60f, half.y - 120f);
            _arrow.gameObject.SetActive(true);
            _arrow.anchoredPosition = local + new Vector2(0f, Mathf.Sin(Time.realtimeSinceStartup * 4f) * 6f);
        }

        private static void Build()
        {
            _canvas = GameUi.OverlayCanvas("CampusLife_WalkHint", 380);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            var bg = GameUi.Image(_canvas.transform, "bar", null, GameUi.NoteBack);
            bg.raycastTarget = false;
            _bar = bg.rectTransform;
            _bar.anchorMin = _bar.anchorMax = new Vector2(0.5f, 1f);
            _bar.pivot = new Vector2(0.5f, 1f);
            _bar.anchoredPosition = new Vector2(0f, -86f);
            _text = GameUi.Text(_bar, "text", "", 22f, Color.white, TMPro.TextAlignmentOptions.Center, false);
            _text.enableWordWrapping = false;
            _text.raycastTarget = false;
            _text.rectTransform.anchorMin = Vector2.zero;
            _text.rectTransform.anchorMax = Vector2.one;
            _text.rectTransform.offsetMin = new Vector2(20f, 8f);
            _text.rectTransform.offsetMax = new Vector2(-20f, -8f);
            var arrow = GameUi.Text(_canvas.transform, "arrow", "▼", 46f, GameUi.Gold, TMPro.TextAlignmentOptions.Center, false);
            arrow.raycastTarget = false;
            _arrow = arrow.rectTransform;
            _arrow.anchorMin = _arrow.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.pivot = new Vector2(0.5f, 0f);
            _arrow.sizeDelta = new Vector2(80f, 60f);
            _arrow.gameObject.SetActive(false);
        }
    }
}
