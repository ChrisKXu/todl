using System.IO;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public string Text { get; }

        public int Length => this.Text.Length;

        private SourceText(string text)
        {
            this.Text = text;
        }

        public static SourceText FromString(string text)
        {
            return new SourceText(text);
        }

        public static async Task<SourceText> FromFileAsync(string fileName)
        {
            return FromString(await File.ReadAllTextAsync(fileName));
        }

        public TextSpan GetTextSpan(int start, int length) => new(this, start, length);
    }
}
