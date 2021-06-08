using System;

namespace Todl.CodeAnalysis.Text
{
    /// <summary>
    /// TextSpan represents a portion of the source text
    /// </summary>
    public struct TextSpan
    {
        public SourceText SourceText { get; }
        public int Start { get; }
        public int Length { get; }

        internal TextSpan(SourceText sourceText, int start, int length)
        {
            this.SourceText = sourceText;
            this.Start = start;
            this.Length = length;
        }

        public override string ToString()
        {
            return this.SourceText.Text.Substring(this.Start, this.Length);
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

            return base.Equals(obj);
        }

        public ReadOnlySpan<char> ToReadOnlyTextSpan()
        {
            return this.SourceText.Text.ToCharArray(this.Start, this.Length);
        }
    }
}
