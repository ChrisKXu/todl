using System;
using System.IO;

namespace Todl.Compiler.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public string FilePath { get; init; }
        public string Text { get; init; }

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
