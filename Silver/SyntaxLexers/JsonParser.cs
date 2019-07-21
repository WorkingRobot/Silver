using SyntaxHighlighter;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Silver.SyntaxLexers
{
    class JsonParser : IHighlighter
    {
        const string REGEX_BRACKET = @"[{}\[\],:]";
        const string REGEX_NUMBERS = @"\d+(?:\.\d+)?";
        const string REGEX_STRINGS = @"\""(?:\\.|[^""\\])*\""";

        public readonly Regex RegexBracket = new Regex(REGEX_BRACKET);
        public readonly Regex RegexNumbers = new Regex(REGEX_NUMBERS);
        public readonly Regex RegexStrings = new Regex(REGEX_STRINGS);

        readonly Brush ForegroundBracket = Brushes.Orange;
        readonly Brush ForegroundNumbers = Brushes.DarkCyan;
        readonly Brush ForegroundStrings = Brushes.DarkRed;

        public int Highlight(FormattedText text, int previousBlockCode)
        {
            foreach (Match m in RegexBracket.Matches(text.Text))
            {
                text.SetForegroundBrush(ForegroundBracket, m.Index, m.Length);
            }
            foreach (Match m in RegexNumbers.Matches(text.Text))
            {
                text.SetForegroundBrush(ForegroundNumbers, m.Index, m.Length);
            }
            foreach (Match m in RegexStrings.Matches(text.Text))
            {
                text.SetForegroundBrush(ForegroundStrings, m.Index, m.Length);
            }
            return -1;
        }
    }
}
