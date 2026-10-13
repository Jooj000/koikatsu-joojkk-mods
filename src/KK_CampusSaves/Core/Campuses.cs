using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Joojkk.CampusSaves
{
    /// <summary>One campus: its slot in the game's list and its vault.</summary>
    internal sealed class Campus
    {
        public string Id = "";
        public string Name = "";
        public int Emblem;
        public string Slot = "";
        public List<string> Imported = new List<string>();
        public Vault Vault;

        public string Dir { get { return Path.Combine(Campuses.Root, Id); } }
        private string InfoPath { get { return Path.Combine(Dir, "campus.txt"); } }

        public void Write()
        {
            Directory.CreateDirectory(Dir);
            var s = "id=" + Id + "\nname=" + Name.Replace('\n', ' ') + "\nemblem=" + Emblem + "\nslot=" + Slot + "\nimported=" + string.Join("|", Imported.ToArray()) + "\n";
            File.WriteAllText(InfoPath + ".tmp", s, new UTF8Encoding(false));
            if (File.Exists(InfoPath)) File.Delete(InfoPath);
            File.Move(InfoPath + ".tmp", InfoPath);
        }

        public static Campus Read(string dir)
        {
            try
            {
                var c = new Campus { Id = Path.GetFileName(dir) };
                foreach (var l in File.ReadAllLines(Path.Combine(dir, "campus.txt"), Encoding.UTF8))
                {
                    var eq = l.IndexOf('=');
                    if (eq < 0) continue;
                    var x = l.Substring(eq + 1);
                    switch (l.Substring(0, eq))
                    {
                        case "name": c.Name = x; break;
                        case "emblem": c.Emblem = int.Parse(x); break;
                        case "slot": c.Slot = x; break;
                        case "imported": c.Imported = x.Split('|').Where(i => i != "").ToList(); break;
                    }
                }
                c.Vault = new Vault(dir);
                return c;
            }
            catch (Exception e)
            {
                CampusSavesPlugin.Log.LogWarning("Campus folder " + dir + " skipped: " + e.Message);
                return null;
            }
        }

        /// <summary>The slot file was written by this version: a save in the middle of the day comes back from it.</summary>
        public SaveVersion HeadVersion()
        {
            var v = Vault.Versions.LastOrDefault();
            if (v == null || Slot == "") return null;
            var head = Path.Combine(Campuses.GameDir, Slot);
            return File.Exists(head) && File.GetLastWriteTimeUtc(head).Ticks == v.HeadTicks ? v : null;
        }

        /// <summary>
        /// The last 10 automatic versions (or the set number), then one per hour for a day, one per day after that, and 200
        /// at most. Hand saves and imported ones are never pruned. Only the list shrinks: the pieces wait for compaction.
        /// </summary>
        public void Prune(int keep, DateTime now)
        {
            var autos = Vault.Versions.Where(v => v.Kind == "auto").OrderByDescending(v => v.At).ToList();
            var seen = new HashSet<string>();
            for (var i = 0; i < autos.Count; i++)
            {
                var v = autos[i];
                if (i < keep) continue;
                var age = now - v.At;
                var bucket = age.TotalDays < 1 ? v.At.ToString("yyyyMMddHH") : v.At.ToString("yyyyMMdd");
                if (i < 200 && seen.Add(bucket)) continue;
                Vault.Remove(v);
            }
        }
    }

    /// <summary>All the campuses, and the single background thread that does the slow work with them.</summary>
    internal static class Campuses
    {
        public static string Root { get { return Path.Combine(UserData.Path, "save/campus"); } }
        public static string GameDir { get { return SaveData.Path; } }
        public static readonly List<Campus> All = new List<Campus>();
        public static volatile string Status = "";

        private static readonly Queue<Action> Work = new Queue<Action>();
        private static Thread _worker;
        private static volatile bool _running;

        public static bool Busy { get { lock (Work) return _running || Work.Count > 0; } }

        public static void Scan()
        {
            lock (All)
            {
                All.Clear();
                Directory.CreateDirectory(Root);
                foreach (var d in Directory.GetDirectories(Root))
                {
                    if (!File.Exists(Path.Combine(d, "campus.txt"))) continue;
                    var c = Campus.Read(d);
                    if (c != null) All.Add(c);
                }
            }
        }

        public static Campus ById(string id) { lock (All) return All.FirstOrDefault(c => c.Id == id); }

        public static Campus BySlot(string slot) { lock (All) return All.FirstOrDefault(c => c.Slot == slot || c.Imported.Contains(slot)); }

        public static Campus ByName(string name, int emblem) { lock (All) return All.FirstOrDefault(c => c.Name == name && c.Emblem == emblem); }

        public static Campus Create(string name, int emblem, string slot)
        {
            var c = new Campus { Id = Guid.NewGuid().ToString("N").Substring(0, 12), Name = name, Emblem = emblem, Slot = slot };
            Directory.CreateDirectory(c.Dir);
            c.Vault = new Vault(c.Dir);
            c.Write();
            lock (All) All.Add(c);
            return c;
        }

        public static void Run(Action a)
        {
            lock (Work)
            {
                Work.Enqueue(a);
                if (_worker == null)
                {
                    _worker = new Thread(Loop) { IsBackground = true, Name = "CampusSaves" };
                    _worker.Start();
                }
            }
        }

        private static void Loop()
        {
            while (true)
            {
                Action a = null;
                lock (Work)
                {
                    if (Work.Count > 0) { a = Work.Dequeue(); _running = true; }
                    else _running = false;
                }
                if (a == null) { Thread.Sleep(50); continue; }
                try { a(); }
                catch (Exception e)
                {
                    Status = "Failed: " + e.Message;
                    CampusSavesPlugin.Log.LogError(e);
                }
            }
        }

        /// <summary>Waits for the thread to let go of the slot file. False if it did not in time.</summary>
        public static bool WaitIdle(int ms)
        {
            var until = Environment.TickCount + ms;
            while (Busy)
            {
                if (Environment.TickCount > until) return false;
                Thread.Sleep(20);
            }
            return true;
        }

        /// <summary>Copies a slot into a campus's vault as an imported version. The file itself is not touched.</summary>
        public static SaveVersion Import(Campus c, string slot)
        {
            var path = Path.Combine(GameDir, slot);
            var v = c.Vault.Absorb(path, new SaveVersion { Name = slot, Kind = "imported", Trigger = "import", At = File.GetLastWriteTime(path), HeadTicks = File.GetLastWriteTimeUtc(path).Ticks });
            if (!c.Imported.Contains(slot)) c.Imported.Add(slot);
            c.Write();
            return v;
        }

        /// <summary>The next free slot, the way the game names them.</summary>
        public static string FreeSlot()
        {
            for (var i = 1; i <= 100; i++)
            {
                var name = "file" + i.ToString("00") + ".dat";
                if (!File.Exists(Path.Combine(GameDir, name))) return name;
            }
            return null;
        }
    }

    /// <summary>The start of a save file, which holds the campus's name and the day.</summary>
    internal sealed class SaveHeader
    {
        public string Name = "";
        public int Emblem, Week;

        public static SaveHeader Read(string path)
        {
            try
            {
                using (var r = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    r.ReadString();
                    var h = new SaveHeader { Name = r.ReadString(), Emblem = r.ReadInt32() };
                    r.ReadBoolean();
                    h.Week = r.ReadInt32();
                    return h;
                }
            }
            catch { return null; }
        }
    }
}
