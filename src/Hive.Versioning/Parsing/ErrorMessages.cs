using System.Text;
using Hive.Versioning.Resources;

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

            if (reports.Length > 128) // arbitrary limit
                return SR.Version_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors)
        {
            var sb = new StringBuilder();

            var reports = errors.ToArray();

            if (reports.Length > 128) // arbitrary limit
                return SR.Range_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        private static void FormatMessageAtPosition(StringBuilder builder, StringPart text, long position, long len, string message)
        {
            _ = builder
                .Append(text).AppendLine()
                .Append(' ', (int)position).Append('^');
            if (len > 0)
                _ = builder.Append('~', (int)len - 1);
            _ = builder.Append(' ').AppendLine(message);
        }
    }
}
