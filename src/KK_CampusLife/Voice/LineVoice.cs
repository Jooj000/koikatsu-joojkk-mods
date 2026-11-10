using System;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Voice for Campus Life lines: each girl reacts with a fitting clip of her own recorded game voice (see <see
    /// cref="GameVoice"/>) while the English text is shown.
    /// </summary>
    internal static class LineVoice
    {
        /// <summary>A Campus Life line is on screen: voice it according to the settings.</summary>
        public static void OnLine(string text, VoiceTag tag)
        {
            if (!Cfg.VoiceEnabled.Value || tag == null || tag.IsPlayer || tag.IsNarrator) return;
            if (Cfg.GirlVoice.Value == GirlVoiceMode.Off) return;
            try
            {
                var h = GirlInfo.Find(tag.GirlKey);
                var clip = GameVoice.Pick(h, text);
                if (clip != null) GameVoice.Play(h, clip);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }
    }
}
