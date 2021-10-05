using System;
using System.Collections.Generic;
using Hive.Utilities;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning.Parsing
{
    /// <summary>
    /// An error action reported while parsing a version or version range.
    /// </summary>
    /// <typeparam name="TAction">The action type being used.</typeparam>
    public struct ActionErrorReport<TAction> : IEquatable<ActionErrorReport<TAction>>
        where TAction : struct
    {
        /// <summary>
        /// The action being reported.
        /// </summary>
        public TAction Action { get; }
        /// <summary>
        /// The offset into the input text that the error was reported at.
        /// </summary>
        public long TextOffset { get; }
        /// <summary>
        /// The length of input text associated with this error.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Constructs an <see cref="ActionErrorReport{TAction}"/> from an action and text position.
        /// </summary>
        /// <param name="action">The action being reported.</param>
        /// <param name="offset">The position in the text.</param>
        /// <param name="length">The length of the section of text associated with the action.</param>
        public ActionErrorReport(TAction action, long offset, long length)
            => (Action, TextOffset, Length) = (action, offset, length);

        /// <summary>
        /// Compares two <see cref="ActionErrorReport{TAction}"/>s for equality.
        /// </summary>
        /// <param name="left">The first report to compare.</param>
        /// <param name="right">The second report to compare.</param>
        /// <returns><see langword="true"/> if they are equal, <see langword="false"/> othwerwise.</returns>
        public static bool operator ==(ActionErrorReport<TAction> left, ActionErrorReport<TAction> right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="ActionErrorReport{TAction}"/>s for inequality.
        /// </summary>
        /// <param name="left">The first report to compare.</param>
        /// <param name="right">The second report to compare.</param>
        /// <returns><see langword="true"/> if they are not equal, <see langword="false"/> othwerwise.</returns>
        public static bool operator !=(ActionErrorReport<TAction> left, ActionErrorReport<TAction> right)
            => !(left == right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is ActionErrorReport<TAction> report
            && Equals(report);

        /// <inheritdoc/>
        public bool Equals(ActionErrorReport<TAction> other)
            => EqualityComparer<TAction>.Default.Equals(Action, other.Action)
            && TextOffset == other.TextOffset
            && Length == other.Length;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Action, TextOffset, Length);
    }

    /// <summary>
    /// A type for recording parser errors in a memory-efficient manner.
    /// </summary>
    /// <remarks>
    /// If this type is default constructed, errors will not be reported.
    /// </remarks>
    /// <typeparam name="TAction">The action type to use for reports.</typeparam>
    public ref struct ParserErrorState<TAction>
        where TAction : struct
    {
        /// <summary>
        /// Gets whether or not this instance is reporting errors.
        /// </summary>
        public bool ReportErrors { get; internal set; }
        /// <summary>
        /// Gets the full input text associated with the parse this object is for.
        /// </summary>
        public StringPart InputText { get; internal set; }

        /// <summary>
        /// Constructs a new <see cref="ParserErrorState{TAction}"/> with the specified full text,
        /// and enables error reporting for this instance.
        /// </summary>
        /// <param name="text"></param>
        public ParserErrorState(in StringPart text)
        {
            ReportErrors = true;
            InputText = text;
            reports = default;
            reports.Reserve(16);
        }

        private ArrayBuilder<ActionErrorReport<TAction>> reports;

        private static long GetTextOffset(in StringPart start, in StringPart end)
#if !NETSTANDARD2_0
        {
            unsafe
            {
                fixed (char* istart = start)
                fixed (char* iloc = end)
                {
                    if (iloc == null)
                        return start.Length; // this happens when location is empty
                    return iloc - istart;
                }
            }
        }
#else
            => end.Start - start.Start;
#endif
        /// <summary>
        /// Reports a parser error.
        /// </summary>
        /// <param name="action">The error action.</param>
        /// <param name="textLocation">The substring of the input text that starts at the error location.</param>
        /// <param name="endTextLocation">A substring of the input text that starts after the end of the error segment.</param>
        public void Report(TAction action, in StringPart textLocation, in StringPart endTextLocation)
        {
            if (!ReportErrors) return;

            reports.Add(new(action, GetTextOffset(InputText, textLocation), GetTextOffset(textLocation, endTextLocation)));
        }
        /// <summary>
        /// Reports a parser error.
        /// </summary>
        /// <param name="action">The error action.</param>
        /// <param name="textLocation">The substring of the input text that starts at the error location.</param>
        /// <param name="length">The length of the region associated with the action.</param>
        public void Report(TAction action, in StringPart textLocation, long length)
        {
            if (!ReportErrors) return;

            reports.Add(new(action, GetTextOffset(InputText, textLocation), length));
        }
        /// <summary>
        /// Reports a parser error.
        /// </summary>
        /// <param name="action">The error action.</param>
        /// <param name="textLocation">The offset into the string that the error ocurred.</param>
        /// <param name="length">The length of the region associated with the action.</param>
        public void Report(TAction action, long textLocation, long length)
        {
            if (!ReportErrors) return;

            reports.Add(new(action, textLocation, length));
        }

        /// <summary>
        /// Imports the errors from some other <see cref="ParserErrorState{TAction2}"/>, using the specified converter.
        /// </summary>
        /// <typeparam name="TAction2">The action type of the other error state to import.</typeparam>
        /// <param name="state">The other state to import errors from.</param>
        /// <param name="convert">The delegate to use to convert action types.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="convert"/> is <see langword="null"/>.</exception>
        // TODO: can I do this in a better way than via a pesistent delegate?
        public void FromState<TAction2>(ref ParserErrorState<TAction2> state, Func<TAction2, TAction> convert)
            where TAction2 : struct
        {
            if (!ReportErrors) return;

            if (convert is null)
                throw new ArgumentNullException(nameof(convert));
            for (var i = 0; i < state.reports.Count; i++)
            {
                Report(convert(state.reports[i].Action), state.reports[i].TextOffset, state.reports[i].Length);
            }
            state.Dispose();
        }

        /// <summary>
        /// Gets all current error reports.
        /// </summary>
        /// <returns>An array of all error reports stored in this error state.</returns>
        public ActionErrorReport<TAction>[] ToArray() => reports.ToArray();

        /// <summary>
        /// Disposes and cleans up the memory used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!ReportErrors) return;

            reports.Dispose();
        }

    }
}
