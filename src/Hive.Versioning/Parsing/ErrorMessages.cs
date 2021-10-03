using System.Text;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using Hive.Utilities;
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning.Parsing
{
    public static class ErrorMessages
    {
        public static string GetVersionErrorMessage(ref ParserErrorState<VersionParseAction> errors)
        {
            var sb = new StringBuilder();

            var reports = errors.ToArray();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Action.ToString());
            }

            return sb.ToString();
        }

        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors)
        {
            var sb = new StringBuilder();

            var reports = errors.ToArray();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Action.ToString());
            }

            return sb.ToString();
        }

        private static void FormatMessageAtPosition(StringBuilder builder, StringPart text, long position, string message)
        {
            _ = builder
                .Append(text).AppendLine()
                .Append(' ', (int)position).Append("^ ")
                .AppendLine(message);
        }
    }
}
