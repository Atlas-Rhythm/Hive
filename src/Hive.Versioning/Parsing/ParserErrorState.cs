using System;
using System.Collections.Generic;
using Hive.Utilities;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning
{

    public struct ActionErrorReport<TAction> : IEquatable<ActionErrorReport<TAction>>
        where TAction : struct
    {
        public TAction Action { get; }
        public long TextOffset { get; }

        public ActionErrorReport(TAction action, long offset)
            => (Action, TextOffset) = (action, offset);

        public static bool operator ==(ActionErrorReport<TAction> left, ActionErrorReport<TAction> right)
            => left.Equals(right);

        public static bool operator !=(ActionErrorReport<TAction> left, ActionErrorReport<TAction> right)
            => !(left == right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is ActionErrorReport<TAction> report
            && Equals(report);

        public bool Equals(ActionErrorReport<TAction> other)
            => EqualityComparer<TAction>.Default.Equals(Action, other.Action)
            && TextOffset == other.TextOffset;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Action, TextOffset);
    }

    public ref struct ParserErrorState<TAction>
        where TAction : struct
    {
        public bool ReportErrors { get; internal set; }

        public StringPart InputText { get; internal set; }

        public ParserErrorState(in StringPart text)
        {
            ReportErrors = true;
            InputText = text;
            reports = default;
        }

        private ArrayBuilder<ActionErrorReport<TAction>> reports;

        private long GetTextOffset(in StringPart location)
#if !NETSTANDARD2_0
        {
            unsafe
            {
                fixed (char* istart = InputText)
                fixed (char* iloc = location)
                {
                    if (iloc == null)
                        return InputText.Length; // this happens when location is empty
                    return iloc - istart;
                }
            }
        }
#else
            => location.Start - InputText.Start;
#endif

        public void Report(TAction action, in StringPart textLocation)
        {
            if (!ReportErrors) return;

            reports.Add(new(action, GetTextOffset(textLocation)));
        }
        public void Report(TAction action, long textLocation)
        {
            if (!ReportErrors) return;

            reports.Add(new(action, textLocation));
        }

        // TODO: can I do this in a better way than via a pesistent delegate?
        public void FromState<TAction2>(ref ParserErrorState<TAction2> state, Func<TAction2, TAction> convert)
            where TAction2 : struct
        {
            if (!ReportErrors) return;

            if (convert is null)
                throw new ArgumentNullException(nameof(convert));
            for (var i = 0; i < state.reports.Count; i++)
            {
                Report(convert(state.reports[i].Action), state.reports[i].TextOffset);
            }
            state.Dispose();
        }

        public void Dispose()
        {
            if (!ReportErrors) return;

            reports.Dispose();
        }

    }
}
