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

        private record GeneratedMessage(string Message,
            string? Suggestion = null,
            (long Start, long Length)? Span = null,
            bool ShowSuggestion = true);

        private ref struct MessageInfo
        {
            public readonly StringPart Text;

            public MessageInfo(in StringPart text)
            {
                Text = text;
            }
        }

        private static (long Start, long Length) SpanFromReport<T>(in ActionErrorReport<T> report)
            where T : struct
            => (report.TextOffset, report.Length);

        #region Version
        public static string GetVersionErrorMessage(ref ParserErrorState<VersionParseAction> errors)
        {
            var reports = errors.ToArray();

            var msgInfo = new MessageInfo(errors.InputText);
            var msg = ProcessVersionErrorMessage(in msgInfo, reports);
            return FormatMessage(in msgInfo, msg);
        }

        private static GeneratedMessage ProcessVersionErrorMessage(in MessageInfo msgs, IReadOnlyList<ActionErrorReport<VersionParseAction>> reports)
        {
            var range = ScanForErrorRange(reports, VersionIsError);

            if (range.Length == 0)
                return new(SR.ParsingSuccessful);

            var ourTextStart = reports[0].TextOffset;

            if (range.Length == 1)
            {
                var report = reports[range.Start];
#pragma warning disable IDE0010 // Add missing cases
                // In all of my bad version tests, these are the only 2 that actually get hit.
                switch (report.Action)
#pragma warning restore IDE0010 // Add missing cases
                {
                    case VersionParseAction.ExtraInput:
                        return ProcessVersionExtraInput(in msgs, reports, range, ourTextStart, report);
                    case VersionParseAction.ECoreVersionDot:
                        return ProcessVersionECoreVersionDot(in msgs, reports, range, ourTextStart, report);

                    default:
                        break;
                }
            }

            if (range.Length == 2)
            {
                switch (reports[range.Start].Action)
                {
                    case VersionParseAction.ENumericId:
                        {
                            // This means that there *was* no number provided. We can just suggest a 0.0.1 prefix.
                            var versionPart = msgs.Text.Slice(
                                (int)reports[range.Start].TextOffset,
                                FindEndOfVersion(msgs.Text, (int)reports[range.Start].TextOffset)).ToString();
                            if (versionPart.Length > 0 && versionPart[0] is not '-' and not '+')
                                versionPart = "-" + versionPart;
                            var suggest = "0.0.1" + versionPart;
                            return new(SR.Version_MustBeginWithMajorMinorPatch, suggest, SpanFromReport(reports[range.Start]));
                        }

                    case VersionParseAction.EValidNumericId:
                        if (TryMatchTooBigCoreNumber(in msgs, reports, range.Start, out var tbMsg))
                            return tbMsg;
                        break;
                }
            }

            if (reports.Count > 128) // arbitrary limit
                return new(SR.Version_InputInvalid); // generic error message for when we don't want to spend time generating long ass messages

            // TODO: fix this
            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, msgs.Text, report.TextOffset, report.Length, report.Action.ToString());
            }

            return new(sb.ToString());
        }

        #region 1-long error range
        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "All other cases are correctly falling through.")]
        private static GeneratedMessage ProcessVersionExtraInput(in MessageInfo msgs,
            IReadOnlyList<ActionErrorReport<VersionParseAction>> reports,
            (int Start, int Length) range,
            long ourTextStart,
            ActionErrorReport<VersionParseAction> report)
        {
            // This could potentially mean a *lot* of things

            // It could mean the obvious: there's extra input.
            // It coult also mean that the version ended with a number with a leading zero.
            // It could also mean that the version ended with some incorrect attempt at a prerelease or build identifier.

            if (range.Start - 1 >= 0)
            {
                // Lets start by checking for leading zeroes:
                // If it is a leading zero issue, then the sequence of reports is this:
                //    ...
                //    FValidNumericId
                //    FCoreVersion
                //    ExtraInput
                // Before passing off to check for leading zeroes, we need to remove that FCoreVersion report.
                // But first, lets make sure its where we expect.
                if (TryMatchLeadingZeroNumId(in msgs, (int)ourTextStart, reports.SkipIndex(range.Start - 1).ToLazyList(), range.Start - 1, out var lzMsg))
                    return lzMsg;

                switch (reports[range.Start - 1].Action)
                {
                    // Then, lets try to check for a poor man's prerelease.
                    // We expect the same FCoreVersion then ExtraInput, just with different starting characters.
                    case VersionParseAction.FCoreVersion:
                        {
                            if (TryMatchBadPrerelease(in msgs, (int)ourTextStart, false, false, report, out var brMsg))
                                return brMsg;
                        }
                        break;

                    // We could still have a bad prerelease, but we'd have expected a preceeding FPrerelease.
                    // We could *also* still have a LZNumId, just in the prerelease instead.
                    case VersionParseAction.FPrerelease:
                        {
                            if (TryMatchBadPrerelease(in msgs, (int)ourTextStart, true, false, report, out var brMsg))
                                return brMsg;
                        }
                        break;

                    // We could also have a bad build ID, lets check that.
                    case VersionParseAction.FBuild:
                        {
                            if (TryMatchBadPrerelease(in msgs, (int)ourTextStart, false, true, report, out var brMsg))
                                return brMsg;
                        }
                        break;

                    default: break;
                }
            }

            return ExtraInputMesage(in msgs, report);
        }

        private static bool TryMatchBadPrerelease(in MessageInfo msgs,
            int ourTextStart, bool inPre, bool inBuild,
            ActionErrorReport<VersionParseAction> report,
            [MaybeNullWhen(false)] out GeneratedMessage message)
        {
            message = null;

            // We'll check for several possible delineators.
            if (msgs.Text[(int)report.TextOffset] is not '_' and not '=' and not '/' and not '\\' and not '+')
            {
                // These delimiters are based on where we are
                if (inPre)
                {
                    if (msgs.Text[(int)report.TextOffset] is not '-')
                        return false;
                }
                else
                {
                    if (msgs.Text[(int)report.TextOffset] is not '.')
                        return false;
                }
            }

            // There's a pretty good bet that this is supposed to be a prerelease.
            // Lets scan to the next alphanumeric char, and cut to there, dropping in a dash.
            var position = (int)report.TextOffset;
            while (position < msgs.Text.Length
                && !(msgs.Text[position]
                    is (>= 'a' and <= 'z')
                    or (>= 'A' and <= 'Z')
                    or (>= '0' and <= '9')))
            {
                position++;
            }

            if (position == msgs.Text.Length)
                return false; // it probably wasn't supposed to be a prerelease


            // call it close enough, lets try for something good
            var part1 = msgs.Text.Slice(ourTextStart, (int)report.TextOffset);
            var part2 = msgs.Text.Slice(position, FindEndOfVersion(msgs.Text, position) - position);
            // instead of returning the full report range, just mark the start
            var suggested = part1.ToString() + (inPre || inBuild ? "." : "-") + part2.ToString();
            message = new((inPre, inBuild) switch
            {
                (true, _) => SR.Version_PrereleaseContainsDot,
                (false, true) => SR.Version_BuildContainsDot,
                (false, false) => SR.Version_PrereleaseUsesDash,
            }, suggested, (report.TextOffset, 0));
            return true;
        }

        private static GeneratedMessage ProcessVersionECoreVersionDot(in MessageInfo msgs,
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
                return new(SR.WhatHow.Format( // this resource should accurately describe the situation
                        SR.UnexpectedFValidNumericIdCount.Format(vnumCount)
                    ), Span: SpanFromReport(report));

            // so there are actually 2 things that this could potentially mean
            // 1. it could mean exactly what you think; the input was lacking in some parts of the versoin
            // OR 2. it coult mean that the input had leading zeroes.

            // in order to check for number 2, we look at the immediately preceeding FValidNumericId and check if it is just `0`.
            if (TryMatchLeadingZeroNumId(in msgs, (int)ourTextStart, reports, range.Start, out var lzMsg))
                return lzMsg;

            // if we get here, we're processing no. 1
            var message = vnumCount switch
            {
                1 => SR.Version_ExpectMinorNumber,
                2 => SR.Version_ExpectPatchNumber,
                _ => throw new InvalidOperationException()
            };

            // for the suggestion, lets trim out the rest as well

            var part1 = msgs.Text.Slice((int)ourTextStart, (int)(report.TextOffset - ourTextStart));
            var part2 = msgs.Text.Slice((int)report.TextOffset, (int)(FindEndOfVersion(msgs.Text, (int)report.TextOffset) - report.TextOffset));
            var suggestion =
                part1.ToString() + vnumCount switch
                {
                    1 => ".0.0",
                    2 => ".0",
                    _ => throw new InvalidOperationException()
                } + part2.ToString();

            return new(message, suggestion, SpanFromReport(report));
        }
        #endregion

        #region 2-long error range
        private static bool TryMatchTooBigCoreNumber(in MessageInfo msgs,
            IReadOnlyList<ActionErrorReport<VersionParseAction>> reports,
            int eindex,
            [MaybeNullWhen(false)] out GeneratedMessage message)
        {
            message = null;
            if (eindex + 1 >= reports.Count) return false;

            var freport = reports[eindex];
            var ereport = reports[eindex + 1];

            if (freport.Action is not VersionParseAction.EValidNumericId)
                return false;
            if (ereport.Action is not VersionParseAction.ECoreVersionNumber)
                return false;

            var restOfVer = (int)(freport.TextOffset + freport.Length);
            var contSuggestion = "0" + msgs.Text.Slice(restOfVer, FindEndOfVersion(msgs.Text, restOfVer) - restOfVer).ToString();
            message = new(SR.Version_NumberTooBig, contSuggestion, Span: SpanFromReport(freport), ShowSuggestion: false);
            return true;
        }
        #endregion

        private static bool TryMatchLeadingZeroNumId(in MessageInfo msgs,
            int ownStart,
            IReadOnlyList<ActionErrorReport<VersionParseAction>> reports,
            int eindex,
            [MaybeNullWhen(false)] out GeneratedMessage message)
        {
            message = null;
            if (eindex < 1) return false;

            var freport = reports[eindex - 1];
            var ereport = reports[eindex];
            if (freport.Action is not VersionParseAction.FValidNumericId and not VersionParseAction.FNumericId)
                return false;
            if (freport.Length != 1)
                return false;

            if (msgs.Text[(int)freport.TextOffset] != '0')
                return false;

            if (msgs.Text[(int)ereport.TextOffset] is < '0' or > '9')
                return false; // we need to actually continue with more digits

            // now we've matched it

            // find first nonzero digit, or last actual digit
            var offs = (int)ereport.TextOffset;
            while (offs < msgs.Text.Length && msgs.Text[offs] == '0') offs++;
            if (offs >= msgs.Text.Length || msgs.Text[offs] is < '0' or > '9') // we hit the end or ended up at a non-digit
                offs--; // so we back up to find the last zero

            // find the end of the run of digits
            var end = offs;
            while (end < msgs.Text.Length && msgs.Text[end] is >= '0' and <= '9') end++;

            // now we trim to the start of freport for the first half
            var part1 = msgs.Text.Slice(ownStart, (int)freport.TextOffset - ownStart);
            // then from offs to the end of the string (though this will consume the rest of a range, if we're processing that)
            var part2 = msgs.Text.Slice(offs, FindEndOfVersion(msgs.Text, offs) - offs);
            // then our suggestion is just the two parts concatenated
            message = new(SR.NumIdsDoNotHaveLeadingZeroes, part1.ToString() + part2.ToString(), ((int)freport.TextOffset, end - (int)freport.TextOffset));
            return true;
        }

        private static int FindEndOfVersion(in StringPart text, int from)
        {
            // TODO: impelment
            return text.Length;
        }
        #endregion

        #region VersionRange
        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors)
        {
            var reports = errors.ToArray();

            var msgInfo = new MessageInfo(errors.InputText);
            var msg = ProcessVersionRangeMessage(in msgInfo, reports);
            return FormatMessage(in msgInfo, msg);
        }

        private static GeneratedMessage ProcessVersionRangeMessage(in MessageInfo msgs, ActionErrorReport<AnyParseAction>[] reports)
        {
            var range = ScanForErrorRange(reports, RangeIsError);

            if (range.Length == 0)
                return new(SR.ParsingSuccessful);

            if (range.Length == 1 && reports[range.Start].Action.Value == RangeParseAction.ExtraInput)
                return ExtraInputMesage(in msgs, reports[range.Start]);

            if (reports.Length > 128) // arbitrary limit
                return new(SR.Range_InputInvalid); // generic error message for when we don't want to spend time generating long ass messages

            // TODO: fix
            var sb = new StringBuilder();

            foreach (var report in reports)
            {
                FormatMessageAtPosition(sb, msgs.Text, report.TextOffset, report.Length, report.Action.ToString());
            }

            return new(sb.ToString());
        }
        #endregion

        private static GeneratedMessage ExtraInputMesage<T>(in MessageInfo msgs, ActionErrorReport<T> report)
            where T : struct
            => new(SR.ExtraInputAtEnd, Span: SpanFromReport(report));

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

        private static string FormatMessage(in MessageInfo msgs, GeneratedMessage message)
        {
            var msgString = message.Message;
            if (message.Suggestion is not null && message.ShowSuggestion)
                msgString = SR.Suggestion.Format(msgString, message.Suggestion);
            if (message.Span is { } span)
            {
                return FormatMessageAtPosition(msgs.Text, span.Start, span.Length, msgString);
            }
            else
            {
                return msgString;
            }
        }

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
