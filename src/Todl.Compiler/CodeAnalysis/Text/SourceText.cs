using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Todl.Compiler.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public string FilePath { get; init; }
        public string Text { get; init; }
        public int Version { get; init; }

        public int Length => this.Text.Length;

        private int[] _lineStarts;

        public int LineCount => GetLineMap().Length;

        public static SourceText FromString(string text)
            => new() { Text = text };

        public static SourceText FromFile(string filePath)
            => new()
            {
                FilePath = filePath,
                Text = File.ReadAllText(filePath)
            };

        /// <summary>
        /// Applies non-overlapping <paramref name="changes"/> against <see cref="Text"/> and
        /// returns a new SourceText with Version incremented. This is a naive O(n) rebuild,
        /// not incremental parsing - callers still fully reparse the result.
        /// </summary>
        public SourceText WithChanges(IReadOnlyList<TextChange> changes)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (changes.Count == 0)
                return this;

            // Fast path: WithChange (the common per-keystroke case) hands us exactly one
            // change - skip the sort/array allocation. Otherwise, defensively sort by Start
            // since callers may hand us changes out of order.
            IReadOnlyList<TextChange> ordered = changes.Count == 1
                ? changes
                : changes.OrderBy(change => change.Span.Start).ToArray();

            for (var i = 0; i < ordered.Count; i++)
            {
                var span = ordered[i].Span;
                if (span.Start < 0 || span.End > Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(changes), $"Change span {span} is out of bounds for text of length {Length}.");
                }

                if (i > 0 && span.Start < ordered[i - 1].Span.End)
                {
                    throw new ArgumentException("Text changes must not overlap.", nameof(changes));
                }
            }

            var builder = new StringBuilder(Length);
            var position = 0;
            foreach (var change in ordered)
            {
                builder.Append(Text, position, change.Span.Start - position);
                builder.Append(change.NewText);
                position = change.Span.End;
            }
            builder.Append(Text, position, Length - position);

            return new SourceText
            {
                FilePath = FilePath,
                Text = builder.ToString(),
                Version = Version + 1
            };
        }

        public SourceText WithChange(TextSpan span, string newText)
            => WithChanges(new[] { new TextChange(span, newText) });

        /// <summary>
        /// Base-behavior stub: a single full-span range when the texts differ. Sufficient for
        /// callers that always fully reparse; a real diff algorithm is a non-goal here.
        /// </summary>
        public IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (oldText.Text == Text)
                return Array.Empty<TextChangeRange>();

            return new[] { new TextChangeRange(new TextSpan(0, oldText.Length), Length) };
        }

        /// <remarks>No synchronization on first access; a redundant array allocation under concurrent first-read is benign — last write wins, and every result is valid.</remarks>
        private int[] GetLineMap()
        {
            if (_lineStarts != null)
                return _lineStarts;

            var text = Text;
            if (text.Length == 0)
                return _lineStarts = new int[1] { 0 };

            static bool IsBreak(ReadOnlySpan<char> span, int i) =>
                span[i] == '\n' || (span[i] == '\r' && (i + 1 >= span.Length || span[i + 1] != '\n'));

            // Count line breaks to size the array.
            var count = 1; // at least one line (line 0 starts at offset 0)
            for (int i = 0; i < text.Length; i++)
            {
                if (IsBreak(text, i))
                    count++;
            }

            _lineStarts = new int[count];
            _lineStarts[0] = 0;
            var idx = 1;
            for (int i = 0; i < text.Length && idx < count; i++)
            {
                if (IsBreak(text, i))
                    _lineStarts[idx++] = i + 1;
            }

            return _lineStarts;
        }

        public LinePosition GetLinePosition(int offset)
        {
            var map = GetLineMap();

            if (offset > Length)
                offset = Length;
            if (offset < 0)
                offset = 0;

            // Binary search for the line containing this offset.
            int lo = 0, hi = map.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (map[mid] <= offset)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            return new LinePosition(lo, offset - map[lo]);
        }

        public int GetOffset(LinePosition linePosition)
        {
            var map = GetLineMap();

            // Clamp line number
            var line = Math.Max(0, Math.Min(linePosition.Line, map.Length - 1));
            var startOfLine = map[line];
            var charNum = Math.Max(0, linePosition.Character);
            var offset = startOfLine + charNum;

            // Clamp to text length
            return Math.Min(offset, Length);
        }

        public string ToString(TextSpan span)
            => Text.Substring(span.Start, span.Length);

        public ReadOnlySpan<char> AsSpan(TextSpan span)
            => Text.AsSpan(span.Start, span.Length);
    }
}
