using System;
using System.Collections.Generic;

namespace Joojkk.CampusLife
{
    /// <summary>How a girl looks while she says a line in a staged scene.</summary>
    internal enum Feel { Normal, Smile, Laugh, Shy, Sad, Surprised, Pout, Serious, Worried, Love }

    internal sealed class FaceCue
    {
        public string GirlKey = "";
        public Feel Feel;
    }

    /// <summary>
    /// Faces for staged scenes (<see cref="SceneBuilder.Face"/>), set straight on her ChaControl when the line shows, as
    /// the game's ADV Expression command does. Each girl's own face comes back when the scene ends.
    /// </summary>
    internal static class Expressions
    {
        private sealed class Look
        {
            public int Brow, Eyes, Mouth;
            public float Blush;
            public byte Tears;
            public float EyesOpen = 1f;
        }

        // Patterns measured in game (close-ups of every pattern inside a talk scene; on the map the AI resets faces): eyes
        // 0 open, 1 closed happy, 2 narrowed, 3 closed looking down, 4 open soft, 6 half-lidded, 22 round "O O".
        private static readonly Dictionary<Feel, Look> Table = new Dictionary<Feel, Look>
        {
            { Feel.Normal,    new Look { Brow = 0, Eyes = 0, Mouth = 0 } },
            { Feel.Smile,     new Look { Brow = 0, Eyes = 0, Mouth = 1 } },
            { Feel.Laugh,     new Look { Brow = 0, Eyes = 1, Mouth = 2 } },
            { Feel.Shy,       new Look { Brow = 2, Eyes = 6, Mouth = 1, Blush = 0.8f } },
            { Feel.Sad,       new Look { Brow = 2, Eyes = 3, Mouth = 6, Tears = 1 } },
            { Feel.Surprised, new Look { Brow = 1, Eyes = 22, Mouth = 3 } },
            { Feel.Pout,      new Look { Brow = 3, Eyes = 2, Mouth = 4, Blush = 0.3f } },
            { Feel.Serious,   new Look { Brow = 3, Eyes = 2, Mouth = 0 } },
            { Feel.Worried,   new Look { Brow = 2, Eyes = 4, Mouth = 6 } },
            { Feel.Love,      new Look { Brow = 2, Eyes = 6, Mouth = 1, Blush = 1f, EyesOpen = 0.8f } },
        };

        private static readonly Dictionary<string, Look> Saved = new Dictionary<string, Look>();

        public static void Apply(List<FaceCue> cues)
        {
            if (cues == null) return;
            foreach (var c in cues) Set(Social.Get(c.GirlKey), c.Feel);
        }

        public static void Set(SaveData.Heroine h, Feel feel)
        {
            Look look;
            if (h == null || !Table.TryGetValue(feel, out look)) return;
            Set(h, look);
        }

        /// <summary>Raw patterns.</summary>
        public static void SetRaw(SaveData.Heroine h, int brow, int eyes, int mouth, float blush, int tears)
        {
            Set(h, new Look { Brow = brow, Eyes = eyes, Mouth = mouth, Blush = blush, Tears = (byte)Math.Max(0, Math.Min(3, tears)) });
        }

        private static void Set(SaveData.Heroine h, Look look)
        {
            try
            {
                var cha = h.chaCtrl;
                if (cha == null || cha.fileStatus == null) return;
                var key = GirlInfo.Key(h);
                if (!Saved.ContainsKey(key))
                {
                    var st = cha.fileStatus;
                    Saved[key] = new Look { Brow = st.eyebrowPtn, Eyes = st.eyesPtn, Mouth = st.mouthPtn, Blush = st.hohoAkaRate, Tears = st.tearsLv, EyesOpen = st.eyesOpenMax };
                }
                Put(cha, look);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Face: " + e.Message); }
        }

        private static void Put(ChaControl cha, Look look)
        {
            cha.ChangeEyebrowPtn(look.Brow);
            cha.ChangeEyesPtn(look.Eyes);
            cha.ChangeMouthPtn(look.Mouth);
            cha.ChangeEyesOpenMax(look.EyesOpen);
            cha.ChangeHohoAkaRate(look.Blush);
            cha.tearsLv = look.Tears;
        }

        /// <summary>Every girl a scene changed gets her own face back.</summary>
        public static void ResetAll()
        {
            foreach (var kv in Saved)
            {
                try
                {
                    var h = Social.Get(kv.Key);
                    if (h != null && h.chaCtrl != null) Put(h.chaCtrl, kv.Value);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Face reset: " + e.Message); }
            }
            Saved.Clear();
        }
    }
}
