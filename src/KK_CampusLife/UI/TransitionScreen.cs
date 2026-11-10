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
                    // The next day and the end of a period load with no map change, so the screen stayed plain gray. At
                    // night or from your room it is your room that shows (the game's own picture of it); otherwise the
                    // picture of the map you are on, when it has one.
                    // With no map (loading the game, or a map with no picture yet) it is your room.
                    {
                        // The default is the shipped photo 33_1; your room (at night, or map 0) is the shipped photo
                        // of it (0_2), the game's picture only without it.
                        var inAct = _played && G.Act != null;
                        var room = inAct && (EventBackdrop.TimeSlot() == 2 || G.PlayerMapNo == 0);
                        Texture pic = null;
                        var what = "";
                        try
                        {
                            if (room) { pic = MapShots.For(0); what = "your room (the map photo, "; if (pic == null) { pic = EventBackdrop.Picture("@room"); what = "your room (the game's picture, "; } }
                            else if (inAct) { pic = MapShots.For(G.PlayerMapNo); what = "the map you are on ("; }
                        }
                        catch { }
                        if (pic == null) { try { pic = DefaultPicture(); what = "the default, the entrance 33_1 ("; } catch { } }
                        if (pic != null)
                        {
                            _photo.enabled = true;
                            _photo.texture = pic;
                            _photo.uvRect = new Rect(0f, 0f, 1f, 1f);
                            _photoFit.aspectRatio = (float)pic.width / Mathf.Max(1, pic.height);
                            CampusLifePlugin.Log.LogInfo("Loading screen: " + what + pic.width + "x" + pic.height + ")");
                        }
                    }
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
            // The end of a period 12 s after a map change still said "Going to: Pool" (the 15 s window of that change): a
            // destination belongs to one loading only.
            _destAt = -100f;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private static void ShowDestination()
        {
            _shownDest = _dest;
            _title.text = "Going to: " + PlaceName(_dest);
            // The full-size picture of the destination when there is one (MapShots); the game's small thumbnail only until then.
            var big = MapShots.For(_dest);
            if (big != null)
            {
                _photo.enabled = true;
                _photo.texture = big;
                _photo.uvRect = new Rect(0f, 0f, 1f, 1f);
                _photoFit.aspectRatio = (float)big.width / Mathf.Max(1, big.height);
                return;
            }
            // "o problema era na piscina, quarto, etc". Your room is never walked in (the night menu), so it never gets a
            // picture of its own: the game's own full-size picture of it is shown.
            var adv = GamePicture(_dest);
            if (adv != null)
            {
                CampusLifePlugin.Log.LogInfo("Loading screen: going to map " + _dest + ", the game's own picture of it (" + adv.width + "x" + adv.height + ")");
                _photo.enabled = true;
                _photo.texture = adv;
                _photo.uvRect = new Rect(0f, 0f, 1f, 1f);
                _photoFit.aspectRatio = (float)adv.width / Mathf.Max(1, adv.height);
                return;
            }
            Sprite s;
            Thumbs.TryGetValue(_dest, out s);
            _photo.enabled = s != null && s.texture != null;
            if (!_photo.enabled) return;
            // The game's thumbnails carry their own title on top and a thin frame: only the picture below is shown.
            var tr = s.textureRect;
            float w = s.texture.width, hgt = s.texture.height;
            var crop = new Rect(tr.x + tr.width * 0.03f, tr.y + tr.height * 0.04f, tr.width * 0.94f, tr.height * 0.66f);
            _photoFit.aspectRatio = crop.width / Mathf.Max(1f, crop.height);
            // The thumbnail is a few hundred pixels wide, so blown up to the screen its pixels showed as blocks. Until the
            // map has its own picture it is shown blurred (a real blur at 384 wide), like a photo out of focus behind the
            // heart, never as squares.
            var soft = SoftThumb(_dest, s.texture, crop);
            if (soft != null) { _photo.texture = soft; _photo.uvRect = new Rect(0f, 0f, 1f, 1f); return; }
            _photo.texture = s.texture;
            _photo.uvRect = new Rect(crop.x / w, crop.y / hgt, crop.width / w, crop.height / hgt);
        }

        /// <summary>One box blur pass along rows (or columns) with radius r, edges clamped.</summary>
        private static void BoxBlur(Color32[] px, int w, int h, int r, bool rows)
        {
            int n = rows ? w : h, lines = rows ? h : w, win = 2 * r + 1;
            var line = new Color32[n];
            for (var L = 0; L < lines; L++)
            {
                for (var i = 0; i < n; i++) line[i] = px[rows ? L * w + i : i * w + L];
                int sr = 0, sg = 0, sb = 0;
                for (var k = -r; k <= r; k++) { var c = line[Mathf.Clamp(k, 0, n - 1)]; sr += c.r; sg += c.g; sb += c.b; }
                for (var i = 0; i < n; i++)
                {
                    px[rows ? L * w + i : i * w + L] = new Color32((byte)(sr / win), (byte)(sg / win), (byte)(sb / win), 255);
                    var add = line[Mathf.Min(i + r + 1, n - 1)];
                    var sub = line[Mathf.Max(i - r, 0)];
                    sr += add.r - sub.r; sg += add.g - sub.g; sb += add.b - sub.b;
                }
            }
        }

        /// <summary>The loading screen's picture when there is no map to show: his 33_1, any 33 next, the room
        /// last.</summary>
        private static Texture DefaultPicture()
        {
            return (Texture)MapShots.Exact(33, 1) ?? (Texture)MapShots.For(33) ?? EventBackdrop.Picture("@room");
        }

        /// <summary>The game's ADV picture of a map the player never roams (his room), null for the others.</summary>
        private static Texture2D GamePicture(int no)
        {
            try
            {
                var raw = G.MapName(no) ?? "";
                if (raw.Contains("自室")) return EventBackdrop.Picture("@room");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Loading screen game picture " + no + ": " + e.Message); }
            return null;
        }

        private static readonly Dictionary<int, Texture2D> Softs = new Dictionary<int, Texture2D>();

        /// <summary>The thumbnail's picture part, shrunk to a sixth in two smooth steps (a blur that stretches without blocks).</summary>
        private static Texture2D SoftThumb(int no, Texture src, Rect crop)
        {
            Texture2D t;
            if (Softs.TryGetValue(no, out t)) return t;
            t = null;
            var t0 = Time.realtimeSinceStartup;
            var prev = RenderTexture.active;
            RenderTexture full = null, small = null;
            Texture2D cut = null;
            try
            {
                full = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
                full.filterMode = FilterMode.Bilinear;
                Graphics.Blit(src, full);
                RenderTexture.active = full;
                int cw = Mathf.Max(8, (int)crop.width), ch = Mathf.Max(8, (int)crop.height);
                cut = new Texture2D(cw, ch, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                cut.ReadPixels(new Rect(crop.x, crop.y, cw, ch), 0, 0);
                cut.Apply();
                // Shrunk to 50x19 and drawn back up, the bilinear steps still showed as soft squares. The picture is drawn
                // up to 384x216 and blurred for real (three box passes each way, close to a gaussian), so stretched to the
                // screen it reads as a photo out of focus.
                int w = 384, h = Mathf.Clamp(Mathf.RoundToInt(384f * ch / cw), 96, 384);   // the thumbnail's own shape
                small = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                small.filterMode = FilterMode.Bilinear;
                Graphics.Blit(cut, small);
                RenderTexture.active = small;
                t = new Texture2D(w, h, TextureFormat.RGB24, false) { name = "cl_softthumb_" + no, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                var px = t.GetPixels32();
                for (var pass = 0; pass < 3; pass++) { BoxBlur(px, w, h, 9, true); BoxBlur(px, w, h, 9, false); }
                t.SetPixels32(px);
                t.Apply();
                Object.DontDestroyOnLoad(t);
                CampusLifePlugin.Log.LogInfo("Loading screen: map " + no + " has no picture of its own yet, its " + cw + "x" + ch + " thumbnail is shown blurred (" + w + "x" + h + ", " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms)");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Loading screen soft picture " + no + ": " + e.Message); t = null; }
            finally
            {
                RenderTexture.active = prev;
                if (full != null) RenderTexture.ReleaseTemporary(full);
                if (small != null) RenderTexture.ReleaseTemporary(small);
                if (cut != null) Object.Destroy(cut);
            }
            Softs[no] = t;
            return t;
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
            _photo.color = new Color(0.92f, 0.92f, 0.95f, 0.62f); // clearly visible and barely tinted (found 62 % brightness too dark); the strip keeps the text readable
            _photo.raycastTarget = false;
            _photo.rectTransform.anchorMin = _photo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _photoFit = _photo.gameObject.AddComponent<AspectRatioFitter>();
            _photoFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _photoFit.aspectRatio = 16f / 9f;
            _photo.enabled = false;

            _heart = GameUi.Image(root, "heart", null, Color.white);
            _heart.sprite = _heartSprite ?? (_heartSprite = MakeHeart(HeartSize));
            _heart.preserveAspect = true;
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

        // The card's heart is a 53x47 sprite of the game's atlas, so copying it and stretching it to 280 px looked
        // blocky. The loading heart is drawn here in the same look (white border, maroon outline, pink fill with a lit rim
        // and a gloss) at 512 px with mipmaps.
        private const int HeartSize = 512;

        /// <summary>Kept for the clipboard's call: the card's heart is no longer copied (it is too small to show big).</summary>
        public static void OfferGameHeart(ChaStatusComponent comp) { }

        /// <summary>The loading heart made ahead of time (ClipboardWarm).</summary>
        public static void WarmHeart() { if (_heartSprite == null) _heartSprite = MakeHeart(HeartSize); }

        /// <summary>Signed distance to a heart with its tip at (0,0) and its lobes up to y≈1.06 (negative inside).</summary>
        private static float HeartDist(float x, float y)
        {
            x = Mathf.Abs(x);
            if (y + x > 1f) return Mathf.Sqrt((x - 0.25f) * (x - 0.25f) + (y - 0.75f) * (y - 0.75f)) - 0.35355f;
            var a = x * x + (y - 1f) * (y - 1f);
            var m = 0.5f * Mathf.Max(x + y, 0f);
            var b = (x - m) * (x - m) + (y - m) * (y - m);
            return Mathf.Sqrt(Mathf.Min(a, b)) * (x - y > 0f ? 1f : -1f);
        }

        private static float Smooth(float e0, float e1, float x)
        {
            var t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>The girlfriend heart in the game's card style, n×n, edges smoothed by the distance itself.</summary>
        private static Sprite MakeHeart(int n)
        {
            var t0 = Time.realtimeSinceStartup;
            var tex = new Texture2D(n, n, TextureFormat.ARGB32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, name = "cl_loading_heart" };
            var px = new Color[n * n];
            var white = Color.white;
            var maroon = new Color(0.392f, 0.039f, 0.039f);
            var fill = new Color(1f, 0.404f, 0.541f);
            var light = new Color(1f, 0.729f, 0.906f);
            var dark = new Color(0.69f, 0.243f, 0.337f);
            const float span = 1.62f, border = 0.075f, outline = 0.045f;
            var aa = span / n;
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var u = ((x + 0.5f) / n - 0.5f) * span;
                    var v = ((y + 0.5f) / n) * span - 0.30f;
                    var d = HeartDist(u, v);
                    if (d > border + aa) { px[y * n + x] = new Color(1f, 1f, 1f, 0f); continue; }
                    Color c;
                    if (d > outline) c = Color.Lerp(maroon, white, Smooth(outline - aa, outline + aa, d));
                    else if (d > 0f) c = maroon;
                    else
                    {
                        var ex = HeartDist(u + 0.01f, v) - HeartDist(u - 0.01f, v);
                        var ey = HeartDist(u, v + 0.01f) - HeartDist(u, v - 0.01f);
                        var len = Mathf.Sqrt(ex * ex + ey * ey);
                        if (len < 1e-5f) len = 1f;
                        var lit = -ex / len * 0.6f + ey / len * 0.8f; // above zero toward the upper left
                        var rim = 1f - Smooth(0f, 0.09f, -d);
                        c = fill;
                        c = lit > 0f ? Color.Lerp(c, light, rim * Mathf.Min(1f, lit * 1.3f)) : Color.Lerp(c, dark, rim * Mathf.Min(1f, -lit * 1.2f));
                        var gx = (u + 0.30f) / 0.13f;
                        var gy = (v - 0.86f) / 0.085f;
                        var g = gx * gx + gy * gy;
                        if (g < 1f) c = Color.Lerp(c, white, Smooth(1f, 0.55f, g));
                        c = Color.Lerp(maroon, c, Smooth(0f, 2f * aa, -d));
                    }
                    c.a = Smooth(border + aa, border - aa, d);
                    px[y * n + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            Object.DontDestroyOnLoad(tex);
            CampusLifePlugin.Log.LogInfo("Loading heart: drawn at " + n + "x" + n + " with mipmaps in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
