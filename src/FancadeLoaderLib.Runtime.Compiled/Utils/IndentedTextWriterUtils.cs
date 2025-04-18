using System.CodeDom.Compiler;

namespace FancadeLoaderLib.Runtime.Compiled.Utils;

internal static class IndentedTextWriterUtils
{
    public static IDisposable CurlyIndent(this IndentedTextWriter writer, string? openingLine = null)
    {
        if (openingLine is not null)
        {
            writer.WriteLine(openingLine);
        }

        writer.WriteLine('{');

        writer.Indent++;

        return new Disposable(() =>
        {
            writer.Indent--;
            writer.WriteLine('}');
            writer.WriteLine();
        });
    }

    public static void CurlyWriteLine(this IndentedTextWriter writer, string text)
    {
        writer.WriteLine('{');
        writer.Indent++;

        writer.WriteLine(text);

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }
}
