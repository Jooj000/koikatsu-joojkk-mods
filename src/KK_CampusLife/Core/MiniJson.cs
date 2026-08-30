using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Tiny JSON reader/writer (.NET 3.5 has none). Objects become Dictionary&lt;string, object&gt;,
    /// arrays List&lt;object&gt;, numbers double, plus string, bool and null.
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) return null;
            var p = new Parser(json);
            var value = p.ReadValue();
            return value;
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder(4096);
            Write(sb, value);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }
            var s = value as string;
            if (s != null) { WriteString(sb, s); return; }
            if (value is bool) { sb.Append((bool)value ? "true" : "false"); return; }
            var dict = value as IDictionary;
            if (dict != null)
            {
                sb.Append('{');
                var first = true;
                foreach (DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, e.Key.ToString());
                    sb.Append(':');
                    Write(sb, e.Value);
                }
                sb.Append('}');
                return;
            }
            var list = value as IEnumerable;
            if (list != null)
            {
                sb.Append('[');
                var first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item);
                }
                sb.Append(']');
                return;
            }
            if (value is float || value is double || value is decimal)
            {
                sb.Append(Convert.ToDouble(value).ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            if (value is int || value is long || value is short || value is byte || value is uint || value is ulong || value is sbyte || value is ushort)
            {
                sb.Append(Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (value is Enum)
            {
                sb.Append(Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture));
                return;
            }
            WriteString(sb, value.ToString());
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; }

            private void SkipWs()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ReadValue()
            {
                SkipWs();
                if (_i >= _s.Length) return null;
                var c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': _i += 4; return true;
                    case 'f': _i += 5; return false;
                    case 'n': _i += 4; return null;
                    default: return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                _i++; // {
                while (true)
                {
                    SkipWs();
                    if (_i >= _s.Length) break;
                    if (_s[_i] == '}') { _i++; break; }
                    if (_s[_i] == ',') { _i++; continue; }
                    var key = ReadString();
                    SkipWs();
                    if (_i < _s.Length && _s[_i] == ':') _i++;
                    d[key] = ReadValue();
                }
                return d;
            }

            private List<object> ReadArray()
            {
                var l = new List<object>();
                _i++; // [
                while (true)
                {
                    SkipWs();
                    if (_i >= _s.Length) break;
                    if (_s[_i] == ']') { _i++; break; }
                    if (_s[_i] == ',') { _i++; continue; }
                    l.Add(ReadValue());
                }
                return l;
            }

            private string ReadString()
            {
                SkipWs();
                var sb = new StringBuilder();
                if (_i < _s.Length && _s[_i] == '"') _i++;
                while (_i < _s.Length)
                {
                    var c = _s[_i++];
                    if (c == '"') break;
                    if (c == '\\' && _i < _s.Length)
                    {
                        var e = _s[_i++];
                        switch (e)
                        {
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'u':
                                if (_i + 4 <= _s.Length)
                                {
                                    sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber));
                                    _i += 4;
                                }
                                break;
                            default: sb.Append(e); break;
                        }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            private object ReadNumber()
            {
                var start = _i;
                while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                double d;
                double.TryParse(_s.Substring(start, _i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
                if (start == _i) _i++; // never loop forever on garbage
                return d;
            }
        }
    }

    /// <summary>Typed accessors for parsed JSON objects.</summary>
    internal static class JsonExt
    {
        public static int GetInt(this Dictionary<string, object> d, string key, int def = 0)
        {
            object o;
            if (d != null && d.TryGetValue(key, out o) && o != null)
            {
                if (o is double) return (int)Math.Round((double)o);
                int r;
                if (int.TryParse(o.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out r)) return r;
            }
            return def;
        }

        public static float GetFloat(this Dictionary<string, object> d, string key, float def = 0f)
        {
            object o;
            if (d != null && d.TryGetValue(key, out o) && o != null)
            {
                if (o is double) return (float)(double)o;
                float r;
                if (float.TryParse(o.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out r)) return r;
            }
            return def;
        }

        public static bool GetBool(this Dictionary<string, object> d, string key, bool def = false)
        {
            object o;
            if (d != null && d.TryGetValue(key, out o) && o is bool) return (bool)o;
            return def;
        }

        public static string GetString(this Dictionary<string, object> d, string key, string def = "")
        {
            object o;
            if (d != null && d.TryGetValue(key, out o) && o != null) return o.ToString();
            return def;
        }

        public static Dictionary<string, object> GetObj(this Dictionary<string, object> d, string key)
        {
            object o;
            if (d != null && d.TryGetValue(key, out o)) return o as Dictionary<string, object>;
            return null;
        }

        public static List<object> GetList(this Dictionary<string, object> d, string key)
        {
            object o;
            if (d != null && d.TryGetValue(key, out o)) return o as List<object>;
            return null;
        }
    }
}
