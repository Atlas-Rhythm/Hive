using System;
using Hive.Utilities;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning
{
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

        public struct ActionErrorReport
        {
            public TAction Action { get; }

            public long TextOffset { get; }

            public ActionErrorReport(TAction action, long offset)
                => (Action, TextOffset) = (action, offset);
        }

        private readonly ArrayBuilder<ActionErrorReport> reports;

        private long GetTextOffset(in StringPart location)
#if !NETSTANDARD2_0
        {
            unsafe
            {
                fixed (char* istart = InputText)
                fixed (char* iloc = location)
                    return iloc - istart;
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
        public void FromState<TAction2>(in ParserErrorState<TAction2> state, Func<TAction2, TAction> convert)
            where TAction2 : struct
        {
            for (var i = 0; i < state.reports.Count; i++)
            {
                Report(convert(state.reports[i].Action), state.reports[i].TextOffset);
            }
        }

        public void Dispose()
        {
            reports.Dispose();
        }

    }
}
