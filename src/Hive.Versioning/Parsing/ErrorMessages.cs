using System.Text;
using Hive.Versioning.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hive.Utilities;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning.Parsing
{
    public static class ErrorMessages
    {
        // TODO: refactor to make data more easily passed up and down the callstack

        public static string GetVersionErrorMessage(ref ParserErrorState<VersionParseAction> errors)
        {
            var reports = errors.ToArray();

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
                        return ProcessVersionExtraInput(text, reports, range, ourTextStart, report);
                    case VersionParseAction.ECoreVersionDot:
                        return ProcessVersionECoreVersionDot(text, reports, range, ourTextStart, report);

                    default:
                        break;
                }
            }

            if (reports.Count > 128) // arbitrary limit
                return SR.Version_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages

            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, text, report.TextOffset, report.Length, report.Action.ToString());
            }

            return sb.ToString();
        }

        private static string ProcessVersionExtraInput(in StringPart text,
            IReadOnlyList<ActionErrorReport<VersionParseAction>> reports,
            (int Start, int Length) range,
            long ourTextStart,
            ActionErrorReport<VersionParseAction> report)
        {
            // This could potentially mean a *lot* of things

            // It could mean the obvious: there's extra input.
            // It coult also mean that the version ended with a number with a leading zero.
            // It could also mean that the version ended with some incorrect attempt at a prerelease or build identifier.

            // Lets start by checking for leading zeroes:
            // If it is a leading zero issue, then the sequence of reports is this:
            //    ...
            //    FValidNumericId
            //    FCoreVersion
            //    ExtraInput
            // Before passing off to check for leading zeroes, we need to remove that FCoreVersion report.
            // But first, lets make sure its where we expect.
            if (range.Start - 1 >= 0 && reports[range.Start - 1].Action == VersionParseAction.FCoreVersion)
            {
                if (TryMatchLeadingZeroNumId(text, (int)ourTextStart, reports.SkipIndex(range.Start - 1).ToLazyList(), range.Start - 1, out var lzMsg, out var lzSuggest))
                    return FormatMessageAtPosition(text, report.TextOffset, report.Length, SR.Suggestion.Format(lzMsg, lzSuggest));

                // Then, lets try to check for a poor man's prerelease.
                // We expect the same FCoreVersion then ExtraInput, just with different starting characters.
                if (TryMatchBadPrerelease(text, (int)ourTextStart, report, out var brMsg, out var brSuggest))
                    return FormatMessageAtPosition(text, report.TextOffset, report.Length, SR.Suggestion.Format(brMsg, brSuggest));
            }

            return ExtraInputMesage(text, report);
        }

        private static bool TryMatchBadPrerelease(in StringPart text,
            int ourTextStart,
            ActionErrorReport<VersionParseAction> report,
            [MaybeNullWhen(false)] out string message, [MaybeNullWhen(false)] out string suggest)
        {
            message = suggest = null;

            // We'll check for several possible delineators.
            if (text[(int)report.TextOffset] is not '.' and not '_' and not '=' and not '/' and not '\\')
                return false;

            // There's a pretty good bet that this is supposed to be a prerelease.
            // Lets scan to the next alphanumeric char, and cut to there, dropping in a dash.
            var position = (int)report.TextOffset;
            while (position < text.Length
                && !(text[position]
                    is (>= 'a' and <= 'z')
                    or (>= 'A' and <= 'Z')
                    or (>= '0' and <= '9')))
            {
                position++;
            }

            if (position == text.Length)
                return false; // it probably wasn't supposed to be a prerelease

            message = SR.Version_PrereleaseUsesDash;

            // call it close enough, lets try for something good
            var part1 = text.Slice(ourTextStart, (int)report.TextOffset);
            var part2 = text.Slice(position, FindEndOfVersion(text, position) - position);
            suggest = part1.ToString() + "-" + part2.ToString();
            return true;
        }

        private static string ProcessVersionECoreVersionDot(in StringPart text,
            IReadOnlyList<ActionErrorReport<VersionParseAction>> reports,
            (int Start, int Length) range,
            long ourTextStart,
            ActionErrorReport<VersionParseAction> report)
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

            // so there are actually 2 things that this could potentially mean
            // 1. it could mean exactly what you think; the input was lacking in some parts of the versoin
            // OR 2. it coult mean that the input had leading zeroes.

            // in order to check for number 2, we look at the immediately preceeding FValidNumericId and check if it is just `0`.
            if (TryMatchLeadingZeroNumId(text, (int)ourTextStart, reports, range.Start, out var lzMsg, out var lzSuggest))
                return FormatMessageAtPosition(text, report.TextOffset, report.Length, SR.Suggestion.Format(lzMsg, lzSuggest));

            // if we get here, we're processing no. 1
            var message = vnumCount switch
            {
                1 => SR.Version_ExpectMinorNumber,
                2 => SR.Version_ExpectPatchNumber,
                _ => throw new InvalidOperationException()
            };

            // for the suggestion, lets trim out the rest as well

            var part1 = text.Slice((int)ourTextStart, (int)(report.TextOffset - ourTextStart));
            var part2 = text.Slice((int)report.TextOffset, (int)(FindEndOfVersion(text, (int)report.TextOffset) - report.TextOffset));
            var suggestion =
                part1.ToString() + vnumCount switch
                {
                    1 => ".0.0",
                    2 => ".0",
                    _ => throw new InvalidOperationException()
                } + part2.ToString();

            return FormatMessageAtPosition(text, report.TextOffset, report.Length, SR.Suggestion.Format(message, suggestion));
        }

        private static bool TryMatchLeadingZeroNumId(in StringPart text, int ownStart, IReadOnlyList<ActionErrorReport<VersionParseAction>> reports, int eindex,
            [MaybeNullWhen(false)] out string message, [MaybeNullWhen(false)] out string suggest)
        {
            message = suggest = null;
            if (eindex < 1) return false;

            var freport = reports[eindex - 1];
            var ereport = reports[eindex];
            if (freport.Action != VersionParseAction.FValidNumericId)
                return false;
            if (freport.Length != 1)
                return false;

            if (text[(int)freport.TextOffset] != '0')
                return false;

            if (text[(int)ereport.TextOffset] is < '0' or > '9')
                return false; // we need to actually continue with more digits

            // now we've matched it
            message = SR.NumIdsDoNotHaveLeadingZeroes;

            // find first nonzero digit, or last actual digit
            var offs = (int)ereport.TextOffset;
            while (text[offs] == '0' && offs < text.Length) offs++;
            if (offs >= text.Length || text[offs] is < '0' or > '9') // we hit the end or ended up at a non-digit
                offs--; // so we back up to find the last zero

            // now we trim to the start of freport for the first half
            var part1 = text.Slice(ownStart, (int)freport.TextOffset - ownStart);
            // then from offs to the end of the string (though this will consume the rest of a range, if we're processing that)
            var part2 = text.Slice(offs, FindEndOfVersion(text, offs) - offs);
            // then our suggestion is just the two parts concatenated
            suggest = part1.ToString() + part2.ToString();
            return true;
        }

        private static int FindEndOfVersion(in StringPart text, int from)
        {
            // TODO: impelment
            return text.Length;
        }

        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors)
        {
            var reports = errors.ToArray();

            var range = ScanForErrorRange(reports, RangeIsError);

            if (range.Length == 0)
                return SR.ParsingSuccessful;

            if (range.Length == 1 && reports[range.Start].Action.Value == RangeParseAction.ExtraInput)
                return ExtraInputMesage(errors.InputText, reports[range.Start]);

            if (reports.Length > 128) // arbitrary limit
                return SR.Range_InputInvalid; // generic error message for when we don't want to spend time generating long ass messages


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
