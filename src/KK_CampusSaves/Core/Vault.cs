using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Joojkk.CampusSaves
{
    /// <summary>A run of records that sit next to each other in the pack.</summary>
    internal struct Run
    {
        public long Offset;
        public int Count;
    }

    /// <summary>One saved state of a campus: what it is, and the recipe to rebuild its file.</summary>
    internal sealed class SaveVersion
    {
        public int Id;
        public string Name = "";
        public string Kind = "manual";      // manual, auto or imported
        public string Trigger = "";
        public DateTime At;
        public int Parent;
        public long Size;
        public string Sha1 = "";
        public string Resume = "";          // where a save made during the day goes back to; empty for a night save
        public List<Run> Runs = new List<Run>();
    }

    /// <summary>
    /// The versions of one campus. "pack.dat" only grows: each record is the piece's SHA-1, its length and its bytes, so
    /// a crash can only leave a cut tail, which the next open drops. A version is its own small text file, replaced in one
    /// rename. The save file itself is never needed to read this, and this is never needed to play that file.
    /// </summary>
    internal sealed class Vault
    {
        private const int Head = 24;
        private const string PackName = "pack.dat";
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("JKPACK1\n");

        private struct Hash : IEquatable<Hash>
        {
            public ulong A, B;
            public uint C;
            public Hash(byte[] h)
            {
                A = BitConverter.ToUInt64(h, 0);
                B = BitConverter.ToUInt64(h, 8);
                C = BitConverter.ToUInt32(h, 16);
            }
            public bool Equals(Hash o) { return A == o.A && B == o.B && C == o.C; }
            public override bool Equals(object o) { return o is Hash && Equals((Hash)o); }
            public override int GetHashCode() { return (int)A; }
        }

        private struct Rec
        {
            public long Offset;
            public int Len;
        }

        public readonly string Dir;
        public readonly List<SaveVersion> Versions = new List<SaveVersion>();
        private readonly Dictionary<Hash, Rec> _index = new Dictionary<Hash, Rec>();
        private long _packEnd;

        private string PackPath { get { return Path.Combine(Dir, PackName); } }
        public long PackSize { get { return _packEnd; } }

        public Vault(string dir)
        {
            Dir = dir;
            Directory.CreateDirectory(dir);
            OpenPack();
            foreach (var f in Directory.GetFiles(dir, "v*.ver").OrderBy(x => x, StringComparer.Ordinal))
            {
                var v = ReadVersion(f);
                if (v != null) Versions.Add(v);
            }
            Versions.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private void OpenPack()
        {
            if (!File.Exists(PackPath))
            {
                using (var w = new FileStream(PackPath, FileMode.CreateNew, FileAccess.Write)) w.Write(Magic, 0, Magic.Length);
                _packEnd = Magic.Length;
                return;
            }
            var head = new byte[Head];
            using (var f = new FileStream(PackPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1 << 16))
            {
                var at = (long)Magic.Length;
                f.Position = at;
                while (at + Head <= f.Length)
                {
                    if (f.Read(head, 0, Head) != Head) break;
                    var len = BitConverter.ToInt32(head, 20);
                    if (len <= 0 || len > Chunker.Max || at + Head + len > f.Length) break;
                    _index[new Hash(head)] = new Rec { Offset = at, Len = len };
                    at += Head + len;
                    f.Position = at;
                }
                if (at < f.Length) f.SetLength(at);   // the cut tail of a save that never finished
                _packEnd = at;
            }
        }

        /// <summary>Turns a save file into a version. Returns null when it is the same as the last one ("nothing changed").</summary>
        public SaveVersion Absorb(string file, SaveVersion v)
        {
            var whole = SHA1.Create();
            var piece = SHA1.Create();
            var head = new byte[Head];
            using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            using (var pack = new FileStream(PackPath, FileMode.Open, FileAccess.Write, FileShare.Read, 1 << 20))
            {
                pack.Position = _packEnd;
                var c = new Chunker(input);
                long runEnd = -1;
                int len;
                while ((len = c.Next()) > 0)
                {
                    whole.TransformBlock(c.Piece, 0, len, null, 0);
                    v.Size += len;
                    var h = piece.ComputeHash(c.Piece, 0, len);
                    var key = new Hash(h);
                    Rec r;
                    if (!_index.TryGetValue(key, out r))
                    {
                        r = new Rec { Offset = _packEnd, Len = len };
                        Buffer.BlockCopy(h, 0, head, 0, 20);
                        Buffer.BlockCopy(BitConverter.GetBytes(len), 0, head, 20, 4);
                        pack.Write(head, 0, Head);
                        pack.Write(c.Piece, 0, len);
                        _packEnd += Head + len;
                        _index[key] = r;
                    }
                    if (v.Runs.Count > 0 && r.Offset == runEnd)
                    {
                        var last = v.Runs[v.Runs.Count - 1];
                        last.Count++;
                        v.Runs[v.Runs.Count - 1] = last;
                    }
                    else v.Runs.Add(new Run { Offset = r.Offset, Count = 1 });
                    runEnd = r.Offset + Head + r.Len;
                }
                pack.Flush();
            }
            whole.TransformFinalBlock(new byte[0], 0, 0);
            v.Sha1 = Hex(whole.Hash);
            var prev = Versions.LastOrDefault();
            if (prev != null && prev.Sha1 == v.Sha1) return null;
            v.Id = prev != null ? prev.Id + 1 : 1;
            WriteVersion(v);
            Versions.Add(v);
            return v;
        }

        /// <summary>Rebuilds a version into a file, and the file only appears if its hash is right.</summary>
        public void Restore(SaveVersion v, string dest)
        {
            var tmp = dest + ".tmp";
            var whole = SHA1.Create();
            var head = new byte[Head];
            var buf = new byte[Chunker.Max];
            using (var pack = new FileStream(PackPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            using (var o = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
            {
                foreach (var run in v.Runs)
                {
                    pack.Position = run.Offset;
                    for (var i = 0; i < run.Count; i++)
                    {
                        if (pack.Read(head, 0, Head) != Head) throw new IOException("cut pack");
                        var len = BitConverter.ToInt32(head, 20);
                        if (len <= 0 || len > buf.Length || pack.Read(buf, 0, len) != len) throw new IOException("bad record");
                        whole.TransformBlock(buf, 0, len, null, 0);
                        o.Write(buf, 0, len);
                    }
                }
            }
            whole.TransformFinalBlock(new byte[0], 0, 0);
            if (Hex(whole.Hash) != v.Sha1)
            {
                File.Delete(tmp);
                throw new IOException("version " + v.Id + " does not match its hash");
            }
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(tmp, dest);
        }

        /// <summary>Takes a version off the list. Its pieces stay until the pack is compacted.</summary>
        public void Remove(SaveVersion v)
        {
            Versions.Remove(v);
            var p = VersionPath(v.Id);
            if (File.Exists(p)) File.Delete(p);
        }

        /// <summary>Bytes of the pack no version uses.</summary>
        public long Garbage()
        {
            var used = new Dictionary<long, bool>();
            long live = Magic.Length;
            var head = new byte[Head];
            using (var pack = new FileStream(PackPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
                foreach (var v in Versions)
                    foreach (var run in v.Runs)
                    {
                        var at = run.Offset;
                        for (var i = 0; i < run.Count; i++)
                        {
                            pack.Position = at;
                            pack.Read(head, 0, Head);
                            var size = Head + BitConverter.ToInt32(head, 20);
                            if (!used.ContainsKey(at)) { used[at] = true; live += size; }
                            at += size;
                        }
                    }
            return _packEnd - live;
        }

        public static string Hex(byte[] b)
        {
            var s = new StringBuilder(b.Length * 2);
            foreach (var x in b) s.Append(x.ToString("x2"));
            return s.ToString();
        }

        private string VersionPath(int id) { return Path.Combine(Dir, "v" + id.ToString("000000") + ".ver"); }

        private void WriteVersion(SaveVersion v)
        {
            var i = CultureInfo.InvariantCulture;
            var s = new StringBuilder();
            s.Append("id=").Append(v.Id).Append('\n');
            s.Append("name=").Append(Clean(v.Name)).Append('\n');
            s.Append("kind=").Append(v.Kind).Append('\n');
            s.Append("trigger=").Append(v.Trigger).Append('\n');
            s.Append("at=").Append(v.At.ToString("o", i)).Append('\n');
            s.Append("parent=").Append(v.Parent).Append('\n');
            s.Append("size=").Append(v.Size).Append('\n');
            s.Append("sha1=").Append(v.Sha1).Append('\n');
            s.Append("resume=").Append(Clean(v.Resume)).Append('\n');
            s.Append("runs\n");
            foreach (var r in v.Runs) s.Append(r.Offset).Append(' ').Append(r.Count).Append('\n');
            var p = VersionPath(v.Id);
            File.WriteAllText(p + ".tmp", s.ToString(), new UTF8Encoding(false));
            if (File.Exists(p)) File.Delete(p);
            File.Move(p + ".tmp", p);
        }

        private static string Clean(string s) { return (s ?? "").Replace('\n', ' ').Replace('\r', ' '); }

        private static SaveVersion ReadVersion(string path)
        {
            try
            {
                var v = new SaveVersion();
                var inRuns = false;
                foreach (var l in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (inRuns)
                    {
                        var sp = l.Split(' ');
                        if (sp.Length == 2) v.Runs.Add(new Run { Offset = long.Parse(sp[0]), Count = int.Parse(sp[1]) });
                        continue;
                    }
                    if (l == "runs") { inRuns = true; continue; }
                    var eq = l.IndexOf('=');
                    if (eq < 0) continue;
                    var k = l.Substring(0, eq);
                    var x = l.Substring(eq + 1);
                    switch (k)
                    {
                        case "id": v.Id = int.Parse(x); break;
                        case "name": v.Name = x; break;
                        case "kind": v.Kind = x; break;
                        case "trigger": v.Trigger = x; break;
                        case "at": v.At = DateTime.Parse(x, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); break;
                        case "parent": v.Parent = int.Parse(x); break;
                        case "size": v.Size = long.Parse(x); break;
                        case "sha1": v.Sha1 = x; break;
                        case "resume": v.Resume = x; break;
                    }
                }
                return inRuns ? v : null;
            }
            catch { return null; }
        }
    }
}
