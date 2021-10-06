using System.Text;
using Hive.Versioning.Resources;
using System;

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
            var reports = errors.ToArray();

            if (reports.Length > 128) // arbitrary limit
                return SR.Version_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages

            var range = ScanForErrorRange(reports, VersionIsError);

            if (range.Length == 0)
                return SR.ParsingSuccessful;

            if (range.Length == 1 && reports[range.Start].Action == VersionParseAction.ExtraInput)
                return ExtraInputMesage(errors.InputText, reports, range.Start);

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors)
        {
            var reports = errors.ToArray();

            if (reports.Length > 128) // arbitrary limit
                return SR.Range_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages

            var range = ScanForErrorRange(reports, RangeIsError);

            if (range.Length == 0)
                return SR.ParsingSuccessful;

            if (range.Length == 1 && reports[range.Start].Action.Value == RangeParseAction.ExtraInput)
                return ExtraInputMesage(errors.InputText, reports, range.Start);

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        private static string ExtraInputMesage<T>(in StringPart text, ActionErrorReport<T>[] reports, int index)
            where T : struct
        {
            var strb = new StringBuilder();
            var report = reports[index];
            FormatMessageAtPosition(strb, text, report.TextOffset, report.Length, SR.ExtraInputAtEnd);
            return strb.ToString();
        }

        private static (int Start, int Length) ScanForErrorRange<T>(ActionErrorReport<T>[] reports, Func<T, bool> checkIsError)
            where T : struct
        {
            var end = reports.Length - 1;
            var position = end;

            while (position >= 0 && checkIsError(reports[position].Action))
                position--;

            return (position + 1, end - position);
        }

        private static bool VersionIsError(VersionParseAction action)
            => action switch
            {
                VersionParseAction.None => false,
                VersionParseAction.ECoreVersionNumber => true,
                VersionParseAction.ECoreVersionDot => true,
                VersionParseAction.FCoreVersion => false,
                VersionParseAction.EPrerelease => true,
                VersionParseAction.EPrereleaseId => true,
                VersionParseAction.FPrerelease => false,
                VersionParseAction.EBuild => true,
                VersionParseAction.EBuildId => true,
                VersionParseAction.EBuildIdDot => true,
                VersionParseAction.FBuild => false,
                VersionParseAction.EAlphaNumericId => true,
                VersionParseAction.FAlphaNumericId => false,
                VersionParseAction.ENumericId => true,
                VersionParseAction.EValidNumericId => true,
                VersionParseAction.FNumericId => false,
                VersionParseAction.FValidNumericId => false,
                VersionParseAction.ExtraInput => true,
                _ => false,
            };

        private static bool RangeIsError(AnyParseAction action)
            => action.IsVersionAction ? VersionIsError(action.VersionAction)
            : action.Value switch
            {
                RangeParseAction.None => false,
                RangeParseAction.EStarRange1 => true,
                RangeParseAction.EStarRange2 => true,
                RangeParseAction.EStarRange3 => true,
                RangeParseAction.FStarRange => false,
                RangeParseAction.EHyphenVersion => true,
                RangeParseAction.EHyphenVersion2 => true,
                RangeParseAction.EHyphen => true,
                RangeParseAction.FHyphenRange => false,
                RangeParseAction.ECaret => true,
                RangeParseAction.ECaretVersion => true,
                RangeParseAction.FCaretRange => false,
                RangeParseAction.ESubrange1 => true,
                RangeParseAction.EOrderedSubrange => true,
                RangeParseAction.EClosedSubrange => true,
                RangeParseAction.FSubrange => false,
                RangeParseAction.ECompareType => true,
                RangeParseAction.EComparer => true,
                RangeParseAction.EComparerVersion => true,
                RangeParseAction.FComparer => false,
                RangeParseAction.EComponent => true,
                RangeParseAction.FComponent => false,
                RangeParseAction.FStar => false,
                RangeParseAction.FZero => false,
                RangeParseAction.ExtraInput => true,
                _ => false,
            };

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
