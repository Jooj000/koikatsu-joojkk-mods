using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The pictures and text that come with the mod live inside the DLL, so the plugin is one file that works from any
    /// folder under BepInEx\plugins. A file with the same name in a folder next to the DLL (for example
    /// "backgrounds\fest.jpg") wins over the embedded one, so older installs and replaced pictures keep working.
    /// </summary>
    internal static class Bundled
    {
        private const string Prefix = "CampusLife.";
        private static HashSet<string> _names;

        private static Assembly Asm { get { return typeof(Bundled).Assembly; } }

        private static HashSet<string> Names
        {
            get
            {
                if (_names == null)
                {
                    _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    try { foreach (var n in Asm.GetManifestResourceNames()) _names.Add(n); }
                    catch (Exception e) { CampusLifePlugin.Log.LogDebug("Embedded files: " + e.Message); }
                }
                return _names;
            }
        }

        /// <summary>The folder next to the DLL whose files replace the embedded ones (null if unknown).</summary>
        public static string DiskFolder(string folder)
        {
            try
            {
                var dir = Path.GetDirectoryName(Asm.Location);
                return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, folder);
            }
            catch { return null; }
        }

        private static string DiskPath(string folder, string file)
        {
            var dir = DiskFolder(folder);
            return dir == null ? null : Path.Combine(dir, file);
        }

        private static string ResourceName(string folder, string file) { return Prefix + folder + "." + file; }

        /// <summary>Is this file shipped with the mod (on disk next to the DLL or embedded)?</summary>
        public static bool Has(string folder, string file)
        {
            var disk = DiskPath(folder, file);
            return disk != null && File.Exists(disk) || Names.Contains(ResourceName(folder, file));
        }

        /// <summary>The file's bytes, or null when the mod has no such file.</summary>
        public static byte[] Read(string folder, string file)
        {
            try
            {
                var disk = DiskPath(folder, file);
                if (disk != null && File.Exists(disk)) return File.ReadAllBytes(disk);
                var name = ResourceName(folder, file);
                if (!Names.Contains(name)) return null;
                using (var st = Asm.GetManifestResourceStream(name))
                {
                    if (st == null) return null;
                    var buf = new byte[st.Length];
                    var read = 0;
                    while (read < buf.Length) { var n = st.Read(buf, read, buf.Length - read); if (n <= 0) break; read += n; }
                    return buf;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Shipped file " + folder + "/" + file + ": " + e.Message); return null; }
        }

        /// <summary>The embedded text file, or null.</summary>
        public static string ReadText(string folder, string file)
        {
            var b = Read(folder, file);
            if (b == null) return null;
            var s = System.Text.Encoding.UTF8.GetString(b);
            return s.Length > 0 && s[0] == (char)0xFEFF ? s.Substring(1) : s;
        }
    }

    /// <summary>
    /// The game's staff titles as a college ("Math Professor" in place of the school's math teacher). XUnity.AutoTranslator
    /// reads its translation files from its own folder, so the mod writes its small file there when it is missing or
    /// different, and asks the translator to reload, the same as its Alt+R. Without the translator nothing happens.
    /// </summary>
    internal static class StaffTitles
    {
        private const string FileName = "zzz_JoojkkCampus.txt";

        public static void Install()
        {
            try
            {
                var text = Bundled.ReadText("translation", FileName);
                if (string.IsNullOrEmpty(text)) return;
                var settings = Type.GetType("XUnity.AutoTranslator.Plugin.Core.Configuration.Settings, XUnity.AutoTranslator.Plugin.Core");
                var plugin = Type.GetType("XUnity.AutoTranslator.Plugin.Core.AutoTranslationPlugin, XUnity.AutoTranslator.Plugin.Core");
                if (settings == null || plugin == null) return;
                var dirField = settings.GetField("TranslationsPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var dir = dirField == null ? null : dirField.GetValue(null) as string;
                if (string.IsNullOrEmpty(dir)) return;
                var path = Path.Combine(dir, FileName);
                if (File.Exists(path) && File.ReadAllText(path, System.Text.Encoding.UTF8).Replace("\r\n", "\n").Trim() == text.Replace("\r\n", "\n").Trim()) return;
                Directory.CreateDirectory(dir);
                File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
                CampusLifePlugin.Log.LogInfo("Staff titles written to " + path);
                var currentField = plugin.GetField("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var current = currentField == null ? null : currentField.GetValue(null);
                var reload = plugin.GetMethod("ReloadTranslations", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (current != null && reload != null) reload.Invoke(current, null);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Staff titles: " + e.Message); }
        }
    }
}
