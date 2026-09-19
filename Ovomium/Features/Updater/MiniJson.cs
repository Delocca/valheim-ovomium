using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ovomium.Features.Updater
{
    /// <summary>
    /// Lecteur JSON minimal (aucune bibliothèque JSON n'est référencée par le mod) : objets en
    /// <c>Dictionary&lt;string, object&gt;</c>, tableaux en <c>List&lt;object&gt;</c>, chaînes, <c>double</c>, <c>bool</c>,
    /// <c>null</c>. Lève <see cref="System.FormatException"/> sur un document malformé.
    /// </summary>
    internal sealed class MiniJson
    {
        private readonly string m_text;
        private int m_pos;

        private MiniJson(string text)
        {
            m_text = text;
        }

        public static object Parse(string text)
        {
            var parser = new MiniJson(text);
            object value = parser.ReadValue();
            parser.SkipWhitespace();
            if (parser.m_pos != text.Length)
                throw parser.Error("données après la fin du document");
            return value;
        }

        /// <summary>Valeur d'une clé d'objet, ou null si absente / pas du type attendu.</summary>
        public static T Get<T>(object obj, string key) where T : class
        {
            return obj is Dictionary<string, object> dict && dict.TryGetValue(key, out object value) ? value as T : null;
        }

        private object ReadValue()
        {
            SkipWhitespace();
            if (m_pos >= m_text.Length)
                throw Error("fin prématurée");
            char c = m_text[m_pos];
            switch (c)
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return ReadString();
                case 't': Expect("true"); return true;
                case 'f': Expect("false"); return false;
                case 'n': Expect("null"); return null;
                default: return ReadNumber();
            }
        }

        private Dictionary<string, object> ReadObject()
        {
            var result = new Dictionary<string, object>();
            m_pos++; // {
            SkipWhitespace();
            if (Peek() == '}') { m_pos++; return result; }
            while (true)
            {
                SkipWhitespace();
                if (Peek() != '"')
                    throw Error("clé attendue");
                string key = ReadString();
                SkipWhitespace();
                if (Peek() != ':')
                    throw Error("« : » attendu");
                m_pos++;
                result[key] = ReadValue();
                SkipWhitespace();
                char c = Peek();
                m_pos++;
                if (c == '}') return result;
                if (c != ',') throw Error("« , » ou « } » attendu");
            }
        }

        private List<object> ReadArray()
        {
            var result = new List<object>();
            m_pos++; // [
            SkipWhitespace();
            if (Peek() == ']') { m_pos++; return result; }
            while (true)
            {
                result.Add(ReadValue());
                SkipWhitespace();
                char c = Peek();
                m_pos++;
                if (c == ']') return result;
                if (c != ',') throw Error("« , » ou « ] » attendu");
            }
        }

        private string ReadString()
        {
            var sb = new StringBuilder();
            m_pos++; // "
            while (true)
            {
                if (m_pos >= m_text.Length)
                    throw Error("chaîne non terminée");
                char c = m_text[m_pos++];
                if (c == '"')
                    return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (m_pos >= m_text.Length)
                    throw Error("chaîne non terminée");
                sb.Append(ReadEscape(m_text[m_pos++]));
            }
        }

        private string ReadEscape(char code)
        {
            switch (code)
            {
                case '"': return "\"";
                case '\\': return "\\";
                case '/': return "/";
                case 'b': return "\b";
                case 'f': return "\f";
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case 'u':
                    if (m_pos + 4 > m_text.Length)
                        throw Error("séquence \\u tronquée");
                    string hex = m_text.Substring(m_pos, 4);
                    m_pos += 4;
                    return ((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString();
                default:
                    throw Error($"séquence d'échappement inconnue \\{code}");
            }
        }

        private double ReadNumber()
        {
            int start = m_pos;
            while (m_pos < m_text.Length && "+-0123456789.eE".IndexOf(m_text[m_pos]) >= 0)
                m_pos++;
            string token = m_text.Substring(start, m_pos - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw Error($"nombre invalide « {token} »");
            return value;
        }

        private void Expect(string literal)
        {
            if (string.CompareOrdinal(m_text, m_pos, literal, 0, literal.Length) != 0)
                throw Error($"« {literal} » attendu");
            m_pos += literal.Length;
        }

        private char Peek()
        {
            return m_pos < m_text.Length ? m_text[m_pos] : '\0';
        }

        private void SkipWhitespace()
        {
            while (m_pos < m_text.Length && char.IsWhiteSpace(m_text[m_pos]))
                m_pos++;
        }

        private System.FormatException Error(string message)
        {
            return new System.FormatException($"JSON invalide (position {m_pos}) : {message}");
        }
    }
}
