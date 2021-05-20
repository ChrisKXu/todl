using System.IO;
using System.Threading.Tasks;

namespace Todl.CodeAnalysis
{
    public sealed class SourceText
    {
        private readonly string text;

        public string Text => text;

        public int Length => text.Length;

        private SourceText(string text)
        {
            this.text = text;
        }

        public static SourceText FromString(string text)
        {
            return new SourceText(text);
        }

        public static async Task<SourceText> FromFileAsync(string fileName)
        {
            return FromString(await File.ReadAllTextAsync(fileName));
        }
    }
}