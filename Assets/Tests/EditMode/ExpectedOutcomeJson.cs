using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.13. The smallest JSON reader the expected-outcome tables need: objects (as
    /// ordered dictionaries), arrays, strings, numbers, booleans and null. Unity ships no general JSON
    /// reader the Edit Mode assembly can use and the harness must compile the same source, so the
    /// tables are read here rather than through a package.
    /// </summary>
    internal static class ExpectedOutcomeJson
    {
        internal sealed class JsonObject : List<KeyValuePair<string, object>>
        {
            internal bool TryGet(string key, out object value)
            {
                foreach (var pair in this)
                {
                    if (string.Equals(pair.Key, key, StringComparison.Ordinal))
                    {
                        value = pair.Value;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            internal object Get(string key) => TryGet(key, out var value) ? value : null;

            internal bool Has(string key) => TryGet(key, out _);
        }

        internal static object Parse(string text)
        {
            var reader = new Reader(text);
            reader.SkipWhitespace();
            var value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.AtEnd)
            {
                throw reader.Error("unexpected text after the document");
            }

            return value;
        }

        private sealed class Reader
        {
            private readonly string text;
            private int at;

            internal Reader(string text)
            {
                this.text = text ?? string.Empty;
                if (this.text.Length > 0 && this.text[0] == '﻿')
                {
                    at = 1;
                }
            }

            internal bool AtEnd => at >= text.Length;

            internal FormatException Error(string what)
            {
                var line = 1;
                for (var index = 0; index < Math.Min(at, text.Length); index++)
                {
                    if (text[index] == '\n') line++;
                }

                return new FormatException($"JSON {what} (line {line})");
            }

            internal void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(text[at])) at++;
            }

            internal object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("ended where a value was expected");
                var c = text[at];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || char.IsDigit(c)) return ReadNumber();
                        throw Error($"unexpected character '{c}'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(text, at, word, 0, word.Length) != 0)
                {
                    throw Error($"expected '{word}'");
                }

                at += word.Length;
            }

            private JsonObject ReadObject()
            {
                var result = new JsonObject();
                at++;
                SkipWhitespace();
                if (!AtEnd && text[at] == '}')
                {
                    at++;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || text[at] != '"') throw Error("expected a property name");
                    var key = ReadString();
                    SkipWhitespace();
                    if (AtEnd || text[at] != ':') throw Error($"expected ':' after \"{key}\"");
                    at++;
                    var value = ReadValue();
                    if (result.Has(key)) throw Error($"duplicate property \"{key}\"");
                    result.Add(new KeyValuePair<string, object>(key, value));
                    SkipWhitespace();
                    if (AtEnd) throw Error("ended inside an object");
                    if (text[at] == ',')
                    {
                        at++;
                        continue;
                    }

                    if (text[at] == '}')
                    {
                        at++;
                        return result;
                    }

                    throw Error("expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                at++;
                SkipWhitespace();
                if (!AtEnd && text[at] == ']')
                {
                    at++;
                    return result;
                }

                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (AtEnd) throw Error("ended inside an array");
                    if (text[at] == ',')
                    {
                        at++;
                        continue;
                    }

                    if (text[at] == ']')
                    {
                        at++;
                        return result;
                    }

                    throw Error("expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                var builder = new StringBuilder();
                at++;
                while (true)
                {
                    if (AtEnd) throw Error("ended inside a string");
                    var c = text[at++];
                    if (c == '"') return builder.ToString();
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (AtEnd) throw Error("ended inside an escape");
                    var escaped = text[at++];
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (at + 4 > text.Length) throw Error("short \\u escape");
                            builder.Append((char)int.Parse(
                                text.Substring(at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            at += 4;
                            break;
                        default: throw Error($"unknown escape '\\{escaped}'");
                    }
                }
            }

            private object ReadNumber()
            {
                var start = at;
                if (text[at] == '-') at++;
                while (!AtEnd && (char.IsDigit(text[at]) || text[at] == '.' || text[at] == 'e' ||
                    text[at] == 'E' || text[at] == '+' || text[at] == '-'))
                {
                    at++;
                }

                var literal = text.Substring(start, at - start);
                if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw Error($"bad number '{literal}'");
                }

                return value;
            }
        }
    }
}
