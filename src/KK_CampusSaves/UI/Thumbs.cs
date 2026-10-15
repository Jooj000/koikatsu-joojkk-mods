using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusSaves
{
    /// <summary>The picture of a version: taken when saving, because neither the game's saves nor the old versions have one.</summary>
    internal static class Thumbs
    {
        public const int W = 480, H = 270;
        private const int Corner = 16;

        /// <summary>A small picture of the 3D view, without the interface, or null when the view is dark, as in the night menu.</summary>
        public static byte[] Take()
        {
            var active = RenderTexture.active;
            RenderTexture rt = null;
            Texture2D t = null;
            try
            {
                var cam = Camera.main ?? Camera.allCameras.OrderBy(c => c.depth).FirstOrDefault();
                if (cam == null) return null;
                rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
                var old = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = old;
                RenderTexture.active = rt;
                t = new Texture2D(W, H, TextureFormat.RGB24, false);
                t.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                t.Apply();
                return Dark(t) ? null : t.EncodeToJPG(85);
            }
            catch (Exception e)
            {
                CampusSavesPlugin.Log.LogWarning("Picture: " + e.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = active;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (t != null) UnityEngine.Object.Destroy(t);
            }
        }

        private static bool Dark(Texture2D t)
        {
            var px = t.GetPixels32();
            long sum = 0;
            for (var i = 0; i < px.Length; i += 37) sum += px[i].r + px[i].g + px[i].b;
            return sum / (3L * (px.Length / 37 + 1)) < 24;
        }

        /// <summary>A save with no view of its own shows the picture of the moment before it.</summary>
        public static void Reuse(Vault vault, SaveVersion v)
        {
            var to = vault.PicturePath(v.Id);
            if (File.Exists(to)) return;
            var from = vault.Snapshot().Where(x => x.Id < v.Id).OrderByDescending(x => x.Id).Select(x => vault.PicturePath(x.Id)).FirstOrDefault(File.Exists);
            if (from != null) File.Copy(from, to);
        }

        /// <summary>Reads a picture with its corners cut round, or null when the version has none.</summary>
        public static Texture2D Load(string path)
        {
            if (!File.Exists(path)) return null;
            var src = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!src.LoadImage(File.ReadAllBytes(path))) return null;
                var dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                var px = src.GetPixels32();
                RoundCorners(px, src.width, src.height, Corner);
                dst.SetPixels32(px);
                dst.Apply(false, true);
                return dst;
            }
            catch (Exception e)
            {
                CampusSavesPlugin.Log.LogWarning("Picture: " + e.Message);
                return null;
            }
            finally { UnityEngine.Object.Destroy(src); }
        }

        private static void RoundCorners(Color32[] px, int w, int h, int r)
        {
            for (var y = 0; y < r; y++)
                for (var x = 0; x < r; x++)
                {
                    var dx = r - x - 0.5f;
                    var dy = r - y - 0.5f;
                    var a = (byte)(Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f) * 255f);
                    px[y * w + x].a = a;
                    px[y * w + w - 1 - x].a = a;
                    px[(h - 1 - y) * w + x].a = a;
                    px[(h - 1 - y) * w + w - 1 - x].a = a;
                }
        }
    }

    /// <summary>The interface's own textures, drawn here so that the mod carries no image files.</summary>
    internal static class Art
    {
        public static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.ARGB32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>A rounded card; the style that uses it stretches only the middle.</summary>
        public static Texture2D Card(Color c, int r)
        {
            var size = r * 2 + 2;
            return Paint(size, c, (x, y) =>
            {
                var dx = Mathf.Max(r - x, x - (size - r));
                var dy = Mathf.Max(r - y, y - (size - r));
                return dx <= 0f || dy <= 0f || dx * dx + dy * dy <= r * r;
            });
        }

        public static Texture2D Lens()
        {
            return Paint(64, Color.white, (x, y) =>
            {
                var d = Mathf.Sqrt((x - 27f) * (x - 27f) + (y - 27f) * (y - 27f));
                if (d >= 13f && d <= 20f) return true;
                var t = Mathf.Clamp((x + y - 80f) / 2f, 0f, 14f);
                var px = x - (40f + t);
                var py = y - (40f + t);
                return px * px + py * py <= 16f;
            });
        }

        public static Texture2D Trash()
        {
            return Paint(64, Color.white, (x, y) =>
            {
                if (x >= 24f && x <= 40f && y >= 7f && y <= 15f) return x <= 28f || x >= 36f || y <= 11f;
                if (x >= 11f && x <= 53f && y >= 15f && y <= 21f) return true;
                if (x < 16f || x > 48f || y < 25f || y > 58f) return false;
                var slot = y >= 31f && y <= 52f && ((x >= 23f && x <= 27f) || (x >= 37f && x <= 41f));
                return !slot;
            });
        }

        public static Texture2D Close()
        {
            return Paint(64, Color.white, (x, y) => x >= 12f && x <= 52f && y >= 12f && y <= 52f && (Mathf.Abs(x - y) <= 6.5f || Mathf.Abs(x + y - 64f) <= 6.5f));
        }

        /// <summary>The arrow down into a tray.</summary>
        public static Texture2D Download()
        {
            return Paint(64, Color.white, (x, y) =>
            {
                var d = Mathf.Abs(x - 32f);
                if (d <= 4.5f && y >= 7f && y <= 30f) return true;
                if (y >= 26f && y <= 46f && d <= 46f - y) return true;
                if (x >= 8f && x <= 56f && y >= 51f && y <= 58f) return true;
                return y >= 40f && y <= 58f && (x >= 8f && x <= 15f || x >= 49f && x <= 56f);
            });
        }

        /// <summary>A hard drive with its lid off: the frame, the platter and the arm.</summary>
        public static Texture2D Drive()
        {
            return Paint(64, Color.white, (x, y) =>
            {
                var frame = x >= 9f && x <= 55f && y >= 5f && y <= 59f && !(x >= 14f && x <= 50f && y >= 10f && y <= 54f);
                var d = Mathf.Sqrt((x - 32f) * (x - 32f) + (y - 27f) * (y - 27f));
                var platter = d >= 7f && d <= 14f || d <= 3f;
                var along = (x - 46f) * -0.56f + (y - 48f) * -0.83f;
                var side = (x - 46f) * 0.83f - (y - 48f) * 0.56f;
                var arm = along >= -2f && along <= 17f && Mathf.Abs(side) <= 2f;
                return frame || platter && !(along >= -2f && along <= 19f && Mathf.Abs(side) <= 4f) || arm;
            });
        }

        /// <summary>An open box with an arrow coming out of it.</summary>
        public static Texture2D Crate()
        {
            return Paint(64, Color.white, (x, y) =>
            {
                var d = Mathf.Abs(x - 32f);
                if (d <= 3.5f && y >= 14f && y <= 36f) return true;
                if (y >= 3f && y <= 18f && d <= (y - 3f) * 0.9f) return true;
                if (x >= 13f && x <= 51f && y >= 40f && y <= 60f) return true;
                var lift = 40f - y;
                if (lift <= 0f || lift >= 10f) return false;
                return x >= 13f - lift && x <= 25f - lift || x >= 39f + lift && x <= 51f + lift;
            });
        }

        /// <summary>A cone with three scoops, in colours: the game's own save list, plain vanilla and friends.</summary>
        public static Texture2D IceCream()
        {
            var pink = new Color(1f, 0.56f, 0.70f);
            var mint = new Color(0.56f, 0.92f, 0.76f);
            var cream = new Color(1f, 0.96f, 0.80f);
            var cone = new Color(0.86f, 0.62f, 0.32f);
            return Paint(64, (x, y) =>
            {
                Func<float, float, float, bool> ball = (cx, cy, r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
                if (ball(32f, 15f, 12f)) return cream;
                if (ball(21f, 31f, 12f)) return pink;
                if (ball(43f, 31f, 12f)) return mint;
                if (y >= 37f && y <= 62f && Mathf.Abs(x - 32f) <= (62f - y) * 0.72f) return cone;
                return Color.clear;
            });
        }

        /// <summary>Paints a shape four samples per pixel, with y going down as on the screen.</summary>
        private static Texture2D Paint(int size, Color c, Func<float, float, bool> inside)
        {
            return Paint(size, (x, y) => inside(x, y) ? c : Color.clear);
        }

        private static Texture2D Paint(int size, Func<float, float, Color> at)
        {
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var sum = Color.clear;
                    for (var sy = 0; sy < 4; sy++)
                        for (var sx = 0; sx < 4; sx++)
                        {
                            var s = at(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f);
                            sum += new Color(s.r * s.a, s.g * s.a, s.b * s.a, s.a);
                        }
                    px[(size - 1 - y) * size + x] = sum.a > 0f ? new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a / 16f) : Color.clear;
                }
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }
    }
}
