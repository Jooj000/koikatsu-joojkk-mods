using System;
using System.IO;

namespace Joojkk.CampusSaves
{
    /// <summary>
    /// Cuts a stream by its content (gear hash), about 16 KiB a piece, so an insertion early in a save moves the bytes
    /// after it but not the cuts. Uses one fixed buffer: a save never sits in memory whole.
    /// </summary>
    internal sealed class Chunker
    {
        public const int Min = 2 * 1024;
        public const int Max = 64 * 1024;
        private const ulong Mask = (1UL << 14) - 1;
        private static readonly ulong[] Gear = MakeGear();

        private readonly Stream _in;
        private readonly byte[] _read = new byte[1 << 20];
        private int _pos, _have;

        /// <summary>The current piece, valid until the next call of <see cref="Next"/>.</summary>
        public readonly byte[] Piece = new byte[Max];

        public Chunker(Stream input) { _in = input; }

        private static ulong[] MakeGear()
        {
            // Fixed seed: the cuts have to be the same in every session, or nothing would ever repeat.
            var r = new Random(0x4A4F4F4A);
            var g = new ulong[256];
            var b = new byte[8];
            for (var i = 0; i < 256; i++)
            {
                r.NextBytes(b);
                g[i] = BitConverter.ToUInt64(b, 0);
            }
            return g;
        }

        /// <summary>Fills <see cref="Piece"/> and returns its length, 0 at the end.</summary>
        public int Next()
        {
            var len = 0;
            ulong h = 0;
            while (len < Max)
            {
                if (_pos == _have)
                {
                    _have = _in.Read(_read, 0, _read.Length);
                    _pos = 0;
                    if (_have <= 0) { _have = 0; break; }
                }
                var c = _read[_pos++];
                Piece[len++] = c;
                h = (h << 1) + Gear[c];
                if (len >= Min && (h & Mask) == 0) break;
            }
            return len;
        }
    }
}
