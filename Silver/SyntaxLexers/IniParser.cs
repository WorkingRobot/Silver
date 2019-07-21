using SyntaxHighlighter;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Silver.SyntaxLexers
{
    class IniParser : IHighlighter
    {
        const string REGEX_COMMENT = @"[ \t]*(;|#).*$";
        const string REGEX_SECTION = @"^[ \t]*(\[)(.*?)(\])";
        const string REGEX_INILINE = @"^(?:[ \t]*([""']?)(.+?)\1[ \t]*(=))[ \t]*((?:\""(?:\\.|[^""\\\n])*\"")|(?:(?:\\.|[^\\\n])+))";

        public readonly Regex RegexComment = new Regex(REGEX_COMMENT, RegexOptions.Multiline);
        public readonly Regex RegexSection = new Regex(REGEX_SECTION, RegexOptions.Multiline);
        public readonly Regex RegexIniLine = new Regex(REGEX_INILINE, RegexOptions.Multiline);

        readonly Brush ForegroundComment = Brushes.Green;
        readonly Brush ForegroundSection = Brushes.Orange;
        readonly Brush ForegroundKey = Brushes.Blue;
        readonly Brush ForegroundStrings = Brushes.DarkRed;

        public int Highlight(FormattedText text, int previousBlockCode)
        {
            foreach (Match m in RegexSection.Matches(text.Text))
            {
                var g = m.Groups[2];
                text.SetForegroundBrush(ForegroundSection, g.Index, g.Length);
            }
            foreach (Match m in RegexIniLine.Matches(text.Text))
            {
                var g = m.Groups[2];
                text.SetForegroundBrush(ForegroundKey, g.Index, g.Length);
                g = m.Groups[4];
                text.SetForegroundBrush(ForegroundStrings, g.Index, g.Length);
            }
            foreach (Match m in RegexComment.Matches(text.Text))
            {
                text.SetForegroundBrush(ForegroundComment, m.Index, m.Length);
            }
            return -1;
        }
    }
}
