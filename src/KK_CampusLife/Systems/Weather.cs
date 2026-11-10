using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Rain.
    /// <list type="bullet">
    /// <item>The weather is a value of the day, rolled by the month (June is the rainy season, September has typhoons) and
    /// kept in the save: clear, cloudy, rain or a storm, and the period the rain starts (all day, from lunch, after
    /// school).</item>
    /// <item>What you see: one particle system of thin streaks that follows the camera, only on outdoor maps, and the
    /// map's lights a little dimmer (more outside). No textures loaded, no collisions, no sound: a couple of thousand
    /// quads at most.</item>
    /// <item>What it changes: the director keeps girls off outdoor spots while it rains (a few love the rain and stay out),
    /// the dialogue context has <c>weather</c>, <c>outside</c> and <c>rainlover</c>, and the rain's start brings its
    /// events (stuck at the entrance without an umbrella, a girl running in soaked).</item>
    /// </list>
    /// </summary>
    internal static class Weather
    {
        public const int Clear = 0, Cloudy = 1, Rain = 2, Storm = 3;
        private const string Key = "weather";
        private static CampusState S { get { return Campus.S; } }

        /// <summary>Set by the controller between the start and the end of H (H maps get no rain).</summary>
        public static bool InH;

        // ---------------- the day's weather ----------------

        private static int Packed
        {
            get
            {
                if (S == null) return 0;
                int v;
                if (!S.Buffs.TryGetValue(Key, out v) || v / 100 != S.Day) { Roll(); S.Buffs.TryGetValue(Key, out v); }
                return v;
            }
        }

        public static int Kind { get { return Packed / 10 % 10; } }

        /// <summary>The period index the rain starts at: 0 all day, 1 from lunch, 2 after school.</summary>
        public static int From { get { return Packed % 10; } }

        private static int Index(PeriodMask m) { return m == PeriodMask.Lunch ? 1 : m == PeriodMask.Club || m == PeriodMask.After ? 2 : 0; }

        public static bool Raining { get { return S != null && Kind >= Rain && Index(G.CurrentMask) >= From; } }

        /// <summary>Rain later today (the sky is heavy, umbrellas come out).</summary>
        public static bool Later { get { return S != null && Kind >= Rain && Index(G.CurrentMask) < From; } }

        /// <summary>clear, cloudy, rain or storm, as it is now.</summary>
        public static string Now { get { return Raining ? (Kind == Storm ? "storm" : "rain") : Kind >= Cloudy ? "cloudy" : "clear"; } }

        private static void Roll()
        {
            if (S == null) return;
            int month;
            try { month = Conversation.MonthNow; } catch { month = 4; }
            // Rain chance by month (Japan: tsuyu in June, typhoons in September), then cloudy.
            float rain;
            switch (month)
            {
                case 6: rain = 0.55f; break;
                case 7: case 9: rain = 0.35f; break;
                case 4: case 5: case 10: rain = 0.25f; break;
                case 11: case 3: rain = 0.2f; break;
                default: rain = 0.12f; break;
            }
            var r = Random.value;
            var kind = r < rain ? (Random.value < (month == 9 || month == 6 ? 0.3f : 0.12f) ? Storm : Rain) : r < rain + 0.25f ? Cloudy : Clear;
            var from = kind >= Rain ? (Random.value < 0.45f ? 0 : Random.value < 0.5f ? 1 : 2) : 0;
            Set(kind, from, "month " + month + ", rain " + (int)(rain * 100) + "%");
        }

        public static void Set(int kind, int from, string why)
        {
            if (S == null) return;
            S.Buffs[Key] = S.Day * 100 + kind * 10 + from;
            _applied = "";
            CampusLifePlugin.Log.LogInfo("Weather: day " + S.Day + " " + Name(kind) + (kind >= Rain ? (from == 0 ? " all day" : from == 1 ? " from lunch" : " after class") : "") + " (" + why + ")");
        }

        public static string Name(int kind) { return kind == Storm ? "storm" : kind == Rain ? "rain" : kind == Cloudy ? "cloudy" : "clear"; }

        public static string Describe()
        {
            return "weather " + Name(Kind) + (Kind >= Rain ? " from " + From : "") + ", now " + Now + (Later ? " (later)" : "") + ", outside " + Outdoor(G.PlayerMapNo) +
                   ", fx " + (_fx != null && _fx.activeSelf) + ", veil " + (_veil != null && _veil.activeInHierarchy ? "on layer " + _veil.layer + (_veil.transform.parent != null ? " under " + _veil.transform.parent.name : " loose") : "off") + ", lights dimmed " + Dimmed.Count;
        }

        /// <summary>New day: the forecast in a toast.</summary>
        public static void OnDay()
        {
            var k = Kind;
            if (k >= Rain) Toasts.Show(From == 0 ? "It's raining today." : "The sky is heavy: rain " + (From == 1 ? "from lunch." : "after class."));
        }

        /// <summary>New period: when the rain starts now, it says so and brings its event.</summary>
        public static void OnPeriod(Cycle.Type period)
        {
            if (S == null || Kind < Rain || From == 0 || Index(G.ToMask(period)) != From) return;
            Toasts.Show(Kind == Storm ? "A storm breaks over the university." : "It starts to rain.");
            if (Cfg.HappeningsEnabled.Value && string.IsNullOrEmpty(S.PendingHappening) && !Calendar.Quiet)
                S.PendingHappening = G.ToMask(period) == PeriodMask.After ? "rain" : "drenched";
        }

        // ---------------- places and girls ----------------

        // The pool is a hall with a roof.
        private static readonly string[] OutdoorWords = { "屋上", "中庭", "グラウンド", "校庭", "運動場", "門", "裏庭", "校舎裏", "公園", "野原", "駅", "Roof", "Courtyard", "Ground", "Field", "Park", "Gate" };
        private static readonly Dictionary<int, bool> OutdoorCache = new Dictionary<int, bool>();

        public static bool Outdoor(int mapNo)
        {
            if (mapNo < 0) return false;
            bool o;
            if (OutdoorCache.TryGetValue(mapNo, out o)) return o;
            var name = G.MapName(mapNo) ?? "";
            o = name != "" && OutdoorWords.Any(name.Contains);
            if (name != "") OutdoorCache[mapNo] = o;
            return o;
        }

        /// <summary>A few girls love the rain (about one in seven, always the same ones).</summary>
        public static bool RainLover(SaveData.Heroine h)
        {
            return h != null && Math.Abs((GirlInfo.Key(h) + "rain").GetHashCode()) % 7 == 0;
        }

        public static int Sheltered;

        /// <summary>The director keeps her off an outdoor spot while it rains (a rain lover goes anyway, half the
        /// time).</summary> <summary>A rain lover goes out in it unless she is sleepy, upset or stressed (context, not a
        /// coin).</summary>
        private static bool MoodGoes(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var m = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            return m != Mood.Sleepy && m != Mood.Upset && m != Mood.Stressed;
        }

        public static bool Shelters(int mapNo, SaveData.Heroine h)
        {
            if (!Raining || !Outdoor(mapNo)) return false;
            if (RainLover(h) && Kind != Storm && MoodGoes(h)) return false;
            Sheltered++;
            return true;
        }

        // ---------------- what you see ----------------

        private static GameObject _fx;
        private static ParticleSystem _ps;
        private static string _applied = "";
        private static float _next;
        private static readonly Dictionary<Light, float> Dimmed = new Dictionary<Light, float>();

        /// <summary>Every frame from the plugin: keeps the rain over the camera; the rest twice a second.</summary>
        public static void Update()
        {
            try
            {
                var on = Campus.On && S != null && !InH && G.Act != null && Raining && Outdoor(G.PlayerMapNo);
                var cam = on ? Camera.main : null;
                if (cam != null && _fx != null && _fx.activeSelf)
                {
                    _fx.transform.position = cam.transform.position + Vector3.up * 7f + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 3f;
                    Cover(cam);
                }
                if (Time.realtimeSinceStartup < _next) return;
                _next = Time.realtimeSinceStartup + 0.5f;
                var state = (cam != null ? "rain" : S != null && !InH && G.Act != null && Raining ? "indoor" : "off") + ":" + G.PlayerMapNo + ":" + Kind;
                if (state == _applied) return;
                _applied = state;
                if (cam != null)
                {
                    if (_fx == null) Build();
                    // On a layer this camera draws: the game's cameras do not all draw Default (nothing showed).
                    var layer = DrawnLayer(cam);
                    _fx.layer = layer;
                    Veil().layer = layer;
                    CampusLifePlugin.Log.LogInfo("Weather fx: camera " + cam.name + " mask 0x" + cam.cullingMask.ToString("X") + ", layer " + layer + " " + LayerMask.LayerToName(layer) + ", near " + cam.nearClipPlane.ToString("0.###") + ", fov " + cam.fieldOfView.ToString("0") + ", scale " + cam.transform.lossyScale.ToString("0.###") + ", veil " + (_veil != null && _veil.activeInHierarchy) +
                        "; cameras: " + string.Join(", ", Camera.allCameras.Select(c => c.name + " d" + c.depth + " 0x" + c.cullingMask.ToString("X") + (c.targetTexture != null ? " rt" : "")).ToArray()));
                    var em = _ps.emission;
                    em.rateOverTime = Kind == Storm ? 2400f : 1300f;
                    _fx.SetActive(true);
                    _ps.Play();
                    _veilMat.SetColor("_TintColor", Kind == Storm ? new Color(0.5f, 0.5f, 0.5f, 0.68f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    _veil.SetActive(true);
                }
                else if (_fx != null) { _ps.Stop(); _fx.SetActive(false); if (_veil != null) _veil.SetActive(false); }
                Dim(cam != null ? (Kind == Storm ? 0.62f : 0.72f) : state.StartsWith("indoor", StringComparison.Ordinal) ? 0.88f : 1f);
                CampusLifePlugin.Log.LogInfo("Weather fx: " + state + (_ps != null ? ", " + _ps.main.maxParticles + " drops max" : ""));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Weather fx: " + e.Message); }
        }

        /// <summary>The map's lights at a fraction of what they were (1 = back to normal).</summary>
        private static void Dim(float k)
        {
            foreach (var kv in Dimmed.ToList())
                if (kv.Key != null) kv.Key.intensity = kv.Value;
            Dimmed.Clear();
            if (k >= 0.999f) return;
            foreach (var l in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (l == null || !l.enabled || l.type != LightType.Directional) continue;
                Dimmed[l] = l.intensity;
                l.intensity *= k;
            }
        }

        private static void Build()
        {
            _fx = new GameObject("JoojkkRain");
            UnityEngine.Object.DontDestroyOnLoad(_fx);
            _fx.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // the box emits along its forward: straight down
            _ps = _fx.AddComponent<ParticleSystem>();
            var main = _ps.main;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new Color(0.86f, 0.9f, 0.96f, 0.6f);
            main.maxParticles = 2600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.box = new Vector3(26f, 26f, 0.2f);
            var r = _fx.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2f;
            r.velocityScale = 0.04f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.material = Material(Streak());
        }

        private static GameObject _veil;
        private static Mesh _veilMesh;
        private static Material _veilMat;

        /// <summary>
        /// The grey sky: one quad over the whole view (a single draw call, no fog or image effect), darker at the top
        /// where the blue sky is and lighter at the ground. It hangs on the camera itself (placed from our Update, it
        /// lagged a frame behind the camera and its edge showed when the view turned), so it goes with the camera
        /// when a map unloads and is made again.
        /// </summary>
        private static GameObject Veil()
        {
            if (_veil != null) return _veil;
            if (_veilMesh == null)
                _veilMesh = new Mesh
                {
                    vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) },
                    uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                    colors = new[] { Color.white, Color.white, Color.white, Color.white },
                    triangles = new[] { 0, 2, 1, 2, 3, 1 },
                    bounds = new Bounds(Vector3.zero, Vector3.one * 1000f)
                };
            if (_veilMat == null)
            {
                _veilMat = Material(Sky());
                _veilMat.renderQueue = 4000; // after the hair and the drops, so everything gets the same grey
            }
            _veil = new GameObject("JoojkkRainVeil");
            _veil.AddComponent<MeshFilter>().sharedMesh = _veilMesh;
            var vr = _veil.AddComponent<MeshRenderer>();
            vr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vr.receiveShadows = false;
            vr.sharedMaterial = _veilMat;
            return _veil;
        }

        /// <summary>Default when the camera draws it, else the map's layer, else the first one it draws.</summary>
        private static int DrawnLayer(Camera cam)
        {
            var mask = cam.cullingMask;
            if ((mask & 1) != 0) return 0;
            foreach (var n in new[] { "Map", "MapNoShadow", "Chara" })
            {
                var l = LayerMask.NameToLayer(n);
                if (l >= 0 && (mask & (1 << l)) != 0) return l;
            }
            for (var l = 0; l < 32; l++)
                if ((mask & (1 << l)) != 0) return l;
            return 0;
        }

        /// <summary>
        /// The veil just past the camera's near plane, as big as the view (a child of the camera). The map load can take
        /// the camera, and the veil with it (after the warp only the drops showed): the new one gets the drops' layer
        /// here, and the sizes allow for a scaled camera.
        /// </summary>
        private static void Cover(Camera cam)
        {
            var go = Veil();
            if (_fx != null && go.layer != _fx.layer) go.layer = _fx.layer;
            var v = go.transform;
            var d = cam.nearClipPlane + 0.02f;
            if (v.parent != cam.transform) { v.SetParent(cam.transform, false); v.localRotation = Quaternion.identity; }
            var h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
            var ls = cam.transform.lossyScale;
            var sx = Mathf.Abs(ls.x) > 1e-4f ? Mathf.Abs(ls.x) : 1f;
            var sy = Mathf.Abs(ls.y) > 1e-4f ? Mathf.Abs(ls.y) : 1f;
            var sz = Mathf.Abs(ls.z) > 1e-4f ? Mathf.Abs(ls.z) : 1f;
            v.localPosition = new Vector3(0f, 0f, d / sz);
            v.localScale = new Vector3(h * cam.aspect / sx, h / sy, 1f);
        }

        private static Material Material(Texture2D tex)
        {
            Shader sh = null;
            foreach (var n in new[] { "Particles/Alpha Blended", "Legacy Shaders/Particles/Alpha Blended", "Particles/Additive", "Sprites/Default", "Unlit/Transparent", "UI/Default" })
                if ((sh = Shader.Find(n)) != null) { CampusLifePlugin.Log.LogInfo("Weather fx: shader " + n); break; }
            var m = new Material(sh) { mainTexture = tex };
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return m;
        }

        /// <summary>A thin streak: bright down the middle, fading to the sides and the ends.</summary>
        private static Texture2D Streak()
        {
            var tex = new Texture2D(8, 64, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 64; y++)
                for (var x = 0; x < 8; x++)
                {
                    var a = (1f - Mathf.Abs(x - 3.5f) / 4f) * Mathf.Sin(Mathf.PI * y / 63f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Overcast grey, 62% over the sky fading to 20% at the bottom of the view.</summary>
        private static Texture2D Sky()
        {
            var tex = new Texture2D(2, 32, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++)
            {
                var t = y / 31f;
                var c = new Color(0.46f, 0.5f, 0.56f, Mathf.Lerp(0.2f, 0.62f, t * t));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
