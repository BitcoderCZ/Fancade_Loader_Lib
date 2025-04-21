using System.CodeDom.Compiler;
using System.Globalization;

#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace FancadeLoaderLib.Runtime.Compiled.Utils;

internal static class IndentedTextWriterUtils
{
    public static IDisposable CurlyIndent(this IndentedTextWriter writer, string? openingLine = null, bool newLine = true)
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

            if (newLine)
            {
                writer.WriteLine();
            }
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

#if NET6_0_OR_GREATER
    public static void WriteInv(this TextWriter writer, ref InvariantInterpolatedStringHandler handler)
        => writer.Write(handler.ToStringAndClear());

    public static void WriteLineInv(this TextWriter writer, ref InvariantInterpolatedStringHandler handler)
        => writer.WriteLine(handler.ToStringAndClear());
#else
    public static void WriteInv(this IndentedTextWriter writer, FormattableString value)
        => writer.Write(FormattableString.Invariant(value));

    public static void WriteLineInv(this IndentedTextWriter writer, FormattableString value)
        => writer.WriteLine(FormattableString.Invariant(value));
#endif

#if NET6_0_OR_GREATER
    [InterpolatedStringHandler]
    public ref struct InvariantInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _handler;

        public InvariantInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture);
        }

        public InvariantInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> initialBuffer)
        {
            _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture, initialBuffer);
        }

        public void AppendLiteral(string value)
            => _handler.AppendLiteral(value);

        public void AppendFormatted<T>(T value)
            => _handler.AppendFormatted(value);

        public void AppendFormatted<T>(T value, string? format)
            => _handler.AppendFormatted(value, format);

        public void AppendFormatted<T>(T value, int alignment)
            => _handler.AppendFormatted(value, alignment);
            
        public void AppendFormatted<T>(T value, int alignment, string? format)
            => _handler.AppendFormatted(value, alignment, format);
            
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
            => _handler.AppendFormatted(value);

        public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);
            
        public void AppendFormatted(string? value)
            => _handler.AppendFormatted(value);

        public void AppendFormatted(string? value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object? value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);

        public string ToStringAndClear()
            => _handler.ToStringAndClear();
            
        public override string ToString()
            => _handler.ToString();
    }
#endif
}
