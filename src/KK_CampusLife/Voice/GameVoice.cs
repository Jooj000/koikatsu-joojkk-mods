using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>What a line expresses; used to match an English line with a recorded Japanese clip.</summary>
    internal enum VoiceCue { Neutral, Thanks, Happy, Laugh, Surprise, Embarrassed, Sad, Angry, Agree, Refuse, Greeting, Flirty }

    internal sealed class VoiceClip
    {
        public string Bundle;
        public string File;
        public string Text;
        public VoiceCue Cue;
    }

    /// <summary>
    /// Reuses the girls' own recorded voices. Every personality has a conversation sheet (ExcelData) whose rows
    /// hold a voice clip, its Japanese text and how it changes favor/anger. We classify those clips by emotion and
    /// play a fitting one, with lip sync, while the English Campus Life line is shown.
    /// </summary>
    internal static class GameVoice
    {
        private static readonly Dictionary<string, List<VoiceClip>> Catalogs = new Dictionary<string, List<VoiceClip>>();
        private static readonly Dictionary<string, Queue<string>> Recent = new Dictionary<string, Queue<string>>();

        // ---------------- classification ----------------

        private static readonly KeyValuePair<VoiceCue, string[]>[] JpKeys =
        {
            Kv(VoiceCue.Thanks, "ありがと", "ありがとう", "感謝", "助かった", "助かる"),
            Kv(VoiceCue.Laugh, "ふふ", "えへ", "あはは", "あはっ", "くすっ", "うふ", "へへ", "ははっ", "にひ", "くふ"),
            Kv(VoiceCue.Surprise, "えっ", "ええっ", "えぇっ", "わっ", "うそ", "嘘", "まさか", "びっくり", "ほんとに", "本当に？"),
            Kv(VoiceCue.Embarrassed, "恥ずかし", "照れ", "ばか", "バカ", "変なこと", "そんなこと言わ", "あぅ", "はぅ"),
            Kv(VoiceCue.Angry, "最低", "なによ", "何よ", "うるさい", "ふんっ", "ひどい", "怒", "サイテー", "いい加減", "しつこい"),
            Kv(VoiceCue.Sad, "ごめん", "すみません", "寂し", "悲し", "残念", "はぁ…", "つらい", "辛い"),
            Kv(VoiceCue.Refuse, "いや", "だめ", "ダメ", "無理", "嫌", "遠慮", "お断り", "やめ"),
            Kv(VoiceCue.Agree, "うん", "いいよ", "いいね", "もちろん", "はい", "了解", "わかった", "分かった", "オッケー", "そうだね"),
            Kv(VoiceCue.Greeting, "おはよう", "こんにちは", "やっほ", "お疲れ", "おつかれ", "また明日", "じゃあね", "バイバイ"),
            Kv(VoiceCue.Flirty, "エッチ", "えっち", "キス", "ドキドキ", "二人きり", "触", "したい"),
            Kv(VoiceCue.Happy, "嬉し", "うれし", "楽し", "やった", "よかった", "好き", "素敵", "すごい", "すてき", "最高"),
        };

        private static KeyValuePair<VoiceCue, string[]> Kv(VoiceCue c, params string[] k) { return new KeyValuePair<VoiceCue, string[]>(c, k); }

        private static VoiceCue CueOfJapanese(string jp, int favor, int lewd, int anger)
        {
            foreach (var kv in JpKeys)
                if (kv.Value.Any(jp.Contains)) return kv.Key;
            if (anger > 0) return VoiceCue.Angry;
            if (lewd > 0) return VoiceCue.Flirty;
            if (favor > 0) return VoiceCue.Happy;
            if (favor < 0) return VoiceCue.Sad;
            return VoiceCue.Neutral;
        }

        private static bool Has(string s, params string[] keys)
        {
            return keys.Any(k => s.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Emotion of an English Campus Life line.</summary>
        public static VoiceCue CueOfEnglish(string text)
        {
            var t = text ?? "";
            if (Has(t, "thank", "lifesaver", "owe you", "appreciate", "grateful")) return VoiceCue.Thanks;
            if (Has(t, "pervert", "die.", "gross", "how dare", "leave me alone", "know your place", "hmph", "ugh", "not funny", "nothing.", "push your luck")) return VoiceCue.Angry;
            if (Has(t, "haha", "hehe", "pfft", "ahaha", "*laugh")) return VoiceCue.Laugh;
            if (Has(t, "sorry", "apolog", "lost", "failed", "bombed", "doomed", "rather not", "lonely", "worst", "sad")) return VoiceCue.Sad;
            if (Has(t, "absolutely not", "not happening", "nope", "can't accept", "no way i", "wrong girl", "in your dreams")) return VoiceCue.Refuse;
            if (Has(t, "w-what", "th-th", "y-you", "embarrass", "don't tell", "don't laugh", "idiot", "baka", "blush", "d-don't")) return VoiceCue.Embarrassed;
            if (Has(t, "naughty", "kiss", "private", "bold", "show you", "pillow", "in private", "wear it", "~")) return VoiceCue.Flirty;
            if (Has(t, "eh?!", "huh?!", "what?!", "no way!", "seriously?!", "really?", "wait, really", "you saw", "?!")) return VoiceCue.Surprise;
            if (Has(t, "hi!", "hello", "good morning", "hey there", "you came", "you made it")) return VoiceCue.Greeting;
            if (Has(t, "okay", "sure", "of course", "deal", "fine.", "i accept", "let's", "leave it to")) return VoiceCue.Agree;
            if (Has(t, "happy", "yay", "the best", "amazing", "awesome", "love", "glad", "won", "passed", "!")) return VoiceCue.Happy;
            return VoiceCue.Neutral;
        }

        private static VoiceCue[] Fallbacks(VoiceCue c)
        {
            switch (c)
            {
                case VoiceCue.Thanks: return new[] { VoiceCue.Happy, VoiceCue.Agree };
                case VoiceCue.Laugh: return new[] { VoiceCue.Happy, VoiceCue.Flirty };
                case VoiceCue.Surprise: return new[] { VoiceCue.Embarrassed, VoiceCue.Neutral };
                case VoiceCue.Embarrassed: return new[] { VoiceCue.Surprise, VoiceCue.Flirty };
                case VoiceCue.Sad: return new[] { VoiceCue.Neutral };
                case VoiceCue.Angry: return new[] { VoiceCue.Refuse, VoiceCue.Embarrassed };
                case VoiceCue.Agree: return new[] { VoiceCue.Happy, VoiceCue.Neutral };
                case VoiceCue.Refuse: return new[] { VoiceCue.Angry, VoiceCue.Embarrassed };
                case VoiceCue.Greeting: return new[] { VoiceCue.Happy, VoiceCue.Neutral };
                case VoiceCue.Flirty: return new[] { VoiceCue.Embarrassed, VoiceCue.Laugh, VoiceCue.Happy };
                case VoiceCue.Happy: return new[] { VoiceCue.Laugh, VoiceCue.Agree };
                default: return new[] { VoiceCue.Agree, VoiceCue.Happy };
            }
        }

        // ---------------- catalog ----------------

        public static List<VoiceClip> Catalog(SaveData.Heroine h)
        {
            if (h == null) return null;
            var id = h.FixCharaIDOrPersonality;
            string bundle, file;
            try
            {
                if (!Singleton<Communication>.IsInstance() || !Singleton<Communication>.Instance.GetTalkFilePath(id, out bundle, out file))
                    return null;
            }
            catch { return null; }
            var key = bundle + "|" + file;
            List<VoiceClip> list;
            if (Catalogs.TryGetValue(key, out list)) return list;

            list = new List<VoiceClip>();
            try
            {
                var op = AssetBundleManager.LoadAsset(bundle, file, typeof(ExcelData));
                var excel = op == null || op.IsEmpty() ? null : op.GetAsset<ExcelData>();
                if (excel != null && excel.list != null)
                    foreach (var row in excel.list)
                        if (row != null && row.list != null) ReadRow(row.list, list);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Voice catalog for personality " + id + " failed: " + e.Message);
            }
            Catalogs[key] = list;
            CampusLifePlugin.Log.LogInfo("Voice catalog c" + id + ": " + list.Count + " clips (" +
                    string.Join(", ", list.GroupBy(c => c.Cue).Select(g => g.Key + " " + g.Count()).ToArray()) + ")");
            return list;
        }

        /// <summary>Talk sheet rows hold: favor, lewdness, anger, voice bundle, voice file, text, pose, expression...</summary>
        private static void ReadRow(List<string> cells, List<VoiceClip> into)
        {
            for (var c = 3; c + 2 < cells.Count; c++)
            {
                var bundle = cells[c];
                if (string.IsNullOrEmpty(bundle) || bundle.IndexOf("sound/data/pcm/", StringComparison.Ordinal) < 0) continue;
                var file = cells[c + 1];
                var text = cells[c + 2];
                if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(text)) continue;
                if (file.Contains(",") || file.Contains("callName")) continue; // multi-part or says the player's name
                if (text.Contains("[P") || text.Contains("[H")) continue;       // mentions names
                int favor, lewd, anger;
                int.TryParse(cells[c - 3], out favor);
                int.TryParse(cells[c - 2], out lewd);
                int.TryParse(cells[c - 1], out anger);
                into.Add(new VoiceClip { Bundle = bundle, File = file, Text = text, Cue = CueOfJapanese(text, favor, lewd, anger) });
            }
        }

        // ---------------- picking & playing ----------------

        public static VoiceClip Pick(SaveData.Heroine h, string englishText)
        {
            var cat = Catalog(h);
            if (cat == null || cat.Count == 0) return null;
            var cue = CueOfEnglish(englishText);
            var key = GirlInfo.Key(h);
            Queue<string> recent;
            if (!Recent.TryGetValue(key, out recent)) { recent = new Queue<string>(); Recent[key] = recent; }

            var order = new List<VoiceCue> { cue };
            order.AddRange(Fallbacks(cue));
            foreach (var c in order)
            {
                var pool = cat.Where(x => x.Cue == c && !recent.Contains(x.File)).ToList();
                if (pool.Count == 0) continue;
                if (Cfg.ShortGameClips.Value)
                {
                    // Short reactions fit any English sentence best.
                    var shortOnes = pool.Where(x => x.Text.Length <= 14).ToList();
                    if (shortOnes.Count >= 2) pool = shortOnes;
                    else pool = pool.OrderBy(x => x.Text.Length).Take(Math.Max(3, pool.Count / 3)).ToList();
                }
                var pick = pool[Random.Range(0, pool.Count)];
                recent.Enqueue(pick.File);
                while (recent.Count > 6) recent.Dequeue();
                return pick;
            }
            return null;
        }

        /// <summary>Plays the clip from her head (3D, lip sync) if she is on screen, otherwise as a 2D voice.</summary>
        public static bool Play(SaveData.Heroine h, VoiceClip clip)
        {
            if (h == null || clip == null || !Singleton<Manager.Voice>.IsInstance()) return false;
            try
            {
                var cha = h.chaCtrl;
                Transform head = null;
                if (cha != null && cha.gameObject.activeInHierarchy && cha.objHeadBone != null) head = cha.objHeadBone.transform;
                var setting = new Illusion.Game.Utils.Voice.Setting
                {
                    no = h.voiceNo,
                    assetBundleName = clip.Bundle,
                    assetName = clip.File,
                    pitch = h.voicePitch,
                    voiceTrans = head,
                    isAsync = false,
                    is2D = head == null,
                };
                var t = head != null ? Illusion.Game.Utils.Voice.OnecePlayChara(setting) : Illusion.Game.Utils.Voice.OnecePlay(setting);
                if (t != null && cha != null && head != null) cha.SetVoiceTransform(t);
                return t != null;
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Game voice failed: " + e.Message);
                return false;
            }
        }
    }
}
