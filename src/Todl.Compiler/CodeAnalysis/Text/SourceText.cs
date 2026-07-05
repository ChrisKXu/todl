using System;
using System.IO;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public string FilePath { get; init; }
        public string Text { get; init; }

        public int Length => this.Text.Length;

        public static SourceText FromString(string text)
            => new() { Text = text };

        public static SourceText FromFile(string filePath)
            => new()
            {
                FilePath = filePath,
                Text = File.ReadAllText(filePath)
            };

        public TextSpan GetTextSpan(int start, int length) => new(this, start, length);
        public string ToString(TextSpan span)
            => Text.Substring(span.Start, span.Length);

        public ReadOnlySpan<char> AsSpan(TextSpan span)
            => Text.AsSpan(span.Start, span.Length);
    }
}
