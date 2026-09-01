using System;
using System.Collections.Generic;
using ActionGame;
using HarmonyLib;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The game covers every map change and load with a plain white fade (Manager.Scene.sceneFade), which blinds and says
    /// nothing. While that fade is white (outside story scenes, or while something loads), this draws over it, following its alpha: a gray screen
    /// with a faint picture of the destination (the map's own thumbnail), a big red heart, "Going to: (place)" or
    /// "Loading game", and a tip or a little joke. White flashes inside story scenes (ADV) without a load are left alone.
    /// </summary>
    internal static class TransitionScreen
    {
        private static Canvas _canvas;
        private static CanvasGroup _group;
        private static Image _heart;
        private static RawImage _photo;
        private static AspectRatioFitter _photoFit;
        private static TextMeshProUGUI _title, _tip;
        private static Sprite _heartSprite;
        private static bool _on, _played;
        private static int _dest = -1, _shownDest = -2;
        private static float _destAt = -100f, _tipAt;
        private static readonly Dictionary<int, Sprite> Thumbs = new Dictionary<int, Sprite>();
        private static readonly Color Gray = new Color(0.31f, 0.31f, 0.34f, 1f);
        private const float TipSeconds = 7f;

        public static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ActionMap), nameof(ActionMap.Change), new[] { typeof(int), typeof(Scene.Data.FadeType) }), prefix: new HarmonyMethod(typeof(TransitionScreen), nameof(ChangePrefix)));
        }

        /// <summary>The destination of a map change (and its thumbnail, loaded once per map: a small picture).</summary>
        private static void ChangePrefix(ActionMap __instance, int no)
        {
            try
            {
                if (__instance == null || __instance.no == no) return;
                _dest = no;
                _destAt = Time.realtimeSinceStartup;
                if (Cfg.LoadingScreen.Value && !Thumbs.ContainsKey(no)) Thumbs[no] = LoadThumb(__instance, no);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Loading screen: " + e.Message); }
        }

        private static Sprite LoadThumb(ActionMap map, int no)
        {
            try
            {
                MapInfo.Param p;
                if (map.infoDic == null || !map.infoDic.TryGetValue(no, out p) || p == null || string.IsNullOrEmpty(p.ThumbnailBundle) || string.IsNullOrEmpty(p.ThumbnailAsset)) return null;
                var s = AssetBundleManager.LoadAsset(p.ThumbnailBundle, p.ThumbnailAsset, typeof(Sprite), null).GetAsset<Sprite>();
                if (s != null) return s;
                var t = AssetBundleManager.LoadAsset(p.ThumbnailBundle, p.ThumbnailAsset, typeof(Texture2D), null).GetAsset<Texture2D>();
                return t != null ? Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f)) : null;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Loading screen picture for map " + no + ": " + e.Message); return null; }
        }

        /// <summary>The loading screen is up (notifications wait until it is gone).</summary>
        public static bool Visible { get { return _on; } }

        public static void Update()
        {
            if (!Cfg.LoadingScreen.Value || !Singleton<Scene>.IsInstance()) { End(); return; }
            // "Loading game" until the story's action scene has been on screen once (from the title or a save load).
            if (G.Act == null) _played = false;
            else if (!_on && !_played && !Singleton<Scene>.Instance.IsNowLoadingFade) _played = true;
            var sc = Singleton<Scene>.Instance;
            var fade = sc.sceneFade;
            if (fade == null) { End(); return; }
            var c = fade._Color;
            var white = c.r > 0.85f && c.g > 0.85f && c.b > 0.85f;
            var alpha = fade.gameObject.activeInHierarchy && white ? c.a : 0f;
            if (!_on)
            {
                if (alpha < 0.01f || !Loading(sc)) return;
                Begin();
                if (!_on) return;
            }
            if (alpha <= 0.001f) { End(); return; }

            _group.alpha = alpha;
            Toasts.Hold(Time.unscaledDeltaTime);
            var order = fade.canvas != null ? fade.canvas.sortingOrder : 30000;
            _canvas.sortingOrder = Mathf.Min(order + 1, short.MaxValue);
            if (_destAt > Time.realtimeSinceStartup - 15f && _dest != _shownDest) ShowDestination();
            if (Time.realtimeSinceStartup - _tipAt > TipSeconds) NextTip();
            _heart.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.realtimeSinceStartup * 3.5f));
        }

        /// <summary>
        /// Should the white fade get our screen? Yes from its first frame when it is not part of a story scene (going to
        /// bed, map changes, period ends: the load only starts once the screen is white), and always when something loads.
        /// </summary>
        private static bool Loading(Scene sc)
        {
            try
            {
                if (sc.IsNowLoading || !ADV.Program.isADVProcessing) return true;
                var act = G.Act;
                if (act != null && act.Map != null && act.Map.isMapLoading) return true;
            }
            catch { }
            return Time.realtimeSinceStartup - _destAt < 3f;
        }

        private static void Begin()
        {
            try
            {
                if (_canvas == null) Build();
                _canvas.gameObject.SetActive(true);
                _on = true;
                _shownDest = -2;
                if (_destAt > Time.realtimeSinceStartup - 15f) ShowDestination();
                else
                {
                    _title.text = _played ? "Loading..." : "Loading game";
                    _photo.enabled = false;
                }
                NextTip();
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogWarning("Loading screen: " + e.Message);
                if (_canvas != null) Object.Destroy(_canvas.gameObject);
                _canvas = null;
                _on = false;
            }
        }

        private static void End()
        {
            if (!_on) return;
            _on = false;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private static void ShowDestination()
        {
            _shownDest = _dest;
            _title.text = "Going to: " + PlaceName(_dest);
            Sprite s;
            Thumbs.TryGetValue(_dest, out s);
            _photo.enabled = s != null && s.texture != null;
            if (!_photo.enabled) return;
            // The game's thumbnails carry their own title on top and a thin frame: only the picture below is shown.
            var tr = s.textureRect;
            float w = s.texture.width, hgt = s.texture.height;
            var crop = new Rect(tr.x + tr.width * 0.03f, tr.y + tr.height * 0.04f, tr.width * 0.94f, tr.height * 0.66f);
            _photo.texture = s.texture;
            _photo.uvRect = new Rect(crop.x / w, crop.y / hgt, crop.width / w, crop.height / hgt);
            _photoFit.aspectRatio = crop.width / Mathf.Max(1f, crop.height);
        }

        /// <summary>Our English name for school places; otherwise the game's own map name (XUnity translates it).</summary>
        private static string PlaceName(int no)
        {
            var raw = G.MapName(no);
            var id = Places.IdForMapName(raw);
            if (!string.IsNullOrEmpty(id) && id != "classroom")
                foreach (var p in Places.All) if (p.Id == id) return p.Name;
            return string.IsNullOrEmpty(raw) ? "somewhere" : raw;
        }

        private static void NextTip()
        {
            _tipAt = Time.realtimeSinceStartup;
            var list = Random.value < 0.5f ? LoadingLines.Tips : LoadingLines.Jokes;
            _tip.text = list[Random.Range(0, list.Length)];
        }

        private static void Build()
        {
            _canvas = GameUi.OverlayCanvas("CampusLife_LoadingScreen", 30000);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            _group = _canvas.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            var root = _canvas.transform;

            GameUi.Image(root, "gray", null, Gray).rectTransform.Stretch();

            // The destination's picture (the game's small map thumbnail, cheap to load and cached), half over the gray, filling the screen without stretching.
            var holder = GameUi.Rect(root, "photo_holder").Stretch();
            holder.gameObject.AddComponent<RectMask2D>();
            _photo = GameUi.Rect(holder, "photo").gameObject.AddComponent<RawImage>();
            _photo.color = new Color(0.92f, 0.92f, 0.95f, 0.62f); // clearly visible and barely tinted; the strip keeps the text readable
            _photo.raycastTarget = false;
            _photo.rectTransform.anchorMin = _photo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _photoFit = _photo.gameObject.AddComponent<AspectRatioFitter>();
            _photoFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _photoFit.aspectRatio = 16f / 9f;
            _photo.enabled = false;

            _heart = GameUi.Image(root, "heart", null, Color.white);
            _heart.sprite = _heartSprite ?? (_heartSprite = MakeHeart(256));
            var hr = _heart.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
            hr.pivot = new Vector2(0.5f, 0.5f);
            hr.sizeDelta = new Vector2(280f, 280f);
            hr.anchoredPosition = new Vector2(0f, 90f);

            // A soft dark strip behind the destination, so it reads over any picture.
            var strip = GameUi.Image(root, "where_strip", null, new Color(0f, 0f, 0f, 0.3f)).rectTransform;
            strip.anchorMin = new Vector2(0f, 0.5f);
            strip.anchorMax = new Vector2(1f, 0.5f);
            strip.pivot = new Vector2(0.5f, 0.5f);
            strip.sizeDelta = new Vector2(0f, 90f);
            strip.anchoredPosition = new Vector2(0f, -110f);

            _title = GameUi.Text(root, "where", "", 50f, Color.white, TextAlignmentOptions.Center, false);
            var tr = _title.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(1600f, 70f);
            tr.anchoredPosition = new Vector2(0f, -110f);

            var band = GameUi.Image(root, "tip_band", null, new Color(0f, 0f, 0f, 0.35f));
            var br = band.rectTransform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0f, 130f);
            br.anchoredPosition = Vector2.zero;
            _tip = GameUi.Text(band.transform, "tip", "", 30f, GameUi.Dim, TextAlignmentOptions.Center);
            _tip.rectTransform.Stretch(160f, 16f, 160f, 16f);

            _canvas.gameObject.SetActive(false);
        }

        /// <summary>A red heart drawn from its curve, (x²+y²-1)³ = x²y³, smoothed with 4 samples per pixel.</summary>
        private static Sprite MakeHeart(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp, name = "cl_loading_heart" };
            var px = new Color32[n * n];
            var top = new Color(1f, 0.36f, 0.45f);
            var bottom = new Color(0.78f, 0.04f, 0.16f);
            for (var y = 0; y < n; y++)
            {
                var col = Color.Lerp(bottom, top, (float)y / n);
                for (var x = 0; x < n; x++)
                {
                    var inside = 0;
                    for (var s = 0; s < 4; s++)
                    {
                        var u = ((x + 0.25f + 0.5f * (s & 1)) / n - 0.5f) * 2.7f;
                        var v = ((y + 0.25f + 0.5f * (s >> 1)) / n - 0.5f) * 2.7f + 0.12f;
                        var a = u * u + v * v - 1f;
                        if (a * a * a - u * u * v * v * v <= 0f) inside++;
                    }
                    col.a = inside / 4f;
                    px[y * n + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
