using System.Text;
using Hive.Versioning.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hive.Utilities;
using static Hive.Versioning.StaticHelpers;

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
            (long Start, long Length)? ApplyRange = null, // if ApplyRange is null, we use Span
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

        private static GeneratedMessage? ProcessVersionErrorMessage(in MessageInfo msgs, IReadOnlyList<ActionErrorReport<VersionParseAction>> reports)
        {
            var range = ScanForErrorRange(reports, VersionIsError);

            if (range.Length == 0)
                return null;

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
#pragma warning disable IDE0010 // Add missing cases
                // In all of my bad version tests, these are the only 2 that actually get hit.
                switch (reports[range.Start].Action)
#pragma warning restore IDE0010 // Add missing cases
                {
                    case VersionParseAction.ENumericId:
                        return ProcessENumericId(msgs, reports, range);

                    case VersionParseAction.EValidNumericId:
                        if (TryMatchTooBigCoreNumber(in msgs, reports, range.Start, out var tbMsg))
                            return tbMsg;
                        break;

                    default:
                        break;
                }
            }

            if (range.Length >= 2)
            {
                // Now we'll check for bad prerelease identifiers.
                var last = range.Start + range.Length - 1;
                if (reports[last].Action == VersionParseAction.EPrerelease
                    && reports[last - 1].Action == VersionParseAction.EPrereleaseId)
                {
                    // This is a bad prerelease identifier.
                    var message = SR.Version_PrereleasIdsAreAlphaNumeric;
                    // TODO: generate suggestion somehow
                    return new(message, Span: SpanFromReport(reports[last - 1]));
                }

                // Let's also check for bad build identifiers.
                if (reports[last].Action == VersionParseAction.EBuild
                    && reports[last - 1].Action == VersionParseAction.EBuildId)
                {
                    // This is a bad prerelease identifier.
                    var message = SR.Version_BuildIdsAreAlphaNumeric;
                    // TODO: generate suggestion somehow
                    return new(message, Span: SpanFromReport(reports[last - 1]));
                }
            }

            var realStart = reports[range.Start].TextOffset;
            var lastReport = reports[range.Start + range.Length - 1];
            var realEnd = lastReport.TextOffset + lastReport.Length;

            return new(SR.ReportVersionInput, Span: (realStart, realEnd));
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
                            // if the above fails, then we found an invalid character in the prerelease ID.
                            // TODO: create suggestion here
                            return new(SR.Version_PrereleasIdsAreAlphaNumeric, Span: SpanFromReport(report));
                        }

                    // We could also have a bad build ID, lets check that.
                    case VersionParseAction.FBuild:
                        {
                            if (TryMatchBadPrerelease(in msgs, (int)ourTextStart, false, true, report, out var brMsg))
                                return brMsg;
                            // if the above fails, then we found an invalid character in the build ID.
                            // TODO: create suggestion here
                            return new(SR.Version_BuildIdsAreAlphaNumeric, Span: SpanFromReport(report));
                        }

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
        private static GeneratedMessage ProcessENumericId(in MessageInfo msgs, IReadOnlyList<ActionErrorReport<VersionParseAction>> reports, (int Start, int Length) range)
        {
            // This means that one or more numbers were left out.
            var versionPart = msgs.Text.Slice(
                (int)reports[range.Start].TextOffset,
                FindEndOfVersion(msgs.Text, (int)reports[range.Start].TextOffset) - (int)reports[range.Start].TextOffset).ToString();

            // lets figure out what parts we *do* have
            var fnumIdx = range.Start;
            while (fnumIdx - 1 >= 0
                && reports[fnumIdx - 1].Action == VersionParseAction.FValidNumericId)
                fnumIdx--;

            // importantly, if we're processing this error, then we *did* find a dot
            var versionPrefix = (range.Start - fnumIdx) switch
            {
                0 => "0.0.0",
                1 => "0.0",
                2 => "0",
                _ => throw new InvalidOperationException()
            };

            var fullPre = msgs.Text.Slice(
                (int)reports[fnumIdx].TextOffset,
                (int)reports[range.Start].TextOffset);

            if (versionPart.Length > 0 && versionPart[0] is not '-' and not '+')
                versionPart = "-" + versionPart;

            var suggest = fullPre.ToString() + versionPrefix + versionPart;
            return new(SR.Version_MustBeginWithMajorMinorPatch, suggest, SpanFromReport(reports[range.Start]));
        }

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
            // To be conservative with our guess, we'll only count whitespace and some special characters.
            while (from < text.Length
                && text[from] is not '~'
                    and not '<' and not '>'
                    and not '=' and not '^'
                    and not '|'
                && !char.IsWhiteSpace(text[from]))
            {
                from++;
            }
            return from;
        }
        #endregion

        #region VersionRange
        public static string GetVersionRangeErrorMessage(ref ParserErrorState<AnyParseAction> errors, bool tryReparse)
        {
            var reports = errors.ToArray();

            var origMsgInfo = new MessageInfo(errors.InputText);
            var msgInfo = origMsgInfo;
            var msgs = new List<GeneratedMessage>();
            var startOffset = 0;
            var printSuggestion = true;

            while (true)
            {
                var msg = ProcessVersionRangeMessage(in msgInfo, reports);

                if (msg is null)
                    break;

                msgs.Add(msg with
                {
                    Span = msg.Span is { } sp ? (sp.Start + startOffset, sp.Length) : null,
                    ApplyRange = msg.ApplyRange is { } sp2 ? (sp2.Start + startOffset, sp2.Length) : null,
                });

                if (!tryReparse)
                    break;

                var maybeReplaceRange = msg.ApplyRange ?? msg.Span;
                if (msg.Suggestion is not null && maybeReplaceRange is { } replaceRange)
                {
                    printSuggestion = msg.ShowSuggestion;

                    var start = msgInfo.Text.Slice(0, (int)replaceRange.Start);
                    var end = msgInfo.Text.Slice((int)(replaceRange.Start + replaceRange.Length));
                    StringPart newText = start.ToString() + msg.Suggestion + end.ToString();

                    startOffset += (int)replaceRange.Length - msg.Suggestion.Length;

                    // reset errors and attempt a reparse using the new replacement
                    errors.Dispose();
                    errors = new ParserErrorState<AnyParseAction>(newText);
                    msgInfo = new(newText);
                    // we don't actually care about the outputs, only the errors
                    _ = VersionRange.TryParse(ref errors, newText, out _);
                    reports = errors.ToArray();
                }
                else
                {
                    printSuggestion = false;
                    break;
                }
            }

            if (msgs.Count == 0)
                return SR.ParsingSuccessful;

            // build a sequence of messages, where each message ocurrs exactly once, ordered according to first ocurrence, and keep a list of all locations
            var messageLocations = msgs
                .GroupBy(m => m.Message)
                .Select(g => (Msg: g.Key, Locs: g.Select(m => m.Span).OrderBy(t => t?.Start).Distinct().ToLazyList()))
                .Select(t => (t.Msg, t.Locs, fst: t.Locs.Min(t => t?.Start ?? long.MaxValue)))
                .OrderBy(t => t.fst)
                // then take that list, and build up a partitioning of the locations such that they are all mutually exclusive
                .Select(t => (t.Msg, OverlapPartitioner(t.Locs.WhereNotNull())));

            // now we can go ahead and *build* that error message
            // input text is in origMsgInfo.Text
            var sb = new StringBuilder();
            // always start with our original text
            _ = sb.Append(origMsgInfo.Text).AppendLine();
            foreach (var (msg, locations) in messageLocations)
            {
                // now we emit our location information
                var msgPosition = 0L;
                foreach (var line in locations)
                {
                    var lastEnd = 0L;
                    foreach (var (start, len) in line)
                    {
                        if (lastEnd == 0) // first iteration
                            msgPosition = start;
                        _ = sb.Append(' ', (int)(start - lastEnd)).Append('^');
                        if (len > 0)
                            _ = sb.Append('~', (int)len - 1);
                        lastEnd = start + len;
                    }
                    _ = sb.AppendLine();
                }

                // now we append our message
                _ = sb.Append(' ', (int)msgPosition).Append(msg).AppendLine();
            }

            if (printSuggestion)
            {
                // now we want to append our suggestion
                // final suggested value is in msgInfo.Text
                _ = sb.AppendLine().Append(SR.Suggestion.Format(msgInfo.Text.ToString()));
            }

            return sb.ToString(); // and then we're done
        }

        private static IEnumerable<IEnumerable<(long Start, long Length)>> OverlapPartitioner(IEnumerable<(long Start, long Length)> src)
        {
            // this assumes our input is already sorted by start position
            var effectiveRanges = new List<(long Start, long Length)>();
            var outputs = new List<List<(long Start, long Length)>>();

            foreach (var (start, len) in src)
            {
                var insertIdx = 0;
                for (; insertIdx < effectiveRanges.Count; insertIdx++)
                {
                    var effRange = effectiveRanges[insertIdx];
                    if (start >= effRange.Start + effRange.Length)
                        break; // if this is the case, then, because we enter sorted, we can insert at this index
                }

                if (insertIdx >= effectiveRanges.Count)
                {
                    effectiveRanges.Add((start, len));
                    outputs.Add(new() { (start, len) });
                }
                else
                {
                    var effRange = effectiveRanges[insertIdx];
                    effectiveRanges[insertIdx] = (effRange.Start, start + len - effRange.Start);
                    outputs[insertIdx].Add((start, len));
                }
            }

            return outputs;
        }

        private static GeneratedMessage? ProcessVersionRangeMessage(in MessageInfo msgs, IReadOnlyList<ActionErrorReport<AnyParseAction>> reports)
        {
            var range = ScanForErrorRange(reports, RangeIsError);

            if (range.Length == 0)
                return null;

            if (range.Length == 1)
            {
                var report = reports[range.Start];

                // 1-long error reports *should* only ever be range parser errors or ExtraInput
                Assert(!report.Action.IsVersionAction || report.Action.Value == RangeParseAction.ExtraInput);

                switch (report.Action.Value)
                {
                    case RangeParseAction.ExtraInput:
                        return ExtraInputMesage(in msgs, report);

                    case RangeParseAction.EClosedSubrange:
                    case RangeParseAction.EOrderedSubrange:
                        return ProcessRangeBadSubrange(in msgs, reports, range.Start);

                    default: break;
                }
            }

            if (range.Length == 2)
            {
                if (reports[range.Start + 1].Action.Value == RangeParseAction.ExtraInput
                    && reports[range.Start].Action.Value is RangeParseAction.EClosedSubrange or RangeParseAction.EOrderedSubrange)
                    return ProcessRangeBadSubrange(in msgs, reports, range.Start);
            }

            // TODO: more advanced processing of potential error cases

            // lets check the last error to try generate more ExtraInput messsages
            if (reports[range.Start + range.Length - 1].Action.Value == RangeParseAction.ExtraInput)
                return ExtraInputMesage(in msgs, reports[range.Start + range.Length - 1]);


            var realStart = reports[range.Start].TextOffset;
            var lastReport = reports[range.Start + range.Length - 1];
            var realEnd = lastReport.TextOffset + lastReport.Length;

            return new(SR.ReportRangeInput, Span: (realStart, realEnd));
        }

        private static GeneratedMessage ProcessRangeBadSubrange(in MessageInfo msgs, IReadOnlyList<ActionErrorReport<AnyParseAction>> reports, int start)
        {
            if (start - 2 < 0)
                return new(SR.WhatHow.Format($"ProcessRangeBadSubrange (start - 2 < 0, start = {start})"), Span: SpanFromReport(reports[start]));

            // EClosedSubrange should always be after 2 FComparers
            var c1 = reports[start - 2];
            var c2 = reports[start - 1];
            Assert(c1.Action.Value == RangeParseAction.FComparer);
            Assert(c2.Action.Value == RangeParseAction.FComparer);

            // we know what the error was, lets do a bunch of work to build up a corrected
            // we also know, as it happens, that the two comparers are valid, so we can just parse them to build a suggestion
            var ct1 = msgs.Text.Slice((int)c1.TextOffset, (int)c1.Length);
            var ct2 = msgs.Text.Slice((int)c2.TextOffset, (int)c2.Length);

            ParserErrorState<AnyParseAction> errors = default;
            Assert(RangeParser.TryParseComparer(ref errors, ref ct1, out var comparer1) && ct1.Length == 0);
            Assert(RangeParser.TryParseComparer(ref errors, ref ct2, out var comparer2) && ct2.Length == 0);

            static VersionRange.ComparisonType GetDirection(in VersionRange.VersionComparer comparer)
                => comparer.Type & VersionRange.ComparisonType._DirectionMask;
            static bool HasDirection(in VersionRange.VersionComparer comparer)
                => GetDirection(comparer) != VersionRange.ComparisonType.None;

            if (comparer1.CompareTo > comparer2.CompareTo)
            {
                // if 1 is higher than 2, then swap them
                var tmp = comparer1;
                comparer1 = comparer2;
                comparer2 = tmp;
            }

            bool isSingle;
            if (comparer1.CompareTo == comparer2.CompareTo)
            {
                isSingle = true;
                comparer2 = new(comparer1.CompareTo,
                    GetDirection(comparer1) == GetDirection(comparer2)
                    ? comparer1.Type & comparer2.Type
                    : VersionRange.ComparisonType.ExactEqual);
            }
            else
            {
                isSingle = false;
                // make sure that they both have directionality associated with tem
                if (!HasDirection(comparer1))
                    comparer1 = new(comparer1.CompareTo, VersionRange.ComparisonType.Greater | comparer1.Type);
                if (!HasDirection(comparer2))
                    comparer2 = new(comparer2.CompareTo, VersionRange.ComparisonType.Less | comparer2.Type);

                static void FlipDirection(ref VersionRange.VersionComparer comparer)
                {
                    var newCompare = (~comparer.Type & VersionRange.ComparisonType._DirectionMask) | (comparer.Type & ~VersionRange.ComparisonType._DirectionMask);
                    comparer = new(comparer.CompareTo, newCompare);
                }

                if (GetDirection(comparer1) == GetDirection(comparer2))
                {
                    // if they have the same direction, we should flip one of them
                    // which one we should flip depends on their direction
                    if (GetDirection(comparer1) == VersionRange.ComparisonType.Less)
                        FlipDirection(ref comparer1); // if they both point down, flip lower
                    else if (GetDirection(comparer1) == VersionRange.ComparisonType.Greater)
                        FlipDirection(ref comparer2); // if they both point up, flip upper
                }

                if (GetDirection(comparer1) == VersionRange.ComparisonType.Less
                    || GetDirection(comparer2) == VersionRange.ComparisonType.Greater)
                {
                    // if at this point they are pointing the wrong direction , they should both be swapped.
                    FlipDirection(ref comparer1);
                    FlipDirection(ref comparer2);
                }

                // the lower bound must match the upper bound
                if (!comparer1.Matches(comparer2))
                {
                    // if it doesn't, then we should add an equals
                    comparer1 = new(comparer1.CompareTo, comparer1.Type | VersionRange.ComparisonType.ExactEqual);
                }
                // the upper bound must match the lower bound
                if (!comparer2.Matches(comparer1))
                {
                    // if it doesn't, then we should add an equals
                    comparer2 = new(comparer2.CompareTo, comparer2.Type | VersionRange.ComparisonType.ExactEqual);
                }
            }

            // now we should have a pretty much valid pair of comparers
            var sb = new StringBuilder();
            if (!isSingle) sb = comparer1.ToString(sb).Append(' ');
            sb = comparer2.ToString(sb);

            return new(SR.Range_BoundedRegionNotClosed, Suggestion: sb.ToString(), Span: SpanFromReport(reports[start]));
        }
        #endregion

        private static GeneratedMessage ExtraInputMesage<T>(in MessageInfo msgs, ActionErrorReport<T> report)
            where T : struct
            => new(SR.ExtraInputAtEnd, Suggestion: "", Span: SpanFromReport(report));

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

        private static string FormatMessage(in MessageInfo msgs, GeneratedMessage? message)
        {
            message ??= new(SR.ParsingSuccessful);

            var msgString = message.Message;

            var suggestion = !string.IsNullOrEmpty(message.Suggestion) && message.ShowSuggestion
                ? SR.Suggestion.Format(message.Suggestion)
                : null;

            msgString = message.Span is { } span
                ? FormatMessageAtPosition(msgs.Text, span.Start, span.Length, msgString)
                : msgString;

            return msgString + "\n" + suggestion;
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
            _ = builder.AppendLine()
                .Append(' ', (int)position)
                .AppendLine(message);
        }
    }
}
