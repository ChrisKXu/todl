using System.CodeDom.Compiler;

namespace Todl.Compiler.SourceGenerators;

public static class IndentedTextWriterExtensions
{
    public static void BeginCurlyBrace(this IndentedTextWriter indentedTextWriter)
    {
        indentedTextWriter.WriteLine("{");
        ++indentedTextWriter.Indent;
    }

    public static void EndCurlyBrace(this IndentedTextWriter indentedTextWriter, bool appendEndingSemilcolon = false)
    {
        --indentedTextWriter.Indent;
        if (appendEndingSemilcolon)
        {
            indentedTextWriter.WriteLine("};");
        }
        else
        {
            indentedTextWriter.WriteLine("}");
        }
    }
}
