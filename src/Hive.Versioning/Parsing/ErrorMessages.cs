using System.Text;
using Hive.Versioning.Resources;
using System;
using System.Collections.Generic;

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

            return ProcessVersionErrorMessage(errors.InputText, reports);
        }

        private static string ProcessVersionErrorMessage(in StringPart text, IReadOnlyList<ActionErrorReport<VersionParseAction>> reports)
        {
            var range = ScanForErrorRange(reports, VersionIsError);

            if (range.Length == 0)
                return SR.ParsingSuccessful;

            var ourTextStart = reports[0].TextOffset;

            if (range.Length == 1)
            {
                var report = reports[range.Start];
                switch (report.Action)
                {
                    case VersionParseAction.ExtraInput:
                        return ExtraInputMesage(text, report);

                    case VersionParseAction.ECoreVersionDot:
                        {
                            var vnumCount = 0;
                            var pos = range.Start - 1;
                            while (pos >= 0 && reports[pos].Action == VersionParseAction.FValidNumericId)
                            {
                                vnumCount++;
                                pos--;
                            }

                            if (vnumCount is < 1 or > 2)
                                return FormatMessageAtPosition(text, report.TextOffset, report.Length,
                                    SR.WhatHow.Format( // this resource should accurately describe the situation
                                        SR.UnexpectedFValidNumericIdCount.Format(vnumCount)
                                    ));

                            var message = vnumCount switch
                            {
                                1 => SR.Version_ExpectMinorNumber,
                                2 => SR.Version_ExpectPatchNumber,
                                _ => throw new InvalidOperationException()
                            };

                            var suggestion =
                                text.Slice((int)ourTextStart, (int)(report.TextOffset - ourTextStart)).ToString()
                                + vnumCount switch
                                {
                                    1 => ".0.0",
                                    2 => ".0",
                                    _ => throw new InvalidOperationException()
                                };

                            return FormatMessageAtPosition(text, report.TextOffset, report.Length, SR.Suggestion.Format(message, suggestion));
                        }

                    default:
                        break;
                }
            }

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, text, report.TextOffset, report.Length, report.Action.ToString());
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
                return ExtraInputMesage(errors.InputText, reports[range.Start]);

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, errors.InputText, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        private static string ExtraInputMesage<T>(in StringPart text, ActionErrorReport<T> report)
            where T : struct
        {
            var strb = new StringBuilder();
            FormatMessageAtPosition(strb, text, report.TextOffset, report.Length, SR.ExtraInputAtEnd);
            return strb.ToString();
        }

        private static (int Start, int Length) ScanForErrorRange<T>(IReadOnlyList<ActionErrorReport<T>> reports, Func<T, bool> checkIsError)
            where T : struct
        {
            var end = reports.Count - 1;
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

        private static string FormatMessageAtPosition(StringPart text, long position, long len, string message)
        {
            var sb = new StringBuilder();
            FormatMessageAtPosition(sb, text, position, len, message);
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
