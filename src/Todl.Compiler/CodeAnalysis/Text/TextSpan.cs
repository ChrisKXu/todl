using System;
using System.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Text
{
    /// <summary>
    /// TextSpan represents a portion of the source text
    /// </summary>
    public struct TextSpan : IEquatable<TextSpan>, IEquatable<string>
    {
        public SourceText SourceText { get; }
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        internal TextSpan(SourceText sourceText, int start, int length)
        {
            SourceText = sourceText;
            Start = start;
            Length = length;
        }

        public override string ToString()
        {
            return SourceText.Text.Substring(Start, Length);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is string other)
            {
                return this.ToString().Equals(other);
            }

            if (obj is TextSpan textSpan)
            {
                return Equals(textSpan);
            }

            return base.Equals(obj);
        }

        public ReadOnlySpan<char> ToReadOnlyTextSpan()
        {
            return SourceText.Text.AsSpan(Start, Length);
        }

        public static TextSpan FromTextSpans(TextSpan start, TextSpan end)
        {
            Debug.Assert(start.SourceText == end.SourceText);

            return start.SourceText.GetTextSpan(start.Start, end.End - start.Start);
        }

        public bool Equals(TextSpan other)
        {
            return SourceText == other.SourceText
                && Start == other.Start
                && Length == other.Length;
        }

        public bool Equals(string other)
        {
            return ToReadOnlyTextSpan() == other.AsSpan();
        }
    }
}
